using LayerBase.Core.Event;

namespace LayerBase.Actor;

/// <summary>
/// 单个 TEvent 的 Actor EventStream 构建计划。
///
/// 作用：
/// 1. 保存事件类型 ID。
/// 2. 保存 EventStreamOptions。
/// 3. 将 EventMetaData 解析结果编译到运行时结构中。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
public readonly struct ActorEventStreamPlan<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 当前事件类型 ID。
    /// </summary>
    public readonly int EventId;

    /// <summary>
    /// 当前事件类型的 EventStream 配置。
    /// </summary>
    public readonly EventStreamOptions StreamOptions;

    /// <summary>
    /// 构造 ActorEventStreamPlan。
    /// </summary>
    /// <param name="eventId">
    /// 当前事件类型 ID。
    /// </param>
    /// <param name="streamOptions">
    /// 当前事件类型的 EventStream 配置。
    /// </param>
    public ActorEventStreamPlan(
        int eventId,
        EventStreamOptions streamOptions)
    {
        EventId = eventId;
        StreamOptions = streamOptions;
    }
}
