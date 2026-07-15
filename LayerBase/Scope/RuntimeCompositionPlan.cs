using LayerBase.Layers;
using LayerBase.Modules;

namespace LayerBase.Scope;

internal sealed class RuntimeCompositionPlan
{
    public RuntimeCompositionPlan(
        LayerBuildPlan[] layers,
        ScopeExecutionPlan[] scopes,
        ResolvedServiceContribution[] services,
        ResolvedContextContribution[] contexts,
        ResolvedLocalCallContribution[] localCalls,
        ResolvedLayerToolContribution[] tools)
    {
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
        Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        LocalCalls = localCalls ?? throw new ArgumentNullException(nameof(localCalls));
        Tools = tools ?? throw new ArgumentNullException(nameof(tools));
    }

    public LayerBuildPlan[] Layers { get; }

    public ScopeExecutionPlan[] Scopes { get; }

    public ResolvedServiceContribution[] Services { get; }

    public ResolvedContextContribution[] Contexts { get; }

    public ResolvedLocalCallContribution[] LocalCalls { get; }

    public ResolvedLayerToolContribution[] Tools { get; }

    public static RuntimeCompositionPlan Empty { get; } =
        new(
            Array.Empty<LayerBuildPlan>(),
            Array.Empty<ScopeExecutionPlan>(),
            Array.Empty<ResolvedServiceContribution>(),
            Array.Empty<ResolvedContextContribution>(),
            Array.Empty<ResolvedLocalCallContribution>(),
            Array.Empty<ResolvedLayerToolContribution>());

    public static RuntimeCompositionPlan Build(
        IReadOnlyList<Layer> pushedLayers,
        IReadOnlyList<IAssemblyModule> modules)
    {
        if (pushedLayers == null)
            throw new ArgumentNullException(nameof(pushedLayers));
        if (modules == null)
            throw new ArgumentNullException(nameof(modules));

        var contributions = AssemblyModuleComposer.Compose(modules);
        var layerPlans = pushedLayers
            .OrderBy(static layer => layer.RouteIndex)
            .Select(static layer => new LayerBuildPlan(layer.RouteIndex, layer.GetType()))
            .ToArray();

        ValidateLayerIndexes(layerPlans);

        var layerTypeIndex = BuildLayerTypeIndex(layerPlans);
        var layerContributionBuilders = new List<LayerScopeContributionBuilder>[layerPlans.Length];
        var scopeIdsByType = new Dictionary<Type, int>();
        scopeIdsByType[typeof(MainScope)] = ScopeDefinitionIds.Main;

        var services = ResolveServices(contributions.Services, layerTypeIndex, scopeIdsByType);
        for (int serviceIndex = 0; serviceIndex < services.Length; serviceIndex++)
        {
            var service = services[serviceIndex];
            GetOrCreateContributionBuilder(layerContributionBuilders, service.OwnerLayerIndex, service.OwnerScopeId)
                .AddService(serviceIndex);
        }

        var contexts = ResolveContexts(contributions.Contexts, services, layerTypeIndex, scopeIdsByType);
        for (int contextIndex = 0; contextIndex < contexts.Length; contextIndex++)
        {
            var context = contexts[contextIndex];
            GetOrCreateContributionBuilder(layerContributionBuilders, context.OwnerLayerIndex, context.OwnerScopeId)
                .AddContext(contextIndex);
        }

        var localCalls = ResolveLocalCalls(contributions.LocalCalls, layerTypeIndex, scopeIdsByType);
        for (int localCallIndex = 0; localCallIndex < localCalls.Length; localCallIndex++)
        {
            var localCall = localCalls[localCallIndex];
            GetOrCreateContributionBuilder(layerContributionBuilders, localCall.OwnerLayerIndex, localCall.OwnerScopeId)
                .AddLocalCall(localCallIndex);
        }

        var tools = ResolveTools(contributions.Tools, layerTypeIndex, scopeIdsByType);
        for (int toolIndex = 0; toolIndex < tools.Length; toolIndex++)
        {
            var tool = tools[toolIndex];
            GetOrCreateContributionBuilder(layerContributionBuilders, tool.OwnerLayerIndex, tool.OwnerScopeId)
                .AddTool(toolIndex);
        }

        ApplyScopeContributions(layerPlans, layerContributionBuilders);

        var scopes = BuildScopeExecutionPlans(layerPlans, scopeIdsByType);
        return new RuntimeCompositionPlan(layerPlans, scopes, services, contexts, localCalls, tools);
    }

    private static ResolvedServiceContribution[] ResolveServices(
        ServiceContributionPlan[] services,
        LayerTypeIndex layerTypeIndex,
        Dictionary<Type, int> scopeIdsByType)
    {
        var resolved = new List<ResolvedServiceContribution>();
        foreach (var service in services)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                service.ModuleId,
                service.ServiceType,
                service.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(service.OwnerScopeType, scopeIdsByType);

            resolved.Add(new ResolvedServiceContribution(
                service.ModuleId,
                ownerLayerIndex,
                ownerScopeId,
                service.ServiceType,
                service.ImplementationType,
                service.Lifetime));
        }

        return resolved.OrderBy(static service => service.OwnerLayerIndex)
                       .ThenBy(static service => service.OwnerScopeId)
                       .ThenBy(static service => service.ServiceType.FullName, StringComparer.Ordinal)
                       .ThenBy(static service => service.ImplementationType.FullName, StringComparer.Ordinal)
                       .ToArray();
    }

    private static ResolvedContextContribution[] ResolveContexts(
        ContextContributionPlan[] contexts,
        ResolvedServiceContribution[] services,
        LayerTypeIndex layerTypeIndex,
        Dictionary<Type, int> scopeIdsByType)
    {
        var resolved = new List<ResolvedContextContribution>();
        foreach (var context in contexts)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                context.ModuleId,
                context.ContextType,
                context.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(context.OwnerScopeType, scopeIdsByType);
            int ownerServiceIndex = Array.FindIndex(services, service =>
                service.OwnerLayerIndex == ownerLayerIndex &&
                service.OwnerScopeId == ownerScopeId &&
                service.ServiceType == context.OwnerServiceType);

            if (ownerServiceIndex < 0)
                throw new InvalidOperationException(
                    $"Context contribution `{context.ContextType.FullName}` from module `{context.ModuleId}` must target a service in the same layer and scope.");

            resolved.Add(new ResolvedContextContribution(
                context.ModuleId,
                ownerLayerIndex,
                ownerScopeId,
                ownerServiceIndex,
                context.ContextType,
                context.OwnerServiceType));
        }

        return resolved.OrderBy(static context => context.OwnerLayerIndex)
                       .ThenBy(static context => context.OwnerScopeId)
                       .ThenBy(static context => context.ContextType.FullName, StringComparer.Ordinal)
                       .ToArray();
    }

    private static ResolvedLocalCallContribution[] ResolveLocalCalls(
        LocalCallContributionPlan[] localCalls,
        LayerTypeIndex layerTypeIndex,
        Dictionary<Type, int> scopeIdsByType)
    {
        var seen = new HashSet<LocalCallKey>();
        var resolved = new List<ResolvedLocalCallContribution>();
        foreach (var localCall in localCalls)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                localCall.ModuleId,
                localCall.HandlerType,
                localCall.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(localCall.OwnerScopeType, scopeIdsByType);
            var key = new LocalCallKey(ownerScopeId, localCall.RequestType, localCall.ResponseType);
            if (!seen.Add(key))
                throw new InvalidOperationException(
                    $"Local call `{localCall.RequestType.FullName}->{localCall.ResponseType.FullName}` has more than one handler in scope `{ownerScopeId}`.");

            resolved.Add(new ResolvedLocalCallContribution(
                localCall.ModuleId,
                ownerLayerIndex,
                ownerScopeId,
                localCall.RequestType,
                localCall.ResponseType,
                localCall.HandlerType));
        }

        return resolved.OrderBy(static localCall => localCall.OwnerScopeId)
                       .ThenBy(static localCall => localCall.RequestType.FullName, StringComparer.Ordinal)
                       .ThenBy(static localCall => localCall.ResponseType.FullName, StringComparer.Ordinal)
                       .ThenBy(static localCall => localCall.OwnerLayerIndex)
                       .ToArray();
    }

    private static ResolvedLayerToolContribution[] ResolveTools(
        LayerToolContributionPlan[] tools,
        LayerTypeIndex layerTypeIndex,
        Dictionary<Type, int> scopeIdsByType)
    {
        var seen = new HashSet<LayerToolKey>();
        var resolved = new List<ResolvedLayerToolContribution>();
        foreach (var tool in tools)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                tool.ModuleId,
                tool.ContractType,
                tool.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(tool.OwnerScopeType, scopeIdsByType);
            var key = new LayerToolKey(ownerLayerIndex, ownerScopeId, tool.ContractType, tool.LocalKey);
            if (!seen.Add(key))
                throw new InvalidOperationException(
                    $"Tool `{tool.ContractType.FullName}` with key `{tool.LocalKey}` is already registered for layer `{ownerLayerIndex}` and scope `{ownerScopeId}`.");

            resolved.Add(new ResolvedLayerToolContribution(
                tool.ModuleId,
                ownerLayerIndex,
                ownerScopeId,
                tool.ContractType,
                tool.LocalKey));
        }

        return resolved.OrderBy(static tool => tool.OwnerLayerIndex)
                       .ThenBy(static tool => tool.OwnerScopeId)
                       .ThenBy(static tool => tool.ContractType.FullName, StringComparer.Ordinal)
                       .ThenBy(static tool => tool.LocalKey, StringComparer.Ordinal)
                       .ToArray();
    }

    private static void ApplyScopeContributions(
        LayerBuildPlan[] layerPlans,
        List<LayerScopeContributionBuilder>[] layerContributionBuilders)
    {
        for (int i = 0; i < layerPlans.Length; i++)
        {
            var builders = layerContributionBuilders[i];
            if (builders == null || builders.Count == 0)
                continue;

            layerPlans[i] = layerPlans[i].WithScopeContributions(
                builders.OrderBy(static contribution => contribution.OwnerScopeId)
                        .Select(static contribution => contribution.Build())
                        .ToArray());
        }
    }

    private static void ValidateLayerIndexes(LayerBuildPlan[] layerPlans)
    {
        for (int i = 0; i < layerPlans.Length; i++)
        {
            if (layerPlans[i].LayerIndex != i)
                throw new InvalidOperationException(
                    "Layer indexes must be assigned only by LayersBuilder.Push in push order.");
        }
    }

    private static LayerTypeIndex BuildLayerTypeIndex(LayerBuildPlan[] layerPlans)
    {
        var layerIndexByType = new Dictionary<Type, int>();
        var ambiguousLayerTypes = new HashSet<Type>();
        foreach (var layerPlan in layerPlans)
        {
            if (layerIndexByType.ContainsKey(layerPlan.LayerType))
            {
                ambiguousLayerTypes.Add(layerPlan.LayerType);
                continue;
            }

            layerIndexByType[layerPlan.LayerType] = layerPlan.LayerIndex;
        }

        return new LayerTypeIndex(layerIndexByType, ambiguousLayerTypes);
    }

    private static LayerScopeContributionBuilder GetOrCreateContributionBuilder(
        List<LayerScopeContributionBuilder>[] layerContributionBuilders,
        int ownerLayerIndex,
        int ownerScopeId)
    {
        var builders = layerContributionBuilders[ownerLayerIndex];
        if (builders == null)
        {
            builders = new List<LayerScopeContributionBuilder>();
            layerContributionBuilders[ownerLayerIndex] = builders;
        }

        foreach (var builder in builders)
        {
            if (builder.OwnerScopeId == ownerScopeId)
                return builder;
        }

        var created = new LayerScopeContributionBuilder(ownerScopeId);
        builders.Add(created);
        return created;
    }

    private static ScopeExecutionPlan[] BuildScopeExecutionPlans(
        LayerBuildPlan[] layerPlans,
        Dictionary<Type, int> scopeIdsByType)
    {
        var slices = layerPlans
            .OrderBy(static layer => layer.LayerIndex)
            .Select(static layer => new ScopeLayerSlice(layer.LayerIndex))
            .ToArray();

        return scopeIdsByType
            .OrderBy(static item => item.Value)
            .Select(item => new ScopeExecutionPlan(
                new ScopeDescriptor(item.Value, item.Key.Name, item.Key),
                item.Value == ScopeDefinitionIds.Main ? ScopeOptions.Main : ScopeOptions.Inline,
                layerSlices: slices))
            .ToArray();
    }

    private static int ResolveScopeId(Type scopeType, Dictionary<Type, int> scopeIdsByType)
    {
        if (scopeIdsByType.TryGetValue(scopeType, out int existing))
            return existing;

        var field = scopeType.GetField("ScopeId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (field == null || field.FieldType != typeof(int))
            throw new InvalidOperationException(
                $"Scope `{scopeType.FullName}` must expose a public static int ScopeId.");

        int scopeId = (int)field.GetValue(null)!;
        if (scopeId < 0)
            throw new InvalidOperationException($"Scope `{scopeType.FullName}` has an invalid negative ScopeId.");

        if (scopeIdsByType.ContainsValue(scopeId))
            throw new InvalidOperationException($"Scope id `{scopeId}` is already registered.");

        scopeIdsByType[scopeType] = scopeId;
        return scopeId;
    }

    private static int ResolveOwnerLayer(
        AssemblyModuleId moduleId,
        Type contributionType,
        Type ownerLayerType,
        LayerTypeIndex layerTypeIndex)
    {
        if (!layerTypeIndex.TryResolve(ownerLayerType, out int ownerLayerIndex, out bool ambiguous))
        {
            if (ambiguous)
                throw new InvalidOperationException(
                    $"Assembly module `{moduleId}` contribution `{contributionType.FullName}` targets owner layer `{ownerLayerType.FullName}`, but that layer type was pushed more than once.");

            throw new InvalidOperationException(
                $"Assembly module `{moduleId}` contribution `{contributionType.FullName}` targets owner layer `{ownerLayerType.FullName}`, but that layer was not pushed.");
        }

        return ownerLayerIndex;
    }

    private sealed class LayerScopeContributionBuilder
    {
        private int _serviceStart = -1;
        private int _contextStart = -1;
        private int _localCallStart = -1;
        private int _toolStart = -1;

        public LayerScopeContributionBuilder(int ownerScopeId)
        {
            OwnerScopeId = ownerScopeId;
        }

        public int OwnerScopeId { get; }

        public int ServiceCount { get; private set; }

        public void AddService(int serviceIndex)
        {
            if (_serviceStart < 0)
                _serviceStart = serviceIndex;

            ServiceCount++;
        }

        public int ContextCount { get; private set; }

        public void AddContext(int contextIndex)
        {
            if (_contextStart < 0)
                _contextStart = contextIndex;

            ContextCount++;
        }

        public int LocalCallCount { get; private set; }

        public void AddLocalCall(int localCallIndex)
        {
            if (_localCallStart < 0)
                _localCallStart = localCallIndex;

            LocalCallCount++;
        }

        public int ToolCount { get; private set; }

        public void AddTool(int toolIndex)
        {
            if (_toolStart < 0)
                _toolStart = toolIndex;

            ToolCount++;
        }

        public LayerScopeContribution Build()
        {
            return new LayerScopeContribution(
                OwnerScopeId,
                NormalizeStart(_serviceStart),
                ServiceCount,
                NormalizeStart(_contextStart),
                ContextCount,
                NormalizeStart(_localCallStart),
                LocalCallCount,
                NormalizeStart(_toolStart),
                ToolCount);
        }

        private static int NormalizeStart(int start)
        {
            return start < 0 ? 0 : start;
        }
    }

    private sealed class LayerTypeIndex
    {
        private readonly Dictionary<Type, int> _layerIndexByType;
        private readonly HashSet<Type> _ambiguousLayerTypes;

        public LayerTypeIndex(Dictionary<Type, int> layerIndexByType, HashSet<Type> ambiguousLayerTypes)
        {
            _layerIndexByType = layerIndexByType;
            _ambiguousLayerTypes = ambiguousLayerTypes;
        }

        public bool TryResolve(Type layerType, out int layerIndex, out bool ambiguous)
        {
            ambiguous = _ambiguousLayerTypes.Contains(layerType);
            if (ambiguous)
            {
                layerIndex = -1;
                return false;
            }

            return _layerIndexByType.TryGetValue(layerType, out layerIndex);
        }
    }

    private readonly struct LocalCallKey : IEquatable<LocalCallKey>
    {
        private readonly int _scopeId;
        private readonly Type _requestType;
        private readonly Type _responseType;

        public LocalCallKey(int scopeId, Type requestType, Type responseType)
        {
            _scopeId = scopeId;
            _requestType = requestType;
            _responseType = responseType;
        }

        public bool Equals(LocalCallKey other)
        {
            return _scopeId == other._scopeId &&
                   _requestType == other._requestType &&
                   _responseType == other._responseType;
        }

        public override bool Equals(object? obj)
        {
            return obj is LocalCallKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_scopeId, _requestType, _responseType);
        }
    }

    private readonly struct LayerToolKey : IEquatable<LayerToolKey>
    {
        private readonly int _layerIndex;
        private readonly int _scopeId;
        private readonly Type _contractType;
        private readonly string _localKey;

        public LayerToolKey(int layerIndex, int scopeId, Type contractType, string localKey)
        {
            _layerIndex = layerIndex;
            _scopeId = scopeId;
            _contractType = contractType;
            _localKey = localKey;
        }

        public bool Equals(LayerToolKey other)
        {
            return _layerIndex == other._layerIndex &&
                   _scopeId == other._scopeId &&
                   _contractType == other._contractType &&
                   string.Equals(_localKey, other._localKey, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is LayerToolKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_layerIndex, _scopeId, _contractType, _localKey);
        }
    }
}

internal sealed class LayerBuildPlan
{
    public LayerBuildPlan(int layerIndex, Type layerType)
        : this(layerIndex, layerType, Array.Empty<LayerScopeContribution>())
    {
    }

    private LayerBuildPlan(int layerIndex, Type layerType, LayerScopeContribution[] scopeContributions)
    {
        if (layerIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));

        LayerIndex = layerIndex;
        LayerType = layerType ?? throw new ArgumentNullException(nameof(layerType));
        ScopeContributions = scopeContributions ?? throw new ArgumentNullException(nameof(scopeContributions));
    }

    public int LayerIndex { get; }

    public Type LayerType { get; }

    public LayerScopeContribution[] ScopeContributions { get; }

    public LayerBuildPlan WithScopeContributions(LayerScopeContribution[] scopeContributions)
    {
        return new LayerBuildPlan(LayerIndex, LayerType, scopeContributions);
    }
}

internal readonly struct LayerScopeContribution
{
    public LayerScopeContribution(
        int ownerScopeId,
        int serviceStart,
        int serviceCount,
        int contextStart,
        int contextCount,
        int localCallStart,
        int localCallCount,
        int toolStart,
        int toolCount)
    {
        if (ownerScopeId < 0)
            throw new ArgumentOutOfRangeException(nameof(ownerScopeId));
        if (serviceStart < 0)
            throw new ArgumentOutOfRangeException(nameof(serviceStart));
        if (serviceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(serviceCount));
        if (contextStart < 0)
            throw new ArgumentOutOfRangeException(nameof(contextStart));
        if (contextCount < 0)
            throw new ArgumentOutOfRangeException(nameof(contextCount));
        if (localCallStart < 0)
            throw new ArgumentOutOfRangeException(nameof(localCallStart));
        if (localCallCount < 0)
            throw new ArgumentOutOfRangeException(nameof(localCallCount));
        if (toolStart < 0)
            throw new ArgumentOutOfRangeException(nameof(toolStart));
        if (toolCount < 0)
            throw new ArgumentOutOfRangeException(nameof(toolCount));

        OwnerScopeId = ownerScopeId;
        ServiceStart = serviceStart;
        ServiceCount = serviceCount;
        ContextStart = contextStart;
        ContextCount = contextCount;
        LocalCallStart = localCallStart;
        LocalCallCount = localCallCount;
        ToolStart = toolStart;
        ToolCount = toolCount;
    }

    public int OwnerScopeId { get; }

    public int ServiceStart { get; }

    public int ServiceCount { get; }

    public int ContextStart { get; }

    public int ContextCount { get; }

    public int LocalCallStart { get; }

    public int LocalCallCount { get; }

    public int ToolStart { get; }

    public int ToolCount { get; }
}

internal readonly struct ResolvedServiceContribution
{
    public ResolvedServiceContribution(
        AssemblyModuleId moduleId,
        int ownerLayerIndex,
        int ownerScopeId,
        Type serviceType,
        Type implementationType,
        LayerBase.DI.ServiceLifetime lifetime)
    {
        ModuleId = moduleId;
        OwnerLayerIndex = ownerLayerIndex;
        OwnerScopeId = ownerScopeId;
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Lifetime = lifetime;
    }

    public AssemblyModuleId ModuleId { get; }

    public int OwnerLayerIndex { get; }

    public int OwnerScopeId { get; }

    public Type ServiceType { get; }

    public Type ImplementationType { get; }

    public LayerBase.DI.ServiceLifetime Lifetime { get; }
}

internal readonly struct ResolvedContextContribution
{
    public ResolvedContextContribution(
        AssemblyModuleId moduleId,
        int ownerLayerIndex,
        int ownerScopeId,
        int ownerServiceIndex,
        Type contextType,
        Type ownerServiceType)
    {
        ModuleId = moduleId;
        OwnerLayerIndex = ownerLayerIndex;
        OwnerScopeId = ownerScopeId;
        OwnerServiceIndex = ownerServiceIndex;
        ContextType = contextType ?? throw new ArgumentNullException(nameof(contextType));
        OwnerServiceType = ownerServiceType ?? throw new ArgumentNullException(nameof(ownerServiceType));
    }

    public AssemblyModuleId ModuleId { get; }

    public int OwnerLayerIndex { get; }

    public int OwnerScopeId { get; }

    public int OwnerServiceIndex { get; }

    public Type ContextType { get; }

    public Type OwnerServiceType { get; }
}

internal readonly struct ResolvedLocalCallContribution
{
    public ResolvedLocalCallContribution(
        AssemblyModuleId moduleId,
        int ownerLayerIndex,
        int ownerScopeId,
        Type requestType,
        Type responseType,
        Type handlerType)
    {
        ModuleId = moduleId;
        OwnerLayerIndex = ownerLayerIndex;
        OwnerScopeId = ownerScopeId;
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
    }

    public AssemblyModuleId ModuleId { get; }

    public int OwnerLayerIndex { get; }

    public int OwnerScopeId { get; }

    public Type RequestType { get; }

    public Type ResponseType { get; }

    public Type HandlerType { get; }
}

internal readonly struct ResolvedLayerToolContribution
{
    public ResolvedLayerToolContribution(
        AssemblyModuleId moduleId,
        int ownerLayerIndex,
        int ownerScopeId,
        Type contractType,
        string localKey)
    {
        ModuleId = moduleId;
        OwnerLayerIndex = ownerLayerIndex;
        OwnerScopeId = ownerScopeId;
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
    }

    public AssemblyModuleId ModuleId { get; }

    public int OwnerLayerIndex { get; }

    public int OwnerScopeId { get; }

    public Type ContractType { get; }

    public string LocalKey { get; }
}
