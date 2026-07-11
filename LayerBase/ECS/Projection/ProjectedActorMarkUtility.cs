using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// 确保 ProjectedActorRef 组件类型在 Arch ECS 中注册。
/// 通过访问 Component&lt;ProjectedActorRef&gt; 触发静态构造函数。
/// </summary>
internal static class ProjectedActorRefRegistration
{
    internal static readonly ComponentType ComponentType = Component<ProjectedActorRef>.ComponentType;
}

/// <summary>
/// Projected Actor 标记工具。
/// 作用：统一标记 Entity 为可投影，并保证 ProjectedActorRef 热路径组件存在。
/// </summary>
internal static class ProjectedActorMarkUtility
{
    /// <summary>
    /// 将 Entity 标记为可投影 Actor。
    ///
    /// world 参数作用：
    /// 当前 ECS World。
    ///
    /// entity 参数作用：
    /// 被标记的目标 Entity。
    ///
    /// meta 参数作用：
    /// Entity 对应的 ProjectedActorMeta。
    ///
    /// actorTypeId 参数作用：
    /// Actor 类型编号，用于后续 Lazy 创建 Actor。
    ///
    /// keepAliveOverrideTicks 参数作用：
    /// 显式覆盖的保活时长。
    /// null 表示使用 ActorOptions 中的 KeepAliveTicks。
    ///
    /// releasePolicy 参数作用：
    /// 兼容旧释放策略。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkProjected(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta,
        int actorTypeId,
        long? keepAliveOverrideTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ProjectedActorOptions options =
            ProjectedActorTypeRegistry.GetOptions(actorTypeId);

        long effectiveKeepAliveTicks =
            keepAliveOverrideTicks ?? options.KeepAliveTicks;

        meta.MarkProjected(
            actorTypeId,
            effectiveKeepAliveTicks,
            releasePolicy,
            in options);

        ProjectedActorRef actorRef =
            ProjectedActorRef.CreateProjectable(
                actorTypeId,
                effectiveKeepAliveTicks,
                releasePolicy,
                in options);

        if (world.Has<ProjectedActorRef>(entity))
        {
            world.Set(entity, actorRef);
        }
        else
        {
            world.Add(entity, actorRef);
        }

        if (options.CreatePolicy == ProjectedActorCreatePolicy.OnMark)
        {
            EnsureOnMarkProjectedActor(
                world,
                entity,
                ref meta);
        }
    }

    /// <summary>
    /// EnsureOnMarkProjectedActor 作用：
    /// 当 ActorOptions.CreatePolicy 为 OnMark 时，立即创建并绑定 ProjectedActor。
    ///
    /// world 参数作用：
    /// 当前 ECS World。
    ///
    /// entity 参数作用：
    /// 被标记为可投影的 Entity。
    ///
    /// meta 参数作用：
    /// Entity 对应的 ProjectedActorMeta。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureOnMarkProjectedActor(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta)
    {
        if (meta.ActorId.IsValid)
        {
            return;
        }

        if (!world.Has<ProjectedActorRef>(entity))
        {
            return;
        }

        ref ProjectedActorRef actorRef =
            ref world.Get<ProjectedActorRef>(entity);

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectedActorBinding.EnsureProjectedActor(
            world,
            world.GetActorWorld(),
            entity,
            ref meta,
            ref actorRef,
            nowTicks);
    }
}
