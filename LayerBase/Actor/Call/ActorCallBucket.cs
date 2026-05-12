namespace LayerBase.Actor;

internal sealed class ActorCallBucket<TRequest, TResponse> : IActorEventBucket
    where TRequest : struct
    where TResponse : struct
{
    private ActorCallColumnRuntime[] _columns = Array.Empty<ActorCallColumnRuntime>();
    private int _count;
    private int _cursor;

    public void AddColumn(ActorCallColumnRuntime column)
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
        // Call bucket 涉及 request/response 语义，先保留旧路径。
        // 只调用一次 PumpOne，保持兼容。
        if (maxEvents <= 0)
        {
            return ActorPumpManyResult.NoWork();
        }

        PumpOneResult result = PumpOne(
            budget: ref budget,
            options: in options,
            stats: stats,
            bucketIndex: bucketIndex);

        if (result == PumpOneResult.Processed)
        {
            return ActorPumpManyResult.ProcessedBatch(1);
        }

        if (result == PumpOneResult.BucketLimited)
        {
            return new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.BucketLimited);
        }

        if (result == PumpOneResult.ActorLimited)
        {
            return new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.ActorLimited);
        }

        return ActorPumpManyResult.NoWork();
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