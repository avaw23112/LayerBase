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

    /// <summary>
    /// 退场策略。
    /// </summary>
    public ProjectedActorRetirePolicy RetirePolicy;

    /// <summary>
    /// 创建策略。
    /// </summary>
    public ProjectedActorCreatePolicy CreatePolicy;

    /// <summary>
    /// Touch 节流间隔。
    /// </summary>
    public long TouchIntervalTicks;

    /// <summary>
    /// 下一次允许 Touch 的时间戳。
    /// </summary>
    public long NextTouchTicks;

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
                KeepAliveTicks = 0,
                RetirePolicy = ProjectedActorRetirePolicy.ReturnToPool,
                CreatePolicy = ProjectedActorCreatePolicy.Lazy,
                TouchIntervalTicks = 0,
                NextTouchTicks = 0
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

    /// <summary>
    /// MarkProjected 内部工具接收 ProjectedActorOptions。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkProjected(
        int                         actorTypeId,
        long                        keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy,
        in ProjectedActorOptions    options)
    {
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ReleasePolicy = releasePolicy;
        RetirePolicy = options.RetirePolicy;
        CreatePolicy = options.CreatePolicy;
        TouchIntervalTicks = options.TouchIntervalTicks;
        NextTouchTicks = 0;
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
    Disabled = 3,
    Released = 4
}

public enum ProjectedActorReleasePolicy : byte
{
    DestroyImmediately = 0,
    ReturnToPool = 1,
    DetachAndLetActorFinish = 2
}
