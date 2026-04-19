using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.Event;

internal struct LayerRange
{
    public int SyncStart;
    public int SyncCount;
    public int AsyncStart;
    public int AsyncCount;
    public int ParallelStart;
    public int ParallelCount;
}

internal sealed class EventBucket<T> : IEventBucket where T : struct
{
    private readonly object _lock = new();
    private readonly GlobalEventCenter _owner;
    private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();

    public GlobalEventCenter Owner => _owner;

    private EventHandleDelegate<T>[] _syncHandlers = Array.Empty<EventHandleDelegate<T>>();
    private HandlerCircuit[] _syncCircuits = Array.Empty<HandlerCircuit>();
    private string[] _syncNames = Array.Empty<string>();

    private EventHandleDelegateAsync<T>[] _asyncHandlers = Array.Empty<EventHandleDelegateAsync<T>>();
    private HandlerCircuit[] _asyncCircuits = Array.Empty<HandlerCircuit>();
    private string[] _asyncNames = Array.Empty<string>();

    private ParallelHandlerEntry<T>[] _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();
    private LayerRange[] _ranges = Array.Empty<LayerRange>();
    private ulong _subscriberMask;
    private int _isDirty;

    public EventBucket(GlobalEventCenter center) => _owner = center;

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
        if (propagation == Propagation.Global) {
            var ps = _flatParallel; for (int j = 0; j < ps.Length; j++) ps[j].Enqueue(-1, in value);
            var res = DispatchSync(0, _syncHandlers.Length, in value);
            if (res == EventHandledState.Handled) return res;
            DispatchAsync(0, _asyncHandlers.Length, in value); return res;
        }
        var targetMask = mask;
        if (propagation == Propagation.Bubble) targetMask &= _owner._bubbleMasksArr[sourceIndex];
        else targetMask &= _owner._dropMasksArr[sourceIndex];
        if (targetMask == 0) return EventHandledState.Continue;
        int first = _owner.FindFirstBit(targetMask), last = _owner.FindLastBit(targetMask);
        if (propagation == Propagation.Bubble) {
            for (int l = last; l >= first; l--) {
                if ((targetMask & (1UL << l)) == 0) continue;
                var r = _ranges[l]; for(int j = 0; j < r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(l, in value);
                var s = DispatchSyncBackward(r.SyncStart, r.SyncStart + r.SyncCount, in value);
                if (s == EventHandledState.Handled) return s;
                DispatchAsyncBackward(r.AsyncStart, r.AsyncStart + r.AsyncCount, in value);
            }
            return EventHandledState.Continue;
        } else {
            for (int l = first; l <= last; l++) {
                if ((targetMask & (1UL << l)) == 0) continue;
                var r = _ranges[l]; for(int j = 0; j < r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(l, in value);
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
        var r = _ranges[layerIndex]; for (int j = 0; j < r.ParallelCount; j++) _flatParallel[r.ParallelStart + j].Enqueue(layerIndex, in value);
        var s = DispatchSync(r.SyncStart, r.SyncStart + r.SyncCount, in value);
        if (s == EventHandledState.Handled) return s;
        DispatchAsync(r.AsyncStart, r.AsyncStart + r.AsyncCount, in value); return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventHandledState DispatchSync(int start, int end, in T value)
    {
        var hs = _syncHandlers; int combinedState = 0, i = start;
        try {
            for (; i <= end - 2; i += 2) {
                var r1 = hs[i](in value); var r2 = hs[i+1](in value); combinedState |= (int)r1 | (int)r2;
                if ((combinedState & 1) != 0) return EventHandledState.Handled;
            }
            for (; i < end; i++) { combinedState |= (int)hs[i](in value); if ((combinedState & 1) != 0) return EventHandledState.Handled; }
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
    private EventHandledState DispatchSyncBackward(int start, int end, in T value)
    {
        var hs = _syncHandlers; int combinedState = 0, i = end - 1;
        try {
            for (; i >= start; i--) { combinedState |= (int)hs[i](in value); if ((combinedState & 1) != 0) return EventHandledState.Handled; }
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
        HandlerCircuit circuit; string name;
        if (isSync) { if (index < 0 || index >= _syncCircuits.Length) return; circuit = _syncCircuits[index]; name = _syncNames[index]; }
        else { if (index < 0 || index >= _asyncCircuits.Length) return; circuit = _asyncCircuits[index]; name = _asyncNames[index]; }
        EventMetaDataHandler.OnEventExpectation(value, e);
        if (circuit.TryDisable()) { LayerHub.LayerHub.ReportLayerEventError(-1, name, typeof(T).Name, e); MarkDirty(); }
    }

    public void Post(in T value, int sourceIndex, Propagation propagation)
    {
        EnsureClean();
        var mask = Volatile.Read(ref _subscriberMask); if (mask == 0) return;
        var targetMask = mask;
        if (propagation == Propagation.Bubble) targetMask &= _owner._bubbleMasksArr[sourceIndex];
        else if (propagation == Propagation.Drop) targetMask &= _owner._dropMasksArr[sourceIndex];
        if (targetMask != 0) {
            var firstLayer = (propagation == Propagation.Bubble) ? _owner.FindLastBit(targetMask) : _owner.FindFirstBit(targetMask);
            var @event = new Event<T>(value) { TargetMask = targetMask, Propagation = (int)propagation };
            _owner.EnqueueEventInternal(firstLayer, in @event); _owner.WakeLayer(firstLayer);
        }
    }

    public void PostLocal(int layerIndex, in T value)
    {
        EnsureClean();
        if ((_subscriberMask & (1UL << layerIndex)) != 0) {
            var @event = new Event<T>(value) { TargetMask = 1UL << layerIndex, Propagation = (int)Propagation.Global };
            _owner.EnqueueEventInternal(layerIndex, in @event); _owner.WakeLayer(layerIndex);
        }
    }

    private HandlerBucket<T> GetOrCreate(int layerIndex)
    {
        if (layerIndex >= _buckets.Length) lock (_lock) { if (layerIndex >= _buckets.Length) {
            var next = new HandlerBucket<T>?[Math.Max(layerIndex + 1, _buckets.Length * 2)];
            Array.Copy(_buckets, next, _buckets.Length); _buckets = next;
        } }
        var b = _buckets[layerIndex];
        if (b == null) lock (_lock) { b = _buckets[layerIndex] ??= new HandlerBucket<T>(MarkDirty); }
        return b;
    }
}
