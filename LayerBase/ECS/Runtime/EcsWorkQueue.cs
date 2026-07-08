using LayerBase.ECS.Runtime.Queues;
using LayerBase.ECS.Runtime.Submission;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsWorkQueue
{
    private readonly SpscRing<EcsSubmissionBatch> _ring = new(1024);
    private readonly Queue<EcsSubmissionBatch> _overflow = new();
    private readonly object _overflowLock = new();
    private int _pendingBatches;

    public int Count => Volatile.Read(ref _pendingBatches);

    public void Enqueue(EcsSubmissionBatch batch)
    {
        Interlocked.Increment(ref _pendingBatches);

        if (_ring.TryEnqueue(batch))
        {
            return;
        }

        lock (_overflowLock)
        {
            _overflow.Enqueue(batch);
        }
    }

    public bool TryDequeue(out EcsSubmissionBatch? batch)
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

    public void MarkCompleted()
    {
        Interlocked.Decrement(ref _pendingBatches);
    }

    public void Clear()
    {
        while (_ring.TryDequeue(out _))
        {
        }

        lock (_overflowLock)
        {
            _overflow.Clear();
        }

        Volatile.Write(ref _pendingBatches, 0);
    }
}
