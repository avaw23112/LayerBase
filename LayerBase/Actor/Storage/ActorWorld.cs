namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    private BehaviourArchetype[] _archetypes = Array.Empty<BehaviourArchetype>();
    private readonly Dictionary<BehaviourSignature, BehaviourArchetype> _archetypeMap = new();
    private readonly Dictionary<BehaviourSignature, ActorQueryCache> _queryCacheBySignature = new();
    private IActorEventBucket[] _eventBucketsByEventId = Array.Empty<IActorEventBucket>();
    private int _bucketCursor;
    internal ActorLifecycleScheduler Lifecycle { get; }
    private bool _hasPendingDestroy;
    internal LayerRuntime? Runtime { get; }
    internal ActorMailOptions DefaultMailOptions { get; }

    internal ActorWorld()
    {
        DefaultMailOptions = ActorMailOptions.Default;
        Lifecycle = new ActorLifecycleScheduler(this);
    }

    internal ActorWorld(ActorMailOptions defaultMailOptions)
    {
        DefaultMailOptions = defaultMailOptions;
        Lifecycle = new ActorLifecycleScheduler(this);
    }

    internal ActorWorld(LayerRuntime runtime)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        DefaultMailOptions = ActorMailOptions.Default;
        Lifecycle = new ActorLifecycleScheduler(this);
    }

    private BehaviourArchetype GetOrCreateArchetype(BehaviourSignature signature)
    {
        if (_archetypeMap.TryGetValue(signature, out BehaviourArchetype? existing))
        {
            return existing;
        }

        int archetypeId = _archetypes.Length;
        var archetype = new BehaviourArchetype(archetypeId, signature);

        Array.Resize(ref _archetypes, archetypeId + 1);
        _archetypes[archetypeId] = archetype;
        _archetypeMap.Add(signature, archetype);

        InvalidateQueryCache();
        return archetype;
    }

    private void InvalidateQueryCache()
    {
        _queryCacheBySignature.Clear();
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
}
