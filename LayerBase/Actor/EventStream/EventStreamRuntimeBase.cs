namespace LayerBase.Actor;

/// <summary>
/// EventStreamRuntime 非泛型基类。
/// </summary>
internal abstract class EventStreamRuntimeBase : IEventStreamCenterRuntime
{
    /// <summary>
    /// 当前 EventStreamRuntime 所属 ActorWorld 的运行时编号。
    /// </summary>
    public abstract int RuntimeIndex { get; }

    /// <summary>
    /// 当前 EventStreamRuntime 所属 Actor archetype 编号。
    /// </summary>
    public abstract int ArchetypeId { get; }

    /// <summary>
    /// 当前 EventStreamRuntime 对应的事件类型 ID。
    /// </summary>
    public abstract int EventTypeId { get; }

    public abstract bool IsEmpty { get; }

    public abstract int Pump(int maxCount);

    public abstract void UnregisterHandler(int slotIndex);
}
