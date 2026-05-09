using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

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

    internal EventMailPool<TEvent> GetOrCreateEventMailPool<TEvent>()
        where TEvent : struct
    {
        int eventTypeId = EventTypeId<TEvent>.Id;
        EnsureEventMailPoolCapacity(eventTypeId);

        if (_eventMailPoolsByEventId[eventTypeId] is not EventMailPool<TEvent> pool)
        {
            pool = new EventMailPool<TEvent>();
            _eventMailPoolsByEventId[eventTypeId] = pool;
        }

        return pool;
    }

    internal ActorEventFastCache<TEvent> GetOrCreateFastCache<TEvent>()
        where TEvent : struct
    {
        int eventTypeId = EventTypeId<TEvent>.Id;
        EnsureFastCacheCapacity(eventTypeId);

        if (_fastCachesByEventId[eventTypeId] is not ActorEventFastCache<TEvent> cache)
        {
            cache = new ActorEventFastCache<TEvent>();
            _fastCachesByEventId[eventTypeId] = cache;
        }

        return cache;
    }

    internal void InvalidateAllFastCaches<TEvent>()
        where TEvent : struct
    {
        int eventTypeId = EventTypeId<TEvent>.Id;
        if ((uint)eventTypeId >= (uint)_fastCachesByEventId.Length)
        {
            return;
        }

        if (_fastCachesByEventId[eventTypeId] is ActorEventFastCache<TEvent> cache)
        {
            cache.InvalidateAll();
        }
    }

    internal PostResult PostQueuedGrowDirect<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        ActorMailOptions options)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        EventMailPool<TEvent> pool = GetOrCreateEventMailPool<TEvent>();

        if (mail.BufferId == 0)
        {
            mail.BufferId = pool.Rent(options.InitialCapacity);
            mail.Head = 0;
            mail.Tail = 0;
            mail.Count = 0;
            mail.Capacity = pool.GetCapacity(mail.BufferId);
        }

        if (mail.Count >= mail.Capacity)
        {
            int nextCapacity = Math.Min(
                Math.Max(mail.Capacity * Math.Max(options.GrowFactor, 2), mail.Capacity + 1),
                options.MaxCapacity);
            if (nextCapacity <= mail.Capacity)
            {
                return PostResult.Failure(
                    ActorPostStatus.MailFullRejected,
                    "Actor mail reached max capacity.",
                    PostFailureKind.MailboxFull);
            }

            pool.Resize(mail.BufferId, mail.Head, mail.Count, nextCapacity);
            mail.Head = 0;
            mail.Tail = mail.Count;
            mail.Capacity = nextCapacity;
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
            dirtySlots.AddIfNotExists(slotIndex);
            _dirtyEventBuckets.AddIfNotExists(bucketIndex);
        }

        return PostResult.Success;
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

    private void EnsureEventMailPoolCapacity(int eventTypeId)
    {
        if ((uint)eventTypeId < (uint)_eventMailPoolsByEventId.Length)
        {
            return;
        }

        int newSize = _eventMailPoolsByEventId.Length == 0 ? 4 : _eventMailPoolsByEventId.Length;
        while (newSize <= eventTypeId)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _eventMailPoolsByEventId, newSize);
    }

    private void EnsureFastCacheCapacity(int eventTypeId)
    {
        if ((uint)eventTypeId < (uint)_fastCachesByEventId.Length)
        {
            return;
        }

        int newSize = _fastCachesByEventId.Length == 0 ? 4 : _fastCachesByEventId.Length;
        while (newSize <= eventTypeId)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _fastCachesByEventId, newSize);
    }
}
