namespace LayerBase.Actor;

public enum ActorCallFailureKind
{
    None = 0,
    InvalidActorId = 1,
    ActorNotFound = 2,
    PendingDestroy = 3,
    UnsupportedRequest = 4,
    MailboxFull = 5,
    Disposed = 6
}
