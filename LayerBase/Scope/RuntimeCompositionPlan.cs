using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Tools;

namespace LayerBase.Scope;

public readonly struct EventMetaDataBuildPlan
{
    public EventMetaDataBuildPlan(
        int eventId,
        Type eventType,
        int ownerLayerIndex,
        int ownerScopeId,
        EventMetaDataFactory metaDataFactory,
        LayerPrewarmTargets prewarmTargets)
    {
        EventId = eventId;
        EventType = eventType;
        OwnerLayerIndex = ownerLayerIndex;
        OwnerScopeId = ownerScopeId;
        MetaDataFactory = metaDataFactory;
        PrewarmTargets = prewarmTargets;
    }

    public int EventId { get; }

    public Type EventType { get; }

    public int OwnerLayerIndex { get; }

    public int OwnerScopeId { get; }

    public EventMetaDataFactory MetaDataFactory { get; }

    public LayerPrewarmTargets PrewarmTargets { get; }
}

internal sealed class RuntimeCompositionPlan
{
    public RuntimeCompositionPlan(
        LayerBuildPlan[] layers,
        ScopeExecutionPlan[] scopes,
        ResolvedServiceContribution[] services,
        ResolvedContextContribution[] contexts,
        ResolvedLocalCallContribution[] localCalls,
        ResolvedEventHandlerContribution[] eventHandlers,
        ResolvedLayerToolContribution[] tools,
        EventMetaDataBuildPlan[] events)
    {
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
        Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        LocalCalls = localCalls ?? throw new ArgumentNullException(nameof(localCalls));
        EventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
        Tools = tools ?? throw new ArgumentNullException(nameof(tools));
        Events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public LayerBuildPlan[] Layers { get; }

    public ScopeExecutionPlan[] Scopes { get; }

    public ResolvedServiceContribution[] Services { get; }

    public ResolvedContextContribution[] Contexts { get; }

    public ResolvedLocalCallContribution[] LocalCalls { get; }

    public ResolvedEventHandlerContribution[] EventHandlers { get; }

    public ResolvedLayerToolContribution[] Tools { get; }

    public EventMetaDataBuildPlan[] Events { get; }

    public static RuntimeCompositionPlan Empty { get; } =
        new(
            Array.Empty<LayerBuildPlan>(),
            Array.Empty<ScopeExecutionPlan>(),
            Array.Empty<ResolvedServiceContribution>(),
            Array.Empty<ResolvedContextContribution>(),
            Array.Empty<ResolvedLocalCallContribution>(),
            Array.Empty<ResolvedEventHandlerContribution>(),
            Array.Empty<ResolvedLayerToolContribution>(),
            Array.Empty<EventMetaDataBuildPlan>());

    internal ReadOnlySpan<EventMetaDataBuildPlan> GetEventMetaDataPlans(int scopeId)
    {
        if (Events.Length == 0)
            return ReadOnlySpan<EventMetaDataBuildPlan>.Empty;

        int start = 0;
        int count = 0;

        for (int i = 0; i < Events.Length; i++)
        {
            if (Events[i].OwnerScopeId == scopeId)
            {
                if (count == 0)
                    start = i;
                count++;
            }
        }

        if (count == 0)
            return ReadOnlySpan<EventMetaDataBuildPlan>.Empty;

        return new ReadOnlySpan<EventMetaDataBuildPlan>(Events, start, count);
    }

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

        var scopeRegistry = new ScopeDefinitionRegistry();
        foreach (var sd in contributions.ScopeDefinitions)
        {
            scopeRegistry.Add(sd.Definition, $"module:{sd.ModuleId}");
        }

        foreach (var layer in pushedLayers)
        {
            if (layer is IGeneratedScopeDefinitionProvider provider)
            {
                foreach (var definition in provider.__GetScopeDefinitions())
                {
                    GeneratedScopeDefinition reconciled = definition;
                    var scopeIdField = definition.ScopeType.GetField(
                        "ScopeId",
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);
                    if (scopeIdField != null && scopeIdField.FieldType == typeof(int))
                    {
                        int explicitScopeId = (int)scopeIdField.GetValue(null)!;
                        if (explicitScopeId != definition.ScopeId)
                        {
                            string identity = ScopeDefinitionIds.GetIdentity(definition.ScopeType);
                            reconciled = new GeneratedScopeDefinition(
                                explicitScopeId,
                                identity,
                                definition.ScopeType,
                                definition.Factory);
                        }
                    }

                    scopeRegistry.Add(reconciled, $"layer:{layer.GetType().FullName}");
                }
            }
        }

        var layerTypeIndex = BuildLayerTypeIndex(layerPlans);
        var layerContributionBuilders = new List<LayerScopeContributionBuilder>[layerPlans.Length];

        var services = ResolveServices(contributions.Services, layerTypeIndex, scopeRegistry);
        for (int serviceIndex = 0; serviceIndex < services.Length; serviceIndex++)
        {
            var service = services[serviceIndex];
            GetOrCreateContributionBuilder(layerContributionBuilders, service.OwnerLayerIndex, service.OwnerScopeId)
                .AddService(serviceIndex);
        }

        var contexts = ResolveContexts(contributions.Contexts, services, layerTypeIndex, scopeRegistry);
        for (int contextIndex = 0; contextIndex < contexts.Length; contextIndex++)
        {
            var context = contexts[contextIndex];
            GetOrCreateContributionBuilder(layerContributionBuilders, context.OwnerLayerIndex, context.OwnerScopeId)
                .AddContext(contextIndex);
        }

        var localCalls = ResolveLocalCalls(contributions.LocalCalls, layerTypeIndex, scopeRegistry);
        for (int localCallIndex = 0; localCallIndex < localCalls.Length; localCallIndex++)
        {
            var localCall = localCalls[localCallIndex];
            GetOrCreateContributionBuilder(layerContributionBuilders, localCall.OwnerLayerIndex, localCall.OwnerScopeId)
                .AddLocalCall(localCallIndex);
        }

        var eventHandlers = ResolveEventHandlers(contributions.EventHandlers, layerTypeIndex, scopeRegistry);
        for (int eventHandlerIndex = 0; eventHandlerIndex < eventHandlers.Length; eventHandlerIndex++)
        {
            var eventHandler = eventHandlers[eventHandlerIndex];
            GetOrCreateContributionBuilder(layerContributionBuilders, eventHandler.OwnerLayerIndex, eventHandler.OwnerScopeId)
                .AddEventHandler(eventHandlerIndex);
        }

        var localTools = CollectLocalLayerTools(pushedLayers);
        var tools = ResolveTools(contributions.Tools.Concat(localTools).ToArray(), layerTypeIndex, scopeRegistry);

        var eventPlans = ResolveEventPlans(contributions.Events, layerTypeIndex, scopeRegistry);

        ApplyScopeContributions(layerPlans, layerContributionBuilders);

        var scopes = BuildScopeExecutionPlans(layerPlans, scopeRegistry);
        return new RuntimeCompositionPlan(layerPlans, scopes, services, contexts, localCalls, eventHandlers, tools, eventPlans);
    }

    private static LayerToolContributionPlan[] CollectLocalLayerTools(IReadOnlyList<Layer> pushedLayers)
    {
        var moduleId = new AssemblyModuleId("__local_layer_tools");
        var plans = new List<LayerToolContributionPlan>();
        foreach (var layer in pushedLayers)
        {
            if (layer is not IGeneratedLayerToolProvider provider)
            {
                continue;
            }

            foreach (var contribution in provider.__GetLayerToolContributions())
            {
                if (contribution.OwnerLayerType == null)
                    throw new InvalidOperationException(
                        $"LayerTool contribution `{contribution.ContractType.FullName}` must declare an owner layer.");

                if (contribution.OwnerScopeType == null)
                    throw new InvalidOperationException(
                        $"LayerTool contribution `{contribution.ContractType.FullName}` must declare an owner scope.");

                if (!typeof(IScopeDefinition).IsAssignableFrom(contribution.OwnerScopeType))
                    throw new InvalidOperationException(
                        $"Owner scope `{contribution.OwnerScopeType.FullName}` must implement {nameof(IScopeDefinition)}.");

                if (!contribution.ContractType.IsAssignableFrom(contribution.ImplementationType))
                    throw new InvalidOperationException(
                        $"LayerTool implementation `{contribution.ImplementationType.FullName}` must implement contract `{contribution.ContractType.FullName}`.");

                plans.Add(new LayerToolContributionPlan(
                    moduleId,
                    contribution.ContractType,
                    contribution.ImplementationType,
                    contribution.LocalKey,
                    contribution.OwnerLayerType,
                    contribution.OwnerScopeType,
                    contribution.Cache,
                    contribution.Factory,
                    plans.Count));
            }
        }

        return plans.ToArray();
    }

    private static ResolvedServiceContribution[] ResolveServices(
        ServiceContributionPlan[] services,
        LayerTypeIndex layerTypeIndex,
        ScopeDefinitionRegistry scopeRegistry)
    {
        var resolved = new List<ResolvedServiceContribution>();
        foreach (var service in services)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                service.ModuleId,
                service.ServiceType,
                service.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(service.OwnerScopeType, scopeRegistry);

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
        ScopeDefinitionRegistry scopeRegistry)
    {
        var resolved = new List<ResolvedContextContribution>();
        foreach (var context in contexts)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                context.ModuleId,
                context.ContextType,
                context.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(context.OwnerScopeType, scopeRegistry);
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
        ScopeDefinitionRegistry scopeRegistry)
    {
        var seen = new Dictionary<LocalCallKey, (Type OwnerLayerType, Type HandlerType)>();
        var resolved = new List<ResolvedLocalCallContribution>();
        foreach (var localCall in localCalls)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                localCall.ModuleId,
                localCall.HandlerType,
                localCall.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(localCall.OwnerScopeType, scopeRegistry);
            var key = new LocalCallKey(ownerScopeId, localCall.RequestType, localCall.ResponseType);
            if (seen.TryGetValue(key, out var existing))
                throw new ScopeLocalCallRouteConflictException(
                    ownerScopeId,
                    localCall.RequestType,
                    localCall.ResponseType,
                    existing.OwnerLayerType,
                    existing.HandlerType,
                    localCall.OwnerLayerType,
                    localCall.HandlerType);

            resolved.Add(new ResolvedLocalCallContribution(
                localCall.ModuleId,
                ownerLayerIndex,
                ownerScopeId,
                localCall.RequestType,
                localCall.ResponseType,
                localCall.HandlerType));
            seen.Add(key, (localCall.OwnerLayerType, localCall.HandlerType));
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
        ScopeDefinitionRegistry scopeRegistry)
    {
        var seen = new HashSet<LayerToolKey>();
        var implementationTypes = new HashSet<Type>();
        var resolved = new List<ResolvedLayerToolContribution>();
        foreach (var tool in tools)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                tool.ModuleId,
                tool.ContractType,
                tool.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(tool.OwnerScopeType, scopeRegistry);
            var key = new LayerToolKey(tool.ContractType, tool.LocalKey);
            if (!seen.Add(key))
                throw new InvalidOperationException(
                    $"Tool `{tool.ContractType.FullName}` with key `{tool.LocalKey}` is already registered in this runtime.");

            if (!implementationTypes.Add(tool.ImplementationType))
                throw new InvalidOperationException(
                    $"LayerTool implementation `{tool.ImplementationType.FullName}` is already registered in this runtime.");

            resolved.Add(new ResolvedLayerToolContribution(
                tool.ModuleId,
                ownerLayerIndex,
                ownerScopeId,
                tool.ContractType,
                tool.ImplementationType,
                tool.LocalKey,
                tool.Cache,
                tool.Factory));
        }

        return resolved.OrderBy(static tool => tool.OwnerLayerIndex)
                       .ThenBy(static tool => tool.OwnerScopeId)
                       .ThenBy(static tool => tool.ContractType.FullName, StringComparer.Ordinal)
                       .ThenBy(static tool => tool.LocalKey, StringComparer.Ordinal)
                       .ToArray();
    }

    private static EventMetaDataBuildPlan[] ResolveEventPlans(
        EventContributionPlan[] events,
        LayerTypeIndex layerTypeIndex,
        ScopeDefinitionRegistry scopeRegistry)
    {
        var resolved = new List<EventMetaDataBuildPlan>();
        var seen = new HashSet<(int ScopeId, int EventId)>();

        foreach (var ev in events)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                ev.ModuleId,
                ev.EventType,
                ev.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(ev.OwnerScopeType, scopeRegistry);

            IEventMetaData metaData = ev.MetaDataFactory()
                ?? throw new InvalidOperationException(
                    $"Event metadata factory for `{ev.EventType.FullName}` returned null.");

            int eventId = metaData.EventId;
            EventIdentity identity = metaData.GetIdentity();

            if (identity.EventType != ev.EventType)
            {
                throw new InvalidOperationException(
                    $"Event metadata `{metaData.GetType().FullName}` represents " +
                    $"`{identity.EventType.FullName}`, but contribution declares " +
                    $"`{ev.EventType.FullName}`.");
            }

            var key = (ownerScopeId, eventId);

            if (!seen.Add(key))
                throw new InvalidOperationException(
                    $"Event `{ev.EventType.FullName}` is already registered for scope `{ev.OwnerScopeType.FullName}`.");

            resolved.Add(new EventMetaDataBuildPlan(
                eventId,
                ev.EventType,
                ownerLayerIndex,
                ownerScopeId,
                ev.MetaDataFactory,
                ev.PrewarmTargets));
        }

        return resolved.OrderBy(static plan => plan.OwnerScopeId)
                       .ThenBy(static plan => plan.EventId)
                       .ThenBy(static plan => plan.OwnerLayerIndex)
                       .ToArray();
    }

    private static ResolvedEventHandlerContribution[] ResolveEventHandlers(
        EventHandlerContributionPlan[] eventHandlers,
        LayerTypeIndex layerTypeIndex,
        ScopeDefinitionRegistry scopeRegistry)
    {
        var resolved = new List<ResolvedEventHandlerContribution>();
        foreach (var eventHandler in eventHandlers)
        {
            int ownerLayerIndex = ResolveOwnerLayer(
                eventHandler.ModuleId,
                eventHandler.HandlerType,
                eventHandler.OwnerLayerType,
                layerTypeIndex);
            int ownerScopeId = ResolveScopeId(eventHandler.OwnerScopeType, scopeRegistry);

            resolved.Add(new ResolvedEventHandlerContribution(
                eventHandler.ModuleId,
                ownerLayerIndex,
                ownerScopeId,
                eventHandler.EventType,
                eventHandler.HandlerType,
                eventHandler.OwnerServiceType));
        }

        return resolved.OrderBy(static handler => handler.OwnerLayerIndex)
                       .ThenBy(static handler => handler.OwnerScopeId)
                       .ThenBy(static handler => handler.EventType.FullName, StringComparer.Ordinal)
                       .ThenBy(static handler => handler.HandlerType.FullName, StringComparer.Ordinal)
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
        ScopeDefinitionRegistry registry)
    {
        int[] layerIndexes = layerPlans
            .OrderBy(static layer => layer.LayerIndex)
            .Select(static layer => layer.LayerIndex)
            .ToArray();

        return registry.OrderedDefinitions
            .Select(definition =>
            {
                IScopeDefinition instance = definition.CreateDefinition();

                return new ScopeExecutionPlan(
                    new ScopeDescriptor(
                        definition.ScopeId,
                        definition.ScopeType.Name,
                        definition.ScopeType),
                    instance.Options,
                    layerSlices: layerIndexes
                        .Select(static index => new ScopeLayerSlice(index))
                        .ToArray(),
                    lifecyclePlan:
                        ScopeLifecyclePlan.EmptyForLayerIndexes(layerIndexes));
            })
            .ToArray();
    }

    private static int ResolveScopeId(Type scopeType, ScopeDefinitionRegistry scopeRegistry)
    {
        if (scopeRegistry.TryGet(scopeType, out GeneratedScopeDefinition existing))
            return existing.ScopeId;

        int scopeId = ScopeDefinitionIds.FromType(scopeType);
        if (scopeId < 0)
            throw new InvalidOperationException($"Scope `{scopeType.FullName}` has an invalid negative ScopeId.");

        string identity = ScopeDefinitionIds.GetIdentity(scopeType);
        var auto = new GeneratedScopeDefinition(
            scopeId,
            identity,
            scopeType,
            () => (IScopeDefinition)Activator.CreateInstance(scopeType)!);

        scopeRegistry.Add(auto, source: $"auto:{scopeType.FullName}");
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
        private int _eventHandlerStart = -1;

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

        public int EventHandlerCount { get; private set; }

        public void AddEventHandler(int eventHandlerIndex)
        {
            if (_eventHandlerStart < 0)
                _eventHandlerStart = eventHandlerIndex;

            EventHandlerCount++;
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
                NormalizeStart(_eventHandlerStart),
                EventHandlerCount);
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
        private readonly Type _contractType;
        private readonly string _localKey;

        public LayerToolKey(Type contractType, string localKey)
        {
            _contractType = contractType;
            _localKey = localKey;
        }

        public bool Equals(LayerToolKey other)
        {
            return _contractType == other._contractType &&
                   string.Equals(_localKey, other._localKey, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is LayerToolKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_contractType, _localKey);
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
        int eventHandlerStart,
        int eventHandlerCount)
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
        if (eventHandlerStart < 0)
            throw new ArgumentOutOfRangeException(nameof(eventHandlerStart));
        if (eventHandlerCount < 0)
            throw new ArgumentOutOfRangeException(nameof(eventHandlerCount));
        OwnerScopeId = ownerScopeId;
        ServiceStart = serviceStart;
        ServiceCount = serviceCount;
        ContextStart = contextStart;
        ContextCount = contextCount;
        LocalCallStart = localCallStart;
        LocalCallCount = localCallCount;
        EventHandlerStart = eventHandlerStart;
        EventHandlerCount = eventHandlerCount;
    }

    public int OwnerScopeId { get; }

    public int ServiceStart { get; }

    public int ServiceCount { get; }

    public int ContextStart { get; }

    public int ContextCount { get; }

    public int LocalCallStart { get; }

    public int LocalCallCount { get; }

    public int EventHandlerStart { get; }

    public int EventHandlerCount { get; }

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

internal readonly struct ResolvedEventHandlerContribution
{
    public ResolvedEventHandlerContribution(
        AssemblyModuleId moduleId,
        int ownerLayerIndex,
        int ownerScopeId,
        Type eventType,
        Type handlerType,
        Type ownerServiceType)
    {
        ModuleId = moduleId;
        OwnerLayerIndex = ownerLayerIndex;
        OwnerScopeId = ownerScopeId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        OwnerServiceType = ownerServiceType ?? throw new ArgumentNullException(nameof(ownerServiceType));
    }

    public AssemblyModuleId ModuleId { get; }

    public int OwnerLayerIndex { get; }

    public int OwnerScopeId { get; }

    public Type EventType { get; }

    public Type HandlerType { get; }

    public Type OwnerServiceType { get; }
}

internal readonly struct ResolvedLayerToolContribution
{
    public ResolvedLayerToolContribution(
        AssemblyModuleId moduleId,
        int ownerLayerIndex,
        int ownerScopeId,
        Type contractType,
        Type implementationType,
        string localKey,
        bool cache,
        LayerBase.Tools.LayerToolFactoryInvoker? factory)
    {
        ModuleId = moduleId;
        OwnerLayerIndex = ownerLayerIndex;
        OwnerScopeId = ownerScopeId;
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
        Cache = cache;
        Factory = factory;
    }

    public AssemblyModuleId ModuleId { get; }

    public int OwnerLayerIndex { get; }

    public int OwnerScopeId { get; }

    public Type ContractType { get; }

    public Type ImplementationType { get; }

    public string LocalKey { get; }

    public bool Cache { get; }

    public LayerBase.Tools.LayerToolFactoryInvoker? Factory { get; }
}
