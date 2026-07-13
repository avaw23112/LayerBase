using System;
using LayerBase.Core.DataStruct;

namespace LayerBase.Actor.RuntimeCommands;

internal sealed class ActorLifecycleInbox
{
    private readonly LockedBoundedRingQueue<ActorCommandEnvelope> _fastLane;
    private readonly Queue<ActorCommandEnvelope> _overflow = new();
    private readonly object _gate = new();
    private bool _closed;

    public ActorLifecycleInbox(int capacity)
    {
        _fastLane = new LockedBoundedRingQueue<ActorCommandEnvelope>(Math.Max(16, capacity));
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

    public bool IsFull => false;

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
}
