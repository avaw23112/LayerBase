using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public PostResult Post<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        return TryPost(actorId, in value, postPolicy, fullPolicy);
    }

    public PostResult TryPost<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return PostResult.Failure("Invalid ActorId.ArchetypeId.", PostFailureKind.InvalidActorId);
        }

        BehaviourArchetype archetype = _archetypes[actorId.ArchetypeId];
        return archetype.Post(actorId, in value, postPolicy, fullPolicy);
    }

    public void PostMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        foreach (ActorId actorId in actorIds)
        {
            _ = TryPost(actorId, in value, postPolicy, fullPolicy);
        }
    }
}
