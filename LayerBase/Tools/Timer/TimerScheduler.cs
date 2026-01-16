using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.EventStateTrace;

namespace LayerBase.Tools.Timer
{
    /// <summary>
    /// 由外部Tick驱动的简单定时器，使用FreeList复用定时任务内存。
    /// </summary>
    public sealed class TimerScheduler
    {
        private readonly PriorityQueue<TimerToken, double> _timeline = new();
        private readonly Dictionary<int, ITimerQueue> _queues = new();
        private readonly object _lock = new();
        private double _currentTime;

        public double CurrentTime => _currentTime;

        public void Tick(double deltaTime)
        {
            if (deltaTime < 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));

            List<(TimerToken token, ITimerQueue queue)> due = new();
            lock (_lock)
            {
                _currentTime += deltaTime;
                while (_timeline.TryPeek(out var token, out var dueTime) && dueTime <= _currentTime)
                {
                    _timeline.Dequeue();
                    if (_queues.TryGetValue(token.TypeId, out var queue))
                    {
                        due.Add((token, queue));
                    }
                }
            }

            for (int i = 0; i < due.Count; i++)
            {
                var (token, queue) = due[i];
                queue.TryInvoke(token);
            }
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, EventHandlerDelegate<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleDelegate(due, payload, handler));
        }

        public TimerToken RegisterAt<T>(double timePoint, in T value, EventHandlerDelegate<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleDelegate(due, payload, handler));
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, EventHandlerDelegateAsync<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleDelegateAsync(due, payload, handler));
        }

        public TimerToken RegisterAt<T>(double timePoint, in T value, EventHandlerDelegateAsync<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleDelegateAsync(due, payload, handler));
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, IEventHandler<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleHandler(due, payload, handler));
        }

        public TimerToken RegisterAt<T>(double timePoint, in T value, IEventHandler<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleHandler(due, payload, handler));
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, IEventHandlerAsync<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleHandlerAsync(due, payload, handler));
        }

        public TimerToken RegisterAt<T>(double timePoint, in T value, IEventHandlerAsync<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleHandlerAsync(due, payload, handler));
        }

        public bool Cancel(in TimerToken token)
        {
            if (!token.IsValid) return false;

            lock (_lock)
            {
                if (!_queues.TryGetValue(token.TypeId, out var queue))
                {
                    return false;
                }

                return queue.Cancel(token);
            }
        }

        private TimerToken RegisterAfterInternal<T>(double delay, Func<TimerQueue<T>, double, TimerToken> registrar) where T : struct
        {
            lock (_lock)
            {
                var queue = GetQueue<T>();
                double due = Normalize(_currentTime + delay);
                var token = registrar(queue, due);
                _timeline.Enqueue(token, due);
                return token;
            }
        }

        private TimerToken RegisterAtInternal<T>(double timePoint, Func<TimerQueue<T>, double, TimerToken> registrar) where T : struct
        {
            lock (_lock)
            {
                var queue = GetQueue<T>();
                double due = Normalize(timePoint);
                var token = registrar(queue, due);
                _timeline.Enqueue(token, due);
                return token;
            }
        }

        private double Normalize(double timePoint)
        {
            if (double.IsNaN(timePoint) || double.IsInfinity(timePoint))
            {
                throw new ArgumentOutOfRangeException(nameof(timePoint));
            }
            return timePoint < 0 ? 0 : timePoint;
        }

        private TimerQueue<T> GetQueue<T>() where T : struct
        {
            int typeId = EventTypeId<T>.Id;
            if (_queues.TryGetValue(typeId, out var queue))
            {
                return (TimerQueue<T>)queue;
            }

            var timerQueue = new TimerQueue<T>();
            _queues[typeId] = timerQueue;
            return timerQueue;
        }
    }

    internal interface ITimerQueue
    {
        bool TryInvoke(in TimerToken token);
        bool Cancel(in TimerToken token);
    }

    internal sealed class TimerQueue<T> : ITimerQueue where T : struct
    {
        private readonly FreeList<TimerTask<T>> _tasks = new FreeList<TimerTask<T>>(slabSize: 128);
        private readonly object _lock = new();

        internal TimerToken ScheduleDelegate(double executeAt, in T value, EventHandlerDelegate<T> handler)
        {
            lock (_lock)
            {
                var slotRef = _tasks.Rent();
                ref var slot = ref _tasks.Resolve(slotRef);
                slot.Value = TimerTask<T>.FromDelegate(executeAt, value, handler);
                return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
            }
        }

        internal TimerToken ScheduleDelegateAsync(double executeAt, in T value, EventHandlerDelegateAsync<T> handler)
        {
            lock (_lock)
            {
                var slotRef = _tasks.Rent();
                ref var slot = ref _tasks.Resolve(slotRef);
                slot.Value = TimerTask<T>.FromDelegateAsync(executeAt, value, handler);
                return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
            }
        }

        internal TimerToken ScheduleHandler(double executeAt, in T value, IEventHandler<T> handler)
        {
            lock (_lock)
            {
                var slotRef = _tasks.Rent();
                ref var slot = ref _tasks.Resolve(slotRef);
                slot.Value = TimerTask<T>.FromHandler(executeAt, value, handler);
                return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
            }
        }

        internal TimerToken ScheduleHandlerAsync(double executeAt, in T value, IEventHandlerAsync<T> handler)
        {
            lock (_lock)
            {
                var slotRef = _tasks.Rent();
                ref var slot = ref _tasks.Resolve(slotRef);
                slot.Value = TimerTask<T>.FromHandlerAsync(executeAt, value, handler);
                return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
            }
        }

        public bool TryInvoke(in TimerToken token)
        {
            if (token.TypeId != EventTypeId<T>.Id)
            {
                return false;
            }

            SlotRef slotRef;
            TimerTask<T> task;

            lock (_lock)
            {
                if (!_tasks.TryBorrow(token.Index, token.Version, out slotRef))
                {
                    return false;
                }

                ref var slot = ref _tasks.Resolve(slotRef);
                task = slot.Value;
                slot.Value = default;
            }

            try
            {
                if (task.Kind != TimerTaskKind.None)
                {
                    ExecuteTask(ref task);
                }
                return true;
            }
            finally
            {
                lock (_lock)
                {
                    _tasks.Release(slotRef);
                }
            }
        }

        public bool Cancel(in TimerToken token)
        {
            if (token.TypeId != EventTypeId<T>.Id)
            {
                return false;
            }

            lock (_lock)
            {
                if (!_tasks.TryBorrow(token.Index, token.Version, out var slotRef))
                {
                    return false;
                }

                _tasks.Release(slotRef);
                return true;
            }
        }

        private static void ExecuteTask(ref TimerTask<T> task)
        {
            var evt = new Event<T>(task.Payload);
            switch (task.Kind)
            {
                case TimerTaskKind.EventHandlerDelegate:
                    task.HandlerDelegate!.Invoke(in evt);
                    break;
                case TimerTaskKind.EventHandlerDelegateAsync:
                    task.HandlerDelegateAsync!.Invoke(evt).Forget();
                    break;
                case TimerTaskKind.EventHandler:
                    task.Handler!.Deal(in evt);
                    break;
                case TimerTaskKind.EventHandlerAsync:
                    task.HandlerAsync!.Deal(evt).Forget();
                    break;
            }
        }
    }

    internal enum TimerTaskKind
    {
        None = 0,
        EventHandlerDelegate,
        EventHandlerDelegateAsync,
        EventHandler,
        EventHandlerAsync,
    }

    internal struct TimerTask<T> where T : struct
    {
        public double ExecuteAt;
        public T Payload;
        public TimerTaskKind Kind;
        public EventHandlerDelegate<T>? HandlerDelegate;
        public EventHandlerDelegateAsync<T>? HandlerDelegateAsync;
        public IEventHandler<T>? Handler;
        public IEventHandlerAsync<T>? HandlerAsync;

        public static TimerTask<T> FromDelegate(double executeAt, in T payload, EventHandlerDelegate<T> handler)
        {
            return new TimerTask<T>
            {
                ExecuteAt = executeAt,
                Payload = payload,
                HandlerDelegate = handler,
                Kind = TimerTaskKind.EventHandlerDelegate,
            };
        }

        public static TimerTask<T> FromDelegateAsync(double executeAt, in T payload, EventHandlerDelegateAsync<T> handler)
        {
            return new TimerTask<T>
            {
                ExecuteAt = executeAt,
                Payload = payload,
                HandlerDelegateAsync = handler,
                Kind = TimerTaskKind.EventHandlerDelegateAsync,
            };
        }

        public static TimerTask<T> FromHandler(double executeAt, in T payload, IEventHandler<T> handler)
        {
            return new TimerTask<T>
            {
                ExecuteAt = executeAt,
                Payload = payload,
                Handler = handler,
                Kind = TimerTaskKind.EventHandler,
            };
        }

        public static TimerTask<T> FromHandlerAsync(double executeAt, in T payload, IEventHandlerAsync<T> handler)
        {
            return new TimerTask<T>
            {
                ExecuteAt = executeAt,
                Payload = payload,
                HandlerAsync = handler,
                Kind = TimerTaskKind.EventHandlerAsync,
            };
        }
    }
}
