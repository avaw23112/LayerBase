using System;
using System.Collections.Generic;

namespace LayerBase.Actor.RuntimeCommands;

internal sealed class ActorCommandPayloadStorage
{
    private readonly Dictionary<int, PayloadEntry> _store = new();
    private int _nextHandle;
    private readonly object _gate = new();

    public int Store<T>(T payload)
    {
        lock (_gate)
        {
            int handle = AllocateHandleNoLock();
            _store.Add(handle, new PayloadEntry(payload, typeof(T)));
            return handle;
        }
    }

    public T Retrieve<T>(int handle)
    {
        lock (_gate)
        {
            if (_store.TryGetValue(handle, out PayloadEntry entry))
            {
                if (entry.Payload is T typed)
                {
                    return typed;
                }

                if (entry.Payload is null && CanAssignNull(typeof(T)))
                {
                    return default!;
                }

                throw new InvalidOperationException(
                    $"Payload handle {handle} contains {entry.PayloadType.FullName}, not {typeof(T).FullName}.");
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

    private int AllocateHandleNoLock()
    {
        for (int attempts = 0; attempts < int.MaxValue; attempts++)
        {
            unchecked
            {
                _nextHandle++;
            }

            if (_nextHandle == 0)
            {
                _nextHandle = 1;
            }

            if (!_store.ContainsKey(_nextHandle))
            {
                return _nextHandle;
            }
        }

        throw new InvalidOperationException("Actor payload handle space is exhausted.");
    }

    private static bool CanAssignNull(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    public int Count
    {
        get
        {
            lock (_gate) return _store.Count;
        }
    }

    private readonly struct PayloadEntry
    {
        public readonly object? Payload;
        public readonly Type PayloadType;

        public PayloadEntry(object? payload, Type payloadType)
        {
            Payload = payload;
            PayloadType = payloadType;
        }
    }
}
