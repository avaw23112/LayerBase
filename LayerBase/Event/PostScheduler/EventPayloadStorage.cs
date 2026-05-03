using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Core.DataStruct;

namespace LayerBase.Core.Event;

internal interface IEventStore : IDisposable
{
    void Release(int index, int version);
    void Dispatch(int index, int version, EventCenter center);
    void DispatchDefault(EventCenter center);
}

internal static class PayloadStoreCache<T> where T : struct
{
    public static readonly EventStore<T>?[] Stores = new EventStore<T>[1024];

    static PayloadStoreCache()
    {
        LayerHub.RegisterCacheResetter(Reset);
        LayerHub.RegisterRuntimeCacheResetter(ResetRuntime);
    }

    private static void Reset()
    {
        for (int i = 0; i < 1024; i++)
        {
            Stores[i]?.Dispose();
            Stores[i] = null;
        }
    }

    private static void ResetRuntime(int runtimeId)
    {
        if ((uint)runtimeId >= (uint)Stores.Length) return;

        Stores[runtimeId]?.Dispose();
        Stores[runtimeId] = null;
    }
}

internal sealed class EventStore<T> : IEventStore where T : struct
{
    private T[] _buffer;
    private int[] _versions;
    private int[] _nextFree;
    private int _freeHead = -1;
    private int _capacity;
    private readonly object _lock = new();
    private bool _disposed;

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
            if (_disposed) throw new ObjectDisposedException(nameof(EventStore<T>));
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
            if (_disposed || handle.Index < 0 || handle.Index >= _capacity || _versions[handle.Index] != handle.Version)
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
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EventStore<T>));
            if (index < 0 || index >= _capacity || _versions[index] != version)
                throw new InvalidOperationException("Invalid payload handle");

            return ref _buffer[index];
        }
    }

    public void Release(int index, int version)
    {
        lock (_lock)
        {
            if (_disposed || index < 0 || index >= _capacity || _versions[index] != version) return;
            
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
        lock (_lock)
        {
            if (_disposed) return;
        }
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
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;

            Array.Clear(_buffer, 0, _buffer.Length);
            Array.Clear(_versions, 0, _versions.Length);
            Array.Clear(_nextFree, 0, _nextFree.Length);

            _buffer = Array.Empty<T>();
            _versions = Array.Empty<int>();
            _nextFree = Array.Empty<int>();

            _freeHead = -1;
            _capacity = 0;
        }
    }
}

internal sealed class EventPayloadStorage : IDisposable
{
    private IEventStore?[] _typeIdStores = new IEventStore[256];

    public PayloadHandle Store<T>(int runtimeId, in T payload) where T : struct
    {
        var store = GetStoreFast<T>(runtimeId);
        return store.Add(in payload);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventStore<T> GetStoreFast<T>(int runtimeId) where T : struct
    {
        var typeId = EventTypeId<T>.Id;
        if ((uint)typeId < (uint)_typeIdStores.Length)
        {
            var s = _typeIdStores[typeId];
            if (s != null) return (EventStore<T>)s;
        }
        
        return GetStoreFastSlow<T>(runtimeId, typeId);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private EventStore<T> GetStoreFastSlow<T>(int runtimeId, int typeId) where T : struct
    {
        EventStore<T>? store = null;
        if ((uint)runtimeId < 1024)
        {
            store = PayloadStoreCache<T>.Stores[runtimeId];
        }

        if (store == null)
        {
            store = CreateStoreGlobal<T>(runtimeId);
        }

        RegisterStoreLocal(typeId, store);
        return store;
    }

    private void RegisterStoreLocal(int typeId, IEventStore store)
    {
        if (typeId >= _typeIdStores.Length)
        {
            Array.Resize(ref _typeIdStores, Math.Max(typeId + 1, _typeIdStores.Length * 2));
        }
        _typeIdStores[typeId] = store;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private EventStore<T> CreateStoreGlobal<T>(int runtimeId) where T : struct
    {
        lock (PayloadStoreCache<T>.Stores)
        {
            EventStore<T>? store = null;
            if ((uint)runtimeId < 1024)
            {
                store = PayloadStoreCache<T>.Stores[runtimeId];
                if (store != null) return store;

                store = new EventStore<T>();
                PayloadStoreCache<T>.Stores[runtimeId] = store;
            }
            else
            {
                store = new EventStore<T>();
            }
            return store;
        }
    }

    public ref T GetRef<T>(int runtimeId, PayloadHandle handle) where T : struct
    {
        var store = GetStoreFast<T>(runtimeId);
        return ref store.GetRef(handle.Index, handle.Version);
    }

    public void EnsureStore<T>(int runtimeId) where T : struct
    {
        GetStoreFast<T>(runtimeId);
    }
    
    public void Release(PayloadHandle handle)
    {
        if (handle.IsInvalid) return;
        var store = GetStoreByTypeId(handle.EventTypeId);
        store?.Release(handle.Index, handle.Version);
    }

    public void Dispatch(PayloadHandle handle, EventCenter center)
    {
        if (handle.IsInvalid) return;
        var store = GetStoreByTypeId(handle.EventTypeId);
        if (store == null)
        {
            LayerHub.ReportWarning(0,"DEBUG","POST",$"Store not found for typeId {handle.EventTypeId}");
            return;
        }
        store.Dispatch(handle.Index, handle.Version, center);
    }

    public void DispatchDefault(int eventTypeId, EventCenter center)
    {
        var store = GetStoreByTypeId(eventTypeId);
        if (store == null)
        {
            LayerHub.ReportWarning(0,"DEBUG","",$"Store not found for typeId {eventTypeId}");
            return;
        }
        store.DispatchDefault(center);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEventStore? GetStoreByTypeId(int typeId)
    {
        if ((uint)typeId < (uint)_typeIdStores.Length)
        {
            return _typeIdStores[typeId];
        }
        return null;
    }

    public void Dispose()
    {
        Array.Clear(_typeIdStores, 0, _typeIdStores.Length);
    }
}
