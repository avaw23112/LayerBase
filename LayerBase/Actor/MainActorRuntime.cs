using System.Diagnostics;
using Arch.Core;
using LayerBase.Core.Event;
using LayerBase.ECS.Projection;
using LayerBase.Lifetime;
using LayerBase.Scope;

namespace LayerBase.Actor;

internal sealed class MainActorRuntime : ILifetimeParticipant, IDisposable
{
    private readonly ActorWorld _world;
    private readonly ActorProjectionRuntime _projectionRuntime;
    private readonly int _generation;
    private MainActorRuntimeState _state = MainActorRuntimeState.Created;
    private long _pumpCount;
    private bool _admissionClosed;
    private bool _drainCompleted;

    string ILifetimeParticipant.LifetimeName => "MainActorRuntime";

    void ILifetimeParticipant.CloseAdmission() => CloseAdmission();

    void ILifetimeParticipant.RequestStop() => RequestStop();

    LifetimeDrainResult ILifetimeParticipant.Drain(in Scope.ShutdownDeadline deadline)
    {
        Drain();
        return LifetimeDrainResult.Drained;
    }

    void ILifetimeParticipant.Release(TerminalCleanupRunner cleanup)
    {
        Dispose();
    }

    public MainActorRuntime(LayerRuntime runtime, int generation)
    {
        _world = new ActorWorld(runtime ?? throw new ArgumentNullException(nameof(runtime)));
        _projectionRuntime = new ActorProjectionRuntime(_world);
        _generation = generation;
        Client = new ActorClient(_world, generation);
        Factory = new ActorFactory(_world, generation);
        ProjectedActorCommandSink = _projectionRuntime.CommandSink;
    }

    public ActorWorld World => _world;

    public ActorClient Client { get; }

    public ActorFactory Factory { get; }

    public IProjectedActorCommandSink ProjectedActorCommandSink { get; }

    public void BindProjectionWorld(World world)
    {
        _projectionRuntime.BindMainWorld(world);
    }

    public void PrepareRuntimeBuild()
    {
        _world.PrepareRuntimeBuild();
    }

    public void CompleteRuntimeBuild()
    {
        _world.CompleteRuntimeBuild();
        _state = MainActorRuntimeState.Running;
    }

    public void Pump(
        float deltaTime,
        float fixedDeltaTime,
        bool pumpFixedUpdate,
        ref RuntimeFrameBudget budget)
    {
        _pumpCount++;
        _projectionRuntime.Pump(ref budget);
        if (!CanContinue(ref budget))
            return;

        _world.Pump(
            deltaTime: deltaTime,
            fixedDeltaTime: fixedDeltaTime,
            pumpFixedUpdate: pumpFixedUpdate,
            budget: ref budget);
    }

    public void CloseAdmission()
    {
        _admissionClosed = true;
    }

    public void RequestStop()
    {
        _state = MainActorRuntimeState.Stopping;
        _world.RuntimeStop();
    }

    public void Drain()
    {
        _drainCompleted = true;
    }

    public void RuntimeStop()
    {
        _state = MainActorRuntimeState.Stopping;
        _world.RuntimeStop();
        _projectionRuntime.Dispose();
        _state = MainActorRuntimeState.Stopped;
    }

    internal MainActorDiagnosticsSnapshot CaptureDiagnostics()
    {
        return new MainActorDiagnosticsSnapshot(
            _state,
            actorCount: 0,
            pendingMailCount: 0,
            pendingCallCount: 0,
            pendingLifecycleCount: 0,
            pendingDestroyCount: 0,
            pumpCount: Volatile.Read(ref _pumpCount),
            lastPumpDurationTicks: 0,
            faultCount: 0);
    }

    public bool DispatchCallRoute(
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

    public bool DispatchProjectionRoute(
        int routeId,
        ScopeRuntime scope,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        return ActorProjectionScopeEventDispatcher.DispatchCommandRoute(
            routeId,
            scope,
            this,
            runtimeId,
            payload,
            payloadStorage);
    }

    private static bool CanContinue(ref RuntimeFrameBudget budget)
    {
        return budget.CanContinue(Stopwatch.GetTimestamp());
    }

    internal void EnqueueProjectionCommand(ProjectedActorScopeCommand command, ScopeEndpoint resultEndpoint)
    {
        _projectionRuntime.EnqueueCommand(command, resultEndpoint);
    }

    internal void EnqueueProjectionBatch<TEvent>(ProjectionBatchLease<TEvent> lease)
        where TEvent : struct
    {
        _projectionRuntime.EnqueuePostBatch(lease);
    }

    public bool DispatchCommandRoute(
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
        _state = MainActorRuntimeState.Disposed;
        _projectionRuntime.Dispose();
        _world.Dispose();
    }
}
