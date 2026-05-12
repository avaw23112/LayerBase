namespace LayerBase.Actor;

public sealed class ActorCallException : InvalidOperationException
{
    public ActorCallFailureKind FailureKind { get; }
    public ActorId ActorId { get; }
    public bool HasActorId { get; }
    public Type? RequestType { get; }
    public Type? ResponseType { get; }

    public ActorCallException(
        ActorCallFailureKind failureKind,
        string?              message        = null,
        Exception?           innerException = null)
        : base(message ?? GetDefaultMessage(failureKind, null, null, false), innerException)
    {
        FailureKind = failureKind;
    }

    public ActorCallException(
        ActorCallFailureKind failureKind,
        ActorId              actorId,
        string?              message        = null,
        Exception?           innerException = null)
        : base(message ?? GetDefaultMessage(failureKind, null, null, true), innerException)
    {
        FailureKind = failureKind;
        ActorId = actorId;
        HasActorId = true;
    }

    public ActorCallException(
        ActorCallFailureKind failureKind,
        Type                 requestType,
        Type                 responseType,
        string?              message        = null,
        Exception?           innerException = null)
        : base(message ?? GetDefaultMessage(failureKind, requestType, responseType, false), innerException)
    {
        FailureKind = failureKind;
        RequestType = requestType;
        ResponseType = responseType;
    }

    private static string GetDefaultMessage(
        ActorCallFailureKind failureKind,
        Type?                requestType,
        Type?                responseType,
        bool                 hasActorId)
    {
        return failureKind switch
               {
                   ActorCallFailureKind.InvalidActorId => "Actor call failed because the ActorId is invalid.",
                   ActorCallFailureKind.ActorNotFound => hasActorId
                       ? "Actor call failed because the target actor no longer exists."
                       : "Actor call failed because the target actor was not found.",
                   ActorCallFailureKind.PendingDestroy =>
                       "Actor call failed because the target actor is pending destroy.",
                   ActorCallFailureKind.UnsupportedRequest =>
                       $"Actor call route is not supported for request '{requestType?.Name}' and response '{responseType?.Name}'.",
                   ActorCallFailureKind.MailboxFull => "Actor call failed because the actor mailbox is full.",
                   ActorCallFailureKind.Disposed    => "Actor call failed because the ActorWorld is disposed.",
                   _                                => "Actor call failed."
               };
    }
}