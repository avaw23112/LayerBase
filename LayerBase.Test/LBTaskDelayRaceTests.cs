using LayerBase.Async;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class LBTaskDelayRaceTests
{
    [Test]
    public async Task Fifty_thousand_short_cancellable_delays_complete_normally()
    {
        int count = 50_000;
        var tasks = new LBTask[count];

        for (int i = 0; i < count; i++)
        {
            using var cts = new CancellationTokenSource();
            tasks[i] = LBTask.Delay(TimeSpan.FromMilliseconds(1), cts.Token);
        }

        await Task.Delay(10_000);

        int completed = 0;
        for (int i = 0; i < count; i++)
        {
            try
            {
                await tasks[i];
                completed++;
            }
            catch
            {
            }
        }

        Assert.That(LBTask.DelayHeapPendingCount, Is.Zero,
            "All delays should be resolved from the heap.");
        Assert.That(completed, Is.EqualTo(count),
            "All 50,000 delays should complete exactly once.");
    }

    [Test]
    public async Task Cancel_immediately_after_schedule_throws()
    {
        using var cts = new CancellationTokenSource();
        var task = LBTask.Delay(TimeSpan.FromDays(1), cts.Token);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }

    [Test]
    public async Task Cancel_and_timer_race_repeatedly()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var cts = new CancellationTokenSource();
            var task = LBTask.Delay(TimeSpan.FromMilliseconds(1), cts.Token);

            await Task.WhenAll(
                Task.Run(() => cts.Cancel()),
                Task.Run(async () => { await Task.Delay(1); }));

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
                $"Iteration {i}: delay should complete exactly once.");
        }

        Assert.That(LBTask.DelayHeapPendingCount, Is.Zero,
            "All delays should be resolved from the heap after race iterations.");
    }

    [Test]
    public async Task Reused_work_item_ignores_stale_lease_after_cancel()
    {
        using var cts = new CancellationTokenSource();

        for (int i = 0; i < 100; i++)
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
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("Final delay did not complete within timeout - stale lease may have interfered.");
        }
    }

    [Test]
    public async Task All_delays_resolve_with_empty_heap()
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
            catch
            {
            }
        }

        await Task.Delay(500);

        Assert.That(LBTask.DelayHeapPendingCount, Is.Zero,
            "The heap should have zero pending items after all delays resolve.");
        Assert.That(LBTask.DelayHeapPeakPendingCount, Is.GreaterThan(0),
            "Peak pending count should be greater than zero during execution.");
        Assert.That(LBTask.DelayHeapLockContentionCount, Is.GreaterThan(0),
            "Lock contention count should be greater than zero during execution.");
    }

    [Test]
    public async Task No_unexpected_exceptions_from_race_conditions()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var cts = new CancellationTokenSource();
            var task = LBTask.Delay(TimeSpan.FromMilliseconds(1), cts.Token);

            cts.Cancel();

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception at iteration {i}: {ex.GetType().Name}: {ex.Message}");
            }
        }
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
