using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.DI;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Layers;
using LayerBase.Scope;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public class ScopeEcsMigrationTests
{
    [Test]
    public void Each_scope_has_independent_world_and_scheduler()
    {
        LayerHub.Reset();
        var runtime = new LayerRuntime(2301);
        var plans = CreateTwoScopePlans(230);

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);

        ScopeRuntime mainScope = host.MainScope;
        ScopeRuntime customScope = host.Scopes[1];

        Assert.That(mainScope.EcsWorld, Is.Not.SameAs(customScope.EcsWorld));
        Assert.That(mainScope.EcsScheduler, Is.Not.SameAs(customScope.EcsScheduler));
        Assert.That(mainScope.EcsScheduler.World, Is.SameAs(mainScope.EcsWorld));
        Assert.That(customScope.EcsScheduler.World, Is.SameAs(customScope.EcsWorld));
        Assert.That(mainScope.EcsWorld.EcsScheduler, Is.SameAs(mainScope.EcsScheduler));
        Assert.That(customScope.EcsWorld.EcsScheduler, Is.SameAs(customScope.EcsScheduler));
    }

    [Test]
    public void Runtime_ecs_world_is_main_scope_facade()
    {
        LayerHub.Reset();
        var runtime = new LayerRuntime(2302);
        var plans = CreateTwoScopePlans(231);

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);

        Assert.That(runtime.EcsWorld, Is.SameAs(runtime.ScopeHost.MainScope.EcsWorld));
        Assert.That(runtime.EcsWorld, Is.Not.SameAs(host.Scopes[1].EcsWorld));
    }

    [Test]
    public void Service_query_uses_owner_scope_world()
    {
        LayerHub.Reset();
        var runtime = new LayerRuntime(2303);
        var plans = CreateTwoScopePlans(232);

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        var service = new ScopeEcsProbeService();
        AttachScopeRuntime(service, runtime, host.Scopes[1]);

        Assert.That(service.ECSWorld(), Is.SameAs(host.Scopes[1].EcsWorld));
        Assert.That(service.ECSWorld(), Is.Not.SameAs(runtime.EcsWorld));
    }

    [Test]
    public void Context_query_uses_owner_service_scope_world()
    {
        LayerHub.Reset();
        var runtime = new LayerRuntime(2304);
        var plans = CreateTwoScopePlans(233);

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        var context = new ScopeEcsProbeContext();
        AttachScopeRuntime(context, runtime, host.Scopes[1]);

        Assert.That(context.ECSWorld(), Is.SameAs(host.Scopes[1].EcsWorld));
        Assert.That(context.ECSWorld(), Is.Not.SameAs(runtime.EcsWorld));
    }

    [Test]
    public void Push_layer_query_uses_main_scope_world()
    {
        LayerHub.Reset();
        var runtime = new LayerRuntime(2305);
        var layer = new ScopeEcsProbeLayer();

        layer.AttachToContext(runtime);

        Assert.That(layer.ECSWorld(), Is.SameAs(runtime.EcsWorld));
    }

    [Test]
    public void Service_blueprint_uses_owner_scope_world()
    {
        LayerHub.Reset();
        var runtime = new LayerRuntime(2306);
        var plans = CreateTwoScopePlans(234);

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        var service = new ScopeEcsProbeService();
        AttachScopeRuntime(service, runtime, host.Scopes[1]);

        Entity entity = service.CreateEntity()
                               .WithComponent<ScopeEcsProbeComponent>()
                               .Build();

        Assert.That(host.Scopes[1].EcsWorld.IsAlive(entity), Is.True);
        Assert.That(runtime.EcsWorld.IsAlive(entity), Is.False);
    }

    [Test]
    public void Scope_ecs_scheduler_owns_batch_options_and_command_buffer_without_nested_worker()
    {
        Type schedulerType = typeof(ScopeEcsScheduler);

        Assert.That(schedulerType.GetProperty(nameof(ScopeEcsScheduler.BatchOptions)), Is.Not.Null);
        Assert.That(schedulerType.GetProperty(nameof(ScopeEcsScheduler.CommandBuffer)), Is.Not.Null);
        Assert.That(schedulerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                 .Where(static field => field.FieldType == typeof(CommandBuffer))
                                 .Select(static field => field.Name),
            Is.Not.Empty);

        string[] forbiddenTypeNames =
        {
            "EcsWorker",
            "EcsWorkQueue",
            "EcsSubmissionBatchPool",
            "EcsResultQueue"
        };

        Type[] assemblyTypes = typeof(LayerRuntime).Assembly.GetTypes();
        foreach (string forbiddenTypeName in forbiddenTypeNames)
        {
            Assert.That(
                assemblyTypes.Any(type => type.Name == forbiddenTypeName),
                Is.False,
                $"{forbiddenTypeName} must not be introduced by Scope-local ECS scheduling.");
        }
    }

    [Test]
    public void Query_batch_options_default_to_disabled_faster_capacity_shape()
    {
        EcsQueryBatchOptions options = EcsQueryBatchOptions.Default;

        Assert.That(options.EnableImplicitBatching, Is.False);
        Assert.That(options.DefaultBatchLimitBytes, Is.EqualTo(512 * 1024));
        Assert.That(options.MinBatchEntityCount, Is.EqualTo(256));
        Assert.That(options.MaxBatchEntityCount, Is.EqualTo(32768));
        Assert.That(options.ResolveBatchEntityCount(accessBytesPerEntity: 16), Is.EqualTo(32768));
        Assert.That(options with { EnableImplicitBatching = true }, Is.EqualTo(new EcsQueryBatchOptions(true, 512 * 1024, 256, 32768)));
    }

    [Test]
    public void Projection_batch_buffer_flushes_at_batch_boundary_and_reuses_buffer()
    {
        var sink = new RecordingProjectedActorSink();
        ProjectionBatchBuffer<ScopeEcsProjectionEvent> buffer =
            ProjectionBatchBuffer<ScopeEcsProjectionEvent>.Rent(initialCapacity: 1);

        buffer.Add(new ActorId(1, 1, 1), new ScopeEcsProjectionEvent(10));
        buffer.Add(new ActorId(1, 2, 1), new ScopeEcsProjectionEvent(20));
        buffer.FlushTo(sink);

        Assert.That(sink.Values, Is.EqualTo(new[] { 10, 20 }));
        Assert.That(buffer.Count, Is.EqualTo(0));

        buffer.Add(new ActorId(1, 3, 1), new ScopeEcsProjectionEvent(30));
        buffer.FlushTo(sink);
        buffer.Dispose();

        Assert.That(sink.Values, Is.EqualTo(new[] { 10, 20, 30 }));
    }

    [Test]
    public void Blueprint_cache_contains_no_world_or_scope_runtime()
    {
        Type cacheType = typeof(EntityBlueprintCache<ScopeEcsBlueprint>);
        FieldInfo[] fields = cacheType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.That(
            fields.Where(static field =>
                    field.FieldType == typeof(World) ||
                    field.FieldType == typeof(ScopeRuntime) ||
                    field.FieldType == typeof(Entity) ||
                    typeof(LayerBase.Actor.IPooledActor).IsAssignableFrom(field.FieldType))
                .Select(static field => field.Name),
            Is.Empty);
    }

    private static ScopeExecutionPlan[] CreateTwoScopePlans(int scopeId)
    {
        return new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(scopeId, nameof(ScopeEcsCustomScope), typeof(ScopeEcsCustomScope)),
                ScopeOptions.Inline)
        };
    }

    private static void AttachScopeRuntime(object target, LayerRuntime runtime, ScopeRuntime scope)
    {
        MethodInfo? method = typeof(ServiceLayerBinder).GetMethod(
            "AttachScopeRuntime",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(object), typeof(LayerRuntime), typeof(ScopeRuntime) },
            modifiers: null);

        Assert.That(method, Is.Not.Null, "ServiceLayerBinder must expose an internal OwnerScope binding path.");
        method!.Invoke(null, new object[] { target, runtime, scope });
    }
}

internal readonly struct ScopeEcsCustomScope : IScopeDefinition
{
}

internal sealed class ScopeEcsProbeService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

internal sealed class ScopeEcsProbeContext : IInternalLayerContext
{
    public int LayerIndex { get; set; } = -1;
}

internal sealed class ScopeEcsProbeLayer : Layer
{
}

internal struct ScopeEcsProbeComponent : IComponent
{
    public int Value;
}

internal readonly struct ScopeEcsProjectionEvent
{
    public ScopeEcsProjectionEvent(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

internal sealed class RecordingProjectedActorSink : IProjectedActorCommandSink
{
    private readonly List<int> _values = new();

    public IReadOnlyList<int> Values => _values;

    public ProjectedActorEnsureResult Ensure(Entity entity, int actorTypeId, long nowTicks)
    {
        return ProjectedActorEnsureResult.Invalid;
    }

    public bool Exists(ActorId actorId) => actorId.IsValid;

    public bool IsDisabled(ActorId actorId) => false;

    public bool EnableIfDisabled(ActorId actorId) => false;

    public bool Disable(ActorId actorId) => false;

    public bool Release(ActorId actorId, ProjectedActorReleasePolicy releasePolicy) => true;

    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        if (value is ScopeEcsProjectionEvent evt)
            _values.Add(evt.Value);
    }
}

internal sealed class ScopeEcsBlueprint : IEntityBlueprint
{
    public void Config(ref EntityBlueprintBuilder builder)
    {
        builder.WithComponent<ScopeEcsProbeComponent>();
    }
}
