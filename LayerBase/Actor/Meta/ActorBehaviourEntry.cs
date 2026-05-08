namespace LayerBase.Actor;

internal readonly struct ActorBehaviourEntry
{
    public readonly int EventTypeId;
    public readonly Type EventType;
    public readonly object Invoker;

    public ActorBehaviourEntry(int eventTypeId, Type eventType, object invoker)
    {
        EventTypeId = eventTypeId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
    }
}
