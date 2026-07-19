namespace LayerBase.DI;

internal sealed class ScopeServiceProvider : IServiceProvider, IDisposable
{
    private readonly ServiceProvider _root;
    private readonly int _ownerScopeId;
    private readonly Dictionary<Type, ServiceDescriptor> _descriptors;
    private readonly Dictionary<Type, object> _instances = new();
    private readonly ScopeOwnedResourceList _resources = new();
    private int _disposed;

    public ScopeServiceProvider(
        ServiceProvider root,
        int ownerScopeId,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _ownerScopeId = ownerScopeId;
        _descriptors = new Dictionary<Type, ServiceDescriptor>();
        foreach (ServiceDescriptor descriptor in descriptors)
            _descriptors[descriptor.ServiceType] = descriptor;
    }

    public int OwnerScopeId => _ownerScopeId;

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    internal IEnumerable<Type> ServiceTypes => _descriptors.Keys;

    internal bool Contains(Type serviceType)
    {
        return _descriptors.ContainsKey(serviceType);
    }

    public object? GetService(Type serviceType)
    {
        return GetService(serviceType, new ServiceProvider.ResolutionContext());
    }

    public T Get<T>()
    {
        object? service = GetService(typeof(T));
        if (service == null)
            throw new InvalidOperationException($"Service not registered in scope {_ownerScopeId}: {typeof(T)}");

        return (T)service;
    }

    internal object? GetService(
        Type serviceType,
        ServiceProvider.ResolutionContext context)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(ScopeServiceProvider));
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));

        if (!_descriptors.TryGetValue(serviceType, out ServiceDescriptor? descriptor))
            return null;

        return _root.Resolve(this, descriptor);
    }

    internal object GetOrCreateCached(
        ServiceDescriptor descriptor,
        ServiceProvider.ResolutionContext context)
    {
        if (_instances.TryGetValue(descriptor.ServiceType, out object? existing))
            return existing;

        object instance = _root.CreateInstance(this, descriptor, context);
        _instances.Add(descriptor.ServiceType, instance);
        return instance;
    }

    internal void RegisterResource(object instance)
    {
        _resources.Add(instance);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _resources.ReleaseAll();
        _instances.Clear();
    }
}
