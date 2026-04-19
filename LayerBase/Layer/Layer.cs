using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.Delay;

namespace LayerBase.Layers;

public abstract class Layer : Node, ILayerContext
{
    private readonly ConcurrentDictionary<Type, IDelayPublisherUpdater> m_delayPublishers = new();
    private readonly ServiceCollection m_serviceCollection;
    private readonly List<IUpdate> m_serviceUpdates = new();

    // 挂起队列：用于在 Build 完成前暂存操作
    private List<Action<Layer>> m_pendingOps = new();
    private ServiceProvider? m_serviceProvider;

    protected Layer()
    {
        m_serviceCollection = new ServiceCollection();
        ServiceLayerBinder.Attach(this, this);
    }

    public int RouteIndex { get; private set; } = -1;

    public List<IAutoSubscribe> DiscoveredSubscribers { get; private set; } = new();

    public virtual bool HasActiveLogic =>
        m_serviceUpdates.Count > 0 || m_delayPublishers.Count > 0 || DiscoveredSubscribers.Count > 0;

    public void RegisterService(IService service)
    {
        service.ConfigureServices(m_serviceCollection);
    }

    public T GetService<T>() where T : class
    {
        if (m_serviceProvider == null) throw new InvalidOperationException("Layer not built.");
        return m_serviceProvider.Get<T>();
    }

    public void Build()
    {
        var descriptors = m_serviceCollection.ToDescriptors();
        var newProvider = new ServiceProvider(descriptors, this);
        var oldProvider = Interlocked.Exchange(ref m_serviceProvider, newProvider);
        oldProvider?.Dispose();

        // 1. 染色与自动订阅
        DiscoveredSubscribers = newProvider.InitializeAutoSubscriptions(this, descriptors);

        // 2. 核心：执行所有在 Build 期间挂起的手动操作 (Subscribe/Delay)
        var ops = Interlocked.Exchange(ref m_pendingOps, new List<Action<Layer>>());
        if (ops != null)
            foreach (var op in ops)
                op(this);

        // 3. 业务初始化
        foreach (var desc in descriptors)
        {
            var instance = newProvider.GetService(desc.ServiceType);
            if (instance is IInitializable init) init.Initialize();
        }

        // 4. 挂载更新链
        m_serviceUpdates.Clear();
        foreach (var desc in descriptors)
        {
            var instance = newProvider.GetService(desc.ServiceType);
            if (instance is IUpdate up) m_serviceUpdates.Add(up);
        }
    }

    internal void SetRouteIndex(int routeIndex)
    {
        RouteIndex = routeIndex;
        ServiceLayerBinder.Attach(this, this);
    }

    public virtual void Pump(float deltaTime)
    {
        if (RouteIndex == -1) return;
        LayerHub.LayerHub.EventCenter.PumpLayer(RouteIndex);

        foreach (var updater in m_delayPublishers.Values) updater.Update(deltaTime);
        for (var i = 0; i < m_serviceUpdates.Count; i++) m_serviceUpdates[i].Update();
    }

    public void Subscribe<T>(EventHandleDelegate<T> handler) where T : struct
    {
        if (RouteIndex != -1) LayerHub.LayerHub.EventCenter.Subscribe(RouteIndex, handler);
        else m_pendingOps.Add(l => l.Subscribe(handler));
    }

    public void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler) where T : struct
    {
        if (RouteIndex != -1) LayerHub.LayerHub.EventCenter.SubscribeAsync(RouteIndex, handler);
        else m_pendingOps.Add(l => l.SubscribeAsync(handler));
    }

    /// <summary>
    /// 获取针对特定事件的链式 API 流。
    /// </summary>
    public LayerEventStream<T> OnEvent<T>() where T : struct
    {
        return new LayerEventStream<T>(this);
    }

    public void SubscribeParallel<T>(EventHandleDelegate<T>                  handler,
                                     Action<int, string, string, Exception>? reportError = null) where T : struct
    {
        if (RouteIndex != -1)
            LayerHub.LayerHub.EventCenter.SubscribeParallel(RouteIndex, handler,
                reportError ?? LayerHub.LayerHub.ReportLayerEventError);
        else
            m_pendingOps.Add(l => l.SubscribeParallel(handler, reportError));
    }

    public IDelayPublisher<T> SubscribeDelay<T>() where T : struct
    {
        return (IDelayPublisher<T>)m_delayPublishers.GetOrAdd(typeof(T), _ => new DelayPublisher<T>(this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendLocal<T>(in T value) where T : struct
    {
        return LayerHub.LayerHub.EventCenter.SendLocal(RouteIndex, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendBubble<T>(in T value) where T : struct
    {
        return LayerHub.LayerHub.EventCenter.Send(value, RouteIndex, Propagation.Bubble);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendDrop<T>(in T value) where T : struct
    {
        return LayerHub.LayerHub.EventCenter.Send(value, RouteIndex, Propagation.Drop);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendGlobal<T>(in T value) where T : struct
    {
        return LayerHub.LayerHub.EventCenter.Send(value, RouteIndex, Propagation.Global);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostLocal<T>(in T value) where T : struct
    {
        LayerHub.LayerHub.EventCenter.PostLocal(RouteIndex, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostBubble<T>(in T value) where T : struct
    {
        LayerHub.LayerHub.EventCenter.Post(value, RouteIndex, Propagation.Bubble);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostDrop<T>(in T value) where T : struct
    {
        LayerHub.LayerHub.EventCenter.Post(value, RouteIndex, Propagation.Drop);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostGlobal<T>(in T value) where T : struct
    {
        LayerHub.LayerHub.EventCenter.Post(value, RouteIndex, Propagation.Global);
    }
}