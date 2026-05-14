namespace LayerBase.Actor;

/// <summary>
/// EventStreamRuntime 基类。
///
/// 作用：
/// 提供类型擦除的 EventStreamCenter 访问接口。
/// 用于 TypedActorStorage 管理多个事件类型的 EventStreamCenter。
/// </summary>
internal abstract class EventStreamRuntimeBase : IEventStreamCenterRuntime
{
    /// <summary>
    /// 事件类型 ID。
    /// </summary>
    public abstract int EventTypeId { get; }

    /// <summary>
    /// 该事件流是否为空。
    /// </summary>
    public abstract bool IsEmpty { get; }

    /// <summary>
    /// Pump 该事件类型的事件流。
    /// </summary>
    /// <param name="maxCount">
    /// 本次最多处理多少封邮件。
    /// </param>
    /// <returns>
    /// 实际处理数量。
    /// </returns>
    public abstract int Pump(int maxCount);

    /// <summary>
    /// 注销指定 slot 的事件处理器。
    /// </summary>
    /// <param name="slotIndex">
    /// slot 索引。
    /// </param>
    public abstract void UnregisterHandler(int slotIndex);
}
