namespace LayerBase.Actor;

internal static class EventPostRuntime<TEvent>
    where TEvent : struct
{
    private static EventPostState<TEvent>?[] s_statesByWorld = new EventPostState<TEvent>?[4];

    public static void BindWorld(ActorWorld world, EventPostState<TEvent> state)
    {
        int worldIndex = world.RuntimeIndex;
        EnsureWorldCapacity(worldIndex);
        s_statesByWorld[worldIndex] = state;
    }

    public static EventPostState<TEvent>? GetState(ActorWorld world)
    {
        int worldIndex = world.RuntimeIndex;
        if ((uint)worldIndex >= (uint)s_statesByWorld.Length)
        {
            return null;
        }

        return s_statesByWorld[worldIndex];
    }

    public static void UnbindWorld(int worldIndex)
    {
        if ((uint)worldIndex < (uint)s_statesByWorld.Length)
        {
            s_statesByWorld[worldIndex] = null;
        }
    }

    private static void EnsureWorldCapacity(int worldIndex)
    {
        if ((uint)worldIndex < (uint)s_statesByWorld.Length)
        {
            return;
        }

        int newSize = s_statesByWorld.Length;
        while (newSize <= worldIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref s_statesByWorld, newSize);
    }
}
