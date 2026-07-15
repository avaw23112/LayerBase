using LayerBase.DI;

namespace LayerBase.Modules;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AssemblyModuleAttribute : Attribute
{
    public AssemblyModuleAttribute(string? id = null)
    {
        Id = id;
    }

    public string? Id { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ModuleServiceAttribute : Attribute
{
    public ModuleServiceAttribute(
        Type ownerLayerType,
        Type ownerScopeType,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        OwnerLayerType = ownerLayerType ?? throw new ArgumentNullException(nameof(ownerLayerType));
        OwnerScopeType = ownerScopeType ?? throw new ArgumentNullException(nameof(ownerScopeType));
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Lifetime = lifetime;
    }

    public Type OwnerLayerType { get; }

    public Type OwnerScopeType { get; }

    public Type ServiceType { get; }

    public Type ImplementationType { get; }

    public ServiceLifetime Lifetime { get; }
}

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property |
    AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ModuleIgnoreAttribute : Attribute
{
}
