using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 的热路径缓存组件。
///
/// 作用：
/// 1. 作为普通 ECS 组件参与 Projection Query。
/// 2. 保存 ProjectionExecutor 热路径所需的 ActorId。
/// 3. 保存冷路径创建 Actor 所需的 ActorTypeId。
/// 4. 保存 Projection 生命周期状态（ExpireAtTicks），不再依赖 IPooledActor。
/// </summary>
public struct ProjectedActorRef
{
    /// <summary>
    /// 当前 Entity 绑定的 ActorId。
    ///
    /// 参数作用：
    /// ProjectionExecutor 通过它直接定位目标 Actor。
    /// 如果 ActorId 无效，则会尝试创建新的 Projected Actor。
    /// </summary>
    public ActorId ActorId;

    /// <summary>
    /// Projected Actor 类型 ID。
    ///
    /// 参数作用：
    /// ActorId 无效时，EnsureProjectedActor 使用该字段创建正确类型的 Actor。
    /// </summary>
    internal int ActorTypeId;

    /// <summary>
    /// KeepAliveTicks 参数作用：
    /// 最后一次 Touch 后，Actor 还能保持 Active 的时长。
    /// 这是从 ActorOptions 缓存来的类型级策略。
    /// </summary>
    internal long KeepAliveTicks;

    /// <summary>
    /// ExpireAtTicks 参数作用：
    /// 当前 Actor 的实际到期时间。
    /// 每次 Touch 时刷新为 nowTicks + KeepAliveTicks。
    /// Sweep 时通过 nowTicks >= ExpireAtTicks 判断是否退场。
    /// </summary>
    internal long ExpireAtTicks;

    /// <summary>
    /// TouchIntervalTicks 参数作用：
    /// 两次真实 Touch 之间的最小间隔。
    /// 用于避免短时间重复刷新 ExpireAtTicks。
    /// </summary>
    internal long TouchIntervalTicks;

    /// <summary>
    /// NextTouchTicks 参数作用：
    /// 下一次允许真实 Touch 的时间。
    /// nowTicks 小于该值时，可以跳过真实刷新。
    /// </summary>
    internal long NextTouchTicks;

    /// <summary>
    /// 退场策略。
    /// </summary>
    internal ProjectedActorRetirePolicy RetirePolicy;

    /// <summary>
    /// 创建策略。
    /// </summary>
    internal ProjectedActorCreatePolicy CreatePolicy;

    /// <summary>
    /// Projected Actor 释放策略。
    /// </summary>
    internal ProjectedActorReleasePolicy ReleasePolicy;

    /// <summary>
    /// 当前 ActorId 是否有效。
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ActorId.IsValid;
    }

    /// <summary>
    /// 构造未绑定但可投影的 ProjectedActorRef。
    /// </summary>
    public ProjectedActorRef(
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ActorId = ActorId.Invalid;
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ExpireAtTicks = 0;
        ReleasePolicy = releasePolicy;
        TouchIntervalTicks = 0;
        NextTouchTicks = 0;
        RetirePolicy = ProjectedActorRetirePolicy.ReturnToPool;
        CreatePolicy = ProjectedActorCreatePolicy.Lazy;
    }

    /// <summary>
    /// 构造未绑定但可投影的 ProjectedActorRef（带完整选项）。
    /// </summary>
    internal ProjectedActorRef(
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy,
        ProjectedActorRetirePolicy retirePolicy,
        ProjectedActorCreatePolicy createPolicy,
        long touchIntervalTicks)
    {
        ActorId = ActorId.Invalid;
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ExpireAtTicks = 0;
        ReleasePolicy = releasePolicy;
        TouchIntervalTicks = touchIntervalTicks;
        NextTouchTicks = 0;
        RetirePolicy = retirePolicy;
        CreatePolicy = createPolicy;
    }

    /// <summary>
    /// 构造已绑定 ActorId 的 ProjectedActorRef。
    /// </summary>
    public ProjectedActorRef(
        ActorId actorId,
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ActorId = actorId;
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ExpireAtTicks = 0;
        ReleasePolicy = releasePolicy;
        TouchIntervalTicks = 0;
        NextTouchTicks = 0;
        RetirePolicy = ProjectedActorRetirePolicy.ReturnToPool;
        CreatePolicy = ProjectedActorCreatePolicy.Lazy;
    }

    /// <summary>
    /// 创建未绑定但可投影的 ProjectedActorRef。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ProjectedActorRef CreateProjectable(
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        return new ProjectedActorRef(
            actorTypeId,
            keepAliveTicks,
            releasePolicy);
    }

    /// <summary>
    /// 创建未绑定但可投影的 ProjectedActorRef（带完整选项）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ProjectedActorRef CreateProjectable(
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy,
        in ProjectedActorOptions options)
    {
        return new ProjectedActorRef(
            actorTypeId,
            keepAliveTicks,
            releasePolicy,
            options.RetirePolicy,
            options.CreatePolicy,
            options.TouchIntervalTicks);
    }

    /// <summary>
    /// 绑定 ActorId 并初始化到期时间。
    /// </summary>
    /// <param name="actorId">新绑定的 ActorId。</param>
    /// <param name="nowTicks">当前时间戳。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind(
        ActorId actorId,
        long nowTicks)
    {
        ActorId = actorId;
        ExpireAtTicks = ProjectedActorTime.BuildDeadline(nowTicks, KeepAliveTicks);
        NextTouchTicks = nowTicks + TouchIntervalTicks;
    }

    /// <summary>
    /// 清空 ActorId。
    ///
    /// 作用：
    /// 只清空 ActorId，不清空 ActorTypeId、KeepAliveTicks、ReleasePolicy。
    /// 因为 Entity 仍然是可投影实体，后续可以再次创建 Actor。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearActor()
    {
        ActorId = ActorId.Invalid;
        ExpireAtTicks = 0;
    }
}
