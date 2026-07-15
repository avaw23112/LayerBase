using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Text;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Tools.Job;

namespace LayerBase;

/// <summary>
/// Layer 事件信息的严重级别。
/// </summary>
public enum LayerEventInfoType
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// 描述 Layer 运行时产生的事件信息，包括来源、事件名、消息和异常。
/// </summary>
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

/// <summary>
/// LayerBase 的全局静态入口。管理多个 LayerRuntime 实例的生命周期，
/// 并提供便捷 API 访问 Primary Runtime 的事件发送、投递和跨层调用。
/// </summary>
public static class LayerHub
{
    private static readonly List<WeakReference<LayerRuntime>> s_runtimes = new();
    private static readonly object s_lock = new();
    private static readonly Stack<int> s_freeRuntimeIds = new();
    private static int s_runtimeIdCounter;
    private static readonly ConcurrentBag<Action> s_cacheResetters = new();
    private static readonly ConcurrentBag<Action<int>> s_runtimeCacheResetters = new();

    // Primary runtime for static convenience APIs
    private static LayerRuntime? s_primaryRuntime;

    public static event Action<LayerEventInfo>? OnLayerEventInfo;

    /// <summary>
    /// 创建一个新的独立 LayerRuntime。第一个创建的 Runtime 自动成为 Primary。
    /// </summary>
    public static LayerRuntime.LayersBuilder CreateLayers()
    {
        int id;
        if (s_freeRuntimeIds.Count > 0)
        {
            id = s_freeRuntimeIds.Pop();
        }
        else
        {
            id = s_runtimeIdCounter++;
            if (id >= 256)
                throw new InvalidOperationException("Max 256 concurrent LayerRuntimes supported by static caches.");
        }

        var runtime = new LayerRuntime(id);
        if (s_primaryRuntime == null) s_primaryRuntime = runtime;
        new EventPrewarmBootstrapper();
        return new LayerRuntime.LayersBuilder(runtime);
    }

    /// <summary>
    /// 推进 Hub 跟踪的所有活跃 Runtime。用户也可以手动泵送各自的 Runtime。
    /// </summary>
    public static void Pump(float deltaTime)
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

    /// <summary>
    /// 重置全局注册表，释放所有跟踪的 Runtime，并清除静态缓存。
    /// </summary>
    public static void Reset()
    {
        lock (s_lock)
        {
            var runtimes = new HashSet<LayerRuntime>();
            if (s_primaryRuntime != null) runtimes.Add(s_primaryRuntime);
            foreach (var weak in s_runtimes)
            {
                if (weak.TryGetTarget(out var runtime)) runtimes.Add(runtime);
            }

            s_primaryRuntime = null;
            s_runtimes.Clear();

            foreach (var runtime in runtimes)
            {
                runtime.Dispose();
            }

            s_freeRuntimeIds.Clear();
            s_runtimeIdCounter = 0;

            foreach (var resetter in s_cacheResetters) resetter();
            ServiceLayerBinder.Reset();
            LayerServiceRegistry.Reset();
            EventIdentityRegistry.Reset();
            OnLayerEventInfo = null;
        }
    }

    /// <summary>
    /// 便捷 API：向 Primary Runtime 同步发送事件。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Send<T>(in T value) where T : struct
    {
        s_primaryRuntime?.Send(value);
    }

    /// <summary>
    /// 便捷 API：向 Primary Runtime 投递事件。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Post<T>(in T value) where T : struct
    {
        _ = TryPost(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult TryPost<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        return s_primaryRuntime != null
            ? s_primaryRuntime.TryPost(value, policy)
            : PostResult.Failure();
    }

    /// <summary>
    /// 便捷 API：在 Primary Runtime 中将事件类型标记为脏。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkDirty<T>() where T : struct
    {
        s_primaryRuntime?.MarkDirty<T>();
    }

    /// <summary>
    /// 便捷 API：向 Primary Runtime 投递最新值事件（合并模式）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PostLatest<T>(in T value) where T : struct
    {
        s_primaryRuntime?.PostLatest(value);
    }

    /// <summary>
    /// 便捷 API：向 Primary Runtime 投递合并事件。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PostCoalesced<T>(in T value) where T : struct
    {
        s_primaryRuntime?.PostCoalesced(value);
    }

    /// <summary>
    /// 便捷 API：在 Primary Runtime 的当前 Scope 上执行本地调用。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LBTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request,
                                                                   CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (s_primaryRuntime == null)
            throw new InvalidOperationException("No Primary LayerRuntime created. Call CreateLayers().Build() first.");
        return s_primaryRuntime.CallAsync<TRequest, TResponse>(request, cancellationToken);
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

            if ((uint)runtime.Id < 256)
            {
                s_freeRuntimeIds.Push(runtime.Id);
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
            Internal_NotifyEvent(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error,
                ex));
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
            Internal_NotifyEvent(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error,
                ex));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReportWarning(int layerIndex, string source, string eventName, string message)
    {
        Internal_NotifyEvent(new LayerEventInfo(layerIndex, source, eventName, message, LayerEventInfoType.Warning));
    }


    internal static void RegisterCacheResetter(Action resetter)
    {
        s_cacheResetters.Add(resetter);
    }

    internal static void RegisterRuntimeCacheResetter(Action<int> resetter)
    {
        s_runtimeCacheResetters.Add(resetter);
    }

    internal static void ClearRuntimeCaches(int runtimeId)
    {
        foreach (var resetter in s_runtimeCacheResetters)
        {
            resetter(runtimeId);
        }
    }
}
