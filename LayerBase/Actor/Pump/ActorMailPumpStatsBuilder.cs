using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

internal sealed class ActorMailPumpStatsBuilder
{
    private readonly Dictionary<long, int> _actorProcessedCounts = new();
    private readonly Dictionary<int, int> _bucketProcessedCounts = new();

    public ActorMailPumpStatsMode StatsMode;
    public int ProcessedTotal;
    public int BucketLimitHits;
    public int ActorLimitHits;
    public int EmptyBucketChecks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(ActorMailPumpStatsMode statsMode = ActorMailPumpStatsMode.Full)
    {
        StatsMode = statsMode;

        if (statsMode == ActorMailPumpStatsMode.Full)
        {
            _actorProcessedCounts.Clear();
            _bucketProcessedCounts.Clear();
        }

        ProcessedTotal = 0;
        BucketLimitHits = 0;
        ActorLimitHits = 0;
        EmptyBucketChecks = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanProcessBucket(int bucketIndex, in ActorMailPumpOptions options)
    {
        // 调度状态数据必须保留，不受 StatsMode 影响。
        if (options.MaxMailsPerBucketPerPump <= 0)
        {
            return true;
        }

        // StatsMode.None 或 Basic 时，不做 Bucket 级限流检查。
        // 因为没有记录 Bucket 处理次数。
        if (StatsMode != ActorMailPumpStatsMode.Full)
        {
            return true;
        }

        return !_bucketProcessedCounts.TryGetValue(bucketIndex, out int count)
               || count < options.MaxMailsPerBucketPerPump;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordBucketProcessed(int bucketIndex)
    {
        // StatsMode.Full 时记录 Bucket 级细节。
        if (StatsMode != ActorMailPumpStatsMode.Full)
        {
            return;
        }

        if (_bucketProcessedCounts.TryGetValue(bucketIndex, out int count))
        {
            _bucketProcessedCounts[bucketIndex] = count + 1;
        }
        else
        {
            _bucketProcessedCounts[bucketIndex] = 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanProcessActor(long actorKey, in ActorMailPumpOptions options)
    {
        // 调度状态数据必须保留，不受 StatsMode 影响。
        if (options.MaxMailsPerActorPerPump <= 0)
        {
            return true;
        }

        // StatsMode.None 或 Basic 时，不做 Actor 级限流检查。
        // 因为没有记录 Actor 处理次数。
        if (StatsMode != ActorMailPumpStatsMode.Full)
        {
            return true;
        }

        return !_actorProcessedCounts.TryGetValue(actorKey, out int count)
               || count < options.MaxMailsPerActorPerPump;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordActorProcessed(long actorKey)
    {
        // StatsMode.Full 时记录 Actor 级细节。
        if (StatsMode != ActorMailPumpStatsMode.Full)
        {
            return;
        }

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
