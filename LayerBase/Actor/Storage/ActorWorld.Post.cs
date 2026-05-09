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
        if (postPolicy != null || fullPolicy != null || actorId.FastIndex < 0)
        {
            return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
        }

        int fastIndex = actorId.FastIndex;
        if ((uint)fastIndex >= (uint)_fastStates.Length)
        {
            return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
        }

        ref ActorFastState state = ref _fastStates[fastIndex];
        ActorEventFastCache<TEvent> cache = GetOrCreateFastCache<TEvent>();
        if (cache.TryGet(
                fastIndex,
                state.Version,
                actorId.Generation,
                out int slotIndex,
                out EventMail<TEvent>[] mails,
                out DirtySlotList dirtySlots,
                out int bucketIndex,
                out ActorMailOptions options))
        {
            return PostQueuedGrowDirect(slotIndex, in value, mails, dirtySlots, bucketIndex, options);
        }

        return TryBindHotOrFallbackSafe(actorId, in value, postPolicy, fullPolicy, state.Version);
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

    private PostResult TryBindHotOrFallbackSafe<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy,
        int version)
        where TEvent : struct
    {
        int fastIndex = actorId.FastIndex;
        if (!TryBindHotFastCache<TEvent>(fastIndex, version, actorId.Generation))
        {
            return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
        }

        ref ActorFastState state = ref _fastStates[fastIndex];
        ActorEventFastCache<TEvent> cache = GetOrCreateFastCache<TEvent>();
        if (!cache.TryGet(
                fastIndex,
                state.Version,
                actorId.Generation,
                out int slotIndex,
                out EventMail<TEvent>[] mails,
                out DirtySlotList dirtySlots,
                out int bucketIndex,
                out ActorMailOptions options))
        {
            return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
        }

        return PostQueuedGrowDirect(slotIndex, in value, mails, dirtySlots, bucketIndex, options);
    }

    private PostResult TryPostToSafe<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
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
}
