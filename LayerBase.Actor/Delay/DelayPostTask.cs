namespace LayerBase.Actor;

internal sealed class DelayPostTask<TEvent> : IActorDelayTask
    where TEvent : struct
{
    private readonly ActorWorld _world;
    private readonly ActorId _actorId;
    private readonly TEvent _value;

    public DelayPostTask(
        ActorWorld world,
        ActorId actorId,
        in TEvent value)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _actorId = actorId;
        _value = value;
    }

    public void Execute()
    {
        _ = _world.PostTo(_actorId, in _value);
    }

    public void Cancel()
    {
    }
}
