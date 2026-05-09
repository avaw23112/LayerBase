namespace LayerBase.Actor;

internal readonly struct ActorCallEntry
{
    public readonly int RouteId;
    public readonly Type RequestType;
    public readonly Type ResponseType;
    public readonly Delegate Invoker;

    public ActorCallEntry(
        int routeId,
        Type requestType,
        Type responseType,
        Delegate invoker)
    {
        RouteId = routeId;
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
    }
}
