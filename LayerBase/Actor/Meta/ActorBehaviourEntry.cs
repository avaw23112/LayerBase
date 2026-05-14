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

/// <summary>
/// 委托：在 Actor 销毁或归还对象池时注销 EventStream handler。
///
/// 参数说明：
/// archetypeId：Actor 类型对应的行为原型编号。
/// slotIndex：Actor 在 TypedActorStorage 中的 slot 下标。
/// world：当前 ActorWorld。
///
/// 作用：
/// 保存 TEvent 的强泛型注销路径。
/// 让销毁时可以直接访问 EventStreamRuntime<TEvent>，
/// 避免遍历 ActorWorld 内全部 EventStreamRuntime。
/// </summary>
internal delegate void ActorStreamHandlerUnregister(
    int archetypeId,
    int slotIndex,
    ActorWorld world);

internal readonly struct ActorBehaviourEntry
{
    public readonly int EventTypeId;
    public readonly Type EventType;
    public readonly object Invoker;
    public readonly ActorEventColumnFactory? Factory;
    public readonly ActorStreamHandlerRegister? StreamRegister;
    public readonly ActorStreamHandlerUnregister? StreamUnregister;
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
        StreamUnregister = null;
        IsStreamHandler = false;
    }

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
        Factory = null;
        StreamRegister = streamRegister ?? throw new ArgumentNullException(nameof(streamRegister));
        StreamUnregister = streamUnregister ?? throw new ArgumentNullException(nameof(streamUnregister));
        IsStreamHandler = true;
    }
}
