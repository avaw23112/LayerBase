namespace LayerBase.Actor;

internal abstract class ActorEventColumnRuntime
{
    private DirtyBucketList? _dirtyBuckets;
    private int _bucketIndex;

    internal void BindDirtyBucket(DirtyBucketList dirtyBuckets, int bucketIndex)
    {
        _dirtyBuckets = dirtyBuckets;
        _bucketIndex = bucketIndex;
    }

    protected void NotifyBucketDirty()
    {
        _dirtyBuckets?.Mark(_bucketIndex);
    }

    public abstract ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats);

    public abstract bool HasPendingWork();

    public abstract void EnsureSlotCapacity(int slotIndex);

    public abstract void RefreshPostRowBinding();

    public abstract void ClearMail(int slotIndex);

    public abstract int GetPendingCount(int slotIndex);

    public abstract int GetTotalPendingCount();
}

internal abstract class ActorCallColumnRuntime
{
    private DirtyBucketList? _dirtyBuckets;
    private int _bucketIndex;

    internal void BindDirtyBucket(DirtyBucketList dirtyBuckets, int bucketIndex)
    {
        _dirtyBuckets = dirtyBuckets;
        _bucketIndex = bucketIndex;
    }

    protected void NotifyBucketDirty()
    {
        _dirtyBuckets?.Mark(_bucketIndex);
    }

    public abstract ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats);

    public abstract bool HasPendingWork();

    public abstract void EnsureSlotCapacity(int slotIndex);

    public abstract void ClearMail(int slotIndex);

    public abstract int GetPendingCount(int slotIndex);

    public abstract int GetTotalPendingCount();
}
