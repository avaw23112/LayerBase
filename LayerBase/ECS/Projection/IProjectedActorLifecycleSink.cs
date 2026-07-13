using System;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;
using LayerBase;

namespace LayerBase.ECS.Projection;

internal interface IProjectedActorLifecycleSink
{
    ControlEnqueueResult TryEnableProjectedActor(ActorId actorId);

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

    public ControlEnqueueResult TryEnableProjectedActor(ActorId actorId)
    {
        return _world.EnableProjectedActorIfDisabled(actorId)
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

internal sealed class LayerRuntimeProjectedActorLifecycleSink : IProjectedActorLifecycleSink
{
    private readonly LayerRuntime _runtime;

    public LayerRuntimeProjectedActorLifecycleSink(LayerRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public ControlEnqueueResult TryDisableProjectedActor(ActorId actorId)
    {
        if (_runtime.IsOwnerThreadForActorWorld)
        {
            return _runtime.Actors.DisableProjectedActor(actorId)
                ? ControlEnqueueResult.AcceptedFast
                : ControlEnqueueResult.Failed;
        }

        return _runtime.EnqueueActorLifecycle(new ActorCommandEnvelope(
            ActorCommandKind.Disable,
            actorId,
            routeId: 0,
            payloadHandle: 0));
    }

    public ControlEnqueueResult TryEnableProjectedActor(ActorId actorId)
    {
        if (_runtime.IsOwnerThreadForActorWorld)
        {
            return _runtime.Actors.EnableProjectedActorIfDisabled(actorId)
                ? ControlEnqueueResult.AcceptedFast
                : ControlEnqueueResult.Failed;
        }

        return _runtime.EnqueueActorLifecycle(new ActorCommandEnvelope(
            ActorCommandKind.Enable,
            actorId,
            routeId: 0,
            payloadHandle: 0));
    }

    public ControlEnqueueResult TryReleaseProjectedActor(
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy)
    {
        if (_runtime.IsOwnerThreadForActorWorld)
        {
            return _runtime.Actors.ReleaseProjectedActor(actorId, releasePolicy)
                ? ControlEnqueueResult.AcceptedFast
                : ControlEnqueueResult.Failed;
        }

        return _runtime.EnqueueActorLifecycle(new ActorCommandEnvelope(
            ActorCommandKind.Release,
            actorId,
            routeId: (int)releasePolicy,
            payloadHandle: 0));
    }
}
