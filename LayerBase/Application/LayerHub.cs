using System.Runtime.CompilerServices;
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
    private static int GetNextLayerIndexInternal() => Interlocked.Increment(ref s_layerIndexCounter) - 1;

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
            EventCenter = new GlobalEventCenter();
            ServiceProvider.ResetRoot(); // 新增：重置全局单例容器
            ServiceLayerBinder.Reset();
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
        handler?.Invoke(info);
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

    public static LayerCallTarget<TLayer> For<TLayer>() where TLayer : Layers.Layer
    {
        return new LayerCallTarget<TLayer>();
    }

    public static LBTask<TResponse> CallAsync<TLayer, TRequest, TResponse>(TRequest request,
                                                                            CancellationToken cancellationToken = default)
        where TLayer : Layers.Layer
        where TRequest : struct
        where TResponse : struct
    {
        return For<TLayer>().CallAsync<TRequest, TResponse>(request, cancellationToken);
    }

    internal static void RegisterLayerInstance(Layers.Layer layer)
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

    internal static TLayer ResolveLayerTarget<TLayer>() where TLayer : Layers.Layer
    {
        if (TryResolveLayerTarget<TLayer>(out var layer, out var error)) return layer!;
        throw error!;
    }

    internal static bool TryResolveLayerTarget<TLayer>(out TLayer? layer, out Exception? error)
        where TLayer : Layers.Layer
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
        return EventCenter.Send(value, 0, Propagation.Global);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Post<T>(in T value) where T : struct
    {
        EventCenter.Post(value, 0, Propagation.Global);
    }

    public sealed class LayersBuilder
    {
        private readonly ResponsibilityChain _chain = new(new RcOwnerToken());
        private bool _debugMode;

        public LayersBuilder Push(Layer layer)
        {
            if (s_layerIndexCounter >= 64)
                throw new InvalidOperationException("LayerBase currently supports a maximum of 64 layers due to bitmap routing constraints.");
            
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
        public LayersBuilder SetDebugMode(bool enabled) => SetDebug(enabled);

        public void Build()
        {
            if (s_chain == null) throw new InvalidOperationException("No layers added.");
            s_chain.Build(1024, true);
            if (_debugMode) ReportTopology();
        }

        private void ReportTopology()
        {
            if (s_chain == null) return;
            var summary = s_chain.GetTopologySummary();
            ReportInfo(new LayerEventInfo(-1, "System", "Topology", summary, LayerEventInfoType.Info));
        }
    }

    public readonly struct LayerCallTarget<TLayer> where TLayer : Layers.Layer
    {
        public LBTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request,
                                                                CancellationToken cancellationToken = default)
            where TRequest : struct
            where TResponse : struct
        {
            if (TryResolveLayerTarget<TLayer>(out var layer, out var error))
                return layer!.CallAsync<TRequest, TResponse>(request, cancellationToken);

            return LBTask<TResponse>.FromException(error!);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetCachedTarget<TLayer>(int version, out TLayer? layer, out Exception? error)
        where TLayer : Layers.Layer
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

    private readonly struct LayerTypeBinding
    {
        private LayerTypeBinding(Layers.Layer? layer, int count)
        {
            Layer = layer;
            Count = count;
        }

        public Layers.Layer? Layer { get; }
        public int Count { get; }
        public bool IsAmbiguous => Count > 1;

        public static LayerTypeBinding Create(Layers.Layer layer)
        {
            return new LayerTypeBinding(layer, 1);
        }

        public LayerTypeBinding WithAdditional(Layers.Layer layer)
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

    private static class LayerTargetCache<TLayer> where TLayer : Layers.Layer
    {
        public static int Version = -1;
        public static TLayer? Layer;
        public static LayerTargetState State;
    }
}
