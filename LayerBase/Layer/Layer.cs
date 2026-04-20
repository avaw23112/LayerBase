using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.Delay;

namespace LayerBase.Layers;

[AttributeUsage(AttributeTargets.Class)]
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
    private readonly ConcurrentDictionary<Type, IDelayPublisherUpdater> m_delayPublishers = new();
    private readonly List<IDelayPublisherUpdater> m_delayUpdaters = new(); // 优化：消除 Values 迭代分配
    private readonly ServiceCollection m_serviceCollection;
    private readonly List<IUpdate> m_serviceUpdates = new();
    private readonly List<IDisposable> m_subscriptions = new();
    private GlobalEventCenter _center;
    private bool m_disposed;

    private List<Action<Layer>> m_pendingOps = new();
    private ServiceProvider? m_serviceProvider;

    protected Layer()
    {
        _center = LayerHub.EventCenter;
        m_serviceCollection = new ServiceCollection();
        ServiceLayerBinder.Attach(this, this);
    }

    public int RouteIndex { get; private set; } = -1;
    public List<IAutoSubscribe> DiscoveredSubscribers { get; private set; } = new();

    // 优化：不再包含 DiscoveredSubscribers。仅有订阅者的层被称为“被动层”，不应参与逻辑位图轮询。
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

    public void RegisterService(IService service)
    {
        service.ConfigureServices(m_serviceCollection);
    }

    public T GetService<T>() where T : class
    {
        return m_serviceProvider?.Get<T>() ?? throw new InvalidOperationException("Layer not built.");
    }

    public void Build()
    {
        var descriptors = m_serviceCollection.ToDescriptors();
        var newProvider = new ServiceProvider(descriptors, this);
        var oldProvider = Interlocked.Exchange(ref m_serviceProvider, newProvider);
        oldProvider?.Dispose();
        DiscoveredSubscribers = newProvider.InitializeAutoSubscriptions(this, descriptors);
        var ops = Interlocked.Exchange(ref m_pendingOps, new List<Action<Layer>>());
        if (ops != null)
            foreach (var op in ops)
                op(this);
        foreach (var desc in descriptors)
        {
            var instance = newProvider.GetService(desc.ServiceType);
            if (instance is IInitializable init) init.Initialize();
        }

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
            lock (m_subscriptions)
            {
                m_subscriptions.Add(UnsubscribeDelegateToken<T>.Rent(LayerHub.EventCenter, RouteIndex, handler));
            }
        }
        else
        {
            m_pendingOps.Add(l => l.Subscribe(handler));
        }
    }

    public void SubscribeNotify<T>(EventNotifyDelegate<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
        {
            LayerHub.EventCenter.SubscribeNotify(RouteIndex, handler);
            lock (m_subscriptions)
            {
                m_subscriptions.Add(UnsubscribeNotifyToken<T>.Rent(LayerHub.EventCenter, RouteIndex, handler));
            }
        }
        else
        {
            m_pendingOps.Add(l => l.SubscribeNotify(handler));
        }
    }

    public void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler) where T : struct
    {
        ThrowIfDisposed();
        if (RouteIndex != -1)
        {
            LayerHub.EventCenter.SubscribeAsync(RouteIndex, handler);
            lock (m_subscriptions)
            {
                m_subscriptions.Add(UnsubscribeDelegateAsyncToken<T>.Rent(LayerHub.EventCenter, RouteIndex, handler));
            }
        }
        else
        {
            m_pendingOps.Add(l => l.SubscribeAsync(handler));
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
        else m_pendingOps.Add(l => l.SubscribeParallel(handler, reportError));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendLocal<T>(in T value) where T : struct
    {
        return LayerHub.EventCenter.SendLocal(RouteIndex, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendBubble<T>(in T value) where T : struct
    {
        return LayerHub.EventCenter.Send(value, RouteIndex, Propagation.Bubble);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendDrop<T>(in T value) where T : struct
    {
        return LayerHub.EventCenter.Send(value, RouteIndex, Propagation.Drop);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState SendGlobal<T>(in T value) where T : struct
    {
        return LayerHub.EventCenter.Send(value, RouteIndex, Propagation.Global);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostLocal<T>(in T value) where T : struct
    {
        LayerHub.EventCenter.PostLocal(RouteIndex, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostBubble<T>(in T value) where T : struct
    {
        LayerHub.EventCenter.Post(value, RouteIndex, Propagation.Bubble);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostDrop<T>(in T value) where T : struct
    {
        LayerHub.EventCenter.Post(value, RouteIndex, Propagation.Drop);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostGlobal<T>(in T value) where T : struct
    {
        LayerHub.EventCenter.Post(value, RouteIndex, Propagation.Global);
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
}
