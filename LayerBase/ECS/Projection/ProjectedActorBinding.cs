using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorBinding
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World                  world,
        ActorWorld             actorWorld,
        Entity                 entity,
        ref ProjectedActorMeta meta,
        long                   nowTicks)
    {
        ProjectedActorHandle handle = ProjectedActorTypeRegistry.CreateActorByTypeId(actorWorld, meta.ActorTypeId);
        if (!handle.IsValid)
        {
            return ActorId.Invalid;
        }

        handle.Actor.RecycleDeadlineTicks = ProjectedActorTime.BuildDeadline(nowTicks, meta.KeepAliveTicks);
        ProjectedActorBindingUtility.Bind(world, entity, ref meta, handle.ActorId);
        world.AddActiveProjectedActor(entity, ref meta);
        return handle.ActorId;
    }

    /// <summary>
    /// 确保 Entity 拥有 Projected Actor（热路径版本）。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// actorWorld：ActorWorld。
    /// entity：当前 Entity。
    /// meta：当前 Entity 的 ProjectedActorMeta。
    /// actorRef：当前 Entity 的 ProjectedActorRef。
    /// nowTicks：当前时间戳。
    ///
    /// 返回值：
    /// 有效 ActorId，或 ActorId.Invalid。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World                  world,
        ActorWorld             actorWorld,
        Entity                 entity,
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef  actorRef,
        long                   nowTicks)
    {
        ProjectedActorHandle handle = ProjectedActorTypeRegistry.CreateActorByTypeId(actorWorld, meta.ActorTypeId);
        if (!handle.IsValid)
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        handle.Actor.RecycleDeadlineTicks = ProjectedActorTime.BuildDeadline(nowTicks, meta.KeepAliveTicks);
        ProjectedActorBindingUtility.Bind(ref meta, ref actorRef, handle.ActorId);
        world.AddActiveProjectedActor(entity, ref meta);
        return handle.ActorId;
    }

    /// <summary>
    /// 确保 Entity 拥有 Projected Actor（不读 meta 的热路径版本）。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// actorWorld：ActorWorld。
    /// entity：当前 Entity。
    /// actorRef：ProjectedActorRef 热路径缓存。
    /// nowTicks：当前时间戳。
    ///
    /// 返回值：
    /// 有效 ActorId，或 ActorId.Invalid。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World                 world,
        ActorWorld            actorWorld,
        Entity                entity,
        ref ProjectedActorRef actorRef,
        long                  nowTicks)
    {
        ProjectedActorHandle handle =
            ProjectedActorTypeRegistry.CreateActorByTypeId(
                actorWorld,
                actorRef.ActorTypeId);

        if (!handle.IsValid)
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        handle.Actor.RecycleDeadlineTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                actorRef.KeepAliveTicks);

        actorRef.Bind(handle.ActorId);

        ref ProjectedActorMeta meta =
            ref world.GetProjectionMeta(entity);

        meta.BindActor(handle.ActorId);

        world.AddActiveProjectedActor(
            entity,
            ref meta);

        return handle.ActorId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TouchProjectedActor(
        ActorWorld             actorWorld,
        ref ProjectedActorMeta meta,
        long                   nowTicks)
    {
        if (!meta.ActorId.IsValid)
        {
            return;
        }

        if (!actorWorld.TryGetPooledActor(meta.ActorId, out IPooledActor pooledActor))
        {
            meta.ClearActor();
            return;
        }

        pooledActor.RecycleDeadlineTicks = ProjectedActorTime.BuildDeadline(nowTicks, meta.KeepAliveTicks);
    }

    /// <summary>
    /// 刷新 Projected Actor 保活时间（热路径版本）。
    ///
    /// 参数说明：
    /// actorWorld：ActorWorld。
    /// meta：ProjectedActorMeta。
    /// actorRef：ProjectedActorRef。
    /// nowTicks：当前时间戳。
    ///
    /// 返回值：
    /// true：actor 仍然有效。
    /// false：actor 已失效，meta/ref 已清理。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TouchProjectedActor(
        ActorWorld             actorWorld,
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef  actorRef,
        long                   nowTicks)
    {
        ActorId actorId = actorRef.ActorId;

        if (!actorId.IsValid)
        {
            ProjectedActorBindingUtility.Clear(ref meta, ref actorRef);
            return false;
        }

        if (!actorWorld.TryGetPooledActor(actorId, out IPooledActor pooledActor))
        {
            ProjectedActorBindingUtility.Clear(ref meta, ref actorRef);
            return false;
        }

        pooledActor.RecycleDeadlineTicks = ProjectedActorTime.BuildDeadline(nowTicks, meta.KeepAliveTicks);
        return true;
    }

    /// <summary>
    /// 刷新 Projected Actor 保活时间（不读 meta 的热路径版本）。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// actorWorld：ActorWorld。
    /// entity：当前 Entity。
    /// actorRef：ProjectedActorRef 热路径缓存。
    /// nowTicks：当前时间戳。
    ///
    /// 返回值：
    /// true 表示 actor 仍然有效；false 表示 actor 已失效。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TouchProjectedActor(
        World                 world,
        ActorWorld            actorWorld,
        Entity                entity,
        ref ProjectedActorRef actorRef,
        long                  nowTicks)
    {
        ActorId actorId = actorRef.ActorId;

        if (!actorId.IsValid)
        {
            ClearByEntity(world, entity, ref actorRef);
            return false;
        }

        if (!actorWorld.TryGetPooledActor(
                actorId,
                out IPooledActor pooledActor))
        {
            ClearByEntity(world, entity, ref actorRef);
            return false;
        }

        pooledActor.RecycleDeadlineTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                actorRef.KeepAliveTicks);

        return true;
    }

    /// <summary>
    /// 根据 Entity 清理 meta/ref。
    /// </summary>
    /// <param name="world">ECS World。</param>
    /// <param name="entity">当前 Entity。</param>
    /// <param name="actorRef">ProjectedActorRef。</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearByEntity(
        World world,
        Entity entity,
        ref ProjectedActorRef actorRef)
    {
        actorRef.ClearActor();

        ref ProjectedActorMeta meta =
            ref world.GetProjectionMeta(entity);

        meta.ClearActor();
    }
}
