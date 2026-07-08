using Arch.Core;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.Layers;
using LayerBase.Test;

namespace EventsTest;

public class testManager : ILayerContext
{
}

public struct PositionComponent :　IComponent
{
    public float X;
    public float Y;
}

public struct VelocityComponent :　IComponent
{
    public float X;
    public float Y;
}

public struct AoiComponent :　IComponent
{
    public bool IsVisible;
}

public struct MoveViewEvent : IActorEvent
{
    public float X;
    public float Y;

    public MoveViewEvent(float x, float y)
    {
        X = x;
        Y = y;
    }
}

internal sealed partial class ProbeActor : IPooledActor
{
    public static List<MoveViewEvent> Received { get; } = new();
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }

    [ActorBehaviour]
    private void OnMove(in MoveViewEvent value)
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

public sealed partial class EnemyViewService : IService, LayerBase.DI.Options.IUpdate
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Update()
    {
        UpdateEnemyView();
    }

    [Query]
    [Bring<MoveViewEvent>]
    private static ProjectResult OnUpdateEnemyView(
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

[TestFixture]
public class QueryBringTests
{
    private static LayerRuntime CreateRuntime()
    {
        return LayerHub.CreateLayers()
                       .Push(new LayerTestLayer())
                       .Build();
    }

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        ProbeActor.Received.Clear();
        ProbeActor.RentCount = 0;
        ProbeActor.ReturnCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void QueryWithBringInService()
    {
        LayerRuntime runtime = CreateRuntime();
        Entity entity = runtime.EcsWorld.CreateEntity()
                               .WithComponent<PositionComponent>()
                               .WithComponent<VelocityComponent>()
                               .WithComponent<AoiComponent>()
                               .Build();
        runtime.EcsWorld.WithProjectedActor<ProbeActor>(entity);

        entity.Set(new PositionComponent() { X = 10f, Y = 20f }, new VelocityComponent { X = 3f, Y = 4f },
            new AoiComponent { IsVisible = true });

        runtime.Pump(0.1f);
        runtime.Pump(0.1f);
        PositionComponent position = runtime.EcsWorld.Get<PositionComponent>(entity);

        // ECS 数据修改
        Assert.That(position.X, Is.EqualTo(16f));
        Assert.That(position.Y, Is.EqualTo(28f));
        Assert.That(ProbeActor.Received, !Is.Empty);
    }
}

#region Test Layer

internal partial class LayerTestLayer : Layer
{
    [Mount] private EnemyViewService service;
}

#endregion

public class QueryGeneratorTest
{
}
