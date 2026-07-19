using LayerBase.Core.Event;
using NUnit.Framework;
using System.Reflection;

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

    [Test]
    public void Canceling_overdue_head_does_not_corrupt_list()
    {
        int maxExpired = 1;
        var options = new TimeSchedulerOptions(1.0f, 8, 64, 0, maxExpired, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        var sink = new CollectingSink();

        scheduler.Schedule(10, 0);
        scheduler.Schedule(20, 0);
        var h3 = scheduler.Schedule(30, 0);
        scheduler.Schedule(40, 0);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10 }));

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10, 20 }));

        Assert.That(scheduler.Cancel(h3), Is.True);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 10, 20, 40 }));
    }

    [Test]
    public void Catchup_timer_does_not_starve_old_overdue()
    {
        int maxExpired = 1;
        var options = new TimeSchedulerOptions(1.0f, 8, 64, 0, maxExpired, 64,
            TimerRepeatMode.FixedRate, TimerCatchUpPolicy.FireAllCapped);
        using var scheduler = new TimeScheduler<int>(options);
        var sink = new CollectingSink();

        var catchup = scheduler.Schedule(
            1,
            delaySeconds: 1,
            repeatCount: 2,
            intervalSeconds: 1,
            repeatMode: TimerRepeatMode.FixedRate,
            catchUpPolicy: TimerCatchUpPolicy.FireAllCapped);
        scheduler.Schedule(2, delaySeconds: 1);

        SetExpireTickForTest(scheduler, catchup, 0);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 1 }));

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired, Is.EqualTo(new[] { 1, 2 }));
    }

    private static void SetExpireTickForTest(TimeScheduler<int> scheduler, TimerHandle handle, long expireTick)
    {
        var poolField = typeof(TimeScheduler<int>).GetField("_pool", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Cannot find TimeScheduler pool.");
        var pool = (Array)(poolField.GetValue(scheduler)
            ?? throw new InvalidOperationException("TimeScheduler pool is null."));
        object entry = pool.GetValue(handle.Index)
            ?? throw new InvalidOperationException("Timer entry is null.");
        var expireTickField = entry.GetType().GetField("ExpireTick")
            ?? throw new InvalidOperationException("Cannot find TimerEntry.ExpireTick.");
        expireTickField.SetValue(entry, expireTick);
        pool.SetValue(entry, handle.Index);
    }
}
