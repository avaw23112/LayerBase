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
        IReadOnlyDictionary<Type, int>? scopeTypeRoutes,
        ScopeTypeIdResolver? scopeTypeResolver)
    {
        _scopes = scopes;
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

            IReadOnlyDictionary<Type, int>? scopeTypeRoutes = scopeTypeResolver == null
                ? CreateScopeTypeRoutes(plans)
                : null;
            return new ScopeRuntimeHost(scopes, scopeTypeRoutes, scopeTypeResolver);
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

    public static ScopeRuntimeHost Create(
        LayerRuntime runtime,
        ScopeCompositionPlan plan,
        ModuleCallDispatchHandler[]? moduleCallDispatchers = null,
        ModuleEventDispatchHandler[]? moduleEventDispatchers = null,
        ScopeRuntimeOptions? options = null,
        ActorWorld? sharedActorWorld = null,
        ScopePostDispatcher? fallbackPostDispatcher = null,
        ScopeCallDispatcher? fallbackCallDispatcher = null,
        ScopeTypeIdResolver? scopeTypeResolver = null)
    {
        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        return Create(
            plan,
            moduleCallDispatchers,
            moduleEventDispatchers,
            options,
            sharedActorWorld ?? runtime.Actors,
            runtime,
            fallbackPostDispatcher,
            fallbackCallDispatcher,
            scopeTypeResolver);
    }

    public static ScopeRuntimeHost Create(
        ScopeCompositionPlan plan,
        ModuleCallDispatchHandler[]? moduleCallDispatchers = null,
        ModuleEventDispatchHandler[]? moduleEventDispatchers = null,
        ScopeRuntimeOptions? options = null,
        ActorWorld? sharedActorWorld = null,
        LayerRuntime? owningRuntime = null,
        ScopePostDispatcher? fallbackPostDispatcher = null,
        ScopeCallDispatcher? fallbackCallDispatcher = null,
        ScopeTypeIdResolver? scopeTypeResolver = null)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        moduleCallDispatchers ??= Array.Empty<ModuleCallDispatchHandler>();
        moduleEventDispatchers ??= Array.Empty<ModuleEventDispatchHandler>();

        ScopeCallDispatcher? callDispatcher = CreateModuleCallDispatcher(
            plan.CallRoutes, moduleCallDispatchers) ?? fallbackCallDispatcher;
        ScopePostDispatcher? postDispatcher = CreateModuleEventDispatcher(
            plan.EventRoutes, plan.EventHandlerRoutes, moduleEventDispatchers) ?? fallbackPostDispatcher;

        ScopePlan[] scopePlans = plan.Scopes;
        var scopes = new ScopeRuntime[scopePlans.Length];

        try
        {
            for (int i = 0; i < scopePlans.Length; i++)
            {
                ScopePlan scopePlan = scopePlans[i] ?? throw new ArgumentException("Scope plan list cannot contain null.", nameof(plan));
                IService[] services = ResolveServices(scopePlan.Services);
                scopes[i] = new ScopeRuntime(
                    scopePlan.Descriptor,
                    services,
                    options,
                    sharedActorWorld,
                    owningRuntime,
                    postDispatcher: postDispatcher,
                    callDispatcher: callDispatcher);

                scopes[i].UpdateServiceBindings(scopePlan.Services);
            }

            for (int i = 0; i < scopePlans.Length; i++)
            {
                scopes[i].SetContexts(scopePlans[i].Contexts.ToArray());
            }

            for (int i = 0; i < scopePlans.Length; i++)
            {
                ScopePlan scopePlan = scopePlans[i];
                ScopeRuntime scope = scopes[i];
                for (int serviceIndex = 0; serviceIndex < scopePlan.Services.Length; serviceIndex++)
                {
                    ScopeServicePlan servicePlan = scopePlan.Services[serviceIndex];
                    servicePlan.BindingInitializer?.Invoke(servicePlan.Instance, scope, servicePlan.ServiceSlot);
                }
            }

            IReadOnlyDictionary<Type, int>? scopeTypeRoutes = scopeTypeResolver == null
                ? CreateScopeTypeRoutes(scopePlans)
                : null;
            return new ScopeRuntimeHost(scopes, scopeTypeRoutes, scopeTypeResolver);
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

        ScopeCompositionPlan plan = ScopeCompositionBuilder.Build(catalog);
        return Create(
            plan,
            moduleCallDispatchers,
            moduleEventDispatchers,
            options,
            sharedActorWorld,
            owningRuntime,
            fallbackPostDispatcher,
            fallbackCallDispatcher);
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

    private static IReadOnlyDictionary<Type, int> CreateScopeTypeRoutes(IReadOnlyList<ScopePlan> plans)
    {
        var routes = new Dictionary<Type, int>();
        for (int i = 0; i < plans.Count; i++)
        {
            ScopePlan plan = plans[i];
            if (plan.ScopeType != null)
            {
                routes.Add(plan.ScopeType, plan.Descriptor.ScopeId);
            }
        }

        return routes;
    }

    private static IService[] ResolveServices(IReadOnlyList<ScopeServicePlan> servicePlans)
    {
        if (servicePlans.Count == 0)
        {
            return Array.Empty<IService>();
        }

        int maxSlot = servicePlans.Max(static plan => plan.ServiceSlot);
        var services = new IService[maxSlot + 1];
        for (int i = 0; i < servicePlans.Count; i++)
        {
            ScopeServicePlan servicePlan = servicePlans[i];
            services[servicePlan.ServiceSlot] = servicePlan.Instance;
        }

        return services;
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
