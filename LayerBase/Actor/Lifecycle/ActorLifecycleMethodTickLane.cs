namespace LayerBase.Actor;

internal sealed class ActorLifecycleMethodTickLane
{
    private readonly ActorLifecycleMethodFreeList _hot = new();
    private readonly ActorLifecycleMethodFreeList[] _warm;
    private readonly ActorLifecycleMethodFreeList[] _cold;
    private readonly ActorLifecycleMethodFreeList _dormant = new();

    public ActorLifecycleMethodTickLane(int warmBucketCount = 3, int coldBucketCount = 10)
    {
        _warm = CreateBuckets(warmBucketCount);
        _cold = CreateBuckets(coldBucketCount);
    }

    public ActorLifecycleHandle Add(
        ActorId                     actorId,
        IActor                      actor,
        ActorLifecycleMethodInvoker invoker,
        TickTier                    tier,
        int                         tickPhase)
    {
        return tier switch
               {
                   TickTier.Hot => _hot.Add(actorId, actor, invoker),
                   TickTier.Warm => _warm[NormalizeBucketIndex(tickPhase, _warm.Length)].Add(actorId, actor, invoker),
                   TickTier.Cold => _cold[NormalizeBucketIndex(tickPhase, _cold.Length)].Add(actorId, actor, invoker),
                   TickTier.Dormant => _dormant.Add(actorId, actor, invoker),
                   _ => _hot.Add(actorId, actor, invoker)
               };
    }

    public void Remove(ActorLifecycleHandle handle, TickTier tier, int tickPhase)
    {
        switch (tier)
        {
            case TickTier.Hot:
                _hot.Remove(handle);
                return;
            case TickTier.Warm:
                _warm[NormalizeBucketIndex(tickPhase, _warm.Length)].Remove(handle);
                return;
            case TickTier.Cold:
                _cold[NormalizeBucketIndex(tickPhase, _cold.Length)].Remove(handle);
                return;
            case TickTier.Dormant:
                _dormant.Remove(handle);
                return;
            default:
                _hot.Remove(handle);
                return;
        }
    }

    public void Pump(
        int                     frameIndex,
        ref LifecycleFrameState state,
        ref RuntimeFrameBudget  budget,
        int                     timeCheckInterval)
    {
        _hot.PumpBudgeted(ref state, ref budget, timeCheckInterval);
        if (!budget.HasRemainingWork())
        {
            return;
        }

        _warm[GetFrameBucketIndex(frameIndex, _warm.Length)]
            .PumpBudgeted(ref state, ref budget, timeCheckInterval);
        if (!budget.HasRemainingWork())
        {
            return;
        }

        _cold[GetFrameBucketIndex(frameIndex, _cold.Length)]
            .PumpBudgeted(ref state, ref budget, timeCheckInterval);
    }

    private static ActorLifecycleMethodFreeList[] CreateBuckets(int bucketCount)
    {
        int size = bucketCount <= 0 ? 1 : bucketCount;
        var buckets = new ActorLifecycleMethodFreeList[size];
        for (int i = 0; i < buckets.Length; i++)
        {
            buckets[i] = new ActorLifecycleMethodFreeList();
        }

        return buckets;
    }

    private static int NormalizeBucketIndex(int tickPhase, int bucketCount)
    {
        if (bucketCount <= 1 || tickPhase < 0)
        {
            return 0;
        }

        return tickPhase % bucketCount;
    }

    private static int GetFrameBucketIndex(int frameIndex, int bucketCount)
    {
        if (bucketCount <= 1)
        {
            return 0;
        }

        int normalized = frameIndex % bucketCount;
        return normalized < 0 ? normalized + bucketCount : normalized;
    }
}
