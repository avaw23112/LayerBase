using LayerBase.Scope;

namespace LayerBase.DI;

internal sealed class ServiceCatalog
{
    private readonly Dictionary<int, ScopeServicePlan> _plans;

    public ServiceCatalog(IEnumerable<ServiceDescriptor> descriptors)
    {
        if (descriptors == null)
            throw new ArgumentNullException(nameof(descriptors));

        _plans = descriptors
            .GroupBy(static descriptor => descriptor.OwnerScopeId)
            .ToDictionary(
                static group => group.Key,
                static group => ScopeServicePlan.Compile(group.Key, group));

        _plans.TryAdd(
            ScopeDefinitionIds.Main,
            ScopeServicePlan.Empty(ScopeDefinitionIds.Main));
    }

    public ScopeServicePlan GetPlanOrEmpty(int ownerScopeId)
    {
        if (_plans.TryGetValue(ownerScopeId, out ScopeServicePlan? plan))
            return plan;

        return ScopeServicePlan.Empty(ownerScopeId);
    }
}
