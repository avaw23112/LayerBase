using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase;
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
        if (ShouldDeferActorWorldAccess(world, actorWorld))
        {
            return ActorId.Invalid;
        }

        ProjectedActorHandle handle = ProjectedActorTypeRegistry.CreateActorByTypeId(actorWorld, meta.ActorTypeId);
        if (!handle.IsValid)
        {
            return ActorId.Invalid;
        }

        ProjectedActorBindingUtility.Bind(world, entity, ref meta, handle.ActorId, nowTicks);
        world.AddActiveProjectedActor(entity, ref meta);
        return handle.ActorId;
    }

    /// <summary>
    /// 确保 Entity 拥有 Projected Actor（热路径版本）。
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
        if (ShouldDeferActorWorldAccess(world, actorWorld))
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        ProjectedActorHandle handle = ProjectedActorTypeRegistry.CreateActorByTypeId(actorWorld, meta.ActorTypeId);
        if (!handle.IsValid)
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        // 绑定 ActorId 并初始化 ExpireAtTicks
        ProjectedActorBindingUtility.Bind(ref meta, ref actorRef, handle.ActorId, nowTicks);
        world.AddActiveProjectedActor(entity, ref meta);
        return handle.ActorId;
    }

    /// <summary>
    /// 确保 Entity 拥有 Projected Actor（不读 meta 的热路径版本）。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World                 world,
        ActorWorld            actorWorld,
        Entity                entity,
        ref ProjectedActorRef actorRef,
        long                  nowTicks)
    {
        if (ShouldDeferActorWorldAccess(world, actorWorld))
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        ProjectedActorHandle handle =
            ProjectedActorTypeRegistry.CreateActorByTypeId(
                actorWorld,
                actorRef.ActorTypeId);

        if (!handle.IsValid)
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        // 绑定 ActorId 并初始化 ExpireAtTicks
        actorRef.Bind(handle.ActorId, nowTicks);

        ref ProjectedActorMeta meta =
            ref world.GetProjectionMeta(entity);

        meta.BindActor(handle.ActorId);

        world.AddActiveProjectedActor(
            entity,
            ref meta);

        return handle.ActorId;
    }

    /// <summary>
    /// 旧 TouchProjectedActor 兼容方法。
    /// 新代码必须使用 RefreshProjectedActorInterest。
    /// </summary>
    [Obsolete("Use RefreshProjectedActorInterest instead. This method no longer refreshes ExpireAtTicks.")]
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
    }

    /// <summary>
    /// 刷新 Projected Actor 保活时间（热路径版本）。
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

        if (ShouldDeferActorWorldAccess(actorWorld))
        {
            RefreshDeadline(ref actorRef, nowTicks);
            return true;
        }

        if (!actorWorld.TryGetPooledActor(actorId, out IPooledActor pooledActor))
        {
            ProjectedActorBindingUtility.Clear(ref meta, ref actorRef);
            return false;
        }

        // 刷新 ExpireAtTicks
        RefreshDeadline(ref actorRef, nowTicks);
        return true;
    }

    /// <summary>
    /// 刷新 Projected Actor 保活时间（不读 meta 的热路径版本）。
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

        if (ShouldDeferActorWorldAccess(world, actorWorld))
        {
            RefreshDeadline(ref actorRef, nowTicks);
            return true;
        }

        if (!actorWorld.TryGetPooledActor(
                actorId,
                out IPooledActor pooledActor))
        {
            ClearByEntity(world, entity, ref actorRef);
            return false;
        }

        // 刷新 ExpireAtTicks
        RefreshDeadline(ref actorRef, nowTicks);
        return true;
    }

    /// <summary>
    /// 根据 Entity 清理 meta/ref。
    /// </summary>
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

    /// <summary>
    /// 刷新 Projected Actor 兴趣（热路径版本）。
    ///
    /// 行为：
    /// 1. ActorId 无效时不能被节流跳过，必须 Ensure。
    /// 2. Disabled 状态不能因为 NextTouchTicks 跳过 Enable。
    /// 3. Active 状态才允许节流直接 return true。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RefreshProjectedActorInterest(
        World                 world,
        ActorWorld            actorWorld,
        Entity                entity,
        ref ProjectedActorRef actorRef,
        long                  nowTicks)
    {
        ActorId actorId = actorRef.ActorId;

        if (!actorId.IsValid)
        {
            actorId = EnsureProjectedActor(
                world,
                actorWorld,
                entity,
                ref actorRef,
                nowTicks);

            return actorId.IsValid;
        }

        if (ShouldDeferActorWorldAccess(world, actorWorld))
        {
            _ = world.TryEnableProjectedActor(actorId);
            RefreshDeadline(
                ref actorRef,
                nowTicks);

            return true;
        }

        if (actorWorld.IsProjectedActorDisabled(actorId))
        {
            if (!actorWorld.EnableProjectedActorIfDisabled(actorId))
            {
                ClearByEntity(world, entity, ref actorRef);
                return false;
            }

            // Disabled 恢复后刷新 ExpireAtTicks
            RefreshDeadline(
                ref actorRef,
                nowTicks);

            return true;
        }

        if (nowTicks < actorRef.NextTouchTicks)
        {
            return true;
        }

        // 刷新 ExpireAtTicks
        RefreshDeadline(
            ref actorRef,
            nowTicks);

        return true;
    }

    /// <summary>
    /// RefreshDeadline 作用：
    /// 刷新 ProjectedActor 的兴趣到期时间。
    ///
    /// 注意：不再需要 TryGetPooledActor，因为 ExpireAtTicks 由 Projection 系统内部维护。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RefreshDeadline(
        ref ProjectedActorRef actorRef,
        long                  nowTicks)
    {
        // ExpireAtTicks 作用：
        // 保存当前 Actor 的实际到期时间。
        // 它由 Projection 系统内部维护，不再写入 IPooledActor。
        actorRef.ExpireAtTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                actorRef.KeepAliveTicks);

        // NextTouchTicks 作用：
        // 控制下一次允许真实 Touch 的时间。
        actorRef.NextTouchTicks =
            nowTicks + actorRef.TouchIntervalTicks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldDeferActorWorldAccess(
        World world,
        ActorWorld actorWorld)
    {
        return world.TryGetRuntime(out LayerRuntime? runtime) &&
               runtime != null &&
               ReferenceEquals(runtime.Actors, actorWorld) &&
               !runtime.IsOwnerThreadForActorWorld;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldDeferActorWorldAccess(ActorWorld actorWorld)
    {
        return false;
    }
}
