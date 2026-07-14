using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.ECS.Runtime;
using LayerBase.Event.Delay;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
using LayerBase.Snap;
using LayerBase.Tooling;
using LayerBase.Worker;

namespace LayerBase;

/// <summary>
/// LayerBase 的运行时实例。每个 LayerRuntime 拥有独立的 Layer 链、事件中心、
/// 调度器、定时器、Actor 世界、ECS 世界和服务容器。
/// 通过 LayerRuntime.LayersBuilder（由 LayerHub.CreateLayers 返回）进行构建。
/// </summary>
public sealed partial class LayerRuntime : IDisposable
{
    #region External Dependencies
    // 核心子系统，在构造时创建，贯穿 Runtime 生命周期。
    private readonly RuntimeKernel _kernel;
    internal WorldServiceRoot Services { get; }
    internal EventCenter EventCenter { get; set; }
    public ActorWorld Actors => _kernel.Actors;
    public WorkerRuntime Worker { get; }
    public ScopeRuntimeHost? ScopeHost
    {
        get => _kernel.ScopeHost;
        private set => _kernel.ScopeHost = value;
    }
    public LayerExceptionHub ExceptionHub => _kernel.Exceptions;
    public LayerHubExceptionCallbacks ExceptionCallbacks { get; }
    private readonly LayerRuntimeExceptionSink _exceptionSink;
    private readonly PostIngressQueue _postIngress = new();
    private Action<Exception>? _completionExceptionHandler;
    #endregion

    #region Runtime State - Configuration
    // 运行时配置参数。
    private FixedUpdateOptions _fixedUpdateOptions = FixedUpdateOptions.Disabled;
    #endregion

    #region Runtime State - Subsystems
    // 各子系统实例，在 Build 过程中创建。
    private LayerChain? _chain;
    internal LayerBaseSynchronizationContext? _context;
    private PostScheduler? _scheduler;
    private TimeScheduler<ITimerAction>? _timer;
    private RuntimeTimerSink? _timerSink;
    private ServiceProvider? _worldProvider;
    private FullSnapRuntime? _fullSnap;
    internal DelayPublisherManager? DelayManager { get; private set; }
    internal List<ILayerBaseModule>? _installedModules;
    internal bool _moduleMode;
    private ScopeRuntimeOptions? _mainScopeRuntimeOptions;
    private bool _runtimeResourcesAliasedToMainScope;
    #endregion

    #region Runtime State - Layer Bindings
    // Layer 注册信息：索引计数器、类型绑定表、版本号。
    private int _layerIndexCounter;
    private int _layerTypeBindingsVersion;
    private readonly Dictionary<Type, LayerTypeBinding> _layerTypeBindings = new();
    #endregion

    #region Runtime State - Identification
    // Runtime 的唯一标识符和释放标记。
    private readonly int _id;
    private readonly int _ownerThreadId;
    private int _drainingExceptions;
    private int _buildState;
    private int _disposeStarted;
    private int _disposeCompleted;
    private readonly ManualResetEventSlim _disposeFinished = new(false);
    private Exception? _disposeException;
    private bool _disposed;
    #endregion

    #region Runtime State - Timing
    private float _fixedUpdateAccumulator;
    #endregion

    #region Properties
    public int Id => _id;

    internal RuntimeBuildState BuildState => (RuntimeBuildState)Volatile.Read(ref _buildState);

    internal LayerBase.DI.IServiceProvider ServiceProvider
    {
        get
        {
            if (_worldProvider != null)
            {
                return _worldProvider;
            }

            if (ScopeHost?.MainScope.ServiceProvider is { } scopeProvider)
            {
                return scopeProvider;
            }

            throw new InvalidOperationException("Runtime not built.");
        }
    }

    internal T GetService<T>() where T : class => ServiceProvider.Get<T>();

    internal PostScheduler Scheduler => _scheduler ?? throw new InvalidOperationException("Runtime not built.");

    internal TimeScheduler<ITimerAction> Timer => _timer ?? throw new InvalidOperationException("Runtime not built.");

    public IFullSnapRuntime FullSnap => _fullSnap ?? throw new InvalidOperationException("Runtime not built.");

    public LayerToolRegistry Tools => _kernel.Tools;

    public ScopeRuntime MainScope => ScopeHost?.MainScope ?? throw new InvalidOperationException("Runtime not built.");

    public bool IsDebugMode { get; internal set; }
    #endregion

    #region Events
    public event Action<LayerEventInfo>? OnLayerEventInfo;
    #endregion

    #region Constructors & Initialization
    internal LayerRuntime(int id)
    {
        _id = id;
        _kernel = new RuntimeKernel(this);
        EventCenter = new EventCenter();
        Worker = new WorkerRuntime(Math.Max(1, Environment.ProcessorCount - 1));
        Services = new WorldServiceRoot(this);
        InitializeEcsWorld();
        ExceptionCallbacks = new LayerHubExceptionCallbacks();
        _exceptionSink = new LayerRuntimeExceptionSink(this, ExceptionCallbacks);
        _completionExceptionHandler = ex => ReportLayerEventError(-1, "System", "Completion", ex);
        _ownerThreadId = Environment.CurrentManagedThreadId;
        LayerHub.Internal_Register(this);
    }

    private EventBuildPolicyTable? _policyTable;

    public EventBuildPolicyTable PolicyTable =>
        ScopeHost?.MainScope.PolicyTable ??
        _policyTable ??
        throw new InvalidOperationException("Runtime not built.");

    internal void InitializeScheduler(PostSchedulerOptions options)
    {
        _postIngress.SetCapacity(options.MaxIngressQueueCapacity);
        BuildEventPolicies(options);
    }

    public void RebuildEventPolicies()
    {
        if (_scheduler == null) throw new InvalidOperationException("Runtime not built.");
        BuildEventPolicies(_scheduler.Options);
    }

    private void BuildEventPolicies(PostSchedulerOptions options)
    {
        _policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        var metaData = LayerBase.Event.EventMetaData.EventMetaDataHandler.GetAllMetaData().ToList();
        var plans = new List<PostTypePlan>();

        foreach (var (type, meta) in metaData)
        {
            var eventId = meta.EventId;
            _ = meta.GetIdentity();

            var postPolicy = meta.GetPostPolicy();
            _policyTable.SetMetaData(eventId, meta);
            if (postPolicy != null)
                _policyTable.SetPostPolicy(eventId, postPolicy.Value);

            var timerPolicy = meta.GetTimerPolicy();
            if (timerPolicy != null)
                _policyTable.SetTimerPolicy(eventId, timerPolicy.Value);

            var bufferPolicy = meta.GetBufferPolicy();
            if (bufferPolicy != null)
                _policyTable.SetBufferPolicy(eventId, bufferPolicy.Value);

            var actorMailOptions = meta.GetActorMailOptions();
            if (actorMailOptions != null)
                _policyTable.SetActorMailOptions(eventId, actorMailOptions.Value);

            var effectivePolicy =
                postPolicy ?? new EventPostPolicy(PostDeliveryMode.Normal, options.DefaultBackpressure, 0);
            plans.Add(new PostTypePlan(eventId, effectivePolicy.Mode, effectivePolicy.Backpressure,
                effectivePolicy.MaxPending, options.DefaultBackpressure, effectivePolicy.MergeFailure));
        }

        if (_scheduler == null)
        {
            _scheduler = new PostScheduler(_id, EventCenter, options, _policyTable);
            EventCenter.PostScheduler = _scheduler;
        }
        else
        {
            _scheduler.UpdatePolicyTable(_policyTable);
        }

        _scheduler.BuildPlans(plans.ToArray());
    }

    internal void InitializeTimer(TimeSchedulerOptions options)
    {
        _timer = new TimeScheduler<ITimerAction>(options);
        _timerSink = new RuntimeTimerSink(_scheduler!, _policyTable!);
    }

    internal void InitializeDelay(DelayBufferOptions options)
    {
        DelayManager = DelayPublisherManager.Create(options, _policyTable!);
    }

    internal void BuildServiceProvider()
    {
        _worldProvider = new ServiceProvider(Services);
    }

    internal void BuildFullSnapCache()
    {
        _fullSnap = new FullSnapRuntime(this);
        if (_chain == null) return;

        var visited = new HashSet<object>(LayerBase.Snap.ReferenceEqualityComparer.Instance);
        foreach (var layer in _chain.GetNodes().OfType<Layer>())
        {
            if (layer is IGeneratedFullSnapNode layerNode && visited.Add(layerNode))
                _fullSnap.Register(layerNode);

            foreach (IGeneratedFullSnapNode node in layer.GetFullSnapNodes())
            {
                if (visited.Add(node))
                    _fullSnap.Register(node);
            }
        }
    }

    internal int GetNextLayerIndex()
    {
        return Interlocked.Increment(ref _layerIndexCounter) - 1;
    }

    internal IReadOnlyDictionary<RuntimeTypeHandle, int> GetLayerTypeIndexMap()
    {
        var map = new Dictionary<RuntimeTypeHandle, int>();
        if (_chain == null)
        {
            return map;
        }

        foreach (Layer layer in _chain.GetNodes())
        {
            map[layer.GetType().TypeHandle] = layer.RouteIndex;
        }

        return map;
    }

    internal void MarkDelayDirty()
    {
        _chain?.MarkDelayDirty();
    }

    private void InitializeScopeHost()
    {
        if (_chain == null)
        {
            return;
        }

        // 如果通过 LayersBuilder.Install() 安装了模块，优先使用模块路径构建 scope host
        if (_installedModules != null && _installedModules.Count > 0)
        {
            if (TryBuildFromInstalledModules())
            {
                return;
            }
        }

        ScopeHostFactoryDelegate? generatedScopeHostFactory = CreateGeneratedScopeHostFactory();

        var scopedServices = new List<LayerBase.DI.IService>();
        var seen = new HashSet<object>(LayerBase.Snap.ReferenceEqualityComparer.Instance);
        foreach (Layer layer in _chain.GetNodes())
        {
            foreach (LayerBase.DI.IService service in layer.GetResolvedServices())
            {
                if (!ScopeRuntimePlanner.IsScopedServiceType(service.GetType()) ||
                    !seen.Add(service))
                {
                    continue;
                }

                scopedServices.Add(service);
            }
        }

        ScopeHost = generatedScopeHostFactory?.Invoke(scopedServices, _mainScopeRuntimeOptions, Actors, this)
                    ?? ScopeRuntimeHost.Create(
                        ScopeRuntimePlanner.Build(scopedServices, resolver: null, _mainScopeRuntimeOptions),
                        sharedActorWorld: Actors,
                        owningRuntime: this);
        BindRuntimeResourceProxiesToMainScope();
    }

    private bool TryBuildFromInstalledModules()
    {
        if (_installedModules == null || _installedModules.Count == 0)
        {
            return false;
        }

        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(_installedModules);
        if (catalog.ScopeDefinitions.Count == 0)
        {
            return false;
        }

        ScopeCompositionPlan plan = ScopeCompositionBuilder.Build(this, catalog);
        ApplyMainScopeRuntimeOptions(plan);
        ScopeHost = ScopeRuntimeHost.Create(
            this,
            plan,
            CreateModuleCallDispatchers(catalog),
            CreateModuleEventDispatchers(catalog));
        BindRuntimeResourceProxiesToMainScope();

        ApplyLayerServiceHandles(catalog);

        if (ScopeHost != null)
        {
            _moduleMode = true;
        }

        return ScopeHost != null;
    }

    private static ModuleCallDispatchHandler[] CreateModuleCallDispatchers(ModuleRuntimeCatalog catalog)
    {
        var dispatchers = new ModuleCallDispatchHandler[catalog.Modules.Count];
        var modulesRequiringCallDispatcher = new HashSet<int>(
            catalog.CallRoutes
                   .Where(static route => route.IsValid)
                   .Select(static route => (int)route.ModuleSlot));

        for (int i = 0; i < catalog.Modules.Count; i++)
        {
            if (catalog.Modules[i] is IModuleScopeDispatchProvider provider &&
                provider.ModuleCallDispatcher != null)
            {
                dispatchers[i] = provider.ModuleCallDispatcher;
                continue;
            }

            if (modulesRequiringCallDispatcher.Contains(i))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.MissingModuleDispatcher,
                    $"Installed module '{catalog.Modules[i].GetType().FullName}' declares a call handler but does not provide a module call dispatcher.");
            }

            dispatchers[i] = MissingModuleCallDispatcher;
        }

        return dispatchers;
    }

    private static ModuleEventDispatchHandler[] CreateModuleEventDispatchers(ModuleRuntimeCatalog catalog)
    {
        var dispatchers = new ModuleEventDispatchHandler[catalog.Modules.Count];
        var modulesRequiringEventDispatcher = new HashSet<int>(
            catalog.EventHandlerRoutes.Select(static route => (int)route.ModuleSlot));

        for (int i = 0; i < catalog.Modules.Count; i++)
        {
            if (catalog.Modules[i] is IModuleScopeDispatchProvider provider &&
                provider.ModuleEventDispatcher != null)
            {
                dispatchers[i] = provider.ModuleEventDispatcher;
                continue;
            }

            if (modulesRequiringEventDispatcher.Contains(i))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.MissingModuleDispatcher,
                    $"Installed module '{catalog.Modules[i].GetType().FullName}' declares an event handler but does not provide a module event dispatcher.");
            }

            dispatchers[i] = MissingModuleEventDispatcher;
        }

        return dispatchers;
    }

    private static void MissingModuleCallDispatcher(
        ScopeRuntime scope,
        ushort localHandlerId,
        int serviceSlot,
        ScopeCallMessage message)
    {
        message.Promise.SetException(new InvalidOperationException(
            $"Installed module does not provide a scope call dispatcher for local handler id {localHandlerId}."));
    }

    private static void MissingModuleEventDispatcher(
        ScopeRuntime scope,
        ushort localHandlerId,
        int serviceSlot,
        ScopePostMessage message)
    {
        throw new InvalidOperationException(
            $"Installed module does not provide a scope event dispatcher for local handler id {localHandlerId}.");
    }

    private void ApplyLayerServiceHandles(ModuleRuntimeCatalog catalog)
    {
        if (_chain == null)
        {
            return;
        }

        var handlesByLayerType = new Dictionary<Type, List<LayerServiceHandle>>();
        for (int i = 0; i < catalog.Services.Count; i++)
        {
            ServiceContribution service = catalog.Services[i];
            if (!catalog.ScopeIds.TryGetValue(service.OwnerScopeType, out int scopeId) ||
                !catalog.ServiceSlots.TryGetValue(service.ServiceType, out int serviceSlot))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidServiceContribution,
                    $"Service '{Type.GetTypeFromHandle(service.ServiceType)?.FullName ?? "<unknown>"}' is missing scope id or service slot in the runtime catalog.");
            }

            LayerServiceHandle handle = new(service.ServiceType, scopeId, serviceSlot);
            for (int j = 0; j < service.OwnerLayerTypes.Length; j++)
            {
                Type? layerType = Type.GetTypeFromHandle(service.OwnerLayerTypes[j]);
                if (layerType == null)
                {
                    throw new ModuleBuildException(
                        ModuleBuildErrorCodes.MissingLayerContract,
                        $"Service '{Type.GetTypeFromHandle(service.ServiceType)?.FullName ?? "<unknown>"}' references an unknown owner layer type.");
                }

                if (!handlesByLayerType.TryGetValue(layerType, out List<LayerServiceHandle>? handles))
                {
                    handles = new List<LayerServiceHandle>();
                    handlesByLayerType[layerType] = handles;
                }

                handles.Add(handle);
            }
        }

        foreach (Layer layer in _chain.GetNodes())
        {
            if (handlesByLayerType.TryGetValue(layer.GetType(), out List<LayerServiceHandle>? handles))
            {
                layer.SetServiceHandles(handles.ToArray());
            }
            else
            {
                layer.SetServiceHandles(Array.Empty<LayerServiceHandle>());
            }
        }
    }

    private ScopeHostFactoryDelegate? CreateGeneratedScopeHostFactory()
    {
        foreach (Layer layer in _chain.GetNodes())
        {
            if (layer is IScopeHostFactoryRegistrar registrar)
            {
                return registrar.CreateScopeHostFactory();
            }
        }

        return null;
    }

    private void EnsurePostSchedulersKnowAllocatedEventTypes()
    {
        if (ScopeHost == null)
        {
            return;
        }

        IReadOnlyList<ScopeRuntime> scopes = ScopeHost.Scopes;
        for (int i = 0; i < scopes.Count; i++)
        {
            scopes[i].RebuildEventPolicies();
        }
    }

    private void ApplyMainScopeRuntimeOptions(ScopeCompositionPlan plan)
    {
        if (_mainScopeRuntimeOptions == null || plan.Scopes.Length == 0)
        {
            return;
        }

        ScopePlan main = plan.Scopes[0];
        if (main.Descriptor.ScopeId != 0)
        {
            return;
        }

        plan.Scopes[0] = new ScopePlan(
            main.Descriptor,
            main.ScopeType,
            main.Services,
            main.Contexts,
            _mainScopeRuntimeOptions,
            main.ResourcePlan);
    }

    private ScopeRuntimeOptions CreateMainScopeRuntimeOptions(
        PostSchedulerOptions postOptions,
        TimeSchedulerOptions timerOptions,
        DelayBufferOptions delayOptions)
    {
        _postIngress.SetCapacity(postOptions.MaxIngressQueueCapacity);
        ScopeRuntimeOptions baseOptions = ScopeOptionResolver.ResolveMain().RuntimeOptions;
        return new ScopeRuntimeOptions(
            baseOptions.PostQueueCapacity,
            baseOptions.CallQueueCapacity,
            baseOptions.ContinuationQueueCapacity,
            baseOptions.CompletionQueueCapacity,
            postOptions,
            timerOptions,
            delayOptions,
            EcsOptions);
    }

    private void BindRuntimeResourceProxiesToMainScope()
    {
        if (ScopeHost == null)
        {
            return;
        }

        ScopeRuntime mainScope = ScopeHost.MainScope;
        if (!_runtimeResourcesAliasedToMainScope)
        {
            _scheduler?.Dispose();
            _timer?.Dispose();
            DelayManager?.Clear();
            EventCenter.Reset();
            if (!ReferenceEquals(EcsScheduler, mainScope.EcsScheduler))
            {
                EcsScheduler.Dispose();
            }

            if (!ReferenceEquals(EcsWorld, mainScope.EcsWorld))
            {
                EcsWorld.Dispose();
            }
        }

        EventCenter = mainScope.EventCenter;
        _scheduler = mainScope.PostScheduler;
        _timer = mainScope.Timer;
        DelayManager = mainScope.DelayManager;
        AdoptMainScopeEcsResources(mainScope);
        _runtimeResourcesAliasedToMainScope = true;
    }
    #endregion

    #region Lifecycle - Pump
    public void Pump(float deltaTime)
    {
        if (_disposed) return;

        using var runtimeScope = LayerRuntimeExecution.Enter(this);

        PumpCore(deltaTime);
    }

    private void PumpCore(float deltaTime)
    {
        if (_scheduler != null)
        {
            Worker.DrainEventsTo(_scheduler, _scheduler.Options.MaxIngressPostsPerPump);
        }

        TryDrainExceptions();

        if (_scheduler != null)
        {
            var ingressResult = _postIngress.DrainTo(
                _scheduler,
                _scheduler.Options.MaxIngressPostsPerPump);

            if (IsDebugMode && ingressResult.Failed > 0)
            {
                ReportWarning(-1, "PostIngressQueue", "DrainTo",
                    $"PostFromAnyThread failed: {ingressResult.Failed}/{ingressResult.Drained}");
            }
        }

        ScopeHost?.Pump(deltaTime);
        ScopeRuntime? mainScope = ScopeHost?.MainScope;
        PostPumpStats postStats = mainScope?.LastPostPumpStats ?? new PostPumpStats(0, 0, 0, 0);

        if (mainScope != null)
        {
            mainScope.ExecuteInOwnerScope(() => PumpLayerCallbacks(deltaTime));
        }
        else
        {
            PumpLayerCallbacks(deltaTime);
        }

        if (_scheduler != null)
        {
            RuntimeFrameBudget actorBudget = CreateActorBudget(_scheduler.Options, postStats);
            DrainActorCommands();
            Actors.Pump(
                deltaTime: deltaTime,
                fixedDeltaTime: _fixedUpdateOptions.Enabled ? _fixedUpdateOptions.FixedDeltaTime : 0f,
                pumpFixedUpdate: _fixedUpdateOptions.Enabled,
                budget: ref actorBudget);
        }

        TryDrainExceptions();
    }

    private void PumpLayerCallbacks(float deltaTime)
    {
        if (_fixedUpdateOptions.Enabled)
        {
            _fixedUpdateAccumulator += deltaTime;
            int steps = 0;
            while (_fixedUpdateAccumulator >= _fixedUpdateOptions.FixedDeltaTime &&
                   steps < _fixedUpdateOptions.MaxStepsPerPump)
            {
                _chain?.PumpFixed(_fixedUpdateOptions.FixedDeltaTime);
                _fixedUpdateAccumulator -= _fixedUpdateOptions.FixedDeltaTime;
                steps++;
            }
        }

        _chain?.Pump(deltaTime);
    }
    #endregion

    #region Public API - Event Send / Post
    private static RuntimeFrameBudget CreateActorBudget(PostSchedulerOptions options, PostPumpStats postStats)
    {
        long deadlineTicks = 0;

        if (options.MaxMillisecondsPerPump > 0)
        {
            long budgetTicks = (long)(Stopwatch.Frequency * options.MaxMillisecondsPerPump / 1000.0);
            deadlineTicks = Stopwatch.GetTimestamp() + budgetTicks;
        }

        return new RuntimeFrameBudget(
            maxEvents: options.MaxEventsPerPump,
            usedEvents: postStats.ProcessedCount,
            deadlineTicks: deadlineTicks);
    }
    public void ReportInfo(LayerEventInfo info)
    {
        OnLayerEventInfo?.Invoke(info);
        LayerHub.Internal_NotifyEvent(info);
    }

    internal void ReportException(in LayerExceptionRecord record)
    {
        ExceptionHub.Report(in record);
        if (IsOwnerThread)
        {
            TryDrainExceptions();
        }
    }

    private void TryDrainExceptions()
    {
        if (!IsOwnerThread)
        {
            return;
        }

        if (Interlocked.Exchange(ref _drainingExceptions, 1) != 0)
        {
            return;
        }

        try
        {
            ExceptionHub.DrainAndDispatch(_exceptionSink);
        }
        finally
        {
            Volatile.Write(ref _drainingExceptions, 0);
        }
    }

    private bool IsOwnerThread => Environment.CurrentManagedThreadId == _ownerThreadId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportLayerEventError(int layerIndex, string source, string eventName, Exception ex)
    {
        var record = new LayerExceptionRecord(
            exception: ex,
            scopeId: ScopeExecution.Current.ScopeId,
            serviceId: -1,
            phase: LayerExceptionPhase.EventDispatch,
            queueKind: LayerQueueKind.None,
            messageId: -1,
            trace: ScopeTrace.Empty,
            threadId: Environment.CurrentManagedThreadId,
            tick: 0,
            queueCapacity: 0,
            queueCount: 0,
            layerIndex: layerIndex,
            source: source,
            eventName: eventName);

        ReportException(in record);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportLayerEventError(int layerIndex, int sourceId, int eventNameId, Exception ex)
    {
        var source = EventDiagnosticSymbols.Resolve(sourceId);
        var eventName = EventDiagnosticSymbols.Resolve(eventNameId);
        ReportLayerEventError(layerIndex, source, eventName, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportWarning(int layerIndex, string source, string eventName, string message)
    {
        ReportInfo(new LayerEventInfo(layerIndex, source, eventName, message, LayerEventInfoType.Warning));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Send<T>(in T value) where T : struct
    {
        EventCenter.Send(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Post<T>(in T value) where T : struct
    {
        _ = TryPost(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PostResult TryPost<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        return policy.HasValue
            ? Scheduler.TryPost(value, policy.Value)
            : Scheduler.TryPost(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkDirty<T>() where T : struct
    {
        Scheduler.MarkDirty<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostLatest<T>(in T value) where T : struct
    {
        Scheduler.TryPostLatest(value);
    }

    internal void PostFromAnyThread<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        if (_disposed) return;
        _postIngress.Enqueue(value, policy);
    }

    internal bool TryPostFromAnyThread<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        if (_disposed) return false;
        return _postIngress.Enqueue(value, policy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostCoalesced<T>(in T value) where T : struct
    {
        Scheduler.TryPostCoalesced(value);
    }

    internal TimerHandle SchedulePost<T>(in T value, float delaySeconds) where T : struct
    {
        var eventId = EventTypeId<T>.Id;
        var timerPolicy = PolicyTable.GetTimerPolicy(eventId);

        return Timer.Schedule(
            new PostEventAction<T>(value, timerPolicy?.ExpiredPostPolicy),
            delaySeconds, repeatCount: 0, intervalSeconds: 0,
            repeatMode: timerPolicy?.RepeatMode,
            catchUpPolicy: timerPolicy?.CatchUpPolicy);
    }
    #endregion

    #region Public API - Cross-Layer Call
    public LayerCallTarget<TLayer> For<TLayer>() where TLayer : Layer
    {
        TryResolveLayerTarget<TLayer>(out var layer, out var error);
        return new LayerCallTarget<TLayer>(this, layer, error);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> CallAsync<TLayer, TRequest, TResponse>(TRequest request,
                                                                    CancellationToken cancellationToken = default)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        var version = GetLayerTypeBindingsVersion();
        if (LayerHub.GetCallCacheVersion<TLayer, TRequest, TResponse>(_id) != version)
            return CallAsyncSlow<TLayer, TRequest, TResponse>(version, request, cancellationToken);

        var invoker = LayerHub.GetCallInvoker<TLayer, TRequest, TResponse>(_id);
        if (invoker != null) return invoker(request, cancellationToken);
        return LBTask<TResponse>.FromException(LayerHub.GetCallError<TLayer, TRequest, TResponse>(_id)!);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private LBTask<TResponse> CallAsyncSlow<TLayer, TRequest, TResponse>(int version, TRequest request,
                                                                         CancellationToken cancellationToken)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        UpdateLayerCallCache<TLayer, TRequest, TResponse>(version);
        var invoker = LayerHub.GetCallInvoker<TLayer, TRequest, TResponse>(_id);
        if (invoker != null) return invoker(request, cancellationToken);
        return LBTask<TResponse>.FromException(LayerHub.GetCallError<TLayer, TRequest, TResponse>(_id)!);
    }

    private void UpdateLayerCallCache<TLayer, TRequest, TResponse>(int version)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        if (TryResolveLayerTarget<TLayer>(out var layer, out var error))
        {
            try
            {
                var invoker = layer!.GetCallInvoker<TRequest, TResponse>();
                LayerHub.UpdateLayerCallCache<TLayer, TRequest, TResponse>(_id, version, invoker, null);
            }
            catch (Exception ex)
            {
                LayerHub.UpdateLayerCallCache<TLayer, TRequest, TResponse>(_id, version, null, ex);
            }
        }
        else
        {
            LayerHub.UpdateLayerCallCache<TLayer, TRequest, TResponse>(_id, version, null, error);
        }
    }
    #endregion

    #region Lifecycle - Dispose
    public void RequestStop()
    {
        ScopeHost?.RequestStop();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            WaitForDisposeCompletion();
            return;
        }

        _disposed = true;
        MarkBuildState(RuntimeBuildState.Disposed);
        List<Exception>? exceptions = null;

        void Capture(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                (exceptions ??= new List<Exception>()).Add(ex);
            }
        }

        try
        {
            try
            {
                Capture(_postIngress.Clear);
                Capture(() => ScopeHost?.Dispose());
                ScopeHost = null;
                if (!_runtimeResourcesAliasedToMainScope)
                {
                    Capture(EcsScheduler.Dispose);
                }

                Capture(Worker.Dispose);
                Capture(CloseActorInboxes);
                Capture(() => _chain?.DisposeLayers());
                _chain = null;
                Capture(Actors.RuntimeStop);
                Capture(Actors.Dispose);
                if (!_runtimeResourcesAliasedToMainScope)
                {
                    Capture(EcsWorld.Dispose);
                }

                Capture(Services.Dispose);
                if (!_runtimeResourcesAliasedToMainScope)
                {
                    Capture(() => _scheduler?.Dispose());
                    Capture(() => _timer?.Dispose());
                    Capture(() => DelayManager?.Clear());
                    Capture(EventCenter.Reset);
                    Capture(() => _context?.Dispose());
                }

                _scheduler = null;
                _timer = null;
                DelayManager = null;
                _context = null;
                Capture(TryDrainExceptions);
            }
            finally
            {
                LayerHub.ClearRuntimeCaches(_id);
                LayerHub.Internal_Unregister(this);
            }

            if (exceptions is { Count: > 0 })
            {
                throw new AggregateException("One or more runtime components failed during disposal.", exceptions);
            }
        }
        catch (Exception ex)
        {
            _disposeException = ex;
            throw;
        }
        finally
        {
            Volatile.Write(ref _disposeCompleted, 1);
            _disposeFinished.Set();
        }
    }

    private void WaitForDisposeCompletion()
    {
        if (Volatile.Read(ref _disposeCompleted) == 0)
        {
            _disposeFinished.Wait();
        }

        if (_disposeException != null)
        {
            throw _disposeException;
        }
    }

    internal void MarkBuildState(RuntimeBuildState state)
    {
        Volatile.Write(ref _buildState, (int)state);
    }

    internal void AbortFrameworkBuild()
    {
        if (_disposed) return;

        _disposed = true;

        void Capture(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                // Preserve the original build exception; abort is best-effort cleanup.
            }
        }

        try
        {
            Capture(_postIngress.Clear);
            Capture(() => ScopeHost?.Dispose());
            ScopeHost = null;
            if (!_runtimeResourcesAliasedToMainScope)
            {
                Capture(EcsScheduler.Dispose);
            }

            Capture(Worker.Dispose);
            Capture(CloseActorInboxes);
            Capture(() => _chain?.DisposeLayerInstancesOnly());
            _chain = null;
            Capture(Actors.RuntimeStop);
            Capture(Actors.Dispose);
            if (!_runtimeResourcesAliasedToMainScope)
            {
                Capture(EcsWorld.Dispose);
            }

            Capture(Services.Dispose);
            if (!_runtimeResourcesAliasedToMainScope)
            {
                Capture(() => _scheduler?.Dispose());
                Capture(() => _timer?.Dispose());
                Capture(() => DelayManager?.Clear());
                Capture(EventCenter.Reset);
                Capture(() => _context?.Dispose());
            }

            _scheduler = null;
            _timer = null;
            DelayManager = null;
            _context = null;
            Capture(TryDrainExceptions);
        }
        finally
        {
            LayerHub.ClearRuntimeCaches(_id);
            LayerHub.Internal_Unregister(this);
        }
    }
    #endregion

    #region Internal - Layer Registration
    internal void RegisterLayerInstance(Layer layer)
    {
        var layerType = layer.GetType();
        lock (_layerTypeBindings)
        {
            if (_layerTypeBindings.TryGetValue(layerType, out var existing))
                _layerTypeBindings[layerType] = existing.WithAdditional(layer);
            else
                _layerTypeBindings[layerType] = LayerTypeBinding.Create(layer);
            InvalidateLayerTargetCaches();
        }
    }

    internal bool TryResolveLayerTarget<TLayer>(out TLayer? layer, out Exception? error)
        where TLayer : Layer
    {
        var version = Volatile.Read(ref _layerTypeBindingsVersion);
        if (LayerHub.TryGetCachedTarget(_id, version, out layer, out error)) return error == null;

        lock (_layerTypeBindings)
        {
            version = _layerTypeBindingsVersion;
            if (LayerHub.TryGetCachedTarget(_id, version, out layer, out error)) return error == null;

            LayerHub.LayerTargetState state;
            if (!_layerTypeBindings.TryGetValue(typeof(TLayer), out var binding))
            {
                layer = null;
                error = new LayerCallTargetNotFoundException(typeof(TLayer));
                state = LayerHub.LayerTargetState.Missing;
            }
            else if (binding.IsAmbiguous)
            {
                layer = null;
                error = new LayerCallTargetAmbiguousException(typeof(TLayer));
                state = LayerHub.LayerTargetState.Ambiguous;
            }
            else
            {
                layer = (TLayer)binding.Layer!;
                error = null;
                state = LayerHub.LayerTargetState.Found;
            }

            LayerHub.UpdateLayerTargetCache<TLayer>(_id, version, layer, state);
            return error == null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InvalidateLayerTargetCaches()
    {
        Interlocked.Increment(ref _layerTypeBindingsVersion);
    }

    public int GetLayerTypeBindingsVersion() => Volatile.Read(ref _layerTypeBindingsVersion);
    #endregion

    #region Diagnostics
    public string GetTopologySummary() => _chain?.GetTopologySummary() ?? "No layers built.";

    public string GetPolicyMarkdown()
    {
        EventBuildPolicyTable? policyTable = ScopeHost?.MainScope.PolicyTable ?? _policyTable;
        if (policyTable == null)
        {
            return "Runtime not built.";
        }

        var sb = new StringBuilder();

        sb.AppendLine("# LayerBase Runtime Policy Dump");
        sb.AppendLine();
        sb.AppendLine("## Event Policies");
        sb.AppendLine(
            "| RuntimeId | StableId | StableKey | Version | Event Type | Post Mode | Backpressure | MaxPending | MergeFailure | Timer | Buffer |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |");

        foreach (var snapshot in policyTable.ExportSnapshots())
        {
            var post = snapshot.PostPolicy;
            var timer = snapshot.TimerPolicy;
            var buffer = snapshot.BufferPolicy;
            var identity = snapshot.Identity;

            sb.Append("| ")
              .Append(snapshot.RuntimeId)
              .Append(" | ")
              .Append(identity.StableId)
              .Append(" | `")
              .Append(identity.StableKey)
              .Append("` | ")
              .Append(identity.Version)
              .Append(" | ")
              .Append(identity.EventType.Name)
              .Append(" | ")
              .Append(post?.Mode.ToString() ?? "Normal")
              .Append(" | ")
              .Append(post?.Backpressure.ToString() ?? "Default")
              .Append(" | ")
              .Append(post?.MaxPending.ToString() ?? "0")
              .Append(" | ")
              .Append(post?.MergeFailure.ToString() ?? "Reject")
              .Append(" | ")
              .Append(timer != null ? "Yes" : "No")
              .Append(" | ")
              .Append(buffer != null ? "Yes" : "No")
              .AppendLine(" |");
        }

        return sb.ToString();
    }

    public string GetTopologyMarkdown()
    {
        if (_chain == null) return "No layers built.";

        var sb = new StringBuilder();
        sb.AppendLine("# LayerBase Topology Snapshot");
        sb.AppendLine();


        sb.AppendLine("## 1. Layers");
        sb.AppendLine("| Index | Layer Type | Active Logic |");
        sb.AppendLine("| :--- | :--- | :--- |");
        foreach (var layer in _chain.GetNodes().OfType<Layer>())
            sb.AppendLine($"| {layer.RouteIndex} | {layer.GetType().Name} | {layer.HasActiveLogic} |");
        sb.AppendLine();


        sb.AppendLine("## 2. Event Subscriptions");
        sb.AppendLine("| Event Type | Subscribed Layers |");
        sb.AppendLine("| :--- | :--- |");

        var eventMap = new Dictionary<Type, List<string>>();
        foreach (var layer in _chain.GetNodes().OfType<Layer>())


        foreach (var evt in layer.SubscribedEvents)
        {
            if (!eventMap.TryGetValue(evt, out var layers))
                eventMap[evt] = layers = new List<string>();
            layers.Add(layer.GetType().Name);
        }

        if (eventMap.Count == 0) sb.AppendLine("| (None) | |");
        foreach (var kvp in eventMap.OrderBy(x => x.Key.Name))
            sb.AppendLine($"| {kvp.Key.Name} | {string.Join(", ", kvp.Value)} |");
        sb.AppendLine();


        sb.AppendLine("## 3. Call Routes");
        sb.AppendLine("| Request | Response | Target Layer | Handler |");
        sb.AppendLine("| :--- | :--- | :--- | :--- |");
        var hasCalls = false;
        foreach (var layer in _chain.GetNodes().OfType<Layer>())
        foreach (var call in layer.CallHandlers)
        {
            sb.AppendLine($"| {call.Req.Name} | {call.Resp.Name} | {layer.GetType().Name} | {call.Handler.Name} |");
            hasCalls = true;
        }

        if (!hasCalls) sb.AppendLine("| (None) | | | |");
        sb.AppendLine();


        sb.AppendLine("## 4. Shared Fields");
        sb.AppendLine("| OwnerType | LocalKey | Type | Role | Layer |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
        var hasFields = false;
        foreach (var layer in _chain.GetNodes().OfType<Layer>())
        foreach (var field in layer.SharedFields)
        {
            var role = field.IsProvider ? "**Provide**" : "Use";
            sb.AppendLine(
                $"| {field.OwnerType.Name} | `{field.Key}` | {field.FieldType.Name} | {role} | {layer.GetType().Name} |");
            hasFields = true;
        }

        if (!hasFields) sb.AppendLine("| (None) | | | | |");
        sb.AppendLine();


        sb.AppendLine("## 5. Health Audit");
        var issues = new List<string>();

        var allLayers = _chain.GetNodes().OfType<Layer>().ToList();
        var allSubscribed = allLayers.SelectMany(l => l.SubscribedEvents).ToHashSet();
        var allProduced = allLayers.SelectMany(l => l.ProducedEvents).ToHashSet();
        var allCallHandlers = allLayers.SelectMany(l => l.CallHandlers.Select(ch => ch.Req)).ToHashSet();
        var allCallInvoked = CallUsageTracker.GetUsedRequestTypes().ToHashSet();
        var allProvideKeys = allLayers.SelectMany(l =>
            l.SharedFields.Where(f => f.IsProvider).Select(f => $"{f.OwnerType.FullName}_{f.Key}")).ToHashSet();
        var allUseKeys = allLayers.SelectMany(l =>
            l.SharedFields.Where(f => !f.IsProvider).Select(f => $"{f.OwnerType.FullName}_{f.Key}")).ToHashSet();


        foreach (var evt in allSubscribed)
            if (!allProduced.Contains(evt))
                issues.Add($"- **Zombie Event**: `{evt.Name}` is subscribed but never produced (Send/Post).");


        foreach (var evt in allProduced)
            if (!allSubscribed.Contains(evt))
                issues.Add($"- **Unused Producer**: Event `{evt.Name}` is produced but has no subscribers.");


        foreach (var req in allCallHandlers)
            if (!allCallInvoked.Contains(req))
                issues.Add(
                    $"- **Dead Call Route**: Request `{req.Name}` has a handler but is never invoked via `CallAsync`.");


        foreach (var key in allProvideKeys)
            if (!allUseKeys.Contains(key))
            {
                var keyName = key.Substring(key.IndexOf('_') + 1);
                issues.Add(
                    $"- **Orphaned Provide**: Shared key `{keyName}` is published but never consumed via `[From]`. (Scope: {key.Split('_')[0]})");
            }

        if (issues.Count == 0)
            sb.AppendLine("No health issues detected. All bindings are active and used.");
        else
            foreach (var issue in issues)
                sb.AppendLine(issue);

        return sb.ToString();
    }
    #endregion

    #region Nested Types
    public sealed class LayersBuilder
    {
        private readonly LayerRuntime _runtime;
        private readonly ResponsibilityChain _chain = new(new RcOwnerToken());
        private bool _debugMode;
        private LayerChain? _layerChain;
        private int _pendingLayerCount;
        private PostSchedulerOptions _postOptions = PostSchedulerOptions.Default;
        private TimeSchedulerOptions _timerOptions = TimeSchedulerOptions.Default;
        private DelayBufferOptions _delayOptions = DelayBufferOptions.Default;
        private FixedUpdateOptions _fixedUpdateOptions = FixedUpdateOptions.Default;
        private readonly List<Action<LayerToolRegistry>> _toolConfigurators = new();
        private bool _built;

        internal LayersBuilder(LayerRuntime runtime) => _runtime = runtime;

        internal LayerRuntime RuntimeForTest => _runtime;

        public LayersBuilder Push(Layer layer)
        {
            if (_built) throw new InvalidOperationException("Cannot push layers after Build has been called.");
            if (_pendingLayerCount >= 64)
                throw new InvalidOperationException(
                    "LayerBase currently supports a maximum of 64 layers due to bitmap routing constraints.");

            _pendingLayerCount++;
            if (_layerChain == null)
            {
                _layerChain = new LayerChain(_chain, _runtime);
                _runtime._chain = _layerChain;
            }

            layer.AttachToContext(_runtime);
            _layerChain.AddNode(layer);
            return this;
        }

        public LayersBuilder Install(params ILayerBaseModule[] modules)
        {
            if (_built) throw new InvalidOperationException("Cannot install modules after Build has been called.");
            if (modules == null || modules.Length == 0)
            {
                return this;
            }

            (_runtime._installedModules ??= new List<ILayerBaseModule>()).AddRange(modules);
            return this;
        }

        public LayersBuilder SetDebug(bool enabled = true)
        {
            _debugMode = enabled;
            _runtime.IsDebugMode = enabled;
            return this;
        }

        public LayersBuilder SetPostOptions(PostSchedulerOptions options)
        {
            _postOptions = options;
            return this;
        }

        public LayersBuilder SetTimerOptions(TimeSchedulerOptions options)
        {
            _timerOptions = options;
            return this;
        }

        public LayersBuilder SetDelayOptions(DelayBufferOptions options)
        {
            _delayOptions = options;
            return this;
        }

        public LayersBuilder SetFixedUpdateOptions(FixedUpdateOptions options)
        {
            _fixedUpdateOptions = options;
            return this;
        }

        public LayersBuilder SetEcsOptions(EcsRuntimeOptions options)
        {
            if (_built) throw new InvalidOperationException("Cannot configure ECS after Build has been called.");
            _runtime.ConfigureEcs(options);
            return this;
        }

        public LayersBuilder SetEcsExecutionMode(EcsExecutionMode executionMode)
        {
            return SetEcsOptions(new EcsRuntimeOptions(executionMode));
        }

        public LayersBuilder ConfigureTools(Action<LayerToolRegistry> configure)
        {
            if (_built) throw new InvalidOperationException("Cannot configure tools after Build has been called.");
            _toolConfigurators.Add(configure ?? throw new ArgumentNullException(nameof(configure)));
            return this;
        }

        public LayerRuntime Build()
        {
            if (_built) throw new InvalidOperationException("LayersBuilder.Build can only be called once.");
            if (_layerChain == null) throw new InvalidOperationException("No layers added.");
            _built = true;
            _runtime.MarkBuildState(RuntimeBuildState.Building);

            try
            {
                foreach (var configureTools in _toolConfigurators)
                {
                    configureTools(_runtime.Tools);
                }

                _layerChain.Prebuild();

                _runtime._fixedUpdateOptions = _fixedUpdateOptions;
                _runtime._mainScopeRuntimeOptions = _runtime.CreateMainScopeRuntimeOptions(
                    _postOptions,
                    _timerOptions,
                    _delayOptions);
                _runtime.BuildServiceProvider();
                _runtime.Actors.PrepareRuntimeBuild();
                _runtime.InitializeScopeHost();
                _layerChain.BuildAutoBindings();
                _runtime.EnsurePostSchedulersKnowAllocatedEventTypes();
                _layerChain.Build(1024, true);
                _runtime.ScopeHost?.Start();
                _runtime._context = _runtime.ScopeHost?.MainScope.ContextForTest;
                _runtime.Actors.CompleteRuntimeBuild();
                _runtime.BuildFullSnapCache();
                _runtime.PolicyTable.Freeze();
                _runtime.Worker.Start();

                if (_debugMode)
                {
                    _runtime.ReportInfo(new LayerEventInfo(-1, "System", "Topology", _runtime.GetTopologySummary(),
                        LayerEventInfoType.Info));
                    _runtime.ReportInfo(new LayerEventInfo(-1, "System", "TopologySnapshot", _runtime.GetTopologyMarkdown(),
                        LayerEventInfoType.Info));
                    _runtime.ReportInfo(new LayerEventInfo(-1, "System", "PolicyDump", _runtime.GetPolicyMarkdown(),
                        LayerEventInfoType.Info));
                }

                _runtime.MarkBuildState(RuntimeBuildState.Running);
                return _runtime;
            }
            catch
            {
                _runtime.MarkBuildState(RuntimeBuildState.Faulted);
                _runtime.AbortFrameworkBuild();
                throw;
            }
        }
    }

    internal readonly struct LayerTypeBinding
    {
        private LayerTypeBinding(Layer? layer, int count)
        {
            Layer = layer;
            Count = count;
        }

        public Layer? Layer { get; }
        public int Count { get; }
        public bool IsAmbiguous => Count > 1;

        public static LayerTypeBinding Create(Layer layer)
        {
            return new LayerTypeBinding(layer, 1);
        }

        public LayerTypeBinding WithAdditional(Layer layer)
        {
            return new LayerTypeBinding(Layer ?? layer, Count + 1);
        }
    }

    internal enum RuntimeBuildState
    {
        Created = 0,
        Building = 1,
        Running = 2,
        Faulted = 3,
        Disposed = 4
    }

    public readonly struct LayerCallTarget<TLayer> where TLayer : Layer
    {
        private readonly LayerRuntime _owner;
        private readonly TLayer? _layer;
        private readonly Exception? _error;

        internal LayerCallTarget(LayerRuntime owner, TLayer? layer, Exception? error)
        {
            _owner = owner;
            _layer = layer;
            _error = error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LBTask<TResponse> CallAsync<TRequest, TResponse>(TRequest          request,
                                                                CancellationToken cancellationToken = default)
            where TRequest : struct
            where TResponse : struct
        {
            if (_layer != null)
                return _layer.CallAsync<TRequest, TResponse>(request, cancellationToken);

            if (_error != null)
                return LBTask<TResponse>.FromException(_error);

            if (_owner.TryResolveLayerTarget<TLayer>(out var layer, out var error))
                return layer!.CallAsync<TRequest, TResponse>(request, cancellationToken);

            return LBTask<TResponse>.FromException(error!);
        }
    }

    private sealed class RuntimeTimerSink : IExpiredTimerSink<ITimerAction>
    {
        private readonly PostScheduler _scheduler;
        private readonly EventBuildPolicyTable _policyTable;

        public RuntimeTimerSink(PostScheduler scheduler, EventBuildPolicyTable policyTable)
        {
            _scheduler = scheduler;
            _policyTable = policyTable;
        }

        public bool TryAcceptExpired(in ITimerAction payload, TimerHandle handle) => payload.Execute(_scheduler);
    }
    #endregion
}
