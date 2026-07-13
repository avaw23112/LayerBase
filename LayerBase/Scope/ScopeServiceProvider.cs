using LayerBase.DI;

namespace LayerBase.Scope;

internal sealed class ScopeServiceProvider : LayerBase.DI.IServiceProvider, IDisposable
{
    private readonly Dictionary<Type, object> _instances;
    private readonly object[]? _slotInstances;
    private bool _disposed;

    public ScopeServiceProvider(
        IReadOnlyList<IService>      services,
        IReadOnlyList<ILayerContext> contexts)
    {
        _instances = new Dictionary<Type, object>();

        for (int i = 0; i < services.Count; i++)
        {
            Register(services[i].GetType(), services[i]);
        }

        for (int i = 0; i < contexts.Count; i++)
        {
            Register(contexts[i].GetType(), contexts[i]);
        }
    }

    public ScopeServiceProvider(object[] instances)
    {
        _instances = new Dictionary<Type, object>();
        _slotInstances = instances ?? throw new ArgumentNullException(nameof(instances));
        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i] != null)
            {
                Register(instances[i].GetType(), instances[i]);
            }
        }
    }

    public object? GetService(Type serviceType)
    {
        ThrowIfDisposed();

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (_instances.TryGetValue(serviceType, out object? instance))
        {
            return instance;
        }

        foreach ((Type registeredType, object value) in _instances)
        {
            if (serviceType.IsAssignableFrom(registeredType))
            {
                return value;
            }
        }

        return null;
    }

    public T Get<T>()
    {
        object? service = GetService(typeof(T));
        if (service == null)
        {
            throw new InvalidOperationException($"Scope service not registered: {typeof(T)}");
        }

        return (T)service;
    }

    public T GetAt<T>(int slot) where T : class
    {
        if (_slotInstances != null && (uint)slot < (uint)_slotInstances.Length)
        {
            return (T)_slotInstances[slot];
        }
        return Get<T>();
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void Register(Type serviceType, object instance)
    {
        if (!_instances.ContainsKey(serviceType))
        {
            _instances.Add(serviceType, instance);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopeServiceProvider));
        }
    }
}
