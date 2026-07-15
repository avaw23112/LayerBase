namespace LayerBase.DI;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ProvideAttribute : Attribute
{
    public ProvideAttribute(string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey))
            throw new ArgumentException("Shared field localKey cannot be null or whitespace.", nameof(localKey));

        LocalKey = localKey;
    }

    public string LocalKey { get; }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class FromAttribute : Attribute
{
    public FromAttribute(Type providerServiceType, string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey))
            throw new ArgumentException("Shared field localKey cannot be null or whitespace.", nameof(localKey));

        ProviderServiceType = providerServiceType ?? throw new ArgumentNullException(nameof(providerServiceType));
        LocalKey = localKey;
    }

    public Type ProviderServiceType { get; }
    public string LocalKey { get; }
}
