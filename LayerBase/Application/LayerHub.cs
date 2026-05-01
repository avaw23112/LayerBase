using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Text;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Tools.Job;

namespace LayerBase;

public enum LayerEventInfoType { Debug, Info, Warning, Error }

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
    private static readonly List<WeakReference<LayerRuntime>> s_runtimes = new();
    private static readonly object s_lock = new();
    private static int s_runtimeIdCounter;
    private static readonly ConcurrentBag<Action> s_cacheResetters = new();
    
    // Primary runtime for static convenience APIs
    private static LayerRuntime? s_primaryRuntime;

    public static event Action<LayerEventInfo>? OnLayerEventInfo;

    /// <summary>
    /// Creates a new isolated LayerRuntime. The first one created becomes the Primary runtime.
    /// </summary>
    public static LayerRuntime.LayersBuilder CreateLayers()
    {
        lock (s_lock)
        {
            var id = s_runtimeIdCounter++;
            if (id >= 256) throw new InvalidOperationException("Max 256 concurrent LayerRuntimes supported by static caches.");
            var runtime = new LayerRuntime(id);
            if (s_primaryRuntime == null) s_primaryRuntime = runtime;
            return new LayerRuntime.LayersBuilder(runtime);
        }
    }

    /// <summary>
    /// Optional: Pumps all active runtimes tracked by the Hub.
    /// Users can also pump their runtimes manually.
    /// </summary>
    public static void Pump(float deltaTime)
    {
        lock (s_lock)
        {
            for (var i = s_runtimes.Count - 1; i >= 0; i--)
            {
                if (s_runtimes[i].TryGetTarget(out var runtime))
                {
                    runtime.Pump(deltaTime);
                }
                else
                {
                    s_runtimes.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Resets the global registry and static caches. 
    /// Note: Does NOT dispose runtimes held by external references unless they are the primary one.
    /// </summary>
    public static void Reset()
    {
        lock (s_lock)
        {
            s_primaryRuntime?.Dispose();
            s_primaryRuntime = null;
            s_runtimes.Clear();
            s_runtimeIdCounter = 0;
            foreach (var resetter in s_cacheResetters) resetter();
            ServiceLayerBinder.Reset();
            LayerServiceRegistry.Reset();
            OnLayerEventInfo = null;
        }
    }

    /// <summary>
    /// Convenience API: Sends event to the Primary runtime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventHandledState Send<T>(in T value) where T : struct
    {
        return s_primaryRuntime?.Send(value) ?? EventHandledState.Continue;
    }

    /// <summary>
    /// Convenience API: Posts event to the Primary runtime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Post<T>(in T value) where T : struct
    {
        s_primaryRuntime?.Post(value);
    }

    /// <summary>
    /// Convenience API: Marks an event type as dirty in the Primary runtime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkDirty<T>() where T : struct
    {
        s_primaryRuntime?.MarkDirty<T>();
    }

    /// <summary>
    /// Convenience API: Posts a latest event to the Primary runtime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PostLatest<T>(in T value) where T : struct
    {
        s_primaryRuntime?.PostLatest(value);
    }

    /// <summary>
    /// Convenience API: Posts a coalesced event to the Primary runtime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PostCoalesced<T>(in T value) where T : struct
    {
        s_primaryRuntime?.PostCoalesced(value);
    }

    /// <summary>
    /// Convenience API: Creates a call target for the Primary runtime.
    /// </summary>
    public static LayerRuntime.LayerCallTarget<TLayer> For<TLayer>() where TLayer : Layer
    {
        if (s_primaryRuntime == null) throw new InvalidOperationException("No Primary LayerRuntime created. Call CreateLayers().Build() first.");
        return s_primaryRuntime.For<TLayer>();
    }

    /// <summary>
    /// Convenience API: Performs a call on the Primary runtime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LBTask<TResponse> CallAsync<TLayer, TRequest, TResponse>(TRequest request,
                                                                           CancellationToken cancellationToken =
                                                                               default)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        if (s_primaryRuntime == null) throw new InvalidOperationException("No Primary LayerRuntime created. Call CreateLayers().Build() first.");
        return s_primaryRuntime.CallAsync<TLayer, TRequest, TResponse>(request, cancellationToken);
    }

    internal static void Internal_Register(LayerRuntime runtime)
    {
        lock (s_lock) s_runtimes.Add(new WeakReference<LayerRuntime>(runtime));
    }

    internal static void Internal_Unregister(LayerRuntime runtime)
    {
        lock (s_lock)
        {
            if (ReferenceEquals(s_primaryRuntime, runtime)) s_primaryRuntime = null;
            for (var i = s_runtimes.Count - 1; i >= 0; i--)
            {
                if (s_runtimes[i].TryGetTarget(out var r) && r == runtime)
                {
                    s_runtimes.RemoveAt(i);
                    break;
                }
            }
        }
    }

    internal static void Internal_NotifyEvent(LayerEventInfo info)
    {
        OnLayerEventInfo?.Invoke(info);
    }

    public static void InitializeJobScheduler(int workerCount)
    {
        JobSchedulers.ConfigureDefault(workerCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReportLayerEventError(int layerIndex, string source, string eventName, Exception ex)
    {
        if (s_primaryRuntime != null)
        {
            s_primaryRuntime.ReportLayerEventError(layerIndex, source, eventName, ex);
        }
        else
        {
            Internal_NotifyEvent(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error, ex));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReportLayerEventError(int layerIndex, int sourceId, int eventNameId, Exception ex)
    {
        if (s_primaryRuntime != null)
        {
            s_primaryRuntime.ReportLayerEventError(layerIndex, sourceId, eventNameId, ex);
        }
        else
        {
            var source = EventDiagnosticSymbols.Resolve(sourceId);
            var eventName = EventDiagnosticSymbols.Resolve(eventNameId);
            Internal_NotifyEvent(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error, ex));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReportWarning(int layerIndex, string source, string eventName, string message)
    {
        Internal_NotifyEvent(new LayerEventInfo(layerIndex, source, eventName, message, LayerEventInfoType.Warning));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetCachedTarget<TLayer>(int runtimeId, int version, out TLayer? layer, out Exception? error)
        where TLayer : Layer
    {
        if (runtimeId >= 256)
        {
             layer = null;
             error = null;
             return false;
        }

        if (Volatile.Read(ref LayerTargetCache<TLayer>.Versions[runtimeId]) != version)
        {
            layer = null;
            error = null;
            return false;
        }

        switch (LayerTargetCache<TLayer>.States[runtimeId])
        {
            case LayerTargetState.Found:
                layer = LayerTargetCache<TLayer>.Layers[runtimeId];
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

    internal static void UpdateLayerTargetCache<TLayer>(int runtimeId, int version, TLayer? layer, LayerTargetState state)
        where TLayer : Layer
    {
        if (runtimeId >= 256) return;
        LayerTargetCache<TLayer>.Layers[runtimeId] = layer;
        LayerTargetCache<TLayer>.States[runtimeId] = state;
        Volatile.Write(ref LayerTargetCache<TLayer>.Versions[runtimeId], version);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetCallCacheVersion<TLayer, TRequest, TResponse>(int runtimeId)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        if (runtimeId >= 256) return -1;
        return Volatile.Read(ref LayerCallCache<TLayer, TRequest, TResponse>.Versions[runtimeId]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static LayerCallInvoker<TRequest, TResponse>? GetCallInvoker<TLayer, TRequest, TResponse>(int runtimeId)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        if (runtimeId >= 256) return null;
        return LayerCallCache<TLayer, TRequest, TResponse>.Invokers[runtimeId];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Exception? GetCallError<TLayer, TRequest, TResponse>(int runtimeId)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        if (runtimeId >= 256) return null;
        return LayerCallCache<TLayer, TRequest, TResponse>.Errors[runtimeId];
    }

    internal static void UpdateLayerCallCache<TLayer, TRequest, TResponse>(int runtimeId, int version, LayerCallInvoker<TRequest, TResponse>? invoker, Exception? error)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        if (runtimeId >= 256) return;
        lock (s_lock)
        {
            LayerCallCache<TLayer, TRequest, TResponse>.Invokers[runtimeId] = invoker;
            LayerCallCache<TLayer, TRequest, TResponse>.Errors[runtimeId] = error;
            Volatile.Write(ref LayerCallCache<TLayer, TRequest, TResponse>.Versions[runtimeId], version);
        }
    }

    internal static void RegisterCacheResetter(Action resetter)
    {
        s_cacheResetters.Add(resetter);
    }

    private static class LayerCallCache<TLayer, TRequest, TResponse>
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        public static readonly int[] Versions = new int[256];
        public static readonly LayerCallInvoker<TRequest, TResponse>?[] Invokers = new LayerCallInvoker<TRequest, TResponse>[256];
        public static readonly Exception?[] Errors = new Exception[256];

        static LayerCallCache()
        {
            for (int i = 0; i < 256; i++) Versions[i] = -1;
            RegisterCacheResetter(Reset);
        }

        private static void Reset()
        {
            for (int i = 0; i < 256; i++)
            {
                Invokers[i] = null;
                Errors[i] = null;
                Volatile.Write(ref Versions[i], -1);
            }
        }
    }

    internal enum LayerTargetState : byte
    {
        Unknown = 0,
        Found = 1,
        Missing = 2,
        Ambiguous = 3
    }

    private static class LayerTargetCache<TLayer> where TLayer : Layer
    {
        public static readonly int[] Versions = new int[256];
        public static readonly TLayer?[] Layers = new TLayer[256];
        public static readonly LayerTargetState[] States = new LayerTargetState[256];

        static LayerTargetCache()
        {
            for (int i = 0; i < 256; i++) Versions[i] = -1;
            RegisterCacheResetter(Reset);
        }

        private static void Reset()
        {
            for (int i = 0; i < 256; i++)
            {
                Layers[i] = null;
                States[i] = LayerTargetState.Unknown;
                Volatile.Write(ref Versions[i], -1);
            }
        }
    }
}
