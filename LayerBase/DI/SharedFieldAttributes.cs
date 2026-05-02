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
public sealed class FromAttribute : Attribute
{
    public FromAttribute(Type ownerType, string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey))
            throw new ArgumentException("Shared field localKey cannot be null or whitespace.", nameof(localKey));

        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        LocalKey = localKey;
    }

    public Type OwnerType { get; }
    public string LocalKey { get; }
}

