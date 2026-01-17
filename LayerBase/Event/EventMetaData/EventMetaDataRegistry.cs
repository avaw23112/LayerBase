using System;

namespace LayerBase.Event.EventMetaData;

/// <summary>
/// Public entry point for registering event metadata discovered by the source generator.
/// </summary>
public static class EventMetaDataRegistry
{
    public static void RegisterMetaData<EventType>(IEventMetaData metaData) where EventType : struct
    {
        if (metaData == null) throw new ArgumentNullException(nameof(metaData));
        EventMetaDataHandler.RegisterMetaData<EventType>(metaData);
    }
}
