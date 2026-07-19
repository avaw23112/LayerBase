using LayerBase.Async;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class LBTaskDelayRuntimeTests
{
    [Test]
    public void LBTaskDelay_uses_no_custom_delay_thread()
    {
        var source = File.ReadAllText(FindRepositoryFile("LayerBase.Task", "LBTask.cs"));

        Assert.That(source, Does.Contain("Task.Delay"));
        Assert.That(source, Does.Not.Contain("DelayScheduler"));
        Assert.That(source, Does.Not.Contain("DelayWorkItem"));
        Assert.That(source, Does.Not.Contain("DelayHeap"));
        Assert.That(source, Does.Not.Contain("s_heap"));
        Assert.That(source, Does.Not.Contain("new Timer("));
    }

    [Test]
    public void Delay_cancellation_completes()
    {
        using var cts = new CancellationTokenSource();
        var delay = LBTask.Delay(TimeSpan.FromMinutes(10), cts.Token);

        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await AwaitWithTimeout(delay, TimeSpan.FromSeconds(2)));
    }

    [Test]
    public async Task Delay_completes_normally()
    {
        await AwaitWithTimeout(LBTask.Delay(TimeSpan.FromMilliseconds(10)), TimeSpan.FromSeconds(2));
    }

    private static async Task AwaitWithTimeout(LBTask task, TimeSpan timeout)
    {
        var awaiter = task.GetAwaiter();
        if (awaiter.IsCompleted)
        {
            awaiter.GetResult();
            return;
        }

        var completion = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        awaiter.OnCompleted(() =>
        {
            try
            {
                awaiter.GetResult();
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        var timeoutTask = Task.Delay(timeout);
        var completed = await Task.WhenAny(completion.Task, timeoutTask);
        if (!ReferenceEquals(completed, completion.Task))
            Assert.Fail("LBTask.Delay did not complete before the test timeout.");

        await completion.Task;
    }

    private static string FindRepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        Assert.Fail($"Could not find repository file: {Path.Combine(parts)}");
        return string.Empty;
    }
}
