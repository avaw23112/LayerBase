namespace LayerBase.Actor;

internal static class EventMailPoolRuntime<TEvent>
    where TEvent : struct
{
    private static ActorWorld?[] s_worlds = new ActorWorld?[4];
    private static EventMailPool<TEvent>?[] s_mailPools = new EventMailPool<TEvent>?[4];

    public static void BindWorld(
        ActorWorld            world,
        EventMailPool<TEvent> mailPool)
    {
        int index = world.RuntimeIndex;
        EnsureCapacity(index);
        s_worlds[index] = world;
        s_mailPools[index] = mailPool;
    }

    public static bool TryGetMailPool(
        ActorWorld                 world,
        out EventMailPool<TEvent>? mailPool)
    {
        int index = world.RuntimeIndex;
        if ((uint)index < (uint)s_worlds.Length
            && ReferenceEquals(s_worlds[index], world))
        {
            mailPool = s_mailPools[index];
            return mailPool != null;
        }

        mailPool = null;
        return false;
    }

    private static void EnsureCapacity(int worldIndex)
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
        Array.Resize(ref s_mailPools, newSize);
    }
}