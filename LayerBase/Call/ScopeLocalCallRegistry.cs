using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Call;

internal readonly struct ScopeLocalCallRouteEntry
{
    public ScopeLocalCallRouteEntry(
        int routeId,
        Type requestType,
        Type responseType,
        Type handlerType,
        Type ownerLayerType,
        object invoker,
        IScopeLocalCallDispatcher dispatcher)
    {
        RouteId = routeId;
        RequestType = requestType;
        ResponseType = responseType;
        HandlerType = handlerType;
        OwnerLayerType = ownerLayerType;
        Invoker = invoker;
        Dispatcher = dispatcher;
    }

    public int RouteId { get; }
    public Type RequestType { get; }
    public Type ResponseType { get; }
    public Type HandlerType { get; }
    public Type OwnerLayerType { get; }
    public object Invoker { get; }
    public IScopeLocalCallDispatcher Dispatcher { get; }
}

internal sealed class ScopeLocalCallRegistry
{
    private readonly int _scopeId;
    private object?[] _invokers = Array.Empty<object?>();
    private IScopeLocalCallDispatcher?[] _dispatchers = Array.Empty<IScopeLocalCallDispatcher?>();
    private Type?[] _handlerTypes = Array.Empty<Type?>();
    private Type?[] _ownerLayerTypes = Array.Empty<Type?>();

    public ScopeLocalCallRegistry(int scopeId)
    {
        _scopeId = scopeId;
    }

    public void Clear()
    {
        _invokers = Array.Empty<object?>();
        _dispatchers = Array.Empty<IScopeLocalCallDispatcher?>();
        _handlerTypes = Array.Empty<Type?>();
        _ownerLayerTypes = Array.Empty<Type?>();
    }

    public void Register(in ScopeLocalCallRouteEntry entry)
    {
        EnsureCapacity(entry.RouteId);

        if (_invokers[entry.RouteId] != null)
        {
            if (_handlerTypes[entry.RouteId] == entry.HandlerType) return;
            throw new ScopeLocalCallRouteConflictException(
                _scopeId,
                entry.RequestType,
                entry.ResponseType,
                _ownerLayerTypes[entry.RouteId] ?? typeof(LayerRuntime),
                _handlerTypes[entry.RouteId] ?? _invokers[entry.RouteId]!.GetType(),
                entry.OwnerLayerType,
                entry.HandlerType);
        }

        _invokers[entry.RouteId] = entry.Invoker;
        _dispatchers[entry.RouteId] = entry.Dispatcher;
        _handlerTypes[entry.RouteId] = entry.HandlerType;
        _ownerLayerTypes[entry.RouteId] = entry.OwnerLayerType;
    }

    public void Dispatch(
        int runtimeId,
        ScopeCallEnvelope envelope,
        EventPayloadStorage payloadStorage)
    {
        var routeId = envelope.RouteId;
        var dispatchers = _dispatchers;
        if ((uint)routeId >= (uint)dispatchers.Length || dispatchers[routeId] == null)
        {
            envelope.Completion?.TrySetException(
                new ScopeLocalCallRouteNotFoundException(
                    _scopeId,
                    typeof(object),
                    typeof(object)));
            return;
        }

        dispatchers[routeId]!.Dispatch(runtimeId, envelope, payloadStorage);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> CallAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (cancellationToken.IsCancellationRequested)
            return LBTask<TResponse>.FromCanceled(cancellationToken);

        var routeId = ScopeLocalCallRouteId<TRequest, TResponse>.Id;
        var invokers = _invokers;
        if ((uint)routeId >= (uint)invokers.Length || invokers[routeId] == null)
            return LBTask<TResponse>.FromException(
                new ScopeLocalCallRouteNotFoundException(
                    _scopeId,
                    typeof(TRequest),
                    typeof(TResponse)));

        return ((ScopeLocalCallInvoker<TRequest, TResponse>)invokers[routeId]!)(request, cancellationToken);
    }

    private void EnsureCapacity(int routeId)
    {
        if ((uint)routeId < (uint)_invokers.Length) return;

        var newSize = Math.Max(routeId + 1, _invokers.Length == 0 ? 4 : _invokers.Length * 2);
        Array.Resize(ref _invokers, newSize);
        Array.Resize(ref _dispatchers, newSize);
        Array.Resize(ref _handlerTypes, newSize);
        Array.Resize(ref _ownerLayerTypes, newSize);
    }
}
