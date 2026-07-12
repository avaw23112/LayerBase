using System;

namespace LayerBase.Actor.RuntimeCommands;

internal readonly struct ActorCommandEnvelope
{
    public ActorCommandEnvelope(
        ActorCommandKind kind,
        ActorId actorId,
        int routeId,
        int payloadHandle)
    {
        Kind = kind;
        ActorId = actorId;
        RouteId = routeId;
        PayloadHandle = payloadHandle;
    }

    public ActorCommandKind Kind { get; }
    public ActorId ActorId { get; }
    public int RouteId { get; }
    public int PayloadHandle { get; }
}
