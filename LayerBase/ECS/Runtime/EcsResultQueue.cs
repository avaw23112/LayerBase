using LayerBase.ECS.Runtime.Queues;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsResultQueue
{
    private readonly SpscRing<IEcsResultItem> _ring = new(16_384);
    private readonly Queue<IEcsResultItem> _overflow = new();
    private readonly object _overflowLock = new();

    public void Enqueue(IEcsResultItem item)
    {
        if (_ring.TryEnqueue(item))
        {
            return;
        }

        lock (_overflowLock)
        {
            _overflow.Enqueue(item);
        }
    }

    public EcsDrainStats DrainToMainThread(LayerRuntime runtime, int maxCount)
    {
        int drained = 0;
        int failed = 0;

        while ((maxCount <= 0 || drained < maxCount) &&
               TryDequeue(out IEcsResultItem? item))
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
        while (TryDequeue(out IEcsResultItem? item))
        {
            if (item == null)
            {
                continue;
            }

            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private bool TryDequeue(out IEcsResultItem? item)
    {
        if (_ring.TryDequeue(out item))
        {
            return true;
        }

        lock (_overflowLock)
        {
            if (_overflow.Count == 0)
            {
                item = null;
                return false;
            }

            item = _overflow.Dequeue();
            return true;
        }
    }
}
