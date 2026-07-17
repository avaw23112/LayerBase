using System.Diagnostics;
using LayerBase;
using LayerBase.Async;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
[NonParallelizable]
public sealed class ScopeWorkerBlockingTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Event_wakes_worker_within_250ms()
    {
        var firstTickDone = new ManualResetEventSlim(false);
        var updateCount = 0;

        using var runtime = new LayerRuntime(30001);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateSimpleWorkerPlan(
                    firstTickDone,
                    () => Interlocked.Increment(ref updateCount))
            },
            runtimeId: 30001,
            generation: 1);

        ScopeRuntime workerScope = host.Scopes[1];
        host.StartWorkers();

        Assert.That(firstTickDone.Wait(TimeSpan.FromSeconds(2)), Is.True);

        var stopTask = workerScope.RequestStopAsync();

        Assert.That(SpinWait.SpinUntil(() => stopTask.GetAwaiter().IsCompleted,
            TimeSpan.FromSeconds(2)), Is.True);

        Assert.That(stopTask.GetAwaiter().GetResult().State,
            Is.EqualTo(ScopeControlResult.Succeeded));
    }

    [Test]
    public void Dispose_control_call_wakes_worker_within_250ms()
    {
        var firstTickDone = new ManualResetEventSlim(false);
        var updateCount = 0;

        using var runtime = new LayerRuntime(30002);
        var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateSimpleWorkerPlan(
                    firstTickDone,
                    () => Interlocked.Increment(ref updateCount))
            },
            runtimeId: 30002,
            generation: 1);

        ScopeRuntime workerScope = host.Scopes[1];
        host.StartWorkers();

        Assert.That(firstTickDone.Wait(TimeSpan.FromSeconds(2)), Is.True);

        var sw = Stopwatch.StartNew();
        var disposeTask = workerScope.RequestDisposeAsync();

        Assert.That(SpinWait.SpinUntil(() => disposeTask.GetAwaiter().IsCompleted,
            TimeSpan.FromSeconds(2)), Is.True);

        Assert.That(disposeTask.GetAwaiter().GetResult().State,
            Is.EqualTo(ScopeControlResult.Succeeded));
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(250));
        host.Dispose();
    }

    [Test]
    public void SynchronizationContext_post_wakes_worker_within_250ms()
    {
        var firstTickDone = new ManualResetEventSlim(false);
        var continuationPosted = new ManualResetEventSlim(false);
        var continuationThreadId = 0;
        var updateCount = 0;

        using var runtime = new LayerRuntime(30003);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateSimpleWorkerPlan(
                    firstTickDone,
                    () => Interlocked.Increment(ref updateCount))
            },
            runtimeId: 30003,
            generation: 1);

        ScopeRuntime workerScope = host.Scopes[1];
        host.StartWorkers();

        Assert.That(firstTickDone.Wait(TimeSpan.FromSeconds(2)), Is.True);

        LayerBaseSynchronizationContext? context = workerScope.SynchronizationContext;
        Assert.That(context, Is.Not.Null);

        var sw = Stopwatch.StartNew();
        context.Post(_ =>
        {
            continuationThreadId = Environment.CurrentManagedThreadId;
            continuationPosted.Set();
        }, null);

        Assert.That(continuationPosted.Wait(TimeSpan.FromMilliseconds(250)), Is.True);
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(250));
        Assert.That(continuationThreadId, Is.EqualTo(workerScope.OwnerThreadId));
    }

    [Test]
    public void Event_does_not_prematurely_execute_update()
    {
        var firstTickDone = new ManualResetEventSlim(false);
        var continuationDone = new ManualResetEventSlim(false);
        var updateCount = 0;

        using var runtime = new LayerRuntime(30004);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateSimpleWorkerPlan(
                    firstTickDone,
                    () => Interlocked.Increment(ref updateCount))
            },
            runtimeId: 30004,
            generation: 1);

        ScopeRuntime workerScope = host.Scopes[1];
        host.StartWorkers();

        Assert.That(firstTickDone.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(Volatile.Read(ref updateCount), Is.EqualTo(1));

        Thread.Sleep(100);

        LayerBaseSynchronizationContext? context = workerScope.SynchronizationContext;
        Assert.That(context, Is.Not.Null);

        context.Post(_ => continuationDone.Set(), null);
        Assert.That(continuationDone.Wait(TimeSpan.FromMilliseconds(250)), Is.True);

        Assert.That(Volatile.Read(ref updateCount), Is.EqualTo(1));
    }

    [Test]
    public void Tick_overrun_does_not_wait()
    {
        var firstUpdateEnd = new ManualResetEventSlim(false);
        var releaseFirstUpdate = new ManualResetEventSlim(false);
        var secondUpdateStart = new ManualResetEventSlim(false);
        var firstUpdateEndTime = 0L;
        var secondUpdateStartTime = 0L;

        using var runtime = new LayerRuntime(30005);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateOverrunPlan(
                    firstUpdateEnd,
                    releaseFirstUpdate,
                    secondUpdateStart,
                    () => firstUpdateEndTime = Stopwatch.GetTimestamp(),
                    () => secondUpdateStartTime = Stopwatch.GetTimestamp())
            },
            runtimeId: 30005,
            generation: 1);

        host.StartWorkers();

        Assert.That(firstUpdateEnd.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Thread.Sleep(140);
        releaseFirstUpdate.Set();

        Assert.That(secondUpdateStart.Wait(TimeSpan.FromSeconds(2)), Is.True);

        double elapsedMs = (secondUpdateStartTime - firstUpdateEndTime) * 1000.0 / Stopwatch.Frequency;
        Assert.That(elapsedMs, Is.LessThan(25));
    }

    [Test]
    public void Overrun_does_not_exceed_reasonable_tick_rate()
    {
        var firstUpdateDone = new ManualResetEventSlim(false);
        var releaseFirstUpdate = new ManualResetEventSlim(false);
        var updateCount = 0;

        using var runtime = new LayerRuntime(30006);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateCatchUpPlan(
                    firstUpdateDone,
                    releaseFirstUpdate,
                    () => Interlocked.Increment(ref updateCount))
            },
            runtimeId: 30006,
            generation: 1);

        host.StartWorkers();

        Assert.That(firstUpdateDone.Wait(TimeSpan.FromSeconds(2)), Is.True);

        long overrunStart = Stopwatch.GetTimestamp();
        Thread.Sleep(120);
        releaseFirstUpdate.Set();

        Thread.Sleep(500);

        int totalUpdates = Volatile.Read(ref updateCount);
        long elapsedMs = (Stopwatch.GetTimestamp() - overrunStart) * 1000L / Stopwatch.Frequency;

        double effectiveHz = totalUpdates * 1000.0 / elapsedMs;
        Assert.That(effectiveHz, Is.LessThan(150));
    }

    [Test]
    public void FrameDelay_does_not_cause_busy_loop()
    {
        var firstTickDone = new ManualResetEventSlim(false);
        var updateCount = 0;

        using var runtime = new LayerRuntime(30007);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateSimpleWorkerPlan(
                    firstTickDone,
                    () => Interlocked.Increment(ref updateCount))
            },
            runtimeId: 30007,
            generation: 1);

        ScopeRuntime workerScope = host.Scopes[1];
        host.StartWorkers();

        Assert.That(firstTickDone.Wait(TimeSpan.FromSeconds(2)), Is.True);

        LayerBaseSynchronizationContext? context = workerScope.SynchronizationContext;
        Assert.That(context, Is.Not.Null);

        using (context.EnterScope())
        {
            var frameTask = LBTask.NextFrame();
        }

        Assert.That(context.HasPendingWork, Is.True);
        Assert.That(context.HasReadyWork, Is.False);
        Thread.Sleep(300);

        int currentCount = Volatile.Read(ref updateCount);
        Assert.That(currentCount, Is.EqualTo(1));
    }

    [Test]
    public void Owner_thread_remains_constant_across_blocking_and_wake()
    {
        var updateThreadId = 0;
        var firstTickDone = new ManualResetEventSlim(false);
        var continuationDone = new ManualResetEventSlim(false);
        var continuationThreadId = 0;

        using var runtime = new LayerRuntime(30008);
        var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateOwnerThreadPlan(
                    firstTickDone,
                    continuationDone,
                    id => updateThreadId = id,
                    id => continuationThreadId = id)
            },
            runtimeId: 30008,
            generation: 1);

        ScopeRuntime workerScope = host.Scopes[1];
        host.StartWorkers();

        Assert.That(firstTickDone.Wait(TimeSpan.FromSeconds(2)), Is.True);

        LayerBaseSynchronizationContext? context = workerScope.SynchronizationContext;
        Assert.That(context, Is.Not.Null);

        context.Post(_ =>
        {
            continuationThreadId = Environment.CurrentManagedThreadId;
            continuationDone.Set();
        }, null);

        Assert.That(continuationDone.Wait(TimeSpan.FromSeconds(2)), Is.True);

        var stopTask = workerScope.RequestStopAsync();
        workerScope.PumpIngress();

        Assert.That(stopTask.GetAwaiter().GetResult().State,
            Is.EqualTo(ScopeControlResult.Succeeded));

        host.Dispose();

        Assert.That(updateThreadId, Is.Not.EqualTo(0));
        Assert.That(continuationThreadId, Is.EqualTo(updateThreadId));
    }

    private static ScopeExecutionPlan CreateSimpleWorkerPlan(
        ManualResetEventSlim firstTickDone,
        Action incrementUpdate)
    {
        var started = 0;
        var update = new UpdateInvoker[]
        {
            _ =>
            {
                if (Interlocked.Exchange(ref started, 1) == 0)
                {
                    incrementUpdate();
                    firstTickDone.Set();
                }
            }
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(2, nameof(SimpleWorkerScope), typeof(SimpleWorkerScope)),
            ScopeOptions.Worker(tickRateHz: 1),
            lifecyclePlan: new ScopeLifecyclePlan(
                layers,
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                update,
                Array.Empty<FixedUpdateInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>()));
    }

    private static ScopeExecutionPlan CreateOverrunPlan(
        ManualResetEventSlim firstUpdateEnd,
        ManualResetEventSlim releaseFirstUpdate,
        ManualResetEventSlim secondUpdateStart,
        Action captureFirstUpdateEnd,
        Action captureSecondUpdateStart)
    {
        var firstStarted = 0;
        var secondStarted = 0;
        var update = new UpdateInvoker[]
        {
            _ =>
            {
                if (Interlocked.Exchange(ref firstStarted, 1) == 0)
                {
                    firstUpdateEnd.Set();
                    releaseFirstUpdate.Wait(TimeSpan.FromSeconds(2));
                    captureFirstUpdateEnd();
                    return;
                }

                if (Interlocked.Exchange(ref secondStarted, 1) == 0)
                {
                    captureSecondUpdateStart();
                    secondUpdateStart.Set();
                }
            }
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(2, nameof(OverrunScope), typeof(OverrunScope)),
            ScopeOptions.Worker(tickRateHz: 20),
            lifecyclePlan: new ScopeLifecyclePlan(
                layers,
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                update,
                Array.Empty<FixedUpdateInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>()));
    }

    private static ScopeExecutionPlan CreateCatchUpPlan(
        ManualResetEventSlim firstUpdateDone,
        ManualResetEventSlim releaseFirstUpdate,
        Action incrementUpdate)
    {
        var firstStarted = 0;
        var update = new UpdateInvoker[]
        {
            _ =>
            {
                incrementUpdate();

                if (Interlocked.Exchange(ref firstStarted, 1) == 0)
                {
                    firstUpdateDone.Set();
                    releaseFirstUpdate.Wait(TimeSpan.FromSeconds(2));
                }
            }
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(2, nameof(CatchUpScope), typeof(CatchUpScope)),
            ScopeOptions.Worker(tickRateHz: 100),
            lifecyclePlan: new ScopeLifecyclePlan(
                layers,
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                update,
                Array.Empty<FixedUpdateInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>()));
    }

    private static ScopeExecutionPlan CreateOwnerThreadPlan(
        ManualResetEventSlim firstTickDone,
        ManualResetEventSlim continuationDone,
        Action<int> captureUpdateThread,
        Action<int> captureContinuationThread)
    {
        var started = 0;
        var update = new UpdateInvoker[]
        {
            _ =>
            {
                if (Interlocked.Exchange(ref started, 1) == 0)
                {
                    captureUpdateThread(Environment.CurrentManagedThreadId);
                    firstTickDone.Set();
                }
            }
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(2, nameof(OwnerThreadScope), typeof(OwnerThreadScope)),
            ScopeOptions.Worker(tickRateHz: 1),
            lifecyclePlan: new ScopeLifecyclePlan(
                layers,
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                update,
                Array.Empty<FixedUpdateInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>()));
    }

    private sealed class SimpleWorkerScope : IScopeDefinition {  public ScopeOptions Options => ScopeOptions.Inline; }
    private sealed class OverrunScope : IScopeDefinition {  public ScopeOptions Options => ScopeOptions.Inline; }
    private sealed class CatchUpScope : IScopeDefinition {  public ScopeOptions Options => ScopeOptions.Inline; }
    private sealed class OwnerThreadScope : IScopeDefinition {  public ScopeOptions Options => ScopeOptions.Inline; }
}
