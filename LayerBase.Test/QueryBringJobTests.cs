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


public class testManager : ILayerContext
{
    
}
public sealed partial class EnemyViewService : IService,LayerBase.DI.Options.IUpdate
{
    [Mount] private testManager s;
    public void ConfigureServices(IServiceCollection services)
    {
            
    }
    public void Update()
    {
        UpdateEnemyView();
    }

    [Query]
    [Bring<MoveViewEvent>]
    private ProjectResult OnUpdateEnemyView(
        ref PositionComponent position,
        in  VelocityComponent velocity,
        in  AoiComponent      aoi,
        ref MoveViewEvent     moveEvent)
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

        moveEvent = new MoveViewEvent(
            x: position.X,
            y: position.Y);

        return ProjectResult.Success;
    }

  
}

public struct PositionComponent : IComponent
{
    public float X;
    public float Y;
}

public struct VelocityComponent : IComponent
{
    public float X;
    public float Y;
}

public struct AoiComponent : IComponent
{
    public bool IsVisible;
}

public readonly struct MoveViewEvent : IActorEvent
{
    public readonly float X;
    public readonly float Y;

    public MoveViewEvent(
        float x,
        float y)
    {
        X = x;
        Y = y;
    }
}

public struct JobPositionComponent :　IComponent
{
    public float X;
    public float Y;
}

public struct JobVelocityComponent:　IComponent
{
    public float X;
    public float Y;
}

public struct JobAoiComponent:　IComponent
{
    public bool IsVisible;
}

public struct JobMoveViewEvent:IActorEvent
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
internal sealed partial class JobActor : IActor
{
    [ActorBehaviour]
    private void OnMove(in JobMoveViewEvent value)
    {
        Assert.That(value.X, Is.GreaterThan(0));
    }
}

internal sealed partial class JobProbeActor : IPooledActor
{
    public static List<JobMoveViewEvent> Received { get; } = new();
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }

    public long RecycleDeadlineTicks { get; set; }

    [ActorBehaviour]
    private void OnMove(in JobMoveViewEvent value)
    {
        Received.Add(value);
    }

    public void OnRent()
    {
        RentCount++;
        RecycleDeadlineTicks = 0;
    }

    public void OnReturn()
    {
        ReturnCount++;
        RecycleDeadlineTicks = 0;
    }
}

#endregion

#region Test Layer

internal partial class JobTestLayer : Layer
{
    [Mount] private EnemyViewService enemyViewService;
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
        // 逻辑说明：
        // 验证纯 Query + Job ForEach 能正确遍历并修改组件数据。
        // 不创建 Actor，不 Touch Actor，不 Post ActorEvent。

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
            Entity entity,
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
        // 逻辑说明：
        // 验证 Query + Bring + Job 在 ProjectResult.Success 时：
        // 1. 修改 ECS 数据
        // 2. Touch/Ensure Actor
        // 3. Add Event 到 Batch
        // 4. Post ActorEvent

        LayerRuntime runtime = CreateRuntime();
        RegisterProbeActor(runtime, actorTypeId: 1);

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f },
            new JobAoiComponent { IsVisible = true });
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 1, keepAliveSeconds: 0.5f);

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
            Entity entity,
            ref JobPositionComponent position,
            ref JobVelocityComponent velocity,
            ref JobAoiComponent aoi,
            ref JobMoveViewEvent moveEvent)
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
        // 逻辑说明：
        // 验证 Query + Bring + Job 在 ProjectResult.Touch 时：
        // 1. Touch/Ensure Actor
        // 2. 不 Add Event 到 Batch
        // 3. 不 Post ActorEvent

        LayerRuntime runtime = CreateRuntime();
        RegisterProbeActor(runtime, actorTypeId: 2);

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 10f, Y = 20f },
            new JobVelocityComponent { X = 0f, Y = 0f },  // 零速度 -> Touch
            new JobAoiComponent { IsVisible = true });
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 2, keepAliveSeconds: 0.5f);

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

        // ECS 数据未修改（零速度时不修改位置）
        Assert.That(position.X, Is.EqualTo(10f));
        Assert.That(position.Y, Is.EqualTo(20f));
        // Actor 被 Touch/Ensure
        Assert.That(actorId.IsValid, Is.True);
        Assert.That(runtime.Actors.IsAlive(actorId), Is.True);
        // 没有 Post 事件
        Assert.That(JobProbeActor.Received, Is.Empty);
    }

    #endregion

    #region Query + Bring Job Tests (Fail)

    [Test]
    public void QueryWithBring_Fail_DoesNotTouchActorAndDoesNotPostEvent()
    {
        // 逻辑说明：
        // 验证 Query + Bring + Job 在 ProjectResult.Fail 时：
        // 1. 不 Touch Actor
        // 2. 不 Add Event 到 Batch
        // 3. 不 Post ActorEvent

        LayerRuntime runtime = CreateRuntime();
        RegisterProbeActor(runtime, actorTypeId: 3);

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 10f, Y = 20f },
            new JobVelocityComponent { X = 3f, Y = 4f },
            new JobAoiComponent { IsVisible = false });  // 不可见 -> Fail
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 3, keepAliveSeconds: 0.01f);

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

        // ECS 数据未修改（Fail 时不修改位置）
        Assert.That(position.X, Is.EqualTo(10f));
        Assert.That(position.Y, Is.EqualTo(20f));
        // Actor 不被 Touch（会因超时被回收）
        Assert.That(actorId.IsValid, Is.False);
        // 没有 Post 事件
        Assert.That(JobProbeActor.Received, Is.Empty);
    }
    
    [Test]
    public void QueryWithBringInService()
    {
        LayerRuntime runtime = CreateRuntime();
        RegisterProbeActor(runtime, actorTypeId: 3);
        Entity entity = runtime.EcsWorld.CreateEntity()
                               .WithComponent<JobPositionComponent>()
                               .WithComponent<JobVelocityComponent>()
                               .WithComponent<JobAoiComponent>()
                               .WithProjectedActor<JobProbeActor>()
                               .Build();
        
        entity.Set(new JobPositionComponent() { X = 10f, Y = 20f }, new JobVelocityComponent { X = 3f, Y = 4f }, new JobAoiComponent { IsVisible = true });
        
        runtime.Pump(0.1f);

        JobPositionComponent position = runtime.EcsWorld.Get<JobPositionComponent>(entity);

        // ECS 数据未修改（Fail 时不修改位置）
        Assert.That(position.X, Is.EqualTo(10f));
        Assert.That(position.Y, Is.EqualTo(20f));
        // 没有 Post 事件
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

    private static void RegisterProbeActor(LayerRuntime runtime, int actorTypeId)
    {
        runtime.ProjectedActorTypes.RegisterGenerated(
            actorTypeId,
            typeof(JobProbeActor),
            static actorWorld => actorWorld.CreateProjectedActor<JobProbeActor>());
    }

    #endregion

    #region QueryService

    #endregion
}
