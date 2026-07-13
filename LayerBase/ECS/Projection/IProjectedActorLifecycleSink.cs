using System;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal interface IProjectedActorLifecycleSink
{
    bool TryDisableProjectedActor(ActorId actorId);

    bool TryReleaseProjectedActor(
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

    public bool TryDisableProjectedActor(ActorId actorId)
    {
        return _world.DisableProjectedActor(actorId);
    }

    public bool TryReleaseProjectedActor(
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy)
    {
        return _world.ReleaseProjectedActor(actorId, releasePolicy);
    }
}
