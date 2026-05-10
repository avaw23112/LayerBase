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
        if (postPolicy != null || fullPolicy != null)
        {
            return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
        }

        if (PostFast(actorId, in value))
        {
            return PostResult.Success;
        }

        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult PostTo<TEvent>(
        ActorId   actorId,
        in TEvent value)
        where TEvent : struct
    {
        return PostFast(actorId, in value) ? PostResult.Success : PostResult.Failure(message:"Post failed",failureKind:PostFailureKind.Unknown);
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
            _ = PostTo(actorId, in value, postPolicy, fullPolicy);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool PostFast<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        if (!EventPostRuntime<TEvent>.TryGetRows(this, out EventPostRow<TEvent>[]? rows)
            || rows == null)
        {
            return false;
        }

        int archetypeId = actorId.ArchetypeId;
        if ((uint)archetypeId >= (uint)rows.Length)
        {
            return false;
        }
        
        ref readonly EventPostRow<TEvent> row = ref rows[archetypeId];
        if (!row.IsValid)
        {
            return false;
        }

        int slotIndex = actorId.SlotIndex;
        if ((uint)slotIndex >= (uint)row.Generations.Length)
        {
            return false;
        }

        if (row.Generations[slotIndex] != actorId.Generation)
        {
            return false;
        }

        return PostQueuedGrowFastNoResult(
            slotIndex,
            in value,
            row.Mails,
            row.DirtySlots,
            row.BucketIndex,
            row.Pool);
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
