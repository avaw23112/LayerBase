namespace LayerBase.Event.EventMetaData;

public static class EventMetaDataRegistry
{
    public static void RegisterMetaData<EventType>(IEventMetaData metaData) where EventType : struct
    {
        if (metaData == null) throw new ArgumentNullException(nameof(metaData));
        EventMetaDataHandler.RegisterMetaData<EventType>(metaData);
    }

    public static Actor.ActorMailOptions? GetActorMailOptions<TEvent>() where TEvent : struct
    {
        return EventMetaDataHandler.GetActorMailOptions<TEvent>();
    }
}