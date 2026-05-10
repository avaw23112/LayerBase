namespace LayerBase.Actor;

internal enum ActorPostRouteKind : byte
{
    QueuedGrow = 0,
    QueuedRejectNew = 1,
    QueuedDropOldest = 2,
    Latest = 3,
    Dirty = 4,
    Disabled = 5,
    DiagnosticOnly = 6
}
