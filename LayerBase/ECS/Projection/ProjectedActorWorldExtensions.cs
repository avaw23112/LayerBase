using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.ECS.Projection.Generated;

namespace LayerBase.ECS.Projection;

public static class ProjectedActorWorldExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WithProjectedActor<TActor>(
        this World                  world,
        Entity                      entity,
        float                       keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy    = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        int actorTypeId = ActorType<TActor>.Id;
        if (actorTypeId < 0)
        {
            throw new InvalidOperationException(
                $"ProjectedActor type {typeof(TActor).Name} was not generated. Make sure it implements IPooledActor and is visible to the generator.");
        }

        ProjectedActorTypeRegistry.RegisterGenerated(actorTypeId, typeof(TActor),
            static actorWorld => actorWorld.CreateProjectedActor<TActor>());

        long keepAliveTicks = ProjectedActorTime.SecondsToTicks(keepAliveSeconds);
        WithProjectedActor(world, entity, actorTypeId, keepAliveTicks, releasePolicy);
    }

    /// <summary>
    /// 将 Entity 标记为可投影 Actor。
    ///
    /// world 参数作用：
    /// 当前 ECS World。
    ///
    /// entity 参数作用：
    /// 需要绑定 ProjectedActor 的 Entity。
    ///
    /// actorTypeId 参数作用：
    /// ProjectedActor 类型编号。
    ///
    /// keepAliveOverrideTicks 参数作用：
    /// 显式覆盖的保活时长。
    /// null 表示使用 ProjectedActorOptions.KeepAliveTicks。
    ///
    /// releasePolicy 参数作用：
    /// 兼容旧释放策略。
    /// </summary>
    internal static void WithProjectedActor(
        this World world,
        Entity entity,
        int actorTypeId,
        long? keepAliveOverrideTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ref ProjectedActorMeta meta =
            ref world.GetProjectionMeta(entity);

        ProjectedActorMarkUtility.MarkProjected(
            world,
            entity,
            ref meta,
            actorTypeId,
            keepAliveOverrideTicks,
            releasePolicy);
    }


}
