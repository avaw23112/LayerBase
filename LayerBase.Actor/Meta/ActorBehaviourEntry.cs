namespace LayerBase.Actor;

internal readonly struct ActorBehaviourEntry
{
    public readonly int EventTypeId;
    public readonly Type EventType;
    public readonly object Invoker;
    public readonly ActorEventColumnFactory Factory;

    public ActorBehaviourEntry(
        int eventTypeId,
        Type eventType,
        object invoker,
        ActorEventColumnFactory factory)
    {
        EventTypeId = eventTypeId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }
}
