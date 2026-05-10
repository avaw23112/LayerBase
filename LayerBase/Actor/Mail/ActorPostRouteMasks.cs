namespace LayerBase.Actor;

internal static class ActorPostRouteMasks
{
    public const uint QueuedRoutes =
        (1u << ActorPostRouteCode.QueuedGrowPhysicalSafe) |
        (1u << ActorPostRouteCode.QueuedGrowPostableStamp) |
        (1u << ActorPostRouteCode.QueuedGrowUnchecked) |
        (1u << ActorPostRouteCode.QueuedRejectNewPhysicalSafe) |
        (1u << ActorPostRouteCode.QueuedRejectNewPostableStamp) |
        (1u << ActorPostRouteCode.QueuedRejectNewUnchecked) |
        (1u << ActorPostRouteCode.QueuedDropOldestPhysicalSafe) |
        (1u << ActorPostRouteCode.QueuedDropOldestPostableStamp) |
        (1u << ActorPostRouteCode.QueuedDropOldestUnchecked);

    public const uint StampRoutes =
        (1u << ActorPostRouteCode.QueuedGrowPostableStamp) |
        (1u << ActorPostRouteCode.QueuedRejectNewPostableStamp) |
        (1u << ActorPostRouteCode.QueuedDropOldestPostableStamp) |
        (1u << ActorPostRouteCode.LatestPostableStamp) |
        (1u << ActorPostRouteCode.DirtyPostableStamp);

    public const uint UncheckedRoutes =
        (1u << ActorPostRouteCode.QueuedGrowUnchecked) |
        (1u << ActorPostRouteCode.QueuedRejectNewUnchecked) |
        (1u << ActorPostRouteCode.QueuedDropOldestUnchecked) |
        (1u << ActorPostRouteCode.LatestUnchecked) |
        (1u << ActorPostRouteCode.DirtyUnchecked);
}
