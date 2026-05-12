namespace LayerBase.Actor;

internal static class ActorGroupIdAllocator
{
    private static readonly Dictionary<Type, int> s_typeToId = new();
    private static readonly object s_lock = new();
    private static int s_nextId;

    public static int GetOrCreate(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        lock (s_lock)
        {
            if (s_typeToId.TryGetValue(type, out int existing))
            {
                return existing;
            }

            int id = Interlocked.Increment(ref s_nextId);
            s_typeToId.Add(type, id);
            return id;
        }
    }
}