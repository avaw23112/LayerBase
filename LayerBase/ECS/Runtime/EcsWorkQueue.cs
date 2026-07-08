using System.Collections.Concurrent;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsWorkQueue
{
    private readonly ConcurrentQueue<IEcsWorkItem> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(IEcsWorkItem item)
    {
        _queue.Enqueue(item);
    }

    public bool TryDequeue(out IEcsWorkItem? item)
    {
        return _queue.TryDequeue(out item);
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out _))
        {
        }
    }
}
