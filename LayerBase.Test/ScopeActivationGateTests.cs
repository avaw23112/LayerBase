using System.Collections.Concurrent;
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeActivationGateTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Worker_scope_does_not_pump_update_before_runtime_start()
    {
        var probe = new ActivationGateProbe();

        using var runtime = LayerHub.CreateLayers()
            .Push(new ActivationGateLayer(probe))
            .Build();

        Assert.That(probe.UpdatesBeforeRuntimeStart, Is.EqualTo(0));
        Assert.That(probe.Trace, Does.Contain("RuntimeStart"));
        Assert.That(runtime.ScopeHost.Scopes.Single(static scope => scope.ScopeId == ActivationGateWorkerScope.ScopeId).State,
            Is.EqualTo(ScopeRuntimeState.Running));
    }

    private sealed class ActivationGateLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly ActivationGateProbe _probe;

        public ActivationGateLayer(ActivationGateProbe probe)
        {
            _probe = probe;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    ActivationGateWorkerScope.ScopeId,
                    "scope:test:ActivationGateWorkerScope",
                    typeof(ActivationGateWorkerScope),
                    static () => new ActivationGateWorkerScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new MainInitializeDelayService(_probe));
            RegisterService(
                typeof(WorkerUpdateBeforeStartService),
                new WorkerUpdateBeforeStartService(_probe),
                typeof(ActivationGateWorkerScope));
        }
    }

    private sealed class ActivationGateWorkerScope : IScopeDefinition
    {
        public const int ScopeId = 778;

        public ScopeOptions Options => ScopeOptions.Worker(tickRateHz: 1000);
    }

    private sealed class ActivationGateProbe
    {
        private int _runtimeStarted;
        private int _updatesBeforeRuntimeStart;

        public ConcurrentQueue<string> Trace { get; } = new();

        public int UpdatesBeforeRuntimeStart => Volatile.Read(ref _updatesBeforeRuntimeStart);

        public void MarkRuntimeStarted()
        {
            Volatile.Write(ref _runtimeStarted, 1);
            Trace.Enqueue("RuntimeStart");
        }

        public void RecordUpdate()
        {
            if (Volatile.Read(ref _runtimeStarted) == 0)
                Interlocked.Increment(ref _updatesBeforeRuntimeStart);
        }
    }

    private sealed class MainInitializeDelayService : IService, IInitializable
    {
        private readonly ActivationGateProbe _probe;

        public MainInitializeDelayService(ActivationGateProbe probe)
        {
            _probe = probe;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            _probe.Trace.Enqueue("MainInitialize");
            Thread.Sleep(100);
        }
    }

    private sealed class WorkerUpdateBeforeStartService : IService, IUpdate, IRuntimeStart
    {
        private readonly ActivationGateProbe _probe;

        public WorkerUpdateBeforeStartService(ActivationGateProbe probe)
        {
            _probe = probe;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            _probe.RecordUpdate();
        }

        public void RuntimeStart()
        {
            _probe.MarkRuntimeStarted();
        }
    }
}
