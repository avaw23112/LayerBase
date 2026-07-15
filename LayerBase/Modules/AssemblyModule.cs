using LayerBase.DI;
using LayerBase.Scope;

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
    {
        ModuleId = moduleId;
        Services = services ?? Array.Empty<ServiceContribution>();
    }

    public AssemblyModuleId ModuleId { get; }

    public IReadOnlyList<ServiceContribution> Services { get; }

    public static AssemblyModuleManifest Empty(AssemblyModuleId moduleId)
    {
        return new AssemblyModuleManifest(moduleId);
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
    public CompositionContributions(ServiceContributionPlan[] services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public ServiceContributionPlan[] Services { get; }

    public static CompositionContributions Empty { get; } =
        new(Array.Empty<ServiceContributionPlan>());
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
        }

        return new CompositionContributions(servicePlans.ToArray());
    }
}
