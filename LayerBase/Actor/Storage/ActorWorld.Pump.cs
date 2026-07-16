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
        try
        {
            if (DelayScheduler.HasPending)
            {
                DelayScheduler.Tick(deltaTime);
            }

            SweepPendingDestroy();

            // Pump Call buckets (old system, still needed for Ask/Call)
            PumpCallBuckets(ref budget);

            if (!CanContinue(ref budget))
            {
                return;
            }

            // Pump EventStream runtimes
            PumpEventStreams(ref budget);

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
        finally
        {
            Lifecycle.EndFrame();
        }
    }

    private void PumpCallBuckets(ref RuntimeFrameBudget budget)
    {
        // 跳过空 Call Bucket。
        // 如果项目没有使用 Actor Call，Call 路径会成为固定分支成本。
        if (!_hasCallBuckets || _dirtyCallBuckets.Count == 0)
        {
            return;
        }

        while (budget.HasRemainingWork())
        {
            if (_dirtyCallBuckets.Count == 0)
            {
                break;
            }

            if (!_dirtyCallBuckets.TryPeek(out int bucketIndex))
            {
                break;
            }

            if ((uint)bucketIndex >= (uint)_callBucketsByRouteId.Length)
            {
                _dirtyCallBuckets.Pop();
                continue;
            }

            IActorCallBucket? bucket = _callBucketsByRouteId[bucketIndex];
            if (bucket == null)
            {
                _dirtyCallBuckets.Pop();
                continue;
            }

            var options = ActorMailPumpOptions.Default;
            var stats = new ActorMailPumpStatsBuilder();
            int maxEvents = budget.RemainingWorkItems;

            ActorPumpManyResult result = bucket.PumpMany(
                ref budget,
                options,
                stats,
                bucketIndex,
                maxEvents);

            if (result.HasProcessed)
            {
                if (result.HasMoreWork)
                {
                    _dirtyCallBuckets.MoveHeadToTail();
                }
                else
                {
                    _dirtyCallBuckets.Pop();
                }
            }
            else if (result.Result == PumpOneResult.NoWork ||
                     result.Result == PumpOneResult.EmptyBucket)
            {
                _dirtyCallBuckets.Pop();
            }
            else
            {
                _dirtyCallBuckets.MoveHeadToTail();
                break;
            }
        }
    }

    private void PumpEventStreams(ref RuntimeFrameBudget budget)
    {
        for (int i = 0; i < _eventStreamRuntimes.Count; i++)
        {
            IEventStreamCenterRuntime runtime = _eventStreamRuntimes[i];
            if (runtime.IsEmpty)
            {
                continue;
            }

            int maxCount = budget.RemainingWorkItems;
            if (maxCount <= 0)
            {
                break;
            }

            int processed = runtime.Pump(maxCount);
            budget.Consume(processed);

            if (!CanContinue(ref budget))
            {
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CanContinue(ref RuntimeFrameBudget budget)
    {
        return budget.CanContinue(Stopwatch.GetTimestamp());
    }
}
