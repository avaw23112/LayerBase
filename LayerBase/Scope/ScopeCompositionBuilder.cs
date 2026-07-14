using LayerBase.DI;
using LayerBase.Modules;
using LayerBase.Scope.Resources;

namespace LayerBase.Scope;

internal static class ScopeCompositionBuilder
{
    public static ScopeCompositionPlan Build(ModuleRuntimeCatalog catalog)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        IReadOnlyDictionary<RuntimeTypeHandle, ScopeDefinitionContribution> scopeDefinitions = catalog.ScopeDefinitions;
        IReadOnlyList<ServiceContribution> services = catalog.Services;
        IReadOnlyList<ContextContribution> contexts = catalog.Contexts;
        IReadOnlyDictionary<RuntimeTypeHandle, int> scopeIds = catalog.ScopeIds;
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceSlots = catalog.ServiceSlots;

        int maxScopeId = scopeDefinitions.Count > 0
            ? scopeDefinitions.Values.Max(d => RequireScopeId(scopeIds, d.ScopeType))
            : 0;

        var scopeDescriptorsById = new ScopeDescriptor[maxScopeId + 1];
        var scopeTypesById = new Type?[maxScopeId + 1];
        var scopeServicesById = new ScopeServicePlan[maxScopeId + 1][];
        var scopeContextsById = new List<ScopeContextPlan>[maxScopeId + 1];
        var resourcePlansById = new ScopeResourcePlan[maxScopeId + 1];
        var serviceScopeIds = new Dictionary<RuntimeTypeHandle, int>();

        scopeDescriptorsById[0] = ScopeDescriptors.Main;

        foreach (ScopeDefinitionContribution definition in scopeDefinitions.Values)
        {
            int scopeId = RequireScopeId(scopeIds, definition.ScopeType);

            scopeTypesById[scopeId] = Type.GetTypeFromHandle(definition.ScopeType);
            if (scopeTypesById[scopeId] == null)
            {
                throw new InvalidOperationException(
                    $"Scope definition contains an unknown scope type handle '{definition.ScopeType}'.");
            }

            scopeDescriptorsById[scopeId] = new ScopeDescriptor(
                scopeId,
                GetTypeName(definition.ScopeType),
                definition.Threading,
                definition.Clock,
                definition.TickRateHz,
                definition.StopPolicy);
        }

        foreach (IGrouping<RuntimeTypeHandle, ServiceContribution> scopeGroup in services
                     .GroupBy(static service => service.OwnerScopeType))
        {
            int scopeId = RequireScopeId(scopeIds, scopeGroup.Key);

            int serviceCount = scopeGroup
                .Select(service => RequireServiceSlot(serviceSlots, service.ServiceType) + 1)
                .DefaultIfEmpty(0)
                .Max();

            scopeServicesById[scopeId] = new ScopeServicePlan[serviceCount];
        }

        for (int i = 0; i < services.Count; i++)
        {
            ServiceContribution service = services[i];
            int scopeId = RequireScopeId(scopeIds, service.OwnerScopeType);

            serviceScopeIds[service.ServiceType] = scopeId;

            int serviceSlot = RequireServiceSlot(serviceSlots, service.ServiceType);

            IService? instance = service.Factory?.Invoke();
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Service factory returned null for Service '{GetTypeName(service.ServiceType)}'.");
            }

            ScopeServicePlan[] scopeServices = scopeServicesById[scopeId]
                ?? throw new InvalidOperationException(
                    $"Scope '{GetTypeName(service.OwnerScopeType)}' has no service plan array.");
            if ((uint)serviceSlot >= (uint)scopeServices.Length)
            {
                throw new InvalidOperationException(
                    $"Service slot {serviceSlot} for Service '{GetTypeName(service.ServiceType)}' is outside Scope '{GetTypeName(service.OwnerScopeType)}' service plan length {scopeServices.Length}.");
            }

            scopeServices[serviceSlot] = new ScopeServicePlan(
                serviceSlot,
                Type.GetTypeFromHandle(service.ServiceType),
                instance,
                service.BindingInitializer);
        }

        for (int i = 0; i < contexts.Count; i++)
        {
            ContextContribution context = contexts[i];
            if (!serviceScopeIds.TryGetValue(context.OwnerServiceType, out int scopeId))
            {
                throw new InvalidOperationException(
                    $"Context '{GetTypeName(context.ContextType)}' targets Service '{GetTypeName(context.OwnerServiceType)}', but the service was not composed into a scope.");
            }

            int ownerServiceSlot = RequireServiceSlot(serviceSlots, context.OwnerServiceType);

            ScopeServicePlan[] ownerServices = scopeServicesById[scopeId]
                ?? throw new InvalidOperationException(
                    $"Scope id {scopeId} has no service plan array for Context '{GetTypeName(context.ContextType)}'.");
            if ((uint)ownerServiceSlot >= (uint)ownerServices.Length)
            {
                throw new InvalidOperationException(
                    $"Owner service slot {ownerServiceSlot} for Context '{GetTypeName(context.ContextType)}' is outside Scope service plan length {ownerServices.Length}.");
            }

            IService ownerService = ownerServices[ownerServiceSlot].Instance;
            if (ownerService == null)
            {
                throw new InvalidOperationException(
                    $"Context '{GetTypeName(context.ContextType)}' owner Service '{GetTypeName(context.OwnerServiceType)}' has no composed instance.");
            }

            ILayerContext? instance = context.Factory?.Invoke(ownerService);
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Context factory returned null for Context '{GetTypeName(context.ContextType)}'.");
            }

            List<ScopeContextPlan> contextPlans = scopeContextsById[scopeId] ??= new List<ScopeContextPlan>();
            contextPlans.Add(new ScopeContextPlan(
                contextSlot: contextPlans.Count,
                contextType: Type.GetTypeFromHandle(context.ContextType),
                ownerServiceSlot: ownerServiceSlot,
                instance: instance));
        }

        var scopes = new ScopePlan[maxScopeId + 1];
        ResolvedScopeOption mainOption = ScopeOptionResolver.ResolveMain();
        scopes[0] = new ScopePlan(
            mainOption.Descriptor,
            typeof(MainScope),
            Array.Empty<ScopeServicePlan>(),
            Array.Empty<ScopeContextPlan>(),
            mainOption.RuntimeOptions,
            ScopeResourcePlan.Empty);

        for (int scopeId = 1; scopeId <= maxScopeId; scopeId++)
        {
            if (scopeTypesById[scopeId] == null)
            {
                throw new InvalidOperationException(
                    $"Scope composition is missing descriptor/type for scope id {scopeId}.");
            }

            ScopeServicePlan[] servicesForScope = scopeServicesById[scopeId] ?? Array.Empty<ScopeServicePlan>();
            ScopeContextPlan[] contextsForScope = scopeContextsById[scopeId]?.ToArray() ?? Array.Empty<ScopeContextPlan>();
            resourcePlansById[scopeId] = BuildResourcePlan(
                servicesForScope,
                contextsForScope,
                catalog.ResourceExports,
                catalog.ResourceImports);

            Type scopeType = scopeTypesById[scopeId]!;
            ResolvedScopeOption scopeOption = ScopeOptionResolver.Resolve(
                scopeType,
                scopeId,
                scopeDescriptorsById[scopeId]);

            scopes[scopeId] = new ScopePlan(
                scopeOption.Descriptor,
                scopeType,
                servicesForScope,
                contextsForScope,
                scopeOption.RuntimeOptions,
                resourcePlansById[scopeId]);
        }

        return new ScopeCompositionPlan(
            scopes,
            catalog.CallRoutes.ToArray(),
            catalog.EventRoutes.ToArray(),
            catalog.EventHandlerRoutes.ToArray(),
            catalog.MessageRouteIds);
    }

    public static ScopeCompositionPlan Build(LayerRuntime runtime, ModuleRuntimeCatalog catalog)
    {
        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        ScopeCompositionPlan basePlan = Build(catalog);
        IReadOnlyDictionary<RuntimeTypeHandle, int> layerIndexMap = runtime.GetLayerTypeIndexMap();

        var scopePlans = new ScopePlan[basePlan.Scopes.Length];
        for (int scopeIdx = 0; scopeIdx < basePlan.Scopes.Length; scopeIdx++)
        {
            ScopePlan original = basePlan.Scopes[scopeIdx];
            ScopeServicePlan[] services = original.Services;
            ScopeServicePlan[] updatedServices = new ScopeServicePlan[services.Length];
            for (int svcIdx = 0; svcIdx < services.Length; svcIdx++)
            {
                ScopeServicePlan svc = services[svcIdx];
                LayerMembership membership = ComputeMembership(svc, catalog, layerIndexMap);
                updatedServices[svcIdx] = new ScopeServicePlan(
                    svc.ServiceSlot,
                    svc.ServiceType,
                    svc.Instance,
                    svc.BindingInitializer,
                    membership);
            }

            ScopeContextPlan[] contexts = original.Contexts;
            ScopeContextPlan[] updatedContexts = new ScopeContextPlan[contexts.Length];
            for (int ctxIdx = 0; ctxIdx < contexts.Length; ctxIdx++)
            {
                ScopeContextPlan ctx = contexts[ctxIdx];
                LayerMembership membership = ComputeContextMembership(ctx, updatedServices, catalog, layerIndexMap);
                updatedContexts[ctxIdx] = new ScopeContextPlan(
                    ctx.ContextSlot,
                    ctx.ContextType,
                    ctx.OwnerServiceSlot,
                    ctx.Instance,
                    membership);
            }

            scopePlans[scopeIdx] = new ScopePlan(
                original.Descriptor,
                original.ScopeType,
                updatedServices,
                updatedContexts,
                original.RuntimeOptions,
                original.ResourcePlan);
        }

        return new ScopeCompositionPlan(
            scopePlans,
            basePlan.CallRoutes,
            basePlan.EventRoutes,
            basePlan.EventHandlerRoutes,
            basePlan.MessageRouteIds);
    }

    private static LayerMembership ComputeMembership(
        ScopeServicePlan service,
        ModuleRuntimeCatalog catalog,
        IReadOnlyDictionary<RuntimeTypeHandle, int> layerIndexMap)
    {
        if (service.ServiceType == null)
        {
            return LayerMembership.Empty;
        }

        RuntimeTypeHandle serviceTypeHandle = service.ServiceType.TypeHandle;
        ServiceContribution? found = null;
        for (int i = 0; i < catalog.Services.Count; i++)
        {
            if (catalog.Services[i].ServiceType.Equals(serviceTypeHandle))
            {
                found = catalog.Services[i];
                break;
            }
        }

        if (found == null || found.Value.OwnerLayerTypes.Length == 0)
        {
            return LayerMembership.Empty;
        }

        RuntimeTypeHandle[] ownerLayers = found.Value.OwnerLayerTypes;
        int minIndex = int.MaxValue;
        int maxIndex = int.MinValue;
        foreach (RuntimeTypeHandle layerType in ownerLayers)
        {
            if (layerIndexMap.TryGetValue(layerType, out int routeIndex))
            {
                if (routeIndex < minIndex) minIndex = routeIndex;
                if (routeIndex > maxIndex) maxIndex = routeIndex;
            }
        }

        if (minIndex == int.MaxValue)
        {
            return LayerMembership.Empty;
        }

        return new LayerMembership(minIndex, maxIndex - minIndex + 1);
    }

    private static ScopeResourcePlan BuildResourcePlan(
        ScopeServicePlan[] services,
        ScopeContextPlan[] contexts,
        IReadOnlyList<ScopeResourceExportContribution> exports,
        IReadOnlyList<ScopeResourceImportContribution> imports)
    {
        if ((exports.Count == 0 && imports.Count == 0) ||
            (services.Length == 0 && contexts.Length == 0))
        {
            return ScopeResourcePlan.Empty;
        }

        var candidates = new List<ScopeResourceObjectCandidate>(services.Length + contexts.Length);
        for (int i = 0; i < services.Length; i++)
        {
            Type? serviceType = services[i].ServiceType;
            if (serviceType != null)
            {
                candidates.Add(new ScopeResourceObjectCandidate(serviceType.TypeHandle, services[i].ServiceSlot));
            }
        }

        int contextOffset = services.Length;
        for (int i = 0; i < contexts.Length; i++)
        {
            Type? contextType = contexts[i].ContextType;
            if (contextType != null)
            {
                candidates.Add(new ScopeResourceObjectCandidate(
                    contextType.TypeHandle,
                    contextOffset + contexts[i].ContextSlot));
            }
        }

        return ScopeResourcePlanBuilder.Build(candidates, exports, imports);
    }

    private static int RequireScopeId(
        IReadOnlyDictionary<RuntimeTypeHandle, int> scopeIds,
        RuntimeTypeHandle scopeType)
    {
        if (!scopeIds.TryGetValue(scopeType, out int scopeId))
        {
            throw new InvalidOperationException(
                $"Scope '{GetTypeName(scopeType)}' is missing scope id in the runtime catalog.");
        }

        if (scopeId <= 0)
        {
            throw new InvalidOperationException(
                $"Scope '{GetTypeName(scopeType)}' has invalid scope id {scopeId}.");
        }

        return scopeId;
    }

    private static int RequireServiceSlot(
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceSlots,
        RuntimeTypeHandle serviceType)
    {
        if (!serviceSlots.TryGetValue(serviceType, out int serviceSlot))
        {
            throw new InvalidOperationException(
                $"Service '{GetTypeName(serviceType)}' is missing service slot in the runtime catalog.");
        }

        if (serviceSlot < 0)
        {
            throw new InvalidOperationException(
                $"Service '{GetTypeName(serviceType)}' has invalid service slot {serviceSlot}.");
        }

        return serviceSlot;
    }

    private static LayerMembership ComputeContextMembership(
        ScopeContextPlan context,
        ScopeServicePlan[] scopeServices,
        ModuleRuntimeCatalog catalog,
        IReadOnlyDictionary<RuntimeTypeHandle, int> layerIndexMap)
    {
        if (context.OwnerServiceSlot >= 0 && context.OwnerServiceSlot < scopeServices.Length)
        {
            return scopeServices[context.OwnerServiceSlot].Membership;
        }

        return LayerMembership.Empty;
    }

    private static string GetTypeName(RuntimeTypeHandle handle)
    {
        return Type.GetTypeFromHandle(handle)?.Name ?? "<unknown>";
    }
}
