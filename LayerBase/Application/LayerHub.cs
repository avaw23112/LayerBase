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
    private static readonly AsyncLocal<LayerRuntime?> s_currentRuntime = new();

    public static LayerRuntime? Current => s_currentRuntime.Value;

    internal static void SetCurrent(LayerRuntime? runtime) => s_currentRuntime.Value = runtime;

    public static event Action<LayerEventInfo>? OnLayerEventInfo;

    public static LayerRuntime.LayersBuilder CreateLayers()
    {
        lock (s_lock)
        {
            var id = s_runtimeIdCounter++;
            if (id >= 64) throw new InvalidOperationException("Max 64 concurrent LayerRuntimes supported.");
            return new LayerRuntime.LayersBuilder(new LayerRuntime(id));
        }
    }

    public static void Pump(float deltaTime)
    {
        lock (s_lock)
        {
            for (var i = s_runtimes.Count - 1; i >= 0; i--)
            {
                if (s_runtimes[i].TryGetTarget(out var runtime))
                {
                    var prev = Current;
                    s_currentRuntime.Value = runtime;
                    try
                    {
                        runtime.Pump(deltaTime);
                    }
                    finally
                    {
                        s_currentRuntime.Value = prev;
                    }
                }
                else
                {
                    s_runtimes.RemoveAt(i);
                }
            }
        }
    }

    public static void Reset()
    {
        lock (s_lock)
        {
            for (var i = s_runtimes.Count - 1; i >= 0; i--)
            {
                if (s_runtimes[i].TryGetTarget(out var runtime)) runtime.Dispose();
            }
            s_runtimes.Clear();
            s_runtimeIdCounter = 0;
            s_currentRuntime.Value = null;
            foreach (var resetter in s_cacheResetters) resetter();
            ServiceProvider.ResetRoot();
            ServiceLayerBinder.Reset();
            LayerServiceRegistry.Reset();
            OnLayerEventInfo = null;
        }
    }

    internal static void Internal_Register(LayerRuntime runtime)
    {
        lock (s_lock) s_runtimes.Add(new WeakReference<LayerRuntime>(runtime));
    }

    internal static void Internal_Unregister(LayerRuntime runtime)
    {
        lock (s_lock)
        {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReportLayerEventError(int layerIndex, string source, string eventName, Exception ex)
    {
        if (Current != null)
        {
            Current.ReportLayerEventError(layerIndex, source, eventName, ex);
        }
        else
        {
            Internal_NotifyEvent(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error,
                ex));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReportWarning(int layerIndex, string source, string eventName, string message)
    {
        if (Current != null)
        {
            Current.ReportWarning(layerIndex, source, eventName, message);
        }
        else
        {
            Internal_NotifyEvent(new LayerEventInfo(layerIndex, source, eventName, message, LayerEventInfoType.Warning));
        }
    }

    public static void InitializeJobScheduler(int workerCount)
    {
        JobSchedulers.ConfigureDefault(workerCount);
    }

    public static LayerCallTarget<TLayer> For<TLayer>() where TLayer : Layer
    {
        var runtime = Current ?? throw new InvalidOperationException("No current LayerRuntime context.");
        runtime.TryResolveLayerTarget<TLayer>(out var layer, out var error);
        return new LayerCallTarget<TLayer>(layer, error);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LBTask<TResponse> CallAsync<TLayer, TRequest, TResponse>(TRequest request,
                                                                           CancellationToken cancellationToken =
                                                                               default)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        var runtime = Current ?? throw new InvalidOperationException("No current LayerRuntime context.");
        var runtimeId = runtime.Id;
        var version = runtime.GetLayerTypeBindingsVersion();
        
        if (LayerCallCache<TLayer, TRequest, TResponse>.Versions[runtimeId] != version)
            return CallAsyncSlow<TLayer, TRequest, TResponse>(runtime, version, request, cancellationToken);

        var invoker = LayerCallCache<TLayer, TRequest, TResponse>.Invokers[runtimeId];
        if (invoker != null) return invoker(request, cancellationToken);

        return LBTask<TResponse>.FromException(LayerCallCache<TLayer, TRequest, TResponse>.Errors[runtimeId]!);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static LBTask<TResponse> CallAsyncSlow<TLayer, TRequest, TResponse>(LayerRuntime runtime, int version, TRequest request,
        CancellationToken cancellationToken)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        UpdateLayerCallCache<TLayer, TRequest, TResponse>(runtime, version);

        var runtimeId = runtime.Id;
        var invoker = LayerCallCache<TLayer, TRequest, TResponse>.Invokers[runtimeId];
        if (invoker != null) return invoker(request, cancellationToken);

        return LBTask<TResponse>.FromException(LayerCallCache<TLayer, TRequest, TResponse>.Errors[runtimeId]!);
    }

    private static void UpdateLayerCallCache<TLayer, TRequest, TResponse>(LayerRuntime runtime, int version)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        var runtimeId = runtime.Id;
        lock (s_lock)
        {
            if (LayerCallCache<TLayer, TRequest, TResponse>.Versions[runtimeId] == version) return;

            if (runtime.TryResolveLayerTarget<TLayer>(out var layer, out var error))
            {
                try
                {
                    var invoker = layer!.GetCallInvoker<TRequest, TResponse>();
                    LayerCallCache<TLayer, TRequest, TResponse>.Invokers[runtimeId] = invoker;
                    LayerCallCache<TLayer, TRequest, TResponse>.Errors[runtimeId] = null;
                }
                catch (Exception ex)
                {
                    LayerCallCache<TLayer, TRequest, TResponse>.Invokers[runtimeId] = null;
                    LayerCallCache<TLayer, TRequest, TResponse>.Errors[runtimeId] = ex;
                }
            }
            else
            {
                LayerCallCache<TLayer, TRequest, TResponse>.Invokers[runtimeId] = null;
                LayerCallCache<TLayer, TRequest, TResponse>.Errors[runtimeId] = error;
            }

            Volatile.Write(ref LayerCallCache<TLayer, TRequest, TResponse>.Versions[runtimeId], version);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventHandledState Send<T>(in T value) where T : struct
    {
        return Current?.Send(value) ?? EventHandledState.Continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Post<T>(in T value) where T : struct
    {
        Current?.Post(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetCachedTarget<TLayer>(int runtimeId, int version, out TLayer? layer, out Exception? error)
        where TLayer : Layer
    {
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
        LayerTargetCache<TLayer>.Layers[runtimeId] = layer;
        LayerTargetCache<TLayer>.States[runtimeId] = state;
        Volatile.Write(ref LayerTargetCache<TLayer>.Versions[runtimeId], version);
    }

    private static void RegisterCacheResetter(Action resetter)
    {
        s_cacheResetters.Add(resetter);
    }

    public readonly struct LayerCallTarget<TLayer> where TLayer : Layer
    {
        private readonly TLayer? _layer;
        private readonly Exception? _error;

        internal LayerCallTarget(TLayer? layer, Exception? error)
        {
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

            var runtime = Current ?? throw new InvalidOperationException("No current LayerRuntime context.");
            if (runtime.TryResolveLayerTarget<TLayer>(out var layer, out var error))
                return layer!.CallAsync<TRequest, TResponse>(request, cancellationToken);

            return LBTask<TResponse>.FromException(error!);
        }
    }

    private static class LayerCallCache<TLayer, TRequest, TResponse>
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        public static readonly int[] Versions = new int[64];
        public static readonly LayerCallInvoker<TRequest, TResponse>?[] Invokers = new LayerCallInvoker<TRequest, TResponse>[64];
        public static readonly Exception?[] Errors = new Exception[64];

        static LayerCallCache()
        {
            for (int i = 0; i < 64; i++) Versions[i] = -1;
            RegisterCacheResetter(Reset);
        }

        private static void Reset()
        {
            for (int i = 0; i < 64; i++)
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
        public static readonly int[] Versions = new int[64];
        public static readonly TLayer?[] Layers = new TLayer[64];
        public static readonly LayerTargetState[] States = new LayerTargetState[64];

        static LayerTargetCache()
        {
            for (int i = 0; i < 64; i++) Versions[i] = -1;
            RegisterCacheResetter(Reset);
        }

        private static void Reset()
        {
            for (int i = 0; i < 64; i++)
            {
                Layers[i] = null;
                States[i] = LayerTargetState.Unknown;
                Volatile.Write(ref Versions[i], -1);
            }
        }
    }
}
