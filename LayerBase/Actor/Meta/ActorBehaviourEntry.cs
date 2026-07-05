namespace LayerBase.Actor;

internal delegate void ActorStreamHandlerRegister(object actor, int archetypeId, int slotIndex, int generation, ActorWorld world);

internal delegate void ActorStreamHandlerUnregister(
    int archetypeId,
    int slotIndex,
    ActorWorld world);

internal readonly struct ActorBehaviourEntry
{
    public readonly int EventTypeId;
    public readonly Type EventType;
    public readonly object Invoker;
    public readonly ActorStreamHandlerRegister StreamRegister;
    public readonly ActorStreamHandlerUnregister StreamUnregister;

    public ActorBehaviourEntry(
        int                          eventTypeId,
        Type                         eventType,
        object                       handlerFactory,
        ActorStreamHandlerRegister   streamRegister,
        ActorStreamHandlerUnregister streamUnregister)
    {
        EventTypeId = eventTypeId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Invoker = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
        StreamRegister = streamRegister ?? throw new ArgumentNullException(nameof(streamRegister));
        StreamUnregister = streamUnregister ?? throw new ArgumentNullException(nameof(streamUnregister));
    }
}
