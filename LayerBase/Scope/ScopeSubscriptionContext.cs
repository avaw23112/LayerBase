using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Event.Delay;

namespace LayerBase.Scope;

public readonly struct ScopeSubscriptionContext
{
    internal ScopeSubscriptionContext(
        ScopeRuntime scope,
        LayerMembership membership,
        int serviceSlot)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Membership = membership;
        ServiceSlot = serviceSlot;
    }

    public ScopeRuntime Scope { get; }

    public LayerMembership Membership { get; }

    public int ServiceSlot { get; }

    public void SubscribeFlow<T>(EventHandleDelegate<T> handler)
        where T : struct
    {
        Scope.RegisterSubscribeFlow(Membership, ServiceSlot, handler);
    }

    public void SubscribeAsync<T>(EventHandleDelegateAsync<T> handler)
        where T : struct
    {
        Scope.RegisterSubscribeAsync(Membership, ServiceSlot, handler);
    }

    public void SubscribeNotify<T>(EventNotifyDelegate<T> handler)
        where T : struct
    {
        Scope.RegisterSubscribeNotify(Membership, ServiceSlot, handler);
    }

    public void Subscribe<T>(EventNotifyDelegate<T> handler)
        where T : struct
    {
        Scope.RegisterSubscribe(Membership, ServiceSlot, handler);
    }

    public void SubscribeParallel<T>(EventNotifyDelegate<T> handler)
        where T : struct
    {
        Scope.RegisterSubscribeParallel(Membership, ServiceSlot, handler);
    }

    public IDelayPublisher<T> SubscribeDelay<T>()
        where T : struct
    {
        return Scope.GetOrCreateDelayPublisher<T>();
    }
}
