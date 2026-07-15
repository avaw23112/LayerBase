using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using LayerBase;
using LayerBase.DI.Options;
using LayerBase.Layers;

namespace LayerBase.DI;

internal sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _map;
    private readonly LayerRuntime _runtime;
    private readonly Layer _ownerLayer;
    private ResolutionContext? _activeResolutionContext;
    private int _disposed;

    private static readonly ConcurrentDictionary<MemberInfo, Func<object, object?, object>> s_setterCache = new();
    private static readonly ConcurrentDictionary<Type, bool> s_scannedTypes = new();
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

    public ServiceProvider(
        LayerRuntime runtime,
        IEnumerable<ServiceDescriptor> descriptors,
        Layer ownerLayer)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _ownerLayer = ownerLayer ?? throw new ArgumentNullException(nameof(ownerLayer));
        if (descriptors == null)
            throw new ArgumentNullException(nameof(descriptors));

        _map = new ConcurrentDictionary<Type, ServiceDescriptor>();
        foreach (var descriptor in descriptors)
            _map[descriptor.ServiceType] = descriptor;
    }

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        foreach (var lazy in _instances.Values)
        {
            if (lazy.IsValueCreated && lazy.Value is IDisposable disposable)
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

    internal List<IAutoSubscribe> InitializeAutoSubscriptions(
        Layer owner,
        IEnumerable<ServiceDescriptor> orderedDescriptors)
    {
        var discovered = new List<IAutoSubscribe>();
        foreach (var descriptor in orderedDescriptors)
        {
            var instance = GetService(descriptor.ServiceType);
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
        foreach (var descriptor in orderedDescriptors)
        {
            var instance = GetService(descriptor.ServiceType);
            if (instance != null)
                resolved.Add(new ResolvedService(descriptor, instance));
        }

        return resolved;
    }

    internal void InjectMembers(object instance)
    {
        InjectMembers(instance, new ResolutionContext());
    }

    private object? GetServiceInternal(Type serviceType, ResolutionContext context)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(ServiceProvider));
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));

        return _map.TryGetValue(serviceType, out var descriptor)
            ? Resolve(descriptor, context)
            : null;
    }

    private object Resolve(ServiceDescriptor descriptor, ResolutionContext context)
    {
        var instance = descriptor.Lifetime switch
        {
            ServiceLifetime.Instance => GetOrCreateCached(descriptor, context),
            ServiceLifetime.Singleton => GetOrCreateCached(descriptor, context),
            ServiceLifetime.Scoped => GetOrCreateCached(descriptor, context),
            ServiceLifetime.Transient => CreateInstance(descriptor, context),
            _ => throw new NotSupportedException($"Unsupported lifetime {descriptor.Lifetime}")
        };

        var existingBinding = ServiceLayerBinder.GetBinding(instance);
        if (existingBinding != null &&
            (existingBinding.RuntimeId != _runtime.Id ||
             existingBinding.LayerIndex != _ownerLayer.RouteIndex))
        {
            throw new InvalidOperationException(
                $"Service instance {instance.GetType().Name} is already bound to another Layer provider.");
        }

        ServiceLayerBinder.AttachLayer(instance, _ownerLayer);
        return instance;
    }

    private object GetOrCreateCached(ServiceDescriptor descriptor, ResolutionContext context)
    {
        var implementationType = descriptor.ImplType ?? descriptor.ServiceType;
        if (context.CallStack.Contains(implementationType))
            throw new InvalidOperationException($"Circular dependency detected: {implementationType}");

        var lazy = _instances.GetOrAdd(
            descriptor.ServiceType,
            _ => new Lazy<object>(
                () => CreateInstance(descriptor, context),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazy.Value;
        }
        catch
        {
            _instances.TryRemove(descriptor.ServiceType, out _);
            throw;
        }
    }

    private object CreateInstance(ServiceDescriptor descriptor, ResolutionContext context)
    {
        if (descriptor.Lifetime == ServiceLifetime.Instance)
            return descriptor.Instance ?? throw new InvalidOperationException($"No instance for {descriptor.ServiceType}");

        if (descriptor.ImplType == null && descriptor.Factory == null)
            throw new InvalidOperationException($"No implementation for {descriptor.ServiceType}");

        var implementationType = descriptor.ImplType ?? descriptor.ServiceType;
        if (!context.CallStack.Add(implementationType))
            throw new InvalidOperationException($"Circular dependency detected: {implementationType}");

        var previousContext = _activeResolutionContext;
        _activeResolutionContext = context;
        try
        {
            if (descriptor.Factory != null)
                return descriptor.Factory(this);

            var ctor = SelectConstructor(descriptor.ImplType!);
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var dependency = GetServiceInternal(parameters[i].ParameterType, context);
                if (dependency == null)
                    throw new InvalidOperationException(
                        $"Unable to resolve dependency {parameters[i].ParameterType} for {descriptor.ImplType}");

                args[i] = dependency;
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
            throw new InvalidOperationException(
                $"Multiple [Mount] constructors found for {implementationType}.");

        if (markedConstructors.Length == 1)
            return markedConstructors[0];

        var publicConstructors = implementationType.GetConstructors(
            BindingFlags.Instance |
            BindingFlags.Public);

        if (publicConstructors.Length == 0)
            throw new InvalidOperationException(
                $"No public constructor found for {implementationType}. Use [Mount] on a non-public constructor if it should be used by DI.");

        var maxParameterCount = publicConstructors.Max(static ctor => ctor.GetParameters().Length);
        var candidates = publicConstructors
            .Where(ctor => ctor.GetParameters().Length == maxParameterCount)
            .ToArray();

        if (candidates.Length > 1)
            throw new InvalidOperationException(
                $"Ambiguous public constructors found for {implementationType}. Use [Mount] to select the constructor explicitly.");

        return candidates[0];
    }

    private void InjectMembers(object instance, ResolutionContext context)
    {
        var type = instance.GetType();
        if (s_scannedTypes.TryGetValue(type, out var hasMount) && !hasMount)
            return;

        var foundAny = false;

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.GetCustomAttribute<MountAttribute>() == null)
                continue;

            foundAny = true;
            var dependency = GetServiceInternal(field.FieldType, context);
            if (dependency == null)
                continue;

            var setter = s_setterCache.GetOrAdd(field, static member => CreateFieldSetter((FieldInfo)member));
            setter(instance, dependency);
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (property.GetCustomAttribute<MountAttribute>() == null || !property.CanWrite)
                continue;

            foundAny = true;
            var dependency = GetServiceInternal(property.PropertyType, context);
            if (dependency == null)
                continue;

            var setter = s_setterCache.GetOrAdd(property, static member => CreatePropertySetter((PropertyInfo)member));
            setter(instance, dependency);
        }

        s_scannedTypes[type] = foundAny;
    }

    private static Func<object, object?, object> CreateFieldSetter(FieldInfo field)
    {
        if (!s_canCompileExpression)
            return (obj, val) =>
            {
                field.SetValue(obj, val);
                return obj;
            };

        var target = Expression.Parameter(typeof(object));
        var value = Expression.Parameter(typeof(object));
        var expr = Expression.Assign(
            Expression.Field(Expression.Convert(target, field.DeclaringType!), field),
            Expression.Convert(value, field.FieldType));
        var lambda = Expression.Lambda<Action<object, object?>>(expr, target, value);
        var compiled = lambda.Compile();
        return (obj, val) =>
        {
            compiled(obj, val);
            return obj;
        };
    }

    private static Func<object, object?, object> CreatePropertySetter(PropertyInfo property)
    {
        if (!s_canCompileExpression)
            return (obj, val) =>
            {
                property.SetValue(obj, val);
                return obj;
            };

        var target = Expression.Parameter(typeof(object));
        var value = Expression.Parameter(typeof(object));
        var expr = Expression.Assign(
            Expression.Property(Expression.Convert(target, property.DeclaringType!), property),
            Expression.Convert(value, property.PropertyType));
        var lambda = Expression.Lambda<Action<object, object?>>(expr, target, value);
        var compiled = lambda.Compile();
        return (obj, val) =>
        {
            compiled(obj, val);
            return obj;
        };
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
