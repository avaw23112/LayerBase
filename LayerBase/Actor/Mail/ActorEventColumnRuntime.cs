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
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats);

    /// <summary>
    /// 批量 Pump 当前 Column。
    ///
    /// 参数说明：
    /// budget：当前帧预算，包含事件数量预算和时间预算。
    /// options：邮箱 Pump 配置。
    /// stats：Pump 统计构建器。
    /// maxEvents：当前 Column 本次最多允许连续处理多少事件。
    ///
    /// 作用：
    /// 默认实现只调用一次 PumpOne，用于保持兼容。
    /// 真正的高性能 Column 可以 override 这个方法。
    /// </summary>
    public virtual ActorPumpManyResult PumpMany(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       maxEvents)
    {
        if (maxEvents <= 0)
        {
            return ActorPumpManyResult.NoWork();
        }

        ActorColumnPumpResult result = PumpOne(
            budget: ref budget,
            options: in options,
            stats: stats);

        if (result == ActorColumnPumpResult.Processed)
        {
            return ActorPumpManyResult.ProcessedBatch(
                processed: 1,
                hasMoreWork: HasPendingWork());
        }

        if (result == ActorColumnPumpResult.ActorLimited)
        {
            return new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.ActorLimited,
                hasMoreWork: true);
        }

        return ActorPumpManyResult.NoWork();
    }

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
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats);

    public abstract bool HasPendingWork();

    public abstract void EnsureSlotCapacity(int slotIndex);

    public abstract void ClearMail(int slotIndex);

    public abstract int GetPendingCount(int slotIndex);

    public abstract int GetTotalPendingCount();
}