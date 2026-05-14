using System.Runtime.CompilerServices;
using LayerBase.ECS.Projection;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal ProjectedActorHandle CreateProjectedActor<TActor>()
        where TActor : class, IPooledActor, new()
    {
        TActor actor = CreateActor<TActor>(usePool: true);
        IGeneratedActorMeta generated = ActorGeneratedAccess.RequireGenerated(actor);

        return new ProjectedActorHandle(
            generated.GetId(),
            actor);
    }

    internal bool TryGetActor(
        ActorId     actorId,
        out IActor? actor)
    {
        actor = null;

        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        BehaviourArchetype? archetype = _archetypes[actorId.ArchetypeId];
        if (archetype == null)
        {
            return false;
        }

        return archetype.TryGetActor(
            actorId,
            out actor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetPooledActor(
        ActorId          actorId,
        out IPooledActor pooledActor)
    {
        if (!TryGetActor(
                actorId,
                out IActor? actor))
        {
            pooledActor = null!;
            return false;
        }

        pooledActor = actor as IPooledActor;
        return pooledActor != null;
    }

    internal bool ReleaseProjectedActor(
        ActorId                     actorId,
        ProjectedActorReleasePolicy releasePolicy)
    {
        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        BehaviourArchetype? archetype = _archetypes[actorId.ArchetypeId];
        if (archetype == null)
        {
            return false;
        }

        return archetype.ReleaseProjectedActor(
            actorId,
            this,
            releasePolicy);
    }
}
