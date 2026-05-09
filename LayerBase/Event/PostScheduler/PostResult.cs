namespace LayerBase.Core.Event;

public readonly struct PostResult
{
    public readonly bool IsSuccess;
    public readonly string? ErrorMessage;
    public readonly PostFailureKind FailureKind;
    public readonly Actor.ActorPostStatus ActorStatus;
    internal readonly bool CountsAsPending;

    public PostResult(
        bool isSuccess,
        string? errorMessage = null,
        bool countsAsPending = true,
        PostFailureKind failureKind = PostFailureKind.None,
        Actor.ActorPostStatus actorStatus = Actor.ActorPostStatus.Success)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        FailureKind = isSuccess ? PostFailureKind.None : failureKind;
        ActorStatus = isSuccess ? Actor.ActorPostStatus.Success : actorStatus;
        CountsAsPending = isSuccess && countsAsPending;
    }

    public static PostResult Success => new(true);
    public static PostResult Failure(string message, PostFailureKind failureKind = PostFailureKind.Unknown)
        => new(false, message, countsAsPending: true, failureKind: failureKind);
    public static PostResult Failure(
        Actor.ActorPostStatus actorStatus,
        string message,
        PostFailureKind failureKind = PostFailureKind.Unknown)
        => new(false, message, countsAsPending: true, failureKind: failureKind, actorStatus: actorStatus);

    public static PostResult Enqueued() => new(true, countsAsPending: true);
    public static PostResult Coalesced() => new(true, countsAsPending: false);
    internal static PostResult Dropped() => new(true, countsAsPending: false);
}
