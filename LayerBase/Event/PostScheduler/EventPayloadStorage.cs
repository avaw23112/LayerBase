using System.Runtime.CompilerServices;
using LayerBase.Core.DataStruct;

namespace LayerBase.Core.Event;

internal interface IEventStore : IDisposable
{
    void Release(PayloadHandle handle);
    void Dispatch(PayloadHandle handle, EventCenter center);
    void DispatchDefault(EventCenter center);
}

internal sealed class EventStore<T> : IEventStore where T : struct
{
    private static int s_nextStoreId;

    private readonly int _storeId;
    private T[] _buffer;
    private int[] _versions;
    private int[] _nextFree;
    private int _freeHead = -1;
    private int _capacity;
    private bool _disposed;

    public EventStore(int initialCapacity = 256, int storeId = 0)
    {
        _storeId = storeId != 0 ? storeId : Interlocked.Increment(ref s_nextStoreId);
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
        if (_disposed) throw new ObjectDisposedException(nameof(EventStore<T>));
        if (_freeHead == -1) Grow();

        int index = _freeHead;
        _freeHead = _nextFree[index];

        _buffer[index] = value;
        int version = _versions[index];

        return new PayloadHandle(EventTypeId<T>.Id, index, version, _storeId);
    }

    public bool TryGet(PayloadHandle handle, out T value)
    {
        if (_disposed ||
            handle.StoreId != _storeId ||
            handle.Index < 0 ||
            handle.Index >= _capacity ||
            _versions[handle.Index] != handle.Version)
        {
            value = default;
            return false;
        }

        value = _buffer[handle.Index];
        return true;
    }

    public ref T GetRef(PayloadHandle handle)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(EventStore<T>));
        if (handle.StoreId != _storeId ||
            handle.Index < 0 ||
            handle.Index >= _capacity ||
            _versions[handle.Index] != handle.Version)
            throw new InvalidOperationException("Invalid payload handle");

        return ref _buffer[handle.Index];
    }

    public void Release(PayloadHandle handle)
    {
        if (_disposed ||
            handle.StoreId != _storeId ||
            handle.Index < 0 ||
            handle.Index >= _capacity ||
            _versions[handle.Index] != handle.Version) return;

        _buffer[handle.Index] = default;
        _versions[handle.Index]++;
        if (_versions[handle.Index] == 0) _versions[handle.Index] = 1;

        _nextFree[handle.Index] = _freeHead;
        _freeHead = handle.Index;
    }

    public void Dispatch(PayloadHandle handle, EventCenter center)
    {
        if (TryGet(handle, out var value))
        {
            center.Send(value);
        }
    }

    public void DispatchDefault(EventCenter center)
    {
        if (_disposed) return;
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

internal sealed class EventPayloadStorage : IDisposable
{
    private static int s_nextStorageId;

    private readonly int _storageId = Interlocked.Increment(ref s_nextStorageId);
    private IEventStore?[] _typeIdStores = new IEventStore[256];
    private bool _disposed;

    public PayloadHandle Store<T>(int runtimeId, in T payload) where T : struct
    {
        var store = GetStoreFast<T>(runtimeId);
        return store.Add(in payload);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventStore<T> GetStoreFast<T>(int runtimeId) where T : struct
    {
        if (_disposed) throw new ObjectDisposedException(nameof(EventPayloadStorage));

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
        var store = CreateStoreLocal<T>();
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
    private EventStore<T> CreateStoreLocal<T>() where T : struct
    {
        return new EventStore<T>(storeId: _storageId);
    }

    public ref T GetRef<T>(int runtimeId, PayloadHandle handle) where T : struct
    {
        var store = GetStoreFast<T>(runtimeId);
        return ref store.GetRef(handle);
    }

    public void EnsureStore<T>(int runtimeId) where T : struct
    {
        GetStoreFast<T>(runtimeId);
    }

    public void Release(PayloadHandle handle)
    {
        if (handle.IsInvalid) return;
        var store = GetStoreByTypeId(handle.EventTypeId);
        store?.Release(handle);
    }

    public void Dispatch(PayloadHandle handle, EventCenter center)
    {
        if (handle.IsInvalid) return;
        var store = GetStoreByTypeId(handle.EventTypeId);
        if (store == null)
        {
            LayerHub.ReportWarning(0, "DEBUG", "POST", $"Store not found for typeId {handle.EventTypeId}");
            return;
        }

        store.Dispatch(handle, center);
    }

    public void DispatchDefault(int eventTypeId, EventCenter center)
    {
        var store = GetStoreByTypeId(eventTypeId);
        if (store == null)
        {
            LayerHub.ReportWarning(0, "DEBUG", "", $"Store not found for typeId {eventTypeId}");
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
        if (_disposed) return;
        _disposed = true;

        for (var i = 0; i < _typeIdStores.Length; i++)
        {
            _typeIdStores[i]?.Dispose();
            _typeIdStores[i] = null;
        }
    }
}
