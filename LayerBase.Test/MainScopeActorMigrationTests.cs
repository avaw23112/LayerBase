using System.Reflection;
using LayerBase.Actor;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class MainScopeActorMigrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Layer_runtime_owns_main_actor_runtime_not_actor_world_directly()
    {
        Type[] runtimeFieldTypes = typeof(LayerRuntime)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(static field => field.FieldType)
            .ToArray();

        Assert.That(runtimeFieldTypes, Does.Not.Contain(typeof(ActorWorld)));
        Assert.That(runtimeFieldTypes.Count(static type => type == typeof(MainActorRuntime)), Is.EqualTo(1));
    }

    [Test]
    public void Main_scope_exposes_actor_client_and_factory_without_owning_runtime()
    {
        using var runtime = new LayerRuntime(9401);

        ScopeRuntime mainScope = runtime.ScopeHost.MainScope;

        Assert.DoesNotThrow(() => _ = mainScope.ActorClient);
        Assert.DoesNotThrow(() => _ = mainScope.ActorFactory);
    }

    [Test]
    public void Custom_scope_exposes_actor_client_without_factory()
    {
        using var runtime = new LayerRuntime(9402);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(ActorWorkerScope.ScopeId, nameof(ActorWorkerScope), typeof(ActorWorkerScope)),
                    ScopeOptions.Inline)
            },
            runtime.Id,
            runtime.Generation);

        ScopeRuntime customScope = host.Scopes.Single(static scope => scope.ScopeId == ActorWorkerScope.ScopeId);

        Assert.DoesNotThrow(() => _ = customScope.ActorClient);
        Assert.Throws<InvalidOperationException>(() => _ = customScope.ActorFactory);
    }

    [Test]
    public void Main_scope_dispose_releases_actor_world()
    {
        var runtime = new LayerRuntime(9403);
        MainActorRuntime actorRuntime = runtime.MainActorRuntime;

        runtime.Dispose();

        Assert.Throws<ObjectDisposedException>(() => actorRuntime.PrepareRuntimeBuild());
    }

    [Test]
    public void Runtime_build_installs_composition_scope_host()
    {
        var runtime = new LayerRuntime(9404);
        try
        {
            var layer = new ActorWorkerLayer();
            var builder = new LayerRuntime.LayersBuilder(runtime);

            runtime = builder
                .Push(layer)
                .AddAssemblyModule(new ActorWorkerModule())
                .Build();

            Assert.That(runtime.TryGetScope<ActorWorkerScope>(out var scope), Is.True);
            Assert.That(scope.Address.ScopeId, Is.EqualTo(ActorWorkerScope.ScopeId));
            Assert.That(runtime.ScopeHost.Scopes.Any(static item => item.ScopeId == ActorWorkerScope.ScopeId), Is.True);
            Assert.DoesNotThrow(() => _ = runtime.ScopeHost.MainScope.ActorClient);
            Assert.DoesNotThrow(() => _ = runtime.ScopeHost.MainScope.ActorFactory);
        }
        finally
        {
            runtime.Dispose();
        }
    }

    public readonly struct ActorWorkerScope : IScopeDefinition
    {
        public const int ScopeId = 21;
    }

    private sealed class ActorWorkerLayer : Layer
    {
    }

    private sealed class ActorWorkerService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class ActorWorkerModule : IAssemblyModule
    {
        public AssemblyModuleId Id { get; } = new("actor-worker");

        public AssemblyModuleManifest Manifest { get; } =
            new(
                new AssemblyModuleId("actor-worker"),
                ServiceContribution.ForTypes(
                    typeof(ActorWorkerService),
                    typeof(ActorWorkerService),
                    typeof(ActorWorkerLayer),
                    typeof(ActorWorkerScope)));
    }
}
