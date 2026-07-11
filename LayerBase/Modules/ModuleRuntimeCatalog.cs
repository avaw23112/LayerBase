using LayerBase.Scope;

namespace LayerBase.Modules;

public sealed class ModuleRuntimeCatalog
{
    internal ModuleRuntimeCatalog(
        IReadOnlyList<ILayerBaseModule> modules,
        IReadOnlyDictionary<ILayerBaseModule, int> moduleSlots,
        IReadOnlyDictionary<RuntimeTypeHandle, LayerContractContribution> layerContracts,
        IReadOnlyDictionary<RuntimeTypeHandle, ScopeDefinitionContribution> scopeDefinitions,
        IReadOnlyDictionary<RuntimeTypeHandle, ScopeMessageContractContribution> messageContracts,
        IReadOnlyList<ServiceContribution> services,
        IReadOnlyList<ContextContribution> contexts,
        IReadOnlyList<ScopeHandlerContribution> handlers,
        IReadOnlyDictionary<RuntimeTypeHandle, int> scopeIds,
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceSlots,
        IReadOnlyDictionary<RuntimeTypeHandle, int> messageRouteIds,
        IReadOnlyList<ScopeCallRoute> callRoutes,
        IReadOnlyList<ScopeEventRoute> eventRoutes,
        IReadOnlyList<ScopeEventHandlerRoute> eventHandlerRoutes)
    {
        Modules = modules;
        ModuleSlots = moduleSlots;
        LayerContracts = layerContracts;
        ScopeDefinitions = scopeDefinitions;
        MessageContracts = messageContracts;
        Services = services;
        Contexts = contexts;
        Handlers = handlers;
        ScopeIds = scopeIds;
        ServiceSlots = serviceSlots;
        MessageRouteIds = messageRouteIds;
        CallRoutes = callRoutes;
        EventRoutes = eventRoutes;
        EventHandlerRoutes = eventHandlerRoutes;
    }

    public IReadOnlyList<ILayerBaseModule> Modules { get; }

    public IReadOnlyDictionary<ILayerBaseModule, int> ModuleSlots { get; }

    public IReadOnlyDictionary<RuntimeTypeHandle, LayerContractContribution> LayerContracts { get; }

    public IReadOnlyDictionary<RuntimeTypeHandle, ScopeDefinitionContribution> ScopeDefinitions { get; }

    public IReadOnlyDictionary<RuntimeTypeHandle, ScopeMessageContractContribution> MessageContracts { get; }

    public IReadOnlyList<ServiceContribution> Services { get; }

    public IReadOnlyList<ContextContribution> Contexts { get; }

    public IReadOnlyList<ScopeHandlerContribution> Handlers { get; }

    public IReadOnlyDictionary<RuntimeTypeHandle, int> ScopeIds { get; }

    public IReadOnlyDictionary<RuntimeTypeHandle, int> ServiceSlots { get; }

    public IReadOnlyDictionary<RuntimeTypeHandle, int> MessageRouteIds { get; }

    public IReadOnlyList<ScopeCallRoute> CallRoutes { get; }

    public IReadOnlyList<ScopeEventRoute> EventRoutes { get; }

    public IReadOnlyList<ScopeEventHandlerRoute> EventHandlerRoutes { get; }
}

public sealed class ModuleBuildException : InvalidOperationException
{
    public ModuleBuildException(string code, string message)
        : base(message)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; }
}
