namespace LayerBase.Actor;

internal sealed class DelayPostTask<TEvent> : IActorDelayTask
    where TEvent : struct
{
    private readonly ActorWorld _world;
    private readonly ActorId _actorId;
    private readonly TEvent _value;
    private readonly ActorPostPolicy? _postPolicy;
    private readonly ActorMailFullPolicy? _fullPolicy;

    public DelayPostTask(
        ActorWorld world,
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _actorId = actorId;
        _value = value;
        _postPolicy = postPolicy;
        _fullPolicy = fullPolicy;
    }

    public void Execute()
    {
        _ = _world.PostTo(_actorId, in _value, _postPolicy, _fullPolicy);
    }

    public void Cancel()
    {
    }
}
