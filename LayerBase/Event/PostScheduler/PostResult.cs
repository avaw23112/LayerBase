namespace LayerBase.Core.Event;

public readonly struct PostResult
{
    public readonly bool IsSuccess;
    public readonly string? ErrorMessage;
    internal readonly bool CountsAsPending;

    public PostResult(bool isSuccess, string? errorMessage = null, bool countsAsPending = true)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        CountsAsPending = isSuccess && countsAsPending;
    }

    public static PostResult Success => new(true);
    public static PostResult Failure(string message) => new(false, message);

    public static PostResult Enqueued() => new(true, countsAsPending: true);
    public static PostResult Coalesced() => new(true, countsAsPending: false);
    internal static PostResult Dropped() => new(true, countsAsPending: false);
}
