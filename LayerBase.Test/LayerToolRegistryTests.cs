using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
using LayerBase.Tools;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class LayerToolRegistryTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        RuntimeTool.Created = 0;
        RuntimeTool.Disposed = 0;
        NonCachedTool.Created = 0;
        BlockingToolFactory.Reset();
        FailingToolFactory.Attempts = 0;
    }

    [Test]
    public void One_runtime_has_one_layer_tool_registry()
    {
        var runtime = BuildRuntime("tools");
        var tools = runtime.Tools;

        Assert.That(runtime.Tools, Is.Not.Null);
        Assert.That(runtime.Tools, Is.SameAs(tools));
    }

    [Test]
    public void Bound_objects_access_same_registry()
    {
        var layer = new ToolLayer();
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .AddAssemblyModule(ToolModule("tools"))
                              .Build();

        var service = layer.GetService<ToolService>();

        Assert.That(layer.Tools(), Is.Not.SameAs(runtime.Tools));
        Assert.That(service.Tools(), Is.Not.SameAs(runtime.Tools));
        Assert.That(layer.Tools().GetOrCreate<RuntimeTool>(), Is.Not.Null);
        Assert.That(service.Tools().GetOrCreate<RuntimeTool>(), Is.Not.Null);
    }

    [Test]
    public void Cached_tool_is_same_across_scopes_and_runtimes_are_isolated()
    {
        var first = BuildRuntime("first");
        var second = BuildRuntime("second");

        var firstA = first.Tools.GetOrCreate<RuntimeTool>();
        var firstB = first.Tools.GetOrCreate<RuntimeTool>();
        var secondTool = second.Tools.GetOrCreate<RuntimeTool>();

        Assert.That(firstB, Is.SameAs(firstA));
        Assert.That(secondTool, Is.Not.SameAs(firstA));
    }

    [Test]
    public void Contract_key_is_unique_per_runtime()
    {
        var module = new TestAssemblyModule(
            "duplicates",
            tools: new[]
            {
                LayerToolContribution.ForTypes(typeof(IRuntimeTool), typeof(RuntimeTool), "default", typeof(ToolLayer)),
                LayerToolContribution.ForTypes(typeof(IRuntimeTool), typeof(AlternateRuntimeTool), "default", typeof(ToolLayer))
            });

        Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new ToolLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Implementation_type_is_unique_per_runtime()
    {
        var module = new TestAssemblyModule(
            "duplicates",
            tools: new[]
            {
                LayerToolContribution.ForTypes(typeof(RuntimeTool), typeof(RuntimeTool), "default", typeof(ToolLayer)),
                LayerToolContribution.ForTypes(typeof(IRuntimeTool), typeof(RuntimeTool), "named", typeof(ToolLayer))
            });

        Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new ToolLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Tool_owner_layer_must_be_pushed()
    {
        var module = new TestAssemblyModule(
            "missing-layer",
            tools: new[]
            {
                LayerToolContribution.ForTypes(typeof(RuntimeTool), typeof(RuntimeTool), "default", typeof(MissingToolLayer))
            });

        Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new ToolLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Bound_tools_are_limited_to_owner_layer_and_scope()
    {
        var layer = new ToolLayer();
        layer.RegisterService(
            typeof(ScopedToolService),
            new ScopedToolService(),
            typeof(SecondaryToolScope));
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .AddAssemblyModule(new TestAssemblyModule(
                                  "visibility",
                                  tools: new[]
                                  {
                                      LayerToolContribution.ForTypes(
                                          typeof(RuntimeTool),
                                          typeof(RuntimeTool),
                                          "default",
                                          typeof(ToolLayer),
                                          typeof(MainScope)),
                                      LayerToolContribution.ForTypes(
                                          typeof(ScopedRuntimeTool),
                                          typeof(ScopedRuntimeTool),
                                          "default",
                                          typeof(ToolLayer),
                                          typeof(SecondaryToolScope)),
                                      LayerToolContribution.ForTypes(
                                          typeof(OtherLayerTool),
                                          typeof(OtherLayerTool),
                                          "default",
                                          typeof(OtherToolLayer),
                                          typeof(MainScope))
                                  }))
                              .Push(new OtherToolLayer())
                              .Build();

        var mainService = layer.GetService<ToolService>();
        var scopedService = layer.GetService<ScopedToolService>();

        Assert.That(mainService.Tools().GetOrCreate<RuntimeTool>(), Is.Not.Null);
        Assert.Throws<InvalidOperationException>(() => mainService.Tools().GetOrCreate<ScopedRuntimeTool>());
        Assert.Throws<InvalidOperationException>(() => mainService.Tools().GetOrCreate<OtherLayerTool>());

        Assert.That(scopedService.Tools().GetOrCreate<ScopedRuntimeTool>(), Is.Not.Null);
        Assert.Throws<InvalidOperationException>(() => scopedService.Tools().GetOrCreate<RuntimeTool>());
    }

    [Test]
    public void Concurrent_get_or_create_publishes_one_instance()
    {
        var runtime = BuildRuntime("tools");

        var tools = Enumerable.Range(0, 32)
                              .AsParallel()
                              .Select(_ => runtime.Tools.GetOrCreate<RuntimeTool>())
                              .ToArray();

        Assert.That(tools.Distinct().Count(), Is.EqualTo(1));
        Assert.That(RuntimeTool.Created, Is.EqualTo(1));
    }

    [Test]
    public void Factory_failure_does_not_poison_cache()
    {
        var module = new TestAssemblyModule(
            "factory",
            tools: new[]
            {
                LayerToolContribution.ForFactory(
                    typeof(FailingTool),
                    typeof(FailingTool),
                    "default",
                    typeof(ToolLayer),
                    cache: true,
                    static (in LayerToolCreateContext context) => FailingToolFactory.Create(in context))
            });
        var runtime = LayerHub.CreateLayers()
                              .Push(new ToolLayer())
                              .AddAssemblyModule(module)
                              .Build();

        Assert.Throws<InvalidOperationException>(() => runtime.Tools.GetOrCreate<FailingTool>());
        var tool = runtime.Tools.GetOrCreate<FailingTool>();

        Assert.That(tool, Is.Not.Null);
        Assert.That(FailingToolFactory.Attempts, Is.EqualTo(2));
    }

    [Test]
    public void Cache_false_creates_every_time()
    {
        var module = new TestAssemblyModule(
            "non-cache",
            tools: new[]
            {
                LayerToolContribution.ForTypes(typeof(NonCachedTool), typeof(NonCachedTool), "default", typeof(ToolLayer), cache: false)
            });
        var runtime = LayerHub.CreateLayers()
                              .Push(new ToolLayer())
                              .AddAssemblyModule(module)
                              .Build();

        var first = runtime.Tools.GetOrCreate<NonCachedTool>();
        var second = runtime.Tools.GetOrCreate<NonCachedTool>();

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(NonCachedTool.Created, Is.EqualTo(2));
    }

    [Test]
    public void Factory_does_not_run_under_global_lock()
    {
        var module = new TestAssemblyModule(
            "factory-lock",
            tools: new[]
            {
                LayerToolContribution.ForFactory(
                    typeof(BlockingTool),
                    typeof(BlockingTool),
                    "default",
                    typeof(ToolLayer),
                    cache: true,
                    static (in LayerToolCreateContext context) => BlockingToolFactory.Create(in context)),
                LayerToolContribution.ForTypes(typeof(QuickTool), typeof(QuickTool), "default", typeof(ToolLayer))
            });
        var runtime = LayerHub.CreateLayers()
                              .Push(new ToolLayer())
                              .AddAssemblyModule(module)
                              .Build();

        var blockingTask = Task.Run(() => runtime.Tools.GetOrCreate<BlockingTool>());
        Assert.That(BlockingToolFactory.Started.Wait(TimeSpan.FromSeconds(2)), Is.True);

        var quick = runtime.Tools.GetOrCreate<QuickTool>();
        BlockingToolFactory.Release.Set();
        Assert.That(blockingTask.Wait(TimeSpan.FromSeconds(2)), Is.True);

        Assert.That(quick, Is.Not.Null);
    }

    [Test]
    public void Create_context_does_not_expose_service_provider_or_scope_resources()
    {
        var propertyNames = typeof(LayerToolCreateContext)
            .GetProperties()
            .Select(static property => property.Name)
            .ToArray();
        var methodNames = typeof(LayerToolCreateContext)
            .GetMethods()
            .Select(static method => method.Name)
            .ToArray();

        Assert.That(propertyNames, Is.EquivalentTo(new[] { "RuntimeId", "Generation", "Registry" }));
        Assert.That(methodNames, Does.Not.Contain("GetService"));
        Assert.That(methodNames, Does.Not.Contain("GetFactory"));
    }

    [Test]
    public void Tool_dependency_cycle_is_reported()
    {
        var module = new TestAssemblyModule(
            "cycle",
            tools: new[]
            {
                LayerToolContribution.ForFactory(
                    typeof(CycleToolA),
                    typeof(CycleToolA),
                    "default",
                    typeof(ToolLayer),
                    cache: true,
                    static (in LayerToolCreateContext context) => context.Registry.GetOrCreate<CycleToolB>()),
                LayerToolContribution.ForFactory(
                    typeof(CycleToolB),
                    typeof(CycleToolB),
                    "default",
                    typeof(ToolLayer),
                    cache: true,
                    static (in LayerToolCreateContext context) => context.Registry.GetOrCreate<CycleToolA>())
            });
        var runtime = LayerHub.CreateLayers()
                              .Push(new ToolLayer())
                              .AddAssemblyModule(module)
                              .Build();

        Assert.Throws<InvalidOperationException>(() => runtime.Tools.GetOrCreate<CycleToolA>());
    }

    [Test]
    public void Clear_cache_disposes_once_and_allows_recreate()
    {
        var runtime = BuildRuntime("tools");
        var first = runtime.Tools.GetOrCreate<RuntimeTool>();

        runtime.Tools.ClearCache<RuntimeTool>();
        var second = runtime.Tools.GetOrCreate<RuntimeTool>();

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(RuntimeTool.Disposed, Is.EqualTo(1));
    }

    [Test]
    public void Runtime_dispose_clears_tools()
    {
        var runtime = BuildRuntime("tools");

        runtime.Tools.GetOrCreate<RuntimeTool>();
        runtime.Dispose();

        Assert.That(RuntimeTool.Disposed, Is.EqualTo(1));
        Assert.Throws<ObjectDisposedException>(() => runtime.Tools.GetOrCreate<RuntimeTool>());
    }

    [Test]
    public async Task Scope_dispose_does_not_clear_tools()
    {
        var module = new TestAssemblyModule(
            "scope",
            services: new[]
            {
                ServiceContribution.ForTypes(
                    typeof(ScopedToolService),
                    typeof(ScopedToolService),
                    typeof(ToolLayer),
                    typeof(SecondaryToolScope),
                    ServiceLifetime.Singleton)
            },
            tools: new[]
            {
                LayerToolContribution.ForTypes(typeof(RuntimeTool), typeof(RuntimeTool), "default", typeof(ToolLayer))
            });
        var runtime = LayerHub.CreateLayers()
                              .Push(new ToolLayer())
                              .AddAssemblyModule(module)
                              .Build();
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(SecondaryToolScope.ScopeId, nameof(SecondaryToolScope), typeof(SecondaryToolScope)),
                    ScopeOptions.Inline)
            },
            runtime.Id,
            runtime.Generation);
        var first = runtime.Tools.GetOrCreate<RuntimeTool>();

        var scope = host.Scopes[1];
        var disposeTask = scope.RequestDisposeAsync();
        scope.PumpIngress();
        _ = await disposeTask;

        Assert.That(runtime.Tools.GetOrCreate<RuntimeTool>(), Is.SameAs(first));
        Assert.That(RuntimeTool.Disposed, Is.EqualTo(0));
    }

    [Test]
    public void Runtime_lookup_uses_precomputed_slot()
    {
        var runtime = BuildRuntime("tools");
        var slot = runtime.Tools.ResolveSlot<RuntimeTool>();

        var first = runtime.Tools.GetOrCreate<RuntimeTool>(slot);
        var second = runtime.Tools.GetOrCreate<RuntimeTool>(slot);

        Assert.That(slot.Index, Is.EqualTo(runtime.Tools.Diagnostics.Single(static tool => tool.ContractType == typeof(RuntimeTool)).Slot));
        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void Tool_contribution_keeps_owner_scope()
    {
        var properties = typeof(LayerToolContribution)
            .GetProperties()
            .Select(static property => property.Name)
            .ToArray();

        Assert.That(properties, Does.Contain(nameof(LayerToolContribution.OwnerScopeType)));
    }

    private static LayerRuntime BuildRuntime(string moduleId)
    {
        return LayerHub.CreateLayers()
                       .Push(new ToolLayer())
                       .AddAssemblyModule(ToolModule(moduleId))
                       .Build();
    }

    private static IAssemblyModule ToolModule(string id)
    {
        return new TestAssemblyModule(
            id,
            services: new[]
            {
                ServiceContribution.ForTypes(
                    typeof(ToolService),
                    typeof(ToolService),
                    typeof(ToolLayer),
                    typeof(MainScope),
                    ServiceLifetime.Singleton)
            },
            tools: new[]
            {
                LayerToolContribution.ForTypes(typeof(RuntimeTool), typeof(RuntimeTool), "default", typeof(ToolLayer)),
                LayerToolContribution.ForTypes(typeof(IRuntimeTool), typeof(NamedRuntimeTool), "named", typeof(ToolLayer))
            });
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
                Array.Empty<EventHandlerContribution>(),
                tools ?? Array.Empty<LayerToolContribution>());
        }

        public AssemblyModuleId Id { get; }

        public AssemblyModuleManifest Manifest { get; }
    }

    private sealed class ToolLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ToolService, ToolService>();
        }
    }

    private sealed class MissingToolLayer : Layer
    {
    }

    private sealed class OtherToolLayer : Layer
    {
    }

    private sealed class ToolService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(this);
        }
    }

    private interface IRuntimeTool
    {
    }

    private sealed class RuntimeTool : IRuntimeTool, IDisposable
    {
        public RuntimeTool()
        {
            Interlocked.Increment(ref Created);
        }

        public static int Created;
        public static int Disposed;

        public void Dispose()
        {
            Interlocked.Increment(ref Disposed);
        }
    }

    private sealed class NamedRuntimeTool : IRuntimeTool
    {
    }

    private sealed class AlternateRuntimeTool : IRuntimeTool
    {
    }

    private sealed class NonCachedTool
    {
        public NonCachedTool()
        {
            Interlocked.Increment(ref Created);
        }

        public static int Created;
    }

    private sealed class BlockingTool
    {
    }

    private sealed class QuickTool
    {
    }

    private sealed class CycleToolA
    {
    }

    private sealed class CycleToolB
    {
    }

    [Scope<SecondaryToolScope>]
    private sealed class ScopedToolService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(this);
        }
    }

    private sealed class ScopedRuntimeTool
    {
    }

    private sealed class OtherLayerTool
    {
    }

    private sealed class SecondaryToolScope : IScopeDefinition
    {
        public const int ScopeId = 9;
        public ScopeOptions Options => ScopeOptions.Inline;
        
    }

    private sealed class FailingTool
    {
    }

    private static class FailingToolFactory
    {
        public static int Attempts;

        public static object Create(in LayerToolCreateContext context)
        {
            if (Interlocked.Increment(ref Attempts) == 1)
            {
                throw new InvalidOperationException("first attempt fails");
            }

            return new FailingTool();
        }
    }

    private static class BlockingToolFactory
    {
        public static ManualResetEventSlim Started = new(false);
        public static ManualResetEventSlim Release = new(false);

        public static void Reset()
        {
            Started.Dispose();
            Release.Dispose();
            Started = new ManualResetEventSlim(false);
            Release = new ManualResetEventSlim(false);
        }

        public static object Create(in LayerToolCreateContext context)
        {
            Started.Set();
            if (!Release.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Blocking tool factory was not released.");
            }

            return new BlockingTool();
        }
    }
}
