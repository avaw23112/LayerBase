namespace LayerBase.Core.Event;

public readonly struct PostResult
{
    public readonly bool IsSuccess;
    public readonly string? ErrorMessage;
    public readonly PostFailureKind FailureKind;
    internal readonly bool CountsAsPending;

    public PostResult(
        bool isSuccess,
        string? errorMessage = null,
        bool countsAsPending = true,
        PostFailureKind failureKind = PostFailureKind.None)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        FailureKind = isSuccess ? PostFailureKind.None : failureKind;
        CountsAsPending = isSuccess && countsAsPending;
    }

    public static PostResult Success => new(true);
    public static PostResult Failure(string message, PostFailureKind failureKind = PostFailureKind.Unknown)
        => new(false, message, countsAsPending: true, failureKind: failureKind);

    public static PostResult Enqueued() => new(true, countsAsPending: true);
    public static PostResult Coalesced() => new(true, countsAsPending: false);
    internal static PostResult Dropped() => new(true, countsAsPending: false);
}
