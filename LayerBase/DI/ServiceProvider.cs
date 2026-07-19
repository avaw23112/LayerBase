using LayerBase.Scope;

namespace LayerBase.DI;

internal sealed class ServiceProvider : IServiceProvider, IDisposable
{
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

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        foreach (ScopeServiceProvider provider in _scopeProviders.Values)
            provider.Dispose();
    }

    public object? GetService(Type serviceType)
    {
        return GetProvider(ScopeDefinitionIds.Main).GetService(serviceType);
    }

    public T Get<T>()
    {
        return GetProvider(ScopeDefinitionIds.Main).Get<T>();
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
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(ServiceProvider));

        if (_scopeProviders.TryGetValue(ownerScopeId, out ScopeServiceProvider? provider))
            return provider;

        throw new InvalidOperationException($"Scope service provider not found for scope {ownerScopeId}.");
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
