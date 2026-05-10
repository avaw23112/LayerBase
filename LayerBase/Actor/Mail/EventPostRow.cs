namespace LayerBase.Actor;

internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;
    public readonly DirtySlotList DirtySlots;
    public readonly int BucketIndex;
    public readonly int[] Generations;
    public readonly ActorSlotFlags[] SlotFlags;

    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[] generations,
        ActorSlotFlags[] slotFlags)
    {
        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        Generations = generations;
        SlotFlags = slotFlags;
    }

    public bool IsValid => Mails != null;
}
