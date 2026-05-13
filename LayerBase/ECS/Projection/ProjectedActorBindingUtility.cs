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
        UpsertRef(world, entity, actorId);
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
        UpsertRef(world, entity, ActorId.Invalid);
    }

    /// <summary>
    /// 插入或更新 ProjectedActorRef。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// entity：目标 Entity。
    /// actorId：要写入的 ActorId。
    ///
    /// 作用：
    /// 如果组件存在则 Set，不存在则 Add。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpsertRef(
        World world,
        Entity entity,
        ActorId actorId)
    {
        var actorRef = new ProjectedActorRef(actorId);

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
