using LayerBase.Core.Event;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public class PostSchedulerLatestTests
{
    private struct TestEvent
    {
        public int Value;
        public TestEvent(int value) { Value = value; }
    }

    [Test]
    public void Direct_latest_post_should_deliver_only_last()
    {
        var eventCenter = new EventCenter();
        var policyTable = new EventBuildPolicyTable();
        int typeId = EventTypeId<TestEvent>.Id;

        var latestPolicy = new EventPostPolicy(
            PostDeliveryMode.Latest,
            BackpressurePolicy.RejectNew,
            maxPending: 1);

        using var scheduler = new PostScheduler(0, eventCenter, PostSchedulerOptions.Default, policyTable);
        scheduler.BuildPlans(new[]
        {
            PostTypePlan.FromPolicy(typeId, latestPolicy, BackpressurePolicy.RejectNew)
        });

        int received = 0, lastValue = 0;
        eventCenter.SubscribeNotify<TestEvent>(0, (in TestEvent v) => { received++; lastValue = v.Value; });

        scheduler.TryPost(new TestEvent(10));
        scheduler.TryPost(new TestEvent(20));
        scheduler.TryPost(new TestEvent(30));

        var stats = scheduler.Pump();

        Assert.That(stats.ProcessedCount, Is.EqualTo(1), "Latest should deliver exactly 1 item");
        Assert.That(received, Is.EqualTo(1), "Handler should be called once");
        Assert.That(lastValue, Is.EqualTo(30), "Handler should receive the last value");
    }

    [Test]
    public void Timer_post_without_expired_override_uses_plans_latest()
    {
        var eventCenter = new EventCenter();
        var policyTable = new EventBuildPolicyTable();
        int typeId = EventTypeId<TestEvent>.Id;

        var latestPolicy = new EventPostPolicy(
            PostDeliveryMode.Latest,
            BackpressurePolicy.RejectNew,
            maxPending: 1);

        using var scheduler = new PostScheduler(0, eventCenter, PostSchedulerOptions.Default, policyTable);
        scheduler.BuildPlans(new[]
        {
            PostTypePlan.FromPolicy(typeId, latestPolicy, BackpressurePolicy.RejectNew)
        });

        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        var timer = new PostTimerScheduler(0, TimeSchedulerOptions.Default, payloadStorage, scheduler, policyTable);
        timer.PrewarmEvent<TestEvent>();

        int received = 0, lastValue = 0;
        eventCenter.SubscribeNotify<TestEvent>(0, (in TestEvent v) => { received++; lastValue = v.Value; });

        _ = timer.Schedule(new TestEvent(100), 0.001f);
        _ = timer.Schedule(new TestEvent(200), 0.001f);
        _ = timer.Schedule(new TestEvent(300), 0.001f);

        timer.Tick(0.02f);
        var stats = scheduler.Pump();

        Assert.That(stats.ProcessedCount, Is.EqualTo(1),
            string.Format("Processed {0} items, expected 1", stats.ProcessedCount));
        Assert.That(received, Is.EqualTo(1), "Handler should be called once");
        Assert.That(lastValue, Is.EqualTo(100),
            string.Format("Handler should receive the first scheduled value (last posted due to LIFO wheel), got {0}", lastValue));
    }

    [Test]
    public void Post_then_tryPost_should_also_work_with_latest()
    {
        var eventCenter = new EventCenter();
        var policyTable = new EventBuildPolicyTable();
        int typeId = EventTypeId<TestEvent>.Id;

        var latestPolicy = new EventPostPolicy(
            PostDeliveryMode.Latest,
            BackpressurePolicy.RejectNew,
            maxPending: 1);

        using var scheduler = new PostScheduler(0, eventCenter, PostSchedulerOptions.Default, policyTable);
        scheduler.BuildPlans(new[]
        {
            PostTypePlan.FromPolicy(typeId, latestPolicy, BackpressurePolicy.RejectNew)
        });

        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        var store = payloadStorage.GetStoreFast<TestEvent>();
        store.Add(new TestEvent(100));
        store.Add(new TestEvent(200));
        store.Add(new TestEvent(300));

        int received = 0, lastValue = 0;
        eventCenter.SubscribeNotify<TestEvent>(0, (in TestEvent v) => { received++; lastValue = v.Value; });

        for (int i = 0; i < 3; i++)
        {
            var handle = new PayloadHandle(typeId, i, 1);
            var result = payloadStorage.Post(handle, scheduler);
            Assert.That(result.IsSuccess, Is.True,
                string.Format("Post of handle {0} should succeed", i));
        }

        Assert.That(scheduler.HasPendingWork, Is.True);
        var stats = scheduler.Pump();

        Assert.That(stats.ProcessedCount, Is.EqualTo(1),
            string.Format("Via payload Post: Processed {0} items, expected 1", stats.ProcessedCount));
        Assert.That(received, Is.EqualTo(1),
            string.Format("Via payload Post: Handler called {0} times, expected 1", received));
    }
}
