using LayerBase.Async;

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
}
