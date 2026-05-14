using System.Runtime.CompilerServices;
using Arch.Core;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 标记工具。
/// 作用：统一标记 Entity 为可投影，并保证 ProjectedActorRef 热路径组件存在。
/// </summary>
internal static class ProjectedActorMarkUtility
{
    /// <summary>
    /// 将 Entity 标记为可投影 Actor。
    /// </summary>
    /// <param name="world">ECS World。</param>
    /// <param name="entity">目标 Entity。</param>
    /// <param name="meta">ProjectedActorMeta 引用。</param>
    /// <param name="actorTypeId">Projected Actor 类型 ID。</param>
    /// <param name="keepAliveTicks">保活时间。</param>
    /// <param name="releasePolicy">释放策略。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkProjected(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta,
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        meta.MarkProjected(
            actorTypeId,
            keepAliveTicks,
            releasePolicy);

        ProjectedActorRef actorRef =
            ProjectedActorRef.CreateProjectable(
                actorTypeId,
                keepAliveTicks,
                releasePolicy);

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
