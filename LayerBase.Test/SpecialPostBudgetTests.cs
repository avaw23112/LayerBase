using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using NUnit.Framework;

namespace LayerBase.Test;

public partial struct BudgetCoalescedEvent
{
    public int Id;
    public int Value;
}

public class BudgetCoalescedMeta : EventMetaData<BudgetCoalescedEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0);

    public override int GetPostCoalesceKey(in BudgetCoalescedEvent value) => value.Id;

    public override bool TryMergePostEvent(ref BudgetCoalescedEvent current, in BudgetCoalescedEvent next)
    {
        current.Value += next.Value;
        return true;
    }
}

public partial struct BudgetDirtyEvent
{
}

public class BudgetDirtyMeta : EventMetaData<BudgetDirtyEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.DirtySignal, BackpressurePolicy.RejectNew, 0);
}

public partial struct BudgetLatestEvent
{
    public int Value;
}

public class BudgetLatestMeta : EventMetaData<BudgetLatestEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0);
}

[TestFixture]
public class SpecialPostBudgetTests
{
    private static PostScheduler CreateScheduler(PostSchedulerOptions options, EventCenter eventCenter)
    {
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int coalescedId = EventTypeId<BudgetCoalescedEvent>.Id;
        int dirtyId = EventTypeId<BudgetDirtyEvent>.Id;
        int latestId = EventTypeId<BudgetLatestEvent>.Id;

        policyTable.SetMetaData(coalescedId, new BudgetCoalescedMeta());
        policyTable.SetMetaData(dirtyId, new BudgetDirtyMeta());
        policyTable.SetMetaData(latestId, new BudgetLatestMeta());

        var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(coalescedId, PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0,
                options.DefaultBackpressure),
            new PostTypePlan(dirtyId, PostDeliveryMode.DirtySignal, BackpressurePolicy.RejectNew, 0,
                options.DefaultBackpressure),
            new PostTypePlan(latestId, PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0,
                options.DefaultBackpressure),
        });

        return scheduler;
    }

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Coalesced_events_obey_MaxEventsPerPump_budget()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 3, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        for (int i = 0; i < 10; i++)
            scheduler.TryPost(new BudgetCoalescedEvent { Id = i, Value = i * 10 });

        var stats = scheduler.Pump();
        Assert.That(stats.ProcessedCount, Is.EqualTo(3), "First pump should process exactly 3 coalesced events");

        Assert.That(scheduler.HasPendingWork, Is.True, "Scheduler should still have pending snapshot items");
    }

    [Test]
    public void Multiple_pumps_drain_coalesced_queue_within_budget()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 3, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        for (int i = 0; i < 10; i++)
            scheduler.TryPost(new BudgetCoalescedEvent { Id = i, Value = i * 10 });

        int totalProcessed = 0;
        int pumps = 0;

        while (scheduler.HasPendingWork && pumps < 10)
        {
            var stats = scheduler.Pump();
            totalProcessed += stats.ProcessedCount;
            pumps++;
        }

        Assert.That(totalProcessed, Is.EqualTo(10));
        Assert.That(pumps, Is.EqualTo(4), "Should take 4 pumps (3+3+3+1)");
    }

    [Test]
    public void No_budget_processes_all_special_events_in_single_pump()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        for (int i = 0; i < 10; i++)
            scheduler.TryPost(new BudgetCoalescedEvent { Id = i, Value = i * 10 });

        var stats = scheduler.Pump();
        Assert.That(stats.ProcessedCount, Is.EqualTo(10));
    }

    [Test]
    public void Mix_of_special_events_shares_budget()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 3, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        scheduler.TryPost(default(BudgetDirtyEvent));
        scheduler.TryPost(default(BudgetDirtyEvent));
        scheduler.TryPost(default(BudgetDirtyEvent));

        scheduler.TryPostLatest(new BudgetLatestEvent { Value = 1 });
        scheduler.TryPostLatest(new BudgetLatestEvent { Value = 2 });
        scheduler.TryPostLatest(new BudgetLatestEvent { Value = 3 });

        scheduler.TryPost(new BudgetCoalescedEvent { Id = 1, Value = 10 });
        scheduler.TryPost(new BudgetCoalescedEvent { Id = 2, Value = 20 });
        scheduler.TryPost(new BudgetCoalescedEvent { Id = 3, Value = 30 });
        scheduler.TryPost(new BudgetCoalescedEvent { Id = 4, Value = 40 });

        var stats = scheduler.Pump();
        Assert.That(stats.ProcessedCount, Is.LessThanOrEqualTo(3));
        Assert.That(scheduler.HasPendingWork, Is.True);
    }

    [Test]
    public void Repeated_pumps_fully_drain_special_events()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 2, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        scheduler.TryPost(default(BudgetDirtyEvent));

        scheduler.TryPostLatest(new BudgetLatestEvent { Value = 1 });
        scheduler.TryPostLatest(new BudgetLatestEvent { Value = 2 });
        scheduler.TryPostLatest(new BudgetLatestEvent { Value = 3 });

        scheduler.TryPost(new BudgetCoalescedEvent { Id = 1, Value = 10 });
        scheduler.TryPost(new BudgetCoalescedEvent { Id = 2, Value = 20 });
        scheduler.TryPost(new BudgetCoalescedEvent { Id = 3, Value = 30 });
        scheduler.TryPost(new BudgetCoalescedEvent { Id = 4, Value = 40 });

        int totalProcessed = 0;
        while (scheduler.HasPendingWork)
        {
            totalProcessed += scheduler.Pump().ProcessedCount;
        }

        Assert.That(totalProcessed, Is.EqualTo(6),
            "1 dirty + 1 latest + 4 coalesced = 6 events total");
    }
}
