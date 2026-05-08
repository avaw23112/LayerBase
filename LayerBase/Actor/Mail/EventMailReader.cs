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
            value = default;
            return false;
        }

        value = bufferPool.Read(mail.BufferId, mail.Head);
        mail.Head = (mail.Head + 1) % mail.Capacity;
        mail.Count--;

        if (mail.Count == 0)
        {
            mail.Head = 0;
        }

        return true;
    }

    public static void ReleaseIfEmpty<TEvent>(
        ref EventMail<TEvent> mail,
        RingQueueBuffer<TEvent> bufferPool,
        ActorMailOptions options)
        where TEvent : struct
    {
        if (mail.Count != 0 || !options.ReleaseWhenEmpty || mail.BufferId == 0)
        {
            return;
        }

        bufferPool.Release(mail.BufferId);
        mail = default;
    }
}
