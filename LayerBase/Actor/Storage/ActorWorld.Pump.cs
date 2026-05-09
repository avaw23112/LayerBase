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
        if (_state == ActorWorldState.Disposed || _state == ActorWorldState.Stopping)
        {
            return;
        }

        DelayScheduler.Tick(deltaTime);
        LastMailPumpStats = PumpActorBehaviours(ref budget, MailPumpOptions);
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

        SweepPendingDestroy();
    }

    private static bool CanContinue(ref RuntimeFrameBudget budget)
    {
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
        PumpOneResult callResult = TryPumpOneFromBuckets(
            _callBucketsByRouteId,
            ref _callBucketCursor,
            ref budget,
            options,
            stats);
        if (callResult == PumpOneResult.Processed
            || callResult == PumpOneResult.BucketLimited
            || callResult == PumpOneResult.ActorLimited)
        {
            return callResult;
        }

        return TryPumpOneFromBuckets(
            _eventBucketsByEventId,
            ref _bucketCursor,
            ref budget,
            options,
            stats);
    }

    private static PumpOneResult TryPumpOneFromBuckets(
        IActorEventBucket[] buckets,
        ref int cursor,
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats)
    {
        if (buckets.Length == 0)
        {
            return PumpOneResult.NoWork;
        }

        int checkedCount = 0;
        bool sawBucketLimit = false;
        bool sawActorLimit = false;
        while (checkedCount < buckets.Length)
        {
            int index = cursor;
            cursor = index + 1 == buckets.Length ? 0 : index + 1;
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
        for (int i = 0; i < _callBucketsByRouteId.Length; i++)
        {
            if (_callBucketsByRouteId[i]?.HasPendingWork() == true)
            {
                count++;
            }
        }

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
