using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Actor;

internal static class ActorEventPostPlanBuilder
{
    public static ActorEventPostPlan<TEvent> Build<TEvent>(ActorMailOptions worldDefaultMailOptions)
        where TEvent : struct
    {
        EventMetaData<TEvent>? metaData = EventMetaDataHandler.ResolveRegisteredMetaData<TEvent>();
        ActorMailOptions mailOptions = metaData?.GetActorMailOptions() ?? worldDefaultMailOptions;

        return new ActorEventPostPlan<TEvent>(
            eventId: EventTypeId<TEvent>.Id,
            mailOptions: mailOptions);
    }
}