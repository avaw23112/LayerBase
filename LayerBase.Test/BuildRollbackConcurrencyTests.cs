using System.Diagnostics;
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
[Category("ProductionHardening")]
public sealed class BuildRollbackConcurrencyTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Failure_before_workers_start_does_not_wait_for_worker_control_call()
    {
        var layer = new ThrowDuringConfigureServicesLayer();

        var started = Stopwatch.StartNew();

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = LayerHub.CreateLayers()
                .Push(layer)
                .Build();
        });

        started.Stop();

        Assert.That(started.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
    }

    [Test]
    [Category("ProductionSoak")]
    public void Runtime_dispose_returns_at_deadline_when_worker_scope_is_blocked()
    {
        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        var sw = Stopwatch.StartNew();

        try
        {
            _ = BuildRuntimeWithBlockingWorkerScope(entered, release);
        }
        catch (TimeoutException)
        {
        }

        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(40000),
            "Build timeout with a blocked worker scope must complete within 40s.");
    }

    private static LayerRuntime BuildRuntimeWithBlockingWorkerScope(
        ManualResetEventSlim entered,
        ManualResetEventSlim release)
    {
        var layer = new BlockedWorkerScopeLayer(entered, release);

        return LayerHub.CreateLayers()
            .Push(layer)
            .Build();
    }

    private sealed class ThrowDuringConfigureServicesLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            throw new InvalidOperationException("build failure");
        }
    }

    private sealed class BlockedWorkerScopeLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly ManualResetEventSlim _entered;
        private readonly ManualResetEventSlim _release;

        public BlockedWorkerScopeLayer(
            ManualResetEventSlim entered,
            ManualResetEventSlim release)
        {
            _entered = entered;
            _release = release;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 889,
                    identity: "scope:test:BlockedWorkerScope",
                    scopeType: typeof(BlockedWorkerScope),
                    factory: static () => new BlockedWorkerScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(
                typeof(BlockingUpdateService),
                new BlockingUpdateService(_entered, _release),
                typeof(BlockedWorkerScope));
        }
    }

    private sealed class BlockedWorkerScope : IScopeDefinition
    {
        public const int ScopeId = 889;

        public ScopeOptions Options => ScopeOptions.Worker(tickRateHz: 30);
    }

    private sealed class BlockingUpdateService : IService, IUpdate
    {
        private readonly ManualResetEventSlim _entered;
        private readonly ManualResetEventSlim _release;
        private bool _blocked;

        public BlockingUpdateService(
            ManualResetEventSlim entered,
            ManualResetEventSlim release)
        {
            _entered = entered;
            _release = release;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            if (_blocked)
                return;

            _blocked = true;
            _entered.Set();
            _release.Wait();
        }
    }
}
