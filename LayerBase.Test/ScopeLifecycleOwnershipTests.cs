using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeLifecycleOwnershipTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Disposing_secondary_scope_disposes_only_secondary_services()
    {
        var runtimeStopLog = new List<string>();
        var disposeLog = new List<string>();

        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new OwnerLayer(runtimeStopLog, disposeLog))
            .Build();

        Assert.That(runtime.ScopeHost.Scopes, Has.Count.EqualTo(2));

        runtime.ScopeHost.Scopes[1].Dispose();

        Assert.That(runtimeStopLog, Does.Contain("Secondary"));
        Assert.That(runtimeStopLog, Has.No.Member("Main"));
    }

    [Test]
    public void Runtime_stop_runs_each_scope_services_exactly_once()
    {
        var runtimeStopLog = new List<string>();
        var disposeLog = new List<string>();

        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new OwnerLayer(runtimeStopLog, disposeLog))
            .Build();

        runtime.Dispose();

        Assert.That(runtimeStopLog, Has.Count.EqualTo(2));
        Assert.That(runtimeStopLog, Contains.Item("Main"));
        Assert.That(runtimeStopLog, Contains.Item("Secondary"));
    }

    private sealed class MainService : IService, IInitializable, IRuntimeStop, IDisposable
    {
        private readonly List<string> _runtimeStopLog;
        private readonly List<string> _disposeLog;
        public MainService(List<string> runtimeStopLog, List<string> disposeLog)
        {
            _runtimeStopLog = runtimeStopLog;
            _disposeLog = disposeLog;
        }
        public void ConfigureServices(IServiceCollection services) { }
        public void Initialize() { }
        public void RuntimeStop() => _runtimeStopLog.Add("Main");
        public void Dispose() => _disposeLog.Add("MainDispose");
    }

    private sealed class SecondaryService : IService, IInitializable, IRuntimeStop, IDisposable
    {
        private readonly List<string> _runtimeStopLog;
        private readonly List<string> _disposeLog;
        public SecondaryService(List<string> runtimeStopLog, List<string> disposeLog)
        {
            _runtimeStopLog = runtimeStopLog;
            _disposeLog = disposeLog;
        }
        public void ConfigureServices(IServiceCollection services) { }
        public void Initialize() { }
        public void RuntimeStop() => _runtimeStopLog.Add("Secondary");
        public void Dispose() => _disposeLog.Add("SecondaryDispose");
    }

    private sealed class OwnerLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly List<string> _runtimeStopLog;
        private readonly List<string> _disposeLog;
        public OwnerLayer(List<string> runtimeStopLog, List<string> disposeLog)
        {
            _runtimeStopLog = runtimeStopLog;
            _disposeLog = disposeLog;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(
                typeof(MainService),
                new MainService(_runtimeStopLog, _disposeLog),
                typeof(MainScope));

            RegisterService(
                typeof(SecondaryService),
                new SecondaryService(_runtimeStopLog, _disposeLog),
                typeof(SecondaryScope));
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 777,
                    identity: "scope:test:SecondaryScope",
                    scopeType: typeof(SecondaryScope),
                    factory: static () => new SecondaryScope())
            };
        }
    }

    private sealed class SecondaryScope : IScopeDefinition
    {
        public const int ScopeId = 777;
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
