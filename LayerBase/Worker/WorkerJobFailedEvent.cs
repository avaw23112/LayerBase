using LayerBase.Core;

namespace LayerBase.Worker;

public readonly struct WorkerJobFailedEvent : ILayerDto
{
    public WorkerJobFailedEvent(WorkerHandle handle, Type jobType, Exception exception)
    {
        Handle = handle;
        JobType = jobType;
        Exception = exception;
    }

    public WorkerHandle Handle { get; }

    public Type JobType { get; }

    public Exception Exception { get; }
}
