using System.Collections.Concurrent;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsResultBatchPool
{
    private readonly ConcurrentBag<EcsResultBatch> _pool = new();
    private readonly int _initialCapacity;
    private readonly int _maxRetained;
    private int _count;

    public EcsResultBatchPool(int initialCapacity, int maxRetained = 1024)
    {
        _initialCapacity = Math.Max(1, initialCapacity);
        _maxRetained = Math.Max(0, maxRetained);
    }

    public int Count => Volatile.Read(ref _count);

    public EcsResultBatch Rent()
    {
        if (_pool.TryTake(out EcsResultBatch? batch))
        {
            Interlocked.Decrement(ref _count);
            return batch;
        }

        return new EcsResultBatch(_initialCapacity);
    }

    public void Return(EcsResultBatch batch, bool disposeItems = false)
    {
        batch.Clear(disposeItems);
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
