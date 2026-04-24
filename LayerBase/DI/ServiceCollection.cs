namespace LayerBase.DI;

/// <summary>
///     切片服务容器
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