using LayerBase.Async;

namespace EventsTest;

[TestFixture]
public class LBTaskExtensionTests
{
    [Test]
    public async Task WithCancellation_returns_source_result_when_source_wins()
    {
        var tcs = new LBTaskCompletionSource<int>();
        using var cts = new CancellationTokenSource();

        var task = tcs.Task.WithCancellation(cts.Token);
        tcs.SetResult(42);

        Assert.That(await task, Is.EqualTo(42));
    }

    [Test]
    public void WithCancellation_returns_canceled_when_token_wins()
    {
        var tcs = new LBTaskCompletionSource<int>();
        using var cts = new CancellationTokenSource();
        var task = tcs.Task.WithCancellation(cts.Token);

        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }

    [Test]
    public async Task WithTimeout_returns_source_result_when_source_wins()
    {
        var tcs = new LBTaskCompletionSource<int>();
        var task = tcs.Task.WithTimeout(TimeSpan.FromSeconds(30));

        tcs.SetResult(7);

        Assert.That(await task, Is.EqualTo(7));
    }

    [Test]
    public void WithTimeout_throws_when_timeout_wins()
    {
        var tcs = new LBTaskCompletionSource<int>();
        var task = tcs.Task.WithTimeout(TimeSpan.FromMilliseconds(10));

        Assert.ThrowsAsync<TimeoutException>(async () => await task);
    }

    [Test]
    public void Delay_observes_cancellation_after_scheduling()
    {
        using var cts = new CancellationTokenSource();
        var task = LBTask.Delay(TimeSpan.FromSeconds(30), cts.Token);

        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }

    [Test]
    public void Disposing_completion_source_does_not_recycle_pending_source_before_task_observes_completion()
    {
        var tcs = new LBTaskCompletionSource<int>();
        var task = tcs.Task;

        tcs.Dispose();

        var next = new LBTaskCompletionSource<int>();
        next.SetResult(123);

        Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }
}