using LayerBase.DI;
using LayerBase.Scope;
using LayerBase.Scope.Resources;

namespace LayerBase.Modules;

public interface ILayerBaseModule
{
    ModuleManifest Manifest { get; }
}

public sealed class ModuleManifest
{
    public static readonly ModuleManifest Empty = new(
        Array.Empty<LayerContractContribution>(),
        Array.Empty<ScopeDefinitionContribution>(),
        Array.Empty<ScopeMessageContractContribution>(),
        Array.Empty<ServiceContribution>(),
        Array.Empty<ContextContribution>(),
        Array.Empty<ScopeHandlerContribution>(),
        Array.Empty<ScopeResourceExportContribution>(),
        Array.Empty<ScopeResourceImportContribution>());

    public ModuleManifest(
        IReadOnlyList<LayerContractContribution> layerContracts,
        IReadOnlyList<ScopeDefinitionContribution> scopeDefinitions,
        IReadOnlyList<ScopeMessageContractContribution> messageContracts,
        IReadOnlyList<ServiceContribution> services,
        IReadOnlyList<ContextContribution> contexts,
        IReadOnlyList<ScopeHandlerContribution> handlers)
        : this(
            layerContracts,
            scopeDefinitions,
            messageContracts,
            services,
            contexts,
            handlers,
            Array.Empty<ScopeResourceExportContribution>(),
            Array.Empty<ScopeResourceImportContribution>())
    {
    }

    public ModuleManifest(
        IReadOnlyList<LayerContractContribution> layerContracts,
        IReadOnlyList<ScopeDefinitionContribution> scopeDefinitions,
        IReadOnlyList<ScopeMessageContractContribution> messageContracts,
        IReadOnlyList<ServiceContribution> services,
        IReadOnlyList<ContextContribution> contexts,
        IReadOnlyList<ScopeHandlerContribution> handlers,
        IReadOnlyList<ScopeResourceExportContribution> resourceExports,
        IReadOnlyList<ScopeResourceImportContribution> resourceImports)
    {
        LayerContracts = layerContracts ?? throw new ArgumentNullException(nameof(layerContracts));
        ScopeDefinitions = scopeDefinitions ?? throw new ArgumentNullException(nameof(scopeDefinitions));
        MessageContracts = messageContracts ?? throw new ArgumentNullException(nameof(messageContracts));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        Handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        ResourceExports = resourceExports ?? throw new ArgumentNullException(nameof(resourceExports));
        ResourceImports = resourceImports ?? throw new ArgumentNullException(nameof(resourceImports));
    }

    public IReadOnlyList<LayerContractContribution> LayerContracts { get; }

    public IReadOnlyList<ScopeDefinitionContribution> ScopeDefinitions { get; }

    public IReadOnlyList<ScopeMessageContractContribution> MessageContracts { get; }

    public IReadOnlyList<ServiceContribution> Services { get; }

    public IReadOnlyList<ContextContribution> Contexts { get; }

    public IReadOnlyList<ScopeHandlerContribution> Handlers { get; }

    public IReadOnlyList<ScopeResourceExportContribution> ResourceExports { get; }

    public IReadOnlyList<ScopeResourceImportContribution> ResourceImports { get; }
}

public enum ScopeMessageKind
{
    Event,
    Call
}

public delegate IService ServiceFactory();

public delegate void ServiceBindingInitializer(IService service, ScopeRuntime ownerScope, int serviceSlot);

public delegate ILayerContext ContextFactory(IService ownerService);

public readonly struct LayerContractContribution
{
    public LayerContractContribution(RuntimeTypeHandle layerType)
    {
        LayerType = layerType;
    }

    public RuntimeTypeHandle LayerType { get; }
}

public readonly struct ScopeDefinitionContribution
{
    public ScopeDefinitionContribution(
        RuntimeTypeHandle scopeType,
        ScopeThreadingMode threading,
        ScopeClockMode clock,
        int tickRateHz,
        ScopeStopPolicy stopPolicy)
    {
        ScopeType = scopeType;
        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        StopPolicy = stopPolicy;
    }

    public RuntimeTypeHandle ScopeType { get; }

    public ScopeThreadingMode Threading { get; }

    public ScopeClockMode Clock { get; }

    public int TickRateHz { get; }

    public ScopeStopPolicy StopPolicy { get; }
}

public readonly struct ScopeMessageContractContribution
{
    public ScopeMessageContractContribution(
        RuntimeTypeHandle messageType,
        RuntimeTypeHandle targetScopeType,
        RuntimeTypeHandle resultType,
        ScopeMessageKind kind)
    {
        MessageType = messageType;
        TargetScopeType = targetScopeType;
        ResultType = resultType;
        Kind = kind;
    }

    public RuntimeTypeHandle MessageType { get; }

    public RuntimeTypeHandle TargetScopeType { get; }

    public RuntimeTypeHandle ResultType { get; }

    public ScopeMessageKind Kind { get; }
}

public readonly struct ServiceContribution
{
    public ServiceContribution(
        RuntimeTypeHandle serviceType,
        RuntimeTypeHandle[] ownerLayerTypes,
        RuntimeTypeHandle ownerScopeType,
        ServiceFactory? factory,
        ServiceBindingInitializer? bindingInitializer,
        int moduleLocalServiceId)
    {
        ServiceType = serviceType;
        OwnerLayerTypes = ownerLayerTypes ?? throw new ArgumentNullException(nameof(ownerLayerTypes));
        OwnerScopeType = ownerScopeType;
        Factory = factory;
        BindingInitializer = bindingInitializer;
        ModuleLocalServiceId = moduleLocalServiceId;
    }

    public RuntimeTypeHandle ServiceType { get; }

    public RuntimeTypeHandle[] OwnerLayerTypes { get; }

    public RuntimeTypeHandle OwnerScopeType { get; }

    public ServiceFactory? Factory { get; }

    public ServiceBindingInitializer? BindingInitializer { get; }

    public int ModuleLocalServiceId { get; }
}

public readonly struct ContextContribution
{
    public ContextContribution(
        RuntimeTypeHandle contextType,
        RuntimeTypeHandle ownerServiceType,
        ContextFactory? factory,
        int moduleLocalContextId)
    {
        ContextType = contextType;
        OwnerServiceType = ownerServiceType;
        Factory = factory;
        ModuleLocalContextId = moduleLocalContextId;
    }

    public RuntimeTypeHandle ContextType { get; }

    public RuntimeTypeHandle OwnerServiceType { get; }

    public ContextFactory? Factory { get; }

    public int ModuleLocalContextId { get; }
}

public readonly struct ScopeHandlerContribution
{
    public ScopeHandlerContribution(
        RuntimeTypeHandle messageType,
        RuntimeTypeHandle serviceType,
        RuntimeTypeHandle scopeType,
        int moduleLocalHandlerId,
        ScopeMessageKind kind)
    {
        MessageType = messageType;
        ServiceType = serviceType;
        ScopeType = scopeType;
        ModuleLocalHandlerId = moduleLocalHandlerId;
        Kind = kind;
    }

    public RuntimeTypeHandle MessageType { get; }

    public RuntimeTypeHandle ServiceType { get; }

    public RuntimeTypeHandle ScopeType { get; }

    public int ModuleLocalHandlerId { get; }

    public ScopeMessageKind Kind { get; }
}
