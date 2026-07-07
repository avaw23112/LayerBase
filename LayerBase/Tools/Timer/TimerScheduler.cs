using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;

namespace LayerBase.Tools.Timer;

/// <summary>
/// 时间调度器，支持在指定时间点或按频率周期性地执行事件或任务。
/// 内部使用计时轮（TimerTimeline，基于最小堆）管理定时任务，支持单次、重复和频率门控模式。
/// </summary>
public sealed class TimerScheduler
{
    private readonly List<(TimerToken token, ITimerQueue queue)> _dueCache = new(64);
    private readonly List<IFrequencyQueue> _frequencyDueCache = new(16);
    private readonly Dictionary<int, IFrequencyQueue> _frequencyQueues = new();
    private readonly object _lock = new();
    private readonly Dictionary<int, ITimerQueue> _queues = new();

    private readonly TimerTimeline _timeline = new();
    private double _frequencyAccumulator;
    private bool _frequencyGateOpen = true;
    private double _frequencySeconds;
    private int _isTicking;

    /// <summary>
    /// 当前调度器内部时间（秒）。
    /// </summary>
    public double CurrentTime { get; private set; }

    /// <summary>
    /// 频率门控是否处于开放状态。开放时才允许执行频率任务。
    /// </summary>
    public bool IsFrequencyGateOpen => Volatile.Read(ref _frequencyGateOpen);

    /// <summary>
    /// 设置调度器的频率周期。为 0 表示门控持续开放，不做频率限制。
    /// </summary>
    public void SetFrequency(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));

        lock (_lock)
        {
            _frequencySeconds = seconds;
            _frequencyAccumulator = 0;
            _frequencyGateOpen = seconds == 0;
        }
    }

    /// <summary>
    /// 推进调度器时间，检查并执行所有到期的定时任务和频率门控任务。
    /// 该方法不可重入，同一时刻只能有一个 Tick 在执行。
    /// </summary>
    public void Tick(double deltaTime)
    {
        if (Interlocked.Exchange(ref _isTicking, 1) != 0)
            throw new InvalidOperationException("TimerScheduler.Tick is not reentrant.");

        try
        {
            _dueCache.Clear();
            _frequencyDueCache.Clear();
            bool gateOpen;
            var frequencyTriggered = false;

            lock (_lock)
            {
                gateOpen = _frequencySeconds == 0;
                CurrentTime += deltaTime;
                if (_frequencySeconds > 0)
                {
                    _frequencyAccumulator += deltaTime;
                    if (_frequencyAccumulator >= _frequencySeconds)
                    {
                        gateOpen = true;
                        frequencyTriggered = true;
                        while (_frequencyAccumulator >= _frequencySeconds) _frequencyAccumulator -= _frequencySeconds;
                    }
                }

                while (_timeline.TryPeek(out var token, out var dueTime) && dueTime <= CurrentTime)
                    if (_timeline.TryDequeue(out token, out _))
                        if (_queues.TryGetValue(token.TypeId, out var queue))
                            _dueCache.Add((token, queue));

                if (frequencyTriggered)
                    foreach (var fq in _frequencyQueues.Values)
                        _frequencyDueCache.Add(fq);
            }


            for (var i = 0; i < _dueCache.Count; i++)
            {
                var (token, queue) = _dueCache[i];
                queue.TryInvoke(token);
            }


            if (frequencyTriggered)
                for (var i = 0; i < _frequencyDueCache.Count; i++)
                    _frequencyDueCache[i].ExecuteAll();

            Volatile.Write(ref _frequencyGateOpen, gateOpen);
        }
        finally
        {
            Volatile.Write(ref _isTicking, 0);
        }
    }

    /// <summary>
    /// 在指定延迟后执行一次操作。
    /// </summary>
    /// <param name="delay">延迟秒数。</param>
    /// <param name="action">要执行的操作。</param>
    /// <returns>定时任务令牌。</returns>
    public TimerToken RegisterAfter(double delay, Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return RegisterAfter<EmptyPayload>(delay, default, _ => action());
    }

    /// <summary>
    /// 在指定延迟后注册事件处理委托。
    /// </summary>
    public TimerToken RegisterAfter<T>(double delay, in T value, EventHandleDelegate<T> handle) where T : struct
    {
        if (handle == null) throw new ArgumentNullException(nameof(handle));
        if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
        var localValue = value;
        return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleDelegate(due, localValue, handle));
    }

    public TimerToken RegisterAt<T>(double timePoint, in T value, EventHandleDelegate<T> handle) where T : struct
    {
        if (handle == null) throw new ArgumentNullException(nameof(handle));
        var localValue = value;
        return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleDelegate(due, localValue, handle));
    }

    public TimerToken RegisterAfter<T>(double delay, in T value, EventHandleDelegateAsync<T> handle) where T : struct
    {
        if (handle == null) throw new ArgumentNullException(nameof(handle));
        if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
        var localValue = value;
        return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleDelegateAsync(due, localValue, handle));
    }

    public TimerToken RegisterAt<T>(double timePoint, in T value, EventHandleDelegateAsync<T> handle) where T : struct
    {
        if (handle == null) throw new ArgumentNullException(nameof(handle));
        var localValue = value;
        return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleDelegateAsync(due, localValue, handle));
    }

    public TimerToken RegisterAfter<T>(double delay, in T value, IEventHandler<T> handler) where T : struct
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
        var localValue = value;
        return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleHandler(due, localValue, handler));
    }

    public TimerToken RegisterAt<T>(double timePoint, in T value, IEventHandler<T> handler) where T : struct
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var localValue = value;
        return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleHandler(due, localValue, handler));
    }

    public TimerToken RegisterAfter<T>(double delay, in T value, IEventHandlerAsync<T> handler) where T : struct
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
        var localValue = value;
        return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleHandlerAsync(due, localValue, handler));
    }

    public TimerToken RegisterAt<T>(double timePoint, in T value, IEventHandlerAsync<T> handler) where T : struct
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var localValue = value;
        return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleHandlerAsync(due, localValue, handler));
    }

    public TimerToken RegisterAfter<T>(double delay, in T value, Action<Event<T>> action) where T : struct
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
        var localValue = value;
        return RegisterAfterInternal<T>(delay, (queue, due) => queue.ScheduleEventAction(due, localValue, action));
    }

    public TimerToken RegisterAt<T>(double timePoint, in T value, Action<Event<T>> action) where T : struct
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        var localValue = value;
        return RegisterAtInternal<T>(timePoint, (queue, due) => queue.ScheduleEventAction(due, localValue, action));
    }

    public TimerToken RegisterOnFrequency<T>(in T value, EventHandleDelegate<T> handle) where T : struct
    {
        if (handle == null) throw new ArgumentNullException(nameof(handle));
        var localValue = value;
        return RegisterFrequencyInternal<T>(queue => queue.RegisterDelegate(localValue, handle));
    }

    public TimerToken RegisterOnFrequency(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return RegisterFrequencyInternal<EmptyPayload>(queue => queue.RegisterEventAction(default, _ => action()));
    }

    public TimerToken RegisterOnFrequency<T>(in T value, EventHandleDelegateAsync<T> handle) where T : struct
    {
        if (handle == null) throw new ArgumentNullException(nameof(handle));
        var localValue = value;
        return RegisterFrequencyInternal<T>(queue => queue.RegisterDelegateAsync(localValue, handle));
    }

    public TimerToken RegisterOnFrequency<T>(in T value, IEventHandler<T> handler) where T : struct
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var localValue = value;
        return RegisterFrequencyInternal<T>(queue => queue.RegisterHandler(localValue, handler));
    }

    public TimerToken RegisterOnFrequency<T>(in T value, IEventHandlerAsync<T> handler) where T : struct
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var localValue = value;
        return RegisterFrequencyInternal<T>(queue => queue.RegisterHandlerAsync(localValue, handler));
    }

    public TimerToken RegisterOnFrequency<T>(in T value, Action<Event<T>> action) where T : struct
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        var localValue = value;
        return RegisterFrequencyInternal<T>(queue => queue.RegisterEventAction(localValue, action));
    }

    public bool Cancel(in TimerToken token)
    {
        if (!token.IsValid) return false;

        lock (_lock)
        {
            if (_queues.TryGetValue(token.TypeId, out var queue) && queue.Cancel(token)) return true;
            if (_frequencyQueues.TryGetValue(token.TypeId, out var freqQueue) && freqQueue.Cancel(token)) return true;
            return false;
        }
    }

    private TimerToken RegisterAfterInternal<T>(double delay, Func<TimerQueue<T>, double, TimerToken> registrar)
        where T : struct
    {
        lock (_lock)
        {
            var queue = GetQueue<T>();
            var due = Normalize(CurrentTime + delay);
            var token = registrar(queue, due);
            _timeline.Enqueue(token, due);
            return token;
        }
    }

    private TimerToken RegisterAtInternal<T>(double timePoint, Func<TimerQueue<T>, double, TimerToken> registrar)
        where T : struct
    {
        lock (_lock)
        {
            var queue = GetQueue<T>();
            var due = Normalize(timePoint);
            var token = registrar(queue, due);
            _timeline.Enqueue(token, due);
            return token;
        }
    }

    private double Normalize(double timePoint)
    {
        if (double.IsNaN(timePoint) || double.IsInfinity(timePoint))
            throw new ArgumentOutOfRangeException(nameof(timePoint));
        return timePoint < 0 ? 0 : timePoint;
    }

    private TimerQueue<T> GetQueue<T>() where T : struct
    {
        var typeId = EventTypeId<T>.Id;
        if (_queues.TryGetValue(typeId, out var queue)) return (TimerQueue<T>)queue;

        var timerQueue = new TimerQueue<T>();
        _queues[typeId] = timerQueue;
        return timerQueue;
    }

    private FrequencyQueue<T> GetFrequencyQueue<T>() where T : struct
    {
        var typeId = EventTypeId<T>.Id;
        if (_frequencyQueues.TryGetValue(typeId, out var queue)) return (FrequencyQueue<T>)queue;

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

    private readonly struct EmptyPayload
    {
    }

    private sealed class TimerTimeline
    {
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
            var lastIndex = _items.Count - 1;
            _items[0] = _items[lastIndex];
            _items.RemoveAt(lastIndex);

            if (_items.Count > 0) BubbleDown(0);

            token = root.Token;
            due = root.Due;
            return true;
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                if (_items[parent].Due <= _items[index].Due) break;

                Swap(parent, index);
                index = parent;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                var left = index * 2 + 1;
                if (left >= _items.Count) break;

                var right = left + 1;
                var smallest = right < _items.Count && _items[right].Due < _items[left].Due
                    ? right
                    : left;

                if (_items[index].Due <= _items[smallest].Due) break;

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

        private struct Entry
        {
            public TimerToken Token;
            public double Due;
        }
    }
}