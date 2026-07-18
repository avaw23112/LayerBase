using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using NUnit.Framework;

namespace LayerBase.Test;

public partial struct CLCoalescedEventA
{
    public int Id;
    public int Value;
}

public class CLCoalescedMetaA : EventMetaData<CLCoalescedEventA>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, maxPending: 1);
    public override int GetPostCoalesceKey(in CLCoalescedEventA value) => value.Id;
    public override bool TryMergePostEvent(ref CLCoalescedEventA current, in CLCoalescedEventA next)
    {
        current.Value += next.Value;
        return true;
    }
}

public partial struct CLCoalescedEventB
{
    public int Id;
    public int Value;
}

public class CLCoalescedMetaB : EventMetaData<CLCoalescedEventB>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, maxPending: 2);
    public override int GetPostCoalesceKey(in CLCoalescedEventB value) => value.Id;
    public override bool TryMergePostEvent(ref CLCoalescedEventB current, in CLCoalescedEventB next)
    {
        current.Value += next.Value;
        return true;
    }
}

[TestFixture]
[Category("ProductionHardening")]
public class PostSchedulerCoalescedLimitTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Event_a_limit_does_not_reject_first_event_b_item()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 10);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CLCoalescedEventA>.Id;
        int idB = EventTypeId<CLCoalescedEventB>.Id;

        policyTable.SetMetaData(idA, new CLCoalescedMetaA());
        policyTable.SetMetaData(idB, new CLCoalescedMetaB());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 1, options.DefaultBackpressure),
            new PostTypePlan(idB, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 2, options.DefaultBackpressure),
        });

        var r1 = scheduler.TryPost(new CLCoalescedEventA { Id = 1, Value = 10 });
        Assert.That(r1.IsSuccess, Is.True, "First EventA should succeed");

        var r2 = scheduler.TryPost(new CLCoalescedEventB { Id = 1, Value = 100 });
        Assert.That(r2.IsSuccess, Is.True, "First EventB should succeed despite EventA having limit 1");
    }

    [Test]
    public void Per_type_drop_oldest_does_not_evict_other_event_type()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 10);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CLCoalescedEventA>.Id;
        int idB = EventTypeId<CLCoalescedEventB>.Id;

        policyTable.SetMetaData(idA, new CLCoalescedMetaA());
        policyTable.SetMetaData(idB, new CLCoalescedMetaB());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 1, options.DefaultBackpressure),
            new PostTypePlan(idB, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 2, options.DefaultBackpressure),
        });

        var r1 = scheduler.TryPost(new CLCoalescedEventA { Id = 1, Value = 10 });
        Assert.That(r1.IsSuccess, Is.True);

        var r2 = scheduler.TryPost(new CLCoalescedEventB { Id = 1, Value = 100 });
        Assert.That(r2.IsSuccess, Is.True);

        var r3 = scheduler.TryPost(new CLCoalescedEventB { Id = 2, Value = 200 });
        Assert.That(r3.IsSuccess, Is.True);

        var r4 = scheduler.TryPost(new CLCoalescedEventA { Id = 2, Value = 20 });
        Assert.That(r4.IsSuccess, Is.True, "Second EventA with different coalesce key should evict oldest EventA");
    }

    [Test]
    public void Global_special_limit_can_evict_global_oldest()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 2);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CLCoalescedEventA>.Id;
        int idB = EventTypeId<CLCoalescedEventB>.Id;

        policyTable.SetMetaData(idA, new CLCoalescedMetaA());
        policyTable.SetMetaData(idB, new CLCoalescedMetaB());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 0, options.DefaultBackpressure),
            new PostTypePlan(idB, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 0, options.DefaultBackpressure),
        });

        var r1 = scheduler.TryPost(new CLCoalescedEventA { Id = 1, Value = 10 });
        Assert.That(r1.IsSuccess, Is.True);

        var r2 = scheduler.TryPost(new CLCoalescedEventB { Id = 1, Value = 100 });
        Assert.That(r2.IsSuccess, Is.True);

        var r3 = scheduler.TryPost(new CLCoalescedEventA { Id = 2, Value = 20 });
        Assert.That(r3.IsSuccess, Is.True, "Global limit 2, but DropOldest should evict oldest");
    }

    [Test]
    public void Counts_return_to_zero_after_dispatch()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 10);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CLCoalescedEventA>.Id;

        policyTable.SetMetaData(idA, new CLCoalescedMetaA());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 1, options.DefaultBackpressure),
        });

        scheduler.TryPost(new CLCoalescedEventA { Id = 1, Value = 10 });
        Assert.That(scheduler.PendingCount, Is.GreaterThan(0));

        scheduler.Pump();
        Assert.That(scheduler.PendingCount, Is.EqualTo(0));
    }

    [Test]
    public void Counts_return_to_zero_after_dispose()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 10);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CLCoalescedEventA>.Id;

        policyTable.SetMetaData(idA, new CLCoalescedMetaA());

        var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 1, options.DefaultBackpressure),
        });

        scheduler.TryPost(new CLCoalescedEventA { Id = 1, Value = 10 });
        Assert.That(scheduler.PendingCount, Is.GreaterThan(0));

        scheduler.Dispose();
        Assert.That(scheduler.PendingCount, Is.EqualTo(0));
    }
}
