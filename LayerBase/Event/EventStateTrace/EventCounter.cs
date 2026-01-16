using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.EventStateTrace;

internal class EventGlobalInfo
{
    public int eventTypeId;
    public EventCategoryToken eventCategoryToken;
    public int eventCount;
}

/// <summary>
/// 对每一类事件的数量进行统计
/// </summary>
internal class EventCounter
{
    Dictionary<EventCategoryToken,EventGlobalInfo> m_eventGlobalInfos = new();
    
    public int Increment<T>() where T : struct
    { 
        if (EventMetaDataHandler.Category<T>().IsEmpty)
        {
            return -1;
        }
        var category = EventMetaDataHandler.Category<T>();
        if (!m_eventGlobalInfos.TryGetValue(category,out EventGlobalInfo? info))
        { 
            info = new EventGlobalInfo
            {
                eventTypeId = EventTypeId<T>.Id,
                eventCategoryToken = EventMetaDataHandler.Category<T>(),
                eventCount = 0
            };
            m_eventGlobalInfos.Add(category, info);
        }
        info.eventCount++;
        return info.eventCount;
    }

    public int Decrement<T>() where T : struct
    {
        if (EventMetaDataHandler.Category<T>().IsEmpty)
        {
            return -1;
        }
        var category = EventMetaDataHandler.Category<T>();
        if (!m_eventGlobalInfos.TryGetValue(category, out EventGlobalInfo? info))
        {
            throw new Exception("不可能完成的事件");
        }
        if (info.eventCount <= 0)
        {
            throw new Exception("不可能完成的事件");
        }
        info.eventCount--;
        return info.eventCount;
    }

    public int Decrement(Type type)
    {
        var  category = EventMetaDataHandler.Category(type);
        if (!m_eventGlobalInfos.TryGetValue(category, out EventGlobalInfo? info))
        {
            throw new Exception("不可能完成的事件");
        }
        if (info.eventCount <= 0)
        {
            throw new Exception("不可能完成的事件");
        }
        info.eventCount--;
        return info.eventCount;
    }
}