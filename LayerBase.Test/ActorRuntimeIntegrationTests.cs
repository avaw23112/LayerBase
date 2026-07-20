using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;
using System.Reflection;
using ServiceUpdate = LayerBase.DI.Options.IUpdate;

namespace LayerBase.Test;

public struct RuntimeSchedulerEvent
{
    public int Value;

    public RuntimeSchedulerEvent(int value)
    {
        Value = value;
    }
}

public struct RuntimeActorEvent
{
    public int Value;

    public RuntimeActorEvent(int value)
    {
        Value = value;
    }
}

internal sealed class CustomActorScope : IScopeDefinition
{

public ScopeOptions Options => ScopeOptions.Inline;
}

internal static class ActorRuntimeIntegrationTrace
{
    public static List<string> Entries { get; } = new();
}

internal sealed class UpdateOrderingLayer : Layer
{
}

internal sealed class BudgetLayer : Layer
{
}

internal sealed partial class UpdateOrderingService : IService, ServiceUpdate
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    public void OnScheduler(in RuntimeSchedulerEvent value)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"scheduler:{value.Value}");
    }

    public void Update()
    {
        ActorRuntimeIntegrationTrace.Entries.Add("update");
    }
}

internal sealed partial class BudgetService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    public void OnScheduler(in RuntimeSchedulerEvent value)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"scheduler:{value.Value}");
    }
}

internal sealed partial class IntegrationActor : IActor
{
    [ActorBehaviour]
    private void OnActor(in RuntimeActorEvent value)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"actor:{value.Value}");
    }
}

internal sealed partial class RuntimeLifecycleActor : IActor, LayerBase.Actor.IStart, LayerBase.Actor.IFixedUpdate,
                                                      LayerBase.Actor.IUpdate, LayerBase.Actor.ILateUpdate
{
    [ActorBehaviour]
    private void OnActor(in RuntimeActorEvent value)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"actor:{value.Value}");
    }

    public void Start()
    {
        ActorRuntimeIntegrationTrace.Entries.Add("start");
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"fixed:{fixedDeltaTime:0.###}");
    }

    void LayerBase.Actor.IUpdate.Update(float deltaTime)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"actor-update:{deltaTime:0.###}");
    }

    public void LateUpdate(float deltaTime)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"late:{deltaTime:0.###}");
    }
}

[TestFixture]
public class ActorRuntimeIntegrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        ActorRuntimeIntegrationTrace.Entries.Clear();
    }

    [Test]
    public void Runtime_exposes_actor_world_and_pump_advances_actor_processing()
    {
        LayerRuntime runtime = BuildRuntime(new UpdateOrderingLayer(), new UpdateOrderingService(),
            PostSchedulerOptions.Default);

        Assert.That(runtime.Actors, Is.Not.Null);

        IntegrationActor actor = runtime.Actors.CreateActor<IntegrationActor>();
        actor.PostInside(new RuntimeActorEvent(7));

        runtime.Pump(0.016f);

        Assert.That(ActorRuntimeIntegrationTrace.Entries, Does.Contain("actor:7"));
    }

    [Test]
    public void Main_actor_runtime_owns_actor_world_outside_scope_runtime()
    {
        LayerRuntime runtime = BuildRuntime(new UpdateOrderingLayer(), new UpdateOrderingService(),
            PostSchedulerOptions.Default);

        Assert.That(runtime.Actors, Is.Not.Null);

        var scopeActorWorldMembers = typeof(ScopeRuntime)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static member =>
            {
                return member switch
                {
                    FieldInfo field => field.FieldType == typeof(ActorWorld),
                    PropertyInfo property => property.PropertyType == typeof(ActorWorld),
                    _ => false
                };
            })
            .Select(static member => member.Name)
            .ToArray();

        Assert.That(scopeActorWorldMembers, Is.Empty);
    }

    [Test]
    public void Layer_runtime_does_not_store_actor_world_as_runtime_unit()
    {
        var actorWorldFields = typeof(LayerRuntime)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static field => field.FieldType == typeof(ActorWorld))
            .Select(static field => field.Name)
            .ToArray();

        Assert.That(actorWorldFields, Is.Empty);
    }

    [Test]
    public void Runtime_dispose_disposes_main_scope_actor_world()
    {
        LayerRuntime runtime = BuildRuntime(new UpdateOrderingLayer(), new UpdateOrderingService(),
            PostSchedulerOptions.Default);
        ActorWorld actorWorld = runtime.Actors;

        runtime.Dispose();

        Assert.That(
            () => actorWorld.CreateActor<IntegrationActor>(),
            Throws.TypeOf<ObjectDisposedException>());
    }

    [Test]
    public void Scope_actor_capabilities_are_split_by_scope()
    {
        using var fixture = BuildMainAndCustomScopeHost();
        ScopeRuntimeHost host = fixture.Host;

        Assert.DoesNotThrow(() => _ = host.MainScope.ActorFactory);
        Assert.DoesNotThrow(() => _ = host.MainScope.ActorClient);
        Assert.DoesNotThrow(() => _ = host.Scopes[1].ActorClient);
        Assert.Throws<InvalidOperationException>(() => _ = host.Scopes[1].ActorFactory);
    }

    [Test]
    public void Custom_scope_actor_post_routes_to_main_scope_actor_world()
    {
        using var fixture = BuildMainAndCustomScopeHost();
        ScopeRuntimeHost host = fixture.Host;
        IntegrationActor actor = host.MainScope.ActorFactory.Create<IntegrationActor>();
        ActorHandle handle = ActorHandle.FromActorId(actor.GetActorId(), runtimeGeneration: 1);

        host.Scopes[1].ActorClient.Post(handle, new RuntimeActorEvent(42));

        var budget = new RuntimeFrameBudget(0, 0, 0);
        host.MainScope.PumpIngress();
        fixture.Runtime.MainActorRuntime.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "actor:42" }));
    }

    [Test]
    public void Custom_scope_actor_call_routes_to_main_scope_actor_world()
    {
        ActorCallRuntimeTrace.Reset();
        using var fixture = BuildMainAndCustomScopeHost();
        ScopeRuntimeHost host = fixture.Host;
        ActorCallRuntimeActor actor = host.MainScope.ActorFactory.Create<ActorCallRuntimeActor>();
        ActorHandle handle = ActorHandle.FromActorId(actor.GetActorId(), runtimeGeneration: 1);

        var task = host.Scopes[1].ActorClient.Call<ActorCallRuntimeRequest, ActorCallRuntimeResponse>(
            handle,
            new ActorCallRuntimeRequest(6));

        Assert.That(task.GetAwaiter().IsCompleted, Is.False);

        var budget = new RuntimeFrameBudget(0, 0, 0);
        host.MainScope.PumpIngress();
        fixture.Runtime.MainActorRuntime.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref budget);

        var awaiter = task.GetAwaiter();
        Assert.That(awaiter.IsCompleted, Is.True);
        Assert.That(awaiter.GetResult().Value, Is.EqualTo(12));
        Assert.That(ActorCallRuntimeTrace.Entries, Is.EqualTo(new[] { "ask:6" }));
    }

    [Test]
    public void Actor_world_runs_after_post_scheduler_and_after_layer_update()
    {
        LayerRuntime runtime = BuildRuntime(new UpdateOrderingLayer(), new UpdateOrderingService(),
            PostSchedulerOptions.Default);
        IntegrationActor actor = runtime.Actors.CreateActor<IntegrationActor>();

        runtime.Post(new RuntimeSchedulerEvent(1));
        actor.PostInside(new RuntimeActorEvent(2));

        runtime.Pump(0.016f);

        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "scheduler:1", "update", "actor:2" }));
    }

    [Test]
    public void Post_scheduler_budget_exhaustion_prevents_actor_pump_until_next_frame()
    {
        var options = new PostSchedulerOptions(
            readyCapacity: 16,
            nextCapacity: 16,
            maxEventsPerPump: 1,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew);

        LayerRuntime runtime = BuildRuntime(new BudgetLayer(), new BudgetService(), options);
        IntegrationActor actor = runtime.Actors.CreateActor<IntegrationActor>();

        runtime.Post(new RuntimeSchedulerEvent(9));
        actor.PostInside(new RuntimeActorEvent(3));

        runtime.Pump(0.016f);
        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "scheduler:9" }));

        runtime.Pump(0.016f);
        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "scheduler:9", "actor:3" }));
    }

    [Test]
    public void Actor_lifecycle_runs_after_behaviour_and_after_layer_update()
    {
        LayerRuntime runtime = BuildRuntime(new UpdateOrderingLayer(), new UpdateOrderingService(),
            PostSchedulerOptions.Default);
        RuntimeLifecycleActor actor = runtime.Actors.CreateActor<RuntimeLifecycleActor>();

        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "start" }));
        ActorRuntimeIntegrationTrace.Entries.Clear();

        runtime.Post(new RuntimeSchedulerEvent(1));
        actor.PostInside(new RuntimeActorEvent(2));

        runtime.Pump(0.016f);

        Assert.That(
            ActorRuntimeIntegrationTrace.Entries,
            Is.EqualTo(new[]
                { "scheduler:1", "update", "actor:2", "fixed:0.017", "actor-update:0.016", "late:0.016" }));
    }

    [Test]
    public void Actor_fixed_update_is_controlled_by_runtime_fixed_update_options()
    {
        var runtime = new LayerRuntime(1);
        var layer = new UpdateOrderingLayer();
        layer.RegisterService(new UpdateOrderingService());

        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(layer);
        builder.SetPostOptions(PostSchedulerOptions.Default);
        builder.SetFixedUpdateOptions(new FixedUpdateOptions(true, 0.02f, 4));
        runtime = builder.Build();

        runtime.Actors.CreateActor<RuntimeLifecycleActor>();
        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "start" }));
        ActorRuntimeIntegrationTrace.Entries.Clear();

        runtime.Pump(0.016f);

        Assert.That(ActorRuntimeIntegrationTrace.Entries, Does.Contain("fixed:0.02"));
    }

    private static LayerRuntime BuildRuntime(Layer layer, IService service, PostSchedulerOptions options)
    {
        var runtime = new LayerRuntime(1);
        layer.RegisterService(service);

        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(layer);
        builder.SetPostOptions(options);
        return builder.Build();
    }

    private static ScopeRuntimeHostFixture BuildMainAndCustomScopeHost()
    {
        var runtime = new LayerRuntime(1);
        var plans = new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(100, nameof(CustomActorScope), typeof(CustomActorScope)),
                ScopeOptions.Inline)
        };

        ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        StartAllScopes(host);
        runtime.MainActorRuntime.PrepareRuntimeBuild();
        runtime.MainActorRuntime.CompleteRuntimeBuild();
        return new ScopeRuntimeHostFixture(runtime, host);
    }

    private static void StartAllScopes(ScopeRuntimeHost host)
    {
        foreach (ScopeRuntime scope in host.Scopes)
            scope.RunRuntimeStartOnOwnerThread();
    }

    private sealed class ScopeRuntimeHostFixture : IDisposable
    {
        public ScopeRuntimeHostFixture(LayerRuntime runtime, ScopeRuntimeHost host)
        {
            Runtime = runtime;
            Host = host;
        }

        public LayerRuntime Runtime { get; }

        public ScopeRuntimeHost Host { get; }

        public void Dispose()
        {
            Host.Dispose();
            Runtime.Dispose();
        }
    }
}
