using System;
using LayerBase.Core.DataStruct;

namespace LayerBase.Actor.RuntimeCommands;

internal sealed class ActorLifecycleInbox
{
    private readonly LockedBoundedRingQueue<ActorCommandEnvelope> _queue;

    public ActorLifecycleInbox(int capacity)
    {
        _queue = new LockedBoundedRingQueue<ActorCommandEnvelope>(capacity > 0 ? capacity : 128);
    }

    public int Count => _queue.Count;
    public bool IsFull => _queue.Count >= _queue.Capacity;

    public bool TryEnqueue(ActorCommandEnvelope envelope)
    {
        return _queue.TryEnqueue(envelope);
    }

    public bool TryDequeue(out ActorCommandEnvelope envelope)
    {
        return _queue.TryDequeue(out envelope);
    }

    public int Drain(Action<ActorCommandEnvelope> action, int maxCount = 0)
    {
        int drained = 0;
        while (_queue.TryDequeue(out ActorCommandEnvelope envelope))
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
        while (_queue.TryDequeue(out _)) { }
    }

    public void Close()
    {
        _queue.Close();
    }
}
