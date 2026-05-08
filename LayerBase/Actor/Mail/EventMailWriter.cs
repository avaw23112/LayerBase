using LayerBase.Core.Event;

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
        if (effectivePostPolicy != ActorPostPolicy.Queued)
        {
            return PostResult.Failure($"Actor post policy '{effectivePostPolicy}' is not supported in phase 3.");
        }

        if (mail.BufferId == 0)
        {
            mail.BufferId = bufferPool.Rent(options.InitialCapacity);
            mail.Head = 0;
            mail.Count = 0;
            mail.Capacity = bufferPool.GetCapacity(mail.BufferId);
        }

        if (mail.Count >= mail.Capacity)
        {
            ActorMailFullPolicy effectiveFullPolicy = fullPolicy ?? options.FullPolicy;
            if (effectiveFullPolicy == ActorMailFullPolicy.RejectNew)
            {
                return PostResult.Failure("Actor mail is full.");
            }

            return PostResult.Failure($"Actor full policy '{effectiveFullPolicy}' is not supported in phase 3.");
        }

        int tail = (mail.Head + mail.Count) % mail.Capacity;
        bufferPool.Write(mail.BufferId, tail, in value);
        mail.Count++;

        if (mail.Count == 1)
        {
            dirtySlots.AddIfNotExists(slotIndex);
        }

        return PostResult.Success;
    }
}
