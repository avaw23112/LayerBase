using LayerBase.Core.Event;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public class TimerDynamicEventPlanTests
{
    private struct DynamicEventA { public int X; }
    private struct DynamicEventB { public int Y; }

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Schedule_post_for_event_first_seen_after_build_succeeds()
    {
        int idA = EventTypeId<DynamicEventA>.Id;
        var policyTable = new EventBuildPolicyTable();
        var eventCenter = new EventCenter();
        using var scheduler = new PostScheduler(0, eventCenter, PostSchedulerOptions.Default, policyTable);
        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        var timer = new PostTimerScheduler(0, TimeSchedulerOptions.Default, payloadStorage, scheduler);

        timer.CompilePlans(policyTable, idA);
        timer.PrewarmEvent<DynamicEventA>();

        var handle = timer.Schedule(new DynamicEventB { Y = 42 }, 0.001f);
        Assert.That(handle.IsInvalid, Is.False);

        timer.Tick(0.02f);
        var stats = scheduler.Pump();
        Assert.That(stats.ProcessedCount, Is.EqualTo(1));

        timer.Dispose();
    }

    [Test]
    public void Dynamic_timer_event_uses_default_policy()
    {
        var policyTable = new EventBuildPolicyTable();
        var eventCenter = new EventCenter();
        using var scheduler = new PostScheduler(0, eventCenter, PostSchedulerOptions.Default, policyTable);
        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        var timer = new PostTimerScheduler(0, TimeSchedulerOptions.Default, payloadStorage, scheduler);

        timer.CompilePlans(policyTable, -1);

        var handle = timer.Schedule(new DynamicEventA { X = 10 }, 0.001f);
        Assert.That(handle.IsInvalid, Is.False);

        timer.Tick(0.02f);
        var stats = scheduler.Pump();
        Assert.That(stats.ProcessedCount, Is.EqualTo(1));

        timer.Dispose();
    }

    [Test]
    public void Dynamic_timer_event_does_not_leak_payload_on_failure()
    {
        var policyTable = new EventBuildPolicyTable();
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1, 1, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        var timer = new PostTimerScheduler(0, TimeSchedulerOptions.Default, payloadStorage, scheduler);

        timer.CompilePlans(policyTable, -1);

        var handle = timer.Schedule(new DynamicEventA { X = 1 }, 0.001f);
        Assert.That(handle.IsInvalid, Is.False);

        timer.Tick(0.02f);

        var diag = payloadStorage.CaptureDiagnostics();
        Assert.That(diag.Outstanding, Is.EqualTo(0));

        timer.Dispose();
    }

    [Test]
    public void Timer_plan_array_grows_geometrically()
    {
        var policyTable = new EventBuildPolicyTable();
        var eventCenter = new EventCenter();
        using var scheduler = new PostScheduler(0, eventCenter, PostSchedulerOptions.Default, policyTable);
        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        var timer = new PostTimerScheduler(0, TimeSchedulerOptions.Default, payloadStorage, scheduler);

        timer.CompilePlans(policyTable, -1);

        int scheduleCount = 100;
        for (int i = 0; i < scheduleCount; i++)
        {
            var handle = timer.Schedule(new DynamicEventA { X = i }, 0.001f);
            Assert.That(handle.IsInvalid, Is.False);
        }

        timer.Tick(0.02f);
        var stats = scheduler.Pump();
        Assert.That(stats.ProcessedCount, Is.EqualTo(scheduleCount));

        timer.Dispose();
    }

    [Test]
    public void Existing_compiled_timer_policy_is_preserved_after_growth()
    {
        var policyTable = new EventBuildPolicyTable();
        int idA = EventTypeId<DynamicEventA>.Id;
        int idB = EventTypeId<DynamicEventB>.Id;

        policyTable.SetTimerPolicy(idA, new EventTimerPolicy(
            TimerRepeatMode.FixedRate,
            TimerCatchUpPolicy.FireAllCapped,
            0, false,
            new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0)));

        var eventCenter = new EventCenter();
        using var scheduler = new PostScheduler(0, eventCenter, PostSchedulerOptions.Default, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0, PostSchedulerOptions.Default.DefaultBackpressure),
        });

        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        var timer = new PostTimerScheduler(0, TimeSchedulerOptions.Default, payloadStorage, scheduler);
        timer.CompilePlans(policyTable, idA);
        timer.PrewarmEvent<DynamicEventA>();

        int received = 0;
        eventCenter.SubscribeNotify<DynamicEventA>(0, (in DynamicEventA v) => { received++; });

        _ = timer.Schedule(new DynamicEventA { X = 10 }, 0.001f);
        _ = timer.Schedule(new DynamicEventA { X = 20 }, 0.001f);
        _ = timer.Schedule(new DynamicEventB { Y = 2 }, 0.001f);

        timer.Tick(0.02f);
        var stats = scheduler.Pump();
        Assert.That(received, Is.EqualTo(1), "DynamicEventA handler should be called exactly once (Latest mode)");
        Assert.That(stats.ProcessedCount, Is.EqualTo(2), "Latest-A + Normal-B should dispatch");

        timer.Dispose();
    }
}
