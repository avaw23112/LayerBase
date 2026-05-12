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
        ActorPostRouteCode routeCode = ResolveRouteCode(mailOptions);

        return new ActorEventPostPlan<TEvent>(
            eventId: EventTypeId<TEvent>.Id,
            routeCode: routeCode,
            mailOptions: mailOptions);
    }

    private static ActorPostRouteCode ResolveRouteCode(ActorMailOptions options)
    {
        return options.PostPolicy switch
               {
                   ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.Grow
                       => ActorPostRouteCode.QueuedGrow,
                   ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.RejectNew
                       => ActorPostRouteCode.QueuedRejectNew,
                   ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.DropOldest
                       => ActorPostRouteCode.QueuedDropOldest,
                   ActorPostPolicy.Latest
                       => ActorPostRouteCode.Latest,
                   ActorPostPolicy.Dirty
                       => ActorPostRouteCode.Dirty,
                   _ => ActorPostRouteCode.Disabled
               };
    }
}