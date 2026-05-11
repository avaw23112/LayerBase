namespace LayerBase.Actor;

public enum ActorPostStatus
{
    Success = 0,
    ActorNotFound = 1,
    ActorNotAlive = 2,
    ActorDisabledRejected = 3,
    ActorPendingDestroy = 4,
    MailFullRejected = 5,
    EventNotSupported = 6,
    MergeFailed = 7,
    PhysicalTargetInvalid = 8,
    RejectedByPostableStamp = 9
}
