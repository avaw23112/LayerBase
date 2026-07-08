using System.Collections.Concurrent;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsResultBatchPool
{
    private readonly ConcurrentBag<EcsResultBatch> _pool = new();
    private readonly int _initialCapacity;

    public EcsResultBatchPool(int initialCapacity)
    {
        _initialCapacity = Math.Max(1, initialCapacity);
    }

    public EcsResultBatch Rent()
    {
        return _pool.TryTake(out EcsResultBatch? batch)
            ? batch
            : new EcsResultBatch(_initialCapacity);
    }

    public void Return(EcsResultBatch batch, bool disposeItems = false)
    {
        batch.Clear(disposeItems);
        _pool.Add(batch);
    }
}
