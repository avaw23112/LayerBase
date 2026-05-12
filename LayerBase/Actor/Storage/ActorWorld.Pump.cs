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
        if (CanUsePumpManyFastPath(in options))
        {
            return PumpActorBehavioursManyFast(
                budget: ref budget,
                options: in options);
        }

        return PumpActorBehavioursOneByOne(
            budget: ref budget,
            options: in options);
    }

    /// <summary>
    /// 判断 ActorWorld 是否可以使用批量 Pump 快路径。
    ///
    /// 参数说明：
    /// options：Actor 邮箱 Pump 配置。
    ///
    /// 返回值：
    /// true 表示可以使用 PumpManyFast。
    /// false 表示保留旧的逐事件 PumpOne。
    /// </summary>
    private static bool CanUsePumpManyFastPath(
        in ActorMailPumpOptions options)
    {
        return options.MaxMailsPerActorPerPump <= 0
               && options.MaxMailsPerBucketPerPump <= 0;
    }

    private ActorMailPumpStats PumpActorBehavioursOneByOne(
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

    private ActorMailPumpStats PumpActorBehavioursManyFast(
        ref RuntimeFrameBudget   budget,
        in  ActorMailPumpOptions options)
    {
        ActorMailPumpStatsBuilder stats = _mailPumpStatsBuilder;
        stats.Reset();

        int processedSinceTimeCheck = 0;

        while (budget.HasRemainingEventBudget()
               && (options.MaxTotalMailsPerPump <= 0 ||
                   stats.ProcessedTotal < options.MaxTotalMailsPerPump))
        {
            if (processedSinceTimeCheck <= 0)
            {
                if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
                {
                    break;
                }

                processedSinceTimeCheck = options.TimeCheckInterval;
            }

            int remainingByBudget = budget.RemainingEventBudget;

            int remainingByOption = options.MaxTotalMailsPerPump > 0
                ? options.MaxTotalMailsPerPump - stats.ProcessedTotal
                : remainingByBudget;

            int maxEvents = Math.Min(
                remainingByBudget,
                remainingByOption);

            ActorPumpManyResult result = TryPumpMany(
                budget: ref budget,
                options: in options,
                stats: stats,
                maxEvents: maxEvents);

            if (result.Processed > 0)
            {
                processedSinceTimeCheck -= result.Processed;
                continue;
            }

            if (result.Result == PumpOneResult.EmptyBucket)
            {
                stats.EmptyBucketChecks++;

                if (options.MaxEmptyBucketChecksPerPump > 0 &&
                    stats.EmptyBucketChecks >= options.MaxEmptyBucketChecksPerPump)
                {
                    break;
                }

                continue;
            }

            if (result.Result == PumpOneResult.BucketLimited ||
                result.Result == PumpOneResult.ActorLimited ||
                result.Result == PumpOneResult.NoWork)
            {
                break;
            }
        }

        return stats.Build(CountRemainingDirtyBuckets());
    }

    private ActorPumpManyResult TryPumpMany(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       maxEvents)
    {
        // Call bucket 涉及 request/response 语义，先保留旧路径。
        // 但仍然需要处理 Call bucket，否则 Call 事件不会被处理。
        ActorPumpManyResult callResult = TryPumpManyFromDirtyBuckets(
            dirtyBuckets: _dirtyCallBuckets,
            buckets: _callBucketsByRouteId,
            cursor: ref _callBucketCursor,
            budget: ref budget,
            options: in options,
            stats: stats,
            maxEvents: maxEvents);

        if (callResult.Processed > 0 ||
            callResult.Result == PumpOneResult.BucketLimited ||
            callResult.Result == PumpOneResult.ActorLimited)
        {
            return callResult;
        }

        // 处理 Event bucket。
        return TryPumpManyFromDirtyBuckets(
            dirtyBuckets: _dirtyEventBuckets,
            buckets: _eventBucketsByEventId,
            cursor: ref _bucketCursor,
            budget: ref budget,
            options: in options,
            stats: stats,
            maxEvents: maxEvents);
    }

    private static ActorPumpManyResult TryPumpManyFromDirtyBuckets(
        DirtyBucketList           dirtyBuckets,
        IActorEventBucket[]       buckets,
        ref int                   cursor,
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       maxEvents)
    {
        if (dirtyBuckets.Count == 0 || buckets.Length == 0)
        {
            return ActorPumpManyResult.NoWork();
        }

        int checkedCount = 0;
        bool sawBucketLimit = false;
        bool sawActorLimit = false;
        int initialCount = dirtyBuckets.Count;

        while (checkedCount < initialCount &&
               dirtyBuckets.TryPeek(out int bucketIndex))
        {
            cursor = bucketIndex;
            checkedCount++;

            IActorEventBucket? current = buckets[bucketIndex];
            if (current == null)
            {
                dirtyBuckets.Pop();
                continue;
            }

            ActorPumpManyResult result = current.PumpMany(
                budget: ref budget,
                options: in options,
                stats: stats,
                bucketIndex: bucketIndex,
                maxEvents: maxEvents);

            if (result.Processed > 0)
            {
                if (current.HasPendingWork())
                {
                    dirtyBuckets.MoveHeadToTail();
                }
                else
                {
                    dirtyBuckets.Pop();
                }

                return result;
            }

            if (result.Result == PumpOneResult.BucketLimited)
            {
                sawBucketLimit = true;
                dirtyBuckets.MoveHeadToTail();
            }
            else if (result.Result == PumpOneResult.ActorLimited)
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
            return new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.BucketLimited);
        }

        if (sawActorLimit)
        {
            return new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.ActorLimited);
        }

        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.EmptyBucket);
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