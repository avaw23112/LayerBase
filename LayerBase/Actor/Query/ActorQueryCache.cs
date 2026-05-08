namespace LayerBase.Actor;

internal sealed class ActorQueryCache
{
    public BehaviourSignature QuerySignature { get; }
    public BehaviourArchetype[] Archetypes { get; }

    public ActorQueryCache(BehaviourSignature querySignature, BehaviourArchetype[] archetypes)
    {
        QuerySignature = querySignature;
        Archetypes = archetypes ?? throw new ArgumentNullException(nameof(archetypes));
    }
}
