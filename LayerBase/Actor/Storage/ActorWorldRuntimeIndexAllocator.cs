namespace LayerBase.Actor;

internal static class ActorWorldRuntimeIndexAllocator
{
    private static int s_nextIndex;
    private static readonly Stack<int> s_free = new();

    public static int Rent()
    {
        lock (s_free)
        {
            return s_free.Count > 0
                ? s_free.Pop()
                : s_nextIndex++;
        }
    }

    public static void Return(int index)
    {
        lock (s_free)
        {
            s_free.Push(index);
        }
    }
}