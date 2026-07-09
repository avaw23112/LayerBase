using LayerBase;

namespace LayerBase.Tooling;

public sealed class LayerToolRegistry
{
    private readonly LayerRuntime? _runtime;
    private readonly Dictionary<Type, LayerToolEntry> _byImplementation = new();
    private readonly Dictionary<(Type Contract, string Key), LayerToolEntry> _byContractAndKey = new();
    private readonly Dictionary<Type, List<LayerToolEntry>> _byContract = new();
    private readonly Dictionary<string, List<LayerToolEntry>> _byToolId = new(StringComparer.Ordinal);
    private readonly List<LayerToolEntry> _entries = new();

    public LayerToolRegistry()
    {
    }

    public LayerToolRegistry(LayerRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public void Register<TContract, TImplementation>(
        string toolId,
        string key,
        string? path,
        bool cache,
        Type? ownerLayerType,
        Type? ownerServiceType,
        Type? ownerManagerType,
        Func<LayerToolCreateContext, object> factory)
        where TImplementation : TContract
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        var implementationType = typeof(TImplementation);
        var contractType = typeof(TContract);
        var contractKey = (contractType, key);

        if (_byImplementation.ContainsKey(implementationType))
        {
            throw new LayerToolException($"LayerTool type is already registered: {implementationType.FullName}");
        }

        if (_byContractAndKey.ContainsKey(contractKey))
        {
            throw new LayerToolException(
                $"LayerTool entry is already registered. Contract={contractType.FullName}, Key={key}");
        }

        var entry = new LayerToolEntry(
            contractType: contractType,
            implementationType: implementationType,
            toolId: toolId,
            key: key,
            path: path,
            cache: cache,
            ownerLayerType: ownerLayerType,
            ownerServiceType: ownerServiceType,
            ownerManagerType: ownerManagerType,
            factory: factory);

        _byImplementation.Add(implementationType, entry);
        _byContractAndKey.Add(contractKey, entry);
        AddToIndex(_byContract, contractType, entry);
        AddToIndex(_byToolId, toolId, entry);
        _entries.Add(entry);
    }

    public void Register<TContract, TImplementation>(
        string key,
        string? path,
        bool cache,
        Func<LayerToolCreateContext, object> factory)
        where TImplementation : TContract
    {
        Register<TContract, TImplementation>(
            toolId: typeof(TImplementation).FullName ?? typeof(TImplementation).Name,
            key: key,
            path: path,
            cache: cache,
            ownerLayerType: null,
            ownerServiceType: null,
            ownerManagerType: null,
            factory: factory);
    }

    public T Create<T>()
    {
        var entry = GetByImplementation(typeof(T));
        return (T)entry.Create(CreateContext());
    }

    public TContract Create<TContract>(string key)
    {
        var entry = GetByContractAndKey(typeof(TContract), key);
        return (TContract)entry.Create(CreateContext());
    }

    public T GetOrCreate<T>()
    {
        var entry = GetByImplementation(typeof(T));
        return (T)entry.GetOrCreate(CreateContext());
    }

    public TContract GetOrCreate<TContract>(string key)
    {
        var entry = GetByContractAndKey(typeof(TContract), key);
        return (TContract)entry.GetOrCreate(CreateContext());
    }

    public bool TryCreate<TContract>(string key, out TContract? value)
    {
        if (!_byContractAndKey.TryGetValue((typeof(TContract), key), out var entry))
        {
            value = default;
            return false;
        }

        value = (TContract)entry.Create(CreateContext());
        return true;
    }

    public bool TryGetOrCreate<TContract>(string key, out TContract? value)
    {
        if (!_byContractAndKey.TryGetValue((typeof(TContract), key), out var entry))
        {
            value = default;
            return false;
        }

        value = (TContract)entry.GetOrCreate(CreateContext());
        return true;
    }

    public LayerToolEntry GetEntry<T>()
    {
        return GetByImplementation(typeof(T));
    }

    public LayerToolEntry GetEntry<TContract>(string key)
    {
        return GetByContractAndKey(typeof(TContract), key);
    }

    public bool TryGetEntry<TContract>(string key, out LayerToolEntry? entry)
    {
        return _byContractAndKey.TryGetValue((typeof(TContract), key), out entry);
    }

    public bool TryGetEntry<T>(out LayerToolEntry? entry)
    {
        return _byImplementation.TryGetValue(typeof(T), out entry);
    }

    public IReadOnlyList<LayerToolEntry> GetEntries()
    {
        return _entries;
    }

    public IReadOnlyList<LayerToolEntry> GetEntries<TContract>()
    {
        return _byContract.TryGetValue(typeof(TContract), out var entries)
            ? entries
            : Array.Empty<LayerToolEntry>();
    }

    public IReadOnlyList<LayerToolEntry> GetEntriesByToolId(string toolId)
    {
        if (toolId == null)
        {
            throw new ArgumentNullException(nameof(toolId));
        }

        return _byToolId.TryGetValue(toolId, out var entries)
            ? entries
            : Array.Empty<LayerToolEntry>();
    }

    public IReadOnlyList<LayerToolEntry> GetCachedEntries()
    {
        var entries = new List<LayerToolEntry>();

        foreach (var entry in _entries)
        {
            if (entry.HasCachedValue)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    public void ClearCache<T>()
    {
        GetByImplementation(typeof(T)).ClearCache();
    }

    public void ClearCache<TContract>(string key)
    {
        GetByContractAndKey(typeof(TContract), key).ClearCache();
    }

    public void ClearAllCaches()
    {
        foreach (var entry in _entries)
        {
            entry.ClearCache();
        }
    }

    public LayerToolDiagnosticsReport CreateDiagnosticsReport()
    {
        var infos = new List<LayerToolEntryInfo>(_entries.Count);
        var cachedCount = 0;

        foreach (var entry in _entries)
        {
            if (entry.HasCachedValue)
            {
                cachedCount++;
            }

            infos.Add(new LayerToolEntryInfo(
                toolId: entry.ToolId,
                contractType: entry.ContractType,
                implementationType: entry.ImplementationType,
                key: entry.Key,
                path: entry.Path,
                cache: entry.Cache,
                hasCachedValue: entry.HasCachedValue,
                ownerLayerType: entry.OwnerLayerType,
                ownerServiceType: entry.OwnerServiceType,
                ownerManagerType: entry.OwnerManagerType));
        }

        return new LayerToolDiagnosticsReport(infos, Array.Empty<LayerToolWarning>(), cachedCount);
    }

    private LayerToolEntry GetByImplementation(Type type)
    {
        if (!_byImplementation.TryGetValue(type, out var entry))
        {
            throw new LayerToolException($"LayerTool type is not registered: {type.FullName}");
        }

        return entry;
    }

    private LayerToolEntry GetByContractAndKey(Type contract, string key)
    {
        if (!_byContractAndKey.TryGetValue((contract, key), out var entry))
        {
            throw new LayerToolException(
                $"LayerTool entry is not registered. Contract={contract.FullName}, Key={key}");
        }

        return entry;
    }

    private LayerToolCreateContext CreateContext()
    {
        return _runtime == null
            ? new LayerToolCreateContext(this)
            : new LayerToolCreateContext(_runtime, this);
    }

    private static void AddToIndex<TKey>(
        Dictionary<TKey, List<LayerToolEntry>> index,
        TKey key,
        LayerToolEntry entry)
        where TKey : notnull
    {
        if (!index.TryGetValue(key, out var entries))
        {
            entries = new List<LayerToolEntry>();
            index.Add(key, entries);
        }

        entries.Add(entry);
    }
}
