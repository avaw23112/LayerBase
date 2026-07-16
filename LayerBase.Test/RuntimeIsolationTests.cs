using System.Reflection;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
using LayerBase.Tools;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class RuntimeIsolationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        IsolationTool.Disposed = 0;
    }

    [Test]
    public void Static_manifest_is_shared_and_immutable()
    {
        var services = new[]
        {
            ServiceContribution.ForTypes(
                typeof(IsolationService),
                typeof(IsolationService),
                typeof(IsolationLayer),
                typeof(MainScope))
        };
        var manifest = new AssemblyModuleManifest(new AssemblyModuleId("immutable"), services);

        services[0] = ServiceContribution.ForTypes(
            typeof(AlternateIsolationService),
            typeof(AlternateIsolationService),
            typeof(IsolationLayer),
            typeof(MainScope));

        Assert.That(manifest.Services.Single().ServiceType, Is.EqualTo(typeof(IsolationService)));
        Assert.That(manifest.Services.GetType().IsArray, Is.False);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ServiceContribution>)manifest.Services).Add(services[0]));
    }

    [Test]
    public void Runtime_plans_are_independent()
    {
        var first = BuildRuntime("first");
        var second = BuildRuntime("second");

        Assert.That(second.CompositionPlan, Is.Not.SameAs(first.CompositionPlan));
        Assert.That(second.CompositionPlan.Layers, Is.Not.SameAs(first.CompositionPlan.Layers));
        Assert.That(second.CompositionPlan.Scopes, Is.Not.SameAs(first.CompositionPlan.Scopes));
    }

    [Test]
    public void Scope_states_are_independent()
    {
        var first = BuildRuntime("first");
        var second = BuildRuntime("second");

        Assert.That(second.ScopeHost.MainScope, Is.Not.SameAs(first.ScopeHost.MainScope));
        Assert.That(second.ScopeHost.MainScope.EventCenter, Is.Not.SameAs(first.ScopeHost.MainScope.EventCenter));
        Assert.That(second.ScopeHost.MainScope.PostScheduler, Is.Not.SameAs(first.ScopeHost.MainScope.PostScheduler));
        Assert.That(second.ScopeHost.MainScope.Timer, Is.Not.SameAs(first.ScopeHost.MainScope.Timer));
        Assert.That(second.ScopeHost.MainScope.EcsWorld, Is.Not.SameAs(first.ScopeHost.MainScope.EcsWorld));
    }

    [Test]
    public void Different_runtimes_do_not_share_tool_registry_or_cached_tools()
    {
        var first = BuildRuntime("first");
        var second = BuildRuntime("second");

        Assert.That(second.Tools, Is.Not.SameAs(first.Tools));
        Assert.That(second.Tools.GetOrCreate<IsolationTool>(), Is.Not.SameAs(first.Tools.GetOrCreate<IsolationTool>()));
    }

    [Test]
    public void Bound_objects_access_runtime_tool_cache_through_views()
    {
        var layer = new IsolationLayer();
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .AddAssemblyModule(ToolModule("tools"))
                              .Build();
        var service = layer.GetService<IsolationService>();

        Assert.That(layer.Tools(), Is.Not.SameAs(runtime.Tools));
        Assert.That(service.Tools(), Is.Not.SameAs(runtime.Tools));
        Assert.That(layer.Tools().GetOrCreate<IsolationTool>(), Is.SameAs(runtime.Tools.GetOrCreate<IsolationTool>()));
        Assert.That(service.Tools().GetOrCreate<IsolationTool>(), Is.SameAs(runtime.Tools.GetOrCreate<IsolationTool>()));
    }

    [Test]
    public void Different_runtimes_do_not_share_actor_world_or_event_center()
    {
        var first = BuildRuntime("first");
        var second = BuildRuntime("second");

        Assert.That(second.Actors, Is.Not.SameAs(first.Actors));
        Assert.That(second.ScopeHost.MainScope.EventCenter, Is.Not.SameAs(first.ScopeHost.MainScope.EventCenter));
    }

    [Test]
    public void Disposing_runtime_a_keeps_runtime_b_running()
    {
        var first = BuildRuntime("first");
        var second = BuildRuntime("second");
        var secondTool = second.Tools.GetOrCreate<IsolationTool>();

        first.Dispose();

        Assert.That(second.Tools.GetOrCreate<IsolationTool>(), Is.SameAs(secondTool));
        Assert.DoesNotThrow(() => second.Pump(0.016f));
    }

    [Test]
    public async Task Scope_dispose_does_not_clear_runtime_tools()
    {
        var runtime = BuildRuntime("tools");
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(IsolationScope.ScopeId, nameof(IsolationScope), typeof(IsolationScope)),
                    ScopeOptions.Inline)
            },
            runtime.Id,
            runtime.Generation);
        var tool = runtime.Tools.GetOrCreate<IsolationTool>();

        var scope = host.Scopes[1];
        var disposeTask = scope.RequestDisposeAsync();
        scope.PumpIngress();
        _ = await disposeTask;

        Assert.That(runtime.Tools.GetOrCreate<IsolationTool>(), Is.SameAs(tool));
        Assert.That(IsolationTool.Disposed, Is.EqualTo(0));
    }

    [Test]
    public void Runtime_dispose_does_not_clear_static_manifest()
    {
        var module = ToolModule("manifest");
        var manifest = module.Manifest;
        var runtime = LayerHub.CreateLayers()
                              .Push(new IsolationLayer())
                              .AddAssemblyModule(module)
                              .Build();

        runtime.Dispose();

        Assert.That(module.Manifest, Is.SameAs(manifest));
        Assert.That(module.Manifest.Tools.Count, Is.EqualTo(1));
    }

    [Test]
    public void No_layer_hub_current_exists()
    {
        var members = typeof(LayerHub)
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Select(static member => member.Name)
            .ToArray();

        Assert.That(members, Does.Not.Contain("Current"));
    }

    [Test]
    public void No_global_service_provider_exists()
    {
        var serviceProviderStaticFields = typeof(LayerRuntime).Assembly
            .GetTypes()
            .SelectMany(static type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(static field => field.FieldType.Name.Contains("ServiceProvider", StringComparison.Ordinal))
            .Select(static field => $"{field.DeclaringType?.FullName}.{field.Name}")
            .ToArray();

        Assert.That(serviceProviderStaticFields, Is.Empty);
    }

    [Test]
    public void No_static_mutable_tool_cache_exists()
    {
        var staticToolCacheFields = typeof(LayerToolRegistry)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static field => field.GetCustomAttribute<ThreadStaticAttribute>() == null)
            .Where(static field =>
                field.FieldType == typeof(LayerToolRegistry) ||
                field.FieldType.IsArray ||
                field.FieldType.Name.Contains("Dictionary", StringComparison.Ordinal))
            .Select(static field => field.Name)
            .ToArray();

        Assert.That(staticToolCacheFields, Is.Empty);
    }

    [Test]
    public void Runtime_access_does_not_expose_scope_resources()
    {
        var exposedTypes = typeof(LayerRuntimeAccess)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Select(member => member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                MethodInfo method when !method.IsSpecialName => method.ReturnType,
                _ => null
            })
            .Where(static type => type != null)
            .Cast<Type>()
            .ToArray();

        Assert.That(exposedTypes, Does.Not.Contain(typeof(ScopeRuntime)));
        Assert.That(exposedTypes, Does.Not.Contain(typeof(EventCenter)));
        Assert.That(exposedTypes, Does.Not.Contain(typeof(ActorWorld)));
        Assert.That(exposedTypes.Select(static type => type.Name), Does.Not.Contain("World"));
    }

    private static LayerRuntime BuildRuntime(string id)
    {
        return LayerHub.CreateLayers()
                       .Push(new IsolationLayer())
                       .AddAssemblyModule(ToolModule(id))
                       .Build();
    }

    private static IAssemblyModule ToolModule(string id)
    {
        return new TestAssemblyModule(
            id,
            tools: new[]
            {
                LayerToolContribution.ForTypes(
                    typeof(IsolationTool),
                    typeof(IsolationTool),
                    "default",
                    typeof(IsolationLayer))
            });
    }

    private sealed class TestAssemblyModule : IAssemblyModule
    {
        public TestAssemblyModule(string id, LayerToolContribution[]? tools = null)
        {
            Id = new AssemblyModuleId(id);
            Manifest = new AssemblyModuleManifest(
                Id,
                Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                Array.Empty<LocalCallContribution>(),
                Array.Empty<EventHandlerContribution>(),
                tools ?? Array.Empty<LayerToolContribution>());
        }

        public AssemblyModuleId Id { get; }

        public AssemblyModuleManifest Manifest { get; }
    }

    private sealed class IsolationLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IsolationService, IsolationService>();
        }
    }

    private sealed class IsolationService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class AlternateIsolationService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class IsolationTool : IDisposable
    {
        public static int Disposed;

        public void Dispose()
        {
            Interlocked.Increment(ref Disposed);
        }
    }

    private readonly struct IsolationScope : IScopeDefinition
    {
        public const int ScopeId = 16;
    }
}
