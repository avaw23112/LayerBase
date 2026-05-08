using System.Diagnostics;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public void Pump(ref RuntimeFrameBudget budget)
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
