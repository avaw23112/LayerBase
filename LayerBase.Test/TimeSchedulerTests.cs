using LayerBase.Core.Event;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class TimeSchedulerTests
{
    private class MockSink : IExpiredTimerSink<int>
    {
        public List<int> Received = new();

        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            Received.Add(payload);
            return true;
        }
    }

    [Test]
    public void Basic_Timer_Once()
    {
        // 0.1s tick, 10 slots = 1.0s span
        var options = new TimeSchedulerOptions(0.1f, 10, 64, 1.0f, 1024, 64, TimerRepeatMode.Once,
            TimerCatchUpPolicy.SkipMissed);
        var scheduler = new TimeScheduler<int>(options);
        var sink = new MockSink();

        scheduler.Schedule(1, 0.25f); // Should be tick 3 (0.3s)

        scheduler.Tick(0.2f, sink);
        Assert.That(sink.Received.Count, Is.EqualTo(0));

        scheduler.Tick(0.15f, sink);
        Assert.That(sink.Received.Count, Is.EqualTo(1));
        Assert.That(sink.Received[0], Is.EqualTo(1));
    }

    [Test]
    public void Long_Timer_Uses_Heap_And_Promotes()
    {
        var options = new TimeSchedulerOptions(0.1f, 10, 64, 1.0f, 1024, 64, TimerRepeatMode.Once,
            TimerCatchUpPolicy.SkipMissed);
        var scheduler = new TimeScheduler<int>(options);
        var sink = new MockSink();

        // Wheel span is 1.0s. 2.0s should go to heap (Tick 20).
        scheduler.Schedule(1, 2.0f);

        // Tick up to 1.0s (Tick 10)
        scheduler.Tick(1.0f, sink);
        Assert.That(sink.Received.Count, Is.EqualTo(0));

        scheduler.Tick(0.9f, sink); // Tick 19
        Assert.That(sink.Received.Count, Is.EqualTo(0));

        scheduler.Tick(0.15f, sink); // Tick 20
        Assert.That(sink.Received.Count, Is.EqualTo(1));
    }

    [Test]
    public void Cancel_Prevents_Execution()
    {
        var scheduler = new TimeScheduler<int>(TimeSchedulerOptions.Default);
        var sink = new MockSink();

        var handle = scheduler.Schedule(1, 0.1f);
        scheduler.Cancel(handle);

        scheduler.Tick(0.2f, sink);
        Assert.That(sink.Received.Count, Is.EqualTo(0));
    }

    [Test]
    public void Cancel_Long_Timer_Removes_From_Heap()
    {
        var options = new TimeSchedulerOptions(0.1f, 4, 4, 0.4f, 1024, 64, TimerRepeatMode.Once,
            TimerCatchUpPolicy.SkipMissed);
        var scheduler = new TimeScheduler<int>(options);

        var handle = scheduler.Schedule(1, 5.0f);

        Assert.That(GetLongHeapCount(scheduler), Is.EqualTo(1));
        Assert.That(scheduler.Cancel(handle), Is.True);
        Assert.That(GetLongHeapCount(scheduler), Is.EqualTo(0));
    }

    [Test]
    public void Large_Delta_CatchUp_Is_Capped_By_Default()
    {
        var options = new TimeSchedulerOptions(0.1f, 64, 64, 6.4f, 1024, 64, TimerRepeatMode.FixedRate,
            TimerCatchUpPolicy.FireAllCapped);
        var scheduler = new TimeScheduler<int>(options);
        var sink = new MockSink();

        scheduler.Schedule(1, 0.1f, repeatCount: 100, intervalSeconds: 0.1f,
            repeatMode: TimerRepeatMode.FixedRate,
            catchUpPolicy: TimerCatchUpPolicy.FireAllCapped);

        scheduler.Tick(2.0f, sink);

        Assert.That(sink.Received.Count, Is.LessThanOrEqualTo(8));
    }

    [Test]
    public void FixedDelay_Repeating_Timer()
    {
        var options = new TimeSchedulerOptions(0.1f, 100, 64, 10.0f, 1024, 64, TimerRepeatMode.FixedDelay,
            TimerCatchUpPolicy.SkipMissed);
        var scheduler = new TimeScheduler<int>(options);
        var sink = new MockSink();

        scheduler.Schedule(1, 0.1f, repeatCount: 2, intervalSeconds: 0.1f);

        scheduler.Tick(0.15f, sink); // 1st fire
        Assert.That(sink.Received.Count, Is.EqualTo(1));

        scheduler.Tick(0.1f, sink); // 2nd fire
        Assert.That(sink.Received.Count, Is.EqualTo(2));

        scheduler.Tick(0.1f, sink); // 3rd fire
        Assert.That(sink.Received.Count, Is.EqualTo(3));

        scheduler.Tick(0.1f, sink);
        Assert.That(sink.Received.Count, Is.EqualTo(3));
    }

    [Test]
    public void FixedRate_Repeating_Timer()
    {
        var options = new TimeSchedulerOptions(0.1f, 100, 64, 10.0f, 1024, 64, TimerRepeatMode.FixedRate,
            TimerCatchUpPolicy.SkipMissed);
        var scheduler = new TimeScheduler<int>(options);
        var sink = new MockSink();

        // Schedule with FixedRate
        scheduler.Schedule(1, 0.1f, repeatCount: 2, intervalSeconds: 0.1f, repeatMode: TimerRepeatMode.FixedRate);

        // Large tick jump (0.25s) to simulate lag
        scheduler.Tick(0.25f, sink); // Ticks 1 and 2 should have fired. 
        // But wait! My implementation only processes ONE slot per Tick.
        // Tick(0.25s) with 0.1s duration means 2 subticks.
        // Subtick 1: currentTick 1. Processes Slot 1.
        // Subtick 2: currentTick 2. Processes Slot 2.

        Assert.That(sink.Received.Count, Is.EqualTo(2));
    }

    private static int GetLongHeapCount(TimeScheduler<int> scheduler)
    {
        var heap = typeof(TimeScheduler<int>)
            .GetField("_longHeap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(scheduler)!;

        return (int)heap.GetType()
            .GetProperty("Count", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!
            .GetValue(heap)!;
    }
}
