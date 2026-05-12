namespace LayerBase.Actor;

internal sealed class ActorEventBucket<TEvent> : IActorEventBucket
    where TEvent : struct
{
    private ActorEventColumnRuntime[] _columns = Array.Empty<ActorEventColumnRuntime>();
    private int _count;
    private int _cursor;

    public void AddColumn(ActorEventColumnRuntime column)
    {
        EnsureCapacity(_count + 1);
        _columns[_count] = column;
        _count++;
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
                result: PumpOneResult.BucketLimited);
        }

        int totalProcessed = 0;
        int checkedCount = 0;
        bool actorLimited = false;

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

                return ActorPumpManyResult.ProcessedBatch(totalProcessed);
            }

            if (result.Result == PumpOneResult.ActorLimited)
            {
                actorLimited = true;
            }

            if (result.Result == PumpOneResult.BucketLimited)
            {
                return result;
            }
        }

        if (totalProcessed > 0)
        {
            return ActorPumpManyResult.ProcessedBatch(totalProcessed);
        }

        return actorLimited
            ? new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.ActorLimited)
            : ActorPumpManyResult.NoWork();
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