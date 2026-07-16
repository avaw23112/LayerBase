using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorBinding
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta,
        long nowTicks)
    {
        ProjectedActorEnsureResult result = world.ProjectedActorCommands.Ensure(entity, meta.ActorTypeId, nowTicks);
        if (result.Accepted)
        {
            meta.EnsurePending = true;
            return ActorId.Invalid;
        }

        return result.ActorId;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        ProjectedActorEnsureResult result = world.ProjectedActorCommands.Ensure(entity, meta.ActorTypeId, nowTicks);
        if (result.Accepted)
        {
            meta.EnsurePending = true;
            return ActorId.Invalid;
        }

        if (!result.IsValid)
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        ProjectedActorBindingUtility.Bind(ref meta, ref actorRef, result.ActorId, nowTicks);
        world.AddActiveProjectedActor(entity, ref meta);
        return result.ActorId;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World world,
        Entity entity,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        ProjectedActorEnsureResult result = world.ProjectedActorCommands.Ensure(entity, actorRef.ActorTypeId, nowTicks);
        if (result.Accepted)
        {
            ref ProjectedActorMeta pendingMeta = ref world.GetProjectionMeta(entity);
            pendingMeta.EnsurePending = true;
            return ActorId.Invalid;
        }

        if (!result.IsValid)
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        actorRef.Bind(result.ActorId, nowTicks);

        ref ProjectedActorMeta meta = ref world.GetProjectionMeta(entity);
        meta.BindActor(result.ActorId);
        world.AddActiveProjectedActor(entity, ref meta);
        return result.ActorId;
    }

    [Obsolete("Use RefreshProjectedActorInterest instead. This method no longer refreshes ExpireAtTicks.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TouchProjectedActor(
        ref ProjectedActorMeta meta,
        long nowTicks)
    {
        if (!meta.ActorId.IsValid)
            return;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TouchProjectedActor(
        World world,
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        ActorId actorId = actorRef.ActorId;

        if (!actorId.IsValid)
        {
            ProjectedActorBindingUtility.Clear(ref meta, ref actorRef);
            return false;
        }

        if (!world.ProjectedActorCommands.Exists(actorId))
        {
            ProjectedActorBindingUtility.Clear(ref meta, ref actorRef);
            return false;
        }

        RefreshDeadline(ref actorRef, nowTicks);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TouchProjectedActor(
        World world,
        Entity entity,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        ActorId actorId = actorRef.ActorId;

        if (!actorId.IsValid)
        {
            ClearByEntity(world, entity, ref actorRef);
            return false;
        }

        if (!world.ProjectedActorCommands.Exists(actorId))
        {
            ClearByEntity(world, entity, ref actorRef);
            return false;
        }

        RefreshDeadline(ref actorRef, nowTicks);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearByEntity(
        World world,
        Entity entity,
        ref ProjectedActorRef actorRef)
    {
        actorRef.ClearActor();

        ref ProjectedActorMeta meta = ref world.GetProjectionMeta(entity);
        meta.ClearActor();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RefreshProjectedActorInterest(
        World world,
        Entity entity,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        ActorId actorId = actorRef.ActorId;

        if (!actorId.IsValid)
        {
            ref ProjectedActorMeta meta = ref world.GetProjectionMeta(entity);
            if (meta.EnsurePending)
                return false;

            actorId = EnsureProjectedActor(
                world,
                entity,
                ref actorRef,
                nowTicks);

            return actorId.IsValid;
        }

        ref ProjectedActorMeta actorMeta = ref world.GetProjectionMeta(entity);
        if (actorMeta.EnablePending)
            return false;

        if (world.ProjectedActorCommands.IsDisabled(actorId))
        {
            if (!world.ProjectedActorCommands.EnableIfDisabled(entity, actorRef.ActorTypeId, actorId, nowTicks))
            {
                ClearByEntity(world, entity, ref actorRef);
                return false;
            }

            actorMeta.EnablePending = true;

            RefreshDeadline(ref actorRef, nowTicks);
            return true;
        }

        if (nowTicks < actorRef.NextTouchTicks)
            return true;

        RefreshDeadline(ref actorRef, nowTicks);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RefreshDeadline(
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        actorRef.ExpireAtTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                actorRef.KeepAliveTicks);

        actorRef.NextTouchTicks =
            nowTicks + actorRef.TouchIntervalTicks;
    }
}
