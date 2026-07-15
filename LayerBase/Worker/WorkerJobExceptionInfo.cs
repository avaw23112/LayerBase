namespace LayerBase.Worker;

public readonly struct WorkerJobExceptionInfo
{
    public WorkerJobExceptionInfo(string typeName, string message, string? stackTrace)
    {
        TypeName = typeName ?? string.Empty;
        Message = message ?? string.Empty;
        StackTrace = stackTrace;
    }

    public string TypeName { get; }

    public string Message { get; }

    public string? StackTrace { get; }

    public static WorkerJobExceptionInfo None => new(string.Empty, string.Empty, null);

    internal static WorkerJobExceptionInfo FromException(Exception exception)
    {
        return new WorkerJobExceptionInfo(
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message,
            exception.StackTrace);
    }
}
