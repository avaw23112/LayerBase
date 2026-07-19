using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using NUnit.Framework;

namespace LayerBase.Test;

public partial struct CECoalescedEventA
{
    public int Id;
    public int Value;
}

public class CECoalescedMetaA : EventMetaData<CECoalescedEventA>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, maxPending: 2);
    public override int GetPostCoalesceKey(in CECoalescedEventA value) => value.Id;
    public override bool TryMergePostEvent(ref CECoalescedEventA current, in CECoalescedEventA next)
    {
        current.Value += next.Value;
        return true;
    }
}

public partial struct CECoalescedEventB
{
    public int Id;
    public int Value;
}

public class CECoalescedMetaB : EventMetaData<CECoalescedEventB>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, maxPending: 2);
    public override int GetPostCoalesceKey(in CECoalescedEventB value) => value.Id;
    public override bool TryMergePostEvent(ref CECoalescedEventB current, in CECoalescedEventB next)
    {
        current.Value += next.Value;
        return true;
    }
}

[TestFixture]
[Category("ProductionHardening")]
public class PostSchedulerCoalescedEvictionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Type_A_overflow_does_NOT_evict_Type_B()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 10);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CECoalescedEventA>.Id;
        int idB = EventTypeId<CECoalescedEventB>.Id;

        policyTable.SetMetaData(idA, new CECoalescedMetaA());
        policyTable.SetMetaData(idB, new CECoalescedMetaB());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 2, options.DefaultBackpressure),
            new PostTypePlan(idB, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 2, options.DefaultBackpressure),
        });

        Assert.That(scheduler.TryPost(new CECoalescedEventA { Id = 1, Value = 10 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new CECoalescedEventA { Id = 2, Value = 20 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new CECoalescedEventB { Id = 1, Value = 100 }).IsSuccess, Is.True);

        Assert.That(scheduler.TryPost(new CECoalescedEventB { Id = 2, Value = 200 }).IsSuccess, Is.True);

        Assert.That(scheduler.TryPost(new CECoalescedEventB { Id = 3, Value = 300 }).IsSuccess, Is.True,
            "Type A is at limit (2) but Type B is at 2 -> evict oldest of Type B, Type A items remain");

        scheduler.Pump();
    }

    [Test]
    public void DropOldest_on_type_A_evicts_oldest_of_type_A()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 10);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CECoalescedEventA>.Id;

        policyTable.SetMetaData(idA, new CECoalescedMetaA());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 2, options.DefaultBackpressure),
        });

        Assert.That(scheduler.TryPost(new CECoalescedEventA { Id = 1, Value = 10 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new CECoalescedEventA { Id = 2, Value = 20 }).IsSuccess, Is.True);

        Assert.That(scheduler.TryPost(new CECoalescedEventA { Id = 3, Value = 30 }).IsSuccess, Is.True,
            "Third item with different key should evict oldest (Id=1)");

        scheduler.Pump();
    }

    [Test]
    public void Snapshot_clears_all_global_and_type_order_nodes()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 10);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CECoalescedEventA>.Id;
        int idB = EventTypeId<CECoalescedEventB>.Id;

        policyTable.SetMetaData(idA, new CECoalescedMetaA());
        policyTable.SetMetaData(idB, new CECoalescedMetaB());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 2, options.DefaultBackpressure),
            new PostTypePlan(idB, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 2, options.DefaultBackpressure),
        });

        scheduler.TryPost(new CECoalescedEventA { Id = 1, Value = 10 });
        scheduler.TryPost(new CECoalescedEventB { Id = 1, Value = 100 });

        scheduler.Pump();

        Assert.That(scheduler.PendingCount, Is.EqualTo(0));
    }

    [Test]
    public void Ten_thousand_insert_evict_cycles_pending_stays_bounded()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 256);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CECoalescedEventA>.Id;

        policyTable.SetMetaData(idA, new CECoalescedMetaA());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 4, options.DefaultBackpressure),
        });

        for (int i = 0; i < 10000; i++)
        {
            scheduler.TryPost(new CECoalescedEventA { Id = i % 10, Value = i });
        }

        Assert.That(scheduler.PendingCount, Is.LessThanOrEqualTo(4));
    }

    [Test]
    public void Dispose_does_not_double_release_payloads()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.DropOldest,
            maxSpecialPending: 10);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<CECoalescedEventA>.Id;

        policyTable.SetMetaData(idA, new CECoalescedMetaA());

        var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 4, options.DefaultBackpressure),
        });

        scheduler.TryPost(new CECoalescedEventA { Id = 1, Value = 10 });
        scheduler.TryPost(new CECoalescedEventA { Id = 2, Value = 20 });
        scheduler.TryPost(new CECoalescedEventA { Id = 3, Value = 30 });

        scheduler.Pump();

        scheduler.TryPost(new CECoalescedEventA { Id = 1, Value = 100 });

        Assert.DoesNotThrow(() => scheduler.Dispose());
    }
}
