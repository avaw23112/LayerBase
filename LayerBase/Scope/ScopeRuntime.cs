using System.Collections.Concurrent;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.Event.Delay;
using LayerBase.Worker;

namespace LayerBase.Scope;

internal sealed class ScopeRuntime : IDisposable
{
    private readonly int _runtimeId;
    private readonly int _generation;
    private readonly LayerRuntime _runtime;
    private readonly MainActorRuntime? _mainActors;
    private ActorAccessor _actors;
    private bool _hasActorAccessor;
    private ScopeRuntimeState _state = ScopeRuntimeState.Created;
    private ScopeTimerSink? _timerSink;
    private IReadOnlyDictionary<int, ScopeEndpoint> _scopeEndpoints =
        new Dictionary<int, ScopeEndpoint>();
    private IReadOnlyDictionary<int, ScopeRuntime> _scopeRuntimes =
        new Dictionary<int, ScopeRuntime>();
    private ScopeRuntime? _mainScopeRuntime;
    private float _fixedUpdateAccumulator;
    private bool _runtimeStopRun;
    private bool _lifecycleDisposeRun;
    private bool _disposeRequestedFromControl;
    private int _ownerThreadId;
    private ScopeCallCompletion<ScopeDisposeResponse>? _pendingDisposeCompletion;
    private readonly ConcurrentDictionary<Type, IDelayPublisherInternal> _delayPublishers = new();

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
        _runtimeId = runtimeId;
        _generation = generation;
        if (Descriptor.ScopeId == ScopeDefinitionIds.Main)
            _mainActors = new MainActorRuntime(runtime, generation);
        Transport = new ScopeTransport(
            new ScopeAddress(runtimeId, generation, Descriptor.ScopeId));
        EventCenter = new EventCenter();
        LocalCalls = new ScopeLocalCallRegistry(Descriptor.ScopeId);
        if (_mainActors != null)
        {
            _mainActors.BindProjectionSink();
            _actors = _mainActors.Accessor;
            _hasActorAccessor = true;
        }
        EcsScheduler = new ScopeEcsScheduler(
            _generation,
            Descriptor.ScopeId,
            World.Create(),
            EcsRuntimeOptions.Default);
        EcsWorld = EcsScheduler.World;
        EcsWorld.BindRuntime(runtime);
        EcsWorld.BindEcsScheduler(EcsScheduler);
        if (_mainActors != null)
            EcsWorld.BindProjectedActorCommandSink(_mainActors.ProjectedActorCommandSink);
    }

    public ScopeDescriptor Descriptor { get; }

    public int ScopeId => Descriptor.ScopeId;

    public ScopeOptions Options { get; }

    public ScopeTransport Transport { get; }

    public ScopeEndpoint Endpoint => Transport.Endpoint;

    public EventCenter EventCenter { get; }

    public PostScheduler? PostScheduler { get; private set; }

    public TimeScheduler<ITimerAction>? Timer { get; private set; }

    public DelayPublisherManager? DelayManager { get; private set; }

    public ActorAccessor Actors =>
        _hasActorAccessor
            ? _actors
            : throw new InvalidOperationException("Scope actor accessor is not initialized.");

    public MainActorRuntime? MainActors => _mainActors;

    public ScopeEcsScheduler EcsScheduler { get; }

    public World EcsWorld { get; }

    public LayerBaseSynchronizationContext? SynchronizationContext { get; private set; }

    public ScopeLocalCallRegistry LocalCalls { get; }

    public LayerProviderRuntime[] LayerProviders { get; }

    public ScopeLayerSlice[] LayerSlices { get; }

    public ScopeLifecyclePlan LifecyclePlan { get; private set; }

    public ScopeRuntimeState State => _state;

    public EventBuildPolicyTable? PolicyTable { get; private set; }

    public void InstallSynchronizationContext()
    {
        BindOwnerThreadIfNeeded();
        SynchronizationContext ??= LayerBaseSynchronizationContext.Install();
    }

    public void InitializeOrUpdateScheduler(
        PostSchedulerOptions options,
        EventBuildPolicyTable policyTable,
        PostTypePlan[] plans)
    {
        PolicyTable = policyTable ?? throw new ArgumentNullException(nameof(policyTable));
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
        Timer = new TimeScheduler<ITimerAction>(options);
        _timerSink = new ScopeTimerSink(RequireScheduler());
    }

    public void InitializeDelay(DelayBufferOptions options)
    {
        DelayManager = DelayPublisherManager.Create(options, RequirePolicyTable());
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
        Timer?.Tick(deltaTime, _timerSink!);
    }

    public void BindMainActorEndpoint(ScopeEndpoint mainEndpoint)
    {
        if (_mainActors != null)
        {
            _mainActors.BindProjectionSink();
            _actors = _mainActors.Accessor;
            EcsWorld.BindProjectedActorCommandSink(_mainActors.ProjectedActorCommandSink);
        }
        else
        {
            _actors = new ActorAccessor(new RemoteActorAccessor(
                new ScopeRef<MainScope>(mainEndpoint),
                Descriptor.ScopeId,
                _generation));
            EcsWorld.BindProjectedActorCommandSink(new ScopeEventProjectedActorCommandSink(
                new ScopeRef<MainScope>(mainEndpoint),
                Descriptor.ScopeId,
                _generation));
        }

        _hasActorAccessor = true;
    }

    public void BindScopeEndpoints(IReadOnlyList<ScopeRuntime> scopes)
    {
        var endpoints = new Dictionary<int, ScopeEndpoint>(scopes.Count);
        var runtimes = new Dictionary<int, ScopeRuntime>(scopes.Count);
        for (int i = 0; i < scopes.Count; i++)
        {
            endpoints[scopes[i].ScopeId] = scopes[i].Endpoint;
            runtimes[scopes[i].ScopeId] = scopes[i];
        }

        _scopeEndpoints = endpoints;
        _scopeRuntimes = runtimes;
        _mainScopeRuntime = runtimes.TryGetValue(ScopeDefinitionIds.Main, out var mainScope)
            ? mainScope
            : null;
    }

    public bool TryPostEventToScope<TEvent>(
        int scopeId,
        in TEvent value)
        where TEvent : struct
    {
        if (!_scopeEndpoints.TryGetValue(scopeId, out ScopeEndpoint endpoint))
            return false;

        IScopeEventWriter? writer = endpoint.EventWriter;
        return writer != null && writer.Post(in value).IsAccepted;
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
        DrainCallInbox();
        DisposeAfterControlIfNeeded();
        if (_state == ScopeRuntimeState.Disposed)
            return;

        DrainEventInbox();
        DisposeAfterControlIfNeeded();
    }

    public void PumpScopeResources(float deltaTime)
    {
        BindOwnerThreadIfNeeded();
        var context = SynchronizationContext;
        if (context != null)
        {
            using var scope = context.EnterScope();
            PumpScopeResourcesCore(deltaTime);
            return;
        }

        PumpScopeResourcesCore(deltaTime);
    }

    private void PumpScopeResourcesCore(float deltaTime)
    {
        PumpIngress();
        if (!CanPumpLifecycle())
            return;

        PumpSynchronizationContext(
            CompletionExceptionPolicy.Throw,
            null);
        TickTimer(deltaTime);
        DelayManager?.Tick(deltaTime);
        PostScheduler?.Pump();
        PumpUpdate(deltaTime);
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

    public void ClearLocalCallRegistry()
    {
        LocalCalls.Clear();
    }

    public void SetLifecyclePlan(ScopeLifecyclePlan lifecyclePlan)
    {
        LifecyclePlan = lifecyclePlan ?? throw new ArgumentNullException(nameof(lifecyclePlan));
    }

    public void PumpFixedUpdate(FixedUpdateOptions options, float deltaTime)
    {
        if (!options.Enabled || !CanPumpLifecycle())
            return;

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
        if (!CanPumpLifecycle())
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
        return _state is ScopeRuntimeState.Created or ScopeRuntimeState.Running or ScopeRuntimeState.StopRequested;
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
        _mainActors?.RuntimeStop();
    }

    public void RunLifecycleDispose()
    {
        if (_lifecycleDisposeRun)
            return;

        _lifecycleDisposeRun = true;
        LifecyclePlan.DisposeReverse();
    }

    private void DrainCallInbox()
    {
        while (Transport.CallInbox.TryDequeue(out var envelope))
        {
            try
            {
                if (TryDispatchLifecycleControl(envelope))
                    continue;

                if (_mainActors != null &&
                    _mainActors.TryDispatchCall(
                        envelope.RouteId,
                        _runtimeId,
                        envelope,
                        Transport.CallPayloadStorage))
                {
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

    private bool TryDispatchLifecycleControl(ScopeCallEnvelope envelope)
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
            default:
                envelope.Completion?.TrySetException(
                    new InvalidOperationException($"Unknown scope lifecycle control route {envelope.RouteId}."));
                return true;
        }
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

    private void StopOnOwnerThread()
    {
        if (_state == ScopeRuntimeState.Disposed ||
            _state == ScopeRuntimeState.Disposing ||
            _state == ScopeRuntimeState.Stopped)
        {
            return;
        }

        _state = ScopeRuntimeState.Stopping;
        Transport.CloseBusinessAdmission();
        RunRuntimeStop();
        _state = ScopeRuntimeState.Stopped;
    }

    private void DisposeAfterControlIfNeeded()
    {
        if (!_disposeRequestedFromControl)
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
                if (TryDispatchScopeFaultEvent(envelope))
                    continue;

                if (_mainActors != null &&
                    _mainActors.TryDispatchProjectionCommand(
                        envelope.RouteId,
                        this,
                        _runtimeId,
                        envelope.Payload,
                        Transport.EventPayloadStorage))
                {
                    continue;
                }

                if (ActorProjectionScopeEventDispatcher.TryDispatchResult(
                        envelope.RouteId,
                        EcsWorld,
                        _runtimeId,
                        envelope.Payload,
                        Transport.EventPayloadStorage))
                {
                    continue;
                }

                if (WorkerScopeEventDispatcher.TryDispatch(
                        envelope.RouteId,
                        _runtimeId,
                        envelope.Payload,
                        Transport.EventPayloadStorage,
                        scheduler))
                {
                    continue;
                }

                if (_mainActors != null &&
                    _mainActors.TryDispatchCommand(
                        envelope.RouteId,
                        _runtimeId,
                        envelope.Payload,
                        Transport.EventPayloadStorage))
                {
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

    private bool TryDispatchScopeFaultEvent(ScopeEventEnvelope envelope)
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
        ApplyFaultPolicy(faultEvent.Record);
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

        _state = ScopeRuntimeState.Faulted;
        Transport.CloseBusinessAdmission();

        if (Descriptor.ScopeId == ScopeDefinitionIds.Main)
        {
            _runtime.ReportScopeFault(record);
            ApplyFaultPolicy(record);
            return;
        }

        ScopeRuntime? mainScope = _mainScopeRuntime;
        if (mainScope != null)
            _ = mainScope.EnqueueScopeFaultEvent(record);
    }

    private ScopePostResult EnqueueScopeFaultEvent(in ScopeFaultRecord record)
    {
        if (_state == ScopeRuntimeState.Disposed)
            return ScopePostResult.RuntimeDisposed;

        var faultEvent = new ScopeFaultEvent(record);
        var payload = Transport.EventPayloadStorage.Store(_runtimeId, in faultEvent);
        var envelope = new ScopeEventEnvelope(
            new ScopeAddress(record.RuntimeId, record.RuntimeGeneration, record.SourceScopeId),
            ScopeFaultRouteIds.FaultEvent,
            ScopeEventClass.Critical,
            payload);

        var result = Transport.EventInbox.TryEnqueue(envelope, envelope.Class.ToAdmissionClass());
        if (result == ScopeEnqueueResult.Accepted)
            return ScopePostResult.Accepted;

        Transport.EventPayloadStorage.Release(payload);
        return ScopeTransport.ToPostResult(result);
    }

    private void ApplyFaultPolicy(in ScopeFaultRecord record)
    {
        if (!_scopeRuntimes.TryGetValue(record.SourceScopeId, out var sourceScope))
            return;

        switch (sourceScope.Options.FaultPolicy)
        {
            case ScopeFaultPolicy.StopScope:
                _ = sourceScope.RequestStopAsync();
                break;
            case ScopeFaultPolicy.StopRuntime:
                if (_scopeRuntimes.TryGetValue(ScopeDefinitionIds.Main, out var mainScope))
                    _ = mainScope.RequestStopAsync();
                break;
        }
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

        _state = ScopeRuntimeState.Disposing;
        var context = SynchronizationContext;
        if (context != null)
        {
            context.BeginClose(new OperationCanceledException("The scope runtime is disposing."));
            context.DrainClosingOperations(PostScheduler?.Options.MaxCompletionsPerPump ?? 0);
        }

        RunLifecycleDispose();
        _mainActors?.Dispose();
        ReleaseCallInbox();
        ReleaseEventInbox();
        LocalCalls.Clear();
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

    private sealed class ScopeTimerSink : IExpiredTimerSink<ITimerAction>
    {
        private readonly PostScheduler _scheduler;

        public ScopeTimerSink(PostScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public bool TryAcceptExpired(in ITimerAction payload, TimerHandle handle)
        {
            return payload.Execute(_scheduler);
        }
    }
}
