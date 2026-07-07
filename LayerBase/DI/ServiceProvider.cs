using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using LayerBase.DI.Options;
using LayerBase.Layers;

namespace LayerBase.DI;

/// <summary>
/// 服务提供者的内部实现。支持依赖注入的生命周期管理（Singleton、Scoped、Transient），
/// 构造函数自动解析、[Mount] 标记的字段/属性注入，以及循环依赖检测。
/// </summary>
internal sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _map;
    private readonly WorldServiceRoot _worldRoot;
    private readonly Layer? _ownerLayer;
    private ResolutionContext? _activeResolutionContext;
    private int _disposed;

    private static readonly ConcurrentDictionary<MemberInfo, Func<object, object?, object>> s_setterCache = new();
    private static readonly ConcurrentDictionary<Type, bool> s_scannedTypes = new();

    // 进程启动时检测是否支持 Expression.Compile（JIT）。
    // true 表示可以使用编译委托（Windows / 启用了 JIT 的 Mono）；
    // false 表示回退到 FieldInfo.SetValue（IL2CPP / AOT）。
    private static readonly bool s_canCompileExpression;

    static ServiceProvider()
    {
        try
        {
            Expression.Lambda<Action>(Expression.Empty()).Compile();
            s_canCompileExpression = true;
        }
        catch
        {
            s_canCompileExpression = false;
        }
    }

    internal ServiceProvider(WorldServiceRoot worldRoot)
    {
        _worldRoot = worldRoot ?? throw new ArgumentNullException(nameof(worldRoot));
        _map = new ConcurrentDictionary<Type, ServiceDescriptor>();
        _ownerLayer = null;
    }

    public ServiceProvider(WorldServiceRoot worldRoot, IEnumerable<ServiceDescriptor> descriptors,
                           Layer?           ownerLayer = null)
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

    internal void InjectMembers(object instance)
    {
        InjectMembers(instance, new ResolutionContext());
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
                           ServiceLifetime.Singleton => _worldRoot.GetOrCreate(desc, _ownerLayer,
                               () => CreateInstance(desc, context)),
                           ServiceLifetime.Scoped => GetOrCreateCached(desc, context),
                           ServiceLifetime.Transient => CreateInstance(desc, context),
                           _ => throw new NotSupportedException($"Unsupported lifetime {desc.Lifetime}")
                       };


        if (desc.Lifetime == ServiceLifetime.Singleton || desc.Lifetime == ServiceLifetime.Instance)
        {
            var existingBinding = ServiceLayerBinder.GetBinding(instance);

            if (existingBinding != null && existingBinding.RuntimeId != _worldRoot.Runtime.Id)
            {
                throw new InvalidOperationException(
                    $"Singleton/Instance service {instance.GetType().Name} is already bound to another LayerRuntime.");
            }

            if (existingBinding == null || existingBinding.Layer != null)
            {
                // Singleton / Instance 是 Runtime 级服务。
                // 即使它之前被某个 Layer 绑定过，也要覆盖成 Runtime binding。
                ServiceLayerBinder.AttachRuntime(instance, _worldRoot.Runtime);
            }
        }
        else if (_ownerLayer != null)
        {
            // Scoped / Transient 是 Layer 级服务。
            // 它们需要知道自己属于哪个 Layer，才能使用 Subscribe、Delay、OnEvent 等 Layer-only API。
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
                () => { return CreateInstance(desc, context); },
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

            var ctor = SelectConstructor(desc.ImplType!);

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

    /// <summary>
    /// 选择用于 DI 创建实例的构造函数。
    /// 优先选择带有 [Mount] 标记的构造函数；如果没有，则选择参数最多的 public 构造函数。
    /// </summary>
    private static ConstructorInfo SelectConstructor(Type implementationType)
    {
        var allConstructors = implementationType.GetConstructors(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        var markedConstructors = allConstructors
                                 .Where(static ctor => ctor.GetCustomAttribute<MountAttribute>() != null)
                                 .ToArray();

        if (markedConstructors.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple [Mount] constructors found for {implementationType}.");
        }

        if (markedConstructors.Length == 1)
        {
            return markedConstructors[0];
        }

        var publicConstructors = implementationType.GetConstructors(
            BindingFlags.Instance |
            BindingFlags.Public);

        if (publicConstructors.Length == 0)
        {
            throw new InvalidOperationException(
                $"No public constructor found for {implementationType}. Use [Mount] on a non-public constructor if it should be used by DI.");
        }

        var maxParameterCount = publicConstructors.Max(static ctor => ctor.GetParameters().Length);

        var candidates = publicConstructors
                         .Where(ctor => ctor.GetParameters().Length == maxParameterCount)
                         .ToArray();

        if (candidates.Length > 1)
        {
            throw new InvalidOperationException(
                $"Ambiguous public constructors found for {implementationType}. Use [Mount] to select the constructor explicitly.");
        }

        return candidates[0];
    }

    private void InjectMembers(object instance, ResolutionContext context)
    {
        var t = instance.GetType();

        if (s_scannedTypes.TryGetValue(t, out var hasMount) && !hasMount)
            return;

        var foundAny = false;

        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (f.GetCustomAttribute<MountAttribute>() == null) continue;
            foundAny = true;
            var dep = GetServiceInternal(f.FieldType, context);
            if (dep != null)
            {
                var setter = s_setterCache.GetOrAdd(f, mi => CreateFieldSetter((FieldInfo)mi));
                setter(instance, dep);
            }
        }

        foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (p.GetCustomAttribute<MountAttribute>() == null || !p.CanWrite) continue;
            foundAny = true;
            var dep = GetServiceInternal(p.PropertyType, context);
            if (dep != null)
            {
                var setter = s_setterCache.GetOrAdd(p, mi => CreatePropertySetter((PropertyInfo)mi));
                setter(instance, dep);
            }
        }

        s_scannedTypes[t] = foundAny;
    }

    private static Func<object, object?, object> CreateFieldSetter(FieldInfo field)
    {
        if (!s_canCompileExpression)
            return (obj, val) => { field.SetValue(obj, val); return obj; };

        var target = Expression.Parameter(typeof(object));
        var value = Expression.Parameter(typeof(object));
        var expr = Expression.Assign(
            Expression.Field(Expression.Convert(target, field.DeclaringType!), field),
            Expression.Convert(value, field.FieldType));
        var lambda = Expression.Lambda<Action<object, object?>>(expr, target, value);
        var compiled = lambda.Compile();
        return (obj, val) => { compiled(obj, val); return obj; };
    }

    private static Func<object, object?, object> CreatePropertySetter(PropertyInfo property)
    {
        if (!s_canCompileExpression)
            return (obj, val) => { property.SetValue(obj, val); return obj; };

        var target = Expression.Parameter(typeof(object));
        var value = Expression.Parameter(typeof(object));
        var expr = Expression.Assign(
            Expression.Property(Expression.Convert(target, property.DeclaringType!), property),
            Expression.Convert(value, property.PropertyType));
        var lambda = Expression.Lambda<Action<object, object?>>(expr, target, value);
        var compiled = lambda.Compile();
        return (obj, val) => { compiled(obj, val); return obj; };
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