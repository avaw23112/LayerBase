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
/// 4. 保存 Touch 时刷新回收时间所需的 KeepAliveTicks。
/// 5. 避免 ProjectionExecutor 每行读取 ProjectedActorMeta。
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
    ///
    /// 注意：
    /// 该字段是 internal，业务层不应依赖它。
    /// </summary>
    internal int ActorTypeId;

    /// <summary>
    /// Projected Actor 保活时间。
    ///
    /// 参数作用：
    /// TouchProjectedActor 使用该字段刷新 IPooledActor.RecycleDeadlineTicks。
    /// </summary>
    internal long KeepAliveTicks;

    /// <summary>
    /// Projected Actor 释放策略。
    ///
    /// 参数作用：
    /// 与 ProjectedActorMeta.ReleasePolicy 保持一致。
    /// 当前热路径一般不直接读取它，但需要保留配置同步能力。
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
    /// <param name="actorTypeId">Projected Actor 类型 ID。</param>
    /// <param name="keepAliveTicks">Projected Actor 保活时间。如果传入负数，则修正为 0。</param>
    /// <param name="releasePolicy">Projected Actor 释放策略。</param>
    public ProjectedActorRef(
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ActorId = ActorId.Invalid;
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ReleasePolicy = releasePolicy;
    }

    /// <summary>
    /// 构造已绑定 ActorId 的 ProjectedActorRef。
    /// </summary>
    /// <param name="actorId">已绑定的 ActorId。</param>
    /// <param name="actorTypeId">Projected Actor 类型 ID。</param>
    /// <param name="keepAliveTicks">保活时间。</param>
    /// <param name="releasePolicy">释放策略。</param>
    public ProjectedActorRef(
        ActorId actorId,
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ActorId = actorId;
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ReleasePolicy = releasePolicy;
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
    /// 绑定 ActorId。
    /// </summary>
    /// <param name="actorId">新绑定的 ActorId。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind(
        ActorId actorId)
    {
        ActorId = actorId;
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
    }
}
