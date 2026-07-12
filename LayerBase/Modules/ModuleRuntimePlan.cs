using LayerBase.Scope;
using LayerBase.Scope.Resources;

namespace LayerBase.Modules;

internal sealed class ModuleRuntimePlan
{
    public ModuleSlot[] Modules { get; init; } = Array.Empty<ModuleSlot>();
    public ScopeRuntimePlan[] Scopes { get; init; } = Array.Empty<ScopeRuntimePlan>();
    public ModuleEventDispatchHandler[] EventDispatchers { get; init; } = Array.Empty<ModuleEventDispatchHandler>();
    public ModuleCallDispatchHandler[] CallDispatchers { get; init; } = Array.Empty<ModuleCallDispatchHandler>();
    public ServiceFactory[] ServiceFactories { get; init; } = Array.Empty<ServiceFactory>();
    public ContextFactory[] ContextFactories { get; init; } = Array.Empty<ContextFactory>();
    public ScopeResourceExportContribution[] ResourceExports { get; init; } = Array.Empty<ScopeResourceExportContribution>();
    public ScopeResourceImportContribution[] ResourceImports { get; init; } = Array.Empty<ScopeResourceImportContribution>();
}

internal readonly struct ModuleSlot
{
    public ModuleSlot(int moduleId, ILayerBaseModule module)
    {
        ModuleId = moduleId;
        Module = module ?? throw new ArgumentNullException(nameof(module));
    }

    public int ModuleId { get; }
    public ILayerBaseModule Module { get; }
}
