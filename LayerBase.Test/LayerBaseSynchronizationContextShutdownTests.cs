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

    private static MainThreadCompletionQueue GetCompletionQueue(LayerBaseSynchronizationContext context)
    {
        var property = typeof(LayerBaseSynchronizationContext).GetProperty(
            "CompletionQueue",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null);
        return (MainThreadCompletionQueue)property!.GetValue(context)!;
    }
}
