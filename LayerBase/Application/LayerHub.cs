using System.Runtime.CompilerServices;
using System.Text;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Tools.Job;

namespace LayerBase;

public enum LayerEventInfoType
{
    Debug,
    Info,
    Warning,
    Error
}

public readonly struct LayerEventInfo
{
    public readonly int LayerIndex;
    public readonly string Source;
    public readonly string EventName;
    public readonly string Message;
    public readonly Exception? Exception;
    public readonly LayerEventInfoType Type;

    public LayerEventInfo(int layerIndex, string source, string eventName, string message, LayerEventInfoType type,
                          Exception? exception = null)
    {
        LayerIndex = layerIndex;
        Source = source;
        EventName = eventName;
        Message = message;
        Type = type;
        Exception = exception;
    }

    public override string ToString()
    {
        return $"[{Type}] [Layer {LayerIndex}] {Source} -> {EventName}: {Message}";
    }
}

public static class LayerHub
{
    private static LayerChain? s_chain;
    private static LayerBaseSynchronizationContext? s_context;
    private static int s_layerIndexCounter;
    private static int s_layerTypeBindingsVersion;
    private static readonly Dictionary<Type, LayerTypeBinding> s_layerTypeBindings = new();
    private static readonly object s_lock = new();

    public static GlobalEventCenter EventCenter { get; internal set; } = new();

    public static bool IsDebugMode { get; private set; }
    public static event Action<LayerEventInfo>? OnLayerEventInfo;

    internal static int GetNextLayerIndex()
    {
        return GetNextLayerIndexInternal();
    }

    // 内部实现
    private static int GetNextLayerIndexInternal()
    {
        return Interlocked.Increment(ref s_layerIndexCounter) - 1;
    }

    public static LayersBuilder CreateLayers()
    {
        lock (s_lock)
        {
            if (SynchronizationContext.Current == null)
                s_context = LayerBaseSynchronizationContext.InstallAsCurrent();
            else if (s_context == null && SynchronizationContext.Current is not LayerBaseSynchronizationContext ctx)
                s_context = LayerBaseSynchronizationContext.Install();

            return new LayersBuilder();
        }
    }

    public static void Pump(float deltaTime)
    {
        s_context?.Update();
        s_chain?.Pump(deltaTime);
    }

    public static void Reset()
    {
        lock (s_lock)
        {
            s_chain = null;
            s_layerIndexCounter = 0;
            s_layerTypeBindings.Clear();
            InvalidateLayerTargetCaches();
            EventCenter.Reset();
            ServiceProvider.ResetRoot(); // New: Reset global singleton container
            ServiceLayerBinder.Reset();
            LayerServiceRegistry.Reset();
            OnLayerEventInfo = null;
            IsDebugMode = false;

            s_context?.Dispose();
            if (SynchronizationContext.Current == s_context) SynchronizationContext.SetSynchronizationContext(null);
            s_context = null;
        }
    }

    internal static void ReportInfo(LayerEventInfo info)
    {
        var handler = OnLayerEventInfo;
        if (handler != null)
            try
            {
                handler.Invoke(info);
            }
            catch
            {
                // Ignore observer exceptions to avoid crashing the framework
            }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReportLayerEventError(int layerIndex, string source, string eventName, Exception ex)
    {
        ReportInfo(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error, ex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReportWarning(int layerIndex, string source, string eventName, string message)
    {
        ReportInfo(new LayerEventInfo(layerIndex, source, eventName, message, LayerEventInfoType.Warning));
    }

    public static void InitializeJobScheduler(int workerCount)
    {
        JobSchedulers.ConfigureDefault(workerCount);
    }

    public static LayerCallTarget<TLayer> For<TLayer>() where TLayer : Layer
    {
        return new LayerCallTarget<TLayer>();
    }

    public static LBTask<TResponse> CallAsync<TLayer, TRequest, TResponse>(TRequest request,
                                                                           CancellationToken cancellationToken =
                                                                               default)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        return For<TLayer>().CallAsync<TRequest, TResponse>(request, cancellationToken);
    }

    internal static void RegisterLayerInstance(Layer layer)
    {
        var layerType = layer.GetType();
        lock (s_lock)
        {
            if (s_layerTypeBindings.TryGetValue(layerType, out var existing))
                s_layerTypeBindings[layerType] = existing.WithAdditional(layer);
            else
                s_layerTypeBindings[layerType] = LayerTypeBinding.Create(layer);
            InvalidateLayerTargetCaches();
        }
    }

    internal static TLayer ResolveLayerTarget<TLayer>() where TLayer : Layer
    {
        if (TryResolveLayerTarget<TLayer>(out var layer, out var error)) return layer!;
        throw error!;
    }

    internal static bool TryResolveLayerTarget<TLayer>(out TLayer? layer, out Exception? error)
        where TLayer : Layer
    {
        var version = Volatile.Read(ref s_layerTypeBindingsVersion);
        if (TryGetCachedTarget(version, out layer, out error)) return error == null;

        lock (s_lock)
        {
            version = s_layerTypeBindingsVersion;
            if (TryGetCachedTarget(version, out layer, out error)) return error == null;

            LayerTargetState state;
            if (!s_layerTypeBindings.TryGetValue(typeof(TLayer), out var binding))
            {
                layer = null;
                error = new LayerCallTargetNotFoundException(typeof(TLayer));
                state = LayerTargetState.Missing;
            }
            else if (binding.IsAmbiguous)
            {
                layer = null;
                error = new LayerCallTargetAmbiguousException(typeof(TLayer));
                state = LayerTargetState.Ambiguous;
            }
            else
            {
                layer = (TLayer)binding.Layer!;
                error = null;
                state = LayerTargetState.Found;
            }

            LayerTargetCache<TLayer>.Layer = layer;
            LayerTargetCache<TLayer>.State = state;
            Volatile.Write(ref LayerTargetCache<TLayer>.Version, version);
            return error == null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventHandledState Send<T>(in T value) where T : struct
    {
        return EventCenter.Send(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Post<T>(in T value) where T : struct
    {
        EventCenter.Post(value);
    }

    public static string GetTopologyMarkdown()
    {
        if (s_chain == null) return "No layers built.";

        var sb = new StringBuilder();
        sb.AppendLine("# LayerBase Topology Snapshot");
        sb.AppendLine();

        // 1. Layer Overview
        sb.AppendLine("## 1. Layers");
        sb.AppendLine("| Index | Layer Type | Active Logic |");
        sb.AppendLine("| :--- | :--- | :--- |");
        foreach (var layer in s_chain.GetNodes().OfType<Layer>())
            sb.AppendLine($"| {layer.RouteIndex} | {layer.GetType().Name} | {layer.HasActiveLogic} |");
        sb.AppendLine();

        // 2. Event Subscriptions
        sb.AppendLine("## 2. Event Subscriptions");
        sb.AppendLine("| Event Type | Subscribed Layers |");
        sb.AppendLine("| :--- | :--- |");

        var eventMap = new Dictionary<Type, List<string>>();
        foreach (var layer in s_chain.GetNodes().OfType<Layer>())
            // Note: Currently we only capture events explicitly tracked in SubscribedEvents
            // In a real scenario, we might want to query EventCenter if possible
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

        // 3. Call Routes
        sb.AppendLine("## 3. Call Routes");
        sb.AppendLine("| Request | Response | Target Layer | Handler |");
        sb.AppendLine("| :--- | :--- | :--- | :--- |");
        var hasCalls = false;
        foreach (var layer in s_chain.GetNodes().OfType<Layer>())
        foreach (var call in layer.CallHandlers)
        {
            sb.AppendLine($"| {call.Req.Name} | {call.Resp.Name} | {layer.GetType().Name} | {call.Handler.Name} |");
            hasCalls = true;
        }

        if (!hasCalls) sb.AppendLine("| (None) | | | |");
        sb.AppendLine();

        // 4. Shared Fields (Provide/Use)
        sb.AppendLine("## 4. Shared Fields");
        sb.AppendLine("| OwnerType | LocalKey | Type | Role | Layer |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
        var hasFields = false;
        foreach (var layer in s_chain.GetNodes().OfType<Layer>())
        foreach (var field in layer.SharedFields)
        {
            var role = field.IsProvider ? "**Provide**" : "Use";
            sb.AppendLine(
                $"| {field.OwnerType.Name} | `{field.Key}` | {field.FieldType.Name} | {role} | {layer.GetType().Name} |");
            hasFields = true;
        }

        if (!hasFields) sb.AppendLine("| (None) | | | | |");
        sb.AppendLine();

        // 5. Health Audit
        sb.AppendLine("## 5. Health Audit");
        var issues = new List<string>();

        var allLayers = s_chain.GetNodes().OfType<Layer>().ToList();
        var allSubscribed = allLayers.SelectMany(l => l.SubscribedEvents).ToHashSet();
        var allProduced = allLayers.SelectMany(l => l.ProducedEvents).ToHashSet();
        var allCallHandlers = allLayers.SelectMany(l => l.CallHandlers.Select(ch => ch.Req)).ToHashSet();
        var allCallInvoked = allLayers.SelectMany(l => l.InvokedCalls).Concat(CallUsageTracker.GetUsedRequestTypes())
                                      .ToHashSet();
        var allProvideKeys = allLayers.SelectMany(l =>
            l.SharedFields.Where(f => f.IsProvider).Select(f => $"{f.OwnerType.FullName}_{f.Key}")).ToHashSet();
        var allUseKeys = allLayers.SelectMany(l =>
            l.SharedFields.Where(f => !f.IsProvider).Select(f => $"{f.OwnerType.FullName}_{f.Key}")).ToHashSet();

        // Check Zombie Events
        foreach (var evt in allSubscribed)
            if (!allProduced.Contains(evt))
                issues.Add($"- **Zombie Event**: `{evt.Name}` is subscribed but never produced (Send/Post).");

        // Check Unsent Events (Produced but no one listening)
        foreach (var evt in allProduced)
            if (!allSubscribed.Contains(evt))
                issues.Add($"- **Unused Producer**: Event `{evt.Name}` is produced but has no subscribers.");

        // Check Uncalled Call Routes
        foreach (var req in allCallHandlers)
            if (!allCallInvoked.Contains(req))
                issues.Add(
                    $"- **Dead Call Route**: Request `{req.Name}` has a handler but is never invoked via `CallAsync`.");

        // Check Orphaned Provides
        foreach (var key in allProvideKeys)
            if (!allUseKeys.Contains(key))
            {
                var keyName = key.Substring(key.IndexOf('_') + 1);
                issues.Add(
                    $"- **Orphaned Provide**: Shared key `{keyName}` is published but never consumed via `[From]`. (Scope: {key.Split('_')[0]})");
            }

        if (issues.Count == 0)
            sb.AppendLine("�?No health issues detected. All bindings are active and used.");
        else
            foreach (var issue in issues)
                sb.AppendLine(issue);

        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetCachedTarget<TLayer>(int version, out TLayer? layer, out Exception? error)
        where TLayer : Layer
    {
        if (Volatile.Read(ref LayerTargetCache<TLayer>.Version) != version)
        {
            layer = null;
            error = null;
            return false;
        }

        switch (LayerTargetCache<TLayer>.State)
        {
            case LayerTargetState.Found:
                layer = LayerTargetCache<TLayer>.Layer;
                error = null;
                return true;
            case LayerTargetState.Missing:
                layer = null;
                error = new LayerCallTargetNotFoundException(typeof(TLayer));
                return true;
            case LayerTargetState.Ambiguous:
                layer = null;
                error = new LayerCallTargetAmbiguousException(typeof(TLayer));
                return true;
            default:
                layer = null;
                error = null;
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InvalidateLayerTargetCaches()
    {
        Interlocked.Increment(ref s_layerTypeBindingsVersion);
    }

    public sealed class LayersBuilder
    {
        private readonly ResponsibilityChain _chain = new(new RcOwnerToken());
        private bool _debugMode;

        public LayersBuilder Push(Layer layer)
        {
            if (s_layerIndexCounter >= 64)
                throw new InvalidOperationException(
                    "LayerBase currently supports a maximum of 64 layers due to bitmap routing constraints.");

            if (s_chain == null) s_chain = new LayerChain(_chain);
            s_chain.AddNode(layer);
            return this;
        }

        public LayersBuilder SetDebug(bool enabled = true)
        {
            _debugMode = enabled;
            IsDebugMode = enabled;
            return this;
        }

        [Obsolete("Use SetDebug(bool) instead.")]
        public LayersBuilder SetDebugMode(bool enabled)
        {
            return SetDebug(enabled);
        }

        public void Build()
        {
            if (s_chain == null) throw new InvalidOperationException("No layers added.");
            s_chain.Build(1024, true);
            if (_debugMode)
            {
                ReportTopology();
                ReportInfo(new LayerEventInfo(-1, "System", "TopologySnapshot", GetTopologyMarkdown(),
                    LayerEventInfoType.Info));
            }
        }

        private void ReportTopology()
        {
            if (s_chain == null) return;
            var summary = s_chain.GetTopologySummary();
            ReportInfo(new LayerEventInfo(-1, "System", "Topology", summary, LayerEventInfoType.Info));
        }
    }

    public readonly struct LayerCallTarget<TLayer> where TLayer : Layer
    {
        public LBTask<TResponse> CallAsync<TRequest, TResponse>(TRequest          request,
                                                                CancellationToken cancellationToken = default)
            where TRequest : struct
            where TResponse : struct
        {
            if (TryResolveLayerTarget<TLayer>(out var layer, out var error))
                return layer!.CallAsync<TRequest, TResponse>(request, cancellationToken);

            return LBTask<TResponse>.FromException(error!);
        }
    }

    private readonly struct LayerTypeBinding
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

    private enum LayerTargetState : byte
    {
        Unknown = 0,
        Found = 1,
        Missing = 2,
        Ambiguous = 3
    }

    private static class LayerTargetCache<TLayer> where TLayer : Layer
    {
        public static int Version = -1;
        public static TLayer? Layer;
        public static LayerTargetState State;
    }
}



