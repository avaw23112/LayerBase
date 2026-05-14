using System.Runtime.CompilerServices;
using System.Diagnostics;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    internal EventPostState<TEvent> GetOrCreateEventPostState<TEvent>(ActorEventPostPlan<TEvent> plan)
        where TEvent : struct
    {
        EventPostState<TEvent>? existing = EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);
        if (existing != null)
        {
            return existing;
        }

        EventMailPool<TEvent> pool = GlobalEventMailPools.GetOrCreate<TEvent>(plan.MailOptions);
        EventPostRow<TEvent>[] rows = CreateRows<TEvent>(_archetypes.Length);
        var state = new EventPostState<TEvent>(
            routeCode: plan.RouteCode,
            pool: pool,
            options: plan.MailOptions,
            rowsByArchetype: rows);

        EventPostRuntime<TEvent>.BindWorld(this, state);
        _eventPostRuntimeUnbinders.Add(() => EventPostRuntime<TEvent>.UnbindWorld(RuntimeIndex));
        return state;
    }

    internal void RegisterEventPostRow<TEvent>(
        int                           archetypeId,
        EventMail<TEvent>[]           mails,
        DirtySlotList                 dirtySlots,
        int                           bucketIndex,
        in ActorEventPostPlan<TEvent> plan,
        int[]                         generations,
        bool[]                        actorExists)
        where TEvent : struct
    {
        EventPostState<TEvent> state = GetOrCreateEventPostState(plan);
        EnsureRowsCapacity(ref state.RowsByArchetype, archetypeId);
        state.RowsByArchetype[archetypeId] = new EventPostRow<TEvent>(
            mails,
            dirtySlots,
            bucketIndex,
            generations,
            actorExists);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetPhysicalRowWithGeneration<TEvent>(
        in  ActorId               actorId,
        EventPostState<TEvent>   state,
        out EventPostRow<TEvent> row,
        out int                  slotIndex)
        where TEvent : struct
    {
        EventPostRow<TEvent>[] rows = state.RowsByArchetype;
        int archetypeId = actorId.ArchetypeId;
        if ((uint)archetypeId >= (uint)rows.Length)
        {
            row = default;
            slotIndex = default;
            return false;
        }
        row = rows[actorId.ArchetypeId];
        slotIndex = actorId.SlotIndex;
        return row.Generations[slotIndex] == actorId.Generation && row.ActorExists[slotIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostQueuedGrowCore<TEvent>(
        int                    slotIndex,
        in TEvent              value,
        in EventMail<TEvent>[] mails,
        DirtySlotList          dirtySlots,
        int                    bucketIndex,
        EventMailPool<TEvent>  pool,
        in ActorMailOptions    options)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        EnsureMailAllocated(ref mail, pool, options.InitialCapacity);
        if (mail.Count >= mail.Capacity && !pool.TryGrow(ref mail))
        {
            PostResult growFailure = HandleGrowFailure(ref mail, in value,options);
            if (!growFailure.IsSuccess || !growFailure.CountsAsPending)
            {
                return;
            }
        }
        WriteQueued(ref mail, in value, dirtySlots, slotIndex, bucketIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostQueuedRejectNewCore<TEvent>(
        int                    slotIndex,
        in TEvent              value,
        in EventMail<TEvent>[] mails,
        DirtySlotList          dirtySlots,
        int                    bucketIndex,
        EventMailPool<TEvent>  pool,
        in ActorMailOptions    options)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        EnsureMailAllocated(ref mail, pool, options.InitialCapacity);
        if (mail.Count >= mail.Capacity)
        {
            return;
        }
        WriteQueued(ref mail, in value, dirtySlots, slotIndex, bucketIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostQueuedDropOldestCore<TEvent>(
        int                    slotIndex,
        in TEvent              value,
        in EventMail<TEvent>[] mails,
        DirtySlotList          dirtySlots,
        int                    bucketIndex,
        EventMailPool<TEvent>  pool,
        in ActorMailOptions    options)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        EnsureMailAllocated(ref mail, pool, options.InitialCapacity);

        if (mail.Count >= mail.Capacity)
        {
            TEvent[] buffer = mail.Buffer!;
            mail.Head++;
            if (mail.Head == mail.Capacity)
            {
                mail.Head = 0;
            }

            buffer[mail.Tail] = value;
            mail.Tail++;
            if (mail.Tail == mail.Capacity)
            {
                mail.Tail = 0;
            }

            dirtySlots.Mark(slotIndex);
            return;
        }
        WriteQueued(ref mail, in value, dirtySlots, slotIndex, bucketIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostLatestCore<TEvent>(
        int                    slotIndex,
        in TEvent              value,
        in EventMail<TEvent>[] mails,
        DirtySlotList          dirtySlots,
        int                    bucketIndex,
        EventMailPool<TEvent>  pool)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        bool wasEmpty = mail.Count == 0;
        if (mail.BufferId == 0)
        {
            mail.SingleValue = value;
            mail.Head = 0;
            mail.Tail = 0;
            mail.Count = 1;
            mail.Capacity = 0;

            if (wasEmpty)
            {
                dirtySlots.Mark(slotIndex);
                _dirtyEventBuckets.Mark(bucketIndex);
            }

            return;
        }

        mail.Buffer![0] = value;
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 1;

        if (wasEmpty)
        {
            dirtySlots.Mark(slotIndex);
            _dirtyEventBuckets.Mark(bucketIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostDirtyCore<TEvent>(
        int                    slotIndex,
        in TEvent              value,
        in EventMail<TEvent>[] mails,
        DirtySlotList          dirtySlots,
        int                    bucketIndex,
        EventMailPool<TEvent>  pool)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        if (mail.Count > 0)
        {
            return;
        }

        if (mail.BufferId == 0)
        {
            mail.SingleValue = value;
            mail.Head = 0;
            mail.Tail = 0;
            mail.Count = 1;
            mail.Capacity = 0;
            dirtySlots.Mark(slotIndex);
            _dirtyEventBuckets.Mark(bucketIndex);
            return;
        }

        mail.Buffer![0] = value;
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 1;
        dirtySlots.Mark(slotIndex);
        _dirtyEventBuckets.Mark(bucketIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureMailAllocated<TEvent>(ref EventMail<TEvent> mail, EventMailPool<TEvent> pool,
                                                    int                   initialCapacity)
        where TEvent : struct
    {
        if (mail.Buffer != null)
        {
            return;
        }
        EventMailRentResult<TEvent> rent = pool.RentWithBuffer(initialCapacity);
        mail.BufferId = rent.BufferId;
        mail.Buffer = rent.Buffer;
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 0;
        mail.Capacity = rent.Buffer.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteQueued<TEvent>(
        ref EventMail<TEvent> mail,
        in  TEvent            value,
        DirtySlotList         dirtySlots,
        int                   slotIndex,
        int                   bucketIndex)
        where TEvent : struct
    {
        TEvent[] buffer = mail.Buffer!;
        buffer[mail.Tail] = value;
        mail.Tail++;
        mail.Count++;

        if (mail.Tail == mail.Capacity)
        {
            mail.Tail = 0;
        }

        if (mail.Count == 1)
        {
            dirtySlots.Mark(slotIndex);
            _dirtyEventBuckets.Mark(bucketIndex);
        }
    }

    private static PostResult HandleGrowFailure<TEvent>(
        ref EventMail<TEvent> mail,
        in  TEvent            value,
        ActorMailOptions      options)
        where TEvent : struct
    {
        switch (options.GrowFailurePolicy)
        {
            case ActorMailFullPolicy.RejectNew:
                return PostResult.Failure(
                    ActorPostStatus.MailFullRejected,
                    PostFailureKind.MailboxFull);

            case ActorMailFullPolicy.DropOldest:
                mail.Head++;
                if (mail.Head == mail.Capacity)
                {
                    mail.Head = 0;
                }

                mail.Count--;
                return PostResult.Success;

            case ActorMailFullPolicy.DropNewest:
                mail.Tail--;
                if (mail.Tail < 0)
                {
                    mail.Tail = mail.Capacity - 1;
                }

                mail.Count--;
                mail.Buffer![mail.Tail] = value;
                mail.Count++;
                return PostResult.Success;

            case ActorMailFullPolicy.OverwriteLatest:
                if (mail.Count > 0)
                {
                    int latestIndex = ActorMailCapacity.Wrap(mail.Head + mail.Count - 1, mail.Capacity);
                    mail.Buffer![latestIndex] = value;
                    return PostResult.Coalesced();
                }

                return PostResult.Success;

            default:
                return PostResult.Failure(
                    ActorPostStatus.MailFullRejected,
                    PostFailureKind.MailboxFull);
        }
    }

    private static EventPostRow<TEvent>[] CreateRows<TEvent>(int archetypeCapacity)
        where TEvent : struct
    {
        EventPostRow<TEvent> invalid = new EventPostRow<TEvent>(
            Array.Empty<EventMail<TEvent>>(),
            DirtySlotList.Empty,
            -1,
            Array.Empty<int>(),
            Array.Empty<bool>());
        
        var rows = new EventPostRow<TEvent>[Math.Max(archetypeCapacity, 1)];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = invalid;
        }

        return rows;
    }


    private static void EnsureRowsCapacity<TEvent>(
        ref EventPostRow<TEvent>[] rows,
        int                        archetypeId)
        where TEvent : struct
    {
        if ((uint)archetypeId < (uint)rows.Length)
        {
            return;
        }

        int oldSize = rows.Length;
        int newSize = rows.Length == 0 ? 4 : rows.Length;
        while (newSize <= archetypeId)
        {
            newSize <<= 1;
        }

        Array.Resize(ref rows, newSize);
        EventPostRow<TEvent> invalid = new EventPostRow<TEvent>(
            Array.Empty<EventMail<TEvent>>(),
            DirtySlotList.Empty,
            -1,
            Array.Empty<int>(),
            Array.Empty<bool>());
        for (int i = oldSize; i < newSize; i++)
        {
            rows[i] = invalid;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static PostResult BuildEventNotSupportedCold<TEvent>()
        where TEvent : struct
    {
        return PostResult.Failure(
            ActorPostStatus.EventNotSupported,
            PostFailureKind.UnsupportedEvent);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult BuildRouteUnsupportedCold<TEvent>()
        where TEvent : struct
    {
        return PostResult.Failure(
            ActorPostStatus.EventNotSupported,
            PostFailureKind.UnsupportedEvent);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult BuildPostFailureCold(in ActorId actorId)
    {
        return PostResult.Failure(
            ActorPostStatus.PhysicalTargetInvalid,
            PostFailureKind.PhysicalTargetInvalid);
    }
    
}