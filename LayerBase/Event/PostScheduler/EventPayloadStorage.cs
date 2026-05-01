using System.Collections.Concurrent;

namespace LayerBase.Core.Event;

internal interface IEventStore : IDisposable
{
    void Release(int index, int version);
    void Dispatch(int index, int version, EventCenter center);
    void DispatchDefault(EventCenter center);
}

internal sealed class EventStore<T> : IEventStore where T : struct
{
    private T[] _buffer;
    private int[] _versions;
    private int[] _nextFree;
    private int _freeHead = -1;
    private int _capacity;
    private readonly object _lock = new();

    public EventStore(int initialCapacity = 256)
    {
        _capacity = initialCapacity;
        _buffer = new T[initialCapacity];
        _versions = new int[initialCapacity];
        _nextFree = new int[initialCapacity];
        
        for (int i = 0; i < initialCapacity; i++)
        {
            _versions[i] = 1;
            _nextFree[i] = i + 1;
        }
        _nextFree[initialCapacity - 1] = -1;
        _freeHead = 0;
    }

    public PayloadHandle Add(in T value)
    {
        lock (_lock)
        {
            if (_freeHead == -1) Grow();
            
            int index = _freeHead;
            _freeHead = _nextFree[index];
            
            _buffer[index] = value;
            int version = _versions[index];
            
            return new PayloadHandle(EventTypeId<T>.Id, index, version);
        }
    }

    public bool TryGet(PayloadHandle handle, out T value)
    {
        lock (_lock)
        {
            if (handle.Index < 0 || handle.Index >= _capacity || _versions[handle.Index] != handle.Version)
            {
                value = default;
                return false;
            }
            
            value = _buffer[handle.Index];
            return true;
        }
    }

    public ref T GetRef(int index, int version)
    {
        // Internal use, assuming lock is held by caller if needed or handled by design.
        // Actually, PostScheduler uses _bufferLock for coalescing.
        if (index < 0 || index >= _capacity || _versions[index] != version)
            throw new InvalidOperationException("Invalid payload handle");
        
        return ref _buffer[index];
    }

    public void Release(int index, int version)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _capacity || _versions[index] != version) return;
            
            _buffer[index] = default;
            _versions[index]++;
            if (_versions[index] == 0) _versions[index] = 1;
            
            _nextFree[index] = _freeHead;
            _freeHead = index;
        }
    }

    public void Dispatch(int index, int version, EventCenter center)
    {
        if (TryGet(new PayloadHandle(EventTypeId<T>.Id, index, version), out var value))
        {
            center.Send(value);
        }
    }

    public void DispatchDefault(EventCenter center)
    {
        center.Send(default(T));
    }

    private void Grow()
    {
        int oldCapacity = _capacity;
        int newCapacity = oldCapacity * 2;
        
        Array.Resize(ref _buffer, newCapacity);
        Array.Resize(ref _versions, newCapacity);
        Array.Resize(ref _nextFree, newCapacity);
        
        for (int i = oldCapacity; i < newCapacity; i++)
        {
            _versions[i] = 1;
            _nextFree[i] = i + 1;
        }
        _nextFree[newCapacity - 1] = -1;
        _freeHead = oldCapacity;
        _capacity = newCapacity;
    }

    public void Dispose()
    {
    }
}

internal sealed class EventPayloadStorage : IDisposable
{
    private readonly ConcurrentDictionary<int, IEventStore> _stores = new();
    
    public PayloadHandle Store<T>(in T payload) where T : struct
    {
        var typeId = EventTypeId<T>.Id;
        var store = (EventStore<T>)_stores.GetOrAdd(typeId, _ => new EventStore<T>());
        return store.Add(in payload);
    }

    public ref T GetRef<T>(PayloadHandle handle) where T : struct
    {
        if (_stores.TryGetValue(handle.EventTypeId, out var store))
        {
            return ref ((EventStore<T>)store).GetRef(handle.Index, handle.Version);
        }
        throw new InvalidOperationException("Store not found");
    }

    public void EnsureStore<T>() where T : struct
    {
        _stores.GetOrAdd(EventTypeId<T>.Id, _ => new EventStore<T>());
    }
    
    public void Release(PayloadHandle handle)
    {
        if (handle.IsInvalid) return;
        if (_stores.TryGetValue(handle.EventTypeId, out var store))
        {
            store.Release(handle.Index, handle.Version);
        }
    }

    public void Dispatch(PayloadHandle handle, EventCenter center)
    {
        if (handle.IsInvalid) return;
        if (_stores.TryGetValue(handle.EventTypeId, out var store))
        {
            store.Dispatch(handle.Index, handle.Version, center);
        }
    }

    public void DispatchDefault(int eventTypeId, EventCenter center)
    {
        if (_stores.TryGetValue(eventTypeId, out var store))
        {
            store.DispatchDefault(center);
        }
    }

    public void Dispose()
    {
        foreach (var store in _stores.Values)
        {
            store.Dispose();
        }
        _stores.Clear();
    }
}
