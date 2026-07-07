using System;
using System.Threading;
using System.Threading.Tasks;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public class ConcurrencySimplifiedTests
{
    [Test]
    public async Task RunBackground_ShouldExecuteOnThreadPool()
    {
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        int backgroundThreadId = 0;

        await LBTask.RunBackground(() => { backgroundThreadId = Thread.CurrentThread.ManagedThreadId; });

        Assert.That(backgroundThreadId, Is.Not.EqualTo(0));
        Assert.That(backgroundThreadId, Is.Not.EqualTo(mainThreadId));
    }

    [Test]
    public async Task RunBackground_WithResult_ShouldReturnResult()
    {
        var result = await LBTask.RunBackground(() => { return 42; });

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void RunBackground_WithException_ShouldPropagateException()
    {
        var task = LBTask.RunBackground(() => { throw new InvalidOperationException("Test exception"); });

        Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
    }

    [Test]
    public void SwitchToMainThread_ShouldResumeOnMainThread()
    {
        var runtime = new LayerRuntime(1);
        var builder = new LayerRuntime.LayersBuilder(runtime);

        // Dummy layer to make builder happy
        builder.Push(new TestLayer());
        runtime = builder.Build();

        int resumeThreadId = 0;
        int mainThreadId = Thread.CurrentThread.ManagedThreadId;

        // Run this wrapper logic to simulate a business flow
        async LBTask Flow()
        {
            await LBTask.RunBackground(() =>
            {
                // Background work
                Thread.Sleep(10);
            });

            await LBTask.SwitchToMainThread();
            resumeThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        using (runtime.ScopeHost.MainScope.SynchronizationContext!.EnterScope())
        {
            _ = Flow();
        }

        // Pump runtime to process completion
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (resumeThreadId == 0 && !cts.IsCancellationRequested)
        {
            runtime.Pump(0.1f);
            Thread.Sleep(10);
        }

        Assert.That(resumeThreadId, Is.EqualTo(mainThreadId));
    }

    [Test]
    public void RunBackground_ShouldCompleteOnMainThreadIfContextAvailable()
    {
        var runtime = new LayerRuntime(2);
        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(new TestLayer());
        runtime = builder.Build();

        int completionThreadId = 0;
        int mainThreadId = Thread.CurrentThread.ManagedThreadId;

        LBTask<string> task;
        using (runtime.ScopeHost.MainScope.SynchronizationContext!.EnterScope())
        {
            task = LBTask.RunBackground(() =>
            {
                // Just some work
                return "Done";
            });
        }

        // Attach a continuation that checks thread
        task.GetAwaiter().OnCompleted(() =>
        {
            // This might run on main thread if SetResult happened on main thread
            completionThreadId = Thread.CurrentThread.ManagedThreadId;
        });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (completionThreadId == 0 && !cts.IsCancellationRequested)
        {
            runtime.Pump(0.1f);
            Thread.Sleep(10);
        }

        Assert.That(completionThreadId, Is.EqualTo(mainThreadId));
    }

    [Test]
    public void MaxCompletionsPerPump_ShouldLimitProcessing()
    {
        var runtime = new LayerRuntime(3);
        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(new TestLayer());
        builder.SetPostOptions(new PostSchedulerOptions(
            1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew,
            maxCompletionsPerPump: 1)); // Limit to 1 completion per pump
        runtime = builder.Build();

        int completedCount = 0;
        using (runtime.ScopeHost.MainScope.SynchronizationContext!.EnterScope())
        {
            LBTask.RunBackground(() => { Thread.Sleep(10); }).GetAwaiter()
                  .OnCompleted(() => Interlocked.Increment(ref completedCount));
            LBTask.RunBackground(() => { Thread.Sleep(10); }).GetAwaiter()
                  .OnCompleted(() => Interlocked.Increment(ref completedCount));
        }

        // Wait for background tasks to definitely finish and enqueue completions
        Thread.Sleep(200);

        runtime.Pump(0.1f);
        Assert.That(completedCount, Is.EqualTo(1), "Only 1 completion should be processed in the first pump");

        runtime.Pump(0.1f);
        Assert.That(completedCount, Is.EqualTo(2), "The second completion should be processed in the second pump");
    }

    private class TestLayer : Layer
    {
    }
}
