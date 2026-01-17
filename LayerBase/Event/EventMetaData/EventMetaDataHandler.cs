using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

internal static class EventMetaDataHandler
{
    /// <summary>
    /// Key: EvetType Value : EventMetaData
    /// </summary>
    private static Dictionary<Type,IEventMetaData>  m_mapEventMetaDatas = new();
    
    //----------------内部调用----------------------------------
    
    /// <summary>
    /// 用于接受EvenTracer的特定类别事件创建的事件
    /// </summary>
    /// <param name="category"></param>
    /// <param name="e"></param>
    internal static void OnClassicEventCreated(ref EventCategoryToken category, ref Core.EventStateTrace.EventState e)
    {
        foreach (var m_MapEventMetaData in m_mapEventMetaDatas.Values)
        {
            if (m_MapEventMetaData.GetEventCategoryToken() == category)
            {
                m_MapEventMetaData.OnEventCreated(ref e);
            }
        }
    }
    
    /// <summary>
    /// 用于接受EvenTracer的特定类别事件销毁的事件
    /// </summary>
    /// <param name="category"></param>
    /// <param name="e"></param>
    internal static void OnClassicEventDestroyed(ref EventCategoryToken category, ref Core.EventStateTrace.EventState e)
    {
        foreach (var m_MapEventMetaData in m_mapEventMetaDatas.Values)
        {
            if (m_MapEventMetaData.GetEventCategoryToken() == category)
            {
                m_MapEventMetaData.OnEventDestroyed(ref e);
            }
        }
    }
    
    //----------------内部调用----------------------------------
    
    /// <summary>
    /// 由源生成器调用，注册元数据信息
    /// </summary>
    /// <param name="metaData"></param>
    /// <typeparam name="EventType"></typeparam>
    public static void RegisterMetaData<EventType>(IEventMetaData metaData)
    {
        m_mapEventMetaDatas.Add(typeof(EventType), metaData);
    }

    public static EventCategoryToken Category<EventType>() where EventType : struct
    {
        if (m_mapEventMetaDatas.TryGetValue(typeof(EventType), out IEventMetaData metaData))
        {
            return metaData.GetEventCategoryToken();
        }
        return EventCategoryToken.Empty;
    }

    public static EventCategoryToken Category(Type eventType)
    {
        if (m_mapEventMetaDatas.TryGetValue(eventType, out IEventMetaData metaData))
        {
            return metaData.GetEventCategoryToken();
        }
        return EventCategoryToken.Empty;
    }
}