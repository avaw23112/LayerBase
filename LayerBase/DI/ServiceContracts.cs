using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Event.Delay;
using LayerBase.Layers;

namespace LayerBase.DI;

// (Previous interfaces remain unchanged...)
public interface ILayerContext
{
}

public interface IInternalLayerContext : ILayerContext
{
    int LayerIndex { get; set; }
}

public interface IService 
{
    void ConfigureServices(IServiceCollection services);
}

public interface IServiceCollection
{
    IServiceCollection Add(ServiceDescriptor           descriptor);
    IServiceCollection AddSingleton<TService>(TService instance);
    IServiceCollection AddSingleton<TService, TImpl>() where TImpl : TService;
    IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory);
    IServiceCollection AddTransient<TService, TImpl>() where TImpl : TService;
    IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory);
    IServiceCollection AddScoped<TService, TImpl>() where TImpl : TService;
    IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory);
    IReadOnlyList<ServiceDescriptor> ToDescriptors();
}

public interface IServiceProvider
{
    object? GetService(Type serviceType);
    T Get<T>();
}

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

public interface IAutoSubscribe
{
    void AutoBind(Layer layer);
    IEnumerable<EventDependency> GetEventDependencies();
    IEnumerable<Type> GetSubscribedEvents();
}

internal static class ServiceLayerBinder
{
    private static ConditionalWeakTable<object, Layer> s_layerMap = new();

    public static void Reset()
    {
        s_layerMap = new ConditionalWeakTable<object, Layer>();
    }

    public static void Attach(object service, Layer layer)
    {
        if (service == null || layer == null) return;
        s_layerMap.Remove(service);
        s_layerMap.Add(service, layer);
        if (service is IInternalLayerContext internalCtx) internalCtx.LayerIndex = layer.RouteIndex;
    }

    public static Layer Require(object service)
    {
        if (s_layerMap.TryGetValue(service, out var layer)) return layer;
        throw new InvalidOperationException($"Object {service.GetType().Name} is not attached to any Layer.");
    }

    public static int GetIndex(ILayerContext context)
    {
        if (context is IInternalLayerContext internalCtx && internalCtx.LayerIndex != -1) return internalCtx.LayerIndex;
        return Require(context).RouteIndex;
    }
}

public static class ServiceExtensions
{
    private static Layer GetLayer(this IService service)
    {
        return ServiceLayerBinder.Require(service);
    }

    public static EventHandledState SendLocal<TValue>(this IService service, in TValue value) where TValue : struct
    {
        return service.GetLayer().SendLocal(value);
    }

    public static EventHandledState SendGlobal<TValue>(this IService service, in TValue value)
        where TValue : struct
    {
        return service.GetLayer().SendGlobal(value);
    }

    public static EventHandledState SendBubble<TValue>(this IService service, in TValue value)
        where TValue : struct
    {
        return service.GetLayer().SendBubble(value);
    }

    public static EventHandledState SendDrop<TValue>(this IService service, in TValue value) where TValue : struct
    {
        return service.GetLayer().SendDrop(value);
    }

    public static void PostLocal<TValue>(this IService service, in TValue value) where TValue : struct
    {
        service.GetLayer().PostLocal(value);
    }

    public static void PostGlobal<TValue>(this IService service, in TValue value) where TValue : struct
    {
        service.GetLayer().PostGlobal(value);
    }

    public static void PostBubble<TValue>(this IService service, in TValue value) where TValue : struct
    {
        service.GetLayer().PostBubble(value);
    }

    public static void PostDrop<TValue>(this IService service, in TValue value) where TValue : struct
    {
        service.GetLayer().PostDrop(value);
    }

    // 核心修复：通过内部转换调用 Publish
    public static void DelayLocal<TValue>(this IService service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        ((DelayPublisher<TValue>)service.GetLayer().SubscribeDelay<TValue>()).Publish(value, ttl, DelayDirection.Local,
            contractId);
    }

    public static void DelayGlobal<TValue>(this IService service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        ((DelayPublisher<TValue>)service.GetLayer().SubscribeDelay<TValue>()).Publish(value, ttl,
            DelayDirection.BroadCast, contractId);
    }

    public static void DelayBubble<TValue>(this IService service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        ((DelayPublisher<TValue>)service.GetLayer().SubscribeDelay<TValue>()).Publish(value, ttl, DelayDirection.Bubble,
            contractId);
    }

    public static void DelayDrop<TValue>(this IService service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        ((DelayPublisher<TValue>)service.GetLayer().SubscribeDelay<TValue>()).Publish(value, ttl, DelayDirection.Drop,
            contractId);
    }

    public static void Subscribe<TValue>(this IService service, EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().Subscribe(handler);
    }

    public static void SubscribeAsync<TValue>(this IService service, EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().SubscribeAsync(handler);
    }

    public static void SubscribeParallel<TValue>(this IService service, EventHandleDelegate<TValue> handler,
                                                 Action<int, string, string, Exception>? reportError = null)
        where TValue : struct
    {
        service.GetLayer()
               .SubscribeParallel(handler, reportError ?? LayerBase.LayerHub.ReportLayerEventError);
    }

    /// <summary>
    /// 获取针对特定事件的链式 API 流。
    /// </summary>
    public static LayerEventStream<TValue> OnEvent<TValue>(this IService service) where TValue : struct
    {
        return service.GetLayer().OnEvent<TValue>();
    }

    public static T GetService<T>(this IService service) where T : class
    {
        return service.GetLayer().GetService<T>();
    }
}

public static class LayerContextExtensions
{
    private static Layer GetLayer(this ILayerContext service)
    {
        return ServiceLayerBinder.Require(service);
    }

    public static EventHandledState SendLocal<TValue>(this ILayerContext service, in TValue value) where TValue : struct
    {
        return service.GetLayer().SendLocal(value);
    }

    public static EventHandledState SendGlobal<TValue>(this ILayerContext service, in TValue value)
        where TValue : struct
    {
        return service.GetLayer().SendGlobal(value);
    }

    public static EventHandledState SendBubble<TValue>(this ILayerContext service, in TValue value)
        where TValue : struct
    {
        return service.GetLayer().SendBubble(value);
    }

    public static EventHandledState SendDrop<TValue>(this ILayerContext service, in TValue value) where TValue : struct
    {
        return service.GetLayer().SendDrop(value);
    }

    public static void PostLocal<TValue>(this ILayerContext service, in TValue value) where TValue : struct
    {
        service.GetLayer().PostLocal(value);
    }

    public static void PostGlobal<TValue>(this ILayerContext service, in TValue value) where TValue : struct
    {
        service.GetLayer().PostGlobal(value);
    }

    public static void PostBubble<TValue>(this ILayerContext service, in TValue value) where TValue : struct
    {
        service.GetLayer().PostBubble(value);
    }

    public static void PostDrop<TValue>(this ILayerContext service, in TValue value) where TValue : struct
    {
        service.GetLayer().PostDrop(value);
    }

    // 核心修复：通过内部转换调用 Publish
    public static void DelayLocal<TValue>(this ILayerContext service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        ((DelayPublisher<TValue>)service.GetLayer().SubscribeDelay<TValue>()).Publish(value, ttl, DelayDirection.Local,
            contractId);
    }

    public static void DelayGlobal<TValue>(this ILayerContext service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        ((DelayPublisher<TValue>)service.GetLayer().SubscribeDelay<TValue>()).Publish(value, ttl,
            DelayDirection.BroadCast, contractId);
    }

    public static void DelayBubble<TValue>(this ILayerContext service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        ((DelayPublisher<TValue>)service.GetLayer().SubscribeDelay<TValue>()).Publish(value, ttl, DelayDirection.Bubble,
            contractId);
    }

    public static void DelayDrop<TValue>(this ILayerContext service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        ((DelayPublisher<TValue>)service.GetLayer().SubscribeDelay<TValue>()).Publish(value, ttl, DelayDirection.Drop,
            contractId);
    }

    public static void Subscribe<TValue>(this ILayerContext service, EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().Subscribe(handler);
    }

    public static void SubscribeAsync<TValue>(this ILayerContext service, EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().SubscribeAsync(handler);
    }

    public static void SubscribeParallel<TValue>(this ILayerContext service, EventHandleDelegate<TValue> handler,
                                                 Action<int, string, string, Exception>? reportError = null)
        where TValue : struct
    {
        service.GetLayer()
               .SubscribeParallel(handler, reportError ?? LayerBase.LayerHub.ReportLayerEventError);
    }

    /// <summary>
    /// 获取针对特定事件的链式 API 流。
    /// </summary>
    public static LayerEventStream<TValue> OnEvent<TValue>(this ILayerContext service) where TValue : struct
    {
        return service.GetLayer().OnEvent<TValue>();
    }

    public static T GetService<T>(this ILayerContext service) where T : class
    {
        return service.GetLayer().GetService<T>();
    }
}
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InjectAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class OwnerLayerAttribute : Attribute
{
    public OwnerLayerAttribute(Type layerType)
    {
        LayerType = layerType;
    }

    public Type LayerType { get; }
}