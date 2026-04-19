using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
#if NETCOREAPP || NET5_0_OR_GREATER
using System.Numerics;
#endif
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

public sealed class GlobalEventCenter
{
    private readonly ConcurrentDictionary<int, object> _eventBuckets = new();
    private readonly object _lock = new();
    internal ulong[] _bubbleMasksArr = Array.Empty<ulong>();
    internal ulong[] _dropMasksArr = Array.Empty<ulong>();
    private long _eventPendingMask;
    private string[] _layerNames = Array.Empty<string>();
    private IEventQueue[] _layerSlots = Array.Empty<IEventQueue>();
    private int _isResetting; 

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

    internal void Unsubscribe<T>(int layerIndex, IEventHandler<T> handler) where T : struct => GetBucket<T>().Remove(layerIndex, handler);
    internal void UnsubscribeAsync<T>(int layerIndex, IEventHandlerAsync<T> handler) where T : struct => GetBucket<T>().Remove(layerIndex, handler);
    internal void Unsubscribe<T>(int layerIndex, EventHandleDelegate<T> handleDelegate) where T : struct => GetBucket<T>().Remove(layerIndex, handleDelegate);
    internal void UnsubscribeAsync<T>(int layerIndex, EventHandleDelegateAsync<T> handleDelegate) where T : struct => GetBucket<T>().Remove(layerIndex, handleDelegate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState Send<T>(in T value, int sourceIndex, Propagation propagation) where T : struct
    {
        if (Volatile.Read(ref _isResetting) == 1) return EventHandledState.Continue;
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached.Dispatch(in value, sourceIndex, propagation);
        return GetBucket<T>().Dispatch(in value, sourceIndex, propagation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState SendLocal<T>(int layerIndex, in T value) where T : struct
    {
        if (Volatile.Read(ref _isResetting) == 1) return EventHandledState.Continue;
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached.DispatchLocal(layerIndex, in value);
        return GetBucket<T>().DispatchLocal(layerIndex, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Post<T>(in T value, int sourceIndex, Propagation propagation) where T : struct
    {
        if (Volatile.Read(ref _isResetting) == 1) return;
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) { cached.Post(in value, sourceIndex, propagation); return; }
        GetBucket<T>().Post(in value, sourceIndex, propagation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostLocal<T>(int layerIndex, in T value) where T : struct
    {
        if (Volatile.Read(ref _isResetting) == 1) return;
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) { cached.PostLocal(layerIndex, in value); return; }
        GetBucket<T>().PostLocal(layerIndex, in value);
    }

    internal void WakeLayer(int layerIndex) { if (layerIndex >= 0 && layerIndex < 64) AtomicSetBit(ref _eventPendingMask, layerIndex); }
    internal void PumpLayer(int layerIndex) 
    { 
        if (Volatile.Read(ref _isResetting) == 1) return;
        var slots = _layerSlots; 
        if (layerIndex >= 0 && layerIndex < slots.Length) slots[layerIndex]?.Pump(); 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState DispatchLocal<T>(int layerIndex, in Event<T> @event) where T : struct
    {
        if (Volatile.Read(ref _isResetting) == 1) return EventHandledState.Continue;
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached.DispatchLocal(layerIndex, in @event.Value);
        return GetBucket<T>().DispatchLocal(layerIndex, in @event.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int InternalFindFirstBit(ulong mask)
    {
#if NETCOREAPP || NET5_0_OR_GREATER
        return BitOperations.TrailingZeroCount(mask);
#else
        if (mask == 0) return 64;
        int count = 0;
        if ((mask & 0xFFFFFFFF) == 0) { mask >>= 32; count += 32; }
        if ((mask & 0xFFFF) == 0) { mask >>= 16; count += 16; }
        if ((mask & 0xFF) == 0) { mask >>= 8; count += 8; }
        if ((mask & 0xF) == 0) { mask >>= 4; count += 4; }
        if ((mask & 0x3) == 0) { mask >>= 2; count += 2; }
        if ((mask & 0x1) == 0) { count += 1; }
        return count;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int InternalFindLastBit(ulong mask)
    {
#if NETCOREAPP || NET5_0_OR_GREATER
        return 63 - BitOperations.LeadingZeroCount(mask);
#else
        if (mask == 0) return -1;
        int count = 0;
        if ((mask & 0xFFFFFFFF00000000UL) == 0) { mask <<= 32; count += 32; }
        if ((mask & 0xFFFF000000000000UL) == 0) { mask <<= 16; count += 16; }
        if ((mask & 0xFF00000000000000UL) == 0) { mask <<= 8; count += 8; }
        if ((mask & 0xF000000000000000UL) == 0) { mask <<= 4; count += 4; }
        if ((mask & 0xC000000000000000UL) == 0) { mask <<= 2; count += 2; }
        if ((mask & 0x8000000000000000UL) == 0) { count += 1; }
        return 63 - count;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref TElement GetArrayDataRef<TElement>(TElement[] array) 
    {
#if NET5_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference(array);
#else
        return ref MemoryMarshal.GetReference(array.AsSpan());
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int FindFirstBit(ulong mask) => InternalFindFirstBit(mask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int FindLastBit(ulong mask) => InternalFindLastBit(mask);

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
        if (Interlocked.Exchange(ref _isResetting, 1) == 1) return;
        
        lock (_lock)
        {
            foreach (var bucket in _eventBuckets.Values) {
                if (bucket is IResetable b) b.Reset();
            }
            _eventBuckets.Clear();
            var oldSlots = _layerSlots;
            _layerSlots = Array.Empty<IEventQueue>();
            foreach (var slot in oldSlots) slot?.Dispose();
            _layerNames = Array.Empty<string>();
            _bubbleMasksArr = Array.Empty<ulong>();
            _dropMasksArr = Array.Empty<ulong>();
            _eventPendingMask = 0;
        }

        Volatile.Write(ref _isResetting, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        var slots = _layerSlots; 
        if (layerIndex >= 0 && layerIndex < slots.Length) slots[layerIndex]?.EnqueueEvent(@event);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EnqueueEventBatchInternal<T>(int layerIndex, ReadOnlySpan<Event<T>> events) where T : struct
    {
        var slots = _layerSlots;
        if (layerIndex >= 0 && layerIndex < slots.Length) {
            var s = slots[layerIndex];
            if (s != null) for (int i = 0; i < events.Length; i++) s.EnqueueEvent(events[i]);
        }
    }

    private static class BucketCache<T> where T : struct { public static EventBucket<T>? Instance; }

    private interface IResetable { void Reset(); }

    private interface IEventQueue : IDisposable { void EnqueueEvent<T>(in Event<T> @event) where T : struct; void Pump(); }
    private sealed class LayerEventQueue : IEventQueue
    {
        private readonly GlobalEventCenter _center;
        private readonly int _layerIndex;
        private readonly ConcurrentDictionary<int, IUnmanagedList> _queuesByType = new();
        private readonly ConcurrentQueue<IUnmanagedList> _dirtyQueues = new();
        private readonly Action<IUnmanagedList> _onDirtyCallback;
        private bool _disposed;

        public LayerEventQueue(GlobalEventCenter center, int layerIndex)
        {
            _center = center;
            _layerIndex = layerIndex;
            _onDirtyCallback = list => _dirtyQueues.Enqueue(list);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var q in _queuesByType.Values) q.Dispose();
            _queuesByType.Clear();
        }

        public void EnqueueEvent<T>(in Event<T> @event) where T : struct
        {
            if (_disposed) return;
            var typeId = EventTypeId<T>.Id;
            if (!_queuesByType.TryGetValue(typeId, out var list))
                list = _queuesByType.GetOrAdd(typeId, _ => new UnmanagedList<T>(_center, _layerIndex, _onDirtyCallback));
            ((UnmanagedList<T>)list).Post(@event);
        }

        public void Pump() 
        { 
            if (_disposed || _dirtyQueues.IsEmpty) return; 
            AtomicClearBit(ref _center._eventPendingMask, _layerIndex); 
            while (_dirtyQueues.TryDequeue(out var list))
            {
                list.Pump();
            }
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void AddOptimized<T>(int layerIndex, IntPtr ptr, object target, string name) where T : struct
    {
        GetBucket<T>().AddOptimized(layerIndex, ptr, target, name);
    }

    private struct LayerRange
    {
        public int SyncStart;
        public int SyncCount;
        public int AsyncStart;
        public int AsyncCount;
        public int ParallelStart;
        public int ParallelCount;
    }

    private sealed class EventBucket<T> : IResetable where T : struct
    {
        private readonly object _lock = new();
        public readonly GlobalEventCenter Owner;
        private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();

        private EventHandleDelegate<T>[] _syncHandlers = Array.Empty<EventHandleDelegate<T>>();
        private IntPtr[] _syncBridges = Array.Empty<IntPtr>();
        private object?[] _syncTargets = Array.Empty<object?>();

        private HandlerCircuit[] _syncCircuits = Array.Empty<HandlerCircuit>();
        private string[] _syncNames = Array.Empty<string>();

        private EventHandleDelegateAsync<T>[] _asyncHandlers = Array.Empty<EventHandleDelegateAsync<T>>();
        private HandlerCircuit[] _asyncCircuits = Array.Empty<HandlerCircuit>();
        private string[] _asyncNames = Array.Empty<string>();

        private ParallelHandlerEntry<T>[] _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();
        private LayerRange[] _ranges = Array.Empty<LayerRange>();
        private ulong _subscriberMask;
        private int _isDirty;
        private int _syncCountTotal, _asyncCountTotal, _parallelCountTotal;

        public EventBucket(GlobalEventCenter center) => Owner = center;

        public void Reset() 
        { 
            HandlerBucket<T>?[] snapshot;
            lock (_lock) 
            { 
                snapshot = new HandlerBucket<T>?[_buckets.Length];
                Array.Copy(_buckets, snapshot, _buckets.Length);
            } 
            
            foreach (var b in snapshot) b?.Reset(); 
            
            lock (_lock) Rebuild(); 
        }

        public void MarkDirty() => Interlocked.Exchange(ref _isDirty, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureClean() { if (Volatile.Read(ref _isDirty) == 1) lock (_lock) { if (_isDirty == 1) Rebuild(); } }

        private void Rebuild()
        {
            int totalSync = 0, totalAsync = 0, totalParallel = 0;
            ulong newMask = 0;
            for (int i = 0; i < _buckets.Length; i++) {
                var b = _buckets[i];
                if (b == null || !b.HasHandlers) continue;
                int bSync = 0, bAsync = 0;
                foreach (var h in b.MasterOrdered) { if (!h.Circuit.IsDisabled) { if (h.SyncHandler != null || h.StaticBridgePtr != IntPtr.Zero) bSync++; else if (h.AsyncHandler != null) bAsync++; } }
                foreach (var h in b.MasterUnordered) { if (!h.Circuit.IsDisabled) { if (h.SyncWrapper != null) bSync++; else if (h.AsyncWrapper != null) bAsync++; } }
                totalSync += bSync; totalAsync += bAsync; totalParallel += b.MasterParallel.Count;
                if (bSync > 0 || bAsync > 0 || b.MasterParallel.Count > 0) newMask |= (1UL << i);
            }

            if (_syncHandlers.Length < totalSync) {
                _syncHandlers = new EventHandleDelegate<T>[Math.Max(totalSync, _syncHandlers.Length * 2)];
                _syncBridges = new IntPtr[_syncHandlers.Length];
                _syncTargets = new object[_syncHandlers.Length];
                _syncCircuits = new HandlerCircuit[_syncHandlers.Length];
                _syncNames = new string[_syncHandlers.Length];
            }
            if (_asyncHandlers.Length < totalAsync) {
                _asyncHandlers = new EventHandleDelegateAsync<T>[Math.Max(totalAsync, _asyncHandlers.Length * 2)];
                _asyncCircuits = new HandlerCircuit[_asyncHandlers.Length];
                _asyncNames = new string[_asyncHandlers.Length];
            }
            if (_flatParallel.Length < totalParallel) {
                _flatParallel = new ParallelHandlerEntry<T>[Math.Max(totalParallel, _flatParallel.Length * 2)];
            }
            if (_ranges.Length < _buckets.Length) {
                _ranges = new LayerRange[Math.Max(_buckets.Length, _ranges.Length * 2)];
            }

            int sIdx = 0, aIdx = 0, pIdx = 0;
            for (int i = 0; i < _buckets.Length; i++) {
                var b = _buckets[i];
                if (b == null) { if (i < _ranges.Length) _ranges[i] = default; continue; }
                _ranges[i].SyncStart = sIdx; _ranges[i].AsyncStart = aIdx; _ranges[i].ParallelStart = pIdx;
                if (b.HasHandlers) {
                    foreach (var h in b.MasterOrdered) { if (!h.Circuit.IsDisabled) {
                        if (h.SyncHandler != null) { _syncHandlers[sIdx] = h.SyncHandler; _syncBridges[sIdx] = IntPtr.Zero; _syncTargets[sIdx] = null; _syncCircuits[sIdx] = h.Circuit; _syncNames[sIdx] = h.FullName; sIdx++; }
                        else if (h.StaticBridgePtr != IntPtr.Zero) { _syncHandlers[sIdx] = null!; _syncBridges[sIdx] = h.StaticBridgePtr; _syncTargets[sIdx] = h.Target; _syncCircuits[sIdx] = h.Circuit; _syncNames[sIdx] = h.FullName; sIdx++; }
                    } }
                    foreach (var h in b.MasterUnordered) { if (!h.Circuit.IsDisabled && h.SyncWrapper != null) { _syncHandlers[sIdx] = h.SyncWrapper; _syncBridges[sIdx] = IntPtr.Zero; _syncTargets[sIdx] = null; _syncCircuits[sIdx] = h.Circuit; _syncNames[sIdx] = h.FullName; sIdx++; } }
                    foreach (var h in b.MasterOrdered) { if (!h.Circuit.IsDisabled && h.AsyncHandler != null) { _asyncHandlers[aIdx] = h.AsyncHandler; _asyncCircuits[aIdx] = h.Circuit; _asyncNames[aIdx] = h.FullName; aIdx++; } }
                    foreach (var h in b.MasterUnordered) { if (!h.Circuit.IsDisabled && h.AsyncWrapper != null) { _asyncHandlers[aIdx] = h.AsyncWrapper; _asyncCircuits[aIdx] = h.Circuit; _asyncNames[aIdx] = h.FullName; aIdx++; } }
                    foreach (var h in b.MasterParallel) { _flatParallel[pIdx++] = h; }
                }
                _ranges[i].SyncCount = sIdx - _ranges[i].SyncStart; _ranges[i].AsyncCount = aIdx - _ranges[i].AsyncStart; _ranges[i].ParallelCount = pIdx - _ranges[i].ParallelStart;
            }

            Array.Clear(_syncHandlers, sIdx, _syncHandlers.Length - sIdx);
            Array.Clear(_syncBridges, sIdx, _syncBridges.Length - sIdx);
            Array.Clear(_syncTargets, sIdx, _syncTargets.Length - sIdx);
            Array.Clear(_syncCircuits, sIdx, _syncCircuits.Length - sIdx);
            Array.Clear(_syncNames, sIdx, _syncNames.Length - sIdx);
            Array.Clear(_asyncHandlers, aIdx, _asyncHandlers.Length - aIdx);
            Array.Clear(_asyncCircuits, aIdx, _asyncCircuits.Length - aIdx);
            Array.Clear(_asyncNames, aIdx, _asyncNames.Length - aIdx);
            Array.Clear(_flatParallel, pIdx, _flatParallel.Length - pIdx);

            _syncCountTotal = sIdx; _asyncCountTotal = aIdx; _parallelCountTotal = pIdx;
            _subscriberMask = newMask;
            Volatile.Write(ref _isDirty, 0);
        }

        public void Add(int layerIndex, IEventHandler<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void Add(int layerIndex, IEventHandlerAsync<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void AddParallel(int layerIndex, IEventHandler<T> h, Action<int, string, string, Exception> re) { GetOrCreate(layerIndex).AddParallel(h, re); }
        public void Add(int layerIndex, EventHandleDelegate<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void Add(int layerIndex, EventHandleDelegateAsync<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void AddParallel(int layerIndex, EventHandleDelegate<T> h, Action<int, string, string, Exception> re) { GetOrCreate(layerIndex).AddParallel(h, re); }
        
        internal void AddOptimized(int layerIndex, IntPtr ptr, object target, string name) { GetOrCreate(layerIndex).MasterOrdered.Add(OrderedHandlerEntry<T>.CreateOptimized(ptr, target, name)); MarkDirty(); }

        public void Remove(int layerIndex, IEventHandler<T> h) { if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null) { _buckets[layerIndex]!.Remove(h); MarkDirty(); } }
        public void Remove(int layerIndex, IEventHandlerAsync<T> h) { if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null) { _buckets[layerIndex]!.Remove(h); MarkDirty(); } }
        public void Remove(int layerIndex, EventHandleDelegate<T> h) { if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null) { _buckets[layerIndex]!.Remove(h); MarkDirty(); } }
        public void Remove(int layerIndex, EventHandleDelegateAsync<T> h) { if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null) { _buckets[layerIndex]!.Remove(h); MarkDirty(); } }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState Dispatch(in T value, int sourceIndex, Propagation propagation)
        {
            EnsureClean();
            var mask = Volatile.Read(ref _subscriberMask);
            if (mask == 0) return EventHandledState.Continue;

            if (propagation == Propagation.Global)
            {
                for (int j = 0; j < _parallelCountTotal; j++) _flatParallel[j].Enqueue(-1, in value);
                var res = DispatchSync(0, _syncCountTotal, in value);
                if (res == EventHandledState.Handled) return res;
                DispatchAsync(0, _asyncCountTotal, in value);
                return res;
            }

            var targetMask = mask;
            var bubble = Owner._bubbleMasksArr; 
            var drop = Owner._dropMasksArr;     
            if (propagation == Propagation.Bubble && sourceIndex < bubble.Length) targetMask &= bubble[sourceIndex];
            else if (propagation == Propagation.Drop && sourceIndex < drop.Length) targetMask &= drop[sourceIndex];
            
            if (targetMask == 0) return EventHandledState.Continue;

            ref var rangesRef = ref GlobalEventCenter.GetArrayDataRef(_ranges);
            ref var flatParallelRef = ref GlobalEventCenter.GetArrayDataRef(_flatParallel);

            if (propagation == Propagation.Bubble)
            {
                while (targetMask != 0)
                {
                    int l = GlobalEventCenter.InternalFindLastBit(targetMask);
                    ref var r = ref Unsafe.Add(ref rangesRef, l);
                    for(int j = 0; j < r.ParallelCount; j++) 
                        Unsafe.Add(ref flatParallelRef, r.ParallelStart + j).Enqueue(l, in value);
                    
                    var s = DispatchSyncBackward(r.SyncStart, r.SyncStart + r.SyncCount, in value);
                    if (s == EventHandledState.Handled) return s;
                    DispatchAsyncBackward(r.AsyncStart, r.AsyncStart + r.AsyncCount, in value);
                    targetMask &= ~(1UL << l);
                }
                return EventHandledState.Continue;
            }
            else
            {
                while (targetMask != 0)
                {
                    int l = GlobalEventCenter.InternalFindFirstBit(targetMask);
                    ref var r = ref Unsafe.Add(ref rangesRef, l);
                    for(int j = 0; j < r.ParallelCount; j++) 
                        Unsafe.Add(ref flatParallelRef, r.ParallelStart + j).Enqueue(l, in value);
                    
                    var s = DispatchSync(r.SyncStart, r.SyncStart + r.SyncCount, in value);
                    if (s == EventHandledState.Handled) return s;
                    DispatchAsync(r.AsyncStart, r.AsyncStart + r.AsyncCount, in value);
                    targetMask &= (targetMask - 1);
                }
                return EventHandledState.Continue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState DispatchLocal(int layerIndex, in T value)
        {
            EnsureClean();
            if (layerIndex >= _ranges.Length) return EventHandledState.Continue;
            var r = _ranges[layerIndex];
            for (int j = 0; j < r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(layerIndex, in value);
            var s = DispatchSync(r.SyncStart, r.SyncStart + r.SyncCount, in value);
            if (s == EventHandledState.Handled) return s;
            DispatchAsync(r.AsyncStart, r.AsyncStart + r.AsyncCount, in value);
            return s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe EventHandledState DispatchSync(int start, int end, in T value)
        {
            if (start >= end) return EventHandledState.Continue;

            ref var hBase = ref GlobalEventCenter.GetArrayDataRef(_syncHandlers);
            ref var bBase = ref GlobalEventCenter.GetArrayDataRef(_syncBridges);
            ref var tBase = ref GlobalEventCenter.GetArrayDataRef(_syncTargets);

            int combinedState = 0; int i = start;
            try {
                for (; i < end; i++) {
                    EventHandledState res;
                    IntPtr b = Unsafe.Add(ref bBase, i);
                    if (b != IntPtr.Zero)
                    {
                        res = ((delegate*<object, in T, EventHandledState>)b)(Unsafe.Add(ref tBase, i)!, in value);
                    }
                    else
                    {
                        res = Unsafe.Add(ref hBase, i)(in value);
                    }
                    combinedState |= (int)res;
                    if ((combinedState & 1) != 0) return EventHandledState.Handled;
                }
            }
            catch (Exception e) { HandleFault(i, true, in value, e); return EventHandledState.Continue; }
            return (combinedState & 2) != 0 ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchAsync(int start, int end, in T value)
        {
            var hs = _asyncHandlers; int i = start;
            try { for (; i < end; i++) AsyncFaultContext<T>.Observe(this, -1, _asyncCircuits[i], _asyncNames[i], in value, hs[i](value)); }
            catch (Exception e) { HandleFault(i, false, in value, e); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe EventHandledState DispatchSyncBackward(int start, int end, in T value)
        {
            if (start >= end) return EventHandledState.Continue;

            ref var hBase = ref GlobalEventCenter.GetArrayDataRef(_syncHandlers);
            ref var bBase = ref GlobalEventCenter.GetArrayDataRef(_syncBridges);
            ref var tBase = ref GlobalEventCenter.GetArrayDataRef(_syncTargets);

            int combinedState = 0; int i = end - 1;
            try {
                for (; i >= start; i--) {
                    EventHandledState res;
                    IntPtr b = Unsafe.Add(ref bBase, i);
                    if (b != IntPtr.Zero)
                    {
                        res = ((delegate*<object, in T, EventHandledState>)b)(Unsafe.Add(ref tBase, i)!, in value);
                    }
                    else
                    {
                        res = Unsafe.Add(ref hBase, i)(in value);
                    }
                    combinedState |= (int)res;
                    if ((combinedState & 1) != 0) return EventHandledState.Handled;
                }
            }
            catch (Exception e) { HandleFault(i, true, in value, e); return EventHandledState.Continue; }
            return (combinedState & 2) != 0 ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchAsyncBackward(int start, int end, in T value)
        {
            var hs = _asyncHandlers; int i = end - 1;
            try { for (; i >= start; i--) AsyncFaultContext<T>.Observe(this, -1, _asyncCircuits[i], _asyncNames[i], in value, hs[i](value)); }
            catch (Exception e) { HandleFault(i, false, in value, e); }
        }

        private void HandleFault(int index, bool isSync, in T value, Exception e)
        {
            HandlerCircuit? circuit = null; string? name = null;
            if (isSync) { if (index >= 0 && index < _syncCountTotal) { circuit = _syncCircuits[index]; name = _syncNames[index]; } }
            else { if (index >= 0 && index < _asyncCountTotal) { circuit = _asyncCircuits[index]; name = _asyncNames[index]; } }
            EventMetaDataHandler.OnEventExpectation(value, e);
            if (circuit != null && circuit.TryDisable()) { LayerHub.LayerHub.ReportLayerEventError(-1, name ?? "Unknown", typeof(T).Name, e); MarkDirty(); }
        }

        public void Post(in T value, int sourceIndex, Propagation propagation)
        {
            if (Volatile.Read(ref Owner._isResetting) == 1) return;
            EnsureClean();
            var mask = Volatile.Read(ref _subscriberMask);
            if (mask == 0) return;
            var targetMask = mask;
            var bubble = Owner._bubbleMasksArr;
            var drop = Owner._dropMasksArr;
            if (propagation == Propagation.Bubble && sourceIndex < bubble.Length) targetMask &= bubble[sourceIndex];
            else if (propagation == Propagation.Drop && sourceIndex < drop.Length) targetMask &= drop[sourceIndex];
            
            if (targetMask != 0) {
                var firstLayer = (propagation == Propagation.Bubble) ? Owner.FindLastBit(targetMask) : Owner.FindFirstBit(targetMask);
                var @event = new Event<T>(value) { TargetMask = targetMask, Propagation = (int)propagation };
                Owner.EnqueueEventInternal(firstLayer, in @event);
                Owner.WakeLayer(firstLayer);
            }
        }

        public void PostLocal(int layerIndex, in T value)
        {
            if (Volatile.Read(ref Owner._isResetting) == 1) return;
            EnsureClean();
            if (layerIndex >= 0 && layerIndex < 64 && (_subscriberMask & (1UL << layerIndex)) != 0) {
                var @event = new Event<T>(value) { TargetMask = 1UL << layerIndex, Propagation = (int)Propagation.Global };
                Owner.EnqueueEventInternal(layerIndex, in @event);
                Owner.WakeLayer(layerIndex);
            }
        }

        private HandlerBucket<T> GetOrCreate(int layerIndex)
        {
            if (layerIndex >= _buckets.Length) lock (_lock) { if (layerIndex >= _buckets.Length) {
                var next = new HandlerBucket<T>?[Math.Max(layerIndex + 1, _buckets.Length * 2)];
                Array.Copy(_buckets, next, _buckets.Length);
                _buckets = next;
            } }
            var b = _buckets[layerIndex];
            if (b == null) lock (_lock) { b = _buckets[layerIndex] ??= new HandlerBucket<T>(MarkDirty); }
            return b;
        }
    }

    private sealed class AsyncFaultContext<T> where T : struct
    {
        private static readonly ConcurrentBag<AsyncFaultContext<T>> s_pool = new();
        private readonly Action _continuation;
        private EventBucket<T>? _owner;
        private HandlerCircuit? _circuit;
        private string? _handlerFullName;
        private int _layerIndex;
        private T _payload;
        private LBTask _task;

        private AsyncFaultContext() => _continuation = Complete;

        public static void Observe(EventBucket<T> owner, int layerIndex, HandlerCircuit circuit, string handlerFullName, in T payload, LBTask task)
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
}
