using LayerBase.Core.Event;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class TimerCatchUpPolicyTests
{
    private const int TimerPayload = 999;
    private const int BlockerBase = 100;

    private sealed class CollectingSink : IExpiredTimerSink<int>
    {
        public List<int> Expired = new();
        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            Expired.Add(payload);
            return true;
        }
    }

    private static TimeSchedulerOptions MakeOptions(TimerCatchUpPolicy catchUp)
    {
        return new TimeSchedulerOptions(
            tickDurationSeconds: 1f,
            wheelSize: 4,
            initialTimerCapacity: 64,
            longTimerThresholdSeconds: 0.5f,
            maxExpiredPerTick: 4,
            maxPromotePerTick: 1,
            defaultRepeatMode: TimerRepeatMode.FixedRate,
            defaultCatchUpPolicy: catchUp,
            maxCatchUpTicksPerPump: 128);
    }

    private static void SetupBlockers(TimeScheduler<int> scheduler, int count)
    {
        for (int i = 0; i < count; i++)
            scheduler.Schedule(BlockerBase + i, delaySeconds: 2f);
    }

    private static void ScheduleRepeatTimer(TimeScheduler<int> scheduler, TimerCatchUpPolicy catchUp)
    {
        scheduler.Schedule(
            TimerPayload,
            delaySeconds: 0f,
            repeatCount: 100,
            intervalSeconds: 2f,
            repeatMode: TimerRepeatMode.FixedRate,
            catchUpPolicy: catchUp);
    }

    [Test]
    public void Fire_all_capped_replays_missed_fixed_rate_intervals()
    {
        using var scheduler = new TimeScheduler<int>(MakeOptions(TimerCatchUpPolicy.FireAllCapped));
        var sink = new CollectingSink();

        SetupBlockers(scheduler, 14);
        ScheduleRepeatTimer(scheduler, TimerCatchUpPolicy.FireAllCapped);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired.Count(e => e == TimerPayload), Is.EqualTo(1));

        for (int i = 2; i <= 22; i++)
            scheduler.Tick(1f, sink);

        int timerFires = sink.Expired.Count(e => e == TimerPayload);

        Assert.That(timerFires, Is.GreaterThan(8),
            "FireAllCapped should catch up missed intervals");
    }

    [Test]
    public void Skip_missed_only_fires_once_per_tick()
    {
        using var scheduler = new TimeScheduler<int>(MakeOptions(TimerCatchUpPolicy.SkipMissed));
        var sink = new CollectingSink();

        SetupBlockers(scheduler, 14);
        ScheduleRepeatTimer(scheduler, TimerCatchUpPolicy.SkipMissed);

        scheduler.Tick(1f, sink);
        Assert.That(sink.Expired.Count(e => e == TimerPayload), Is.EqualTo(1));

        for (int i = 2; i <= 22; i++)
            scheduler.Tick(1f, sink);

        int timerFires = sink.Expired.Count(e => e == TimerPayload);

        Assert.That(timerFires, Is.LessThan(8),
            "SkipMissed should skip missed intervals");
    }

    [Test]
    public void Fire_all_capped_catches_up_more_than_skip_missed()
    {
        using var fireScheduler = new TimeScheduler<int>(MakeOptions(TimerCatchUpPolicy.FireAllCapped));
        using var skipScheduler = new TimeScheduler<int>(MakeOptions(TimerCatchUpPolicy.SkipMissed));
        var fireSink = new CollectingSink();
        var skipSink = new CollectingSink();

        SetupBlockers(fireScheduler, 14);
        SetupBlockers(skipScheduler, 14);
        ScheduleRepeatTimer(fireScheduler, TimerCatchUpPolicy.FireAllCapped);
        ScheduleRepeatTimer(skipScheduler, TimerCatchUpPolicy.SkipMissed);

        for (int i = 1; i <= 22; i++)
        {
            fireScheduler.Tick(1f, fireSink);
            skipScheduler.Tick(1f, skipSink);
        }

        int fireTimerFires = fireSink.Expired.Count(e => e == TimerPayload);
        int skipTimerFires = skipSink.Expired.Count(e => e == TimerPayload);

        Assert.That(fireTimerFires, Is.GreaterThan(skipTimerFires),
            "FireAllCapped should catch up more intervals than SkipMissed");
    }

    [Test]
    public void Overdue_fairness_is_preserved()
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

        for (int i = 0; i < 6; i++)
            scheduler.Schedule(100 + i, delaySeconds: 0f, repeatCount: 0);

        scheduler.Tick(0.01f, CreateSink(e => { expired.Add(e); }));

        Assert.That(expired.Count, Is.EqualTo(2),
            "MaxExpiredPerTick=2 limits the first tick to 2.");

        scheduler.Schedule(200, delaySeconds: 0f, repeatCount: 0);
        scheduler.Schedule(201, delaySeconds: 0f, repeatCount: 0);

        scheduler.Tick(0.01f, CreateSink(e => { expired.Add(e); }));

        Assert.That(expired.Count, Is.EqualTo(4),
            "Second tick processes 2 more from overdue queue.");

        Assert.That(expired[2], Is.EqualTo(102));
        Assert.That(expired[3], Is.EqualTo(103));
    }

    private static IExpiredTimerSink<int> CreateSink(Action<int> onExpired)
    {
        return new TestTimerSink(onExpired);
    }

    private sealed class TestTimerSink : IExpiredTimerSink<int>
    {
        private readonly Action<int> _onExpired;
        public TestTimerSink(Action<int> onExpired) => _onExpired = onExpired;
        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            _onExpired(payload);
            return true;
        }
    }
}
