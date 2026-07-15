using LayerBase.Call;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase;

internal enum TopologyAuditSeverity
{
    Error = 0,
    Warning = 1
}

internal readonly struct TopologyAuditDiagnostic
{
    public TopologyAuditDiagnostic(
        TopologyAuditSeverity severity,
        string code,
        string message,
        int scopeId,
        int layerIndex)
    {
        Severity = severity;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ScopeId = scopeId;
        LayerIndex = layerIndex;
    }

    public TopologyAuditSeverity Severity { get; }

    public string Code { get; }

    public string Message { get; }

    public int ScopeId { get; }

    public int LayerIndex { get; }
}

internal static class TopologyAudit
{
    public static TopologyAuditDiagnostic[] Run(
        LayerRuntime runtime,
        IReadOnlyList<Layer> layers)
    {
        if (runtime == null)
            throw new ArgumentNullException(nameof(runtime));
        if (layers == null)
            throw new ArgumentNullException(nameof(layers));

        var diagnostics = new List<TopologyAuditDiagnostic>();
        var plan = runtime.CompositionPlan;
        var scopeIds = ValidateScopes(plan, diagnostics);
        var layerIndexes = ValidateLayers(plan, layers, diagnostics);

        ValidateScopeSlices(plan, scopeIds, layerIndexes, diagnostics);
        ValidateLayerContributions(plan, scopeIds, layerIndexes, diagnostics);
        ValidateResolvedContributions(plan, scopeIds, layerIndexes, diagnostics);
        ValidateRuntimeLocalCalls(layers, scopeIds, layerIndexes, diagnostics);

        var ordered = diagnostics
            .OrderBy(static diagnostic => diagnostic.Severity)
            .ThenBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.ScopeId)
            .ThenBy(static diagnostic => diagnostic.LayerIndex)
            .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();

        var errors = ordered
            .Where(static diagnostic => diagnostic.Severity == TopologyAuditSeverity.Error)
            .ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(
                "LayerBase topology audit failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(static error =>
                    $"{error.Code}: {error.Message}")));
        }

        return ordered;
    }

    private static HashSet<int> ValidateScopes(
        RuntimeCompositionPlan plan,
        List<TopologyAuditDiagnostic> diagnostics)
    {
        var scopeIds = new HashSet<int>();
        foreach (var scope in plan.Scopes)
        {
            if (!scopeIds.Add(scope.Descriptor.ScopeId))
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_SCOPE_ID_DUPLICATE",
                    scope.Descriptor.ScopeId,
                    -1,
                    $"Scope id {scope.Descriptor.ScopeId} is registered more than once.");
            }
        }

        if (!scopeIds.Contains(ScopeDefinitionIds.Main))
        {
            AddError(
                diagnostics,
                "LBTOPOLOGY_MAIN_SCOPE_MISSING",
                ScopeDefinitionIds.Main,
                -1,
                "Main scope is missing from the composition plan.");
        }

        return scopeIds;
    }

    private static HashSet<int> ValidateLayers(
        RuntimeCompositionPlan plan,
        IReadOnlyList<Layer> layers,
        List<TopologyAuditDiagnostic> diagnostics)
    {
        var layerIndexes = new HashSet<int>();
        foreach (var layer in layers.OrderBy(static layer => layer.RouteIndex))
        {
            if (!layerIndexes.Add(layer.RouteIndex))
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_LAYER_INDEX_DUPLICATE",
                    ScopeDefinitionIds.Main,
                    layer.RouteIndex,
                    $"Layer index {layer.RouteIndex} is assigned more than once.");
            }
        }

        foreach (var layerPlan in plan.Layers)
        {
            if (!layerIndexes.Contains(layerPlan.LayerIndex))
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_LAYER_NOT_PUSHED",
                    ScopeDefinitionIds.Main,
                    layerPlan.LayerIndex,
                    $"Layer plan `{layerPlan.LayerType.FullName}` references index {layerPlan.LayerIndex}, but that layer was not pushed.");
            }
        }

        return layerIndexes;
    }

    private static void ValidateScopeSlices(
        RuntimeCompositionPlan plan,
        HashSet<int> scopeIds,
        HashSet<int> layerIndexes,
        List<TopologyAuditDiagnostic> diagnostics)
    {
        foreach (var scope in plan.Scopes.OrderBy(static scope => scope.Descriptor.ScopeId))
        {
            foreach (var slice in scope.LayerSlices)
            {
                if (!layerIndexes.Contains(slice.LayerIndex))
                {
                    AddError(
                        diagnostics,
                        "LBTOPOLOGY_LAYER_NOT_PUSHED",
                        scope.Descriptor.ScopeId,
                        slice.LayerIndex,
                        $"Scope `{scope.Descriptor.Name}` contains a slice for missing layer index {slice.LayerIndex}.");
                }
            }

            foreach (var lifecycleSlice in scope.LifecyclePlan.Layers)
            {
                if (!layerIndexes.Contains(lifecycleSlice.LayerIndex))
                {
                    AddError(
                        diagnostics,
                        "LBTOPOLOGY_LAYER_NOT_PUSHED",
                        scope.Descriptor.ScopeId,
                        lifecycleSlice.LayerIndex,
                        $"Scope `{scope.Descriptor.Name}` lifecycle plan references missing layer index {lifecycleSlice.LayerIndex}.");
                }
            }
        }

        foreach (var scopeId in scopeIds)
        {
            if (scopeId < 0)
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_SCOPE_ID_INVALID",
                    scopeId,
                    -1,
                    $"Scope id {scopeId} is invalid.");
            }
        }
    }

    private static void ValidateLayerContributions(
        RuntimeCompositionPlan plan,
        HashSet<int> scopeIds,
        HashSet<int> layerIndexes,
        List<TopologyAuditDiagnostic> diagnostics)
    {
        foreach (var layer in plan.Layers.OrderBy(static layer => layer.LayerIndex))
        foreach (var contribution in layer.ScopeContributions.OrderBy(static contribution => contribution.OwnerScopeId))
        {
            if (!scopeIds.Contains(contribution.OwnerScopeId))
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_SCOPE_NOT_INSTALLED",
                    contribution.OwnerScopeId,
                    layer.LayerIndex,
                    $"Layer `{layer.LayerType.FullName}` has contributions for unknown scope {contribution.OwnerScopeId}.");
                continue;
            }

            if (!layerIndexes.Contains(layer.LayerIndex))
                continue;

            ValidateRange(
                plan.Services,
                contribution.ServiceStart,
                contribution.ServiceCount,
                contribution.OwnerScopeId,
                layer.LayerIndex,
                "service",
                static item => item.OwnerLayerIndex,
                static item => item.OwnerScopeId,
                diagnostics);
            ValidateRange(
                plan.Contexts,
                contribution.ContextStart,
                contribution.ContextCount,
                contribution.OwnerScopeId,
                layer.LayerIndex,
                "context",
                static item => item.OwnerLayerIndex,
                static item => item.OwnerScopeId,
                diagnostics);
            ValidateRange(
                plan.LocalCalls,
                contribution.LocalCallStart,
                contribution.LocalCallCount,
                contribution.OwnerScopeId,
                layer.LayerIndex,
                "local call",
                static item => item.OwnerLayerIndex,
                static item => item.OwnerScopeId,
                diagnostics);
            ValidateRange(
                plan.EventHandlers,
                contribution.EventHandlerStart,
                contribution.EventHandlerCount,
                contribution.OwnerScopeId,
                layer.LayerIndex,
                "event handler",
                static item => item.OwnerLayerIndex,
                static item => item.OwnerScopeId,
                diagnostics);
        }
    }

    private static void ValidateRange<T>(
        T[] source,
        int start,
        int count,
        int ownerScopeId,
        int ownerLayerIndex,
        string contributionName,
        Func<T, int> getLayerIndex,
        Func<T, int> getScopeId,
        List<TopologyAuditDiagnostic> diagnostics)
    {
        if (count == 0)
            return;

        if (start < 0 || count < 0 || start + count > source.Length)
        {
            AddError(
                diagnostics,
                "LBTOPOLOGY_OBJECT_SLOT_OUT_OF_RANGE",
                ownerScopeId,
                ownerLayerIndex,
                $"Layer {ownerLayerIndex} scope {ownerScopeId} has an invalid {contributionName} contribution range [{start}, {start + count}).");
            return;
        }

        for (int i = start; i < start + count; i++)
        {
            if (getLayerIndex(source[i]) != ownerLayerIndex ||
                getScopeId(source[i]) != ownerScopeId)
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_OWNER_SCOPE_MISMATCH",
                    ownerScopeId,
                    ownerLayerIndex,
                    $"Layer {ownerLayerIndex} scope {ownerScopeId} has a {contributionName} contribution range entry that belongs to layer {getLayerIndex(source[i])} scope {getScopeId(source[i])}.");
            }
        }
    }

    private static void ValidateResolvedContributions(
        RuntimeCompositionPlan plan,
        HashSet<int> scopeIds,
        HashSet<int> layerIndexes,
        List<TopologyAuditDiagnostic> diagnostics)
    {
        foreach (var service in plan.Services)
            ValidateOwner(diagnostics, "service", service.ServiceType, service.OwnerScopeId, service.OwnerLayerIndex, scopeIds, layerIndexes);

        foreach (var context in plan.Contexts)
            ValidateOwner(diagnostics, "context", context.ContextType, context.OwnerScopeId, context.OwnerLayerIndex, scopeIds, layerIndexes);

        var localCallKeys = new HashSet<ScopedCallKey>();
        foreach (var localCall in plan.LocalCalls)
        {
            ValidateOwner(diagnostics, "local call", localCall.HandlerType, localCall.OwnerScopeId, localCall.OwnerLayerIndex, scopeIds, layerIndexes);
            ValidateClosedType(diagnostics, localCall.RequestType, localCall.OwnerScopeId, localCall.OwnerLayerIndex, "request");
            ValidateClosedType(diagnostics, localCall.ResponseType, localCall.OwnerScopeId, localCall.OwnerLayerIndex, "response");

            var key = new ScopedCallKey(localCall.OwnerScopeId, localCall.RequestType, localCall.ResponseType);
            if (!localCallKeys.Add(key))
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_LOCAL_CALL_DUPLICATE",
                    localCall.OwnerScopeId,
                    localCall.OwnerLayerIndex,
                    $"Scope {localCall.OwnerScopeId} has duplicate local call handlers for `{localCall.RequestType.FullName}` -> `{localCall.ResponseType.FullName}`.");
            }
        }

        foreach (var handler in plan.EventHandlers)
        {
            ValidateOwner(diagnostics, "event handler", handler.HandlerType, handler.OwnerScopeId, handler.OwnerLayerIndex, scopeIds, layerIndexes);
            ValidateClosedType(diagnostics, handler.EventType, handler.OwnerScopeId, handler.OwnerLayerIndex, "event");
        }

        foreach (var tool in plan.Tools)
        {
            if (!layerIndexes.Contains(tool.OwnerLayerIndex))
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_LAYER_NOT_PUSHED",
                    ScopeDefinitionIds.Main,
                    tool.OwnerLayerIndex,
                    $"Tool `{tool.ContractType.FullName}` references missing layer index {tool.OwnerLayerIndex}.");
            }
        }
    }

    private static void ValidateRuntimeLocalCalls(
        IReadOnlyList<Layer> layers,
        HashSet<int> scopeIds,
        HashSet<int> layerIndexes,
        List<TopologyAuditDiagnostic> diagnostics)
    {
        foreach (var layer in layers.OrderBy(static layer => layer.RouteIndex))
        foreach (var entry in layer.LocalCallRouteEntries.OrderBy(static entry => entry.OwnerScopeId)
                                                         .ThenBy(static entry => entry.RouteId))
        {
            if (!scopeIds.Contains(entry.OwnerScopeId))
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_SCOPE_NOT_INSTALLED",
                    entry.OwnerScopeId,
                    layer.RouteIndex,
                    $"LocalCall route `{entry.RequestType.FullName}` -> `{entry.ResponseType.FullName}` targets unknown scope {entry.OwnerScopeId}.");
            }

            if (!layerIndexes.Contains(layer.RouteIndex))
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_LAYER_NOT_PUSHED",
                    entry.OwnerScopeId,
                    layer.RouteIndex,
                    $"LocalCall route `{entry.RequestType.FullName}` -> `{entry.ResponseType.FullName}` belongs to an unpushed layer index {layer.RouteIndex}.");
            }

            if (entry.RouteId < 0)
            {
                AddError(
                    diagnostics,
                    "LBTOPOLOGY_ROUTE_ID_INVALID",
                    entry.OwnerScopeId,
                    layer.RouteIndex,
                    $"LocalCall route `{entry.RequestType.FullName}` -> `{entry.ResponseType.FullName}` has invalid route id {entry.RouteId}.");
            }

            ValidateClosedType(diagnostics, entry.RequestType, entry.OwnerScopeId, layer.RouteIndex, "request");
            ValidateClosedType(diagnostics, entry.ResponseType, entry.OwnerScopeId, layer.RouteIndex, "response");
        }
    }

    private static void ValidateOwner(
        List<TopologyAuditDiagnostic> diagnostics,
        string contributionName,
        Type contributionType,
        int ownerScopeId,
        int ownerLayerIndex,
        HashSet<int> scopeIds,
        HashSet<int> layerIndexes)
    {
        if (!scopeIds.Contains(ownerScopeId))
        {
            AddError(
                diagnostics,
                "LBTOPOLOGY_SCOPE_NOT_INSTALLED",
                ownerScopeId,
                ownerLayerIndex,
                $"{contributionName} `{contributionType.FullName}` references unknown scope {ownerScopeId}.");
        }

        if (!layerIndexes.Contains(ownerLayerIndex))
        {
            AddError(
                diagnostics,
                "LBTOPOLOGY_LAYER_NOT_PUSHED",
                ownerScopeId,
                ownerLayerIndex,
                $"{contributionName} `{contributionType.FullName}` references missing layer index {ownerLayerIndex}.");
        }
    }

    private static void ValidateClosedType(
        List<TopologyAuditDiagnostic> diagnostics,
        Type type,
        int scopeId,
        int layerIndex,
        string role)
    {
        if (type.ContainsGenericParameters)
        {
            AddError(
                diagnostics,
                "LBTOPOLOGY_OPEN_GENERIC_TYPE",
                scopeId,
                layerIndex,
                $"Local topology {role} type `{type.FullName}` must be a closed type.");
        }
    }

    private static void AddError(
        List<TopologyAuditDiagnostic> diagnostics,
        string code,
        int scopeId,
        int layerIndex,
        string message)
    {
        diagnostics.Add(new TopologyAuditDiagnostic(
            TopologyAuditSeverity.Error,
            code,
            message,
            scopeId,
            layerIndex));
    }

    private readonly struct ScopedCallKey : IEquatable<ScopedCallKey>
    {
        private readonly int _scopeId;
        private readonly Type _requestType;
        private readonly Type _responseType;

        public ScopedCallKey(int scopeId, Type requestType, Type responseType)
        {
            _scopeId = scopeId;
            _requestType = requestType;
            _responseType = responseType;
        }

        public bool Equals(ScopedCallKey other)
        {
            return _scopeId == other._scopeId &&
                   _requestType == other._requestType &&
                   _responseType == other._responseType;
        }

        public override bool Equals(object? obj)
        {
            return obj is ScopedCallKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_scopeId, _requestType, _responseType);
        }
    }
}
