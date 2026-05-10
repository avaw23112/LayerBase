using LayerBase.Core.Event;

namespace LayerBase.Actor;

public static class ActorQueryPostExtensions
{
    public static void PostAll<TEvent>(
        this ActorQueryResult query,
        in TEvent value)
        where TEvent : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value);
    }

    public static void PostAll<TEvent1, TEvent2>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2)
        where TEvent1 : struct
        where TEvent2 : struct
    {
        query = query.RefreshIfNeeded();
        PostAllSingle(query, in value1);
        PostAllSingle(query, in value2);
    }

    public static void PostAll<TEvent1, TEvent2, TEvent3>(
        this ActorQueryResult query,
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3)
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
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4)
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
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5)
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
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6)
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
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7)
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
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8)
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
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9)
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
        in TEvent10 value10)
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
        in TEvent11 value11)
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
        in TEvent12 value12)
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
        EventPostState<TEvent>? state = EventPostRuntime<TEvent>.GetState(query.World);
        if (state == null)
        {
            return;
        }

        if (state.Route == ActorPostRouteKind.DiagnosticOnly)
        {
            return;
        }

        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            int archetypeId = archetype.ArchetypeId;
            if ((uint)archetypeId >= (uint)state.RowsByArchetype.Length)
            {
                continue;
            }

            EventPostRow<TEvent> row = state.RowsByArchetype[archetypeId];
            if (!row.IsValid)
            {
                continue;
            }

            int maxSlot = Math.Min(row.Mails.Length, row.SlotFlags.Length);
            for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
            {
                if (!query.World.CanPostSlot(row, state, slotIndex))
                {
                    continue;
                }

                _ = state.Route switch
                {
                    ActorPostRouteKind.QueuedGrow => query.World.PostQueuedGrowCore(
                        slotIndex,
                        in value,
                        row.Mails,
                        row.DirtySlots,
                        row.BucketIndex,
                        state.Pool,
                        state.Options),
                    ActorPostRouteKind.QueuedRejectNew => query.World.PostQueuedRejectNewCore(
                        slotIndex,
                        in value,
                        row.Mails,
                        row.DirtySlots,
                        row.BucketIndex,
                        state.Pool,
                        state.Options),
                    ActorPostRouteKind.QueuedDropOldest => query.World.PostQueuedDropOldestCore(
                        slotIndex,
                        in value,
                        row.Mails,
                        row.DirtySlots,
                        row.BucketIndex,
                        state.Pool,
                        state.Options),
                    ActorPostRouteKind.Latest => query.World.PostLatestCore(
                        slotIndex,
                        in value,
                        row.Mails,
                        row.DirtySlots,
                        row.BucketIndex,
                        state.Pool),
                    ActorPostRouteKind.Dirty => query.World.PostDirtyCore(
                        slotIndex,
                        in value,
                        row.Mails,
                        row.DirtySlots,
                        row.BucketIndex,
                        state.Pool),
                    _ => PostResult.Success
                };
            }
        }
    }
}
