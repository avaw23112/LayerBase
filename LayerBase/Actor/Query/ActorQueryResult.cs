namespace LayerBase.Actor;

public readonly struct ActorQueryResult
{
    private readonly ActorWorld _world;
    internal readonly ActorQueryCache Cache;
    private readonly int _version;

    internal ActorQueryResult(ActorWorld world, ActorQueryCache cache, int version)
    {
        _world = world;
        Cache = cache;
        _version = version;
    }

    public bool IsValid => _world.QueryVersion == _version;

    public ActorQueryResult RefreshIfNeeded()
    {
        if (IsValid)
        {
            return this;
        }

        return _world.RebuildQuery(Cache.Descriptor);
    }

    public IEnumerable<IActor> DebugActors
    {
        get
        {
            ActorQueryResult query = RefreshIfNeeded();
            foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
            {
                foreach (IActor actor in archetype.EnumerateActors())
                {
                    yield return actor;
                }
            }
        }
    }
}
