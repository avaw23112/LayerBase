using LayerBase.Actor;
using Arch.Core;
using LayerBase.Scope;

namespace LayerBase.ECS.Projection;

internal interface IProjectedActorCommandSink
{
    ProjectedActorEnsureResult Ensure(Entity entity, int actorTypeId, long nowTicks);

    bool Exists(ActorId actorId);

    bool IsDisabled(ActorId actorId);

    bool EnableIfDisabled(ActorId actorId);

    bool Disable(ActorId actorId);

    bool Release(ActorId actorId, ProjectedActorReleasePolicy releasePolicy);

    void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct;
}

internal readonly struct ProjectedActorEnsureResult
{
    public ProjectedActorEnsureResult(ActorId actorId)
    {
        ActorId = actorId;
    }

    public ActorId ActorId { get; }

    public bool IsValid => ActorId.IsValid;

    public static ProjectedActorEnsureResult Invalid => new(ActorId.Invalid);
}

internal sealed class MainScopeProjectedActorCommandSink : IProjectedActorCommandSink
{
    private readonly ActorWorld _actorWorld;

    public MainScopeProjectedActorCommandSink(ActorWorld actorWorld)
    {
        _actorWorld = actorWorld ?? throw new ArgumentNullException(nameof(actorWorld));
    }

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

    public bool EnableIfDisabled(ActorId actorId)
    {
        return _actorWorld.EnableProjectedActorIfDisabled(actorId);
    }

    public bool Disable(ActorId actorId)
    {
        return _actorWorld.DisableProjectedActor(actorId);
    }

    public bool Release(ActorId actorId, ProjectedActorReleasePolicy releasePolicy)
    {
        return _actorWorld.ReleaseProjectedActor(actorId, releasePolicy);
    }

    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        _actorWorld.PostTo(actorId, in value);
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
        _mainScope.Post(new ActorProjectionCommandBatchScopeEvent(command));
        return ProjectedActorEnsureResult.Invalid;
    }

    public bool Exists(ActorId actorId) => actorId.IsValid;

    public bool IsDisabled(ActorId actorId) => false;

    public bool EnableIfDisabled(ActorId actorId)
    {
        var command = ProjectedActorScopeCommand.Enable(_originScopeId, actorId);
        return _mainScope.Post(new ActorProjectionCommandBatchScopeEvent(command)).IsAccepted;
    }

    public bool Disable(ActorId actorId)
    {
        var command = ProjectedActorScopeCommand.Disable(_originScopeId, actorId);
        return _mainScope.Post(new ActorProjectionCommandBatchScopeEvent(command)).IsAccepted;
    }

    public bool Release(ActorId actorId, ProjectedActorReleasePolicy releasePolicy)
    {
        var command = ProjectedActorScopeCommand.Release(_originScopeId, actorId, releasePolicy);
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

    public bool EnableIfDisabled(ActorId actorId) => false;

    public bool Disable(ActorId actorId) => false;

    public bool Release(ActorId actorId, ProjectedActorReleasePolicy releasePolicy) => false;

    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
    }
}
