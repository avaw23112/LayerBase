using System;
using LayerBase.Core.DataStruct;

namespace LayerBase.Actor.RuntimeCommands;

internal sealed class ActorLifecycleInbox
{
    private readonly LockedBoundedRingQueue<ActorCommandEnvelope> _fastLane;
    private readonly Queue<ActorCommandEnvelope> _overflow = new();
    private readonly object _gate = new();
    private readonly int _overflowCapacity;
    private bool _closed;

    public ActorLifecycleInbox(int capacity)
    {
        _fastLane = new LockedBoundedRingQueue<ActorCommandEnvelope>(Math.Max(16, capacity));
        _overflowCapacity = _fastLane.Capacity;
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

    public bool IsFull
    {
        get
        {
            lock (_gate)
            {
                return _fastLane.Count >= _fastLane.Capacity &&
                       _overflow.Count >= _overflowCapacity;
            }
        }
    }

    public int OverflowCount
    {
        get
        {
            lock (_gate)
            {
                return _overflow.Count;
            }
        }
    }

    public ControlEnqueueResult TryEnqueue(ActorCommandEnvelope envelope)
    {
        lock (_gate)
        {
            if (_closed)
            {
                return ControlEnqueueResult.Closed;
            }

            if (_fastLane.TryEnqueue(envelope))
            {
                return ControlEnqueueResult.AcceptedFast;
            }

            if (_overflow.Count >= _overflowCapacity)
            {
                return ControlEnqueueResult.Failed;
            }

            _overflow.Enqueue(envelope);
            return ControlEnqueueResult.AcceptedOverflow;
        }
    }

    public bool TryDequeue(out ActorCommandEnvelope envelope)
    {
        lock (_gate)
        {
            if (_fastLane.TryDequeue(out envelope))
            {
                return true;
            }

            if (_overflow.Count > 0)
            {
                envelope = _overflow.Dequeue();
                return true;
            }

            envelope = default;
            return false;
        }
    }

    public int Drain(Action<ActorCommandEnvelope> action, int maxCount = 0)
    {
        int drained = 0;
        while (TryDequeue(out ActorCommandEnvelope envelope))
        {
            action(envelope);
            drained++;
            if (maxCount > 0 && drained >= maxCount)
                break;
        }
        return drained;
    }

    public int CloseAndDrain(Action<ActorCommandEnvelope> action)
    {
        ActorCommandEnvelope[] pending;
        lock (_gate)
        {
            _closed = true;
            pending = DetachAllNoLock();
        }

        for (int i = 0; i < pending.Length; i++)
        {
            action(pending[i]);
        }

        return pending.Length;
    }

    public void Clear()
    {
        lock (_gate)
        {
            while (_fastLane.TryDequeue(out _)) { }
            _overflow.Clear();
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            _closed = true;
        }
    }

    private ActorCommandEnvelope[] DetachAllNoLock()
    {
        int count = _fastLane.Count + _overflow.Count;
        if (count == 0)
        {
            return Array.Empty<ActorCommandEnvelope>();
        }

        ActorCommandEnvelope[] pending = new ActorCommandEnvelope[count];
        int index = 0;
        while (_fastLane.TryDequeue(out ActorCommandEnvelope envelope))
        {
            pending[index++] = envelope;
        }

        while (_overflow.Count > 0)
        {
            pending[index++] = _overflow.Dequeue();
        }

        return pending;
    }
}
