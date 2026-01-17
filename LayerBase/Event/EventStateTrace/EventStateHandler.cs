using LayerBase.Core.EventCatalogue;

namespace LayerBase.Core.EventStateTrace;

public delegate void EventCompletedHandler(
    ref EventState state
);

public delegate void ClassifiedEventCompletedHandler(
    ref EventCategoryToken eventCategoryToken,ref EventState state 
);

public delegate void ClassifiedEventCreatedHandler(
    ref EventCategoryToken eventCategoryToken,ref EventState state
);
