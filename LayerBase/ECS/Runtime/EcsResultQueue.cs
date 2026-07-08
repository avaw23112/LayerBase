using LayerBase.ECS.Runtime.Queues;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsResultQueue
{
    private readonly EcsResultBatchPool _batchPool = new(initialCapacity: 16);
    private readonly SpscRing<EcsResultBatch> _ring = new(4_096);
    private readonly Queue<EcsResultBatch> _overflow = new();
    private readonly object _overflowLock = new();
    private EcsResultBatch? _producerBatch;
    private EcsResultBatch? _drainBatch;

    public void Enqueue(IEcsResultItem item)
    {
        EcsResultBatch? batch = _producerBatch;
        if (batch != null)
        {
            batch.Add(item);
            return;
        }

        batch = _batchPool.Rent();
        batch.Add(item);
        Publish(batch);
    }

    public void BeginBatch()
    {
        if (_producerBatch != null)
        {
            return;
        }

        _producerBatch = _batchPool.Rent();
    }

    public void EndBatch()
    {
        EcsResultBatch? batch = _producerBatch;
        if (batch == null)
        {
            return;
        }

        _producerBatch = null;
        if (batch.Count == 0)
        {
            _batchPool.Return(batch);
            return;
        }

        Publish(batch);
    }

    private void Publish(EcsResultBatch batch)
    {
        if (_ring.TryEnqueue(batch))
        {
            return;
        }

        lock (_overflowLock)
        {
            _overflow.Enqueue(batch);
        }
    }

    public EcsDrainStats DrainToMainThread(LayerRuntime runtime, int maxCount)
    {
        int drained = 0;
        int failed = 0;

        while ((maxCount <= 0 || drained < maxCount) &&
               TryDequeueItem(out IEcsResultItem? item))
        {
            if (item == null)
            {
                continue;
            }

            try
            {
                item.Apply(runtime);
            }
            catch (Exception ex)
            {
                failed++;
                runtime.ReportLayerEventError(-1, "EcsResultQueue", item.DebugName, ex);
            }

            drained++;
        }

        return new EcsDrainStats(drained, failed);
    }

    public void Clear()
    {
        if (_producerBatch != null)
        {
            _batchPool.Return(_producerBatch, disposeItems: true);
            _producerBatch = null;
        }

        if (_drainBatch != null)
        {
            _batchPool.Return(_drainBatch, disposeItems: true);
            _drainBatch = null;
        }

        while (TryDequeueBatch(out EcsResultBatch? batch))
        {
            if (batch != null)
            {
                _batchPool.Return(batch, disposeItems: true);
            }
        }
    }

    private bool TryDequeueItem(out IEcsResultItem? item)
    {
        while (true)
        {
            EcsResultBatch? batch = _drainBatch;
            if (batch != null && batch.TryDequeue(out item))
            {
                if (batch.RemainingCount == 0)
                {
                    _batchPool.Return(batch);
                    _drainBatch = null;
                }

                return true;
            }

            if (batch != null)
            {
                _batchPool.Return(batch);
                _drainBatch = null;
            }

            if (!TryDequeueBatch(out batch))
            {
                item = null;
                return false;
            }

            _drainBatch = batch;
        }
    }

    private bool TryDequeueBatch(out EcsResultBatch? batch)
    {
        if (_ring.TryDequeue(out batch))
        {
            return true;
        }

        lock (_overflowLock)
        {
            if (_overflow.Count == 0)
            {
                batch = null;
                return false;
            }

            batch = _overflow.Dequeue();
            return true;
        }
    }
}
