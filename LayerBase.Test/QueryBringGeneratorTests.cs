using LayerBase.Core;
using LayerBase.DI;
using LayerBase.ECS;

namespace Game.Tests;

public sealed partial class EnemyViewService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        
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