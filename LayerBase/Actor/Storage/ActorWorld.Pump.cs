using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        if (DelayScheduler.HasPending)
        {
            DelayScheduler.Tick(deltaTime);
        }

        SweepPendingDestroy();
        LastMailPumpStats = PumpActorBehaviours(ref budget, MailPumpOptions);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CanContinue(ref RuntimeFrameBudget budget)
    {
        return budget.HasRemainingEventBudget()
               && budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp());
    }

    private ActorMailPumpStats PumpActorBehaviours(
        ref RuntimeFrameBudget   budget,
        in  ActorMailPumpOptions options)
    {
        ActorMailPumpStatsBuilder stats = _mailPumpStatsBuilder;
        stats.Reset();
        int processedSinceTimeCheck = 0;
        while (budget.HasRemainingEventBudget()
               && (options.MaxTotalMailsPerPump <= 0 || stats.ProcessedTotal < options.MaxTotalMailsPerPump))
        {
            if (processedSinceTimeCheck <= 0)
            {
                if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
                {
                    break;
                }

                processedSinceTimeCheck = options.TimeCheckInterval;
            }

            PumpOneResult result = TryPumpOne(ref budget, options, stats);
            if (result == PumpOneResult.Processed)
            {
                processedSinceTimeCheck--;
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
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats)
    {
        PumpOneResult callResult = TryPumpOneFromDirtyBuckets(
            _dirtyCallBuckets,
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

        return TryPumpOneFromDirtyBuckets(
            _dirtyEventBuckets,
            _eventBucketsByEventId,
            ref _bucketCursor,
            ref budget,
            options,
            stats);
    }

    private static PumpOneResult TryPumpOneFromDirtyBuckets(
        DirtyBucketList           dirtyBuckets,
        IActorEventBucket[]       buckets,
        ref int                   cursor,
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats)
    {
        if (dirtyBuckets.Count == 0 || buckets.Length == 0)
        {
            return PumpOneResult.NoWork;
        }

        int checkedCount = 0;
        bool sawBucketLimit = false;
        bool sawActorLimit = false;
        int initialCount = dirtyBuckets.Count;
        while (checkedCount < initialCount && dirtyBuckets.TryPeek(out int bucketIndex))
        {
            cursor = bucketIndex;
            checkedCount++;

            IActorEventBucket? current = buckets[bucketIndex];
            if (current == null)
            {
                dirtyBuckets.Pop();
                continue;
            }

            PumpOneResult result = current.PumpOne(ref budget, options, stats, bucketIndex);
            if (result == PumpOneResult.Processed)
            {
                if (current.HasPendingWork())
                {
                    dirtyBuckets.MoveHeadToTail();
                }
                else
                {
                    dirtyBuckets.Pop();
                }

                return PumpOneResult.Processed;
            }

            if (result == PumpOneResult.BucketLimited)
            {
                sawBucketLimit = true;
                dirtyBuckets.MoveHeadToTail();
            }
            else if (result == PumpOneResult.ActorLimited)
            {
                sawActorLimit = true;
                dirtyBuckets.MoveHeadToTail();
            }
            else
            {
                dirtyBuckets.Pop();
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
        return _dirtyCallBuckets.Count + _dirtyEventBuckets.Count;
    }
}