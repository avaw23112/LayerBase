using Arch.Core;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.ECS.Projection;

internal enum ProjectedActorScopeCommandKind : byte
{
    Ensure = 1,
    Enable = 2,
    Disable = 3,
    Release = 4
}

internal readonly struct ActorProjectionCommandBatchScopeEvent
{
    public ActorProjectionCommandBatchScopeEvent(ProjectedActorScopeCommand command)
    {
        Command = command;
        Count = 1;
    }

    public ProjectedActorScopeCommand Command { get; }

    public int Count { get; }
}

internal readonly struct ProjectedActorScopeCommand
{
    private ProjectedActorScopeCommand(
        ProjectedActorScopeCommandKind kind,
        int originScopeId,
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy,
        long nowTicks)
    {
        Kind = kind;
        OriginScopeId = originScopeId;
        Entity = entity;
        ActorTypeId = actorTypeId;
        ActorId = actorId;
        ReleasePolicy = releasePolicy;
        NowTicks = nowTicks;
    }

    public ProjectedActorScopeCommandKind Kind { get; }

    public int OriginScopeId { get; }

    public Entity Entity { get; }

    public int ActorTypeId { get; }

    public ActorId ActorId { get; }

    public ProjectedActorReleasePolicy ReleasePolicy { get; }

    public long NowTicks { get; }

    public static ProjectedActorScopeCommand Ensure(
        int originScopeId,
        Entity entity,
        int actorTypeId,
        long nowTicks)
    {
        return new ProjectedActorScopeCommand(
            ProjectedActorScopeCommandKind.Ensure,
            originScopeId,
            entity,
            actorTypeId,
            ActorId.Invalid,
            ProjectedActorReleasePolicy.ReturnToPool,
            nowTicks);
    }

    public static ProjectedActorScopeCommand Enable(int originScopeId, ActorId actorId)
    {
        return new ProjectedActorScopeCommand(
            ProjectedActorScopeCommandKind.Enable,
            originScopeId,
            default,
            -1,
            actorId,
            ProjectedActorReleasePolicy.ReturnToPool,
            0);
    }

    public static ProjectedActorScopeCommand Disable(int originScopeId, ActorId actorId)
    {
        return new ProjectedActorScopeCommand(
            ProjectedActorScopeCommandKind.Disable,
            originScopeId,
            default,
            -1,
            actorId,
            ProjectedActorReleasePolicy.ReturnToPool,
            0);
    }

    public static ProjectedActorScopeCommand Release(
        int originScopeId,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy)
    {
        return new ProjectedActorScopeCommand(
            ProjectedActorScopeCommandKind.Release,
            originScopeId,
            default,
            -1,
            actorId,
            releasePolicy,
            0);
    }
}

internal readonly struct ActorProjectionResultBatchScopeEvent
{
    public ActorProjectionResultBatchScopeEvent(ProjectedActorScopeResult result)
    {
        Result = result;
        Count = 1;
    }

    public ProjectedActorScopeResult Result { get; }

    public int Count { get; }
}

internal readonly struct ProjectedActorScopeResult
{
    public ProjectedActorScopeResult(
        ProjectedActorScopeCommandKind kind,
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        bool success,
        long nowTicks)
    {
        Kind = kind;
        Entity = entity;
        ActorTypeId = actorTypeId;
        ActorId = actorId;
        Success = success;
        NowTicks = nowTicks;
    }

    public ProjectedActorScopeCommandKind Kind { get; }

    public Entity Entity { get; }

    public int ActorTypeId { get; }

    public ActorId ActorId { get; }

    public bool Success { get; }

    public long NowTicks { get; }
}

internal static class ActorProjectionScopeEventDispatcher
{
    public static bool TryDispatchCommand(
        int routeId,
        ScopeRuntime runtime,
        ActorWorld actorWorld,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (routeId != EventTypeId<ActorProjectionCommandBatchScopeEvent>.Id)
            return false;

        if (!payloadStorage.TryGet<ActorProjectionCommandBatchScopeEvent>(runtimeId, payload, out var batch))
            return true;

        ProjectedActorScopeCommand command = batch.Command;
        ProjectedActorScopeResult result = Execute(actorWorld, command);
        runtime.TryPostEventToScope(
            command.OriginScopeId,
            new ActorProjectionResultBatchScopeEvent(result));
        return true;
    }

    public static bool TryDispatchResult(
        int routeId,
        World world,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (routeId != EventTypeId<ActorProjectionResultBatchScopeEvent>.Id)
            return false;

        if (payloadStorage.TryGet<ActorProjectionResultBatchScopeEvent>(runtimeId, payload, out var batch))
        {
            ProjectedActorScopeResult result = batch.Result;
            world.ApplyProjectedActorResult(in result);
        }

        return true;
    }

    private static ProjectedActorScopeResult Execute(
        ActorWorld actorWorld,
        ProjectedActorScopeCommand command)
    {
        switch (command.Kind)
        {
            case ProjectedActorScopeCommandKind.Ensure:
                ProjectedActorHandle handle =
                    ProjectedActorTypeRegistry.CreateActorByTypeId(actorWorld, command.ActorTypeId);
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    handle.ActorId,
                    handle.IsValid,
                    command.NowTicks);

            case ProjectedActorScopeCommandKind.Enable:
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    command.ActorId,
                    actorWorld.EnableProjectedActorIfDisabled(command.ActorId),
                    command.NowTicks);

            case ProjectedActorScopeCommandKind.Disable:
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    command.ActorId,
                    actorWorld.DisableProjectedActor(command.ActorId),
                    command.NowTicks);

            case ProjectedActorScopeCommandKind.Release:
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    command.ActorId,
                    actorWorld.ReleaseProjectedActor(command.ActorId, command.ReleasePolicy),
                    command.NowTicks);

            default:
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    command.ActorId,
                    success: false,
                    command.NowTicks);
        }
    }
}
