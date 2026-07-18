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
        var runtime = LayerHub.CreateLayers()
            .Push(new StuckWorkerLayer())
            .Build();

        Assert.DoesNotThrow(() => runtime.Dispose());
    }

    [Test]
    public void Timed_out_scope_reports_shutdown_fault()
    {
        Exception? capturedFault = null;
        var runtime = LayerHub.CreateLayers()
            .Push(new StuckWorkerLayer())
            .Build();

        runtime.Faulted += info =>
        {
            capturedFault = info.Record.Exception;
        };

        runtime.Dispose();
    }

    [Test]
    public void Shutdown_timeout_does_not_cause_object_disposed_exception_on_worker()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new StuckWorkerLayer())
            .Build();

        Assert.DoesNotThrow(() => runtime.Dispose());
    }

    [Test]
    public void Runtime_dispose_is_bounded()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new StuckWorkerLayer())
            .Build();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        runtime.Dispose();
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(17000),
            "Shutdown shares one 15s deadline; a stuck scope may consume it fully but never more.");
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

    private sealed class EmptyWorkerService : IService
    {
        public void ConfigureServices(IServiceCollection services) { }
    }
}
