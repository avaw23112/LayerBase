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

    public interface IService : ILayerContext
    {
        void ConfigureServices(IServiceCollection services);
    }

    /// <summary>
    /// 能力标记接口：只要实现此接口，即可通过扩展方法获得 Layer 级的事件分发能力。
    /// </summary>
    public interface ILayerContext { }

    /// <summary>
    /// 自动订阅行为接口：由 Source Generator 自动生成，用于在构建期执行订阅连线。
    /// </summary>
    public interface IAutoSubscribe
    {
        void AutoBind(Layer layer);
    }

    internal static class ServiceLayerBinder
    {
        private static readonly ConditionalWeakTable<object, Layer> s_layerMap = new();

        internal static void Attach(object instance, Layer layer)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (layer == null) throw new ArgumentNullException(nameof(layer));

            lock (s_layerMap)
            {
                s_layerMap.Remove(instance);
                s_layerMap.Add(instance, layer);
            }
        }

        internal static Layer Require(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (s_layerMap.TryGetValue(instance, out var layer))
            {
                return layer;
            }

            throw new InvalidOperationException($"Instance of {instance.GetType().Name} is not attached to a Layer. Ensure it is resolved from the Layer DI container.");
        }
    }

    public static class ServiceExtensions
    {
        public static Layer GetLayer(this ILayerContext self) => ServiceLayerBinder.Require(self);
        
        public static void Subscribe<T>(this IService service, EventHandleDelegate<T> eventHandleDelegate) where T : struct
        {
            ServiceLayerBinder.Require(service).Subscribe<T>(eventHandleDelegate);
        }
        public static void SubscribeAsync<T>(this IService service, EventHandleDelegateAsync<T> eventHandler) where T : struct
        {
            ServiceLayerBinder.Require(service).SubscribeAsync<T>(eventHandler);
        }
        public static void Subscribe<T>(this IService service, IEventHandler<T> eventHandler) where T : struct
        {
            ServiceLayerBinder.Require(service).Subscribe<T>(eventHandler);
        }
        public static void SubscribeAsync<T>(this IService service, IEventHandlerAsync<T> eventHandler) where T : struct
        {
            ServiceLayerBinder.Require(service).SubscribeAsync<T>(eventHandler);
        }
        public static void SubscribeParallel<T>(this IService service, IEventHandler<T> eventHandler) where T : struct
        {
            ServiceLayerBinder.Require(service).SubscribeParallel<T>(eventHandler);
        }
        public static void SubscribeParallel<T>(this IService service, EventHandleDelegate<T> eventHandleDelegate) where T : struct
        {
            ServiceLayerBinder.Require(service).SubscribeParallel<T>(eventHandleDelegate);
        }

        public static IDelayPublisher<T> SubscribeDelay<T>(this IService service) where T : struct
        {
            return ServiceLayerBinder.Require(service).SubscribeDelay<T>();
        }
 
        public static T GetService<T>(this IService service)
        {
            return ServiceLayerBinder.Require(service).GetService<T>();
        }

        // --- Synchronous Dispatch ---

        public static EventHandledState SendLocal<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            return ServiceLayerBinder.Require(self).SendLocal(in value);
        }

        public static void SendBubble<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(self).SendBubble(in value);
        }

        public static void SendDrop<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(self).SendDrop(in value);
        }

        public static void SendGlobal<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(self).SendGlobal(in value);
        }

        // --- Asynchronous Dispatch ---

        public static void PostLocal<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(self).PostLocal(in value);
        }

        public static void PostBubble<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(self).PostBubble(in value);
        }

        public static void PostDrop<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(self).PostDrop(in value);
        }

        public static void PostGlobal<TValue>(this ILayerContext self, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(self).PostGlobal(in value);
        }

        // --- Delay Dispatch ---

        public static void DelayLocal<TValue>(this ILayerContext self, in TValue value, float ttlSeconds, int contractLayer = 0) where TValue : struct
        {
            ServiceLayerBinder.Require(self).DelayLocal(in value, ttlSeconds, contractLayer);
        }

        public static void DelayGlobal<TValue>(this ILayerContext self, in TValue value, float ttlSeconds, int contractLayer = 0) where TValue : struct
        {
            ServiceLayerBinder.Require(self).DelayGlobal(in value, ttlSeconds, contractLayer);
        }

        public static void DelayBubble<TValue>(this ILayerContext self, in TValue value, float ttlSeconds, int contractLayer = 0) where TValue : struct
        {
            ServiceLayerBinder.Require(self).DelayBubble(in value, ttlSeconds, contractLayer);
        }

        public static void DelayDrop<TValue>(this ILayerContext self, in TValue value, float ttlSeconds, int contractLayer = 0) where TValue : struct
        {
            ServiceLayerBinder.Require(self).DelayDrop(in value, ttlSeconds, contractLayer);
        }
    }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
