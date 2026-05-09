namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public ActorRef<TActor> GetActorRef<TActor>(ActorId actorId)
        where TActor : class, IActor
    {
        return TryGetActorRef(actorId, out ActorRef<TActor> actorRef)
            ? actorRef
            : default;
    }

    public bool TryGetActorRef<TActor>(ActorId actorId, out ActorRef<TActor> actorRef)
        where TActor : class, IActor
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            actorRef = default;
            return false;
        }

        BehaviourArchetype archetype = _archetypes[actorId.ArchetypeId];
        if (!archetype.TryGetStorage(actorId.TypeStorageIndex, out TypedActorStorage<TActor>? storage)
            || storage == null
            || !storage.IsAlive(actorId.SlotIndex, actorId.Generation))
        {
            actorRef = default;
            return false;
        }

        actorRef = new ActorRef<TActor>(storage, actorId.SlotIndex, actorId.Generation);
        return true;
    }

    public ActorEventRef<TActor, TEvent> GetActorEventRef<TActor, TEvent>(ActorId actorId)
        where TActor : class, IActor
        where TEvent : struct
    {
        if (!TryGetActorRef(actorId, out ActorRef<TActor> actorRef))
        {
            return default;
        }

        return actorRef.Bind<TEvent>();
    }
}
