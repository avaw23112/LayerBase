using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.ECS.Projection.Generated;

namespace LayerBase.ECS.Projection;

public static class ProjectedActorWorldExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithProjectedActor<TActor>(
        this World world,
        Entity entity,
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        int actorTypeId =  ActorType<TActor>.Id;
        if (actorTypeId < 0)
        {
            throw new InvalidOperationException($"ProjectedActor type {typeof(TActor).Name} was not generated. Make sure it implements IPooledActor and is visible to the generator.");
        }
        ProjectedActorTypeRegistry.RegisterGenerated(actorTypeId,typeof(TActor),static actorWorld => actorWorld.CreateProjectedActor<TActor>());
        WithProjectedActor(world, entity, actorTypeId, keepAliveSeconds, releasePolicy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WithProjectedActor(
        this World world,
        Entity entity,
        int actorTypeId,
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
    {
        ref ProjectedActorMeta meta = ref world.GetProjectionMeta(entity);
        meta.MarkProjected(actorTypeId, ProjectedActorTime.SecondsToTicks(keepAliveSeconds), releasePolicy);
    }
    

}
