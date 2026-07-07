namespace LayerBase.Actor;

public delegate void ActorForEachAction<TActor, TState>(
    TActor     actor,
    ref TState state)
    where TActor : class, IActor, new();

public delegate void ActorStorageForEachAction<TActor, TState>(
    TActor?[]        actors,
    ActorSlotState[] states,
    bool[]           enabled,
    int              maxSlot,
    ref TState       state)
    where TActor : class, IActor, new();

public static class ActorQueryForEachExtensions
{
    public static void ForEachActor<TActor>(
        this ActorQueryResult query,
        Action<TActor>        action)
        where TActor : class, IActor, new()
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        query = query.RefreshIfNeeded();
        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            archetype.ForEachActor(action);
        }
    }

    public static void ForEachActor<TActor, TState>(
        this ActorQueryResult              query,
        ref  TState                        state,
        ActorForEachAction<TActor, TState> action)
        where TActor : class, IActor, new()
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        query = query.RefreshIfNeeded();
        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            archetype.ForEachActor(ref state, action);
        }
    }

    public static void ForEachStorage<TActor, TState>(
        this ActorQueryResult                     query,
        ref  TState                               state,
        ActorStorageForEachAction<TActor, TState> action)
        where TActor : class, IActor, new()
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        query = query.RefreshIfNeeded();
        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            archetype.ForEachStorage(ref state, action);
        }
    }
}