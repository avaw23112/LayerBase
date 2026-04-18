using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;
using LayerBase.Tools.Job;

namespace LayerBase.Core.Event
{
    internal interface IHandlerBucket { }

    internal sealed class HandlerCircuit
    {
        private int _disabled;
        public bool IsDisabled => Volatile.Read(ref _disabled) == 1;
        public bool TryDisable() => Interlocked.Exchange(ref _disabled, 1) == 0;
    }

    internal sealed class HandlerBucket<T> : IHandlerBucket where T : struct
    {
        private static readonly string s_eventFullName = typeof(T).FullName ?? typeof(T).Name;
        private readonly object _lock = new();
        private OrderedHandlerEntry<T>[] _orderedHandlers = Array.Empty<OrderedHandlerEntry<T>>();
        private UnorderedHandlerEntry<T>[] _unorderedHandlers = Array.Empty<UnorderedHandlerEntry<T>>();
        private ParallelHandlerEntry<T>[] _parallelHandlers = Array.Empty<ParallelHandlerEntry<T>>();

        public bool HasHandlers =>
            Volatile.Read(ref _orderedHandlers).Length != 0 ||
            Volatile.Read(ref _unorderedHandlers).Length != 0 ||
            Volatile.Read(ref _parallelHandlers).Length != 0;

        public void Add(IEventHandler<T> handler)
        {
            lock (_lock)
            {
                var current = _unorderedHandlers;
                var next = new UnorderedHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = UnorderedHandlerEntry<T>.Create(handler);
                Volatile.Write(ref _unorderedHandlers, next);
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
            }
        }

        public void AddParallel(IEventHandler<T> handler, Action<string, string, string, Exception> reportError)
        {
            lock (_lock)
            {
                var current = _parallelHandlers;
                var next = new ParallelHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = ParallelHandlerEntry<T>.Create(handler, reportError);
                Volatile.Write(ref _parallelHandlers, next);
            }
        }

        public void AddParallel(EventHandleDelegate<T> handler, Action<string, string, string, Exception> reportError)
        {
            lock (_lock)
            {
                var current = _parallelHandlers;
                var next = new ParallelHandlerEntry<T>[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = ParallelHandlerEntry<T>.Create(handler, reportError);
                Volatile.Write(ref _parallelHandlers, next);
            }
        }

        public EventHandledState Dispatch(string layerFullName, in Event<T> @event)
        {
            var parallelHandlers = Volatile.Read(ref _parallelHandlers);
            var unorderedHandlers = Volatile.Read(ref _unorderedHandlers);
            var orderedHandlers = Volatile.Read(ref _orderedHandlers);
            if (parallelHandlers.Length == 0 && unorderedHandlers.Length == 0 && orderedHandlers.Length == 0)
            {
                return EventHandledState.Continue;
            }

            for (int i = 0; i < parallelHandlers.Length; i++)
                parallelHandlers[i].Enqueue(layerFullName, in @event);

            for (int i = 0; i < unorderedHandlers.Length; i++)
            {
                var handler = unorderedHandlers[i];
                if (handler.Circuit.IsDisabled) continue;

                try
                {
                    if (handler.IsAsync)
                    {
                        var payload = @event.Value;
                        AsyncFaultContext.Observe(layerFullName, handler.Circuit, handler.FullName, in payload, handler.AsyncHandler!.Deal(payload));
                    }
                    else
                    {
                        handler.SyncHandler!.Deal(in @event.Value);
                    }
                }
                catch (Exception e)
                {
                    EventMetaDataHandler.OnEventExpectation(@event.Value, e);
                    if (handler.Circuit.TryDisable())
                        LayerBase.LayerHub.LayerHub.ReportLayerEventError(layerFullName, handler.FullName, s_eventFullName, e);
                }
            }

            bool handledAndContinueSeen = false;
            for (int i = 0; i < orderedHandlers.Length; i++)
            {
                var handler = orderedHandlers[i];
                if (handler.Circuit.IsDisabled) continue;

                try
                {
                    if (handler.IsAsync)
                    {
                        var payload = @event.Value;
                        AsyncFaultContext.Observe(layerFullName, handler.Circuit, handler.FullName, in payload, handler.AsyncHandler!(payload));
                        continue;
                    }

                    var result = handler.SyncHandler!(in @event.Value);
                    if (result == EventHandledState.Handled) return EventHandledState.Handled;
                    if (result == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
                }
                catch (Exception e)
                {
                    EventMetaDataHandler.OnEventExpectation(@event.Value, e);
                    if (handler.Circuit.TryDisable())
                        LayerBase.LayerHub.LayerHub.ReportLayerEventError(layerFullName, handler.FullName, s_eventFullName, e);
                }
            }

            return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        private sealed class AsyncFaultContext
        {
            private static readonly ConcurrentBag<AsyncFaultContext> s_pool = new();
            private readonly Action _continuation;
            private string? _layerFullName;
            private HandlerCircuit? _circuit;
            private string? _handlerFullName;
            private T _payload;
            private LBTask _task;

            private AsyncFaultContext() { _continuation = Complete; }

            public static void Observe(string layerFullName, HandlerCircuit circuit, string handlerFullName, in T payload, LBTask task)
            {
                if (!s_pool.TryTake(out var context)) context = new AsyncFaultContext();
                context._layerFullName = layerFullName;
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
                    EventMetaDataHandler.OnEventExpectation(_payload, ex);
                    if (_circuit!.TryDisable())
                        LayerBase.LayerHub.LayerHub.ReportLayerEventError(_layerFullName!, _handlerFullName!, s_eventFullName, ex);
                }
                finally
                {
                    _layerFullName = null; _circuit = null; _handlerFullName = null; _payload = default; _task = default;
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
        public static ParallelHandlerEntry<T> Create(IEventHandler<T> handler, Action<string, string, string, Exception> reportError) => new(new ParallelSubscriptionQueue<T>(handler, reportError));
        public static ParallelHandlerEntry<T> Create(EventHandleDelegate<T> handler, Action<string, string, string, Exception> reportError) => new(new ParallelSubscriptionQueue<T>(handler, reportError));
        public void Enqueue(string layerFullName, in Event<T> @event) => _subscriptionQueue.Enqueue(layerFullName, in @event);
        public bool TryMatch(IEventHandler<T> handler) => _subscriptionQueue.TryMatch(handler);
        public bool TryMatch(EventHandleDelegate<T> handler) => _subscriptionQueue.TryMatch(handler);
    }

    internal sealed class ParallelSubscriptionQueue<T> where T : struct
    {
        private readonly ConcurrentQueue<Event<T>> _events = new();
        private readonly IEventHandler<T>? _syncHandler;
        private readonly EventHandleDelegate<T>? _syncDelegate;
        private readonly HandlerCircuit _circuit;
        private readonly Action<string, string, string, Exception> _reportError;
        private readonly Action _drainAction;
        private readonly string _fullName;
        private readonly string _eventFullName;
        private string? _layerFullName;
        private int _scheduled;

        public ParallelSubscriptionQueue(IEventHandler<T> handler, Action<string, string, string, Exception> reportError)
        {
            _syncHandler = handler; _reportError = reportError; _circuit = new HandlerCircuit();
            _fullName = handler.GetType().FullName ?? handler.GetType().Name;
            _eventFullName = typeof(T).FullName ?? typeof(T).Name; _drainAction = Drain;
        }

        public ParallelSubscriptionQueue(EventHandleDelegate<T> handler, Action<string, string, string, Exception> reportError)
        {
            _syncDelegate = handler; _reportError = reportError; _circuit = new HandlerCircuit();
            _fullName = GetHandlerFullName(handler); _eventFullName = typeof(T).FullName ?? typeof(T).Name; _drainAction = Drain;
        }

        public void Enqueue(string layerFullName, in Event<T> @event) 
        { 
            _layerFullName = layerFullName;
            if (!_circuit.IsDisabled) { _events.Enqueue(@event); TryScheduleDrain(); } 
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
                while (_events.TryDequeue(out var @event))
                {
                    if (_circuit.IsDisabled) { ClearPending(); break; }
                    try { if (_syncHandler != null) _syncHandler.Deal(in @event.Value); else _syncDelegate!(in @event.Value); }
                    catch (Exception e)
                    {
                        EventMetaDataHandler.OnEventExpectation(@event.Value, e);
                        if (_circuit.TryDisable()) _reportError(_layerFullName ?? "UnknownLayer", _fullName, _eventFullName, e);
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
