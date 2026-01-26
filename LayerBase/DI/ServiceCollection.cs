using LayerBase.DI.Options;

namespace LayerBase.DI;

/// <summary>
/// 切片服务容器
/// </summary>
public class ServiceCollection: IServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();
    public IServiceCollection Add(ServiceDescriptor descriptor)
    {
        if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
        _descriptors.RemoveAll(d => d.ServiceType == descriptor.ServiceType);
        _descriptors.Add(descriptor);
        return this;
    }

    public IServiceCollection AddSingleton<TService, TImpl>() where TImpl : TService
        => Add(ServiceDescriptor.Singleton<TService, TImpl>());
    public IServiceCollection AddSingleton<TService>(TService instance)
        => Add(ServiceDescriptor.Singleton(instance));

    public IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory)
        => Add(ServiceDescriptor.Singleton(factory));

    public IServiceCollection AddTransient<TService, TImpl>() where TImpl : TService
        => Add(ServiceDescriptor.Transient<TService, TImpl>());

    public IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory)
        => Add(ServiceDescriptor.Transient(factory));

    public IServiceCollection AddScoped<TService, TImpl>() where TImpl : TService
        => Add(ServiceDescriptor.LayerScoped<TService, TImpl>());

    public IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory)
        => Add(ServiceDescriptor.LayerScoped(factory));

    public IReadOnlyList<ServiceDescriptor> ToDescriptors() => _descriptors;

    public void Reset() => _descriptors.Clear();
}