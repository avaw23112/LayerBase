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
    public void Continuation_drain_must_not_execute_callback_while_channel_lock_is_held()
    {
        var inbox = new ReliableContinuationInbox(capacity: 1);

        Assert.That(inbox.TryEnqueue(new LayerContinuation(
            () =>
            {
                Task<bool> enqueue = Task.Run(() =>
                    inbox.TryEnqueue(new LayerContinuation(static () => { }, -1, -1, ScopeTrace.Empty)));

                Assert.That(enqueue.Wait(TimeSpan.FromMilliseconds(250)), Is.True,
                    "Continuation callback ran while the channel lock was held.");
                Assert.That(enqueue.Result, Is.True);
            },
            serviceId: -1,
            taskId: -1,
            trace: ScopeTrace.Empty)), Is.True);

        inbox.Drain(continuation => continuation.Action());
    }

    [Test]
    public void Continuation_overflow_must_be_bounded()
    {
        var inbox = new ReliableContinuationInbox(capacity: 1);
        var continuation = new LayerContinuation(static () => { }, -1, -1, ScopeTrace.Empty);

        Assert.That(inbox.TryEnqueue(continuation), Is.True);
        Assert.That(inbox.TryEnqueue(continuation), Is.True);
        Assert.That(inbox.TryEnqueue(continuation), Is.False);
        Assert.That(inbox.Count, Is.EqualTo(2));
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

    [Test]
    public void Promise_continuation_queue_full_must_not_drop_completed_continuation()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1225,
                name: "PromiseContinuationFullScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>(),
            new ScopeRuntimeOptions(continuationQueueCapacity: 1));
        var promise = new ScopePromise<int>(runtime);
        int continuationRan = 0;

        Assert.That(runtime.TryEnqueueContinuation(static () => { }), Is.True);
        Assert.That(runtime.TryEnqueueContinuation(static () => { }), Is.True);

        promise.SetResult(42);
        promise.OnCompleted(() => Interlocked.Exchange(ref continuationRan, 1));

        runtime.Pump(0);
        Assert.That(Volatile.Read(ref continuationRan), Is.EqualTo(1));
        Assert.That(promise.GetResult(), Is.EqualTo(42));
    }

    [Test]
    public void GetResult_exception_must_unregister_promise()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1222,
                name: "PromiseExceptionScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>());
        var promise = new ScopePromise<int>(runtime);

        promise.SetException(new InvalidOperationException("boom"));

        Assert.That(runtime.AwaitRegistry.PendingCount, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => promise.GetResult());
        Assert.That(runtime.AwaitRegistry.PendingCount, Is.EqualTo(0));
    }

    [Test]
    public void CancelAll_must_keep_completed_promise_registered_until_continuation_is_queued()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1223,
                name: "PromiseCancelAllCompletedScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>());
        var promise = new ScopePromise<int>(runtime);
        int continuationRan = 0;

        promise.SetResult(42);
        runtime.AwaitRegistry.CancelAll(new InvalidOperationException("scope is stopping."));

        Assert.That(runtime.AwaitRegistry.PendingCount, Is.EqualTo(1));

        promise.OnCompleted(() => Interlocked.Exchange(ref continuationRan, 1));

        Assert.That(runtime.AwaitRegistry.PendingCount, Is.EqualTo(0));
        runtime.Pump(0);
        Assert.That(continuationRan, Is.EqualTo(1));
        Assert.That(promise.GetResult(), Is.EqualTo(42));
    }

    [Test]
    public void Await_registry_cancel_all_must_surface_promise_protocol_exceptions()
    {
        var registry = new ScopeAwaitRegistry();
        Assert.That(registry.TryRegister(new ThrowingPromiseControl()), Is.True);

        var error = Assert.Throws<InvalidOperationException>(() =>
            registry.CancelAll(new InvalidOperationException("scope is stopping.")));

        Assert.That(error!.Message, Does.Contain("protocol"));
    }

    [Test]
    public void OnCompleted_after_scope_stop_must_abandon_continuation_without_running_it()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1224,
                name: "PromiseLateContinuationScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>());
        var promise = new ScopePromise<int>(runtime);
        int continuationRan = 0;

        runtime.Stop();

        Assert.DoesNotThrow(() =>
            promise.OnCompleted(() => Interlocked.Exchange(ref continuationRan, 1)));
        Assert.That(continuationRan, Is.EqualTo(0));
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => promise.GetResult())!;
        Assert.That(ex.Message, Does.Contain("scope is stopping"));
    }

    private sealed class ThrowingPromiseControl : IScopePromiseControl
    {
        public bool IsCompleted => false;

        public bool IsCancelled => false;

        public bool TrySetResult(object? result)
        {
            return false;
        }

        public bool TrySetException(Exception exception)
        {
            throw new InvalidOperationException("protocol violation");
        }
    }
}
