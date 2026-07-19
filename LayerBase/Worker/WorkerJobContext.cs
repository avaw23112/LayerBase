namespace LayerBase.Worker;

public readonly struct WorkerJobContext
{
    internal WorkerJobContext(int executionLaneId, CancellationToken cancellationToken)
    {
        ExecutionLaneId = executionLaneId;
        CancellationToken = cancellationToken;
    }

    public int ExecutionLaneId { get; }

    public bool IsCancellationRequested => CancellationToken.IsCancellationRequested;

    public CancellationToken CancellationToken { get; }
}
