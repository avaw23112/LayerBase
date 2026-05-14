namespace LayerBase.Actor;

internal readonly struct ActorEventPostPlan<TEvent>
    where TEvent : struct
{
    public readonly int EventId;
    public readonly ActorMailOptions MailOptions;

    public ActorEventPostPlan(
        int                eventId,
        ActorMailOptions   mailOptions)
    {
        EventId = eventId;
        MailOptions = mailOptions;
    }
}