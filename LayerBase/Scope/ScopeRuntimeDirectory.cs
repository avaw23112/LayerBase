namespace LayerBase.Scope;

internal sealed class ScopeRuntimeDirectory
{
    private readonly Dictionary<int, int> _slotByScopeId;
    private readonly ScopeRuntime[] _runtimes;
    private readonly ScopeEndpoint[] _endpoints;

    public ScopeRuntimeDirectory(ScopeRuntime[] runtimes)
    {
        _runtimes = runtimes ?? throw new ArgumentNullException(nameof(runtimes));
        if (runtimes.Length == 0)
            throw new ArgumentException("Scope host must contain at least MainScope.", nameof(runtimes));
        if (runtimes[0].Descriptor.ScopeId != ScopeDefinitionIds.Main)
            throw new InvalidOperationException("MainScope must be the first scope runtime.");

        _slotByScopeId = new Dictionary<int, int>(runtimes.Length);
        _endpoints = new ScopeEndpoint[runtimes.Length];

        for (int i = 0; i < runtimes.Length; i++)
        {
            int scopeId = runtimes[i].ScopeId;
            if (scopeId < 0)
                throw new InvalidOperationException($"Scope id cannot be negative: {scopeId}.");
            if (_slotByScopeId.ContainsKey(scopeId))
                throw new InvalidOperationException($"Duplicate scope id: {scopeId}.");

            _slotByScopeId.Add(scopeId, i);
            _endpoints[i] = runtimes[i].Endpoint;
        }
    }

    public int ScopeCount => _runtimes.Length;

    public ScopeRuntime MainScope => _runtimes[0];

    internal ScopeRuntime[] Runtimes => _runtimes;

    internal ScopeEndpoint[] Endpoints => _endpoints;

    public bool TryGetRuntime(int scopeId, out ScopeRuntime? runtime)
    {
        if (_slotByScopeId.TryGetValue(scopeId, out int slot))
        {
            runtime = _runtimes[slot];
            return true;
        }
        runtime = null;
        return false;
    }

    public bool TryGetEndpoint(int scopeId, out ScopeEndpoint endpoint)
    {
        if (_slotByScopeId.TryGetValue(scopeId, out int slot))
        {
            endpoint = _endpoints[slot];
            return true;
        }
        endpoint = default;
        return false;
    }

    public ScopeEndpoint GetRequiredEndpoint(int scopeId)
    {
        if (TryGetEndpoint(scopeId, out var endpoint))
            return endpoint;
        throw new KeyNotFoundException($"Scope id {scopeId} not found.");
    }
}
