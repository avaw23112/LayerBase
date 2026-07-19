using System.Collections.Concurrent;

namespace LayerBase.Scope;

internal readonly record struct ScopeFaultKey(
    int SourceScopeId,
    ScopeFaultPhase Phase,
    Type ExceptionType);

internal sealed class ScopeFaultInbox
{
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<ScopeFaultKey, MergedFaultEntry> _faults = new();
    private readonly ConcurrentQueue<ScopeFaultKey> _order = new();
    private readonly int _maxCapacity;
    private int _droppedCount;
    private int _capacityExceededCount;
    private int _mergedCount;
    private int _highWatermark;

    public ScopeFaultInbox(int maxCapacity = 64)
    {
        _maxCapacity = maxCapacity > 0 ? maxCapacity : 64;
    }

    public int DroppedCount => Volatile.Read(ref _droppedCount);

    public int CapacityExceededCount => Volatile.Read(ref _capacityExceededCount);

    public int MergedCount => Volatile.Read(ref _mergedCount);

    public int HighWatermark => Volatile.Read(ref _highWatermark);

    public int Count => _faults.Count;

    public bool TryEnqueue(in ScopeFaultRecord record)
    {
        var key = new ScopeFaultKey(
            record.SourceScopeId,
            record.Phase,
            record.Exception.GetType());

        lock (_gate)
        {
            if (_faults.TryGetValue(key, out var existing))
            {
                existing.MergeCount++;
                _faults[key] = existing;
                Interlocked.Increment(ref _mergedCount);
                return true;
            }

            if (_faults.Count >= _maxCapacity)
            {
                Interlocked.Increment(ref _droppedCount);
                Interlocked.Increment(ref _capacityExceededCount);
                return false;
            }

            var entry = new MergedFaultEntry(record, 1);
            _faults[key] = entry;
            _order.Enqueue(key);
            UpdateHighWatermark(_faults.Count);
            return true;
        }
    }

    public bool TryDequeue(out ScopeFaultRecord record)
    {
        lock (_gate)
        {
            while (_order.TryDequeue(out var key))
            {
                if (_faults.TryRemove(key, out var entry))
                {
                    record = entry.Record;
                    return true;
                }
            }

            record = default;
            return false;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _faults.Clear();
            while (_order.TryDequeue(out _)) { }
            _droppedCount = 0;
            _capacityExceededCount = 0;
            _mergedCount = 0;
            _highWatermark = 0;
        }
    }

    private void UpdateHighWatermark(int count)
    {
        int current;
        do
        {
            current = Volatile.Read(ref _highWatermark);
            if (count <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref _highWatermark, count, current) != current);
    }

    internal struct MergedFaultEntry
    {
        public ScopeFaultRecord Record;
        public int MergeCount;

        public MergedFaultEntry(ScopeFaultRecord record, int mergeCount)
        {
            Record = record;
            MergeCount = mergeCount;
        }
    }
}
