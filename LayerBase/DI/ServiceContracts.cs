using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LayerBase.Core.EventHandler;
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

    public interface IService
    {
        void ConfigureServices(IServiceCollection services);
    }

    internal static class ServiceLayerBinder
    {
        private static readonly ConditionalWeakTable<IService, Layer> s_layerMap = new();

        internal static void Attach(IService service, Layer layer)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (layer == null) throw new ArgumentNullException(nameof(layer));

            s_layerMap.Remove(service);
            s_layerMap.Add(service, layer);
        }

        internal static Layer Require(IService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (s_layerMap.TryGetValue(service, out var layer))
            {
                return layer;
            }

            throw new InvalidOperationException("Service is not attached to a Layer.");
        }
    }

    public static class ServiceExtensions
    {
        public static Layer GetLayer(this IService service) => ServiceLayerBinder.Require(service);
        
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
   
 
        public static T GetService<T>(this IService service)
        {
            return ServiceLayerBinder.Require(service).GetService<T>();
        }

        public static void BroadCast<TValue>(this IService service, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(service).BroadCast(in value);
        }

        public static void Drop<TValue>(this IService service, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(service).Drop(in value);
        }

        public static void Bubble<TValue>(this IService service, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(service).Bubble(in value);
        }

        public static void Post<TValue>(this IService service, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(service).Post(in value);
        }

        public static void PostBubble<TValue>(this IService service, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(service).PostBubble(in value);
        }

        public static void PostDrop<TValue>(this IService service, in TValue value) where TValue : struct
        {
            ServiceLayerBinder.Require(service).PostDrop(in value);
        }
    }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
