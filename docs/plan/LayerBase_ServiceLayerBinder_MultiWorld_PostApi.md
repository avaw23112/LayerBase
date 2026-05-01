# ServiceLayerBinder 多世界绑定与发送 API 补全修改方案

## 修改范围

只修改以下部分：

```text
LayerBase/DI/ServiceContracts.cs
```
---

## 1. 新增 `ServiceLayerBinding`

位置：`LayerBase/DI/ServiceContracts.cs`

```csharp
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.DI;

/// <summary>
/// 服务对象与某个 LayerRuntime 的绑定信息。
///
/// 该对象作为 ServiceLayerBinder 的 ConditionalWeakTable value 使用。
/// 它不注入到 service 对象自身，因此本轮不会改变对象实例布局。
/// </summary>
internal sealed class ServiceLayerBinding
{
    /// <summary>
    /// 当前对象所属 Runtime 的 ID。
    /// 用于多世界下识别对象绑定在哪个 Runtime。
    /// </summary>
    public readonly int RuntimeId;

    /// <summary>
    /// 当前对象所属 Layer 的索引。
    /// 用于 LayerIndex、诊断、订阅组织。
    /// </summary>
    public readonly int LayerIndex;

    /// <summary>
    /// 当前对象所属 Layer。
    /// Subscribe、OnEvent、GetService、Delay 等仍然通过 Layer 完成。
    /// </summary>
    public readonly Layer Layer;

    /// <summary>
    /// 当前对象所属 Runtime。
    /// Post、SchedulePost 等需要访问 Scheduler、Timer、PolicyTable。
    /// </summary>
    public readonly LayerRuntime Runtime;

    /// <summary>
    /// 当前 Runtime 的 EventCenter。
    /// Send 可以直接使用它，避免 Require 后再经过 Layer.Send。
    /// </summary>
    public readonly EventCenter EventCenter;

    /// <summary>
    /// 创建服务绑定信息。
    /// </summary>
    /// <param name="runtimeId">
    /// 当前对象所属 Runtime 的 ID。
    /// </param>
    /// <param name="layerIndex">
    /// 当前对象所属 Layer 的索引。
    /// </param>
    /// <param name="layer">
    /// 当前对象所属 Layer。
    /// </param>
    /// <param name="runtime">
    /// 当前对象所属 Runtime。
    /// </param>
    public ServiceLayerBinding(
        int runtimeId,
        int layerIndex,
        Layer layer,
        LayerRuntime runtime)
    {
        RuntimeId = runtimeId;
        LayerIndex = layerIndex;
        Layer = layer;
        Runtime = runtime;
        EventCenter = runtime.EventCenter;
    }
}
```

---

## 2. 修改 `ServiceLayerBinder`

将原来的：

```text
ConditionalWeakTable<object, Layer>
```

改为：

```text
ConditionalWeakTable<object, ServiceLayerBinding>
```

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Layers;

namespace LayerBase.DI;

/// <summary>
/// 服务对象与 LayerRuntime 的绑定器。
///
/// 当前版本只负责多世界绑定，不做字段注入优化。
/// </summary>
internal static class ServiceLayerBinder
{
    /// <summary>
    /// 对象到绑定信息的弱引用表。
    ///
    /// key 是 service / manager / handler 实例。
    /// value 是该对象所属 Runtime 与 Layer 的绑定信息。
    /// </summary>
    private static ConditionalWeakTable<object, ServiceLayerBinding> s_bindingMap = new();

    /// <summary>
    /// 重置绑定表。
    /// </summary>
    public static void Reset()
    {
        s_bindingMap = new ConditionalWeakTable<object, ServiceLayerBinding>();
    }

    /// <summary>
    /// 把对象绑定到指定 Layer。
    /// </summary>
    /// <param name="service">
    /// 需要绑定的服务对象。
    /// </param>
    /// <param name="layer">
    /// service 所属的 Layer。
    /// </param>
    public static void Attach(object service, Layer layer)
    {
        if (service == null || layer == null)
        {
            return;
        }

        var runtime = layer.OwnerContext;

        if (runtime == null)
        {
            throw new InvalidOperationException("Layer is not attached to LayerRuntime.");
        }

        var binding = new ServiceLayerBinding(
            runtimeId: runtime.Id,
            layerIndex: layer.RouteIndex,
            layer: layer,
            runtime: runtime);

        s_bindingMap.Remove(service);
        s_bindingMap.Add(service, binding);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = layer.RouteIndex;
        }
    }

    /// <summary>
    /// 获取对象的绑定信息。
    /// </summary>
    /// <param name="service">
    /// 已绑定到 Layer 的对象。
    /// </param>
    /// <returns>
    /// 该对象所属 Runtime 与 Layer 的绑定信息。
    /// </returns>
    public static ServiceLayerBinding RequireBinding(object service)
    {
        if (s_bindingMap.TryGetValue(service, out var binding))
        {
            return binding;
        }

        throw new InvalidOperationException(
            $"Object {service.GetType().Name} is not attached to any Layer.");
    }

    /// <summary>
    /// 获取对象所属 Layer。
    /// 保留给现有冷路径 API 使用。
    /// </summary>
    /// <param name="service">
    /// 已绑定到 Layer 的对象。
    /// </param>
    /// <returns>
    /// 对象所属 Layer。
    /// </returns>
    public static Layer Require(object service)
    {
        return RequireBinding(service).Layer;
    }

    /// <summary>
    /// 获取对象所属 Layer 的索引。
    /// </summary>
    /// <param name="context">
    /// LayerContext 对象。
    /// </param>
    /// <returns>
    /// LayerIndex。
    /// </returns>
    public static int GetIndex(ILayerContext context)
    {
        if (context is IInternalLayerContext internalContext &&
            internalContext.LayerIndex != -1)
        {
            return internalContext.LayerIndex;
        }

        return RequireBinding(context).LayerIndex;
    }
}
```

---

## 3. 修改 `ServiceExtensions`

位置：`LayerBase/DI/ServiceContracts.cs`

### 3.1 内部获取 Binding

```csharp
private static ServiceLayerBinding GetBinding(this IService service)
{
    return ServiceLayerBinder.RequireBinding(service);
}
```

### 3.2 Send

```csharp
/// <summary>
/// 同步发送事件。
/// </summary>
/// <typeparam name="TValue">
/// 事件结构体类型。
/// </typeparam>
/// <param name="service">
/// 当前服务对象。
/// </param>
/// <param name="value">
/// 要发送的事件值。
/// </param>
/// <returns>
/// 事件处理结果。
/// </returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static EventHandledState Send<TValue>(
    this IService service,
    in TValue value)
    where TValue : struct
{
    return service
        .GetBinding()
        .EventCenter
        .Send(value);
}
```

### 3.3 Post

`EventPostPolicy? policy = default` 表示：

```text
未传入 policy：使用 PostScheduler 的默认策略路径。
传入 policy：使用调用方指定策略。
```

```csharp
/// <summary>
/// 投递事件。
/// </summary>
/// <typeparam name="TValue">
/// 事件结构体类型。
/// </typeparam>
/// <param name="service">
/// 当前服务对象。
/// </param>
/// <param name="value">
/// 要投递的事件值。
/// </param>
/// <param name="policy">
/// 本次投递使用的策略。
/// 传入 default 表示使用事件元数据或 Scheduler 默认策略。
/// </param>
/// <returns>
/// PostResult 表示投递结果。
/// 调用方可以忽略该返回值以保持原有调用习惯。
/// </returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static PostResult Post<TValue>(
    this IService service,
    in TValue value,
    EventPostPolicy? policy = default)
    where TValue : struct
{
    var scheduler = service.GetBinding().Runtime.Scheduler;

    return policy.HasValue
        ? scheduler.TryPost(value, policy.Value)
        : scheduler.TryPost(value);
}
```

### 3.4 MarkDirty

```csharp
/// <summary>
/// 标记某种事件为脏。
///
/// DirtySignal 表示只记录“这个事件类型需要刷新一次”，不保存事件负载。
/// </summary>
/// <typeparam name="TValue">
/// 事件结构体类型。
/// </typeparam>
/// <param name="service">
/// 当前服务对象。
/// </param>
/// <returns>
/// PostResult 表示标记结果。
/// </returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static PostResult MarkDirty<TValue>(this IService service)
    where TValue : struct
{
    return service
        .GetBinding()
        .Runtime
        .Scheduler
        .MarkDirty<TValue>();
}
```

### 3.5 PostLatest

```csharp
/// <summary>
/// 以 Latest 模式投递事件。
///
/// Latest 表示同一事件类型只保留最后一次投递的值。
/// </summary>
/// <typeparam name="TValue">
/// 事件结构体类型。
/// </typeparam>
/// <param name="service">
/// 当前服务对象。
/// </param>
/// <param name="value">
/// 要投递的最新事件值。
/// </param>
/// <param name="backpressure">
/// 队列满或无法接收新事件时的背压策略。
/// </param>
/// <param name="capacity">
/// 策略容量参数。
/// 默认 0 表示沿用当前策略约定。
/// </param>
/// <returns>
/// PostResult 表示投递结果。
/// </returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static PostResult PostLatest<TValue>(
    this IService service,
    in TValue value,
    BackpressurePolicy backpressure = BackpressurePolicy.RejectNew,
    int capacity = 0)
    where TValue : struct
{
    return service
        .GetBinding()
        .Runtime
        .Scheduler
        .TryPost(
            value,
            new EventPostPolicy(
                PostDeliveryMode.Latest,
                backpressure,
                capacity));
}
```

### 3.6 PostCoalesced

```csharp
/// <summary>
/// 以 Coalesced 模式投递事件。
///
/// Coalesced 表示多个同类事件可以按合并规则合成一个事件。
/// </summary>
/// <typeparam name="TValue">
/// 事件结构体类型。
/// </typeparam>
/// <param name="service">
/// 当前服务对象。
/// </param>
/// <param name="value">
/// 要投递并尝试合并的事件值。
/// </param>
/// <param name="backpressure">
/// 队列满或无法接收新事件时的背压策略。
/// </param>
/// <param name="capacity">
/// 策略容量参数。
/// 默认 0 表示沿用当前策略约定。
/// </param>
/// <returns>
/// PostResult 表示投递结果。
/// </returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static PostResult PostCoalesced<TValue>(
    this IService service,
    in TValue value,
    BackpressurePolicy backpressure = BackpressurePolicy.RejectNew,
    int capacity = 0)
    where TValue : struct
{
    return service
        .GetBinding()
        .Runtime
        .Scheduler
        .TryPost(
            value,
            new EventPostPolicy(
                PostDeliveryMode.Coalesced,
                backpressure,
                capacity));
}
```

### 3.7 SchedulePost

```csharp
/// <summary>
/// 延迟指定时间后投递事件。
/// </summary>
/// <typeparam name="TValue">
/// 事件结构体类型。
/// </typeparam>
/// <param name="service">
/// 当前服务对象。
/// </param>
/// <param name="value">
/// 到期后要投递的事件值。
/// </param>
/// <param name="delaySeconds">
/// 延迟秒数。
/// </param>
/// <param name="expiredPostPolicy">
/// 定时器到期后使用的 Post 策略。
/// 传入 default 表示使用事件元数据中的 TimerPolicy.ExpiredPostPolicy。
/// </param>
/// <param name="repeatCount">
/// 重复次数。
/// 默认 0 表示只执行一次。
/// </param>
/// <param name="intervalSeconds">
/// 重复执行间隔秒数。
/// repeatCount 大于 0 时生效。
/// </param>
/// <param name="repeatMode">
/// 重复模式。
/// 传入 default 表示使用事件元数据中的 TimerPolicy.RepeatMode。
/// </param>
/// <param name="catchUpPolicy">
/// 补帧策略。
/// 传入 default 表示使用事件元数据中的 TimerPolicy.CatchUpPolicy。
/// </param>
/// <returns>
/// 定时任务句柄。
/// </returns>
public static TimerHandle SchedulePost<TValue>(
    this IService service,
    in TValue value,
    float delaySeconds,
    EventPostPolicy? expiredPostPolicy = default,
    int repeatCount = 0,
    float intervalSeconds = 0,
    TimerRepeatMode? repeatMode = default,
    TimerCatchUpPolicy? catchUpPolicy = default)
    where TValue : struct
{
    var binding = service.GetBinding();
    var runtime = binding.Runtime;
    var eventId = EventTypeId<TValue>.Id;
    var timerPolicy = runtime.PolicyTable.GetTimerPolicy(eventId);

    return runtime.Timer.Schedule(
        new PostEventAction<TValue>(
            value,
            expiredPostPolicy ?? timerPolicy?.ExpiredPostPolicy),
        delaySeconds,
        repeatCount: repeatCount,
        intervalSeconds: intervalSeconds,
        repeatMode: repeatMode ?? timerPolicy?.RepeatMode,
        catchUpPolicy: catchUpPolicy ?? timerPolicy?.CatchUpPolicy);
}
```

### 3.8 Delay

```csharp
/// <summary>
/// 延迟发布事件到 DelayPublisher。
/// </summary>
/// <typeparam name="TValue">
/// 事件结构体类型。
/// </typeparam>
/// <param name="service">
/// 当前服务对象。
/// </param>
/// <param name="value">
/// 要延迟发布的事件值。
/// </param>
/// <param name="ttl">
/// 事件在 Delay 缓冲区中的存活时间，单位为秒。
/// </param>
/// <param name="contractId">
/// 延迟通道 ID。
/// 默认 0 表示默认通道。
/// </param>
public static void Delay<TValue>(
    this IService service,
    in TValue value,
    float ttl,
    int contractId = 0)
    where TValue : struct
{
    service
        .GetBinding()
        .Layer
        .SubscribeDelay<TValue>()
        .Publish(value, ttl, contractId);
}
```

### 3.9 冷路径 API

以下 API 只把 `GetLayer()` 替换为 `GetBinding().Layer`：

```csharp
public static void SubscribeFlow<TValue>(
    this IService service,
    EventHandleDelegate<TValue> handler)
    where TValue : struct
{
    service.GetBinding().Layer.SubscribeFlow(handler);
}

public static void SubscribeAsync<TValue>(
    this IService service,
    EventHandleDelegateAsync<TValue> handler)
    where TValue : struct
{
    service.GetBinding().Layer.SubscribeAsync(handler);
}

public static void Subscribe<TValue>(
    this IService service,
    EventNotifyDelegate<TValue> handler)
    where TValue : struct
{
    service.GetBinding().Layer.Subscribe(handler);
}

public static void SubscribeParallel<TValue>(
    this IService service,
    EventNotifyDelegate<TValue> handler,
    Action<int, string, string, Exception>? reportError = null)
    where TValue : struct
{
    var binding = service.GetBinding();

    binding.Layer.SubscribeParallel(
        handler,
        reportError ?? binding.Runtime.ReportLayerEventError);
}

public static LayerEventStream<TValue> OnEvent<TValue>(
    this IService service)
    where TValue : struct
{
    return service.GetBinding().Layer.OnEvent<TValue>();
}

public static T GetService<T>(this IService service)
    where T : class
{
    return service.GetBinding().Layer.GetService<T>();
}
```

---

## 4. `LayerContextExtensions` 同步修改

对 `ILayerContext` 提供与 `IService` 完全一致的一组 API。

唯一差异是第一个参数类型改为：

```csharp
this ILayerContext context
```

内部统一使用：

```csharp
private static ServiceLayerBinding GetBinding(this ILayerContext context)
{
    return ServiceLayerBinder.RequireBinding(context);
}
```

需要补全的方法列表：

```text
Send<TValue>(in TValue value)
Post<TValue>(in TValue value, EventPostPolicy? policy = default)
MarkDirty<TValue>()
PostLatest<TValue>(in TValue value, BackpressurePolicy backpressure = BackpressurePolicy.RejectNew, int capacity = 0)
PostCoalesced<TValue>(in TValue value, BackpressurePolicy backpressure = BackpressurePolicy.RejectNew, int capacity = 0)
SchedulePost<TValue>(in TValue value, float delaySeconds, EventPostPolicy? expiredPostPolicy = default, int repeatCount = 0, float intervalSeconds = 0, TimerRepeatMode? repeatMode = default, TimerCatchUpPolicy? catchUpPolicy = default)
Delay<TValue>(in TValue value, float ttl, int contractId = 0)
SubscribeFlow<TValue>(EventHandleDelegate<TValue> handler)
SubscribeAsync<TValue>(EventHandleDelegateAsync<TValue> handler)
Subscribe<TValue>(EventNotifyDelegate<TValue> handler)
SubscribeParallel<TValue>(EventNotifyDelegate<TValue> handler, Action<int, string, string, Exception>? reportError = null)
OnEvent<TValue>()
GetService<T>()
```

---

## 5. 本轮不做

```text
不新增 ISend / IPost / IDelay。
不做对象字段注入。
不改源生成器。
不删除扩展方法。
不修改 Build 顺序。
不修改 DelayPublisherManager.Instance。
不修改 ServiceProvider 多世界化之外的行为。
```
