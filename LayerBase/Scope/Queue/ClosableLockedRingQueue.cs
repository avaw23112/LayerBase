using System;

namespace LayerBase.Scope.Queue;

internal sealed class ClosableLockedRingQueue<T> : IClosableBoundedQueue<T>
{
    private readonly T[] _buffer;
    private readonly object _gate = new();
    private int _head;
    private int _tail;
    private int _count;
    private bool _closed;

    public ClosableLockedRingQueue(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _buffer = new T[capacity];
    }

    public int Count
    {
        get { lock (_gate) return _count; }
    }

    public int Capacity => _buffer.Length;

    public bool IsClosed
    {
        get { lock (_gate) return _closed; }
    }

    public QueueEnqueueResult TryEnqueue(in T item)
    {
        lock (_gate)
        {
            if (_closed)
                return QueueEnqueueResult.Closed;

            if (_count == _buffer.Length)
                return QueueEnqueueResult.Full;

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;
            return QueueEnqueueResult.Accepted;
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

    public void CloseAndDrain(Action<T> drain)
    {
        if (drain == null)
        {
            throw new ArgumentNullException(nameof(drain));
        }

        T[] items;
        int count;
        lock (_gate)
        {
            _closed = true;
            count = _count;
            items = new T[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = _buffer[_head];
                _buffer[_head] = default!;
                _head = (_head + 1) % _buffer.Length;
            }

            _tail = _head;
            _count = 0;
        }

        for (int i = 0; i < count; i++)
        {
            drain(items[i]);
        }
    }
}
