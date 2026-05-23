using System;

namespace LayerBase.DI.Options;

/// <summary>
/// Declares that the annotated context or event handler belongs to a specific IService domain.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OwnerServiceAttribute : Attribute
{
    public OwnerServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }

    public Type ServiceType { get; }
}