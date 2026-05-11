namespace LayerBase.Actor;

internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;
    public readonly DirtySlotList DirtySlots;
    public readonly int BucketIndex;

    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex)
    {
        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
    }

    public bool IsValid => Mails.Length > 0;
}
