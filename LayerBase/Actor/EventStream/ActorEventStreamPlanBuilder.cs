using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Actor;

/// <summary>
/// Actor EventStream 构建计划生成器。
///
/// 作用：
/// 1. 从 EventMetaData&lt;TEvent&gt; 读取 ActorMailOptions。
/// 2. 解析出 EventStreamOptions。
/// 3. 生成 ActorEventStreamPlan&lt;TEvent&gt;。
/// </summary>
public static class ActorEventStreamPlanBuilder
{
    /// <summary>
    /// 构建 ActorEventStreamPlan。
    ///
    /// 作用：
    /// 1. 读取 EventMetaData&lt;TEvent&gt;。
    /// 2. 解析 ActorMailOptions。
    /// 3. 提取 EventStreamOptions。
    /// 4. 返回 ActorEventStreamPlan&lt;TEvent&gt;。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    /// <returns>
    /// 构建好的 ActorEventStreamPlan。
    /// </returns>
    public static ActorEventStreamPlan<TEvent> Build<TEvent>()
        where TEvent : struct
    {
        int eventId = EventTypeId<TEvent>.Id;
        EventStreamOptions streamOptions = ResolveStreamOptions<TEvent>();
        return new ActorEventStreamPlan<TEvent>(
            eventId,
            streamOptions);
    }

    private static EventStreamOptions ResolveStreamOptions<TEvent>()
        where TEvent : struct
    {
        ActorMailOptions? metaOptions =
            EventMetaDataRegistry.GetActorMailOptions<TEvent>();
        if (metaOptions.HasValue)
        {
            return new EventStreamOptions(
                metaOptions.Value.SegmentCapacity,
                metaOptions.Value.MaxRetainedSegments);
        }

        return EventStreamOptions.Default;
    }
}
