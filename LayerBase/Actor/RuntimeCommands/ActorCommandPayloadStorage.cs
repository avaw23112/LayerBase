using System;
using System.Collections.Generic;

namespace LayerBase.Actor.RuntimeCommands;

internal sealed class ActorCommandPayloadStorage
{
    private readonly Dictionary<int, object> _store = new();
    private int _nextHandle;
    private readonly object _gate = new();

    public int Store<T>(T payload)
    {
        int handle = System.Threading.Interlocked.Increment(ref _nextHandle);
        lock (_gate)
        {
            _store[handle] = payload!;
        }
        return handle;
    }

    public T Retrieve<T>(int handle)
    {
        lock (_gate)
        {
            if (_store.TryGetValue(handle, out object? value))
            {
                return (T)value;
            }
            throw new InvalidOperationException($"Payload handle {handle} not found.");
        }
    }

    public void Free(int handle)
    {
        lock (_gate)
        {
            _store.Remove(handle);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _store.Clear();
        }
    }

    public int Count
    {
        get
        {
            lock (_gate) return _store.Count;
        }
    }
}