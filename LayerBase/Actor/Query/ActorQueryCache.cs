namespace LayerBase.Actor;

internal sealed class ActorQueryCache
{
    public ActorQueryDescriptor Descriptor { get; }
    public BehaviourArchetype[] Archetypes { get; }

    public ActorQueryCache(ActorQueryDescriptor descriptor, BehaviourArchetype[] archetypes)
    {
        Descriptor = descriptor;
        Archetypes = archetypes ?? throw new ArgumentNullException(nameof(archetypes));
    }
}