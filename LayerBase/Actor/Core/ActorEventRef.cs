using LayerBase.Core.Event;

namespace LayerBase.Actor;

public readonly struct ActorEventRef<TActor, TEvent>
    where TActor : class, IActor
    where TEvent : struct
{
    private readonly EventColumn<TActor, TEvent>? _column;
    private readonly TypedActorStorage<TActor>? _storage;
    private readonly int _slotIndex;
    private readonly int _generation;

    internal ActorEventRef(
        EventColumn<TActor, TEvent>? column,
        TypedActorStorage<TActor>? storage,
        int slotIndex,
        int generation)
    {
        _column = column;
        _storage = storage;
        _slotIndex = slotIndex;
        _generation = generation;
    }

    public bool IsAlive => _storage != null && _storage.IsAlive(_slotIndex, _generation);

    public PostResult Post(in TEvent value)
    {
        if (_storage == null || _column == null || !_storage.IsAlive(_slotIndex, _generation))
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "ActorEventRef target is not alive.",
                PostFailureKind.InvalidActorId);
        }

        return _column.PostQueuedFast(_slotIndex, in value);
    }
}
