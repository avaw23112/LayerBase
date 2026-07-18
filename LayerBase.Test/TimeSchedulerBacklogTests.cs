using LayerBase.Core.Event;

namespace EventsTest;

[TestFixture]
[Category("ProductionHardening")]
public sealed class TimeSchedulerBacklogTests
{
    private sealed class CollectingSink : IExpiredTimerSink<int>
    {
        private readonly List<int> _expired;

        public CollectingSink(List<int> expired) => _expired = expired;

        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            _expired.Add(payload);
            return true;
        }
    }

    [Test]
    public void Same_tick_timers_expire_in_schedule_order()
    {
        var options = new TimeSchedulerOptions(
            1.0f, 4, 64, 0, 1024, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var expired = new List<int>();
        var sink = new CollectingSink(expired);

        scheduler.Schedule(1, 0.5f);
        scheduler.Schedule(2, 0.5f);
        scheduler.Schedule(3, 0.5f);

        scheduler.Tick(1f, sink);

        Assert.That(expired, Is.EqualTo(new[] { 1, 2, 3 }));
    }
}
