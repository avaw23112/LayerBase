using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
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
    private readonly ConcurrentDictionary<int, object> _eventBuckets = new();
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
    internal EventHandledState Send<T>(in T value, int sourceIndex, Propagation propagation) where T : struct
    {
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached.Dispatch(in value, sourceIndex, propagation);
        return GetBucket<T>().Dispatch(in value, sourceIndex, propagation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState SendLocal<T>(int layerIndex, in T value) where T : struct
    {
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached.DispatchLocal(layerIndex, in value);
        return GetBucket<T>().DispatchLocal(layerIndex, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Post<T>(in T value, int sourceIndex, Propagation propagation) where T : struct
    {
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) { cached.Post(in value, sourceIndex, propagation); return; }
        GetBucket<T>().Post(in value, sourceIndex, propagation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostLocal<T>(int layerIndex, in T value) where T : struct
    {
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) { cached.PostLocal(layerIndex, in value); return; }
        GetBucket<T>().PostLocal(layerIndex, in value);
    }

    internal void WakeLayer(int layerIndex) { if (layerIndex >= 0 && layerIndex < 64) AtomicSetBit(ref _eventPendingMask, layerIndex); }
    internal void PumpLayer(int layerIndex) { if (layerIndex >= 0 && layerIndex < _layerSlots.Length) _layerSlots[layerIndex].Pump(); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState DispatchLocal<T>(int layerIndex, in Event<T> @event) where T : struct
    {
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached.DispatchLocal(layerIndex, in @event.Value);
        return GetBucket<T>().DispatchLocal(layerIndex, in @event.Value);
    }

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
        foreach (var bucket in _eventBuckets.Values) {
            if (bucket is IResetable b) b.Reset();
        }
        _eventBuckets.Clear();
        _layerSlots = Array.Empty<IEventQueue>();
        _layerNames = Array.Empty<string>();
        _bubbleMasksArr = Array.Empty<ulong>();
        _dropMasksArr = Array.Empty<ulong>();
        _eventPendingMask = 0;
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
        if (layerIndex >= 0 && layerIndex < _layerSlots.Length) _layerSlots[layerIndex].EnqueueEvent(@event);
    }

    private static class BucketCache<T> where T : struct { public static EventBucket<T>? Instance; }

    private interface IResetable { void Reset(); }

    private interface IEventQueue { void EnqueueEvent<T>(in Event<T> @event) where T : struct; void Pump(); }
    private sealed class LayerEventQueue : IEventQueue
    {
        private readonly GlobalEventCenter _center;
        private readonly int _layerIndex;
        private readonly ConcurrentDictionary<int, IUnmanagedList> _queuesByType = new();
        private readonly ConcurrentQueue<IUnmanagedList> _dirtyQueues = new();
        private readonly Action<IUnmanagedList> _onDirtyCallback;

        public LayerEventQueue(GlobalEventCenter center, int layerIndex) 
        { 
            _center = center; 
            _layerIndex = layerIndex;
            _onDirtyCallback = list => _dirtyQueues.Enqueue(list);
        }

        public void EnqueueEvent<T>(in Event<T> @event) where T : struct
        {
            var typeId = EventTypeId<T>.Id;
            if (!_queuesByType.TryGetValue(typeId, out var list))
                list = _queuesByType.GetOrAdd(typeId, _ => new UnmanagedList<T>(_center, _layerIndex, _onDirtyCallback));
            ((UnmanagedList<T>)list).Post(@event);
        }

        public void Pump() 
        { 
            if (_dirtyQueues.IsEmpty) return; 
            AtomicClearBit(ref _center._eventPendingMask, _layerIndex); 
            while (_dirtyQueues.TryDequeue(out var list))
            {
                list.Pump();
            }
        }
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

        // 🚀 SOA: 同步分发引擎 (极致连续指针)
        private EventHandleDelegate<T>[] _syncHandlers = Array.Empty<EventHandleDelegate<T>>();
        private HandlerCircuit[] _syncCircuits = Array.Empty<HandlerCircuit>();
        private string[] _syncNames = Array.Empty<string>();

        // 🚀 SOA: 异步分发引擎 (极致连续指针)
        private EventHandleDelegateAsync<T>[] _asyncHandlers = Array.Empty<EventHandleDelegateAsync<T>>();
        private HandlerCircuit[] _asyncCircuits = Array.Empty<HandlerCircuit>();
        private string[] _asyncNames = Array.Empty<string>();

        private ParallelHandlerEntry<T>[] _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();
        private LayerRange[] _ranges = Array.Empty<LayerRange>();
        private ulong _subscriberMask;
        private int _isDirty;

        public EventBucket(GlobalEventCenter center) => Owner = center;

        public void Reset() { lock (_lock) { foreach (var b in _buckets) b?.Reset(); Rebuild(); } }

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
                foreach (var h in b.MasterOrdered) { if (!h.Circuit.IsDisabled) { if (h.SyncHandler != null) bSync++; else if (h.AsyncHandler != null) bAsync++; } }
                foreach (var h in b.MasterUnordered) { if (!h.Circuit.IsDisabled) { if (h.SyncWrapper != null) bSync++; else if (h.AsyncWrapper != null) bAsync++; } }
                totalSync += bSync; totalAsync += bAsync; totalParallel += b.MasterParallel.Count;
                if (bSync > 0 || bAsync > 0 || b.MasterParallel.Count > 0) newMask |= (1UL << i);
            }

            var sH = new EventHandleDelegate<T>[totalSync];
            var sC = new HandlerCircuit[totalSync];
            var sN = new string[totalSync];
            var aH = new EventHandleDelegateAsync<T>[totalAsync];
            var aC = new HandlerCircuit[totalAsync];
            var aN = new string[totalAsync];
            var pH = new ParallelHandlerEntry<T>[totalParallel];
            var newRanges = new LayerRange[_buckets.Length];

            int sIdx = 0, aIdx = 0, pIdx = 0;
            for (int i = 0; i < _buckets.Length; i++) {
                var b = _buckets[i];
                if (b == null || !b.HasHandlers) continue;
                newRanges[i].SyncStart = sIdx; newRanges[i].AsyncStart = aIdx; newRanges[i].ParallelStart = pIdx;
                foreach (var h in b.MasterOrdered) { if (!h.Circuit.IsDisabled && h.SyncHandler != null) { sH[sIdx] = h.SyncHandler; sC[sIdx] = h.Circuit; sN[sIdx] = h.FullName; sIdx++; } }
                foreach (var h in b.MasterUnordered) { if (!h.Circuit.IsDisabled && h.SyncWrapper != null) { sH[sIdx] = h.SyncWrapper; sC[sIdx] = h.Circuit; sN[sIdx] = h.FullName; sIdx++; } }
                foreach (var h in b.MasterOrdered) { if (!h.Circuit.IsDisabled && h.AsyncHandler != null) { aH[aIdx] = h.AsyncHandler; aC[aIdx] = h.Circuit; aN[aIdx] = h.FullName; aIdx++; } }
                foreach (var h in b.MasterUnordered) { if (!h.Circuit.IsDisabled && h.AsyncWrapper != null) { aH[aIdx] = h.AsyncWrapper; aC[aIdx] = h.Circuit; aN[aIdx] = h.FullName; aIdx++; } }
                foreach (var h in b.MasterParallel) { pH[pIdx++] = h; }
                newRanges[i].SyncCount = sIdx - newRanges[i].SyncStart; newRanges[i].AsyncCount = aIdx - newRanges[i].AsyncStart; newRanges[i].ParallelCount = pIdx - newRanges[i].ParallelStart;
            }

            _syncHandlers = sH; _syncCircuits = sC; _syncNames = sN;
            _asyncHandlers = aH; _asyncCircuits = aC; _asyncNames = aN;
            _flatParallel = pH; _ranges = newRanges; _subscriberMask = newMask;
            Volatile.Write(ref _isDirty, 0);
        }

        public void Add(int layerIndex, IEventHandler<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void Add(int layerIndex, IEventHandlerAsync<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void AddParallel(int layerIndex, IEventHandler<T> h, Action<int, string, string, Exception> re) { GetOrCreate(layerIndex).AddParallel(h, re); }
        public void Add(int layerIndex, EventHandleDelegate<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void Add(int layerIndex, EventHandleDelegateAsync<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void AddParallel(int layerIndex, EventHandleDelegate<T> h, Action<int, string, string, Exception> re) { GetOrCreate(layerIndex).AddParallel(h, re); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState Dispatch(in T value, int sourceIndex, Propagation propagation)
        {
            EnsureClean();
            var mask = Volatile.Read(ref _subscriberMask);
            if (mask == 0) return EventHandledState.Continue;

            if (propagation == Propagation.Global)
            {
                var ps = _flatParallel;
                for (int j = 0; j < ps.Length; j++) ps[j].Enqueue(-1, in value);
                var res = DispatchSync(0, _syncHandlers.Length, in value);
                if (res == EventHandledState.Handled) return res;
                DispatchAsync(0, _asyncHandlers.Length, in value);
                return res;
            }

            var targetMask = mask;
            if (propagation == Propagation.Bubble) targetMask &= Owner._bubbleMasksArr[sourceIndex];
            else targetMask &= Owner._dropMasksArr[sourceIndex];
            if (targetMask == 0) return EventHandledState.Continue;

            int first = Owner.FindFirstBit(targetMask);
            int last = Owner.FindLastBit(targetMask);

            if (propagation == Propagation.Bubble)
            {
                for (int l = last; l >= first; l--)
                {
                    if ((targetMask & (1UL << l)) == 0) continue;
                    var r = _ranges[l];
                    for(int j = 0; j < r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(l, in value);
                    var s = DispatchSyncBackward(r.SyncStart, r.SyncStart + r.SyncCount, in value);
                    if (s == EventHandledState.Handled) return s;
                    DispatchAsyncBackward(r.AsyncStart, r.AsyncStart + r.AsyncCount, in value);
                }
                return EventHandledState.Continue;
            }
            else
            {
                for (int l = first; l <= last; l++)
                {
                    if ((targetMask & (1UL << l)) == 0) continue;
                    var r = _ranges[l];
                    for(int j = 0; j < r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(l, in value);
                    var s = DispatchSync(r.SyncStart, r.SyncStart + r.SyncCount, in value);
                    if (s == EventHandledState.Handled) return s;
                    DispatchAsync(r.AsyncStart, r.AsyncStart + r.AsyncCount, in value);
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

        // 🚀 核心优化：全量 Inlining + 位运算 + 原生数组读取，彻底消灭接口及虚调用
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSync(int start, int end, in T value)
        {
            var hs = _syncHandlers;
            int combinedState = 0;
            int i = start;
            try {
                for (; i <= end - 2; i += 2) {
                    var r1 = hs[i](in value);
                    var r2 = hs[i+1](in value);
                    combinedState |= (int)r1 | (int)r2;
                    if ((combinedState & 1) != 0) return EventHandledState.Handled;
                }
                for (; i < end; i++) {
                    combinedState |= (int)hs[i](in value);
                    if ((combinedState & 1) != 0) return EventHandledState.Handled;
                }
            }
            catch (Exception e) { HandleFault(i, true, in value, e); return EventHandledState.Continue; }
            return (combinedState & 2) != 0 ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchAsync(int start, int end, in T value)
        {
            var hs = _asyncHandlers;
            int i = start;
            try { for (; i < end; i++) AsyncFaultContext<T>.Observe(this, -1, _asyncCircuits[i], _asyncNames[i], in value, hs[i](value)); }
            catch (Exception e) { HandleFault(i, false, in value, e); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSyncBackward(int start, int end, in T value)
        {
            var hs = _syncHandlers;
            int combinedState = 0;
            int i = end - 1;
            try {
                for (; i >= start; i--) {
                    combinedState |= (int)hs[i](in value);
                    if ((combinedState & 1) != 0) return EventHandledState.Handled;
                }
            }
            catch (Exception e) { HandleFault(i, true, in value, e); return EventHandledState.Continue; }
            return (combinedState & 2) != 0 ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchAsyncBackward(int start, int end, in T value)
        {
            var hs = _asyncHandlers;
            int i = end - 1;
            try { for (; i >= start; i--) AsyncFaultContext<T>.Observe(this, -1, _asyncCircuits[i], _asyncNames[i], in value, hs[i](value)); }
            catch (Exception e) { HandleFault(i, false, in value, e); }
        }

        private void HandleFault(int index, bool isSync, in T value, Exception e)
        {
            HandlerCircuit circuit; string name;
            if (isSync) { if (index < 0 || index >= _syncCircuits.Length) return; circuit = _syncCircuits[index]; name = _syncNames[index]; }
            else { if (index < 0 || index >= _asyncCircuits.Length) return; circuit = _asyncCircuits[index]; name = _asyncNames[index]; }
            EventMetaDataHandler.OnEventExpectation(value, e);
            if (circuit.TryDisable()) { LayerHub.LayerHub.ReportLayerEventError(-1, name, typeof(T).Name, e); MarkDirty(); }
        }

        public void Post(in T value, int sourceIndex, Propagation propagation)
        {
            EnsureClean();
            var mask = Volatile.Read(ref _subscriberMask);
            if (mask == 0) return;
            var targetMask = mask;
            if (propagation == Propagation.Bubble) targetMask &= Owner._bubbleMasksArr[sourceIndex];
            else if (propagation == Propagation.Drop) targetMask &= Owner._dropMasksArr[sourceIndex];
            if (targetMask != 0) {
                var firstLayer = (propagation == Propagation.Bubble) ? Owner.FindLastBit(targetMask) : Owner.FindFirstBit(targetMask);
                var @event = new Event<T>(value) { TargetMask = targetMask, Propagation = (int)propagation };
                Owner.EnqueueEventInternal(firstLayer, in @event);
                Owner.WakeLayer(firstLayer);
            }
        }

        public void PostLocal(int layerIndex, in T value)
        {
            EnsureClean();
            if ((_subscriberMask & (1UL << layerIndex)) != 0) {
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
