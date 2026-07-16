using System.Buffers;
using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Scope;

namespace LayerBase.ECS.Projection.Flow;

internal struct ProjectionBatchBuffer<TEvent> : IDisposable
    where TEvent : struct
{
    private ActorId[] _actorIds;
    private TEvent[] _events;
    private IProjectedActorCommandSink? _autoFlushSink;
    private int _autoFlushLimit;

    public int Count { get; private set; }

    /// <summary>
    /// GrowCount 作用：
    /// 记录 Grow 调用次数，用于测试容量预测。
    /// </summary>
    internal int GrowCount { get; private set; }

    private ProjectionBatchBuffer(
        ActorId[] actorIds,
        TEvent[] events,
        IProjectedActorCommandSink? autoFlushSink,
        int autoFlushLimit)
    {
        _actorIds = actorIds;
        _events = events;
        _autoFlushSink = autoFlushSink;
        _autoFlushLimit = Math.Max(0, autoFlushLimit);
        Count = 0;
        GrowCount = 0;
    }

    /// <summary>
    /// Rent 支持 initialCapacity 参数。
    ///
    /// 参数说明：
    /// initialCapacity：初始容量，用于容量预测。
    /// </summary>
    public static ProjectionBatchBuffer<TEvent> Rent(
        int initialCapacity = 64,
        IProjectedActorCommandSink? autoFlushSink = null,
        int autoFlushLimit = 0)
    {
        int safeCapacity = initialCapacity <= 0
            ? 64
            : initialCapacity;

        return new ProjectionBatchBuffer<TEvent>(
            ArrayPool<ActorId>.Shared.Rent(safeCapacity),
            ArrayPool<TEvent>.Shared.Rent(safeCapacity),
            autoFlushSink,
            autoFlushLimit);
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

        if (_autoFlushSink != null &&
            _autoFlushLimit > 0 &&
            Count >= _autoFlushLimit)
        {
            FlushTo(_autoFlushSink);
        }
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

    public void FlushTo(IProjectedActorCommandSink commandSink)
    {
        int capacity = _actorIds.Length;
        IProjectedActorCommandSink? autoFlushSink = _autoFlushSink;
        int autoFlushLimit = _autoFlushLimit;
        commandSink.PostBatch(ref this);
        if (_actorIds.Length == 0)
        {
            int safeCapacity = capacity <= 0 ? 64 : capacity;
            _actorIds = ArrayPool<ActorId>.Shared.Rent(safeCapacity);
            _events = ArrayPool<TEvent>.Shared.Rent(safeCapacity);
        }

        _autoFlushSink = autoFlushSink;
        _autoFlushLimit = autoFlushLimit;
        Count = 0;
    }

    internal ProjectionBatchLease<TEvent> Detach()
    {
        var value = new ProjectionBatchLease<TEvent>(
            _actorIds,
            _events,
            Count);
        _actorIds = Array.Empty<ActorId>();
        _events = Array.Empty<TEvent>();
        _autoFlushSink = null;
        _autoFlushLimit = 0;
        Count = 0;
        return value;
    }

    internal ActorPostBatchScopeEvent<TEvent> DetachToScopeEvent()
    {
        var value = new ActorPostBatchScopeEvent<TEvent>(
            _actorIds,
            _events,
            Count);
        _actorIds = Array.Empty<ActorId>();
        _events = Array.Empty<TEvent>();
        _autoFlushSink = null;
        _autoFlushLimit = 0;
        Count = 0;
        return value;
    }

    public void Dispose()
    {
        if (_actorIds.Length > 0)
            ArrayPool<ActorId>.Shared.Return(_actorIds, clearArray: false);
        if (_events.Length > 0)
            ArrayPool<TEvent>.Shared.Return(_events, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>());
        _actorIds = Array.Empty<ActorId>();
        _events = Array.Empty<TEvent>();
        _autoFlushSink = null;
        _autoFlushLimit = 0;
        Count = 0;
    }
}
