using System.Collections.Concurrent;
using System.Reflection;
using LayerBase;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.DI;

internal sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly ConcurrentDictionary<ServiceKey, Lazy<object>> _instances = new();
    private readonly ConcurrentDictionary<ServiceKey, ServiceDescriptor> _map;
    private readonly Dictionary<Type, ServiceDescriptor[]> _descriptorsByType;
    private readonly LayerRuntime _runtime;
    private readonly Layer _ownerLayer;
    private ResolutionContext? _activeResolutionContext;
    private int _disposed;

    public ServiceProvider(
        LayerRuntime runtime,
        IEnumerable<ServiceDescriptor> descriptors,
        Layer ownerLayer)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _ownerLayer = ownerLayer ?? throw new ArgumentNullException(nameof(ownerLayer));
        if (descriptors == null)
            throw new ArgumentNullException(nameof(descriptors));

        var descriptorArray = descriptors.ToArray();
        _map = new ConcurrentDictionary<ServiceKey, ServiceDescriptor>();
        foreach (var descriptor in descriptorArray)
            _map[new ServiceKey(descriptor.OwnerScopeId, descriptor.ServiceType)] = descriptor;

        _descriptorsByType = descriptorArray
            .GroupBy(static descriptor => descriptor.ServiceType)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray());
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
        return GetServiceInternal(serviceType, ScopeDefinitionIds.Main, context);
    }

    public T Get<T>()
    {
        var service = GetService(typeof(T));
        if (service == null)
            throw new InvalidOperationException($"Service not registered: {typeof(T)}");

        return (T)service;
    }

    internal T Get<T>(int ownerScopeId)
    {
        var service = GetService(typeof(T), ownerScopeId);
        if (service == null)
            throw new InvalidOperationException($"Service not registered in scope {ownerScopeId}: {typeof(T)}");

        return (T)service;
    }

    internal object? GetService(Type serviceType, int ownerScopeId)
    {
        var context = _activeResolutionContext ?? new ResolutionContext();
        return GetServiceInternal(serviceType, ownerScopeId, context);
    }

    internal List<IAutoSubscribe> InitializeAutoSubscriptions(
        Layer owner,
        IEnumerable<ServiceDescriptor> orderedDescriptors)
    {
        var discovered = new List<IAutoSubscribe>();
        foreach (var descriptor in orderedDescriptors)
        {
            var instance = GetService(descriptor.ServiceType, descriptor.OwnerScopeId);
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
            var instance = GetService(descriptor.ServiceType, descriptor.OwnerScopeId);
            if (instance != null)
                resolved.Add(new ResolvedService(descriptor, instance));
        }

        return resolved;
    }

    internal void DisposeScope(int ownerScopeId)
    {
        var keysToRemove = new List<ServiceKey>();
        foreach (var kvp in _instances)
        {
            if (kvp.Key.OwnerScopeId == ownerScopeId)
            {
                if (kvp.Value.IsValueCreated && kvp.Value.Value is IDisposable disposable)
                    disposable.Dispose();
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
            _instances.TryRemove(key, out _);
    }

    internal void InjectMembers(object instance)
    {
        InjectMembers(instance, new ResolutionContext());
    }

    private object? GetServiceInternal(Type serviceType, int ownerScopeId, ResolutionContext context)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(ServiceProvider));
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));

        return TryResolveDescriptor(serviceType, ownerScopeId, out var descriptor)
            ? Resolve(descriptor, context)
            : null;
    }

    private bool TryResolveDescriptor(Type serviceType, int ownerScopeId, out ServiceDescriptor descriptor)
    {
        if (_map.TryGetValue(new ServiceKey(ownerScopeId, serviceType), out descriptor!))
        {
            return true;
        }

        if (_map.TryGetValue(new ServiceKey(ScopeDefinitionIds.Main, serviceType), out descriptor!))
        {
            return true;
        }

        if (_descriptorsByType.TryGetValue(serviceType, out var candidates) &&
            candidates.Select(static candidate => candidate.OwnerScopeId).Distinct().Count() == 1)
        {
            descriptor = candidates[^1];
            return true;
        }

        descriptor = default!;
        return false;
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

        if (!_runtime.ScopeHost.TryGetRuntime(descriptor.OwnerScopeId, out var ownerScope))
        {
            throw new InvalidOperationException(
                $"Service `{descriptor.ServiceType.FullName}` targets unknown scope id {descriptor.OwnerScopeId}.");
        }

        ServiceLayerBinder.AttachScopeObject(instance, _ownerLayer, ownerScope);
        return instance;
    }

    private object GetOrCreateCached(ServiceDescriptor descriptor, ResolutionContext context)
    {
        var implementationType = descriptor.ImplType ?? descriptor.ServiceType;
        if (context.CallStack.Contains(implementationType))
            throw new InvalidOperationException($"Circular dependency detected: {implementationType}");

        var lazy = _instances.GetOrAdd(
            new ServiceKey(descriptor.OwnerScopeId, descriptor.ServiceType),
            _ => new Lazy<object>(
                () => CreateInstance(descriptor, context),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazy.Value;
        }
        catch
        {
            _instances.TryRemove(new ServiceKey(descriptor.OwnerScopeId, descriptor.ServiceType), out _);
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
                var dependency = GetServiceInternal(parameters[i].ParameterType, descriptor.OwnerScopeId, context);
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
        if (instance is IGeneratedMountInject generatedMount)
        {
            generatedMount.__InjectMounts(this);
        }
    }

    private sealed class ResolutionContext
    {
        public readonly HashSet<Type> CallStack = new();
    }

    internal readonly struct ServiceKey : IEquatable<ServiceKey>
    {
        private readonly int _ownerScopeId;
        private readonly Type _serviceType;

        public ServiceKey(int ownerScopeId, Type serviceType)
        {
            _ownerScopeId = ownerScopeId;
            _serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        }

        public int OwnerScopeId => _ownerScopeId;

        public bool Equals(ServiceKey other)
        {
            return _ownerScopeId == other._ownerScopeId &&
                   _serviceType == other._serviceType;
        }

        public override bool Equals(object? obj)
        {
            return obj is ServiceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_ownerScopeId, _serviceType);
        }
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
