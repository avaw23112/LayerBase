using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LayerBase.Async;

/// <summary>Lightweight awaitable task (no result).</summary>
[AsyncMethodBuilder(typeof(LBTaskMethodBuilder))]
public readonly struct LBTask
{
    internal readonly IArchTaskSource? Source;

    internal LBTask(IArchTaskSource? source)
    {
        Source = source;
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(Source);
    }

    public static LBTask CompletedTask => new(null);

    public static LBTask FromException(Exception ex)
    {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        var src = ArchTaskSource.Rent();
        src.SetException(ex);
        return new LBTask(src);
    }

    public static LBTask FromCanceled(CancellationToken token)
    {
        var src = ArchTaskSource.Rent();
        src.SetCanceled(token);
        return new LBTask(src);
    }

    public static LBTask Yield()
    {
        var src = ArchTaskSource.Rent();
        ThreadPool.QueueUserWorkItem(static state => ((ArchTaskSource)state!).SetResult(), src);
        return new LBTask(src);
    }

    public static LBTask NextFrame(SynchronizationContext? ctx = null, CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return FromCanceled(token);
        ctx ??= SynchronizationContext.Current;
        var src = ArchTaskSource.Rent();
        if (ctx is LayerBaseSynchronizationContext lbCtx)
            lbCtx.ScheduleInFrames(static state => ((ArchTaskSource)state!).SetResult(), src, 1);
        else if (ctx != null)
            ctx.Post(static state => ((ArchTaskSource)state!).SetResult(), src);
        else
            ThreadPool.QueueUserWorkItem(static state => ((ArchTaskSource)state!).SetResult(), src);
        return new LBTask(src);
    }

    public static LBTask Delay(TimeSpan delay, CancellationToken token = default)
    {
        if (delay <= TimeSpan.Zero) return CompletedTask;
        if (token.IsCancellationRequested) return FromCanceled(token);

        var src = ArchTaskSource.Rent();
        var work = DelayWorkItem.Rent(src, token);
        DelayScheduler.Schedule(work, delay);
        work.RegisterCancellation();
        return new LBTask(src);
    }

    public static LBTask Run(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        var src = ArchTaskSource.Rent();
        var work = RunActionWorkItem.Rent(action, src);
        ThreadPool.QueueUserWorkItem(RunActionWorkItem.InvokeOnThreadPool, work);
        return new LBTask(src);
    }

    public static LBTask RunOnMainThread(Action action, SynchronizationContext ctx)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        var src = ArchTaskSource.Rent();
        var work = RunActionWorkItem.Rent(action, src);
        ctx.Post(RunActionWorkItem.InvokeOnContext, work);
        return new LBTask(src);
    }

    private sealed class DelayWorkItem
    {
        private static readonly ObjectPool<DelayWorkItem> Pool = new(() => new DelayWorkItem());

        public static readonly WaitCallback OnTimer = static state =>
            ((DelayWorkItem)state!).TryComplete(false);

        private int _completed;
        private long _dueTimestamp;
        private int _registrationInitializing;
        private int _returnPending;
        private ArchTaskSource? _source;
        private CancellationToken _token;

        public CancellationTokenRegistration CancellationRegistration;

        public long DueTimestamp
        {
            get => Volatile.Read(ref _dueTimestamp);
            set => Volatile.Write(ref _dueTimestamp, value);
        }

        public static DelayWorkItem Rent(ArchTaskSource source, CancellationToken token)
        {
            var work = Pool.Rent();
            work._source = source;
            work._token = token;
            work._dueTimestamp = 0;
            work._completed = 0;
            work._registrationInitializing = 0;
            work._returnPending = 0;
            work.CancellationRegistration = default;
            return work;
        }

        public void RegisterCancellation()
        {
            if (!_token.CanBeCanceled) return;

            Volatile.Write(ref _registrationInitializing, 1);
            var registration = _token.Register(static state => ((DelayWorkItem)state!).TryCancel(), this);
            CancellationRegistration = registration;
            Volatile.Write(ref _registrationInitializing, 0);

            if (Volatile.Read(ref _completed) != 0)
            {
                registration.Dispose();
                CancellationRegistration = default;
            }

            if (Interlocked.Exchange(ref _returnPending, 0) == 1) ReturnToPool();
        }

        private void TryCancel()
        {
            DelayScheduler.Cancel(this);
            TryComplete(true);
        }

        private void TryComplete(bool canceled)
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;

            try
            {
                CancellationRegistration.Dispose();
                if (canceled)
                    _source!.SetCanceled(_token);
                else
                    _source!.SetResult();
            }
            finally
            {
                if (Volatile.Read(ref _registrationInitializing) == 1)
                    Volatile.Write(ref _returnPending, 1);
                else
                    ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            _source = null;
            _token = default;
            _dueTimestamp = 0;
            _registrationInitializing = 0;
            _returnPending = 0;
            CancellationRegistration = default;
            Pool.Return(this);
        }
    }

    private static class DelayScheduler
    {
        private static readonly object s_lock = new();
        private static readonly List<DelayWorkItem> s_heap = new();
        private static readonly Timer s_timer = new(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        public static void Schedule(DelayWorkItem work, TimeSpan delay)
        {
            var due = Stopwatch.GetTimestamp() + ToTimestampTicks(delay);
            work.DueTimestamp = due;

            lock (s_lock)
            {
                HeapPush(work);
                if (ReferenceEquals(s_heap[0], work)) ArmTimer(due);
            }
        }

        public static bool Cancel(DelayWorkItem work)
        {
            lock (s_lock)
            {
                for (var i = 0; i < s_heap.Count; i++)
                {
                    if (!ReferenceEquals(s_heap[i], work)) continue;

                    HeapRemoveAt(i);
                    if (s_heap.Count > 0) ArmTimer(s_heap[0].DueTimestamp);
                    else s_timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    return true;
                }
            }

            return false;
        }

        private static void OnTimer(object? state)
        {
            while (true)
            {
                DelayWorkItem? dueWork = null;
                var now = Stopwatch.GetTimestamp();

                lock (s_lock)
                {
                    if (s_heap.Count == 0)
                    {
                        s_timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                        return;
                    }

                    var next = s_heap[0];
                    var nextDue = next.DueTimestamp;
                    if (nextDue > now)
                    {
                        ArmTimer(nextDue);
                        return;
                    }

                    dueWork = HeapPop();
                }

                if (dueWork != null) ThreadPool.QueueUserWorkItem(DelayWorkItem.OnTimer, dueWork);
            }
        }

        private static long ToTimestampTicks(TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero) return 0;

            var ticks = delay.TotalSeconds * Stopwatch.Frequency;
            if (ticks >= long.MaxValue) return long.MaxValue;

            return (long)ticks;
        }

        private static void ArmTimer(long dueTimestamp)
        {
            var now = Stopwatch.GetTimestamp();
            var ticks = dueTimestamp - now;
            if (ticks <= 0)
            {
                s_timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
                return;
            }

            var seconds = ticks / (double)Stopwatch.Frequency;
            s_timer.Change(TimeSpan.FromSeconds(seconds), Timeout.InfiniteTimeSpan);
        }

        private static void HeapPush(DelayWorkItem item)
        {
            s_heap.Add(item);
            var index = s_heap.Count - 1;
            while (index > 0)
            {
                var parent = (index - 1) >> 1;
                if (s_heap[parent].DueTimestamp <= item.DueTimestamp) break;

                s_heap[index] = s_heap[parent];
                index = parent;
            }

            s_heap[index] = item;
        }

        private static DelayWorkItem HeapPop()
        {
            var root = s_heap[0];
            var lastIndex = s_heap.Count - 1;
            var last = s_heap[lastIndex];
            s_heap.RemoveAt(lastIndex);
            if (lastIndex == 0) return root;

            var index = 0;
            while (true)
            {
                var left = (index << 1) + 1;
                if (left >= s_heap.Count) break;

                var right = left + 1;
                var child = right < s_heap.Count &&
                            s_heap[right].DueTimestamp < s_heap[left].DueTimestamp
                    ? right
                    : left;

                if (s_heap[child].DueTimestamp >= last.DueTimestamp) break;

                s_heap[index] = s_heap[child];
                index = child;
            }

            s_heap[index] = last;
            return root;
        }

        private static void HeapRemoveAt(int removeIndex)
        {
            var lastIndex = s_heap.Count - 1;
            if (removeIndex == lastIndex)
            {
                s_heap.RemoveAt(lastIndex);
                return;
            }

            var replacement = s_heap[lastIndex];
            s_heap.RemoveAt(lastIndex);
            s_heap[removeIndex] = replacement;

            var index = removeIndex;
            while (index > 0)
            {
                var parent = (index - 1) >> 1;
                if (s_heap[parent].DueTimestamp <= replacement.DueTimestamp) break;

                s_heap[index] = s_heap[parent];
                index = parent;
            }

            if (index != removeIndex)
            {
                s_heap[index] = replacement;
                return;
            }

            while (true)
            {
                var left = (index << 1) + 1;
                if (left >= s_heap.Count) break;

                var right = left + 1;
                var child = right < s_heap.Count &&
                            s_heap[right].DueTimestamp < s_heap[left].DueTimestamp
                    ? right
                    : left;

                if (s_heap[child].DueTimestamp >= replacement.DueTimestamp) break;

                s_heap[index] = s_heap[child];
                index = child;
            }

            s_heap[index] = replacement;
        }
    }

    private sealed class RunActionWorkItem
    {
        private static readonly ObjectPool<RunActionWorkItem> Pool = new(() => new RunActionWorkItem());

        public static readonly WaitCallback InvokeOnThreadPool = static state =>
            ((RunActionWorkItem)state!).Invoke();

        public static readonly SendOrPostCallback InvokeOnContext = static state =>
            ((RunActionWorkItem)state!).Invoke();

        private Action? _action;
        private ArchTaskSource? _source;

        public static RunActionWorkItem Rent(Action action, ArchTaskSource source)
        {
            var work = Pool.Rent();
            work._action = action;
            work._source = source;
            return work;
        }

        private void Invoke()
        {
            try
            {
                _action!();
                _source!.SetResult();
            }
            catch (Exception ex)
            {
                _source!.SetException(ex);
            }
            finally
            {
                _action = null;
                _source = null;
                Pool.Return(this);
            }
        }
    }

    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly IArchTaskSource? _source;

        internal Awaiter(IArchTaskSource? source)
        {
            _source = source;
        }

        public bool IsCompleted => _source == null || _source.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            if (_source == null)
            {
                continuation();
                return;
            }

            _source.OnCompleted(continuation);
        }

        public void GetResult()
        {
            _source?.GetResult();
        }
    }
}

/// <summary>Lightweight awaitable task with result.</summary>
[AsyncMethodBuilder(typeof(LBTaskMethodBuilder<>))]
public readonly struct LBTask<T>
{
    internal readonly IArchTaskSource<T>? Source;
    internal readonly T? Result;
    internal readonly bool HasResult;

    internal LBTask(IArchTaskSource<T>? source)
    {
        Source = source;
        Result = default;
        HasResult = false;
    }

    internal LBTask(T result)
    {
        Source = null;
        Result = result;
        HasResult = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Awaiter GetAwaiter()
    {
        return new Awaiter(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LBTask<T> FromResult(T value)
    {
        return new LBTask<T>(value);
    }

    public static LBTask<T> FromException(Exception ex)
    {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        var src = ArchTaskSource<T>.Rent();
        src.SetException(ex);
        return new LBTask<T>(src);
    }

    public static LBTask<T> FromCanceled(CancellationToken token)
    {
        var src = ArchTaskSource<T>.Rent();
        src.SetCanceled(token);
        return new LBTask<T>(src);
    }

    public static LBTask<T> Run(Func<T> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        var src = ArchTaskSource<T>.Rent();
        var work = RunFuncWorkItem.Rent(func, src);
        ThreadPool.QueueUserWorkItem(RunFuncWorkItem.InvokeOnThreadPool, work);
        return new LBTask<T>(src);
    }

    public static LBTask<T> RunOnMainThread(Func<T> func, SynchronizationContext ctx)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        var src = ArchTaskSource<T>.Rent();
        var work = RunFuncWorkItem.Rent(func, src);
        ctx.Post(RunFuncWorkItem.InvokeOnContext, work);
        return new LBTask<T>(src);
    }

    private sealed class RunFuncWorkItem
    {
        private static readonly ObjectPool<RunFuncWorkItem> Pool = new(() => new RunFuncWorkItem());

        public static readonly WaitCallback InvokeOnThreadPool = static state =>
            ((RunFuncWorkItem)state!).Invoke();

        public static readonly SendOrPostCallback InvokeOnContext = static state =>
            ((RunFuncWorkItem)state!).Invoke();

        private Func<T>? _func;
        private ArchTaskSource<T>? _source;

        public static RunFuncWorkItem Rent(Func<T> func, ArchTaskSource<T> source)
        {
            var work = Pool.Rent();
            work._func = func;
            work._source = source;
            return work;
        }

        private void Invoke()
        {
            try
            {
                _source!.SetResult(_func!());
            }
            catch (Exception ex)
            {
                _source!.SetException(ex);
            }
            finally
            {
                _func = null;
                _source = null;
                Pool.Return(this);
            }
        }
    }

    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly LBTask<T> _task;

        internal Awaiter(LBTask<T> task)
        {
            _task = task;
        }

        public bool IsCompleted => _task.HasResult || _task.Source == null || _task.Source.IsCompleted;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            if (_task.HasResult || _task.Source == null)
            {
                continuation();
                return;
            }

            _task.Source.OnCompleted(continuation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult()
        {
            if (_task.HasResult) return _task.Result!;
            return _task.Source == null ? default! : _task.Source.GetResult();
        }
    }
}

