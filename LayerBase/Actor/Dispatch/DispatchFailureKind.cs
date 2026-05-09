namespace LayerBase.Actor;

public enum DispatchFailureKind
{
    None = 0,
    InvalidActorId = 1,
    ActorNotFound = 2,
    PendingDestroy = 3,
    UnsupportedEvent = 4,
    HandlerException = 5
}
