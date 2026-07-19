using System.Reflection;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.DI;

internal sealed class ScopeServiceProvider : IServiceProvider, IDisposable
{
    private readonly ScopeRuntime _ownerScope;
    private readonly ScopeServicePlan _plan;
    private readonly Layer _ownerLayer;
    private readonly object?[] _instances;
    private readonly ScopeOwnedResourceList _resources = new();
    private int _disposed;

    [ThreadStatic]
    private static ResolutionContext? t_activeResolutionContext;

    public ScopeServiceProvider(
        ScopeRuntime ownerScope,
        ScopeServicePlan plan,
        Layer ownerLayer)
    {
        _ownerScope = ownerScope ?? throw new ArgumentNullException(nameof(ownerScope));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _ownerLayer = ownerLayer ?? throw new ArgumentNullException(nameof(ownerLayer));
        _instances = new object?[plan.SlotCount];
    }

    public int OwnerScopeId => _ownerScope.ScopeId;

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    internal IEnumerable<Type> ServiceTypes => _plan.ServiceTypes;

    internal bool Contains(Type serviceType)
    {
        return _plan.Contains(serviceType);
    }

    public object? GetService(Type serviceType)
    {
        return GetService(serviceType, new ResolutionContext());
    }

    public T Get<T>()
    {
        object? service = GetService(typeof(T));
        if (service == null)
            throw new InvalidOperationException($"Service not registered in scope {OwnerScopeId}: {typeof(T)}");

        return (T)service;
    }

    internal object? GetService(
        Type serviceType,
        ResolutionContext context)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(ScopeServiceProvider));
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));

        RequireOwnerThreadIfBound();

        if (!_plan.TryGetDescriptor(serviceType, out int slot, out ServiceDescriptor? descriptor))
            return null;

        return Resolve(slot, descriptor, context);
    }

    internal void InjectMembers(object instance)
    {
        InjectMembers(this, instance);
    }

    internal object GetOrCreateCached(
        int slot,
        ServiceDescriptor descriptor,
        ResolutionContext context)
    {
        if (_instances[slot] is { } existing)
            return existing;

        object instance = CreateInstance(descriptor, context);
        _instances[slot] = instance;
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

        RequireOwnerThreadIfBound();
        _resources.ReleaseAll();
        Array.Clear(_instances,0,_instances.Length);
    }

    private object Resolve(
        int slot,
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
                ServiceLifetime.Instance => GetOrCreateCached(slot, descriptor, context),
                ServiceLifetime.Singleton => GetOrCreateCached(slot, descriptor, context),
                ServiceLifetime.Scoped => GetOrCreateCached(slot, descriptor, context),
                ServiceLifetime.Transient => CreateInstance(descriptor, context),
                _ => throw new NotSupportedException($"Unsupported lifetime {descriptor.Lifetime}")
            };

            AttachInstance(instance, descriptor);
            return instance;
        }
        finally
        {
            t_activeResolutionContext = previousContext;
            context.CallStack.Remove(serviceKey);
        }
    }

    private void RequireOwnerThreadIfBound()
    {
        if (_ownerScope.OwnerThreadId != 0)
            _ownerScope.RequireOwnerThread();
    }

    private object CreateInstance(
        ServiceDescriptor descriptor,
        ResolutionContext context)
    {
        if (descriptor.Lifetime == ServiceLifetime.Instance)
        {
            RegisterResource(descriptor.Instance!);
            return descriptor.Instance!;
        }

        if (descriptor.ImplType == null && descriptor.Factory == null)
            throw new InvalidOperationException($"No implementation for {descriptor.ServiceType}");

        if (descriptor.Factory != null)
        {
            object factoryResult = descriptor.Factory(this);
            RegisterResource(factoryResult);
            return factoryResult;
        }

        var ctor = SelectConstructor(descriptor.ImplType!);
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            object? dependency = GetService(parameters[i].ParameterType, context);
            if (dependency == null)
                throw new InvalidOperationException(
                    $"Unable to resolve dependency {parameters[i].ParameterType} for {descriptor.ImplType}");

            args[i] = dependency;
        }

        object instance = ctor.Invoke(args);
        InjectMembers(instance);
        RegisterResource(instance);
        return instance;
    }

    private void AttachInstance(
        object instance,
        ServiceDescriptor descriptor)
    {
        var existingBinding = ServiceLayerBinder.GetBinding(instance);
        if (existingBinding != null &&
            (existingBinding.LayerIndex != _ownerLayer.RouteIndex ||
             existingBinding.OwnerScope.ScopeId != OwnerScopeId))
        {
            throw new InvalidOperationException(
                $"Service instance {instance.GetType().Name} is already bound to another Scope provider.");
        }

        ServiceLayerBinder.AttachScopeObject(instance, _ownerLayer, _ownerScope);
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
}
