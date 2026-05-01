namespace LayerBase.Core.Event;

public readonly struct PostResult
{
    public readonly bool IsSuccess;
    public readonly string? ErrorMessage;

    public PostResult(bool isSuccess, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static PostResult Success => new(true);
    public static PostResult Failure(string message) => new(false, message);
}
