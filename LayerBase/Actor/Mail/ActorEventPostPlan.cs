using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Actor;

internal readonly struct ActorEventPostPlan<TEvent>
    where TEvent : struct
{
    public readonly int EventId;
    public readonly EventIdentity Identity;
    public readonly EventCategoryToken Category;
    public readonly ActorPostRouteKind Route;
    public readonly ActorMailOptions MailOptions;
    public readonly ActorSlotFlags RejectMask;
    public readonly bool RejectDisabled;
    public readonly EventMetaData<TEvent>? MetaData;

    public ActorEventPostPlan(
        int eventId,
        EventIdentity identity,
        EventCategoryToken category,
        ActorPostRouteKind route,
        ActorMailOptions mailOptions,
        ActorSlotFlags rejectMask,
        bool rejectDisabled,
        EventMetaData<TEvent>? metaData)
    {
        EventId = eventId;
        Identity = identity;
        Category = category;
        Route = route;
        MailOptions = mailOptions;
        RejectMask = rejectMask;
        RejectDisabled = rejectDisabled;
        MetaData = metaData;
    }
}
