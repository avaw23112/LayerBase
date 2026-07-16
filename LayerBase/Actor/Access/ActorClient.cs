using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.Actor;

public readonly struct ActorFactory
{
    private readonly ActorWorld? _world;

    internal ActorFactory(ActorWorld world, int runtimeGeneration)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        RuntimeGeneration = runtimeGeneration;
    }

    public int RuntimeGeneration { get; }

    private ActorWorld World =>
        _world ?? throw new InvalidOperationException("Actor factory is not initialized.");

    public TActor Create<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        return World.CreateActor<TActor>(usePool);
    }

    public ActorHandle CreateHandle<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        TActor actor = Create<TActor>(usePool);
        return ActorHandle.FromActorId(actor.GetActorId(), RuntimeGeneration);
    }
}

public readonly struct ActorClient
{
    private readonly ActorWorld? _world;
    private readonly ScopeRef<MainScope> _mainScope;
    private readonly int _originScopeId;
    private readonly bool _isMainScope;
    private readonly bool _isInitialized;

    internal ActorClient(ActorWorld world, int runtimeGeneration)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _mainScope = default;
        _originScopeId = ScopeDefinitionIds.Main;
        RuntimeGeneration = runtimeGeneration;
        _isMainScope = true;
        _isInitialized = true;
    }

    internal ActorClient(ScopeRef<MainScope> mainScope, int originScopeId, int runtimeGeneration)
    {
        _world = null;
        _mainScope = mainScope;
        _originScopeId = originScopeId;
        RuntimeGeneration = runtimeGeneration;
        _isMainScope = false;
        _isInitialized = true;
    }

    public int RuntimeGeneration { get; }

    public ScopePostResult Post<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        return Post(ActorHandle.FromActorId(actorId, RuntimeGeneration), in value);
    }

    public ScopePostResult Post<TEvent>(ActorHandle target, in TEvent value)
        where TEvent : struct
    {
        EnsureInitialized();
        if (_isMainScope)
        {
            LocalWorld.PostTo(target.ActorId, in value);
            return ScopePostResult.Accepted;
        }

        ActorCommandDispatcherRegistry.EnsurePostRegistered<TEvent>();
        var batch = new ActorCommandBatch<TEvent>(_originScopeId, target, in value);
        return _mainScope.PostInternal(
            EventTypeId<ActorCommandBatch<TEvent>>.Id,
            ScopeEventClass.Internal,
            in batch);
    }

    public void PostMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value)
        where TEvent : struct
    {
        EnsureInitialized();
        if (_isMainScope)
        {
            LocalWorld.PostToMany(actorIds, in value);
            return;
        }

        for (int i = 0; i < actorIds.Length; i++)
        {
            ScopePostResult result = Post(ActorHandle.FromActorId(actorIds[i], RuntimeGeneration), in value);
            if (!result.IsAccepted)
                throw new InvalidOperationException($"Actor post failed: {result.Status}.");
        }
    }

    public LBTask<TResponse> Call<TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return Call<TRequest, TResponse>(
            ActorHandle.FromActorId(actorId, RuntimeGeneration),
            in request,
            cancellationToken);
    }

    public LBTask<TResponse> Call<TRequest, TResponse>(
        ActorHandle target,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        EnsureInitialized();
        if (_isMainScope)
            return LocalWorld.Ask<TRequest, TResponse>(target.ActorId, in request, cancellationToken);

        ActorCallDispatcherRegistry.EnsureRegistered<TRequest, TResponse>();
        var actorRequest = new ActorCallRequest<TRequest, TResponse>(_originScopeId, target, in request);
        return _mainScope.CallInternal<ActorCallRequest<TRequest, TResponse>, TResponse>(
            ScopeLocalCallRouteId<ActorCallRequest<TRequest, TResponse>, TResponse>.Id,
            ScopeCallClass.BusinessRequest,
            in actorRequest,
            cancellationToken);
    }

    public bool Destroy(ActorId actorId)
    {
        return Destroy(ActorHandle.FromActorId(actorId, RuntimeGeneration));
    }

    public bool Destroy(ActorHandle target)
    {
        EnsureInitialized();
        if (_isMainScope)
            return LocalWorld.DestroyActor(target.ActorId);

        ActorCommandDispatcherRegistry.EnsureDestroyRegistered();
        var command = new ActorDestroyCommand(_originScopeId, target);
        return _mainScope.PostInternal(
            EventTypeId<ActorDestroyCommand>.Id,
            ScopeEventClass.Internal,
            in command).IsAccepted;
    }

    private ActorWorld LocalWorld =>
        _world ?? throw new InvalidOperationException("Actor client is not bound to the main actor world.");

    private void EnsureInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Actor client is not initialized.");
    }
}
