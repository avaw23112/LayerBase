namespace LayerBase.Actor;

internal enum ActorMailWriteMode : byte
{
    General = 0,
    QueuedGrow = 1,
    Latest = 2,
    Dirty = 3,
    Coalesced = 4
}
