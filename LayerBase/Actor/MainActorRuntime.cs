using System.Diagnostics;
using LayerBase.Core.Event;
using LayerBase.ECS.Projection;
using LayerBase.Scope;

namespace LayerBase.Actor;

internal sealed class MainActorRuntime : IDisposable
{
    private readonly ActorWorld _world;
    private readonly int _generation;
    private readonly Queue<IProjectionWork> _projectionWorks = new();
    private MainActorRuntimeState _state = MainActorRuntimeState.Created;
    private long _pumpCount;

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
        _state = MainActorRuntimeState.Running;
    }

    public void Pump(
        float deltaTime,
        float fixedDeltaTime,
        bool pumpFixedUpdate,
        ref RuntimeFrameBudget budget)
    {
        _pumpCount++;
        PumpProjectionWorks(ref budget);
        if (!CanContinue(ref budget))
        {
            return;
        }

        _world.Pump(
            deltaTime: deltaTime,
            fixedDeltaTime: fixedDeltaTime,
            pumpFixedUpdate: pumpFixedUpdate,
            budget: ref budget);
    }

    public void RuntimeStop()
    {
        _state = MainActorRuntimeState.Stopping;
        _world.RuntimeStop();
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
        return ActorProjectionScopeEventDispatcher.TryEnqueueCommand(
            routeId,
            this,
            scope,
            runtimeId,
            payload,
            payloadStorage);
    }

    internal void EnqueueProjectionCommand(
        ScopeRuntime scope,
        ProjectedActorScopeCommand command)
    {
        _projectionWorks.Enqueue(new ProjectionCommandWork(scope, command));
    }

    internal void EnqueueProjectionPostBatch<TEvent>(ActorPostBatchScopeEvent<TEvent> batch)
        where TEvent : struct
    {
        _projectionWorks.Enqueue(new ProjectionPostBatchWork<TEvent>(batch));
    }

    private void PumpProjectionWorks(ref RuntimeFrameBudget budget)
    {
        while (_projectionWorks.Count > 0 && CanContinue(ref budget))
        {
            IProjectionWork work = _projectionWorks.Dequeue();
            work.Execute(this);
            budget.ConsumeEvent();
        }
    }

    private static bool CanContinue(ref RuntimeFrameBudget budget)
    {
        return budget.HasRemainingEventBudget()
               && budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp());
    }

    private interface IProjectionWork : IDisposable
    {
        void Execute(MainActorRuntime runtime);
    }

    private sealed class ProjectionCommandWork : IProjectionWork
    {
        private readonly ScopeRuntime _scope;
        private readonly ProjectedActorScopeCommand _command;

        public ProjectionCommandWork(ScopeRuntime scope, ProjectedActorScopeCommand command)
        {
            _scope = scope;
            _command = command;
        }

        public void Execute(MainActorRuntime runtime)
        {
            ProjectedActorScopeResult result = ActorProjectionScopeEventDispatcher.Execute(
                runtime._world,
                _command);
            _scope.TryPostEventToScope(
                _command.OriginScopeId,
                new ActorProjectionResultBatchScopeEvent(result));
        }

        public void Dispose()
        {
        }
    }

    private sealed class ProjectionPostBatchWork<TEvent> : IProjectionWork
        where TEvent : struct
    {
        private readonly ActorPostBatchScopeEvent<TEvent> _batch;

        public ProjectionPostBatchWork(ActorPostBatchScopeEvent<TEvent> batch)
        {
            _batch = batch;
        }

        public void Execute(MainActorRuntime runtime)
        {
            try
            {
                _batch.PostTo(runtime._world);
            }
            finally
            {
                _batch.Dispose();
            }
        }

        public void Dispose()
        {
            _batch.Dispose();
        }
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
        _state = MainActorRuntimeState.Disposed;
        while (_projectionWorks.Count > 0)
        {
            _projectionWorks.Dequeue().Dispose();
        }

        _world.Dispose();
    }
}
