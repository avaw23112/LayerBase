using System.Reflection;
using LayerBase;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Scope;
using LayerBase.Scope.Lifecycle;

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
    public void Context_bound_task_completed_after_close_must_not_resume_on_completing_thread()
    {
        int ownerThreadId = Environment.CurrentManagedThreadId;
        var context = LayerBaseSynchronizationContext.Install(allowThreadPoolFallbackOnDispose: false);
        LBTaskCompletionSource source;
        LBTask task;
        int completionThreadId = -1;
        int continuationThreadId = -1;
        using (context.EnterScope())
        {
            source = new LBTaskCompletionSource();
            task = source.Task;
            task.GetAwaiter().OnCompleted(() =>
            {
                continuationThreadId = Environment.CurrentManagedThreadId;
            });
        }

        using var start = new ManualResetEventSlim(false);
        Task worker = Task.Run(() =>
        {
            completionThreadId = Environment.CurrentManagedThreadId;
            start.Wait();
            source.SetResult();
        });

        context.BeginClose(new ObjectDisposedException("test"));
        start.Set();

        Assert.That(worker.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(continuationThreadId, Is.EqualTo(-1),
            "Completing worker must not resume owner continuation.");

        context.DrainClosingOperations();

        Assert.That(continuationThreadId, Is.EqualTo(ownerThreadId));
        Assert.That(continuationThreadId, Is.Not.EqualTo(completionThreadId));
        Assert.That(task.GetAwaiter().IsCompleted, Is.True);
        context.FinalizeClose();
    }

    [Test]
    public void Completion_after_initial_closing_drain_must_not_be_dropped()
    {
        int ownerThreadId = Environment.CurrentManagedThreadId;
        var context = LayerBaseSynchronizationContext.Install(allowThreadPoolFallbackOnDispose: false);
        LBTaskCompletionSource source;
        LBTask task;
        int continuationThreadId = -1;

        using (context.EnterScope())
        {
            source = new LBTaskCompletionSource();
            task = source.Task;
            task.GetAwaiter().OnCompleted(() =>
            {
                continuationThreadId = Environment.CurrentManagedThreadId;
            });
        }

        using var allowCompletion = new ManualResetEventSlim(false);
        Task worker = Task.Run(() =>
        {
            allowCompletion.Wait();
            source.SetResult();
        });

        context.BeginClose(new ObjectDisposedException("test"));
        context.DrainClosingOperations();

        allowCompletion.Set();

        Assert.That(worker.Wait(TimeSpan.FromSeconds(2)), Is.True);

        context.FinalizeClose();

        Assert.That(continuationThreadId, Is.EqualTo(ownerThreadId));
        Assert.That(task.GetAwaiter().IsCompleted, Is.True);
        Assert.That(context.PendingOperationCount, Is.EqualTo(0));
        Assert.That(context.PendingSourceCount, Is.EqualTo(0));
    }

    [Test]
    public void Complete_before_OnCompleted_then_finalize_must_not_drop_continuation()
    {
        int ownerThreadId = Environment.CurrentManagedThreadId;
        var context = LayerBaseSynchronizationContext.Install(allowThreadPoolFallbackOnDispose: false);
        LBTaskCompletionSource source;
        LBTask.Awaiter awaiter;
        int continuationThreadId = -1;

        using (context.EnterScope())
        {
            source = new LBTaskCompletionSource();
            awaiter = source.Task.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.False);
        }

        source.SetResult();
        context.BeginClose(new ObjectDisposedException("test"));
        context.DrainClosingOperations();

        Assert.Throws<InvalidOperationException>(() => context.FinalizeClose());

        awaiter.OnCompleted(() =>
        {
            continuationThreadId = Environment.CurrentManagedThreadId;
        });

        context.DrainClosingOperations();
        context.FinalizeClose();

        Assert.That(continuationThreadId, Is.EqualTo(ownerThreadId));
        Assert.That(context.PendingSourceCount, Is.EqualTo(0));
    }

    [Test]
    public void Context_must_not_finalize_while_accepted_source_is_pending()
    {
        var context = LayerBaseSynchronizationContext.Install(allowThreadPoolFallbackOnDispose: false);
        LBTaskCompletionSource source;

        using (context.EnterScope())
        {
            source = new LBTaskCompletionSource();
            _ = source.Task;
        }

        context.BeginClose(new ObjectDisposedException("test"));

        Assert.That(context.PendingSourceCount, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => context.FinalizeClose());

        source.SetException(new OperationCanceledException());
        context.DrainClosingOperations();
        context.FinalizeClose();

        Assert.That(context.PendingSourceCount, Is.EqualTo(0));
    }

    [Test]
    public void Context_dispose_from_non_owner_must_not_run_continuations_on_disposer()
    {
        int ownerThreadId = Environment.CurrentManagedThreadId;
        var context = LayerBaseSynchronizationContext.Install(allowThreadPoolFallbackOnDispose: false);
        LBTask task;

        using (context.EnterScope())
        {
            task = LBTask.NextFrame(context);
        }

        int continuationThreadId = -1;
        task.GetAwaiter().OnCompleted(() =>
        {
            continuationThreadId = Environment.CurrentManagedThreadId;
        });

        int disposerThreadId = -1;
        Task dispose = Task.Run(() =>
        {
            disposerThreadId = Environment.CurrentManagedThreadId;
            Assert.Throws<InvalidOperationException>(() => context.Dispose());
        });

        Assert.That(dispose.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(continuationThreadId, Is.EqualTo(-1));

        context.Dispose();

        Assert.That(continuationThreadId, Is.EqualTo(ownerThreadId));
        Assert.That(continuationThreadId, Is.Not.EqualTo(disposerThreadId));
    }

    [Test]
    public void Context_close_must_release_all_registered_task_sources()
    {
        var context = LayerBaseSynchronizationContext.Install(allowThreadPoolFallbackOnDispose: false);
        var tasks = new List<LBTask>();

        using (context.EnterScope())
        {
            for (int i = 0; i < 1024; i++)
            {
                tasks.Add(LBTask.NextFrame(context));
            }
        }

        context.BeginClose(new ObjectDisposedException("test"));
        context.DrainClosingOperations();
        context.FinalizeClose();

        Assert.That(context.PendingOperationCount, Is.EqualTo(0));

        foreach (LBTask task in tasks)
        {
            Assert.That(task.GetAwaiter().IsCompleted, Is.True);
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        }
    }

    [Test]
    public void Scope_stop_must_finish_context_cancellation_before_service_dispose()
    {
        var service = new AsyncDisposeOrderProbe();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1301,
                name: "ContextDisposeOrderScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        runtime.Start();
        runtime.Pump(0);

        runtime.Stop();

        Assert.That(service.ContinuationCompletedBeforeDispose, Is.True);
        Assert.That(service.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Throwing_closing_continuation_must_not_abort_scope_cleanup()
    {
        var first = new DisposeProbeService();
        var throwing = new ThrowingCancellationContinuationService();
        var last = new DisposeProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1302,
                name: "ThrowingClosingContinuationScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { first, throwing, last });

        runtime.Start();

        Assert.DoesNotThrow(runtime.Stop);

        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(throwing.DisposeCount, Is.EqualTo(1));
        Assert.That(last.DisposeCount, Is.EqualTo(1));
        Assert.That(runtime.StateForTest, Is.EqualTo(ScopeRuntimeState.Stopped));
        Assert.That(runtime.ContextForTest!.IsFinalized, Is.True);
        Assert.That(runtime.ExceptionCountForTest, Is.EqualTo(1));
    }

    [Test]
    [Category("Concurrency")]
    public void Frame_ingress_must_accept_concurrent_producers_without_loss()
    {
        var context = LayerBaseSynchronizationContext.Install();
        const int producerCount = 8;
        const int perProducer = 1000;
        int executed = 0;

        Task[] producers = Enumerable.Range(0, producerCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < perProducer; i++)
                {
                    context.ScheduleInFrames(() => Interlocked.Increment(ref executed), frames: 1);
                }
            }))
            .ToArray();

        Assert.That(Task.WaitAll(producers, TimeSpan.FromSeconds(5)), Is.True);

        context.Update();
        context.Update();

        Assert.That(executed, Is.EqualTo(producerCount * perProducer));
    }

    [Test]
    [Category("Concurrency")]
    public void Context_schedule_dispose_race_must_terminalize_every_accepted_work()
    {
        const int attempts = 1000;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            var context = LayerBaseSynchronizationContext.Install();
            int accepted = 0;
            int executed = 0;
            int cancelled = 0;

            using var start = new ManualResetEventSlim(false);

            Task producer = Task.Run(() =>
            {
                start.Wait();
                try
                {
                    context.ScheduleForTest(
                        invoke: () => Interlocked.Increment(ref executed),
                        cancel: _ => Interlocked.Increment(ref cancelled),
                        frames: 1);
                    Interlocked.Increment(ref accepted);
                }
                catch (ObjectDisposedException)
                {
                }
            });

            Task disposer = Task.Run(() =>
            {
                start.Wait();
                try
                {
                    context.Dispose();
                }
                catch (InvalidOperationException)
                {
                }
            });

            start.Set();

            Assert.That(Task.WaitAll(new[] { producer, disposer }, TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(executed + cancelled, Is.EqualTo(accepted), $"Attempt {attempt}");
        }
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

    private sealed class AsyncDisposeOrderProbe : IService, IInitializable, IDisposable
    {
        private LBTask _task;
        private bool _continuationCompleted;

        public bool ContinuationCompletedBeforeDispose { get; private set; }

        public int DisposeCount { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            _task = LBTask.NextFrame();
            _task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    _task.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                }

                _continuationCompleted = true;
            });
        }

        public void Dispose()
        {
            ContinuationCompletedBeforeDispose = _continuationCompleted;
            DisposeCount++;
        }
    }

    private sealed class DisposeProbeService : IService, IDisposable
    {
        public int DisposeCount { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class ThrowingCancellationContinuationService : IService, IInitializable, IDisposable
    {
        private LBTask _task;

        public int DisposeCount { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            _task = LBTask.NextFrame();
            _task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    _task.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                }

                throw new InvalidOperationException("closing continuation failure");
            });
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
