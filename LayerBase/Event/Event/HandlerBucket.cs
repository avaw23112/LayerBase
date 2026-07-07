using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;
using LayerBase.Tools.Job;

namespace LayerBase.Core.Event;

/// <summary>
/// 非泛型事件桶接口，用于 IL2CPP 安全的非泛型订阅路径。
/// 避免运行时 MakeGenericMethod，所有 EventBucket&lt;T&gt; 都实现此接口。
/// </summary>
internal interface IEventBucketNonGeneric
{
    void AddFlow(int layerIndex, object handler);
    void AddAsync(int layerIndex, object handler);
    void AddNotify(int layerIndex, object handler);
    void AddSubscribe(int layerIndex, object handler);
    void AddParallel(int layerIndex, object handler, Action<int, int, int, Exception> reportError);
    void RemoveFlow(int layerIndex, object handler);
    void RemoveAsync(int layerIndex, object handler);
    void RemoveNotify(int layerIndex, object handler);
    void RemoveSubscribe(int layerIndex, object handler);
    void RemoveParallel(int layerIndex, object handler);
}

internal interface IHandlerBucket
{
    void Reset();
}

internal sealed class HandlerCircuit
{
    private int _disabled;
    public bool IsDisabled => Volatile.Read(ref _disabled) == 1;

    public bool TryDisable()
    {
        return Interlocked.Exchange(ref _disabled, 1) == 0;
    }

    public void Reset()
    {
        Volatile.Write(ref _disabled, 0);
    }
}

internal sealed class HandlerBucket<T> : IHandlerBucket where T : struct
{
    private readonly object _lock = new();
    private readonly Action _onDirty;
    internal List<NotifyHandlerEntry<T>> MasterNotify = new();
    internal List<OrderedHandlerEntry<T>> MasterOrdered = new();
    internal List<ParallelHandlerEntry<T>> MasterParallel = new();
    internal List<NotifyHandlerEntry<T>> MasterSubscribe = new();
    internal List<UnorderedHandlerEntry<T>> MasterUnordered = new();

    public HandlerBucket(Action onDirty)
    {
        _onDirty = onDirty;
    }

    public bool HasHandlers => MasterOrdered.Count > 0 || MasterUnordered.Count > 0 || MasterParallel.Count > 0 ||
                               MasterNotify.Count > 0 || MasterSubscribe.Count > 0;

    public void Reset()
    {
        lock (_lock)
        {
            foreach (var h in MasterOrdered) h.Circuit.Reset();
            foreach (var h in MasterUnordered) h.Circuit.Reset();
            foreach (var h in MasterParallel) h.Reset();
            foreach (var h in MasterNotify) h.Circuit.Reset();
            foreach (var h in MasterSubscribe) h.Circuit.Reset();
        }
    }

    public void Add(IEventHandler<T> h)
    {
        lock (_lock)
        {
            MasterUnordered = CopyWith(MasterUnordered, UnorderedHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void Add(IEventHandlerAsync<T> h)
    {
        lock (_lock)
        {
            MasterUnordered = CopyWith(MasterUnordered, UnorderedHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void AddNotify(EventNotifyDelegate<T> h)
    {
        lock (_lock)
        {
            MasterNotify = CopyWith(MasterNotify, NotifyHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void AddSubscribe(EventNotifyDelegate<T> h)
    {
        lock (_lock)
        {
            MasterSubscribe = CopyWith(MasterSubscribe, NotifyHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }


    public void Add(EventHandleDelegate<T> h)
    {
        lock (_lock)
        {
            MasterOrdered = CopyWith(MasterOrdered, OrderedHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void Add(EventHandleDelegateAsync<T> h)
    {
        lock (_lock)
        {
            MasterOrdered = CopyWith(MasterOrdered, OrderedHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }


    public void AddParallel(IEventHandler<T> h, Action<int, int, int, Exception> re)
    {
        lock (_lock)
        {
            MasterParallel = CopyWith(MasterParallel, ParallelHandlerEntry<T>.Create(h, re));
            _onDirty();
        }
    }

    public void AddParallel(EventNotifyDelegate<T> h, Action<int, int, int, Exception> re)
    {
        lock (_lock)
        {
            MasterParallel = CopyWith(MasterParallel, ParallelHandlerEntry<T>.Create(h, re));
            _onDirty();
        }
    }

    public void Remove(IEventHandler<T> h)
    {
        lock (_lock)
        {
            MasterUnordered = CopyWithout(MasterUnordered, x => x.Source == h);
            _onDirty();
        }
    }

    public void Remove(IEventHandlerAsync<T> h)
    {
        lock (_lock)
        {
            MasterUnordered = CopyWithout(MasterUnordered, x => x.Source == h);
            _onDirty();
        }
    }

    public void Remove(EventHandleDelegate<T> h)
    {
        lock (_lock)
        {
            MasterOrdered = CopyWithout(MasterOrdered, x => x.SyncHandler == h);
            _onDirty();
        }
    }

    public void RemoveParallel(EventNotifyDelegate<T> h)
    {
        lock (_lock)
        {
            MasterParallel = CopyWithout(MasterParallel, x => x.StopIfSource(h));
            _onDirty();
        }
    }

    public void RemoveParallel(IEventHandler<T> h)
    {
        lock (_lock)
        {
            MasterParallel = CopyWithout(MasterParallel, x => x.StopIfSource(h));
            _onDirty();
        }
    }

    public void Remove(EventHandleDelegateAsync<T> h)
    {
        lock (_lock)
        {
            MasterOrdered = CopyWithout(MasterOrdered, x => x.AsyncHandler == h);
            _onDirty();
        }
    }

    public void RemoveSubscribe(EventNotifyDelegate<T> h)
    {
        lock (_lock)
        {
            MasterSubscribe = CopyWithout(MasterSubscribe, x => x.Handler == h);
            _onDirty();
        }
    }

    public void RemoveNotify(EventNotifyDelegate<T> h)
    {
        lock (_lock)
        {
            MasterNotify = CopyWithout(MasterNotify, x => x.Handler == h);
            _onDirty();
        }
    }

    private static List<TEntry> CopyWith<TEntry>(List<TEntry> source, TEntry entry)
    {
        var next = new List<TEntry>(source.Count + 1);
        next.AddRange(source);
        next.Add(entry);
        return next;
    }

    private static List<TEntry> CopyWithout<TEntry>(List<TEntry> source, Predicate<TEntry> remove)
    {
        var next = new List<TEntry>(source.Count);
        for (var i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (!remove(item)) next.Add(item);
        }

        return next;
    }
}

internal readonly struct NotifyHandlerEntry<T> where T : struct
{
    public readonly EventNotifyDelegate<T> Handler;
    public readonly int HandlerNameId;
    public readonly HandlerCircuit Circuit;
    public readonly object? Source;

    private NotifyHandlerEntry(EventNotifyDelegate<T> h, int n, HandlerCircuit c, object? src)
    {
        Handler = h;
        HandlerNameId = n;
        Circuit = c;
        Source = src;
    }

    public static NotifyHandlerEntry<T> Create(EventNotifyDelegate<T> h)
    {
        return new NotifyHandlerEntry<T>(h, HandlerNameSymbol.FromDelegate(h), new HandlerCircuit(), null);
    }
}

internal readonly struct OrderedHandlerEntry<T> where T : struct
{
    public readonly EventHandleDelegate<T>? SyncHandler;
    public readonly EventHandleDelegateAsync<T>? AsyncHandler;
    public readonly int HandlerNameId;
    public readonly HandlerCircuit Circuit;

    private OrderedHandlerEntry(EventHandleDelegate<T>? s, EventHandleDelegateAsync<T>? a, int n, HandlerCircuit c)
    {
        SyncHandler = s;
        AsyncHandler = a;
        HandlerNameId = n;
        Circuit = c;
    }

    public static OrderedHandlerEntry<T> Create(EventHandleDelegate<T> h)
    {
        return new OrderedHandlerEntry<T>(h, null, HandlerNameSymbol.FromDelegate(h), new HandlerCircuit());
    }

    public static OrderedHandlerEntry<T> Create(EventHandleDelegateAsync<T> h)
    {
        return new OrderedHandlerEntry<T>(null, h, HandlerNameSymbol.FromDelegate(h), new HandlerCircuit());
    }
}

internal readonly struct UnorderedHandlerEntry<T> where T : struct
{
    public readonly EventHandleDelegate<T>? SyncWrapper;
    public readonly EventHandleDelegateAsync<T>? AsyncWrapper;
    public readonly int HandlerNameId;
    public readonly HandlerCircuit Circuit;
    public readonly object Source;

    private UnorderedHandlerEntry(EventHandleDelegate<T>? s, EventHandleDelegateAsync<T>? a, int n, HandlerCircuit c,
                                  object                  src)
    {
        SyncWrapper = s;
        AsyncWrapper = a;
        HandlerNameId = n;
        Circuit = c;
        Source = src;
    }

    public static UnorderedHandlerEntry<T> Create(IEventHandler<T> h)
    {
        return new UnorderedHandlerEntry<T>(
            new SyncHandlerWrapper<T>(h).Invoke,
            null,
            HandlerNameSymbol.FromInstance(h),
            new HandlerCircuit(),
            h);
    }

    public static UnorderedHandlerEntry<T> Create(IEventHandlerAsync<T> h)
    {
        return new UnorderedHandlerEntry<T>(
            null,
            new AsyncHandlerWrapper<T>(h).Invoke,
            HandlerNameSymbol.FromInstance(h),
            new HandlerCircuit(),
            h);
    }


    private sealed class SyncHandlerWrapper<TValue> where TValue : struct
    {
        private readonly IEventHandler<TValue> _handler;

        public SyncHandlerWrapper(IEventHandler<TValue> handler)
        {
            _handler = handler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState Invoke(in TValue val)
        {
            _handler.Deal(in val);
            return EventHandledState.Continue;
        }
    }

    private sealed class AsyncHandlerWrapper<TValue> where TValue : struct
    {
        private readonly IEventHandlerAsync<TValue> _handler;

        public AsyncHandlerWrapper(IEventHandlerAsync<TValue> handler)
        {
            _handler = handler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LBTask Invoke(TValue val)
        {
            return _handler.Deal(val);
        }
    }
}

internal readonly struct ParallelHandlerEntry<T> where T : struct
{
    private readonly ParallelSubscriptionQueue<T> _q;
    public object Source => _q.Source;

    private ParallelHandlerEntry(ParallelSubscriptionQueue<T> q)
    {
        _q = q;
    }

    public static ParallelHandlerEntry<T> Create(IEventHandler<T> h, Action<int, int, int, Exception> re)
    {
        return new ParallelHandlerEntry<T>(new ParallelSubscriptionQueue<T>(h, re));
    }

    public static ParallelHandlerEntry<T> Create(EventNotifyDelegate<T> h, Action<int, int, int, Exception> re)
    {
        return new ParallelHandlerEntry<T>(new ParallelSubscriptionQueue<T>(h, re));
    }

    public void Enqueue(int l, in T v)
    {
        _q.Enqueue(l, in v);
    }

    public void Reset()
    {
        _q.Reset();
    }

    public void Stop()
    {
        _q.Stop();
    }

    public bool StopIfSource(EventNotifyDelegate<T> h)
    {
        if (!_q.HasSource(h)) return false;
        Stop();
        return true;
    }

    public bool StopIfSource(IEventHandler<T> h)
    {
        if (!_q.HasSource(h)) return false;
        Stop();
        return true;
    }

    public HandlerCircuit Circuit => _q.Circuit;
}

internal sealed class ParallelSubscriptionQueue<T> where T : struct
{
    private readonly Action _drainInstance;
    private readonly int _eventNameId, _handlerNameId;
    private readonly Action<int, int, int, Exception> _err;
    private readonly ConcurrentQueue<T> _evs = new();
    private readonly EventNotifyDelegate<T>? _sd;
    private readonly IEventHandler<T>? _sh;
    public readonly HandlerCircuit Circuit = new();
    private int _lIdx, _sched;

    public ParallelSubscriptionQueue(IEventHandler<T> h, Action<int, int, int, Exception> re)
    {
        _sh = h;
        _err = re;
        _handlerNameId = HandlerNameSymbol.FromInstance(h);
        _eventNameId = EventTypeSymbol<T>.NameId;
        _drainInstance = Drain;
    }

    public ParallelSubscriptionQueue(EventNotifyDelegate<T> h, Action<int, int, int, Exception> re)
    {
        _sd = h;
        _err = re;
        _handlerNameId = HandlerNameSymbol.FromDelegate(h);
        _eventNameId = EventTypeSymbol<T>.NameId;
        _drainInstance = Drain;
    }

    public object Source => (object?)_sh ?? _sd!;

    public bool HasSource(EventNotifyDelegate<T> h)
    {
        return _sd == h;
    }

    public bool HasSource(IEventHandler<T> h)
    {
        return ReferenceEquals(_sh, h);
    }

    public void Enqueue(int l, in T v)
    {
        _lIdx = l;
        if (!Circuit.IsDisabled)
        {
            _evs.Enqueue(v);
            TrySched();
        }
    }

    public void Reset()
    {
        Circuit.Reset();
        while (_evs.TryDequeue(out _)) ;
    }

    public void Stop()
    {
        Circuit.TryDisable();
        while (_evs.TryDequeue(out _)) ;
    }

    private void TrySched()
    {
        if (!Circuit.IsDisabled && Interlocked.CompareExchange(ref _sched, 1, 0) == 0)
            if (!JobSchedulers.Default.TrySchedule(_drainInstance))
                ThreadPool.QueueUserWorkItem(_ => Drain());
    }

    private void Drain()
    {
        try
        {
            while (_evs.TryDequeue(out var p))
            {
                if (Circuit.IsDisabled) break;
                try
                {
                    if (_sh != null) _sh.Deal(in p);
                    else _sd!(in p);
                }
                catch (Exception e)
                {
                    EventMetaDataHandler.OnEventExpectation(p, e);
                    if (Circuit.TryDisable()) _err(_lIdx, _handlerNameId, _eventNameId, e);
                    break;
                }
            }
        }
        finally
        {
            Volatile.Write(ref _sched, 0);
            if (!_evs.IsEmpty) TrySched();
        }
    }
}