using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Core.EventStateTrace;
using LayerBase.Event.EventMetaData;
using LayerBase.Tools.Job;

namespace LayerBase.Core.Event
{
    /// <summary>
    /// Event dispatcher for one layer.
    /// </summary>
    internal class EventDispatcher
    {
        private readonly ConcurrentDictionary<int, IHandlerBucket> _buckets = new();
        private readonly string _layerFullName;

        internal EventDispatcher(string layerFullName)
        {
            _layerFullName = string.IsNullOrWhiteSpace(layerFullName) ? "UnknownLayer" : layerFullName;
        }

        internal EventStateTracer? StateTracer { get; set; }
        internal EventLogTracer? LogTracer { get; set; }
        internal Action<string, string, string, Exception>? ErrorReporter { get; set; }

        public void Subscribe<EventArg>(IEventHandler<EventArg> handler) where EventArg : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetBucket<EventArg>().Add(handler);
        }

        public void SubscribeAsync<EventArg>(IEventHandlerAsync<EventArg> handler) where EventArg : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetBucket<EventArg>().Add(handler);
        }

        public void SubscribeParallel<EventArg>(IEventHandler<EventArg> handler) where EventArg : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetBucket<EventArg>().AddParallel(handler, ReportHandlerError);
        }

        public void Subscribe<EventArg>(EventHandleDelegate<EventArg> handleDelegate) where EventArg : struct
        {
            if (handleDelegate == null) throw new ArgumentNullException(nameof(handleDelegate));
            GetBucket<EventArg>().Add(handleDelegate);
        }

        public void SubscribeAsync<EventArg>(EventHandleDelegateAsync<EventArg> handleDelegate) where EventArg : struct
        {
            if (handleDelegate == null) throw new ArgumentNullException(nameof(handleDelegate));
            GetBucket<EventArg>().Add(handleDelegate);
        }

        public void SubscribeParallel<EventArg>(EventHandleDelegate<EventArg> handleDelegate) where EventArg : struct
        {
            if (handleDelegate == null) throw new ArgumentNullException(nameof(handleDelegate));
            GetBucket<EventArg>().AddParallel(handleDelegate, ReportHandlerError);
        }

        public bool Unsubscribe<T>(IEventHandler<T> handler) where T : struct
        {
            if (handler == null) return false;
            return TryGetBucket<T>(out var bucket) && bucket.Remove(handler);
        }

        public bool Unsubscribe<T>(IEventHandlerAsync<T> handler) where T : struct
        {
            if (handler == null) return false;
            return TryGetBucket<T>(out var bucket) && bucket.Remove(handler);
        }

        public bool Unsubscribe<T>(EventHandleDelegate<T> handleDelegate) where T : struct
        {
            if (handleDelegate == null) return false;
            return TryGetBucket<T>(out var bucket) && bucket.Remove(handleDelegate);
        }

        public bool Unsubscribe<T>(EventHandleDelegateAsync<T> handleDelegateAsync) where T : struct
        {
            if (handleDelegateAsync == null) return false;
            return TryGetBucket<T>(out var bucket) && bucket.Remove(handleDelegateAsync);
        }

        public EventHandledState Dispatch<T>(in Event<T> @event) where T : struct
        {
            if (!@event.IsVaild())
            {
                return EventHandledState.Handled;
            }

            return TryGetBucket<T>(out var bucket)
                ? bucket.Dispatch(this, in @event)
                : EventHandledState.Continue;
        }

        public EventHandledState Dispatch<T>(in Event<T> @event, ref EventState eventState) where T : struct
        {
            if (!@event.IsVaild())
            {
                return EventHandledState.Handled;
            }

            return TryGetBucket<T>(out var bucket)
                ? bucket.Dispatch(this, in @event, ref eventState)
                : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HandlerBucket<T> GetBucket<T>() where T : struct
        {
            int typeId = EventTypeId<T>.Id;
            var bucket = _buckets.GetOrAdd(typeId, static _ => new HandlerBucket<T>());
            if (bucket is HandlerBucket<T> typedBucket)
            {
                return typedBucket;
            }

            throw new InvalidOperationException(
                $"typeId:{typeId} Type:{typeof(T).Name} mapped to incompatible handler bucket.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetBucket<T>(out HandlerBucket<T> bucket) where T : struct
        {
            int typeId = EventTypeId<T>.Id;
            if (_buckets.TryGetValue(typeId, out var baseBucket) && baseBucket is HandlerBucket<T> typedBucket)
            {
                bucket = typedBucket;
                return true;
            }

            bucket = default!;
            return false;
        }

        private void ReportHandlerError(string handlerFullName, string eventFullName, Exception exception)
        {
            ErrorReporter?.Invoke(_layerFullName, handlerFullName, eventFullName, exception);
        }

        private static string GetHandlerDisplayName(Delegate handler, int index = -1)
        {
            var method = handler.Method;
            string typeName = method.DeclaringType?.Name ?? handler.Target?.GetType()?.Name ?? "Global";
            string methodName = method.Name;

            if (methodName.StartsWith("<") && methodName.Contains(">"))
            {
                methodName = "lambda";
            }

            string name = $"{typeName}.{methodName}";
            if (index >= 0)
            {
                name = $"#{index}:{name}";
            }
            return name;
        }

        private static string GetHandlerFullName(Delegate handler)
        {
            var method = handler.Method;
            string typeName = method.DeclaringType?.FullName ?? handler.Target?.GetType()?.FullName ?? "Global";
            string methodName = method.Name;

            if (methodName.StartsWith("<") && methodName.Contains(">"))
            {
                methodName = "lambda";
            }

            return $"{typeName}.{methodName}";
        }

        private interface IHandlerBucket
        {
        }

        private sealed class HandlerCircuit
        {
            private int _disabled;

            public bool IsDisabled => Volatile.Read(ref _disabled) == 1;

            public bool TryDisable()
            {
                return Interlocked.Exchange(ref _disabled, 1) == 0;
            }
        }

        private sealed class HandlerBucket<T> : IHandlerBucket where T : struct
        {
            private static readonly string s_eventFullName = typeof(T).FullName ?? typeof(T).Name;
            private readonly object _lock = new();
            private OrderedHandlerEntry<T>[] _orderedHandlers = Array.Empty<OrderedHandlerEntry<T>>();
            private UnorderedHandlerEntry<T>[] _unorderedHandlers = Array.Empty<UnorderedHandlerEntry<T>>();
            private ParallelHandlerEntry<T>[] _parallelHandlers = Array.Empty<ParallelHandlerEntry<T>>();

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

            public void AddParallel(IEventHandler<T> handler, Action<string, string, Exception> reportError)
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

            public void AddParallel(EventHandleDelegate<T> handler, Action<string, string, Exception> reportError)
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

            public bool Remove(IEventHandler<T> handler)
            {
                lock (_lock)
                {
                    var unorderedCurrent = _unorderedHandlers;
                    for (int i = 0; i < unorderedCurrent.Length; i++)
                    {
                        if (unorderedCurrent[i].TryMatch(handler))
                        {
                            Volatile.Write(ref _unorderedHandlers, RemoveAt(unorderedCurrent, i));
                            return true;
                        }
                    }

                    var parallelCurrent = _parallelHandlers;
                    for (int i = 0; i < parallelCurrent.Length; i++)
                    {
                        if (parallelCurrent[i].TryMatch(handler))
                        {
                            Volatile.Write(ref _parallelHandlers, RemoveAt(parallelCurrent, i));
                            return true;
                        }
                    }

                    return false;
                }
            }

            public bool Remove(IEventHandlerAsync<T> handler)
            {
                lock (_lock)
                {
                    var current = _unorderedHandlers;
                    for (int i = 0; i < current.Length; i++)
                    {
                        if (current[i].TryMatch(handler))
                        {
                            Volatile.Write(ref _unorderedHandlers, RemoveAt(current, i));
                            return true;
                        }
                    }

                    return false;
                }
            }

            public bool Remove(EventHandleDelegate<T> handler)
            {
                lock (_lock)
                {
                    var orderedCurrent = _orderedHandlers;
                    for (int i = 0; i < orderedCurrent.Length; i++)
                    {
                        if (orderedCurrent[i].TryMatch(handler))
                        {
                            Volatile.Write(ref _orderedHandlers, RemoveAt(orderedCurrent, i));
                            return true;
                        }
                    }

                    var parallelCurrent = _parallelHandlers;
                    for (int i = 0; i < parallelCurrent.Length; i++)
                    {
                        if (parallelCurrent[i].TryMatch(handler))
                        {
                            Volatile.Write(ref _parallelHandlers, RemoveAt(parallelCurrent, i));
                            return true;
                        }
                    }

                    return false;
                }
            }

            public bool Remove(EventHandleDelegateAsync<T> handler)
            {
                lock (_lock)
                {
                    var current = _orderedHandlers;
                    for (int i = 0; i < current.Length; i++)
                    {
                        if (current[i].TryMatch(handler))
                        {
                            Volatile.Write(ref _orderedHandlers, RemoveAt(current, i));
                            return true;
                        }
                    }

                    return false;
                }
            }

            public EventHandledState Dispatch(EventDispatcher dispatcher, in Event<T> @event)
            {
                var tracer = dispatcher.StateTracer;
                bool hasTracer = tracer != null;
                EventState state = default;
                if (hasTracer)
                {
                    state = tracer!.Resolve(@event.TraceToken);
                }

                return DispatchCore(dispatcher, in @event, hasTracer, ref state);
            }

            public EventHandledState Dispatch(EventDispatcher dispatcher, in Event<T> @event, ref EventState state)
            {
                bool hasTracer = dispatcher.StateTracer != null;
                return DispatchCore(dispatcher, in @event, hasTracer, ref state);
            }

            private EventHandledState DispatchCore(
                EventDispatcher dispatcher,
                in Event<T> @event,
                bool hasTracer,
                ref EventState state)
            {
                var parallelHandlers = Volatile.Read(ref _parallelHandlers);
                var unorderedHandlers = Volatile.Read(ref _unorderedHandlers);
                var orderedHandlers = Volatile.Read(ref _orderedHandlers);
                if (parallelHandlers.Length == 0 && unorderedHandlers.Length == 0 && orderedHandlers.Length == 0)
                {
                    return EventHandledState.Continue;
                }

                for (int i = 0; i < parallelHandlers.Length; i++)
                {
                    var handler = parallelHandlers[i];
                    handler.Enqueue(in @event);
                    if (hasTracer)
                    {
                        dispatcher.LogTracer?.TryRecordHandler(ref state, handler.DisplayName, EventHandledState.Continue);
                    }
                }

                for (int i = 0; i < unorderedHandlers.Length; i++)
                {
                    var handler = unorderedHandlers[i];
                    if (handler.Circuit.IsDisabled)
                    {
                        continue;
                    }

                    try
                    {
                        if (handler.IsAsync)
                        {
                            var payload = @event.Value;
                            var circuit = handler.Circuit;
                            var fullName = handler.FullName;
                            handler.AsyncHandler!.Deal(payload).Forget(ex =>
                            {
                                EventMetaDataHandler.OnEventExpectation(payload, ex);
                                if (circuit.TryDisable())
                                {
                                    dispatcher.ReportHandlerError(fullName, s_eventFullName, ex);
                                }
                            });
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
                        {
                            dispatcher.ReportHandlerError(handler.FullName, s_eventFullName, e);
                        }
                    }
                    finally
                    {
                        if (hasTracer)
                        {
                            dispatcher.LogTracer?.TryRecordHandler(ref state, handler.DisplayName, EventHandledState.Continue);
                        }
                    }
                }

                bool handledAndContinueSeen = false;
                for (int i = 0; i < orderedHandlers.Length; i++)
                {
                    var handler = orderedHandlers[i];
                    if (handler.Circuit.IsDisabled)
                    {
                        continue;
                    }

                    try
                    {
                        if (handler.IsAsync)
                        {
                            var payload = @event.Value;
                            var circuit = handler.Circuit;
                            var fullName = handler.FullName;
                            handler.AsyncHandler!(payload).Forget(ex =>
                            {
                                EventMetaDataHandler.OnEventExpectation(payload, ex);
                                if (circuit.TryDisable())
                                {
                                    dispatcher.ReportHandlerError(fullName, s_eventFullName, ex);
                                }
                            });
                            if (hasTracer)
                            {
                                dispatcher.LogTracer?.TryRecordHandler(ref state, $"#{i}:{handler.DisplayName}",
                                    EventHandledState.Continue);
                            }

                            continue;
                        }

                        var result = handler.SyncHandler!(in @event.Value);
                        if (hasTracer)
                        {
                            dispatcher.LogTracer?.TryRecordHandler(ref state, $"#{i}:{handler.DisplayName}", result);
                        }

                        if (result == EventHandledState.Handled)
                        {
                            return EventHandledState.Handled;
                        }

                        if (result == EventHandledState.HandledAndContinue)
                        {
                            handledAndContinueSeen = true;
                        }
                    }
                    catch (Exception e)
                    {
                        EventMetaDataHandler.OnEventExpectation(@event.Value, e);
                        if (handler.Circuit.TryDisable())
                        {
                            dispatcher.ReportHandlerError(handler.FullName, s_eventFullName, e);
                        }
                    }
                }

                return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
            }

            private static TEntry[] RemoveAt<TEntry>(TEntry[] current, int index)
            {
                if (current.Length == 1)
                {
                    return Array.Empty<TEntry>();
                }

                var next = new TEntry[current.Length - 1];
                if (index > 0)
                {
                    Array.Copy(current, 0, next, 0, index);
                }

                if (index < current.Length - 1)
                {
                    Array.Copy(current, index + 1, next, index, current.Length - index - 1);
                }

                return next;
            }
        }

        private readonly struct OrderedHandlerEntry<T> where T : struct
        {
            public readonly EventHandleDelegate<T>? SyncHandler;
            public readonly EventHandleDelegateAsync<T>? AsyncHandler;
            public readonly string DisplayName;
            public readonly string FullName;
            public readonly HandlerCircuit Circuit;
            public bool IsAsync => AsyncHandler != null;

            private OrderedHandlerEntry(
                EventHandleDelegate<T>? syncHandler,
                EventHandleDelegateAsync<T>? asyncHandler,
                string displayName,
                string fullName,
                HandlerCircuit circuit)
            {
                SyncHandler = syncHandler;
                AsyncHandler = asyncHandler;
                DisplayName = displayName;
                FullName = fullName;
                Circuit = circuit;
            }

            public static OrderedHandlerEntry<T> Create(EventHandleDelegate<T> handler)
            {
                return new OrderedHandlerEntry<T>(
                    handler,
                    null,
                    GetHandlerDisplayName(handler),
                    GetHandlerFullName(handler),
                    new HandlerCircuit());
            }

            public static OrderedHandlerEntry<T> Create(EventHandleDelegateAsync<T> handler)
            {
                return new OrderedHandlerEntry<T>(
                    null,
                    handler,
                    GetHandlerDisplayName(handler),
                    GetHandlerFullName(handler),
                    new HandlerCircuit());
            }

            public bool TryMatch(EventHandleDelegate<T> handler)
            {
                return SyncHandler != null && Equals(SyncHandler, handler);
            }

            public bool TryMatch(EventHandleDelegateAsync<T> handler)
            {
                return AsyncHandler != null && Equals(AsyncHandler, handler);
            }
        }

        private readonly struct UnorderedHandlerEntry<T> where T : struct
        {
            public readonly IEventHandler<T>? SyncHandler;
            public readonly IEventHandlerAsync<T>? AsyncHandler;
            public readonly string DisplayName;
            public readonly string FullName;
            public readonly HandlerCircuit Circuit;
            public bool IsAsync => AsyncHandler != null;

            private UnorderedHandlerEntry(
                IEventHandler<T>? syncHandler,
                IEventHandlerAsync<T>? asyncHandler,
                string displayName,
                string fullName,
                HandlerCircuit circuit)
            {
                SyncHandler = syncHandler;
                AsyncHandler = asyncHandler;
                DisplayName = displayName;
                FullName = fullName;
                Circuit = circuit;
            }

            public static UnorderedHandlerEntry<T> Create(IEventHandler<T> handler)
            {
                var fullName = handler.GetType().FullName ?? handler.GetType().Name;
                return new UnorderedHandlerEntry<T>(
                    handler,
                    null,
                    handler.GetType().Name,
                    fullName,
                    new HandlerCircuit());
            }

            public static UnorderedHandlerEntry<T> Create(IEventHandlerAsync<T> handler)
            {
                var fullName = handler.GetType().FullName ?? handler.GetType().Name;
                return new UnorderedHandlerEntry<T>(
                    null,
                    handler,
                    handler.GetType().Name,
                    fullName,
                    new HandlerCircuit());
            }

            public bool TryMatch(IEventHandler<T> handler)
            {
                return SyncHandler != null && EqualityComparer<IEventHandler<T>>.Default.Equals(SyncHandler, handler);
            }

            public bool TryMatch(IEventHandlerAsync<T> handler)
            {
                return AsyncHandler != null && EqualityComparer<IEventHandlerAsync<T>>.Default.Equals(AsyncHandler, handler);
            }
        }

        private readonly struct ParallelHandlerEntry<T> where T : struct
        {
            private readonly ParallelSubscriptionQueue<T> _subscriptionQueue;

            public string DisplayName => _subscriptionQueue.DisplayName;

            private ParallelHandlerEntry(ParallelSubscriptionQueue<T> subscriptionQueue)
            {
                _subscriptionQueue = subscriptionQueue;
            }

            public static ParallelHandlerEntry<T> Create(
                IEventHandler<T> handler,
                Action<string, string, Exception> reportError)
            {
                return new ParallelHandlerEntry<T>(new ParallelSubscriptionQueue<T>(handler, reportError));
            }

            public static ParallelHandlerEntry<T> Create(
                EventHandleDelegate<T> handler,
                Action<string, string, Exception> reportError)
            {
                return new ParallelHandlerEntry<T>(new ParallelSubscriptionQueue<T>(handler, reportError));
            }

            public void Enqueue(in Event<T> @event)
            {
                _subscriptionQueue.Enqueue(in @event);
            }

            public bool TryMatch(IEventHandler<T> handler)
            {
                return _subscriptionQueue.TryMatch(handler);
            }

            public bool TryMatch(EventHandleDelegate<T> handler)
            {
                return _subscriptionQueue.TryMatch(handler);
            }
        }

        private sealed class ParallelSubscriptionQueue<T> where T : struct
        {
            private readonly ConcurrentQueue<Event<T>> _events = new();
            private readonly IEventHandler<T>? _syncHandler;
            private readonly EventHandleDelegate<T>? _syncDelegate;
            private readonly HandlerCircuit _circuit;
            private readonly Action<string, string, Exception> _reportError;
            private readonly Action _drainAction;
            private readonly string _fullName;
            private readonly string _eventFullName;
            private int _scheduled;

            internal string DisplayName { get; }

            internal ParallelSubscriptionQueue(IEventHandler<T> handler, Action<string, string, Exception> reportError)
            {
                _syncHandler = handler;
                _reportError = reportError;
                _circuit = new HandlerCircuit();
                _fullName = handler.GetType().FullName ?? handler.GetType().Name;
                _eventFullName = typeof(T).FullName ?? typeof(T).Name;
                DisplayName = $"[P]{handler.GetType().Name}";
                _drainAction = Drain;
            }

            internal ParallelSubscriptionQueue(EventHandleDelegate<T> handler, Action<string, string, Exception> reportError)
            {
                _syncDelegate = handler;
                _reportError = reportError;
                _circuit = new HandlerCircuit();
                _fullName = GetHandlerFullName(handler);
                _eventFullName = typeof(T).FullName ?? typeof(T).Name;
                DisplayName = $"[P]{GetHandlerDisplayName(handler)}";
                _drainAction = Drain;
            }

            internal void Enqueue(in Event<T> @event)
            {
                if (_circuit.IsDisabled)
                {
                    return;
                }

                _events.Enqueue(@event);
                TryScheduleDrain();
            }

            internal bool TryMatch(IEventHandler<T> handler)
            {
                return _syncHandler != null && EqualityComparer<IEventHandler<T>>.Default.Equals(_syncHandler, handler);
            }

            internal bool TryMatch(EventHandleDelegate<T> handler)
            {
                return _syncDelegate != null && Equals(_syncDelegate, handler);
            }

            private void TryScheduleDrain()
            {
                if (_circuit.IsDisabled)
                {
                    ClearPending();
                    return;
                }

                if (Interlocked.CompareExchange(ref _scheduled, 1, 0) != 0)
                {
                    return;
                }

                if (!JobSchedulers.Default.TrySchedule(_drainAction))
                {
                    ThreadPool.QueueUserWorkItem(static state => ((ParallelSubscriptionQueue<T>)state!).Drain(), this);
                }
            }

            private void Drain()
            {
                try
                {
                    while (_events.TryDequeue(out var @event))
                    {
                        if (_circuit.IsDisabled)
                        {
                            ClearPending();
                            break;
                        }

                        try
                        {
                            if (_syncHandler != null)
                            {
                                _syncHandler.Deal(in @event.Value);
                            }
                            else
                            {
                                _syncDelegate!(in @event.Value);
                            }
                        }
                        catch (Exception e)
                        {
                            EventMetaDataHandler.OnEventExpectation(@event.Value, e);
                            if (_circuit.TryDisable())
                            {
                                _reportError(_fullName, _eventFullName, e);
                            }
                            ClearPending();
                            break;
                        }
                    }
                }
                finally
                {
                    Volatile.Write(ref _scheduled, 0);
                    if (!_events.IsEmpty)
                    {
                        TryScheduleDrain();
                    }
                }
            }

            private void ClearPending()
            {
                while (_events.TryDequeue(out _))
                {
                }
            }
        }
    }
}
