namespace LayerBase.Actor;

internal sealed class ActorEventBucket<TEvent> : IActorEventBucket
    where TEvent : struct
{
    private IActorEventColumn<TEvent>[] _columns = Array.Empty<IActorEventColumn<TEvent>>();
    private int _cursor;

    public void AddColumn(IActorEventColumn<TEvent> column)
    {
        int oldLength = _columns.Length;
        Array.Resize(ref _columns, oldLength + 1);
        _columns[oldLength] = column;
    }

    public PumpOneResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats,
        int bucketIndex)
    {
        if (_columns.Length == 0)
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
        while (checkedCount < _columns.Length)
        {
            int index = _cursor;
            _cursor = index + 1 == _columns.Length ? 0 : index + 1;
            checkedCount++;

            ActorColumnPumpResult result = _columns[index].PumpOne(ref budget, options, stats);
            if (result == ActorColumnPumpResult.Processed)
            {
                stats.ProcessedTotal++;
                stats.RecordBucketProcessed(bucketIndex);
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
        for (int i = 0; i < _columns.Length; i++)
        {
            if (_columns[i].HasPendingWork())
            {
                return true;
            }
        }

        return false;
    }
}
