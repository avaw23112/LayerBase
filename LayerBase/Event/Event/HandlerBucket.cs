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
    private static readonly string s_eventFullName = typeof(T).FullName ?? typeof(T).Name;
    private readonly object _lock = new();
    
    private OrderedHandlerEntry<T>[] _allOrdered = Array.Empty<OrderedHandlerEntry<T>>();
    private UnorderedHandlerEntry<T>[] _allUnordered = Array.Empty<UnorderedHandlerEntry<T>>();
    
    private OrderedHandlerEntry<T>[] _activeOrdered = Array.Empty<OrderedHandlerEntry<T>>();
    private UnorderedHandlerEntry<T>[] _activeUnordered = Array.Empty<UnorderedHandlerEntry<T>>();
    
    private ParallelHandlerEntry<T>[] _parallelHandlers = Array.Empty<ParallelHandlerEntry<T>>();
    private int _totalHandlerCount;
    private int _isDirty;

    public bool HasHandlers => Volatile.Read(ref _totalHandlerCount) > 0;

    public void Reset()
    {
        lock (_lock)
        {
            foreach (var h in _allOrdered) h.Circuit.Reset();   
            foreach (var h in _allUnordered) h.Circuit.Reset(); 
            foreach (var h in _parallelHandlers) h.Reset();     
            RebuildActiveArrays();
            Volatile.Write(ref _isDirty, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureClean()
    {
        if (Interlocked.CompareExchange(ref _isDirty, 0, 1) == 1)
        {
            lock (_lock)
            {
                RebuildActiveArrays();
            }
        }
    }

    private void RebuildActiveArrays()
    {
        _activeOrdered = _allOrdered.Where(h => !h.Circuit.IsDisabled).ToArray();
        _activeUnordered = _allUnordered.Where(h => !h.Circuit.IsDisabled).ToArray();
    }

    public void Add(IEventHandler<T> handler)
    {
        lock (_lock)
        {
            var entry = UnorderedHandlerEntry<T>.Create(handler);
            _allUnordered = AddToArray(_allUnordered, entry);   
            RebuildActiveArrays();
            Interlocked.Increment(ref _totalHandlerCount);      
        }
    }

    public void Add(IEventHandlerAsync<T> handler)
    {
        lock (_lock)
        {
            var entry = UnorderedHandlerEntry<T>.Create(handler);
            _allUnordered = AddToArray(_allUnordered, entry);   
            RebuildActiveArrays();
            Interlocked.Increment(ref _totalHandlerCount);      
        }
    }

    public void Add(EventHandleDelegate<T> handler)
    {
        lock (_lock)
        {
            var entry = OrderedHandlerEntry<T>.Create(handler); 
            _allOrdered = AddToArray(_allOrdered, entry);       
            RebuildActiveArrays();
            Interlocked.Increment(ref _totalHandlerCount);      
        }
    }

    public void Add(EventHandleDelegateAsync<T> handler)
    {
        lock (_lock)
        {
            var entry = OrderedHandlerEntry<T>.Create(handler); 
            _allOrdered = AddToArray(_allOrdered, entry);       
            RebuildActiveArrays();
            Interlocked.Increment(ref _totalHandlerCount);      
        }
    }

    public void AddParallel(IEventHandler<T> handler, Action<int, string, string, Exception> reportError)
    {
        lock (_lock)
        {
            _parallelHandlers = AddToArray(_parallelHandlers, ParallelHandlerEntry<T>.Create(handler, reportError));
            Interlocked.Increment(ref _totalHandlerCount);      
        }
    }

    public void AddParallel(EventHandleDelegate<T> handler, Action<int, string, string, Exception> reportError)
    {
        lock (_lock)
        {
            _parallelHandlers = AddToArray(_parallelHandlers, ParallelHandlerEntry<T>.Create(handler, reportError));
            Interlocked.Increment(ref _totalHandlerCount);      
        }
    }

    private TEntry[] AddToArray<TEntry>(TEntry[] old, TEntry entry)
    {
        var next = new TEntry[old.Length + 1];
        Array.Copy(old, next, old.Length);
        next[old.Length] = entry;
        return next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState Dispatch(int layerIndex, in T value)
    {
        if (Volatile.Read(ref _isDirty) == 1) EnsureClean();    

        var activeOrdered = Volatile.Read(ref _activeOrdered);  
        var total = Volatile.Read(ref _totalHandlerCount);      
        
        if (total == 0) return EventHandledState.Continue;      
        
        if (total == 1 && activeOrdered.Length == 1)
        {
            try
            {
                return InvokeOrderedDirect(layerIndex, in value, in activeOrdered[0]);
            }
            catch (Exception e)
            {
                HandleOrderedFault(layerIndex, in value, in activeOrdered[0], e);
                return EventHandledState.Continue;
            }
        }
        
        return DispatchFullDirect(layerIndex, in value);        
    }

    private EventHandledState DispatchFullDirect(int layerIndex, in T value)
    {
        var parallel = Volatile.Read(ref _parallelHandlers);    
        var unordered = Volatile.Read(ref _activeUnordered);    
        var ordered = Volatile.Read(ref _activeOrdered);        

        for (var i = 0; i < parallel.Length; i++) parallel[i].Enqueue(layerIndex, in value);

        int uIdx = 0;
        int uLen = unordered.Length;
        try
        {
            for (; uIdx <= uLen - 2; uIdx += 2)
            {
                ref var h1 = ref unordered[uIdx];
                if (h1.IsAsync) AsyncFaultContext.Observe(this, layerIndex, h1.Circuit, h1.FullName, in value, h1.AsyncHandler!.Deal(value));
                else h1.SyncHandler!.Deal(in value);

                uIdx++; // 👈 推进索引，以防第二个报错
                ref var h2 = ref unordered[uIdx];
                if (h2.IsAsync) AsyncFaultContext.Observe(this, layerIndex, h2.Circuit, h2.FullName, in value, h2.AsyncHandler!.Deal(value));
                else h2.SyncHandler!.Deal(in value);
                uIdx--; // 👈 还原，配合循环步长
            }

            for (; uIdx < uLen; uIdx++)
            {
                ref var handler = ref unordered[uIdx];
                if (handler.IsAsync)
                    AsyncFaultContext.Observe(this, layerIndex, handler.Circuit, handler.FullName, in value,
                        handler.AsyncHandler!.Deal(value));
                else handler.SyncHandler!.Deal(in value);
            }
        }
        catch (Exception e)
        {
            HandleUnorderedFault(layerIndex, in value, in unordered[uIdx], e);
            return EventHandledState.Continue;
        }

        int oIdx = 0;
        int oLen = ordered.Length;
        EventHandledState handledAndContinueSeen = EventHandledState.Continue;
        try
        {
            for (; oIdx <= oLen - 2; oIdx += 2)
            {
                var r1 = InvokeOrderedDirect(layerIndex, in value, in ordered[oIdx]);
                if (r1 == EventHandledState.Handled) return EventHandledState.Handled;
                if (r1 == EventHandledState.HandledAndContinue) handledAndContinueSeen = EventHandledState.HandledAndContinue;

                oIdx++; // 👈 推进索引
                var r2 = InvokeOrderedDirect(layerIndex, in value, in ordered[oIdx]);
                if (r2 == EventHandledState.Handled) return EventHandledState.Handled;
                if (r2 == EventHandledState.HandledAndContinue) handledAndContinueSeen = EventHandledState.HandledAndContinue;
                oIdx--; // 👈 还原
            }

            for (; oIdx < oLen; oIdx++)
            {
                var result = InvokeOrderedDirect(layerIndex, in value, in ordered[oIdx]);
                if (result == EventHandledState.Handled) return EventHandledState.Handled;
                if (result == EventHandledState.HandledAndContinue) handledAndContinueSeen = EventHandledState.HandledAndContinue;
            }
        }
        catch (Exception e)
        {
            HandleOrderedFault(layerIndex, in value, in ordered[oIdx], e);
            return EventHandledState.Continue;
        }

        return handledAndContinueSeen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventHandledState InvokeOrderedDirect(int layerIndex, in T value, in OrderedHandlerEntry<T> handler)
    {
        if (handler.IsAsync)
        {
            AsyncFaultContext.Observe(this, layerIndex, handler.Circuit, handler.FullName, in value,
                handler.AsyncHandler!(value));
            return EventHandledState.Continue;
        }
        return handler.SyncHandler!(in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleUnorderedFault(int layerIndex, in T value, in UnorderedHandlerEntry<T> handler, Exception e)
    {
        EventMetaDataHandler.OnEventExpectation(value, e);      
        if (handler.Circuit.TryDisable())
        {
            LayerHub.LayerHub.ReportLayerEventError(layerIndex, handler.FullName, s_eventFullName, e);
            Interlocked.Exchange(ref _isDirty, 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleOrderedFault(int layerIndex, in T value, in OrderedHandlerEntry<T> handler, Exception e)
    {
        EventMetaDataHandler.OnEventExpectation(value, e);      
        if (handler.Circuit.TryDisable())
        {
            LayerHub.LayerHub.ReportLayerEventError(layerIndex, handler.FullName, s_eventFullName, e);
            Interlocked.Exchange(ref _isDirty, 1);
        }
    }

    private sealed class AsyncFaultContext
    {
        private static readonly ConcurrentBag<AsyncFaultContext> s_pool = new();
        private readonly Action _continuation;
        private HandlerBucket<T>? _owner;
        private HandlerCircuit? _circuit;
        private string? _handlerFullName;
        private int _layerIndex;
        private T _payload;
        private LBTask _task;

        private AsyncFaultContext()
        {
            _continuation = Complete;
        }

        public static void Observe(HandlerBucket<T> owner, int layerIndex, HandlerCircuit circuit, string handlerFullName, in T payload,
                                   LBTask task)
        {
            if (!s_pool.TryTake(out var context)) context = new AsyncFaultContext();
            context._owner = owner;
            context._layerIndex = layerIndex;
            context._circuit = circuit;
            context._handlerFullName = handlerFullName;
            context._payload = payload;
            context._task = task;
            task.GetAwaiter().OnCompleted(context._continuation);
        }

        private void Complete()
        {
            try
            {
                _task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                EventMetaDataHandler.OnEventExpectation(_payload, ex);
                if (_circuit != null && _circuit.TryDisable())  
                {
                    LayerHub.LayerHub.ReportLayerEventError(_layerIndex, _handlerFullName!, s_eventFullName, ex);
                    if (_owner != null) Interlocked.Exchange(ref _owner._isDirty, 1);
                }
            }
            finally
            {
                _owner = null;
                _circuit = null;
                _handlerFullName = null;
                _payload = default;
                _task = default;
                s_pool.Add(this);
            }
        }
    }
}

internal readonly struct OrderedHandlerEntry<T> where T : struct
{
    public readonly EventHandleDelegate<T>? SyncHandler;
    public readonly EventHandleDelegateAsync<T>? AsyncHandler;
    public readonly string FullName;
    public readonly HandlerCircuit Circuit;
    public bool IsAsync => AsyncHandler != null;

    private OrderedHandlerEntry(EventHandleDelegate<T>? sync, EventHandleDelegateAsync<T>? async, string name,
                                HandlerCircuit          c)
    {
        SyncHandler = sync;
        AsyncHandler = async;
        FullName = name;
        Circuit = c;
    }

    public static OrderedHandlerEntry<T> Create(EventHandleDelegate<T> h)
    {
        return new OrderedHandlerEntry<T>(h, null, GetHandlerFullName(h), new HandlerCircuit());
    }

    public static OrderedHandlerEntry<T> Create(EventHandleDelegateAsync<T> h)
    {
        return new OrderedHandlerEntry<T>(null, h, GetHandlerFullName(h), new HandlerCircuit());
    }

    private static string GetHandlerFullName(Delegate handler)
    {
        var method = handler.Method;
        var typeName = method.DeclaringType?.FullName ?? handler.Target?.GetType()?.FullName ?? "Global";
        var methodName = method.Name;
        if (methodName.StartsWith("<") && methodName.Contains(">")) methodName = "lambda";
        return $"{typeName}.{methodName}";
    }
}

internal readonly struct UnorderedHandlerEntry<T> where T : struct
{
    public readonly IEventHandler<T>? SyncHandler;
    public readonly IEventHandlerAsync<T>? AsyncHandler;
    public readonly string FullName;
    public readonly HandlerCircuit Circuit;
    public bool IsAsync => AsyncHandler != null;

    private UnorderedHandlerEntry(IEventHandler<T>? sync, IEventHandlerAsync<T>? async, string name, HandlerCircuit c)
    {
        SyncHandler = sync;
        AsyncHandler = async;
        FullName = name;
        Circuit = c;
    }

    public static UnorderedHandlerEntry<T> Create(IEventHandler<T> h)
    {
        return new UnorderedHandlerEntry<T>(h, null, h.GetType().FullName ?? h.GetType().Name, new HandlerCircuit());
    }

    public static UnorderedHandlerEntry<T> Create(IEventHandlerAsync<T> h)
    {
        return new UnorderedHandlerEntry<T>(null, h, h.GetType().FullName ?? h.GetType().Name, new HandlerCircuit());
    }
}

internal readonly struct ParallelHandlerEntry<T> where T : struct
{
    private readonly ParallelSubscriptionQueue<T> _subscriptionQueue;

    private ParallelHandlerEntry(ParallelSubscriptionQueue<T> sq)
    {
        _subscriptionQueue = sq;
    }

    public static ParallelHandlerEntry<T> Create(IEventHandler<T> h, Action<int, string, string, Exception> re)
    {
        return new ParallelHandlerEntry<T>(new ParallelSubscriptionQueue<T>(h, re));
    }

    public static ParallelHandlerEntry<T> Create(EventHandleDelegate<T> h, Action<int, string, string, Exception> re)
    {
        return new ParallelHandlerEntry<T>(new ParallelSubscriptionQueue<T>(h, re));
    }

    public void Enqueue(int layerIndex, in T value)
    {
        _subscriptionQueue.Enqueue(layerIndex, in value);
    }

    public void Reset()
    {
        _subscriptionQueue.Reset();
    }

    public HandlerCircuit Circuit => _subscriptionQueue.Circuit;
}

internal sealed class ParallelSubscriptionQueue<T> where T : struct
{
    private readonly Action _drainAction;
    private readonly string _eventFullName;
    private readonly ConcurrentQueue<T> _events = new();
    private readonly string _fullName;
    private readonly Action<int, string, string, Exception> _reportError;
    private readonly EventHandleDelegate<T>? _syncDelegate;
    private readonly IEventHandler<T>? _syncHandler;
    public readonly HandlerCircuit Circuit;
    private int _layerIndex;
    private int _scheduled;

    public ParallelSubscriptionQueue(IEventHandler<T> h, Action<int, string, string, Exception> re)
    {
        _syncHandler = h;
        _reportError = re;
        Circuit = new HandlerCircuit();
        _fullName = h.GetType().FullName ?? h.GetType().Name;
        _eventFullName = typeof(T).FullName ?? typeof(T).Name;
        _drainAction = Drain;
    }

    public ParallelSubscriptionQueue(EventHandleDelegate<T> h, Action<int, string, string, Exception> re)
    {
        _syncDelegate = h;
        _reportError = re;
        Circuit = new HandlerCircuit();
        _fullName = GetHandlerFullName(h);
        _eventFullName = typeof(T).FullName ?? typeof(T).Name;
        _drainAction = Drain;
    }

    public void Enqueue(int layerIndex, in T value)
    {
        _layerIndex = layerIndex;
        if (!Circuit.IsDisabled)
        {
            _events.Enqueue(value);
            TryScheduleDrain();
        }
    }

    public void Reset()
    {
        Circuit.Reset();
        ClearPending();
    }

    private void TryScheduleDrain()
    {
        if (Circuit.IsDisabled)
        {
            ClearPending();
            return;
        }

        if (Interlocked.CompareExchange(ref _scheduled, 1, 0) == 0)
            if (!JobSchedulers.Default.TrySchedule(_drainAction))
                ThreadPool.QueueUserWorkItem(static state => ((ParallelSubscriptionQueue<T>)state!).Drain(), this);
    }

    private void Drain()
    {
        try
        {
            while (_events.TryDequeue(out var payload))
            {
                if (Circuit.IsDisabled)
                {
                    ClearPending();
                    break;
                }

                try
                {
                    if (_syncHandler != null) _syncHandler.Deal(in payload);
                    else _syncDelegate!(in payload);
                }
                catch (Exception e)
                {
                    EventMetaDataHandler.OnEventExpectation(payload, e);
                    if (Circuit.TryDisable()) _reportError(_layerIndex, _fullName, _eventFullName, e);
                    Interlocked.Exchange(ref _scheduled, 0);    
                    ClearPending();
                    break;
                }
            }
        }
        finally
        {
            Volatile.Write(ref _scheduled, 0);
            if (!_events.IsEmpty) TryScheduleDrain();
        }
    }

    private void ClearPending()
    {
        while (_events.TryDequeue(out _))
        {
        }
    }

    private static string GetHandlerFullName(Delegate handler)
    {
        var method = handler.Method;
        var typeName = method.DeclaringType?.FullName ?? handler.Target?.GetType()?.FullName ?? "Global";
        var methodName = method.Name;
        if (methodName.StartsWith("<") && methodName.Contains(">")) methodName = "lambda";
        return $"{typeName}.{methodName}";
    }
}
