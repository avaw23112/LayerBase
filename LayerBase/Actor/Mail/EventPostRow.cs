namespace LayerBase.Actor;

internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 当前事件类型对应的邮箱列。
    /// 下标是 ActorId.SlotIndex。
    /// </summary>
    public readonly EventMail<TEvent>[] Mails;

    /// <summary>
    /// 当前事件列的 dirty slot 列表。
    /// 当某个 slot 从无事件变为有事件时，会写入这里。
    /// </summary>
    public readonly DirtySlotList DirtySlots;

    /// <summary>
    /// 当前事件类型对应的 dirty bucket 下标。
    /// ActorWorld.Pump 会通过它找到有待处理事件的 bucket。
    /// </summary>
    public readonly int BucketIndex;

    /// <summary>
    /// 当前 archetype 下每个 slot 的 generation 缓存。
    /// 下标是 ActorId.SlotIndex。
    /// 值等于 ActorId.Generation 时，表示这个 ActorId 当前仍然有效。
    /// 注意：这个数组只缓存 generation，不考虑 alive/pending destroy 状态。
    /// </summary>
    public readonly int[] Generations;

    /// <summary>
    /// 当前 archetype 下每个 slot 的 Actor 引用是否有效。
    /// 下标是 ActorId.SlotIndex。
    /// </summary>
    public readonly bool[] ActorExists;

    /// <summary>
    /// 当前 row 是否有效。
    /// 空 Mails 表示该 archetype 不支持这个事件类型。
    /// </summary>
    public bool IsValid => Mails.Length > 0;

    /// <summary>
    /// 构造 EventPostRow。
    /// </summary>
    /// <param name="mails">
    /// 当前事件类型对应的邮箱数组。
    /// </param>
    /// <param name="dirtySlots">
    /// 当前事件列的 dirty slot 列表。
    /// </param>
    /// <param name="bucketIndex">
    /// 当前事件类型对应的 bucket 下标。
    /// </param>
    /// <param name="generations">
    /// 当前 archetype 下每个 slot 的 generation 缓存。
    /// </param>
    /// <param name="actorExists">
    /// 当前 archetype 下每个 slot 的 Actor 引用是否有效。
    /// </param>
    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList       dirtySlots,
        int                 bucketIndex,
        int[]               generations,
        bool[]              actorExists)
    {
        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        Generations = generations;
        ActorExists = actorExists;
    }
}