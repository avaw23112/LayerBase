namespace LayerBase.Actor;

internal static class ActorEventRuntime<TEvent>
    where TEvent : struct
{
    private static ActorWorld?[] s_worlds = new ActorWorld?[4];
    private static ActorEventFastCache<TEvent>?[] s_fastCaches = new ActorEventFastCache<TEvent>?[4];
    private static EventMailPool<TEvent>?[] s_mailPools = new EventMailPool<TEvent>?[4];

    public static void BindWorld(
        ActorWorld world,
        ActorEventFastCache<TEvent> fastCache,
        EventMailPool<TEvent> mailPool)
    {
        int index = world.RuntimeIndex;
        EnsureCapacity(index);
        s_worlds[index] = world;
        s_fastCaches[index] = fastCache;
        s_mailPools[index] = mailPool;
    }

    public static bool TryGetFastCache(
        ActorWorld world,
        out ActorEventFastCache<TEvent>? fastCache)
    {
        int index = world.RuntimeIndex;
        if ((uint)index < (uint)s_worlds.Length
            && ReferenceEquals(s_worlds[index], world))
        {
            fastCache = s_fastCaches[index];
            return fastCache != null;
        }

        fastCache = null;
        return false;
    }

    public static bool TryGetMailPool(
        ActorWorld world,
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

    public static ActorEventFastCache<TEvent> GetFastCache(ActorWorld world)
    {
        return s_fastCaches[world.RuntimeIndex]!;
    }

    public static EventMailPool<TEvent> GetMailPool(ActorWorld world)
    {
        return s_mailPools[world.RuntimeIndex]!;
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
        Array.Resize(ref s_fastCaches, newSize);
        Array.Resize(ref s_mailPools, newSize);
    }
}
