using LayerBase.Actor;
using Arch.Core;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Scope;

namespace LayerBase.ECS.Projection;

internal interface IProjectedActorCommandSink
{
    bool CompletesSynchronously { get; }

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
    public ProjectedActorEnsureResult(
        ActorId actorId,
        bool accepted = true,
        bool completedSynchronously = true)
    {
        ActorId = actorId;
        Accepted = accepted;
        CompletedSynchronously = completedSynchronously;
    }

    public ActorId ActorId { get; }

    public bool Accepted { get; }

    public bool CompletedSynchronously { get; }

    public bool IsValid => ActorId.IsValid;

    public static ProjectedActorEnsureResult Invalid => new(ActorId.Invalid, accepted: false, completedSynchronously: false);

    public static ProjectedActorEnsureResult Pending(bool accepted)
    {
        return new ProjectedActorEnsureResult(ActorId.Invalid, accepted, completedSynchronously: false);
    }
}

internal sealed class MainScopeProjectedActorCommandSink : IProjectedActorCommandSink
{
    private readonly ActorWorld _actorWorld;

    public MainScopeProjectedActorCommandSink(ActorWorld actorWorld)
    {
        _actorWorld = actorWorld ?? throw new ArgumentNullException(nameof(actorWorld));
    }

    public bool CompletesSynchronously => true;

    public ProjectedActorEnsureResult Ensure(Entity entity, int actorTypeId, long nowTicks)
    {
        ProjectedActorHandle handle =
            ProjectedActorTypeRegistry.CreateActorByTypeId(_actorWorld, actorTypeId);

        return handle.IsValid
            ? new ProjectedActorEnsureResult(handle.ActorId)
            : ProjectedActorEnsureResult.Invalid;
    }

    public bool Exists(ActorId actorId)
    {
        return _actorWorld.TryGetPooledActor(actorId, out _);
    }

    public bool IsDisabled(ActorId actorId)
    {
        return _actorWorld.IsProjectedActorDisabled(actorId);
    }

    public bool EnableIfDisabled(Entity entity, int actorTypeId, ActorId actorId, long nowTicks)
    {
        return _actorWorld.EnableProjectedActorIfDisabled(actorId);
    }

    public bool Disable(Entity entity, int actorTypeId, ActorId actorId, long nowTicks)
    {
        return _actorWorld.DisableProjectedActor(actorId);
    }

    public bool Release(
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy,
        long nowTicks)
    {
        return _actorWorld.ReleaseProjectedActor(actorId, releasePolicy);
    }

    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        _actorWorld.PostTo(actorId, in value);
    }

    public void PostBatch<TEvent>(ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        batch.PostTo(this);
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

    public bool CompletesSynchronously => false;

    public ProjectedActorEnsureResult Ensure(Entity entity, int actorTypeId, long nowTicks)
    {
        var command = ProjectedActorScopeCommand.Ensure(
            _originScopeId,
            entity,
            actorTypeId,
            nowTicks);
        ScopePostResult result = _mainScope.Post(new ActorProjectionCommandBatchScopeEvent(command));
        return ProjectedActorEnsureResult.Pending(result.IsAccepted);
    }

    public bool Exists(ActorId actorId) => actorId.IsValid;

    public bool IsDisabled(ActorId actorId) => false;

    public bool EnableIfDisabled(Entity entity, int actorTypeId, ActorId actorId, long nowTicks)
    {
        var command = ProjectedActorScopeCommand.Enable(_originScopeId, entity, actorTypeId, actorId, nowTicks);
        return _mainScope.Post(new ActorProjectionCommandBatchScopeEvent(command)).IsAccepted;
    }

    public bool Disable(Entity entity, int actorTypeId, ActorId actorId, long nowTicks)
    {
        var command = ProjectedActorScopeCommand.Disable(_originScopeId, entity, actorTypeId, actorId, nowTicks);
        return _mainScope.Post(new ActorProjectionCommandBatchScopeEvent(command)).IsAccepted;
    }

    public bool Release(
        Entity entity,
        int actorTypeId,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy,
        long nowTicks)
    {
        var command = ProjectedActorScopeCommand.Release(_originScopeId, entity, actorTypeId, actorId, releasePolicy, nowTicks);
        return _mainScope.Post(new ActorProjectionCommandBatchScopeEvent(command)).IsAccepted;
    }

    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        ActorCommandDispatcherRegistry.EnsurePostRegistered<TEvent>();
        var handle = ActorHandle.FromActorId(actorId, _runtimeGeneration);
        var batch = new ActorCommandBatch<TEvent>(_originScopeId, handle, in value);
        _mainScope.Post(in batch);
    }

    public void PostBatch<TEvent>(ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        if (batch.Count == 0)
            return;

        ActorProjectionScopeEventDispatcher.EnsurePostBatchRegistered<TEvent>();
        ActorPostBatchScopeEvent<TEvent> value = batch.DetachToScopeEvent();
        ScopePostResult result = _mainScope.Post(in value);
        if (!result.IsAccepted)
            value.Dispose();
    }
}

internal sealed class RejectingProjectedActorCommandSink : IProjectedActorCommandSink
{
    public static readonly RejectingProjectedActorCommandSink Instance = new();

    private RejectingProjectedActorCommandSink()
    {
    }

    public bool CompletesSynchronously => false;

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
