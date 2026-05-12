namespace LayerBase.Actor;

internal static class EventMailReader
{
    public static bool TryDequeue<TEvent>(
        ref EventMail<TEvent> mail,
        EventMailPool<TEvent> bufferPool,
        out TEvent            value)
        where TEvent : struct
    {
        if (mail.Count == 0)
        {
            value = default;
            return false;
        }

        if (mail.Buffer == null)
        {
            value = mail.SingleValue;
            mail.SingleValue = default;
            mail.Count = 0;
            mail.Head = 0;
            mail.Tail = 0;
            mail.Capacity = 0;
            return true;
        }

        TEvent[] buffer = mail.Buffer;
        value = buffer[mail.Head];
        mail.Head++;
        if (mail.Head == mail.Capacity)
        {
            mail.Head = 0;
        }

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
        EventMailPool<TEvent> bufferPool,
        ActorMailOptions      options)
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
        EventMailPool<TEvent> bufferPool)
        where TEvent : struct
    {
        if (mail.BufferId != 0)
        {
            bufferPool.Release(mail.BufferId);
        }

        mail = default;
    }
}