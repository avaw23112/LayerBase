using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult PostTo<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        return TryPostTo(actorId, in value, postPolicy, fullPolicy);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPostTo<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                "Invalid ActorId.ArchetypeId.",
                PostFailureKind.InvalidActorId);
        }

        BehaviourArchetype archetype = _archetypes[actorId.ArchetypeId];
        return archetype.Post(actorId, in value, postPolicy, fullPolicy);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostToMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        foreach (ActorId actorId in actorIds)
        {
            _ = TryPostTo(actorId, in value, postPolicy, fullPolicy);
        }
    }
}
