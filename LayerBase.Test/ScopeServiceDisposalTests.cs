using LayerBase;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeServiceDisposalTests
{
    private readonly List<string> _disposeLog = new();

    [SetUp]
    public void SetUp()
    {
        _disposeLog.Clear();
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Secondary_scope_service_is_disposed_once()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ServiceDisposalLayer(_disposeLog))
            .Build();

        runtime.ScopeHost.Scopes[1].Dispose();

        Assert.That(_disposeLog, Has.Exactly(1).Matches<string>(s => s.Contains("SecondaryService")));
    }

    [Test]
    public void Main_scope_service_is_disposed_once()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ServiceDisposalLayer(_disposeLog))
            .Build();

        runtime.Dispose();

        Assert.That(_disposeLog, Has.Some.Contains("MainService"));
    }

    [Test]
    public void Same_instance_registered_as_multiple_interfaces_is_disposed_once()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new MultiInterfaceLayer(_disposeLog))
            .Build();

        runtime.Dispose();

        Assert.That(_disposeLog, Has.Exactly(1).Matches<string>(s => s.Contains("MultiService")));
    }

    [Test]
    public void Disposing_secondary_scope_does_not_dispose_main_service()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ServiceDisposalLayer(_disposeLog))
            .Build();

        runtime.ScopeHost.Scopes[1].Dispose();

        Assert.That(_disposeLog, Has.None.Matches<string>(s => s.Contains("MainService")));
    }

    [Test]
    public void Runtime_dispose_after_secondary_dispose_does_not_double_dispose()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ServiceDisposalLayer(_disposeLog))
            .Build();

        runtime.ScopeHost.Scopes[1].Dispose();
        runtime.Dispose();

        Assert.That(_disposeLog, Has.Some.Contains("MainService"));
        Assert.That(_disposeLog, Has.Some.Contains("SecondaryService"));
    }

    private sealed class TracedDisposable : IService, IDisposable
    {
        private readonly List<string> _log;
        private readonly string _name;
        private int _disposeCount;

        public TracedDisposable(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public void ConfigureServices(IServiceCollection services) { }

        public void Dispose()
        {
            int count = Interlocked.Increment(ref _disposeCount);
            _log.Add($"{_name}:{count}");
        }
    }

    private sealed class ServiceDisposalLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly List<string> _log;

        public ServiceDisposalLayer(List<string> log)
        {
            _log = log;
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

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(
                typeof(IService),
                new TracedDisposable(_log, "MainService"),
                typeof(MainScope));

            RegisterService(
                typeof(IService),
                new TracedDisposable(_log, "SecondaryService"),
                typeof(SecondaryScope));
        }
    }

    private sealed class MultiInterfaceLayer : Layer
    {
        private readonly List<string> _log;

        public MultiInterfaceLayer(List<string> log)
        {
            _log = log;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            var instance = new TracedDisposable(_log, "MultiService");
            RegisterService(typeof(IService), instance);
        }
    }

    private sealed class SecondaryScope : IScopeDefinition
    {
        public const int ScopeId = 777;
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
