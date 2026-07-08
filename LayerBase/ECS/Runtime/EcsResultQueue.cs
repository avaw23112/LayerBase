using System.Collections.Concurrent;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsResultQueue
{
    private readonly ConcurrentQueue<IEcsResultItem> _queue = new();

    public void Enqueue(IEcsResultItem item)
    {
        _queue.Enqueue(item);
    }

    public EcsDrainStats DrainToMainThread(LayerRuntime runtime, int maxCount)
    {
        int drained = 0;
        int failed = 0;

        while ((maxCount <= 0 || drained < maxCount) &&
               _queue.TryDequeue(out IEcsResultItem? item))
        {
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
        while (_queue.TryDequeue(out IEcsResultItem? item))
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
