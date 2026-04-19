using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Tools.Job;

namespace LayerBase;

public enum LayerEventInfoType { Debug, Info, Warning, Error }

public readonly struct LayerEventInfo
{
    public readonly LayerRuntime Runtime;
    public readonly int LayerIndex;
    public readonly string Source;
    public readonly string EventName;
    public readonly string Message;
    public readonly Exception? Exception;
    public readonly LayerEventInfoType Type;

    public LayerEventInfo(LayerRuntime runtime, int layerIndex, string source, string eventName, string message, 
                          LayerEventInfoType type, Exception? exception = null)
    {
        Runtime = runtime;
        LayerIndex = layerIndex;
        Source = source;
        EventName = eventName;
        Message = message;
        Type = type;
        Exception = exception;
    }

    public override string ToString() => $"[Runtime:{Runtime.GetHashCode():X}] [{Type}] [Layer {LayerIndex}] {Source} -> {EventName}: {Message}";
}

/// <summary>
///     LayerBase 运行环境。
/// </summary>
public sealed class LayerRuntime : IDisposable
{
    private LayerChain? _chain;
    private LayerBaseSynchronizationContext? _context;
    private int _layerIndexCounter;
    private bool _disposed;

    public GlobalEventCenter EventCenter { get; internal set; }
    public bool IsDebugMode { get; internal set; }
    public event Action<LayerEventInfo>? OnEventInfo;

    internal LayerRuntime()
    {
        EventCenter = new GlobalEventCenter((l, s, e, ex) => ReportError(l, s, e, ex));
        LayerHub.Internal_Register(this);
    }

    internal int GetNextLayerIndex() => _layerIndexCounter++;

    public void Pump(float deltaTime)
    {
        if (_disposed) return;
        _context?.Update();
        _chain?.Pump(deltaTime);
    }

    public void ReportInfo(int layerIndex, string source, string eventName, string message, LayerEventInfoType type = LayerEventInfoType.Info, Exception? ex = null)
    {
        var info = new LayerEventInfo(this, layerIndex, source, eventName, message, type, ex);
        OnEventInfo?.Invoke(info);
        LayerHub.Internal_NotifyEvent(info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportError(int layerIndex, string source, string eventName, Exception ex)
    {
        ReportInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportWarning(int layerIndex, string source, string eventName, string message)
    {
        ReportInfo(layerIndex, source, eventName, message, LayerEventInfoType.Warning);
    }

    /// <summary>
    ///     向该运行环境发送一个全局事件。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState Send<T>(in T value) where T : struct
    {
        return EventCenter.Send(value, 0, Propagation.Global);
    }

    /// <summary>
    ///     向该运行环境投递一个异步全局事件。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T>(in T value) where T : struct
    {
        EventCenter.Post(value, 0, Propagation.Global);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _chain = null;
        EventCenter = null!;
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }

    public sealed class LayersBuilder
    {
        private readonly LayerRuntime _runtime;
        private readonly ResponsibilityChain _chain = new(new RcOwnerToken());
        private bool _debugMode;

        internal LayersBuilder() => _runtime = new LayerRuntime();

        public LayersBuilder Push(Layer layer)
        {
            if (_runtime._layerIndexCounter >= 64)
                throw new InvalidOperationException("LayerBase supports max 64 layers.");
            
            layer.AttachToContext(_runtime);
            if (_runtime._chain == null) _runtime._chain = new LayerChain(_chain, _runtime);
            _runtime._chain.AddNode(layer);
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
            if (_runtime._chain == null) throw new InvalidOperationException("No layers.");
            if (SynchronizationContext.Current == null)
                _runtime._context = LayerBaseSynchronizationContext.InstallAsCurrent();
            else if (_runtime._context == null && SynchronizationContext.Current is not LayerBaseSynchronizationContext)
                _runtime._context = LayerBaseSynchronizationContext.Install();

            _runtime._chain.Build(1024, true);
            if (_debugMode) ReportTopology();
            return _runtime;
        }

        private void ReportTopology()
        {
            if (_runtime._chain == null) return;
            _runtime.ReportInfo(-1, "System", "Topology", _runtime._chain.GetTopologySummary());
        }
    }
}

/// <summary>
///     LayerBase 全局中心。负责管理所有 LayerRuntime 实例及其全局驱动。
/// </summary>
public static class LayerHub
{
    private static readonly List<WeakReference<LayerRuntime>> s_runtimes = new();
    private static readonly object s_lock = new();

    public static event Action<LayerEventInfo>? OnLayerEventInfo;

    public static LayerRuntime.LayersBuilder CreateLayers() => new();

    /// <summary>
    ///     驱动进程内所有活跃的 LayerRuntime。
    /// </summary>
    public static void Pump(float deltaTime)
    {
        lock (s_lock)
        {
            for (int i = s_runtimes.Count - 1; i >= 0; i--)
            {
                if (s_runtimes[i].TryGetTarget(out var runtime)) runtime.Pump(deltaTime);
                else s_runtimes.RemoveAt(i);
            }
        }
    }

    internal static void Internal_Register(LayerRuntime runtime)
    {
        lock (s_lock) s_runtimes.Add(new WeakReference<LayerRuntime>(runtime));
    }

    internal static void Internal_NotifyEvent(LayerEventInfo info) => OnLayerEventInfo?.Invoke(info);

    public static void InitializeJobScheduler(int workerCount) => JobSchedulers.ConfigureDefault(workerCount);

    public static void Reset()
    {
        lock (s_lock)
        {
            s_runtimes.Clear();
            ServiceLayerBinder.Reset();
        }
    }
}