using System.Reflection;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Scope;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public class ProjectionScopeMigrationTests
{
    [Test]
    public void Projection_binding_surface_does_not_accept_actor_world()
    {
        Type[] projectionTypes =
        {
            typeof(ProjectedActorBinding),
            typeof(ActiveProjectedActorList)
        };

        foreach (Type type in projectionTypes)
        {
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.Instance);

            foreach (MethodInfo method in methods)
            {
                bool acceptsActorWorld = method.GetParameters()
                                               .Any(static parameter => parameter.ParameterType == typeof(ActorWorld));

                Assert.That(
                    acceptsActorWorld,
                    Is.False,
                    $"{type.FullName}.{method.Name} must route projection lifecycle through IProjectedActorCommandSink.");
            }
        }
    }

    [Test]
    public void World_projection_resource_owner_is_command_sink_not_actor_world()
    {
        Type worldType = typeof(World);

        Assert.That(
            worldType.GetField("_projectedActorCommandSink", BindingFlags.NonPublic | BindingFlags.Instance),
            Is.Not.Null,
            "World should own only a projected actor command sink for projection lifecycle operations.");

        FieldInfo[] worldFields = worldType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(
            worldFields.Where(static field => field.FieldType == typeof(ActorWorld)).Select(static field => field.Name),
            Is.Empty,
            "ECS World must not keep ActorWorld as a resource.");
    }

    [Test]
    public void Custom_scope_ensure_round_trip_binds_actor_id_through_scope_event()
    {
        LayerHub.Reset();
        var runtime = new LayerRuntime(2201);
        var plans = new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(220, nameof(ProjectionCustomScope), typeof(ProjectionCustomScope)),
                ScopeOptions.Inline)
        };

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        host.MainScope.MainActors!.PrepareRuntimeBuild();
        RegisterProjectionProbe(actorTypeId: 220);
        host.MainScope.MainActors.CompleteRuntimeBuild();

        ScopeRuntime customScope = host.Scopes[1];
        Entity entity = customScope.EcsWorld.Create(new ProjectedActorRef());
        customScope.EcsWorld.WithProjectedActor(
            entity,
            actorTypeId: 220,
            keepAliveOverrideTicks: ProjectedActorTime.SecondsToTicks(0.5f),
            releasePolicy: ProjectedActorReleasePolicy.ReturnToPool);

        customScope.EcsWorld.Query().TouchProjectedActor();

        Assert.That(customScope.EcsWorld.GetProjectionMeta(entity).ActorId.IsValid, Is.False);

        host.MainScope.PumpIngress();
        PumpMainActors(host.MainScope);
        customScope.PumpIngress();

        ActorId actorId = customScope.EcsWorld.GetProjectionMeta(entity).ActorId;
        Assert.That(actorId.IsValid, Is.True);
        Assert.That(host.MainScope.MainActors!.World.IsAlive(actorId), Is.True);
    }

    [Test]
    public void Custom_scope_projection_post_routes_mail_to_main_scope_actor()
    {
        LayerHub.Reset();
        ProjectionProbeActor.Received.Clear();

        var runtime = new LayerRuntime(2202);
        var plans = new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(221, nameof(ProjectionCustomScope), typeof(ProjectionCustomScope)),
                ScopeOptions.Inline)
        };

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        host.MainScope.MainActors!.PrepareRuntimeBuild();
        RegisterProjectionProbe(actorTypeId: 221);
        host.MainScope.MainActors.CompleteRuntimeBuild();

        ScopeRuntime customScope = host.Scopes[1];
        Entity entity = customScope.EcsWorld.Create(
            new ProjectionPositionComponent { X = 1f, Y = 2f },
            new ProjectionVelocityComponent { X = 3f, Y = 4f },
            new ProjectedActorRef());
        customScope.EcsWorld.WithProjectedActor(
            entity,
            actorTypeId: 221,
            keepAliveOverrideTicks: ProjectedActorTime.SecondsToTicks(0.5f),
            releasePolicy: ProjectedActorReleasePolicy.ReturnToPool);

        customScope.EcsWorld.Query().TouchProjectedActor();
        host.MainScope.PumpIngress();
        PumpMainActors(host.MainScope);
        customScope.PumpIngress();

        customScope.EcsWorld
                   .Query<ProjectionPositionComponent, ProjectionVelocityComponent>()
                   .Bring<ProjectionMoveViewEvent>()
                   .ForEach(static (
                       in Entity _,
                       ref ProjectionPositionComponent position,
                       ref ProjectionVelocityComponent velocity,
                       ref ProjectionMoveViewEvent output) =>
                   {
                       position.X += velocity.X;
                       position.Y += velocity.Y;
                       output = new ProjectionMoveViewEvent(position.X, position.Y);
                   })
                   .Batch()
                   .Post();

        var budget = new RuntimeFrameBudget(0, 0, 0);
        host.MainScope.PumpIngress();
        host.MainScope.MainActors!.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(ProjectionProbeActor.Received.Count, Is.EqualTo(1));
        Assert.That(ProjectionProbeActor.Received[0].X, Is.EqualTo(4f));
        Assert.That(ProjectionProbeActor.Received[0].Y, Is.EqualTo(6f));
    }

    [Test]
    public void Custom_scope_projection_batch_post_uses_single_scope_event_payload()
    {
        LayerHub.Reset();
        ProjectionProbeActor.Received.Clear();

        var runtime = new LayerRuntime(2203);
        var plans = new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(222, nameof(ProjectionCustomScope), typeof(ProjectionCustomScope)),
                ScopeOptions.Inline)
        };

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        host.MainScope.MainActors!.PrepareRuntimeBuild();
        RegisterProjectionProbe(actorTypeId: 222);
        host.MainScope.MainActors.CompleteRuntimeBuild();

        ScopeRuntime customScope = host.Scopes[1];
        Entity first = CreateProjectedEntity(customScope, 222, 1f, 2f, 3f, 4f);
        Entity second = CreateProjectedEntity(customScope, 222, 10f, 20f, 30f, 40f);

        customScope.EcsWorld.Query().TouchProjectedActor();
        host.MainScope.PumpIngress();
        PumpMainActors(host.MainScope);
        customScope.PumpIngress();

        customScope.EcsWorld
                   .Query<ProjectionPositionComponent, ProjectionVelocityComponent>()
                   .Bring<ProjectionMoveViewEvent>()
                   .ForEach(static (
                       in Entity _,
                       ref ProjectionPositionComponent position,
                       ref ProjectionVelocityComponent velocity,
                       ref ProjectionMoveViewEvent output) =>
                   {
                       position.X += velocity.X;
                       position.Y += velocity.Y;
                       output = new ProjectionMoveViewEvent(position.X, position.Y);
                   })
                   .Batch()
                   .Post();

        Assert.That(host.MainScope.Transport.EventInbox.TryDequeue(out ScopeEventEnvelope envelope), Is.True);
        Assert.That(envelope.RouteId, Is.EqualTo(EventTypeId<ActorPostBatchScopeEvent<ProjectionMoveViewEvent>>.Id));
        Assert.That(
            host.MainScope.Transport.EventPayloadStorage.TryGet<ActorPostBatchScopeEvent<ProjectionMoveViewEvent>>(
                runtime.Id,
                envelope.Payload,
                out var batch),
            Is.True);
        Assert.That(batch.Count, Is.EqualTo(2));
        Assert.That(host.MainScope.Transport.EventInbox.TryDequeue(out _), Is.False);

        try
        {
            Assert.That(
                host.MainScope.MainActors!.TryDispatchProjectionCommand(
                    envelope.RouteId,
                    host.MainScope,
                    runtime.Id,
                    envelope.Payload,
                    host.MainScope.Transport.EventPayloadStorage),
                Is.True);
        }
        finally
        {
            host.MainScope.Transport.EventPayloadStorage.Release(envelope.Payload);
        }

        var budget = new RuntimeFrameBudget(0, 0, 0);
        host.MainScope.MainActors!.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(ProjectionProbeActor.Received.Count, Is.EqualTo(2));
        Assert.That(customScope.EcsWorld.GetProjectionMeta(first).ActorId.IsValid, Is.True);
        Assert.That(customScope.EcsWorld.GetProjectionMeta(second).ActorId.IsValid, Is.True);
    }

    [Test]
    public void Custom_scope_release_waits_for_result_before_clearing_binding()
    {
        LayerHub.Reset();

        var runtime = new LayerRuntime(2204);
        var plans = new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(223, nameof(ProjectionCustomScope), typeof(ProjectionCustomScope)),
                ScopeOptions.Inline)
        };

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        host.MainScope.MainActors!.PrepareRuntimeBuild();
        RegisterProjectionProbe(actorTypeId: 223);
        host.MainScope.MainActors.CompleteRuntimeBuild();

        ScopeRuntime customScope = host.Scopes[1];
        Entity entity = CreateProjectedEntity(customScope, 223, 1f, 2f, 3f, 4f);

        customScope.EcsWorld.Query().TouchProjectedActor();
        host.MainScope.PumpIngress();
        PumpMainActors(host.MainScope);
        customScope.PumpIngress();

        ActorId actorId = customScope.EcsWorld.GetProjectionMeta(entity).ActorId;
        Assert.That(actorId.IsValid, Is.True);

        ref ProjectedActorRef actorRef = ref customScope.EcsWorld.Get<ProjectedActorRef>(entity);
        actorRef.ExpireAtTicks = 0;
        customScope.EcsWorld.SweepProjectedActors();

        ref ProjectedActorMeta pendingMeta = ref customScope.EcsWorld.GetProjectionMeta(entity);
        Assert.That(pendingMeta.State, Is.EqualTo(ProjectedActorState.ReleasePending));
        Assert.That(pendingMeta.ActorId, Is.EqualTo(actorId));
        Assert.That(customScope.EcsWorld.Get<ProjectedActorRef>(entity).ActorId, Is.EqualTo(actorId));

        host.MainScope.PumpIngress();
        PumpMainActors(host.MainScope);
        customScope.PumpIngress();

        ref ProjectedActorMeta releasedMeta = ref customScope.EcsWorld.GetProjectionMeta(entity);
        Assert.That(releasedMeta.ActorId.IsValid, Is.False);
        Assert.That(customScope.EcsWorld.Get<ProjectedActorRef>(entity).ActorId.IsValid, Is.False);
    }

    [Test]
    public void Projection_command_respects_actor_world_frame_budget_used_by_post_scheduler()
    {
        LayerHub.Reset();

        var runtime = new LayerRuntime(2205);
        var plans = new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(224, nameof(ProjectionCustomScope), typeof(ProjectionCustomScope)),
                ScopeOptions.Inline)
        };

        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(runtime, plans, runtime.Id, generation: 1);
        host.MainScope.MainActors!.PrepareRuntimeBuild();
        RegisterProjectionProbe(actorTypeId: 224);
        host.MainScope.MainActors.CompleteRuntimeBuild();

        ScopeRuntime customScope = host.Scopes[1];
        Entity entity = customScope.EcsWorld.Create(new ProjectedActorRef());
        customScope.EcsWorld.WithProjectedActor(
            entity,
            actorTypeId: 224,
            keepAliveOverrideTicks: ProjectedActorTime.SecondsToTicks(0.5f),
            releasePolicy: ProjectedActorReleasePolicy.ReturnToPool);

        customScope.EcsWorld.Query().TouchProjectedActor();
        host.MainScope.PumpIngress();

        var exhaustedBudget = new RuntimeFrameBudget(maxEvents: 1, usedEvents: 1, deadlineTicks: 0);
        host.MainScope.MainActors!.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref exhaustedBudget);
        customScope.PumpIngress();

        Assert.That(customScope.EcsWorld.GetProjectionMeta(entity).ActorId.IsValid, Is.False);

        var availableBudget = new RuntimeFrameBudget(maxEvents: 1, usedEvents: 0, deadlineTicks: 0);
        host.MainScope.MainActors!.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref availableBudget);
        customScope.PumpIngress();

        Assert.That(customScope.EcsWorld.GetProjectionMeta(entity).ActorId.IsValid, Is.True);
        Assert.That(availableBudget.UsedEvents, Is.EqualTo(1));
    }

    private static void RegisterProjectionProbe(int actorTypeId)
    {
        ProjectedActorTypeRegistry.RegisterGenerated(
            actorTypeId,
            typeof(ProjectionProbeActor),
            static actorWorld => actorWorld.CreateProjectedActor<ProjectionProbeActor>());
    }

    private static void PumpMainActors(ScopeRuntime mainScope)
    {
        var budget = new RuntimeFrameBudget(0, 0, 0);
        mainScope.MainActors!.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref budget);
    }

    private static Entity CreateProjectedEntity(
        ScopeRuntime scope,
        int actorTypeId,
        float x,
        float y,
        float vx,
        float vy)
    {
        Entity entity = scope.EcsWorld.Create(
            new ProjectionPositionComponent { X = x, Y = y },
            new ProjectionVelocityComponent { X = vx, Y = vy },
            new ProjectedActorRef());
        scope.EcsWorld.WithProjectedActor(
            entity,
            actorTypeId,
            keepAliveOverrideTicks: ProjectedActorTime.SecondsToTicks(0.5f),
            releasePolicy: ProjectedActorReleasePolicy.ReturnToPool);
        return entity;
    }
}

internal readonly struct ProjectionCustomScope : IScopeDefinition
{
}
