using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Core.UnmanagedList;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.Event;

public enum Propagation
{
    Global,
    Bubble,
    Drop
}

internal sealed class GlobalEventCenter
{
    private readonly ConcurrentDictionary<int, IEventBucket> _eventBuckets = new();
    private readonly object _lock = new();
    internal ulong[] _bubbleMasksArr = Array.Empty<ulong>();
    internal ulong[] _dropMasksArr = Array.Empty<ulong>();
    private long _eventPendingMask;
    private string[] _layerNames = Array.Empty<string>();
    private IEventQueue[] _layerSlots = Array.Empty<IEventQueue>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ulong GetEventPendingMask() => (ulong)Volatile.Read(ref _eventPendingMask);

    internal void EnsureSlots(int count, string name)
    {
        if (_layerSlots.Length < count || (count > 0 && _layerNames.Length < count))
            lock (_lock)
            {
                if (_layerSlots.Length < count)
                {
                    var newSlots = new IEventQueue[count];
                    Array.Copy(_layerSlots, newSlots, _layerSlots.Length);
                    for (var i = _layerSlots.Length; i < count; i++) newSlots[i] = new LayerEventQueue(this, i);
                    _layerSlots = newSlots;
                    var newBubble = new ulong[count];
                    var newDrop = new ulong[count];
                    for (var i = 0; i < count; i++)
                    {
                        newBubble[i] = (1UL << (i + 1)) - 1;
                        newDrop[i] = ~((1UL << i) - 1);
                    }
                    _bubbleMasksArr = newBubble;
                    _dropMasksArr = newDrop;
                }
                if (_layerNames.Length < count)
                {
                    var newNames = new string[count];
                    Array.Copy(_layerNames, newNames, _layerNames.Length);
                    for (var i = 0; i < _layerNames.Length; i++) if(newNames[i] == null) newNames[i] = "UnknownLayer";
                    _layerNames = newNames;
                }
            }
        if (count > 0) _layerNames[count - 1] = name;
    }

    internal void Subscribe<T>(int layerIndex, IEventHandler<T> handler) where T : struct => GetBucket<T>().Add(layerIndex, handler);
    internal void SubscribeAsync<T>(int layerIndex, IEventHandlerAsync<T> handler) where T : struct => GetBucket<T>().Add(layerIndex, handler);
    internal void SubscribeParallel<T>(int layerIndex, IEventHandler<T> handler, Action<int, string, string, Exception> reportError) where T : struct => GetBucket<T>().AddParallel(layerIndex, handler, reportError);
    internal void Subscribe<T>(int layerIndex, EventHandleDelegate<T> handleDelegate) where T : struct => GetBucket<T>().Add(layerIndex, handleDelegate);
    internal void SubscribeAsync<T>(int layerIndex, EventHandleDelegateAsync<T> handleDelegate) where T : struct => GetBucket<T>().Add(layerIndex, handleDelegate);
    internal void SubscribeParallel<T>(int layerIndex, EventHandleDelegate<T> handleDelegate, Action<int, string, string, Exception> reportError) where T : struct => GetBucket<T>().AddParallel(layerIndex, handleDelegate, reportError);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState Send<T>(in T value, int sourceIndex, Propagation propagation) where T : struct => GetBucket<T>().Dispatch(value, sourceIndex, propagation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState SendLocal<T>(int layerIndex, in T value) where T : struct => GetBucket<T>().DispatchLocal(layerIndex, in value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Post<T>(in T value, int sourceIndex, Propagation propagation) where T : struct => GetBucket<T>().Post(value, sourceIndex, propagation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostLocal<T>(int layerIndex, in T value) where T : struct => GetBucket<T>().PostLocal(layerIndex, value);

    internal void WakeLayer(int layerIndex) { if (layerIndex >= 0 && layerIndex < 64) AtomicSetBit(ref _eventPendingMask, layerIndex); }
    internal void PumpLayer(int layerIndex) { if (layerIndex >= 0 && layerIndex < _layerSlots.Length) _layerSlots[layerIndex].Pump(); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState DispatchLocal<T>(int layerIndex, in Event<T> @event) where T : struct => GetBucket<T>().DispatchLocal(layerIndex, in @event.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int FindFirstBit(ulong mask)
    {
        if (mask == 0) return -1;
#if NETCOREAPP || NET5_0_OR_GREATER
        return System.Numerics.BitOperations.TrailingZeroCount(mask);
#else
        return TrailingZeroCountFallback(mask);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int FindLastBit(ulong mask)
    {
        if (mask == 0) return -1;
#if NETCOREAPP || NET5_0_OR_GREATER
        return 63 - System.Numerics.BitOperations.LeadingZeroCount(mask);
#else
        return 63 - LeadingZeroCountFallback(mask);
#endif
    }

    private static void AtomicSetBit(ref long mask, int bit)
    {
        var bitVal = 1L << bit;
        long initial, computed;
        do { initial = Volatile.Read(ref mask); if ((initial & bitVal) != 0) return; computed = initial | bitVal; } 
        while (Interlocked.CompareExchange(ref mask, computed, initial) != initial);
    }

    private static void AtomicClearBit(ref long mask, int bit)
    {
        var bitVal = 1L << bit;
        long initial, computed;
        do { initial = Volatile.Read(ref mask); if ((initial & bitVal) == 0) return; computed = initial & ~bitVal; } 
        while (Interlocked.CompareExchange(ref mask, computed, initial) != initial);
    }

    internal void Reset()
    {
        foreach (var bucket in _eventBuckets.Values) bucket.Reset();
        _eventBuckets.Clear();
        _layerSlots = Array.Empty<IEventQueue>();
        _layerNames = Array.Empty<string>();
        _bubbleMasksArr = Array.Empty<ulong>();
        _dropMasksArr = Array.Empty<ulong>();
        _eventPendingMask = 0;
    }

    private EventBucket<T> GetBucket<T>() where T : struct
    {
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached;
        var typeId = EventTypeId<T>.Id;
        var bucket = (EventBucket<T>)_eventBuckets.GetOrAdd(typeId, _ => new EventBucket<T>(this));
        BucketCache<T>.Instance = bucket;
        return bucket;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EnqueueEventInternal<T>(int layerIndex, in Event<T> @event) where T : struct
    {
        if (layerIndex >= 0 && layerIndex < _layerSlots.Length) _layerSlots[layerIndex].EnqueueEvent(@event);
    }

    private static class BucketCache<T> where T : struct { public static EventBucket<T>? Instance; }

    internal interface IEventBucket { void Reset(); void MarkDirty(); }

    private interface IEventQueue { void EnqueueEvent<T>(in Event<T> @event) where T : struct; void Pump(); }
    private sealed class LayerEventQueue : IEventQueue
    {
        private readonly GlobalEventCenter _center;
        private readonly int _layerIndex;
        private readonly ConcurrentDictionary<int, IUnmanagedList> _queuesByType = new();
        public LayerEventQueue(GlobalEventCenter center, int layerIndex) { _center = center; _layerIndex = layerIndex; }
        public void EnqueueEvent<T>(in Event<T> @event) where T : struct
        {
            var typeId = EventTypeId<T>.Id;
            if (!_queuesByType.TryGetValue(typeId, out var list))
                list = _queuesByType.GetOrAdd(typeId, _ => new UnmanagedList<T>(_center, _layerIndex));
            ((UnmanagedList<T>)list).Post(@event);
        }
        public void Pump() { if (_queuesByType.Count == 0) return; AtomicClearBit(ref _center._eventPendingMask, _layerIndex); foreach (var list in _queuesByType.Values) list.Pump(); }
    }

    private static int TrailingZeroCountFallback(ulong v)
    {
        if (v == 0) return 64;
        int count = 0;
        if ((v & 0xFFFFFFFF) == 0) { v >>= 32; count += 32; }
        if ((v & 0xFFFF) == 0) { v >>= 16; count += 16; }
        if ((v & 0xFF) == 0) { v >>= 8; count += 8; }
        if ((v & 0xF) == 0) { v >>= 4; count += 4; }
        if ((v & 0x3) == 0) { v >>= 2; count += 2; }
        if ((v & 0x1) == 0) { count += 1; }
        return count;
    }

    private static int LeadingZeroCountFallback(ulong v)
    {
        if (v == 0) return 64;
        int count = 0;
        if ((v & 0xFFFFFFFF00000000UL) == 0) { v <<= 32; count += 32; }
        if ((v & 0xFFFF000000000000UL) == 0) { v <<= 16; count += 16; }
        if ((v & 0xFF00000000000000UL) == 0) { v <<= 8; count += 8; }
        if ((v & 0xF000000000000000UL) == 0) { v <<= 4; count += 4; }
        if ((v & 0xC000000000000000UL) == 0) { v <<= 2; count += 2; }
        if ((v & 0x8000000000000000UL) == 0) { count += 1; }
        return count;
    }

    public interface IResetable { void Reset(); }
}

internal sealed class AsyncFaultContext<T> where T : struct
{
    private static readonly ConcurrentBag<AsyncFaultContext<T>> s_pool = new();
    private readonly Action _continuation;
    private GlobalEventCenter.IEventBucket? _owner;
    private HandlerCircuit? _circuit;
    private string? _handlerFullName;
    private int _layerIndex;
    private T _payload;
    private LBTask _task;

    private AsyncFaultContext() => _continuation = Complete;

    public static void Observe(GlobalEventCenter.IEventBucket owner, int layerIndex, HandlerCircuit circuit, string handlerFullName, in T payload, LBTask task)
    {
        if (!s_pool.TryTake(out var context)) context = new AsyncFaultContext<T>();
        context._owner = owner; context._layerIndex = layerIndex; context._circuit = circuit;
        context._handlerFullName = handlerFullName; context._payload = payload; context._task = task;
        task.GetAwaiter().OnCompleted(context._continuation);
    }

    private void Complete()
    {
        try { _task.GetAwaiter().GetResult(); }
        catch (Exception ex)
        {
            EventMetaDataHandler.OnEventExpectation(_payload, ex);
            if (_circuit != null && _circuit.TryDisable()) { LayerHub.LayerHub.ReportLayerEventError(_layerIndex, _handlerFullName!, typeof(T).Name, ex); _owner?.MarkDirty(); }
        }
        finally { _owner = null; _circuit = null; _handlerFullName = null; _payload = default; _task = default; s_pool.Add(this); }
    }
}
