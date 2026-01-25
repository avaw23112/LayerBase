using System;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Core.EventStateTrace;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.Event
{
    /// <summary>
    /// 事件分发器：管理当前层绑定的处理器/委托并分发。
    /// </summary>
    internal class EventDispatcher
    {
        private readonly Dictionary<int, List<Delegate>> _orderedDelegates = new();
        private readonly Dictionary<int, List<IEventHandler>> _unorderedHandlers = new();
        private readonly object _lock = new();

        internal EventStateTracer? StateTracer { get; set; }
        internal EventLogTracer? LogTracer { get; set; }

        /// <summary>
        /// 注册无序处理器（不控制传播）。
        /// </summary>
        public void Subscribe<EventArg>(IEventHandler<EventArg> handler) where EventArg : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            int typeId = EventTypeId<EventArg>.Id;
            lock (_lock)
            {
                if (!_unorderedHandlers.TryGetValue(typeId, out var list))
                {
                    list = new List<IEventHandler>(capacity: 4);
                    _unorderedHandlers[typeId] = list;
                }

                list.Add(handler);
            }
        }

        public void Subscribe<EventArg>(IEventHandlerAsync<EventArg> handler) where EventArg : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            int typeId = EventTypeId<EventArg>.Id;
            lock (_lock)
            {
                if (!_unorderedHandlers.TryGetValue(typeId, out var list))
                {
                    list = new List<IEventHandler>(capacity: 4);
                    _unorderedHandlers[typeId] = list;
                }

                list.Add(handler);
            }
        }

        /// <summary>
        /// 注册有序委托（返回值可截断传播）。
        /// </summary>
        public void Subscribe<EventArg>(EventHandleDelegate<EventArg> handleDelegate) where EventArg : struct
        {
            if (handleDelegate == null) throw new ArgumentNullException(nameof(handleDelegate));
            int typeId = EventTypeId<EventArg>.Id;

            lock (_lock)
            {
                if (!_orderedDelegates.TryGetValue(typeId, out var list))
                {
                    list = new List<Delegate>(capacity: 4);
                    _orderedDelegates[typeId] = list;
                }

                list.Add(handleDelegate);
            }
        }

        public void Subscribe<EventArg>(EventHandleDelegateAsync<EventArg> handleDelegate) where EventArg : struct
        {
            if (handleDelegate == null) throw new ArgumentNullException(nameof(handleDelegate));
            int typeId = EventTypeId<EventArg>.Id;

            lock (_lock)
            {
                if (!_orderedDelegates.TryGetValue(typeId, out var list))
                {
                    list = new List<Delegate>(capacity: 4);
                    _orderedDelegates[typeId] = list;
                }

                list.Add(handleDelegate);
            }
        }

        public bool Unsubscribe<T>(IEventHandler<T> handler) where T : struct
        {
            if (handler == null) return false;
            int typeId = EventTypeId<T>.Id;

            lock (_lock)
            {
                if (!_unorderedHandlers.TryGetValue(typeId, out var list) || list.Count == 0)
                    return false;
                bool removed = list.Remove(handler);

                if (removed && list.Count == 0)
                {
                    _unorderedHandlers.Remove(typeId);
                }
                return removed;
            }
        }

        public bool Unsubscribe<T>(IEventHandlerAsync<T> handler) where T : struct
        {
            if (handler == null) return false;
            int typeId = EventTypeId<T>.Id;

            lock (_lock)
            {
                if (!_unorderedHandlers.TryGetValue(typeId, out var list) || list.Count == 0)
                    return false;
                bool removed = list.Remove(handler);

                if (removed && list.Count == 0)
                {
                    _unorderedHandlers.Remove(typeId);
                }
                return removed;
            }
        }

        public bool Unsubscribe<T>(EventHandleDelegate<T> handleDelegate) where T : struct
        {
            if (handleDelegate == null) return false;
            int typeId = EventTypeId<T>.Id;

            lock (_lock)
            {
                if (!_orderedDelegates.TryGetValue(typeId, out var list) || list.Count == 0)
                    return false;
                bool removed = list.Remove(handleDelegate);

                if (removed && list.Count == 0)
                {
                    _orderedDelegates.Remove(typeId);
                }
                return removed;
            }
        }

        public bool Unsubscribe<T>(EventHandleDelegateAsync<T> handleDelegateAsync) where T : struct
        {
            if (handleDelegateAsync == null) return false;
            int typeId = EventTypeId<T>.Id;

            lock (_lock)
            {
                if (!_orderedDelegates.TryGetValue(typeId, out var list) || list.Count == 0)
                    return false;
                bool removed = list.Remove(handleDelegateAsync);

                if (removed && list.Count == 0)
                {
                    _orderedDelegates.Remove(typeId);
                }
                return removed;
            }
        }

        /// <summary>
        /// 分发事件：先无序处理，再按序委托。
        /// </summary>
        public EventHandledState Dispatch<T>(in Event<T> @event) where T : struct
        {
            if (!@event.IsVaild())
            {
                return EventHandledState.Handled;
            }

            int typeId = EventTypeId<T>.Id;
            HandleUnordered(@event, typeId);
            return HandleOrdered(@event, typeId);
        }

        private EventHandledState HandleOrdered<T>(in Event<T> @event, int typeId) where T : struct
        {
            if (!_orderedDelegates.TryGetValue(typeId, out var list) || list.Count == 0)
            {
                return EventHandledState.Continue;
            }

            bool handledAndContinueSeen = false;
            bool isReleaseMode = StateTracer == null;
            var tracer = StateTracer;
            EventState state = default;
            if (tracer != null)
            {
                state = tracer.Resolve(@event.TraceToken);
            }

            for (int i = 0; i < list.Count; i++)
            {
                var handler = list[i];
                try
                {
                    if (handler is EventHandleDelegate<T> syncDelegate)
                    {
                        var result = syncDelegate(in @event.Value);
                        if (result == EventHandledState.Handled)
                        {
                            @event.MarkHandled();
                            return EventHandledState.Handled;
                        }

                        if (result == EventHandledState.HandledAndContinue)
                        {
                            @event.MarkHandledAndContinue();
                            handledAndContinueSeen = true;
                        }
                        else
                        {
                            @event.MarkContinue();
                        }

                        if (!isReleaseMode && tracer != null)
                        {
                            LogTracer?.TryRecordHandler(ref state, GetHandlerDisplayName(syncDelegate, i), result);
                        }
                    }
                    else if (handler is EventHandleDelegateAsync<T> asyncDelegate)
                    {
                        asyncDelegate(@event.Value).Forget();
                        if (!isReleaseMode && tracer != null)
                        {
                            LogTracer?.TryRecordHandler(ref state, GetHandlerDisplayName(asyncDelegate, i),
                                EventHandledState.Continue);
                        }
                    }
                }
                catch (Exception e)
                {
                    EventMetaDataHandler.OnEventExpectation(@event.Value, e);
                }
            }

            return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        private void HandleUnordered<T>(in Event<T> @event, int typeId) where T : struct
        {
            var tracer = StateTracer;
            bool isReleaseMode = tracer == null;
            EventState state = default;
            if (tracer != null)
            {
                state = tracer.Resolve(@event.TraceToken);
            }

            if (_unorderedHandlers.TryGetValue(typeId, out var handlers) && handlers.Count != 0)
            {
                for (int i = 0; i < handlers.Count; i++)
                {
                    var handler = handlers[i];
                    try
                    {
                        if (handler is IEventHandler<T> syncHandler)
                        {
                            syncHandler.Deal(in @event.Value);
                        }
                        if (handler is IEventHandlerAsync<T> asyncHandler)
                        {
                            asyncHandler.Deal(@event.Value).Forget();
                        }
                    }
                    catch (Exception e)
                    {
                        EventMetaDataHandler.OnEventExpectation(@event.Value, e);
                    }
                    finally
                    {
                        if (!isReleaseMode && tracer != null)
                        {
                            LogTracer?.TryRecordHandler(ref state, handler.GetType().Name, EventHandledState.Continue);
                        }
                    }
                }
            }
        }

        private static string GetHandlerDisplayName(Delegate handler, int index = -1)
        {
            var method = handler.Method;
            string typeName = method.DeclaringType?.Name ?? handler.Target?.GetType()?.Name ?? "Global";
            string methodName = method.Name;

            // 编译器生成的闭包/局部函数名通常包含尖括号，替换成更友好的标识。
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

    }
}
