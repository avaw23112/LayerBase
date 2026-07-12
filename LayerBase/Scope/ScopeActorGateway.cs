using System.Runtime.CompilerServices;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;

namespace LayerBase.Scope;

public sealed class ScopeActorGateway
{
    private readonly LayerRuntime? _runtime;
    private readonly ActorWorld _world;
    private readonly int _scopeId;

    internal ScopeActorGateway(LayerRuntime? runtime, ActorWorld world, int scopeId)
    {
        _runtime = runtime;
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _scopeId = scopeId;
    }

    internal ScopeActorGateway(ActorWorld world)
    {
        _runtime = null;
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _scopeId = -1;
    }

    internal ActorWorld InnerWorld => _world;

    internal LayerRuntime? Runtime => _runtime;

    public int ScopeId => _scopeId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        if (_runtime == null || !ReferenceEquals(_runtime.Actors, _world))
        {
            _world.PostTo(actorId, in value);
            return;
        }

        var capturedValue = value;
        Action<ActorWorld> postAction = world => world.PostTo(actorId, in capturedValue);
        int payloadHandle = ActorCommandPayloadStorage.Store(postAction);
        var envelope = new ActorCommandEnvelope(
            ActorCommandKind.Post,
            actorId,
            routeId: 0,
            payloadHandle: payloadHandle);
        _runtime.EnqueueActorEvent(envelope);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostToMany<TEvent>(ReadOnlySpan<ActorId> actorIds, in TEvent value)
        where TEvent : struct
    {
        if (_runtime == null || !ReferenceEquals(_runtime.Actors, _world))
        {
            _world.PostToMany(actorIds, in value);
            return;
        }

        for (int i = 0; i < actorIds.Length; i++)
        {
            var capturedValue = value;
            ActorId capturedId = actorIds[i];
            Action<ActorWorld> postAction = world => world.PostTo(capturedId, in capturedValue);
            int payloadHandle = ActorCommandPayloadStorage.Store(postAction);
            var envelope = new ActorCommandEnvelope(
                ActorCommandKind.Post,
                capturedId,
                routeId: 0,
                payloadHandle: payloadHandle);
            _runtime.EnqueueActorEvent(envelope);
        }
    }
}
