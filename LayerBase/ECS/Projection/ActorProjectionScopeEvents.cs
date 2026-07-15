using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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

internal enum ProjectedActorScopeResultCode : byte
{
    Applied = 1,
    ActorMissing = 2,
    CreateFailed = 3
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

    public static ProjectedActorScopeCommand Enable(
        int originScopeId,
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        long nowTicks)
    {
        return new ProjectedActorScopeCommand(
            ProjectedActorScopeCommandKind.Enable,
            originScopeId,
            entity,
            actorTypeId,
            actorId,
            ProjectedActorReleasePolicy.ReturnToPool,
            nowTicks);
    }

    public static ProjectedActorScopeCommand Disable(
        int originScopeId,
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        long nowTicks)
    {
        return new ProjectedActorScopeCommand(
            ProjectedActorScopeCommandKind.Disable,
            originScopeId,
            entity,
            actorTypeId,
            actorId,
            ProjectedActorReleasePolicy.ReturnToPool,
            nowTicks);
    }

    public static ProjectedActorScopeCommand Release(
        int originScopeId,
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy,
        long nowTicks)
    {
        return new ProjectedActorScopeCommand(
            ProjectedActorScopeCommandKind.Release,
            originScopeId,
            entity,
            actorTypeId,
            actorId,
            releasePolicy,
            nowTicks);
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

internal readonly struct ActorPostBatchScopeEvent<TEvent>
    where TEvent : struct
{
    public ActorPostBatchScopeEvent(
        ActorId[] actorIds,
        TEvent[] events,
        int count)
    {
        ActorIds = actorIds ?? throw new ArgumentNullException(nameof(actorIds));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Count = count;
    }

    public ActorId[] ActorIds { get; }

    public TEvent[] Events { get; }

    public int Count { get; }

    public void PostTo(ActorWorld actorWorld)
    {
        int i = 0;
        int length = Count;
        int unrolledLength = length - (length % 4);

        for (; i < unrolledLength; i += 4)
        {
            actorWorld.PostTo(ActorIds[i], in Events[i]);
            actorWorld.PostTo(ActorIds[i + 1], in Events[i + 1]);
            actorWorld.PostTo(ActorIds[i + 2], in Events[i + 2]);
            actorWorld.PostTo(ActorIds[i + 3], in Events[i + 3]);
        }

        for (; i < length; i++)
            actorWorld.PostTo(ActorIds[i], in Events[i]);
    }

    public void Dispose()
    {
        if (ActorIds.Length > 0)
            ArrayPool<ActorId>.Shared.Return(ActorIds, clearArray: false);
        if (Events.Length > 0)
            ArrayPool<TEvent>.Shared.Return(Events, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>());
    }
}

internal readonly struct ProjectedActorScopeResult
{
    public ProjectedActorScopeResult(
        ProjectedActorScopeCommandKind kind,
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        ProjectedActorScopeResultCode code,
        long nowTicks)
    {
        Kind = kind;
        Entity = entity;
        ActorTypeId = actorTypeId;
        ActorId = actorId;
        Code = code;
        NowTicks = nowTicks;
    }

    public ProjectedActorScopeCommandKind Kind { get; }

    public Entity Entity { get; }

    public int ActorTypeId { get; }

    public ActorId ActorId { get; }

    public ProjectedActorScopeResultCode Code { get; }

    public bool Success => Code == ProjectedActorScopeResultCode.Applied;

    public long NowTicks { get; }
}

internal interface IActorProjectionPostBatchDispatcher
{
    void Dispatch(
        ActorWorld actorWorld,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage);
}

internal sealed class ActorProjectionPostBatchDispatcher<TEvent> : IActorProjectionPostBatchDispatcher
    where TEvent : struct
{
    public void Dispatch(
        ActorWorld actorWorld,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (!payloadStorage.TryGet<ActorPostBatchScopeEvent<TEvent>>(runtimeId, payload, out var batch))
            return;

        try
        {
            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }
}

internal static class ActorProjectionScopeEventDispatcher
{
    private static readonly ConcurrentDictionary<int, IActorProjectionPostBatchDispatcher> s_postBatchDispatchers = new();

    public static void EnsurePostBatchRegistered<TEvent>()
        where TEvent : struct
    {
        int routeId = EventTypeId<ActorPostBatchScopeEvent<TEvent>>.Id;
        s_postBatchDispatchers.GetOrAdd(routeId, static _ => new ActorProjectionPostBatchDispatcher<TEvent>());
    }

    public static bool TryDispatchCommand(
        int routeId,
        ScopeRuntime runtime,
        ActorWorld actorWorld,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (routeId != EventTypeId<ActorProjectionCommandBatchScopeEvent>.Id)
            return TryDispatchPostBatch(routeId, actorWorld, runtimeId, payload, payloadStorage);

        if (!payloadStorage.TryGet<ActorProjectionCommandBatchScopeEvent>(runtimeId, payload, out var batch))
            return true;

        ProjectedActorScopeCommand command = batch.Command;
        ProjectedActorScopeResult result = Execute(actorWorld, command);
        runtime.TryPostEventToScope(
            command.OriginScopeId,
            new ActorProjectionResultBatchScopeEvent(result));
        return true;
    }

    private static bool TryDispatchPostBatch(
        int routeId,
        ActorWorld actorWorld,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (!s_postBatchDispatchers.TryGetValue(routeId, out IActorProjectionPostBatchDispatcher? dispatcher))
            return false;

        dispatcher.Dispatch(actorWorld, runtimeId, payload, payloadStorage);
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
                    handle.IsValid
                        ? ProjectedActorScopeResultCode.Applied
                        : ProjectedActorScopeResultCode.CreateFailed,
                    command.NowTicks);

            case ProjectedActorScopeCommandKind.Enable:
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    command.ActorId,
                    actorWorld.EnableProjectedActorIfDisabled(command.ActorId)
                        ? ProjectedActorScopeResultCode.Applied
                        : ProjectedActorScopeResultCode.ActorMissing,
                    command.NowTicks);

            case ProjectedActorScopeCommandKind.Disable:
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    command.ActorId,
                    actorWorld.DisableProjectedActor(command.ActorId)
                        ? ProjectedActorScopeResultCode.Applied
                        : ProjectedActorScopeResultCode.ActorMissing,
                    command.NowTicks);

            case ProjectedActorScopeCommandKind.Release:
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    command.ActorId,
                    actorWorld.ReleaseProjectedActor(command.ActorId, command.ReleasePolicy)
                        ? ProjectedActorScopeResultCode.Applied
                        : ProjectedActorScopeResultCode.ActorMissing,
                    command.NowTicks);

            default:
                return new ProjectedActorScopeResult(
                    command.Kind,
                    command.Entity,
                    command.ActorTypeId,
                    command.ActorId,
                    ProjectedActorScopeResultCode.ActorMissing,
                    command.NowTicks);
        }
    }
}
