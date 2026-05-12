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