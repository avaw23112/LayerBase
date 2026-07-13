using LayerBase.DI;
using LayerBase.DI.Options;
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
        using var scope = CreateInlineScope(service);

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
}
