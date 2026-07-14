using System.Buffers;
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;
using LayerBase.ECS.Runtime;

namespace LayerBase.ECS.Projection.Flow;

internal struct ProjectionBatchBuffer<TEvent> : IDisposable
    where TEvent : struct
{
    private ActorId[] _actorIds;
    private TEvent[] _events;

    public int Count { get; private set; }

    /// <summary>
    /// GrowCount 作用：
    /// 记录 Grow 调用次数，用于测试容量预测。
    /// </summary>
    internal int GrowCount { get; private set; }

    private ProjectionBatchBuffer(ActorId[] actorIds, TEvent[] events)
    {
        _actorIds = actorIds;
        _events = events;
        Count = 0;
        GrowCount = 0;
    }

    /// <summary>
    /// Rent 支持 initialCapacity 参数。
    ///
    /// 参数说明：
    /// initialCapacity：初始容量，用于容量预测。
    /// </summary>
    public static ProjectionBatchBuffer<TEvent> Rent(int initialCapacity = 64)
    {
        int safeCapacity = initialCapacity <= 0
            ? 64
            : initialCapacity;

        return new ProjectionBatchBuffer<TEvent>(
            ArrayPool<ActorId>.Shared.Rent(safeCapacity),
            ArrayPool<TEvent>.Shared.Rent(safeCapacity));
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
        GrowCount++;
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
        int i = 0;
        int length = Count;
        int unrolledLength = length - (length % 4);
        
        for (; i < unrolledLength; i += 4)
        {
            actorWorld.PostTo(_actorIds[i], in _events[i]);
            actorWorld.PostTo(_actorIds[i + 1], in _events[i+1]);
            actorWorld.PostTo(_actorIds[i + 2], in _events[i+2]);
            actorWorld.PostTo(_actorIds[i + 3], in _events[i+3]);
        }
        for (; i < length; i++)
        {
            actorWorld.PostTo(_actorIds[i], in _events[i]);
        }
    }

    public void PostToRuntimeOwner(LayerRuntime runtime)
    {
        if (Count == 0)
        {
            return;
        }

        ActorId[] actorIds = new ActorId[Count];
        TEvent[] events = new TEvent[Count];
        Array.Copy(_actorIds, actorIds, Count);
        Array.Copy(_events, events, Count);

        Action<ActorWorld> postAction = world =>
        {
            for (int i = 0; i < actorIds.Length; i++)
            {
                world.PostTo(actorIds[i], in events[i]);
            }
        };

        int payloadHandle = runtime.ActorPayloads.Store(postAction);
        var envelope = new ActorCommandEnvelope(
            ActorCommandKind.PostMany,
            ActorId.Invalid,
            routeId: 0,
            payloadHandle: payloadHandle);
        if (!runtime.EnqueueActorEvent(envelope))
        {
            runtime.ActorPayloads.Free(payloadHandle);
        }
    }

    public void PostToOrEnqueue(World world, ActorWorld actorWorld, string debugName)
    {
        if (!world.TryGetRuntime(out LayerRuntime? runtime) ||
            runtime == null ||
            !EcsThreadGuard.TryGetCurrentResultQueue(runtime.Id, out EcsResultQueue? results) ||
            results == null)
        {
            PostTo(actorWorld);
            return;
        }

        results.Enqueue(new ActorEventBatchResult<TEvent>(debugName, this, actorWorld));
        _actorIds = Array.Empty<ActorId>();
        _events = Array.Empty<TEvent>();
        Count = 0;
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
