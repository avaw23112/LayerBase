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

        PumpActorBehaviours(ref budget);
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
    private void PumpActorBehaviours(ref RuntimeFrameBudget budget)
    {
        while (budget.HasRemainingEventBudget())
        {
            if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
            {
                return;
            }

            if (!TryPumpOne(ref budget))
            {
                return;
            }
        }
    }

    private bool TryPumpOne(ref RuntimeFrameBudget budget)
    {
        IActorEventBucket[] buckets = _eventBucketsByEventId;
        if (buckets.Length == 0)
        {
            return false;
        }

        int checkedCount = 0;
        while (checkedCount < buckets.Length)
        {
            int index = _bucketCursor;
            _bucketCursor = index + 1 == buckets.Length ? 0 : index + 1;
            checkedCount++;

            IActorEventBucket? current = buckets[index];
            if (current != null && current.PumpOne(ref budget))
            {
                return true;
            }
        }

        return false;
    }
}
