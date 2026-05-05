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

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OwnerLayerAttribute : Attribute
{
    public OwnerLayerAttribute(Type layerType)
    {
        LayerType = layerType;
    }

    public Type LayerType { get; }
}

/// <summary>
/// 逻辑分层（Layer）的基类。Layer 是事件路由、服务管理和逻辑执行的基本单元。
/// </summary>
public abstract class Layer : Node, IDisposable
{
    private readonly List<(Type Req, Type Resp, Type Handler)> m_callHandlers = new();
    private readonly List<RegisteredService> m_activeServices = new();
    private readonly object m_callRouteLock = new();

    private readonly ConcurrentDictionary<Type, IDelayPublisherInternal> m_delayPublishers = new();
    private readonly List<RegisteredService> m_manualServices = new();
    private readonly List<Type> m_producedEvents = new();

    private readonly HashSet<Type> m_registeredServiceTypes = new();
    private readonly ServiceCollection m_serviceCollection;
    private readonly List<IUpdate> m_serviceUpdates = new();
    private readonly List<IPostBuild> m_postBuilds = new();
    private readonly List<IRuntimeStart> m_runtimeStarts = new();
    private readonly List<IRuntimeStop> m_runtimeStops = new();
    private readonly List<IFixedUpdate> m_fixedUpdates = new();
    private readonly List<(Type OwnerType, string Key, Type FieldType, bool IsProvider)> m_sharedFields = new();
    private readonly List<IDisposable> m_subscriptions = new();
    private readonly List<Type> m_subscribedEvents = new();

    private Type?[] m_callRouteHandlerTypes = Array.Empty<Type?>();

    private object?[] m_callRouteInvokers = Array.Empty<object?>();
    private bool m_collectingGeneratedServices;
    private int m_disposed;
    private int m_nextServiceScopeId;

    private ConcurrentQueue<Action<Layer>> m_pendingOps = new();
    private List<ServiceProvider.ResolvedService> m_resolvedServices = new();
    private ServiceProvider? m_serviceProvider;

    public LayerRuntime? OwnerContext { get; private set; }

    internal void AttachToContext(LayerRuntime context)
    {
        OwnerContext = context;
        ServiceLayerBinder.Attach(this, this);
    }

    protected Layer()
    {
        m_serviceCollection = new ServiceCollection();
    }

    /// <summary>
    /// 获取 Layer 的路由索引。
    /// </summary>
    public int RouteIndex { get; private set; } = -1;
    public IReadOnlyList<IAutoSubscribe> DiscoveredSubscribers { get; private set; } = Array.Empty<IAutoSubscribe>();
    public IReadOnlyList<(Type Req, Type Resp, Type Handler)> CallHandlers => m_callHandlers;
    public IReadOnlyList<Type> ProducedEvents => m_producedEvents;
    public IReadOnlyList<(Type OwnerType, string Key, Type FieldType, bool IsProvider)> SharedFields => m_sharedFields;
    public IReadOnlyList<Type> SubscribedEvents => m_subscribedEvents;


    public virtual bool HasActiveLogic =>
        m_serviceUpdates.Count > 0 || m_delayPublishers.Count > 0 || m_fixedUpdates.Count > 0;

    internal bool HasDelayPublisher
    {
        get
        {
            if (m_delayPublishers.IsEmpty) return false;
            foreach (var kvp in m_delayPublishers)
            {
                if (kvp.Value.HasActiveDelays) return true;
            }
            return false;
        }
    }


    /// <summary>
    /// 释放 Layer 资源。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref m_disposed, 1) != 0) return;
        lock (m_subscriptions)
        {
            foreach (var sub in m_subscriptions) sub.Dispose();
            m_subscriptions.Clear();
        }

        while (m_pendingOps.TryDequeue(out _))
        {
        }

        DetachResolvedObjects();
        ReleaseDelayPublishers();

        m_serviceUpdates.Clear();
        m_activeServices.Clear();
        m_resolvedServices.Clear();

        m_serviceProvider?.Dispose();
        m_serviceProvider = null;

        DetachFromContext();
    }

    private void ReleaseDelayPublishers()
    {
        if (m_delayPublishers.IsEmpty)
        {
            return;
        }

        var manager = OwnerContext?.DelayManager;

        foreach (var publisher in m_delayPublishers.Values)
        {
            if (manager != null && publisher.PublisherId >= 0)
            {
                // manager.UnregisterPublisher will call publisher.Deactivate()
                manager.UnregisterPublisher(publisher.PublisherId);
            }
            
            // Safety fallback: ensure publisher is deactivated even if manager is gone 
            // or if it was registered in a different manager (e.g. during a context switch)
            publisher.Deactivate();
        }

        m_delayPublishers.Clear();

        OwnerContext?.MarkDelayDirty();
    }

    private void DetachResolvedObjects()
    {
        ServiceLayerBinder.Detach(this);

        foreach (var registration in m_activeServices)
        {
            ServiceLayerBinder.Detach(registration.Service);
        }

        foreach (var registration in m_manualServices)
        {
            ServiceLayerBinder.Detach(registration.Service);
        }

        foreach (var resolved in m_resolvedServices)
        {
            ServiceLayerBinder.Detach(resolved.Instance);
        }
    }

    internal void DetachFromContext()
    {
        ServiceLayerBinder.Detach(this);

        OwnerContext = null;
        RouteIndex = -1;
    }

    /// <summary>
    /// 配置 Layer 专属的服务容器。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// 初始化 Layer 的服务。
    /// </summary>
    internal void InitializeServices()
    {
        // 触发生成器实现的 Layer 自动挂载逻辑
        if (this is IAutoLayerMount layerMount)
        {
            layerMount.__AutoMountServices(this);
        }
    }

    /// <summary>
    /// 手动注册一个服务到当前 Layer。
    /// </summary>
    /// <param name="service">要注册的服务实例。</param>
    public void RegisterService(IService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        if (m_serviceProvider != null)
        {
            throw new InvalidOperationException(
                "RegisterService must be called before the layer is built. Register services before LayerHub.CreateLayers().Push(...).Build().");
        }

        if (!m_registeredServiceTypes.Add(service.GetType()))
            return;

        // 绑定 Service 到当前 Layer (写入绑定槽位)。允许用户在 Push 前手动注册；
        // PrepareBuild/AddActiveService 会在 Layer 已附加到 Runtime 后再次写入有效绑定。
        if (OwnerContext != null)
        {
            ServiceLayerBinder.Attach(service, this);
        }

        var registration = new RegisteredService(service, Interlocked.Increment(ref m_nextServiceScopeId));
        if (m_collectingGeneratedServices)
        {
            AddActiveService(registration);
            return;
        }

        m_manualServices.Add(registration);
    }

    /// <summary>
    /// 从当前 Layer 解析服务实例。
    /// </summary>
    /// <typeparam name="T">服务类型。</typeparam>
    /// <returns>服务实例。</returns>
    public T GetService<T>() where T : class
    {
        return m_serviceProvider?.Get<T>() ?? throw new InvalidOperationException("Layer not built.");
    }

    internal void PrepareBuild()
    {
        DetachResolvedObjects();

        lock (m_subscriptions)
        {
            foreach (var sub in m_subscriptions) sub.Dispose();
            m_subscriptions.Clear();
        }

        ReleaseDelayPublishers();

        m_serviceUpdates.Clear();
        m_postBuilds.Clear();
        m_runtimeStarts.Clear();
        m_runtimeStops.Clear();
        m_fixedUpdates.Clear();
        m_activeServices.Clear();
        m_resolvedServices.Clear();
        m_serviceCollection.Reset();
        m_callRouteInvokers = Array.Empty<object?>();
        m_callRouteHandlerTypes = Array.Empty<Type?>();
        m_callHandlers.Clear();
        m_producedEvents.Clear();
        m_sharedFields.Clear();
        m_subscribedEvents.Clear();
        m_registeredServiceTypes.Clear();

        foreach (var registration in m_manualServices)
        {
            m_registeredServiceTypes.Add(registration.Service.GetType());
            AddActiveService(registration);
        }

        m_collectingGeneratedServices = true;
        InitializeServices();
        ConfigureServices(m_serviceCollection);
        m_collectingGeneratedServices = false;

        var descriptors = m_serviceCollection.ToDescriptors();
        var newProvider = new ServiceProvider(OwnerContext!.Services, descriptors, this);
        var oldProvider = Interlocked.Exchange(ref m_serviceProvider, newProvider);
        oldProvider?.Dispose();

        newProvider.InjectMembers(this);
        foreach (var registration in m_activeServices)
        {
            newProvider.InjectMembers(registration.Service);
        }

        m_resolvedServices = newProvider.ResolveOrderedServices(descriptors);
    }

    internal void BuildAutoBinding()
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
    }
    internal void LifecycleBuild()
    {
        foreach (var resolved in m_resolvedServices)
        {
            if (resolved.Instance is IInitializable init) init.Initialize();
            if (resolved.Instance is IUpdate up) m_serviceUpdates.Add(up);
            if (resolved.Instance is IFixedUpdate fixedUpdate) m_fixedUpdates.Add(fixedUpdate);
            if (resolved.Instance is IPostBuild postBuild) m_postBuilds.Add(postBuild);
            if (resolved.Instance is IRuntimeStart runtimeStart) m_runtimeStarts.Add(runtimeStart);
            if (resolved.Instance is IRuntimeStop runtimeStop) m_runtimeStops.Add(runtimeStop);
        }

        if (this is IFixedUpdate layerFixedUpdate) m_fixedUpdates.Add(layerFixedUpdate);
        if (this is IPostBuild layerPostBuild) m_postBuilds.Add(layerPostBuild);
        if (this is IRuntimeStart layerRuntimeStart) m_runtimeStarts.Add(layerRuntimeStart);
        if (this is IRuntimeStop layerRuntimeStop) m_runtimeStops.Add(layerRuntimeStop);
    }

    internal void RunPostBuild()
    {
        for (var i = 0; i < m_postBuilds.Count; i++)
        {
            m_postBuilds[i].PostBuild();
        }
    }

    internal void RunRuntimeStart()
    {
        for (var i = 0; i < m_runtimeStarts.Count; i++)
        {
            m_runtimeStarts[i].RuntimeStart();
        }
    }

    internal void RunRuntimeStop()
    {
        for (var i = 0; i < m_runtimeStops.Count; i++)
        {
            m_runtimeStops[i].RuntimeStop();
        }
    }

    internal void PumpFixed(float fixedDeltaTime)
    {
        for (var i = 0; i < m_fixedUpdates.Count; i++)
        {
            m_fixedUpdates[i].FixedUpdate(fixedDeltaTime);
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

        if (registration.Service is IAutoServiceMount autoMount)
        {
            autoMount.__AutoMountContexts(m_serviceCollection);
        }

        registration.Service.ConfigureServices(m_serviceCollection);
    }

    public void RecordSubscribedEvent(Type eventType)
    {
        if (eventType == null) throw new ArgumentNullException(nameof(eventType));
        m_subscribedEvents.Add(eventType);
    }

    public void RecordProducedEvent(Type eventType)
    {
        if (eventType == null) throw new ArgumentNullException(nameof(eventType));
        m_producedEvents.Add(eventType);
    }

    internal void RecordSharedField(Type ownerType, string key, Type fieldType, bool isProvider)
    {
        m_sharedFields.Add((ownerType, key, fieldType, isProvider));
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


    public virtual void Pump(float deltaTime)
    {
        for (var i = 0; i < m_serviceUpdates.Count; i++) m_serviceUpdates[i].Update();
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref m_disposed) != 0) throw new ObjectDisposedException(nameof(Layer));
    }

    public void SubscribeFlow<T>(EventHandleDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.SubscribeFlow(RouteIndex, handler);
            m_subscriptions.Add(UnsubscribeFlowToken<T>.Rent(OwnerContext.EventCenter, RouteIndex, handler));
        }
        else
        {
            m_pendingOps.Enqueue(l => l.SubscribeFlow(handler));
        }
    }

    public void SubscribeNotify<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.SubscribeNotify(RouteIndex, handler);
            m_subscriptions.Add(UnsubscribeNotifyToken<T>.Rent(OwnerContext.EventCenter, RouteIndex, handler));
        }
        else
        {
            m_pendingOps.Enqueue(l => l.SubscribeNotify(handler));
        }
    }

    public void Subscribe<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.Subscribe(RouteIndex, handler);
            m_subscriptions.Add(UnsubscribeToken<T>.Rent(OwnerContext.EventCenter, RouteIndex, handler));
        }
        else
        {
            m_pendingOps.Enqueue(l => l.Subscribe(handler));
        }
    }

    public void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.SubscribeAsync(RouteIndex, handler);
            m_subscriptions.Add(UnsubscribeDelegateAsyncToken<T>.Rent(OwnerContext.EventCenter, RouteIndex, handler));
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

    public void SubscribeParallel<T>(EventNotifyDelegate<T>               handler,
                                     Action<int, int, int, Exception>? reportError = null) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.SubscribeParallel(RouteIndex, handler, reportError ?? OwnerContext.ReportLayerEventError);
            m_subscriptions.Add(UnsubscribeParallelToken<T>.Rent(OwnerContext.EventCenter, RouteIndex, handler));
        }
        else
        {
            m_pendingOps.Enqueue(l => l.SubscribeParallel(handler, reportError));
        }
    }

    public IDelayPublisher<T> SubscribeDelay<T>() where T : struct
    {
        var type = typeof(T);
        if (m_delayPublishers.TryGetValue(type, out var existing)) return (IDelayPublisher<T>)existing;

        var manager = OwnerContext?.DelayManager;
        if (manager == null) throw new InvalidOperationException("DelayPublisherManager not initialized.");

        var publisher = new DelayPublisher<T>(manager, this);
        int id = manager.RegisterPublisher(publisher);
        publisher.SetId(id);

        var actual = m_delayPublishers.GetOrAdd(type, publisher);
        if (actual == publisher)
        {
            OwnerContext?.MarkDelayDirty();
        }
        return (IDelayPublisher<T>)actual;
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
                if (handlerTypes[routeId] == handler.GetType()) return;

                throw new LayerCallRouteConflictException(
                    GetType(),
                    typeof(TRequest),
                    typeof(TResponse),
                    handlerTypes[routeId] ?? invokers[routeId]!.GetType(),
                    handler.GetType());
            }

            invokers[routeId] = invoker;
            handlerTypes[routeId] = handler.GetType();
            m_callHandlers.Add((typeof(TRequest), typeof(TResponse), handler.GetType()));
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
        if (Volatile.Read(ref m_disposed) != 0) ThrowDisposed();
        if (cancellationToken.IsCancellationRequested) return LBTask<TResponse>.FromCanceled(cancellationToken);

        var routeId = LayerCallRouteId<TRequest, TResponse>.Id;
        var invokers = Volatile.Read(ref m_callRouteInvokers);
        if ((uint)routeId >= (uint)invokers.Length || invokers[routeId] == null)
            ThrowRouteNotFound<TRequest, TResponse>();

        return ((LayerCallInvoker<TRequest, TResponse>)invokers[routeId]!)(request, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowDisposed()
    {
        throw new ObjectDisposedException(GetType().Name);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowRouteNotFound<TRequest, TResponse>()
        where TRequest : struct
        where TResponse : struct
    {
        throw new LayerCallRouteNotFoundException(GetType(), typeof(TRequest), typeof(TResponse));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState Send<T>(in T value) where T : struct
    {
        if (OwnerContext == null) throw new InvalidOperationException("Layer not attached to context.");
        return OwnerContext.EventCenter.Send(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T>(in T value) where T : struct
    {
        _ = TryPost(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPost<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        if (OwnerContext == null) return PostResult.Failure("Layer not attached to context.");
        return OwnerContext.TryPost(value, policy);
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

    private sealed class UnsubscribeFlowToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeFlowToken<T>> Pool = new();
        private EventCenter? _center;
        private EventHandleDelegate<T>? _handler;
        private int _disposed;
        private int _layerIndex;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _center?.UnsubscribeFlow(_layerIndex, _handler!);
            _center = null;
            _handler = null;
            Pool.Add(this);
        }

        public static UnsubscribeFlowToken<T> Rent(EventCenter c, int l, EventHandleDelegate<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeFlowToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            t._disposed = 0;
            return t;
        }
    }

    private sealed class UnsubscribeDelegateAsyncToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeDelegateAsyncToken<T>> Pool = new();
        private EventCenter? _center;
        private EventHandleDelegateAsync<T>? _handler;
        private int _disposed;
        private int _layerIndex;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _center?.UnsubscribeAsync(_layerIndex, _handler!);
            _center = null;
            _handler = null;
            Pool.Add(this);
        }

        public static UnsubscribeDelegateAsyncToken<T> Rent(EventCenter c, int l, EventHandleDelegateAsync<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeDelegateAsyncToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            t._disposed = 0;
            return t;
        }
    }

    private sealed class UnsubscribeNotifyToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeNotifyToken<T>> Pool = new();
        private EventCenter? _center;
        private EventNotifyDelegate<T>? _handler;
        private int _disposed;
        private int _layerIndex;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _center?.UnsubscribeNotify(_layerIndex, _handler!);
            _center = null;
            _handler = null;
            Pool.Add(this);
        }

        public static UnsubscribeNotifyToken<T> Rent(EventCenter c, int l, EventNotifyDelegate<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeNotifyToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            t._disposed = 0;
            return t;
        }
    }

    private sealed class UnsubscribeToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeToken<T>> Pool = new();
        private EventCenter? _center;
        private EventNotifyDelegate<T>? _handler;
        private int _disposed;
        private int _layerIndex;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _center?.Unsubscribe(_layerIndex, _handler!);
            _center = null;
            _handler = null;
            Pool.Add(this);
        }

        public static UnsubscribeToken<T> Rent(EventCenter c, int l, EventNotifyDelegate<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            t._disposed = 0;
            return t;
        }
    }

    private sealed class UnsubscribeParallelToken<T> : IDisposable where T : struct
    {
        private static readonly ConcurrentBag<UnsubscribeParallelToken<T>> Pool = new();
        private EventCenter? _center;
        private EventNotifyDelegate<T>? _handler;
        private int _disposed;
        private int _layerIndex;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _center?.UnsubscribeParallel(_layerIndex, _handler!);
            _center = null;
            _handler = null;
            Pool.Add(this);
        }

        public static UnsubscribeParallelToken<T> Rent(EventCenter c, int l, EventNotifyDelegate<T> h)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeParallelToken<T>();
            t._center = c;
            t._layerIndex = l;
            t._handler = h;
            t._disposed = 0;
            return t;
        }
    }
}
