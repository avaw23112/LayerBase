using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
using LayerBase.Tools;

namespace EventsTest;

[TestFixture]
public sealed class BuildActivationOrderTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        BuildOrderPrewarmProbe.Trace = null;
        BuildOrderPrewarmProbe.Centers = null;
        BuildOrderPrewarmProbe.EnsureRegistered();
    }

    [TearDown]
    public void TearDown()
    {
        BuildOrderPrewarmProbe.Trace = null;
        BuildOrderPrewarmProbe.Centers = null;
    }

    [Test]
    public void Build_returns_running_runtime_for_master_compatibility()
    {
        using var runtime = LayerHub.CreateLayers()
                                    .Push(new BuildOrderLayer(new List<string>()))
                                    .Build();

        Assert.That(runtime.State, Is.EqualTo(RuntimeState.Running));
        Assert.DoesNotThrow(() => runtime.Pump(0.016f));
    }

    [Test]
    public void Explicit_activate_is_idempotent_when_running()
    {
        using var runtime = LayerHub.CreateLayers()
                                    .Push(new BuildOrderLayer(new List<string>()))
                                    .Build();

        Assert.DoesNotThrow(() => runtime.Activate());
        Assert.That(runtime.State, Is.EqualTo(RuntimeState.Running));
    }

    [Test]
    public void Public_prewarm_remains_non_throwing_and_idempotent()
    {
        using var runtime = LayerHub.CreateLayers()
                                    .Push(new BuildOrderLayer(new List<string>()))
                                    .Build();

        Assert.DoesNotThrow(() => runtime.Prewarm());
        Assert.DoesNotThrow(() => runtime.Prewarm(new LayerPrewarmOptions(LayerPrewarmTargets.All)));
        Assert.That(runtime.State, Is.EqualTo(RuntimeState.Running));
    }

    [Test]
    public void Prewarm_runs_after_post_build_before_runtime_start()
    {
        var trace = new List<string>();
        BuildOrderPrewarmProbe.Trace = trace;

        using var runtime = LayerHub.CreateLayers()
                                    .Push(new BuildOrderLayer(trace))
                                    .Build();

        Assert.That(trace, Is.EqualTo(new[] { "PostBuild", "Prewarm", "RuntimeStart" }));
        Assert.That(runtime.State, Is.EqualTo(RuntimeState.Running));
    }

    [Test]
    public void Each_scope_prewarms_its_own_event_center()
    {
        var centers = new List<EventCenter>();
        BuildOrderPrewarmProbe.Centers = centers;

        using var runtime = LayerHub.CreateLayers()
                                    .Push(new BuildOrderLayer(new List<string>()))
                                    .AddAssemblyModule(new TestAssemblyModule(
                                        "scope",
                                        services: new[]
                                        {
                                            ServiceContribution.ForTypes(
                                                typeof(IScopedBuildOrderService),
                                                typeof(ScopedBuildOrderService),
                                                typeof(BuildOrderLayer),
                                                typeof(BuildOrderScope),
                                                ServiceLifetime.Singleton)
                                        }))
                                    .Build();

        Assert.That(runtime.CompositionPlan.Scopes.Select(static scope => scope.Descriptor.ScopeId),
            Does.Contain(BuildOrderScope.ScopeId));
        Assert.That(centers.Select(static center => center.GetHashCode()).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Prewarm_does_not_create_all_tools()
    {
        BuildOrderTool.Created = 0;

        using var runtime = LayerHub.CreateLayers()
                                    .Push(new BuildOrderLayer(new List<string>()))
                                    .AddAssemblyModule(new TestAssemblyModule(
                                        "tools",
                                        tools: new[]
                                        {
                                            LayerToolContribution.ForTypes(
                                                typeof(BuildOrderTool),
                                                typeof(BuildOrderTool),
                                                "default",
                                                typeof(BuildOrderLayer))
                                        }))
                                    .Build();

        Assert.That(runtime.Tools.Count, Is.EqualTo(1));
        Assert.That(BuildOrderTool.Created, Is.EqualTo(0));
    }

    [Test]
    public void Scope_registries_are_frozen_before_runtime_start()
    {
        using var runtime = LayerHub.CreateLayers()
                                    .Push(new BuildOrderLayer(new List<string>(), assertFrozenOnRuntimeStart: true))
                                    .Build();

        Assert.That(runtime.State, Is.EqualTo(RuntimeState.Running));
    }

    private sealed class BuildOrderLayer : Layer
    {
        private readonly List<string> _trace;
        private readonly bool _assertFrozenOnRuntimeStart;

        public BuildOrderLayer(List<string> trace, bool assertFrozenOnRuntimeStart = false)
        {
            _trace = trace;
            _assertFrozenOnRuntimeStart = assertFrozenOnRuntimeStart;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new BuildOrderService(_trace, _assertFrozenOnRuntimeStart));
        }
    }

    private sealed class BuildOrderService : IService, IPostBuild, IRuntimeStart
    {
        private readonly List<string> _trace;
        private readonly bool _assertFrozenOnRuntimeStart;

        public BuildOrderService(List<string> trace, bool assertFrozenOnRuntimeStart)
        {
            _trace = trace;
            _assertFrozenOnRuntimeStart = assertFrozenOnRuntimeStart;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void PostBuild()
        {
            _trace.Add("PostBuild");
        }

        public void RuntimeStart()
        {
            if (_assertFrozenOnRuntimeStart)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    ServiceLayerBinder.RequireBinding(this).OwnerScope.PolicyTable!.SetPostPolicy(
                        EventTypeId<BuildOrderRuntimeStartEvent>.Id,
                        new EventPostPolicy(PostDeliveryMode.Normal, BackpressurePolicy.DropNewest, 1)));
            }

            _trace.Add("RuntimeStart");
        }
    }

    private sealed class BuildOrderTool
    {
        public BuildOrderTool()
        {
            Created++;
        }

        public static int Created;
    }

    private readonly struct BuildOrderRuntimeStartEvent
    {
    }

    private interface IScopedBuildOrderService
    {
    }

    private sealed class ScopedBuildOrderService : IScopedBuildOrderService
    {
    }

    private sealed class BuildOrderScope : IScopeDefinition
    {
        public const int ScopeId = 26;
        public ScopeOptions Options => ScopeOptions.Inline;
        
    }

    private sealed class TestAssemblyModule : IAssemblyModule
    {
        public TestAssemblyModule(
            string id,
            ServiceContribution[]? services = null,
            LayerToolContribution[]? tools = null)
        {
            Id = new AssemblyModuleId(id);
            Manifest = new AssemblyModuleManifest(
                Id,
                services ?? Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                Array.Empty<LocalCallContribution>(),
                tools ?? Array.Empty<LayerToolContribution>());
        }

        public AssemblyModuleId Id { get; }

        public AssemblyModuleManifest Manifest { get; }
    }

    private static class BuildOrderPrewarmProbe
    {
        private static int s_registered;

        public static List<string>? Trace;

        public static List<EventCenter>? Centers;

        public static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref s_registered, 1) != 0)
                return;

            LayerBasePrewarmRegistry.Register((center, _) =>
            {
                Trace?.Add("Prewarm");
                Centers?.Add(center);
            });
        }
    }
}
