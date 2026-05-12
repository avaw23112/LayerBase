using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal readonly struct ProjectedActorHandle
{
    public readonly ActorId ActorId;
    public readonly IPooledActor Actor;

    public ProjectedActorHandle(
        ActorId      actorId,
        IPooledActor actor)
    {
        ActorId = actorId;
        Actor = actor;
    }

    public bool IsValid => ActorId.IsValid && Actor != null;
}