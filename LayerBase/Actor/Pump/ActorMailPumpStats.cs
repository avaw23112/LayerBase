namespace LayerBase.Actor;

public readonly struct ActorMailPumpStats
{
    public readonly int ProcessedTotal;
    public readonly int BucketLimitHits;
    public readonly int ActorLimitHits;
    public readonly int EmptyBucketChecks;
    public readonly int RemainingDirtyBuckets;

    public ActorMailPumpStats(
        int processedTotal,
        int bucketLimitHits,
        int actorLimitHits,
        int emptyBucketChecks,
        int remainingDirtyBuckets)
    {
        ProcessedTotal = processedTotal;
        BucketLimitHits = bucketLimitHits;
        ActorLimitHits = actorLimitHits;
        EmptyBucketChecks = emptyBucketChecks;
        RemainingDirtyBuckets = remainingDirtyBuckets;
    }
}