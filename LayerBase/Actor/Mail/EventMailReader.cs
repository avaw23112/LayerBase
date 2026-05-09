namespace LayerBase.Actor;

internal static class EventMailReader
{
    public static bool TryDequeue<TEvent>(
        ref EventMail<TEvent> mail,
        RingQueueBuffer<TEvent> bufferPool,
        out TEvent value)
        where TEvent : struct
    {
        if (mail.Count == 0 || mail.BufferId == 0)
        {
            if (mail.Count == 0)
            {
                value = default;
                return false;
            }

            value = mail.SingleValue;
            mail.SingleValue = default;
            mail.Count = 0;
            mail.Head = 0;
            mail.Tail = 0;
            mail.Capacity = 0;
            return true;
        }

        value = bufferPool.Read(mail.BufferId, mail.Head);
        mail.Head = ActorMailCapacity.Wrap(mail.Head + 1, mail.Capacity);
        mail.Count--;

        if (mail.Count == 0)
        {
            mail.Head = 0;
            mail.Tail = 0;
        }

        return true;
    }

    public static void ReleaseIfEmpty<TEvent>(
        ref EventMail<TEvent> mail,
        RingQueueBuffer<TEvent> bufferPool,
        ActorMailOptions options)
        where TEvent : struct
    {
        if (mail.Count != 0)
        {
            return;
        }

        if (mail.BufferId == 0)
        {
            mail = default;
            return;
        }

        if (!options.ReleaseWhenEmpty)
        {
            return;
        }

        bufferPool.Release(mail.BufferId);
        mail = default;
    }

    public static void ForceRelease<TEvent>(
        ref EventMail<TEvent> mail,
        RingQueueBuffer<TEvent> bufferPool)
        where TEvent : struct
    {
        if (mail.BufferId != 0)
        {
            bufferPool.Release(mail.BufferId);
        }

        mail = default;
    }
}
