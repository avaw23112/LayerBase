using System.Collections.Concurrent;

namespace LayerBase.ECS.Runtime.Submission;

internal sealed class EcsSubmissionBatchPool
{
    private readonly ConcurrentBag<EcsSubmissionBatch> _pool = new();
    private readonly int _initialBatchCapacity;
    private readonly int _maxRetained;
    private int _count;

    public EcsSubmissionBatchPool(int initialBatchCapacity, int maxRetained = 1024)
    {
        _initialBatchCapacity = Math.Max(1, initialBatchCapacity);
        _maxRetained = Math.Max(0, maxRetained);
    }

    public int Count => Volatile.Read(ref _count);

    public EcsSubmissionBatch Rent()
    {
        if (_pool.TryTake(out EcsSubmissionBatch? batch))
        {
            Interlocked.Decrement(ref _count);
            return batch;
        }

        return new EcsSubmissionBatch(_initialBatchCapacity);
    }

    public void Return(EcsSubmissionBatch batch)
    {
        batch.Clear();
        if (_maxRetained == 0)
        {
            return;
        }

        if (Interlocked.Increment(ref _count) > _maxRetained)
        {
            Interlocked.Decrement(ref _count);
            return;
        }

        _pool.Add(batch);
    }
}
