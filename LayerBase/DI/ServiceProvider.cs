using System.Reflection;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.DI;

internal sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly Dictionary<int, ScopeServiceProvider> _scopeProviders;
    private readonly Dictionary<Type, ScopeServiceProvider[]> _providersByServiceType;
    private readonly LayerRuntime _runtime;
    private readonly Layer _ownerLayer;

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

        _scopeProviders = descriptors
            .GroupBy(static descriptor => descriptor.OwnerScopeId)
            .ToDictionary(
                static group => group.Key,
                group => new ScopeServiceProvider(this, group.Key, group));

        _scopeProviders.TryAdd(
            ScopeDefinitionIds.Main,
            new ScopeServiceProvider(this, ScopeDefinitionIds.Main, Array.Empty<ServiceDescriptor>()));

        _providersByServiceType = _scopeProviders.Values
            .SelectMany(static provider => provider.ServiceTypes.Select(type => new { type, provider }))
            .GroupBy(static entry => entry.type)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static entry => entry.provider)
                                     .Distinct()
                                     .ToArray());
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
        return GetDefaultProvider(serviceType).GetService(serviceType);
    }

    public T Get<T>()
    {
        return GetDefaultProvider(typeof(T)).Get<T>();
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
        foreach (var descriptor in orderedDescriptors)
        {
            var instance = GetProvider(descriptor.OwnerScopeId).GetService(descriptor.ServiceType);
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
        int ownerScopeId = ScopeDefinitionIds.Main;
        var binding = ServiceLayerBinder.GetBinding(instance);
        if (binding != null &&
            binding.RuntimeId == _runtime.Id &&
            binding.LayerIndex == _ownerLayer.RouteIndex)
        {
            ownerScopeId = binding.OwnerScope.ScopeId;
        }

        InjectMembers(GetProvider(ownerScopeId), instance);
    }

    internal object Resolve(
        ScopeServiceProvider scopeProvider,
        ServiceDescriptor descriptor)
    {
        var context = t_activeResolutionContext ?? new ResolutionContext();
        return Resolve(scopeProvider, descriptor, context);
    }

    private object Resolve(
        ScopeServiceProvider scopeProvider,
        ServiceDescriptor descriptor,
        ResolutionContext context)
    {
        var serviceKey = new ServiceKey(descriptor.OwnerScopeId, descriptor.ServiceType);
        if (!context.CallStack.Add(serviceKey))
            throw new InvalidOperationException($"Circular dependency detected: {descriptor.ServiceType}");

        var previousContext = t_activeResolutionContext;
        t_activeResolutionContext = context;

        try
        {
            object instance = descriptor.Lifetime switch
            {
                ServiceLifetime.Instance => scopeProvider.GetOrCreateCached(descriptor, context),
                ServiceLifetime.Singleton => scopeProvider.GetOrCreateCached(descriptor, context),
                ServiceLifetime.Scoped => scopeProvider.GetOrCreateCached(descriptor, context),
                ServiceLifetime.Transient => CreateInstance(scopeProvider, descriptor, context),
                _ => throw new NotSupportedException($"Unsupported lifetime {descriptor.Lifetime}")
            };

            var existingBinding = ServiceLayerBinder.GetBinding(instance);
            if (existingBinding != null &&
                (existingBinding.RuntimeId != _runtime.Id ||
                 existingBinding.LayerIndex != _ownerLayer.RouteIndex ||
                 existingBinding.OwnerScope.ScopeId != descriptor.OwnerScopeId))
            {
                throw new InvalidOperationException(
                    $"Service instance {instance.GetType().Name} is already bound to another Scope provider.");
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
            context.CallStack.Remove(serviceKey);
        }
    }

    internal object CreateInstance(
        ScopeServiceProvider scopeProvider,
        ServiceDescriptor descriptor,
        ResolutionContext context)
    {
        if (descriptor.Lifetime == ServiceLifetime.Instance)
        {
            scopeProvider.RegisterResource(descriptor.Instance!);
            return descriptor.Instance!;
        }

        if (descriptor.ImplType == null && descriptor.Factory == null)
            throw new InvalidOperationException($"No implementation for {descriptor.ServiceType}");

        if (descriptor.Factory != null)
        {
            var factoryResult = descriptor.Factory(scopeProvider);
            scopeProvider.RegisterResource(factoryResult);
            return factoryResult;
        }

        var ctor = SelectConstructor(descriptor.ImplType!);
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var dependency = scopeProvider.GetService(parameters[i].ParameterType, context);
            if (dependency == null)
                throw new InvalidOperationException(
                    $"Unable to resolve dependency {parameters[i].ParameterType} for {descriptor.ImplType}");

            args[i] = dependency;
        }

        var instance = ctor.Invoke(args);
        InjectMembers(scopeProvider, instance);
        scopeProvider.RegisterResource(instance);
        return instance;
    }

    private ScopeServiceProvider GetProvider(int ownerScopeId)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(ServiceProvider));

        if (_scopeProviders.TryGetValue(ownerScopeId, out ScopeServiceProvider? provider))
            return provider;

        throw new InvalidOperationException($"Scope service provider not found for scope {ownerScopeId}.");
    }

    private ScopeServiceProvider GetDefaultProvider(Type serviceType)
    {
        ScopeServiceProvider mainProvider = GetProvider(ScopeDefinitionIds.Main);
        if (mainProvider.Contains(serviceType))
            return mainProvider;

        if (_providersByServiceType.TryGetValue(serviceType, out ScopeServiceProvider[]? providers) &&
            providers.Length == 1)
        {
            return providers[0];
        }

        return mainProvider;
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

    private static void InjectMembers(IServiceProvider services, object instance)
    {
        if (instance is IGeneratedMountInject generatedMount)
            generatedMount.__InjectMounts(services);
    }

    internal sealed class ResolutionContext
    {
        public readonly HashSet<ServiceKey> CallStack = new();
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
