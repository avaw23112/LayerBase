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
    public void Layer_runtime_does_not_store_actor_world_or_main_actor_runtime()
    {
        Type[] runtimeFieldTypes = typeof(LayerRuntime)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(static field => field.FieldType)
            .ToArray();

        Assert.That(runtimeFieldTypes, Does.Not.Contain(typeof(ActorWorld)));
        Assert.That(runtimeFieldTypes, Does.Not.Contain(typeof(MainActorRuntime)));
    }

    [Test]
    public void Main_scope_owns_local_actor_runtime()
    {
        using var runtime = new LayerRuntime(9401);

        ScopeRuntime mainScope = runtime.ScopeHost.MainScope;

        Assert.That(mainScope.MainActors, Is.Not.Null);
        Assert.That(runtime.MainActorRuntime, Is.SameAs(mainScope.MainActors));
        Assert.That(mainScope.Actors.IsLocal, Is.True);
        Assert.DoesNotThrow(() => _ = mainScope.Actors.Local);
    }

    [Test]
    public void Custom_scope_uses_remote_actor_accessor_to_main_scope()
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

        Assert.That(customScope.MainActors, Is.Null);
        Assert.That(customScope.Actors.IsLocal, Is.False);
        Assert.DoesNotThrow(() => _ = customScope.Actors.Remote);
        Assert.Throws<InvalidOperationException>(() => customScope.Actors.Local.ToString());
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
            Assert.That(runtime.ScopeHost.MainScope.MainActors, Is.SameAs(runtime.MainActorRuntime));
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
