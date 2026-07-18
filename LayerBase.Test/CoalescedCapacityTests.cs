using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using NUnit.Framework;

namespace LayerBase.Test;

public partial struct CapacityTestEvent
{
    public int Id;
    public int Value;
}

public class CapacityTestMeta : EventMetaData<CapacityTestEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0);

    public override int GetPostCoalesceKey(in CapacityTestEvent value) => value.Id;

    public override bool TryMergePostEvent(ref CapacityTestEvent current, in CapacityTestEvent next)
    {
        current.Value += next.Value;
        return true;
    }
}

[TestFixture]
public class CoalescedCapacityTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Global_MaxSpecialPending_limits_new_coalesced_keys()
    {
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew,
            maxSpecialPending: 3);
        var eventCenter = new EventCenter();
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        var typeId = EventTypeId<CapacityTestEvent>.Id;
        policyTable.SetMetaData(typeId, new CapacityTestMeta());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(typeId, PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0,
                options.DefaultBackpressure)
        });

        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 1, Value = 10 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 2, Value = 20 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 3, Value = 30 }).IsSuccess, Is.True);

        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 4, Value = 40 }).IsSuccess, Is.False);

        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 1, Value = 5 }).IsSuccess, Is.True);
    }

    [Test]
    public void TypeLevel_MaxPending_overrides_global_maxSpecialPending()
    {
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew,
            maxSpecialPending: 10);
        var eventCenter = new EventCenter();
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        var typeId = EventTypeId<CapacityTestEvent>.Id;
        policyTable.SetMetaData(typeId, new CapacityTestMeta());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(typeId, PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 2,
                options.DefaultBackpressure)
        });

        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 1, Value = 10 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 2, Value = 20 }).IsSuccess, Is.True);

        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 3, Value = 30 }).IsSuccess, Is.False);
    }

    [Test]
    public void DropOldest_backpressure_evicts_oldest_new_key_when_at_capacity()
    {
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew,
            maxSpecialPending: 2);
        var eventCenter = new EventCenter();
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        var typeId = EventTypeId<CapacityTestEvent>.Id;
        policyTable.SetMetaData(typeId, new CapacityTestMeta());

        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(typeId, PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 0,
                options.DefaultBackpressure)
        });

        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 1, Value = 10 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 2, Value = 20 }).IsSuccess, Is.True);

        Assert.That(scheduler.TryPost(new CapacityTestEvent { Id = 3, Value = 30 }).IsSuccess, Is.True);

        int received = 0;
        eventCenter.SubscribeNotify<CapacityTestEvent>(0, (in CapacityTestEvent e) => received++);

        scheduler.Pump();
        Assert.That(received, Is.EqualTo(2));
    }
}
