namespace LayerBase.DI;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ProvideAttribute : Attribute
{
    public ProvideAttribute(string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey))
        {
            throw new ArgumentException("Scope resource localKey cannot be null or whitespace.", nameof(localKey));
        }

        LocalKey = localKey;
    }

    public string LocalKey { get; }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class FromAttribute : Attribute
{
    public FromAttribute(Type providerType, string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey))
        {
            throw new ArgumentException("Scope resource localKey cannot be null or whitespace.", nameof(localKey));
        }

        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
        LocalKey = localKey;
    }

    public Type ProviderType { get; }

    public string LocalKey { get; }
}
