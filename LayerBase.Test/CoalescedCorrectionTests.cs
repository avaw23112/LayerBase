using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public class CoalescedCorrectionTests
{
    public partial struct DamageEvent
    {
        public int TargetId;
        public int Amount;
    }

    public class DamageEventMeta : EventMetaData<DamageEvent>
    {
        public override EventPostPolicy? PostPolicy => new EventPostPolicy(
            PostDeliveryMode.Coalesced, 
            BackpressurePolicy.RejectNew, 
            0, 
            MergeFailurePolicy.Reject);

        public override int GetPostCoalesceKey(in DamageEvent value) => value.TargetId;

        public override bool TryMergePostEvent(ref DamageEvent current, in DamageEvent next)
        {
            if (current.TargetId != next.TargetId) return false;
            current.Amount += next.Amount;
            return true;
        }
    }

    public partial struct DirtyEvent { }
    public class DirtyEventMeta : EventMetaData<DirtyEvent>
    {
        public override EventPostPolicy? PostPolicy => new EventPostPolicy(
            PostDeliveryMode.DirtySignal, 
            BackpressurePolicy.RejectNew, 
            0);
    }

    [SetUp]
    public void SetUp()
    {
        EventMetaDataHandler.Clear();
        EventMetaDataHandler.RegisterMetaData<DamageEvent>(new DamageEventMeta());
        EventMetaDataHandler.RegisterMetaData<DirtyEvent>(new DirtyEventMeta());
    }

    [Test]
    public void Test_True_Data_Coalescing()
    {
        var runtime = LayerHub.CreateLayers().Push(new TestLayer()).Build();
        var received = new List<DamageEvent>();
        runtime.EventCenter.SubscribeNotify<DamageEvent>(0, (in DamageEvent e) => received.Add(e));

        runtime.PostCoalesced(new DamageEvent { TargetId = 1, Amount = 10 });
        runtime.PostCoalesced(new DamageEvent { TargetId = 2, Amount = 20 });
        runtime.PostCoalesced(new DamageEvent { TargetId = 1, Amount = 15 });

        runtime.Pump(0);

        Assert.That(received.Count, Is.EqualTo(2));
        Assert.That(received.Any(e => e.TargetId == 1 && e.Amount == 25), Is.True);
        Assert.That(received.Any(e => e.TargetId == 2 && e.Amount == 20), Is.True);
        
        // Ordering: target 1 was first, target 2 was second
        Assert.That(received[0].TargetId, Is.EqualTo(1));
        Assert.That(received[1].TargetId, Is.EqualTo(2));
    }

    [Test]
    public void Test_DirtySignal_Semantics()
    {
        var runtime = LayerHub.CreateLayers().Push(new TestLayer()).Build();
        int callCount = 0;
        runtime.EventCenter.SubscribeNotify<DirtyEvent>(0, (in DirtyEvent _) => callCount++);

        runtime.MarkDirty<DirtyEvent>();
        runtime.MarkDirty<DirtyEvent>();
        runtime.MarkDirty<DirtyEvent>();

        runtime.Pump(0);

        Assert.That(callCount, Is.EqualTo(1));
    }

    private class TestLayer : Layer { }
}
