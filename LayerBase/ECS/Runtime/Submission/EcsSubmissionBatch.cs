namespace LayerBase.ECS.Runtime.Submission;

internal sealed class EcsSubmissionBatch
{
    private EcsSubmissionEntry[] _entries;
    private int _count;

    public EcsSubmissionBatch(int capacity)
    {
        _entries = new EcsSubmissionEntry[Math.Max(1, capacity)];
        JobArena = new EcsJobArena(capacity * 64);
    }

    public EcsJobArena JobArena { get; }

    public int Count => _count;

    public long Sequence { get; set; }

    public void Add(IEcsWorkItem item)
    {
        AddEntry(EcsSubmissionEntry.FromItem(item));
    }

    public void AddRecord(in EcsWorkRecord record)
    {
        AddEntry(EcsSubmissionEntry.FromRecord(in record));
    }

    public void EnsureCapacity(int entryCapacity, int jobArenaCapacity)
    {
        if (entryCapacity > _entries.Length)
        {
            Array.Resize(ref _entries, entryCapacity);
        }

        JobArena.EnsureCapacity(jobArenaCapacity);
    }

    private void AddEntry(in EcsSubmissionEntry entry)
    {
        int index = _count;
        if ((uint)index >= (uint)_entries.Length)
        {
            Array.Resize(ref _entries, _entries.Length * 2);
        }

        _entries[index] = entry;
        _count = index + 1;
    }

    public ReadOnlySpan<EcsSubmissionEntry> AsSpan()
    {
        return _entries.AsSpan(0, _count);
    }

    public void Clear()
    {
        Array.Clear(_entries, 0, _count);
        JobArena.Reset();
        _count = 0;
        Sequence = 0;
    }
}
