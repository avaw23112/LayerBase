using System.Reflection;
using LayerBase.Async;

namespace EventsTest;

public class LayerBaseSynchronizationContextShutdownTests
{
    [Test]
    public void Post_after_dispose_must_throw_instead_of_dropping_work()
    {
        var context = LayerBaseSynchronizationContext.Install();
        context.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            context.Post(static _ => throw new InvalidOperationException("must not run"), null));
    }

    [Test]
    public void Send_on_owner_thread_after_dispose_must_throw_instead_of_running_inline()
    {
        var context = LayerBaseSynchronizationContext.Install();
        context.Dispose();
        var invoked = false;

        Assert.Throws<ObjectDisposedException>(() =>
            context.Send(_ => invoked = true, null));
        Assert.That(invoked, Is.False);
    }

    [Test]
    public void Completion_queue_after_context_dispose_must_reject_new_items()
    {
        var context = LayerBaseSynchronizationContext.Install();
        var queue = GetCompletionQueue(context);

        context.Dispose();

        Assert.Throws<ObjectDisposedException>(() => queue.Enqueue(static () => { }));
    }

    [Test]
    public void Completion_queue_close_must_not_cancel_items_while_channel_lock_is_held()
    {
        var queue = new MainThreadCompletionQueue();

        queue.Enqueue(
            static () => { },
            error =>
            {
                Task enqueue = Task.Run(() =>
                    Assert.Throws<ObjectDisposedException>(() =>
                        queue.Enqueue(static () => { })));

                Assert.That(enqueue.Wait(TimeSpan.FromMilliseconds(250)), Is.True,
                    "CancelOnClose ran while the completion queue lock was held.");
            });

        queue.Close(new ObjectDisposedException("test"));
    }

    [Test]
    public void Update_must_not_wrap_work_in_catch_rethrow()
    {
        string source = File.ReadAllText(FindRepositoryFile(
            "LayerBase.Task",
            "LayerBaseSynchronizationContext.cs"));

        Assert.That(source, Does.Not.Contain("catch (Exception)\r\n            {\r\n                throw;\r\n            }"));
        Assert.That(source, Does.Not.Contain("catch (Exception)\n            {\n                throw;\n            }"));
    }

    [Test]
    public void Context_dispose_must_cancel_already_accepted_completion_items()
    {
        var context = LayerBaseSynchronizationContext.Install();
        LBTask task;
        using (context.EnterScope())
        {
            task = LBTask.SwitchToMainThread();
        }

        context.Dispose();

        var awaiter = task.GetAwaiter();
        Assert.That(awaiter.IsCompleted, Is.True);
        Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
    }

    [Test]
    public void Context_dispose_must_complete_continuation_for_cancelled_completion_items()
    {
        var context = LayerBaseSynchronizationContext.Install();
        LBTask task;
        int continuationRan = 0;
        using (context.EnterScope())
        {
            task = LBTask.SwitchToMainThread();
            task.GetAwaiter().OnCompleted(() => Interlocked.Exchange(ref continuationRan, 1));
        }

        Assert.DoesNotThrow(context.Dispose);

        Assert.That(
            SpinWait.SpinUntil(() => Volatile.Read(ref continuationRan) == 1, TimeSpan.FromSeconds(1)),
            Is.True);
        Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
    }

    [Test]
    public void Context_dispose_must_cancel_next_frame_task()
    {
        var context = LayerBaseSynchronizationContext.Install();
        LBTask task;
        using (context.EnterScope())
        {
            task = LBTask.NextFrame(context);
        }

        context.Dispose();

        var awaiter = task.GetAwaiter();
        Assert.That(awaiter.IsCompleted, Is.True);
        Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
    }

    [Test]
    public void Context_dispose_must_not_cancel_frame_work_while_lock_is_held()
    {
        string source = File.ReadAllText(FindRepositoryFile(
            "LayerBase.Task",
            "LayerBaseSynchronizationContext.cs"));
        int start = source.IndexOf("public void Dispose()", StringComparison.Ordinal);
        int end = source.IndexOf("public static LayerBaseSynchronizationContext Install", StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));
        string method = source.Substring(start, end - start);
        int lockStart = method.IndexOf("lock (_lock)", StringComparison.Ordinal);
        int clear = method.IndexOf("_frameWork.Clear();", lockStart, StringComparison.Ordinal);
        int lockEnd = method.IndexOf("}", clear, StringComparison.Ordinal);

        Assert.That(lockStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(clear, Is.GreaterThan(lockStart));
        Assert.That(lockEnd, Is.GreaterThan(clear));
        string lockedSection = method.Substring(lockStart, lockEnd - lockStart);

        Assert.That(lockedSection, Does.Not.Contain("CancelOnDispose"));
    }

    [Test]
    public void Context_dispose_must_cancel_run_on_main_thread_task()
    {
        var context = LayerBaseSynchronizationContext.Install();
        var invoked = false;
        LBTask task;
        using (context.EnterScope())
        {
            task = LBTask.RunOnMainThread(() => invoked = true, context);
        }

        context.Dispose();

        var awaiter = task.GetAwaiter();
        Assert.That(awaiter.IsCompleted, Is.True);
        Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
        Assert.That(invoked, Is.False);
    }

    [Test]
    public void Context_dispose_must_cancel_run_on_main_thread_result_task()
    {
        var context = LayerBaseSynchronizationContext.Install();
        var invoked = false;
        LBTask<int> task;
        using (context.EnterScope())
        {
            task = LBTask<int>.RunOnMainThread(() =>
            {
                invoked = true;
                return 42;
            }, context);
        }

        context.Dispose();

        var awaiter = task.GetAwaiter();
        Assert.That(awaiter.IsCompleted, Is.True);
        Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
        Assert.That(invoked, Is.False);
    }

    [Test]
    public void Background_completion_after_context_close_must_cancel_source_directly()
    {
        var context = LayerBaseSynchronizationContext.Install();
        using var gate = new ManualResetEventSlim(false);
        LBTask task;
        using (context.EnterScope())
        {
            task = LBTask.RunBackground(() => gate.Wait());
        }

        context.Dispose();
        gate.Set();

        var awaiter = task.GetAwaiter();
        Assert.That(
            SpinWait.SpinUntil(() => awaiter.IsCompleted, TimeSpan.FromSeconds(2)),
            Is.True);
        Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
    }

    [Test]
    public void LBTask_must_reject_second_GetResult_on_same_task()
    {
        LBTask task = LBTask.Run(static () => { });
        var awaiter = task.GetAwaiter();
        Assert.That(
            SpinWait.SpinUntil(() => awaiter.IsCompleted, TimeSpan.FromSeconds(2)),
            Is.True);

        awaiter.GetResult();

        Assert.Throws<InvalidOperationException>(() => awaiter.GetResult());
    }

    [Test]
    public void LBTask_result_must_reject_second_GetResult_on_same_task()
    {
        LBTask<int> task = LBTask<int>.Run(static () => 7);
        var awaiter = task.GetAwaiter();
        Assert.That(
            SpinWait.SpinUntil(() => awaiter.IsCompleted, TimeSpan.FromSeconds(2)),
            Is.True);

        Assert.That(awaiter.GetResult(), Is.EqualTo(7));

        Assert.Throws<InvalidOperationException>(() => awaiter.GetResult());
    }

    [Test]
    public void Scope_bound_context_must_complete_continuation_after_dispose_without_dropping_it()
    {
        var context = LayerBaseSynchronizationContext.Install(allowThreadPoolFallbackOnDispose: false);
        LBTaskCompletionSource source;
        LBTask task;
        int continuationRan = 0;
        using (context.EnterScope())
        {
            source = new LBTaskCompletionSource();
            task = source.Task;
            task.GetAwaiter().OnCompleted(() => Interlocked.Exchange(ref continuationRan, 1));
        }

        context.Dispose();
        source.SetResult();

        Assert.That(
            SpinWait.SpinUntil(() => Volatile.Read(ref continuationRan) == 1, TimeSpan.FromSeconds(1)),
            Is.True);
        Assert.That(task.GetAwaiter().IsCompleted, Is.True);
    }

    private static MainThreadCompletionQueue GetCompletionQueue(LayerBaseSynchronizationContext context)
    {
        var property = typeof(LayerBaseSynchronizationContext).GetProperty(
            "CompletionQueue",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null);
        return (MainThreadCompletionQueue)property!.GetValue(context)!;
    }

    private static string FindRepositoryFile(params string[] parts)
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not locate repository file.");
        return string.Empty;
    }
}
