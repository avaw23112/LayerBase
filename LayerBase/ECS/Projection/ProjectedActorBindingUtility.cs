using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 绑定工具。
/// 作用：统一维护 ProjectedActorMeta 和 ProjectedActorRef 的一致性。
/// </summary>
internal static class ProjectedActorBindingUtility
{
    /// <summary>
    /// 绑定 Projected Actor。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// entity：需要绑定 Actor 的 Entity。
    /// meta：ProjectedActorMeta 引用。
    /// actorId：新绑定的 ActorId。
    ///
    /// 作用：
    /// 同时写入 internal meta 和 public ref。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta,
        ActorId actorId)
    {
        meta.BindActor(actorId);
        UpsertRef(world, entity, in meta, actorId);
    }

    /// <summary>
    /// 绑定 Projected Actor（热路径版本）。
    ///
    /// 参数说明：
    /// meta：ProjectedActorMeta 引用。
    /// actorRef：ProjectedActorRef 引用。
    /// actorId：新绑定的 ActorId。
    ///
    /// 作用：
    /// 用于模板热路径。
    /// 当模板已经拿到 ref ProjectedActorRef 时，不需要再 world.Set。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind(
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef,
        ActorId actorId)
    {
        meta.BindActor(actorId);
        actorRef.Bind(actorId);
    }

    /// <summary>
    /// 清理 Projected Actor 绑定。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// entity：需要清理绑定的 Entity。
    /// meta：ProjectedActorMeta 引用。
    ///
    /// 作用：
    /// 同时清理 internal meta 和 public ref。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta)
    {
        meta.ClearActor();

        UpsertRef(
            world,
            entity,
            in meta,
            ActorId.Invalid);
    }

    /// <summary>
    /// 清理 Projected Actor 绑定（热路径版本）。
    ///
    /// 参数说明：
    /// meta：ProjectedActorMeta 引用。
    /// actorRef：ProjectedActorRef 引用。
    ///
    /// 作用：
    /// 用于模板热路径。
    /// 当 actor 已经失效时，同时清理 meta 和 ref。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef)
    {
        meta.ClearActor();
        actorRef.ClearActor();
    }

    /// <summary>
    /// 确保 Entity 上存在 ProjectedActorRef。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// entity：目标 Entity。
    ///
    /// 作用：
    /// ProjectedActorRef 必须在 Entity 被标记为可投影时就添加。
    /// 否则 Query 如果要求 ProjectedActorRef 组件，会跳过尚未创建 Actor 的可投影实体。
    ///
    /// 注意：
    /// 该方法仅作为兼容兜底，不保留 ActorTypeId/KeepAliveTicks/ReleasePolicy。
    /// MarkProjected 主路径必须写完整 ProjectedActorRef。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureInvalidRef(
        World world,
        Entity entity)
    {
        if (world.Has<ProjectedActorRef>(entity))
        {
            return;
        }

        world.Add(entity, new ProjectedActorRef(0, 0, ProjectedActorReleasePolicy.ReturnToPool));
    }

    /// <summary>
    /// 插入或更新 ProjectedActorRef。
    /// </summary>
    /// <param name="world">ECS World。</param>
    /// <param name="entity">目标 Entity。</param>
    /// <param name="meta">当前 ProjectedActorMeta。</param>
    /// <param name="actorId">要写入的 ActorId。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpsertRef(
        World world,
        Entity entity,
        in ProjectedActorMeta meta,
        ActorId actorId)
    {
        var actorRef = new ProjectedActorRef(
            actorId,
            meta.ActorTypeId,
            meta.KeepAliveTicks,
            meta.ReleasePolicy);

        if (world.Has<ProjectedActorRef>(entity))
        {
            world.Set(entity, actorRef);
        }
        else
        {
            world.Add(entity, actorRef);
        }
    }
}
