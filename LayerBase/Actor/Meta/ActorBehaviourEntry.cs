namespace LayerBase.Actor;

/// <summary>
/// 委托：在 Actor 创建时注册 EventStream handler。
/// </summary>
/// <param name="actor">Actor 实例。</param>
/// <param name="archetypeId">archetype 索引。</param>
/// <param name="slotIndex">slot 索引。</param>
/// <param name="generation">generation。</param>
/// <param name="world">ActorWorld 实例。</param>
internal delegate void ActorStreamHandlerRegister(object actor, int archetypeId, int slotIndex, int generation, ActorWorld world);

internal readonly struct ActorBehaviourEntry
{
    public readonly int EventTypeId;
    public readonly Type EventType;
    public readonly object Invoker;
    public readonly ActorEventColumnFactory? Factory;
    public readonly ActorStreamHandlerRegister? StreamRegister;
    public readonly bool IsStreamHandler;

    public ActorBehaviourEntry(
        int                     eventTypeId,
        Type                    eventType,
        object                  invoker,
        ActorEventColumnFactory factory)
    {
        EventTypeId = eventTypeId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        StreamRegister = null;
        IsStreamHandler = false;
    }

    public ActorBehaviourEntry(
        int                          eventTypeId,
        Type                         eventType,
        object                       handlerFactory,
        ActorStreamHandlerRegister   streamRegister)
    {
        EventTypeId = eventTypeId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Invoker = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
        Factory = null;
        StreamRegister = streamRegister ?? throw new ArgumentNullException(nameof(streamRegister));
        IsStreamHandler = true;
    }
}