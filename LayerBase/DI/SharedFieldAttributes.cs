namespace LayerBase.DI;

public enum PublicType
{
    Global,
    Layer,
    Service
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class PublicAttribute : Attribute
{
    public PublicAttribute(PublicType scope, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Shared field key cannot be null or whitespace.", nameof(key));

        Scope = scope;
        Key = key;
    }

    public PublicType Scope { get; }
    public string Key { get; }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class FromAttribute : Attribute
{
    public FromAttribute(PublicType scope, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Shared field key cannot be null or whitespace.", nameof(key));

        Scope = scope;
        Key = key;
    }

    public PublicType Scope { get; }
    public string Key { get; }
}
