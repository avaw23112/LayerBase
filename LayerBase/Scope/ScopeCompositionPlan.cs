using LayerBase.DI;
using LayerBase.Modules;
using LayerBase.Scope.Resources;

namespace LayerBase.Scope;

public sealed class ScopeCompositionPlan
{
    internal ScopeCompositionPlan(
        ScopePlan[] scopes,
        ScopeCallRoute[] callRoutes,
        ScopeEventRoute[] eventRoutes,
        ScopeEventHandlerRoute[] eventHandlerRoutes,
        IReadOnlyDictionary<RuntimeTypeHandle, int>? messageRouteIds = null)
    {
        Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        CallRoutes = callRoutes ?? throw new ArgumentNullException(nameof(callRoutes));
        EventRoutes = eventRoutes ?? throw new ArgumentNullException(nameof(eventRoutes));
        EventHandlerRoutes = eventHandlerRoutes ?? throw new ArgumentNullException(nameof(eventHandlerRoutes));
        MessageRouteIds = messageRouteIds ?? new Dictionary<RuntimeTypeHandle, int>();
    }

    public ScopePlan[] Scopes { get; }

    public ScopeCallRoute[] CallRoutes { get; }

    public ScopeEventRoute[] EventRoutes { get; }

    public ScopeEventHandlerRoute[] EventHandlerRoutes { get; }

    public IReadOnlyDictionary<RuntimeTypeHandle, int> MessageRouteIds { get; }
}

public sealed class ScopePlan
{
    internal ScopePlan(
        ScopeDescriptor descriptor,
        Type? scopeType,
        ScopeServicePlan[] services,
        ScopeContextPlan[] contexts,
        ScopeResourcePlan? resourcePlan = null)
    {
        Descriptor = descriptor;
        ScopeType = scopeType;
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        ResourcePlan = resourcePlan ?? ScopeResourcePlan.Empty;
    }

    public ScopeDescriptor Descriptor { get; }

    public Type? ScopeType { get; }

    public ScopeServicePlan[] Services { get; }

    public ScopeContextPlan[] Contexts { get; }

    internal ScopeResourcePlan ResourcePlan { get; }
}

public readonly struct ScopeServicePlan
{
    public ScopeServicePlan(
        int serviceSlot,
        Type? serviceType,
        IService instance,
        ServiceBindingInitializer? bindingInitializer,
        LayerMembership membership = default)
    {
        ServiceSlot = serviceSlot;
        ServiceType = serviceType;
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        BindingInitializer = bindingInitializer;
        Membership = membership;
    }

    public int ServiceSlot { get; }

    public Type? ServiceType { get; }

    public IService Instance { get; }

    public ServiceBindingInitializer? BindingInitializer { get; }

    public LayerMembership Membership { get; }
}

public readonly struct ScopeContextPlan
{
    public ScopeContextPlan(
        int contextSlot,
        Type? contextType,
        int ownerServiceSlot,
        ILayerContext instance,
        LayerMembership membership = default)
    {
        ContextSlot = contextSlot;
        ContextType = contextType;
        OwnerServiceSlot = ownerServiceSlot;
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Membership = membership;
    }

    public int ContextSlot { get; }

    public Type? ContextType { get; }

    public int OwnerServiceSlot { get; }

    public ILayerContext Instance { get; }

    public LayerMembership Membership { get; }
}
