using System.Buffers;
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Flow;

internal struct ProjectionBatchBuffer<TEvent> : IDisposable
    where TEvent : struct
{
    private ActorId[] _actorIds;
    private TEvent[] _events;

    public int Count { get; private set; }

    private ProjectionBatchBuffer(ActorId[] actorIds, TEvent[] events)
    {
        _actorIds = actorIds;
        _events = events;
        Count = 0;
    }

    public static ProjectionBatchBuffer<TEvent> Rent(int initialCapacity = 64)
    {
        return new ProjectionBatchBuffer<TEvent>(
            ArrayPool<ActorId>.Shared.Rent(initialCapacity),
            ArrayPool<TEvent>.Shared.Rent(initialCapacity));
    }

    public void Add(ActorId actorId, in TEvent value)
    {
        int index = Count;
        if ((uint)index >= (uint)_actorIds.Length)
        {
            Grow();
        }

        _actorIds[index] = actorId;
        _events[index] = value;
        Count = index + 1;
    }

    private void Grow()
    {
        int newLength = _actorIds.Length << 1;
        ActorId[] newActorIds = ArrayPool<ActorId>.Shared.Rent(newLength);
        TEvent[] newEvents = ArrayPool<TEvent>.Shared.Rent(newLength);

        Array.Copy(_actorIds, newActorIds, Count);
        Array.Copy(_events, newEvents, Count);

        ArrayPool<ActorId>.Shared.Return(_actorIds, clearArray: false);
        ArrayPool<TEvent>.Shared.Return(_events, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>());

        _actorIds = newActorIds;
        _events = newEvents;
    }

    public void PostTo(ActorWorld actorWorld)
    {
        for (int i = 0; i < Count; i++)
        {
            _ = actorWorld.PostTo(_actorIds[i], in _events[i]);
        }
    }

    public void Dispose()
    {
        ArrayPool<ActorId>.Shared.Return(_actorIds, clearArray: false);
        ArrayPool<TEvent>.Shared.Return(_events, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>());
        _actorIds = Array.Empty<ActorId>();
        _events = Array.Empty<TEvent>();
        Count = 0;
    }
}