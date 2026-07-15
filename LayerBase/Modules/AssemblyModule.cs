using LayerBase.DI;
using LayerBase.Scope;
using LayerBase.Tools;

namespace LayerBase.Modules;

public readonly struct AssemblyModuleId : IComparable<AssemblyModuleId>, IEquatable<AssemblyModuleId>
{
    public AssemblyModuleId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Assembly module id is required.", nameof(value));

        Value = value;
    }

    public string Value { get; }

    public int CompareTo(AssemblyModuleId other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    public bool Equals(AssemblyModuleId other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is AssemblyModuleId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AssemblyModuleId(string value)
    {
        return new AssemblyModuleId(value);
    }
}

public interface IAssemblyModule
{
    AssemblyModuleId Id { get; }

    AssemblyModuleManifest Manifest { get; }
}

public sealed class AssemblyModuleManifest
{
    public AssemblyModuleManifest(AssemblyModuleId moduleId, params ServiceContribution[] services)
        : this(
            moduleId,
            services,
            Array.Empty<ContextContribution>(),
            Array.Empty<LocalCallContribution>(),
            Array.Empty<EventHandlerContribution>(),
            Array.Empty<LayerToolContribution>())
    {
    }

    public AssemblyModuleManifest(
        AssemblyModuleId moduleId,
        ServiceContribution[] services,
        ContextContribution[] contexts,
        LocalCallContribution[] localCalls,
        LayerToolContribution[] tools)
        : this(
            moduleId,
            services,
            contexts,
            localCalls,
            Array.Empty<EventHandlerContribution>(),
            tools)
    {
    }

    public AssemblyModuleManifest(
        AssemblyModuleId moduleId,
        ServiceContribution[] services,
        ContextContribution[] contexts,
        LocalCallContribution[] localCalls,
        EventHandlerContribution[] eventHandlers,
        LayerToolContribution[] tools)
    {
        ModuleId = moduleId;
        Services = services ?? Array.Empty<ServiceContribution>();
        Contexts = contexts ?? Array.Empty<ContextContribution>();
        LocalCalls = localCalls ?? Array.Empty<LocalCallContribution>();
        EventHandlers = eventHandlers ?? Array.Empty<EventHandlerContribution>();
        Tools = tools ?? Array.Empty<LayerToolContribution>();
    }

    public AssemblyModuleId ModuleId { get; }

    public IReadOnlyList<ServiceContribution> Services { get; }

    public IReadOnlyList<ContextContribution> Contexts { get; }

    public IReadOnlyList<LocalCallContribution> LocalCalls { get; }

    public IReadOnlyList<EventHandlerContribution> EventHandlers { get; }

    public IReadOnlyList<LayerToolContribution> Tools { get; }

    public static AssemblyModuleManifest Empty(AssemblyModuleId moduleId)
    {
        return new AssemblyModuleManifest(moduleId);
    }
}

public readonly struct ContextContribution
{
    private ContextContribution(
        Type contextType,
        Type ownerServiceType,
        Type? ownerLayerType,
        Type? ownerScopeType)
    {
        ContextType = contextType ?? throw new ArgumentNullException(nameof(contextType));
        OwnerServiceType = ownerServiceType ?? throw new ArgumentNullException(nameof(ownerServiceType));
        OwnerLayerType = ownerLayerType;
        OwnerScopeType = ownerScopeType;
    }

    public Type ContextType { get; }

    public Type OwnerServiceType { get; }

    public Type? OwnerLayerType { get; }

    public Type? OwnerScopeType { get; }

    public static ContextContribution ForTypes(
        Type contextType,
        Type ownerServiceType,
        Type? ownerLayerType,
        Type? ownerScopeType)
    {
        return new ContextContribution(contextType, ownerServiceType, ownerLayerType, ownerScopeType);
    }
}

public readonly struct LocalCallContribution
{
    private LocalCallContribution(
        Type requestType,
        Type responseType,
        Type handlerType,
        Type? ownerLayerType,
        Type? ownerScopeType)
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        OwnerLayerType = ownerLayerType;
        OwnerScopeType = ownerScopeType;
    }

    public Type RequestType { get; }

    public Type ResponseType { get; }

    public Type HandlerType { get; }

    public Type? OwnerLayerType { get; }

    public Type? OwnerScopeType { get; }

    public static LocalCallContribution ForTypes(
        Type requestType,
        Type responseType,
        Type handlerType,
        Type? ownerLayerType,
        Type? ownerScopeType)
    {
        return new LocalCallContribution(requestType, responseType, handlerType, ownerLayerType, ownerScopeType);
    }
}

public readonly struct LayerToolContribution
{
    private LayerToolContribution(
        Type contractType,
        Type implementationType,
        string localKey,
        Type? ownerLayerType,
        bool cache,
        LayerToolFactoryInvoker? factory)
    {
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        LocalKey = string.IsNullOrWhiteSpace(localKey)
            ? throw new ArgumentException("Tool local key is required.", nameof(localKey))
            : localKey;
        OwnerLayerType = ownerLayerType;
        Cache = cache;
        Factory = factory;
    }

    public Type ContractType { get; }

    public Type ImplementationType { get; }

    public string LocalKey { get; }

    public Type? OwnerLayerType { get; }

    public bool Cache { get; }

    public LayerToolFactoryInvoker? Factory { get; }

    public static LayerToolContribution ForTypes(
        Type contractType,
        Type implementationType,
        string localKey,
        Type? ownerLayerType,
        bool cache = true)
    {
        return new LayerToolContribution(contractType, implementationType, localKey, ownerLayerType, cache, null);
    }

    public static LayerToolContribution ForFactory(
        Type contractType,
        Type implementationType,
        string localKey,
        Type? ownerLayerType,
        bool cache,
        LayerToolFactoryInvoker factory)
    {
        return new LayerToolContribution(contractType, implementationType, localKey, ownerLayerType, cache,
            factory ?? throw new ArgumentNullException(nameof(factory)));
    }
}

public readonly struct EventHandlerContribution
{
    private EventHandlerContribution(
        Type eventType,
        Type handlerType,
        Type ownerServiceType,
        Type? ownerLayerType,
        Type? ownerScopeType)
    {
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        OwnerServiceType = ownerServiceType ?? throw new ArgumentNullException(nameof(ownerServiceType));
        OwnerLayerType = ownerLayerType;
        OwnerScopeType = ownerScopeType;
    }

    public Type EventType { get; }

    public Type HandlerType { get; }

    public Type OwnerServiceType { get; }

    public Type? OwnerLayerType { get; }

    public Type? OwnerScopeType { get; }

    public static EventHandlerContribution ForTypes(
        Type eventType,
        Type handlerType,
        Type ownerServiceType,
        Type? ownerLayerType,
        Type? ownerScopeType)
    {
        return new EventHandlerContribution(eventType, handlerType, ownerServiceType, ownerLayerType, ownerScopeType);
    }
}

public readonly struct ServiceContribution
{
    private ServiceContribution(
        Type serviceType,
        Type implementationType,
        Type? ownerLayerType,
        Type? ownerScopeType,
        ServiceLifetime lifetime)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        OwnerLayerType = ownerLayerType;
        OwnerScopeType = ownerScopeType;
        Lifetime = lifetime;
    }

    public Type ServiceType { get; }

    public Type ImplementationType { get; }

    public Type? OwnerLayerType { get; }

    public Type? OwnerScopeType { get; }

    public ServiceLifetime Lifetime { get; }

    public static ServiceContribution ForTypes(
        Type serviceType,
        Type implementationType,
        Type? ownerLayerType,
        Type? ownerScopeType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        return new ServiceContribution(serviceType, implementationType, ownerLayerType, ownerScopeType, lifetime);
    }
}

internal sealed class CompositionContributions
{
    public CompositionContributions(
        ServiceContributionPlan[] services,
        ContextContributionPlan[] contexts,
        LocalCallContributionPlan[] localCalls,
        EventHandlerContributionPlan[] eventHandlers,
        LayerToolContributionPlan[] tools)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        LocalCalls = localCalls ?? throw new ArgumentNullException(nameof(localCalls));
        EventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
        Tools = tools ?? throw new ArgumentNullException(nameof(tools));
    }

    public ServiceContributionPlan[] Services { get; }

    public ContextContributionPlan[] Contexts { get; }

    public LocalCallContributionPlan[] LocalCalls { get; }

    public EventHandlerContributionPlan[] EventHandlers { get; }

    public LayerToolContributionPlan[] Tools { get; }

    public static CompositionContributions Empty { get; } =
        new(
            Array.Empty<ServiceContributionPlan>(),
            Array.Empty<ContextContributionPlan>(),
            Array.Empty<LocalCallContributionPlan>(),
            Array.Empty<EventHandlerContributionPlan>(),
            Array.Empty<LayerToolContributionPlan>());
}

internal readonly struct ServiceContributionPlan
{
    public ServiceContributionPlan(
        AssemblyModuleId moduleId,
        Type serviceType,
        Type implementationType,
        Type ownerLayerType,
        Type ownerScopeType,
        ServiceLifetime lifetime,
        int serviceIndex)
    {
        ModuleId = moduleId;
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        OwnerLayerType = ownerLayerType ?? throw new ArgumentNullException(nameof(ownerLayerType));
        OwnerScopeType = ownerScopeType ?? throw new ArgumentNullException(nameof(ownerScopeType));
        Lifetime = lifetime;
        ServiceIndex = serviceIndex;
    }

    public AssemblyModuleId ModuleId { get; }

    public Type ServiceType { get; }

    public Type ImplementationType { get; }

    public Type OwnerLayerType { get; }

    public Type OwnerScopeType { get; }

    public ServiceLifetime Lifetime { get; }

    public int ServiceIndex { get; }
}

internal readonly struct ContextContributionPlan
{
    public ContextContributionPlan(
        AssemblyModuleId moduleId,
        Type contextType,
        Type ownerServiceType,
        Type ownerLayerType,
        Type ownerScopeType,
        int contextIndex)
    {
        ModuleId = moduleId;
        ContextType = contextType ?? throw new ArgumentNullException(nameof(contextType));
        OwnerServiceType = ownerServiceType ?? throw new ArgumentNullException(nameof(ownerServiceType));
        OwnerLayerType = ownerLayerType ?? throw new ArgumentNullException(nameof(ownerLayerType));
        OwnerScopeType = ownerScopeType ?? throw new ArgumentNullException(nameof(ownerScopeType));
        ContextIndex = contextIndex;
    }

    public AssemblyModuleId ModuleId { get; }

    public Type ContextType { get; }

    public Type OwnerServiceType { get; }

    public Type OwnerLayerType { get; }

    public Type OwnerScopeType { get; }

    public int ContextIndex { get; }
}

internal readonly struct LocalCallContributionPlan
{
    public LocalCallContributionPlan(
        AssemblyModuleId moduleId,
        Type requestType,
        Type responseType,
        Type handlerType,
        Type ownerLayerType,
        Type ownerScopeType,
        int localCallIndex)
    {
        ModuleId = moduleId;
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        OwnerLayerType = ownerLayerType ?? throw new ArgumentNullException(nameof(ownerLayerType));
        OwnerScopeType = ownerScopeType ?? throw new ArgumentNullException(nameof(ownerScopeType));
        LocalCallIndex = localCallIndex;
    }

    public AssemblyModuleId ModuleId { get; }

    public Type RequestType { get; }

    public Type ResponseType { get; }

    public Type HandlerType { get; }

    public Type OwnerLayerType { get; }

    public Type OwnerScopeType { get; }

    public int LocalCallIndex { get; }
}

internal readonly struct LayerToolContributionPlan
{
    public LayerToolContributionPlan(
        AssemblyModuleId moduleId,
        Type contractType,
        Type implementationType,
        string localKey,
        Type ownerLayerType,
        bool cache,
        LayerToolFactoryInvoker? factory,
        int toolIndex)
    {
        ModuleId = moduleId;
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
        OwnerLayerType = ownerLayerType ?? throw new ArgumentNullException(nameof(ownerLayerType));
        Cache = cache;
        Factory = factory;
        ToolIndex = toolIndex;
    }

    public AssemblyModuleId ModuleId { get; }

    public Type ContractType { get; }

    public Type ImplementationType { get; }

    public string LocalKey { get; }

    public Type OwnerLayerType { get; }

    public bool Cache { get; }

    public LayerToolFactoryInvoker? Factory { get; }

    public int ToolIndex { get; }
}

internal readonly struct EventHandlerContributionPlan
{
    public EventHandlerContributionPlan(
        AssemblyModuleId moduleId,
        Type eventType,
        Type handlerType,
        Type ownerServiceType,
        Type ownerLayerType,
        Type ownerScopeType,
        int eventHandlerIndex)
    {
        ModuleId = moduleId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        OwnerServiceType = ownerServiceType ?? throw new ArgumentNullException(nameof(ownerServiceType));
        OwnerLayerType = ownerLayerType ?? throw new ArgumentNullException(nameof(ownerLayerType));
        OwnerScopeType = ownerScopeType ?? throw new ArgumentNullException(nameof(ownerScopeType));
        EventHandlerIndex = eventHandlerIndex;
    }

    public AssemblyModuleId ModuleId { get; }

    public Type EventType { get; }

    public Type HandlerType { get; }

    public Type OwnerServiceType { get; }

    public Type OwnerLayerType { get; }

    public Type OwnerScopeType { get; }

    public int EventHandlerIndex { get; }
}

internal static class AssemblyModuleComposer
{
    public static CompositionContributions Compose(IReadOnlyList<IAssemblyModule> modules)
    {
        if (modules == null)
            throw new ArgumentNullException(nameof(modules));

        if (modules.Count == 0)
            return CompositionContributions.Empty;

        var uniqueIds = new HashSet<AssemblyModuleId>();
        foreach (var module in modules)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(modules), "Assembly module list contains null.");

            if (!uniqueIds.Add(module.Id))
                throw new InvalidOperationException($"Assembly module `{module.Id}` is registered more than once.");
        }

        var servicePlans = new List<ServiceContributionPlan>();
        var contextPlans = new List<ContextContributionPlan>();
        var localCallPlans = new List<LocalCallContributionPlan>();
        var eventHandlerPlans = new List<EventHandlerContributionPlan>();
        var toolPlans = new List<LayerToolContributionPlan>();
        foreach (var module in modules.OrderBy(static module => module.Id))
        {
            var manifest = module.Manifest ?? throw new InvalidOperationException(
                $"Assembly module `{module.Id}` returned a null manifest.");

            foreach (var contribution in manifest.Services.OrderBy(static service => service.ServiceType.FullName, StringComparer.Ordinal))
            {
                if (contribution.OwnerLayerType == null)
                    throw new InvalidOperationException(
                        $"Service contribution `{contribution.ServiceType.FullName}` from module `{module.Id}` must declare an owner layer.");

                if (contribution.OwnerScopeType == null)
                    throw new InvalidOperationException(
                        $"Service contribution `{contribution.ServiceType.FullName}` from module `{module.Id}` must declare an owner scope.");

                if (!typeof(IScopeDefinition).IsAssignableFrom(contribution.OwnerScopeType))
                    throw new InvalidOperationException(
                        $"Owner scope `{contribution.OwnerScopeType.FullName}` must implement {nameof(IScopeDefinition)}.");

                servicePlans.Add(new ServiceContributionPlan(
                    module.Id,
                    contribution.ServiceType,
                    contribution.ImplementationType,
                    contribution.OwnerLayerType,
                    contribution.OwnerScopeType,
                    contribution.Lifetime,
                    servicePlans.Count));
            }

            foreach (var contribution in manifest.Contexts.OrderBy(static context => context.ContextType.FullName, StringComparer.Ordinal))
            {
                ValidateOwner(module.Id, "Context", contribution.ContextType, contribution.OwnerLayerType, contribution.OwnerScopeType);
                contextPlans.Add(new ContextContributionPlan(
                    module.Id,
                    contribution.ContextType,
                    contribution.OwnerServiceType,
                    contribution.OwnerLayerType!,
                    contribution.OwnerScopeType!,
                    contextPlans.Count));
            }

            foreach (var contribution in manifest.LocalCalls.OrderBy(static call => call.RequestType.FullName, StringComparer.Ordinal)
                                                            .ThenBy(static call => call.ResponseType.FullName, StringComparer.Ordinal)
                                                            .ThenBy(static call => call.HandlerType.FullName, StringComparer.Ordinal))
            {
                ValidateOwner(module.Id, "LocalCall", contribution.HandlerType, contribution.OwnerLayerType, contribution.OwnerScopeType);
                localCallPlans.Add(new LocalCallContributionPlan(
                    module.Id,
                    contribution.RequestType,
                    contribution.ResponseType,
                    contribution.HandlerType,
                    contribution.OwnerLayerType!,
                    contribution.OwnerScopeType!,
                    localCallPlans.Count));
            }

            foreach (var contribution in manifest.EventHandlers.OrderBy(static handler => handler.EventType.FullName, StringComparer.Ordinal)
                                                               .ThenBy(static handler => handler.HandlerType.FullName, StringComparer.Ordinal)
                                                               .ThenBy(static handler => handler.OwnerServiceType.FullName, StringComparer.Ordinal))
            {
                ValidateOwner(module.Id, "EventHandler", contribution.HandlerType, contribution.OwnerLayerType, contribution.OwnerScopeType);
                eventHandlerPlans.Add(new EventHandlerContributionPlan(
                    module.Id,
                    contribution.EventType,
                    contribution.HandlerType,
                    contribution.OwnerServiceType,
                    contribution.OwnerLayerType!,
                    contribution.OwnerScopeType!,
                    eventHandlerPlans.Count));
            }

            foreach (var contribution in manifest.Tools.OrderBy(static tool => tool.ContractType.FullName, StringComparer.Ordinal)
                                                       .ThenBy(static tool => tool.LocalKey, StringComparer.Ordinal))
            {
                ValidateToolOwner(module.Id, contribution.ContractType, contribution.ImplementationType,
                    contribution.OwnerLayerType);
                toolPlans.Add(new LayerToolContributionPlan(
                    module.Id,
                    contribution.ContractType,
                    contribution.ImplementationType,
                    contribution.LocalKey,
                    contribution.OwnerLayerType!,
                    contribution.Cache,
                    contribution.Factory,
                    toolPlans.Count));
            }
        }

        return new CompositionContributions(
            servicePlans.ToArray(),
            contextPlans.ToArray(),
            localCallPlans.ToArray(),
            eventHandlerPlans.ToArray(),
            toolPlans.ToArray());
    }

    private static void ValidateOwner(
        AssemblyModuleId moduleId,
        string contributionKind,
        Type contributionType,
        Type? ownerLayerType,
        Type? ownerScopeType)
    {
        if (ownerLayerType == null)
            throw new InvalidOperationException(
                $"{contributionKind} contribution `{contributionType.FullName}` from module `{moduleId}` must declare an owner layer.");

        if (ownerScopeType == null)
            throw new InvalidOperationException(
                $"{contributionKind} contribution `{contributionType.FullName}` from module `{moduleId}` must declare an owner scope.");

        if (!typeof(IScopeDefinition).IsAssignableFrom(ownerScopeType))
            throw new InvalidOperationException(
                $"Owner scope `{ownerScopeType.FullName}` must implement {nameof(IScopeDefinition)}.");
    }

    private static void ValidateToolOwner(
        AssemblyModuleId moduleId,
        Type contractType,
        Type implementationType,
        Type? ownerLayerType)
    {
        if (ownerLayerType == null)
            throw new InvalidOperationException(
                $"Tool contribution `{contractType.FullName}` from module `{moduleId}` must declare an owner layer.");

        if (!contractType.IsAssignableFrom(implementationType))
            throw new InvalidOperationException(
                $"LayerTool implementation `{implementationType.FullName}` must implement contract `{contractType.FullName}`.");
    }
}
