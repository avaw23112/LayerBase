namespace LayerBase.Actor;

[Flags]
internal enum ActorSlotFlags : byte
{
    None = 0,
    Alive = 1 << 0,
    Enabled = 1 << 1,
    PendingDestroy = 1 << 2,
    Destroying = 1 << 3
}
