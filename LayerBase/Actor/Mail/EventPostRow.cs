namespace LayerBase.Actor;

internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;
    public readonly DirtySlotList DirtySlots;
    public readonly int BucketIndex;
    public readonly int[]? PostableGenerations;

    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[]? postableGenerations)
    {
        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        PostableGenerations = postableGenerations;
    }

    public bool IsValid => Mails.Length > 0;
}
