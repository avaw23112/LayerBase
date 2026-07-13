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

}

internal readonly struct ScopeLifecycleRequestStopEvent
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
