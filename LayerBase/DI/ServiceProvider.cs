using LayerBase.Scope;

namespace LayerBase.DI;

internal sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private const int DisposeStateAlive = 0;
    private const int DisposeStateDisposing = 1;
    private const int DisposeStateDisposed = 2;

    private readonly Dictionary<int, ScopeServiceProvider> _scopeProviders;
    private int _disposed;

    public ServiceProvider(IEnumerable<ScopeServiceProvider> scopeProviders)
    {
        if (scopeProviders == null)
            throw new ArgumentNullException(nameof(scopeProviders));

        _scopeProviders = scopeProviders.ToDictionary(
            static provider => provider.OwnerScopeId,
            static provider => provider);
    }

    public bool IsDisposed => Volatile.Read(ref _disposed) == DisposeStateDisposed;

    public void Dispose()
    {
        int state = Volatile.Read(ref _disposed);
        if (state == DisposeStateDisposed)
            return;

        if (state == DisposeStateDisposing ||
            Interlocked.CompareExchange(ref _disposed, DisposeStateDisposing, DisposeStateAlive) != DisposeStateAlive)
        {
            return;
        }

        try
        {
            foreach (ScopeServiceProvider provider in _scopeProviders.Values)
                provider.Dispose();

            Volatile.Write(ref _disposed, DisposeStateDisposed);
        }
        catch
        {
            Volatile.Write(ref _disposed, DisposeStateAlive);
            throw;
        }
    }

    public object? GetService(Type serviceType)
    {
        object? service = GetProvider(ScopeDefinitionIds.Main).GetService(serviceType);
        if (service != null)
            return service;

        return GetUniqueNonMainService(serviceType);
    }

    public T Get<T>()
    {
        object? service = GetService(typeof(T));
        if (service == null)
            throw new InvalidOperationException($"Service not registered in any scope: {typeof(T)}");

        return (T)service;
    }

    internal T Get<T>(int ownerScopeId)
    {
        return GetProvider(ownerScopeId).Get<T>();
    }

    internal object? GetService(Type serviceType, int ownerScopeId)
    {
        return GetProvider(ownerScopeId).GetService(serviceType);
    }

    internal List<ResolvedService> ResolveOrderedServices(IEnumerable<ServiceDescriptor> orderedDescriptors)
    {
        var resolved = new List<ResolvedService>();
        foreach (ServiceDescriptor descriptor in orderedDescriptors)
        {
            object? instance = GetProvider(descriptor.OwnerScopeId).GetService(descriptor.ServiceType);
            if (instance != null)
                resolved.Add(new ResolvedService(descriptor, instance));
        }

        return resolved;
    }

    internal List<ResolvedService> ResolveOrderedServices(
        IEnumerable<ServiceDescriptor> orderedDescriptors,
        int ownerScopeId)
    {
        var resolved = new List<ResolvedService>();
        ScopeServiceProvider provider = GetProvider(ownerScopeId);
        foreach (ServiceDescriptor descriptor in orderedDescriptors)
        {
            if (descriptor.OwnerScopeId != ownerScopeId)
                continue;

            object? instance = provider.GetService(descriptor.ServiceType);
            if (instance != null)
                resolved.Add(new ResolvedService(descriptor, instance));
        }

        return resolved;
    }

    internal void InjectMembers(object instance, int ownerScopeId)
    {
        GetProvider(ownerScopeId).InjectMembers(instance);
    }

    internal void DisposeScope(int ownerScopeId)
    {
        if (_scopeProviders.TryGetValue(ownerScopeId, out ScopeServiceProvider? provider))
            provider.Dispose();
    }

    internal void InjectMembers(object instance)
    {
        int ownerScopeId =
            ServiceLayerBinder.GetBinding(instance)?.OwnerScope.ScopeId ??
            ScopeDefinitionIds.Main;

        GetProvider(ownerScopeId).InjectMembers(instance);
    }

    private ScopeServiceProvider GetProvider(int ownerScopeId)
    {
        if (Volatile.Read(ref _disposed) != DisposeStateAlive)
            throw new ObjectDisposedException(nameof(ServiceProvider));

        if (_scopeProviders.TryGetValue(ownerScopeId, out ScopeServiceProvider? provider))
            return provider;

        throw new InvalidOperationException($"Scope service provider not found for scope {ownerScopeId}.");
    }

    private object? GetUniqueNonMainService(Type serviceType)
    {
        ScopeServiceProvider? match = null;

        foreach (KeyValuePair<int, ScopeServiceProvider> entry in _scopeProviders)
        {
            if (entry.Key == ScopeDefinitionIds.Main)
                continue;

            ScopeServiceProvider provider = entry.Value;
            if (!provider.Contains(serviceType))
                continue;

            if (match != null)
            {
                throw new InvalidOperationException(
                    $"Service {serviceType} is registered in multiple non-main scopes. Use a scope-specific service lookup.");
            }

            match = provider;
        }

        return match?.GetService(serviceType);
    }

    internal readonly struct ResolvedService
    {
        public ResolvedService(ServiceDescriptor descriptor, object instance)
        {
            Descriptor = descriptor;
            Instance = instance;
        }

        public ServiceDescriptor Descriptor { get; }

        public object Instance { get; }
    }
}
