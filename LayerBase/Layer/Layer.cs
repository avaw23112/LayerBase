using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.Delay;

namespace LayerBase.Layers;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SourceGeneratedServiceInitAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OwnerLayerAttribute : Attribute
{
    public OwnerLayerAttribute(Type layerType)
    {
        LayerType = layerType;
    }

    public Type LayerType { get; }
}

public abstract class Layer : Node, IDisposable
{
    public readonly List<(Type Req, Type Resp, Type Handler)> CallHandlers = new();
    public readonly List<Type> InvokedCalls = new(); // 新增：发出的 Call
    private readonly List<RegisteredService> m_activeServices = new();
    private readonly object m_callRouteLock = new();

    private readonly ConcurrentDictionary<Type, IDelayPublisherUpdater> m_delayPublishers = new();
    private readonly List<IDelayPublisherUpdater> m_delayUpdaters = new(); // 优化：消除 Values 迭代分配
    private readonly List<RegisteredService> m_manualServices = new();
    private readonly ServiceCollection m_serviceCollection;
    private readonly List<IUpdate> m_serviceUpdates = new();
    private readonly List<IDisposable> m_subscriptions = new();
    public readonly List<Type> ProducedEvents = new(); // 新增：产生的事件
    public readonly List<(Type OwnerType, string Key, Type FieldType, bool IsProvider)> SharedFields = new();

    // Metadata for Topology Report
    public readonly List<Type> SubscribedEvents = new();
    public readonly List<Type> NotifySafeSubscribedEvents = new();
    private GlobalEventCenter _center;
    private Type?[] m_callRouteHandlerTypes = Array.Empty<Type?>();

    private object?[] m_callRouteInvokers = Array.Empty<object?>();
    private bool m_collectingGeneratedServices;
    private bool m_disposed;
    private int m_nextServiceScopeId;

    private ConcurrentQueue<Action<Layer>> m_pendingOps = new();
    private List<ServiceProvider.ResolvedService> m_resolvedServices = new();
    private ServiceProvider? m_serviceProvider;

    protected Layer()
    {
        _center = LayerHub.EventCenter;
        m_serviceCollection = new ServiceCollection();
        ServiceLayerBinder.Attach(this, this);
    }

    public int RouteIndex { get; private set; } = -1;
    public List<IAutoSubscribe> DiscoveredSubscribers { get; private set; } = new();

    // 优化：不再包�?DiscoveredSubscribers。仅有订阅者的层被称为“被动层”，不应参与逻辑位图轮询�?
    public virtual bool HasActiveLogic =>
        m_serviceUpdates.Count > 0 || m_delayUpdaters.Count > 0;

    public void Dispose()
    {
        if (m_disposed) return;
        m_disposed = true;
        lock (m_subscriptions)
        {
            foreach (var sub in m_subscriptions) sub.Dispose();
            m_subscriptions.Clear();
        }

        m_serviceProvider?.Dispose();
        m_serviceProvider = null;
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    private readonly HashSet<Type> m_registeredServiceTypes = new();

    public void RegisterService(IService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        if (!m_registeredServiceTypes.Add(service.GetType()))    
            return;

        var registration = new RegisteredService(service, Interlocked.Increment(ref m_nextServiceScopeId));        if (m_collectingGeneratedServices)
        {
            AddActiveService(registration);
            return;
        }

        m_manualServices.Add(registration);
    }

    public T GetService<T>() where T : class
    {
        return m_serviceProvider?.Get<T>() ?? throw new InvalidOperationException("Layer not built.");
    }

    public void Build()
    {
        PrepareBuild();
        SharedFieldBinder.Bind(GetSharedFieldParticipants(true));
        FinalizeBuild();
    }

    internal void PrepareBuild()
    {
        lock (m_subscriptions)
        {
            foreach (var sub in m_subscriptions) sub.Dispose();
            m_subscriptions.Clear();
        }

        m_delayPublishers.Clear();
        m_delayUpdaters.Clear();
        m_serviceUpdates.Clear();
        m_activeServices.Clear();
        m_resolvedServices.Clear();
        m_serviceCollection.Reset();
        m_callRouteInvokers = Array.Empty<object?>();
        m_callRouteHandlerTypes = Array.Empty<Type?>();
        CallHandlers.Clear();
        m_registeredServiceTypes.Clear(); // Clear before re-adding manual services

        foreach (var registration in m_manualServices)
        {
            // Track them so generated ones won't duplicate them
            m_registeredServiceTypes.Add(registration.Service.GetType());
            AddActiveService(registration);
        }

        m_collectingGeneratedServices = true;
        LayerServiceRegistry.Apply(this);
        m_collectingGeneratedServices = false;

        var descriptors = m_serviceCollection.ToDescriptors();
        var newProvider = new ServiceProvider(descriptors, this);
        var oldProvider = Interlocked.Exchange(ref m_serviceProvider, newProvider);
        oldProvider?.Dispose();
        m_resolvedServices = newProvider.ResolveOrderedServices(descriptors);
    }

    internal void FinalizeBuild()
    {
        BindAutoCallHandlers();

        var subscribers = new List<IAutoSubscribe>();
        if (this is IAutoSubscribe layerAutoSubscribe)
        {
            layerAutoSubscribe.AutoBind(this);
            subscribers.Add(layerAutoSubscribe);
        }

        foreach (var resolved in m_resolvedServices)
        {
            if (resolved.Instance is not IAutoSubscribe auto) continue;
            auto.AutoBind(this);
            subscribers.Add(auto);
        }

        DiscoveredSubscribers = subscribers;

        var ops = Interlocked.Exchange(ref m_pendingOps, new ConcurrentQueue<Action<Layer>>());
        if (ops != null)
            foreach (var op in ops)
                op(this);

        foreach (var resolved in m_resolvedServices)
        {
            if (resolved.Instance is IInitializable init) init.Initialize();
            if (resolved.Instance is IUpdate up) m_serviceUpdates.Add(up);
        }
    }

    internal IEnumerable<SharedFieldBinder.Participant> GetSharedFieldParticipants(bool includeGlobalScope)
    {
        if (includeGlobalScope)
            yield return new SharedFieldBinder.Participant(this, this, 0);

        foreach (var service in m_activeServices)
            yield return new SharedFieldBinder.Participant(service.Service, this, service.ScopeId);

        foreach (var resolved in m_resolvedServices)
            yield return new SharedFieldBinder.Participant(resolved.Instance, this,
                resolved.Descriptor.RegistrationScopeId);
    }

    private void AddActiveService(RegisteredService registration)
    {
        m_activeServices.Add(registration);
        ServiceLayerBinder.Attach(registration.Service, this);

        using var _ = m_serviceCollection.PushRegistrationScope(registration.ScopeId);
        registration.Service.ConfigureServices(m_serviceCollection);
    }

    private void BindAutoCallHandlers()
    {
        var boundInstances = new HashSet<object>(ObjectReferenceComparer.Instance);

        BindAutoCallHandler(this, boundInstances);

        foreach (var registration in m_activeServices)
            BindAutoCallHandler(registration.Service, boundInstances);
    }

    private void BindAutoCallHandler(object candidate, HashSet<object> boundInstances)
    {
        if (!boundInstances.Add(candidate)) return;
        if (candidate is IAutoCallBinder autoCallBinder)
            autoCallBinder.AutoBindCalls(this);
    }

    internal void SetRouteIndex(int routeIndex)
    {
        RouteIndex = routeIndex;
        ServiceLayerBinder.Attach(this, this);
    }

    // 拆分后的方法：由 LayerChain 精准调用
    internal void PumpEvents()
    {
        if (RouteIndex != -1)
            LayerHub.EventCenter.PumpLayer(RouteIndex);
    }

    // 拆分后的方法：由逻辑位图驱动
    public virtual void Pump(float deltaTime)
    {
        for (var i = 0; i < m_delayUpdaters.Count; i++) m_delayUpdaters[i].Update(deltaTime);
        for (var i = 0; i < m_serviceUpdates.Count; i++) m_serviceUpdates[i].Update();
    }

    private void ThrowIfDisposed()
    {
        if (m_disposed) throw new ObjectDisposedException(nameof(Layer));
    }

    public void Subscribe<T>(EventHandleDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
        {
            LayerHub.EventCenter.Subscribe(RouteIndex, handler);
            SubscribedEvents.Add(typeof(T));
            lock (m_subscriptions)
            {
                m_subscriptions.Add(UnsubscribeDelegateToken<T>.Rent(LayerHub.EventCenter, RouteIndex, handler));
            }
        }
        else
        {
            m_pendingOps.Enqueue(l => l.Subscribe(handler));
        }
    }

    public void SubscribeNotify<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
        {
            LayerHub.EventCenter.SubscribeNotify(RouteIndex, handler);
            SubscribedEvents.Add(typeof(T));
            lock (m_subscriptions)
            {
                m_subscriptions.Add(UnsubscribeNotifyToken<T>.Rent(LayerHub.EventCenter, RouteIndex, handler));
            }
        }
        else
        {
            m_pendingOps.Enqueue(l => l.SubscribeNotify(handler));
        }
    }

    public void SubscribeNotifySafe<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
        {
            LayerHub.EventCenter.SubscribeNotifySafe(RouteIndex, handler);
            SubscribedEvents.Add(typeof(T));
            lock (m_subscriptions)
            {
                m_subscriptions.Add(UnsubscribeNotifySafeToken<T>.Rent(LayerHub.EventCenter, RouteIndex, handler));
            }
        }
        else
        {
            m_pendingOps.Enqueue(l => l.SubscribeNotifySafe(handler));
        }
    }

    public void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
        {
            LayerHub.EventCenter.SubscribeAsync(RouteIndex, handler);
            SubscribedEvents.Add(typeof(T));
            lock (m_subscriptions)
            {
                m_subscriptions.Add(UnsubscribeDelegateAsyncToken<T>.Rent(LayerHub.EventCenter, RouteIndex, handler));
            }
        }
        else
        {
            m_pendingOps.Enqueue(l => l.SubscribeAsync(handler));
        }
    }

    public LayerEventStream<T> OnEvent<T>() where T : struct
    {
        return new LayerEventStream<T>(this);
    }

    public void SubscribeParallel<T>(EventHandleDelegate<T>                  handler,
                                     Action<int, string, string, Exception>? reportError = null) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
            LayerHub.EventCenter.SubscribeParallel(RouteIndex, handler, reportError ?? LayerHub.ReportLayerEventError);
        else m_pendingOps.Enqueue(l => l.SubscribeParallel(handler, reportError));
    }

    public IDelayPublisher<T> SubscribeDelay<T>() where T : struct
    {
        return (IDelayPublisher<T>)m_delayPublishers.GetOrAdd(typeof(T), _ =>
        {
            var dp = new DelayPublisher<T>(this);
            m_delayUpdaters.Add(dp);
            return dp;
        });
    }

    protected internal void RegisterCallHandler<TRequest, TResponse>(ILayerCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        ThrowIfDisposed();
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        ServiceLayerBinder.Attach(handler, this);
        var routeId = LayerCallRouteId<TRequest, TResponse>.Id;
        var invoker = (LayerCallInvoker<TRequest, TResponse>)handler.HandleAsync;

        lock (m_callRouteLock)
        {
            var invokers = m_callRouteInvokers;
            var handlerTypes = m_callRouteHandlerTypes;

            if (routeId >= invokers.Length)
            {
                var newSize = Math.Max(routeId + 1, invokers.Length == 0 ? 4 : invokers.Length * 2);
                var newInvokers = new object?[newSize];
                var newHandlerTypes = new Type?[newSize];
                Array.Copy(invokers, newInvokers, invokers.Length);
                Array.Copy(handlerTypes, newHandlerTypes, handlerTypes.Length);
                invokers = newInvokers;
                handlerTypes = newHandlerTypes;
            }

            if (invokers[routeId] != null)
            {
                if (handlerTypes[routeId] == handler.GetType()) return; // Already registered

                throw new LayerCallRouteConflictException(
                    GetType(),
                    typeof(TRequest),
                    typeof(TResponse),
                    handlerTypes[routeId] ?? invokers[routeId]!.GetType(),
                    handler.GetType());
            }

            invokers[routeId] = invoker;
            handlerTypes[routeId] = handler.GetType();
            CallHandlers.Add((typeof(TRequest), typeof(TResponse), handler.GetType()));
            Volatile.Write(ref m_callRouteInvokers, invokers);
            Volatile.Write(ref m_callRouteHandlerTypes, handlerTypes);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LayerCallInvoker<TRequest, TResponse> GetCallInvoker<TRequest, TResponse>()
        where TRequest : struct
        where TResponse : struct
    {
        var routeId = LayerCallRouteId<TRequest, TResponse>.Id;
        var invokers = Volatile.Read(ref m_callRouteInvokers);
        if ((uint)routeId >= (uint)invokers.Length || invokers[routeId] == null)
            ThrowRouteNotFound<TRequest, TResponse>();

        return (LayerCallInvoker<TRequest, TResponse>)invokers[routeId]!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LBTask<TResponse> CallAsync<TRequest, TResponse>(TRequest          request,
                                                              CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (m_disposed) ThrowDisposed();
        if (cancellationToken.IsCancellationRequested) return LBTask<TResponse>.FromCanceled(cancellationToken);

        var routeId = LayerCallRouteId<TRequest, TResponse>.Id;
        var invokers = Volatile.Read(ref m_callRouteInvokers);
        if ((uint)routeId >= (uint)invokers.Length || invokers[routeId] == null)
            ThrowRouteNotFound<TRequest, TResponse>();

        return ((LayerCallInvoker<TRequest, TResponse>)invokers[routeId]!)(request, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowDisposed() => throw new ObjectDisposedException(GetType().Name);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowRouteNotFound<TRequest, TResponse>()
        where TRequest : struct
        where TResponse : struct
    {
        throw new LayerCallRouteNotFoundException(GetType(), typeof(TRequest), typeof(TResponse));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendLocal<T>(in T value) where T : struct
    {
        return LayerHub.EventCenter.SendLocal(RouteIndex, value);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState Send<T>(in T value) where T : struct
    {
        return LayerHub.EventCenter.Send(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostLocal<T>(in T value) where T : struct
    {
        LayerHub.EventCenter.PostLocal(RouteIndex, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T>(in T value) where T : struct
    {
        LayerHub.EventCenter.Post(value);
    }

    internal readonly struct RegisteredService
    {
        public RegisteredService(IService service, int scopeId)
        {
            Service = service;
            ScopeId = scopeId;
        }

        public IService Service { get; }
        public int ScopeId { get; }
    }

    private sealed class ObjectReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ObjectReferenceComparer Instance = new();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    private sealed class UnsubscribeDelegateToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeDelegateToken<T>> Pool = new();
        private GlobalEventCenter _center;
        private EventHandleDelegate<T> _handler;
        private int _layerIndex;

        public void Dispose()
        {
            _center.Unsubscribe(_layerIndex, _handler);
            _center = null!;
            _handler = null!;
            Pool.Add(this);
        }

        public static UnsubscribeDelegateToken<T> Rent(GlobalEventCenter c, int l, EventHandleDelegate<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeDelegateToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            return t;
        }
    }

    private sealed class UnsubscribeDelegateAsyncToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeDelegateAsyncToken<T>> Pool = new();
        private GlobalEventCenter _center;
        private EventHandleDelegateAsync<T> _handler;
        private int _layerIndex;

        public void Dispose()
        {
            _center.UnsubscribeAsync(_layerIndex, _handler);
            _center = null!;
            _handler = null!;
            Pool.Add(this);
        }

        public static UnsubscribeDelegateAsyncToken<T> Rent(GlobalEventCenter c, int l, EventHandleDelegateAsync<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeDelegateAsyncToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            return t;
        }
    }

    private sealed class UnsubscribeNotifyToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeNotifyToken<T>> Pool = new();
        private GlobalEventCenter _center;
        private EventNotifyDelegate<T> _handler;
        private int _layerIndex;

        public void Dispose()
        {
            _center.UnsubscribeNotify(_layerIndex, _handler);
            _center = null!;
            _handler = null!;
            Pool.Add(this);
        }

        public static UnsubscribeNotifyToken<T> Rent(GlobalEventCenter c, int l, EventNotifyDelegate<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeNotifyToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            return t;
        }
    }

    private sealed class UnsubscribeNotifySafeToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeNotifySafeToken<T>> Pool = new();
        private GlobalEventCenter _center;
        private EventNotifyDelegate<T> _handler;
        private int _layerIndex;

        public void Dispose()
        {
            _center.UnsubscribeNotifySafe(_layerIndex, _handler);
            _center = null!;
            _handler = null!;
            Pool.Add(this);
        }

        public static UnsubscribeNotifySafeToken<T> Rent(GlobalEventCenter c, int l, EventNotifyDelegate<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeNotifySafeToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            return t;
        }
    }
}

