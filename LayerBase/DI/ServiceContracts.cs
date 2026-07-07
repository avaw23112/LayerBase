using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;

namespace LayerBase.DI;

/// <summary>
/// 服务接口，用于配置分层 DI 容器。
/// </summary>
public interface IService
{
    void ConfigureServices(IServiceCollection services);
}

/// <summary>
/// 服务集合，用于注册和管理 DI 服务。
/// </summary>
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
    IServiceCollection TryAddScoped<TService, TImpl>() where TImpl : TService;
    IReadOnlyList<ServiceDescriptor> ToDescriptors();
}

/// <summary>
/// 服务提供器，用于从 Layer 解析服务。
/// </summary>
public interface IServiceProvider
{
    object? GetService(Type serviceType);
    T Get<T>();
}
