using System.Runtime.CompilerServices;
using System.Text;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.Event.Delay;
using LayerBase.Layers;

namespace LayerBase;

public sealed class LayerRuntime : IDisposable
{
    private LayerChain? _chain;
    internal LayerBaseSynchronizationContext? _context;
    public WorldTaskApi? Tasks { get; private set; }
    private int _layerIndexCounter;
    private int _layerTypeBindingsVersion;
    private readonly Dictionary<Type, LayerTypeBinding> _layerTypeBindings = new();
    private bool _disposed;
    private readonly int _id;

    public int Id => _id;
    public EventCenter EventCenter { get; internal set; }
    
    private PostScheduler? _scheduler;
    public PostScheduler Scheduler => _scheduler ?? throw new InvalidOperationException("Runtime not built.");

    private TimeScheduler<ITimerAction>? _timer;
    public TimeScheduler<ITimerAction> Timer => _timer ?? throw new InvalidOperationException("Runtime not built.");
    
    private RuntimeTimerSink? _timerSink;

    public bool IsDebugMode { get; internal set; }
    public event Action<LayerEventInfo>? OnLayerEventInfo;

    internal LayerRuntime(int id)
    {
        _id = id;
        EventCenter = new EventCenter();
        LayerHub.Internal_Register(this);
    }

    private EventRuntimePolicyTable? _policyTable;
    public EventRuntimePolicyTable PolicyTable => _policyTable ?? throw new InvalidOperationException("Runtime not built.");

    internal void InitializeScheduler(PostSchedulerOptions options)
    {
        _policyTable = new EventRuntimePolicyTable(options.DefaultBackpressure);
        foreach (var (type, meta) in LayerBase.Event.EventMetaData.EventMetaDataHandler.GetAllMetaData())
        {
            var eventId = meta.EventId;
            
            var postPolicy = meta.GetPostPolicy();
            _policyTable.SetMetaData(eventId, meta);
            if (postPolicy != null)
            {
                _policyTable.SetPostPolicy(eventId, postPolicy.Value);
            }

            var timerPolicy = meta.GetTimerPolicy();
            if (timerPolicy != null)
            {
                _policyTable.SetTimerPolicy(eventId, timerPolicy.Value);
            }

            var bufferPolicy = meta.GetBufferPolicy();
            if (bufferPolicy != null)
            {
                _policyTable.SetBufferPolicy(eventId, bufferPolicy.Value);
            }
        }
        _scheduler = new PostScheduler(EventCenter, options, _policyTable);
    }

    internal void InitializeTimer(TimeSchedulerOptions options)
    {
        _timer = new TimeScheduler<ITimerAction>(options);
        _timerSink = new RuntimeTimerSink(_scheduler!, _policyTable!);
    }

    internal void InitializeDelay(DelayBufferOptions options)
    {
        DelayPublisherManager.Initialize(options, _policyTable!);
    }

    internal int GetNextLayerIndex()
    {
        return Interlocked.Increment(ref _layerIndexCounter) - 1;
    }

    public void Pump(float deltaTime)
    {
        if (_disposed) return;
        
        if (_context != null)
        {
            using var scope = _context.EnterScope();
            
            // 1. Time tick
            _timer?.Tick(deltaTime, _timerSink!);
            
            // 2. Delay tick (Stage 4)
            DelayPublisherManager.Instance?.Tick(deltaTime);
            
            // 3. Completion drain (Stage 5 Concurrency Simplified)
            _context.Update(_scheduler?.Options.MaxCompletionsPerPump ?? 0);
            
            // 4. Post pump
            _scheduler?.Pump();
            
            _chain?.Pump(deltaTime);
        }
        else
        {
            // 1. Time tick
            _timer?.Tick(deltaTime, _timerSink!);
            
            // 2. Delay tick (Stage 4)
            DelayPublisherManager.Instance?.Tick(deltaTime);
            
            // 3. Post pump
            _scheduler?.Pump();
            
            _chain?.Pump(deltaTime);
        }
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
    public EventHandledState Send<T>(in T value) where T : struct
    {
        return EventCenter.Send(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T>(in T value) where T : struct
    {
        Scheduler.TryPost(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkDirty<T>() where T : struct
    {
        Scheduler.MarkDirty<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostLatest<T>(in T value) where T : struct
    {
        Scheduler.TryPost(value, new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostCoalesced<T>(in T value) where T : struct
    {
        Scheduler.TryPost(value, new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0));
    }

    public TimerHandle SchedulePost<T>(in T value, float delaySeconds) where T : struct
    {
        var eventId = EventTypeId<T>.Id;
        var timerPolicy = _policyTable?.GetTimerPolicy(eventId);
        
        return Timer.Schedule(
            new PostEventAction<T>(value, timerPolicy?.ExpiredPostPolicy), 
            delaySeconds,
            repeatCount: 0,
            intervalSeconds: 0,
            repeatMode: timerPolicy?.RepeatMode,
            catchUpPolicy: timerPolicy?.CatchUpPolicy
        );
    }

    public LayerCallTarget<TLayer> For<TLayer>() where TLayer : Layer
    {
        TryResolveLayerTarget<TLayer>(out var layer, out var error);
        return new LayerCallTarget<TLayer>(this, layer, error);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> CallAsync<TLayer, TRequest, TResponse>(TRequest request,
                                                                           CancellationToken cancellationToken =
                                                                               default)
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _chain?.DisposeLayers();
        _chain = null;
        _scheduler?.Dispose();
        _timer?.Dispose();
        DelayPublisherManager.Instance?.Clear();
        EventCenter.Reset();
        _context?.Dispose();
        LayerHub.Internal_Unregister(this);
    }

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

    public string GetTopologySummary() => _chain?.GetTopologySummary() ?? "No layers built.";

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
        var allCallInvoked = allLayers.SelectMany(l => l.InvokedCalls).ToHashSet();
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

        internal LayersBuilder(LayerRuntime runtime) => _runtime = runtime;

        public LayersBuilder Push(Layer layer)
        {
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

        public LayerRuntime Build()
        {
            if (_layerChain == null) throw new InvalidOperationException("No layers added.");
            
            if (_runtime._context == null)
                _runtime._context = LayerBaseSynchronizationContext.Install();

            _runtime.Tasks = new WorldTaskApi(_runtime._context);

            _layerChain.Build(1024, true);
            _runtime.InitializeScheduler(_postOptions);
            _runtime.InitializeTimer(_timerOptions);
            _runtime.InitializeDelay(_delayOptions);

            if (_debugMode)
            {
                _runtime.ReportInfo(new LayerEventInfo(-1, "System", "Topology", _runtime.GetTopologySummary(), LayerEventInfoType.Info));
                _runtime.ReportInfo(new LayerEventInfo(-1, "System", "TopologySnapshot", _runtime.GetTopologyMarkdown(), LayerEventInfoType.Info));
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
        private readonly EventRuntimePolicyTable _policyTable;
        public RuntimeTimerSink(PostScheduler scheduler, EventRuntimePolicyTable policyTable)
        {
            _scheduler = scheduler;
            _policyTable = policyTable;
        }
        public bool TryAcceptExpired(in ITimerAction payload, TimerHandle handle) => payload.Execute(_scheduler);
    }
}
