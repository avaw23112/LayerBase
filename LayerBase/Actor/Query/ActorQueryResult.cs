namespace LayerBase.Actor;

public readonly struct ActorQueryResult
{
    private readonly ActorWorld _world;
    internal readonly ActorQueryCache Cache;

    internal ActorQueryResult(ActorWorld world, ActorQueryCache cache)
    {
        _world = world;
        Cache = cache;
    }

    public IEnumerable<IActor> DebugActors
    {
        get
        {
            foreach (BehaviourArchetype archetype in Cache.Archetypes)
            {
                foreach (IActor actor in archetype.EnumerateActors())
                {
                    yield return actor;
                }
            }
        }
    }
}
