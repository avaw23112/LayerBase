namespace LayerBase.Actor;

internal readonly struct ActorCallEntry
{
    public readonly int RouteId;
    public readonly Type RequestType;
    public readonly Type ResponseType;
    public readonly object Invoker;
    public readonly ActorCallColumnFactory Factory;

    public ActorCallEntry(
        int routeId,
        Type requestType,
        Type responseType,
        object invoker,
        ActorCallColumnFactory factory)
    {
        RouteId = routeId;
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }
}
