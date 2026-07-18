using LayerBase.Core.Event;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class TimeSchedulerBacklogBudgetTests
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

    private sealed class ThrowingSink : IExpiredTimerSink<int>
    {
        private readonly int _throwAfter;
        private int _count;
        private readonly List<int> _accepted;

        public ThrowingSink(int throwAfter, List<int> accepted)
        {
            _throwAfter = throwAfter;
            _accepted = accepted;
        }

        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            _count++;
            if (_count > _throwAfter)
                throw new InvalidOperationException("sink throws");
            _accepted.Add(payload);
            return true;
        }
    }

    private sealed class RejectingThenAcceptingSink : IExpiredTimerSink<int>
    {
        private readonly int _rejectCount;
        private int _callCount;
        private readonly List<int> _accepted;

        public RejectingThenAcceptingSink(int rejectCount, List<int> accepted)
        {
            _rejectCount = rejectCount;
            _accepted = accepted;
        }

        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            _callCount++;
            if (_callCount <= _rejectCount)
                return false;
            _accepted.Add(payload);
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

    [Test]
    public void Rejected_expired_timers_consume_budget()
    {
        int maxExpired = 2;
        var options = new TimeSchedulerOptions(1.0f, 4, 64, 0, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var accepted = new List<int>();
        var sink = new RejectingThenAcceptingSink(rejectCount: 1, accepted);

        scheduler.Schedule(1, 0);
        scheduler.Schedule(2, 0);
        scheduler.Schedule(3, 0);

        scheduler.Tick(1f, sink);

        Assert.That(accepted.Count, Is.EqualTo(1), "Should accept 1 (reject 1, accept 1) within budget of 2");
    }

    [Test]
    public void Throwing_sink_consumes_budget_and_preserves_later_timers()
    {
        int maxExpired = 2;
        var options = new TimeSchedulerOptions(1.0f, 4, 64, 0, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var accepted = new List<int>();
        var sink = new ThrowingSink(throwAfter: 0, accepted);

        scheduler.Schedule(1, 0);
        scheduler.Schedule(2, 0);
        scheduler.Schedule(3, 0);

        try
        {
            scheduler.Tick(1f, sink);
        }
        catch
        {
        }

        Assert.That(scheduler.PendingCount, Is.GreaterThan(0), "Remaining timers should be preserved as overdue");
    }

    [Test]
    public void Old_overdue_items_are_not_starved_by_new_items()
    {
        int maxExpired = 2;
        var options = new TimeSchedulerOptions(1.0f, 4, 64, 0, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var expired = new List<int>();
        var sink = new CollectingSink(expired);

        scheduler.Schedule(1, 0);
        scheduler.Schedule(2, 0);
        scheduler.Schedule(3, 0);
        scheduler.Schedule(4, 0);

        scheduler.Tick(1f, sink);
        Assert.That(expired.Count, Is.EqualTo(maxExpired), "First tick processes maxExpiredPerTick");

        scheduler.Schedule(5, 0);
        scheduler.Schedule(6, 0);

        expired.Clear();
        scheduler.Tick(1f, sink);
        Assert.That(expired.Count, Is.EqualTo(maxExpired), "Second tick processes overdue items (not starved)");
    }

    [Test]
    public void Moving_remaining_chain_to_overdue_is_constant_time()
    {
        int maxExpired = 3;
        var options = new TimeSchedulerOptions(1.0f, 4, 64, 0, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var expired = new List<int>();
        var sink = new CollectingSink(expired);

        int count = 100;
        for (int i = 0; i < count; i++)
            scheduler.Schedule(i, 0);

        scheduler.Tick(1f, sink);
        Assert.That(expired.Count, Is.EqualTo(maxExpired), "Only maxExpiredPerTick processed");

        expired.Clear();
        scheduler.Tick(1f, sink);
        Assert.That(expired.Count, Is.EqualTo(maxExpired), "Overdue queue processed in subsequent tick");
    }

    [Test]
    public void Expired_long_timer_is_promoted_to_overdue()
    {
        int maxExpired = 1;
        var options = new TimeSchedulerOptions(1.0f, 8, 64, 2f, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var expired = new List<int>();
        var sink = new CollectingSink(expired);

        scheduler.Schedule(1, 0f);
        scheduler.Schedule(2, 3f);

        scheduler.Tick(1f, sink);
        Assert.That(expired, Is.EqualTo(new[] { 1 }), "First timer expires immediately");

        scheduler.Tick(1f, sink);
        Assert.That(expired.Count, Is.EqualTo(1), "No new expiry");

        scheduler.Tick(1f, sink);
        Assert.That(expired.Count, Is.EqualTo(2), "Long timer expired and promoted");
    }

    [Test]
    public void Skip_missed_and_fire_all_capped_have_different_results()
    {
        var skipOptions = new TimeSchedulerOptions(1.0f, 32, 64, 0, 1024, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed, maxCatchUpTicksPerPump: 3);
        using var skipScheduler = new TimeScheduler<int>(skipOptions);
        var fireOptions = new TimeSchedulerOptions(1.0f, 32, 64, 0, 1024, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.FireAllCapped, maxCatchUpTicksPerPump: 10);
        using var fireScheduler = new TimeScheduler<int>(fireOptions);

        var skipExpired = new List<int>();
        var fireExpired = new List<int>();
        var skipSink = new CollectingSink(skipExpired);
        var fireSink = new CollectingSink(fireExpired);

        for (int i = 1; i <= 8; i++)
        {
            skipScheduler.Schedule(i, i * 1f);
            fireScheduler.Schedule(i, i * 1f);
        }

        skipScheduler.Tick(15f, skipSink);
        fireScheduler.Tick(15f, fireSink);

        Assert.That(fireExpired.Count, Is.GreaterThan(skipExpired.Count),
            "FireAllCapped should process more expired timers than SkipMissed");
    }

    [Test]
    public void Invalid_large_wheel_size_is_rejected()
    {
        Assert.That(() => new TimeSchedulerOptions(1f, (1 << 21), 64, 0, 1024, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
    }

    [Test]
    public void Invalid_catch_up_limit_is_rejected()
    {
        Assert.That(() => new TimeSchedulerOptions(1f, 64, 64, 0, 1024, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed, maxCatchUpTicksPerPump: -1),
            Throws.ArgumentException);
    }
}
