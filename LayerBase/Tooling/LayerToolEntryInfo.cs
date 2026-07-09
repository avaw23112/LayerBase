namespace LayerBase.Tooling;

public sealed class LayerToolEntryInfo
{
    public LayerToolEntryInfo(
        string toolId,
        Type contractType,
        Type implementationType,
        string key,
        string? path,
        bool cache,
        bool hasCachedValue,
        Type? ownerLayerType,
        Type? ownerServiceType,
        Type? ownerManagerType)
    {
        ToolId = toolId ?? throw new ArgumentNullException(nameof(toolId));
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Path = path;
        Cache = cache;
        HasCachedValue = hasCachedValue;
        OwnerLayerType = ownerLayerType;
        OwnerServiceType = ownerServiceType;
        OwnerManagerType = ownerManagerType;
    }

    public string ToolId { get; }

    public Type ContractType { get; }

    public Type ImplementationType { get; }

    public string Key { get; }

    public string? Path { get; }

    public bool Cache { get; }

    public bool HasCachedValue { get; }

    public Type? OwnerLayerType { get; }

    public Type? OwnerServiceType { get; }

    public Type? OwnerManagerType { get; }
}
