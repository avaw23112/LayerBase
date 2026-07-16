using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Scope;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeScopeCallAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeScopeEventAttribute : Attribute
{
}

public interface IAutoScopeEndpointBinder
{
    void AutoBindScopeEndpoints(Layer layer);
}

public interface IScopeCallHandler<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    LBTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IScopeEventHandler<TEvent>
    where TEvent : struct
{
    void Handle(in TEvent value);
}

public static class ScopeCallRegistrationBridge
{
    public static void RegisterForOwner<TRequest, TResponse>(
        Layer layer,
        object owner,
        IScopeCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        layer.RegisterScopeCallHandlerForOwner(owner, handler);
    }
}

public static class ScopeEventRegistrationBridge
{
    public static void RegisterForOwner<TEvent>(
        Layer layer,
        object owner,
        IScopeEventHandler<TEvent> handler)
        where TEvent : struct
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        layer.RegisterScopeEventHandlerForOwner(owner, handler);
    }
}

internal delegate LBTask<TResponse> ScopeRemoteCallInvoker<TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : struct
    where TResponse : struct;

internal delegate void ScopeRemoteEventInvoker<TEvent>(in TEvent value)
    where TEvent : struct;

internal static class ScopeRemoteCallRouteRegistry
{
    private static int s_nextId;

    public static int GetNextId()
    {
        return Interlocked.Increment(ref s_nextId) - 1;
    }
}

internal static class ScopeRemoteCallRouteId<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    public static readonly int Id = ScopeRemoteCallRouteRegistry.GetNextId();
}

internal static class ScopeRemoteEventRouteRegistry
{
    private static int s_nextId;

    public static int GetNextId()
    {
        return Interlocked.Increment(ref s_nextId) - 1;
    }
}

internal static class ScopeRemoteEventRouteId<TEvent>
    where TEvent : struct
{
    public static readonly int Id = ScopeRemoteEventRouteRegistry.GetNextId();
}
