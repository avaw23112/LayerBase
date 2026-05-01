namespace LayerBase.Core.DataStruct;

internal sealed class RingBuffer<T>
{
    private readonly T[] _buffer;
    private readonly int _capacity;
    private int _head;
    private int _tail;
    private int _count;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _buffer = new T[capacity];
    }

    public int Count => _count;
    public bool IsFull => _count == _capacity;
    public bool IsEmpty => _count == 0;

    public bool TryEnqueue(in T item)
    {
        if (_count == _capacity) return false;
        
        _buffer[_tail] = item;
        _tail = (_tail + 1) % _capacity;
        _count++;
        return true;
    }

    public bool TryDequeue(out T item)
    {
        if (_count == 0)
        {
            item = default;
            return false;
        }
        
        item = _buffer[_head];
        _buffer[_head] = default;
        _head = (_head + 1) % _capacity;
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

    public bool TryPeek(out T item)
    {
        if (_count == 0)
        {
            item = default;
            return false;
        }
        item = _buffer[_head];
        return true;
    }
    
    public void DropOldest()
    {
        if (_count == 0) return;
        _buffer[_head] = default;
        _head = (_head + 1) % _capacity;
        _count--;
    }
    
    public void DropNewest()
    {
        if (_count == 0) return;
        _tail = (_tail - 1 + _capacity) % _capacity;
        _buffer[_tail] = default;
        _count--;
    }
}
