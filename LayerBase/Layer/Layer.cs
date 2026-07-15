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
using LayerBase.Scope;
using LayerBase.Snap;

namespace LayerBase.Layers;

/// <summary>
/// 逻辑分层（Layer）的基类。Layer 是事件路由、服务管理和逻辑执行的基本单元。
/// 每个 Layer 拥有自己的服务容器、事件订阅、延迟发布器和调用路由表。
/// </summary>
public abstract class Layer : Node, IDisposable
{
    #region External Dependencies
    // 服务集合，用于在 ConfigureServices 中累积服务描述符，最终生成 ServiceProvider。
    private readonly ServiceCollection _serviceCollection;
    #endregion

    #region Runtime State - Service Management
    // 已注册的服务类型集合，用于去重。
    private readonly HashSet<Type> _registeredServiceTypes = new();
    // 手动注册的服务列表（用户通过 RegisterService 注册）。
    private readonly List<RegisteredService> _manualServices = new();
    // 构建过程中收集到的活跃服务。
    private readonly List<RegisteredService> _activeServices = new();
    // 已解析的服务列表（由 ServiceProvider 创建）。
    private List<ServiceProvider.ResolvedService> _resolvedServices = new();
    // Layer 级服务提供者。
    private ServiceProvider? _serviceProvider;
    // 是否正在收集生成器自动挂载的服务。
    private bool _collectingGeneratedServices;
    // 下一个服务作用域 ID 计数器。
    private int _nextServiceScopeId;
    #endregion

    #region Runtime State - Call Route
    // 调用路由的锁，保护并发注册。
    private readonly object _callRouteLock = new();
    // 已注册的调用处理器的元数据列表。
    private readonly List<(Type Req, Type Resp, Type Handler)> _callHandlers = new();
    private readonly List<ScopeLocalCallRouteEntry> _localCallRouteEntries = new();
    // 按路由 ID 索引的调用路由处理器类型。
    private Type?[] _callRouteHandlerTypes = Array.Empty<Type?>();
    // 按路由 ID 索引的调用路由执行委托。
    private object?[] _callRouteInvokers = Array.Empty<object?>();
    #endregion

    #region Runtime State - Event & Subscribe
    // 事件订阅的 Dispose Token 列表，用于在 Layer 释放时自动取消订阅。
    private readonly List<IDisposable> _subscriptions = new();
    // Layer 订阅的事件类型列表。
    private readonly List<Type> _subscribedEvents = new();
    // Layer 生产的事件类型列表。
    private readonly List<Type> _producedEvents = new();
    // 当 Layer 尚未分配 RouteIndex 时，暂存订阅操作的队列。
    private ConcurrentQueue<Action<Layer>> _pendingOps = new();
    #endregion

    #region Runtime State - Lifecycle
    // IInitializable 服务列表，Scope Activate 时按 Layer 顺序调用。
    private readonly List<IInitializable> _initializables = new();
    // IUpdate 服务列表，每帧调用。
    private readonly List<IUpdate> _serviceUpdates = new();
    // IPostBuild 服务列表，构建完成后调用。
    private readonly List<IPostBuild> _postBuilds = new();
    // IRuntimeStart 服务列表，启动时调用。
    private readonly List<IRuntimeStart> _runtimeStarts = new();
    // IRuntimeStop 服务列表，停止时调用。
    private readonly List<IRuntimeStop> _runtimeStops = new();
    // IFixedUpdate 服务列表，固定步长调用。
    private readonly List<IFixedUpdate> _fixedUpdates = new();
    #endregion

    #region Runtime State - Delay
    // 延迟发布器缓存。键为事件类型，值为对应的发布器实例。
    private readonly ConcurrentDictionary<Type, IDelayPublisherInternal> _delayPublishers = new();
    #endregion

    #region Runtime State - Shared Field Metadata
    // 共享字段声明列表，用于 Layer/Scope/Service 本地数据绑定。
    private readonly List<(Type ProviderServiceType, string Key, Type FieldType, bool IsProvider)> _sharedFields = new();
    #endregion

    #region Runtime State - Disposal
    // 0=未释放，1=已释放。使用 int 配合 Interlocked 保证线程安全。
    private int _disposed;
    #endregion

    #region Properties
    /// <summary>当前 Layer 所属的 Runtime 上下文。</summary>
    public LayerRuntime? OwnerContext { get; private set; }

    /// <summary>当前 Layer 在责任链中的路由索引。</summary>
    public int RouteIndex { get; private set; } = -1;

    /// <summary>通过自动绑定发现的事件订阅者列表。</summary>
    public IReadOnlyList<IAutoSubscribe> DiscoveredSubscribers { get; private set; } = Array.Empty<IAutoSubscribe>();

    /// <summary>已注册的所有调用处理器。</summary>
    public IReadOnlyList<(Type Req, Type Resp, Type Handler)> CallHandlers => _callHandlers;

    internal IReadOnlyList<ScopeLocalCallRouteEntry> LocalCallRouteEntries => _localCallRouteEntries;

    /// <summary>该 Layer 生产（发送/发布）的事件类型列表。</summary>
    public IReadOnlyList<Type> ProducedEvents => _producedEvents;

    /// <summary>该 Layer 声明的共享字段列表。</summary>
    public IReadOnlyList<(Type ProviderServiceType, string Key, Type FieldType, bool IsProvider)> SharedFields => _sharedFields;

    /// <summary>该 Layer 订阅的事件类型列表。</summary>
    public IReadOnlyList<Type> SubscribedEvents => _subscribedEvents;

    /// <summary>当前 Layer 是否包含活跃逻辑。</summary>
    public virtual bool HasActiveLogic =>
        _serviceUpdates.Count > 0 || _delayPublishers.Count > 0 || _fixedUpdates.Count > 0;

    /// <summary>当前 Layer 是否有活跃的延迟发布器。</summary>
    internal bool HasDelayPublisher
    {
        get
        {
            if (_delayPublishers.IsEmpty) return false;
            foreach (var kvp in _delayPublishers)
                if (kvp.Value.HasActiveDelays) return true;
            return false;
        }
    }
    #endregion

    #region Constructors
    protected Layer()
    {
        _serviceCollection = new ServiceCollection();
    }
    #endregion

    #region Lifecycle - Attach / Detach
    internal void AttachToContext(LayerRuntime context)
    {
        OwnerContext = context;
        ServiceLayerBinder.Attach(this, this);
    }

    internal void DetachFromContext()
    {
        ServiceLayerBinder.Detach(this);
        OwnerContext = null;
        RouteIndex = -1;
    }
    #endregion

    #region Lifecycle - Dispose
    /// <summary>释放 Layer 占用的所有资源。</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        lock (_subscriptions)
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
        }

        while (_pendingOps.TryDequeue(out _)) { }

        DetachResolvedObjects();
        ReleaseDelayPublishers();
        _serviceUpdates.Clear();
        _activeServices.Clear();
        _resolvedServices.Clear();
        _serviceProvider?.Dispose();
        _serviceProvider = null;
        DetachFromContext();
    }

    private void DetachResolvedObjects()
    {
        ServiceLayerBinder.Detach(this);
        foreach (var registration in _activeServices)
            ServiceLayerBinder.Detach(registration.Service);
        foreach (var registration in _manualServices)
            ServiceLayerBinder.Detach(registration.Service);
        foreach (var resolved in _resolvedServices)
            ServiceLayerBinder.Detach(resolved.Instance);
    }

    private void ReleaseDelayPublishers()
    {
        if (_delayPublishers.IsEmpty) return;
        var manager = OwnerContext?.DelayManager;
        foreach (var publisher in _delayPublishers.Values)
        {
            if (manager != null && publisher.PublisherId >= 0)
                manager.UnregisterPublisher(publisher.PublisherId);
            publisher.Deactivate();
        }
        _delayPublishers.Clear();
        OwnerContext?.MarkDelayDirty();
    }
    #endregion

    #region Lifecycle - Build
    /// <summary>配置 Layer 专属的服务容器。子类可重写此方法注册所需服务。</summary>
    public virtual void ConfigureServices(IServiceCollection services) { }

    /// <summary>初始化 Layer 的服务。触发生成器实现的自动挂载逻辑。</summary>
    internal void InitializeServices()
    {
        if (this is IAutoLayerMount layerMount)
            layerMount.__AutoMountServices(this);
    }

    /// <summary>准备构建过程：清理旧状态、重新初始化服务容器。</summary>
    internal void PrepareBuild()
    {
        DetachResolvedObjects();

        lock (_subscriptions)
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
        }

        ReleaseDelayPublishers();
        _initializables.Clear();
        _serviceUpdates.Clear();
        _postBuilds.Clear();
        _runtimeStarts.Clear();
        _runtimeStops.Clear();
        _fixedUpdates.Clear();
        _activeServices.Clear();
        _resolvedServices.Clear();
        _serviceCollection.Reset();
        _callRouteInvokers = Array.Empty<object?>();
        _callRouteHandlerTypes = Array.Empty<Type?>();
        _callHandlers.Clear();
        _localCallRouteEntries.Clear();
        _producedEvents.Clear();
        _sharedFields.Clear();
        _subscribedEvents.Clear();
        _registeredServiceTypes.Clear();

        foreach (var registration in _manualServices)
        {
            _registeredServiceTypes.Add(registration.ServiceType);
            AddActiveService(registration);
        }

        _collectingGeneratedServices = true;
        InitializeServices();
        ConfigureServices(_serviceCollection);
        _collectingGeneratedServices = false;

        var descriptors = _serviceCollection.ToDescriptors();
        var newProvider = new ServiceProvider(OwnerContext!, descriptors, this);
        var oldProvider = Interlocked.Exchange(ref _serviceProvider, newProvider);
        oldProvider?.Dispose();

        newProvider.InjectMembers(this);
        foreach (var registration in _activeServices)
            newProvider.InjectMembers(registration.Service);

        _resolvedServices = newProvider.ResolveOrderedServices(descriptors);
    }

    /// <summary>构建自动绑定：调用路由、事件订阅和延迟发布器。</summary>
    internal void BuildAutoBinding()
    {
        BindAutoCallHandlers();

        var subscribers = new List<IAutoSubscribe>();
        if (this is IAutoSubscribe layerAutoSubscribe)
        {
            layerAutoSubscribe.AutoBind(this);
            subscribers.Add(layerAutoSubscribe);
        }

        foreach (var resolved in _resolvedServices)
        {
            if (resolved.Instance is not IAutoSubscribe auto) continue;
            auto.AutoBind(this);
            subscribers.Add(auto);
        }

        foreach (var resolved in _resolvedServices)
        {
            if (resolved.Instance is IAutoSubscribe) continue;
            BindInterfaceEventHandlers(resolved.Instance);
        }

        DiscoveredSubscribers = subscribers;
        var ops = Interlocked.Exchange(ref _pendingOps, new ConcurrentQueue<Action<Layer>>());
        if (ops != null)
            foreach (var op in ops)
                op(this);
    }

    /// <summary>构建生命周期：收集 IInitializable/IUpdate/IFixedUpdate 等接口实现并分类存储。</summary>
    internal void LifecycleBuild()
    {
        foreach (var resolved in _resolvedServices)
        {
            if (resolved.Instance is IInitializable init) _initializables.Add(init);
            if (resolved.Instance is IUpdate up) _serviceUpdates.Add(up);
            if (resolved.Instance is IFixedUpdate fixedUpdate) _fixedUpdates.Add(fixedUpdate);
            if (resolved.Instance is IPostBuild postBuild) _postBuilds.Add(postBuild);
            if (resolved.Instance is IRuntimeStart runtimeStart) _runtimeStarts.Add(runtimeStart);
            if (resolved.Instance is IRuntimeStop runtimeStop) _runtimeStops.Add(runtimeStop);
        }

        if (this is IFixedUpdate layerFixedUpdate) _fixedUpdates.Add(layerFixedUpdate);
        if (this is IInitializable layerInitializable) _initializables.Add(layerInitializable);
        if (this is IPostBuild layerPostBuild) _postBuilds.Add(layerPostBuild);
        if (this is IRuntimeStart layerRuntimeStart) _runtimeStarts.Add(layerRuntimeStart);
        if (this is IRuntimeStop layerRuntimeStop) _runtimeStops.Add(layerRuntimeStop);
    }

    internal void RunInitialize()
    {
        for (var i = 0; i < _initializables.Count; i++)
            _initializables[i].Initialize();
    }

    internal void RunPostBuild()
    {
        for (var i = 0; i < _postBuilds.Count; i++)
            _postBuilds[i].PostBuild();
    }

    internal void RunRuntimeStart()
    {
        for (var i = 0; i < _runtimeStarts.Count; i++)
            _runtimeStarts[i].RuntimeStart();
    }

    internal void RunRuntimeStop()
    {
        for (var i = 0; i < _runtimeStops.Count; i++)
            _runtimeStops[i].RuntimeStop();
    }

    internal ScopeLayerLifecycleSlice AppendScopeLifecycle(
        List<LifecycleInvoker> initialize,
        List<LifecycleInvoker> postBuild,
        List<LifecycleInvoker> runtimeStart,
        List<UpdateInvoker> update,
        List<FixedUpdateInvoker> fixedUpdate,
        List<LifecycleInvoker> runtimeStop,
        List<LifecycleInvoker> dispose)
    {
        var initializeStart = initialize.Count;
        var postBuildStart = postBuild.Count;
        var runtimeStartStart = runtimeStart.Count;
        var updateStart = update.Count;
        var fixedUpdateStart = fixedUpdate.Count;
        var runtimeStopStart = runtimeStop.Count;
        var disposeStart = dispose.Count;

        if (_initializables.Count > 0)
            initialize.Add(RunInitialize);
        if (_postBuilds.Count > 0)
            postBuild.Add(RunPostBuild);
        if (_runtimeStarts.Count > 0)
            runtimeStart.Add(RunRuntimeStart);
        if (HasActiveLogic)
            update.Add(Pump);
        if (_fixedUpdates.Count > 0)
            fixedUpdate.Add(PumpFixed);
        if (_runtimeStops.Count > 0)
            runtimeStop.Add(RunRuntimeStop);
        dispose.Add(Dispose);

        return new ScopeLayerLifecycleSlice(
            RouteIndex,
            initializeStart,
            initialize.Count - initializeStart,
            postBuildStart,
            postBuild.Count - postBuildStart,
            runtimeStartStart,
            runtimeStart.Count - runtimeStartStart,
            updateStart,
            update.Count - updateStart,
            fixedUpdateStart,
            fixedUpdate.Count - fixedUpdateStart,
            runtimeStopStart,
            runtimeStop.Count - runtimeStopStart,
            disposeStart,
            dispose.Count - disposeStart);
    }
    #endregion

    #region Lifecycle - Pump
    /// <summary>每帧推进服务更新。子类可重写此方法添加自定义更新逻辑。</summary>
    public virtual void Pump(float deltaTime)
    {
        for (var i = 0; i < _serviceUpdates.Count; i++)
            _serviceUpdates[i].Update();
    }

    /// <summary>以固定时间步长推进固定更新。</summary>
    internal void PumpFixed(float fixedDeltaTime)
    {
        for (var i = 0; i < _fixedUpdates.Count; i++)
            _fixedUpdates[i].FixedUpdate(fixedDeltaTime);
    }

    internal void SetRouteIndex(int routeIndex)
    {
        RouteIndex = routeIndex;
        ServiceLayerBinder.Attach(this, this);
    }
    #endregion

    #region Public API - Service Management
    /// <summary>手动注册一个服务到当前 Layer，自动推断服务类型。</summary>
    public void RegisterService(IService service)
    {
        RegisterService(service.GetType(), service);
    }

    /// <summary>手动注册一个服务到当前 Layer，并指定其暴露的服务类型。必须在 Build 之前调用。</summary>
    public void RegisterService(Type serviceType, IService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        if (_serviceProvider != null)
            throw new InvalidOperationException(
                "RegisterService must be called before the layer is built. Register services before LayerHub.CreateLayers().Push(...).Build().");

        if (!_registeredServiceTypes.Add(serviceType)) return;

        if (OwnerContext != null)
            ServiceLayerBinder.Attach(service, this);

        var registration = new RegisteredService(serviceType, service, Interlocked.Increment(ref _nextServiceScopeId));
        if (_collectingGeneratedServices)
        {
            AddActiveService(registration);
            return;
        }
        _manualServices.Add(registration);
    }

    /// <summary>从当前 Layer 的服务容器解析指定类型的服务实例。</summary>
    public T GetService<T>() where T : class
    {
        return _serviceProvider?.Get<T>() ?? throw new InvalidOperationException("Layer 尚未构建。");
    }
    #endregion

    #region Public API - Event Send / Post
    /// <summary>同步发送事件到事件中心（立即派发）。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState Send<T>(in T value) where T : struct
    {
        if (OwnerContext == null) throw new InvalidOperationException("Layer 未附加到 Runtime 上下文。");
        return OwnerContext.EventCenter.Send(value);
    }

    /// <summary>投递事件到调度队列（异步派发）。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T>(in T value) where T : struct
    {
        _ = TryPost(value);
    }

    /// <summary>尝试投递事件到调度队列，返回投递结果。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPost<T>(in T value, EventPostPolicy? policy = default) where T : struct
    {
        if (OwnerContext == null) return PostResult.Failure();
        return OwnerContext.TryPost(value, policy);
    }
    #endregion

    #region Public API - Event Subscription
    /// <summary>订阅 Flow 类型的事件处理器（可中断事件流）。</summary>
    public void SubscribeFlow<T>(EventHandleDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.SubscribeFlow(RouteIndex, handler);
            _subscriptions.Add(UnsubscribeToken.Rent(OwnerContext.EventCenter, RouteIndex, handler, typeof(T), UnsubscribeKind.Flow));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeFlow(handler));
        }
    }

    /// <summary>订阅 Notify 类型的事件通知。</summary>
    public void SubscribeNotify<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.SubscribeNotify(RouteIndex, handler);
            _subscriptions.Add(UnsubscribeToken.Rent(OwnerContext.EventCenter, RouteIndex, handler, typeof(T), UnsubscribeKind.Notify));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeNotify(handler));
        }
    }

    /// <summary>订阅事件（同 SubscribeNotify）。</summary>
    public void Subscribe<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.Subscribe(RouteIndex, handler);
            _subscriptions.Add(UnsubscribeToken.Rent(OwnerContext.EventCenter, RouteIndex, handler, typeof(T), UnsubscribeKind.Subscribe));
        }
        else
        {
            _pendingOps.Enqueue(l => l.Subscribe(handler));
        }
    }

    /// <summary>订阅异步事件处理器。</summary>
    public void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            OwnerContext.EventCenter.SubscribeAsync(RouteIndex, handler);
            _subscriptions.Add(UnsubscribeToken.Rent(OwnerContext.EventCenter, RouteIndex, handler, typeof(T), UnsubscribeKind.Async));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeAsync(handler));
        }
    }

    /// <summary>创建流畅的事件流查询对象。</summary>
    public LayerEventStream<T> OnEvent<T>() where T : struct
    {
        return new LayerEventStream<T>(this);
    }

    /// <summary>订阅延迟事件发布器。同一事件类型只会创建一个发布器实例。</summary>
    public IDelayPublisher<T> SubscribeDelay<T>() where T : struct
    {
        var type = typeof(T);
        if (_delayPublishers.TryGetValue(type, out var existing)) return (IDelayPublisher<T>)existing;

        var manager = OwnerContext?.DelayManager;
        if (manager == null) throw new InvalidOperationException("DelayPublisherManager 未初始化。");

        var publisher = new DelayPublisher<T>(manager, this);
        int id = manager.RegisterPublisher(publisher);
        publisher.SetId(id);

        var actual = _delayPublishers.GetOrAdd(type, publisher);
        if (actual == publisher)
            OwnerContext?.MarkDelayDirty();

        return (IDelayPublisher<T>)actual;
    }
    #endregion

    #region Public API - Call Route
    /// <summary>注册一个调用路由处理器。同一请求-响应对只能注册一个处理器。</summary>
    protected internal void RegisterCallHandler<TRequest, TResponse>(IScopeLocalCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        ThrowIfDisposed();
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        ServiceLayerBinder.Attach(handler, this);
        var routeId = ScopeLocalCallRouteId<TRequest, TResponse>.Id;
        var invoker = (ScopeLocalCallInvoker<TRequest, TResponse>)handler.HandleAsync;

        lock (_callRouteLock)
        {
            var invokers = _callRouteInvokers;
            var handlerTypes = _callRouteHandlerTypes;

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
                throw new ScopeLocalCallRouteConflictException(
                    0,
                    typeof(TRequest),
                    typeof(TResponse),
                    GetType(),
                    handlerTypes[routeId] ?? invokers[routeId]!.GetType(),
                    GetType(),
                    handler.GetType());
            }

            invokers[routeId] = invoker;
            handlerTypes[routeId] = handler.GetType();
            _callHandlers.Add((typeof(TRequest), typeof(TResponse), handler.GetType()));
            _localCallRouteEntries.Add(new ScopeLocalCallRouteEntry(
                routeId,
                typeof(TRequest),
                typeof(TResponse),
                handler.GetType(),
                GetType(),
                invoker,
                new ScopeLocalCallDispatcher<TRequest, TResponse>(invoker)));
            Volatile.Write(ref _callRouteInvokers, invokers);
            Volatile.Write(ref _callRouteHandlerTypes, handlerTypes);
        }
    }
    #endregion

    #region Public API - Metadata Recording
    /// <summary>记录当前 Layer 订阅的事件类型。</summary>
    public void RecordSubscribedEvent(Type eventType)
    {
        if (eventType == null) throw new ArgumentNullException(nameof(eventType));
        _subscribedEvents.Add(eventType);
    }

    /// <summary>记录当前 Layer 生产的事件类型。</summary>
    public void RecordProducedEvent(Type eventType)
    {
        if (eventType == null) throw new ArgumentNullException(nameof(eventType));
        _producedEvents.Add(eventType);
    }

    internal void RecordSharedField(Type providerServiceType, string key, Type fieldType, bool isProvider)
    {
        _sharedFields.Add((providerServiceType, key, fieldType, isProvider));
    }
    #endregion

    #region Internal - Call Route Resolution
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ScopeLocalCallInvoker<TRequest, TResponse> GetCallInvoker<TRequest, TResponse>()
        where TRequest : struct
        where TResponse : struct
    {
        var routeId = ScopeLocalCallRouteId<TRequest, TResponse>.Id;
        var invokers = Volatile.Read(ref _callRouteInvokers);
        if ((uint)routeId >= (uint)invokers.Length || invokers[routeId] == null)
            ThrowRouteNotFound<TRequest, TResponse>();
        return (ScopeLocalCallInvoker<TRequest, TResponse>)invokers[routeId]!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LBTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request,
                                                               CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (Volatile.Read(ref _disposed) != 0) ThrowDisposed();
        if (cancellationToken.IsCancellationRequested) return LBTask<TResponse>.FromCanceled(cancellationToken);

        var routeId = ScopeLocalCallRouteId<TRequest, TResponse>.Id;
        var invokers = Volatile.Read(ref _callRouteInvokers);
        if ((uint)routeId >= (uint)invokers.Length || invokers[routeId] == null)
            ThrowRouteNotFound<TRequest, TResponse>();
        return ((ScopeLocalCallInvoker<TRequest, TResponse>)invokers[routeId]!)(request, cancellationToken);
    }
    #endregion

    #region Internal - Shared Field & Snap
    internal IEnumerable<SharedFieldBinder.Participant> GetSharedFieldParticipants()
    {
        var emitted = new HashSet<object>(ObjectReferenceComparer.Instance);
        foreach (var service in _activeServices)
        {
            if (emitted.Add(service.Service))
                yield return new SharedFieldBinder.Participant(
                    service.Service,
                    this,
                    MainScope.ScopeId,
                    service.ScopeId,
                    service.ServiceType);
        }

        foreach (var resolved in _resolvedServices)
        {
            if (!emitted.Add(resolved.Instance))
                continue;

            yield return new SharedFieldBinder.Participant(
                resolved.Instance,
                this,
                MainScope.ScopeId,
                resolved.Descriptor.RegistrationScopeId,
                ResolveProviderServiceType(resolved.Descriptor));
        }
    }

    private Type ResolveProviderServiceType(ServiceDescriptor descriptor)
    {
        foreach (var service in _activeServices)
            if (service.ScopeId == descriptor.RegistrationScopeId)
                return service.ServiceType;

        return descriptor.ServiceType;
    }

    internal IEnumerable<IGeneratedFullSnapNode> GetFullSnapNodes()
    {
        var visited = new HashSet<object>(ObjectReferenceComparer.Instance);
        foreach (var registration in _activeServices)
        {
            if (registration.Service is IGeneratedFullSnapNode node && visited.Add(node))
                yield return node;
        }
        foreach (var resolved in _resolvedServices)
        {
            if (resolved.Instance is IGeneratedFullSnapNode node && visited.Add(node))
                yield return node;
        }
    }
    #endregion

    #region Internal - Auto Binding
    private void BindAutoCallHandlers()
    {
        var boundInstances = new HashSet<object>(ObjectReferenceComparer.Instance);
        BindAutoCallHandler(this, boundInstances);
        foreach (var registration in _activeServices)
            BindAutoCallHandler(registration.Service, boundInstances);
    }

    private void BindAutoCallHandler(object candidate, HashSet<object> boundInstances)
    {
        if (!boundInstances.Add(candidate)) return;
        if (candidate is IAutoCallBinder autoCallBinder)
            autoCallBinder.AutoBindCalls(this);
    }

    private void BindInterfaceEventHandlers(object instance)
    {
        foreach (var iface in instance.GetType().GetInterfaces())
        {
            if (!iface.IsGenericType) continue;
            var genericDefinition = iface.GetGenericTypeDefinition();
            var typeArguments = iface.GetGenericArguments();
            if (typeArguments.Length != 1 || !typeArguments[0].IsValueType) continue;

            if (genericDefinition == typeof(IEventHandler<>))
            {
                OwnerContext.EventCenter.SubscribeFlow(RouteIndex, instance, typeArguments[0]);
                _subscriptions.Add(UnsubscribeToken.Rent(OwnerContext.EventCenter, RouteIndex, instance, typeArguments[0], UnsubscribeKind.Flow));
                RecordSubscribedEvent(typeArguments[0]);
                continue;
            }
            if (genericDefinition == typeof(IEventHandlerAsync<>))
            {
                OwnerContext.EventCenter.SubscribeAsync(RouteIndex, instance, typeArguments[0]);
                _subscriptions.Add(UnsubscribeToken.Rent(OwnerContext.EventCenter, RouteIndex, instance, typeArguments[0], UnsubscribeKind.Async));
                RecordSubscribedEvent(typeArguments[0]);
            }
        }
    }

    private void AddActiveService(RegisteredService registration)
    {
        _activeServices.Add(registration);
        ServiceLayerBinder.Attach(registration.Service, this);

        _serviceCollection.Add(new ServiceDescriptor(
            registration.ServiceType, null, ServiceLifetime.Scoped,
            _ => registration.Service, null, registration.ScopeId));

        using var _ = _serviceCollection.PushRegistrationScope(registration.ScopeId);

        if (registration.Service is IAutoServiceMount autoMount)
            autoMount.__AutoMountContexts(_serviceCollection);

        registration.Service.ConfigureServices(_serviceCollection);
    }
    #endregion

    #region Validation & Guards
    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(Layer));
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
        throw new ScopeLocalCallRouteNotFoundException(0, typeof(TRequest), typeof(TResponse));
    }
    #endregion
    #region Nested Types
    internal readonly struct RegisteredService
    {
        public RegisteredService(Type serviceType, IService service, int scopeId)
        {
            ServiceType = serviceType;
            Service = service;
            ScopeId = scopeId;
        }

        /// <summary>服务注册时声明的类型（通常为接口）。</summary>
        public Type ServiceType { get; }

        /// <summary>服务实例。</summary>
        public IService Service { get; }

        /// <summary>服务在 Layer 内的作用域 ID。</summary>
        public int ScopeId { get; }
    }

    /// <summary>
    /// 基于引用相等性的对象比较器，用于 HashSet 等场景。
    /// </summary>
    private sealed class ObjectReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ObjectReferenceComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private enum UnsubscribeKind { Flow, Async, Notify, Subscribe }

    /// <summary>
    /// 非泛型 UnsubscribeToken，避免 IL2CPP 环境下的 MakeGenericMethod 问题。
    /// Dispose 时根据 Kind 调用 EventCenter 对应的非泛型 Unsubscribe 方法。
    /// </summary>
    private sealed class UnsubscribeToken : IDisposable
    {
        private static readonly ConcurrentBag<UnsubscribeToken> Pool = new();
        private EventCenter? _center;
        private int _layerIndex;
        private object? _handler;
        private Type? _eventType;
        private UnsubscribeKind _kind;
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            switch (_kind)
            {
                case UnsubscribeKind.Flow:
                    _center?.UnsubscribeFlow(_layerIndex, _handler!, _eventType!);
                    break;
                case UnsubscribeKind.Async:
                    _center?.UnsubscribeAsync(_layerIndex, _handler!, _eventType!);
                    break;
                case UnsubscribeKind.Notify:
                    _center?.UnsubscribeNotify(_layerIndex, _handler!, _eventType!);
                    break;
                case UnsubscribeKind.Subscribe:
                    _center?.Unsubscribe(_layerIndex, _handler!, _eventType!);
                    break;
            }
            _center = null;
            _handler = null;
            _eventType = null;
            Pool.Add(this);
        }

        public static UnsubscribeToken Rent(EventCenter c, int l, object handler, Type eventType, UnsubscribeKind kind)
        {
            if (!Pool.TryTake(out var t)) t = new UnsubscribeToken();
            t._center = c;
            t._layerIndex = l;
            t._handler = handler;
            t._eventType = eventType;
            t._kind = kind;
            t._disposed = 0;
            return t;
        }
    }
    #endregion
}
