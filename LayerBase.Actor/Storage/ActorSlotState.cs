namespace LayerBase.Actor;

public enum ActorSlotState : byte
{
    Empty = 0,
    Alive = 1,
    PendingDestroy = 2,
    Destroying = 3
}
