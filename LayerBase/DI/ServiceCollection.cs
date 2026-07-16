namespace LayerBase.DI;

/// <summary>
/// 服务集合，用于在 Layer 的 ConfigureServices 中注册服务描述符。
/// 支持 Singleton、Scoped（Layer 级）和 Transient 三种生命周期。
/// </summary>
public class ServiceCollection : IServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new();
    private int _currentRegistrationScopeId;
    private int _currentOwnerScopeId = LayerBase.Scope.ScopeDefinitionIds.Main;

    public IServiceCollection Add(ServiceDescriptor descriptor)
    {
        _descriptors.Add(
            descriptor
                .WithRegistrationScope(_currentRegistrationScopeId)
                .WithOwnerScope(_currentOwnerScopeId));
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
        _currentOwnerScopeId = LayerBase.Scope.ScopeDefinitionIds.Main;
    }

    internal IDisposable PushRegistrationScope(
        int registrationScopeId,
        int ownerScopeId = LayerBase.Scope.ScopeDefinitionIds.Main)
    {
        var previousRegistrationScopeId = _currentRegistrationScopeId;
        var previousOwnerScopeId = _currentOwnerScopeId;
        _currentRegistrationScopeId = registrationScopeId;
        _currentOwnerScopeId = ownerScopeId;
        return new RegistrationScope(this, previousRegistrationScopeId, previousOwnerScopeId);
    }

    private sealed class RegistrationScope : IDisposable
    {
        private readonly ServiceCollection _owner;
        private readonly int _previousRegistrationScopeId;
        private readonly int _previousOwnerScopeId;
        private bool _disposed;

        public RegistrationScope(
            ServiceCollection owner,
            int previousRegistrationScopeId,
            int previousOwnerScopeId)
        {
            _owner = owner;
            _previousRegistrationScopeId = previousRegistrationScopeId;
            _previousOwnerScopeId = previousOwnerScopeId;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner._currentRegistrationScopeId = _previousRegistrationScopeId;
            _owner._currentOwnerScopeId = _previousOwnerScopeId;
        }
    }
}
