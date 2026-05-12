namespace LayerBase.Actor;

public static class ActorQueryPostExtensions
{
    public static void PostAll<TEvent>(
        this ActorQueryResult query,
        in   TEvent           value)
        where TEvent : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value);
    }

    public static void PostAll<TEvent1, TEvent2>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2)
        where TEvent1 : struct
        where TEvent2 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4,
        in   TEvent5          value5)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
        PostAllSingle(query, in value5);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4,
        in   TEvent5          value5,
        in   TEvent6          value6)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
        PostAllSingle(query, in value5);
        PostAllSingle(query, in value6);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4,
        in   TEvent5          value5,
        in   TEvent6          value6,
        in   TEvent7          value7)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
        PostAllSingle(query, in value5);
        PostAllSingle(query, in value6);
        PostAllSingle(query, in value7);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4,
        in   TEvent5          value5,
        in   TEvent6          value6,
        in   TEvent7          value7,
        in   TEvent8          value8)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
        PostAllSingle(query, in value5);
        PostAllSingle(query, in value6);
        PostAllSingle(query, in value7);
        PostAllSingle(query, in value8);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4,
        in   TEvent5          value5,
        in   TEvent6          value6,
        in   TEvent7          value7,
        in   TEvent8          value8,
        in   TEvent9          value9)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
        where TEvent9 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
        PostAllSingle(query, in value5);
        PostAllSingle(query, in value6);
        PostAllSingle(query, in value7);
        PostAllSingle(query, in value8);
        PostAllSingle(query, in value9);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9,
        TEvent10>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4,
        in   TEvent5          value5,
        in   TEvent6          value6,
        in   TEvent7          value7,
        in   TEvent8          value8,
        in   TEvent9          value9,
        in   TEvent10         value10)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
        where TEvent9 : struct
        where TEvent10 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
        PostAllSingle(query, in value5);
        PostAllSingle(query, in value6);
        PostAllSingle(query, in value7);
        PostAllSingle(query, in value8);
        PostAllSingle(query, in value9);
        PostAllSingle(query, in value10);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9,
        TEvent10, TEvent11>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4,
        in   TEvent5          value5,
        in   TEvent6          value6,
        in   TEvent7          value7,
        in   TEvent8          value8,
        in   TEvent9          value9,
        in   TEvent10         value10,
        in   TEvent11         value11)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
        where TEvent9 : struct
        where TEvent10 : struct
        where TEvent11 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
        PostAllSingle(query, in value5);
        PostAllSingle(query, in value6);
        PostAllSingle(query, in value7);
        PostAllSingle(query, in value8);
        PostAllSingle(query, in value9);
        PostAllSingle(query, in value10);
        PostAllSingle(query, in value11);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9,
        TEvent10, TEvent11, TEvent12>(
        this ActorQueryResult query,
        in   TEvent1          value1,
        in   TEvent2          value2,
        in   TEvent3          value3,
        in   TEvent4          value4,
        in   TEvent5          value5,
        in   TEvent6          value6,
        in   TEvent7          value7,
        in   TEvent8          value8,
        in   TEvent9          value9,
        in   TEvent10         value10,
        in   TEvent11         value11,
        in   TEvent12         value12)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
        where TEvent9 : struct
        where TEvent10 : struct
        where TEvent11 : struct
        where TEvent12 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
        PostAllSingle(query, in value3);
        PostAllSingle(query, in value4);
        PostAllSingle(query, in value5);
        PostAllSingle(query, in value6);
        PostAllSingle(query, in value7);
        PostAllSingle(query, in value8);
        PostAllSingle(query, in value9);
        PostAllSingle(query, in value10);
        PostAllSingle(query, in value11);
        PostAllSingle(query, in value12);
    }

    private static void PostAllSingle<TEvent>(ActorQueryResult query, in TEvent value)
        where TEvent : struct
    {
        EventPostState<TEvent>? state =
            EventPostRuntime<TEvent>.GetStateUnchecked(query.World.RuntimeIndex);

        if (state == null)
        {
            return;
        }

        ActorPostRouteCode routeCode = state.RouteCode;

        if (routeCode == ActorPostRouteCode.Disabled)
        {
            return;
        }

        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            archetype.PostAll(
                query.World,
                state,
                routeCode,
                in value);
        }
    }

    private static void PostAllQueuedByRouteCode<TEvent>(
        ActorQueryResult       query,
        in TEvent              value,
        EventPostState<TEvent> state,
        ActorPostRouteCode     routeCode)
        where TEvent : struct
    {
        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            archetype.PostAll(query.World, state, routeCode, in value);
        }
    }

    private static void PostAllNonQueuedByRouteCode<TEvent>(
        ActorQueryResult       query,
        in TEvent              value,
        EventPostState<TEvent> state,
        ActorPostRouteCode     routeCode)
        where TEvent : struct
    {
        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            archetype.PostAll(query.World, state, routeCode, in value);
        }
    }
}