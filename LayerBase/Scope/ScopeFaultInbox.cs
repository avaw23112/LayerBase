using System.Collections.Concurrent;

namespace LayerBase.Scope;

internal readonly record struct ScopeFaultKey(
    int SourceScopeId,
    ScopeFaultPhase Phase,
    Type ExceptionType);

internal sealed class ScopeFaultInbox
{
    private readonly ConcurrentDictionary<ScopeFaultKey, MergedFaultEntry> _faults = new();
    private readonly ConcurrentQueue<ScopeFaultKey> _order = new();
    private readonly int _maxCapacity;
    private int _droppedCount;
    private int _capacityExceededCount;

    public ScopeFaultInbox(int maxCapacity = 64)
    {
        _maxCapacity = maxCapacity > 0 ? maxCapacity : 64;
    }

    public int DroppedCount => Volatile.Read(ref _droppedCount);

    public int CapacityExceededCount => Volatile.Read(ref _capacityExceededCount);

    public int Count => _faults.Count;

    public bool TryEnqueue(in ScopeFaultRecord record)
    {
        var key = new ScopeFaultKey(
            record.SourceScopeId,
            record.Phase,
            record.Exception.GetType());

        if (_faults.TryGetValue(key, out var existing))
        {
            existing.MergeCount++;
            _faults[key] = existing;
            return true;
        }

        if (_faults.Count >= _maxCapacity)
        {
            Interlocked.Increment(ref _droppedCount);
            return false;
        }

        var entry = new MergedFaultEntry(record, 1);
        if (_faults.TryAdd(key, entry))
        {
            _order.Enqueue(key);
            return true;
        }

        return false;
    }

    public bool TryDequeue(out ScopeFaultRecord record)
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

    public void Clear()
    {
        _faults.Clear();
        while (_order.TryDequeue(out _)) { }
        _droppedCount = 0;
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
