using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using NUnit.Framework;

namespace LayerBase.Test;

public partial struct SBDirtyEvent { }

public class SBDirtyMeta : EventMetaData<SBDirtyEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.DirtySignal, BackpressurePolicy.RejectNew, 0);
}

public partial struct SBLatestEventA
{
    public int Value;
}

public class SBLatestMetaA : EventMetaData<SBLatestEventA>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0);
}

public partial struct SBLatestEventB
{
    public int Value;
}

public class SBLatestMetaB : EventMetaData<SBLatestEventB>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0);
}

[TestFixture]
[Category("ProductionHardening")]
public class PostSchedulerSpecialBudgetTests
{
    private static PostScheduler CreateScheduler(PostSchedulerOptions options, EventCenter eventCenter)
    {
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int dirtyId = EventTypeId<SBDirtyEvent>.Id;
        int latestAId = EventTypeId<SBLatestEventA>.Id;
        int latestBId = EventTypeId<SBLatestEventB>.Id;

        policyTable.SetMetaData(dirtyId, new SBDirtyMeta());
        policyTable.SetMetaData(latestAId, new SBLatestMetaA());
        policyTable.SetMetaData(latestBId, new SBLatestMetaB());

        var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(dirtyId, PostDeliveryMode.DirtySignal, BackpressurePolicy.RejectNew, 0, options.DefaultBackpressure),
            new PostTypePlan(latestAId, PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0, options.DefaultBackpressure),
            new PostTypePlan(latestBId, PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0, options.DefaultBackpressure),
        });
        return scheduler;
    }

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Dirty_budget_one_dispatches_exactly_one()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 1, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        scheduler.TryPost(default(SBDirtyEvent));

        var stats = scheduler.Pump();
        Assert.That(stats.ProcessedCount, Is.EqualTo(1));
    }

    [Test]
    public void Latest_budget_one_dispatches_exactly_one()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 1, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        scheduler.TryPostLatest(new SBLatestEventA { Value = 10 });
        scheduler.TryPostLatest(new SBLatestEventB { Value = 20 });

        var stats = scheduler.Pump();
        Assert.That(stats.ProcessedCount, Is.EqualTo(1));
    }

    [Test]
    public void Dirty_remaining_bits_survive_next_pump()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 1, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        scheduler.TryPost(default(SBDirtyEvent));

        var stats1 = scheduler.Pump();
        Assert.That(stats1.ProcessedCount, Is.EqualTo(1));
    }

    [Test]
    public void Budget_exhaustion_does_not_release_undispatched_latest_payload()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 1, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        scheduler.TryPostLatest(new SBLatestEventA { Value = 42 });

        var stats1 = scheduler.Pump();
        Assert.That(stats1.ProcessedCount, Is.EqualTo(1));
    }

    [Test]
    public void Handler_exception_does_not_drop_remaining_special_events()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 1, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        bool first = true;
        eventCenter.SubscribeNotify<SBLatestEventA>(0, (in SBLatestEventA v) =>
        {
            if (first) { first = false; throw new InvalidOperationException("fail"); }
        });

        scheduler.TryPostLatest(new SBLatestEventA { Value = 1 });
        scheduler.TryPostLatest(new SBLatestEventB { Value = 2 });

        Assert.That(() => scheduler.Pump(), Throws.InvalidOperationException);
        Assert.That(scheduler.HasPendingWork, Is.True);
    }
}
