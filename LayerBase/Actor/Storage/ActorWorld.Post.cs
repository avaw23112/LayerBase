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

        if (PostFast(actorId, in value))
        {
            return PostResult.Success;
        }

        return TryBindHotOrFallbackSafe(actorId, in value, postPolicy, fullPolicy);
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
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        int fastIndex = actorId.FastIndex;
        if ((uint)fastIndex >= (uint)_fastStates.Length)
        {
            return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
        }

        int version = _fastStates[fastIndex].Version;
        if (TryBindHotFastCache<TEvent>(fastIndex, version, actorId.Generation)
            && PostFast(actorId, in value))
        {
            return PostResult.Success;
        }

        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool PostFast<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        int fastIndex = actorId.FastIndex;
        if ((uint)fastIndex >= (uint)_fastStates.Length)
        {
            return false;
        }

        if (!ActorEventRuntime<TEvent>.TryGetFastCache(this, out ActorEventFastCache<TEvent>? cache)
            || cache == null)
        {
            return false;
        }

        ref ActorFastState state = ref _fastStates[fastIndex];
        if (!cache.TryGet(
                fastIndex,
                state.Version,
                actorId.Generation,
                out int slotIndex,
                out EventMail<TEvent>[] mails,
                out DirtySlotList dirtySlots,
                out int bucketIndex))
        {
            return false;
        }

        return PostQueuedGrowFastNoResult(
            slotIndex,
            in value,
            mails,
            dirtySlots,
            bucketIndex,
            cache.Pool);
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
