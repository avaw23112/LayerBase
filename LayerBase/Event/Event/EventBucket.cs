using System.Runtime.CompilerServices;
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

internal sealed class EventBucket<T> : GlobalEventCenter.IEventBucket where T : struct
{
    private readonly object _lock = new();
    private readonly GlobalEventCenter _owner;
    private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();

    public GlobalEventCenter Owner => _owner;

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

    public EventBucket(GlobalEventCenter center) => _owner = center;

    public void Reset() { lock (_lock) { foreach (var b in _buckets) b?.Reset(); Rebuild(); } }

    public void MarkDirty() => Interlocked.Exchange(ref _isDirty, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureClean() { if (Volatile.Read(ref _isDirty) == 1) lock (_lock) { if (_isDirty == 1) Rebuild(); } }

    private void Rebuild()
    {
        var sH = new List<EventHandleDelegate<T>>();
        var sC = new List<HandlerCircuit>();
        var sN = new List<string>();

        var aH = new List<EventHandleDelegateAsync<T>>();
        var aC = new List<HandlerCircuit>();
        var aN = new List<string>();

        var pH = new List<ParallelHandlerEntry<T>>();
        var newRanges = new LayerRange[_buckets.Length];
        ulong newMask = 0;

        for (int i = 0; i < _buckets.Length; i++)
        {
            var b = _buckets[i];
            if (b == null || !b.HasHandlers) continue;

            newRanges[i].SyncStart = sH.Count;
            newRanges[i].AsyncStart = aH.Count;

            // 分两次收集，彻底隔离 Sync 和 Async
            Collect(b.MasterOrdered, true, sH, sC, sN, aH, aC, aN);
            Collect(b.MasterUnordered, true, sH, sC, sN, aH, aC, aN);
            
            Collect(b.MasterOrdered, false, sH, sC, sN, aH, aC, aN);
            Collect(b.MasterUnordered, false, sH, sC, sN, aH, aC, aN);

            newRanges[i].SyncCount = sH.Count - newRanges[i].SyncStart;
            newRanges[i].AsyncCount = aH.Count - newRanges[i].AsyncStart;

            newRanges[i].ParallelStart = pH.Count;
            foreach (var h in b.MasterParallel) pH.Add(h);
            newRanges[i].ParallelCount = pH.Count - newRanges[i].ParallelStart;

            if (newRanges[i].SyncCount > 0 || newRanges[i].AsyncCount > 0 || newRanges[i].ParallelCount > 0)
                newMask |= (1UL << i);
        }

        _syncHandlers = sH.ToArray();
        _syncCircuits = sC.ToArray();
        _syncNames = sN.ToArray();
        _asyncHandlers = aH.ToArray();
        _asyncCircuits = aC.ToArray();
        _asyncNames = aN.ToArray();
        _flatParallel = pH.ToArray();
        _ranges = newRanges;
        _subscriberMask = newMask;
        Volatile.Write(ref _isDirty, 0);

        void Collect<TEntry>(List<TEntry> master, bool sync, 
            List<EventHandleDelegate<T>> sh, List<HandlerCircuit> sc, List<string> sn,
            List<EventHandleDelegateAsync<T>> ah, List<HandlerCircuit> ac, List<string> an)
        {
            foreach (var e in master) {
                if (e is OrderedHandlerEntry<T> o) {
                    if (o.Circuit.IsDisabled) continue;
                    if (sync && o.SyncHandler != null) { sh.Add(o.SyncHandler); sc.Add(o.Circuit); sn.Add(o.FullName); }
                    else if (!sync && o.AsyncHandler != null) { ah.Add(o.AsyncHandler); ac.Add(o.Circuit); an.Add(o.FullName); }
                } else if (e is UnorderedHandlerEntry<T> u) {
                    if (u.Circuit.IsDisabled) continue;
                    if (sync && u.SyncHandler != null) { sh.Add((in T val) => { u.SyncHandler.Deal(in val); return EventHandledState.Continue; }); sc.Add(u.Circuit); sn.Add(u.FullName); }
                    else if (!sync && u.AsyncHandler != null) { ah.Add(val => u.AsyncHandler.Deal(val)); ac.Add(u.Circuit); an.Add(u.FullName); }
                }
            }
        }
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
        if (propagation == Propagation.Bubble) targetMask &= _owner._bubbleMasksArr[sourceIndex];
        else targetMask &= _owner._dropMasksArr[sourceIndex];
        if (targetMask == 0) return EventHandledState.Continue;

        int first = _owner.FindFirstBit(targetMask);
        int last = _owner.FindLastBit(targetMask);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventHandledState DispatchSync(int start, int end, in T value)
    {
        var hs = _syncHandlers;
        var handledAndContinueSeen = false;
        int i = start;
        try {
            for (; i <= end - 2; i += 2) {
                var r1 = hs[i](in value);
                if (r1 == EventHandledState.Handled) return EventHandledState.Handled;
                if (r1 == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
                i++;
                var r2 = hs[i](in value);
                if (r2 == EventHandledState.Handled) return EventHandledState.Handled;
                if (r2 == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
                i--;
            }
            for (; i < end; i++) {
                var r = hs[i](in value);
                if (r == EventHandledState.Handled) return EventHandledState.Handled;
                if (r == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
            }
        }
        catch (Exception e) { HandleFault(i, true, in value, e); return EventHandledState.Continue; }
        return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
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
        var handledAndContinueSeen = false;
        int i = end - 1;
        try {
            for (; i >= start; i--) {
                var r = hs[i](in value);
                if (r == EventHandledState.Handled) return EventHandledState.Handled;
                if (r == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
            }
        }
        catch (Exception e) { HandleFault(i, true, in value, e); return EventHandledState.Continue; }
        return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
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
        if (propagation == Propagation.Bubble) targetMask &= _owner._bubbleMasksArr[sourceIndex];
        else if (propagation == Propagation.Drop) targetMask &= _owner._dropMasksArr[sourceIndex];
        if (targetMask != 0) {
            var firstLayer = (propagation == Propagation.Bubble) ? _owner.FindLastBit(targetMask) : _owner.FindFirstBit(targetMask);
            var @event = new Event<T>(value) { TargetMask = targetMask, Propagation = (int)propagation };
            _owner.EnqueueEventInternal(firstLayer, in @event);
            _owner.WakeLayer(firstLayer);
        }
    }

    public void PostLocal(int layerIndex, in T value)
    {
        EnsureClean();
        if ((_subscriberMask & (1UL << layerIndex)) != 0) {
            var @event = new Event<T>(value) { TargetMask = 1UL << layerIndex, Propagation = (int)Propagation.Global };
            _owner.EnqueueEventInternal(layerIndex, in @event);
            _owner.WakeLayer(layerIndex);
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
