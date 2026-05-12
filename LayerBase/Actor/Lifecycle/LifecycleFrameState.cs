namespace LayerBase.Actor;

internal readonly struct LifecycleFrameState
{
    public readonly ActorWorld World;
    public readonly float DeltaTime;

    public LifecycleFrameState(ActorWorld world, float deltaTime)
    {
        World = world;
        DeltaTime = deltaTime;
    }
}