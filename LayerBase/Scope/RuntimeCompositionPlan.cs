using LayerBase.Layers;
using LayerBase.Modules;

namespace LayerBase.Scope;

internal sealed class RuntimeCompositionPlan
{
    public RuntimeCompositionPlan(LayerBuildPlan[] layers, ScopeExecutionPlan[] scopes)
    {
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
        Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
    }

    public LayerBuildPlan[] Layers { get; }

    public ScopeExecutionPlan[] Scopes { get; }

    public static RuntimeCompositionPlan Empty { get; } =
        new(Array.Empty<LayerBuildPlan>(), Array.Empty<ScopeExecutionPlan>());

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

        foreach (var service in contributions.Services)
        {
            if (!layerTypeIndex.TryResolve(service.OwnerLayerType, out int ownerLayerIndex, out bool ambiguous))
            {
                if (ambiguous)
                    throw new InvalidOperationException(
                        $"Assembly module `{service.ModuleId}` contribution `{service.ServiceType.FullName}` targets owner layer `{service.OwnerLayerType.FullName}`, but that layer type was pushed more than once.");

                throw new InvalidOperationException(
                    $"Assembly module `{service.ModuleId}` contribution `{service.ServiceType.FullName}` targets owner layer `{service.OwnerLayerType.FullName}`, but that layer was not pushed.");
            }

            int ownerScopeId = ResolveScopeId(service.OwnerScopeType, scopeIdsByType);
            var builder = GetOrCreateContributionBuilder(layerContributionBuilders, ownerLayerIndex, ownerScopeId);
            builder.AddService(service.ServiceIndex);
        }

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

        var scopes = BuildScopeExecutionPlans(layerPlans, scopeIdsByType);
        return new RuntimeCompositionPlan(layerPlans, scopes);
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

    private sealed class LayerScopeContributionBuilder
    {
        private int _serviceStart = -1;

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

        public LayerScopeContribution Build()
        {
            return new LayerScopeContribution(OwnerScopeId, _serviceStart, ServiceCount);
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
    public LayerScopeContribution(int ownerScopeId, int serviceStart, int serviceCount)
    {
        if (ownerScopeId < 0)
            throw new ArgumentOutOfRangeException(nameof(ownerScopeId));
        if (serviceStart < 0)
            throw new ArgumentOutOfRangeException(nameof(serviceStart));
        if (serviceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(serviceCount));

        OwnerScopeId = ownerScopeId;
        ServiceStart = serviceStart;
        ServiceCount = serviceCount;
    }

    public int OwnerScopeId { get; }

    public int ServiceStart { get; }

    public int ServiceCount { get; }
}
