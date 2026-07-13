using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Core.DataStruct;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.ECS.Runtime;
using LayerBase.ECS.Runtime.Query;
using LayerBase.Event.Delay;
using LayerBase.Scope.Completion;
using LayerBase.Scope.DI;
using LayerBase.Scope.Lifecycle;
using LayerBase.Scope.Queue;
using LayerBase.Scope.Resources;

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
    private readonly IClosableBoundedQueue<ScopePostMessage> _postInbox;
    private readonly IClosableBoundedQueue<ScopeCallMessage> _callInbox;
    private readonly ReliableContinuationInbox _continuations;
    private readonly IClosableBoundedQueue<float> _manualPumps;
    private ScopeContextPlan[] _contextPlans = Array.Empty<ScopeContextPlan>();
    private ScopeServiceProvider? _serviceProvider;
    private ScopeSubscriptionRegistry? _subscriptionRegistry;
    private readonly ScopePostDispatcher? _postDispatcher;
    private readonly ScopeCallDispatcher? _callDispatcher;
    private readonly ScopeTimerSink _timerSink;
    private readonly EventBuildPolicyTable _policyTable;
    private readonly LayerExceptionOptions _exceptionOptions = new();
    private LayerBaseSynchronizationContext? _context;
    private ScopeRouteTable? _routes;
    private Thread? _workerThread;
    private readonly ActorWorld _actorWorld;
    private readonly bool _ownsActorWorld;
    private readonly object _lifecycleGate = new();
    private ScopeRuntimeState _state = ScopeRuntimeState.Created;
    private volatile bool _workerRunning;
    private int _stopCleanupStarted;
    private int _stopCleanupCompleted;
    private readonly ManualResetEventSlim _stopCleanupFinished = new(false);
    private ManualResetEventSlim? _workerStartedSignal;
    private ManualResetEventSlim? _workerLaunchSignal;
    private int _workerLaunchSucceeded;

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
        DelayManager = DelayPublisherManager.Create(options.DelayBufferOptions, _policyTable);
        _actorWorld = sharedActorWorld ?? new ActorWorld();
        _ownsActorWorld = sharedActorWorld == null;
        Actors = new ScopeActorGateway(owningRuntime, _actorWorld, descriptor.ScopeId);
        EcsWorld = World.Create();
        EcsWorld.BindScopeActors(_actorWorld, Actors);
        EcsQueryRegistry = new EcsQueryRegistry(EcsWorld);
        OwningRuntime = owningRuntime;
        InitializeEcsScheduler(options);

        PostInboxKind = ScopeInboxKind.Locked;
        ContinuationInboxKind = ScopeInboxKind.Locked;

        _postInbox = new ClosableLockedRingQueue<ScopePostMessage>(options.PostQueueCapacity);
        _callInbox = new ClosableLockedRingQueue<ScopeCallMessage>(options.CallQueueCapacity);
        _continuations = new ReliableContinuationInbox(options.ContinuationQueueCapacity);
        _manualPumps = new ClosableLockedRingQueue<float>(options.ContinuationQueueCapacity);
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

    public ILayerContext[] Contexts { get; private set; } = Array.Empty<ILayerContext>();

    internal ScopeServiceProvider ServiceProvider =>
        _serviceProvider ?? throw new InvalidOperationException("Scope service provider is not ready.");

    internal ScopeResourceRegistry ResourceRegistry { get; } = new();

    internal ScopeAwaitRegistry AwaitRegistry { get; } = new();

    public EventCenter EventCenter { get; }

    public PostScheduler PostScheduler { get; }

    public TimeScheduler<ITimerAction> Timer { get; }

    internal DelayPublisherManager DelayManager { get; }

    public ScopeActorGateway Actors { get; }

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

    private ScopeRuntimeState State
    {
        get
        {
            lock (_lifecycleGate) return _state;
        }
    }

    internal void RequireAccess(string apiName)
    {
        if (ReferenceEquals(ScopeExecution.Current.Runtime, this))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Scope '{Descriptor.Name}' local API '{apiName}' must be called from its owner scope execution context.");
    }

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

    internal void SetContexts(ILayerContext[] contexts)
    {
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        _contextPlans = new ScopeContextPlan[contexts.Length];
        for (int i = 0; i < contexts.Length; i++)
        {
            _contextPlans[i] = new ScopeContextPlan(
                contextSlot: i,
                contextType: contexts[i].GetType(),
                ownerServiceSlot: -1,
                instance: contexts[i]);
        }
        BindContexts();
        FinalizeScopeBuild();
    }

    internal void FinalizeScopeBuild()
    {
        RebuildServiceProvider();
        RebuildScopeResources();
        RebindSubscriptions();
    }

    internal void UpdateServiceBindings(IReadOnlyList<ScopeServicePlan> servicePlans)
    {
        if (servicePlans == null) return;
        for (int i = 0; i < servicePlans.Count; i++)
        {
            ScopeServicePlan plan = servicePlans[i];
            if (plan.Membership.Start < 0) continue;
            if ((uint)plan.ServiceSlot >= (uint)Services.Length) continue;

            ScopeObjectBinding existing = ScopeObjectBinder.Require(Services[plan.ServiceSlot]);
            ScopeObjectBinder.Attach(
                Services[plan.ServiceSlot],
                new ScopeObjectBinding(
                    runtime: existing.Runtime,
                    scope: this,
                    serviceSlot: plan.ServiceSlot,
                    contextSlot: existing.ContextSlot,
                    membership: plan.Membership,
                    kind: existing.Kind));
        }
    }

    internal void SetContexts(ScopeContextPlan[] contextPlans)
    {
        _contextPlans = contextPlans ?? throw new ArgumentNullException(nameof(contextPlans));
        var contexts = new ILayerContext[_contextPlans.Length];
        for (int i = 0; i < _contextPlans.Length; i++)
        {
            contexts[i] = _contextPlans[i].Instance;
        }

        Contexts = contexts;
        BindContexts();
    }

    public void Start()
    {
        ThrowIfDisposed();
        if (!TryTransition(ScopeRuntimeState.Created, ScopeRuntimeState.Starting))
        {
            return;
        }

        if (Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            _workerRunning = true;
            _workerStartedSignal = new ManualResetEventSlim(false);
            var launchSignal = new ManualResetEventSlim(false);
            var thread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"LayerBase.Scope.{Descriptor.Name}"
            };

            lock (_lifecycleGate)
            {
                _workerThread = thread;
                _workerLaunchSignal = launchSignal;
                _workerLaunchSucceeded = 0;
            }

            try
            {
                thread.Start();
                Interlocked.Exchange(ref _workerLaunchSucceeded, 1);
            }
            finally
            {
                launchSignal.Set();
            }

            return;
        }

        ExecuteInScope(StartScope);
        TryTransition(ScopeRuntimeState.Starting, ScopeRuntimeState.Running);
    }

    public void Pump(float deltaTime)
    {
        ThrowIfDisposed();
        if (State >= ScopeRuntimeState.Stopped || Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            if (State < ScopeRuntimeState.Stopped &&
                Descriptor.Threading == ScopeThreadingMode.Worker &&
                Descriptor.Clock == ScopeClockMode.Manual)
            {
                if (_manualPumps.TryEnqueue(deltaTime) != QueueEnqueueResult.Accepted)
                {
                    throw new InvalidOperationException(
                        $"Scope '{Descriptor.Name}' manual pump queue is full.");
                }
            }

            return;
        }

        ExecuteInScope(static (r, dt) => r.PumpInternal(dt), deltaTime);
    }

    public void Stop()
    {
        bool shouldStop = false;
        Thread? threadToJoin = null;

        lock (_lifecycleGate)
        {
            if (_state >= ScopeRuntimeState.Disposing) return;
            if (_state >= ScopeRuntimeState.StopRequested)
            {
                threadToJoin = _workerThread;
            }
            else if (_state == ScopeRuntimeState.Created ||
                     _state == ScopeRuntimeState.Starting ||
                     _state == ScopeRuntimeState.Running)
            {
                _state = ScopeRuntimeState.StopRequested;
                shouldStop = true;
                threadToJoin = _workerThread;
            }
        }

        if (!shouldStop && threadToJoin != null)
        {
            if (!ReferenceEquals(Thread.CurrentThread, threadToJoin))
            {
                JoinWorkerIfNeeded(threadToJoin);
            }
            return;
        }

        if (!shouldStop) return;

        CloseBusinessIngress();
        _workerRunning = false;

        if (Descriptor.Threading == ScopeThreadingMode.Worker && threadToJoin != null)
        {
            if (ReferenceEquals(Thread.CurrentThread, threadToJoin))
            {
                ExecuteStopInternalOnce();
            }
            else
            {
                JoinWorkerIfNeeded(threadToJoin);
            }
            return;
        }

        ExecuteStopInternalOnce();
    }

    public bool TryPost(ScopePostMessage message)
    {
        ThrowIfDisposed();
        if (State >= ScopeRuntimeState.StopRequested) return false;
        return _postInbox.TryEnqueue(message) == QueueEnqueueResult.Accepted;
    }

    public bool TryCall(ScopeCallMessage message)
    {
        ThrowIfDisposed();
        if (State >= ScopeRuntimeState.StopRequested) return false;
        return _callInbox.TryEnqueue(message) == QueueEnqueueResult.Accepted;
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
            throw new ArgumentNullException(nameof(continuation));
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
        Thread? threadToJoin = null;
        lock (_lifecycleGate)
        {
            if (_state >= ScopeRuntimeState.Disposing) return;
            _state = ScopeRuntimeState.Disposing;
            threadToJoin = _workerThread;
        }

        CloseBusinessIngress();
        _workerRunning = false;

        if (Descriptor.Threading == ScopeThreadingMode.Worker && threadToJoin != null)
        {
            if (!ReferenceEquals(Thread.CurrentThread, threadToJoin))
            {
                JoinWorkerIfNeeded(threadToJoin);
            }
        }

        ExecuteStopInternalOnce();

        lock (_lifecycleGate) { _state = ScopeRuntimeState.Stopped; }

        _subscriptionRegistry?.Dispose();
        _subscriptionRegistry = null;
        AwaitRegistry.Close();
        EcsScheduler?.Dispose();
        _context?.Dispose();
        Timer.Dispose();
        PostScheduler.Dispose();
        DelayManager.Clear();
        EventCenter.Reset();
        EcsWorld.Dispose();
        if (_ownsActorWorld) _actorWorld.Dispose();

        lock (_lifecycleGate) { _state = ScopeRuntimeState.Disposed; }
    }

    private void WorkerLoop()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            long lastTicks = stopwatch.ElapsedTicks;

            ExecuteInScope(StartScope);
            TryTransition(ScopeRuntimeState.Starting, ScopeRuntimeState.Running);
            _workerStartedSignal?.Set();

            if (Descriptor.Clock == ScopeClockMode.Manual)
            {
                RunManualWorkerLoop();
                ExecuteStopInternalOnce();
                return;
            }

            while (_workerRunning)
            {
                float deltaTime = GetWorkerDeltaTime(stopwatch, ref lastTicks);
                ExecuteInScope(static (r, dt) => r.PumpInternal(dt), deltaTime);
                SleepWorker();
            }

            ExecuteStopInternalOnce();
        }
        catch (Exception ex)
        {
            _workerStartedSignal?.Set();
            ReportException(ex, -1, LayerExceptionPhase.WorkerLoop, LayerQueueKind.None, -1);
            _workerRunning = false;
            ExecuteStopInternalOnce();
        }
    }

    private void RunManualWorkerLoop()
    {
        while (_workerRunning)
        {
            if (_manualPumps.TryDequeue(out float deltaTime))
            {
                ExecuteInScope(static (r, dt) => r.PumpInternal(dt), deltaTime);
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
        var token = ScopeExecution.Enter(this);
        using (GetOrCreateContext().EnterScope())
        {
            try
            {
                action();
            }
            finally
            {
                token.Dispose();
            }
        }
    }

    private void ExecuteInScope(Action<ScopeRuntime, float> action, float deltaTime)
    {
        var token = ScopeExecution.Enter(this);
        using (GetOrCreateContext().EnterScope())
        {
            try
            {
                action(this, deltaTime);
            }
            finally
            {
                token.Dispose();
            }
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

    private void StartContexts()
    {
        for (int i = 0; i < Contexts.Length; i++)
        {
            if (Contexts[i] is not IInitializable initializable)
            {
                continue;
            }

            try
            {
                initializable.Initialize();
            }
            catch (Exception ex)
            {
                ReportException(ex, serviceId: -1, LayerExceptionPhase.ServiceStart, LayerQueueKind.None, messageId: -1);
                ApplyExceptionPolicy(LayerExceptionPhase.ServiceStart, ex);
            }
        }
    }

    private void StartScope()
    {
        EcsScheduler?.Start();
        if (_ownsActorWorld)
        {
            _actorWorld.PrepareRuntimeBuild();
        }

        StartServices();
        StartContexts();
        if (_ownsActorWorld)
        {
            _actorWorld.CompleteRuntimeBuild();
        }
    }

    private void PumpInternal(float deltaTime)
    {
        _context?.Update();
        Timer.Tick(deltaTime, _timerSink);
        DelayManager.Tick(deltaTime);
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

        for (int i = 0; i < Contexts.Length; i++)
        {
            if (Contexts[i] is LayerBase.DI.Options.IUpdate update)
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
        if (!_ownsActorWorld)
        {
            return;
        }

        var actorBudget = new RuntimeFrameBudget(
            maxEvents: 0,
            usedEvents: 0,
            deadlineTicks: 0);
        bool pumpFixedUpdate = Descriptor.Clock == ScopeClockMode.FixedRate;

        _actorWorld.Pump(
            deltaTime: deltaTime,
            fixedDeltaTime: pumpFixedUpdate ? deltaTime : 0f,
            pumpFixedUpdate: pumpFixedUpdate,
            budget: ref actorBudget);
    }

    private void StopInternal()
    {
        try { EcsScheduler?.Stop(); } catch { }

        if (Descriptor.StopPolicy == ScopeStopPolicy.Drain)
        {
            try { DrainPostInbox(); } catch { }
            try { DrainCallInbox(); } catch { }
        }
        else
        {
            try { while (_postInbox.TryDequeue(out _)) { } } catch { }
            try { FailPendingCalls(CreateScopeStoppedException("stopped before pending call was dispatched.")); } catch { }
            try { while (_manualPumps.TryDequeue(out _)) { } } catch { }
        }

        try { AwaitRegistry.CancelAll(CreateScopeStoppedException("scope is stopping.")); } catch { }

        try { DrainContinuations(); } catch { }

        try { CloseCompletionIngress(); } catch { }

        try { DrainContinuations(); } catch { }

        try { _subscriptionRegistry?.Dispose(); } catch { }
        _subscriptionRegistry = null;
        try { DelayManager.Clear(); } catch { }
        try { ResourceRegistry.CloseAndUnbind(); } catch { }
        try { DisposeContexts(); } catch { }
        try { DisposeServices(); } catch { }
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
        bool shouldStop = false;
        lock (_lifecycleGate)
        {
            if (_state >= ScopeRuntimeState.StopRequested) return;
            if (_state == ScopeRuntimeState.Created ||
                _state == ScopeRuntimeState.Starting ||
                _state == ScopeRuntimeState.Running)
            {
                _state = ScopeRuntimeState.StopRequested;
                shouldStop = true;
            }
            else return;
        }

        if (!shouldStop) return;

        CloseBusinessIngress();
        _workerRunning = false;

        if (Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            return;
        }

        ExecuteStopInternalOnce();
    }

    private void WaitForWorkerStartup()
    {
        _workerStartedSignal?.Wait();
    }

    private void WaitForWorkerLaunch()
    {
        _workerLaunchSignal?.Wait();
    }

    private void JoinWorkerIfNeeded(Thread workerThread)
    {
        if (Descriptor.Threading != ScopeThreadingMode.Worker)
        {
            return;
        }

        if (ReferenceEquals(Thread.CurrentThread, workerThread))
        {
            return;
        }

        WaitForWorkerLaunch();
        if (Volatile.Read(ref _workerLaunchSucceeded) == 0)
        {
            return;
        }

        workerThread.Join();
        lock (_lifecycleGate)
        {
            if (ReferenceEquals(_workerThread, workerThread))
            {
                _workerThread = null;
            }
        }
    }

    private void CloseIngressQueues()
    {
        CloseBusinessIngress();
        CloseCompletionIngress();
    }

    private void CloseBusinessIngress()
    {
        _postInbox.Close();
        _callInbox.Close();
        _manualPumps.Close();
    }

    private void CloseCompletionIngress()
    {
        _continuations.Close();
    }

    private void ExecuteStopInternalOnce()
    {
        if (Interlocked.Exchange(ref _stopCleanupStarted, 1) != 0)
        {
            WaitForStopCleanup();
            return;
        }

        try
        {
            lock (_lifecycleGate)
            {
                if (_state < ScopeRuntimeState.StopRequested)
                    _state = ScopeRuntimeState.StopRequested;
                if (_state < ScopeRuntimeState.Disposing)
                    _state = ScopeRuntimeState.Stopping;
            }

            ExecuteInScope(StopInternal);

            lock (_lifecycleGate)
            {
                if (_state < ScopeRuntimeState.Disposing)
                    _state = ScopeRuntimeState.Stopped;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _stopCleanupCompleted, 1);
            _stopCleanupFinished.Set();
        }
    }

    private void WaitForStopCleanup()
    {
        if (Volatile.Read(ref _stopCleanupCompleted) != 0)
        {
            return;
        }

        _stopCleanupFinished.Wait();
    }

    private InvalidOperationException CreateScopeStoppedException(string message)
    {
        return new InvalidOperationException($"Scope '{Descriptor.Name}' {message}");
    }

    private void FailPendingCalls(Exception exception)
    {
        while (_callInbox.TryDequeue(out ScopeCallMessage message))
        {
            message.Promise.SetException(exception);
        }
    }

    private void DisposeServices()
    {
        _serviceProvider?.Dispose();
        _serviceProvider = null;

        for (int i = Services.Length - 1; i >= 0; i--)
        {
            IService service = Services[i];
            try
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                ReportException(ex, i, LayerExceptionPhase.ServiceDispose, LayerQueueKind.None, -1);
            }
            finally
            {
                ScopeObjectBinder.Detach(service);
            }
        }
    }

    private void DisposeContexts()
    {
        for (int i = Contexts.Length - 1; i >= 0; i--)
        {
            ILayerContext context = Contexts[i];
            try
            {
                if (context is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                ReportException(ex, -1, LayerExceptionPhase.ServiceDispose, LayerQueueKind.None, -1);
            }
            finally
            {
                ScopeObjectBinder.Detach(context);
            }
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

    private void BindServices()
    {
        for (int i = 0; i < Services.Length; i++)
        {
            IService service = Services[i];
            ScopeObjectBinder.Attach(
                service,
                new ScopeObjectBinding(
                    runtime: OwningRuntime,
                    scope: this,
                    serviceSlot: i,
                    contextSlot: -1,
                    membership: LayerMembership.Empty,
                    kind: ScopeObjectKind.Service));

            if (service is IServiceScopeBinding binding)
            {
                binding.BindScope(this, i);
            }
        }
    }

    private void BindContexts()
    {
        for (int i = 0; i < Contexts.Length; i++)
        {
            ScopeContextPlan contextPlan = i < _contextPlans.Length
                ? _contextPlans[i]
                : new ScopeContextPlan(i, Contexts[i].GetType(), -1, Contexts[i]);

            ScopeObjectBinder.Attach(
                Contexts[i],
                new ScopeObjectBinding(
                    runtime: OwningRuntime,
                    scope: this,
                    serviceSlot: contextPlan.OwnerServiceSlot,
                    contextSlot: contextPlan.ContextSlot,
                    membership: contextPlan.Membership,
                    kind: ScopeObjectKind.Context));
        }
    }

    private void RebuildServiceProvider()
    {
        _serviceProvider?.Dispose();
        _serviceProvider = new ScopeServiceProvider(Services, Contexts);

        for (int i = 0; i < Services.Length; i++)
        {
            if (Services[i] is IGeneratedScopeMount mount)
            {
                mount.Mount(new ScopeMountContext(this, i, -1));
            }
        }

        for (int i = 0; i < Contexts.Length; i++)
        {
            if (Contexts[i] is IGeneratedScopeMount mount)
            {
                ScopeContextPlan contextPlan = i < _contextPlans.Length
                    ? _contextPlans[i]
                    : new ScopeContextPlan(i, Contexts[i].GetType(), -1, Contexts[i]);
                mount.Mount(new ScopeMountContext(this, contextPlan.OwnerServiceSlot, contextPlan.ContextSlot));
            }
        }
    }

    internal T GetServiceAt<T>(int slot) where T : class
    {
        if ((uint)slot >= (uint)Services.Length)
        {
            throw new InvalidOperationException($"Scope service slot out of range: {slot}.");
        }

        return (T)Services[slot];
    }

    internal T GetMountedObject<T>(int serviceSlot, int contextSlot) where T : class
    {
        if (contextSlot >= 0 && serviceSlot >= 0 && (uint)serviceSlot < (uint)Services.Length &&
            Services[serviceSlot] is T ownerService)
        {
            return ownerService;
        }

        if (serviceSlot >= 0)
        {
            for (int i = 0; i < _contextPlans.Length; i++)
            {
                ScopeContextPlan contextPlan = _contextPlans[i];
                if (contextPlan.OwnerServiceSlot == serviceSlot && contextPlan.Instance is T ownedContext)
                {
                    return ownedContext;
                }
            }
        }

        for (int i = 0; i < Services.Length; i++)
        {
            if (Services[i] is T service)
            {
                return service;
            }
        }

        for (int i = 0; i < Contexts.Length; i++)
        {
            if (Contexts[i] is T context)
            {
                return context;
            }
        }

        throw new InvalidOperationException($"Scope mount dependency not registered: {typeof(T)}.");
    }

    private void RebuildScopeResources()
    {
        object[] candidates = Services.Cast<object>().Concat(Contexts).ToArray();
        var generatedPublishers = candidates.OfType<IGeneratedScopeResourcePublisher>().ToArray();
        var generatedConsumers = candidates.OfType<IGeneratedScopeResourceConsumer>().ToArray();
        if (generatedPublishers.Length > 0 || generatedConsumers.Length > 0)
        {
            EnsureGeneratedScopeResourcesRegistered(candidates);
            var contributions = ScopeResourceContributionRegistry.CollectFor(candidates);
            if (contributions.Exports.Length > 0 || contributions.Imports.Length > 0)
            {
                ResourceRegistry.Initialize(
                    generatedPublishers,
                    generatedConsumers,
                    contributions.Exports,
                    contributions.Imports);
            }
        }
    }

    private static void EnsureGeneratedScopeResourcesRegistered(
        IReadOnlyList<object> candidates)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] is not IGeneratedScopeResourcePublisher &&
                candidates[i] is not IGeneratedScopeResourceConsumer)
            {
                continue;
            }

            RuntimeHelpers.RunClassConstructor(candidates[i].GetType().TypeHandle);
        }
    }

    private void RebindSubscriptions()
    {
        _subscriptionRegistry?.Dispose();
        _subscriptionRegistry = new ScopeSubscriptionRegistry(this);

        BindGeneratedSubscriptions(Services);
        BindGeneratedSubscriptions(Contexts);
    }

    private void BindGeneratedSubscriptions(IEnumerable<object> candidates)
    {
        foreach (object candidate in candidates)
        {
            ScopeObjectBinding binding = ScopeObjectBinder.Require(candidate);

            if (candidate is IAutoScopeSubscribe autoScopeSubscribe)
            {
                autoScopeSubscribe.Bind(new ScopeSubscriptionContext(
                    this,
                    binding.Membership,
                    binding.ServiceSlot));
            }

            _ = binding;
        }
    }

    internal void RegisterSubscribeFlow<T>(
        LayerMembership membership,
        int serviceSlot,
        EventHandleDelegate<T> handler)
        where T : struct
    {
        _subscriptionRegistry!.SubscribeFlow(membership, serviceSlot, handler);
    }

    internal void RegisterSubscribeAsync<T>(
        LayerMembership membership,
        int serviceSlot,
        EventHandleDelegateAsync<T> handler)
        where T : struct
    {
        _subscriptionRegistry!.SubscribeAsync(membership, serviceSlot, handler);
    }

    internal void RegisterSubscribeNotify<T>(
        LayerMembership membership,
        int serviceSlot,
        EventNotifyDelegate<T> handler)
        where T : struct
    {
        _subscriptionRegistry!.SubscribeNotify(membership, serviceSlot, handler);
    }

    internal void RegisterSubscribe<T>(
        LayerMembership membership,
        int serviceSlot,
        EventNotifyDelegate<T> handler)
        where T : struct
    {
        _subscriptionRegistry!.Subscribe(membership, serviceSlot, handler);
    }

    internal void RegisterSubscribeParallel<T>(
        LayerMembership membership,
        int serviceSlot,
        EventNotifyDelegate<T> handler)
        where T : struct
    {
        Action<int, int, int, Exception>? reportError = null;
        if (OwningRuntime != null)
        {
            reportError = OwningRuntime.ReportLayerEventError;
        }

        _subscriptionRegistry!.SubscribeParallel(
            membership,
            serviceSlot,
            handler,
            reportError);
    }

    internal IDelayPublisher<T> GetOrCreateDelayPublisher<T>()
        where T : struct
    {
        return _subscriptionRegistry!.GetOrCreateDelayPublisher<T>();
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
            InvokeContinuation(continuation);
        }
    }

    private void InvokeContinuation(in LayerContinuation continuation)
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

    private void ThrowIfDisposed()
    {
        if (State >= ScopeRuntimeState.Disposing)
        {
            throw new ObjectDisposedException(nameof(ScopeRuntime));
        }
    }

    private bool TryTransition(ScopeRuntimeState from, ScopeRuntimeState to)
    {
        lock (_lifecycleGate)
        {
            if (_state != from) return false;
            _state = to;
            return true;
        }
    }

    private void ForceTransition(ScopeRuntimeState to)
    {
        lock (_lifecycleGate) _state = to;
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
