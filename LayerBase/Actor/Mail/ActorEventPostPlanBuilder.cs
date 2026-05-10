using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Actor;

internal static class ActorEventPostPlanBuilder
{
    public static ActorEventPostPlan<TEvent> Build<TEvent>(ActorMailOptions worldDefaultMailOptions)
        where TEvent : struct
    {
        EventMetaData<TEvent>? metaData = EventMetaDataHandler.ResolveRegisteredMetaData<TEvent>();
        ActorMailOptions mailOptions = metaData?.GetActorMailOptions() ?? worldDefaultMailOptions;
        ActorPostRouteKind route = ResolveRoute(mailOptions);
        ActorSlotFlags rejectMask = ActorSlotFlags.PendingDestroy | ActorSlotFlags.Destroying;
        bool rejectDisabled = mailOptions.DisabledPolicy == ActorMailDisabledPolicy.Reject;

        return new ActorEventPostPlan<TEvent>(
            eventId: EventTypeId<TEvent>.Id,
            identity: metaData?.GetIdentity() ?? EventIdentityRegistry.GetOrCreate<TEvent>(),
            category: metaData?.GetEventCategoryToken() ?? EventCategoryToken.Empty,
            route: route,
            mailOptions: mailOptions,
            rejectMask: rejectMask,
            rejectDisabled: rejectDisabled,
            metaData: metaData);
    }

    private static ActorPostRouteKind ResolveRoute(ActorMailOptions options)
    {
        return options.PostPolicy switch
        {
            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.Grow
                => ActorPostRouteKind.QueuedGrow,
            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.RejectNew
                => ActorPostRouteKind.QueuedRejectNew,
            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.DropOldest
                => ActorPostRouteKind.QueuedDropOldest,
            ActorPostPolicy.Latest
                => ActorPostRouteKind.Latest,
            ActorPostPolicy.Dirty
                => ActorPostRouteKind.Dirty,
            _ => ActorPostRouteKind.DiagnosticOnly
        };
    }
}
