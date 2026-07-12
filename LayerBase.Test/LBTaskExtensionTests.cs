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

    [Test]
    public void SynchronizationContext_send_should_complete_when_disposed_before_update()
    {
        using var started = new ManualResetEventSlim(false);
        using var completed = new ManualResetEventSlim(false);
        using var context = LayerBaseSynchronizationContext.Install();
        Exception? observed = null;

        var thread = new Thread(() =>
        {
            started.Set();
            try
            {
                context.Send(static _ => { }, null);
            }
            catch (Exception ex)
            {
                observed = ex;
            }
            finally
            {
                completed.Set();
            }
        })
        {
            IsBackground = true
        };

        thread.Start();
        Assert.That(started.Wait(TimeSpan.FromSeconds(1)), Is.True);
        Thread.Sleep(50);

        context.Dispose();

        Assert.That(completed.Wait(TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(observed, Is.TypeOf<ObjectDisposedException>());
    }
}
