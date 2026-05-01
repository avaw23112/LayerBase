using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

public interface IEventMetaData
{
    EventCategoryToken GetEventCategoryToken();
    void OnEventExpectation<TValue>(TValue e, Exception exception) where TValue : struct;
    EventPostPolicy? GetPostPolicy();
    EventTimerPolicy? GetTimerPolicy();
    EventBufferPolicy? GetBufferPolicy();
}

public abstract class EventMetaData<TEvent> : IEventMetaData where TEvent : struct
{
    public virtual EventCategoryToken Category => EventCategoryToken.Empty;

    public EventCategoryToken GetEventCategoryToken()
    {
        return Category;
    }


    public virtual void OnEventExpectation<TValue>(TValue e, Exception exception) where TValue : struct
    {
    }

    public virtual EventPostPolicy? PostPolicy => null;
    public virtual EventTimerPolicy? TimerPolicy => null;
    public virtual EventBufferPolicy? BufferPolicy => null;

    public EventPostPolicy? GetPostPolicy() => PostPolicy;
    public EventTimerPolicy? GetTimerPolicy() => TimerPolicy;
    public EventBufferPolicy? GetBufferPolicy() => BufferPolicy;
}
