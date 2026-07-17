using System.Diagnostics;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Layers;
using NUnit.Framework;
using IUpdate = LayerBase.Actor.IUpdate;

namespace LayerBase.Test;

#region Test Components and Events

public struct JobPositionComponent : IComponent
{
    public float X;
    public float Y;
}

public struct JobVelocityComponent :IComponent
{
    public float X;
    public float Y;
}

public struct JobAoiComponent :IComponent
{
    public bool IsVisible;
}

public struct JobMoveViewEvent : IActorEvent
{
    public float X;
    public float Y;

    public JobMoveViewEvent(float x, float y)
    {
        X = x;
        Y = y;
    }
}

#endregion

#region Test Actor

internal sealed partial class JobProbeActor : IPooledActor
{
    public static List<JobMoveViewEvent> Received { get; } = new();
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }

    [ActorBehaviour]
    private void OnMove(in JobMoveViewEvent value)
    {
        Received.Add(value);
    }

    public void OnRent()
    {
        RentCount++;
    }

    public void OnReturn()
    {
        ReturnCount++;
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}

#endregion

[TestFixture]
public class QueryBringJobTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        JobProbeActor.Received.Clear();
        JobProbeActor.RentCount = 0;
        JobProbeActor.ReturnCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    #region Pure Query Job Tests

    [Test]
    public void QueryWithoutBring_ForEach_ExecutesAndMutatesComponent()
    {
        // ?
        // ?Query + Job ForEach ?
        // ?Actor Touch Actor Post ActorEvent?

        LayerRuntime runtime = CreateRuntime();

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f });

        var job = new UpdatePositionJob();
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent>()
               .ForEach(ref job);

        JobPositionComponent position = runtime.EcsWorld.Get<JobPositionComponent>(entity);

        Assert.That(position.X, Is.EqualTo(4f));
        Assert.That(position.Y, Is.EqualTo(6f));
    }

    private readonly struct UpdatePositionJob :
        IQueryJob<JobPositionComponent, JobVelocityComponent>
    {
        public void Execute(
            Entity                   entity,
            ref JobPositionComponent position,
            ref JobVelocityComponent velocity)
        {
            position.X += velocity.X;
            position.Y += velocity.Y;
        }
    }

    #endregion

    #region Query + Bring Job Tests (Success)

    [Test]
    public void QueryWithBring_Success_MutatesEcsTouchesActorAndPostsEvent()
    {
        // ?
        //  Query + Bring + Job ?ProjectResult.Success 
        // 1.  ECS 
        // 2. Touch/Ensure Actor
        // 3. Add Event ?Batch
        // 4. Post ActorEvent

        LayerRuntime runtime = CreateRuntime();

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f },
            new JobAoiComponent { IsVisible = true });
        runtime.EcsWorld.WithProjectedActor<JobProbeActor>(entity, keepAliveSeconds: 0.5f);

        runtime.EcsWorld.Query().TouchProjectedActor();
        PumpActors(runtime);

        var job = new UpdateEnemyViewJob();
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        PumpActors(runtime);

        JobPositionComponent position = runtime.EcsWorld.Get<JobPositionComponent>(entity);
        ActorId actorId = runtime.EcsWorld.GetProjectionMeta(entity).ActorId;

        Assert.That(position.X, Is.EqualTo(4f));
        Assert.That(position.Y, Is.EqualTo(6f));
        Assert.That(actorId.IsValid, Is.True);
        Assert.That(runtime.Actors.IsAlive(actorId), Is.True);
        Assert.That(JobProbeActor.RentCount, Is.EqualTo(1));
        Assert.That(JobProbeActor.Received, Has.Count.EqualTo(1));
        Assert.That(JobProbeActor.Received[0].X, Is.EqualTo(4f));
        Assert.That(JobProbeActor.Received[0].Y, Is.EqualTo(6f));
    }

    private readonly struct UpdateEnemyViewJob :
        IProjectionJob3x1<JobPositionComponent, JobVelocityComponent, JobAoiComponent, JobMoveViewEvent>
    {
        public ProjectResult Execute(
            Entity                   entity,
            ref JobPositionComponent position,
            ref JobVelocityComponent velocity,
            ref JobAoiComponent      aoi,
            ref JobMoveViewEvent     moveEvent)
        {
            if (!aoi.IsVisible)
            {
                return ProjectResult.Fail;
            }

            if (velocity.X == 0f && velocity.Y == 0f)
            {
                return ProjectResult.Touch;
            }

            position.X += velocity.X;
            position.Y += velocity.Y;

            moveEvent = new JobMoveViewEvent(
                x: position.X,
                y: position.Y);

            return ProjectResult.Success;
        }
    }

    #endregion

    #region Query + Bring Job Tests (Touch)

    [Test]
    public void QueryWithBring_Touch_TouchesActorButDoesNotPostEvent()
    {
        // ?
        //  Query + Bring + Job ?ProjectResult.Touch 
        // 1. Touch/Ensure Actor
        // 2. ?Add Event ?Batch
        // 3. ?Post ActorEvent

        LayerRuntime runtime = CreateRuntime();

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 10f, Y = 20f },
            new JobVelocityComponent { X = 0f, Y = 0f }, //  -> Touch
            new JobAoiComponent { IsVisible = true });
        runtime.EcsWorld.WithProjectedActor<JobProbeActor>(entity, keepAliveSeconds: 0.5f);

        var job = new UpdateEnemyViewJob();
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        runtime.Pump(0.016f);

        JobPositionComponent position = runtime.EcsWorld.Get<JobPositionComponent>(entity);
        ActorId actorId = runtime.EcsWorld.GetProjectionMeta(entity).ActorId;

        // ECS ?
        Assert.That(position.X, Is.EqualTo(10f));
        Assert.That(position.Y, Is.EqualTo(20f));
        // Actor ?Touch/Ensure
        Assert.That(actorId.IsValid, Is.True);
        Assert.That(runtime.Actors.IsAlive(actorId), Is.True);
        //  Post 
        Assert.That(JobProbeActor.Received, Is.Empty);
    }

    #endregion

    #region Query + Bring Job Tests (Fail)

    [Test]
    public void QueryWithBring_Fail_DoesNotTouchActorAndDoesNotPostEvent()
    {
        // ?
        //  Query + Bring + Job ?ProjectResult.Fail 
        // 1. ?Touch Actor
        // 2. ?Add Event ?Batch
        // 3. ?Post ActorEvent

        LayerRuntime runtime = CreateRuntime();
        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 10f, Y = 20f },
            new JobVelocityComponent { X = 3f, Y = 4f },
            new JobAoiComponent { IsVisible = false }); // ?-> Fail
        runtime.EcsWorld.WithProjectedActor<JobProbeActor>(entity, keepAliveSeconds: 0.5f);

        var job = new UpdateEnemyViewJob();
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        runtime.Pump(0.016f);

        JobPositionComponent position = runtime.EcsWorld.Get<JobPositionComponent>(entity);
        ActorId actorId = runtime.EcsWorld.GetProjectionMeta(entity).ActorId;

        // ECS Fail ?
        Assert.That(position.X, Is.EqualTo(10f));
        Assert.That(position.Y, Is.EqualTo(20f));
        // Actor  Touch?
        Assert.That(actorId.IsValid, Is.False);
        //  Post 
        Assert.That(JobProbeActor.Received, Is.Empty);
    }

    #endregion

    #region Helpers

    private static LayerRuntime CreateRuntime()
    {
        return LayerHub.CreateLayers()
                       .Push(new JobTestLayer())
                       .Build();
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

    #endregion

    #region Test Layer

    internal partial class JobTestLayer : Layer
    {
    }

    #endregion
}
