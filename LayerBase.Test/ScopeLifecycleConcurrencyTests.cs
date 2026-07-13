using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeLifecycleConcurrencyTests
{
    [Test]
    public void Start_and_stop_must_not_dispose_service_before_start_returns()
    {
        for (int i = 0; i < 8; i++)
        {
            var service = new BlockingStartService();
            using var scope = CreateWorkerScope(service, i);

            Task start = Task.Run(scope.Start);
            Assert.That(service.StartEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);

            Task stop = Task.Run(scope.Stop);

            service.AllowStartReturn.Set();

            Assert.That(Task.WaitAll(new[] { start, stop }, TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(service.InitializeCount, Is.EqualTo(1));
            Assert.That(service.DisposeCount, Is.EqualTo(1));
            Assert.That(service.DisposeBeforeInitializeReturned, Is.EqualTo(0));
        }
    }

    [Test]
    public void Worker_stop_racing_before_worker_thread_publication_must_still_cleanup()
    {
        for (int i = 0; i < 512; i++)
        {
            var service = new CountingDisposeService();
            using var scope = CreateWorkerScope(service, 1300 + i);

            Task start = Task.Run(scope.Start);
            SpinWait.SpinUntil(() =>
                ReadState(scope).ToString() == "Starting" ||
                ReadWorkerThread(scope) != null ||
                start.IsCompleted,
                TimeSpan.FromMilliseconds(50));

            Task stop = Task.Run(scope.Stop);

            Assert.That(Task.WaitAll(new[] { start, stop }, TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(
                service.DisposeCount,
                Is.EqualTo(1),
                "Stop returned without cleanup during worker startup publication race.");
        }
    }

    [Test]
    public void Dispose_must_not_return_before_concurrent_stop_cleanup_finishes()
    {
        var service = new BlockingDisposeService();
        using var scope = CreateWorkerScope(service, 1220);

        scope.Start();

        Task stop = Task.Run(scope.Stop);
        Assert.That(service.DisposeEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);

        Task dispose = Task.Run(scope.Dispose);

        try
        {
            Assert.That(
                dispose.Wait(TimeSpan.FromMilliseconds(150)),
                Is.False,
                "Dispose returned while Stop was still blocked in service cleanup.");
        }
        finally
        {
            service.AllowDisposeReturn.Set();
            Assert.That(Task.WaitAll(new[] { stop, dispose }, TimeSpan.FromSeconds(5)), Is.True);
        }

        Assert.That(service.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_must_release_internal_wait_handles()
    {
        var service = new CountingDisposeService();
        using var scope = CreateWorkerScope(service, 1221);

        scope.Start();
        var stopCleanupFinished = ReadWaitHandle(scope, "_stopCleanupFinished");
        var workerStartedSignal = ReadWaitHandle(scope, "_workerStartedSignal");
        var workerLaunchSignal = ReadWaitHandle(scope, "_workerLaunchSignal");

        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stopCleanupFinished.Wait(0));
        Assert.Throws<ObjectDisposedException>(() => workerStartedSignal.Wait(0));
        Assert.Throws<ObjectDisposedException>(() => workerLaunchSignal.Wait(0));
    }

    [Test]
    public void Inline_request_stop_from_non_owner_thread_must_defer_cleanup_until_owner_pump()
    {
        var service = new CountingDisposeService();
        using var scope = CreateInlineScope(service);

        scope.Start();
        Task requestStop = Task.Run(scope.RequestStop);
        Assert.That(requestStop.Wait(TimeSpan.FromSeconds(2)), Is.True);

        Assert.That(service.DisposeCount, Is.EqualTo(0));

        scope.Pump(0);

        Assert.That(service.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Inline_pump_after_request_stop_must_not_run_business_update_frame()
    {
        var service = new UpdatingDisposeService();
        using var scope = CreateInlineScope(service);

        scope.Start();
        scope.RequestStop();
        scope.Pump(0.016f);

        Assert.That(service.UpdateCount, Is.EqualTo(0));
        Assert.That(service.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Worker_stop_before_start_must_cleanup_scope()
    {
        var service = new CountingDisposeService();
        using var scope = CreateWorkerScope(service, 1400);

        scope.Stop();

        Assert.That(service.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Inline_stop_from_non_owner_thread_must_not_dispose_scope()
    {
        var service = new CountingDisposeService();
        using var scope = CreateInlineScope(service);

        scope.Start();
        var stop = Task.Run(() => Assert.Throws<InvalidOperationException>(scope.Stop));
        Assert.That(stop.Wait(TimeSpan.FromSeconds(2)), Is.True);

        Assert.That(service.DisposeCount, Is.EqualTo(0));

        scope.Stop();

        Assert.That(service.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Inline_dispose_from_non_owner_thread_must_not_defer_and_leak_cleanup()
    {
        var service = new CountingDisposeService();
        using var scope = CreateInlineScope(service);

        scope.Start();
        var dispose = Task.Run(() => Assert.Throws<InvalidOperationException>(scope.Dispose));
        Assert.That(dispose.Wait(TimeSpan.FromSeconds(2)), Is.True);

        Assert.That(service.DisposeCount, Is.EqualTo(0));

        scope.Dispose();

        Assert.That(service.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Schedule_post_after_request_stop_must_reject_business_ingress()
    {
        var service = new CountingDisposeService();
        using var scope = CreateInlineScope(service);

        scope.Start();
        scope.RequestStop();

        Assert.Throws<ScopeStoppedException>(() =>
            scope.SchedulePost(new ScopeLifecycleRequestStopEvent(), 0));
    }

    [Test]
    public void Scope_stop_must_close_timer_and_post_scheduler()
    {
        var service = new CountingDisposeService();
        using var scope = CreateInlineScope(service);
        scope.PostScheduler.BuildPlans(new[]
        {
            new PostTypePlan(
                EventTypeId<ScopeLifecycleRequestStopEvent>.Id,
                PostDeliveryMode.Normal,
                BackpressurePolicy.RejectNew,
                maxPending: 0,
                defaultBackpressure: BackpressurePolicy.RejectNew)
        });

        int postCount = 0;
        var timerSink = new CountingTimerSink();
        scope.EventCenter.SubscribeNotify<ScopeLifecycleRequestStopEvent>(
            0,
            (in ScopeLifecycleRequestStopEvent _) => postCount++);

        Assert.That(scope.PostScheduler.TryPost(new ScopeLifecycleRequestStopEvent()).IsSuccess, Is.True);
        _ = scope.Timer.Schedule(new CountingTimerAction(), 0.1f);

        scope.Start();
        scope.Stop();

        scope.PostScheduler.Pump();
        scope.Timer.Tick(1.0f, timerSink);

        Assert.That(postCount, Is.EqualTo(0));
        Assert.That(timerSink.ExpiredCount, Is.EqualTo(0));
        Assert.That(scope.PostScheduler.TryPost(new ScopeLifecycleRequestStopEvent()).IsSuccess, Is.False);
        Assert.That(scope.Timer.Schedule(new CountingTimerAction(), 0.1f).IsInvalid, Is.True);
    }

    [Test]
    public void Scope_stop_internal_must_not_silently_swallow_framework_cleanup_exceptions()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "LayerBase", "Scope", "ScopeRuntime.cs"));
        int start = source.IndexOf("private void StopInternal()", StringComparison.Ordinal);
        int end = source.IndexOf("private void ReportException", StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));
        Assert.That(source.Substring(start, end - start), Does.Not.Contain("catch { }"));
    }

    [Test]
    public void Stop_internal_must_not_wrap_resource_registry_cleanup_in_broad_catch()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "LayerBase", "Scope", "ScopeRuntime.cs"));
        int start = source.IndexOf("private void StopInternal()", StringComparison.Ordinal);
        int end = source.IndexOf("private void ReportException", StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));
        string method = source.Substring(start, end - start);

        Assert.That(method, Does.Not.Contain("ResourceRegistry.CloseAndUnbind(report);\r\n        }\r\n        catch"));
        Assert.That(method, Does.Not.Contain("ResourceRegistry.CloseAndUnbind(report);\n        }\n        catch"));
    }

    [Test]
    public void Dispose_infrastructure_must_not_reset_started_flag_when_cleanup_throws()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "LayerBase", "Scope", "ScopeRuntime.cs"));
        int start = source.IndexOf("private void DisposeInfrastructureOnce()", StringComparison.Ordinal);
        int end = source.IndexOf("private void WorkerLoop()", StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));
        string method = source.Substring(start, end - start);

        Assert.That(method, Does.Not.Contain("Volatile.Write(ref _disposeInfrastructureStarted, 0);"));
    }

    [Test]
    public void Scope_runtime_host_dispose_must_not_mark_disposed_from_finally()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "LayerBase", "Scope", "ScopeRuntimeHost.cs"));
        int start = source.IndexOf("public void Dispose()", StringComparison.Ordinal);
        int end = source.IndexOf("private void ThrowIfDisposed()", StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));
        string method = source.Substring(start, end - start);

        Assert.That(method, Does.Not.Contain("finally"));
        Assert.That(method, Does.Contain("_disposed = true;"));
    }

    [Test]
    public void RequestStop_inside_handler_must_defer_service_disposal_until_handler_returns()
    {
        var service = new ScopeLifecycleRequestStopHandlerService();
        using var scope = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1211,
                name: "LifecycleRequestStopScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service },
            postDispatcher: static (runtime, message) =>
            {
                runtime.EventCenter.Send((ScopeLifecycleRequestStopEvent)message.Payload);
            });

        scope.SetContexts(Array.Empty<ILayerContext>());
        scope.Start();
        Assert.That(scope.TryPost(new ScopePostMessage(0, new ScopeLifecycleRequestStopEvent())), Is.True);
        scope.Pump(0);

        Assert.That(service.RequestStopWasPublic, Is.True);
        Assert.That(service.DisposedInsideHandler, Is.False);
        Assert.That(service.DisposeCount, Is.EqualTo(1));
        Assert.That(service.HandlerReturnedBeforeDispose, Is.True);
    }

    [Test]
    public void Dispose_inside_inline_handler_must_defer_service_disposal_until_handler_returns()
    {
        var service = new ScopeLifecycleDisposeHandlerService();
        using var scope = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1212,
                name: "LifecycleDisposeScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service },
            postDispatcher: static (runtime, message) =>
            {
                runtime.EventCenter.Send((ScopeLifecycleDisposeEvent)message.Payload);
            });

        scope.SetContexts(Array.Empty<ILayerContext>());
        scope.Start();
        Assert.That(scope.TryPost(new ScopePostMessage(0, new ScopeLifecycleDisposeEvent())), Is.True);
        scope.Pump(0);

        Assert.That(service.DisposedInsideHandler, Is.False);
        Assert.That(service.DisposeCount, Is.EqualTo(1));
        Assert.That(service.HandlerReturnedBeforeDispose, Is.True);
    }

    [Test]
    public void LayerRuntime_RequestStop_must_not_synchronously_stop_worker_scopes()
    {
        var runtime = new LayerRuntime(1213);
        var service = new RuntimeRequestStopBlockingStartService();
        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(
            new IService[] { service },
            static (Type serviceType, out ScopeRuntimeServiceScopeInfo scopeInfo) =>
            {
                scopeInfo = new ScopeRuntimeServiceScopeInfo(
                    typeof(RuntimeRequestStopWorkerScope),
                    new ScopeDescriptor(
                        scopeId: 1213,
                        name: "RuntimeRequestStopWorkerScope",
                        threading: ScopeThreadingMode.Worker,
                        clock: ScopeClockMode.FixedRate,
                        tickRateHz: 60,
                        stopPolicy: ScopeStopPolicy.Drain));
                return true;
            });

        ScopeRuntimeHost host = ScopeRuntimeHost.Create(plans, owningRuntime: runtime);
        typeof(LayerRuntime)
            .GetProperty(nameof(LayerRuntime.ScopeHost))!
            .SetValue(runtime, host);

        Task? requestStop = null;
        try
        {
            host.Start();
            Assert.That(service.StartEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);

            requestStop = Task.Run(runtime.RequestStop);

            Assert.That(
                requestStop.Wait(TimeSpan.FromMilliseconds(150)),
                Is.True,
                "LayerRuntime.RequestStop synchronously stopped and joined worker scopes.");
        }
        finally
        {
            service.AllowStartReturn.Set();
            requestStop?.Wait(TimeSpan.FromSeconds(5));
            runtime.Dispose();
        }
    }

    private static object ReadState(ScopeRuntime scope)
    {
        var field = typeof(ScopeRuntime).GetField(
            "_state",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return field!.GetValue(scope)!;
    }

    private static Thread? ReadWorkerThread(ScopeRuntime scope)
    {
        var field = typeof(ScopeRuntime).GetField(
            "_workerThread",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (Thread?)field!.GetValue(scope);
    }

    private static ManualResetEventSlim ReadWaitHandle(ScopeRuntime scope, string fieldName)
    {
        var field = typeof(ScopeRuntime).GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (ManualResetEventSlim)field!.GetValue(scope)!;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "LayerBase.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static ScopeRuntime CreateWorkerScope(IService service, int iteration)
    {
        return new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1200 + iteration,
                name: $"LifecycleWorkerScope{iteration}",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 120,
                stopPolicy: ScopeStopPolicy.Drain),
            new[] { service });
    }

    private static ScopeRuntime CreateInlineScope(IService service)
    {
        return new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1210,
                name: "LifecycleInlineScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new[] { service });
    }

    private sealed class BlockingStartService : IService, IInitializable, IDisposable
    {
        private int _initializeReturned;

        public readonly ManualResetEventSlim StartEntered = new(false);
        public readonly ManualResetEventSlim AllowStartReturn = new(false);

        public int InitializeCount;
        public int DisposeCount;
        public int DisposeBeforeInitializeReturned;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            Interlocked.Increment(ref InitializeCount);
            StartEntered.Set();
            if (!AllowStartReturn.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Test did not release BlockingStartService.Initialize.");
            }

            Volatile.Write(ref _initializeReturned, 1);
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _initializeReturned) == 0)
            {
                Interlocked.Exchange(ref DisposeBeforeInitializeReturned, 1);
            }

            Interlocked.Increment(ref DisposeCount);
        }
    }

    private sealed class BlockingDisposeService : IService, IDisposable
    {
        public readonly ManualResetEventSlim DisposeEntered = new(false);
        public readonly ManualResetEventSlim AllowDisposeReturn = new(false);

        public int DisposeCount;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
            DisposeEntered.Set();
            if (!AllowDisposeReturn.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Test did not release BlockingDisposeService.Dispose.");
            }
        }
    }

    private sealed class CountingDisposeService : IService, IDisposable
    {
        public int DisposeCount;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
        }
    }

    private sealed class UpdatingDisposeService : IService, IUpdate, IDisposable
    {
        public int UpdateCount;
        public int DisposeCount;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            Interlocked.Increment(ref UpdateCount);
        }

        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
        }
    }

    private sealed class CountingTimerSink : IExpiredTimerSink<ITimerAction>
    {
        public int ExpiredCount;

        public bool TryAcceptExpired(in ITimerAction payload, TimerHandle handle)
        {
            ExpiredCount++;
            return true;
        }
    }

    private sealed class CountingTimerAction : ITimerAction
    {
        public bool Execute(PostScheduler scheduler)
        {
            return true;
        }
    }
}

internal readonly struct ScopeLifecycleRequestStopEvent
{
}

internal readonly struct ScopeLifecycleDisposeEvent
{
}

internal sealed class RuntimeRequestStopWorkerScope
{
}

internal sealed partial class ScopeLifecycleRequestStopHandlerService : IService, IDisposable
{
    private int _handlerReturned;

    public bool RequestStopWasPublic;
    public bool DisposedInsideHandler;
    public bool HandlerReturnedBeforeDispose;
    public int DisposeCount;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    public void OnEvent(in ScopeLifecycleRequestStopEvent _)
    {
        ScopeRuntime scope = ScopeObjectBinder.Require(this).Scope;
        var requestStop = typeof(ScopeRuntime).GetMethod("RequestStop");

        RequestStopWasPublic = requestStop != null;
        requestStop?.Invoke(scope, Array.Empty<object>());
        DisposedInsideHandler = DisposeCount != 0;
        Volatile.Write(ref _handlerReturned, 1);
    }

    public void Dispose()
    {
        HandlerReturnedBeforeDispose = Volatile.Read(ref _handlerReturned) != 0;
        Interlocked.Increment(ref DisposeCount);
    }
}

internal sealed partial class ScopeLifecycleDisposeHandlerService : IService, IDisposable
{
    private int _handlerReturned;

    public bool DisposedInsideHandler;
    public bool HandlerReturnedBeforeDispose;
    public int DisposeCount;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    public void OnEvent(in ScopeLifecycleDisposeEvent _)
    {
        ScopeRuntime scope = ScopeObjectBinder.Require(this).Scope;

        scope.Dispose();
        DisposedInsideHandler = DisposeCount != 0;
        Volatile.Write(ref _handlerReturned, 1);
    }

    public void Dispose()
    {
        HandlerReturnedBeforeDispose = Volatile.Read(ref _handlerReturned) != 0;
        Interlocked.Increment(ref DisposeCount);
    }
}

internal sealed class RuntimeRequestStopBlockingStartService : IService, IInitializable
{
    public readonly ManualResetEventSlim StartEntered = new(false);
    public readonly ManualResetEventSlim AllowStartReturn = new(false);

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Initialize()
    {
        StartEntered.Set();
        if (!AllowStartReturn.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Test did not release RuntimeRequestStopBlockingStartService.Initialize.");
        }
    }
}
