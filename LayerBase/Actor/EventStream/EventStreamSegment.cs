using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

/// <summary>
/// EventStream 的一个分段。
///
/// 作用：
/// 使用连续数组存储邮件，支持 FIFO 读写。
/// 多个 Segment 通过 Next 指针形成链表。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
[SkipLocalsInit]
internal sealed class EventStreamSegment<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 邮件存储数组。
    /// </summary>
    public readonly EventStreamMail<TEvent>[] Items;

    /// <summary>
    /// 当前 Segment 的容量。
    /// </summary>
    public readonly int Capacity;

    /// <summary>
    /// 写入位置索引。
    /// </summary>
    public int WriteIndex;

    /// <summary>
    /// 读取位置索引。
    /// </summary>
    public int ReadIndex;

    /// <summary>
    /// 链表下一个 Segment。
    /// </summary>
    public EventStreamSegment<TEvent>? Next;

    /// <summary>
    /// 构造 EventStreamSegment。
    /// </summary>
    /// <param name="capacity">
    /// Segment 容量。
    /// </param>
    public EventStreamSegment(int capacity)
    {
        Capacity = capacity;
        Items = new EventStreamMail<TEvent>[capacity];
        WriteIndex = 0;
        ReadIndex = 0;
        Next = null;
    }

    /// <summary>
    /// 当前 Segment 是否已满。
    /// </summary>
    public bool IsFull => WriteIndex >= Capacity;

    /// <summary>
    /// 当前 Segment 是否已读空。
    /// </summary>
    public bool IsEmpty => ReadIndex >= WriteIndex;

    /// <summary>
    /// 当前 Segment 中待处理的邮件数量。
    /// </summary>
    public int PendingCount => WriteIndex - ReadIndex;

    /// <summary>
    /// 重置 Segment 状态以便复用。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(bool clearItems)
    {
        if (clearItems)
        {
            Array.Clear(
                Items,
                0,
                WriteIndex);
        }
        WriteIndex = 0;
        ReadIndex = 0;
        Next = null;
    }
}
