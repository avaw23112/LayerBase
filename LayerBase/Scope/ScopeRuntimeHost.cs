using LayerBase.Actor;
using LayerBase.DI;
using LayerBase.Modules;
using System.Runtime.ExceptionServices;

namespace LayerBase.Scope;

public sealed class ScopeRuntimeHost : IDisposable
{
    private readonly ScopeRuntime[] _scopes;
    private readonly ScopeRouteTable _routes;
    private readonly ManualResetEventSlim _disposeFinished = new(false);
    private int _disposeState;
    private Exception? _disposeException;
    private bool _disposed;

    private ScopeRuntimeHost(
        ScopeRuntime[] scopes,
        IReadOnlyDictionary<Type, int>? scopeTypeRoutes,
        ScopeTypeIdResolver? scopeTypeResolver,
        IReadOnlyDictionary<Type, int>? messageRouteIds,
        ScopeMessageRouteResolver? messageRouteResolver)
    {
        _scopes = scopes;
        _routes = new ScopeRouteTable(
            scopes,
            scopeTypeRoutes,
            scopeTypeResolver,
            messageRouteIds,
            messageRouteResolver);
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
        ScopeTypeIdResolver? scopeTypeResolver = null,
        ScopeMessageRouteResolver? messageRouteResolver = null)
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
                scopes[i].FinalizeScopeBuild();
            }

            IReadOnlyDictionary<Type, int>? scopeTypeRoutes = scopeTypeResolver == null
                ? CreateScopeTypeRoutes(plans)
                : null;
            return new ScopeRuntimeHost(
                scopes,
                scopeTypeRoutes,
                scopeTypeResolver,
                messageRouteIds: null,
                messageRouteResolver);
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
        ScopeTypeIdResolver? scopeTypeResolver = null,
        ScopeMessageRouteResolver? messageRouteResolver = null)
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
            scopeTypeResolver,
            messageRouteResolver);
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
        ScopeTypeIdResolver? scopeTypeResolver = null,
        ScopeMessageRouteResolver? messageRouteResolver = null)
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

                scopes[i].SetResourcePlan(scopePlan.ResourcePlan);
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

            for (int i = 0; i < scopePlans.Length; i++)
            {
                scopes[i].FinalizeScopeBuild();
            }

            IReadOnlyDictionary<Type, int>? scopeTypeRoutes = scopeTypeResolver == null
                ? CreateScopeTypeRoutes(scopePlans)
                : null;
            IReadOnlyDictionary<Type, int>? messageRouteIds = messageRouteResolver == null
                ? CreateMessageRouteIds(plan.MessageRouteIds)
                : null;
            return new ScopeRuntimeHost(
                scopes,
                scopeTypeRoutes,
                scopeTypeResolver,
                messageRouteIds,
                messageRouteResolver);
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
            fallbackCallDispatcher,
            scopeTypeResolver: null,
            messageRouteResolver: null);
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
            if (!route.IsValid)
            {
                message.Promise.SetException(new InvalidOperationException(
                    $"Scope message route {callId} is not a call route."));
                return;
            }

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
        if (eventRoutes.Count == 0)
        {
            return null;
        }

        bool hasEventHandlers = false;
        for (int i = 0; i < eventRoutes.Count; i++)
        {
            if (eventRoutes[i].HandlerCount > 0)
            {
                hasEventHandlers = true;
                break;
            }
        }

        if (!hasEventHandlers)
        {
            return null;
        }

        if (moduleEventDispatchers.Count == 0)
        {
            throw new InvalidOperationException("Scope module event dispatcher is not configured.");
        }

        var eventRoutesCopy = eventRoutes;
        var handlerRoutesCopy = eventHandlerRoutes;
        var dispatchersCopy = moduleEventDispatchers;

        return (scope, message) =>
        {
            int eventId = message.EventId;
            if ((uint)eventId >= (uint)eventRoutesCopy.Count)
            {
                throw new InvalidOperationException($"Scope event route id {eventId} is outside route table length {eventRoutesCopy.Count}.");
            }

            ScopeEventRoute route = eventRoutesCopy[eventId];
            if (route.HandlerStart < 0 || route.HandlerCount < 0)
            {
                throw new InvalidOperationException(
                    $"Scope event route {eventId} has invalid handler range start {route.HandlerStart}, count {route.HandlerCount}.");
            }

            int handlerStart = route.HandlerStart;
            int handlerEnd = handlerStart + route.HandlerCount;
            if (handlerEnd < handlerStart || handlerEnd > handlerRoutesCopy.Count)
            {
                throw new InvalidOperationException(
                    $"Scope event route {eventId} references handler range [{handlerStart}, {handlerEnd}) outside handler route table length {handlerRoutesCopy.Count}.");
            }

            for (int i = handlerStart; i < handlerEnd; i++)
            {
                ScopeEventHandlerRoute handlerRoute = handlerRoutesCopy[i];
                ushort moduleSlot = handlerRoute.ModuleSlot;
                if (moduleSlot >= dispatchersCopy.Count)
                {
                    throw new InvalidOperationException(
                        $"Scope event handler route {i} references module slot {moduleSlot} outside event dispatcher table length {dispatchersCopy.Count}.");
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

    private static IReadOnlyDictionary<Type, int> CreateMessageRouteIds(
        IReadOnlyDictionary<RuntimeTypeHandle, int> messageRouteIds)
    {
        var routes = new Dictionary<Type, int>();
        foreach (KeyValuePair<RuntimeTypeHandle, int> entry in messageRouteIds)
        {
            Type? type = Type.GetTypeFromHandle(entry.Key);
            if (type != null)
            {
                routes[type] = entry.Value;
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
        if (maxSlot < 0)
        {
            throw new ModuleBuildException(
                ModuleBuildErrorCodes.InvalidServiceContribution,
                "Scope service plans cannot contain only negative slots.");
        }

        var services = new IService[maxSlot + 1];
        for (int i = 0; i < servicePlans.Count; i++)
        {
            ScopeServicePlan servicePlan = servicePlans[i];
            if (servicePlan.ServiceSlot < 0)
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidServiceContribution,
                    $"Scope service plan at index {i} has negative slot {servicePlan.ServiceSlot}.");
            }

            if (servicePlan.Instance == null)
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidServiceContribution,
                    $"Scope service plan at slot {servicePlan.ServiceSlot} has a null service instance.");
            }

            if (services[servicePlan.ServiceSlot] != null)
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidServiceContribution,
                    $"Scope service slot {servicePlan.ServiceSlot} is contributed more than once.");
            }

            services[servicePlan.ServiceSlot] = servicePlan.Instance;
        }

        for (int i = 0; i < services.Length; i++)
        {
            if (services[i] == null)
            {
                throw new ModuleBuildException(
                    ModuleBuildErrorCodes.InvalidServiceContribution,
                    $"Scope service slot {i} is missing from the composition plan.");
            }
        }

        return services;
    }

    public void Start()
    {
        ThrowIfDisposed();
        int started = 0;
        try
        {
            for (int i = 0; i < _scopes.Length; i++)
            {
                _scopes[i].Start();
                started++;
            }
        }
        catch (Exception startException)
        {
            List<Exception>? cleanupExceptions = null;
            for (int i = started - 1; i >= 0; i--)
            {
                try
                {
                    _scopes[i].Dispose();
                }
                catch (Exception ex)
                {
                    (cleanupExceptions ??= new List<Exception>()).Add(ex);
                }
            }

            Volatile.Write(ref _disposeState, 3);
            if (cleanupExceptions is { Count: > 0 })
            {
                cleanupExceptions.Insert(0, startException);
                throw new AggregateException("Scope host start failed and one or more started scopes failed during cleanup.", cleanupExceptions);
            }

            ExceptionDispatchInfo.Capture(startException).Throw();
            throw;
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
        if (_disposed || Volatile.Read(ref _disposeState) != 0)
        {
            return;
        }

        for (int i = _scopes.Length - 1; i >= 0; i--)
        {
            _scopes[i].Stop();
        }
    }

    public void RequestStop()
    {
        if (_disposed || Volatile.Read(ref _disposeState) != 0)
        {
            return;
        }

        for (int i = _scopes.Length - 1; i >= 0; i--)
        {
            _scopes[i].RequestStop();
        }
    }

    public void Dispose()
    {
        int state = Interlocked.CompareExchange(ref _disposeState, 1, 0);
        if (state != 0)
        {
            WaitForDisposeCompletion();
            return;
        }

        try
        {
            List<Exception>? exceptions = null;
            for (int i = _scopes.Length - 1; i >= 0; i--)
            {
                try
                {
                    _scopes[i].Dispose();
                }
                catch (Exception ex)
                {
                    (exceptions ??= new List<Exception>()).Add(ex);
                }
            }

            try
            {
                _routes.Dispose();
            }
            catch (Exception ex)
            {
                (exceptions ??= new List<Exception>()).Add(ex);
            }

            _disposed = true;
            Volatile.Write(ref _disposeState, exceptions is { Count: > 0 } ? 3 : 2);

            if (exceptions is { Count: > 0 })
            {
                throw new AggregateException("One or more scopes failed during host disposal.", exceptions);
            }
        }
        catch (Exception ex)
        {
            _disposeException = ex;
            Volatile.Write(ref _disposeState, 3);
            throw;
        }
        finally
        {
            _disposeFinished.Set();
        }
    }

    private void WaitForDisposeCompletion()
    {
        int state = Volatile.Read(ref _disposeState);
        if (state == 1)
        {
            _disposeFinished.Wait();
        }

        if (_disposeException != null)
        {
            throw _disposeException;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed || Volatile.Read(ref _disposeState) != 0)
        {
            throw new ObjectDisposedException(nameof(ScopeRuntimeHost));
        }
    }
}
