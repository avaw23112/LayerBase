using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Actor;

namespace LayerBase.Event.EventMetaData;

public delegate IEventMetaData EventMetaDataFactory();

public interface IEventMetaData
{
    int EventId { get; }
    EventCategoryToken GetEventCategoryToken();
    void OnEventExpectation<TValue>(TValue e, Exception exception) where TValue : struct;
    EventPostPolicy? GetPostPolicy();
    EventTimerPolicy? GetTimerPolicy();
    EventBufferPolicy? GetBufferPolicy();
    ActorMailOptions? GetActorMailOptions();

    int GetPostCoalesceKey(object value);
    EventIdentity GetIdentity();
}

public static class EventMetaData
{
    public static bool TryMergePostEvent<TEvent>(
        in  TEvent oldValue,
        in  TEvent newValue,
        out TEvent mergedValue)
        where TEvent : struct
    {
        return EventMetaDataHandler.TryMergePostEvent(in oldValue, in newValue, out mergedValue);
    }
}

public abstract class EventMetaData<TEvent> : IEventMetaData where TEvent : struct
{
    public int EventId => EventTypeId<TEvent>.Id;
    public virtual EventCategoryToken Category => EventCategoryToken.Empty;

    public EventIdentity GetIdentity()
    {
        return EventIdentityRegistry.GetOrCreate<TEvent>();
    }

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
    public virtual ActorMailOptions? ActorMailOptions => null;

    public EventPostPolicy? GetPostPolicy() => PostPolicy;
    public EventTimerPolicy? GetTimerPolicy() => TimerPolicy;
    public EventBufferPolicy? GetBufferPolicy() => BufferPolicy;
    public ActorMailOptions? GetActorMailOptions() => ActorMailOptions;

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