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
    internal WorldServiceRoot Services { get; }
    public EventCenter EventCenter { get; internal set; }
    public ActorWorld Actors { get; }
    public WorkerRuntime Worker { get; }
    public ScopeRuntimeHost? ScopeHost { get; private set; }
    private readonly PostIngressQueue _postIngress = new();
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
    private bool _disposed;
    #endregion

    #region Runtime State - Timing
    private float _fixedUpdateAccumulator;
    #endregion

    #region Properties
    public int Id => _id;

    public LayerBase.DI.IServiceProvider ServiceProvider =>
        _worldProvider ?? throw new InvalidOperationException("Runtime not built.");

    public T GetService<T>() where T : class => ServiceProvider.Get<T>();

    public PostScheduler Scheduler => _scheduler ?? throw new InvalidOperationException("Runtime not built.");

    public TimeScheduler<ITimerAction> Timer => _timer ?? throw new InvalidOperationException("Runtime not built.");

    public IFullSnapRuntime FullSnap => _fullSnap ?? throw new InvalidOperationException("Runtime not built.");

    public LayerToolRegistry Tools { get; }

    public bool IsDebugMode { get; internal set; }
    #endregion

    #region Events
    public event Action<LayerEventInfo>? OnLayerEventInfo;
    #endregion

    #region Constructors & Initialization
    internal LayerRuntime(int id)
    {
        _id = id;
        EventCenter = new EventCenter();
        Actors = new ActorWorld(this);
        Worker = new WorkerRuntime(Math.Max(1, Environment.ProcessorCount - 1));
        Tools = new LayerToolRegistry(this);
        Services = new WorldServiceRoot(this);
        InitializeEcsWorld();
        LayerHub.Internal_Register(this);
    }

    private EventBuildPolicyTable? _policyTable;

    public EventBuildPolicyTable PolicyTable =>
        _policyTable ?? throw new InvalidOperationException("Runtime not built.");

    internal void InitializeScheduler(PostSchedulerOptions options)
    {
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

        RegisterGeneratedScopeHostFactory();

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

        if (scopedServices.Count == 0)
        {
            return;
        }

        ScopeHost = ScopeHostFactory.TryCreate(scopedServices, sharedActorWorld: Actors, owningRuntime: this)
                    ?? ScopeRuntimeHost.Create(ScopeRuntimePlanner.Build(scopedServices), sharedActorWorld: Actors, owningRuntime: this);
    }

    private void RegisterGeneratedScopeHostFactory()
    {
        foreach (Layer layer in _chain.GetNodes())
        {
            if (layer is IScopeHostFactoryRegistrar registrar)
            {
                registrar.RegisterScopeHostFactory();
                return;
            }
        }
    }

    private void EnsurePostSchedulersKnowAllocatedEventTypes()
    {
        _scheduler?.BuildPlans(Array.Empty<PostTypePlan>());
        if (ScopeHost == null)
        {
            return;
        }

        IReadOnlyList<ScopeRuntime> scopes = ScopeHost.Scopes;
        for (int i = 0; i < scopes.Count; i++)
        {
            scopes[i].PostScheduler.BuildPlans(Array.Empty<PostTypePlan>());
        }
    }
    #endregion

    #region Lifecycle - Pump
    public void Pump(float deltaTime)
    {
        if (_disposed) return;

        if (_context != null)
        {
            using var scope = _context.EnterScope();

            // Completion drain (only when sync context exists)
            var policy = IsDebugMode ? CompletionExceptionPolicy.Throw : CompletionExceptionPolicy.ReportAndContinue;
            _context.Update(_scheduler?.Options.MaxCompletionsPerPump ?? 0, policy,
                ex => ReportLayerEventError(-1, "System", "Completion", ex));
        }

        EcsScheduler?.NotifyFrameStart();
        try
        {
            PumpCore(deltaTime);
        }
        finally
        {
            EcsScheduler?.NotifyFrameEnd();
        }
    }

    private void PumpCore(float deltaTime)
    {
        if (_scheduler != null)
        {
            Worker.DrainEventsTo(_scheduler, _scheduler.Options.MaxIngressPostsPerPump);
        }

        EcsScheduler?.DrainResults(EcsOptions.MaxResultsDrainPerPump);

        // 1. Time tick
        _timer?.Tick(deltaTime, _timerSink!);

        // 2. Delay tick
        if (_chain != null && _chain.HasAnyDelay)
            DelayManager?.Tick(deltaTime);

        // 3. FixedUpdate accumulator
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

        // 4. Ingress drain
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

        // 5. Post pump + layer pump + actor pump
        PostPumpStats postStats = _scheduler?.Pump()
                                  ?? new PostPumpStats(0, 0, 0, 0);

        _chain?.Pump(deltaTime);
        ScopeHost?.Pump(deltaTime);
        EcsScheduler?.FlushSubmissions();

        if (_scheduler != null)
        {
            RuntimeFrameBudget actorBudget = CreateActorBudget(_scheduler.Options, postStats);
            bool pumpActorFixedUpdate = _fixedUpdateOptions.Enabled;
            float actorFixedDeltaTime = _fixedUpdateOptions.Enabled
                ? _fixedUpdateOptions.FixedDeltaTime
                : 0f;

            Actors.Pump(
                deltaTime: deltaTime,
                fixedDeltaTime: actorFixedDeltaTime,
                pumpFixedUpdate: pumpActorFixedUpdate,
                budget: ref actorBudget);

            if (EcsScheduler.Mode == EcsExecutionMode.Sync || EcsWorkScheduler.IsSchedulerThread)
            {
                EcsWorld.SweepProjectedActors();
            }
            else
            {
                EcsWorkScheduler.Schedule(PooledEcsWorkItem<object?>.Rent(
                    "SweepProjectedActors",
                    null,
                    static (world, _) => world.SweepProjectedActors()));
            }

            EcsScheduler?.FlushSubmissions();
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportLayerEventError(int layerIndex, string source, string eventName, Exception ex)
    {
        ReportInfo(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error, ex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportLayerEventError(int layerIndex, int sourceId, int eventNameId, Exception ex)
    {
        var source = EventDiagnosticSymbols.Resolve(sourceId);
        var eventName = EventDiagnosticSymbols.Resolve(eventNameId);
        ReportInfo(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error, ex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportWarning(int layerIndex, string source, string eventName, string message)
    {
        ReportInfo(new LayerEventInfo(layerIndex, source, eventName, message, LayerEventInfoType.Warning));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send<T>(in T value) where T : struct
    {
        EventCenter.Send(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T>(in T value) where T : struct
    {
        _ = TryPost(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPost<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        return policy.HasValue
            ? Scheduler.TryPost(value, policy.Value)
            : Scheduler.TryPost(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkDirty<T>() where T : struct
    {
        Scheduler.MarkDirty<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostLatest<T>(in T value) where T : struct
    {
        Scheduler.TryPostLatest(value);
    }

    public void PostFromAnyThread<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        if (_disposed) return;
        _postIngress.Enqueue(value, policy);
    }

    public bool TryPostFromAnyThread<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        if (_disposed) return false;
        _postIngress.Enqueue(value, policy);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostCoalesced<T>(in T value) where T : struct
    {
        Scheduler.TryPostCoalesced(value);
    }

    public TimerHandle SchedulePost<T>(in T value, float delaySeconds) where T : struct
    {
        var eventId = EventTypeId<T>.Id;
        var timerPolicy = _policyTable?.GetTimerPolicy(eventId);

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
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _postIngress.Clear();
        EcsScheduler.Dispose();
        Worker.Dispose();
        ScopeHost?.Dispose();
        ScopeHost = null;
        _chain?.DisposeLayers();
        _chain = null;
        Actors.RuntimeStop();
        Actors.Dispose();
        EcsWorld.Dispose();
        Services.Dispose();
        _scheduler?.Dispose();
        _timer?.Dispose();
        DelayManager?.Clear();
        DelayManager = null;
        EventCenter.Reset();
        _context?.Dispose();
        LayerHub.ClearRuntimeCaches(_id);
        LayerHub.Internal_Unregister(this);
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
        if (_policyTable == null)
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

        foreach (var snapshot in _policyTable.ExportSnapshots())
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

            foreach (var configureTools in _toolConfigurators)
            {
                configureTools(_runtime.Tools);
            }

            if (_runtime._context == null)
                _runtime._context = LayerBaseSynchronizationContext.Install();

            _layerChain.Prebuild();

            _runtime._fixedUpdateOptions = _fixedUpdateOptions;
            _runtime.InitializeScheduler(_postOptions);
            _runtime.InitializeTimer(_timerOptions);
            _runtime.InitializeDelay(_delayOptions);
            _runtime.BuildServiceProvider();
            _runtime.Actors.PrepareRuntimeBuild();
            _runtime.InitializeScopeHost();
            _layerChain.BuildAutoBindings();
            _runtime.EnsurePostSchedulersKnowAllocatedEventTypes();
            _layerChain.Build(1024, true);
            _runtime.ScopeHost?.Start();
            _runtime.Actors.CompleteRuntimeBuild();
            _runtime.BuildFullSnapCache();
            _runtime.PolicyTable.Freeze();
            _runtime.EcsScheduler.Start();
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

            return _runtime;
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
