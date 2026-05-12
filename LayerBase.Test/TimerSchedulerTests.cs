using LayerBase;
using LayerBase.Tools.Timer;

namespace EventsTest;

public class TimerSchedulerTests
{
    [Test]
    public void After_and_at_actions_execute_in_due_order()
    {
        var scheduler = new TimerScheduler();
        var log = new List<string>();

        scheduler.RegisterAfter(0.3, new TimerPayload { Id = 1 }, e => log.Add($"after:{e.Value.Id}"));
        scheduler.RegisterAt(0.6, new TimerPayload { Id = 2 }, e => log.Add($"at:{e.Value.Id}"));

        scheduler.Tick(0.3);
        scheduler.Tick(0.3);

        Assert.That(log, Is.EqualTo(new[] { "after:1", "at:2" }));
    }

    [Test]
    public void Frequency_action_runs_each_gate_and_cancel_removes_pending_task()
    {
        var scheduler = new TimerScheduler();
        var log = new List<string>();

        var token = scheduler.RegisterAfter(0.5, new TimerPayload { Id = 99 }, _ => log.Add("cancelled"));
        Assert.That(scheduler.Cancel(token), Is.True);

        scheduler.SetFrequency(0.25);
        scheduler.RegisterOnFrequency(new TimerPayload { Id = 3 }, e => log.Add($"freq:{e.Value.Id}"));

        scheduler.Tick(0.1);  // gate closed
        scheduler.Tick(0.2);  // gate opens once
        scheduler.Tick(0.25); // gate opens again

        Assert.That(log, Is.EqualTo(new[] { "freq:3", "freq:3" }));
    }

    [Test]
    public void Frequency_gate_is_open_when_frequency_is_zero()
    {
        var scheduler = new TimerScheduler();

        Assert.That(scheduler.IsFrequencyGateOpen, Is.True);

        scheduler.SetFrequency(0.25);
        scheduler.Tick(0.1);
        Assert.That(scheduler.IsFrequencyGateOpen, Is.False);

        scheduler.SetFrequency(0);
        Assert.That(scheduler.IsFrequencyGateOpen, Is.True);

        scheduler.Tick(1.0);
        Assert.That(scheduler.IsFrequencyGateOpen, Is.True);
    }

    [Test]
    public void Tick_continues_after_synchronous_timer_exception_and_reports_error()
    {
        LayerHub.Reset();
        var scheduler = new TimerScheduler();
        var log = new List<string>();
        var errors = new List<LayerEventInfo>();
        Action<LayerEventInfo> handler = info =>
        {
            if (info.Type == LayerEventInfoType.Error) errors.Add(info);
        };

        LayerHub.OnLayerEventInfo += handler;
        try
        {
            scheduler.RegisterAfter(0, new TimerPayload { Id = 1 }, _ => throw new InvalidOperationException("boom"));
            scheduler.RegisterAfter(0, new TimerPayload { Id = 2 }, e => log.Add($"after:{e.Value.Id}"));

            Assert.DoesNotThrow(() => scheduler.Tick(0));
            Assert.That(log, Is.EqualTo(new[] { "after:2" }));
            Assert.That(errors.Any(e => e.Source.Contains("TimerScheduler")), Is.True);
        }
        finally
        {
            LayerHub.OnLayerEventInfo -= handler;
        }
    }

    [Test]
    public void Frequency_callback_can_reenter_scheduler_without_deadlock()
    {
        var scheduler = new TimerScheduler();
        var log = new List<string>();

        scheduler.SetFrequency(0.1);
        scheduler.RegisterOnFrequency(new TimerPayload { Id = 1 }, _ =>
        {
            log.Add("freq");
            scheduler.RegisterAfter(0, new TimerPayload { Id = 2 }, e => log.Add($"after:{e.Value.Id}"));
        });

        var tickTask = Task.Run(() => scheduler.Tick(0.1));
        Assert.That(tickTask.Wait(TimeSpan.FromSeconds(1)), Is.True,
            "Frequency Tick should not deadlock on reentrant registration.");

        scheduler.Tick(0);

        Assert.That(log, Does.Contain("freq"));
        Assert.That(log, Does.Contain("after:2"));
    }

    [Test]
    public void Reentrant_tick_is_reported_without_corrupting_due_cache()
    {
        LayerHub.Reset();
        var scheduler = new TimerScheduler();
        var log = new List<string>();
        var errors = new List<LayerEventInfo>();
        Action<LayerEventInfo> handler = info =>
        {
            if (info.Type == LayerEventInfoType.Error) errors.Add(info);
        };

        LayerHub.OnLayerEventInfo += handler;
        try
        {
            scheduler.RegisterAfter(0, new TimerPayload { Id = 1 }, _ => scheduler.Tick(0));
            scheduler.RegisterAfter(0, new TimerPayload { Id = 2 }, e => log.Add($"after:{e.Value.Id}"));

            Assert.DoesNotThrow(() => scheduler.Tick(0));
            Assert.That(log, Is.EqualTo(new[] { "after:2" }));
            Assert.That(errors.Any(e => e.Exception is InvalidOperationException), Is.True);
        }
        finally
        {
            LayerHub.OnLayerEventInfo -= handler;
        }
    }

    public struct TimerPayload
    {
        public int Id { get; set; }
    }
}