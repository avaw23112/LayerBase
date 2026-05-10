using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    internal EventPostState<TEvent> GetOrCreateEventPostState<TEvent>(ActorEventPostPlan<TEvent> plan)
        where TEvent : struct
    {
        EventPostState<TEvent>? existing = EventPostRuntime<TEvent>.GetState(this);
        if (existing != null)
        {
            return existing;
        }

        EventMailPool<TEvent> pool = GlobalEventMailPools.GetOrCreate<TEvent>(plan.MailOptions);
        EventPostRow<TEvent>[] rows = new EventPostRow<TEvent>[Math.Max(_archetypes.Length, 1)];
        var state = new EventPostState<TEvent>(
            route: plan.Route,
            pool: pool,
            options: plan.MailOptions,
            rejectMask: plan.RejectMask,
            rejectDisabled: plan.RejectDisabled,
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
        int[] generations,
        ActorSlotFlags[] slotFlags,
        ActorEventPostPlan<TEvent> plan)
        where TEvent : struct
    {
        EventPostState<TEvent> state = GetOrCreateEventPostState(plan);
        EnsureRowsCapacity(ref state.RowsByArchetype, archetypeId);
        state.RowsByArchetype[archetypeId] = new EventPostRow<TEvent>(
            mails,
            dirtySlots,
            bucketIndex,
            generations,
            slotFlags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PostResult PostCompiled<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        return state.Route switch
        {
            ActorPostRouteKind.QueuedGrow => PostQueuedGrow(actorId, in value, state),
            ActorPostRouteKind.QueuedRejectNew => PostQueuedRejectNew(actorId, in value, state),
            ActorPostRouteKind.QueuedDropOldest => PostQueuedDropOldest(actorId, in value, state),
            ActorPostRouteKind.Latest => PostLatest(actorId, in value, state),
            ActorPostRouteKind.Dirty => PostDirty(actorId, in value, state),
            ActorPostRouteKind.Disabled => PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                "ActorPost is disabled for this event.",
                PostFailureKind.UnsupportedEvent),
            _ => PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                "Unknown actor post route.",
                PostFailureKind.UnsupportedEvent)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool PostFast<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        EventPostState<TEvent>? state = EventPostRuntime<TEvent>.GetState(this);
        if (state == null)
        {
            return false;
        }

        if (state.Route is ActorPostRouteKind.DiagnosticOnly or ActorPostRouteKind.Disabled)
        {
            return false;
        }

        return PostCompiled(actorId, in value, state).IsSuccess;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedGrow<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
        {
            return failure;
        }

        return PostQueuedGrowCore(
            actorId.SlotIndex,
            in value,
            row.Mails,
            row.DirtySlots,
            row.BucketIndex,
            state.Pool,
            state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedRejectNew<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
        {
            return failure;
        }

        return PostQueuedRejectNewCore(
            actorId.SlotIndex,
            in value,
            row.Mails,
            row.DirtySlots,
            row.BucketIndex,
            state.Pool,
            state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedDropOldest<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
        {
            return failure;
        }

        return PostQueuedDropOldestCore(
            actorId.SlotIndex,
            in value,
            row.Mails,
            row.DirtySlots,
            row.BucketIndex,
            state.Pool,
            state.Options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostLatest<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
        {
            return failure;
        }

        return PostLatestCore(
            actorId.SlotIndex,
            in value,
            row.Mails,
            row.DirtySlots,
            row.BucketIndex,
            state.Pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostDirty<TEvent>(
        ActorId actorId,
        in TEvent value,
        EventPostState<TEvent> state)
        where TEvent : struct
    {
        if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
        {
            return failure;
        }

        return PostDirtyCore(
            actorId.SlotIndex,
            in value,
            row.Mails,
            row.DirtySlots,
            row.BucketIndex,
            state.Pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetValidRow<TEvent>(
        ActorId actorId,
        EventPostState<TEvent> state,
        out EventPostRow<TEvent> row,
        out PostResult failure)
        where TEvent : struct
    {
        EventPostRow<TEvent>[] rows = state.RowsByArchetype;
        int archetypeId = actorId.ArchetypeId;
        if ((uint)archetypeId >= (uint)rows.Length)
        {
            row = default;
            failure = PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                "Invalid ActorId.ArchetypeId.",
                PostFailureKind.InvalidActorId);
            return false;
        }

        row = rows[archetypeId];
        if (!row.IsValid)
        {
            failure = PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                "Target archetype does not support this event.",
                PostFailureKind.UnsupportedEvent);
            return false;
        }

        int slotIndex = actorId.SlotIndex;
        if ((uint)slotIndex >= (uint)row.Generations.Length)
        {
            failure = PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                "Invalid ActorId.SlotIndex.",
                PostFailureKind.InvalidActorId);
            return false;
        }

        if (row.Generations[slotIndex] != actorId.Generation)
        {
            failure = PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                "ActorId generation mismatch.",
                PostFailureKind.InvalidActorId);
            return false;
        }

        ActorSlotFlags flags = row.SlotFlags[slotIndex];
        if ((flags & ActorSlotFlags.Alive) == 0)
        {
            failure = PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "Actor slot is not alive.",
                PostFailureKind.InvalidActorId);
            return false;
        }

        if ((flags & ActorSlotFlags.PendingDestroy) != 0)
        {
            failure = PostResult.Failure(
                ActorPostStatus.ActorPendingDestroy,
                "Actor is pending destroy.",
                PostFailureKind.PendingDestroy);
            return false;
        }

        if ((flags & ActorSlotFlags.Destroying) != 0)
        {
            failure = PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "Actor is destroying.",
                PostFailureKind.Destroying);
            return false;
        }

        if (state.RejectDisabled
            && (flags & ActorSlotFlags.Enabled) == 0)
        {
            failure = PostResult.Failure(
                ActorPostStatus.ActorDisabledRejected,
                "Actor is disabled.",
                PostFailureKind.DisabledActor);
            return false;
        }

        failure = PostResult.Success;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanPostSlot<TEvent>(
        EventPostRow<TEvent> row,
        EventPostState<TEvent> state,
        int slotIndex)
        where TEvent : struct
    {
        if ((uint)slotIndex >= (uint)row.SlotFlags.Length)
        {
            return false;
        }

        ActorSlotFlags flags = row.SlotFlags[slotIndex];
        if ((flags & ActorSlotFlags.Alive) == 0)
        {
            return false;
        }

        if ((flags & state.RejectMask) != 0)
        {
            return false;
        }

        if (state.RejectDisabled
            && (flags & ActorSlotFlags.Enabled) == 0)
        {
            return false;
        }

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
    internal bool PostQueuedGrowFastNoResult<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        EventMailPool<TEvent> pool,
        ActorMailOptions options)
        where TEvent : struct
    {
        return PostQueuedGrowCore(
            slotIndex,
            in value,
            mails,
            dirtySlots,
            bucketIndex,
            pool,
            options).IsSuccess;
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

    private static void EnsureRowsCapacity<TEvent>(
        ref EventPostRow<TEvent>[] rows,
        int archetypeId)
        where TEvent : struct
    {
        if ((uint)archetypeId < (uint)rows.Length)
        {
            return;
        }

        int newSize = rows.Length == 0 ? 4 : rows.Length;
        while (newSize <= archetypeId)
        {
            newSize <<= 1;
        }

        Array.Resize(ref rows, newSize);
    }
}
