using Arch.Core;
using ArchQuery = Arch.Core.Query;

namespace LayerBase.ECS.Runtime.Submission;

internal readonly struct EcsWorkRecord
{
    public EcsWorkRecord(
        int executorId,
        ArchQuery query,
        object? predicate,
        int jobOffset)
    {
        ExecutorId = executorId;
        Query = query;
        Predicate = predicate;
        JobOffset = jobOffset;
    }

    public int ExecutorId { get; }

    public ArchQuery Query { get; }

    public object? Predicate { get; }

    public int JobOffset { get; }
}

internal readonly struct EcsSubmissionEntry
{
    private EcsSubmissionEntry(IEcsWorkItem? item, EcsWorkRecord record, bool isRecord)
    {
        Item = item;
        Record = record;
        IsRecord = isRecord;
    }

    public IEcsWorkItem? Item { get; }

    public EcsWorkRecord Record { get; }

    public bool IsRecord { get; }

    public static EcsSubmissionEntry FromItem(IEcsWorkItem item)
    {
        return new EcsSubmissionEntry(item, default, isRecord: false);
    }

    public static EcsSubmissionEntry FromRecord(in EcsWorkRecord record)
    {
        return new EcsSubmissionEntry(null, record, isRecord: true);
    }
}
