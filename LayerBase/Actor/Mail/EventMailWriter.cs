using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Actor;

internal static class EventMailWriter
{
    public static PostResult Enqueue<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        RingQueueBuffer<TEvent> bufferPool,
        DirtySlotList dirtySlots,
        int slotIndex,
        ActorMailOptions options,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        ActorPostPolicy effectivePostPolicy = postPolicy ?? options.PostPolicy;
        switch (effectivePostPolicy)
        {
            case ActorPostPolicy.Queued:
                return EnqueueQueued(ref mail, in value, bufferPool, dirtySlots, slotIndex, options, fullPolicy);
            case ActorPostPolicy.Latest:
                return EnqueueLatest(ref mail, in value, bufferPool, dirtySlots, slotIndex, options);
            case ActorPostPolicy.Coalesced:
                return EnqueueMerge(ref mail, in value, bufferPool, dirtySlots, slotIndex, options);
            case ActorPostPolicy.Dirty:
                return EnqueueDirty(ref mail, in value, bufferPool, dirtySlots, slotIndex, options);
            default:
                return PostResult.Failure($"Actor post policy '{effectivePostPolicy}' is not supported in this phase.");
        }
    }

    private static PostResult EnqueueMerge<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        RingQueueBuffer<TEvent> bufferPool,
        DirtySlotList dirtySlots,
        int slotIndex,
        ActorMailOptions options)
        where TEvent : struct
    {
        bool wasEmpty = mail.Count == 0;
        EnsureMailAllocated(ref mail, bufferPool, options);

        TEvent merged = value;
        if (!wasEmpty)
        {
            TEvent oldValue = bufferPool.Read(mail.BufferId, mail.Head);
            if (!EventMetaData.TryMergePostEvent(in oldValue, in value, out merged))
            {
                return PostResult.Failure(
                    ActorPostStatus.MergeFailed,
                    "EventMetaData.TryMergePostEvent failed.",
                    PostFailureKind.MergeFailed);
            }
        }

        bufferPool.Write(mail.BufferId, 0, in merged);
        mail.Head = 0;
        mail.Count = 1;

        if (wasEmpty)
        {
            dirtySlots.AddIfNotExists(slotIndex);
            return PostResult.Success;
        }

        return PostResult.Coalesced();
    }

    private static PostResult EnqueueQueued<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        RingQueueBuffer<TEvent> bufferPool,
        DirtySlotList dirtySlots,
        int slotIndex,
        ActorMailOptions options,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        if (mail.BufferId == 0)
        {
            mail.BufferId = bufferPool.Rent(options.InitialCapacity);
            mail.Head = 0;
            mail.Count = 0;
            mail.Capacity = bufferPool.GetCapacity(mail.BufferId);
        }

        if (mail.Count >= mail.Capacity)
        {
            PostResult fullResult = HandleFull(ref mail, in value, bufferPool, dirtySlots, slotIndex, options, fullPolicy);
            if (!fullResult.IsSuccess || !fullResult.CountsAsPending)
            {
                return fullResult;
            }
        }

        int tail = ActorMailCapacity.Wrap(mail.Head + mail.Count, mail.Capacity);
        bufferPool.Write(mail.BufferId, tail, in value);
        mail.Count++;

        if (mail.Count == 1)
        {
            dirtySlots.AddIfNotExists(slotIndex);
        }

        return PostResult.Success;
    }

    private static PostResult EnqueueLatest<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        RingQueueBuffer<TEvent> bufferPool,
        DirtySlotList dirtySlots,
        int slotIndex,
        ActorMailOptions options)
        where TEvent : struct
    {
        bool wasEmpty = mail.Count == 0;
        EnsureMailAllocated(ref mail, bufferPool, options);

        bufferPool.Write(mail.BufferId, 0, in value);
        mail.Head = 0;
        mail.Count = 1;

        if (wasEmpty)
        {
            dirtySlots.AddIfNotExists(slotIndex);
            return PostResult.Success;
        }

        return PostResult.Coalesced();
    }

    private static PostResult EnqueueDirty<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        RingQueueBuffer<TEvent> bufferPool,
        DirtySlotList dirtySlots,
        int slotIndex,
        ActorMailOptions options)
        where TEvent : struct
    {
        if (mail.Count > 0)
        {
            return PostResult.Coalesced();
        }

        EnsureMailAllocated(ref mail, bufferPool, options);
        bufferPool.Write(mail.BufferId, 0, in value);
        mail.Head = 0;
        mail.Count = 1;
        dirtySlots.AddIfNotExists(slotIndex);
        return PostResult.Success;
    }

    private static PostResult HandleFull<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        RingQueueBuffer<TEvent> bufferPool,
        DirtySlotList dirtySlots,
        int slotIndex,
        ActorMailOptions options,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        ActorMailFullPolicy effectiveFullPolicy = fullPolicy ?? options.FullPolicy;
        switch (effectiveFullPolicy)
        {
            case ActorMailFullPolicy.Grow:
                if (TryGrow(ref mail, bufferPool, options))
                {
                    return PostResult.Success;
                }

                return HandleGrowFailure(ref mail, in value, bufferPool, options);

            case ActorMailFullPolicy.RejectNew:
                return PostResult.Failure(
                    ActorPostStatus.MailFullRejected,
                    "Actor mail is full.",
                    PostFailureKind.MailboxFull);

            case ActorMailFullPolicy.DropOldest:
                if (mail.Count > 0)
                {
                    mail.Head = ActorMailCapacity.Wrap(mail.Head + 1, mail.Capacity);
                    mail.Count--;
                }

                return PostResult.Success;

            case ActorMailFullPolicy.DropNewest:
                return PostResult.Dropped();

            case ActorMailFullPolicy.OverwriteLatest:
                if (mail.Count > 0)
                {
                    int latestIndex = ActorMailCapacity.Wrap(mail.Head + mail.Count - 1, mail.Capacity);
                    bufferPool.Write(mail.BufferId, latestIndex, in value);
                    return PostResult.Coalesced();
                }

                return PostResult.Success;

            default:
                return PostResult.Failure($"Actor full policy '{effectiveFullPolicy}' is not supported.");
        }
    }

    private static PostResult HandleGrowFailure<TEvent>(
        ref EventMail<TEvent> mail,
        in TEvent value,
        RingQueueBuffer<TEvent> bufferPool,
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
                if (mail.Count > 0)
                {
                    mail.Head = ActorMailCapacity.Wrap(mail.Head + 1, mail.Capacity);
                    mail.Count--;
                }

                return PostResult.Success;

            case ActorMailFullPolicy.DropNewest:
                return PostResult.Dropped();

            default:
                return PostResult.Failure($"Grow failure policy '{options.GrowFailurePolicy}' is not supported.");
        }
    }

    private static bool TryGrow<TEvent>(
        ref EventMail<TEvent> mail,
        RingQueueBuffer<TEvent> bufferPool,
        ActorMailOptions options)
        where TEvent : struct
    {
        if (mail.Capacity >= options.MaxCapacity)
        {
            return false;
        }

        int nextCapacity = ActorMailCapacity.NormalizePowerOfTwo(
            mail.Capacity * Math.Max(options.GrowFactor, 2));
        if (nextCapacity <= mail.Capacity)
        {
            nextCapacity = mail.Capacity + 1;
        }

        nextCapacity = Math.Min(
            ActorMailCapacity.NormalizePowerOfTwo(nextCapacity),
            options.MaxCapacity);
        if (nextCapacity <= mail.Capacity)
        {
            return false;
        }

        bufferPool.Resize(mail.BufferId, mail.Head, mail.Count, nextCapacity);
        mail.Head = 0;
        mail.Capacity = nextCapacity;
        return true;
    }

    private static void EnsureMailAllocated<TEvent>(
        ref EventMail<TEvent> mail,
        RingQueueBuffer<TEvent> bufferPool,
        ActorMailOptions options)
        where TEvent : struct
    {
        if (mail.BufferId != 0)
        {
            return;
        }

        mail.BufferId = bufferPool.Rent(options.InitialCapacity);
        mail.Head = 0;
        mail.Count = 0;
        mail.Capacity = bufferPool.GetCapacity(mail.BufferId);
    }
}
