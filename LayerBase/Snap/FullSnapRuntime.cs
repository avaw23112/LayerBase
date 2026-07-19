using System.Text.Json;
using System.Text.Json.Nodes;
using LayerBase.Async;
using LayerBase.Scope;

namespace LayerBase.Snap;

internal sealed class FullSnapRuntime
{
    private readonly ScopeRuntimeHost _scopes;
    private readonly Dictionary<int, List<ScopeSnapNodePlan>> _pendingPlans = new();
    private readonly Dictionary<string, ScopeSnapNodePlan> _keys = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<int, int> _scopeNodeCounts = new Dictionary<int, int>();

    public FullSnapRuntime(ScopeRuntimeHost scopes)
    {
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
        Deserialize(document, FullSnapLimits.Default);
    }

    internal void Deserialize(SnapDocument document, FullSnapLimits limits)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        ValidateDocument(document, limits);
        ThrowIfWorkerScopeRequiresAsync();
        foreach (ScopeRuntime scope in _scopes.Scopes)
            scope.DeserializeFullSnapOnOwnerThread(document);
    }

    internal async LBTask DeserializeAsync(SnapDocument document, CancellationToken cancellationToken = default)
    {
        await DeserializeAsync(document, FullSnapLimits.Default, cancellationToken);
    }

    internal async LBTask DeserializeAsync(
        SnapDocument document,
        FullSnapLimits limits,
        CancellationToken cancellationToken = default)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        ValidateDocument(document, limits);
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
        Deserialize(JsonSnapCodec.DecodeFromString(json, FullSnapLimits.Default, options));
    }

    internal void DeserializeJson(
        string json,
        FullSnapLimits limits,
        JsonSerializerOptions? options = null)
    {
        Deserialize(JsonSnapCodec.DecodeFromString(json, limits, options), limits);
    }

    internal async LBTask DeserializeJsonAsync(
        string json,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json, FullSnapLimits.Default, options);
        await DeserializeAsync(document, cancellationToken);
    }

    internal async LBTask DeserializeJsonAsync(
        string json,
        FullSnapLimits limits,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json, limits, options);
        await DeserializeAsync(document, limits, cancellationToken);
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

    private void ValidateDocument(SnapDocument document, FullSnapLimits limits)
    {
        limits.ThrowIfInvalid();
        SnapReadLimits readLimits = limits.ToReadLimits();
        readLimits.ThrowIfInvalid();
        if (document.Sections == null)
        {
            throw new SnapFormatException("FullSnap document sections cannot be null.");
        }

        if (document.FormatVersion < limits.MinFormatVersion ||
            document.FormatVersion > limits.MaxFormatVersion)
        {
            throw new SnapFormatException(
                $"FullSnap FormatVersion {document.FormatVersion} is outside supported range {limits.MinFormatVersion}-{limits.MaxFormatVersion}.");
        }

        int scopeCount = 0;
        var scopeIds = new HashSet<int>();
        foreach (ScopeRuntime scope in _scopes.Scopes)
        {
            scopeCount++;
            if (!scopeIds.Add(scope.ScopeId))
            {
                throw new SnapFormatException($"Duplicate FullSnap ScopeId `{scope.ScopeId}`.");
            }
        }

        if (scopeCount > limits.MaxScopeCount)
        {
            throw new SnapFormatException(
                $"FullSnap scope count exceeds MaxScopeCount ({scopeCount} > {limits.MaxScopeCount}).");
        }

        if (document.Sections.Count > readLimits.MaxSections)
        {
            throw new SnapFormatException(
                $"FullSnap section count exceeds MaxSections ({document.Sections.Count} > {readLimits.MaxSections}).");
        }

        foreach (int plannedScopeId in _pendingPlans.Keys)
        {
            if (!scopeIds.Contains(plannedScopeId))
            {
                throw new SnapFormatException($"FullSnap plan references unknown ScopeId `{plannedScopeId}`.");
            }
        }

        int totalBytes = JsonSnapCodec.GetDocumentByteCount(document);
        if (totalBytes > limits.MaxTotalBytes)
        {
            throw new SnapFormatException(
                $"FullSnap document exceeds MaxTotalBytes ({totalBytes} > {limits.MaxTotalBytes}).");
        }

        var scopeBytes = new Dictionary<int, long>();
        var scopeSections = new Dictionary<int, int>();
        var sectionScopes = BuildSectionScopeMap();
        long totalSectionBytes = 0;
        var sectionKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, SnapSection> entry in document.Sections)
        {
            string key = entry.Key;
            SnapSection section = entry.Value;
            if (section == null)
            {
                throw new SnapFormatException($"FullSnap section `{key}` cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(section.Key))
            {
                throw new SnapFormatException("FullSnap section key cannot be empty.");
            }

            if (!sectionKeys.Add(key))
            {
                throw new SnapFormatException($"Duplicate FullSnap section `{key}`.");
            }

            if (!string.Equals(key, section.Key, StringComparison.Ordinal))
            {
                throw new SnapFormatException(
                    $"FullSnap section dictionary key `{key}` does not match payload key `{section.Key}`.");
            }

            if (section.Version <= 0)
            {
                throw new SnapFormatException($"FullSnap section `{key}` version must be a positive integer.");
            }

            if (!_keys.TryGetValue(key, out ScopeSnapNodePlan plan))
            {
                throw new SnapFormatException($"FullSnap section `{key}` does not match a registered scope payload.");
            }

            if (section.Version != plan.Version)
            {
                throw new SnapFormatException(
                    $"FullSnap section `{key}` version {section.Version} does not match expected version {plan.Version}.");
            }

            if (section.Data == null)
            {
                throw new SnapFormatException($"FullSnap section `{key}` has null data.");
            }

            int sectionBytes = JsonSnapCodec.GetSectionByteCount(section);
            if (sectionBytes > readLimits.MaxSectionBytes)
            {
                throw new SnapFormatException(
                    $"FullSnap section `{key}` exceeds MaxSectionBytes ({sectionBytes} > {readLimits.MaxSectionBytes}).");
            }

            totalSectionBytes += sectionBytes;
            if (totalSectionBytes > readLimits.MaxTotalSectionBytes)
            {
                throw new SnapFormatException(
                    $"FullSnap sections exceed MaxTotalSectionBytes ({totalSectionBytes} > {readLimits.MaxTotalSectionBytes}).");
            }

            int depth = JsonSnapCodec.GetJsonDepth(section.Data);
            if (depth > readLimits.MaxJsonDepth)
            {
                throw new SnapFormatException(
                    $"FullSnap section `{key}` exceeds MaxJsonDepth ({depth} > {readLimits.MaxJsonDepth}).");
            }

            int scopeId = sectionScopes[key];
            scopeSections[scopeId] = scopeSections.TryGetValue(scopeId, out int count) ? count + 1 : 1;
            scopeBytes[scopeId] = scopeBytes.TryGetValue(scopeId, out long bytes)
                ? bytes + sectionBytes
                : sectionBytes;
        }

        foreach (KeyValuePair<int, List<ScopeSnapNodePlan>> scopePlan in _pendingPlans)
        {
            if (scopePlan.Value.Count > limits.MaxSectionsPerScope)
            {
                throw new SnapFormatException(
                    $"FullSnap ScopeId `{scopePlan.Key}` exceeds MaxSectionsPerScope ({scopePlan.Value.Count} > {limits.MaxSectionsPerScope}).");
            }

            foreach (ScopeSnapNodePlan node in scopePlan.Value)
            {
                if (!document.Sections.ContainsKey(node.Key))
                {
                    throw new SnapFormatException(
                        $"Missing required FullSnap section `{node.Key}` for ScopeId `{scopePlan.Key}`.");
                }
            }
        }

        foreach (KeyValuePair<int, int> scopeSectionCount in scopeSections)
        {
            if (scopeSectionCount.Value > limits.MaxSectionsPerScope)
            {
                throw new SnapFormatException(
                    $"FullSnap ScopeId `{scopeSectionCount.Key}` exceeds MaxSectionsPerScope ({scopeSectionCount.Value} > {limits.MaxSectionsPerScope}).");
            }
        }

        foreach (KeyValuePair<int, long> scopeByteCount in scopeBytes)
        {
            if (scopeByteCount.Value > limits.MaxScopeBytes)
            {
                throw new SnapFormatException(
                    $"FullSnap ScopeId `{scopeByteCount.Key}` exceeds MaxScopeBytes ({scopeByteCount.Value} > {limits.MaxScopeBytes}).");
            }
        }
    }

    private Dictionary<string, int> BuildSectionScopeMap()
    {
        var sectionScopes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<int, List<ScopeSnapNodePlan>> scopePlan in _pendingPlans)
        {
            foreach (ScopeSnapNodePlan node in scopePlan.Value)
            {
                sectionScopes[node.Key] = scopePlan.Key;
            }
        }

        return sectionScopes;
    }
}
