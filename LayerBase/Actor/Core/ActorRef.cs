using LayerBase.Core.Event;

namespace LayerBase.Actor;

public readonly struct ActorRef<TActor>
    where TActor : class, IActor
{
    private readonly TypedActorStorage<TActor>? _storage;
    private readonly int _slotIndex;
    private readonly int _generation;

    internal ActorRef(
        TypedActorStorage<TActor>? storage,
        int slotIndex,
        int generation)
    {
        _storage = storage;
        _slotIndex = slotIndex;
        _generation = generation;
    }

    public bool IsAlive => _storage != null && _storage.IsAlive(_slotIndex, _generation);

    public PostResult Post<TEvent>(in TEvent value)
        where TEvent : struct
    {
        if (_storage == null || !_storage.IsAlive(_slotIndex, _generation))
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "ActorRef target is not alive.",
                PostFailureKind.InvalidActorId);
        }

        return _storage.Post(_slotIndex, in value, null, null);
    }

    public ActorEventRef<TActor, TEvent> Bind<TEvent>()
        where TEvent : struct
    {
        if (_storage == null || !_storage.TryGetColumn(out EventColumn<TActor, TEvent>? column))
        {
            return default;
        }

        return new ActorEventRef<TActor, TEvent>(column, _storage, _slotIndex, _generation);
    }
}
