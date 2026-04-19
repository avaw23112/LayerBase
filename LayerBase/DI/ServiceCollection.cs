namespace LayerBase.DI;

/// <summary>
///     切片服务容器
/// </summary>
public class ServiceCollection : IServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new();

    public IServiceCollection Add(ServiceDescriptor descriptor)
    {
        _descriptors.Add(descriptor);
        return this;
    }

    /// <summary>
    ///     注册一个全局单例 (Global Singleton)。
    /// </summary>
    public IServiceCollection AddSingleton<TService>(TService instance)
    {
        return Add(ServiceDescriptor.Singleton(instance));
    }

    public IServiceCollection AddTransient<TService, TImpl>() where TImpl : TService
    {
        return Add(ServiceDescriptor.Transient<TService, TImpl>());
    }

    public IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory)
    {
        return Add(ServiceDescriptor.Transient(factory));
    }

    public IServiceCollection AddScoped<TService, TImpl>() where TImpl : TService
    {
        return Add(ServiceDescriptor.LayerScoped<TService, TImpl>());
    }

    public IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory)
    {
        return Add(ServiceDescriptor.LayerScoped(factory));
    }

    public IReadOnlyList<ServiceDescriptor> ToDescriptors()
    {
        return _descriptors;
    }

    public void Reset()
    {
        _descriptors.Clear();
    }
}