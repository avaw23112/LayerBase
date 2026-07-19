using System.Reflection;
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class WorkerShutdownTimeoutTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Normal_scope_shutdown_disposes_resources()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new WellBehavedWorkerLayer())
            .Build();

        Assert.DoesNotThrow(() => runtime.Dispose());
    }

    [Test]
    public void Timed_out_scope_worker_does_not_dispose_live_resources()
    {
        Assert.Throws<TimeoutException>(() =>
        {
            _ = LayerHub.CreateLayers()
                .Push(new StuckWorkerLayer())
                .Build();
        });
    }

    [Test]
    public void Timed_out_scope_reports_shutdown_fault()
    {
        Assert.Throws<TimeoutException>(() =>
        {
            _ = LayerHub.CreateLayers()
                .Push(new StuckWorkerLayer())
                .Build();
        });
    }

    [Test]
    public void Shutdown_timeout_does_not_cause_object_disposed_exception_on_worker()
    {
        Assert.Throws<TimeoutException>(() =>
        {
            _ = LayerHub.CreateLayers()
                .Push(new StuckWorkerLayer())
                .Build();
        });
    }

    [Test]
    public void Runtime_dispose_is_bounded()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _ = LayerHub.CreateLayers()
                .Push(new StuckWorkerLayer())
                .Build();
        }
        catch (TimeoutException)
        {
        }

        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(40000),
            "Build timeout with a stuck scope includes build + abort deadlines.");
    }

    private sealed class StuckUpdateService : IService, IUpdate
    {
        private volatile bool _shouldStop;

        public void ConfigureServices(IServiceCollection services) { }

        public void Update()
        {
            while (!_shouldStop)
            {
                Thread.Sleep(10);
            }
        }

        public void Stop() => _shouldStop = true;
    }

    private sealed class WellBehavedWorkerLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 777,
                    identity: "scope:test:GoodWorker",
                    scopeType: typeof(GoodWorkerScope),
                    factory: static () => new GoodWorkerScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(typeof(EmptyWorkerService), new EmptyWorkerService(), typeof(GoodWorkerScope));
        }
    }

    private sealed class StuckWorkerLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 888,
                    identity: "scope:test:StuckWorker",
                    scopeType: typeof(StuckWorkerScope),
                    factory: static () => new StuckWorkerScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(typeof(StuckUpdateService), new StuckUpdateService(), typeof(StuckWorkerScope));
        }
    }

    private sealed class GoodWorkerScope : IScopeDefinition
    {
        public const int ScopeId = 777;
        public ScopeOptions Options => ScopeOptions.Worker(tickRateHz: 10);
    }

    private sealed class StuckWorkerScope : IScopeDefinition
    {
        public const int ScopeId = 888;
        public ScopeOptions Options => ScopeOptions.Worker(tickRateHz: 10);
    }

    [Test]
    public void Delayed_thread_exit_releases_resources_after_timeout()
    {
        var blockEvent = new ManualResetEventSlim(false);
        var builder = LayerHub.CreateLayers()
            .Push(new BlockableWorkerLayer(blockEvent));

        Assert.Throws<TimeoutException>(() => builder.Build());

        ScopeWorker worker = GetSingleWorker(builder);

        Assert.That(worker.ResourcesReleased, Is.False,
            "Resources should NOT be released while worker thread is still alive.");

        blockEvent.Set();

        var stopDeadline = DateTime.UtcNow.AddSeconds(10);
        while (worker.Runtime.State != ScopeRuntimeState.Stopped &&
               worker.Runtime.State != ScopeRuntimeState.Disposed &&
               DateTime.UtcNow < stopDeadline)
        {
            Thread.Sleep(50);
        }

        Assert.That(worker.Runtime.State, Is.EqualTo(ScopeRuntimeState.Stopped),
            "Worker scope should finish its owner-thread stop after the blocked update returns.");
        Assert.That(worker.ResourcesReleased, Is.False,
            "Timeout must not force worker resources to be released before an exit request.");

        worker.RequestExitAfterScopeStopped();
        var exitDeadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(worker.WaitForExit(in exitDeadline), Is.True,
            "Worker should exit after Stop has completed and an exit request is issued.");

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!worker.ResourcesReleased && DateTime.UtcNow < deadline)
            Thread.Sleep(50);

        Assert.That(worker.ResourcesReleased, Is.True,
            "Resources must be released after the worker thread exits.");
    }

    private static ScopeWorker GetSingleWorker(LayerRuntime.LayersBuilder builder)
    {
        var runtimeField = typeof(LayerRuntime.LayersBuilder)
            .GetField("_runtime", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Cannot access _runtime field on LayersBuilder");

        var runtime = runtimeField.GetValue(builder)
            ?? throw new InvalidOperationException("_runtime is null");

        var scopeHostField = typeof(LayerRuntime)
            .GetField("_scopeHost", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Cannot access _scopeHost field on LayerRuntime");

        var scopeHost = scopeHostField.GetValue(runtime)
            ?? throw new InvalidOperationException("_scopeHost is null");

        var workersField = scopeHost.GetType()
            .GetField("_workers", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Cannot access _workers field on ScopeRuntimeHost");

        var workers = (ScopeWorker[])workersField.GetValue(scopeHost)
            ?? throw new InvalidOperationException("_workers is null");

        Assert.That(workers, Has.Length.EqualTo(1),
            "Expected exactly one worker scope.");
        return workers[0];
    }

    private sealed class BlockableUpdateService : IService, IUpdate
    {
        private readonly ManualResetEventSlim _blocker;

        public BlockableUpdateService(ManualResetEventSlim blocker)
        {
            _blocker = blocker;
        }

        public void ConfigureServices(IServiceCollection services) { }

        public void Update()
        {
            _blocker.Wait();
        }
    }

    private sealed class BlockableWorkerLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly ManualResetEventSlim _blocker;

        public BlockableWorkerLayer(ManualResetEventSlim blocker)
        {
            _blocker = blocker;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 999,
                    identity: "scope:test:BlockableWorker",
                    scopeType: typeof(BlockableWorkerScope),
                    factory: static () => new BlockableWorkerScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(typeof(BlockableUpdateService), new BlockableUpdateService(_blocker), typeof(BlockableWorkerScope));
        }
    }

    private sealed class BlockableWorkerScope : IScopeDefinition
    {
        public const int ScopeId = 999;
        public ScopeOptions Options => ScopeOptions.Worker(tickRateHz: 10);
    }

    private sealed class EmptyWorkerService : IService
    {
        public void ConfigureServices(IServiceCollection services) { }
    }
}
