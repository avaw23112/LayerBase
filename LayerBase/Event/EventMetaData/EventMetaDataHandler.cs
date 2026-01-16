using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

internal static class EventMetaDataHandler
{
    private static Dictionary<Type,IEventMetaData>  m_eventMetaDatas = new();

    public static void RegisterMetaData<EventType>(IEventMetaData metaData)
    {
        m_eventMetaDatas.Add(typeof(EventType), metaData);
    }

    public static EventCategoryToken Category<EventType>() where EventType : struct
    {
        if (m_eventMetaDatas.TryGetValue(typeof(EventType), out IEventMetaData metaData))
        {
            return metaData.GetEventCategoryToken();
        }
        return EventCategoryToken.Empty;
    }

    public static EventCategoryToken Category(Type eventType)
    {
        if (m_eventMetaDatas.TryGetValue(eventType, out IEventMetaData metaData))
        {
            return metaData.GetEventCategoryToken();
        }
        return EventCategoryToken.Empty;
    }
}