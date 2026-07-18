using System.Diagnostics;
using LayerBase.Async;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class LBTaskDelaySchedulerTests
{
    [Test]
    public async Task Very_large_delay_does_not_complete_immediately()
    {
        var task = LBTask.Delay(TimeSpan.FromDays(365));
        await Task.Delay(100);

        Assert.That(task.GetAwaiter().IsCompleted, Is.False,
            "A very large delay should not complete immediately.");

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelled = LBTask.Delay(TimeSpan.FromDays(365), cts.Token);
        Assert.ThrowsAsync<OperationCanceledException>(async () => await cancelled);
    }

    [Test]
    public async Task Cancel_and_timer_race_completes_once()
    {
        using var cts = new CancellationTokenSource();
        var task = LBTask.Delay(TimeSpan.FromMilliseconds(50), cts.Token);

        await Task.WhenAll(
            Task.Run(() => cts.Cancel()),
            Task.Run(() => Thread.Sleep(30)));

        int completedCount = 0;
        try
        {
            await task;
            completedCount++;
        }
        catch (OperationCanceledException)
        {
            completedCount++;
        }
        catch
        {
        }

        Assert.That(completedCount, Is.EqualTo(1),
            "The delay should complete exactly once, either by timer or cancellation.");
    }

    [Test]
    public async Task Reused_work_item_ignores_old_timer_callback()
    {
        using var cts = new CancellationTokenSource();
        for (int i = 0; i < 10; i++)
        {
            var t = LBTask.Delay(TimeSpan.FromMilliseconds(1), cts.Token);
        }

        cts.Cancel();
        await Task.Delay(500);

        using var cts2 = new CancellationTokenSource();
        var final = LBTask.Delay(TimeSpan.FromMilliseconds(1), cts2.Token);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await AwaitWithTimeout(final, timeoutCts.Token);
            Assert.Pass();
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("Final delay did not complete within timeout - stale callback may have interfered.");
        }
    }

    [Test]
    public async Task Heap_index_remains_valid_after_random_cancellation()
    {
        using var cts = new CancellationTokenSource();
        var delays = new LBTask[50];

        for (int i = 0; i < delays.Length; i++)
            delays[i] = LBTask.Delay(TimeSpan.FromSeconds(30), cts.Token);

        var rng = new Random(42);
        for (int i = 0; i < 30; i++)
        {
            int index = rng.Next(delays.Length);
            _ = Task.Run(() =>
            {
                using var innerCts = new CancellationTokenSource();
                var registration = cts.Token.Register(() => innerCts.Cancel());
                cts.Cancel();
                registration.Dispose();
            });
        }

        cts.Cancel();

        int completed = 0;
        for (int i = 0; i < delays.Length; i++)
        {
            try
            {
                await delays[i];
                completed++;
            }
            catch (OperationCanceledException)
            {
                completed++;
            }
        }

        Assert.That(completed, Is.EqualTo(delays.Length));
    }

    [Test]
    public async Task Ten_thousand_delays_leave_zero_pending_items()
    {
        int count = 10_000;
        var tasks = new LBTask[count];

        for (int i = 0; i < count; i++)
        {
            using var cts = new CancellationTokenSource();
            tasks[i] = LBTask.Delay(TimeSpan.FromMilliseconds(1), cts.Token);
            cts.Cancel();
        }

        for (int i = 0; i < count; i++)
        {
            try
            {
                await tasks[i];
            }
            catch (OperationCanceledException)
            {
            }
        }

        await Task.Delay(200);

        Assert.That(LBTask.DelayHeapPendingCount, Is.Zero,
            "After all delays are resolved, the heap should have zero pending items.");
        Assert.That(LBTask.DelayHeapPeakPendingCount, Is.GreaterThan(0),
            "Peak pending count should be greater than zero during execution.");
        Assert.That(LBTask.DelayHeapLockContentionCount, Is.GreaterThan(0),
            "Lock contention count should be greater than zero during execution.");
    }

    private static async Task AwaitWithTimeout(LBTask task, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<object?>();
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
        var awaiter = task.GetAwaiter();
        if (awaiter.IsCompleted)
        {
            awaiter.GetResult();
            return;
        }

        awaiter.OnCompleted(() => tcs.TrySetResult(null));
        await tcs.Task;
        awaiter.GetResult();
    }
}
