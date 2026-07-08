using System.Collections.Concurrent;

namespace LayerBase.ECS.Runtime.Submission;

internal sealed class EcsSubmissionBatchPool
{
    private readonly ConcurrentBag<EcsSubmissionBatch> _pool = new();
    private readonly int _initialBatchCapacity;

    public EcsSubmissionBatchPool(int initialBatchCapacity)
    {
        _initialBatchCapacity = Math.Max(1, initialBatchCapacity);
    }

    public EcsSubmissionBatch Rent()
    {
        return _pool.TryTake(out EcsSubmissionBatch? batch)
            ? batch
            : new EcsSubmissionBatch(_initialBatchCapacity);
    }

    public void Return(EcsSubmissionBatch batch)
    {
        batch.Clear();
        _pool.Add(batch);
    }
}
