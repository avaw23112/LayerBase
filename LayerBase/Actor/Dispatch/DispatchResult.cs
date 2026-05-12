namespace LayerBase.Actor;

public readonly struct DispatchResult
{
    public readonly bool IsSuccess;
    public readonly DispatchFailureKind FailureKind;
    public readonly string? Message;
    public readonly Exception? Exception;

    private DispatchResult(
        bool                isSuccess,
        DispatchFailureKind failureKind,
        string?             message,
        Exception?          exception)
    {
        IsSuccess = isSuccess;
        FailureKind = failureKind;
        Message = message;
        Exception = exception;
    }

    public static DispatchResult Success()
    {
        return new(true, DispatchFailureKind.None, null, null);
    }

    public static DispatchResult Failure(
        DispatchFailureKind failureKind,
        string              message,
        Exception?          exception = null)
    {
        return new(false, failureKind, message, exception);
    }
}