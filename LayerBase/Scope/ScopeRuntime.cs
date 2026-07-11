using System.Diagnostics;
using System.Threading;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Core.DataStruct;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.ECS.Runtime;
using LayerBase.ECS.Runtime.Query;

namespace LayerBase.Scope;

public delegate void ScopePostDispatcher(ScopeRuntime scope, ScopePostMessage message);

public delegate void ScopeCallDispatcher(ScopeRuntime scope, ScopeCallMessage message);

public enum ScopeInboxKind
{
    Local,
    Locked
}

public sealed class ScopeRuntime : IDisposable
{
    private readonly IBoundedQueue<ScopePostMessage> _postInbox;
    private readonly IBoundedQueue<ScopeCallMessage> _callInbox;
    private readonly IBoundedQueue<LayerContinuation> _continuations;
    private readonly IBoundedQueue<float> _manualPumps;
    private readonly ScopePostDispatcher? _postDispatcher;
    private readonly ScopeCallDispatcher? _callDispatcher;
    private readonly ScopeTimerSink _timerSink;
    private readonly EventBuildPolicyTable _policyTable;
    private readonly LayerExceptionOptions _exceptionOptions = new();
    private LayerBaseSynchronizationContext? _context;
    private ScopeRouteTable? _routes;
    private Thread? _workerThread;
    private volatile bool _started;
    private volatile bool _stopped;
    private volatile bool _disposed;
    private volatile bool _workerRunning;

    public ScopeRuntime(
        ScopeDescriptor descriptor,
        IService[] services,
        ScopeRuntimeOptions? options = null,
        ActorWorld? sharedActorWorld = null,
        LayerRuntime? owningRuntime = null,
        ScopePostDispatcher? postDispatcher = null,
        ScopeCallDispatcher? callDispatcher = null)
    {
        if (descriptor.ScopeId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(descriptor));
        }

        options ??= ScopeRuntimeOptions.Default;

        Descriptor = descriptor;
        Services = services ?? throw new ArgumentNullException(nameof(services));
        EventCenter = new EventCenter();
        _policyTable = new EventBuildPolicyTable(options.PostSchedulerOptions.DefaultBackpressure);
        PostScheduler = new PostScheduler(
            descriptor.ScopeId,
            EventCenter,
            options.PostSchedulerOptions,
            _policyTable);
        PostScheduler.BuildPlans(Array.Empty<PostTypePlan>());
        EventCenter.PostScheduler = PostScheduler;
        Timer = new TimeScheduler<ITimerAction>(options.TimeSchedulerOptions);
        _timerSink = new ScopeTimerSink(PostScheduler);
        Actors = sharedActorWorld ?? new ActorWorld();
        EcsWorld = World.Create();
        EcsWorld.BindScopeActors(Actors);
        EcsQueryRegistry = new EcsQueryRegistry(EcsWorld);
        OwningRuntime = owningRuntime;
        InitializeEcsScheduler(options);

        PostInboxKind = descriptor.Threading == ScopeThreadingMode.Worker
            ? ScopeInboxKind.Locked
            : ScopeInboxKind.Local;
        ContinuationInboxKind = ScopeInboxKind.Locked;

        _postInbox = CreateQueue<ScopePostMessage>(PostInboxKind, options.PostQueueCapacity);
        _callInbox = CreateQueue<ScopeCallMessage>(PostInboxKind, options.CallQueueCapacity);
        _continuations = CreateQueue<LayerContinuation>(ContinuationInboxKind, options.ContinuationQueueCapacity);
        _manualPumps = CreateQueue<float>(ScopeInboxKind.Locked, options.ContinuationQueueCapacity);
        _postDispatcher = postDispatcher;
        _callDispatcher = callDispatcher;

        BindServices();
    }

    private void InitializeEcsScheduler(ScopeRuntimeOptions options)
    {
        EcsRuntimeOptions ecsOptions = options.EcsOptions ?? EcsRuntimeOptions.Default;
        EcsOptions = ecsOptions;

        if (OwningRuntime == null)
        {
            return;
        }

        EcsScheduler = ecsOptions.ExecutionMode switch
        {
            EcsExecutionMode.Sync => new SyncEcsScheduler(OwningRuntime, EcsWorld),
            EcsExecutionMode.Async => new AsyncEcsScheduler(OwningRuntime, EcsWorld, ecsOptions),
            _ => null
        };
    }

    public int ScopeId => Descriptor.ScopeId;

    public ScopeDescriptor Descriptor { get; }

    public IService[] Services { get; }

    public EventCenter EventCenter { get; }

    public PostScheduler PostScheduler { get; }

    public TimeScheduler<ITimerAction> Timer { get; }

    public ActorWorld Actors { get; }

    public World EcsWorld { get; }

    public EcsQueryRegistry EcsQueryRegistry { get; }

    public IEcsScheduler? EcsScheduler { get; private set; }

    public EcsRuntimeOptions EcsOptions { get; private set; }

    public LayerRuntime? OwningRuntime { get; }

    public ScopeInboxKind PostInboxKind { get; }

    public ScopeInboxKind ContinuationInboxKind { get; }

    public int PostInboxCount => _postInbox.Count;

    public int CallInboxCount => _callInbox.Count;

    public int ContinuationCount => _continuations.Count;

    public ScopeRef<TScope> GetScopeRef<TScope>(int targetScopeId)
    {
        ThrowIfDisposed();
        if (_routes == null)
        {
            throw new InvalidOperationException("Scope route table is not bound.");
        }

        return _routes.GetScopeRef<TScope>(targetScopeId);
    }

    public ScopeRef<TScope> GetScopeRef<TScope>()
    {
        ThrowIfDisposed();
        if (_routes == null)
        {
            throw new InvalidOperationException("Scope route table is not bound.");
        }

        return _routes.GetScopeRef<TScope>();
    }

    internal void BindRoutes(ScopeRouteTable routes)
    {
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
    }

    public void Start()
    {
        ThrowIfDisposed();
        if (_started)
        {
            return;
        }

        _started = true;
        _stopped = false;

        if (Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            _workerRunning = true;
            _workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"LayerBase.Scope.{Descriptor.Name}"
            };
            _workerThread.Start();
            return;
        }

        ExecuteInScope(StartScope);
    }

    public void Pump(float deltaTime)
    {
        ThrowIfDisposed();
        if (_stopped || Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            if (!_stopped &&
                Descriptor.Threading == ScopeThreadingMode.Worker &&
                Descriptor.Clock == ScopeClockMode.Manual)
            {
                if (!_manualPumps.TryEnqueue(deltaTime))
                {
                    throw new InvalidOperationException(
                        $"Scope '{Descriptor.Name}' manual pump queue is full.");
                }
            }

            return;
        }

        ExecuteInScope(() => PumpInternal(deltaTime));
    }

    public void Stop()
    {
        if (_disposed || _stopped)
        {
            return;
        }

        _stopped = true;

        if (Descriptor.Threading == ScopeThreadingMode.Worker && _workerThread != null)
        {
            _workerRunning = false;
            if (!ReferenceEquals(Thread.CurrentThread, _workerThread))
            {
                _workerThread.Join();
                _workerThread = null;
            }

            return;
        }

        ExecuteInScope(StopInternal);
    }

    public bool TryPost(ScopePostMessage message)
    {
        ThrowIfDisposed();
        if (_stopped)
        {
            return false;
        }

        return _postInbox.TryEnqueue(message);
    }

    public bool TryCall(ScopeCallMessage message)
    {
        ThrowIfDisposed();
        if (_stopped)
        {
            return false;
        }

        return _callInbox.TryEnqueue(message);
    }

    public bool TryEnqueueContinuation(Action continuation)
    {
        if (continuation == null)
        {
            throw new ArgumentNullException(nameof(continuation));
        }

        return TryEnqueueContinuation(new LayerContinuation(
            continuation,
            serviceId: -1,
            taskId: -1,
            trace: ScopeTrace.Empty));
    }

    public bool TryEnqueueContinuation(in LayerContinuation continuation)
    {
        if (continuation.Action == null)
        {
            throw new ArgumentNullException(nameof(continuation));
        }

        ThrowIfDisposed();
        if (_stopped)
        {
            return false;
        }

        return _continuations.TryEnqueue(continuation);
    }

    public TimerHandle SchedulePost<T>(
        in T value,
        float delaySeconds,
        EventPostPolicy? expiredPostPolicy = default,
        int repeatCount = 0,
        float intervalSeconds = 0,
        TimerRepeatMode? repeatMode = default,
        TimerCatchUpPolicy? catchUpPolicy = default)
        where T : struct
    {
        ThrowIfDisposed();

        int eventId = EventTypeId<T>.Id;
        EventTimerPolicy? timerPolicy = _policyTable.GetTimerPolicy(eventId);

        return Timer.Schedule(
            new PostEventAction<T>(
                value,
                expiredPostPolicy ?? timerPolicy?.ExpiredPostPolicy),
            delaySeconds,
            repeatCount: repeatCount,
            intervalSeconds: intervalSeconds,
            repeatMode: repeatMode ?? timerPolicy?.RepeatMode,
            catchUpPolicy: catchUpPolicy ?? timerPolicy?.CatchUpPolicy);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
        EcsScheduler?.Dispose();
        _context?.Dispose();
        Timer.Dispose();
        PostScheduler.Dispose();
        EventCenter.Reset();
        EcsWorld.Dispose();
        Actors.Dispose();
    }

    private void WorkerLoop()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            long lastTicks = stopwatch.ElapsedTicks;

            ExecuteInScope(StartScope);

            if (Descriptor.Clock == ScopeClockMode.Manual)
            {
                RunManualWorkerLoop();
                ExecuteInScope(StopInternal);
                return;
            }

            while (_workerRunning)
            {
                float deltaTime = GetWorkerDeltaTime(stopwatch, ref lastTicks);
                ExecuteInScope(() => PumpInternal(deltaTime));
                SleepWorker();
            }

            ExecuteInScope(StopInternal);
        }
        catch (Exception ex)
        {
            ReportException(ex, -1, LayerExceptionPhase.WorkerLoop, LayerQueueKind.None, -1);
            _workerRunning = false;
            _stopped = true;
        }
    }

    private void RunManualWorkerLoop()
    {
        while (_workerRunning)
        {
            if (_manualPumps.TryDequeue(out float deltaTime))
            {
                ExecuteInScope(() => PumpInternal(deltaTime));
                continue;
            }

            Thread.Sleep(1);
        }
    }

    private static float GetElapsedSeconds(Stopwatch stopwatch, ref long lastTicks)
    {
        long currentTicks = stopwatch.ElapsedTicks;
        long elapsedTicks = currentTicks - lastTicks;
        lastTicks = currentTicks;
        return elapsedTicks / (float)Stopwatch.Frequency;
    }

    private float GetWorkerDeltaTime(Stopwatch stopwatch, ref long lastTicks)
    {
        if (Descriptor.Clock == ScopeClockMode.FixedRate && Descriptor.TickRateHz > 0)
        {
            _ = GetElapsedSeconds(stopwatch, ref lastTicks);
            return 1f / Descriptor.TickRateHz;
        }

        return GetElapsedSeconds(stopwatch, ref lastTicks);
    }

    private void ExecuteInScope(Action action)
    {
        using (ScopeExecution.Enter(this))
        using (GetOrCreateContext().EnterScope())
        {
            action();
        }
    }

    private LayerBaseSynchronizationContext GetOrCreateContext()
    {
        return _context ??= LayerBaseSynchronizationContext.Install();
    }

    private void StartServices()
    {
        for (int i = 0; i < Services.Length; i++)
        {
            if (Services[i] is not IInitializable initializable)
            {
                continue;
            }

            try
            {
                initializable.Initialize();
            }
            catch (Exception ex)
            {
                ReportException(ex, serviceId: i, LayerExceptionPhase.ServiceStart, LayerQueueKind.None, messageId: -1);
                ApplyExceptionPolicy(LayerExceptionPhase.ServiceStart, ex);
            }
        }
    }

    private void StartScope()
    {
        EcsScheduler?.Start();
        Actors.PrepareRuntimeBuild();
        StartServices();
        Actors.CompleteRuntimeBuild();
    }

    private void PumpInternal(float deltaTime)
    {
        _context?.Update();
        Timer.Tick(deltaTime, _timerSink);
        DrainPostInbox();
        DrainCallInbox();
        PostScheduler.Pump();

        for (int i = 0; i < Services.Length; i++)
        {
            if (Services[i] is LayerBase.DI.Options.IUpdate update)
            {
                update.Update();
            }
        }

        PumpActors(deltaTime);
        EcsWorld.SweepProjectedActors();
        EcsScheduler?.DrainResults(EcsOptions.MaxResultsDrainPerPump);
        DrainContinuations();
    }

    private void PumpActors(float deltaTime)
    {
        var actorBudget = new RuntimeFrameBudget(
            maxEvents: 0,
            usedEvents: 0,
            deadlineTicks: 0);
        bool pumpFixedUpdate = Descriptor.Clock == ScopeClockMode.FixedRate;

        Actors.Pump(
            deltaTime: deltaTime,
            fixedDeltaTime: pumpFixedUpdate ? deltaTime : 0f,
            pumpFixedUpdate: pumpFixedUpdate,
            budget: ref actorBudget);
    }

    private void StopInternal()
    {
        EcsScheduler?.Stop();

        if (Descriptor.StopPolicy == ScopeStopPolicy.Drain)
        {
            DrainPostInbox();
            DrainCallInbox();
            DrainContinuations();
        }
        else
        {
            _postInbox.Clear();
            _callInbox.Clear();
            _continuations.Clear();
            _manualPumps.Clear();
        }

        DisposeServices();
    }

    private void ReportException(
        Exception exception,
        int serviceId,
        LayerExceptionPhase phase,
        LayerQueueKind queueKind,
        int messageId,
        ScopeTrace trace = default,
        int queueCapacity = 0,
        int queueCount = 0)
    {
        if (OwningRuntime == null)
        {
            return;
        }

        var record = new LayerExceptionRecord(
            exception: exception,
            scopeId: ScopeId,
            serviceId: serviceId,
            phase: phase,
            queueKind: queueKind,
            messageId: messageId,
            trace: trace,
            threadId: Environment.CurrentManagedThreadId,
            tick: 0,
            queueCapacity: queueCapacity,
            queueCount: queueCount);

        OwningRuntime.ReportException(in record);
    }

    private void ApplyExceptionPolicy(LayerExceptionPhase phase, Exception exception)
    {
        LayerExceptionPolicy policy = _exceptionOptions.GetPolicy(phase);

        switch (policy)
        {
            case LayerExceptionPolicy.ReportAndContinue:
                return;

            case LayerExceptionPolicy.StopScope:
                RequestStop();
                return;

            case LayerExceptionPolicy.StopRuntime:
                OwningRuntime?.RequestStop();
                return;

            case LayerExceptionPolicy.FailFast:
                Environment.FailFast("LayerBase fatal exception.", exception);
                return;
        }
    }

    private void RequestStop()
    {
        _stopped = true;
        _workerRunning = false;
    }

    private void DisposeServices()
    {
        for (int i = Services.Length - 1; i >= 0; i--)
        {
            IService service = Services[i];
            if (service is IDisposable disposable)
            {
                disposable.Dispose();
            }

            ScopeServiceOwnerRegistry.Unbind(service);
        }
    }

    private void SleepWorker()
    {
        if (!_workerRunning)
        {
            return;
        }

        if (Descriptor.Clock == ScopeClockMode.FixedRate && Descriptor.TickRateHz > 0)
        {
            Thread.Sleep(Math.Max(1, 1000 / Descriptor.TickRateHz));
            return;
        }

        Thread.Sleep(1);
    }

    private static IBoundedQueue<T> CreateQueue<T>(ScopeInboxKind kind, int capacity)
    {
        return kind == ScopeInboxKind.Locked
            ? new LockedBoundedRingQueue<T>(capacity)
            : new LocalRingQueue<T>(capacity);
    }

    private void BindServices()
    {
        for (int i = 0; i < Services.Length; i++)
        {
            IService service = Services[i];
            ScopeServiceOwnerRegistry.Bind(service, this);

            if (service is IGeneratedScopeServiceBinding generatedBinding)
            {
                generatedBinding.BindScope(this, i);
            }
            else if (service is IServiceScopeBinding binding)
            {
                binding.BindScope(this, i);
            }
        }
    }

    private void DrainPostInbox()
    {
        while (_postInbox.TryDequeue(out ScopePostMessage message))
        {
            if (_postDispatcher == null)
            {
                continue;
            }

            try
            {
                _postDispatcher(this, message);
            }
            catch (Exception ex)
            {
                ReportException(ex, serviceId: -1, LayerExceptionPhase.PostDispatch, LayerQueueKind.PostInbox, message.EventId);
                ApplyExceptionPolicy(LayerExceptionPhase.PostDispatch, ex);
            }
        }
    }

    private void DrainCallInbox()
    {
        while (_callInbox.TryDequeue(out ScopeCallMessage message))
        {
            if (_callDispatcher == null)
            {
                message.Promise.SetException(new InvalidOperationException("Scope call dispatcher is not configured."));
                continue;
            }

            try
            {
                _callDispatcher(this, message);
            }
            catch (Exception ex)
            {
                ReportException(ex, serviceId: -1, LayerExceptionPhase.CallDispatch, LayerQueueKind.CallInbox, message.CallId);
                message.Promise.SetException(ex);
            }
        }
    }

    private void DrainContinuations()
    {
        while (_continuations.TryDequeue(out LayerContinuation continuation))
        {
            try
            {
                continuation.Action();
            }
            catch (Exception ex)
            {
                ReportException(
                    ex,
                    continuation.ServiceId,
                    LayerExceptionPhase.Continuation,
                    LayerQueueKind.ContinuationQueue,
                    continuation.TaskId,
                    trace: continuation.Trace);
                ApplyExceptionPolicy(LayerExceptionPhase.Continuation, ex);
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopeRuntime));
        }
    }

    private sealed class ScopeTimerSink : IExpiredTimerSink<ITimerAction>
    {
        private readonly PostScheduler _postScheduler;

        public ScopeTimerSink(PostScheduler postScheduler)
        {
            _postScheduler = postScheduler;
        }

        public bool TryAcceptExpired(in ITimerAction payload, TimerHandle handle)
        {
            return payload.Execute(_postScheduler);
        }
    }
}
