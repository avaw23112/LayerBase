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
#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER || NET8_0_OR_GREATER
        return System.Numerics.BitOperations.TrailingZeroCount(mask);
#else
        return TrailingZeroCountFallback(mask);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int FindLastBit(ulong mask)
    {
        if (mask == 0) return -1;
#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER || NET8_0_OR_GREATER
        return 63 - System.Numerics.BitOperations.LeadingZeroCount(mask);
#else
        for (var i = 63; i >= 0; i--) if ((mask & (1UL << i)) != 0) return i;
        return -1;
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

    private interface IEventBucket { void Reset(); }

    private struct LayerRange { public int Start; public int Count; public int ParallelStart; public int ParallelCount; }

    private sealed class EventBucket<T> : IEventBucket where T : struct
    {
        private readonly object _lock = new();
        public readonly GlobalEventCenter Owner;
        private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();
        
        private EventHandleDelegate<T>[] _orderedSyncs = Array.Empty<EventHandleDelegate<T>>();
        private EventHandleDelegateAsync<T>?[] _orderedAsyncs = Array.Empty<EventHandleDelegateAsync<T>>();
        private HandlerCircuit[] _orderedCircuits = Array.Empty<HandlerCircuit>();
        private string[] _orderedNames = Array.Empty<string>();

        private ParallelHandlerEntry<T>[] _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();
        private LayerRange[] _ranges = Array.Empty<LayerRange>();
        private ulong _subscriberMask;
        private int _isDirty;

        public EventBucket(GlobalEventCenter center) => Owner = center;

        public void Reset() { lock (_lock) { foreach (var b in _buckets) b?.Reset(); Rebuild(); } }

        internal void MarkDirty() => Interlocked.Exchange(ref _isDirty, 1);

        private void EnsureClean() { if (Volatile.Read(ref _isDirty) == 1) lock (_lock) { if (_isDirty == 1) Rebuild(); } }

        private void Rebuild()
        {
            var syncList = new List<EventHandleDelegate<T>>();
            var asyncList = new List<EventHandleDelegateAsync<T>?>();
            var circuitList = new List<HandlerCircuit>();
            var nameList = new List<string>();
            var parallelList = new List<ParallelHandlerEntry<T>>();
            
            var newRanges = new LayerRange[_buckets.Length];
            ulong newMask = 0;

            for (int i = 0; i < _buckets.Length; i++)
            {
                var b = _buckets[i];
                if (b == null || !b.HasHandlers) continue;

                newRanges[i].Start = syncList.Count;
                foreach (var h in b.MasterOrdered) AddToSoa(h);
                foreach (var h in b.MasterUnordered) AddToSoa(OrderedHandlerEntry<T>.Convert(h));
                newRanges[i].Count = syncList.Count - newRanges[i].Start;

                newRanges[i].ParallelStart = parallelList.Count;
                foreach (var h in b.MasterParallel) parallelList.Add(h);
                newRanges[i].ParallelCount = parallelList.Count - newRanges[i].ParallelStart;

                if (newRanges[i].Count > 0 || newRanges[i].ParallelCount > 0) newMask |= (1UL << i);
            }

            _orderedSyncs = syncList.ToArray();
            _orderedAsyncs = asyncList.ToArray();
            _orderedCircuits = circuitList.ToArray();
            _orderedNames = nameList.ToArray();
            _flatParallel = parallelList.ToArray();
            _ranges = newRanges;
            _subscriberMask = newMask;
            Volatile.Write(ref _isDirty, 0);

            void AddToSoa(in OrderedHandlerEntry<T> h)
            {
                if (h.Circuit.IsDisabled) return;
                syncList.Add(h.SyncHandler ?? ((in T _) => EventHandledState.Continue));
                asyncList.Add(h.AsyncHandler);
                circuitList.Add(h.Circuit);
                nameList.Add(h.FullName);
            }
        }

        public void Add(int layerIndex, IEventHandler<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void Add(int layerIndex, IEventHandlerAsync<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void AddParallel(int layerIndex, IEventHandler<T> h, Action<int, string, string, Exception> re) { GetOrCreate(layerIndex).AddParallel(h, re); }
        public void Add(int layerIndex, EventHandleDelegate<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void Add(int layerIndex, EventHandleDelegateAsync<T> h) { GetOrCreate(layerIndex).Add(h); MarkDirty(); }
        public void AddParallel(int layerIndex, EventHandleDelegate<T> h, Action<int, string, string, Exception> re) { GetOrCreate(layerIndex).AddParallel(h, re); }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState Dispatch(in T value, int sourceIndex, Propagation propagation)
        {
            EnsureClean();
            var mask = Volatile.Read(ref _subscriberMask);
            if (mask == 0) return EventHandledState.Continue;

            if (propagation == Propagation.Global)
            {
                var parallels = _flatParallel;
                for(int j=0; j<parallels.Length; j++) parallels[j].Enqueue(-1, in value);
                return DispatchForward(in value, 0, _orderedSyncs.Length);
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
                    for(int j=0; j<r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(l, in value);
                    var state = DispatchBackward(in value, r.Start, r.Start + r.Count);
                    if (state == EventHandledState.Handled) return EventHandledState.Handled;
                }
                return EventHandledState.Continue;
            }
            else
            {
                for (int l = first; l <= last; l++)
                {
                    if ((targetMask & (1UL << l)) == 0) continue;
                    var r = _ranges[l];
                    for(int j=0; j<r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(l, in value);
                    var state = DispatchForward(in value, r.Start, r.Start + r.Count);
                    if (state == EventHandledState.Handled) return EventHandledState.Handled;
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
            for(int j=0; j<r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(layerIndex, in value);
            return DispatchForward(in value, r.Start, r.Start + r.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchForward(in T value, int start, int end)
        {
            var syncs = _orderedSyncs;
            var handledAndContinueSeen = false;
            int i = start;
            try
            {
                for (; i <= end - 2; i += 2)
                {
                    var r1 = Invoke(i, in value);
                    if (r1 == EventHandledState.Handled) return EventHandledState.Handled;
                    if (r1 == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
                    i++;
                    var r2 = Invoke(i, in value);
                    if (r2 == EventHandledState.Handled) return EventHandledState.Handled;
                    if (r2 == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
                    i--;
                }
                for (; i < end; i++)
                {
                    var r = Invoke(i, in value);
                    if (r == EventHandledState.Handled) return EventHandledState.Handled;
                    if (r == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
                }
            }
            catch (Exception e) { HandleFault(i, in value, e); return EventHandledState.Continue; }
            return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchBackward(in T value, int start, int end)
        {
            var syncs = _orderedSyncs;
            var handledAndContinueSeen = false;
            int i = end - 1;
            try
            {
                for (; i >= start; i--)
                {
                    var r = Invoke(i, in value);
                    if (r == EventHandledState.Handled) return EventHandledState.Handled;
                    if (r == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
                }
            }
            catch (Exception e) { HandleFault(i, in value, e); return EventHandledState.Continue; }
            return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState Invoke(int idx, in T value)
        {
            var ah = _orderedAsyncs[idx];
            if (ah != null)
            {
                AsyncFaultContext<T>.Observe(this, -1, _orderedCircuits[idx], _orderedNames[idx], in value, ah(value)); return EventHandledState.Continue; 
            }
            return _orderedSyncs[idx](in value);
        }

        private void HandleFault(int index, in T value, Exception e)
        {
            if (index < 0 || index >= _orderedCircuits.Length) return;
            var circuit = _orderedCircuits[index];
            var name = _orderedNames[index];
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

            if (targetMask != 0)
            {
                var firstLayer = (propagation == Propagation.Bubble) ? Owner.FindLastBit(targetMask) : Owner.FindFirstBit(targetMask);
                var @event = new Event<T>(value) { TargetMask = targetMask, Propagation = (int)propagation };
                Owner.EnqueueEventInternal(firstLayer, in @event);
                Owner.WakeLayer(firstLayer);
            }
        }

        public void PostLocal(int layerIndex, in T value)
        {
            EnsureClean();
            if ((_subscriberMask & (1UL << layerIndex)) != 0)
            {
                var @event = new Event<T>(value) { TargetMask = 1UL << layerIndex, Propagation = (int)Propagation.Global };
                Owner.EnqueueEventInternal(layerIndex, in @event);
                Owner.WakeLayer(layerIndex);
            }
        }
    }

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

#if !NETCOREAPP3_0_OR_GREATER && !NET5_0_OR_GREATER && !NET8_0_OR_GREATER
    private static readonly byte[] DeBruijnTable = { 0, 1, 56, 2, 57, 49, 28, 3, 61, 58, 42, 50, 38, 29, 17, 4, 62, 47, 59, 36, 45, 43, 51, 22, 53, 39, 33, 30, 24, 18, 12, 5, 63, 55, 48, 27, 60, 41, 37, 16, 46, 35, 44, 21, 52, 32, 23, 11, 54, 26, 40, 15, 34, 20, 31, 10, 25, 14, 19, 9, 13, 8, 7, 6 };
    private static int TrailingZeroCountFallback(ulong v) => DeBruijnTable[((ulong)((long)v & -(long)v) * 0x03F79D71B4CB0A89UL) >> 58];
#endif

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
