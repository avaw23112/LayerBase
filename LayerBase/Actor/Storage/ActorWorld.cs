namespace LayerBase.Actor;

public sealed partial class ActorWorld : IDisposable
{
    internal readonly int RuntimeIndex;
    private BehaviourArchetype[] _archetypes = Array.Empty<BehaviourArchetype>();
    private readonly Dictionary<ActorArchetypeKey, BehaviourArchetype> _archetypeMap = new();
    private readonly Dictionary<ActorQueryDescriptor, ActorQueryCache> _queryCacheByDescriptor = new();
    internal GlobalEventMailPoolRegistry GlobalEventMailPools { get; } = new();
    private IActorCallBucket[] _callBucketsByRouteId = Array.Empty<IActorCallBucket>();
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

    private List<IEventStreamCenterRuntime> _eventStreamRuntimes = new();
    private readonly List<Action> _eventStreamUnbinders = new();
    private readonly DirtyBucketList _dirtyEventStreams = new();

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

    internal EventStreamRuntime<TEvent> GetOrCreateEventStreamRuntime<TEvent>(
        ActorEventStreamPlan<TEvent> plan,
        int                          archetypeId = 0)
        where TEvent : struct
    {
        foreach (IEventStreamCenterRuntime existing in _eventStreamRuntimes)
        {
            if (existing is EventStreamRuntime<TEvent> typedExisting &&
                typedExisting.RuntimeIndex == RuntimeIndex &&
                typedExisting.ArchetypeId == archetypeId)
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
