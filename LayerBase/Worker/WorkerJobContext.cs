namespace LayerBase.Worker;

public readonly struct WorkerJobContext
{
    internal WorkerJobContext(int workerIndex, CancellationToken cancellationToken)
    {
        WorkerIndex = workerIndex;
        CancellationToken = cancellationToken;
    }

    public int WorkerIndex { get; }

    public bool IsCancellationRequested => CancellationToken.IsCancellationRequested;

    public CancellationToken CancellationToken { get; }
}
