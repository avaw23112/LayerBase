using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int AllocateStorageRouteId()
    {
        int routeId = _storagesByRouteId.Length;
        Array.Resize(ref _storagesByRouteId, routeId + 1);
        return routeId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindStorageRoute(int routeId, TypedStorageRuntime storage)
    {
        _storagesByRouteId[routeId] = storage;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ActorFastState GetFastStateRef(int fastIndex)
    {
        return ref _fastStates[fastIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int AllocateFastIndex()
    {
        if (_fastIndexFreeList.TryPop(out int fastIndex))
        {
            EnsureFastStateCapacity(fastIndex);
            return fastIndex;
        }

        fastIndex = _fastStates.Length;
        Array.Resize(ref _fastStates, fastIndex + 1);
        return fastIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindFastState(
        int fastIndex,
        int slotIndex,
        int generation,
        int storageRouteId)
    {
        EnsureFastStateCapacity(fastIndex);
        _fastStates[fastIndex].Bind(slotIndex, generation, storageRouteId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkFastStateDead(int fastIndex)
    {
        if ((uint)fastIndex >= (uint)_fastStates.Length)
        {
            return;
        }

        _fastStates[fastIndex].MarkDead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReleaseFastIndex(int fastIndex)
    {
        if ((uint)fastIndex >= (uint)_fastStates.Length)
        {
            return;
        }

        _fastStates[fastIndex].MarkDead();
        _fastIndexFreeList.Push(fastIndex);
    }

    internal EventMailPool<TEvent> GetOrCreateEventMailPoolCold<TEvent>()
        where TEvent : struct
    {
        if (!ActorEventRuntime<TEvent>.TryGetMailPool(this, out EventMailPool<TEvent>? pool)
            || pool == null)
        {
            ActorMailOptions options = ResolveMailOptions(LayerBase.Core.Event.EventTypeId<TEvent>.Id);
            pool = new EventMailPool<TEvent>(options);
            ActorEventRuntime<TEvent>.BindWorld(this, pool);
        }

        return pool;
    }

    internal ActorEventFastCache<TEvent> GetOrCreateFastCacheCold<TEvent>()
        where TEvent : struct
    {
        if (!ActorEventRuntime<TEvent>.TryGetFastCache(this, out ActorEventFastCache<TEvent>? cache)
            || cache == null)
        {
            ActorMailOptions options = ResolveMailOptions(LayerBase.Core.Event.EventTypeId<TEvent>.Id);
            EventMailPool<TEvent> pool = new(options);
            cache = new ActorEventFastCache<TEvent>(pool);
            ActorEventRuntime<TEvent>.BindWorld(this, cache, pool);
        }

        return cache;
    }

    internal void InvalidateAllFastCaches<TEvent>()
        where TEvent : struct
    {
        if (ActorEventRuntime<TEvent>.TryGetFastCache(this, out ActorEventFastCache<TEvent>? cache)
            && cache != null)
        {
            cache.InvalidateAll();
        }
    }

    internal bool PostQueuedGrowFastNoResult<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        EventMailPool<TEvent> pool)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];

        if (mail.BufferId == 0)
        {
            mail.BufferId = pool.RentInitial();
            mail.Head = 0;
            mail.Tail = 0;
            mail.Count = 0;
            mail.Capacity = pool.GetCapacity(mail.BufferId);
        }

        if (mail.Count >= mail.Capacity)
        {
            if (!pool.TryGrow(ref mail))
            {
                return false;
            }
        }

        pool.Write(mail.BufferId, mail.Tail, in value);
        mail.Tail++;
        if (mail.Tail == mail.Capacity)
        {
            mail.Tail = 0;
        }

        mail.Count++;
        if (mail.Count == 1)
        {
            dirtySlots.Mark(slotIndex);
            _dirtyEventBuckets.Mark(bucketIndex);
        }

        return true;
    }

    internal void RegisterEventPostRow<TEvent>(
        int archetypeId,
        EventMail<TEvent>[] mails,
        EventMailPool<TEvent> pool,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[] generations)
        where TEvent : struct
    {
        EventPostRow<TEvent>[] rows = GetOrCreateRowsByArchetypeCold<TEvent>();
        if ((uint)archetypeId >= (uint)rows.Length)
        {
            int newSize = rows.Length == 0 ? 4 : rows.Length;
            while (newSize <= archetypeId)
            {
                newSize <<= 1;
            }

            Array.Resize(ref rows, newSize);
        }

        rows[archetypeId] = new EventPostRow<TEvent>(
            mails,
            pool,
            dirtySlots,
            bucketIndex,
            generations);

        EventPostRuntime<TEvent>.BindWorld(this, rows);
    }

    internal bool TryBindHotFastCache<TEvent>(
        int fastIndex,
        int version,
        int generation)
        where TEvent : struct
    {
        if ((uint)fastIndex >= (uint)_fastStates.Length)
        {
            return false;
        }

        ref ActorFastState state = ref _fastStates[fastIndex];
        if (state.Version != version
            || state.Generation != generation
            || state.StorageRouteId < 0
            || (uint)state.StorageRouteId >= (uint)_storagesByRouteId.Length)
        {
            return false;
        }

        TypedStorageRuntime? storage = _storagesByRouteId[state.StorageRouteId];
        if (storage == null)
        {
            return false;
        }

        return storage.TryBindHotFastCache<TEvent>(this, fastIndex, version, state.SlotIndex, generation);
    }

    private void EnsureFastStateCapacity(int fastIndex)
    {
        if ((uint)fastIndex < (uint)_fastStates.Length)
        {
            return;
        }

        int newSize = _fastStates.Length == 0 ? 4 : _fastStates.Length;
        while (newSize <= fastIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _fastStates, newSize);
    }

    private EventPostRow<TEvent>[] GetOrCreateRowsByArchetypeCold<TEvent>()
        where TEvent : struct
    {
        if (EventPostRuntime<TEvent>.TryGetRows(this, out EventPostRow<TEvent>[]? rows)
            && rows != null)
        {
            return rows;
        }

        rows = new EventPostRow<TEvent>[Math.Max(_archetypes.Length, 4)];
        EventPostRuntime<TEvent>.BindWorld(this, rows);
        return rows;
    }
}
