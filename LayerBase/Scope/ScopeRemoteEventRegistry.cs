using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Scope;

internal readonly struct ScopeRemoteEventRouteEntry
{
    public ScopeRemoteEventRouteEntry(
        int ownerScopeId,
        int routeId,
        Type eventType,
        Type handlerType,
        Type ownerLayerType,
        object invoker,
        IScopeRemoteEventDispatcher dispatcher)
    {
        OwnerScopeId = ownerScopeId;
        RouteId = routeId;
        EventType = eventType;
        HandlerType = handlerType;
        OwnerLayerType = ownerLayerType;
        Invoker = invoker;
        Dispatcher = dispatcher;
    }

    public int OwnerScopeId { get; }
    public int RouteId { get; }
    public Type EventType { get; }
    public Type HandlerType { get; }
    public Type OwnerLayerType { get; }
    public object Invoker { get; }
    public IScopeRemoteEventDispatcher Dispatcher { get; }
}

internal interface IScopeRemoteEventDispatcher
{
    void Dispatch(
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage);
}

internal sealed class ScopeRemoteEventDispatcher<TEvent> : IScopeRemoteEventDispatcher
    where TEvent : struct
{
    private readonly ScopeRemoteEventInvoker<TEvent> _invoker;

    public ScopeRemoteEventDispatcher(ScopeRemoteEventInvoker<TEvent> invoker)
    {
        _invoker = invoker;
    }

    public void Dispatch(
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (!payloadStorage.TryGet<TEvent>(runtimeId, payload, out var value))
            return;

        _invoker(in value);
    }
}

internal sealed class ScopeRemoteEventRegistry
{
    private readonly int _scopeId;
    private object?[] _invokers = Array.Empty<object?>();
    private IScopeRemoteEventDispatcher?[] _dispatchers = Array.Empty<IScopeRemoteEventDispatcher?>();
    private Type?[] _eventTypes = Array.Empty<Type?>();
    private Type?[] _handlerTypes = Array.Empty<Type?>();
    private Type?[] _ownerLayerTypes = Array.Empty<Type?>();

    public ScopeRemoteEventRegistry(int scopeId)
    {
        _scopeId = scopeId;
    }

    public void Clear()
    {
        _invokers = Array.Empty<object?>();
        _dispatchers = Array.Empty<IScopeRemoteEventDispatcher?>();
        _eventTypes = Array.Empty<Type?>();
        _handlerTypes = Array.Empty<Type?>();
        _ownerLayerTypes = Array.Empty<Type?>();
    }

    public void Register(in ScopeRemoteEventRouteEntry entry)
    {
        EnsureCapacity(entry.RouteId);

        if (_invokers[entry.RouteId] != null)
        {
            if (_handlerTypes[entry.RouteId] == entry.HandlerType) return;
            throw new InvalidOperationException(
                $"Scope '{_scopeId}' has duplicate ScopeEvent handlers for event '{entry.EventType.Name}': '{_ownerLayerTypes[entry.RouteId]?.Name}.{_handlerTypes[entry.RouteId]?.Name}' and '{entry.OwnerLayerType.Name}.{entry.HandlerType.Name}'.");
        }

        _invokers[entry.RouteId] = entry.Invoker;
        _dispatchers[entry.RouteId] = entry.Dispatcher;
        _eventTypes[entry.RouteId] = entry.EventType;
        _handlerTypes[entry.RouteId] = entry.HandlerType;
        _ownerLayerTypes[entry.RouteId] = entry.OwnerLayerType;
    }

    public bool TryDispatch(
        int runtimeId,
        ScopeEventEnvelope envelope,
        EventPayloadStorage payloadStorage)
    {
        var routeId = envelope.RouteId;
        var dispatchers = _dispatchers;
        if ((uint)routeId >= (uint)dispatchers.Length || dispatchers[routeId] == null)
            return false;

        dispatchers[routeId]!.Dispatch(runtimeId, envelope.Payload, payloadStorage);
        return true;
    }

    private void EnsureCapacity(int routeId)
    {
        if ((uint)routeId < (uint)_invokers.Length) return;

        var newSize = Math.Max(routeId + 1, _invokers.Length == 0 ? 4 : _invokers.Length * 2);
        Array.Resize(ref _invokers, newSize);
        Array.Resize(ref _dispatchers, newSize);
        Array.Resize(ref _eventTypes, newSize);
        Array.Resize(ref _handlerTypes, newSize);
        Array.Resize(ref _ownerLayerTypes, newSize);
    }
}
