using LayerBase.Core.Event;
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

	public struct TimerPayload
	{
		public int Id { get; set; }
	}
}
