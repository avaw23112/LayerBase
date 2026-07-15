using LayerBase.Async;
using System.Reflection;

namespace LayerBase.Test;

[TestFixture]
public sealed class LBTaskSynchronizationContextTests
{
    [Test]
    public void Yield_posts_completion_to_current_synchronization_context()
    {
        var previous = SynchronizationContext.Current;
        var context = new RecordingSynchronizationContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);

            var task = LBTask.Yield();

            Assert.That(context.PostCount, Is.EqualTo(1));

            var resumed = false;
            task.GetAwaiter().OnCompleted(() => resumed = true);

            Assert.That(resumed, Is.False);
            context.Drain();
            Assert.That(resumed, Is.True);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Test]
    public void Send_from_non_owner_thread_is_not_supported()
    {
        using var context = LayerBaseSynchronizationContext.Install();
        var completed = new ManualResetEventSlim(false);
        Exception? exception = null;

        try
        {
            var worker = new Thread(() =>
            {
                try
                {
                    context.Send(_ => { }, null);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completed.Set();
                }
            });

            worker.Start();

            Assert.That(completed.Wait(TimeSpan.FromMilliseconds(200)), Is.True);
            Assert.That(exception, Is.TypeOf<NotSupportedException>());
        }
        finally
        {
            context.Update();
        }
    }

    [Test]
    public void NextFrame_resumes_on_next_owner_context_update()
    {
        using var context = LayerBaseSynchronizationContext.Install();
        var resumed = false;

        using (context.EnterScope())
        {
            LBTask.NextFrame().GetAwaiter().OnCompleted(() => resumed = true);
        }

        Assert.That(resumed, Is.False);
        context.Update();
        Assert.That(resumed, Is.True);
    }

    [Test]
    public void LBTask_only_supports_one_awaiter_continuation()
    {
        var tcs = new LBTaskCompletionSource();
        var task = tcs.Task;
        var awaiter = task.GetAwaiter();

        awaiter.OnCompleted(() => { });

        Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().OnCompleted(() => { }));
    }

    [Test]
    public void LBTask_result_is_consumed_once()
    {
        var tcs = new LBTaskCompletionSource<int>();
        var task = tcs.Task;
        tcs.SetResult(7);
        var awaiter = task.GetAwaiter();

        Assert.That(awaiter.GetResult(), Is.EqualTo(7));
        Assert.Throws<InvalidOperationException>(() => awaiter.GetResult());
    }

    [Test]
    public void LBTask_source_version_rejects_stale_task_after_source_reuse()
    {
        var first = new LBTaskCompletionSource();
        var staleTask = first.Task;
        object? firstSource = GetSource(staleTask);

        first.SetResult();
        staleTask.GetAwaiter().GetResult();

        var second = new LBTaskCompletionSource();
        var secondTask = second.Task;

        Assert.That(GetSource(secondTask), Is.SameAs(firstSource));

        second.SetResult();
        Assert.Throws<InvalidOperationException>(() => staleTask.GetAwaiter().GetResult());
    }

    [Test]
    public void Disposing_context_cancels_pending_sources()
    {
        LBTask task;
        using (var context = LayerBaseSynchronizationContext.Install())
        {
            using (context.EnterScope())
            {
                task = LBTask.NextFrame();
            }
        }

        var awaiter = task.GetAwaiter();
        Assert.That(awaiter.IsCompleted, Is.True);
        Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
    }

    [Test]
    public void Context_close_cancels_pending_sources_and_drains_registered_continuations()
    {
        using var context = LayerBaseSynchronizationContext.Install();
        var resumed = false;
        var observedCancellation = false;

        using (context.EnterScope())
        {
            var task = LBTask.NextFrame();
            task.GetAwaiter().OnCompleted(() =>
            {
                resumed = true;
                observedCancellation = Assert.Throws<OperationCanceledException>(
                    () => task.GetAwaiter().GetResult()) != null;
            });
        }

        context.BeginClose(new OperationCanceledException("closing"));

        Assert.That(resumed, Is.False);
        context.Update();
        Assert.That(resumed, Is.True);
        Assert.That(observedCancellation, Is.True);
    }

    [Test]
    public void RunBackground_resumes_on_captured_regular_synchronization_context()
    {
        var previous = SynchronizationContext.Current;
        var context = new RecordingSynchronizationContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);

            var task = LBTask.RunBackground(() => 42);
            var resumed = false;
            var result = 0;
            task.GetAwaiter().OnCompleted(() =>
            {
                result = task.GetAwaiter().GetResult();
                resumed = true;
            });

            SpinWait.SpinUntil(() => context.PostCount > 0, TimeSpan.FromSeconds(2));

            Assert.That(context.PostCount, Is.EqualTo(1));
            Assert.That(resumed, Is.False);
            context.Drain();
            Assert.That(resumed, Is.True);
            Assert.That(result, Is.EqualTo(42));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Test]
    public void Delay_resumes_on_captured_synchronization_context()
    {
        var previous = SynchronizationContext.Current;
        var context = new RecordingSynchronizationContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);

            var task = LBTask.Delay(TimeSpan.FromMilliseconds(10));
            var resumed = false;
            task.GetAwaiter().OnCompleted(() =>
            {
                task.GetAwaiter().GetResult();
                resumed = true;
            });

            SpinWait.SpinUntil(() => context.PostCount > 0, TimeSpan.FromSeconds(2));

            Assert.That(context.PostCount, Is.EqualTo(1));
            Assert.That(resumed, Is.False);
            context.Drain();
            Assert.That(resumed, Is.True);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Test]
    public void Completion_never_runs_continuation_inline_on_producer_thread()
    {
        var previous = SynchronizationContext.Current;
        var context = new RecordingSynchronizationContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            var ownerThreadId = Environment.CurrentManagedThreadId;
            var producerThreadId = 0;
            var continuationThreadId = 0;
            var resumed = false;
            var tcs = new LBTaskCompletionSource();
            var task = tcs.Task;

            task.GetAwaiter().OnCompleted(() =>
            {
                continuationThreadId = Environment.CurrentManagedThreadId;
                task.GetAwaiter().GetResult();
                resumed = true;
            });

            var producer = new Thread(() =>
            {
                producerThreadId = Environment.CurrentManagedThreadId;
                tcs.SetResult();
            });

            producer.Start();
            producer.Join();

            Assert.That(context.PostCount, Is.EqualTo(1));
            Assert.That(resumed, Is.False);
            context.Drain();
            Assert.That(resumed, Is.True);
            Assert.That(continuationThreadId, Is.EqualTo(ownerThreadId));
            Assert.That(continuationThreadId, Is.Not.EqualTo(producerThreadId));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _work = new();

        public int PostCount { get; private set; }

        public override void Post(SendOrPostCallback d, object? state)
        {
            PostCount++;
            _work.Enqueue((d, state));
        }

        public void Drain()
        {
            while (_work.Count > 0)
            {
                var work = _work.Dequeue();
                work.Callback(work.State);
            }
        }
    }

    private static object? GetSource(LBTask task)
    {
        return typeof(LBTask)
            .GetField("Source", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(task);
    }
}
