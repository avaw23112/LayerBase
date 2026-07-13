using System;
using System.Threading;
using LayerBase.Async;
using LayerBase.Core.DataStruct;

namespace LayerBase.Scope.Completion;

internal sealed class ReliableContinuationInbox
{
    private readonly LockedBoundedRingQueue<LayerContinuation> _fastLane;
    private readonly int _overflowCapacity;
    private readonly object _gate = new();
    private readonly System.Collections.Generic.Queue<LayerContinuation> _overflow = new();
    private bool _closed;

    public ReliableContinuationInbox(int capacity)
    {
        int normalizedCapacity = capacity > 0 ? capacity : 64;
        _fastLane = new LockedBoundedRingQueue<LayerContinuation>(normalizedCapacity);
        _overflowCapacity = normalizedCapacity;
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _fastLane.Count + _overflow.Count;
            }
        }
    }

    public int FastLaneCapacity => _fastLane.Capacity;

    public int OverflowCount
    {
        get
        {
            lock (_gate) return _overflow.Count;
        }
    }

    public bool IsClosed
    {
        get
        {
            lock (_gate) return _closed;
        }
    }

    public bool TryEnqueue(in LayerContinuation continuation)
    {
        lock (_gate)
        {
            if (_closed) return false;

            if (_fastLane.TryEnqueue(continuation))
                return true;

            if (_overflow.Count >= _overflowCapacity)
                return false;

            _overflow.Enqueue(continuation);
            return true;
        }
    }

    public bool TryDequeue(out LayerContinuation continuation)
    {
        lock (_gate)
        {
            if (_fastLane.TryDequeue(out continuation))
                return true;

            if (_overflow.Count > 0)
            {
                continuation = _overflow.Dequeue();
                return true;
            }

            continuation = default;
            return false;
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            _closed = true;
        }
    }

    public void Drain(Action<LayerContinuation> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        LayerContinuation[] buffer = new LayerContinuation[32];
        while (true)
        {
            int count = DetachBatch(buffer);
            if (count == 0)
            {
                break;
            }

            for (int i = 0; i < count; i++)
            {
                action(buffer[i]);
                buffer[i] = default;
            }
        }
    }

    public int DetachBatch(Span<LayerContinuation> destination)
    {
        if (destination.Length == 0) return 0;

        lock (_gate)
        {
            int count = 0;
            while (count < destination.Length &&
                   _fastLane.TryDequeue(out LayerContinuation continuation))
            {
                destination[count++] = continuation;
            }

            while (count < destination.Length &&
                   _overflow.Count > 0)
            {
                destination[count++] = _overflow.Dequeue();
            }

            return count;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            while (_fastLane.TryDequeue(out _)) { }
            _overflow.Clear();
        }
    }
}
