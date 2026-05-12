namespace LayerBase.Actor;

internal enum ActorPostRouteCode
{
    QueuedGrow,
    QueuedRejectNew,
    QueuedDropOldest,
    Latest,
    Dirty,
    Disabled
}