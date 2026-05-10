namespace LayerBase.Actor;

internal readonly struct ActorEventPostPlan<TEvent>
    where TEvent : struct
{
    public readonly int EventId;
    public readonly byte RouteCode;
    public readonly ActorMailOptions MailOptions;
    public readonly bool RequirePostableStamp;
    public readonly bool RejectDisabled;

    public ActorEventPostPlan(
        int eventId,
        byte routeCode,
        ActorMailOptions mailOptions,
        bool requirePostableStamp,
        bool rejectDisabled)
    {
        EventId = eventId;
        RouteCode = routeCode;
        MailOptions = mailOptions;
        RequirePostableStamp = requirePostableStamp;
        RejectDisabled = rejectDisabled;
    }
}
