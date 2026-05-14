using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

/// <summary>
/// EventStreamSegment 对象池。
///
/// 作用：
/// 避免频繁创建和销毁 Segment 数组。
/// 支持配置最大保留数量，避免高峰后长期占用内存。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal sealed class EventStreamSegmentPool<TEvent>
    where TEvent : struct
{
    private readonly int _segmentCapacity;
    private readonly int _maxRetained;
    private readonly bool _clearItemsOnReturn = RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>();
    private EventStreamSegment<TEvent>? _first;
    private int _count;

    /// <summary>
    /// 构造 EventStreamSegmentPool。
    /// </summary>
    /// <param name="segmentCapacity">
    /// 每个 Segment 的容量。
    /// </param>
    /// <param name="maxRetained">
    /// 最多保留多少个空闲 Segment。
    /// </param>
    public EventStreamSegmentPool(
        int segmentCapacity,
        int maxRetained)
    {
        _segmentCapacity = segmentCapacity;
        _maxRetained = maxRetained;
        _first = null;
        _count = 0;
    }

    /// <summary>
    /// 从池中租借一个 Segment。
    /// 如果池为空，创建新的 Segment。
    /// </summary>
    /// <returns>
    /// 可用的 Segment。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventStreamSegment<TEvent> Rent()
    {
        EventStreamSegment<TEvent>? segment = _first;
        if (segment != null)
        {
            _first = segment.Next;
            segment.Next = null;
            _count--;
            return segment;
        }

        return new EventStreamSegment<TEvent>(_segmentCapacity);
    }

    /// <summary>
    /// 将 Segment 归还到池中。
    /// 如果池已满，丢弃 Segment。
    /// </summary>
    /// <param name="segment">
    /// 要归还的 Segment。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(EventStreamSegment<TEvent> segment)
    {
        segment.Reset(_clearItemsOnReturn);
        
        if (_count >= _maxRetained)
        {
            return;
        }

        segment.Next = _first;
        _first = segment;
        _count++;
    }

    /// <summary>
    /// 清空池中所有 Segment。
    /// </summary>
    public void Clear()
    {
        _first = null;
        _count = 0;
    }
}
