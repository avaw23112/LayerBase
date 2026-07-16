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
    // 已注册的服务集合，用于按 OwnerScope + ServiceType 去重。
    private readonly HashSet<RegisteredServiceKey> _registeredServiceTypes = new();
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
        var manager = OwnerContext?.ScopeHost.MainScope.DelayManager;
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
        _producedEvents.Clear();
        _sharedFields.Clear();
        _subscribedEvents.Clear();
        _registeredServiceTypes.Clear();

        foreach (var registration in _manualServices)
        {
            _registeredServiceTypes.Add(new RegisteredServiceKey(registration.OwnerScopeId, registration.ServiceType));
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
        BindAutoLocalCalls();

        var subscribers = new List<IAutoSubscribe>();
        var boundSubscriberInstances = new HashSet<object>(ObjectReferenceComparer.Instance);
        if (this is IAutoSubscribe layerAutoSubscribe)
        {
            layerAutoSubscribe.AutoBind(this);
            subscribers.Add(layerAutoSubscribe);
            boundSubscriberInstances.Add(layerAutoSubscribe);
        }

        foreach (var resolved in _resolvedServices)
        {
            if (resolved.Instance is not IAutoSubscribe auto) continue;
            if (!boundSubscriberInstances.Add(auto)) continue;
            auto.AutoBind(this);
            subscribers.Add(auto);
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
        RegisterService(serviceType, service, ResolveServiceOwnerScopeId(service.GetType()));
    }

    public void RegisterService(Type serviceType, IService service, Type ownerScopeType)
    {
        if (ownerScopeType == null) throw new ArgumentNullException(nameof(ownerScopeType));
        RegisterService(serviceType, service, ResolveServiceOwnerScopeId(ownerScopeType));
    }

    private void RegisterService(Type serviceType, IService service, int ownerScopeId)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        if (_serviceProvider != null)
            throw new InvalidOperationException(
                "RegisterService must be called before the layer is built. Register services before LayerHub.CreateLayers().Push(...).Build().");

        if (!_registeredServiceTypes.Add(new RegisteredServiceKey(ownerScopeId, serviceType))) return;

        if (OwnerContext != null && OwnerContext.ScopeHost.TryGetRuntime(ownerScopeId, out var ownerScope))
            ServiceLayerBinder.AttachScopeObject(service, this, ownerScope);
        else if (OwnerContext != null)
            ServiceLayerBinder.Attach(service, this);

        var registration = new RegisteredService(
            serviceType,
            service,
            Interlocked.Increment(ref _nextServiceScopeId),
            ownerScopeId);
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

    internal T GetService<T>(int ownerScopeId) where T : class
    {
        return _serviceProvider?.Get<T>(ownerScopeId) ?? throw new InvalidOperationException("Layer 尚未构建。");
    }
    #endregion

    #region Public API - Event Send / Post
    /// <summary>同步发送事件到事件中心（立即派发）。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState Send<T>(in T value) where T : struct
    {
        if (OwnerContext == null) throw new InvalidOperationException("Layer 未附加到 Runtime 上下文。");
        return OwnerContext.ScopeHost.MainScope.EventCenter.Send(value);
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
        var scheduler = OwnerContext.ScopeHost.MainScope.PostScheduler;
        if (scheduler == null) return PostResult.Failure();
        return policy.HasValue
            ? scheduler.TryPost(value, policy.Value)
            : scheduler.TryPost(value);
    }
    #endregion

    #region Public API - Event Subscription
    /// <summary>订阅 Flow 类型的事件处理器（可中断事件流）。</summary>
    public void SubscribeFlow<T>(EventHandleDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ResolveSubscriptionEventCenter(handler.Target);
            center.SubscribeFlow(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<EventHandleDelegate<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeFlow(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeFlow(handler));
        }
    }

    public void SubscribeFlow<T>(IEventHandler<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ResolveSubscriptionEventCenter(handler);
            center.SubscribeFlow(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<IEventHandler<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeFlow(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeFlow(handler));
        }
    }

    internal void SubscribeFlow<T>(EventHandleDelegate<T> handler, ScopeRuntime ownerScope) where T : struct
    {
        ThrowIfDisposed();
        if (ownerScope == null) throw new ArgumentNullException(nameof(ownerScope));
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ownerScope.EventCenter;
            center.SubscribeFlow(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<EventHandleDelegate<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeFlow(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeFlow(handler, ownerScope));
        }
    }

    /// <summary>订阅 Notify 类型的事件通知。</summary>
    public void SubscribeNotify<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ResolveSubscriptionEventCenter(handler.Target);
            center.SubscribeNotify(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<EventNotifyDelegate<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeNotify(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeNotify(handler));
        }
    }

    internal void SubscribeFlow<T>(IEventHandler<T> handler, ScopeRuntime ownerScope) where T : struct
    {
        ThrowIfDisposed();
        if (ownerScope == null) throw new ArgumentNullException(nameof(ownerScope));
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ownerScope.EventCenter;
            center.SubscribeFlow(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<IEventHandler<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeFlow(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeFlow(handler, ownerScope));
        }
    }

    internal void SubscribeNotify<T>(EventNotifyDelegate<T> handler, ScopeRuntime ownerScope) where T : struct
    {
        ThrowIfDisposed();
        if (ownerScope == null) throw new ArgumentNullException(nameof(ownerScope));
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ownerScope.EventCenter;
            center.SubscribeNotify(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<EventNotifyDelegate<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeNotify(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeNotify(handler, ownerScope));
        }
    }

    /// <summary>订阅事件（同 SubscribeNotify）。</summary>
    public void Subscribe<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ResolveSubscriptionEventCenter(handler.Target);
            center.Subscribe(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<EventNotifyDelegate<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.Unsubscribe(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.Subscribe(handler));
        }
    }

    internal void Subscribe<T>(EventNotifyDelegate<T> handler, ScopeRuntime ownerScope) where T : struct
    {
        ThrowIfDisposed();
        if (ownerScope == null) throw new ArgumentNullException(nameof(ownerScope));
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ownerScope.EventCenter;
            center.Subscribe(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<EventNotifyDelegate<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.Unsubscribe(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.Subscribe(handler, ownerScope));
        }
    }

    /// <summary>订阅异步事件处理器。</summary>
    public void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ResolveSubscriptionEventCenter(handler.Target);
            center.SubscribeAsync(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<EventHandleDelegateAsync<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeAsync(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeAsync(handler));
        }
    }

    public void SubscribeAsync<T>(IEventHandlerAsync<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ResolveSubscriptionEventCenter(handler);
            center.SubscribeAsync(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<IEventHandlerAsync<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeAsync(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeAsync(handler));
        }
    }

    internal void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler, ScopeRuntime ownerScope) where T : struct
    {
        ThrowIfDisposed();
        if (ownerScope == null) throw new ArgumentNullException(nameof(ownerScope));
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ownerScope.EventCenter;
            center.SubscribeAsync(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<EventHandleDelegateAsync<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeAsync(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeAsync(handler, ownerScope));
        }
    }

    internal void SubscribeAsync<T>(IEventHandlerAsync<T> handler, ScopeRuntime ownerScope) where T : struct
    {
        ThrowIfDisposed();
        if (ownerScope == null) throw new ArgumentNullException(nameof(ownerScope));
        if (RouteIndex != -1 && OwnerContext != null)
        {
            var center = ownerScope.EventCenter;
            center.SubscribeAsync(RouteIndex, handler);
            _subscriptions.Add(SubscriptionToken<IEventHandlerAsync<T>, T>.Rent(
                center, RouteIndex, handler, static (c, i, h) => c.UnsubscribeAsync(i, h)));
        }
        else
        {
            _pendingOps.Enqueue(l => l.SubscribeAsync(handler, ownerScope));
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

        var manager = OwnerContext?.ScopeHost.MainScope.DelayManager;
        if (manager == null) throw new InvalidOperationException("DelayPublisherManager 未初始化。");

        var publisher = new DelayPublisher<T>(manager, this);
        int id = manager.RegisterPublisher(publisher);
        publisher.SetId(id);

        var actual = _delayPublishers.GetOrAdd(type, publisher);
        if (actual == publisher)
            OwnerContext?.MarkDelayDirty();
        else
            manager.UnregisterPublisher(id);

        return (IDelayPublisher<T>)actual;
    }
    #endregion

    #region Public API - Call Route
    /// <summary>注册一个调用路由处理器。同一请求-响应对只能注册一个处理器。</summary>
    protected internal void RegisterCallHandler<TRequest, TResponse>(IScopeLocalCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        RegisterCallHandler(handler, ResolveOwnerScopeType(handler?.GetType()));
    }

    protected internal void RegisterCallHandler<TRequest, TResponse, TOwnerScope>(
        IScopeLocalCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
        where TOwnerScope : IScopeDefinition
    {
        RegisterCallHandler(handler, typeof(TOwnerScope));
    }

    internal void RegisterCallHandler<TRequest, TResponse>(
        IScopeLocalCallHandler<TRequest, TResponse> handler,
        Type ownerScopeType)
        where TRequest : struct
        where TResponse : struct
    {
        ThrowIfDisposed();
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (ownerScopeType == null) throw new ArgumentNullException(nameof(ownerScopeType));

        int ownerScopeId = ScopeDefinitionIds.Resolve(ownerScopeType);
        if (OwnerContext == null)
            throw new InvalidOperationException("Layer not attached to a runtime context.");
        if (!OwnerContext.ScopeHost.TryGetRuntime(ownerScopeId, out var ownerScope))
            throw new InvalidOperationException(
                $"Scope `{ownerScopeType.FullName}` (id {ownerScopeId}) is not active in this runtime.");

        ServiceLayerBinder.AttachScopeObject(handler, this, ownerScope);
        var routeId = ScopeLocalCallRouteId<TRequest, TResponse>.Id;
        var invoker = (ScopeLocalCallInvoker<TRequest, TResponse>)handler.HandleAsync;

        ownerScope.LocalCalls.Register(new ScopeLocalCallRouteEntry(
            ownerScopeId,
            routeId,
            typeof(TRequest),
            typeof(TResponse),
            handler.GetType(),
            GetType(),
            invoker,
            new ScopeLocalCallDispatcher<TRequest, TResponse>(invoker)));
    }

    internal void RegisterCallHandlerForOwner<TRequest, TResponse>(
        object owner,
        IScopeLocalCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var binding = ServiceLayerBinder.GetBinding(owner);
        if (binding != null)
        {
            RegisterCallHandler(handler, binding.OwnerScope.Descriptor.ScopeType);
            return;
        }

        RegisterCallHandler(handler, ResolveOwnerScopeType(handler.GetType()));
    }

    public void RegisterScopeCallHandlerForOwner<TRequest, TResponse>(
        object owner,
        IScopeCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        ThrowIfDisposed();
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var binding = ServiceLayerBinder.GetBinding(owner);
        var ownerScopeType = binding?.OwnerScope.Descriptor.ScopeType ??
                             (owner is Layer ? typeof(MainScope) : ResolveOwnerScopeType(owner.GetType()));
        int ownerScopeId = ScopeDefinitionIds.Resolve(ownerScopeType);
        if (OwnerContext == null)
            throw new InvalidOperationException("Layer not attached to a runtime context.");
        if (!OwnerContext.ScopeHost.TryGetRuntime(ownerScopeId, out var ownerScope))
            throw new InvalidOperationException(
                $"Scope `{ownerScopeType.FullName}` (id {ownerScopeId}) is not active in this runtime.");

        ServiceLayerBinder.AttachScopeObject(handler, this, ownerScope);
        var routeId = ScopeRemoteCallRouteId<TRequest, TResponse>.Id;
        var invoker = (ScopeRemoteCallInvoker<TRequest, TResponse>)handler.HandleAsync;

        ownerScope.CallRoutes.Register(new ScopeCallRouteEntry(
            ownerScopeId,
            routeId,
            typeof(TRequest),
            typeof(TResponse),
            handler.GetType(),
            GetType(),
            invoker,
            new ScopeLocalCallDispatcher<TRequest, TResponse>(handler.HandleAsync)));
    }

    public void RegisterScopeEventHandlerForOwner<TEvent>(
        object owner,
        IScopeEventHandler<TEvent> handler)
        where TEvent : struct
    {
        ThrowIfDisposed();
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var binding = ServiceLayerBinder.GetBinding(owner);
        var ownerScopeType = binding?.OwnerScope.Descriptor.ScopeType ??
                             (owner is Layer ? typeof(MainScope) : ResolveOwnerScopeType(owner.GetType()));
        int ownerScopeId = ScopeDefinitionIds.Resolve(ownerScopeType);
        if (OwnerContext == null)
            throw new InvalidOperationException("Layer not attached to a runtime context.");
        if (!OwnerContext.ScopeHost.TryGetRuntime(ownerScopeId, out var ownerScope))
            throw new InvalidOperationException(
                $"Scope `{ownerScopeType.FullName}` (id {ownerScopeId}) is not active in this runtime.");

        ServiceLayerBinder.AttachScopeObject(handler, this, ownerScope);
        var routeId = ScopeRemoteEventRouteId<TEvent>.Id;
        var invoker = (ScopeRemoteEventInvoker<TEvent>)handler.Handle;

        ownerScope.EventRoutes.Register(new ScopeEventRouteEntry(
            ownerScopeId,
            routeId,
            typeof(TEvent),
            handler.GetType(),
            GetType(),
            invoker,
            new ScopeEventRouteInvoker<TEvent>(invoker)));
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
    private void BindAutoLocalCalls()
    {
        var boundInstances = new HashSet<object>(ObjectReferenceComparer.Instance);
        BindAutoLocalCall(this, boundInstances);
        foreach (var registration in _activeServices)
            BindAutoLocalCall(registration.Service, boundInstances);
        foreach (var resolved in _resolvedServices)
            BindAutoLocalCall(resolved.Instance, boundInstances);
    }

    private void BindAutoLocalCall(object candidate, HashSet<object> boundInstances)
    {
        if (!boundInstances.Add(candidate)) return;
        if (candidate is IAutoCallBinder autoCallBinder)
            autoCallBinder.AutoBindCalls(this);
        if (candidate is IAutoScopeEndpointBinder autoScopeEndpointBinder)
            autoScopeEndpointBinder.AutoBindScopeEndpoints(this);
    }

    private void AddActiveService(RegisteredService registration)
    {
        _activeServices.Add(registration);
        if (OwnerContext != null && OwnerContext.ScopeHost.TryGetRuntime(registration.OwnerScopeId, out var ownerScope))
            ServiceLayerBinder.AttachScopeObject(registration.Service, this, ownerScope);
        else
            ServiceLayerBinder.Attach(registration.Service, this);

        using var _ = _serviceCollection.PushRegistrationScope(registration.ScopeId, registration.OwnerScopeId);

        _serviceCollection.Add(new ServiceDescriptor(
            registration.ServiceType, null, ServiceLifetime.Scoped,
            _ => registration.Service, null, registration.ScopeId, registration.OwnerScopeId));

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

    private static Type ResolveOwnerScopeType(Type? handlerType)
    {
        if (handlerType == null)
            throw new ArgumentNullException(nameof(handlerType));

        foreach (var attribute in handlerType.GetCustomAttributes(false))
        {
            if (attribute is ScopeAttribute scopeAttribute)
                return scopeAttribute.ScopeType;
        }

        return typeof(MainScope);
    }

    private static int ResolveServiceOwnerScopeId(Type serviceType)
    {
        var ownerScopeType = typeof(IScopeDefinition).IsAssignableFrom(serviceType)
            ? serviceType
            : ResolveOwnerScopeType(serviceType);
        if (ownerScopeType == typeof(MainScope))
            return ScopeDefinitionIds.Main;

        try
        {
            return ScopeDefinitionIds.Resolve(ownerScopeType);
        }
        catch (InvalidOperationException)
        {
            return ScopeDefinitionIds.Main;
        }
    }

    private EventCenter ResolveSubscriptionEventCenter(object? handlerTarget)
    {
        if (handlerTarget != null)
        {
            var binding = ServiceLayerBinder.GetBinding(handlerTarget);
            if (binding != null)
                return binding.OwnerScope.EventCenter;
        }

        return OwnerContext?.ScopeHost.MainScope.EventCenter
               ?? throw new InvalidOperationException("Layer not attached to a runtime context.");
    }
    #endregion
    #region Nested Types
    internal readonly struct RegisteredService
    {
        public RegisteredService(Type serviceType, IService service, int scopeId, int ownerScopeId)
        {
            ServiceType = serviceType;
            Service = service;
            ScopeId = scopeId;
            OwnerScopeId = ownerScopeId;
        }

        /// <summary>服务注册时声明的类型（通常为接口）。</summary>
        public Type ServiceType { get; }

        /// <summary>服务实例。</summary>
        public IService Service { get; }

        /// <summary>服务在 Layer 内的作用域 ID。</summary>
        public int ScopeId { get; }

        /// <summary>服务所属运行 Scope 的 ID。</summary>
        public int OwnerScopeId { get; }
    }

    private readonly struct RegisteredServiceKey : IEquatable<RegisteredServiceKey>
    {
        private readonly int _ownerScopeId;
        private readonly Type _serviceType;

        public RegisteredServiceKey(int ownerScopeId, Type serviceType)
        {
            _ownerScopeId = ownerScopeId;
            _serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        }

        public bool Equals(RegisteredServiceKey other)
        {
            return _ownerScopeId == other._ownerScopeId &&
                   _serviceType == other._serviceType;
        }

        public override bool Equals(object? obj)
        {
            return obj is RegisteredServiceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_ownerScopeId, _serviceType);
        }
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

    private sealed class SubscriptionToken<THandler, TEvent> : IDisposable
        where THandler : class
        where TEvent : struct
    {
        private static readonly ConcurrentBag<SubscriptionToken<THandler, TEvent>> Pool = new();
        private EventCenter? _center;
        private int _layerIndex;
        private THandler? _handler;
        private Action<EventCenter, int, THandler>? _unsubscribe;
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            if (_center != null && _handler != null && _unsubscribe != null)
                _unsubscribe(_center, _layerIndex, _handler);

            _center = null;
            _handler = null;
            _unsubscribe = null;
            Pool.Add(this);
        }

        public static SubscriptionToken<THandler, TEvent> Rent(
            EventCenter center,
            int layerIndex,
            THandler handler,
            Action<EventCenter, int, THandler> unsubscribe)
        {
            if (!Pool.TryTake(out var t)) t = new SubscriptionToken<THandler, TEvent>();
            t._center = center;
            t._layerIndex = layerIndex;
            t._handler = handler;
            t._unsubscribe = unsubscribe;
            t._disposed = 0;
            return t;
        }
    }
    #endregion
}
