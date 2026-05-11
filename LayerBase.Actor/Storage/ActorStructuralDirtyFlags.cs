namespace LayerBase.Actor;

[Flags]
internal enum ActorStructuralDirtyFlags : byte
{
    None = 0,
    PendingDestroy = 1 << 0,
    EnableChanged = 1 << 1,
    SlotRecycle = 1 << 2,
    QueryInvalidated = 1 << 3
}
