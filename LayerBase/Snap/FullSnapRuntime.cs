using System.Runtime.ExceptionServices;
using System.Text.Json;
using LayerBase.Async;
using LayerBase.Scope;

namespace LayerBase.Snap;

internal sealed class FullSnapRuntime : IFullSnapRuntime
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

    public SnapDocument Serialize()
    {
        ThrowIfWorkerScopeRequiresAsync();
        var frozen = new List<ScopeRuntime>();
        try
        {
            EnterSafePointDirect(frozen);
            return WriteFrozenScopesDirect();
        }
        finally
        {
            ExitSafePointDirect(frozen);
        }
    }

    public async LBTask<SnapDocument> SerializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_scopes.HasWorkerScopes)
            return Serialize();

        var frozen = new List<ScopeRuntime>();
        SnapDocument? document = null;
        Exception? failure = null;

        try
        {
            await EnterSafePointAsync(frozen, cancellationToken);
            document = await WriteFrozenScopesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        await ExitSafePointAsync(frozen, cancellationToken);

        if (failure != null)
            ExceptionDispatchInfo.Capture(failure).Throw();

        return document!;
    }

    public void Deserialize(SnapDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        ThrowIfWorkerScopeRequiresAsync();
        var frozen = new List<ScopeRuntime>();
        var restoreStarted = false;

        try
        {
            EnterSafePointDirect(frozen);
            restoreStarted = true;
            ReadFrozenScopesDirect(document);
        }
        catch
        {
            if (!restoreStarted)
                ExitSafePointDirect(frozen);

            throw;
        }

        ExitSafePointDirect(frozen);
    }

    public async LBTask DeserializeAsync(SnapDocument document, CancellationToken cancellationToken = default)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        cancellationToken.ThrowIfCancellationRequested();
        if (!_scopes.HasWorkerScopes)
        {
            Deserialize(document);
            return;
        }

        var frozen = new List<ScopeRuntime>();
        var restoreStarted = false;

        try
        {
            await EnterSafePointAsync(frozen, cancellationToken);
            restoreStarted = true;
            await ReadFrozenScopesAsync(document, cancellationToken);
        }
        catch
        {
            if (!restoreStarted)
                await ExitSafePointAsync(frozen, cancellationToken);

            throw;
        }

        await ExitSafePointAsync(frozen, cancellationToken);
    }

    public string SerializeJson(JsonSerializerOptions? options = null)
    {
        return JsonSnapCodec.EncodeToString(Serialize(), options);
    }

    public async LBTask<string> SerializeJsonAsync(
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = await SerializeAsync(cancellationToken);
        return JsonSnapCodec.EncodeToString(document, options);
    }

    public void DeserializeJson(string json, JsonSerializerOptions? options = null)
    {
        Deserialize(JsonSnapCodec.DecodeFromString(json, options));
    }

    public async LBTask DeserializeJsonAsync(
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

    private void EnterSafePointDirect(List<ScopeRuntime> frozen)
    {
        foreach (ScopeRuntime scope in _scopes.Scopes)
        {
            scope.EnterSafePointForSnap();
            frozen.Add(scope);
        }
    }

    private async LBTask EnterSafePointAsync(
        List<ScopeRuntime> frozen,
        CancellationToken cancellationToken)
    {
        foreach (ScopeRuntime scope in _scopes.Scopes)
        {
            if (!ShouldCoordinateAsync(scope))
                continue;

            ScopeEnterSafePointResponse response = scope.Options.Threading == ScopeThreadingMode.Worker
                ? await scope.RequestEnterSafePointAsync(cancellationToken)
                : scope.EnterSafePointForSnap();
            if (response.Result != ScopeControlResult.Succeeded)
                throw new InvalidOperationException($"Scope `{scope.Descriptor.Name}` rejected FullSnap safe point.");

            frozen.Add(scope);
        }
    }

    private SnapDocument WriteFrozenScopesDirect()
    {
        var document = new SnapDocument();
        foreach (ScopeRuntime scope in _scopes.Scopes)
            AddSections(document, scope.WriteSnapshotForSnap().Sections);

        return document;
    }

    private async LBTask<SnapDocument> WriteFrozenScopesAsync(CancellationToken cancellationToken)
    {
        var document = new SnapDocument();
        foreach (ScopeRuntime scope in _scopes.Scopes)
        {
            if (!ShouldCoordinateAsync(scope))
                continue;

            ScopeWriteSnapshotResponse response = scope.Options.Threading == ScopeThreadingMode.Worker
                ? await scope.RequestWriteSnapshotAsync(cancellationToken)
                : scope.WriteSnapshotForSnap();
            if (response.Result != ScopeControlResult.Succeeded)
                throw new InvalidOperationException($"Scope `{scope.Descriptor.Name}` failed to write FullSnap.");

            AddSections(document, response.Sections);
        }

        return document;
    }

    private void ReadFrozenScopesDirect(SnapDocument document)
    {
        foreach (ScopeRuntime scope in _scopes.Scopes)
            scope.ReadSnapshotForSnap(document);
    }

    private async LBTask ReadFrozenScopesAsync(SnapDocument document, CancellationToken cancellationToken)
    {
        foreach (ScopeRuntime scope in _scopes.Scopes)
        {
            if (!ShouldCoordinateAsync(scope))
                continue;

            ScopeReadSnapshotResponse response = scope.Options.Threading == ScopeThreadingMode.Worker
                ? await scope.RequestReadSnapshotAsync(document, cancellationToken)
                : scope.ReadSnapshotForSnap(document);
            if (response.Result != ScopeControlResult.Succeeded)
                throw new InvalidOperationException($"Scope `{scope.Descriptor.Name}` failed to read FullSnap.");
        }
    }

    private static void AddSections(SnapDocument document, SnapSection[] sections)
    {
        for (int i = 0; i < sections.Length; i++)
            document.AddSection(sections[i]);
    }

    private static void ExitSafePointDirect(List<ScopeRuntime> frozen)
    {
        for (int i = frozen.Count - 1; i >= 0; i--)
            frozen[i].ExitSafePointForSnap();
    }

    private static async LBTask ExitSafePointAsync(
        List<ScopeRuntime> frozen,
        CancellationToken cancellationToken)
    {
        for (int i = frozen.Count - 1; i >= 0; i--)
        {
            ScopeRuntime scope = frozen[i];
            if (scope.Options.Threading == ScopeThreadingMode.Worker)
                await scope.RequestExitSafePointAsync(cancellationToken);
            else
                scope.ExitSafePointForSnap();
        }
    }

    private bool ShouldCoordinateAsync(ScopeRuntime scope)
    {
        if (scope.Options.Threading == ScopeThreadingMode.Worker)
            return true;

        return _scopeNodeCounts.TryGetValue(scope.ScopeId, out int count) && count > 0;
    }
}
