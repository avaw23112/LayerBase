using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Event.Delay;

namespace LayerBase.Scope;

internal sealed class ScopeSubscriptionRegistry : IDisposable
{
    private static readonly Action<int, int, int, Exception> NoopParallelError = static (_, _, _, _) => { };

    private readonly ScopeRuntime _scope;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly Dictionary<Type, IDelayPublisherInternal> _delayPublishers = new();
    private bool _disposed;

    public ScopeSubscriptionRegistry(ScopeRuntime scope)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public void SubscribeFlow<T>(LayerMembership membership, int serviceSlot, EventHandleDelegate<T> handler)
        where T : struct
    {
        int routeKey = ResolveRouteKey(membership, serviceSlot, -1);
        _scope.EventCenter.SubscribeFlow(routeKey, handler);
        _subscriptions.Add(ScopeUnsubscribeToken.Rent(
            _scope.EventCenter,
            routeKey,
            handler,
            typeof(T),
            ScopeUnsubscribeKind.Flow));
    }

    public void SubscribeAsync<T>(LayerMembership membership, int serviceSlot, EventHandleDelegateAsync<T> handler)
        where T : struct
    {
        int routeKey = ResolveRouteKey(membership, serviceSlot, -1);
        _scope.EventCenter.SubscribeAsync(routeKey, handler);
        _subscriptions.Add(ScopeUnsubscribeToken.Rent(
            _scope.EventCenter,
            routeKey,
            handler,
            typeof(T),
            ScopeUnsubscribeKind.Async));
    }

    public void SubscribeNotify<T>(LayerMembership membership, int serviceSlot, EventNotifyDelegate<T> handler)
        where T : struct
    {
        int routeKey = ResolveRouteKey(membership, serviceSlot, -1);
        _scope.EventCenter.SubscribeNotify(routeKey, handler);
        _subscriptions.Add(ScopeUnsubscribeToken.Rent(
            _scope.EventCenter,
            routeKey,
            handler,
            typeof(T),
            ScopeUnsubscribeKind.Notify));
    }

    public void Subscribe<T>(LayerMembership membership, int serviceSlot, EventNotifyDelegate<T> handler)
        where T : struct
    {
        int routeKey = ResolveRouteKey(membership, serviceSlot, -1);
        _scope.EventCenter.Subscribe(routeKey, handler);
        _subscriptions.Add(ScopeUnsubscribeToken.Rent(
            _scope.EventCenter,
            routeKey,
            handler,
            typeof(T),
            ScopeUnsubscribeKind.Subscribe));
    }

    public void SubscribeParallel<T>(
        LayerMembership membership,
        int serviceSlot,
        EventNotifyDelegate<T> handler,
        Action<int, int, int, Exception>? reportError = null)
        where T : struct
    {
        int routeKey = ResolveRouteKey(membership, serviceSlot, -1);
        _scope.EventCenter.SubscribeParallel(routeKey, handler, reportError ?? NoopParallelError);
        _subscriptions.Add(ScopeUnsubscribeToken.Rent(
            _scope.EventCenter,
            routeKey,
            handler,
            typeof(T),
            ScopeUnsubscribeKind.Parallel));
    }

    public void SubscribeFlow(ScopeObjectBinding binding, object handler, Type eventType)
    {
        int routeKey = ResolveRouteKey(binding.Membership, binding.ServiceSlot, binding.ContextSlot);
        _scope.EventCenter.SubscribeFlow(routeKey, handler, eventType);
        _subscriptions.Add(ScopeUnsubscribeToken.Rent(
            _scope.EventCenter,
            routeKey,
            handler,
            eventType,
            ScopeUnsubscribeKind.Flow));
    }

    public void SubscribeAsync(ScopeObjectBinding binding, object handler, Type eventType)
    {
        int routeKey = ResolveRouteKey(binding.Membership, binding.ServiceSlot, binding.ContextSlot);
        _scope.EventCenter.SubscribeAsync(routeKey, handler, eventType);
        _subscriptions.Add(ScopeUnsubscribeToken.Rent(
            _scope.EventCenter,
            routeKey,
            handler,
            eventType,
            ScopeUnsubscribeKind.Async));
    }

    public IDelayPublisher<T> GetOrCreateDelayPublisher<T>()
        where T : struct
    {
        ThrowIfDisposed();

        if (_delayPublishers.TryGetValue(typeof(T), out IDelayPublisherInternal? existing))
        {
            return (IDelayPublisher<T>)existing;
        }

        var publisher = new DelayPublisher<T>(_scope.DelayManager);
        int publisherId = _scope.DelayManager.RegisterPublisher(publisher);
        publisher.SetId(publisherId);
        _delayPublishers.Add(typeof(T), publisher);
        return publisher;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (int i = _subscriptions.Count - 1; i >= 0; i--)
        {
            _subscriptions[i].Dispose();
        }

        _subscriptions.Clear();

        foreach ((_, IDelayPublisherInternal publisher) in _delayPublishers)
        {
            if (publisher.PublisherId >= 0)
            {
                _scope.DelayManager.UnregisterPublisher(publisher.PublisherId);
            }
        }

        _delayPublishers.Clear();
    }

    private static int ResolveRouteKey(LayerMembership membership, int serviceSlot, int contextSlot)
    {
        if (membership.Start >= 0)
        {
            return membership.Start;
        }

        if (serviceSlot >= 0)
        {
            return serviceSlot;
        }

        if (contextSlot >= 0)
        {
            return contextSlot;
        }

        return 0;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopeSubscriptionRegistry));
        }
    }

    private enum ScopeUnsubscribeKind
    {
        Flow,
        Async,
        Notify,
        Subscribe,
        Parallel
    }

    private sealed class ScopeUnsubscribeToken : IDisposable
    {
        private static readonly Stack<ScopeUnsubscribeToken> Pool = new();
        private static readonly object PoolLock = new();

        private EventCenter? _center;
        private int _routeKey;
        private object? _handler;
        private Type? _eventType;
        private ScopeUnsubscribeKind _kind;
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            switch (_kind)
            {
                case ScopeUnsubscribeKind.Flow:
                    _center?.UnsubscribeFlow(_routeKey, _handler!, _eventType!);
                    break;
                case ScopeUnsubscribeKind.Async:
                    _center?.UnsubscribeAsync(_routeKey, _handler!, _eventType!);
                    break;
                case ScopeUnsubscribeKind.Notify:
                    _center?.UnsubscribeNotify(_routeKey, _handler!, _eventType!);
                    break;
                case ScopeUnsubscribeKind.Subscribe:
                    _center?.Unsubscribe(_routeKey, _handler!, _eventType!);
                    break;
                case ScopeUnsubscribeKind.Parallel:
                    _center?.UnsubscribeParallel(_routeKey, _handler!, _eventType!);
                    break;
            }

            _center = null;
            _handler = null;
            _eventType = null;

            lock (PoolLock)
            {
                Pool.Push(this);
            }
        }

        public static ScopeUnsubscribeToken Rent(
            EventCenter center,
            int routeKey,
            object handler,
            Type eventType,
            ScopeUnsubscribeKind kind)
        {
            ScopeUnsubscribeToken token;
            lock (PoolLock)
            {
                token = Pool.Count > 0 ? Pool.Pop() : new ScopeUnsubscribeToken();
            }

            token._center = center;
            token._routeKey = routeKey;
            token._handler = handler;
            token._eventType = eventType;
            token._kind = kind;
            token._disposed = 0;
            return token;
        }
    }
}
