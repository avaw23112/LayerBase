using LayerBase.Core.Event;

namespace EventsTest;

[TestFixture]
[Category("ProductionHardening")]
public sealed class TimeSchedulerExceptionSafetyTests
{
    private sealed class ThrowingThenCollectingSink : IExpiredTimerSink<int>
    {
        private readonly List<int> _expired;
        private int _callCount;

        public ThrowingThenCollectingSink(List<int> expired) => _expired = expired;

        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            _callCount++;
            if (_callCount == 2) throw new InvalidOperationException("simulated fault");
            _expired.Add(payload);
            return true;
        }
    }

    [Test]
    public void Throwing_sink_does_not_lose_remaining_timers()
    {
        var options = new TimeSchedulerOptions(
            1.0f, 4, 64, 0, 1024, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var expired = new List<int>();

        scheduler.Schedule(1, 0.5f);
        scheduler.Schedule(2, 0.5f);
        scheduler.Schedule(3, 0.5f);

        Assert.That(() => scheduler.Tick(1f, new ThrowingThenCollectingSink(expired)),
            Throws.Exception);

        Assert.That(expired.Count, Is.GreaterThan(0));
        Assert.That(scheduler.PendingCount, Is.EqualTo(0));
    }
}
