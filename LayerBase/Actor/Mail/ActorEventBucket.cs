namespace LayerBase.Actor;

internal sealed class ActorEventBucket<TEvent> : IActorEventBucket
    where TEvent : struct
{
    private ActorEventColumnRuntime[] _columns = Array.Empty<ActorEventColumnRuntime>();
    private int _count;
    private int _cursor;

    /// <summary>
    /// 单 Column 快路径缓存。
    /// 当 _count == 1 时，_singleColumn 指向唯一的 Column。
    /// 当 _count > 1 时，_singleColumn 为 null。
    /// </summary>
    private ActorEventColumnRuntime? _singleColumn;

    public void AddColumn(ActorEventColumnRuntime column)
    {
        EnsureCapacity(_count + 1);
        _columns[_count] = column;
        _count++;

        // 更新单 Column 快路径缓存。
        if (_count == 1)
        {
            _singleColumn = column;
        }
        else
        {
            _singleColumn = null;
        }
    }

    public PumpOneResult PumpOne(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       bucketIndex)
    {
        if (_count == 0)
        {
            return PumpOneResult.NoWork;
        }

        if (!stats.CanProcessBucket(bucketIndex, options))
        {
            stats.BucketLimitHits++;
            return PumpOneResult.BucketLimited;
        }

        int checkedCount = 0;
        bool actorLimited = false;
        while (checkedCount < _count)
        {
            int index = _cursor;
            _cursor = index + 1 == _count ? 0 : index + 1;
            checkedCount++;

            ActorColumnPumpResult result = _columns[index].PumpOne(ref budget, options, stats);
            if (result == ActorColumnPumpResult.Processed)
            {
                stats.ProcessedTotal++;
                if (options.MaxMailsPerBucketPerPump > 0)
                {
                    stats.RecordBucketProcessed(bucketIndex);
                }

                return PumpOneResult.Processed;
            }

            if (result == ActorColumnPumpResult.ActorLimited)
            {
                actorLimited = true;
            }
        }

        return actorLimited
            ? PumpOneResult.ActorLimited
            : PumpOneResult.EmptyBucket;
    }

    public ActorPumpManyResult PumpMany(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       bucketIndex,
        int                       maxEvents)
    {
        if (_count == 0 || maxEvents <= 0)
        {
            return ActorPumpManyResult.NoWork();
        }

        if (!stats.CanProcessBucket(bucketIndex, options))
        {
            stats.BucketLimitHits++;

            return new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.BucketLimited,
                hasMoreWork: true);
        }

        // 单 Column 快路径：
        // 当 Bucket 只有一个 Column 时，跳过 cursor 轮转和 checkedCount 循环。
        if (_count == 1)
        {
            ActorPumpManyResult result = _singleColumn!.PumpMany(
                budget: ref budget,
                options: in options,
                stats: stats,
                maxEvents: maxEvents);

            if (result.Processed > 0)
            {
                stats.ProcessedTotal += result.Processed;

                if (options.MaxMailsPerBucketPerPump > 0)
                {
                    for (int i = 0; i < result.Processed; i++)
                    {
                        stats.RecordBucketProcessed(bucketIndex);
                    }
                }
            }

            return result;
        }

        // 多 Column 通用路径。
        int totalProcessed = 0;
        int checkedCount = 0;
        bool actorLimited = false;
        bool hasMoreWork = false;

        while (checkedCount < _count &&
               totalProcessed < maxEvents &&
               budget.HasRemainingEventBudget())
        {
            int index = _cursor;

            // 轮转 cursor，避免长期偏向某一个 column。
            _cursor = index + 1 == _count ? 0 : index + 1;
            checkedCount++;

            ActorEventColumnRuntime column = _columns[index];

            int remaining = maxEvents - totalProcessed;

            ActorPumpManyResult result = column.PumpMany(
                budget: ref budget,
                options: in options,
                stats: stats,
                maxEvents: remaining);

            if (result.Processed > 0)
            {
                totalProcessed += result.Processed;
                stats.ProcessedTotal += result.Processed;

                if (options.MaxMailsPerBucketPerPump > 0)
                {
                    for (int i = 0; i < result.Processed; i++)
                    {
                        stats.RecordBucketProcessed(bucketIndex);
                    }
                }

                // 如果当前 Column 仍有工作，标记 hasMoreWork。
                if (result.HasMoreWork)
                {
                    hasMoreWork = true;
                }

                return ActorPumpManyResult.ProcessedBatch(
                    processed: totalProcessed,
                    hasMoreWork: hasMoreWork || HasOtherColumnsPending(index));
            }

            if (result.Result == PumpOneResult.ActorLimited)
            {
                actorLimited = true;
            }

            if (result.Result == PumpOneResult.BucketLimited)
            {
                return new ActorPumpManyResult(
                    processed: result.Processed,
                    result: result.Result,
                    hasMoreWork: true);
            }
        }

        if (totalProcessed > 0)
        {
            return ActorPumpManyResult.ProcessedBatch(
                processed: totalProcessed,
                hasMoreWork: hasMoreWork);
        }

        return actorLimited
            ? new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.ActorLimited,
                hasMoreWork: true)
            : ActorPumpManyResult.NoWork();
    }

    /// <summary>
    /// 检查除指定索引外的其他 Column 是否有待处理工作。
    ///
    /// 参数说明：
    /// excludeIndex：要排除的 Column 索引。
    ///
    /// 作用：
    /// 在当前 Column 处理完后，检查其他 Column 是否仍有工作。
    /// 避免调用 HasPendingWork() 进行全量扫描。
    /// </summary>
    private bool HasOtherColumnsPending(int excludeIndex)
    {
        for (int i = 0; i < _count; i++)
        {
            if (i != excludeIndex && _columns[i].HasPendingWork())
            {
                return true;
            }
        }

        return false;
    }

    public bool HasPendingWork()
    {
        for (int i = 0; i < _count; i++)
        {
            if (_columns[i].HasPendingWork())
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _columns.Length)
        {
            return;
        }

        int newCapacity = _columns.Length == 0 ? 4 : _columns.Length;
        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        Array.Resize(ref _columns, newCapacity);
    }
}