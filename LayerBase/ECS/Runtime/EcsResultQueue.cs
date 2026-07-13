using LayerBase.ECS.Runtime.Queues;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsResultQueue
{
    private const int DefaultRingCapacity = 4096;
    private const int DefaultOverflowCapacity = 4096;
    private const int DefaultBatchCapacity = 16;

    private readonly EcsResultBatchPool _batchPool;
    private readonly SpscRing<EcsResultBatch> _ring;
    private readonly Queue<EcsResultBatch> _overflow;
    private readonly object _overflowLock = new();
    private readonly int _overflowCapacity;
    private EcsResultBatch? _producerBatch;
    private EcsResultBatch? _drainBatch;
    private bool _closed;

    internal Action? AfterProducerAcceptedForTest;

    public EcsResultQueue(
        int ringCapacity = DefaultRingCapacity,
        int overflowCapacity = DefaultOverflowCapacity,
        int batchCapacity = DefaultBatchCapacity)
    {
        _batchPool = new EcsResultBatchPool(initialCapacity: batchCapacity);
        _ring = new SpscRing<EcsResultBatch>(ringCapacity);
        _overflowCapacity = Math.Max(0, overflowCapacity);
        _overflow = new Queue<EcsResultBatch>(_overflowCapacity);
    }

    public bool Enqueue(IEcsResultItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (IsClosed)
        {
            DisposeResultItem(item);
            return false;
        }

        EcsResultBatch? batch = _producerBatch;
        if (batch != null)
        {
            batch.Add(item);
            return true;
        }

        batch = _batchPool.Rent();
        batch.Add(item);
        return Publish(batch);
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

        _ = Publish(batch);
    }

    private bool Publish(EcsResultBatch batch)
    {
        lock (_overflowLock)
        {
            if (_closed)
            {
                _batchPool.Return(batch, disposeItems: true);
                return false;
            }

            AfterProducerAcceptedForTest?.Invoke();

            if (_ring.TryEnqueue(batch))
            {
                return true;
            }

            if (_overflow.Count >= _overflowCapacity)
            {
                _batchPool.Return(batch, disposeItems: true);
                return false;
            }

            _overflow.Enqueue(batch);
            return true;
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

    public void Close()
    {
        lock (_overflowLock)
        {
            _closed = true;
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
        lock (_overflowLock)
        {
            if (_ring.TryDequeue(out batch))
            {
                return true;
            }

            if (_overflow.Count == 0)
            {
                batch = null;
                return false;
            }

            batch = _overflow.Dequeue();
            return true;
        }
    }

    private bool IsClosed
    {
        get
        {
            lock (_overflowLock) return _closed;
        }
    }

    private static void DisposeResultItem(IEcsResultItem item)
    {
        if (item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
