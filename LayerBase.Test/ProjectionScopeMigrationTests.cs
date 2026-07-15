using System.Reflection;
using Arch.Core;
using LayerBase.Actor;
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
        runtime.MainActorRuntime.PrepareRuntimeBuild();
        RegisterProjectionProbe(actorTypeId: 220);
        runtime.MainActorRuntime.CompleteRuntimeBuild();

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
        customScope.PumpIngress();

        ActorId actorId = customScope.EcsWorld.GetProjectionMeta(entity).ActorId;
        Assert.That(actorId.IsValid, Is.True);
        Assert.That(runtime.Actors.IsAlive(actorId), Is.True);
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
        runtime.MainActorRuntime.PrepareRuntimeBuild();
        RegisterProjectionProbe(actorTypeId: 221);
        runtime.MainActorRuntime.CompleteRuntimeBuild();

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
        runtime.MainActorRuntime.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 1f / 60f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(ProjectionProbeActor.Received.Count, Is.EqualTo(1));
        Assert.That(ProjectionProbeActor.Received[0].X, Is.EqualTo(4f));
        Assert.That(ProjectionProbeActor.Received[0].Y, Is.EqualTo(6f));
    }

    private static void RegisterProjectionProbe(int actorTypeId)
    {
        ProjectedActorTypeRegistry.RegisterGenerated(
            actorTypeId,
            typeof(ProjectionProbeActor),
            static actorWorld => actorWorld.CreateProjectedActor<ProjectionProbeActor>());
    }
}

internal readonly struct ProjectionCustomScope : IScopeDefinition
{
}
