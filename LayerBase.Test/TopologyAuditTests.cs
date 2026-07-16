using System.Reflection;
using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
public sealed class TopologyAuditTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Missing_scope_local_call_route_fails_during_build_audit()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new BrokenLocalCallLayer())
                    .Build());

        Assert.That(ex!.Message, Does.Contain("not active"));
        Assert.That(ex.Message, Does.Contain("999"));
    }

    [Test]
    public void Topology_markdown_includes_scope_and_local_call_routes()
    {
        var runtime = LayerHub.CreateLayers()
                              .Push(new GameplayLayer())
                              .AddAssemblyModule(new TestAssemblyModule(
                                  "calls",
                                  calls: new[]
                                  {
                                      LocalCallContribution.ForTypes(
                                          typeof(TopologyRequest),
                                          typeof(TopologyResponse),
                                          typeof(PathfindingCallHandler),
                                          typeof(GameplayLayer),
                                          typeof(PathfindingScope))
                                  }))
                              .Build();

        var markdown = runtime.GetTopologyMarkdown();

        Assert.That(markdown, Does.Contain("## 2. Scopes"));
        Assert.That(markdown, Does.Contain("PathfindingScope"));
        Assert.That(markdown, Does.Contain("## 4. Scope Local Calls"));
        Assert.That(markdown, Does.Contain("TopologyRequest"));
        Assert.That(markdown, Does.Contain("TopologyResponse"));
    }

    [Test]
    public void Synchronous_event_cycle_error_identifies_scope_partition()
    {
        var layer = new GameplayLayer();
        layer.RegisterService(new DirectCycleService());

        var ex = Assert.Throws<EventCycleException>(() =>
            LayerHub.CreateLayers()
                    .Push(layer)
                    .Build());

        Assert.That(ex!.Message, Does.Contain("Scope 0"));
        Assert.That(ex.Message, Does.Contain("Cycle path"));
    }

    [Test]
    public void Audit_temporary_graph_is_not_kept_in_runtime()
    {
        var runtimeFields = typeof(LayerRuntime)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(static field => field.FieldType)
            .ToArray();

        Assert.That(runtimeFields.Any(IsTypeGraphField), Is.False);
    }

    private static bool IsTypeGraphField(Type type)
    {
        if (!type.IsGenericType)
            return false;

        Type definition = type.GetGenericTypeDefinition();
        if (definition != typeof(Dictionary<,>) && definition != typeof(HashSet<>))
            return false;

        return type.GetGenericArguments().Any(static argument => argument == typeof(Type));
    }

    private sealed class BrokenLocalCallLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            RegisterCallHandler<TopologyRequest, TopologyResponse, MissingScope>(
                new PathfindingCallHandler());
        }
    }

    private sealed class GameplayLayer : Layer
    {
    }

    private readonly struct PathfindingScope : IScopeDefinition
    {
        public const int ScopeId = 25;
    }

    private readonly struct MissingScope : IScopeDefinition
    {
        public const int ScopeId = 999;
    }

    private readonly struct TopologyRequest
    {
    }

    private readonly struct TopologyResponse
    {
    }

    private sealed class PathfindingCallHandler
        : IScopeLocalCallHandler<TopologyRequest, TopologyResponse>
    {
        public async LBTask<TopologyResponse> HandleAsync(
            TopologyRequest request,
            CancellationToken cancellationToken = default)
        {
            await LBTask.CompletedTask;
            return new TopologyResponse();
        }
    }

    private readonly struct CycleEvent
    {
    }

    private sealed class DirectCycleService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<DirectCycleSubscriber>(new DirectCycleSubscriber());
        }
    }

    private sealed class DirectCycleSubscriber : IAutoSubscribe
    {
        public void AutoBind(Layer layer)
        {
            layer.Subscribe<CycleEvent>(Handle);
        }

        public IEnumerable<EventDependency> GetEventDependencies()
        {
            yield return new EventDependency(typeof(CycleEvent), typeof(CycleEvent));
        }

        public IEnumerable<Type> GetSubscribedEvents()
        {
            yield return typeof(CycleEvent);
        }

        private static void Handle(in CycleEvent @event)
        {
        }
    }

    private sealed class TestAssemblyModule : IAssemblyModule
    {
        public TestAssemblyModule(
            string id,
            LocalCallContribution[]? calls = null)
        {
            Id = new AssemblyModuleId(id);
            Manifest = new AssemblyModuleManifest(
                Id,
                Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                calls ?? Array.Empty<LocalCallContribution>(),
                Array.Empty<EventHandlerContribution>(),
                Array.Empty<LayerToolContribution>());
        }

        public AssemblyModuleId Id { get; }

        public AssemblyModuleManifest Manifest { get; }
    }
}
