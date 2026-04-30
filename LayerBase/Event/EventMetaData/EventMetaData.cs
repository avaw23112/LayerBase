using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

public interface IEventMetaData
{
    EventCategoryToken GetEventCategoryToken();
    void OnEventExpectation<EventType>(EventType e, Exception exception) where EventType : struct;
}

public abstract class EventMetaData<EventType> : IEventMetaData where EventType : struct
{
    public virtual EventCategoryToken Category => EventCategoryToken.Empty;

    public EventCategoryToken GetEventCategoryToken()
    {
        return Category;
    }


    public virtual void OnEventExpectation<TValue>(TValue e, Exception exception) where TValue : struct
    {
    }
}