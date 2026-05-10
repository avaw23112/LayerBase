namespace LayerBase.Actor;

internal static class EventPostRuntime<TEvent>
    where TEvent : struct
{
    private static ActorWorld?[] s_worlds = new ActorWorld?[4];
    private static EventPostRow<TEvent>[]?[] s_rowsByWorld = new EventPostRow<TEvent>[4][];

    public static void BindWorld(ActorWorld world, EventPostRow<TEvent>[] rows)
    {
        int worldIndex = world.RuntimeIndex;
        EnsureWorldCapacity(worldIndex);
        s_worlds[worldIndex] = world;
        s_rowsByWorld[worldIndex] = rows;
    }

    public static bool TryGetRows(ActorWorld world, out EventPostRow<TEvent>[]? rows)
    {
        int worldIndex = world.RuntimeIndex;
        if ((uint)worldIndex < (uint)s_worlds.Length
            && ReferenceEquals(s_worlds[worldIndex], world))
        {
            rows = s_rowsByWorld[worldIndex];
            return rows != null;
        }

        rows = null;
        return false;
    }

    private static void EnsureWorldCapacity(int worldIndex)
    {
        if ((uint)worldIndex < (uint)s_worlds.Length)
        {
            return;
        }

        int newSize = s_worlds.Length;
        while (newSize <= worldIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref s_worlds, newSize);
        Array.Resize(ref s_rowsByWorld, newSize);
    }
}
