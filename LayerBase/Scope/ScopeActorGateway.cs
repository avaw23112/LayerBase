using System.Runtime.CompilerServices;
using LayerBase.Actor;

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
        _world.PostTo(actorId, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostToMany<TEvent>(ReadOnlySpan<ActorId> actorIds, in TEvent value)
        where TEvent : struct
    {
        _world.PostToMany(actorIds, in value);
    }
}
