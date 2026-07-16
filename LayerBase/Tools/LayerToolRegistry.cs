using LayerBase.Scope;

namespace LayerBase.Tools;

public sealed class LayerToolRegistry : IDisposable
{
    [ThreadStatic]
    private static HashSet<LayerToolKey>? s_creationStack;

    private readonly LayerRuntime _runtime;
    private readonly LayerToolDescriptor[] _entries;
    private readonly Dictionary<LayerToolKey, int> _lookup;
    private readonly object?[] _cache;
    private readonly object[] _locks;
    private readonly List<int> _creationOrder = new();
    private readonly object _orderLock = new();
    private readonly LayerToolRegistry? _root;
    private readonly int? _accessLayerIndex;
    private readonly int? _accessScopeId;
    private int _disposed;
    private int _createdCount;
    private int _createFailureCount;

    internal LayerToolRegistry(LayerRuntime runtime, IReadOnlyList<ResolvedLayerToolContribution> tools)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        if (tools == null) throw new ArgumentNullException(nameof(tools));

        _entries = tools.Select((tool, index) => new LayerToolDescriptor(
            index,
            tool.OwnerLayerIndex,
            tool.OwnerScopeId,
            tool.ContractType,
            tool.ImplementationType,
            tool.LocalKey,
            tool.Cache,
            tool.Factory)).ToArray();
        _lookup = new Dictionary<LayerToolKey, int>(_entries.Length);
        _cache = new object?[_entries.Length];
        _locks = new object[_entries.Length];

        for (var i = 0; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            _lookup.Add(new LayerToolKey(entry.ContractType, entry.Key), i);
            _locks[i] = new object();
        }
    }

    private LayerToolRegistry Root => _root ?? this;

    internal LayerToolRegistry CreateView(int ownerLayerIndex, int ownerScopeId)
    {
        if (ownerLayerIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(ownerLayerIndex));

        return new LayerToolRegistry(this, ownerLayerIndex, ownerScopeId);
    }

    private LayerToolRegistry(LayerToolRegistry source, int ownerLayerIndex, int ownerScopeId)
    {
        var root = source.Root;
        _runtime = root._runtime;
        _entries = root._entries;
        _lookup = root._lookup;
        _cache = root._cache;
        _locks = root._locks;
        _creationOrder = root._creationOrder;
        _orderLock = root._orderLock;
        _root = root;
        _accessLayerIndex = ownerLayerIndex;
        _accessScopeId = ownerScopeId;
    }

    public int Count => HasAccessFilter
        ? _entries.Count(IsAccessible)
        : _entries.Length;

    public IReadOnlyList<LayerToolDescriptor> Diagnostics => HasAccessFilter
        ? _entries.Where(IsAccessible).ToArray()
        : _entries;

    private bool HasAccessFilter => _accessLayerIndex.HasValue && _accessScopeId.HasValue;

    internal ToolDiagnosticsSnapshot CaptureDiagnostics()
    {
        var root = Root;
        var cachedCount = 0;
        for (int i = 0; i < root._cache.Length; i++)
        {
            if (Volatile.Read(ref root._cache[i]) != null)
                cachedCount++;
        }

        return new ToolDiagnosticsSnapshot(
            root._entries.Length,
            cachedCount,
            Volatile.Read(ref root._createdCount),
            Volatile.Read(ref root._createFailureCount));
    }

    public LayerToolSlot ResolveSlot<T>(string key = "default")
        where T : class
    {
        return ResolveSlot(typeof(T), key);
    }

    public LayerToolSlot ResolveSlot(Type contractType, string key = "default")
    {
        ThrowIfDisposed();
        var index = ResolveIndex(contractType, key);
        return new LayerToolSlot(index, contractType, key);
    }

    public T Create<T>()
        where T : class
    {
        return Create<T>("default");
    }

    public T Create<T>(string key)
        where T : class
    {
        return (T)Create(typeof(T), key);
    }

    public T Create<T>(LayerToolSlot slot)
        where T : class
    {
        return (T)Create(slot);
    }

    public object Create(LayerToolSlot slot)
    {
        ThrowIfDisposed();
        var index = ResolveIndex(slot);
        return CreateInstance(_entries[index]);
    }

    public object Create(Type contractType, string key = "default")
    {
        ThrowIfDisposed();
        var index = ResolveIndex(contractType, key);
        return CreateInstance(_entries[index]);
    }

    public T GetOrCreate<T>()
        where T : class
    {
        return GetOrCreate<T>("default");
    }

    public T GetOrCreate<T>(string key)
        where T : class
    {
        return (T)GetOrCreate(typeof(T), key);
    }

    public T GetOrCreate<T>(LayerToolSlot slot)
        where T : class
    {
        return (T)GetOrCreate(slot);
    }

    public object GetOrCreate(LayerToolSlot slot)
    {
        ThrowIfDisposed();
        var index = ResolveIndex(slot);
        return GetOrCreateAt(index);
    }

    public object GetOrCreate(Type contractType, string key = "default")
    {
        ThrowIfDisposed();
        var index = ResolveIndex(contractType, key);
        return GetOrCreateAt(index);
    }

    private object GetOrCreateAt(int index)
    {
        var entry = _entries[index];
        if (!entry.Cache)
        {
            return CreateInstance(entry);
        }

        var cached = Volatile.Read(ref _cache[index]);
        if (cached != null)
        {
            return cached;
        }

        lock (_locks[index])
        {
            cached = _cache[index];
            if (cached != null)
            {
                return cached;
            }

            var created = CreateInstance(entry);
            Volatile.Write(ref _cache[index], created);
            lock (_orderLock)
            {
                _creationOrder.Add(index);
            }

            return created;
        }
    }

    public bool ClearCache<T>()
        where T : class
    {
        return ClearCache<T>("default");
    }

    public bool ClearCache<T>(string key)
        where T : class
    {
        return ClearCache(typeof(T), key);
    }

    public bool ClearCache(Type contractType, string key = "default")
    {
        ThrowIfDisposed();
        return ClearCacheAt(ResolveIndex(contractType, key));
    }

    public void ClearAllCaches()
    {
        var root = Root;
        if (Interlocked.CompareExchange(ref root._disposed, 0, 0) != 0)
        {
            return;
        }

        if (HasAccessFilter)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                if (IsAccessible(_entries[i]))
                {
                    ClearCacheAt(i);
                }
            }

            return;
        }

        int[] order;
        lock (root._orderLock)
        {
            order = root._creationOrder.ToArray();
            root._creationOrder.Clear();
        }

        for (var i = order.Length - 1; i >= 0; i--)
        {
            ClearCacheAt(order[i]);
        }
    }

    public void Dispose()
    {
        if (_root != null)
        {
            return;
        }

        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        int[] order;
        lock (_orderLock)
        {
            order = _creationOrder.ToArray();
            _creationOrder.Clear();
        }

        for (var i = order.Length - 1; i >= 0; i--)
        {
            DisposeCachedAt(order[i]);
        }
    }

    private bool ClearCacheAt(int index)
    {
        object? old;
        lock (_locks[index])
        {
            old = _cache[index];
            _cache[index] = null;
        }

        if (old is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return old != null;
    }

    private void DisposeCachedAt(int index)
    {
        object? old;
        lock (_locks[index])
        {
            old = _cache[index];
            _cache[index] = null;
        }

        if (old is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private object CreateInstance(LayerToolDescriptor entry)
    {
        var creationKey = new LayerToolKey(entry.ContractType, entry.Key);
        EnterCreation(creationKey);
        try
        {
            if (entry.Factory != null)
            {
                var context = new LayerToolCreateContext(_runtime.Id, _runtime.Generation, this);
                var created = entry.Factory(in context);
                var validated = ValidateCreated(entry, created);
                IncrementCreatedCount();
                return validated;
            }

            var instance = Activator.CreateInstance(entry.ImplementationType)
                           ?? throw new InvalidOperationException(
                               $"LayerTool `{entry.ImplementationType.FullName}` factory returned null.");
            var validatedInstance = ValidateCreated(entry, instance);
            IncrementCreatedCount();
            return validatedInstance;
        }
        catch
        {
            IncrementCreateFailureCount();
            throw;
        }
        finally
        {
            ExitCreation(creationKey);
        }
    }

    private static void EnterCreation(LayerToolKey key)
    {
        var stack = s_creationStack ??= new HashSet<LayerToolKey>();
        if (!stack.Add(key))
        {
            throw new InvalidOperationException("LayerTool dependency cycle detected.");
        }
    }

    private static void ExitCreation(LayerToolKey key)
    {
        s_creationStack?.Remove(key);
    }

    private static object ValidateCreated(LayerToolDescriptor entry, object created)
    {
        if (!entry.ContractType.IsInstanceOfType(created))
        {
            throw new InvalidOperationException(
                $"LayerTool `{entry.ImplementationType.FullName}` does not implement contract `{entry.ContractType.FullName}`.");
        }

        return created;
    }

    private void IncrementCreatedCount()
    {
        var registryRoot = Root;
        Interlocked.Increment(ref registryRoot._createdCount);
    }

    private void IncrementCreateFailureCount()
    {
        var registryRoot = Root;
        Interlocked.Increment(ref registryRoot._createFailureCount);
    }

    private int ResolveIndex(Type contractType, string key)
    {
        if (contractType == null) throw new ArgumentNullException(nameof(contractType));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Tool key is required.", nameof(key));

        if (_lookup.TryGetValue(new LayerToolKey(contractType, key), out var index))
        {
            ValidateAccess(_entries[index]);
            return index;
        }

        throw new InvalidOperationException(
            $"LayerTool `{contractType.FullName}` with key `{key}` is not registered.");
    }

    private int ResolveIndex(LayerToolSlot slot)
    {
        if (slot.Index < 0 || slot.Index >= _entries.Length)
            throw new InvalidOperationException("LayerTool slot is not valid for this registry.");

        var entry = _entries[slot.Index];
        if (entry.ContractType != slot.ContractType ||
            !string.Equals(entry.Key, slot.Key, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("LayerTool slot does not match this registry.");
        }

        ValidateAccess(entry);
        return slot.Index;
    }

    private void ThrowIfDisposed()
    {
        var root = Root;
        if (Interlocked.CompareExchange(ref root._disposed, 0, 0) != 0)
        {
            throw new ObjectDisposedException(nameof(LayerToolRegistry));
        }
    }

    private bool IsAccessible(LayerToolDescriptor entry)
    {
        return !HasAccessFilter ||
               (entry.OwnerLayerIndex == _accessLayerIndex!.Value &&
                entry.OwnerScopeId == _accessScopeId!.Value);
    }

    private void ValidateAccess(LayerToolDescriptor entry)
    {
        if (IsAccessible(entry))
        {
            return;
        }

        throw new InvalidOperationException(
            $"LayerTool `{entry.ContractType.FullName}` with key `{entry.Key}` is not visible from the current Layer/Scope.");
    }

    private readonly struct LayerToolKey : IEquatable<LayerToolKey>
    {
        private readonly Type _contractType;
        private readonly string _key;

        public LayerToolKey(Type contractType, string key)
        {
            _contractType = contractType;
            _key = key;
        }

        public bool Equals(LayerToolKey other)
        {
            return _contractType == other._contractType &&
                   string.Equals(_key, other._key, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is LayerToolKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_contractType, _key);
        }
    }
}

public readonly struct LayerToolSlot
{
    internal LayerToolSlot(int index, Type contractType, string key)
    {
        Index = index;
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    internal int Index { get; }

    internal Type ContractType { get; }

    internal string Key { get; }
}

public readonly struct LayerToolDescriptor
{
    internal LayerToolDescriptor(
        int slot,
        int ownerLayerIndex,
        int ownerScopeId,
        Type contractType,
        Type implementationType,
        string key,
        bool cache,
        LayerToolFactoryInvoker? factory)
    {
        Slot = slot;
        OwnerLayerIndex = ownerLayerIndex;
        OwnerScopeId = ownerScopeId;
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Cache = cache;
        Factory = factory;
    }

    public int Slot { get; }

    public int OwnerLayerIndex { get; }

    public int OwnerScopeId { get; }

    public Type ContractType { get; }

    public Type ImplementationType { get; }

    public string Key { get; }

    public bool Cache { get; }

    internal LayerToolFactoryInvoker? Factory { get; }
}
