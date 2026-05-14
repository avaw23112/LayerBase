using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

/// <summary>
/// EventStreamSegment object pool.
/// </summary>
internal sealed class EventStreamSegmentPool<TEvent>
    where TEvent : struct
{
    private readonly int _segmentCapacity;
    private readonly int _maxRetained;
    private readonly bool _clearItemsOnReturn = RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>();
    private EventStreamSegment<TEvent>? _first;
    private int _count;

    public EventStreamSegmentPool(
        int segmentCapacity,
        int maxRetained)
    {
        _segmentCapacity = segmentCapacity;
        _maxRetained = maxRetained;
        _first = null;
        _count = 0;
    }

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

    public void Clear()
    {
        EventStreamSegment<TEvent>? current = _first;
        while (current != null)
        {
            EventStreamSegment<TEvent>? next = current.Next;
            current.Reset(_clearItemsOnReturn);
            current = next;
        }

        _first = null;
        _count = 0;
    }
}
