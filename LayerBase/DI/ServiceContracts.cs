using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;

namespace LayerBase.DI;

/// <summary>
/// 表示一个分层系统的上下文，用于绑定服务与 Layer。
/// </summary>
public interface ILayerContext
{
}

public interface IInternalLayerContext : ILayerContext
{
    int LayerIndex { get; set; }
}

/// <summary>
/// 由 LayerBase 源生成器自动实现的隐藏绑定接口。
///
/// 作用：
/// 让 IService / ILayerContext 实例自身携带 Layer 绑定信息，
/// 避免 Send、Post、Subscribe 等高频扩展方法每次都进入 ConditionalWeakTable。
/// </summary>
public interface ILayerBindingAccessor
{
    /// <summary>
    /// 当前对象的 Layer 绑定信息。
    /// </summary>
    object? __LayerBaseBinding { get; set; }
}

/// <summary>
/// 由生成器为 Layer 实现的自动挂载接口。
/// </summary>
public interface IAutoLayerMount
{
    void __AutoMountServices(Layers.Layer layer);
}

/// <summary>
/// 服务接口，用于配置分层 DI 容器。
/// </summary>
public interface IService 
{
    /// <summary>
    /// 配置服务集合。
    /// </summary>
    /// <param name="services">服务集合。</param>
    void ConfigureServices(IServiceCollection services);
}

/// <summary>
/// 服务集合，用于注册和管理 DI 服务。
/// </summary>
public interface IServiceCollection
{
    IServiceCollection Add(ServiceDescriptor descriptor);

    IServiceCollection AddSingleton<TService>(TService instance);

    IServiceCollection AddSingleton<TService, TImpl>()
        where TImpl : TService;

    IServiceCollection AddSingleton<TService>(
        Func<IServiceProvider, TService> factory);

    IServiceCollection AddTransient<TService, TImpl>()
        where TImpl : TService;

    IServiceCollection AddTransient<TService>(
        Func<IServiceProvider, TService> factory);

    IServiceCollection AddScoped<TService, TImpl>()
        where TImpl : TService;

    IServiceCollection AddScoped<TService>(
        Func<IServiceProvider, TService> factory);

    IReadOnlyList<ServiceDescriptor> ToDescriptors();
}

/// <summary>
/// 服务提供器，用于从 Layer 解析服务。
/// </summary>
public interface IServiceProvider
{
    /// <summary>
    /// 获取指定类型的服务实例。
    /// </summary>
    object? GetService(Type serviceType);
    
    /// <summary>
    /// 获取指定类型的服务实例。
    /// </summary>
    T Get<T>();
}

/// <summary>
/// 表示事件之间的依赖关系。
/// </summary>
public readonly struct EventDependency
{
    public readonly Type Source;
    public readonly Type Target;

    public EventDependency(Type source, Type target)
    {
        Source = source;
        Target = target;
    }
}

/// <summary>
/// 支持自动订阅的接口，通过此接口可以自动绑定事件与处理逻辑。
/// </summary>
public interface IAutoSubscribe
{
    void AutoBind(Layer layer);
    IEnumerable<EventDependency> GetEventDependencies();
    IEnumerable<Type> GetSubscribedEvents();
}

/// <summary>
/// 服务对象与某个 LayerRuntime 的绑定信息。
///
/// 该对象保存 service / manager / handler 所属的 Layer、Runtime、EventCenter。
/// 扩展方法拿到它之后，可以直接访问对应运行时能力。
/// </summary>
internal sealed class ServiceLayerBinding
{
    /// <summary>
    /// 当前绑定版本。
    ///
    /// ServiceLayerBinder.Reset() 会递增全局版本号。
    /// 对象自身字段里的旧绑定无法被统一清空，
    /// 因此需要通过 Version 判断该绑定是否仍然有效。
    /// </summary>
    public readonly int Version;

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
    public readonly Layer? Layer;

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
    /// <param name="version">
    /// 当前 ServiceLayerBinder 的绑定版本号。
    /// 用于 Reset 后识别旧绑定。
    /// </param>
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
        int version,
        int runtimeId,
        int layerIndex,
        Layer? layer,
        LayerRuntime runtime)
    {
        Version = version;
        RuntimeId = runtimeId;
        LayerIndex = layerIndex;
        Layer = layer;
        Runtime = runtime;
        EventCenter = runtime.EventCenter;
    }
}

/// <summary>
/// 服务对象与 LayerRuntime 的绑定器。
///
/// 设计目标：
/// 1. 支持多世界绑定。
/// 2. 优先读取对象自身的绑定槽位，避免热路径查表。
/// 3. 保留 ConditionalWeakTable 作为兜底，兼容未被源生成器增强的对象。
/// </summary>
internal static class ServiceLayerBinder
{
    /// <summary>
    /// 兜底绑定表。
    ///
    /// 只有对象没有实现 ILayerBindingAccessor 时才使用。
    /// key 是 service / manager / handler 实例。
    /// value 是该对象所属 Runtime 与 Layer 的绑定信息。
    /// </summary>
    private static ConditionalWeakTable<object, ServiceLayerBinding> s_bindingMap = new();

    /// <summary>
    /// 当前绑定版本号。
    ///
    /// Reset 会替换 ConditionalWeakTable。
    /// 但对象自身字段中的旧绑定无法被统一清空。
    /// 所以这里用版本号让旧绑定失效。
    /// </summary>
    private static int s_version;

    /// <summary>
    /// 重置绑定表。
    /// </summary>
    public static void Reset()
    {
        s_bindingMap = new ConditionalWeakTable<object, ServiceLayerBinding>();

        unchecked
        {
            s_version++;
        }
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
        AttachLayer(service, layer);
    }

    public static void AttachRuntime(object service, LayerRuntime runtime)
    {
        if (service == null)
        {
            return;
        }

        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        var binding = new ServiceLayerBinding(
            version: s_version,
            runtimeId: runtime.Id,
            layerIndex: -1,
            layer: null,
            runtime: runtime);

        ApplyBinding(service, binding);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = -1;
        }
    }

    public static void AttachLayer(object service, Layer layer)
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
            version: s_version,
            runtimeId: runtime.Id,
            layerIndex: layer.RouteIndex,
            layer: layer,
            runtime: runtime);

        ApplyBinding(service, binding);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = layer.RouteIndex;
        }
    }

    public static bool IsBoundToLayer(object service, LayerRuntime runtime)
    {
        var binding = GetBinding(service);
        return binding != null && binding.Layer != null && binding.RuntimeId == runtime.Id;
    }

    public static bool HasLayerBinding(object service)
    {
        var binding = GetBinding(service);
        return binding != null && binding.Layer != null;
    }

    public static ServiceLayerBinding? GetBinding(object service)
    {
        if (service is ILayerBindingAccessor accessor &&
            accessor.__LayerBaseBinding is ServiceLayerBinding binding &&
            binding.Version == s_version)
        {
            return binding;
        }

        if (s_bindingMap.TryGetValue(service, out binding) &&
            binding.Version == s_version)
        {
            return binding;
        }

        return null;
    }

    private static void ApplyBinding(object service, ServiceLayerBinding binding)
    {
        if (service is ILayerBindingAccessor accessor)
        {
            accessor.__LayerBaseBinding = binding;
        }
        else
        {
            s_bindingMap.Remove(service);
            s_bindingMap.Add(service, binding);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ServiceLayerBinding RequireBinding(object service)
    {
        if (service is ILayerBindingAccessor accessor &&
            accessor.__LayerBaseBinding is ServiceLayerBinding binding &&
            binding.Version == s_version)
        {
            return binding;
        }

        return RequireBindingSlow(service);
    }

    /// <summary>
    /// 慢路径绑定查找。
    /// </summary>
    /// <param name="service">
    /// 已绑定到 Layer 的对象。
    /// </param>
    /// <returns>
    /// 该对象所属 Runtime 与 Layer 的绑定信息。
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ServiceLayerBinding RequireBindingSlow(object service)
    {
        if (s_bindingMap.TryGetValue(service, out var binding) &&
            binding.Version == s_version)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Layer Require(object service)
    {
        return RequireLayer(RequireBinding(service));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Layer RequireLayer(ServiceLayerBinding binding)
    {
        return binding.Layer ?? ThrowRuntimeOnlyLayerRequired();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Layer ThrowRuntimeOnlyLayerRequired()
    {
        throw new InvalidOperationException(
            "This service is bound to Runtime, not to a specific Layer. Layer-only API is unavailable.");
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

public static class ServiceExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ServiceLayerBinding GetBinding(this IService service)
    {
        return ServiceLayerBinder.RequireBinding(service);
    }

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
        ServiceLayerBinder
            .RequireLayer(service.GetBinding())
            .SubscribeDelay<TValue>()
            .Publish(value, ttl, contractId);
    }

    public static void SubscribeFlow<TValue>(
        this IService service,
        EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(service.GetBinding()).SubscribeFlow(handler);
    }

    public static void SubscribeAsync<TValue>(
        this IService service,
        EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(service.GetBinding()).SubscribeAsync(handler);
    }

    public static void Subscribe<TValue>(
        this IService service,
        EventNotifyDelegate<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(service.GetBinding()).Subscribe(handler);
    }

    public static void SubscribeParallel<TValue>(
        this IService                     service,
        EventNotifyDelegate<TValue>       handler)
        where TValue : struct
    {
        var binding = service.GetBinding();

        ServiceLayerBinder.RequireLayer(binding).SubscribeParallel(
            handler,
            binding.Runtime.ReportLayerEventError);
    }

    public static LayerEventStream<TValue> OnEvent<TValue>(
        this IService service)
        where TValue : struct
    {
        return ServiceLayerBinder.RequireLayer(service.GetBinding()).OnEvent<TValue>();
    }

    public static T GetService<T>(this IService service)
        where T : class
    {
        var binding = service.GetBinding();
        if (binding.Layer != null)
        {
            return binding.Layer.GetService<T>();
        }

        return binding.Runtime.GetService<T>();
    }
}

public static class LayerContextExtensions
{
    private static ServiceLayerBinding GetBinding(this ILayerContext context)
    {
        return ServiceLayerBinder.RequireBinding(context);
    }

    /// <summary>
    /// 同步发送事件。
    /// </summary>
    /// <typeparam name="TValue">
    /// 事件结构体类型。
    /// </typeparam>
    /// <param name="context">
    /// 当前上下文对象。
    /// </param>
    /// <param name="value">
    /// 要发送的事件值。
    /// </param>
    /// <returns>
    /// 事件处理结果。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventHandledState Send<TValue>(
        this ILayerContext context,
        in TValue value)
        where TValue : struct
    {
        return context
            .GetBinding()
            .EventCenter
            .Send(value);
    }

    /// <summary>
    /// 投递事件。
    /// </summary>
    /// <typeparam name="TValue">
    /// 事件结构体类型。
    /// </typeparam>
    /// <param name="context">
    /// 当前上下文对象。
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
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult Post<TValue>(
        this ILayerContext context,
        in TValue value,
        EventPostPolicy? policy = default)
        where TValue : struct
    {
        var scheduler = context.GetBinding().Runtime.Scheduler;

        return policy.HasValue
            ? scheduler.TryPost(value, policy.Value)
            : scheduler.TryPost(value);
    }

    /// <summary>
    /// 标记某种事件为脏。
    /// </summary>
    /// <typeparam name="TValue">
    /// 事件结构体类型。
    /// </typeparam>
    /// <param name="context">
    /// 当前上下文对象。
    /// </param>
    /// <returns>
    /// PostResult 表示标记结果。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult MarkDirty<TValue>(this ILayerContext context)
        where TValue : struct
    {
        return context
            .GetBinding()
            .Runtime
            .Scheduler
            .MarkDirty<TValue>();
    }

    /// <summary>
    /// 以 Latest 模式投递事件。
    /// </summary>
    /// <typeparam name="TValue">
    /// 事件结构体类型。
    /// </typeparam>
    /// <param name="context">
    /// 当前上下文对象。
    /// </param>
    /// <param name="value">
    /// 要投递的最新事件值。
    /// </param>
    /// <param name="backpressure">
    /// 背压策略。
    /// </param>
    /// <param name="capacity">
    /// 容量。
    /// </param>
    /// <returns>
    /// PostResult 表示投递结果。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult PostLatest<TValue>(
        this ILayerContext context,
        in TValue value,
        BackpressurePolicy backpressure = BackpressurePolicy.RejectNew,
        int capacity = 0)
        where TValue : struct
    {
        return context
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

    /// <summary>
    /// 以 Coalesced 模式投递事件。
    /// </summary>
    /// <typeparam name="TValue">
    /// 事件结构体类型。
    /// </typeparam>
    /// <param name="context">
    /// 当前上下文对象。
    /// </param>
    /// <param name="value">
    /// 要投递并尝试合并的事件值。
    /// </param>
    /// <param name="backpressure">
    /// 背压策略。
    /// </param>
    /// <param name="capacity">
    /// 容量。
    /// </param>
    /// <returns>
    /// PostResult 表示投递结果。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult PostCoalesced<TValue>(
        this ILayerContext context,
        in TValue value,
        BackpressurePolicy backpressure = BackpressurePolicy.RejectNew,
        int capacity = 0)
        where TValue : struct
    {
        return context
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

    /// <summary>
    /// 延迟指定时间后投递事件。
    /// </summary>
    public static TimerHandle SchedulePost<TValue>(
        this ILayerContext context,
        in TValue value,
        float delaySeconds,
        EventPostPolicy? expiredPostPolicy = default,
        int repeatCount = 0,
        float intervalSeconds = 0,
        TimerRepeatMode? repeatMode = default,
        TimerCatchUpPolicy? catchUpPolicy = default)
        where TValue : struct
    {
        var binding = context.GetBinding();
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

    /// <summary>
    /// 延迟发布事件到 DelayPublisher。
    /// </summary>
    public static void Delay<TValue>(
        this ILayerContext context,
        in TValue value,
        float ttl,
        int contractId = 0)
        where TValue : struct
    {
        ServiceLayerBinder
            .RequireLayer(context.GetBinding())
            .SubscribeDelay<TValue>()
            .Publish(value, ttl, contractId);
    }

    public static void SubscribeFlow<TValue>(
        this ILayerContext context,
        EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(context.GetBinding()).SubscribeFlow(handler);
    }

    public static void SubscribeAsync<TValue>(
        this ILayerContext context,
        EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(context.GetBinding()).SubscribeAsync(handler);
    }

    public static void Subscribe<TValue>(
        this ILayerContext context,
        EventNotifyDelegate<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(context.GetBinding()).Subscribe(handler);
    }

    public static void SubscribeParallel<TValue>(
        this ILayerContext                context,
        EventNotifyDelegate<TValue>       handler)
        where TValue : struct
    {
        var binding = context.GetBinding();

        ServiceLayerBinder.RequireLayer(binding).SubscribeParallel(
            handler,
            binding.Runtime.ReportLayerEventError);
    }

    public static LayerEventStream<TValue> OnEvent<TValue>(
        this ILayerContext context)
        where TValue : struct
    {
        return ServiceLayerBinder.RequireLayer(context.GetBinding()).OnEvent<TValue>();
    }
    
    public static T GetService<T>(this ILayerContext context)
        where T : class
    {
        var binding = context.GetBinding();
        if (binding.Layer != null)
        {
            return binding.Layer.GetService<T>();
        }

        return binding.Runtime.GetService<T>();
    }
}

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class MountAttribute : Attribute
{
}
