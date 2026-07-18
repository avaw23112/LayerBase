using LayerBase.Core.Event;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class TimerFairnessTests
{
    [Test]
    public void Overdue_timers_are_not_starved_by_new_ones()
    {
        var scheduler = new TimeScheduler<int>(
            new TimeSchedulerOptions(
                tickDurationSeconds: 0.01f,
                wheelSize: 8,
                initialTimerCapacity: 64,
                longTimerThresholdSeconds: 1f,
                maxExpiredPerTick: 2,
                maxPromotePerTick: 8,
                defaultRepeatMode: TimerRepeatMode.FixedDelay,
                defaultCatchUpPolicy: TimerCatchUpPolicy.SkipMissed));

        var expired = new List<int>();

        // Schedule 6 overdue timers
        for (int i = 0; i < 6; i++)
        {
            scheduler.Schedule(
                100 + i,
                delaySeconds: 0f,
                repeatCount: 0);
        }

        // Single tick processes up to 2 per TickOnce
        scheduler.Tick(0.01f,
            CreateSink(e => { expired.Add(e); }));

        Assert.That(expired.Count, Is.EqualTo(2),
            "MaxExpiredPerTick=2 limits the first tick to 2.");

        // Remaining 4 are now in overdue queue
        // Schedule 2 new timers for immediate expiry
        scheduler.Schedule(200, delaySeconds: 0f, repeatCount: 0);
        scheduler.Schedule(201, delaySeconds: 0f, repeatCount: 0);

        // Second tick - should process remaining overdue (2 more) not new ones
        scheduler.Tick(0.01f,
            CreateSink(e => { expired.Add(e); }));

        Assert.That(expired.Count, Is.EqualTo(4),
            "Second tick processes 2 more from overdue queue.");

        Assert.That(expired[2], Is.EqualTo(102));
        Assert.That(expired[3], Is.EqualTo(103));
    }

    private static IExpiredTimerSink<int> CreateSink(
        Action<int> onExpired)
    {
        return new TestTimerSink(onExpired);
    }

    private sealed class TestTimerSink : IExpiredTimerSink<int>
    {
        private readonly Action<int> _onExpired;

        public TestTimerSink(Action<int> onExpired)
        {
            _onExpired = onExpired;
        }

        public bool TryAcceptExpired(
            in int payload,
            TimerHandle handle)
        {
            _onExpired(payload);
            return true;
        }
    }
}
