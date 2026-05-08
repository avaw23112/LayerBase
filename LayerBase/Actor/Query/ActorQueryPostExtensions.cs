namespace LayerBase.Actor;

public static class ActorQueryPostExtensions
{
    public static void PostAll<TEvent>(
        this ActorQueryResult query,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent1 : struct
        where TEvent2 : struct
    {
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
    {
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
    {
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, in value4, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
    {
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, in value4, in value5, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
    {
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
    {
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
    {
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, in value8, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
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
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, in value8, in value9, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9,
        in TEvent10 value10,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
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
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, in value8, in value9, in value10, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9,
        in TEvent10 value10,
        in TEvent11 value11,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
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
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, in value8, in value9, in value10, in value11, postPolicy, fullPolicy);
        }
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TEvent12>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9,
        in TEvent10 value10,
        in TEvent11 value11,
        in TEvent12 value12,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
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
        foreach (var archetype in query.Cache.Archetypes)
        {
            archetype.PostToAliveActors(
                in value1,
                in value2,
                in value3,
                in value4,
                in value5,
                in value6,
                in value7,
                in value8,
                in value9,
                in value10,
                in value11,
                in value12,
                postPolicy,
                fullPolicy);
        }
    }
}