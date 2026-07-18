using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Event.Delay;
using LayerBase.Layers;
using LayerBase.Scope;
using LayerBase.Worker;

namespace LayerBase.DI;

public static class ServiceExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ServiceLayerBinding GetBinding(this IService service)
    {
        return ServiceLayerBinder.RequireBinding(service);
    }

    internal static PostScheduler RequireOwnerScheduler(ServiceLayerBinding binding)
    {
        return binding.OwnerScope.PostScheduler
               ?? throw new InvalidOperationException("Owner scope scheduler is not built.");
    }

    internal static PostTimerScheduler RequireOwnerTimer(ServiceLayerBinding binding)
    {
        return binding.OwnerScope.Timer
               ?? throw new InvalidOperationException("Owner scope timer is not built.");
    }

    internal static EventBuildPolicyTable RequireOwnerPolicyTable(ServiceLayerBinding binding)
    {
        return binding.OwnerScope.PolicyTable
               ?? throw new InvalidOperationException("Owner scope policy table is not built.");
    }

    internal static IDelayPublisher<TValue> RequireOwnerDelayPublisher<TValue>(ServiceLayerBinding binding)
        where TValue : struct
    {
        if (binding.Layer != null && binding.OwnerScope.ScopeId == ScopeDefinitionIds.Main)
            return binding.Layer.SubscribeDelay<TValue>();

        return binding.OwnerScope.SubscribeDelay<TValue>();
    }


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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult Post<TValue>(
        this IService    service,
        in   TValue      value)
        where TValue : struct
    {
        var scheduler = RequireOwnerScheduler(service.GetBinding());
        return scheduler.TryPost(value);
    }

    public static TimerHandle SchedulePost<TValue>(
        this IService service,
        in   TValue   value,
        float         delaySeconds,
        int           repeatCount     = 0,
        float         intervalSeconds = 0)
        where TValue : struct
    {
        return RequireOwnerTimer(service.GetBinding()).Schedule(
            in value,
            delaySeconds,
            repeatCount,
            intervalSeconds);
    }

    public static void Delay<TValue>(
        this IService service,
        in   TValue   value,
        float         ttl,
        int           contractId = 0)
        where TValue : struct
    {
        RequireOwnerDelayPublisher<TValue>(service.GetBinding())
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
            .OwnerScope
            .ActorClient
            .Post(actorId, in value);
    }

    public static void PostToMany<TEvent>(
        this IService         service,
        ReadOnlySpan<ActorId> actorIds,
        in TEvent             value)
        where TEvent : struct
    {
        service
            .GetBinding()
            .OwnerScope
            .ActorClient
            .PostMany(actorIds, in value);
    }

    public static WorkerJobAccessor WorkerJobs(this IService service)
    {
        ServiceLayerBinding binding = service.GetBinding();

        return new WorkerJobAccessor(binding.OwnerScope.WorkerJobs);
    }

    public static void SubscribeFlow<TValue>(
        this IService               service,
        EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        var binding = service.GetBinding();
        ServiceLayerBinder.RequireLayer(binding).SubscribeFlow(handler, binding.OwnerScope);
    }

    public static void SubscribeAsync<TValue>(
        this IService                    service,
        EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        var binding = service.GetBinding();
        ServiceLayerBinder.RequireLayer(binding).SubscribeAsync(handler, binding.OwnerScope);
    }

    public static void Subscribe<TValue>(
        this IService               service,
        EventNotifyDelegate<TValue> handler)
        where TValue : struct
    {
        var binding = service.GetBinding();
        ServiceLayerBinder.RequireLayer(binding).Subscribe(handler, binding.OwnerScope);
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
            return binding.Layer.GetService<T>(binding.OwnerScope.ScopeId);
        }

        return ServiceLayerBinder.RequireLayer(binding).GetService<T>(binding.OwnerScope.ScopeId);
    }
}

public static class LayerContextExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ServiceLayerBinding GetBinding(this ILayerContext context)
    {
        return ServiceLayerBinder.RequireBinding(context);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult Post<TValue>(
        this ILayerContext context,
        in   TValue        value)
        where TValue : struct
    {
        var scheduler = ServiceExtensions.RequireOwnerScheduler(context.GetBinding());
        return scheduler.TryPost(value);
    }

    public static TimerHandle SchedulePost<TValue>(
        this ILayerContext context,
        in   TValue        value,
        float              delaySeconds,
        int                repeatCount     = 0,
        float              intervalSeconds = 0)
        where TValue : struct
    {
        return ServiceExtensions.RequireOwnerTimer(context.GetBinding()).Schedule(
            in value,
            delaySeconds,
            repeatCount,
            intervalSeconds);
    }

    public static void Delay<TValue>(
        this ILayerContext context,
        in   TValue        value,
        float              ttl,
        int                contractId = 0)
        where TValue : struct
    {
        ServiceExtensions.RequireOwnerDelayPublisher<TValue>(context.GetBinding())
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
            .OwnerScope
            .ActorClient
            .Post(actorId, in value);
    }

    public static void PostToMany<TEvent>(
        this ILayerContext    context,
        ReadOnlySpan<ActorId> actorIds,
        in TEvent             value)
        where TEvent : struct
    {
        context
            .GetBinding()
            .OwnerScope
            .ActorClient
            .PostMany(actorIds, in value);
    }

    public static WorkerJobAccessor WorkerJobs(this ILayerContext context)
    {
        ServiceLayerBinding binding = context.GetBinding();

        return new WorkerJobAccessor(binding.OwnerScope.WorkerJobs);
    }

    public static void SubscribeFlow<TValue>(
        this ILayerContext          context,
        EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        var binding = context.GetBinding();
        ServiceLayerBinder.RequireLayer(binding).SubscribeFlow(handler, binding.OwnerScope);
    }

    public static void SubscribeAsync<TValue>(
        this ILayerContext               context,
        EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        var binding = context.GetBinding();
        ServiceLayerBinder.RequireLayer(binding).SubscribeAsync(handler, binding.OwnerScope);
    }

    public static void Subscribe<TValue>(
        this ILayerContext          context,
        EventNotifyDelegate<TValue> handler)
        where TValue : struct
    {
        var binding = context.GetBinding();
        ServiceLayerBinder.RequireLayer(binding).Subscribe(handler, binding.OwnerScope);
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
            return binding.Layer.GetService<T>(binding.OwnerScope.ScopeId);
        }

        return ServiceLayerBinder.RequireLayer(binding).GetService<T>(binding.OwnerScope.ScopeId);
    }
}

