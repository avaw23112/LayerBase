using System.Text.Json;
using LayerBase.Async;
using LayerBase.Scope;

namespace LayerBase.Snap;

internal sealed class FullSnapRuntime
{
    private readonly LayerRuntime _runtime;
    private readonly ScopeRuntimeHost _scopes;
    private readonly Dictionary<int, List<ScopeSnapNodePlan>> _pendingPlans = new();
    private readonly Dictionary<string, ScopeSnapNodePlan> _keys = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<int, int> _scopeNodeCounts = new Dictionary<int, int>();

    public FullSnapRuntime(LayerRuntime runtime, ScopeRuntimeHost scopes)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
    }

    internal IReadOnlyDictionary<int, int> ScopeNodeCounts => _scopeNodeCounts;

    internal void Register(int scopeId, ScopeSnapNodePlan node)
    {
        if (string.IsNullOrWhiteSpace(node.Key))
            throw new SnapFormatException("Snap section key cannot be empty.");

        if (_keys.TryGetValue(node.Key, out ScopeSnapNodePlan existing))
        {
            throw new InvalidOperationException(
                $"Duplicate FullSnap key `{node.Key}` between layer {existing.LayerIndex} and layer {node.LayerIndex}.");
        }

        _keys.Add(node.Key, node);
        if (!_pendingPlans.TryGetValue(scopeId, out var nodes))
        {
            nodes = new List<ScopeSnapNodePlan>();
            _pendingPlans.Add(scopeId, nodes);
        }

        nodes.Add(node);
    }

    internal void FreezePlans()
    {
        var counts = new Dictionary<int, int>();
        foreach (ScopeRuntime scope in _scopes.Scopes)
        {
            ScopeSnapPlan plan;
            if (_pendingPlans.TryGetValue(scope.ScopeId, out var nodes))
            {
                plan = new ScopeSnapPlan(nodes.ToArray());
                counts[scope.ScopeId] = plan.Nodes.Length;
            }
            else
            {
                plan = ScopeSnapPlan.Empty;
                counts[scope.ScopeId] = 0;
            }

            scope.SetSnapPlan(plan);
        }

        _scopeNodeCounts = counts;
    }

    internal SnapDocument Serialize()
    {
        ThrowIfWorkerScopeRequiresAsync();
        var document = new SnapDocument();
        foreach (ScopeRuntime scope in _scopes.Scopes)
            AddSections(document, scope.SerializeFullSnapOnOwnerThread().Sections);

        return document;
    }

    internal async LBTask<SnapDocument> SerializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = new SnapDocument();
        foreach (ScopeRuntime scope in _scopes.Scopes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScopeSerializeFullSnapResponse response = await ScopeOwnerInvocation
                .InvokeAsync<ScopeSerializeFullSnapCall, ScopeSerializeFullSnapResponse>(
                    scope,
                    new ScopeSerializeFullSnapCall(),
                    cancellationToken);
            if (response.Result != ScopeControlResult.Succeeded)
                throw new InvalidOperationException($"Scope `{scope.Descriptor.Name}` failed to serialize FullSnap.");

            AddSections(document, response.Sections);
        }

        return document;
    }

    internal void Deserialize(SnapDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        ThrowIfWorkerScopeRequiresAsync();
        foreach (ScopeRuntime scope in _scopes.Scopes)
            scope.DeserializeFullSnapOnOwnerThread(document);
    }

    internal async LBTask DeserializeAsync(SnapDocument document, CancellationToken cancellationToken = default)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        cancellationToken.ThrowIfCancellationRequested();
        foreach (ScopeRuntime scope in _scopes.Scopes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScopeDeserializeFullSnapResponse response = await ScopeOwnerInvocation
                .InvokeAsync<ScopeDeserializeFullSnapCall, ScopeDeserializeFullSnapResponse>(
                    scope,
                    new ScopeDeserializeFullSnapCall(document),
                    cancellationToken);
            if (response.Result != ScopeControlResult.Succeeded)
                throw new InvalidOperationException($"Scope `{scope.Descriptor.Name}` failed to deserialize FullSnap.");
        }
    }

    internal string SerializeJson(JsonSerializerOptions? options = null)
    {
        return JsonSnapCodec.EncodeToString(Serialize(), options);
    }

    internal async LBTask<string> SerializeJsonAsync(
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = await SerializeAsync(cancellationToken);
        return JsonSnapCodec.EncodeToString(document, options);
    }

    internal void DeserializeJson(string json, JsonSerializerOptions? options = null)
    {
        Deserialize(JsonSnapCodec.DecodeFromString(json, options));
    }

    internal async LBTask DeserializeJsonAsync(
        string json,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json, options);
        await DeserializeAsync(document, cancellationToken);
    }

    private void ThrowIfWorkerScopeRequiresAsync()
    {
        if (_scopes.HasWorkerScopes)
        {
            throw new InvalidOperationException(
                "FullSnap synchronous API cannot coordinate WorkerScope safely. Use SerializeAsync or DeserializeAsync.");
        }
    }

    private static void AddSections(SnapDocument document, SnapSection[] sections)
    {
        for (int i = 0; i < sections.Length; i++)
            document.AddSection(sections[i]);
    }
}
