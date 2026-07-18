using LayerBase.Core.Event;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class TimeSchedulerOverdueIntegrityTests
{
    private sealed class CollectingSink : IExpiredTimerSink<int>
    {
        public List<int> Expired = new();
        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            Expired.Add(payload);
            return true;
        }
    }

    [Test]
    public void Overdue_chain_three_items_over_three_ticks()
    {
        int maxExpired = 1;
        var options = new TimeSchedulerOptions(1.0f, 8, 64, 0, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var sink = new CollectingSink();

        scheduler.Schedule(10, 0);
        scheduler.Schedule(20, 0);
        scheduler.Schedule(30, 0);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10 }));
        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10, 20 }));
        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10, 20, 30 }));
    }

    [Test]
    public void Cancel_overdue_head_then_process_remaining()
    {
        int maxExpired = 1;
        var options = new TimeSchedulerOptions(1.0f, 8, 64, 0, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var sink = new CollectingSink();

        var h1 = scheduler.Schedule(10, 0);
        var h2 = scheduler.Schedule(20, 0);
        var h3 = scheduler.Schedule(30, 0);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10 }));

        bool cancelled = scheduler.Cancel(h2);
        Assert.That(cancelled, Is.True);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10, 30 }), "After cancel, only timer 30 should fire on tick2");
    }

    [Test]
    public void Cancel_overdue_tail_then_process_remaining()
    {
        int maxExpired = 1;
        var options = new TimeSchedulerOptions(1.0f, 8, 64, 0, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var sink = new CollectingSink();

        scheduler.Schedule(10, 0);
        var h2 = scheduler.Schedule(20, 0);
        var h3 = scheduler.Schedule(30, 0);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10 }));

        bool cancelledTail = scheduler.Cancel(h3);
        Assert.That(cancelledTail, Is.True, "Cancel tail (timer 30)");

        sink.Expired.Clear();
        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 20 }), "After cancel tail, timer 20 should fire on tick2");
    }
}
