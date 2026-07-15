using LayerBase.Core.Event;
using LayerBase.ECS.Projection;
using LayerBase.Scope;

namespace LayerBase.Actor;

internal sealed class MainActorRuntime : IDisposable
{
    private readonly ActorWorld _world;
    private readonly int _generation;

    public MainActorRuntime(LayerRuntime runtime, int generation)
    {
        _world = new ActorWorld(runtime ?? throw new ArgumentNullException(nameof(runtime)));
        _generation = generation;
        Accessor = new ActorAccessor(new LocalActorAccessor(_world, generation));
    }

    public ActorWorld World => _world;

    public ActorAccessor Accessor { get; }

    public IProjectedActorCommandSink ProjectedActorCommandSink { get; private set; }
        = null!;

    public void BindProjectionSink()
    {
        ProjectedActorCommandSink = new MainScopeProjectedActorCommandSink(_world);
    }

    public void PrepareRuntimeBuild()
    {
        _world.PrepareRuntimeBuild();
    }

    public void CompleteRuntimeBuild()
    {
        _world.CompleteRuntimeBuild();
    }

    public void Pump(
        float deltaTime,
        float fixedDeltaTime,
        bool pumpFixedUpdate,
        ref RuntimeFrameBudget budget)
    {
        _world.Pump(
            deltaTime: deltaTime,
            fixedDeltaTime: fixedDeltaTime,
            pumpFixedUpdate: pumpFixedUpdate,
            budget: ref budget);
    }

    public void RuntimeStop()
    {
        _world.RuntimeStop();
    }

    public bool TryDispatchCall(
        int routeId,
        int runtimeId,
        ScopeCallEnvelope envelope,
        EventPayloadStorage payloadStorage)
    {
        return ActorCallDispatcherRegistry.TryDispatch(
            routeId,
            _world,
            runtimeId,
            envelope,
            payloadStorage);
    }

    public bool TryDispatchProjectionCommand(
        int routeId,
        ScopeRuntime scope,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        return ActorProjectionScopeEventDispatcher.TryDispatchCommand(
            routeId,
            scope,
            _world,
            runtimeId,
            payload,
            payloadStorage);
    }

    public bool TryDispatchCommand(
        int routeId,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        return ActorCommandDispatcherRegistry.TryDispatch(
            routeId,
            _world,
            runtimeId,
            payload,
            payloadStorage);
    }

    public void Dispose()
    {
        _world.Dispose();
    }
}
