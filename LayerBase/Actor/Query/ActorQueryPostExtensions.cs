namespace LayerBase.Actor;

public static class ActorQueryPostExtensions
{
    public static void PostAll<TEvent>(
        this ActorQueryResult query,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value, postPolicy, fullPolicy);
        }
    }
}
