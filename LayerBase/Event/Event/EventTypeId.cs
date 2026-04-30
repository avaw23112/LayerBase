namespace LayerBase.Core.Event;

internal static class EventTypeIdProvider
{
    private static int s_nextId;
    private static readonly Dictionary<Type, int> s_typeToId = new();
    private static readonly Dictionary<int, Type> s_idToType = new();
    private static readonly object s_lock = new();

    public static int GetOrCreateId(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        lock (s_lock)
        {
            if (s_typeToId.TryGetValue(type, out var id)) return id;
            id = Interlocked.Increment(ref s_nextId);
            s_typeToId[type] = id;
            s_idToType[id] = type;
            return id;
        }
    }

    public static Type? GetType(int id)
    {
        lock (s_lock)
        {
            if (s_idToType.TryGetValue(id, out var type)) return type;
        }

        return null;
    }
}

internal class EventTypeId<Value>
{
    public static readonly int Id = EventTypeIdProvider.GetOrCreateId(typeof(Value));
}

internal class EventTypeId
{
    public static int GetId(Type type)
    {
        return EventTypeIdProvider.GetOrCreateId(type);
    }

    public static Type? GetType(int id)
    {
        return EventTypeIdProvider.GetType(id);
    }
}