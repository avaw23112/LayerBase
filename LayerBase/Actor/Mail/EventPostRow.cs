namespace LayerBase.Actor;

internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;
    public readonly EventMailPool<TEvent> Pool;
    public readonly DirtySlotList DirtySlots;
    public readonly int BucketIndex;
    public readonly int[] Generations;

    public EventPostRow(
        EventMail<TEvent>[] mails,
        EventMailPool<TEvent> pool,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[] generations)
    {
        Mails = mails;
        Pool = pool;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        Generations = generations;
    }

    public bool IsValid => Mails != null;
}
