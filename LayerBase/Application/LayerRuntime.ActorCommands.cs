using System;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;
using LayerBase.ECS.Projection;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    private readonly ActorEventInbox _actorEventInbox = new(256);
    private readonly ActorLifecycleInbox _actorLifecycleInbox = new(128);

    internal ActorEventInbox ActorEventInbox => _actorEventInbox;
    internal ActorLifecycleInbox ActorLifecycleInbox => _actorLifecycleInbox;

    internal int DrainActorCommands(int maxEvents = 0, int maxLifecycle = 0)
    {
        int drained = 0;
        drained += _actorLifecycleInbox.Drain(ProcessLifecycleCommand, maxLifecycle);
        drained += _actorEventInbox.Drain(ProcessEventCommand, maxEvents);
        return drained;
    }

    private void ProcessLifecycleCommand(ActorCommandEnvelope command)
    {
        switch (command.Kind)
        {
            case ActorCommandKind.Disable:
                Actors.DisableProjectedActor(command.ActorId);
                break;
            case ActorCommandKind.Release:
                Actors.ReleaseProjectedActor(command.ActorId, ProjectedActorReleasePolicy.ReturnToPool);
                break;
        }
    }

    private void ProcessEventCommand(ActorCommandEnvelope command)
    {
        if (command.Kind != ActorCommandKind.Post)
            return;

        Action<ActorWorld>? postAction = ActorCommandPayloadStorage.Retrieve<Action<ActorWorld>>(command.PayloadHandle);
        postAction?.Invoke(Actors);
        ActorCommandPayloadStorage.Free(command.PayloadHandle);
    }

    internal bool EnqueueActorEvent(ActorCommandEnvelope envelope)
    {
        if (_disposed) return false;
        return _actorEventInbox.TryEnqueue(envelope);
    }

    internal bool EnqueueActorLifecycle(ActorCommandEnvelope envelope)
    {
        if (_disposed) return false;
        return _actorLifecycleInbox.TryEnqueue(envelope);
    }
}
