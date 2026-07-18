using LayerBase.Core.DataStruct;
using LayerBase.Core.Event;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
[Category("ProductionHardening")]
public sealed class TimeSchedulerBoundaryTests
{
    [Test]
    public void Invalid_timer_options_are_rejected()
    {
        Assert.That(() => new TimeSchedulerOptions(0, 512, 256, 0, 1024, 64, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
        Assert.That(() => new TimeSchedulerOptions(float.NaN, 512, 256, 0, 1024, 64, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
        Assert.That(() => new TimeSchedulerOptions(1 / 60f, 512, 0, 0, 1024, 64, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
        Assert.That(() => new TimeSchedulerOptions(1 / 60f, 512, 256, 0, 0, 64, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
        Assert.That(() => new TimeSchedulerOptions(1 / 60f, 512, 256, 0, 1024, 0, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
    }

    [Test]
    public void Next_power_of_two_rejects_overflow()
    {
        Assert.That(() => BitHelper.NextPowerOfTwo((1 << 30) + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(BitHelper.NextPowerOfTwo(3), Is.EqualTo(4));
        Assert.That(BitHelper.NextPowerOfTwo(1), Is.EqualTo(1));
        Assert.That(BitHelper.NextPowerOfTwo(0), Is.EqualTo(1));
    }

    [Test]
    public void Non_finite_delta_time_is_rejected()
    {
        using var scheduler = new TimeScheduler<int>(TimeSchedulerOptions.Default);
        Assert.That(() => scheduler.Tick(float.NaN, null), Throws.ArgumentException);
        Assert.That(() => scheduler.Tick(float.NegativeInfinity, null), Throws.ArgumentException);
        Assert.That(() => scheduler.Tick(float.PositiveInfinity, null), Throws.ArgumentException);
        Assert.That(() => scheduler.Tick(-1f, null), Throws.ArgumentException);
    }

    [Test]
    public void Catch_up_is_limited_per_pump()
    {
        var options = new TimeSchedulerOptions(
            1 / 60f, 512, 256, 0, 1024, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        scheduler.Tick(100f / 60f, null);
        Assert.That(scheduler.PendingCount, Is.LessThan(100));
    }
}
