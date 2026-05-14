namespace LayerBase.Actor;

public sealed partial class ActorWorld : IDisposable
{
    internal readonly int RuntimeIndex;
    private BehaviourArchetype[] _archetypes = Array.Empty<BehaviourArchetype>();
    private readonly Dictionary<ActorArchetypeKey, BehaviourArchetype> _archetypeMap = new();
    private readonly Dictionary<ActorQueryDescriptor, ActorQueryCache> _queryCacheByDescriptor = new();
    private IActorEventBucket[] _eventBucketsByEventId = Array.Empty<IActorEventBucket>();
    private IActorEventBucket[] _callBucketsByRouteId = Array.Empty<IActorEventBucket>();
    private readonly DirtyBucketList _dirtyEventBuckets = new();
    internal GlobalEventMailPoolRegistry GlobalEventMailPools { get; } = new();
    private readonly List<Action> _eventPostRuntimeUnbinders = new();
    private readonly DirtyBucketList _dirtyCallBuckets = new();
    private int _bucketCursor;
    private int _callBucketCursor;
    private readonly ActorMailPumpStatsBuilder _mailPumpStatsBuilder = new();
    internal int QueryVersion { get; private set; }
    public ActorMailPumpOptions MailPumpOptions { get; set; }
    public ActorMailPumpStats LastMailPumpStats { get; private set; }
    internal ActorLifecycleScheduler Lifecycle { get; }
    internal ActorDelayScheduler DelayScheduler { get; }
    private int _pendingDestroyCount;
    internal LayerRuntime? Runtime { get; }
    internal ActorMailOptions DefaultMailOptions { get; }
    private ActorWorldState _state;

    // EventStream 运行时管理
    private List<IEventStreamCenterRuntime> _eventStreamRuntimes = new();
    private readonly List<Action> _eventStreamUnbinders = new();
    private readonly DirtyBucketList _dirtyEventStreams = new();

    /// <summary>
    /// 是否注册过 Call Bucket。
    /// 用于跳过空 Call Bucket 的检查。
    /// </summary>
    private bool _hasCallBuckets;

    internal ActorWorld()
    {
        RuntimeIndex = ActorWorldRuntimeIndexAllocator.Rent();
        DefaultMailOptions = ActorMailOptions.Default;
        MailPumpOptions = ActorMailPumpOptions.Default;
        LastMailPumpStats = default;
        Lifecycle = new ActorLifecycleScheduler(this);
        DelayScheduler = new ActorDelayScheduler(this, ActorTimeWheelOptions.Default);
        _state = ActorWorldState.Running;
    }

    internal bool IsLifecycleRunnable(ActorId actorId)
    {
        // actorId 参数表示要检查的 Actor。
        // 返回 true 表示该 Actor 仍然 Alive，并且 Enable=true。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId]
            .IsLifecycleRunnable(actorId);
    }

    internal ActorWorld(ActorMailOptions defaultMailOptions)
    {
        RuntimeIndex = ActorWorldRuntimeIndexAllocator.Rent();
        DefaultMailOptions = defaultMailOptions;
        MailPumpOptions = ActorMailPumpOptions.Default;
        LastMailPumpStats = default;
        Lifecycle = new ActorLifecycleScheduler(this);
        DelayScheduler = new ActorDelayScheduler(this, ActorTimeWheelOptions.Default);
        _state = ActorWorldState.Running;
    }

    internal ActorWorld(LayerRuntime runtime)
    {
        RuntimeIndex = ActorWorldRuntimeIndexAllocator.Rent();
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        DefaultMailOptions = ActorMailOptions.Default;
        MailPumpOptions = ActorMailPumpOptions.Default;
        LastMailPumpStats = default;
        Lifecycle = new ActorLifecycleScheduler(this);
        DelayScheduler = new ActorDelayScheduler(this, ActorTimeWheelOptions.Default);
        _state = ActorWorldState.Created;
    }

    private BehaviourArchetype GetOrCreateArchetype(ActorArchetypeKey key)
    {
        if (_archetypeMap.TryGetValue(key, out BehaviourArchetype? existing))
        {
            return existing;
        }

        int archetypeId = _archetypes.Length;
        var archetype = new BehaviourArchetype(
            archetypeId,
            key.Behaviour,
            key.Tags,
            key.Groups);

        Array.Resize(ref _archetypes, archetypeId + 1);
        _archetypes[archetypeId] = archetype;
        _archetypeMap.Add(key, archetype);

        InvalidateQueryCache();
        return archetype;
    }

    private void InvalidateQueryCache()
    {
        _queryCacheByDescriptor.Clear();
        QueryVersion++;
    }

    internal void RegisterColumn<TEvent>(int eventTypeId, ActorEventColumnRuntime column)
        where TEvent : struct
    {
        EnsureEventBucketCapacity(eventTypeId);

        if (_eventBucketsByEventId[eventTypeId] is not ActorEventBucket<TEvent> bucket)
        {
            bucket = new ActorEventBucket<TEvent>();
            _eventBucketsByEventId[eventTypeId] = bucket;
        }

        bucket.AddColumn(column);
        column.BindDirtyBucket(_dirtyEventBuckets, eventTypeId);
    }

    internal void RegisterCallColumn<TRequest, TResponse>(int routeId, ActorCallColumnRuntime column)
        where TRequest : struct
        where TResponse : struct
    {
        EnsureCallBucketCapacity(routeId);

        if (_callBucketsByRouteId[routeId] is not ActorCallBucket<TRequest, TResponse> bucket)
        {
            bucket = new ActorCallBucket<TRequest, TResponse>();
            _callBucketsByRouteId[routeId] = bucket;
            _hasCallBuckets = true;
        }

        bucket.AddColumn(column);
        column.BindDirtyBucket(_dirtyCallBuckets, routeId);
    }

    /// <summary>
    /// 获取或创建 EventStream 运行时。
    ///
    /// 作用：
    /// 如果指定事件类型的 EventStream 运行时已存在，直接返回。
    /// 否则创建新的运行时并注册到当前 ActorWorld。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    /// <param name="plan">
    /// EventStream 构建计划。
    /// </param>
    /// <returns>
    /// EventStream 运行时实例。
    /// </returns>
    internal EventStreamRuntime<TEvent> GetOrCreateEventStreamRuntime<TEvent>(
        ActorEventStreamPlan<TEvent> plan,
        int                          archetypeId = 0)
        where TEvent : struct
    {
        // 检查是否已存在（使用完整 key 查找）
        int eventTypeId = plan.EventId;
        int searchKey = (RuntimeIndex << 20) | (archetypeId << 10) | eventTypeId;
        foreach (var existing in _eventStreamRuntimes)
        {
            if (existing is EventStreamRuntime<TEvent> typedExisting &&
                typedExisting.SearchKey == searchKey)
            {
                return typedExisting;
            }
        }

        var runtime = new EventStreamRuntime<TEvent>(
            RuntimeIndex,
            archetypeId,
            plan.StreamOptions);

        _eventStreamRuntimes.Add(runtime);
        EventStreamRuntime<TEvent>.BindWorld(runtime);

        _eventStreamUnbinders.Add(() =>
        {
            EventStreamRuntime<TEvent>.UnbindWorld(RuntimeIndex, archetypeId);
        });

        return runtime;
    }

    private void EnsureEventStreamCapacity(int eventTypeId)
    {
        // 不再需要，因为使用 List
    }

    /// <summary>
    /// 注销指定 archetype 和 slot 的 EventStream handler。
    /// </summary>
    internal void UnregisterStreamHandler(int archetypeId, int slotIndex, Type eventType)
    {
        foreach (var runtime in _eventStreamRuntimes)
        {
            if (runtime is EventStreamRuntimeBase streamRuntime)
            {
                streamRuntime.UnregisterHandler(slotIndex);
            }
        }
    }

    internal ActorMailOptions ResolveMailOptions(int eventTypeId)
    {
        if (Runtime?.PolicyTable != null)
        {
            return Runtime.PolicyTable.GetActorMailOptions(eventTypeId);
        }

        return DefaultMailOptions;
    }

    private void EnsureEventBucketCapacity(int eventTypeId)
    {
        if ((uint)eventTypeId < (uint)_eventBucketsByEventId.Length)
        {
            return;
        }

        int newSize = _eventBucketsByEventId.Length == 0 ? 4 : _eventBucketsByEventId.Length;
        while (newSize <= eventTypeId)
        {
            newSize *= 2;
        }

        Array.Resize(ref _eventBucketsByEventId, newSize);
    }

    private void EnsureCallBucketCapacity(int routeId)
    {
        if ((uint)routeId < (uint)_callBucketsByRouteId.Length)
        {
            return;
        }

        int newSize = _callBucketsByRouteId.Length == 0 ? 4 : _callBucketsByRouteId.Length;
        while (newSize <= routeId)
        {
            newSize *= 2;
        }

        Array.Resize(ref _callBucketsByRouteId, newSize);
    }
}