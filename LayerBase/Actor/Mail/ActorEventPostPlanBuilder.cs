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
        bool rejectDisabled = mailOptions.DisabledPolicy == ActorMailDisabledPolicy.Reject;
        bool requirePostableStamp = ResolveRequirePostableStamp(mailOptions);
        byte routeCode = ResolveRouteCode(mailOptions, requirePostableStamp);

        return new ActorEventPostPlan<TEvent>(
            eventId: EventTypeId<TEvent>.Id,
            routeCode: routeCode,
            mailOptions: mailOptions,
            requirePostableStamp: requirePostableStamp,
            rejectDisabled: rejectDisabled);
    }

    private static bool ResolveRequirePostableStamp(ActorMailOptions options)
    {
        return options.DisabledPolicy == ActorMailDisabledPolicy.Reject;
    }

    private static byte ResolveRouteCode(
        ActorMailOptions options,
        bool requirePostableStamp)
    {
        byte validation = requirePostableStamp
            ? ActorPostRouteCode.ValidationPostableStamp
            : ActorPostRouteCode.ValidationPhysicalSafe;

        byte writeMode = options.PostPolicy switch
        {
            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.Grow
                => ActorPostRouteCode.WriteQueuedGrow,
            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.RejectNew
                => ActorPostRouteCode.WriteQueuedRejectNew,
            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.DropOldest
                => ActorPostRouteCode.WriteQueuedDropOldest,
            ActorPostPolicy.Latest
                => ActorPostRouteCode.WriteLatest,
            ActorPostPolicy.Dirty
                => ActorPostRouteCode.WriteDirty,
            _ => ActorPostRouteCode.WriteDisabled
        };

        if (writeMode == ActorPostRouteCode.WriteDisabled)
        {
            return ActorPostRouteCode.Disabled;
        }

        return (byte)(writeMode | validation);
    }
}
