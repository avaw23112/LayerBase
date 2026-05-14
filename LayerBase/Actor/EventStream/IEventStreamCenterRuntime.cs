namespace LayerBase.Actor;

/// <summary>
/// EventStreamCenter 运行时接口。
///
/// 作用：
/// 提供类型擦除的 EventStreamCenter 访问接口。
/// 用于 ActorWorld 管理多个事件类型的 EventStreamCenter。
/// </summary>
internal interface IEventStreamCenterRuntime
{
    /// <summary>
    /// 事件类型 ID。
    /// </summary>
    int EventTypeId { get; }

    /// <summary>
    /// Pump 该事件类型的事件流。
    /// </summary>
    /// <param name="maxCount">
    /// 本次最多处理多少封邮件。
    /// </param>
    /// <returns>
    /// 实际处理数量。
    /// </returns>
    int Pump(int maxCount);

    /// <summary>
    /// 该事件流是否为空。
    /// </summary>
    bool IsEmpty { get; }
}
