using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Event.Delay;
using LayerBase.Layers;

namespace LayerBase.DI
{
    public enum ServiceLifetime
    {
        Singleton,
        Instance,
        Transient,
        Scoped
    }

    public interface IServiceCollection
    {
        IServiceCollection Add(ServiceDescriptor descriptor);
        IServiceCollection AddSingleton<TService>(TService instance);
        IServiceCollection AddSingleton<TService, TImpl>() where TImpl : TService;
        IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory);
        IServiceCollection AddTransient<TService, TImpl>() where TImpl : TService;
        IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory);
        IServiceCollection AddScoped<TService, TImpl>() where TImpl : TService;
        IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory);
        IReadOnlyList<ServiceDescriptor> ToDescriptors();
        void Reset();
    }

    /// <summary>
    /// 能力标记接口：只要实现此接口，即可通过扩展方法获得 Layer 级的事件分发能力。
    /// </summary>
    public interface ILayerContext { }

    /// <summary>
    /// 内部契约：用于 DI 容器自动注入层级上下文信息，避开字典查找。
    /// </summary>
    public interface IInternalLayerContext : ILayerContext
    {
        int LayerIndex { get; set; }
    }

    public interface IService : ILayerContext
    {
        void ConfigureServices(IServiceCollection services);
    }

    /// <summary>
    /// 事件依赖关系，描述处理 Source 事件时可能会同步触发 Target 事件。
    /// </summary>
    public readonly struct EventDependency
    {
        public readonly Type Source;
        public readonly Type Target;
        public EventDependency(Type source, Type target) { Source = source; Target = target; }
    }

    /// <summary>
    /// 自动订阅行为接口：由 Source Generator 自动生成，用于在构建期执行订阅连线与依赖审计。
    /// </summary>
    public interface IAutoSubscribe
    {
        void AutoBind(Layer layer);

        /// <summary>
        /// 获取当前组件中声明的同步事件依赖关系（用于启动期环路审计）。
        /// </summary>
        IEnumerable<EventDependency> GetEventDependencies();
    }

    internal static class ServiceLayerBinder
    {
        private static readonly ConditionalWeakTable<object, Layer> s_layerMap = new();

        internal static void Attach(object instance, Layer layer)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (layer == null) throw new ArgumentNullException(nameof(layer));

            if (instance is IInternalLayerContext internalCtx)
            {
                internalCtx.LayerIndex = layer.RouteIndex;
            }

            lock (s_layerMap)
            {
                s_layerMap.Remove(instance);
                s_layerMap.Add(instance, layer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetIndex(ILayerContext instance)
        {
            if (instance is IInternalLayerContext internalCtx) return internalCtx.LayerIndex;
            
            if (s_layerMap.TryGetValue(instance, out var layer)) return layer.RouteIndex;
            return -1;
        }

        internal static Layer Require(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (s_layerMap.TryGetValue(instance, out var layer))
            {
                return layer;
            }

            throw new InvalidOperationException($"Instance of {instance.GetType().Name} is not attached to a Layer.");
        }
    }

    public static class ServiceExtensions
    {
        public static Layer GetLayer(this ILayerContext self) => ServiceLayerBinder.Require(self);

        // ----------------- Events Dispatch (Synchronous) -----------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventHandledState SendLocal<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            return LayerBase.LayerHub.LayerHub.EventCenter.SendLocal(ServiceLayerBinder.GetIndex(self), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendBubble<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            LayerBase.LayerHub.LayerHub.EventCenter.Send(value, ServiceLayerBinder.GetIndex(self), Propagation.Bubble);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendDrop<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            LayerBase.LayerHub.LayerHub.EventCenter.Send(value, ServiceLayerBinder.GetIndex(self), Propagation.Drop);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendGlobal<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            LayerBase.LayerHub.LayerHub.EventCenter.Send(value, 0, Propagation.Global);
        }

        // ----------------- Events Dispatch (Asynchronous) -----------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PostLocal<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            LayerBase.LayerHub.LayerHub.EventCenter.PostLocal(ServiceLayerBinder.GetIndex(self), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PostBubble<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            LayerBase.LayerHub.LayerHub.EventCenter.Post(value, ServiceLayerBinder.GetIndex(self), Propagation.Bubble);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PostDrop<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            LayerBase.LayerHub.LayerHub.EventCenter.Post(value, ServiceLayerBinder.GetIndex(self), Propagation.Drop);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PostGlobal<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            LayerBase.LayerHub.LayerHub.EventCenter.Post(value, 0, Propagation.Global);
        }

        // ----------------- Delay Events -----------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayLocal<TValue>(this ILayerContext self, in TValue value, float ttlSeconds, int contractLayer = 0) where TValue : struct
        {
            ServiceLayerBinder.Require(self).DelayLocal(in value, ttlSeconds, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayGlobal<TValue>(this ILayerContext self, in TValue value, float ttlSeconds, int contractLayer = 0) where TValue : struct
        {
            ServiceLayerBinder.Require(self).DelayGlobal(in value, ttlSeconds, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayBubble<TValue>(this ILayerContext self, in TValue value, float ttlSeconds, int contractLayer = 0) where TValue : struct
        {
            ServiceLayerBinder.Require(self).DelayBubble(in value, ttlSeconds, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayDrop<TValue>(this ILayerContext self, in TValue value, float ttlSeconds, int contractLayer = 0) where TValue : struct
        {
            ServiceLayerBinder.Require(self).DelayDrop(in value, ttlSeconds, contractLayer);
        }

        // ----------------- DI & Subscription -----------------

        public static void Subscribe<T>(this IService service, EventHandleDelegate<T> eventHandleDelegate) where T : struct
            => ServiceLayerBinder.Require(service).Subscribe(eventHandleDelegate);

        public static void SubscribeAsync<T>(this IService service, EventHandleDelegateAsync<T> eventHandler) where T : struct
            => ServiceLayerBinder.Require(service).SubscribeAsync(eventHandler);

        public static void Subscribe<T>(this IService service, IEventHandler<T> eventHandler) where T : struct
            => ServiceLayerBinder.Require(service).Subscribe(eventHandler);

        public static void SubscribeAsync<T>(this IService service, IEventHandlerAsync<T> eventHandler) where T : struct
            => ServiceLayerBinder.Require(service).SubscribeAsync(eventHandler);

        public static void SubscribeParallel<T>(this IService service, IEventHandler<T> eventHandler) where T : struct
            => ServiceLayerBinder.Require(service).SubscribeParallel(eventHandler);

        public static void SubscribeParallel<T>(this IService service, EventHandleDelegate<T> eventHandleDelegate) where T : struct
            => ServiceLayerBinder.Require(service).SubscribeParallel(eventHandleDelegate);

        public static IDelayPublisher<T> SubscribeDelay<T>(this IService service) where T : struct
            => ServiceLayerBinder.Require(service).SubscribeDelay<T>();

        public static T GetService<T>(this IService service)
            => ServiceLayerBinder.Require(service).GetService<T>();
    }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InjectAttribute : Attribute { }
}
