using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.Delay;

namespace LayerBase.Layers;

public abstract class Layer : Node, ILayerContext, IDisposable
{
    private readonly ConcurrentDictionary<Type, IDelayPublisherUpdater> m_delayPublishers = new();
    private readonly ServiceCollection m_serviceCollection;
    private readonly List<IUpdate> m_serviceUpdates = new();
    private readonly List<IDisposable> m_subscriptions = new();
    private bool m_disposed;

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

    private void ThrowIfDisposed()
    {
        if (m_disposed) throw new ObjectDisposedException(nameof(Layer));
    }

    public void Subscribe<T>(EventHandleDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
        {
            LayerHub.LayerHub.EventCenter.Subscribe(RouteIndex, handler);
            lock (m_subscriptions) m_subscriptions.Add(new UnsubscribeDelegateToken<T>(LayerHub.LayerHub.EventCenter, RouteIndex, handler));
        }
        else m_pendingOps.Add(l => l.Subscribe(handler));
    }

    public void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
        {
            LayerHub.LayerHub.EventCenter.SubscribeAsync(RouteIndex, handler);
            lock (m_subscriptions) m_subscriptions.Add(new UnsubscribeDelegateAsyncToken<T>(LayerHub.LayerHub.EventCenter, RouteIndex, handler));
        }
        else m_pendingOps.Add(l => l.SubscribeAsync(handler));
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
        {
            LayerHub.LayerHub.EventCenter.SubscribeParallel(RouteIndex, handler,
                reportError ?? LayerHub.LayerHub.ReportLayerEventError);
            // Parallel unsub logic could be added here if needed, but parallel handlers are often global-lifetime.
            // For now, we skip parallel auto-unsub to keep it simple, or implement if required.
        }
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

    private sealed class UnsubscribeDelegateToken<T> : IDisposable where T : struct
    {
        private readonly GlobalEventCenter _center;
        private readonly int _layerIndex;
        private readonly EventHandleDelegate<T> _handler;
        public UnsubscribeDelegateToken(GlobalEventCenter c, int l, EventHandleDelegate<T> h) { _center = c; _layerIndex = l; _handler = h; }
        public void Dispose() => _center.Unsubscribe(_layerIndex, _handler);
    }

    private sealed class UnsubscribeDelegateAsyncToken<T> : IDisposable where T : struct
    {
        private readonly GlobalEventCenter _center;
        private readonly int _layerIndex;
        private readonly EventHandleDelegateAsync<T> _handler;
        public UnsubscribeDelegateAsyncToken(GlobalEventCenter c, int l, EventHandleDelegateAsync<T> h) { _center = c; _layerIndex = l; _handler = h; }
        public void Dispose() => _center.UnsubscribeAsync(_layerIndex, _handler);
    }
}