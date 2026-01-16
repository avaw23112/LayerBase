using LayerBase.Core.EventCatalogue;

namespace LayerBase.Core.EventStateTrace;

public delegate void EventCompletedHandler(
    ref EventState state
);

public delegate void ClassicEventCompletedHandler(
    ref EventCategoryToken eventCategoryToken,ref EventState state 
);

public delegate void ClassicEventCreatedHandler(
    ref EventCategoryToken eventCategoryToken,ref EventState state
);
public class EventStateHandler
{
    
}