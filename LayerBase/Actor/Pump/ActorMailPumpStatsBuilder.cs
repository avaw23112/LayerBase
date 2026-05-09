namespace LayerBase.Actor;

internal sealed class ActorMailPumpStatsBuilder
{
    private readonly Dictionary<long, int> _actorProcessedCounts = new();
    private readonly Dictionary<int, int> _bucketProcessedCounts = new();

    public int ProcessedTotal;
    public int BucketLimitHits;
    public int ActorLimitHits;
    public int EmptyBucketChecks;

    public bool CanProcessBucket(int bucketIndex, in ActorMailPumpOptions options)
    {
        if (options.MaxMailsPerBucketPerPump <= 0)
        {
            return true;
        }

        return !_bucketProcessedCounts.TryGetValue(bucketIndex, out int count)
               || count < options.MaxMailsPerBucketPerPump;
    }

    public void RecordBucketProcessed(int bucketIndex)
    {
        if (_bucketProcessedCounts.TryGetValue(bucketIndex, out int count))
        {
            _bucketProcessedCounts[bucketIndex] = count + 1;
        }
        else
        {
            _bucketProcessedCounts[bucketIndex] = 1;
        }
    }

    public bool CanProcessActor(long actorKey, in ActorMailPumpOptions options)
    {
        if (options.MaxMailsPerActorPerPump <= 0)
        {
            return true;
        }

        return !_actorProcessedCounts.TryGetValue(actorKey, out int count)
               || count < options.MaxMailsPerActorPerPump;
    }

    public void RecordActorProcessed(long actorKey)
    {
        if (_actorProcessedCounts.TryGetValue(actorKey, out int count))
        {
            _actorProcessedCounts[actorKey] = count + 1;
        }
        else
        {
            _actorProcessedCounts[actorKey] = 1;
        }
    }

    public ActorMailPumpStats Build(int remainingDirtyBuckets)
    {
        return new ActorMailPumpStats(
            ProcessedTotal,
            BucketLimitHits,
            ActorLimitHits,
            EmptyBucketChecks,
            remainingDirtyBuckets);
    }
}
