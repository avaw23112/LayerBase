using System.Runtime.CompilerServices;
using System.Text;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase;

public sealed class LayerRuntime : IDisposable
{
    private LayerChain? _chain;
    private LayerBaseSynchronizationContext? _context;
    private int _layerIndexCounter;
    private int _layerTypeBindingsVersion;
    private readonly Dictionary<Type, LayerTypeBinding> _layerTypeBindings = new();
    private bool _disposed;
    private readonly int _id;

    public int Id => _id;
    public GlobalEventCenter EventCenter { get; internal set; }
    public bool IsDebugMode { get; internal set; }
    public event Action<LayerEventInfo>? OnLayerEventInfo;

    internal LayerRuntime(int id)
    {
        _id = id;
        EventCenter = new GlobalEventCenter();
        LayerHub.Internal_Register(this);
    }

    internal int GetNextLayerIndex()
    {
        return Interlocked.Increment(ref _layerIndexCounter) - 1;
    }

    public void Pump(float deltaTime)
    {
        if (_disposed) return;
        _context?.Update();
        _chain?.Pump(deltaTime);
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
        EventCenter.Post(value);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _chain?.DisposeLayers();
        _chain = null;
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

        public LayerRuntime Build()
        {
            if (_layerChain == null) throw new InvalidOperationException("No layers added.");
            
            if (SynchronizationContext.Current == null)
                _runtime._context = LayerBaseSynchronizationContext.InstallAsCurrent();
            else if (_runtime._context == null && SynchronizationContext.Current is not LayerBaseSynchronizationContext)
                _runtime._context = LayerBaseSynchronizationContext.Install();

            _layerChain.Build(1024, true);
            
            // Set as current context automatically for the building thread
            LayerHub.SetCurrent(_runtime);

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
}
