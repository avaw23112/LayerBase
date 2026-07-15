namespace LayerBase.Worker;

public readonly struct WorkerJobFailedEvent
{
    internal WorkerJobFailedEvent(
        WorkerHandle handle,
        WorkerJobFailureKind kind,
        WorkerJobExceptionInfo error)
    {
        Handle = handle;
        Kind = kind;
        Error = error;
    }

    public WorkerHandle Handle { get; }

    public WorkerJobFailureKind Kind { get; }

    public WorkerJobExceptionInfo Error { get; }
}
