using System.Collections.Concurrent;
using System.Reflection;
using LayerBase.Layers;

namespace LayerBase.DI;

internal sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _map;
    private readonly WorldServiceRoot _worldRoot;
    private readonly Layer? _ownerLayer;
    private ResolutionContext? _activeResolutionContext;
    private int _disposed;

    internal ServiceProvider(WorldServiceRoot worldRoot)
    {
        _worldRoot = worldRoot ?? throw new ArgumentNullException(nameof(worldRoot));
        _map = new ConcurrentDictionary<Type, ServiceDescriptor>();
        _ownerLayer = null;
    }

    public ServiceProvider(WorldServiceRoot worldRoot, IEnumerable<ServiceDescriptor> descriptors, Layer? ownerLayer = null)
    {
        _worldRoot = worldRoot ?? throw new ArgumentNullException(nameof(worldRoot));
        if (descriptors == null) throw new ArgumentNullException(nameof(descriptors));

        _map = new ConcurrentDictionary<Type, ServiceDescriptor>();
        _ownerLayer = ownerLayer;
        foreach (var d in descriptors)
            if (d.Lifetime == ServiceLifetime.Singleton || d.Lifetime == ServiceLifetime.Instance)
                _worldRoot.Register(d);
            else
                _map[d.ServiceType] = d;
    }

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        foreach (var lazy in _instances.Values)
        {
            if (!lazy.IsValueCreated) continue;
            if (lazy.Value is IDisposable disposable)
                disposable.Dispose();
        }
    }

    public object? GetService(Type serviceType)
    {
        var context = _activeResolutionContext ?? new ResolutionContext();
        return GetServiceInternal(serviceType, context);
    }

    public T Get<T>()
    {
        var service = GetService(typeof(T));
        if (service == null)
            throw new InvalidOperationException($"Service not registered: {typeof(T)}");
        return (T)service;
    }

    internal List<IAutoSubscribe> InitializeAutoSubscriptions(Layer                          owner,
                                                              IEnumerable<ServiceDescriptor> orderedDescriptors)
    {
        var discovered = new List<IAutoSubscribe>();
        foreach (var desc in orderedDescriptors)
        {
            var instance = GetService(desc.ServiceType);
            if (instance is IAutoSubscribe auto)
            {
                auto.AutoBind(owner);
                discovered.Add(auto);
            }
        }

        return discovered;
    }

    internal List<ResolvedService> ResolveOrderedServices(IEnumerable<ServiceDescriptor> orderedDescriptors)
    {
        var resolved = new List<ResolvedService>();
        foreach (var desc in orderedDescriptors)
        {
            var instance = GetService(desc.ServiceType);
            if (instance != null) resolved.Add(new ResolvedService(desc, instance));
        }

        return resolved;
    }

    private object? GetServiceInternal(Type serviceType, ResolutionContext context)
    {
        if (IsDisposed) throw new ObjectDisposedException(nameof(ServiceProvider));
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));


        if (_map.TryGetValue(serviceType, out var desc)) return Resolve(desc, context);


        if (_worldRoot.TryGetDescriptor(serviceType, out var parentDesc))
            return Resolve(parentDesc!, context);

        return null;
    }

    private object Resolve(ServiceDescriptor desc, ResolutionContext context)
    {
        var instance = desc.Lifetime switch
                       {
                           ServiceLifetime.Instance => _worldRoot.GetOrCreate(desc, _ownerLayer, () => desc.Instance!),
                           ServiceLifetime.Singleton => _worldRoot.GetOrCreate(desc, _ownerLayer, () => CreateInstance(desc, context)),
                           ServiceLifetime.Scoped => GetOrCreateCached(desc, context),
                           ServiceLifetime.Transient => CreateInstance(desc, context),
                           _ => throw new NotSupportedException($"Unsupported lifetime {desc.Lifetime}")
                       };


        if (desc.Lifetime == ServiceLifetime.Singleton || desc.Lifetime == ServiceLifetime.Instance)
        {
            if (!ServiceLayerBinder.HasLayerBinding(instance))
            {
                ServiceLayerBinder.AttachRuntime(instance, _worldRoot.Runtime);
            }
        }
        else if (_ownerLayer != null)
        {
            ServiceLayerBinder.AttachLayer(instance, _ownerLayer);
        }

        return instance;
    }

    private object GetOrCreateCached(ServiceDescriptor desc, ResolutionContext context)
    {
        var implementationType = desc.ImplType ?? desc.ServiceType;
        if (context.CallStack.Contains(implementationType))
            throw new InvalidOperationException($"Circular dependency detected: {implementationType}");

        var lazy = _instances.GetOrAdd(desc.ServiceType, _ =>
        {
            return new Lazy<object>(
                () =>
                {
                    return CreateInstance(desc, context);
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
        });

        try
        {
            return lazy.Value;
        }
        catch
        {
            _instances.TryRemove(desc.ServiceType, out _);
            throw;
        }
    }

    private object CreateInstance(ServiceDescriptor desc, ResolutionContext context)
    {
        if (desc.ImplType == null && desc.Factory == null)
            throw new InvalidOperationException($"No implementation for {desc.ServiceType}");

        var implementationType = desc.ImplType ?? desc.ServiceType;
        if (!context.CallStack.Add(implementationType))
            throw new InvalidOperationException($"Circular dependency detected: {implementationType}");

        var previousContext = _activeResolutionContext;
        _activeResolutionContext = context;
        try
        {
            if (desc.Factory != null)
                return desc.Factory(this);

            var ctor = desc.ImplType!
                           .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                           .OrderByDescending(c => c.GetParameters().Length)
                           .FirstOrDefault();

            if (ctor == null)
                throw new InvalidOperationException($"No accessible constructor found for {desc.ImplType}");

            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var dep = GetServiceInternal(parameters[i].ParameterType, context);
                if (dep == null)
                    throw new InvalidOperationException(
                        $"Unable to resolve dependency {parameters[i].ParameterType} for {desc.ImplType}");
                args[i] = dep;
            }

            var instance = ctor.Invoke(args);
            InjectMembers(instance, context);
            return instance;
        }
        finally
        {
            _activeResolutionContext = previousContext;
            context.CallStack.Remove(implementationType);
        }
    }

    private void InjectMembers(object instance, ResolutionContext context)
    {
        var t = instance.GetType();

        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (f.GetCustomAttribute<MountAttribute>() == null) continue;
            var dep = GetServiceInternal(f.FieldType, context);
            if (dep != null) f.SetValue(instance, dep);
        }

        foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (p.GetCustomAttribute<MountAttribute>() == null || !p.CanWrite) continue;
            var dep = GetServiceInternal(p.PropertyType, context);
            if (dep != null) p.SetValue(instance, dep, null);
        }
    }

    private sealed class ResolutionContext
    {
        public readonly HashSet<Type> CallStack = new();
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
