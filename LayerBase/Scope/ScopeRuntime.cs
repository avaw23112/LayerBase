using Arch.Core;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.Event.Delay;

namespace LayerBase.Scope;

internal sealed class ScopeRuntime : IDisposable
{
    private readonly int _runtimeId;
    private readonly int _generation;
    private readonly EventPayloadStorage _callPayloadStorage = new();
    private readonly EventPayloadStorage _eventPayloadStorage = new();
    private readonly ActorWorld? _actorWorld;
    private ActorAccessor _actors;
    private bool _hasActorAccessor;
    private ScopeRuntimeState _state = ScopeRuntimeState.Created;
    private ScopeTimerSink? _timerSink;
    private IReadOnlyDictionary<int, ScopeEndpoint> _scopeEndpoints =
        new Dictionary<int, ScopeEndpoint>();
    private int _callSequence;
    private float _fixedUpdateAccumulator;
    private bool _runtimeStopRun;
    private bool _disposeRequestedFromControl;

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
        _runtimeId = runtimeId;
        _generation = generation;
        Transport = new ScopeTransport(
            new ScopeAddress(runtimeId, generation, Descriptor.ScopeId));
        Transport.AttachRuntime(this);
        EventCenter = new EventCenter();
        LocalCalls = new ScopeLocalCallRegistry(Descriptor.ScopeId);
        if (Descriptor.ScopeId == ScopeDefinitionIds.Main)
        {
            _actorWorld = new ActorWorld(runtime);
            _actors = new ActorAccessor(new LocalActorAccessor(_actorWorld, _generation));
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
        if (_actorWorld != null)
            EcsWorld.BindProjectedActorCommandSink(new MainScopeProjectedActorCommandSink(_actorWorld));
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

    public ActorWorld ActorWorld =>
        _actorWorld ?? throw new InvalidOperationException("ActorWorld is only available in MainScope.");

    public ActorAccessor Actors =>
        _hasActorAccessor
            ? _actors
            : throw new InvalidOperationException("Scope actor accessor is not initialized.");

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

    public void TickTimer(float deltaTime)
    {
        Timer?.Tick(deltaTime, _timerSink!);
    }

    public void PrepareActorWorld()
    {
        ActorWorld.PrepareRuntimeBuild();
    }

    public void CompleteActorWorld()
    {
        ActorWorld.CompleteRuntimeBuild();
    }

    public void BindMainActorEndpoint(ScopeEndpoint mainEndpoint)
    {
        if (_actorWorld != null)
        {
            _actors = new ActorAccessor(new LocalActorAccessor(_actorWorld, _generation));
            EcsWorld.BindProjectedActorCommandSink(new MainScopeProjectedActorCommandSink(_actorWorld));
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
        for (int i = 0; i < scopes.Count; i++)
            endpoints[scopes[i].ScopeId] = scopes[i].Endpoint;

        _scopeEndpoints = endpoints;
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

    public void PumpActors(
        float deltaTime,
        float fixedDeltaTime,
        bool pumpFixedUpdate,
        ref RuntimeFrameBudget budget)
    {
        ActorWorld.Pump(
            deltaTime: deltaTime,
            fixedDeltaTime: fixedDeltaTime,
            pumpFixedUpdate: pumpFixedUpdate,
            budget: ref budget);
    }

    public LBTask<TResponse> CallLocalAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
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
        if (cancellationToken.IsCancellationRequested)
            return LBTask<TResponse>.FromCanceled(cancellationToken);

        var completion = new ScopeCallCompletion<TResponse>();
        var queuedCall = new ScopeQueuedCall<TRequest, TResponse>(
            request,
            completion,
            cancellationToken);
        var payload = _callPayloadStorage.Store(_runtimeId, in queuedCall);
        var token = new ScopeCallToken(
            _generation,
            Descriptor.ScopeId,
            Interlocked.Increment(ref _callSequence),
            version: 1);
        var envelope = new ScopeCallEnvelope(
            ScopeCallEnvelopeKind.Request,
            ScopeCallClass.BusinessRequest,
            token,
            Endpoint.Address,
            ScopeLocalCallRouteId<TRequest, TResponse>.Id,
            payload,
            ScopeCallResult.None,
            completion);

        var result = Transport.CallInbox.TryEnqueue(envelope, envelope.Class.ToAdmissionClass());
        if (result == ScopeEnqueueResult.Accepted)
            return completion.Task;

        _callPayloadStorage.Release(payload);
        completion.TrySetException(new InvalidOperationException($"Scope call enqueue failed: {result}."));
        return completion.Task;
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
        var payload = _callPayloadStorage.Store(_runtimeId, in queuedCall);
        var token = new ScopeCallToken(
            _generation,
            Descriptor.ScopeId,
            Interlocked.Increment(ref _callSequence),
            version: 1);
        var envelope = new ScopeCallEnvelope(
            ScopeCallEnvelopeKind.Request,
            ScopeCallClass.Control,
            token,
            Endpoint.Address,
            ScopeLifecycleRouteIds.Resolve<TRequest, TResponse>(),
            payload,
            ScopeCallResult.None,
            completion);

        var result = Transport.CallInbox.TryEnqueue(envelope, envelope.Class.ToAdmissionClass());
        if (result == ScopeEnqueueResult.Accepted)
            return completion.Task;

        _callPayloadStorage.Release(payload);
        completion.TrySetException(new InvalidOperationException($"Scope control call enqueue failed: {result}."));
        return completion.Task;
    }

    public ScopePostResult EnqueueEvent<TEvent>(in TEvent value)
        where TEvent : struct
    {
        if (_state == ScopeRuntimeState.Disposed)
            return ScopePostResult.RuntimeDisposed;

        var payload = _eventPayloadStorage.Store(_runtimeId, in value);
        var envelope = new ScopeEventEnvelope(
            Endpoint.Address,
            EventTypeId<TEvent>.Id,
            ScopeEventClass.Business,
            payload);

        var result = Transport.EventInbox.TryEnqueue(envelope, envelope.Class.ToAdmissionClass());
        if (result == ScopeEnqueueResult.Accepted)
            return ScopePostResult.Accepted;

        _eventPayloadStorage.Release(payload);
        return result switch
        {
            ScopeEnqueueResult.Full => ScopePostResult.QueueFull,
            ScopeEnqueueResult.Closed => ScopePostResult.RuntimeDisposed,
            ScopeEnqueueResult.StaleEndpoint => ScopePostResult.StaleEndpoint,
            _ => ScopePostResult.Rejected
        };
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
        TickTimer(deltaTime);
        DelayManager?.Tick(deltaTime);
        PumpIngress();
        PostScheduler?.Pump();
        PumpUpdate(deltaTime);
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
        if (!options.Enabled)
            return;

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

    public void PumpUpdate(float deltaTime)
    {
        LifecyclePlan.PumpUpdate(deltaTime);
    }

    public void RunRuntimeStop()
    {
        if (_runtimeStopRun)
            return;

        _runtimeStopRun = true;
        LifecyclePlan.RunRuntimeStopReverse();
        _actorWorld?.RuntimeStop();
    }

    private void DrainCallInbox()
    {
        while (Transport.CallInbox.TryDequeue(out var envelope))
        {
            try
            {
                if (TryDispatchLifecycleControl(envelope))
                    continue;

                if (_actorWorld != null &&
                    ActorCallDispatcherRegistry.TryDispatch(
                        envelope.RouteId,
                        _actorWorld,
                        _runtimeId,
                        envelope,
                        _callPayloadStorage))
                {
                    continue;
                }

                LocalCalls.Dispatch(_runtimeId, envelope, _callPayloadStorage);
            }
            finally
            {
                _callPayloadStorage.Release(envelope.Payload);
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
        if (!_callPayloadStorage.TryGet<ScopeQueuedCall<ScopeStopCall, ScopeStopResponse>>(
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
        if (!_callPayloadStorage.TryGet<ScopeQueuedCall<ScopeDisposeCall, ScopeDisposeResponse>>(
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
            _disposeRequestedFromControl = true;
            queuedCall.Completion.TrySetResult(new ScopeDisposeResponse(ScopeControlResult.Succeeded));
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
        if (_disposeRequestedFromControl)
            DisposeOwnerThreadResources();
    }

    private void DrainEventInbox()
    {
        var scheduler = PostScheduler;

        while (Transport.EventInbox.TryDequeue(out var envelope))
        {
            try
            {
                if (_actorWorld != null &&
                    ActorProjectionScopeEventDispatcher.TryDispatchCommand(
                        envelope.RouteId,
                        this,
                        _actorWorld,
                        _runtimeId,
                        envelope.Payload,
                        _eventPayloadStorage))
                {
                    continue;
                }

                if (ActorProjectionScopeEventDispatcher.TryDispatchResult(
                        envelope.RouteId,
                        EcsWorld,
                        _runtimeId,
                        envelope.Payload,
                        _eventPayloadStorage))
                {
                    continue;
                }

                if (_actorWorld != null &&
                    ActorCommandDispatcherRegistry.TryDispatch(
                        envelope.RouteId,
                        _actorWorld,
                        _runtimeId,
                        envelope.Payload,
                        _eventPayloadStorage))
                {
                    continue;
                }

                if (scheduler != null)
                    _eventPayloadStorage.Post(envelope.Payload, scheduler);
            }
            finally
            {
                _eventPayloadStorage.Release(envelope.Payload);
            }
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

    private void DisposeOwnerThreadResources()
    {
        if (_state == ScopeRuntimeState.Disposed)
            return;

        _disposeRequestedFromControl = false;
        if (_state != ScopeRuntimeState.Stopped)
            StopOnOwnerThread();

        _state = ScopeRuntimeState.Disposing;
        ReleaseCallInbox();
        ReleaseEventInbox();
        _callPayloadStorage.Dispose();
        _eventPayloadStorage.Dispose();
        LocalCalls.Clear();
        _actorWorld?.Dispose();
        DelayManager?.Clear();
        DelayManager = null;
        Timer?.Dispose();
        Timer = null;
        PostScheduler?.Dispose();
        PostScheduler = null;
        EventCenter.Reset();
        SynchronizationContext?.Dispose();
        SynchronizationContext = null;
        EcsScheduler.Dispose();
        EcsWorld.Dispose();
        Transport.Dispose();
        _state = ScopeRuntimeState.Disposed;
    }

    private void ReleaseCallInbox()
    {
        while (Transport.CallInbox.TryDequeue(out var envelope))
        {
            envelope.Completion?.TrySetException(new ObjectDisposedException(nameof(ScopeRuntime)));
            _callPayloadStorage.Release(envelope.Payload);
        }
    }

    private void ReleaseEventInbox()
    {
        while (Transport.EventInbox.TryDequeue(out var envelope))
            _eventPayloadStorage.Release(envelope.Payload);
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
