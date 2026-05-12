using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal struct ProjectedActorMeta
{
    public ActorId ActorId;
    public int ActorTypeId;
    public int ActiveListIndex;
    public ProjectedActorState State;
    public ProjectedActorReleasePolicy ReleasePolicy;
    public long KeepAliveTicks;

    public static ProjectedActorMeta None
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return new ProjectedActorMeta
            {
                ActorId = ActorId.Invalid,
                ActorTypeId = -1,
                ActiveListIndex = -1,
                State = ProjectedActorState.None,
                ReleasePolicy = ProjectedActorReleasePolicy.ReturnToPool,
                KeepAliveTicks = 0
            };
        }
    }

    public bool HasActor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ActorId.IsValid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkProjected(
        int                         actorTypeId,
        long                        keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ReleasePolicy = releasePolicy;
        State = ProjectedActorState.Projectable;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindActor(
        ActorId actorId)
    {
        ActorId = actorId;
        State = ProjectedActorState.Active;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearActor()
    {
        ActorId = ActorId.Invalid;
        State = ActorTypeId >= 0
            ? ProjectedActorState.Projectable
            : ProjectedActorState.None;
    }
}

internal enum ProjectedActorState : byte
{
    None = 0,
    Projectable = 1,
    Active = 2,
    PendingRelease = 3
}

public enum ProjectedActorReleasePolicy : byte
{
    DestroyImmediately = 0,
    ReturnToPool = 1,
    DetachAndLetActorFinish = 2
}