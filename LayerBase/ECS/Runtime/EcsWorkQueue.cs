using LayerBase.ECS.Runtime.Queues;
using LayerBase.ECS.Runtime.Submission;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsWorkQueue
{
    private const int DefaultRingCapacity = 1024;
    private const int DefaultOverflowCapacity = 1024;

    private readonly SpscRing<EcsSubmissionBatch> _ring;
    private readonly Queue<EcsSubmissionBatch> _overflow;
    private readonly object _overflowLock = new();
    private readonly int _overflowCapacity;
    private int _pendingBatches;
    private long _completedSequence;
    private bool _closed;

    public EcsWorkQueue(
        int ringCapacity = DefaultRingCapacity,
        int overflowCapacity = DefaultOverflowCapacity)
    {
        _ring = new SpscRing<EcsSubmissionBatch>(ringCapacity);
        _overflowCapacity = Math.Max(0, overflowCapacity);
        _overflow = new Queue<EcsSubmissionBatch>(_overflowCapacity);
    }

    public int Count => Volatile.Read(ref _pendingBatches);

    public long CompletedSequence => Volatile.Read(ref _completedSequence);

    public bool TryEnqueue(EcsSubmissionBatch batch)
    {
        if (batch == null) throw new ArgumentNullException(nameof(batch));

        lock (_overflowLock)
        {
            if (_closed)
            {
                return false;
            }
        }

        if (_ring.TryEnqueue(batch))
        {
            Interlocked.Increment(ref _pendingBatches);
            return true;
        }

        lock (_overflowLock)
        {
            if (_closed || _overflow.Count >= _overflowCapacity)
            {
                return false;
            }

            _overflow.Enqueue(batch);
            Interlocked.Increment(ref _pendingBatches);
            return true;
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

    public void MarkCompleted(long sequence)
    {
        Interlocked.Decrement(ref _pendingBatches);
        if (sequence > 0)
        {
            PublishCompletedSequence(sequence);
        }
    }

    public void Close()
    {
        lock (_overflowLock)
        {
            _closed = true;
        }
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

    public List<EcsSubmissionBatch> DetachAll()
    {
        var batches = new List<EcsSubmissionBatch>();
        long maxSequence = 0;

        while (_ring.TryDequeue(out EcsSubmissionBatch? batch))
        {
            if (batch != null)
            {
                batches.Add(batch);
                if (batch.Sequence > maxSequence)
                {
                    maxSequence = batch.Sequence;
                }
            }
        }

        lock (_overflowLock)
        {
            while (_overflow.Count > 0)
            {
                EcsSubmissionBatch batch = _overflow.Dequeue();
                batches.Add(batch);
                if (batch.Sequence > maxSequence)
                {
                    maxSequence = batch.Sequence;
                }
            }
        }

        Volatile.Write(ref _pendingBatches, 0);
        PublishCompletedSequence(maxSequence);
        return batches;
    }

    private void PublishCompletedSequence(long sequence)
    {
        if (sequence <= 0)
        {
            return;
        }

        while (true)
        {
            long current = Volatile.Read(ref _completedSequence);
            if (current >= sequence)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _completedSequence, sequence, current) == current)
            {
                return;
            }
        }
    }
}
