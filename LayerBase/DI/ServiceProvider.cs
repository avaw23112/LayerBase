using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using LayerBase;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Lifetime;
using LayerBase.Scope;

namespace LayerBase.DI;

internal sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly ConcurrentDictionary<ServiceKey, Lazy<object>> _instances = new();
    private readonly ConcurrentDictionary<ServiceKey, ServiceDescriptor> _map;
    private readonly Dictionary<Type, ServiceDescriptor[]> _descriptorsByType;
    private readonly LayerRuntime _runtime;
    private readonly Layer _ownerLayer;
    private readonly object _lifetimeGate = new();
    private readonly OwnedDisposableRegistry _disposables = new();
    [ThreadStatic]
    private static ResolutionContext? t_activeResolutionContext;
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

        var cleanup = new TerminalCleanupRunner();

        lock (_lifetimeGate)
        {
            _disposables.ReleaseAll(cleanup);
            _instances.Clear();
        }
    }

    public object? GetService(Type serviceType)
    {
        var context = t_activeResolutionContext ?? new ResolutionContext();
        return GetServiceInternal(serviceType, ScopeDefinitionIds.Main, context);
    }

    public T Get<T>()
    {
        var context = t_activeResolutionContext ?? new ResolutionContext();
        var service = GetServiceInternal(typeof(T), ScopeDefinitionIds.Main, context);
        if (service == null)
            throw new InvalidOperationException($"Service not registered: {typeof(T)}");
        return (T)service;
    }

    internal T Get<T>(int ownerScopeId)
    {
        var context = t_activeResolutionContext ?? new ResolutionContext();
        var service = GetServiceInternal(typeof(T), ownerScopeId, context);
        if (service == null)
            throw new InvalidOperationException($"Service not registered in scope {ownerScopeId}: {typeof(T)}");
        return (T)service;
    }

    internal object? GetService(Type serviceType, int ownerScopeId)
    {
        var context = t_activeResolutionContext ?? new ResolutionContext();
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
        var cleanup = new TerminalCleanupRunner();

        lock (_lifetimeGate)
        {
            var removedKeys = new List<ServiceKey>();

            foreach (KeyValuePair<ServiceKey, Lazy<object>> pair in _instances)
            {
                if (pair.Key.OwnerScopeId != ownerScopeId)
                    continue;
                removedKeys.Add(pair.Key);
            }

            foreach (ServiceKey key in removedKeys)
            {
                _instances.TryRemove(key, out _);
            }

            _disposables.ReleaseScope(ownerScopeId, cleanup);
        }
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
        var serviceType = descriptor.ServiceType;
        if (!context.CallStack.Add(serviceType))
            throw new InvalidOperationException($"Circular dependency detected: {serviceType}");

        var previousContext = t_activeResolutionContext;
        t_activeResolutionContext = context;

        try
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
        finally
        {
            t_activeResolutionContext = previousContext;
            context.CallStack.Remove(serviceType);
        }
    }

    private object GetOrCreateCached(ServiceDescriptor descriptor, ResolutionContext context)
    {
        var key = new ServiceKey(descriptor.OwnerScopeId, descriptor.ServiceType);

        if (_instances.TryGetValue(key, out var existing))
            return existing.Value;

        lock (_lifetimeGate)
        {
            if (_instances.TryGetValue(key, out existing))
                return existing.Value;

            var instance = CreateInstance(descriptor, context);
            var lazy = new Lazy<object>(() => instance);
            _instances[key] = lazy;
            return instance;
        }
    }

    private object CreateInstance(ServiceDescriptor descriptor, ResolutionContext context)
    {
        if (descriptor.Lifetime == ServiceLifetime.Instance)
        {
            RegisterDisposable(descriptor.Instance!, descriptor.OwnerScopeId);
            return descriptor.Instance!;
        }

        if (descriptor.ImplType == null && descriptor.Factory == null)
            throw new InvalidOperationException($"No implementation for {descriptor.ServiceType}");

        var implementationType = descriptor.ImplType ?? descriptor.ServiceType;

        bool needUnlock = false;

        if (!Monitor.IsEntered(_lifetimeGate))
        {
            Monitor.Enter(_lifetimeGate);
            needUnlock = true;
        }

        try
        {
            if (descriptor.Factory != null)
            {
                var factoryResult = descriptor.Factory(this);
                RegisterDisposable(factoryResult, descriptor.OwnerScopeId);
                return factoryResult;
            }

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
            RegisterDisposable(instance, descriptor.OwnerScopeId);
            return instance;
        }
        finally
        {
            if (needUnlock)
                Monitor.Exit(_lifetimeGate);
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

    private void RegisterDisposable(object instance, int ownerScopeId)
    {
        if (instance is IDisposable or IAsyncDisposable)
        {
            _disposables.Register(instance, ownerScopeId);
        }
    }

    private sealed class ResolutionContext
    {
        public readonly HashSet<Type> CallStack = new();
    }

    private sealed class ReferenceIdentityComparer :
        IEqualityComparer<object>
    {
        public static readonly ReferenceIdentityComparer Instance =
            new();

        private ReferenceIdentityComparer()
        {
        }

        public new bool Equals(
            object? left,
            object? right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(
            object instance)
        {
            return RuntimeHelpers.GetHashCode(instance);
        }
    }

    private static void DisposeUniqueInstances(
        IEnumerable<Lazy<object>> values)
    {
        var disposed =
            new HashSet<object>(
                ReferenceIdentityComparer.Instance);

        foreach (Lazy<object> lazy in values)
        {
            if (!lazy.IsValueCreated)
                continue;

            object instance = lazy.Value;

            if (!disposed.Add(instance))
                continue;

            if (instance is IDisposable disposable)
                disposable.Dispose();
        }
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
