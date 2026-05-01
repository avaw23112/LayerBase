using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

public interface IEventMetaData
{
    int EventId { get; }
    EventCategoryToken GetEventCategoryToken();
    void OnEventExpectation<TValue>(TValue e, Exception exception) where TValue : struct;
    EventPostPolicy? GetPostPolicy();
    EventTimerPolicy? GetTimerPolicy();
    EventBufferPolicy? GetBufferPolicy();
    
    int GetPostCoalesceKey(object value);
}

public abstract class EventMetaData<TEvent> : IEventMetaData where TEvent : struct
{
    public int EventId => EventTypeId<TEvent>.Id;
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

    public virtual int GetPostCoalesceKey(in TEvent value)
    {
        return 0;
    }

    public virtual bool TryMergePostEvent(ref TEvent current, in TEvent next)
    {
        return false;
    }

    int IEventMetaData.GetPostCoalesceKey(object value)
    {
        if (value is TEvent e) return GetPostCoalesceKey(e);
        return 0;
    }
}
