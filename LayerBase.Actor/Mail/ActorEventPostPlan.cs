namespace LayerBase.Actor;

internal readonly struct ActorEventPostPlan<TEvent>
    where TEvent : struct
{
    public readonly int EventId;
    public readonly ActorPostRouteCode RouteCode;
    public readonly ActorMailOptions MailOptions;

    public ActorEventPostPlan(
        int                eventId,
        ActorPostRouteCode routeCode,
        ActorMailOptions   mailOptions)
    {
        EventId = eventId;
        RouteCode = routeCode;
        MailOptions = mailOptions;
    }
}
