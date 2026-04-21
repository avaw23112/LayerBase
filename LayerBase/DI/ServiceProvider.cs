using System.Collections.Concurrent;
using System.Reflection;
using LayerBase.Layers;

namespace LayerBase.DI;

public sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private static ServiceProvider _root = new();

    public static void ResetRoot()
    {
        var oldRoot = Interlocked.Exchange(ref _root, new ServiceProvider());
        oldRoot.Dispose();
    }

    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _map;
    private readonly Layer? _ownerLayer;
    private int _disposed;

    internal ServiceProvider()
    {
        _map = new ConcurrentDictionary<Type, ServiceDescriptor>();
        _ownerLayer = null;
    }

    public ServiceProvider(IEnumerable<ServiceDescriptor> descriptors, Layer? ownerLayer = null)
    {
        if (descriptors == null) throw new ArgumentNullException(nameof(descriptors));

        _map = new ConcurrentDictionary<Type, ServiceDescriptor>();
        _ownerLayer = ownerLayer;
        foreach (var d in descriptors)
            if (d.Lifetime == ServiceLifetime.Singleton)
                // 真正的全局单例进 Root
                _root._map[d.ServiceType] = d;
            else
                // 局部单例 (Scoped) 或瞬时态进局部 Map
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
        return GetServiceInternal(serviceType, new HashSet<Type>());
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

    private object? GetServiceInternal(Type serviceType, HashSet<Type> callstack)
    {
        if (IsDisposed) throw new ObjectDisposedException(nameof(ServiceProvider));
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        // 1. 尝试从本地 Map 解析 (Scoped, Transient, Instance)
        if (_map.TryGetValue(serviceType, out var desc)) return Resolve(desc, callstack);

        // 2. 尝试从 Root 解析 (Global Singleton)
        if (this != _root && _root._map.TryGetValue(serviceType, out var parentDesc))
            return _root.Resolve(parentDesc, callstack);

        return null;
    }

    private object Resolve(ServiceDescriptor desc, HashSet<Type> callstack)
    {
        var instance = desc.Lifetime switch
                       {
                           ServiceLifetime.Instance => desc.Instance!,
                           ServiceLifetime.Singleton => GetOrCreateCached(desc, callstack),
                           ServiceLifetime.Scoped => GetOrCreateCached(desc, callstack),
                           ServiceLifetime.Transient => CreateInstance(desc, callstack),
                           _ => throw new NotSupportedException($"Unsupported lifetime {desc.Lifetime}")
                       };

        // 只要是从本容器解析出来的对象，就自动绑定到该层
        if (_ownerLayer != null) ServiceLayerBinder.Attach(instance, _ownerLayer);

        return instance;
    }

    private object GetOrCreateCached(ServiceDescriptor desc, HashSet<Type> callstack)
    {
        // 注意：Singleton 在 _root 实例中调用此方法，Scoped 在局部实例中调用
        var lazy = _instances.GetOrAdd(desc.ServiceType, _ =>
        {
            return new Lazy<object>(
                () => 
                {
                    // 使用独立的 callstack 防止跨线程访问非线程安全的 HashSet 及污染检测语义
                    var localCallstack = new HashSet<Type>();
                    return CreateInstance(desc, localCallstack);
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

    private object CreateInstance(ServiceDescriptor desc, HashSet<Type> callstack)
    {
        if (desc.Factory != null)
            return desc.Factory(this);

        if (desc.ImplType == null)
            throw new InvalidOperationException($"No implementation for {desc.ServiceType}");

        if (!callstack.Add(desc.ImplType))
            throw new InvalidOperationException($"Circular dependency detected: {desc.ImplType}");

        try
        {
            var ctor = desc.ImplType
                           .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                           .OrderByDescending(c => c.GetParameters().Length)
                           .FirstOrDefault();

            if (ctor == null)
                throw new InvalidOperationException($"No accessible constructor found for {desc.ImplType}");

            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var dep = GetServiceInternal(parameters[i].ParameterType, callstack);
                if (dep == null)
                    throw new InvalidOperationException(
                        $"Unable to resolve dependency {parameters[i].ParameterType} for {desc.ImplType}");
                args[i] = dep;
            }

            var instance = ctor.Invoke(args);
            InjectMembers(instance);
            return instance;
        }
        finally
        {
            callstack.Remove(desc.ImplType);
        }
    }

    private void InjectMembers(object instance)
    {
        var t = instance.GetType();

        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (f.GetCustomAttribute<InjectAttribute>() == null) continue;
            var dep = GetService(f.FieldType);
            if (dep != null) f.SetValue(instance, dep);
        }

        foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (p.GetCustomAttribute<InjectAttribute>() == null || !p.CanWrite) continue;
            var dep = GetService(p.PropertyType);
            if (dep != null) p.SetValue(instance, dep, null);
        }
    }
}