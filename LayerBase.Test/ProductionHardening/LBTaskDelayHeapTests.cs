using LayerBase.Async;

namespace EventsTest.ProductionHardening;

[TestFixture]
[Category("ProductionHardening")]
public sealed class LBTaskDelayHeapTests
{
    [Test]
    public async Task Delay_heap_indexes_remain_valid_after_swaps()
    {
        using var cts = new CancellationTokenSource();
        var delays = new LBTask[20];

        for (var i = 0; i < delays.Length; i++)
            delays[i] = LBTask.Delay(TimeSpan.FromSeconds(30), cts.Token);

        cts.Cancel();

        var completed = 0;
        for (var i = 0; i < delays.Length; i++)
        {
            try
            {
                await delays[i];
            }
            catch (OperationCanceledException)
            {
                completed++;
            }
        }

        Assert.That(completed, Is.EqualTo(delays.Length));
    }

    [Test]
    public async Task Delay_cancel_removes_item_by_heap_index()
    {
        using var cts = new CancellationTokenSource();
        var delays = new LBTask[10];

        for (var i = 0; i < delays.Length; i++)
            delays[i] = LBTask.Delay(TimeSpan.FromSeconds(30), cts.Token);

        for (var i = 0; i < delays.Length - 1; i++)
            cts.Token.Register(static () => { });

        cts.Cancel();

        var completed = 0;
        for (var i = 0; i < delays.Length; i++)
        {
            try
            {
                await delays[i];
            }
            catch (OperationCanceledException)
            {
                completed++;
            }
        }

        Assert.That(completed, Is.EqualTo(delays.Length));
    }

    [Test]
    public void Cancelled_delay_completes_once()
    {
        using var cts = new CancellationTokenSource();
        var task = LBTask.Delay(TimeSpan.FromSeconds(30), cts.Token);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }

    [Test]
    public async Task Reused_delay_item_ignores_stale_cancellation()
    {
        using var cts = new CancellationTokenSource();

        for (var i = 0; i < 5; i++)
        {
            var task = LBTask.Delay(TimeSpan.FromSeconds(30), cts.Token);
            cts.Token.Register(static () => { });
        }

        cts.Cancel();
        using var cts2 = new CancellationTokenSource();
        var task2 = LBTask.Delay(TimeSpan.FromMilliseconds(1), cts2.Token);
        await task2;

        Assert.Pass();
    }
}
