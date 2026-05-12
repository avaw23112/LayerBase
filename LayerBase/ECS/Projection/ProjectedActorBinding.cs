using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorBinding
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World                  world,
        ActorWorld             actorWorld,
        Entity                 entity,
        ref ProjectedActorMeta meta,
        long                   nowTicks)
    {
        ProjectedActorHandle handle = ProjectedActorTypeRegistry.CreateActorByTypeId(actorWorld, meta.ActorTypeId);
        if (!handle.IsValid)
        {
            return ActorId.Invalid;
        }

        handle.Actor.RecycleDeadlineTicks = ProjectedActorTime.BuildDeadline(nowTicks, meta.KeepAliveTicks);
        ProjectedActorBindingUtility.Bind(world, entity, ref meta, handle.ActorId);
        world.AddActiveProjectedActor(entity, ref meta);
        return handle.ActorId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TouchProjectedActor(
        ActorWorld             actorWorld,
        ref ProjectedActorMeta meta,
        long                   nowTicks)
    {
        if (!meta.ActorId.IsValid)
        {
            return;
        }

        if (!actorWorld.TryGetPooledActor(meta.ActorId, out IPooledActor pooledActor))
        {
            meta.ClearActor();
            return;
        }

        pooledActor.RecycleDeadlineTicks = ProjectedActorTime.BuildDeadline(nowTicks, meta.KeepAliveTicks);
    }
}