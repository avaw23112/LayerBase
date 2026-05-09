using System.Diagnostics;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public void Pump(
        float                  deltaTime,
        float                  fixedDeltaTime,
        bool                   pumpFixedUpdate,
        ref RuntimeFrameBudget budget)
    {
        // deltaTime 参数表示当前帧间隔，通常单位是秒。
        // fixedDeltaTime 参数表示固定逻辑步长，通常用于 IFixedUpdate。
        // pumpFixedUpdate 参数表示本帧是否允许执行 Actor 的 IFixedUpdate。
        // budget 参数表示当前帧剩余调度预算。
        // 当前 RuntimeFrameBudget 名字里叫 Event，但这里可以临时把一次生命周期调用也视为一个调度工作单元。

        LastMailPumpStats = PumpActorBehaviours(ref budget, MailPumpOptions);
        // ActorBehaviour 阶段里 DestroyActor 的对象，本帧不再进入生命周期。
        SweepPendingDestroy();
        if (!CanContinue(ref budget))
        {
            return;
        }

        if (pumpFixedUpdate)
        {
            Lifecycle.PumpFixedUpdate(
                fixedDeltaTime: fixedDeltaTime,
                budget: ref budget);
        }

        if (!CanContinue(ref budget))
        {
            SweepPendingDestroy();
            return;
        }

        Lifecycle.PumpUpdate(
            deltaTime: deltaTime,
            budget: ref budget);

        if (!CanContinue(ref budget))
        {
            SweepPendingDestroy();
            return;
        }

        Lifecycle.PumpLateUpdate(
            deltaTime: deltaTime,
            budget: ref budget);

        // 生命周期阶段里 DestroyActor 的对象，本帧末尾清理。
        SweepPendingDestroy();
    }
    private static bool CanContinue(ref RuntimeFrameBudget budget)
    {
        // budget 参数表示当前帧剩余预算。
        // 同时检查数量预算和真实时间预算。
        return budget.HasRemainingEventBudget()
               && budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp());
    }
    private ActorMailPumpStats PumpActorBehaviours(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options)
    {
        var stats = new ActorMailPumpStatsBuilder();
        while (budget.HasRemainingEventBudget()
               && (options.MaxTotalMailsPerPump <= 0 || stats.ProcessedTotal < options.MaxTotalMailsPerPump))
        {
            if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
            {
                break;
            }

            PumpOneResult result = TryPumpOne(ref budget, options, stats);
            if (result == PumpOneResult.Processed)
            {
                continue;
            }

            if (result == PumpOneResult.EmptyBucket)
            {
                stats.EmptyBucketChecks++;
                if (options.MaxEmptyBucketChecksPerPump > 0
                    && stats.EmptyBucketChecks >= options.MaxEmptyBucketChecksPerPump)
                {
                    break;
                }

                continue;
            }

            if (result == PumpOneResult.BucketLimited || result == PumpOneResult.ActorLimited)
            {
                break;
            }

            if (result == PumpOneResult.NoWork)
            {
                break;
            }
        }

        return stats.Build(CountRemainingDirtyBuckets());
    }

    private PumpOneResult TryPumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats)
    {
        IActorEventBucket[] buckets = _eventBucketsByEventId;
        if (buckets.Length == 0)
        {
            return PumpOneResult.NoWork;
        }

        int checkedCount = 0;
        bool sawBucketLimit = false;
        bool sawActorLimit = false;
        while (checkedCount < buckets.Length)
        {
            int index = _bucketCursor;
            _bucketCursor = index + 1 == buckets.Length ? 0 : index + 1;
            checkedCount++;

            IActorEventBucket? current = buckets[index];
            if (current == null)
            {
                continue;
            }

            PumpOneResult result = current.PumpOne(ref budget, options, stats, index);
            if (result == PumpOneResult.Processed)
            {
                return PumpOneResult.Processed;
            }

            if (result == PumpOneResult.BucketLimited)
            {
                sawBucketLimit = true;
            }
            else if (result == PumpOneResult.ActorLimited)
            {
                sawActorLimit = true;
            }
        }

        if (sawBucketLimit)
        {
            return PumpOneResult.BucketLimited;
        }

        if (sawActorLimit)
        {
            return PumpOneResult.ActorLimited;
        }

        return PumpOneResult.EmptyBucket;
    }

    private int CountRemainingDirtyBuckets()
    {
        int count = 0;
        for (int i = 0; i < _eventBucketsByEventId.Length; i++)
        {
            if (_eventBucketsByEventId[i]?.HasPendingWork() == true)
            {
                count++;
            }
        }

        return count;
    }
}
