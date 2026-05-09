namespace LayerBase.Actor;

public sealed partial class ActorWorld : IDisposable
{
    private BehaviourArchetype[] _archetypes = Array.Empty<BehaviourArchetype>();
    private readonly Dictionary<ActorArchetypeKey, BehaviourArchetype> _archetypeMap = new();
    private readonly Dictionary<ActorQueryDescriptor, ActorQueryCache> _queryCacheByDescriptor = new();
    private IActorEventBucket[] _eventBucketsByEventId = Array.Empty<IActorEventBucket>();
    private IActorEventBucket[] _callBucketsByRouteId = Array.Empty<IActorEventBucket>();
    private int _bucketCursor;
    private int _callBucketCursor;
    private readonly ActorMailPumpStatsBuilder _mailPumpStatsBuilder = new();
    internal int QueryVersion { get; private set; }
    public ActorMailPumpOptions MailPumpOptions { get; set; }
    public ActorMailPumpStats LastMailPumpStats { get; private set; }
    internal ActorLifecycleScheduler Lifecycle { get; }
    internal ActorDelayScheduler DelayScheduler { get; }
    private bool _hasPendingDestroy;
    internal LayerRuntime? Runtime { get; }
    internal ActorMailOptions DefaultMailOptions { get; }
    private ActorWorldState _state;

    internal ActorWorld()
    {
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
        DefaultMailOptions = defaultMailOptions;
        MailPumpOptions = ActorMailPumpOptions.Default;
        LastMailPumpStats = default;
        Lifecycle = new ActorLifecycleScheduler(this);
        DelayScheduler = new ActorDelayScheduler(this, ActorTimeWheelOptions.Default);
        _state = ActorWorldState.Running;
    }

    internal ActorWorld(LayerRuntime runtime)
    {
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

    internal void RegisterColumn<TEvent>(int eventTypeId, IActorEventColumn<TEvent> column)
        where TEvent : struct
    {
        EnsureEventBucketCapacity(eventTypeId);

        if (_eventBucketsByEventId[eventTypeId] is not ActorEventBucket<TEvent> bucket)
        {
            bucket = new ActorEventBucket<TEvent>();
            _eventBucketsByEventId[eventTypeId] = bucket;
        }

        bucket.AddColumn(column);
    }

    internal void RegisterCallColumn<TRequest, TResponse>(int routeId, IActorCallColumn<TRequest, TResponse> column)
        where TRequest : struct
        where TResponse : struct
    {
        EnsureCallBucketCapacity(routeId);

        if (_callBucketsByRouteId[routeId] is not ActorCallBucket<TRequest, TResponse> bucket)
        {
            bucket = new ActorCallBucket<TRequest, TResponse>();
            _callBucketsByRouteId[routeId] = bucket;
        }

        bucket.AddColumn(column);
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
