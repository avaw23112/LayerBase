using LayerBase.Actor;
using LayerBase.DI;
using LayerBase.Modules;

namespace LayerBase.Scope;

public sealed class ScopeRuntimeHost : IDisposable
{
    private readonly ScopeRuntime[] _scopes;
    private readonly ScopeRouteTable _routes;
    private bool _disposed;

    private ScopeRuntimeHost(
        ScopeRuntime[] scopes,
        IReadOnlyList<ScopeRuntimePlan> plans,
        ScopeTypeIdResolver? scopeTypeResolver)
    {
        _scopes = scopes;
        IReadOnlyDictionary<Type, int>? scopeTypeRoutes = scopeTypeResolver == null
            ? CreateScopeTypeRoutes(plans)
            : null;
        _routes = new ScopeRouteTable(scopes, scopeTypeRoutes, scopeTypeResolver);
        for (int i = 0; i < scopes.Length; i++)
        {
            scopes[i].BindRoutes(_routes);
        }
    }

    public IReadOnlyList<ScopeRuntime> Scopes => _scopes;

    public ScopeRouteTable Routes => _routes;

    public bool TryGetScope(int scopeId, out ScopeRuntime scope)
    {
        ThrowIfDisposed();
        return _routes.TryGetScope(scopeId, out scope);
    }

    public ScopeRef<TScope> GetScopeRef<TScope>(int targetScopeId)
    {
        ThrowIfDisposed();
        return _routes.GetScopeRef<TScope>(targetScopeId);
    }

    public ScopeRef<TScope> GetScopeRef<TScope>()
    {
        ThrowIfDisposed();
        return _routes.GetScopeRef<TScope>();
    }

    public bool TryPost(int targetScopeId, ScopePostMessage message)
    {
        ThrowIfDisposed();
        return _routes.TryPost(targetScopeId, message);
    }

    public bool TryCall(int targetScopeId, ScopeCallMessage message)
    {
        ThrowIfDisposed();
        return _routes.TryCall(targetScopeId, message);
    }

    public static ScopeRuntimeHost Create(
        IReadOnlyList<ScopeRuntimePlan> plans,
        ScopeRuntimeOptions? options = null,
        ActorWorld? sharedActorWorld = null,
        LayerRuntime? owningRuntime = null,
        ScopePostDispatcher? postDispatcher = null,
        ScopeCallDispatcher? callDispatcher = null,
        ScopeTypeIdResolver? scopeTypeResolver = null)
    {
        if (plans == null)
        {
            throw new ArgumentNullException(nameof(plans));
        }

        var scopes = new ScopeRuntime[plans.Count];
        try
        {
            for (int i = 0; i < plans.Count; i++)
            {
                ScopeRuntimePlan plan = plans[i] ?? throw new ArgumentException("Scope plan list cannot contain null.", nameof(plans));
                scopes[i] = new ScopeRuntime(
                    plan.Descriptor,
                    plan.Services,
                    options,
                    sharedActorWorld,
                    owningRuntime,
                    postDispatcher,
                    callDispatcher);
            }

            return new ScopeRuntimeHost(scopes, plans, scopeTypeResolver);
        }
        catch
        {
            for (int i = 0; i < scopes.Length; i++)
            {
                scopes[i]?.Dispose();
            }

            throw;
        }
    }

    public static ScopeRuntimeHost CreateFromCatalog(
        ModuleRuntimeCatalog catalog,
        ModuleCallDispatchHandler[]? moduleCallDispatchers = null,
        ModuleEventDispatchHandler[]? moduleEventDispatchers = null,
        ScopeRuntimeOptions? options = null,
        ActorWorld? sharedActorWorld = null,
        LayerRuntime? owningRuntime = null,
        ScopePostDispatcher? fallbackPostDispatcher = null,
        ScopeCallDispatcher? fallbackCallDispatcher = null)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        IReadOnlyDictionary<RuntimeTypeHandle, ScopeDefinitionContribution> scopeDefs = catalog.ScopeDefinitions;
        IReadOnlyList<ServiceContribution> services = catalog.Services;
        IReadOnlyDictionary<RuntimeTypeHandle, int> scopeIds = catalog.ScopeIds;
        IReadOnlyDictionary<RuntimeTypeHandle, int> serviceSlots = catalog.ServiceSlots;

        int maxScopeId = scopeDefs.Count > 0
            ? scopeDefs.Values.Max(d => scopeIds.TryGetValue(d.ScopeType, out int id) ? id : -1)
            : -1;

        var scopeServiceLists = new List<IService>[maxScopeId + 1];
        for (int i = 0; i < scopeServiceLists.Length; i++)
        {
            scopeServiceLists[i] = new List<IService>();
        }

        for (int i = 0; i < services.Count; i++)
        {
            ServiceContribution service = services[i];
            if (!scopeIds.TryGetValue(service.OwnerScopeType, out int scopeId))
            {
                continue;
            }

            IService? instance = service.Factory?.Invoke();
            if (instance == null)
            {
                continue;
            }

            scopeServiceLists[scopeId].Add(instance);
        }

        IReadOnlyList<ScopeCallRoute> callRoutes = catalog.CallRoutes;
        IReadOnlyList<ScopeEventRoute> eventRoutes = catalog.EventRoutes;
        IReadOnlyList<ScopeEventHandlerRoute> eventHandlerRoutes = catalog.EventHandlerRoutes;
        moduleCallDispatchers ??= Array.Empty<ModuleCallDispatchHandler>();
        moduleEventDispatchers ??= Array.Empty<ModuleEventDispatchHandler>();

        ScopeCallDispatcher? callDispatcher = CreateModuleCallDispatcher(
            callRoutes, moduleCallDispatchers) ?? fallbackCallDispatcher;
        ScopePostDispatcher? postDispatcher = CreateModuleEventDispatcher(
            eventRoutes, eventHandlerRoutes, moduleEventDispatchers) ?? fallbackPostDispatcher;

        var plans = new List<ScopeRuntimePlan>(scopeDefs.Count + 1);
        plans.Add(new ScopeRuntimePlan(ScopeDescriptors.Main, null, Array.Empty<IService>()));

        foreach (ScopeDefinitionContribution def in scopeDefs.Values.OrderBy(
            static d => GetTypeName(d.ScopeType), StringComparer.Ordinal))
        {
            if (!scopeIds.TryGetValue(def.ScopeType, out int sid))
            {
                continue;
            }

            IService[] scopeServices = sid < scopeServiceLists.Length
                ? scopeServiceLists[sid].ToArray()
                : Array.Empty<IService>();

            ScopeDescriptor descriptor = new(
                sid + 1,
                GetTypeName(def.ScopeType),
                def.Threading,
                def.Clock,
                def.TickRateHz,
                def.StopPolicy);

            plans.Add(new ScopeRuntimePlan(descriptor, Type.GetTypeFromHandle(def.ScopeType), scopeServices));
        }

        var scopes = new ScopeRuntime[plans.Count];
        try
        {
            for (int i = 0; i < plans.Count; i++)
            {
                ScopeRuntimePlan plan = plans[i];
                scopes[i] = new ScopeRuntime(
                    plan.Descriptor,
                    plan.Services,
                    options,
                    sharedActorWorld,
                    owningRuntime,
                    postDispatcher: i == 0 ? null : postDispatcher,
                    callDispatcher: i == 0 ? null : callDispatcher);
            }

            return new ScopeRuntimeHost(scopes, plans, scopeTypeResolver: null);
        }
        catch
        {
            for (int i = 0; i < scopes.Length; i++)
            {
                scopes[i]?.Dispose();
            }

            throw;
        }
    }

    private static ScopeCallDispatcher? CreateModuleCallDispatcher(
        IReadOnlyList<ScopeCallRoute> callRoutes,
        IReadOnlyList<ModuleCallDispatchHandler> moduleCallDispatchers)
    {
        if (callRoutes.Count == 0 || moduleCallDispatchers.Count == 0)
        {
            return null;
        }

        var routesCopy = callRoutes;
        var dispatchersCopy = moduleCallDispatchers;

        return (scope, message) =>
        {
            int callId = message.CallId;
            if ((uint)callId >= (uint)routesCopy.Count)
            {
                message.Promise.SetException(new InvalidOperationException($"Unknown scope call id {callId}."));
                return;
            }

            ScopeCallRoute route = routesCopy[callId];
            ushort moduleSlot = route.ModuleSlot;
            if (moduleSlot >= dispatchersCopy.Count)
            {
                message.Promise.SetException(new InvalidOperationException(
                    $"Module slot {moduleSlot} out of range for call id {callId}."));
                return;
            }

            ModuleCallDispatchHandler dispatcher = dispatchersCopy[moduleSlot];
            dispatcher(scope, route.LocalHandlerId, route.ServiceSlot, message);
        };
    }

    private static ScopePostDispatcher? CreateModuleEventDispatcher(
        IReadOnlyList<ScopeEventRoute> eventRoutes,
        IReadOnlyList<ScopeEventHandlerRoute> eventHandlerRoutes,
        IReadOnlyList<ModuleEventDispatchHandler> moduleEventDispatchers)
    {
        if (eventRoutes.Count == 0 || moduleEventDispatchers.Count == 0)
        {
            return null;
        }

        var eventRoutesCopy = eventRoutes;
        var handlerRoutesCopy = eventHandlerRoutes;
        var dispatchersCopy = moduleEventDispatchers;

        return (scope, message) =>
        {
            int eventId = message.EventId;
            if ((uint)eventId >= (uint)eventRoutesCopy.Count)
            {
                return;
            }

            ScopeEventRoute route = eventRoutesCopy[eventId];
            int handlerStart = route.HandlerStart;
            int handlerEnd = handlerStart + route.HandlerCount;

            for (int i = handlerStart; i < handlerEnd; i++)
            {
                if ((uint)i >= (uint)handlerRoutesCopy.Count)
                {
                    break;
                }

                ScopeEventHandlerRoute handlerRoute = handlerRoutesCopy[i];
                ushort moduleSlot = handlerRoute.ModuleSlot;
                if (moduleSlot >= dispatchersCopy.Count)
                {
                    continue;
                }

                ModuleEventDispatchHandler dispatcher = dispatchersCopy[moduleSlot];
                dispatcher(scope, handlerRoute.LocalHandlerId, handlerRoute.ServiceSlot, message);
            }
        };
    }

    private static string GetTypeName(RuntimeTypeHandle handle)
    {
        return Type.GetTypeFromHandle(handle)?.Name ?? "<unknown>";
    }

    private static IReadOnlyDictionary<Type, int> CreateScopeTypeRoutes(IReadOnlyList<ScopeRuntimePlan> plans)
    {
        var routes = new Dictionary<Type, int>();
        for (int i = 0; i < plans.Count; i++)
        {
            ScopeRuntimePlan plan = plans[i];
            if (plan.ScopeType != null)
            {
                routes.Add(plan.ScopeType, plan.Descriptor.ScopeId);
            }
        }

        return routes;
    }

    public void Start()
    {
        ThrowIfDisposed();
        for (int i = 0; i < _scopes.Length; i++)
        {
            _scopes[i].Start();
        }
    }

    public void Pump(float deltaTime)
    {
        ThrowIfDisposed();
        for (int i = 0; i < _scopes.Length; i++)
        {
            _scopes[i].Pump(deltaTime);
        }
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        for (int i = _scopes.Length - 1; i >= 0; i--)
        {
            _scopes[i].Stop();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _routes.Dispose();
        for (int i = _scopes.Length - 1; i >= 0; i--)
        {
            _scopes[i].Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopeRuntimeHost));
        }
    }
}
