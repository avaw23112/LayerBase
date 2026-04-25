using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;
using LayerBase.Tools.Job;

namespace LayerBase.Core.Event;

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
    internal List<UnorderedHandlerEntry<T>> MasterUnordered = new();

    public HandlerBucket(Action onDirty)
    {
        _onDirty = onDirty;
    }

    public bool HasHandlers => MasterOrdered.Count > 0 || MasterUnordered.Count > 0 || MasterParallel.Count > 0 ||
                               MasterNotify.Count > 0;

    public void Reset()
    {
        lock (_lock)
        {
            foreach (var h in MasterOrdered) h.Circuit.Reset();
            foreach (var h in MasterUnordered) h.Circuit.Reset();
            foreach (var h in MasterParallel) h.Reset();
            foreach (var h in MasterNotify) h.Circuit.Reset();
        }
    }

    public void Add(IEventHandler<T> h)
    {
        lock (_lock)
        {
            MasterUnordered.Add(UnorderedHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void Add(IEventHandlerAsync<T> h)
    {
        lock (_lock)
        {
            MasterUnordered.Add(UnorderedHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void AddNotify(EventNotifyDelegate<T> h)
    {
        lock (_lock)
        {
            MasterNotify.Add(NotifyHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void Add(EventHandleDelegate<T> h)
    {
        lock (_lock)
        {
            MasterOrdered.Add(OrderedHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void Add(EventHandleDelegateAsync<T> h)
    {
        lock (_lock)
        {
            MasterOrdered.Add(OrderedHandlerEntry<T>.Create(h));
            _onDirty();
        }
    }

    public void AddParallel(IEventHandler<T> h, Action<int, string, string, Exception> re)
    {
        lock (_lock)
        {
            MasterParallel.Add(ParallelHandlerEntry<T>.Create(h, re));
            _onDirty();
        }
    }

    public void AddParallel(EventHandleDelegate<T> h, Action<int, string, string, Exception> re)
    {
        lock (_lock)
        {
            MasterParallel.Add(ParallelHandlerEntry<T>.Create(h, re));
            _onDirty();
        }
    }

    public void Remove(IEventHandler<T> h)
    {
        lock (_lock)
        {
            MasterUnordered.RemoveAll(x => x.Source == h);
            _onDirty();
        }
    }

    public void Remove(IEventHandlerAsync<T> h)
    {
        lock (_lock)
        {
            MasterUnordered.RemoveAll(x => x.Source == h);
            _onDirty();
        }
    }

    public void Remove(EventHandleDelegate<T> h)
    {
        lock (_lock)
        {
            MasterOrdered.RemoveAll(x => x.SyncHandler == h);
            MasterParallel.RemoveAll(x => x.Source == h);
            _onDirty();
        }
    }

    public void Remove(EventHandleDelegateAsync<T> h)
    {
        lock (_lock)
        {
            MasterOrdered.RemoveAll(x => x.AsyncHandler == h);
            _onDirty();
        }
    }

    public void RemoveNotify(EventNotifyDelegate<T> h)
    {
        lock (_lock)
        {
            MasterNotify.RemoveAll(x => x.Handler == h);
            _onDirty();
        }
    }
}

internal readonly struct NotifyHandlerEntry<T> where T : struct
{
    public readonly EventNotifyDelegate<T> Handler;
    public readonly string FullName;
    public readonly HandlerCircuit Circuit;

    private NotifyHandlerEntry(EventNotifyDelegate<T> h, string n, HandlerCircuit c)
    {
        Handler = h;
        FullName = n;
        Circuit = c;
    }

    public static NotifyHandlerEntry<T> Create(EventNotifyDelegate<T> h)
    {
        return new NotifyHandlerEntry<T>(h, GetName(h), new HandlerCircuit());
    }

    private static string GetName(Delegate d)
    {
        var m = d.Method;
        var t = m.DeclaringType?.FullName ?? d.Target?.GetType()?.FullName ?? "Global";
        var nm = m.Name;
        if (nm.StartsWith("<") && nm.Contains(">")) nm = "lambda";
        return $"{t}.{nm}";
    }
}

internal readonly struct OrderedHandlerEntry<T> where T : struct
{
    public readonly EventHandleDelegate<T>? SyncHandler;
    public readonly EventHandleDelegateAsync<T>? AsyncHandler;
    public readonly string FullName;
    public readonly HandlerCircuit Circuit;

    private OrderedHandlerEntry(EventHandleDelegate<T>? s, EventHandleDelegateAsync<T>? a, string n, HandlerCircuit c)
    {
        SyncHandler = s;
        AsyncHandler = a;
        FullName = n;
        Circuit = c;
    }

    public static OrderedHandlerEntry<T> Create(EventHandleDelegate<T> h)
    {
        return new OrderedHandlerEntry<T>(h, null, GetName(h), new HandlerCircuit());
    }

    public static OrderedHandlerEntry<T> Create(EventHandleDelegateAsync<T> h)
    {
        return new OrderedHandlerEntry<T>(null, h, GetName(h), new HandlerCircuit());
    }

    private static string GetName(Delegate d)
    {
        var m = d.Method;
        var t = m.DeclaringType?.FullName ?? d.Target?.GetType()?.FullName ?? "Global";
        var nm = m.Name;
        if (nm.StartsWith("<") && nm.Contains(">")) nm = "lambda";
        return $"{t}.{nm}";
    }
}

internal readonly struct UnorderedHandlerEntry<T> where T : struct
{
    public readonly EventHandleDelegate<T>? SyncWrapper;
    public readonly EventHandleDelegateAsync<T>? AsyncWrapper;
    public readonly string FullName;
    public readonly HandlerCircuit Circuit;
    public readonly object Source;

    private UnorderedHandlerEntry(EventHandleDelegate<T>? s, EventHandleDelegateAsync<T>? a, string n, HandlerCircuit c,
                                  object                  src)
    {
        SyncWrapper = s;
        AsyncWrapper = a;
        FullName = n;
        Circuit = c;
        Source = src;
    }

    public static UnorderedHandlerEntry<T> Create(IEventHandler<T> h)
    {
        // 核心优化：使用实例方法引用代�?Lambda 闭包，消除订阅时的内存分�?
        return new UnorderedHandlerEntry<T>(
            new SyncHandlerWrapper<T>(h).Invoke,
            null,
            h.GetType().Name,
            new HandlerCircuit(),
            h);
    }

    public static UnorderedHandlerEntry<T> Create(IEventHandlerAsync<T> h)
    {
        return new UnorderedHandlerEntry<T>(
            null,
            new AsyncHandlerWrapper<T>(h).Invoke,
            h.GetType().Name,
            new HandlerCircuit(),
            h);
    }

    // 内部包装类，利用实例方法直接绑定，消除闭包开销
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

    public static ParallelHandlerEntry<T> Create(IEventHandler<T> h, Action<int, string, string, Exception> re)
    {
        return new ParallelHandlerEntry<T>(new ParallelSubscriptionQueue<T>(h, re));
    }

    public static ParallelHandlerEntry<T> Create(EventHandleDelegate<T> h, Action<int, string, string, Exception> re)
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

    public HandlerCircuit Circuit => _q.Circuit;
}

internal sealed class ParallelSubscriptionQueue<T> where T : struct
{
    private readonly Action _drainInstance;
    private readonly string _eName, _fName;
    private readonly Action<int, string, string, Exception> _err;
    private readonly ConcurrentQueue<T> _evs = new();
    private readonly EventHandleDelegate<T>? _sd;
    private readonly IEventHandler<T>? _sh;
    public readonly HandlerCircuit Circuit = new();
    private int _lIdx, _sched;

    public ParallelSubscriptionQueue(IEventHandler<T> h, Action<int, string, string, Exception> re)
    {
        _sh = h;
        _err = re;
        _fName = h.GetType().Name;
        _eName = typeof(T).Name;
        _drainInstance = Drain;
    }

    public ParallelSubscriptionQueue(EventHandleDelegate<T> h, Action<int, string, string, Exception> re)
    {
        _sd = h;
        _err = re;
        _fName = "Delegate";
        _eName = typeof(T).Name;
        _drainInstance = Drain;
    }

    public object Source => (object?)_sh ?? _sd!;

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

    private void TrySched()
    {
        if (!Circuit.IsDisabled && Interlocked.CompareExchange(ref _sched, 1, 0) == 0)
            // 使用构造函数中创建好的实例委托，热路径零分�?
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
                    if (Circuit.TryDisable()) _err(_lIdx, _fName, _eName, e);
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

