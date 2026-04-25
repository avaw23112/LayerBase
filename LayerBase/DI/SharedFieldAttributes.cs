using System;

namespace LayerBase.DI;

public struct GlobalScope
{
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class ProvideAttribute : Attribute
{
    public ProvideAttribute(Type ownerType, string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey))
            throw new ArgumentException("Shared field localKey cannot be null or whitespace.", nameof(localKey));

        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        LocalKey = localKey;
    }

    public Type OwnerType { get; }
    public string LocalKey { get; }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class UseAttribute : Attribute
{
    public UseAttribute(Type ownerType, string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey))
            throw new ArgumentException("Shared field localKey cannot be null or whitespace.", nameof(localKey));

        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        LocalKey = localKey;
    }

    public Type OwnerType { get; }
    public string LocalKey { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OwnerServiceAttribute : Attribute
{
    public OwnerServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }

    public Type ServiceType { get; }
}

