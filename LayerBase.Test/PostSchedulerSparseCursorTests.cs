using LayerBase.Core.Event;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class PostSchedulerSparseCursorTests
{
    [Test]
    public void Latest_remaining_snapshot_survives_next_pump()
    {
        var eventCenter = new EventCenter();

        var options = new PostSchedulerOptions(
            readyCapacity: 1024,
            nextCapacity: 1024,
            maxEventsPerPump: 1,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure:
                BackpressurePolicy.RejectNew);

        using var scheduler =
            CreateScheduler(options, eventCenter);

        scheduler.TryPostLatest(
            new SBLatestEventA { Value = 10 });

        scheduler.TryPostLatest(
            new SBLatestEventB { Value = 20 });

        PostPumpStats first = scheduler.Pump();
        PostPumpStats second = scheduler.Pump();

        Assert.That(first.ProcessedCount, Is.EqualTo(1));
        Assert.That(second.ProcessedCount, Is.EqualTo(1));
    }

    [Test]
    public void Sparse_high_event_type_id_dispatches_across_pumps()
    {
        var eventCenter = new EventCenter();

        var options = new PostSchedulerOptions(
            readyCapacity: 1024,
            nextCapacity: 1024,
            maxEventsPerPump: 1,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure:
                BackpressurePolicy.RejectNew);

        using var scheduler =
            CreateScheduler(options, eventCenter);

        scheduler.TryPostLatest(
            new SparseEventLow { Value = 1 });

        scheduler.TryPostLatest(
            new SparseEventHigh { Value = 2 });

        PostPumpStats first = scheduler.Pump();
        PostPumpStats second = scheduler.Pump();

        Assert.That(first.ProcessedCount, Is.EqualTo(1));
        Assert.That(second.ProcessedCount, Is.EqualTo(1));
    }

    [Test]
    public void Pending_dirty_survives_after_budget_exhaustion()
    {
        var eventCenter = new EventCenter();

        var options = new PostSchedulerOptions(
            readyCapacity: 1024,
            nextCapacity: 1024,
            maxEventsPerPump: 1,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure:
                BackpressurePolicy.RejectNew);

        using var scheduler =
            CreateScheduler(options, eventCenter);

        scheduler.TryPostLatest(new SBLatestEventA { Value = 10 });
        scheduler.TryPostLatest(new SBLatestEventB { Value = 20 });

        PostPumpStats first = scheduler.Pump();
        Assert.That(first.ProcessedCount, Is.EqualTo(1));

        Assert.That(scheduler.HasPendingWork, Is.True);

        PostPumpStats second = scheduler.Pump();
        Assert.That(second.ProcessedCount, Is.EqualTo(1));

        Assert.That(scheduler.HasPendingWork, Is.False);
    }

    private static PostScheduler CreateScheduler(
        PostSchedulerOptions options,
        EventCenter eventCenter)
    {
        var policyTable = new EventBuildPolicyTable(
            BackpressurePolicy.RejectNew);

        policyTable.SetPostPolicy(
            EventTypeId<SBLatestEventA>.Id,
            new EventPostPolicy(
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0));

        policyTable.SetPostPolicy(
            EventTypeId<SBLatestEventB>.Id,
            new EventPostPolicy(
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0));

        policyTable.SetPostPolicy(
            EventTypeId<SparseEventLow>.Id,
            new EventPostPolicy(
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0));

        policyTable.SetPostPolicy(
            EventTypeId<SparseEventHigh>.Id,
            new EventPostPolicy(
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0));

        var scheduler = new PostScheduler(
            1, eventCenter, options, policyTable);

        var plans = new[]
        {
            new PostTypePlan(
                EventTypeId<SBLatestEventA>.Id,
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0,
                BackpressurePolicy.RejectNew,
                MergeFailurePolicy.Reject),
            new PostTypePlan(
                EventTypeId<SBLatestEventB>.Id,
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0,
                BackpressurePolicy.RejectNew,
                MergeFailurePolicy.Reject),
            new PostTypePlan(
                EventTypeId<SparseEventLow>.Id,
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0,
                BackpressurePolicy.RejectNew,
                MergeFailurePolicy.Reject),
            new PostTypePlan(
                EventTypeId<SparseEventHigh>.Id,
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0,
                BackpressurePolicy.RejectNew,
                MergeFailurePolicy.Reject),
        };

        scheduler.BuildPlans(plans);
        return scheduler;
    }

    private struct SBLatestEventA
    {
        public int Value { get; set; }
    }

    private struct SBLatestEventB
    {
        public int Value { get; set; }
    }

    private struct SparseEventLow
    {
        public int Value { get; set; }
    }

    private struct SparseEventHigh
    {
        public int Value { get; set; }
    }
}
