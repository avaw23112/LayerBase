using System;
using System.Collections.Generic;
using LayerBase.Core.Event;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public class SchedulePostAllocationTests
{
    private struct BlittableEvent
    {
        public int X;
        public int Y;
    }

    private struct ScheduledLatestEvent
    {
        public int Id;
        public ScheduledLatestEvent(int id) { Id = id; }
    }

    private sealed class TestPostSchedulerHelper : IDisposable
    {
        public PostScheduler PostScheduler { get; }
        public EventPayloadStorage PayloadStorage { get; }
        public PostTimerScheduler Timer { get; }

        public TestPostSchedulerHelper(int runtimeId = 999)
        {
            var eventCenter = new EventCenter();
            var policyTable = new EventBuildPolicyTable();
            PayloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
            PostScheduler = new PostScheduler(
                runtimeId,
                eventCenter,
                PostSchedulerOptions.Default,
                policyTable);
            Timer = new PostTimerScheduler(
                runtimeId,
                TimeSchedulerOptions.Default,
                PayloadStorage,
                PostScheduler,
                policyTable);
        }

        public void EnsureEvent<TEvent>() where TEvent : struct
        {
            PostScheduler.PrewarmEvent<TEvent>();
            Timer.PrewarmEvent<TEvent>();
        }

        public void Dispose()
        {
            Timer.Dispose();
            PostScheduler.Dispose();
            PayloadStorage.Dispose();
        }
    }

    [Test]
    public void Schedule_post_and_cancel_is_zero_alloc_after_prewarm()
    {
        using var helper = new TestPostSchedulerHelper();
        helper.EnsureEvent<BlittableEvent>();

        for (int i = 0; i < 100; i++)
        {
            var h = helper.Timer.Schedule(new BlittableEvent { X = i, Y = i * 2 }, 5.0f);
            helper.Timer.Cancel(h);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        long iterations = 10000;

        for (int i = 0; i < iterations; i++)
        {
            var h = helper.Timer.Schedule(new BlittableEvent { X = i, Y = i * 2 }, 5.0f);
            helper.Timer.Cancel(h);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.EqualTo(0),
            string.Format("Schedule+Cancel allocated {0} bytes over {1} iterations", allocated, iterations));
    }

    [Test]
    public void Cancelled_long_timers_are_removed_from_long_heap_immediately()
    {
        using var helper = new TestPostSchedulerHelper();
        helper.EnsureEvent<BlittableEvent>();

        long iterations = 10000;

        for (int i = 0; i < iterations; i++)
        {
            var h = helper.Timer.Schedule(
                new BlittableEvent { X = i, Y = i },
                100.0f);
            helper.Timer.Cancel(h);
        }

        Assert.That(helper.Timer.PendingCount, Is.EqualTo(0),
            "PendingCount should be 0 after all timers are cancelled");
    }

    [Test]
    public void Long_timer_cancel_loop_does_not_allocate_after_capacity_is_warmed()
    {
        using var helper = new TestPostSchedulerHelper();
        helper.EnsureEvent<BlittableEvent>();

        for (int i = 0; i < 100; i++)
        {
            var h = helper.Timer.Schedule(new BlittableEvent { X = i, Y = i }, 100.0f);
            helper.Timer.Cancel(h);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        long iterations = 10000;

        for (int i = 0; i < iterations; i++)
        {
            var h = helper.Timer.Schedule(new BlittableEvent { X = i, Y = i }, 100.0f);
            helper.Timer.Cancel(h);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.EqualTo(0),
            string.Format("Long timer cancel loop allocated {0} bytes over {1} iterations", allocated, iterations));
    }

    [Test]
    public void Schedule_post_and_expire_using_existing_capacity()
    {
        using var helper = new TestPostSchedulerHelper();
        helper.EnsureEvent<BlittableEvent>();

        for (int i = 0; i < 100; i++)
        {
            _ = helper.Timer.Schedule(new BlittableEvent { X = i, Y = i }, 0.001f);
            helper.Timer.Tick(0.02f);
            _ = helper.PostScheduler.Pump();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        long iterations = 1000;

        for (int i = 0; i < iterations; i++)
        {
            _ = helper.Timer.Schedule(new BlittableEvent { X = i, Y = i }, 0.001f);
            helper.Timer.Tick(0.02f);
            _ = helper.PostScheduler.Pump();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.EqualTo(0),
            string.Format("Schedule+Expire+Pump allocated {0} bytes over {1} iterations", allocated, iterations));
    }

    [Test]
    public void Timer_payload_is_released_on_schedule_failure()
    {
        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        using var postScheduler = new PostScheduler(
            0, new EventCenter(),
            PostSchedulerOptions.Default,
            new EventBuildPolicyTable());

        var timer = new PostTimerScheduler(
            0,
            TimeSchedulerOptions.Default,
            payloadStorage,
            postScheduler,
            new EventBuildPolicyTable());

        var handle = timer.Schedule(new BlittableEvent { X = 1, Y = 2 }, 0.001f);
        timer.Tick(0.02f);
        timer.Dispose();
        postScheduler.Dispose();

        var diag = payloadStorage.CaptureDiagnostics();
        Assert.That(diag.Rented, Is.EqualTo(diag.Returned),
            string.Format("All rented payloads should be returned: Rented={0}, Returned={1}", diag.Rented, diag.Returned));
        Assert.That(diag.Outstanding, Is.EqualTo(0), "No outstanding payloads after dispose");
    }

    [Flags]
    public enum TimerReleaseScenario
    {
        Expired,
        Cancelled,
        Disposed
    }

    [TestCase(TimerReleaseScenario.Expired)]
    [TestCase(TimerReleaseScenario.Cancelled)]
    [TestCase(TimerReleaseScenario.Disposed)]
    public void Timer_payload_is_released_exactly_once(TimerReleaseScenario scenario)
    {
        using var payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Atomic);
        using var postScheduler = new PostScheduler(
            0, new EventCenter(),
            PostSchedulerOptions.Default,
            new EventBuildPolicyTable());

        var timer = new PostTimerScheduler(
            0,
            TimeSchedulerOptions.Default,
            payloadStorage,
            postScheduler,
            new EventBuildPolicyTable());

        var diagBefore = payloadStorage.CaptureDiagnostics();

        switch (scenario)
        {
            case TimerReleaseScenario.Expired:
            {
                _ = timer.Schedule(new BlittableEvent { X = 1, Y = 2 }, 0.001f);
                timer.Tick(0.02f);
                break;
            }
            case TimerReleaseScenario.Cancelled:
            {
                var h = timer.Schedule(new BlittableEvent { X = 1, Y = 2 }, 1.0f);
                timer.Cancel(h);
                break;
            }
            case TimerReleaseScenario.Disposed:
            {
                _ = timer.Schedule(new BlittableEvent { X = 1, Y = 2 }, 1.0f);
                timer.Dispose();
                break;
            }
        }

        timer.Dispose();
        postScheduler.Dispose();

        var diagAfter = payloadStorage.CaptureDiagnostics();
        long rented = diagAfter.Rented - diagBefore.Rented;
        long returned = diagAfter.Returned - diagBefore.Returned;

        Assert.That(rented, Is.EqualTo(1),
            string.Format("Scenario {0}: should have rented exactly 1 payload", scenario));
        Assert.That(returned, Is.EqualTo(1),
            string.Format("Scenario {0}: should have returned exactly 1 payload", scenario));
        Assert.That(diagAfter.Outstanding, Is.EqualTo(0),
            string.Format("Scenario {0}: no outstanding payloads", scenario));
    }
}
