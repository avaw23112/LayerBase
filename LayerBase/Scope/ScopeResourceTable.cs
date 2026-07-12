namespace LayerBase.Scope;

public sealed class ScopeResourceClosedException : InvalidOperationException
{
    public ScopeResourceClosedException(string message) : base(message)
    {
    }
}

public sealed class ScopeResourceGenerationException : InvalidOperationException
{
    public ScopeResourceGenerationException(string message) : base(message)
    {
    }
}

public readonly struct ScopeRead<TView>
{
    private readonly ScopeRuntime? _runtime;
    private readonly int _slot;
    private readonly int _generation;

    internal ScopeRead(ScopeRuntime runtime, int slot, int generation)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _slot = slot;
        _generation = generation;
    }

    public TView Value
    {
        get
        {
            if (_runtime == null)
            {
                throw new ScopeResourceClosedException("Scope resource reader is not bound.");
            }

            return _runtime.GetScopeResource<TView>(_slot, _generation);
        }
    }
}

internal sealed class ScopeResourceTable
{
    private readonly object _gate = new();
    private ScopeResourceEntry[] _entries = Array.Empty<ScopeResourceEntry>();
    private int _generation;
    private bool _closed = true;

    public int Generation
    {
        get
        {
            lock (_gate)
            {
                return _generation;
            }
        }
    }

    public int Initialize(IReadOnlyList<ScopeResourceEntry> entries)
    {
        if (entries == null) throw new ArgumentNullException(nameof(entries));

        lock (_gate)
        {
            _entries = entries.ToArray();
            unchecked
            {
                _generation++;
                if (_generation == 0)
                {
                    _generation = 1;
                }
            }

            _closed = false;
            return _generation;
        }
    }

    public TView Get<TView>(ScopeRuntime runtime, int slot, int generation)
    {
        runtime.RequireAccess("ScopeRead.Value");

        lock (_gate)
        {
            if (_closed)
            {
                throw new ScopeResourceClosedException($"Scope '{runtime.Descriptor.Name}' resource table is closed.");
            }

            if (generation != _generation)
            {
                throw new ScopeResourceGenerationException($"Scope '{runtime.Descriptor.Name}' resource generation has changed.");
            }

            if ((uint)slot >= (uint)_entries.Length)
            {
                throw new ScopeResourceGenerationException($"Scope '{runtime.Descriptor.Name}' resource slot '{slot}' is invalid.");
            }

            object value = _entries[slot].Value;
            if (value is TView typed)
            {
                return typed;
            }

            throw new InvalidOperationException(
                $"Scope resource '{_entries[slot].ProviderType.FullName}.{_entries[slot].LocalKey}' cannot be read as '{typeof(TView).FullName}'.");
        }
    }

    public void CloseAndClear()
    {
        lock (_gate)
        {
            _closed = true;
            _entries = Array.Empty<ScopeResourceEntry>();
        }
    }
}

internal readonly struct ScopeResourceEntry
{
    public ScopeResourceEntry(Type providerType, string localKey, object value)
    {
        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Type ProviderType { get; }

    public string LocalKey { get; }

    public object Value { get; }
}
