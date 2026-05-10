using System.Runtime.CompilerServices;
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
        int archetypeId,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[]? postableGenerations,
        ActorEventPostPlan<TEvent> plan)
        where TEvent : struct
    {
        EventPostState<TEvent> state = GetOrCreateEventPostState(plan);
        EnsureRowsCapacity(ref state.RowsByArchetype, archetypeId);
        state.RowsByArchetype[archetypeId] = new EventPostRow<TEvent>(
            mails,
            dirtySlots,
            bucketIndex,
            postableGenerations);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PostResult PostCompiled<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        byte routeCode = state.RouteCode;
        if (routeCode == ActorPostRouteCode.QueuedGrowPhysicalSafe)
        {
            return PostQueuedGrowPhysicalSafe(actorId, in value, state);
        }

        if (routeCode == ActorPostRouteCode.Disabled)
        {
            return BuildEventNotSupportedCold<TEvent>();
        }

        return PostToNonDefaultCold(actorId, in value, state, routeCode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool PostFast<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        EventPostState<TEvent>? state = EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);
        if (state == null || state.RouteCode == ActorPostRouteCode.Disabled)
        {
            return false;
        }

        return PostCompiled(actorId, in value, state).IsSuccess;
    }



    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult PostToNonDefaultCold<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state,
        byte routeCode)
        where TEvent : struct
    {
        byte validation = (byte)(routeCode & ActorPostRouteCode.ValidationMask);
        byte writeMode = (byte)(routeCode & ActorPostRouteCode.WriteModeMask);

        if (validation == ActorPostRouteCode.ValidationPostableStamp)
        {
            return PostByWriteModePostableStampCold(actorId, in value, state, writeMode);
        }
        if (validation == ActorPostRouteCode.ValidationPhysicalSafe)
        {
            return PostByWriteModePhysicalSafeCold(actorId, in value, state, writeMode);
        }

        return BuildRouteUnsupportedCold<TEvent>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult PostByWriteModePhysicalSafeCold<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state,
        byte writeMode)
        where TEvent : struct
    {
        if (ActorPostRouteCode.WriteQueuedGrow == writeMode)
        {
            return PostQueuedGrowPhysicalSafe(actorId, in value, state);
        }
        if (ActorPostRouteCode.WriteQueuedRejectNew == writeMode)
        {
            return PostQueuedRejectNewPhysicalSafe(actorId, in value, state);
        }
        if (ActorPostRouteCode.WriteQueuedDropOldest == writeMode)
        {
            return PostQueuedDropOldestPhysicalSafe(actorId, in value, state);
        }
        if (ActorPostRouteCode.WriteLatest == writeMode)
        {
            return PostLatestPhysicalSafe(actorId, in value, state);
        }
        if (ActorPostRouteCode.WriteDirty == writeMode)
        {
            return PostDirtyPhysicalSafe(actorId, in value, state);
        }
        return BuildRouteUnsupportedCold<TEvent>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult PostByWriteModePostableStampCold<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state,
        byte writeMode)
        where TEvent : struct
    {
        if (ActorPostRouteCode.WriteQueuedGrow == writeMode)
        {
            return PostQueuedGrowPostableStamp(actorId, in value, state);
        }
        if (ActorPostRouteCode.WriteQueuedRejectNew == writeMode)
        {
            return PostQueuedRejectNewPostableStamp(actorId, in value, state);
        }
        if (ActorPostRouteCode.WriteQueuedDropOldest == writeMode)
        {
            return PostQueuedDropOldestPostableStamp(actorId, in value, state);
        }
        if (ActorPostRouteCode.WriteLatest == writeMode)
        {
            return PostLatestPostableStamp(actorId, in value, state);
        }
        if (ActorPostRouteCode.WriteDirty == writeMode)
        {
            return PostDirtyPostableStamp(actorId, in value, state);
        }
        return BuildRouteUnsupportedCold<TEvent>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedGrowPhysicalSafe<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPhysicalRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex))
        {
            return BuildPostFailureCold(actorId);
        }
        return PostQueuedGrowCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedRejectNewPhysicalSafe<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPhysicalRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex))
        {
            return BuildPostFailureCold(actorId);
        }

        return PostQueuedRejectNewCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedDropOldestPhysicalSafe<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPhysicalRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex))
        {
            return BuildPostFailureCold(actorId);
        }

        return PostQueuedDropOldestCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostLatestPhysicalSafe<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPhysicalRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex))
        {
            return BuildPostFailureCold(actorId);
        }

        return PostLatestCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostDirtyPhysicalSafe<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPhysicalRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex))
        {
            return BuildPostFailureCold(actorId);
        }

        return PostDirtyCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedGrowPostableStamp<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPostableRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex, out PostResult failure))
        {
            return failure;
        }

        return PostQueuedGrowCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedRejectNewPostableStamp<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPostableRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex, out PostResult failure))
        {
            return failure;
        }

        return PostQueuedRejectNewCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedDropOldestPostableStamp<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPostableRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex, out PostResult failure))
        {
            return failure;
        }

        return PostQueuedDropOldestCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostLatestPostableStamp<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPostableRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex, out PostResult failure))
        {
            return failure;
        }

        return PostLatestCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostDirtyPostableStamp<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetPostableRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex, out PostResult failure))
        {
            return failure;
        }

        return PostDirtyCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetPhysicalRow<TEvent>(
        ActorId actorId,
        EventPostState<TEvent> state,
        out EventPostRow<TEvent> row,
        out int slotIndex)
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

        row = rows[archetypeId];
        slotIndex = actorId.SlotIndex;
        return (uint)slotIndex < (uint)row.Mails.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetPostableRow<TEvent>(
        ActorId actorId,
        EventPostState<TEvent> state,
        out EventPostRow<TEvent> row,
        out int slotIndex,
        out PostResult failure)
        where TEvent : struct
    {
        if (!TryGetPhysicalRow(actorId, state, out row, out slotIndex))
        {
            failure = BuildPostFailureCold(actorId);
            return false;
        }

        int[]? postableGenerations = row.PostableGenerations;
        if (postableGenerations == null
            || postableGenerations[slotIndex] != actorId.Generation)
        {
            failure = BuildPostableStampRejectedCold(actorId);
            return false;
        }

        failure = PostResult.Success;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PostResult PostQueuedGrowCore<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        EventMailPool<TEvent> pool,
        ActorMailOptions options)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        EnsureMailAllocated(ref mail, pool);

        if (mail.Count >= mail.Capacity)
        {
            if (!pool.TryGrow(ref mail))
            {
                PostResult growFailure = HandleGrowFailure(ref mail, in value, pool, options);
                if (!growFailure.IsSuccess || !growFailure.CountsAsPending)
                {
                    return growFailure;
                }
            }
        }

        WriteQueued(ref mail, in value, dirtySlots, slotIndex, bucketIndex, pool);
        return PostResult.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PostResult PostQueuedRejectNewCore<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        EventMailPool<TEvent> pool,
        ActorMailOptions options)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        EnsureMailAllocated(ref mail, pool);
        if (mail.Count >= mail.Capacity)
        {
            return PostResult.Failure(
                ActorPostStatus.MailFullRejected,
                "Actor mail is full.",
                PostFailureKind.MailboxFull);
        }

        WriteQueued(ref mail, in value, dirtySlots, slotIndex, bucketIndex, pool);
        return PostResult.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PostResult PostQueuedDropOldestCore<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        EventMailPool<TEvent> pool,
        ActorMailOptions options)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        EnsureMailAllocated(ref mail, pool);
        if (mail.Count >= mail.Capacity)
        {
            DropOldest(ref mail);
        }

        WriteQueued(ref mail, in value, dirtySlots, slotIndex, bucketIndex, pool);
        return PostResult.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PostResult PostLatestCore<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        EventMailPool<TEvent> pool)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        bool wasEmpty = mail.Count == 0;
        EnsureMailAllocated(ref mail, pool);
        pool.Write(mail.BufferId, 0, in value);
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 1;

        if (wasEmpty)
        {
            dirtySlots.Mark(slotIndex);
            _dirtyEventBuckets.Mark(bucketIndex);
            return PostResult.Success;
        }

        return PostResult.Coalesced();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PostResult PostDirtyCore<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        EventMailPool<TEvent> pool)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];
        if (mail.Count > 0)
        {
            return PostResult.Coalesced();
        }

        EnsureMailAllocated(ref mail, pool);
        pool.Write(mail.BufferId, 0, in value);
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 1;
        dirtySlots.Mark(slotIndex);
        _dirtyEventBuckets.Mark(bucketIndex);
        return PostResult.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureMailAllocated<TEvent>(ref EventMail<TEvent> mail, EventMailPool<TEvent> pool)
        where TEvent : struct
    {
        if (mail.BufferId != 0)
        {
            return;
        }

        mail.BufferId = pool.RentInitial();
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 0;
        mail.Capacity = pool.GetCapacity(mail.BufferId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteQueued<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        DirtySlotList dirtySlots,
        int slotIndex,
        int bucketIndex,
        EventMailPool<TEvent> pool)
        where TEvent : struct
    {
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DropOldest<TEvent>(ref EventMail<TEvent> mail)
        where TEvent : struct
    {
        if (mail.Count <= 0)
        {
            return;
        }

        mail.Head = ActorMailCapacity.Wrap(mail.Head + 1, mail.Capacity);
        mail.Count--;
        if (mail.Count == 0)
        {
            mail.Tail = 0;
        }
    }

    private static PostResult HandleGrowFailure<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        EventMailPool<TEvent> pool,
        ActorMailOptions options)
        where TEvent : struct
    {
        switch (options.GrowFailurePolicy)
        {
            case ActorMailFullPolicy.RejectNew:
                return PostResult.Failure(
                    ActorPostStatus.MailFullRejected,
                    "Actor mail reached max capacity.",
                    PostFailureKind.MailboxFull);
            case ActorMailFullPolicy.DropOldest:
                DropOldest(ref mail);
                return PostResult.Success;
            case ActorMailFullPolicy.DropNewest:
                return PostResult.Dropped();
            case ActorMailFullPolicy.OverwriteLatest:
                if (mail.Count > 0)
                {
                    int latestIndex = ActorMailCapacity.Wrap(mail.Head + mail.Count - 1, mail.Capacity);
                    pool.Write(mail.BufferId, latestIndex, in value);
                    return PostResult.Coalesced();
                }

                return PostResult.Success;
            default:
                return PostResult.Failure(
                    ActorPostStatus.MailFullRejected,
                    "Grow failure policy is not supported.",
                    PostFailureKind.MailboxFull);
        }
    }

    private static EventPostRow<TEvent>[] CreateRows<TEvent>(int archetypeCapacity)
        where TEvent : struct
    {
        EventPostRow<TEvent> invalid = CreateInvalidRow<TEvent>();
        var rows = new EventPostRow<TEvent>[Math.Max(archetypeCapacity, 1)];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = invalid;
        }

        return rows;
    }

    private static EventPostRow<TEvent> CreateInvalidRow<TEvent>()
        where TEvent : struct
    {
        return new EventPostRow<TEvent>(
            Array.Empty<EventMail<TEvent>>(),
            DirtySlotList.Empty,
            -1,
            null);
    }

    private static void EnsureRowsCapacity<TEvent>(
        ref EventPostRow<TEvent>[] rows,
        int archetypeId)
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
        EventPostRow<TEvent> invalid = CreateInvalidRow<TEvent>();
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
            $"Event post state is not built for {typeof(TEvent).Name}.",
            PostFailureKind.UnsupportedEvent);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult BuildRouteUnsupportedCold<TEvent>()
        where TEvent : struct
    {
        return PostResult.Failure(
            ActorPostStatus.EventNotSupported,
            $"RouteCode is not supported for {typeof(TEvent).Name}.",
            PostFailureKind.UnsupportedEvent);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult BuildPostFailureCold(ActorId actorId)
    {
        return PostResult.Failure(
            ActorPostStatus.PhysicalTargetInvalid,
            $"ActorId ({actorId.ArchetypeId}, {actorId.SlotIndex}, {actorId.Generation}) cannot locate a current physical mailbox.",
            PostFailureKind.PhysicalTargetInvalid);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult BuildPostableStampRejectedCold(ActorId actorId)
    {
        return PostResult.Failure(
            ActorPostStatus.RejectedByPostableStamp,
            $"ActorId ({actorId.ArchetypeId}, {actorId.SlotIndex}, {actorId.Generation}) failed the postable-generation check.",
            PostFailureKind.RejectedByPostableStamp);
    }
}
