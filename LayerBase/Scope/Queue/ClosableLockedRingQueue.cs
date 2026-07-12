using System;
using System.Threading;
using LayerBase.Core.DataStruct;

namespace LayerBase.Scope.Queue;

internal sealed class ClosableLockedRingQueue<T> : IClosableBoundedQueue<T>
{
    private readonly LockedBoundedRingQueue<T> _inner;
    private volatile bool _closed;
    private readonly int _capacity;

    public ClosableLockedRingQueue(int capacity)
    {
        _inner = new LockedBoundedRingQueue<T>(capacity);
        _capacity = capacity;
    }

    public int Count => _inner.Count;
    public int Capacity => _capacity;
    public bool IsClosed => _closed;

    public QueueEnqueueResult TryEnqueue(in T item)
    {
        if (_closed)
            return QueueEnqueueResult.Closed;

        if (_inner.TryEnqueue(item))
            return QueueEnqueueResult.Accepted;

        if (_closed)
            return QueueEnqueueResult.Closed;

        return QueueEnqueueResult.Full;
    }

    public bool TryDequeue(out T item)
    {
        return _inner.TryDequeue(out item);
    }

    public void Close()
    {
        _closed = true;
    }

    public void CloseAndDrain(Action<T> drain)
    {
        _closed = true;
        while (_inner.TryDequeue(out T item))
        {
            drain(item);
        }
    }
}
