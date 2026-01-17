using LayerBase.Core;
using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

/// <summary>
/// 事件元数据调度：注册、查询及生命周期回调。
/// </summary>
internal static class EventMetaDataHandler
{
    /// <summary>已注册的事件元数据映射（Key: 事件类型）。</summary>
    private static readonly Dictionary<Type, IEventMetaData> s_metaDataByType = new();

    //---------------- 内部调用 ------------------

    internal static void Clear() => s_metaDataByType.Clear();

    /// <summary>接受 EventTracer 的事件创建回调。</summary>
    internal static void OnClassicEventCreated(ref EventCategoryToken category, ref Core.EventStateTrace.EventState state)
    {
        foreach (var metaData in s_metaDataByType.Values)
        {
            if (metaData.GetEventCategoryToken() == category)
            {
                metaData.OnEventCreated(ref state);
            }
        }
    }

    /// <summary>接受 EventTracer 的事件销毁回调。</summary>
    internal static void OnClassicEventDestroyed(ref EventCategoryToken category, ref Core.EventStateTrace.EventState state)
    {
        foreach (var metaData in s_metaDataByType.Values)
        {
            if (metaData.GetEventCategoryToken() == category)
            {
                metaData.OnEventDestroyed(ref state);
            }
        }
    }

    //---------------- 注册与查询 ------------------

    /// <summary>由源生成器调用，注册元数据信息。</summary>
    public static void RegisterMetaData<EventType>(IEventMetaData metaData)
    {
        s_metaDataByType[typeof(EventType)] = metaData;
    }

    public static EventCategoryToken Category<EventType>() where EventType : struct =>
        Category(typeof(EventType));

    public static EventCategoryToken Category(Type eventType) =>
        s_metaDataByType.TryGetValue(eventType, out var metaData)
            ? metaData.GetEventCategoryToken()
            : EventCategoryToken.Empty;

    public static int MaxBufferSize<EventType>() where EventType : struct =>
        MaxBufferSize(typeof(EventType));

    public static int MaxBufferSize(Type eventType) =>
        s_metaDataByType.TryGetValue(eventType, out var metaData)
            ? metaData.MaxBufferSize
            : 0;

    public static int EventHandlerJitter<EventType>() where EventType : struct =>
        EventHandlerJitter(typeof(EventType));

    public static int EventHandlerJitter(Type eventType) =>
        s_metaDataByType.TryGetValue(eventType, out var metaData)
            ? metaData.EventHandlerJitter
            : 0;

    public static EventQueueOverflowStrategy EventQueueOverflowStrategy<EventType>() where EventType : struct =>
        EventQueueOverflowStrategy(typeof(EventType));

    public static EventQueueOverflowStrategy EventQueueOverflowStrategy(Type eventType) =>
        s_metaDataByType.TryGetValue(eventType, out var metaData)
            ? metaData.EventQueueOverflowStrategy
            : Core.EventQueueOverflowStrategy.OverWrite;

    public static void OnEventExpectation<EventType>(EventType e, Exception exception) where EventType : struct
    {
        if (s_metaDataByType.TryGetValue(typeof(EventType), out var metaData))
        {
            metaData.OnEventExpectation(e, exception);
        }
    }

    public static void EventMergeStrategy<EventType>(PooledChunkedOverwriteQueue<Core.Event.Event<EventType>> queue)
        where EventType : struct
    {
        if (s_metaDataByType.TryGetValue(typeof(EventType), out var metaData))
        {
            metaData.EventMergeStrategy(queue);
        }
    }
}
