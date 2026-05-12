using System.Runtime.CompilerServices;

namespace LayerBase.Core.DataStruct;

internal sealed class RingBuffer<T>
{
    private readonly T[] _buffer;
    private readonly int _logicalCapacity;
    private readonly int _physicalCapacity;
    private readonly int _mask;
    private int _head;
    private int _tail;
    private int _count;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        _logicalCapacity = capacity;
        _physicalCapacity = capacity;
        // Force power of two for physical capacity
        if ((_physicalCapacity & (_physicalCapacity - 1)) != 0)
        {
            int p = 1;
            while (p < _physicalCapacity) p <<= 1;
            _physicalCapacity = p;
        }

        _mask = _physicalCapacity - 1;
        _buffer = new T[_physicalCapacity];
    }

    public int Count => _count;
    public bool IsFull => _count >= _logicalCapacity;
    public bool IsEmpty => _count == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(in T item)
    {
        if (_count >= _logicalCapacity) return false;

        _buffer[_tail] = item;
        _tail = (_tail + 1) & _mask;
        _count++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }


        item = _buffer[_head];
        _buffer[_head] = default!;
        _head = (_head + 1) & _mask;
        _count--;
        return true;
    }

    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];
        return true;
    }

    public void DropOldest()
    {
        if (_count == 0) return;
        _buffer[_head] = default!;
        _head = (_head + 1) & _mask;
        _count--;
    }

    public void DropNewest()
    {
        if (_count == 0) return;
        _tail = (_tail - 1) & _mask;
        _buffer[_tail] = default!;
        _count--;
    }
}