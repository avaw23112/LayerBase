using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

internal static class EventPostRuntime<TEvent>
    where TEvent : struct
{
    private static EventPostState<TEvent>?[] s_statesByWorld = new EventPostState<TEvent>?[4];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventPostState<TEvent>? GetState(ActorWorld world)
    {
        return GetStateUnchecked(world.RuntimeIndex);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventPostState<TEvent>? GetStateUnchecked(int worldIndex)
    {
        if ((uint)worldIndex >= (uint)s_statesByWorld.Length)
        {
            return null;
        }
        return s_statesByWorld[worldIndex];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static void BindWorld(ActorWorld world, EventPostState<TEvent> state)
    {
        int worldIndex = world.RuntimeIndex;
        EnsureWorldCapacity(worldIndex);
        s_statesByWorld[worldIndex] = state;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static void UnbindWorld(int worldIndex)
    {
        if ((uint)worldIndex < (uint)s_statesByWorld.Length)
        {
            s_statesByWorld[worldIndex] = null;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

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