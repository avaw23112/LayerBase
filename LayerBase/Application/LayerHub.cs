using System.Runtime.CompilerServices;
using LayerBase.Async;
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

    public static GlobalEventCenter EventCenter { get; internal set; } = new();

    public static bool IsDebugMode { get; private set; }
    public static event Action<LayerEventInfo>? OnLayerEventInfo;

    internal static int GetNextLayerIndex()
    {
        return s_layerIndexCounter++;
    }

    public static LayersBuilder CreateLayers()
    {
        if (SynchronizationContext.Current == null)
            s_context = LayerBaseSynchronizationContext.InstallAsCurrent();
        else if (s_context == null && SynchronizationContext.Current is not LayerBaseSynchronizationContext ctx)
            s_context = LayerBaseSynchronizationContext.Install();

        return new LayersBuilder();
    }

    public static void Pump(float deltaTime)
    {
        s_context?.Update();
        s_chain?.Pump(deltaTime);
    }

    public static void Reset()
    {
        s_chain = null;
        s_layerIndexCounter = 0;
        EventCenter = new GlobalEventCenter();
        ServiceLayerBinder.Reset();
        OnLayerEventInfo = null;
        IsDebugMode = false;

        s_context?.Dispose();
        if (SynchronizationContext.Current == s_context) SynchronizationContext.SetSynchronizationContext(null);
        s_context = null;
    }

    internal static void ReportInfo(LayerEventInfo info)
    {
        OnLayerEventInfo?.Invoke(info);
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
            if (s_chain == null) s_chain = new LayerChain(_chain);
            s_chain.AddNode(layer);
            return this;
        }

        public LayersBuilder SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            IsDebugMode = enabled;
            return this;
        }

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
}