using LayerBase.DI;
using LayerBase.Scope;
using LayerBase.Scope.Completion;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopePromiseShutdownTests
{
    [Test]
    public void Continuation_close_and_drain_must_not_leave_successful_enqueue_unexecuted()
    {
        const int attempts = 2000;
        int producerCount = Math.Max(8, Environment.ProcessorCount * 2);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            var inbox = new ReliableContinuationInbox(capacity: producerCount + 8);
            using var start = new ManualResetEventSlim(false);
            int accepted = 0;
            int executed = 0;

            Task[] producers = Enumerable.Range(0, producerCount)
                .Select(_ => Task.Run(() =>
                {
                    start.Wait();
                    var continuation = new LayerContinuation(
                        () => Interlocked.Increment(ref executed),
                        serviceId: -1,
                        taskId: -1,
                        trace: ScopeTrace.Empty);

                    if (inbox.TryEnqueue(continuation))
                    {
                        Interlocked.Increment(ref accepted);
                    }
                }))
                .ToArray();

            start.Set();
            inbox.Close();
            inbox.Drain(continuation => continuation.Action());

            Assert.That(Task.WaitAll(producers, TimeSpan.FromSeconds(2)), Is.True);

            if (Volatile.Read(ref accepted) != Volatile.Read(ref executed) || inbox.Count != 0)
            {
                Assert.Fail(
                    $"Attempt {attempt} left accepted continuations unexecuted. Accepted={accepted}, Executed={executed}, Count={inbox.Count}.");
            }
        }
    }

    [Test]
    public void Scope_stop_must_cancel_pending_promise_and_run_registered_continuation()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1220,
                name: "PromiseShutdownScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>());
        var promise = new ScopePromise<int>(runtime);
        int continuationRan = 0;

        promise.OnCompleted(() => Interlocked.Exchange(ref continuationRan, 1));
        runtime.Stop();

        Assert.That(continuationRan, Is.EqualTo(1));
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => promise.GetResult())!;
        Assert.That(ex.Message, Does.Contain("scope is stopping"));
    }

    [Test]
    public void Promise_must_remain_registered_until_continuation_is_queued()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1221,
                name: "PromiseResultReadyScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>());
        var promise = new ScopePromise<int>(runtime);
        int continuationRan = 0;

        promise.SetResult(42);

        Assert.That(runtime.AwaitRegistry.PendingCount, Is.EqualTo(1));

        promise.OnCompleted(() => Interlocked.Exchange(ref continuationRan, 1));

        Assert.That(runtime.AwaitRegistry.PendingCount, Is.EqualTo(0));
        runtime.Pump(0);
        Assert.That(continuationRan, Is.EqualTo(1));
        Assert.That(promise.GetResult(), Is.EqualTo(42));
    }
}
