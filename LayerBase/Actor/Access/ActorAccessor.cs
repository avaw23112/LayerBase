using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.Actor;

public readonly struct ActorAccessor
{
    private readonly LocalActorAccessor _local;
    private readonly RemoteActorAccessor _remote;

    internal ActorAccessor(LocalActorAccessor local)
    {
        _local = local;
        _remote = default;
        IsLocal = true;
    }

    internal ActorAccessor(RemoteActorAccessor remote)
    {
        _local = default;
        _remote = remote;
        IsLocal = false;
    }

    public bool IsLocal { get; }

    public LocalActorAccessor Local => IsLocal
        ? _local
        : throw new InvalidOperationException("Actor accessor is remote.");

    public RemoteActorAccessor Remote => !IsLocal
        ? _remote
        : throw new InvalidOperationException("Actor accessor is local.");

    public TActor CreateActor<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        return Local.CreateActor<TActor>(usePool);
    }

    public ActorHandle CreateActorHandle<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        return Local.CreateActorHandle<TActor>(usePool);
    }

    public void PostTo<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        PostTo(ActorHandle.FromActorId(actorId, RuntimeGeneration), in value);
    }

    public void PostTo<TEvent>(ActorHandle target, in TEvent value)
        where TEvent : struct
    {
        if (IsLocal)
        {
            _local.PostTo(target, in value);
            return;
        }

        _remote.PostTo(target, in value);
    }

    public void PostToMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value)
        where TEvent : struct
    {
        if (IsLocal)
        {
            _local.PostToMany(actorIds, in value);
            return;
        }

        for (int i = 0; i < actorIds.Length; i++)
            _remote.PostTo(ActorHandle.FromActorId(actorIds[i], RuntimeGeneration), in value);
    }

    public LBTask<TResponse> Ask<TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return Ask<TRequest, TResponse>(
            ActorHandle.FromActorId(actorId, RuntimeGeneration),
            in request,
            cancellationToken);
    }

    public LBTask<TResponse> Ask<TRequest, TResponse>(
        ActorHandle target,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return IsLocal
            ? _local.Ask<TRequest, TResponse>(target, in request, cancellationToken)
            : _remote.Ask<TRequest, TResponse>(target, in request, cancellationToken);
    }

    public bool DestroyActor(ActorId actorId)
    {
        return DestroyActor(ActorHandle.FromActorId(actorId, RuntimeGeneration));
    }

    public bool DestroyActor(ActorHandle target)
    {
        return IsLocal
            ? _local.DestroyActor(target)
            : _remote.DestroyActor(target);
    }

    private int RuntimeGeneration => IsLocal
        ? _local.RuntimeGeneration
        : _remote.RuntimeGeneration;
}

public readonly struct LocalActorAccessor
{
    private readonly ActorWorld? _world;

    internal LocalActorAccessor(ActorWorld world, int runtimeGeneration)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        RuntimeGeneration = runtimeGeneration;
    }

    public int RuntimeGeneration { get; }

    public ActorWorld World => _world ?? throw new InvalidOperationException("Local actor accessor is not initialized.");

    public TActor CreateActor<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        return World.CreateActor<TActor>(usePool);
    }

    public ActorHandle CreateActorHandle<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        TActor actor = CreateActor<TActor>(usePool);
        return ActorHandle.FromActorId(actor.GetActorId(), RuntimeGeneration);
    }

    public void PostTo<TEvent>(ActorHandle target, in TEvent value)
        where TEvent : struct
    {
        World.PostTo(target.ActorId, in value);
    }

    public void PostToMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value)
        where TEvent : struct
    {
        World.PostToMany(actorIds, in value);
    }

    public LBTask<TResponse> Ask<TRequest, TResponse>(
        ActorHandle target,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return World.Ask<TRequest, TResponse>(target.ActorId, in request, cancellationToken);
    }

    public bool DestroyActor(ActorHandle target)
    {
        return World.DestroyActor(target.ActorId);
    }
}

public readonly struct RemoteActorAccessor
{
    private readonly ScopeRef<MainScope> _mainScope;
    private readonly int _originScopeId;
    private readonly bool _hasEndpoint;

    internal RemoteActorAccessor(ScopeRef<MainScope> mainScope, int originScopeId, int runtimeGeneration)
    {
        _mainScope = mainScope;
        _originScopeId = originScopeId;
        RuntimeGeneration = runtimeGeneration;
        _hasEndpoint = true;
    }

    public int RuntimeGeneration { get; }

    public TActor CreateActor<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        throw new InvalidOperationException("Remote scopes cannot create or access actor objects directly.");
    }

    public ActorHandle CreateActorHandle<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        throw new InvalidOperationException("Remote scopes cannot create or access actor objects directly.");
    }

    public ScopePostResult Post<TEvent>(ActorHandle target, in TEvent value)
        where TEvent : struct
    {
        EnsureEndpoint();
        ActorCommandDispatcherRegistry.EnsurePostRegistered<TEvent>();
        var batch = new ActorCommandBatch<TEvent>(_originScopeId, target, in value);
        return _mainScope.PostInternal(
            EventTypeId<ActorCommandBatch<TEvent>>.Id,
            ScopeEventClass.Internal,
            in batch);
    }

    public void PostTo<TEvent>(ActorHandle target, in TEvent value)
        where TEvent : struct
    {
        ScopePostResult result = Post(target, in value);
        if (!result.IsAccepted)
            throw new InvalidOperationException($"Remote actor post failed: {result.Status}.");
    }

    public LBTask<TResponse> Ask<TRequest, TResponse>(
        ActorHandle target,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        EnsureEndpoint();
        ActorCallDispatcherRegistry.EnsureRegistered<TRequest, TResponse>();
        var actorRequest = new ActorCallRequest<TRequest, TResponse>(_originScopeId, target, in request);
        return _mainScope.CallInternal<ActorCallRequest<TRequest, TResponse>, TResponse>(
            ScopeLocalCallRouteId<ActorCallRequest<TRequest, TResponse>, TResponse>.Id,
            ScopeCallClass.BusinessRequest,
            in actorRequest,
            cancellationToken);
    }

    public bool DestroyActor(ActorHandle target)
    {
        EnsureEndpoint();
        ActorCommandDispatcherRegistry.EnsureDestroyRegistered();
        var command = new ActorDestroyCommand(_originScopeId, target);
        return _mainScope.PostInternal(
            EventTypeId<ActorDestroyCommand>.Id,
            ScopeEventClass.Internal,
            in command).IsAccepted;
    }

    private void EnsureEndpoint()
    {
        if (!_hasEndpoint)
            throw new InvalidOperationException("Remote actor accessor is not initialized.");
    }
}
