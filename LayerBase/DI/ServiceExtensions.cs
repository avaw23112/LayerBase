using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;

namespace LayerBase.DI;

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
        in   TValue   value)
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
        this IService    service,
        in   TValue      value,
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
        this IService      service,
        in   TValue        value,
        BackpressurePolicy backpressure = BackpressurePolicy.RejectNew,
        int                capacity     = 0)
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
        this IService      service,
        in   TValue        value,
        BackpressurePolicy backpressure = BackpressurePolicy.RejectNew,
        int                capacity     = 0)
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
        this IService       service,
        in   TValue         value,
        float               delaySeconds,
        EventPostPolicy?    expiredPostPolicy = default,
        int                 repeatCount       = 0,
        float               intervalSeconds   = 0,
        TimerRepeatMode?    repeatMode        = default,
        TimerCatchUpPolicy? catchUpPolicy     = default)
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
        in   TValue   value,
        float         ttl,
        int           contractId = 0)
        where TValue : struct
    {
        ServiceLayerBinder
            .RequireLayer(service.GetBinding())
            .SubscribeDelay<TValue>()
            .Publish(value, ttl, contractId);
    }

    public static void PostTo<TEvent>(
        this IService service,
        ActorId       actorId,
        in TEvent     value)
        where TEvent : struct
    {
         service
               .GetBinding()
               .Runtime.PostTo(actorId, in value);
    }

    public static void PostToMany<TEvent>(
        this IService         service,
        ReadOnlySpan<ActorId> actorIds,
        in TEvent             value)
        where TEvent : struct
    {
        service
            .GetBinding()
            .Runtime.PostToMany(actorIds, in value);
    }

    public static void SubscribeFlow<TValue>(
        this IService               service,
        EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(service.GetBinding()).SubscribeFlow(handler);
    }

    public static void SubscribeAsync<TValue>(
        this IService                    service,
        EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(service.GetBinding()).SubscribeAsync(handler);
    }

    public static void Subscribe<TValue>(
        this IService               service,
        EventNotifyDelegate<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(service.GetBinding()).Subscribe(handler);
    }

    public static void SubscribeParallel<TValue>(
        this IService               service,
        EventNotifyDelegate<TValue> handler)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        in   TValue        value)
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
        in   TValue        value,
        EventPostPolicy?   policy = default)
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
        in   TValue        value,
        BackpressurePolicy backpressure = BackpressurePolicy.RejectNew,
        int                capacity     = 0)
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
        in   TValue        value,
        BackpressurePolicy backpressure = BackpressurePolicy.RejectNew,
        int                capacity     = 0)
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
        this ILayerContext  context,
        in   TValue         value,
        float               delaySeconds,
        EventPostPolicy?    expiredPostPolicy = default,
        int                 repeatCount       = 0,
        float               intervalSeconds   = 0,
        TimerRepeatMode?    repeatMode        = default,
        TimerCatchUpPolicy? catchUpPolicy     = default)
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
        in   TValue        value,
        float              ttl,
        int                contractId = 0)
        where TValue : struct
    {
        ServiceLayerBinder
            .RequireLayer(context.GetBinding())
            .SubscribeDelay<TValue>()
            .Publish(value, ttl, contractId);
    }

    public static void PostTo<TEvent>(
        this ILayerContext context,
        ActorId            actorId,
        in TEvent          value)
        where TEvent : struct
    {
         context
               .GetBinding()
               .Runtime.PostTo(actorId, in value);
    }

    public static void PostToMany<TEvent>(
        this ILayerContext    context,
        ReadOnlySpan<ActorId> actorIds,
        in TEvent             value)
        where TEvent : struct
    {
        context
            .GetBinding()
            .Runtime.PostToMany(actorIds, in value);
    }

    public static void SubscribeFlow<TValue>(
        this ILayerContext          context,
        EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(context.GetBinding()).SubscribeFlow(handler);
    }

    public static void SubscribeAsync<TValue>(
        this ILayerContext               context,
        EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(context.GetBinding()).SubscribeAsync(handler);
    }

    public static void Subscribe<TValue>(
        this ILayerContext          context,
        EventNotifyDelegate<TValue> handler)
        where TValue : struct
    {
        ServiceLayerBinder.RequireLayer(context.GetBinding()).Subscribe(handler);
    }

    public static void SubscribeParallel<TValue>(
        this ILayerContext          context,
        EventNotifyDelegate<TValue> handler)
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

    public static T Get<T>(this ILayerContext context)
        where T : class
    {
        return context.GetService<T>();
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
