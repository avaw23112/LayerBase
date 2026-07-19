using System.Collections.Concurrent;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.Event.Delay;
using LayerBase.Lifetime;
using LayerBase.Snap;
using LayerBase.Worker;

namespace LayerBase.Scope;

internal sealed class ScopeRuntime : IDisposable
{
    private readonly int _runtimeId;
    private readonly int _generation;
    private readonly LayerRuntime _runtime;
    private ScopeRuntimeHost? _host;
    private ActorClient _actorClient;
    private ActorFactory _actorFactory;
    private bool _hasActorClient;
    private bool _hasActorFactory;
    private ScopeRuntimeState _state = ScopeRuntimeState.Created;
    private ScopeSafePointState _safePointState = ScopeSafePointState.Running;
    private long _safePointToken;
    private ScopeSnapPlan _snapPlan = ScopeSnapPlan.Empty;
    private ScopeRuntimeDirectory? _scopeDirectory;
    private float _fixedUpdateAccumulator;
    private bool _runtimeStopRun;
    private bool _lifecycleDisposeRun;
    private bool _disposeRequestedFromControl;
    private int _ownerThreadId;
    private int _hasIngress;
    private long _tickCount;
    private long _faultCount;
    private ScopeCallCompletion<ScopeDisposeResponse>? _pendingDisposeCompletion;
    private readonly ConcurrentDictionary<Type, IDelayPublisherInternal> _delayPublishers = new();

    private readonly Action<Exception> _eventExpectationFaultReporter;

    private Action? _signalWorker;
    private readonly LifetimeOperationTracker _asyncCallOperations = new();
    private readonly CancellationTokenSource _scopeLifetimeCancellation = new();

    public ScopeRuntime(LayerRuntime runtime, ScopeExecutionPlan plan, int runtimeId, int generation)
    {
        if (runtime == null)
            throw new ArgumentNullException(nameof(runtime));
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));

        Descriptor = plan.Descriptor;
        Options = plan.Options;
        LayerProviders = plan.LayerProviders;
        LayerSlices = plan.LayerSlices;
        LifecyclePlan = plan.LifecyclePlan;
        _runtime = runtime;
        _eventExpectationFaultReporter = ReportEventExpectationFault;
        _runtimeId = runtimeId;
        _generation = generation;
        Transport = new ScopeTransport(
            new ScopeAddress(runtimeId, generation, Descriptor.ScopeId),
            SignalIngress);
        WorkerJobs = new WorkerJobCoordinator(
            this,
            runtime.WorkerExecutor,
            WorkerJobSchedulerOptions.Default);
        EventCenter = new EventCenter();
        EventCenter.BindBusinessOperations(
            _asyncCallOperations,
            Transport);
        LocalCalls = new ScopeLocalCallRegistry(Descriptor.ScopeId);
        CallRoutes = new ScopeCallRouteTable(Descriptor.ScopeId);
        EventRoutes = new ScopeEventRouteTable(Descriptor.ScopeId);
        if (Descriptor.ScopeId == ScopeDefinitionIds.Main)
        {
            _actorClient = _runtime.MainActorRuntime.Client;
            _actorFactory = _runtime.MainActorRuntime.Factory;
            _hasActorClient = true;
            _hasActorFactory = true;
        }
        EcsScheduler = new ScopeEcsScheduler(
            _generation,
            Descriptor.ScopeId,
            World.Create(),
            Options.EcsRuntime);
        EcsWorld = EcsScheduler.World;
        EcsWorld.BindRuntime(runtime);
        EcsWorld.BindEcsScheduler(EcsScheduler);
        if (Descriptor.ScopeId == ScopeDefinitionIds.Main)
        {
            _runtime.MainActorRuntime.BindProjectionWorld(EcsWorld);
            EcsWorld.BindProjectedActorCommandSink(_runtime.MainActorRuntime.ProjectedActorCommandSink);
        }
    }

    internal void BindHost(ScopeRuntimeHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    internal void BindWorkerWakeSignal(Action signalWorker)
    {
        if (signalWorker == null)
            throw new ArgumentNullException(nameof(signalWorker));

        if (Options.Threading != ScopeThreadingMode.Worker)
        {
            throw new InvalidOperationException(
                "Only Worker Scope may bind a wake signal.");
        }

        if (_signalWorker != null)
        {
            throw new InvalidOperationException(
                "Worker wake signal is already bound.");
        }

        _signalWorker = signalWorker;
    }

    public ScopeDescriptor Descriptor { get; }

    public int ScopeId => Descriptor.ScopeId;

    public ScopeOptions Options { get; }

    public ScopeTransport Transport { get; }

    public ScopeEndpoint Endpoint => Transport.Endpoint;

    public WorkerJobCoordinator WorkerJobs { get; }

    public EventCenter EventCenter { get; }

    public PostScheduler? PostScheduler { get; private set; }

    public PostTimerScheduler? Timer { get; private set; }

    public DelayPublisherManager? DelayManager { get; private set; }

    public ActorClient ActorClient =>
        _hasActorClient
            ? _actorClient
            : throw new InvalidOperationException("Scope actor accessor is not initialized.");

    public ActorFactory ActorFactory =>
        _hasActorFactory
            ? _actorFactory
            : throw new InvalidOperationException("Scope actor factory is only available on MainScope.");

    public ScopeEcsScheduler EcsScheduler { get; }

    public World EcsWorld { get; }

    public LayerBaseSynchronizationContext? SynchronizationContext { get; private set; }

    public ScopeLocalCallRegistry LocalCalls { get; }

    public ScopeCallRouteTable CallRoutes { get; }

    public ScopeEventRouteTable EventRoutes { get; }

    internal LifetimeOperationTracker AsyncCallOperations => _asyncCallOperations;

    internal CancellationToken ScopeLifetimeToken => _scopeLifetimeCancellation.Token;

    public LayerProviderRuntime[] LayerProviders { get; }

    public ScopeLayerSlice[] LayerSlices { get; }

    public ScopeLifecyclePlan LifecyclePlan { get; private set; }

    public ScopeRuntimeState State => _state;

    public ScopeSafePointState SafePointState => _safePointState;

    public EventBuildPolicyTable? PolicyTable { get; private set; }

    public void InstallSynchronizationContext()
    {
        BindOwnerThreadIfNeeded();
        if (SynchronizationContext == null)
        {
            SynchronizationContext = LayerBaseSynchronizationContext.Install(
                Options.Threading == ScopeThreadingMode.Worker ? _signalWorker : null);
        }
    }

    public void InitializeOrUpdateScheduler(
        PostSchedulerOptions options,
        EventBuildPolicyTable policyTable,
        PostTypePlan[] plans)
    {
        PolicyTable = policyTable ?? throw new ArgumentNullException(nameof(policyTable));
        EventCenter.BindPolicyTable(policyTable);
        if (PostScheduler == null)
        {
            PostScheduler = new PostScheduler(_runtimeId, EventCenter, options, policyTable);
            EventCenter.PostScheduler = PostScheduler;
        }
        else
        {
            PostScheduler.UpdatePolicyTable(policyTable);
        }

        PostScheduler.BuildPlans(plans);
        _state = ScopeRuntimeState.Running;
    }

    public void InitializeTimer(TimeSchedulerOptions options)
    {
        var scheduler = RequireScheduler();
        var policyTable = RequirePolicyTable();
        Timer = new PostTimerScheduler(
            _runtimeId,
            options,
            scheduler.PayloadStorage,
            scheduler);
        Timer.CompilePlans(policyTable, EventTypeIdAllocator.MaxId);
    }

    internal void CompileTimerPlans()
    {
        if (Timer == null)
            return;
        var policyTable = RequirePolicyTable();
        Timer.CompilePlans(policyTable, EventTypeIdAllocator.MaxId);
    }

    public void InitializeDelay(DelayBufferOptions options)
    {
        DelayManager = DelayPublisherManager.Create(options, RequirePolicyTable());
    }

    internal void Prewarm(in LayerPrewarmOptions options)
    {
        LayerBasePrewarmRegistry.Prewarm(EventCenter, options);
    }

    internal void FreezeRuntimeRegistries()
    {
        PolicyTable?.Freeze();
    }

    public IDelayPublisher<T> SubscribeDelay<T>() where T : struct
    {
        var type = typeof(T);
        if (_delayPublishers.TryGetValue(type, out var existing))
            return (IDelayPublisher<T>)existing;

        var manager = DelayManager
                      ?? throw new InvalidOperationException("DelayPublisherManager is not built.");
        var publisher = new DelayPublisher<T>(manager, MarkDelayDirty);
        int id = manager.RegisterPublisher(publisher);
        publisher.SetId(id);

        var actual = _delayPublishers.GetOrAdd(type, publisher);
        if (ReferenceEquals(actual, publisher))
        {
            MarkDelayDirty();
            return publisher;
        }

        manager.UnregisterPublisher(id);
        return (IDelayPublisher<T>)actual;
    }

    private void MarkDelayDirty()
    {
        if (ScopeId == ScopeDefinitionIds.Main)
            _runtime.MarkDelayDirty();
    }

    public void TickTimer(float deltaTime)
    {
        Timer?.Tick(deltaTime);
    }

    public void BindMainActorEndpoint(ScopeEndpoint mainEndpoint)
    {
        if (Descriptor.ScopeId == ScopeDefinitionIds.Main)
        {
            _actorClient = _runtime.MainActorRuntime.Client;
            _actorFactory = _runtime.MainActorRuntime.Factory;
            _hasActorFactory = true;
            _runtime.MainActorRuntime.BindProjectionWorld(EcsWorld);
            EcsWorld.BindProjectedActorCommandSink(_runtime.MainActorRuntime.ProjectedActorCommandSink);
        }
        else
        {
            _actorClient = new ActorClient(
                new ScopeRef<MainScope>(mainEndpoint),
                Descriptor.ScopeId,
                _generation);
            EcsWorld.BindProjectedActorCommandSink(new ScopeEventProjectedActorCommandSink(
                new ScopeRef<MainScope>(mainEndpoint),
                Descriptor.ScopeId,
                _generation));
        }

        _hasActorClient = true;
    }

    public void BindScopeEndpoints(ScopeRuntimeDirectory directory)
    {
        _scopeDirectory = directory
            ?? throw new ArgumentNullException(nameof(directory));
    }

    internal bool TryGetScopeEndpoint(int scopeId, out ScopeEndpoint endpoint)
    {
        var directory = _scopeDirectory;
        if (directory != null && directory.TryGetEndpoint(scopeId, out endpoint))
            return true;

        endpoint = default;
        return false;
    }

    public bool TryPostEventToScope<TEvent>(
        int scopeId,
        in TEvent value)
        where TEvent : struct
    {
        if (!TryGetScopeEndpoint(scopeId, out ScopeEndpoint endpoint))
            return false;

        return endpoint.Transport != null && endpoint.Transport.EnqueueEvent(
            EventTypeId<TEvent>.Id,
            ScopeEventClass.Internal,
            in value).IsAccepted;
    }

    public LBTask<TResponse> CallLocalAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        RequireOwnerThread();
        if (_state is ScopeRuntimeState.StopRequested or ScopeRuntimeState.Stopped or ScopeRuntimeState.Disposing or ScopeRuntimeState.Disposed)
            return LBTask<TResponse>.FromException(
                new InvalidOperationException($"Scope `{Descriptor.Name}` is not accepting local calls."));

        return LocalCalls.CallAsync<TRequest, TResponse>(request, cancellationToken);
    }

    public LBTask<TResponse> EnqueueCall<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (_state == ScopeRuntimeState.Disposed)
            return LBTask<TResponse>.FromException(new ObjectDisposedException(nameof(ScopeRuntime)));

        return Transport.EnqueueCall<TRequest, TResponse>(in request, cancellationToken);
    }

    internal LBTask<TResponse> EnqueueControlCall<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (_state == ScopeRuntimeState.Disposed)
            return LBTask<TResponse>.FromException(new ObjectDisposedException(nameof(ScopeRuntime)));
        if (cancellationToken.IsCancellationRequested)
            return LBTask<TResponse>.FromCanceled(cancellationToken);

        var completion = new ScopeCallCompletion<TResponse>();
        var queuedCall = new ScopeQueuedCall<TRequest, TResponse>(
            request,
            completion,
            cancellationToken);
        var payload = Transport.CallPayloadStorage.Store(_runtimeId, in queuedCall);
        var envelope = new ScopeCallEnvelope(
            ScopeCallEnvelopeKind.Request,
            ScopeCallClass.Control,
            Transport.NextCallToken(),
            Endpoint.Address,
            ScopeLifecycleRouteIds.Resolve<TRequest, TResponse>(),
            payload,
            ScopeCallResult.None,
            completion);

        var result = Transport.CallInbox.TryEnqueue(envelope, envelope.Class.ToAdmissionClass());
        if (result == ScopeEnqueueResult.Accepted)
            return completion.Task;

        Transport.CallPayloadStorage.Release(payload);
        completion.TrySetException(new InvalidOperationException($"Scope control call enqueue failed: {result}."));
        return completion.Task;
    }

    public ScopePostResult EnqueueEvent<TEvent>(in TEvent value)
        where TEvent : struct
    {
        if (_state == ScopeRuntimeState.Disposed)
            return ScopePostResult.RuntimeDisposed;

        return Transport.EnqueueEvent(in value);
    }

    public void PumpIngress()
    {
        if (Interlocked.Exchange(ref _hasIngress, 0) == 0)
            return;

        DrainCompletionInbox();
        DisposeAfterControlIfNeeded();

        if (_state == ScopeRuntimeState.Disposed)
            return;

        DrainCallInbox();
        DisposeAfterControlIfNeeded();

        if (_state == ScopeRuntimeState.Disposed)
            return;

        if (ShouldYieldBusinessForSafePoint())
        {
            if (Transport.CompletionInbox.Count != 0)
                Volatile.Write(ref _hasIngress, 1);

            return;
        }

        DrainEventInbox();
        DisposeAfterControlIfNeeded();

        if (Transport.CompletionInbox.Count != 0)
            Volatile.Write(ref _hasIngress, 1);
    }

    public void PumpScopeResources(
        float deltaTime,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>? reportException = null)
    {
        BindOwnerThreadIfNeeded();
        var context = SynchronizationContext;
        if (context != null)
        {
            using var scope = context.EnterScope();
            PumpScopeResourcesCore(deltaTime, exceptionPolicy, reportException);
            return;
        }

        PumpScopeResourcesCore(deltaTime, exceptionPolicy, reportException);
    }

    public void PumpScopeResources(
        float deltaTime,
        ref RuntimeFrameBudget budget,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>? reportException = null)
    {
        BindOwnerThreadIfNeeded();
        var context = SynchronizationContext;
        if (context != null)
        {
            using var scope = context.EnterScope();
            PumpScopeResourcesCore(deltaTime, ref budget, exceptionPolicy, reportException);
            return;
        }

        PumpScopeResourcesCore(deltaTime, ref budget, exceptionPolicy, reportException);
    }

    private void PumpScopeResourcesCore(
        float deltaTime,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        _tickCount++;
        PumpIngress();
        if (!CanPumpLifecycle())
            return;

        PumpSynchronizationContext( exceptionPolicy, reportException);
        TickTimer(deltaTime);
        DelayManager?.Tick(deltaTime);
        PostScheduler?.Pump();
        PumpEventExpectations();
        PumpUpdate(deltaTime);
    }

    private void PumpScopeResourcesCore(
        float deltaTime,
        ref RuntimeFrameBudget budget,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        _tickCount++;
        PumpIngress();
        if (!CanPumpLifecycle())
            return;

        PumpSynchronizationContext(exceptionPolicy, reportException);
        TickTimer(deltaTime);
        DelayManager?.Tick(deltaTime);
        PostPumpStats postStats =
            PostScheduler?.Pump(ref budget)
            ?? new PostPumpStats(0, 0, 0, 0);

        budget.Consume(postStats.ProcessedCount);
        PumpEventExpectations();
        PumpUpdate(deltaTime);
    }

    internal void PumpWorkerImmediateWork(
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>? reportException = null)
    {
        RequireOwnerThread();
        var context = SynchronizationContext;
        if (context != null)
        {
            using var scope = context.EnterScope();
            PumpWorkerImmediateWorkCore(exceptionPolicy, reportException);
            return;
        }

        PumpWorkerImmediateWorkCore(exceptionPolicy, reportException);
    }

    private void PumpWorkerImmediateWorkCore(
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        PumpIngress();
        if (!CanPumpLifecycle())
            return;

        PumpSynchronizationContext(
            exceptionPolicy,
            reportException);
        PostScheduler?.Pump();
        PumpEventExpectations();
    }

    internal void PumpWorkerScheduledTick(
        float fixedDeltaTime,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>? reportException = null)
    {
        RequireOwnerThread();
        var context = SynchronizationContext;
        if (context != null)
        {
            using var scope = context.EnterScope();
            PumpWorkerScheduledTickCore(fixedDeltaTime, exceptionPolicy, reportException);
            return;
        }

        PumpWorkerScheduledTickCore(fixedDeltaTime, exceptionPolicy, reportException);
    }

    private void PumpWorkerScheduledTickCore(
        float fixedDeltaTime,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        _tickCount++;
        PumpIngress();
        if (!CanPumpLifecycle())
            return;

        PumpSynchronizationContext(
            exceptionPolicy,
            reportException);
        TickTimer(fixedDeltaTime);
        DelayManager?.Tick(fixedDeltaTime);
        PostScheduler?.Pump();
        PumpEventExpectations();
        PumpUpdate(fixedDeltaTime);
    }

    public void PumpSynchronizationContext(
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        var context = SynchronizationContext;
        if (context == null)
            return;

        context.Update(
            PostScheduler?.Options.MaxCompletionsPerPump ?? 0,
            exceptionPolicy,
            reportException);
    }

    internal void PumpEventExpectations()
    {
        if (!EventCenter.HasPendingExpectations)
            return;

        EventCenter.PumpExpectations(
            _eventExpectationFaultReporter);
    }

    public void SetLifecyclePlan(ScopeLifecyclePlan lifecyclePlan)
    {
        LifecyclePlan = lifecyclePlan ?? throw new ArgumentNullException(nameof(lifecyclePlan));
    }

    public void SetSnapPlan(ScopeSnapPlan snapPlan)
    {
        _snapPlan = snapPlan ?? throw new ArgumentNullException(nameof(snapPlan));
    }

    public void PumpFixedUpdate(FixedUpdateOptions options, float deltaTime)
    {
        if (!options.Enabled ||
            !LifecyclePlan.HasFixedUpdate ||
            !CanPumpLifecycle())
        {
            return;
        }

        try
        {
            _fixedUpdateAccumulator += deltaTime;
            int steps = 0;
            while (_fixedUpdateAccumulator >= options.FixedDeltaTime &&
                   steps < options.MaxStepsPerPump)
            {
                LifecyclePlan.PumpFixedUpdate(options.FixedDeltaTime);
                _fixedUpdateAccumulator -= options.FixedDeltaTime;
                steps++;
            }
        }
        catch (Exception ex)
        {
            ReportFault(ex, ScopeFaultPhase.ServiceUpdate);
        }
    }

    public void PumpUpdate(float deltaTime)
    {
        if (!LifecyclePlan.HasUpdate || !CanPumpLifecycle())
            return;

        try
        {
            LifecyclePlan.PumpUpdate(deltaTime);
        }
        catch (Exception ex)
        {
            ReportFault(ex, ScopeFaultPhase.ServiceUpdate);
        }
    }

    private bool CanPumpLifecycle()
    {
        return _safePointState == ScopeSafePointState.Running &&
               _state is ScopeRuntimeState.Created or ScopeRuntimeState.Running or ScopeRuntimeState.StopRequested;
    }

    public void RequireOwnerThread()
    {
        int ownerThreadId = Volatile.Read(ref _ownerThreadId);
        if (ownerThreadId == 0)
        {
            BindOwnerThreadIfNeeded();
            return;
        }

        if (Environment.CurrentManagedThreadId != ownerThreadId)
        {
            throw new InvalidOperationException(
                $"Scope `{Descriptor.Name}` local call must run on its owner thread.");
        }
    }

    private void BindOwnerThreadIfNeeded()
    {
        int currentThreadId = Environment.CurrentManagedThreadId;
        int ownerThreadId = Volatile.Read(ref _ownerThreadId);
        if (ownerThreadId == currentThreadId)
            return;

        if (ownerThreadId == 0 &&
            Interlocked.CompareExchange(ref _ownerThreadId, currentThreadId, 0) == 0)
            return;

        RequireOwnerThread();
    }

    public void RunRuntimeStop()
    {
        if (_runtimeStopRun)
            return;

        _runtimeStopRun = true;
        LifecyclePlan.RunRuntimeStopReverse();
    }

    public void RunLifecycleDispose()
    {
        if (_lifecycleDisposeRun)
            return;

        _lifecycleDisposeRun = true;
        LifecyclePlan.DisposeReverse();
    }

    private void DrainCompletionInbox()
    {
        while (Transport.CompletionInbox.TryDequeue(
                   out ScopeCompletionEnvelope envelope))
        {
            switch (envelope.Kind)
            {
                case ScopeCompletionKind.WorkerExecutionCompleted:
                {
                    var completion = envelope.WorkerCompletion;
                    WorkerJobs.HandleExecutionCompleted(
                        in completion,
                        PostScheduler);
                    break;
                }

                case ScopeCompletionKind.WorkerCancelRequested:
                    WorkerJobs.HandleCancelRequested(
                        envelope.WorkerHandle);
                    break;

                case ScopeCompletionKind.WorkerExecutionStarted:
                    WorkerJobs.MarkExecutionStarted(
                        envelope.WorkerHandle);
                    break;

                case ScopeCompletionKind.ScopeFault:
                    _runtime.ReportScopeFault(envelope.FaultRecord);
                    break;

                case ScopeCompletionKind.LifetimeOperationCompleted:
                    envelope.OperationLease.TryComplete();
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported scope completion kind: {envelope.Kind}.");
            }

            DisposeAfterControlIfNeeded();

            if (_state == ScopeRuntimeState.Disposed)
                return;
        }
    }

    private void DrainCallInbox()
    {
        while (Transport.CallInbox.TryDequeue(out var envelope))
        {
            try
            {
                if (DispatchLifecycleControlIfMatched(envelope))
                    continue;

                if (Descriptor.ScopeId == ScopeDefinitionIds.Main &&
                    _runtime.MainActorRuntime.DispatchCallRoute(
                        envelope.RouteId,
                        _runtimeId,
                        envelope,
                        Transport.CallPayloadStorage))
                {
                    continue;
                }

                if (envelope.Class == ScopeCallClass.BusinessRequest)
                {
                    CallRoutes.Dispatch(_runtimeId, envelope, Transport.CallPayloadStorage);
                    continue;
                }

                LocalCalls.Dispatch(_runtimeId, envelope, Transport.CallPayloadStorage);
            }
            finally
            {
                Transport.CallPayloadStorage.Release(envelope.Payload);
            }
        }
    }

    private bool DispatchLifecycleControlIfMatched(ScopeCallEnvelope envelope)
    {
        if (envelope.Kind != ScopeCallEnvelopeKind.Request ||
            envelope.Class != ScopeCallClass.Control)
        {
            return false;
        }

        switch (envelope.RouteId)
        {
            case ScopeLifecycleRouteIds.Stop:
                DispatchStopControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.Dispose:
                DispatchDisposeControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.EnterSafePoint:
                DispatchEnterSafePointControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.WriteSnapshot:
                DispatchWriteSnapshotControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.ReadSnapshot:
                DispatchReadSnapshotControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.ExitSafePoint:
                DispatchExitSafePointControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.CaptureDiagnostics:
                DispatchCaptureDiagnosticsControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.Initialize:
                DispatchInitializeControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.PostBuild:
                DispatchPostBuildControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.RuntimeStart:
                DispatchRuntimeStartControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.Prewarm:
                DispatchPrewarmControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.FreezeRuntimeRegistries:
                DispatchFreezeRuntimeRegistriesControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.RecompileTimerPlans:
                DispatchRecompileTimerPlansControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.SerializeFullSnap:
                DispatchSerializeFullSnapControl(envelope);
                return true;
            case ScopeLifecycleRouteIds.DeserializeFullSnap:
                DispatchDeserializeFullSnapControl(envelope);
                return true;
            default:
                envelope.Completion?.TrySetException(
                    new InvalidOperationException($"Unknown scope lifecycle control route {envelope.RouteId}."));
                return true;
        }
    }

    private bool ShouldYieldBusinessForSafePoint()
    {
        return _safePointState is ScopeSafePointState.Frozen or ScopeSafePointState.Restoring or ScopeSafePointState.Releasing;
    }

    private void DispatchStopControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeStopCall, ScopeStopResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope stop payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            StopOnOwnerThread();
            queuedCall.Completion.TrySetResult(new ScopeStopResponse(ScopeControlResult.Succeeded));
        }
        catch (Exception ex)
        {
            _state = ScopeRuntimeState.Faulted;
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchDisposeControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeDisposeCall, ScopeDisposeResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope dispose payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            if (_state != ScopeRuntimeState.Stopped)
                StopOnOwnerThread();

            _state = ScopeRuntimeState.Disposing;
            _pendingDisposeCompletion = queuedCall.Completion;
            _disposeRequestedFromControl = true;
        }
        catch (Exception ex)
        {
            _state = ScopeRuntimeState.Faulted;
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchEnterSafePointControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeEnterSafePointCall, ScopeEnterSafePointResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope safe point payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            queuedCall.Completion.TrySetResult(EnterSafePointForSnap());
        }
        catch (Exception ex)
        {
            _safePointState = ScopeSafePointState.Faulted;
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchWriteSnapshotControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeWriteSnapshotCall, ScopeWriteSnapshotResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope snapshot write payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            queuedCall.Completion.TrySetResult(WriteSnapshotForSnap());
        }
        catch (Exception ex)
        {
            _safePointState = ScopeSafePointState.Faulted;
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchReadSnapshotControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeReadSnapshotCall, ScopeReadSnapshotResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope snapshot read payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            queuedCall.Completion.TrySetResult(ReadSnapshotForSnap(queuedCall.Request.Document));
        }
        catch (Exception ex)
        {
            _safePointState = ScopeSafePointState.Faulted;
            ReportFault(ex, ScopeFaultPhase.Snapshot);
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchExitSafePointControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeExitSafePointCall, ScopeExitSafePointResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope safe point release payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            queuedCall.Completion.TrySetResult(ExitSafePointForSnap());
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchCaptureDiagnosticsControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeCaptureDiagnosticsCall, ScopeCaptureDiagnosticsResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope diagnostics payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            queuedCall.Completion.TrySetResult(
                new ScopeCaptureDiagnosticsResponse(
                    ScopeControlResult.Succeeded,
                    CaptureDiagnosticsOnOwnerThread()));
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchPrewarmControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopePrewarmCall, ScopePrewarmResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope prewarm payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            RequireOwnerThread();
            LayerPrewarmOptions options = queuedCall.Request.Options;
            Prewarm(in options);
            queuedCall.Completion.TrySetResult(new ScopePrewarmResponse(ScopeControlResult.Succeeded));
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchFreezeRuntimeRegistriesControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeFreezeRuntimeRegistriesCall, ScopeFreezeRuntimeRegistriesResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope freeze payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            RequireOwnerThread();
            FreezeRuntimeRegistries();
            queuedCall.Completion.TrySetResult(new ScopeFreezeRuntimeRegistriesResponse(ScopeControlResult.Succeeded));
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchRecompileTimerPlansControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeRecompileTimerPlansCall, ScopeRecompileTimerPlansResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope timer recompile payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            RequireOwnerThread();
            CompileTimerPlans();
            queuedCall.Completion.TrySetResult(new ScopeRecompileTimerPlansResponse(ScopeControlResult.Succeeded));
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchSerializeFullSnapControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeSerializeFullSnapCall, ScopeSerializeFullSnapResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope FullSnap serialize payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            queuedCall.Completion.TrySetResult(SerializeFullSnapOnOwnerThread());
        }
        catch (Exception ex)
        {
            ReportFault(ex, ScopeFaultPhase.Snapshot);
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchDeserializeFullSnapControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeDeserializeFullSnapCall, ScopeDeserializeFullSnapResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope FullSnap deserialize payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            queuedCall.Completion.TrySetResult(DeserializeFullSnapOnOwnerThread(queuedCall.Request.Document));
        }
        catch (Exception ex)
        {
            ReportFault(ex, ScopeFaultPhase.Snapshot);
            queuedCall.Completion.TrySetException(ex);
        }
    }

    internal ScopeEnterSafePointResponse EnterSafePointForSnap()
    {
        RequireOwnerThread();
        if (_state is ScopeRuntimeState.Stopping or ScopeRuntimeState.Stopped or ScopeRuntimeState.Disposing or ScopeRuntimeState.Disposed or ScopeRuntimeState.Faulted)
            throw new InvalidOperationException($"Scope `{Descriptor.Name}` cannot enter FullSnap safe point from state {_state}.");

        if (_safePointState == ScopeSafePointState.Frozen)
            return new ScopeEnterSafePointResponse(ScopeControlResult.Succeeded, _safePointToken);

        if (_safePointState != ScopeSafePointState.Running)
            throw new InvalidOperationException($"Scope `{Descriptor.Name}` cannot enter FullSnap safe point from safe state {_safePointState}.");

        _safePointState = ScopeSafePointState.Requesting;
        EcsScheduler.FlushStructuralChanges();
        _safePointToken++;
        _safePointState = ScopeSafePointState.Frozen;
        return new ScopeEnterSafePointResponse(ScopeControlResult.Succeeded, _safePointToken);
    }

    internal ScopeWriteSnapshotResponse WriteSnapshotForSnap()
    {
        RequireOwnerThread();
        if (_safePointState != ScopeSafePointState.Frozen)
            throw new InvalidOperationException($"Scope `{Descriptor.Name}` must be frozen before writing FullSnap.");

        return new ScopeWriteSnapshotResponse(
            ScopeControlResult.Succeeded,
            ScopeSnapExecutor.Write(_snapPlan));
    }

    internal ScopeReadSnapshotResponse ReadSnapshotForSnap(SnapDocument document)
    {
        RequireOwnerThread();
        if (_safePointState != ScopeSafePointState.Frozen)
            throw new InvalidOperationException($"Scope `{Descriptor.Name}` must be frozen before reading FullSnap.");

        try
        {
            _safePointState = ScopeSafePointState.Restoring;
            ScopeSnapExecutor.Read(_snapPlan, document);
            _safePointState = ScopeSafePointState.Frozen;
            return new ScopeReadSnapshotResponse(ScopeControlResult.Succeeded);
        }
        catch
        {
            _safePointState = ScopeSafePointState.Faulted;
            throw;
        }
    }

    internal ScopeSerializeFullSnapResponse SerializeFullSnapOnOwnerThread()
    {
        RequireOwnerThread();
        try
        {
            EnterSafePointForSnap();
            return new ScopeSerializeFullSnapResponse(
                ScopeControlResult.Succeeded,
                WriteSnapshotForSnap().Sections);
        }
        finally
        {
            ReleaseSafePointAfterFullSnapTransaction();
        }
    }

    internal ScopeDeserializeFullSnapResponse DeserializeFullSnapOnOwnerThread(SnapDocument document)
    {
        RequireOwnerThread();
        try
        {
            EnterSafePointForSnap();
            ReadSnapshotForSnap(document);
            return new ScopeDeserializeFullSnapResponse(ScopeControlResult.Succeeded);
        }
        finally
        {
            ReleaseSafePointAfterFullSnapTransaction();
        }
    }

    private void ReleaseSafePointAfterFullSnapTransaction()
    {
        if (_safePointState == ScopeSafePointState.Running)
            return;

        _safePointState = ScopeSafePointState.Releasing;
        _safePointState = ScopeSafePointState.Running;
    }

    private void DispatchInitializeControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeInitializeCall, ScopeInitializeResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope initialize payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            RequireOwnerThread();
            LifecyclePlan.RunInitialize();
            queuedCall.Completion.TrySetResult(new ScopeInitializeResponse(ScopeControlResult.Succeeded));
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchPostBuildControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopePostBuildCall, ScopePostBuildResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope post build payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            RequireOwnerThread();
            LifecyclePlan.RunPostBuild();
            queuedCall.Completion.TrySetResult(new ScopePostBuildResponse(ScopeControlResult.Succeeded));
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private void DispatchRuntimeStartControl(ScopeCallEnvelope envelope)
    {
        if (!Transport.CallPayloadStorage.TryGet<ScopeQueuedCall<ScopeRuntimeStartCall, ScopeRuntimeStartResponse>>(
                _runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope runtime start payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            RequireOwnerThread();
            LifecyclePlan.RunRuntimeStart();
            queuedCall.Completion.TrySetResult(new ScopeRuntimeStartResponse(ScopeControlResult.Succeeded));
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    internal ScopeExitSafePointResponse ExitSafePointForSnap()
    {
        RequireOwnerThread();
        if (_safePointState == ScopeSafePointState.Running)
            return new ScopeExitSafePointResponse(ScopeControlResult.Succeeded);

        if (_safePointState == ScopeSafePointState.Faulted)
            return new ScopeExitSafePointResponse(ScopeControlResult.Rejected);

        _safePointState = ScopeSafePointState.Releasing;
        _safePointState = ScopeSafePointState.Running;
        return new ScopeExitSafePointResponse(ScopeControlResult.Succeeded);
    }

    internal void StopOnOwnerThread()
    {
        if (_state == ScopeRuntimeState.Disposed ||
            _state == ScopeRuntimeState.Disposing ||
            _state == ScopeRuntimeState.Stopped)
        {
            return;
        }

        _state = ScopeRuntimeState.Stopping;
        Transport.CloseBusinessAdmission();
        WorkerJobs.BeginStopOnOwnerThread();
        RunRuntimeStop();
        _state = ScopeRuntimeState.Stopped;
    }

    private void DisposeAfterControlIfNeeded()
    {
        if (!_disposeRequestedFromControl)
            return;

        if (!WorkerJobs.CanDispose)
            return;

        var completion = _pendingDisposeCompletion;
        try
        {
            DisposeOwnerThreadResources(completion);
        }
        catch (Exception ex)
        {
            completion?.TrySetException(ex);
            _pendingDisposeCompletion = null;
            _disposeRequestedFromControl = false;
            _state = ScopeRuntimeState.Faulted;
            throw;
        }
    }

    private void DrainEventInbox()
    {
        var scheduler = PostScheduler;

        while (Transport.EventInbox.TryDequeue(out var envelope))
        {
            try
            {
                if (DispatchScopeFaultEventIfMatched(envelope))
                    continue;

                if (Descriptor.ScopeId == ScopeDefinitionIds.Main &&
                    _runtime.MainActorRuntime.DispatchProjectionRoute(
                        envelope.RouteId,
                        this,
                        _runtimeId,
                        envelope.Payload,
                        Transport.EventPayloadStorage))
                {
                    continue;
                }

                if (ActorProjectionScopeEventDispatcher.DispatchResultRoute(
                        envelope.RouteId,
                        EcsWorld,
                        _runtimeId,
                        envelope.Payload,
                        Transport.EventPayloadStorage))
                {
                    continue;
                }

                if (Descriptor.ScopeId == ScopeDefinitionIds.Main &&
                    _runtime.MainActorRuntime.DispatchCommandRoute(
                        envelope.RouteId,
                        _runtimeId,
                        envelope.Payload,
                        Transport.EventPayloadStorage))
                {
                    continue;
                }

                if (envelope.Class == ScopeEventClass.Business)
                {
                    EventRoutes.TryDispatch(_runtimeId, envelope, Transport.EventPayloadStorage);
                    continue;
                }

                if (scheduler != null)
                    Transport.EventPayloadStorage.Post(envelope.Payload, scheduler);
            }
            finally
            {
                Transport.EventPayloadStorage.Release(envelope.Payload);
            }
        }
    }

    private bool DispatchScopeFaultEventIfMatched(ScopeEventEnvelope envelope)
    {
        if (envelope.RouteId != ScopeFaultRouteIds.FaultEvent)
            return false;

        if (!Transport.EventPayloadStorage.TryGet<ScopeFaultEvent>(
                _runtimeId,
                envelope.Payload,
                out var faultEvent))
        {
            return true;
        }

        _runtime.ReportScopeFault(faultEvent.Record);
        return true;
    }

    internal void ReportFault(
        Exception exception,
        ScopeFaultPhase phase,
        int routeId = 0,
        int serviceSlot = -1,
        int contextSlot = -1)
    {
        var record = new ScopeFaultRecord(
            _runtimeId,
            _generation,
            Descriptor.ScopeId,
            phase,
            exception,
            routeId,
            serviceSlot,
            contextSlot);

        Interlocked.Increment(ref _faultCount);

        if (Descriptor.ScopeId == ScopeDefinitionIds.Main)
        {
            _runtime.ReportScopeFault(record);
            RequireHost().ApplyFaultPolicy(record);
            return;
        }

        if (TryGetScopeEndpoint(ScopeDefinitionIds.Main, out var mainEndpoint))
        {
            ScopeCompletionEnvelope envelope =
                ScopeCompletionEnvelope.ScopeFault(in record);
            mainEndpoint.Transport.EnqueueCompletion(in envelope);
        }

        RequireHost().ApplyFaultPolicy(record);
    }

    internal void ReportFatalFault(Exception exception, ScopeFaultPhase phase)
    {
        var record = new ScopeFaultRecord(
            _runtimeId,
            _generation,
            Descriptor.ScopeId,
            phase,
            exception);

        Interlocked.Increment(ref _faultCount);
        _state = ScopeRuntimeState.Faulted;
        Transport.CloseBusinessAdmission();

        _runtime.ReportScopeFault(record);
    }

    private void ReportEventExpectationFault(
        Exception exception)
    {
        _runtime.ReportLayerEventError(
            layerIndex: -1,
            source: $"Scope:{Descriptor.Name}",
            eventName: "EventMetaDataExpectation",
            exception);
    }

    private void SignalIngress()
    {
        Volatile.Write(ref _hasIngress, 1);
        _signalWorker?.Invoke();
    }

    internal bool HasIngress =>
        Volatile.Read(ref _hasIngress) != 0;

    internal bool HasImmediateWork
    {
        get
        {
            if (Volatile.Read(ref _hasIngress) != 0)
                return true;

            LayerBaseSynchronizationContext? context =
                SynchronizationContext;

            if (context != null && context.HasReadyWork)
                return true;

            PostScheduler? scheduler = PostScheduler;

            return scheduler != null &&
                   scheduler.HasPendingWork;
        }
    }

    internal bool IsOwnerThread
    {
        get
        {
            int ownerThreadId = Volatile.Read(ref _ownerThreadId);
            return ownerThreadId != 0 && Environment.CurrentManagedThreadId == ownerThreadId;
        }
    }

    internal int OwnerThreadId =>
        Volatile.Read(ref _ownerThreadId);

    private ScopeRuntimeHost RequireHost()
    {
        return _host ?? _runtime.ScopeHost;
    }

    internal ScopeDiagnosticsSnapshot CaptureDiagnostics()
    {
        if (Options.Threading == ScopeThreadingMode.Worker)
            throw new InvalidOperationException(
                $"Scope `{Descriptor.Name}` diagnostics must be captured through its owner-thread control call.");

        return CaptureDiagnosticsOnOwnerThread();
    }

    internal ScopeDiagnosticsSnapshot CaptureDiagnosticsOnOwnerThread()
    {
        RequireOwnerThread();
        var eventInbox = Transport.EventInbox.CaptureDiagnostics();
        var callInbox = Transport.CallInbox.CaptureDiagnostics();
        var tools = Descriptor.ScopeId == ScopeDefinitionIds.Main && _runtime.HasToolRegistry
            ? _runtime.Tools.CaptureDiagnostics()
            : default;

        return new ScopeDiagnosticsSnapshot(
            Descriptor.ScopeId,
            Descriptor.Name,
            _state,
            Volatile.Read(ref _ownerThreadId),
            Volatile.Read(ref _tickCount),
            lastTickDurationTicks: 0,
            maxTickDurationTicks: 0,
            eventInbox.Count,
            eventInbox.Capacity,
            eventInbox.Accepted,
            eventInbox.Rejected,
            eventInbox.HighWatermark,
            callInbox.Count,
            callInbox.Capacity,
            callInbox.Accepted,
            callInbox.Rejected,
            callInbox.HighWatermark,
            PostScheduler?.PendingCount ?? 0,
            Timer?.PendingCount ?? 0,
            DelayManager?.PendingCount ?? 0,
            SynchronizationContext?.PendingCount ?? 0,
            workerJobsPending: WorkerJobs.ActiveCount - WorkerJobs.RunningCount,
            workerJobsRunning: WorkerJobs.RunningCount,
            EcsScheduler.CaptureDiagnostics(),
            tools,
            new SnapDiagnosticsSnapshot(
                _safePointState,
                _snapPlan.Nodes.Length,
                serializeCount: 0,
                deserializeCount: 0,
                failureCount: 0),
            Volatile.Read(ref _faultCount));
    }

    private PostScheduler RequireScheduler()
    {
        return PostScheduler ?? throw new InvalidOperationException("Scope scheduler is not built.");
    }

    private EventBuildPolicyTable RequirePolicyTable()
    {
        return PolicyTable ?? throw new InvalidOperationException("Scope policy table is not built.");
    }

    public void Dispose()
    {
        DisposeOwnerThreadResources();
    }

    internal void DisposeUnstarted()
    {
#if DEBUG
        if (OwnerThreadId != 0 && !IsOwnerThread)
        {
            throw new InvalidOperationException(
                $"Unstarted scope `{Descriptor.Name}` already has a different owner thread.");
        }
#endif

        if (_state == ScopeRuntimeState.Disposed)
            return;

        WorkerJobs.BeginStopOnOwnerThread();

        if (!WorkerJobs.CanDispose)
        {
            throw new InvalidOperationException(
                "Unstarted scope unexpectedly owns active worker jobs.");
        }

        DisposeOwnerThreadResources();
    }

    private void DisposeOwnerThreadResources(
        ScopeCallCompletion<ScopeDisposeResponse>? disposeCompletion = null)
    {
        if (_state == ScopeRuntimeState.Disposed)
        {
            disposeCompletion?.TrySetResult(new ScopeDisposeResponse(ScopeControlResult.Succeeded));
            return;
        }

        _disposeRequestedFromControl = false;
        _pendingDisposeCompletion = null;
        if (_state != ScopeRuntimeState.Stopped)
            StopOnOwnerThread();

        _asyncCallOperations.CloseAdmission();
        _scopeLifetimeCancellation.Cancel();

        if (!_asyncCallOperations.IsDrained)
        {
            if (disposeCompletion != null)
            {
                _pendingDisposeCompletion = disposeCompletion;
                _disposeRequestedFromControl = true;
            }

            return;
        }

        if (WorkerJobs.CanDispose)
        {
            WorkerJobs.DisposeOnOwnerThread();
        }

        _state = ScopeRuntimeState.Disposing;
        var context = SynchronizationContext;
        if (context != null)
        {
            context.BeginClose(new OperationCanceledException("The scope runtime is disposing."));
            context.DrainClosingOperations(PostScheduler?.Options.MaxCompletionsPerPump ?? 0);
        }

        _runtime.DisposeScopeServices(ScopeId);
        RunLifecycleDispose();
        Transport.CloseAllAdmissionAndWaitForWriters();
        ReleaseCallInbox();
        ReleaseEventInbox();
        LocalCalls.Clear();
        CallRoutes.Clear();
        EventRoutes.Clear();
        ReleaseDelayPublishers();
        DelayManager?.Clear();
        DelayManager = null;
        Timer?.Dispose();
        Timer = null;
        PostScheduler?.Dispose();
        PostScheduler = null;
        EventCenter.Reset();
        context?.Dispose();
        SynchronizationContext = null;
        EcsScheduler.Dispose();
        EcsWorld.Dispose();
        _state = ScopeRuntimeState.Disposed;
        disposeCompletion?.TrySetResult(new ScopeDisposeResponse(ScopeControlResult.Succeeded));
        Transport.Dispose();
    }

    private void ReleaseDelayPublishers()
    {
        if (_delayPublishers.IsEmpty)
            return;

        var manager = DelayManager;
        foreach (var publisher in _delayPublishers.Values)
        {
            if (manager != null && publisher.PublisherId >= 0)
                manager.UnregisterPublisher(publisher.PublisherId);
            publisher.Deactivate();
        }

        _delayPublishers.Clear();
        MarkDelayDirty();
    }

    private void ReleaseCallInbox()
    {
        while (Transport.CallInbox.TryDequeue(out var envelope))
        {
            envelope.Completion?.TrySetException(new ObjectDisposedException(nameof(ScopeRuntime)));
            Transport.CallPayloadStorage.Release(envelope.Payload);
        }
    }

    private void ReleaseEventInbox()
    {
        while (Transport.EventInbox.TryDequeue(out var envelope))
            Transport.EventPayloadStorage.Release(envelope.Payload);
    }

}
