namespace LayerBase.DI;

/// <summary>
/// 服务集合，用于在 Layer 的 ConfigureServices 中注册服务描述符。
/// 支持 Singleton、Scoped（Layer 级）和 Transient 三种生命周期。
/// </summary>
public class ServiceCollection : IServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new();
    private int _currentRegistrationScopeId;

    public IServiceCollection Add(ServiceDescriptor descriptor)
    {
        _descriptors.Add(descriptor.WithRegistrationScope(_currentRegistrationScopeId));
        return this;
    }


    public IServiceCollection AddSingleton<TService>(TService instance)
    {
        return Add(ServiceDescriptor.Singleton(instance));
    }

    public IServiceCollection AddSingleton<TService, TImpl>()
        where TImpl : TService
    {
        return Add(ServiceDescriptor.Singleton<TService, TImpl>());
    }

    public IServiceCollection AddSingleton<TService>(
        Func<IServiceProvider, TService> factory)
    {
        return Add(ServiceDescriptor.Singleton(factory));
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

    public IServiceCollection TryAddScoped<TService, TImpl>()
        where TImpl : TService
    {
        var serviceType = typeof(TService);

        for (var i = 0; i < _descriptors.Count; i++)
        {
            if (_descriptors[i].ServiceType == serviceType)
            {
                return this;
            }
        }

        return AddScoped<TService, TImpl>();
    }

    public IReadOnlyList<ServiceDescriptor> ToDescriptors()
    {
        return _descriptors;
    }

    public void Reset()
    {
        _descriptors.Clear();
        _currentRegistrationScopeId = 0;
    }

    internal IDisposable PushRegistrationScope(int registrationScopeId)
    {
        var previous = _currentRegistrationScopeId;
        _currentRegistrationScopeId = registrationScopeId;
        return new RegistrationScope(this, previous);
    }

    private sealed class RegistrationScope : IDisposable
    {
        private readonly ServiceCollection _owner;
        private readonly int _previousScopeId;
        private bool _disposed;

        public RegistrationScope(ServiceCollection owner, int previousScopeId)
        {
            _owner = owner;
            _previousScopeId = previousScopeId;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner._currentRegistrationScopeId = _previousScopeId;
        }
    }
}