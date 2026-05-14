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
        lock (s_free)
        {
            int index = s_free.Count > 0
                ? s_free.Pop()
                : s_nextIndex++;

#if DEBUG
            if (!s_rented.Add(index))
            {
                throw new InvalidOperationException(
                    $"ActorWorld runtime index {index} was rented twice.");
            }
#endif

            return index;
        }
    }

    public static void Return(int index)
    {
        lock (s_free)
        {
#if DEBUG
            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Cannot return negative ActorWorld runtime index {index}.");
            }

            if (!s_rented.Remove(index))
            {
                throw new InvalidOperationException(
                    $"ActorWorld runtime index {index} was returned but is not currently rented.");
            }
#endif

            s_free.Push(index);
        }
    }
}
