using Arch.Core;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Scope;

namespace LayerBase.ECS.Projection;

internal interface IProjectedActorCommandSink
{
    ProjectedActorEnsureResult Ensure(Entity entity, int actorTypeId, long nowTicks);

    bool Exists(ActorId actorId);

    bool IsDisabled(ActorId actorId);

    bool EnableIfDisabled(Entity entity, int actorTypeId, ActorId actorId, long nowTicks);

    bool Disable(Entity entity, int actorTypeId, ActorId actorId, long nowTicks);

    bool Release(
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy,
        long nowTicks);

    void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct;

    void PostBatch<TEvent>(ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct;
}

internal readonly struct ProjectedActorEnsureResult
{
    public ProjectedActorEnsureResult(ActorId actorId, bool accepted = true)
    {
        ActorId = actorId;
        Accepted = accepted;
    }

    public ActorId ActorId { get; }

    public bool Accepted { get; }

    public bool IsValid => ActorId.IsValid;

    public static ProjectedActorEnsureResult Invalid => new(ActorId.Invalid, accepted: false);

    public static ProjectedActorEnsureResult Pending(bool accepted)
    {
        return new ProjectedActorEnsureResult(ActorId.Invalid, accepted);
    }
}

internal sealed class ScopeEventProjectedActorCommandSink : IProjectedActorCommandSink
{
    private readonly ScopeRef<MainScope> _mainScope;
    private readonly int _originScopeId;
    private readonly int _runtimeGeneration;

    public ScopeEventProjectedActorCommandSink(
        ScopeRef<MainScope> mainScope,
        int originScopeId,
        int runtimeGeneration)
    {
        _mainScope = mainScope;
        _originScopeId = originScopeId;
        _runtimeGeneration = runtimeGeneration;
    }

    public ProjectedActorEnsureResult Ensure(Entity entity, int actorTypeId, long nowTicks)
    {
        var command = ProjectedActorScopeCommand.Ensure(
            _originScopeId,
            entity,
            actorTypeId,
            nowTicks);
        ScopePostResult result = PostInternal(new ActorProjectionCommandBatchScopeEvent(command));
        return ProjectedActorEnsureResult.Pending(result.IsAccepted);
    }

    public bool Exists(ActorId actorId) => actorId.IsValid;

    public bool IsDisabled(ActorId actorId) => false;

    public bool EnableIfDisabled(Entity entity, int actorTypeId, ActorId actorId, long nowTicks)
    {
        var command = ProjectedActorScopeCommand.Enable(_originScopeId, entity, actorTypeId, actorId, nowTicks);
        return PostInternal(new ActorProjectionCommandBatchScopeEvent(command)).IsAccepted;
    }

    public bool Disable(Entity entity, int actorTypeId, ActorId actorId, long nowTicks)
    {
        var command = ProjectedActorScopeCommand.Disable(_originScopeId, entity, actorTypeId, actorId, nowTicks);
        return PostInternal(new ActorProjectionCommandBatchScopeEvent(command)).IsAccepted;
    }

    public bool Release(
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy,
        long nowTicks)
    {
        var command = ProjectedActorScopeCommand.Release(_originScopeId, entity, actorTypeId, actorId, releasePolicy, nowTicks);
        return PostInternal(new ActorProjectionCommandBatchScopeEvent(command)).IsAccepted;
    }

    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        ActorCommandDispatcherRegistry.EnsurePostRegistered<TEvent>();
        var handle = ActorHandle.FromActorId(actorId, _runtimeGeneration);
        var batch = new ActorCommandBatch<TEvent>(_originScopeId, handle, in value);
        _mainScope.PostInternal(
            EventTypeId<ActorCommandBatch<TEvent>>.Id,
            ScopeEventClass.Internal,
            in batch);
    }

    public void PostBatch<TEvent>(ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        if (batch.Count == 0)
            return;

        ActorProjectionScopeEventDispatcher.EnsurePostBatchRegistered<TEvent>();
        ActorPostBatchScopeEvent<TEvent> value = batch.DetachToScopeEvent();
        ScopePostResult result = _mainScope.PostInternal(
            EventTypeId<ActorPostBatchScopeEvent<TEvent>>.Id,
            ScopeEventClass.Internal,
            in value);
        if (!result.IsAccepted)
            value.Dispose();
    }

    private ScopePostResult PostInternal(in ActorProjectionCommandBatchScopeEvent value)
    {
        return _mainScope.PostInternal(
            EventTypeId<ActorProjectionCommandBatchScopeEvent>.Id,
            ScopeEventClass.Internal,
            in value);
    }
}

internal sealed class RejectingProjectedActorCommandSink : IProjectedActorCommandSink
{
    public static readonly RejectingProjectedActorCommandSink Instance = new();

    private RejectingProjectedActorCommandSink()
    {
    }

    public ProjectedActorEnsureResult Ensure(Entity entity, int actorTypeId, long nowTicks) => ProjectedActorEnsureResult.Invalid;

    public bool Exists(ActorId actorId) => false;

    public bool IsDisabled(ActorId actorId) => false;

    public bool EnableIfDisabled(Entity entity, int actorTypeId, ActorId actorId, long nowTicks) => false;

    public bool Disable(Entity entity, int actorTypeId, ActorId actorId, long nowTicks) => false;

    public bool Release(
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy,
        long nowTicks) => false;

    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
    }

    public void PostBatch<TEvent>(ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
    }
}
