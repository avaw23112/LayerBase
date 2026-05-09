namespace LayerBase.Core.Event;

public enum PostFailureKind
{
    None = 0,
    Unknown = 1,
    InvalidActorId = 2,
    UnsupportedEvent = 3,
    MailboxFull = 4,
    DisabledActor = 5,
    PendingDestroy = 6,
    Destroying = 7
}
