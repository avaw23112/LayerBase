using LayerBase.Scope;

namespace LayerBase.Modules;

public static class ModuleRuntimeBuilder
{
    public static ModuleRuntimeCatalog Build(params ILayerBaseModule[] modules)
    {
        return Build((IEnumerable<ILayerBaseModule>)modules);
    }

    public static ModuleRuntimeCatalog Build(IEnumerable<ILayerBaseModule> modules)
    {
        if (modules == null)
        {
            throw new ArgumentNullException(nameof(modules));
        }

        ILayerBaseModule[] moduleArray = modules.ToArray();
        var layerContracts = new Dictionary<RuntimeTypeHandle, LayerContractContribution>();
        var scopeDefinitions = new Dictionary<RuntimeTypeHandle, ScopeDefinitionContribution>();
        var messageContracts = new Dictionary<RuntimeTypeHandle, ScopeMessageContractContribution>();
        var services = new List<ServiceContribution>();
        var contexts = new List<ContextContribution>();
        var handlers = new List<ScopeHandlerContribution>();
        var serviceModuleIndex = new Dictionary<RuntimeTypeHandle, int>();
        var handlerModuleIndex = new Dictionary<RuntimeTypeHandle, Dictionary<int, int>>();

        for (int moduleIdx = 0; moduleIdx < moduleArray.Length; moduleIdx++)
        {
            ILayerBaseModule module = moduleArray[moduleIdx];
            ModuleManifest manifest = module.Manifest ?? ModuleManifest.Empty;
            AddLayerContracts(layerContracts, manifest.LayerContracts);
            AddScopeDefinitions(scopeDefinitions, manifest.ScopeDefinitions);
            AddMessageContracts(messageContracts, manifest.MessageContracts);

            for (int i = 0; i < manifest.Services.Count; i++)
            {
                ServiceContribution service = manifest.Services[i];
                services.Add(service);
                serviceModuleIndex[service.ServiceType] = moduleIdx;
            }

            for (int i = 0; i < manifest.Contexts.Count; i++)
            {
                contexts.Add(manifest.Contexts[i]);
            }

            for (int i = 0; i < manifest.Handlers.Count; i++)
            {
                ScopeHandlerContribution handler = manifest.Handlers[i];
                handlers.Add(handler);
                if (!handlerModuleIndex.TryGetValue(handler.ServiceType, out Dictionary<int, int>? handlerMap))
                {
                    handlerMap = new Dictionary<int, int>();
                    handlerModuleIndex[handler.ServiceType] = handlerMap;
                }

                handlerMap[handler.ModuleLocalHandlerId] = moduleIdx;
            }
        }

        ValidateServices(layerContracts, scopeDefinitions, services);
        ValidateContexts(services, contexts);
        ValidateHandlers(messageContracts, services, handlers);

        IReadOnlyDictionary<ILayerBaseModule, int> moduleSlots = AllocateModuleSlots(moduleArray);
        IReadOnlyDictionary<RuntimeTypeHandle, int> scopeIds = AllocateScopeIds(scopeDefinitions.Values);
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceSlots = AllocateServiceSlots(services);
        IReadOnlyDictionary<RuntimeTypeHandle, int> messageRouteIds = AllocateMessageRouteIds(messageContracts.Values);

        var callRoutes = AllocateCallRoutes(messageContracts, handlers, serviceSlots, serviceModuleIndex, handlerModuleIndex, scopeIds);
        var eventRoutes = AllocateEventRoutes(messageContracts, handlers, serviceSlots, serviceModuleIndex, handlerModuleIndex, scopeIds);
        var eventHandlerRoutes = AllocateEventHandlerRoutes(messageContracts, handlers, serviceSlots, serviceModuleIndex, handlerModuleIndex);

        return new ModuleRuntimeCatalog(
            moduleArray,
            moduleSlots,
            layerContracts,
            scopeDefinitions,
            messageContracts,
            services,
            contexts,
            handlers,
            scopeIds,
            serviceSlots,
            messageRouteIds,
            callRoutes,
            eventRoutes,
            eventHandlerRoutes);
    }

    private static IReadOnlyList<ScopeCallRoute> AllocateCallRoutes(
        IReadOnlyDictionary<RuntimeTypeHandle, ScopeMessageContractContribution> messageContracts,
        IReadOnlyList<ScopeHandlerContribution> handlers,
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceSlots,
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceModuleIndex,
        IReadOnlyDictionary<RuntimeTypeHandle, Dictionary<int, int>> handlerModuleIndex,
        IReadOnlyDictionary<RuntimeTypeHandle, int> scopeIds)
    {
        var routes = new List<ScopeCallRoute>();
        var sortedContracts = messageContracts.Values
                                             .Where(static c => c.Kind == ScopeMessageKind.Call)
                                             .OrderBy(static c => GetTypeName(c.MessageType), StringComparer.Ordinal)
                                             .ToList();

        for (int i = 0; i < sortedContracts.Count; i++)
        {
            ScopeMessageContractContribution contract = sortedContracts[i];
            ScopeHandlerContribution? handler = handlers.FirstOrDefault(
                h => h.Kind == ScopeMessageKind.Call && h.MessageType.Equals(contract.MessageType));
            if (handler == null)
            {
                continue;
            }

            ScopeHandlerContribution resolvedHandler = handler.Value;
            RuntimeTypeHandle serviceType = resolvedHandler.ServiceType;
            RuntimeTypeHandle messageType = resolvedHandler.MessageType;

            if (!serviceSlots.TryGetValue(serviceType, out int serviceSlot))
            {
                continue;
            }

            if (!serviceModuleIndex.TryGetValue(serviceType, out int moduleSlotVal))
            {
                continue;
            }

            int localHandlerId = resolvedHandler.ModuleLocalHandlerId;
            if (!scopeIds.TryGetValue(contract.TargetScopeType, out int scopeId))
            {
                continue;
            }

            routes.Add(new ScopeCallRoute(
                scopeId,
                (ushort)moduleSlotVal,
                (ushort)localHandlerId,
                serviceSlot));
        }

        return routes;
    }

    private static IReadOnlyList<ScopeEventRoute> AllocateEventRoutes(
        IReadOnlyDictionary<RuntimeTypeHandle, ScopeMessageContractContribution> messageContracts,
        IReadOnlyList<ScopeHandlerContribution> handlers,
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceSlots,
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceModuleIndex,
        IReadOnlyDictionary<RuntimeTypeHandle, Dictionary<int, int>> handlerModuleIndex,
        IReadOnlyDictionary<RuntimeTypeHandle, int> scopeIds)
    {
        var eventHandlers = handlers
                            .Where(static h => h.Kind == ScopeMessageKind.Event)
                            .OrderBy(static h => GetTypeName(h.MessageType), StringComparer.Ordinal)
                            .ThenBy(static h => GetTypeName(h.ServiceType), StringComparer.Ordinal)
                            .ToList();

        var eventRoutes = new List<ScopeEventRoute>();
        int currentStart = 0;

        var eventGroups = messageContracts.Values
                                          .Where(static c => c.Kind == ScopeMessageKind.Event)
                                          .OrderBy(static c => GetTypeName(c.MessageType), StringComparer.Ordinal)
                                          .ToList();

        var eventTypeToHandlers = new Dictionary<RuntimeTypeHandle, int>();
        for (int i = 0; i < eventHandlers.Count; i++)
        {
            RuntimeTypeHandle messageType = eventHandlers[i].MessageType;
            if (!eventTypeToHandlers.ContainsKey(messageType))
            {
                eventTypeToHandlers[messageType] = 0;
            }

            eventTypeToHandlers[messageType]++;
        }

        for (int i = 0; i < eventGroups.Count; i++)
        {
            ScopeMessageContractContribution contract = eventGroups[i];
            if (!scopeIds.TryGetValue(contract.TargetScopeType, out int scopeId))
            {
                continue;
            }

            int handlerCount = 0;
            if (eventTypeToHandlers.TryGetValue(contract.MessageType, out int count))
            {
                handlerCount = count;
            }

            eventRoutes.Add(new ScopeEventRoute(scopeId, currentStart, handlerCount));
            currentStart += handlerCount;
        }

        return eventRoutes;
    }

    private static IReadOnlyList<ScopeEventHandlerRoute> AllocateEventHandlerRoutes(
        IReadOnlyDictionary<RuntimeTypeHandle, ScopeMessageContractContribution> messageContracts,
        IReadOnlyList<ScopeHandlerContribution> handlers,
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceSlots,
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceModuleIndex,
        IReadOnlyDictionary<RuntimeTypeHandle, Dictionary<int, int>> handlerModuleIndex)
    {
        var eventHandlers = handlers
                            .Where(static h => h.Kind == ScopeMessageKind.Event)
                            .OrderBy(static h => GetTypeName(h.MessageType), StringComparer.Ordinal)
                            .ThenBy(static h => GetTypeName(h.ServiceType), StringComparer.Ordinal);

        var routes = new List<ScopeEventHandlerRoute>();
        foreach (ScopeHandlerContribution handler in eventHandlers)
        {
            RuntimeTypeHandle serviceType = handler.ServiceType;
            if (!serviceSlots.TryGetValue(serviceType, out int serviceSlot))
            {
                continue;
            }

            if (!serviceModuleIndex.TryGetValue(serviceType, out int moduleSlotVal))
            {
                continue;
            }

            int localHandlerId = handler.ModuleLocalHandlerId;

            routes.Add(new ScopeEventHandlerRoute(
                (ushort)moduleSlotVal,
                (ushort)localHandlerId,
                serviceSlot));
        }

        return routes;
    }

    private static IReadOnlyDictionary<ILayerBaseModule, int> AllocateModuleSlots(IReadOnlyList<ILayerBaseModule> modules)
    {
        var slots = new Dictionary<ILayerBaseModule, int>();
        for (int i = 0; i < modules.Count; i++)
        {
            slots[modules[i]] = i;
        }

        return slots;
    }

    private static IReadOnlyDictionary<RuntimeTypeHandle, int> AllocateScopeIds(
        IEnumerable<ScopeDefinitionContribution> scopeDefinitions)
    {
        return scopeDefinitions
               .OrderBy(static scope => GetTypeName(scope.ScopeType), StringComparer.Ordinal)
               .Select(static (scope, index) => new { scope.ScopeType, ScopeId = index })
               .ToDictionary(static item => item.ScopeType, static item => item.ScopeId);
    }

    private static IReadOnlyDictionary<RuntimeTypeHandle, int> AllocateServiceSlots(
        IEnumerable<ServiceContribution> services)
    {
        var slots = new Dictionary<RuntimeTypeHandle, int>();

        foreach (var group in services
                     .GroupBy(static service => service.OwnerScopeType)
                     .OrderBy(static group => GetTypeName(group.Key), StringComparer.Ordinal))
        {
            int serviceSlot = 0;
            foreach (ServiceContribution service in group.OrderBy(static item => GetTypeName(item.ServiceType), StringComparer.Ordinal))
            {
                slots[service.ServiceType] = serviceSlot++;
            }
        }

        return slots;
    }

    private static IReadOnlyDictionary<RuntimeTypeHandle, int> AllocateMessageRouteIds(
        IEnumerable<ScopeMessageContractContribution> messageContracts)
    {
        return messageContracts
               .OrderBy(static message => GetTypeName(message.MessageType), StringComparer.Ordinal)
               .Select(static (message, index) => new { message.MessageType, RouteId = index })
               .ToDictionary(static item => item.MessageType, static item => item.RouteId);
    }

    private static void AddLayerContracts(
        Dictionary<RuntimeTypeHandle, LayerContractContribution> layerContracts,
        IReadOnlyList<LayerContractContribution> contributions)
    {
        for (int i = 0; i < contributions.Count; i++)
        {
            LayerContractContribution contribution = contributions[i];
            layerContracts[contribution.LayerType] = contribution;
        }
    }

    private static void AddScopeDefinitions(
        Dictionary<RuntimeTypeHandle, ScopeDefinitionContribution> scopeDefinitions,
        IReadOnlyList<ScopeDefinitionContribution> contributions)
    {
        for (int i = 0; i < contributions.Count; i++)
        {
            ScopeDefinitionContribution contribution = contributions[i];
            if (scopeDefinitions.ContainsKey(contribution.ScopeType))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.DuplicateScopeDefinition,
                    $"Scope '{GetTypeName(contribution.ScopeType)}' is defined by multiple Modules. Only one ScopeOptions definition is allowed.");
            }

            scopeDefinitions.Add(contribution.ScopeType, contribution);
        }
    }

    private static void AddMessageContracts(
        Dictionary<RuntimeTypeHandle, ScopeMessageContractContribution> messageContracts,
        IReadOnlyList<ScopeMessageContractContribution> contributions)
    {
        for (int i = 0; i < contributions.Count; i++)
        {
            ScopeMessageContractContribution contribution = contributions[i];
            messageContracts[contribution.MessageType] = contribution;
        }
    }

    private static void ValidateServices(
        IReadOnlyDictionary<RuntimeTypeHandle, LayerContractContribution> layerContracts,
        IReadOnlyDictionary<RuntimeTypeHandle, ScopeDefinitionContribution> scopeDefinitions,
        IReadOnlyList<ServiceContribution> services)
    {
        for (int i = 0; i < services.Count; i++)
        {
            ServiceContribution service = services[i];

            if (!scopeDefinitions.ContainsKey(service.OwnerScopeType))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.MissingScopeDefinition,
                    $"Service '{GetTypeName(service.ServiceType)}' targets Scope '{GetTypeName(service.OwnerScopeType)}', but no installed Module defines that Scope.");
            }

            for (int layerIndex = 0; layerIndex < service.OwnerLayerTypes.Length; layerIndex++)
            {
                RuntimeTypeHandle ownerLayer = service.OwnerLayerTypes[layerIndex];
                if (!layerContracts.ContainsKey(ownerLayer))
                {
                    throw new ModuleBuildException(
                        ModuleBuildErrorCodes.MissingLayerContract,
                        $"Service '{GetTypeName(service.ServiceType)}' targets Layer '{GetTypeName(ownerLayer)}', but no installed Module defines that Layer.");
                }
            }

            if (service.Factory == null)
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidServiceContribution,
                    $"Service '{GetTypeName(service.ServiceType)}' does not provide a factory.");
            }
        }
    }

    private static void ValidateContexts(
        IReadOnlyList<ServiceContribution> services,
        IReadOnlyList<ContextContribution> contexts)
    {
        var serviceTypes = new HashSet<RuntimeTypeHandle>(services.Select(static service => service.ServiceType));

        for (int i = 0; i < contexts.Count; i++)
        {
            ContextContribution context = contexts[i];
            if (!serviceTypes.Contains(context.OwnerServiceType))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidContextContribution,
                    $"Context '{GetTypeName(context.ContextType)}' targets Service '{GetTypeName(context.OwnerServiceType)}', but no installed Module contributes that Service.");
            }

            if (context.Factory == null)
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidContextContribution,
                    $"Context '{GetTypeName(context.ContextType)}' does not provide a factory.");
            }
        }
    }

    private static void ValidateHandlers(
        IReadOnlyDictionary<RuntimeTypeHandle, ScopeMessageContractContribution> messageContracts,
        IReadOnlyList<ServiceContribution> services,
        IReadOnlyList<ScopeHandlerContribution> handlers)
    {
        var servicesByType = services.ToDictionary(static service => service.ServiceType);
        var callHandlersByMessage = new Dictionary<RuntimeTypeHandle, ScopeHandlerContribution>();

        for (int i = 0; i < handlers.Count; i++)
        {
            ScopeHandlerContribution handler = handlers[i];
            if (!messageContracts.TryGetValue(handler.MessageType, out ScopeMessageContractContribution contract))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidHandlerContribution,
                    $"Handler service '{GetTypeName(handler.ServiceType)}' handles message '{GetTypeName(handler.MessageType)}', but no installed Module defines that message contract.");
            }

            if (!servicesByType.TryGetValue(handler.ServiceType, out ServiceContribution service))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidHandlerContribution,
                    $"Handler message '{GetTypeName(handler.MessageType)}' targets Service '{GetTypeName(handler.ServiceType)}', but no installed Module contributes that Service.");
            }

            if (!handler.ScopeType.Equals(contract.TargetScopeType))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.HandlerScopeMismatch,
                    $"Handler service '{GetTypeName(handler.ServiceType)}' belongs to Scope '{GetTypeName(handler.ScopeType)}', but message '{GetTypeName(handler.MessageType)}' targets Scope '{GetTypeName(contract.TargetScopeType)}'.");
            }

            if (!service.OwnerScopeType.Equals(handler.ScopeType))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.HandlerScopeMismatch,
                    $"Handler service '{GetTypeName(handler.ServiceType)}' belongs to Scope '{GetTypeName(service.OwnerScopeType)}', but handler contribution targets Scope '{GetTypeName(handler.ScopeType)}'.");
            }

            if (contract.Kind != handler.Kind)
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidHandlerContribution,
                    $"Handler service '{GetTypeName(handler.ServiceType)}' declares {handler.Kind} for message '{GetTypeName(handler.MessageType)}', but the message contract is {contract.Kind}.");
            }

            if (handler.Kind == ScopeMessageKind.Call)
            {
                if (callHandlersByMessage.ContainsKey(handler.MessageType))
                {
                    throw new ModuleBuildException(
                        ModuleBuildErrorCodes.CallMultipleHandlers,
                        $"Call contract '{GetTypeName(handler.MessageType)}' has multiple handlers.");
                }

                callHandlersByMessage.Add(handler.MessageType, handler);
            }
        }

        foreach (ScopeMessageContractContribution contract in messageContracts.Values)
        {
            if (contract.Kind == ScopeMessageKind.Call &&
                !callHandlersByMessage.ContainsKey(contract.MessageType))
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.CallNoHandler,
                    $"Call contract '{GetTypeName(contract.MessageType)}' has no installed handler.");
            }
        }
    }

    private static string GetTypeName(RuntimeTypeHandle handle)
    {
        return Type.GetTypeFromHandle(handle)?.FullName ?? "<unknown>";
    }
}

public static class ModuleBuildErrorCodes
{
    public const string CrossAssemblyContributionMissingModule = "LBM001";
    public const string MissingScopeDefinition = "LBM002";
    public const string HandlerScopeMismatch = "LBM003";
    public const string DuplicateScopeDefinition = "LBM004";
    public const string CallNoHandler = "LBM005";
    public const string CallMultipleHandlers = "LBM006";

    internal const string MissingLayerContract = "LBM101";
    internal const string InvalidServiceContribution = "LBM102";
    internal const string InvalidContextContribution = "LBM103";
    internal const string InvalidHandlerContribution = "LBM104";
}
