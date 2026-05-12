using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Event.Delay;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public class Phase5Tests
{
    public partial struct Phase5DirtySignalEvent
    {
        public int Value;
    }

    public class Phase5DirtySignalEventMeta : EventMetaData<Phase5DirtySignalEvent>
    {
        public override EventPostPolicy? PostPolicy =>
            new EventPostPolicy(PostDeliveryMode.DirtySignal, BackpressurePolicy.RejectNew, 0);
    }

    public partial struct Phase5CoalescedEvent
    {
        public int Value;
    }

    public class Phase5CoalescedEventMeta : EventMetaData<Phase5CoalescedEvent>
    {
        public override EventPostPolicy? PostPolicy =>
            new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0);

        public override bool TryMergePostEvent(ref Phase5CoalescedEvent current, in Phase5CoalescedEvent next)
        {
            current.Value += next.Value;
            return true;
        }
    }

    public partial struct Phase5CustomTimerEvent
    {
        public int Value;
    }

    public class Phase5CustomTimerEventMeta : EventMetaData<Phase5CustomTimerEvent>
    {
        public override EventTimerPolicy? TimerPolicy => new EventTimerPolicy(
            TimerRepeatMode.FixedDelay,
            TimerCatchUpPolicy.SkipMissed,
            0,
            false,
            new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0)
        );
    }

    public partial struct Phase5DefaultTtlEvent
    {
        public int Value;
    }

    public class Phase5DefaultTtlEventMeta : EventMetaData<Phase5DefaultTtlEvent>
    {
        public override EventBufferPolicy? BufferPolicy => new EventBufferPolicy(
            BufferMode.Latest,
            0.1f, // 100ms
            1,
            BufferOverflowPolicy.ReplaceLatest,
            false
        );
    }

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }


    [Test]
    public void TestDirtySignalPolicyFromMetaData()
    {
        EventMetaDataHandler.RegisterMetaData<Phase5DirtySignalEvent>(new Phase5DirtySignalEventMeta());

        var runtime = new LayerRuntime.LayersBuilder(new LayerRuntime(101))
                      .Push(new TestLayer())
                      .Build();

        int callCount = 0;
        runtime.EventCenter.SubscribeNotify<Phase5DirtySignalEvent>(0, (in Phase5DirtySignalEvent _) => callCount++);

        runtime.MarkDirty<Phase5DirtySignalEvent>();
        runtime.MarkDirty<Phase5DirtySignalEvent>();

        runtime.Pump(0);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void TestPostPolicyFromMetaData()
    {
        EventMetaDataHandler.RegisterMetaData<Phase5CoalescedEvent>(new Phase5CoalescedEventMeta());

        var runtime = new LayerRuntime.LayersBuilder(new LayerRuntime(101))
                      .Push(new TestLayer())
                      .Build();

        int callCount = 0;
        int lastValue = 0;
        runtime.EventCenter.SubscribeNotify<Phase5CoalescedEvent>(0, (in Phase5CoalescedEvent e) =>
        {
            callCount++;
            lastValue = e.Value;
        });

        runtime.PostCoalesced(new Phase5CoalescedEvent { Value = 1 });
        runtime.PostCoalesced(new Phase5CoalescedEvent { Value = 2 });
        runtime.PostCoalesced(new Phase5CoalescedEvent { Value = 3 });

        runtime.Pump(0);

        Assert.That(callCount, Is.EqualTo(1));
        Assert.That(lastValue, Is.EqualTo(6)); // 1 + 2 + 3
    }

    [Test]
    public void TestTimerPolicyFromMetaData()
    {
        EventMetaDataHandler.RegisterMetaData<Phase5CustomTimerEvent>(new Phase5CustomTimerEventMeta());

        var runtime = new LayerRuntime.LayersBuilder(new LayerRuntime(102))
                      .Push(new TestLayer())
                      .Build();

        int callCount = 0;
        runtime.EventCenter.SubscribeNotify<Phase5CustomTimerEvent>(0, (in Phase5CustomTimerEvent _) => callCount++);

        runtime.SchedulePost(new Phase5CustomTimerEvent { Value = 1 }, 0.01f);
        runtime.SchedulePost(new Phase5CustomTimerEvent { Value = 2 }, 0.01f);

        runtime.Pump(0.05f);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void TestBufferPolicyFromMetaData()
    {
        EventMetaDataHandler.RegisterMetaData<Phase5DefaultTtlEvent>(new Phase5DefaultTtlEventMeta());

        var testLayer = new TestLayer();
        var runtime = new LayerRuntime.LayersBuilder(new LayerRuntime(103))
                      .Push(testLayer)
                      .Build();

        var publisher = testLayer.GetDelayPublisher<Phase5DefaultTtlEvent>();

        publisher.Publish(new Phase5DefaultTtlEvent { Value = 1 }, 0); // Use default 0.1s

        Assert.That(publisher.TryGet(out _), Is.True);

        runtime.Pump(0.2f);

        Assert.That(publisher.TryGet(out _), Is.False);
    }

    private class TestLayer : Layer
    {
        public DelayPublisher<T> GetDelayPublisher<T>() where T : struct => (DelayPublisher<T>)SubscribeDelay<T>();
    }
}