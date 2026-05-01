using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Event.Delay;
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
    IServiceCollection Add(ServiceDescriptor           descriptor);
    IServiceCollection AddSingleton<TService>(TService instance);
    IServiceCollection AddTransient<TService, TImpl>() where TImpl : TService;
    IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory);
    IServiceCollection AddScoped<TService, TImpl>() where TImpl : TService;
    IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory);
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
        s_layerMap.AddOrUpdate(service, layer);
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

    public static EventHandledState Send<TValue>(this IService service, in TValue value)
        where TValue : struct
    {
        return service.GetLayer().Send(value);
    }

    public static void Post<TValue>(this IService service, in TValue value) where TValue : struct
    {
        service.GetLayer().Post(value);
    }

    public static void Delay<TValue>(this IService service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        service.GetLayer().SubscribeDelay<TValue>().Publish(value, ttl, contractId);
    }

    public static void SubscribeFlow<TValue>(this IService service, EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().SubscribeFlow(handler);
    }

    public static void SubscribeAsync<TValue>(this IService service, EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().SubscribeAsync(handler);
    }

    public static void Subscribe<TValue>(this IService service, EventNotifyDelegate<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().Subscribe(handler);
    }

    public static void SubscribeParallel<TValue>(this IService service, EventNotifyDelegate<TValue> handler,
                                                 Action<int, int, int, Exception>? reportError = null)
        where TValue : struct
    {
        service.GetLayer()
               .SubscribeParallel(handler, reportError);
    }


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

    public static EventHandledState Send<TValue>(this ILayerContext service, in TValue value)
        where TValue : struct
    {
        return service.GetLayer().Send(value);
    }

    public static void Post<TValue>(this ILayerContext service, in TValue value) where TValue : struct
    {
        service.GetLayer().Post(value);
    }

    public static void Delay<TValue>(this ILayerContext service, in TValue value, float ttl, int contractId = 0)
        where TValue : struct
    {
        service.GetLayer().SubscribeDelay<TValue>().Publish(value, ttl, contractId);
    }

    public static void SubscribeFlow<TValue>(this ILayerContext service, EventHandleDelegate<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().SubscribeFlow(handler);
    }

    public static void SubscribeAsync<TValue>(this ILayerContext service, EventHandleDelegateAsync<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().SubscribeAsync(handler);
    }

    public static void Subscribe<TValue>(this ILayerContext service, EventNotifyDelegate<TValue> handler)
        where TValue : struct
    {
        service.GetLayer().Subscribe(handler);
    }

    public static void SubscribeParallel<TValue>(this ILayerContext service, EventNotifyDelegate<TValue> handler,
                                                 Action<int, int, int, Exception>? reportError = null)
        where TValue : struct
    {
        service.GetLayer()
               .SubscribeParallel(handler, reportError);
    }

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
public sealed class MountAttribute : Attribute
{
}