namespace LayerBase.Tooling;

public sealed class LayerToolEntry
{
    private readonly Func<LayerToolCreateContext, object> _factory;
    private object? _cached;

    public LayerToolEntry(
        Type contractType,
        Type implementationType,
        string toolId,
        string key,
        string? path,
        bool cache,
        Type? ownerLayerType,
        Type? ownerServiceType,
        Type? ownerManagerType,
        Func<LayerToolCreateContext, object> factory)
    {
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        ToolId = !string.IsNullOrWhiteSpace(toolId)
            ? toolId
            : throw new ArgumentException("LayerTool id cannot be null or empty.", nameof(toolId));
        Key = !string.IsNullOrWhiteSpace(key)
            ? key
            : throw new ArgumentException("LayerTool key cannot be null or empty.", nameof(key));
        Path = path;
        Cache = cache;
        OwnerLayerType = ownerLayerType;
        OwnerServiceType = ownerServiceType;
        OwnerManagerType = ownerManagerType;
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public Type ContractType { get; }

    public Type ImplementationType { get; }

    public string ToolId { get; }

    public string Key { get; }

    public string? Path { get; }

    public bool Cache { get; }

    public Type? OwnerLayerType { get; }

    public Type? OwnerServiceType { get; }

    public Type? OwnerManagerType { get; }

    public bool HasCache => _cached is not null;

    public bool HasCachedValue => _cached is not null;

    public object Create(LayerToolCreateContext context)
    {
        return _factory(context);
    }

    public object GetOrCreate(LayerToolCreateContext context)
    {
        if (!Cache)
        {
            return Create(context);
        }

        return _cached ??= Create(context);
    }

    public object? TryGetCached()
    {
        return _cached;
    }

    public void SetCached(object? value)
    {
        _cached = value;
    }

    public void ClearCache()
    {
        _cached = null;
    }
}
