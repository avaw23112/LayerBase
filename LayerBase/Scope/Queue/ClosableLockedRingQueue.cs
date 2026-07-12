using System;
using System.Threading;
using LayerBase.Core.DataStruct;

namespace LayerBase.Scope.Queue;

internal sealed class ClosableLockedRingQueue<T> : IClosableBoundedQueue<T>
{
    private readonly LockedBoundedRingQueue<T> _inner;
    private readonly object _gate = new();
    private bool _closed;
    private readonly int _capacity;

    public ClosableLockedRingQueue(int capacity)
    {
        _inner = new LockedBoundedRingQueue<T>(capacity);
        _capacity = capacity;
    }

    public int Count
    {
        get { lock (_gate) return _inner.Count; }
    }

    public int Capacity => _capacity;

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

            if (_inner.TryEnqueue(item))
                return QueueEnqueueResult.Accepted;

            if (_closed)
                return QueueEnqueueResult.Closed;

            return QueueEnqueueResult.Full;
        }
    }

    public bool TryDequeue(out T item)
    {
        lock (_gate)
        {
            return _inner.TryDequeue(out item);
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
        lock (_gate)
        {
            _closed = true;
            while (_inner.TryDequeue(out T item))
            {
                drain(item);
            }
        }
    }
}
