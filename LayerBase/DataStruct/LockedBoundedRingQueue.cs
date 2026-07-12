namespace LayerBase.Core.DataStruct;

public sealed class LockedBoundedRingQueue<T> : IBoundedQueue<T>
{
    private readonly T[] _buffer;
    private readonly object _gate = new();
    private int _head;
    private int _tail;
    private int _count;
    private bool _closed;

    public int Capacity => _buffer.Length;

    public bool IsClosed
    {
        get
        {
            lock (_gate)
            {
                return _closed;
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _count;
            }
        }
    }

    public LockedBoundedRingQueue(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _buffer = new T[capacity];
    }

    public bool TryEnqueue(T item)
    {
        lock (_gate)
        {
            if (_closed)
            {
                return false;
            }

            if (_count == _buffer.Length)
            {
                return false;
            }

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;
            return true;
        }
    }

    public bool TryDequeue(out T item)
    {
        lock (_gate)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _buffer[_head];
            _buffer[_head] = default!;
            _head = (_head + 1) % _buffer.Length;
            _count--;
            return true;
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            _closed = true;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}
