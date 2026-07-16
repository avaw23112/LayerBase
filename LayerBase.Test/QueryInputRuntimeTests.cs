using System.Reflection;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class QueryInputRuntimeTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        QueryInputRuntimeActor.Received.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
        QueryInputRuntimeActor.Received.Clear();
    }

    [Test]
    public void Input_value_is_shared_for_all_entities()
    {
        QueryInputRuntimeLayer layer = new();
        LayerRuntime runtime = LayerHub.CreateLayers()
                                       .Push(layer)
                                       .Build();

        Entity first = CreateEntity(runtime.EcsWorld, position: 0f, scale: 2f);
        Entity second = CreateEntity(runtime.EcsWorld, position: 10f, scale: 3f);

        var frame = new QueryInputFrame(5f);
        layer.Service.Apply(in frame, new QueryInputConfig(1f));

        Assert.That(runtime.EcsWorld.Get<QueryInputPosition>(first).Value, Is.EqualTo(11f));
        Assert.That(runtime.EcsWorld.Get<QueryInputPosition>(second).Value, Is.EqualTo(26f));
    }

    [Test]
    public void Different_invocations_use_different_input()
    {
        QueryInputRuntimeLayer layer = new();
        LayerRuntime runtime = LayerHub.CreateLayers()
                                       .Push(layer)
                                       .Build();

        Entity entity = CreateEntity(runtime.EcsWorld, position: 0f, scale: 4f);

        var firstFrame = new QueryInputFrame(2f);
        var secondFrame = new QueryInputFrame(3f);
        layer.Service.Apply(in firstFrame, new QueryInputConfig(0.5f));
        layer.Service.Apply(in secondFrame, new QueryInputConfig(1.5f));

        Assert.That(runtime.EcsWorld.Get<QueryInputPosition>(entity).Value, Is.EqualTo(22f));
    }

    [Test]
    public void Bring_query_can_use_input()
    {
        QueryInputRuntimeLayer layer = new();
        LayerRuntime runtime = LayerHub.CreateLayers()
                                       .Push(layer)
                                       .Build();

        Entity entity = CreateEntity(runtime.EcsWorld, position: 7f, scale: 2f);
        runtime.EcsWorld.WithProjectedActor<QueryInputRuntimeActor>(entity);
        ref ProjectedActorMeta meta = ref runtime.EcsWorld.GetProjectionMeta(entity);
        ProjectedActorBinding.EnsureProjectedActor(runtime.EcsWorld, entity, ref meta, nowTicks: 0);
        PumpActors(runtime);

        var frame = new QueryInputFrame(4f);
        layer.Service.Project(in frame);
        PumpActors(runtime);

        Assert.That(runtime.EcsWorld.Get<QueryInputPosition>(entity).Value, Is.EqualTo(15f));
        Assert.That(QueryInputRuntimeActor.Received.Select(static evt => evt.Value), Is.EqualTo(new[] { 15f }));
    }

    [Test]
    public void Worker_scope_query_uses_owner_scope_world_with_input()
    {
        LayerHub.Reset();
        var runtime = new LayerRuntime(2411);
        ScopeExecutionPlan[] plans =
        [
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(241, nameof(QueryInputRuntimeScope), typeof(QueryInputRuntimeScope)),
                ScopeOptions.Inline)
        ];

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        ScopeRuntime customScope = host.Scopes[1];
        var service = new QueryInputRuntimeService();
        AttachScopeRuntime(service, runtime, customScope);

        Entity entity = CreateEntity(customScope.EcsWorld, position: 1f, scale: 6f);
        var frame = new QueryInputFrame(2f);

        service.Apply(in frame, new QueryInputConfig(3f));

        Assert.That(customScope.EcsWorld.Get<QueryInputPosition>(entity).Value, Is.EqualTo(16f));
        Assert.That(runtime.EcsWorld.IsAlive(entity), Is.False);
    }

    private static Entity CreateEntity(World world, float position, float scale)
    {
        Entity entity = world.CreateEntity()
                             .WithComponent<QueryInputPosition>()
                             .WithComponent<QueryInputScale>()
                             .Build();

        entity.Set(new QueryInputPosition(position), new QueryInputScale(scale));
        return entity;
    }

    private static void AttachScopeRuntime(object target, LayerRuntime runtime, ScopeRuntime scope)
    {
        MethodInfo? method = typeof(ServiceLayerBinder).GetMethod(
            "AttachScopeRuntime",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(object), typeof(LayerRuntime), typeof(ScopeRuntime)],
            modifiers: null);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(null, [target, runtime, scope]);
    }

    private static void PumpActors(LayerRuntime runtime)
    {
        var budget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
        runtime.MainActorRuntime.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref budget);
    }
}

internal readonly struct QueryInputRuntimeScope : IScopeDefinition
{
}

internal sealed partial class QueryInputRuntimeLayer : Layer
{
    [Mount]
    internal QueryInputRuntimeService Service = null!;
}

internal sealed partial class QueryInputRuntimeService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Query]
    private void OnApply(
        ref QueryInputPosition position,
        in QueryInputScale scale,
        [Input] in QueryInputFrame frame,
        [Input] QueryInputConfig config)
    {
        position.Value += (scale.Value * frame.Delta) + config.Bias;
    }

    [Query]
    [Bring<QueryInputMoveEvent>]
    private ProjectResult OnProject(
        ref QueryInputPosition position,
        in QueryInputScale scale,
        [Input] in QueryInputFrame frame,
        ref QueryInputMoveEvent moveEvent)
    {
        position.Value += scale.Value * frame.Delta;
        moveEvent = new QueryInputMoveEvent(position.Value);
        return ProjectResult.Success;
    }
}

internal sealed partial class QueryInputRuntimeActor : IPooledActor
{
    public static List<QueryInputMoveEvent> Received { get; } = new();

    [ActorBehaviour]
    private void OnMove(in QueryInputMoveEvent value)
    {
        Received.Add(value);
    }

    public void OnRent()
    {
    }

    public void OnReturn()
    {
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}

internal struct QueryInputPosition : IComponent
{
    public QueryInputPosition(float value)
    {
        Value = value;
    }

    public float Value;
}

internal readonly struct QueryInputScale : IComponent
{
    public QueryInputScale(float value)
    {
        Value = value;
    }

    public float Value { get; }
}

internal readonly struct QueryInputFrame
{
    public QueryInputFrame(float delta)
    {
        Delta = delta;
    }

    public float Delta { get; }
}

internal sealed class QueryInputConfig
{
    public QueryInputConfig(float bias)
    {
        Bias = bias;
    }

    public float Bias { get; }
}

internal readonly struct QueryInputMoveEvent : IActorEvent
{
    public QueryInputMoveEvent(float value)
    {
        Value = value;
    }

    public float Value { get; }
}
