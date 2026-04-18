using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;
using LayerBase.Tools.Job;

namespace LayerBase.Core.Event
{
    internal interface IHandlerBucket { }

    /// <summary>
    /// 熔断器：用于物理级屏蔽发生故障的 Handler。
    /// </summary>
    internal sealed class HandlerCircuit
    {
        private int _disabled;
        public bool IsDisabled => Volatile.Read(ref _disabled) == 1;
        
        /// <summary>
        /// 尝试熔断。如果已经是熔断状态，返回 false。
        /// </summary>
        public bool TryDisable() => Interlocked.Exchange(ref _disabled, 1) == 0;
    }

    internal sealed class HandlerBucket<T> : IHandlerBucket where T : struct
    {
        private static readonly string s_eventFullName = typeof(T).FullName ?? typeof(T).Name;
        private readonly object _lock = new();
        
        // 使用数组以获得极致的遍历性能
        private OrderedHandlerEntry<T>[] _orderedHandlers = Array.Empty<OrderedHandlerEntry<T>>();
        private UnorderedHandlerEntry<T>[] _unorderedHandlers = Array.Empty<UnorderedHandlerEntry<T>>();
        private ParallelHandlerEntry<T>[] _parallelHandlers = Array.Empty<ParallelHandlerEntry<T>>();
        
        private int _totalHandlerCount;

        public bool HasHandlers => Volatile.Read(ref _totalHandlerCount) > 0;

        public void Add(IEventHandler<T> handler)
        {
            lock (_lock)
            {
                var current = _unorderedHandlers;
                var next = new UnorderedHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = UnorderedHandlerEntry<T>.Create(handler);
                Volatile.Write(ref _unorderedHandlers, next);
                Interlocked.Increment(ref _totalHandlerCount);
            }
        }

        public void Add(IEventHandlerAsync<T> handler)
        {
            lock (_lock)
            {
                var current = _unorderedHandlers;
                var next = new UnorderedHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = UnorderedHandlerEntry<T>.Create(handler);
                Volatile.Write(ref _unorderedHandlers, next);
                Interlocked.Increment(ref _totalHandlerCount);
            }
        }

        public void Add(EventHandleDelegate<T> handler)
        {
            lock (_lock)
            {
                var current = _orderedHandlers;
                var next = new OrderedHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = OrderedHandlerEntry<T>.Create(handler);
                Volatile.Write(ref _orderedHandlers, next);
                Interlocked.Increment(ref _totalHandlerCount);
            }
        }

        public void Add(EventHandleDelegateAsync<T> handler)
        {
            lock (_lock)
            {
                var current = _orderedHandlers;
                var next = new OrderedHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = OrderedHandlerEntry<T>.Create(handler);
                Volatile.Write(ref _orderedHandlers, next);
                Interlocked.Increment(ref _totalHandlerCount);
            }
        }

        public void AddParallel(IEventHandler<T> handler, Action<int, string, string, Exception> reportError)
        {
            lock (_lock)
            {
                var current = _parallelHandlers;
                var next = new ParallelHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = ParallelHandlerEntry<T>.Create(handler, reportError);
                Volatile.Write(ref _parallelHandlers, next);
                Interlocked.Increment(ref _totalHandlerCount);
            }
        }

        public void AddParallel(EventHandleDelegate<T> handler, Action<int, string, string, Exception> reportError)
        {
            lock (_lock)
            {
                var current = _parallelHandlers;
                var next = new ParallelHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = ParallelHandlerEntry<T>.Create(handler, reportError);
                Volatile.Write(ref _parallelHandlers, next);
                Interlocked.Increment(ref _totalHandlerCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState Dispatch(int layerIndex, in T value)
        {
            int total = Volatile.Read(ref _totalHandlerCount);
            if (total == 0) return EventHandledState.Continue;

            var ordered = _orderedHandlers;
            // 快速路径：仅一个有序 Handler 时避开 FullDispatch 复杂开销
            if (total == 1 && ordered.Length == 1)
            {
                return InvokeOrderedDirect(layerIndex, in value, in ordered[0]);
            }

            return DispatchFullDirect(layerIndex, in value);
        }

        private EventHandledState DispatchFullDirect(int layerIndex, in T value)
        {
            var parallel = Volatile.Read(ref _parallelHandlers);
            var unordered = Volatile.Read(ref _unorderedHandlers);
            var ordered = Volatile.Read(ref _orderedHandlers);

            // 1. 并行分发
            for (int i = 0; i < parallel.Length; i++)
            {
                parallel[i].Enqueue(layerIndex, in value);
            }

            // 2. 无序分发 (快速)
            for (int i = 0; i < unordered.Length; i++)
            {
                var handler = unordered[i];
                if (handler.Circuit.IsDisabled) continue;

                try
                {
                    if (handler.IsAsync)
                        AsyncFaultContext.Observe(layerIndex, handler.Circuit, handler.FullName, in value, handler.AsyncHandler!.Deal(value));
                    else
                        handler.SyncHandler!.Deal(in value);
                }
                catch (Exception e)
                {
                    // 核心修复：一旦抛出异常，立即触发永久熔断，且本次分发直接跳过该 Handler
                    EventMetaDataHandler.OnEventExpectation(value, e);
                    if (handler.Circuit.TryDisable())
                    {
                        LayerBase.LayerHub.LayerHub.ReportLayerEventError(layerIndex, handler.FullName, s_eventFullName, e);
                    }
                }
            }

            // 3. 有序分发 (拦截)
            bool handledAndContinueSeen = false;
            for (int i = 0; i < ordered.Length; i++)
            {
                var result = InvokeOrderedDirect(layerIndex, in value, in ordered[i]);
                if (result == EventHandledState.Handled) return EventHandledState.Handled;
                if (result == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
            }

            return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState InvokeOrderedDirect(int layerIndex, in T value, in OrderedHandlerEntry<T> handler)
        {
            // 物理级检查：如果已熔断，0 开销跳过
            if (handler.Circuit.IsDisabled) return EventHandledState.Continue;
            
            try
            {
                if (handler.IsAsync)
                {
                    AsyncFaultContext.Observe(layerIndex, handler.Circuit, handler.FullName, in value, handler.AsyncHandler!(value));
                    return EventHandledState.Continue;
                }
                return handler.SyncHandler!(in value);
            }
            catch (Exception e)
            {
                // 核心修复：同步有序 Handler 崩溃时，记录并永久禁用
                EventMetaDataHandler.OnEventExpectation(value, e);
                if (handler.Circuit.TryDisable())
                {
                    LayerBase.LayerHub.LayerHub.ReportLayerEventError(layerIndex, handler.FullName, s_eventFullName, e);
                }
                return EventHandledState.Continue;
            }
        }

        /// <summary>
        /// 异步故障观测器：用于监控异步 LBTask 的最终结果。
        /// </summary>
        private sealed class AsyncFaultContext
        {
            private static readonly ConcurrentBag<AsyncFaultContext> s_pool = new();
            private readonly Action _continuation;
            private int _layerIndex;
            private HandlerCircuit? _circuit;
            private string? _handlerFullName;
            private T _payload;
            private LBTask _task;

            private AsyncFaultContext() { _continuation = Complete; }

            public static void Observe(int layerIndex, HandlerCircuit circuit, string handlerFullName, in T payload, LBTask task)
            {
                if (!s_pool.TryTake(out var context)) context = new AsyncFaultContext();
                context._layerIndex = layerIndex;
                context._circuit = circuit;
                context._handlerFullName = handlerFullName;
                context._payload = payload;
                context._task = task;
                task.GetAwaiter().OnCompleted(context._continuation);
            }

            private void Complete()
            {
                try { _task.GetAwaiter().GetResult(); }
                catch (Exception ex)
                {
                    // 异步崩溃：同样触发熔断
                    EventMetaDataHandler.OnEventExpectation(_payload, ex);
                    if (_circuit != null && _circuit.TryDisable())
                    {
                        LayerBase.LayerHub.LayerHub.ReportLayerEventError(_layerIndex, _handlerFullName!, s_eventFullName, ex);
                    }
                }
                finally
                {
                    _circuit = null; _handlerFullName = null; _payload = default; _task = default;
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

        private OrderedHandlerEntry(EventHandleDelegate<T>? syncHandler, EventHandleDelegateAsync<T>? asyncHandler, string fullName, HandlerCircuit circuit)
        {
            SyncHandler = syncHandler; AsyncHandler = asyncHandler; FullName = fullName; Circuit = circuit;
        }

        public static OrderedHandlerEntry<T> Create(EventHandleDelegate<T> handler) => new(handler, null, GetHandlerFullName(handler), new HandlerCircuit());
        public static OrderedHandlerEntry<T> Create(EventHandleDelegateAsync<T> handler) => new(null, handler, GetHandlerFullName(handler), new HandlerCircuit());

        public bool TryMatch(EventHandleDelegate<T> handler) => SyncHandler != null && Equals(SyncHandler, handler);
        public bool TryMatch(EventHandleDelegateAsync<T> handler) => AsyncHandler != null && Equals(AsyncHandler, handler);

        private static string GetHandlerFullName(Delegate handler)
        {
            var method = handler.Method;
            string typeName = method.DeclaringType?.FullName ?? handler.Target?.GetType()?.FullName ?? "Global";
            string methodName = method.Name;
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

        private UnorderedHandlerEntry(IEventHandler<T>? syncHandler, IEventHandlerAsync<T>? asyncHandler, string fullName, HandlerCircuit circuit)
        {
            SyncHandler = syncHandler; AsyncHandler = asyncHandler; FullName = fullName; Circuit = circuit;
        }

        public static UnorderedHandlerEntry<T> Create(IEventHandler<T> handler) => new(handler, null, handler.GetType().FullName ?? handler.GetType().Name, new HandlerCircuit());
        public static UnorderedHandlerEntry<T> Create(IEventHandlerAsync<T> handler) => new(null, handler, handler.GetType().FullName ?? handler.GetType().Name, new HandlerCircuit());

        public bool TryMatch(IEventHandler<T> handler) => SyncHandler != null && EqualityComparer<IEventHandler<T>>.Default.Equals(SyncHandler, handler);
        public bool TryMatch(IEventHandlerAsync<T> handler) => AsyncHandler != null && EqualityComparer<IEventHandlerAsync<T>>.Default.Equals(AsyncHandler, handler);
    }

    internal readonly struct ParallelHandlerEntry<T> where T : struct
    {
        private readonly ParallelSubscriptionQueue<T> _subscriptionQueue;
        private ParallelHandlerEntry(ParallelSubscriptionQueue<T> subscriptionQueue) { _subscriptionQueue = subscriptionQueue; }
        public static ParallelHandlerEntry<T> Create(IEventHandler<T> handler, Action<int, string, string, Exception> reportError) => new(new ParallelSubscriptionQueue<T>(handler, reportError));
        public static ParallelHandlerEntry<T> Create(EventHandleDelegate<T> handler, Action<int, string, string, Exception> reportError) => new(new ParallelSubscriptionQueue<T>(handler, reportError));
        
        public void Enqueue(int layerIndex, in T value) => _subscriptionQueue.Enqueue(layerIndex, in value);
        public bool TryMatch(IEventHandler<T> handler) => _subscriptionQueue.TryMatch(handler);
        public bool TryMatch(EventHandleDelegate<T> handler) => _subscriptionQueue.TryMatch(handler);
    }

    internal sealed class ParallelSubscriptionQueue<T> where T : struct
    {
        private readonly ConcurrentQueue<T> _events = new();
        private readonly IEventHandler<T>? _syncHandler;
        private readonly EventHandleDelegate<T>? _syncDelegate;
        private readonly HandlerCircuit _circuit;
        private readonly Action<int, string, string, Exception> _reportError;
        private readonly Action _drainAction;
        private readonly string _fullName;
        private readonly string _eventFullName;
        private int _layerIndex;
        private int _scheduled;

        public ParallelSubscriptionQueue(IEventHandler<T> handler, Action<int, string, string, Exception> reportError)
        {
            _syncHandler = handler; _reportError = reportError; _circuit = new HandlerCircuit();
            _fullName = handler.GetType().FullName ?? handler.GetType().Name;
            _eventFullName = typeof(T).FullName ?? typeof(T).Name; _drainAction = Drain;
        }

        public ParallelSubscriptionQueue(EventHandleDelegate<T> handler, Action<int, string, string, Exception> reportError)
        {
            _syncDelegate = handler; _reportError = reportError; _circuit = new HandlerCircuit();
            _fullName = GetHandlerFullName(handler); _eventFullName = typeof(T).FullName ?? typeof(T).Name; _drainAction = Drain;
        }

        public void Enqueue(int layerIndex, in T value) 
        { 
            _layerIndex = layerIndex;
            if (!_circuit.IsDisabled) { _events.Enqueue(value); TryScheduleDrain(); } 
        }
        public bool TryMatch(IEventHandler<T> handler) => _syncHandler != null && EqualityComparer<IEventHandler<T>>.Default.Equals(_syncHandler, handler);
        public bool TryMatch(EventHandleDelegate<T> handler) => _syncDelegate != null && Equals(_syncDelegate, handler);

        private void TryScheduleDrain()
        {
            if (_circuit.IsDisabled) { ClearPending(); return; }
            if (Interlocked.CompareExchange(ref _scheduled, 1, 0) == 0)
            {
                if (!JobSchedulers.Default.TrySchedule(_drainAction))
                    ThreadPool.QueueUserWorkItem(static state => ((ParallelSubscriptionQueue<T>)state!).Drain(), this);
            }
        }

        private void Drain()
        {
            try
            {
                while (_events.TryDequeue(out var payload))
                {
                    if (_circuit.IsDisabled) { ClearPending(); break; }
                    try { if (_syncHandler != null) _syncHandler.Deal(in payload); else _syncDelegate!(in payload); }
                    catch (Exception e)
                    {
                        EventMetaDataHandler.OnEventExpectation(payload, e);
                        if (_circuit.TryDisable()) 
                            _reportError(_layerIndex, _fullName, _eventFullName, e);
                        ClearPending(); break;
                    }
                }
            }
            finally { Volatile.Write(ref _scheduled, 0); if (!_events.IsEmpty) TryScheduleDrain(); }
        }

        private void ClearPending() { while (_events.TryDequeue(out _)) { } }
        private static string GetHandlerFullName(Delegate handler)
        {
            var method = handler.Method;
            string typeName = method.DeclaringType?.FullName ?? handler.Target?.GetType()?.FullName ?? "Global";
            string methodName = method.Name;
            if (methodName.StartsWith("<") && methodName.Contains(">")) methodName = "lambda";
            return $"{typeName}.{methodName}";
        }
    }
}
