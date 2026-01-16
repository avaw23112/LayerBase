using System;
using System.Collections.Generic;

namespace LayerBase.DI
{
    public enum ServiceLifetime
    {
        /// <summary>
        /// 全局共享单例
        /// </summary>
        Singleton,
        
        /// <summary>
        /// 即发即弃
        /// </summary>
        Transient,
        
        /// <summary>
        /// 区域内构建
        /// </summary>
        Scoped
    }

    public interface IServiceCollection
    {
        IServiceCollection Add(ServiceDescriptor descriptor);
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

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
