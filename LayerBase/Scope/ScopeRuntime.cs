using Arch.Core;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Event.Delay;

namespace LayerBase.Scope;

internal sealed class ScopeRuntime : IDisposable
{
    private readonly int _runtimeId;
    private readonly int _generation;
    private readonly EventPayloadStorage _callPayloadStorage = new();
    private readonly EventPayloadStorage _eventPayloadStorage = new();
    private ScopeRuntimeState _state = ScopeRuntimeState.Created;
    private ScopeTimerSink? _timerSink;
    private int _callSequence;
    private float _fixedUpdateAccumulator;
    private bool _runtimeStopRun;

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
        LocalCalls = new ScopeLocalCallRegistry();
        EcsWorld = World.Create();
        EcsWorld.BindRuntime(runtime);
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
            LayerCallRouteId<TRequest, TResponse>.Id,
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
        DrainEventInbox();
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
    }

    private void DrainCallInbox()
    {
        while (Transport.CallInbox.TryDequeue(out var envelope))
        {
            try
            {
                LocalCalls.Dispatch(_runtimeId, envelope, _callPayloadStorage);
            }
            finally
            {
                _callPayloadStorage.Release(envelope.Payload);
            }
        }
    }

    private void DrainEventInbox()
    {
        var scheduler = PostScheduler;
        if (scheduler == null)
            return;

        while (Transport.EventInbox.TryDequeue(out var envelope))
        {
            try
            {
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
        if (_state == ScopeRuntimeState.Disposed)
            return;

        _state = ScopeRuntimeState.Disposed;
        RunRuntimeStop();
        ReleaseCallInbox();
        ReleaseEventInbox();
        _callPayloadStorage.Dispose();
        _eventPayloadStorage.Dispose();
        LocalCalls.Clear();
        DelayManager?.Clear();
        DelayManager = null;
        Timer?.Dispose();
        Timer = null;
        PostScheduler?.Dispose();
        PostScheduler = null;
        EventCenter.Reset();
        SynchronizationContext?.Dispose();
        SynchronizationContext = null;
        EcsWorld.Dispose();
        Transport.Dispose();
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
