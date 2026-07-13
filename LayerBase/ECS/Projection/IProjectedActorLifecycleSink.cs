using System;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;

namespace LayerBase.ECS.Projection;

internal interface IProjectedActorLifecycleSink
{
    ControlEnqueueResult TryDisableProjectedActor(ActorId actorId);

    ControlEnqueueResult TryReleaseProjectedActor(
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy);
}

internal sealed class ActorWorldProjectedActorLifecycleSink : IProjectedActorLifecycleSink
{
    private readonly ActorWorld _world;

    public ActorWorldProjectedActorLifecycleSink(ActorWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public ControlEnqueueResult TryDisableProjectedActor(ActorId actorId)
    {
        return _world.DisableProjectedActor(actorId)
            ? ControlEnqueueResult.AcceptedFast
            : ControlEnqueueResult.Failed;
    }

    public ControlEnqueueResult TryReleaseProjectedActor(
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy)
    {
        return _world.ReleaseProjectedActor(actorId, releasePolicy)
            ? ControlEnqueueResult.AcceptedFast
            : ControlEnqueueResult.Failed;
    }
}
