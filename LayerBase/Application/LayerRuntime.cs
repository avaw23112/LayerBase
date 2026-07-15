using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.Event.Delay;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
using LayerBase.Snap;
using LayerBase.Tools;
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
    internal EventCenter EventCenter => _scopeHost.MainScope.EventCenter;
    internal ActorWorld Actors => _mainActorRuntime.World;
    #endregion

    #region Runtime State - Configuration
    // 运行时配置参数。
    private FixedUpdateOptions _fixedUpdateOptions = FixedUpdateOptions.Disabled;
    #endregion

    #region Runtime State - Subsystems
    // 各子系统实例，在 Build 过程中创建。
    private LayerChain? _chain;
    private readonly MainActorRuntime _mainActorRuntime;
    private readonly ScopeRuntimeHost _scopeHost;
    private readonly WorkerJobScheduler _workerJobs;
    private FullSnapRuntime? _fullSnap;
    private LayerToolRegistry? _tools;
    internal DelayPublisherManager? DelayManager => _scopeHost.MainScope.DelayManager;
    #endregion

    #region Runtime State - Layer Bindings
    // Layer 注册信息：索引计数器、类型绑定表、版本号。
    private int _layerIndexCounter;
    #endregion

    #region Runtime State - Identification
    // Runtime 的唯一标识符和释放标记。
    private readonly int _id;
    private readonly int _generation = 1;
    private readonly ScopeRef<MainScope> _mainScope;
    private bool _disposed;
    #endregion

    #region Properties
    public int Id => _id;

    public ScopeRef<MainScope> Main => _mainScope;

    internal PostScheduler Scheduler => _scopeHost.MainScope.PostScheduler ?? throw new InvalidOperationException("Runtime not built.");

    internal TimeScheduler<ITimerAction> Timer => _scopeHost.MainScope.Timer ?? throw new InvalidOperationException("Runtime not built.");

    public IFullSnapRuntime FullSnap => _fullSnap ?? throw new InvalidOperationException("Runtime not built.");

    public LayerToolRegistry Tools => _tools ?? throw new InvalidOperationException("Runtime not built.");

    public bool IsDebugMode { get; internal set; }

    internal bool IsDisposed => _disposed;

    internal int Generation => _generation;

    internal ScopeRuntimeHost ScopeHost => _scopeHost;

    internal MainActorRuntime MainActorRuntime => _mainActorRuntime;

    internal WorkerJobScheduler WorkerJobs => _workerJobs;

    internal RuntimeCompositionPlan CompositionPlan { get; private set; } = RuntimeCompositionPlan.Empty;
    #endregion

    #region Events
    public event Action<LayerEventInfo>? OnLayerEventInfo;

    public event Action<ScopeFaultInfo>? Faulted;
    #endregion

    #region Constructors & Initialization
    internal LayerRuntime(int id)
    {
        _id = id;
        _mainActorRuntime = new MainActorRuntime(this, _generation);
        _scopeHost = ScopeRuntimeHost.CreateMain(this, _id, _generation);
        _workerJobs = new WorkerJobScheduler(WorkerJobSchedulerOptions.Default);
        _mainScope = new ScopeRef<MainScope>(_scopeHost.MainScope.Endpoint);
        LayerHub.Internal_Register(this);
    }

    public ScopeRef<TScope> GetScope<TScope>()
        where TScope : IScopeDefinition
    {
        if (TryGetScope<TScope>(out var scope))
            return scope;

        throw new InvalidOperationException($"Scope `{typeof(TScope).FullName}` is not registered in this runtime.");
    }

    public bool TryGetScope<TScope>(out ScopeRef<TScope> scope)
        where TScope : IScopeDefinition
    {
        return _scopeHost.TryGetScope(out scope);
    }

    internal EventBuildPolicyTable PolicyTable =>
        _scopeHost.MainScope.PolicyTable ?? throw new InvalidOperationException("Runtime not built.");

    internal void InitializeScheduler(PostSchedulerOptions options)
    {
        BuildEventPolicies(options);
    }

    internal void RebuildEventPolicies()
    {
        var scheduler = _scopeHost.MainScope.PostScheduler;
        if (scheduler == null) throw new InvalidOperationException("Runtime not built.");
        BuildEventPolicies(scheduler.Options);
    }

    private void BuildEventPolicies(PostSchedulerOptions options)
    {
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        var metaData = LayerBase.Event.EventMetaData.EventMetaDataHandler.GetAllMetaData().ToList();
        var plans = new List<PostTypePlan>();

        foreach (var (type, meta) in metaData)
        {
            var eventId = meta.EventId;
            _ = meta.GetIdentity();

            var postPolicy = meta.GetPostPolicy();
            policyTable.SetMetaData(eventId, meta);
            if (postPolicy != null)
                policyTable.SetPostPolicy(eventId, postPolicy.Value);

            var timerPolicy = meta.GetTimerPolicy();
            if (timerPolicy != null)
                policyTable.SetTimerPolicy(eventId, timerPolicy.Value);

            var bufferPolicy = meta.GetBufferPolicy();
            if (bufferPolicy != null)
                policyTable.SetBufferPolicy(eventId, bufferPolicy.Value);

            var actorMailOptions = meta.GetActorMailOptions();
            if (actorMailOptions != null)
                policyTable.SetActorMailOptions(eventId, actorMailOptions.Value);

            var effectivePolicy =
                postPolicy ?? new EventPostPolicy(PostDeliveryMode.Normal, options.DefaultBackpressure, 0);
            plans.Add(new PostTypePlan(eventId, effectivePolicy.Mode, effectivePolicy.Backpressure,
                effectivePolicy.MaxPending, options.DefaultBackpressure, effectivePolicy.MergeFailure));
        }

        _scopeHost.MainScope.InitializeOrUpdateScheduler(options, policyTable, plans.ToArray());
    }

    internal void InitializeTimer(TimeSchedulerOptions options)
    {
        _scopeHost.MainScope.InitializeTimer(options);
    }

    internal void InitializeDelay(DelayBufferOptions options)
    {
        _scopeHost.MainScope.InitializeDelay(options);
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
    #endregion

    #region Lifecycle - Pump
    public void Pump(float deltaTime)
    {
        if (_disposed) return;

        var context = _scopeHost.MainScope.SynchronizationContext;
        if (context != null)
        {
            using var scope = context.EnterScope();
            PumpCore(deltaTime);
            return;
        }

        PumpCore(deltaTime);
    }

    private void PumpCore(float deltaTime)
    {
        // 1. Scope call/event ingress
        _scopeHost.MainScope.PumpIngress();

        // 2. OwnerScope continuations
        var policy = IsDebugMode ? CompletionExceptionPolicy.Throw : CompletionExceptionPolicy.ReportAndContinue;
        _scopeHost.MainScope.PumpSynchronizationContext(
            policy,
            ex => ReportLayerEventError(-1, "System", "Completion", ex));

        // 3. Time and delay tick
        _scopeHost.MainScope.TickTimer(deltaTime);
        _scopeHost.MainScope.DelayManager?.Tick(deltaTime);

        // 4. Local post pump
        var scheduler = _scopeHost.MainScope.PostScheduler;
        PostPumpStats postStats = scheduler?.Pump()
                                  ?? new PostPumpStats(0, 0, 0, 0);

        // 5. Scope-local FixedUpdate accumulator
        _scopeHost.MainScope.PumpFixedUpdate(_fixedUpdateOptions, deltaTime);

        // 6. Layer lifecycle update
        _scopeHost.MainScope.PumpUpdate(deltaTime);

        if (scheduler != null)
        {
            RuntimeFrameBudget actorBudget = CreateActorBudget(scheduler.Options, postStats);
            bool pumpActorFixedUpdate = _fixedUpdateOptions.Enabled;
            float actorFixedDeltaTime = _fixedUpdateOptions.Enabled
                ? _fixedUpdateOptions.FixedDeltaTime
                : 0f;

            _mainActorRuntime.Pump(
                deltaTime: deltaTime,
                fixedDeltaTime: actorFixedDeltaTime,
                pumpFixedUpdate: pumpActorFixedUpdate,
                budget: ref actorBudget);

            EcsWorld.SweepProjectedActors();
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
    internal void ReportInfo(LayerEventInfo info)
    {
        OnLayerEventInfo?.Invoke(info);
        LayerHub.Internal_NotifyEvent(info);
    }

    internal void ReportScopeFault(in ScopeFaultRecord record)
    {
        var handlers = Faulted;
        if (handlers == null)
            return;

        var info = new ScopeFaultInfo(record);
        foreach (Action<ScopeFaultInfo> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(info);
            }
            catch
            {
                // Fault callbacks are host-side diagnostics; they must not become a recursive fault channel.
            }
        }
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LBTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request,
                                                            CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return _scopeHost.MainScope.CallLocalAsync<TRequest, TResponse>(request, cancellationToken);
    }

    #endregion

    #region Lifecycle - Dispose
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _scopeHost.MainScope.RunRuntimeStop();
        _workerJobs.BeginStop();
        _chain?.DisposeLayers();
        _chain = null;
        _scopeHost.Dispose();
        _tools?.Dispose();
        _workerJobs.Dispose();
        _mainActorRuntime.Dispose();
        LayerHub.ClearRuntimeCaches(_id);
        LayerHub.Internal_Unregister(this);
    }
    #endregion

    #region Internal - Scope Local Calls
    internal void BuildLocalCallRegistry()
    {
        foreach (var scope in _scopeHost.Scopes)
            scope.ClearLocalCallRegistry();

        if (_chain == null) return;

        foreach (var layer in _chain.GetNodes())
        foreach (var entry in layer.LocalCallRouteEntries)
        {
            if (!_scopeHost.TryGetRuntime(entry.OwnerScopeId, out var ownerScope))
                continue;

            ownerScope.LocalCalls.Register(entry);
        }
    }

    #endregion

    #region Diagnostics
    public string GetTopologySummary() => _chain?.GetTopologySummary() ?? "No layers built.";

    public string GetPolicyMarkdown()
    {
        var policyTable = _scopeHost.MainScope.PolicyTable;
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
        sb.AppendLine("| ProviderServiceType | LocalKey | Type | Role | Layer |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
        var hasFields = false;
        foreach (var layer in _chain.GetNodes().OfType<Layer>())
        foreach (var field in layer.SharedFields)
        {
            var role = field.IsProvider ? "**Provide**" : "Use";
            sb.AppendLine(
                $"| {field.ProviderServiceType.Name} | `{field.Key}` | {field.FieldType.Name} | {role} | {layer.GetType().Name} |");
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
            l.SharedFields.Where(f => f.IsProvider).Select(f => $"{f.ProviderServiceType.FullName}_{f.Key}")).ToHashSet();
        var allUseKeys = allLayers.SelectMany(l =>
            l.SharedFields.Where(f => !f.IsProvider).Select(f => $"{f.ProviderServiceType.FullName}_{f.Key}")).ToHashSet();


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
        private readonly List<IAssemblyModule> _assemblyModules = new();
        private int _pendingLayerCount;
        private PostSchedulerOptions _postOptions = PostSchedulerOptions.Default;
        private TimeSchedulerOptions _timerOptions = TimeSchedulerOptions.Default;
        private DelayBufferOptions _delayOptions = DelayBufferOptions.Default;
        private FixedUpdateOptions _fixedUpdateOptions = FixedUpdateOptions.Default;
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

        public LayersBuilder AddAssemblyModule(IAssemblyModule module)
        {
            if (_built) throw new InvalidOperationException("Cannot add assembly modules after Build has been called.");
            if (module == null) throw new ArgumentNullException(nameof(module));

            _assemblyModules.Add(module);
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

        public LayerRuntime Build()
        {
            if (_built) throw new InvalidOperationException("LayersBuilder.Build can only be called once.");
            if (_layerChain == null) throw new InvalidOperationException("No layers added.");
            _built = true;

            _runtime._scopeHost.MainScope.InstallSynchronizationContext();

            _layerChain.Prebuild();
            _runtime.CompositionPlan = RuntimeCompositionPlan.Build(
                _layerChain.GetNodes().ToArray(),
                _assemblyModules);
            _runtime._tools = new LayerToolRegistry(_runtime, _runtime.CompositionPlan.Tools);
            _runtime.BuildLocalCallRegistry();

            _runtime._fixedUpdateOptions = _fixedUpdateOptions;
            _runtime.InitializeScheduler(_postOptions);
            _runtime.InitializeTimer(_timerOptions);
            _runtime.InitializeDelay(_delayOptions);
            _runtime._mainActorRuntime.PrepareRuntimeBuild();
            _layerChain.Build(1024, true);
            _runtime._mainActorRuntime.CompleteRuntimeBuild();
            _runtime.BuildFullSnapCache();
            _runtime.PolicyTable.Freeze();

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

    #endregion
}
