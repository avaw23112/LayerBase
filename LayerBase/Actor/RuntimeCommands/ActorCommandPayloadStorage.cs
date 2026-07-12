using System;
using System.Collections.Generic;

namespace LayerBase.Actor.RuntimeCommands;

internal static class ActorCommandPayloadStorage
{
    private static readonly Dictionary<int, object> _store = new();
    private static int _nextHandle;

    public static int Store<T>(T payload)
    {
        int handle = System.Threading.Interlocked.Increment(ref _nextHandle);
        lock (_store)
        {
            _store[handle] = payload!;
        }
        return handle;
    }

    public static T Retrieve<T>(int handle)
    {
        lock (_store)
        {
            if (_store.TryGetValue(handle, out object? value))
            {
                return (T)value;
            }
            throw new InvalidOperationException($"Payload handle {handle} not found.");
        }
    }

    public static void Free(int handle)
    {
        lock (_store)
        {
            _store.Remove(handle);
        }
    }

    public static void Clear()
    {
        lock (_store)
        {
            _store.Clear();
        }
    }
}
