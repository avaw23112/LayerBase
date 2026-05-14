namespace LayerBase.Actor;

internal static class ActorWorldRuntimeIndexAllocator
{
    private static int s_nextIndex;
    private static readonly Stack<int> s_free = new();

#if DEBUG
    private static readonly HashSet<int> s_rented = new();
#endif

    public static int Rent()
    {
        int index = s_free.Count > 0
            ? s_free.Pop()
            : s_nextIndex++;
        return index;
    }

    public static void Return(int index)
    {
        s_free.Push(index);
    }
}
