namespace LayerBase.Tooling;

public sealed class LayerToolRegistry
{
    private readonly Dictionary<Type, LayerToolEntry> _byImplementation = new();
    private readonly Dictionary<(Type Contract, string Key), LayerToolEntry> _byContractAndKey = new();

    public void Register<TContract, TImplementation>(
        string key,
        string? path,
        bool cache,
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
            key: key,
            path: path,
            cache: cache,
            factory: factory);

        _byImplementation.Add(implementationType, entry);
        _byContractAndKey.Add(contractKey, entry);
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

    public IReadOnlyList<LayerToolEntry> GetEntries<TContract>()
    {
        var contract = typeof(TContract);
        var entries = new List<LayerToolEntry>();

        foreach (var pair in _byContractAndKey)
        {
            if (pair.Key.Contract == contract)
            {
                entries.Add(pair.Value);
            }
        }

        return entries;
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
        return new LayerToolCreateContext(this);
    }
}
