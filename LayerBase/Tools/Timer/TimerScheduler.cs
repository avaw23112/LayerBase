using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.EventStateTrace;
using Cysharp.Threading.Tasks;

namespace LayerBase.Tools.Timer
{
    /// <summary>
    /// 由外部Tick驱动的简单定时器，使用FreeList复用定时任务内存。
    /// </summary>
    public sealed class TimerScheduler
    {
        private readonly struct EmptyPayload
        {
        }

        private readonly TimerTimeline _timeline = new();
        private readonly Dictionary<int, ITimerQueue> _queues = new();
        private readonly Dictionary<int, IFrequencyQueue> _frequencyQueues = new();
        private readonly object _lock = new();
        private double _currentTime;
        private double _frequencySeconds;
        private double _frequencyAccumulator;
        private bool _frequencyGateOpen = true;

        public double CurrentTime => _currentTime;
        public bool IsFrequencyGateOpen => Volatile.Read(ref _frequencyGateOpen);

        /// <summary>
        /// 设置频率（秒）。传入 0 使频率阀门常开。
        /// </summary>
        public void SetFrequency(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            lock (_lock)
            {
                _frequencySeconds = seconds;
                _frequencyAccumulator = 0;
                _frequencyGateOpen = seconds == 0;
            }
        }

        public void Tick(double deltaTime)
        {
            if (deltaTime < 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));

            List<(TimerToken token, ITimerQueue queue)> due = new();
            List<Action>? frequencyInvokes = null;
            bool gateOpen;
            lock (_lock)
            {
                gateOpen = _frequencySeconds == 0;
                _currentTime += deltaTime;
                if (_frequencySeconds > 0)
                {
                    _frequencyAccumulator += deltaTime;
                    if (_frequencyAccumulator >= _frequencySeconds)
                    {
                        gateOpen = true;
                        while (_frequencyAccumulator >= _frequencySeconds)
                        {
                            _frequencyAccumulator -= _frequencySeconds;
                        }

                        if (_frequencyQueues.Count > 0)
                        {
                            frequencyInvokes ??= new List<Action>();
                            foreach (var fq in _frequencyQueues.Values)
                            {
                                fq.CollectInvocations(frequencyInvokes);
                            }
                        }
                    }
                }
                while (_timeline.TryPeek(out var token, out var dueTime) && dueTime <= _currentTime)
                {
                    if (_timeline.TryDequeue(out token, out _))
                    {
                        if (_queues.TryGetValue(token.TypeId, out var queue))
                        {
                            due.Add((token, queue));
                        }
                    }
                }
            }

            for (int i = 0; i < due.Count; i++)
            {
                var (token, queue) = due[i];
                queue.TryInvoke(token);
            }

            if (frequencyInvokes != null)
            {
                foreach (var invoke in frequencyInvokes)
                {
                    invoke();
                }
            }

            Volatile.Write(ref _frequencyGateOpen, gateOpen);
        }

        public TimerToken RegisterAfter(double delay, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return RegisterAfter<EmptyPayload>(delay, default, _ => action());
        }

        public void FireAfter(double delay, Action action)
        {
            RegisterAfter(delay, action);
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, EventHandleDelegate<T> handle) where T : struct
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleDelegate(due, payload, handle));
        }

        public void FireAfter<T>(double delay, in T value, EventHandleDelegate<T> handle) where T : struct
        {
            RegisterAfter(delay, in value, handle);
        }

        public TimerToken RegisterAt(double timePoint, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return RegisterAt<EmptyPayload>(timePoint, default, _ => action());
        }

        public void FireAt(double timePoint, Action action)
        {
            RegisterAt(timePoint, action);
        }

        public TimerToken RegisterAt<T>(double timePoint, in T value, EventHandleDelegate<T> handle) where T : struct
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleDelegate(due, payload, handle));
        }

        public void FireAt<T>(double timePoint, in T value, EventHandleDelegate<T> handle) where T : struct
        {
            RegisterAt(timePoint, in value, handle);
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, EventHandleDelegateAsync<T> handle) where T : struct
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleDelegateAsync(due, payload, handle));
        }

        public void FireAfter<T>(double delay, in T value, EventHandleDelegateAsync<T> handle) where T : struct
        {
            RegisterAfter(delay, in value, handle);
        }

        public TimerToken RegisterAt<T>(double timePoint, in T value, EventHandleDelegateAsync<T> handle) where T : struct
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleDelegateAsync(due, payload, handle));
        }

        public void FireAt<T>(double timePoint, in T value, EventHandleDelegateAsync<T> handle) where T : struct
        {
            RegisterAt(timePoint, in value, handle);
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, IEventHandler<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleHandler(due, payload, handler));
        }

        public void FireAfter<T>(double delay, in T value, IEventHandler<T> handler) where T : struct
        {
            RegisterAfter(delay, in value, handler);
        }

        public TimerToken RegisterAt<T>(double timePoint, in T value, IEventHandler<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleHandler(due, payload, handler));
        }

        public void FireAt<T>(double timePoint, in T value, IEventHandler<T> handler) where T : struct
        {
            RegisterAt(timePoint, in value, handler);
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, IEventHandlerAsync<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleHandlerAsync(due, payload, handler));
        }

        public void FireAfter<T>(double delay, in T value, IEventHandlerAsync<T> handler) where T : struct
        {
            RegisterAfter(delay, in value, handler);
        }

        public TimerToken RegisterAt<T>(double timePoint, in T value, IEventHandlerAsync<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleHandlerAsync(due, payload, handler));
        }

        public void FireAt<T>(double timePoint, in T value, IEventHandlerAsync<T> handler) where T : struct
        {
            RegisterAt(timePoint, in value, handler);
        }

        public TimerToken RegisterAfter<T>(double delay, in T value, Action<Event<T>> action) where T : struct
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            var payload = value;
            return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleEventAction(due, payload, action));
        }

        public void FireAfter<T>(double delay, in T value, Action<Event<T>> action) where T : struct
        {
            RegisterAfter(delay, in value, action);
        }


        public TimerToken RegisterAt<T>(double timePoint, in T value, Action<Event<T>> action) where T : struct
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var payload = value;
            return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleEventAction(due, payload, action));
        }

        public void FireAt<T>(double timePoint, in T value, Action<Event<T>> action) where T : struct
        {
            RegisterAt(timePoint, in value, action);
        }

        public TimerToken RegisterOnFrequency<T>(in T value, EventHandleDelegate<T> handle) where T : struct
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            var payload = value;
            return RegisterFrequencyInternal<T>(queue => queue.RegisterDelegate(payload, handle));
        }

        public void FireOnFrequency<T>(in T value, EventHandleDelegate<T> handle) where T : struct
        {
            RegisterOnFrequency(in value, handle);
        }

        public TimerToken RegisterOnFrequency(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return RegisterFrequencyInternal<EmptyPayload>(queue => queue.RegisterEventAction(default, _ => action()));
        }

        public void FireOnFrequency(Action action)
        {
            RegisterOnFrequency(action);
        }

        public TimerToken RegisterOnFrequency<T>(in T value, EventHandleDelegateAsync<T> handle) where T : struct
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            var payload = value;
            return RegisterFrequencyInternal<T>(queue => queue.RegisterDelegateAsync(payload, handle));
        }

        public void FireOnFrequency<T>(in T value, EventHandleDelegateAsync<T> handle) where T : struct
        {
            RegisterOnFrequency(in value, handle);
        }

        public TimerToken RegisterOnFrequency<T>(in T value, IEventHandler<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var payload = value;
            return RegisterFrequencyInternal<T>(queue => queue.RegisterHandler(payload, handler));
        }

        public void FireOnFrequency<T>(in T value, IEventHandler<T> handler) where T : struct
        {
            RegisterOnFrequency(in value, handler);
        }

        public TimerToken RegisterOnFrequency<T>(in T value, IEventHandlerAsync<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var payload = value;
            return RegisterFrequencyInternal<T>(queue => queue.RegisterHandlerAsync(payload, handler));
        }

        public void FireOnFrequency<T>(in T value, IEventHandlerAsync<T> handler) where T : struct
        {
            RegisterOnFrequency(in value, handler);
        }

        public TimerToken RegisterOnFrequency<T>(in T value, Action<Event<T>> action) where T : struct
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var payload = value;
            return RegisterFrequencyInternal<T>(queue => queue.RegisterEventAction(payload, action));
        }

        public void FireOnFrequency<T>(in T value, Action<Event<T>> action) where T : struct
        {
            RegisterOnFrequency(in value, action);
        }
     
        public bool Cancel(in TimerToken token)
        {
            if (!token.IsValid) return false;

            lock (_lock)
            {
                if (_queues.TryGetValue(token.TypeId, out var queue) && queue.Cancel(token))
                {
                    return true;
                }

                if (_frequencyQueues.TryGetValue(token.TypeId, out var freqQueue) && freqQueue.Cancel(token))
                {
                    return true;
                }

                return false;
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

        private FrequencyQueue<T> GetFrequencyQueue<T>() where T : struct
        {
            int typeId = EventTypeId<T>.Id;
            if (_frequencyQueues.TryGetValue(typeId, out var queue))
            {
                return (FrequencyQueue<T>)queue;
            }

            var freqQueue = new FrequencyQueue<T>();
            _frequencyQueues[typeId] = freqQueue;
            return freqQueue;
        }

        private TimerToken RegisterFrequencyInternal<T>(Func<FrequencyQueue<T>, TimerToken> registrar) where T : struct
        {
            lock (_lock)
            {
                var queue = GetFrequencyQueue<T>();
                return registrar(queue);
            }
        }

        private sealed class TimerTimeline
        {
            private struct Entry
            {
                public TimerToken Token;
                public double Due;
            }

            private readonly List<Entry> _items = new();

            public void Enqueue(in TimerToken token, double due)
            {
                _items.Add(new Entry { Token = token, Due = due });
                BubbleUp(_items.Count - 1);
            }

            public bool TryPeek(out TimerToken token, out double due)
            {
                if (_items.Count == 0)
                {
                    token = default;
                    due = default;
                    return false;
                }

                var entry = _items[0];
                token = entry.Token;
                due = entry.Due;
                return true;
            }

            public bool TryDequeue(out TimerToken token, out double due)
            {
                if (_items.Count == 0)
                {
                    token = default;
                    due = default;
                    return false;
                }

                var root = _items[0];
                int lastIndex = _items.Count - 1;
                _items[0] = _items[lastIndex];
                _items.RemoveAt(lastIndex);

                if (_items.Count > 0)
                {
                    BubbleDown(0);
                }

                token = root.Token;
                due = root.Due;
                return true;
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (_items[parent].Due <= _items[index].Due)
                    {
                        break;
                    }

                    Swap(parent, index);
                    index = parent;
                }
            }

            private void BubbleDown(int index)
            {
                while (true)
                {
                    int left = (index * 2) + 1;
                    if (left >= _items.Count)
                    {
                        break;
                    }

                    int right = left + 1;
                    int smallest = right < _items.Count && _items[right].Due < _items[left].Due
                        ? right
                        : left;

                    if (_items[index].Due <= _items[smallest].Due)
                    {
                        break;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int a, int b)
            {
                var temp = _items[a];
                _items[a] = _items[b];
                _items[b] = temp;
            }
        }

    }

    internal interface ITimerQueue
    {
        bool TryInvoke(in TimerToken token);
        bool Cancel(in TimerToken token);
    }

    internal interface IFrequencyQueue
    {
        void CollectInvocations(List<Action> invocations);
        bool Cancel(in TimerToken token);
    }

    internal sealed class TimerQueue<T> : ITimerQueue where T : struct
    {
        private readonly FreeList<TimerTask<T>> _tasks = new FreeList<TimerTask<T>>(slabSize: 128);
        private readonly object _lock = new();

        internal TimerToken ScheduleDelegate(double executeAt, in T value, EventHandleDelegate<T> handle)
        {
            lock (_lock)
            {
                var slotRef = _tasks.Rent();
                ref var slot = ref _tasks.Resolve(slotRef);
                slot.Value = TimerTask<T>.FromDelegate(executeAt, value, handle);
                return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
            }
        }

        internal TimerToken ScheduleDelegateAsync(double executeAt, in T value, EventHandleDelegateAsync<T> handle)
        {
            lock (_lock)
            {
                var slotRef = _tasks.Rent();
                ref var slot = ref _tasks.Resolve(slotRef);
                slot.Value = TimerTask<T>.FromDelegateAsync(executeAt, value, handle);
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

        internal TimerToken ScheduleEventAction(double executeAt, in T value, Action<Event<T>> action)
        {
            lock (_lock)
            {
                var slotRef = _tasks.Rent();
                ref var slot = ref _tasks.Resolve(slotRef);
                slot.Value = TimerTask<T>.FromEventAction(executeAt, value, action);
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
            switch (task.Kind)
            {
                case TimerTaskKind.EventHandlerDelegate:
                    task.HandlerDelegate!.Invoke(in task.Payload);
                    break;
                case TimerTaskKind.EventHandlerDelegateAsync:
                    task.HandlerDelegateAsync!.Invoke(task.Payload).Forget();
                    break;
                case TimerTaskKind.EventHandler:
                    task.Handler!.Deal(in task.Payload);
                    break;
                case TimerTaskKind.EventHandlerAsync:
                    task.HandlerAsync!.Deal(task.Payload).Forget();
                    break;
                case TimerTaskKind.EventAction:
                    task.EventAction!.Invoke(new Event<T>(task.Payload));
                    break;
            }
        }
    }

    internal sealed class FrequencyQueue<T> : IFrequencyQueue where T : struct
    {
        private readonly List<FrequencyTask<T>> _tasks = new();
        private readonly Stack<int> _free = new();
        private readonly object _lock = new();

        internal TimerToken RegisterDelegate(in T value, EventHandleDelegate<T> handle)
        {
            lock (_lock)
            {
                var (index, version) = Rent();
                _tasks[index] = FrequencyTask<T>.FromDelegate(value, handle, version);
                return new TimerToken(EventTypeId<T>.Id, index, version);
            }
        }

        internal TimerToken RegisterDelegateAsync(in T value, EventHandleDelegateAsync<T> handle)
        {
            lock (_lock)
            {
                var (index, version) = Rent();
                _tasks[index] = FrequencyTask<T>.FromDelegateAsync(value, handle, version);
                return new TimerToken(EventTypeId<T>.Id, index, version);
            }
        }

        internal TimerToken RegisterHandler(in T value, IEventHandler<T> handler)
        {
            lock (_lock)
            {
                var (index, version) = Rent();
                _tasks[index] = FrequencyTask<T>.FromHandler(value, handler, version);
                return new TimerToken(EventTypeId<T>.Id, index, version);
            }
        }

        internal TimerToken RegisterHandlerAsync(in T value, IEventHandlerAsync<T> handler)
        {
            lock (_lock)
            {
                var (index, version) = Rent();
                _tasks[index] = FrequencyTask<T>.FromHandlerAsync(value, handler, version);
                return new TimerToken(EventTypeId<T>.Id, index, version);
            }
        }

        internal TimerToken RegisterEventAction(in T value, Action<Event<T>> action)
        {
            lock (_lock)
            {
                var (index, version) = Rent();
                _tasks[index] = FrequencyTask<T>.FromEventAction(value, action, version);
                return new TimerToken(EventTypeId<T>.Id, index, version);
            }
        }

        public void CollectInvocations(List<Action> invocations)
        {
            List<FrequencyTask<T>> snapshot;
            lock (_lock)
            {
                snapshot = new List<FrequencyTask<T>>(_tasks.Count);
                for (int i = 0; i < _tasks.Count; i++)
                {
                    var task = _tasks[i];
                    if (task.Active)
                    {
                        snapshot.Add(task);
                    }
                }
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                var task = snapshot[i];
                invocations.Add(() => ExecuteTask(task));
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
                if (token.Index < 0 || token.Index >= _tasks.Count)
                {
                    return false;
                }

                var entry = _tasks[token.Index];
                if (!entry.Active || entry.Version != token.Version)
                {
                    return false;
                }

                _tasks[token.Index] = default;
                _free.Push(token.Index);
                return true;
            }
        }

        private (int index, ushort version) Rent()
        {
            int index = _free.Count > 0 ? _free.Pop() : _tasks.Count;
            if (index == _tasks.Count)
            {
                _tasks.Add(default);
            }

            ushort version = NextVersion(_tasks[index].Version);
            return (index, version);
        }

        private static ushort NextVersion(ushort current)
        {
            ushort next = (ushort)(current + 1);
            if (next == 0)
            {
                next = 1;
            }
            return next;
        }

        private static void ExecuteTask(FrequencyTask<T> task)
        {
            switch (task.Kind)
            {
                case TimerTaskKind.EventHandlerDelegate:
                    task.HandlerDelegate!.Invoke(in task.Payload);
                    break;
                case TimerTaskKind.EventHandlerDelegateAsync:
                    task.HandlerDelegateAsync!.Invoke(task.Payload).Forget();
                    break;
                case TimerTaskKind.EventHandler:
                    task.Handler!.Deal(in task.Payload);
                    break;
                case TimerTaskKind.EventHandlerAsync:
                    task.HandlerAsync!.Deal(task.Payload).Forget();
                    break;
                case TimerTaskKind.EventAction:
                    task.EventAction!.Invoke(new Event<T>(task.Payload));
                    break;
            }
        }
    }

    internal struct FrequencyTask<T> where T : struct
    {
        public bool Active;
        public ushort Version;
        public T Payload;
        public TimerTaskKind Kind;
        public EventHandleDelegate<T>? HandlerDelegate;
        public EventHandleDelegateAsync<T>? HandlerDelegateAsync;
        public IEventHandler<T>? Handler;
        public IEventHandlerAsync<T>? HandlerAsync;
        public Action<Event<T>>? EventAction;

        public static FrequencyTask<T> FromDelegate(in T payload, EventHandleDelegate<T> handle, ushort version)
        {
            return new FrequencyTask<T>
            {
                Active = true,
                Version = version,
                Payload = payload,
                HandlerDelegate = handle,
                Kind = TimerTaskKind.EventHandlerDelegate,
            };
        }

        public static FrequencyTask<T> FromDelegateAsync(in T payload, EventHandleDelegateAsync<T> handle, ushort version)
        {
            return new FrequencyTask<T>
            {
                Active = true,
                Version = version,
                Payload = payload,
                HandlerDelegateAsync = handle,
                Kind = TimerTaskKind.EventHandlerDelegateAsync,
            };
        }

        public static FrequencyTask<T> FromHandler(in T payload, IEventHandler<T> handler, ushort version)
        {
            return new FrequencyTask<T>
            {
                Active = true,
                Version = version,
                Payload = payload,
                Handler = handler,
                Kind = TimerTaskKind.EventHandler,
            };
        }

        public static FrequencyTask<T> FromHandlerAsync(in T payload, IEventHandlerAsync<T> handler, ushort version)
        {
            return new FrequencyTask<T>
            {
                Active = true,
                Version = version,
                Payload = payload,
                HandlerAsync = handler,
                Kind = TimerTaskKind.EventHandlerAsync,
            };
        }

        public static FrequencyTask<T> FromEventAction(in T payload, Action<Event<T>> action, ushort version)
        {
            return new FrequencyTask<T>
            {
                Active = true,
                Version = version,
                Payload = payload,
                EventAction = action,
                Kind = TimerTaskKind.EventAction,
            };
        }
    }

    internal enum TimerTaskKind
    {
        None = 0,
        EventHandlerDelegate,
        EventHandlerDelegateAsync,
        EventHandler,
        EventHandlerAsync,
        EventAction,
    }

    internal struct TimerTask<T> where T : struct
    {
        public double ExecuteAt;
        public T Payload;
        public TimerTaskKind Kind;
        public EventHandleDelegate<T>? HandlerDelegate;
        public EventHandleDelegateAsync<T>? HandlerDelegateAsync;
        public IEventHandler<T>? Handler;
        public IEventHandlerAsync<T>? HandlerAsync;
        public Action<Event<T>>? EventAction;

        public static TimerTask<T> FromDelegate(double executeAt, in T payload, EventHandleDelegate<T> handle)
        {
            return new TimerTask<T>
            {
                ExecuteAt = executeAt,
                Payload = payload,
                HandlerDelegate = handle,
                Kind = TimerTaskKind.EventHandlerDelegate,
            };
        }

        public static TimerTask<T> FromDelegateAsync(double executeAt, in T payload, EventHandleDelegateAsync<T> handle)
        {
            return new TimerTask<T>
            {
                ExecuteAt = executeAt,
                Payload = payload,
                HandlerDelegateAsync = handle,
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

        public static TimerTask<T> FromEventAction(double executeAt, in T payload, Action<Event<T>> action)
        {
            return new TimerTask<T>
            {
                ExecuteAt = executeAt,
                Payload = payload,
                EventAction = action,
                Kind = TimerTaskKind.EventAction,
            };
        }
    }
}
