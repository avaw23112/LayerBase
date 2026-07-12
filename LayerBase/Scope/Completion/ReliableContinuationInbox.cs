using System;
using System.Threading;
using LayerBase.Async;
using LayerBase.Core.DataStruct;

namespace LayerBase.Scope.Completion;

internal sealed class ReliableContinuationInbox
{
    private readonly LockedBoundedRingQueue<LayerContinuation> _fastLane;
    private readonly object _overflowGate = new();
    private readonly System.Collections.Generic.Queue<LayerContinuation> _overflow = new();
    private volatile bool _closed;

    public ReliableContinuationInbox(int capacity)
    {
        _fastLane = new LockedBoundedRingQueue<LayerContinuation>(capacity > 0 ? capacity : 64);
    }

    public int Count
    {
        get
        {
            int fastCount = _fastLane.Count;
            lock (_overflowGate)
            {
                return fastCount + _overflow.Count;
            }
        }
    }

    public int FastLaneCapacity => _fastLane.Capacity;

    public int OverflowCount
    {
        get
        {
            lock (_overflowGate) return _overflow.Count;
        }
    }

    public bool IsClosed => _closed;

    public bool TryEnqueue(in LayerContinuation continuation)
    {
        if (_closed) return false;

        if (_fastLane.TryEnqueue(continuation))
            return true;

        lock (_overflowGate)
        {
            if (_closed) return false;
            _overflow.Enqueue(continuation);
        }

        return true;
    }

    public bool TryDequeue(out LayerContinuation continuation)
    {
        if (_fastLane.TryDequeue(out continuation))
            return true;

        lock (_overflowGate)
        {
            if (_overflow.Count > 0)
            {
                continuation = _overflow.Dequeue();
                return true;
            }
        }

        continuation = default;
        return false;
    }

    public void Close()
    {
        _closed = true;
    }

    public void Drain(Action<LayerContinuation> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        while (_fastLane.TryDequeue(out LayerContinuation continuation))
        {
            action(continuation);
        }

        lock (_overflowGate)
        {
            while (_overflow.Count > 0)
            {
                action(_overflow.Dequeue());
            }
        }
    }

    public void Clear()
    {
        while (_fastLane.TryDequeue(out _)) { }

        lock (_overflowGate)
        {
            _overflow.Clear();
        }
    }
}
