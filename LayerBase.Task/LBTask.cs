using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LayerBase.Async;

/// <summary>Lightweight awaitable task (no result).</summary>
[AsyncMethodBuilder(typeof(LBTaskMethodBuilder))]
public readonly struct LBTask
{
    internal readonly ILBTaskSource? Source;
    internal readonly int Version;

    internal LBTask(ILBTaskSource? source)
    {
        Source = source;
        Version = source?.Version ?? 0;
    }

    internal LBTask(ILBTaskSource? source, int version)
    {
        Source = source;
        Version = source == null ? 0 : version;
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(Source, Version);
    }

    public static LBTask CompletedTask => new(null);

    public static int DelayHeapPendingCount => DelayScheduler.PendingCount;

    public static int DelayHeapPeakPendingCount => DelayScheduler.PeakPendingCount;

    public static int DelayHeapLockContentionCount => DelayScheduler.LockContentionCount;

    public static LBTask FromException(Exception ex)
    {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        var src = LBTaskSource.Rent();
        src.SetException(ex);
        return new LBTask(src);
    }

    public static LBTask FromCanceled(CancellationToken token)
    {
        var src = LBTaskSource.Rent();
        src.SetCanceled(token);
        return new LBTask(src);
    }

    public static LBTask Yield()
    {
        var context = SynchronizationContext.Current;
        var src = LBTaskSource.Rent(context);
        if (context != null)
            context.Post(static state => ((LBTaskSource)state!).SetResult(), src);
        else
            ThreadPool.QueueUserWorkItem(static state => ((LBTaskSource)state!).SetResult(), src);
        return new LBTask(src);
    }

    public static LBTask NextFrame(SynchronizationContext? ctx = null, CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return FromCanceled(token);
        ctx ??= SynchronizationContext.Current;
        var src = LBTaskSource.Rent(ctx);
        if (ctx is LayerBaseSynchronizationContext lbCtx)
            lbCtx.ScheduleInFrames(static state => ((LBTaskSource)state!).SetResult(), src, 1);
        else if (ctx != null)
            ctx.Post(static state => ((LBTaskSource)state!).SetResult(), src);
        else
            ThreadPool.QueueUserWorkItem(static state => ((LBTaskSource)state!).SetResult(), src);
        return new LBTask(src);
    }

    public static LBTask Delay(TimeSpan delay, CancellationToken token = default)
    {
        if (delay <= TimeSpan.Zero) return CompletedTask;
        if (token.IsCancellationRequested) return FromCanceled(token);

        var src = LBTaskSource.Rent();
        var work = DelayWorkItem.Rent(src, token);
        DelayScheduler.Schedule(work, delay);
        work.RegisterCancellation();
        return new LBTask(src);
    }

    public static LBTask Run(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        var src = LBTaskSource.Rent();
        var work = RunActionWorkItem.Rent(action, src);
        ThreadPool.QueueUserWorkItem(RunActionWorkItem.InvokeOnThreadPool, work);
        return new LBTask(src);
    }

    public static LBTask RunOnMainThread(Action action, SynchronizationContext? ctx = null)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        ctx ??= SynchronizationContext.Current;
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        var src = LBTaskSource.Rent(ctx);
        var work = RunActionWorkItem.Rent(action, src);
        
        if (ctx is LayerBaseSynchronizationContext lbCtx)
            lbCtx.ScheduleInFrames(static state => RunActionWorkItem.InvokeOnContext(state), work, 1);
        else if (ctx != null)
            ctx.Post(RunActionWorkItem.InvokeOnContext, work);
        else
            ThreadPool.QueueUserWorkItem(static state => RunActionWorkItem.InvokeOnContext(state), work);
        return new LBTask(src);
    }

    public static LBTask RunBackground(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        var src = LBTaskSource.Rent(SynchronizationContext.Current);

        var success = ParallelExecutor.Instance.TrySchedule(() =>
        {
            try
            {
                action();
                src.SetResult();
            }
            catch (Exception ex)
            {
                src.SetException(ex);
            }
        });

        if (!success)
        {
            src.SetException(new InvalidOperationException("Background task queue is full (Backpressure: RejectNew)."));
        }

        return new LBTask(src);
    }

    public static LBTask RunBackground(Action<CancellationToken> action, CancellationToken cancellationToken)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        var src = LBTaskSource.Rent(SynchronizationContext.Current);

        var success = ParallelExecutor.Instance.TrySchedule(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    src.SetCanceled(cancellationToken);
                    return;
                }

                action(cancellationToken);
                src.SetResult();
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                src.SetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                src.SetException(ex);
            }
        });

        if (!success)
        {
            src.SetException(new InvalidOperationException("Background task queue is full (Backpressure: RejectNew)."));
        }

        return new LBTask(src);
    }

    public static LBTask<TResult> RunBackground<TResult>(Func<TResult> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        var src = LBTaskSource<TResult>.Rent(SynchronizationContext.Current);

        var success = ParallelExecutor.Instance.TrySchedule(() =>
        {
            try
            {
                var result = func();
                src.SetResult(result);
            }
            catch (Exception ex)
            {
                src.SetException(ex);
            }
        });

        if (!success)
        {
            src.SetException(new InvalidOperationException("Background task queue is full (Backpressure: RejectNew)."));
        }

        return new LBTask<TResult>(src);
    }

    public static LBTask<TResult> RunBackground<TResult>(Func<CancellationToken, TResult> func,
                                                         CancellationToken                cancellationToken)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        var src = LBTaskSource<TResult>.Rent(SynchronizationContext.Current);

        var success = ParallelExecutor.Instance.TrySchedule(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    src.SetCanceled(cancellationToken);
                    return;
                }

                var result = func(cancellationToken);
                src.SetResult(result);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                src.SetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                src.SetException(ex);
            }
        });

        if (!success)
        {
            src.SetException(new InvalidOperationException("Background task queue is full (Backpressure: RejectNew)."));
        }

        return new LBTask<TResult>(src);
    }

    public static LBTask SwitchToMainThread()
    {
        var ctx = SynchronizationContext.Current as LayerBaseSynchronizationContext;
        if (ctx == null) return CompletedTask;

        var src = LBTaskSource.Rent(ctx);
        ctx.CompletionQueue.Enqueue(() => src.SetResult());
        return new LBTask(src);
    }

    private sealed class DelayWorkItem
    {
        private static readonly ObjectPool<DelayWorkItem> Pool = new(() => new DelayWorkItem());

        public static readonly WaitCallback OnTimer = static state =>
        {
            var lease = (DelayWorkItemLease)state!;
            try
            {
                lease.Work.TryComplete(false, lease.LeaseVersion);
            }
            finally
            {
                lease.Return();
            }
        };

        private int _completed;
        private long _dueTimestamp;
        private int _leaseVersion;
        private int _registrationInitializing;
        private int _returnPending;
        private LBTaskSource? _source;
        private int _sourceVersion;
        private CancellationToken _token;
        public int HeapIndex = -1;

        public CancellationTokenRegistration CancellationRegistration;

        public long DueTimestamp
        {
            get => Volatile.Read(ref _dueTimestamp);
            set => Volatile.Write(ref _dueTimestamp, value);
        }

        public static DelayWorkItem Rent(LBTaskSource source, CancellationToken token)
        {
            var work = Pool.Rent();
            unchecked
            {
                work._leaseVersion++;
                if (work._leaseVersion == 0) work._leaseVersion = 1;
            }

            work._source = source;
            work._sourceVersion = source.Version;
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
                var registration = _token.Register(static state =>
                {
                    var lease = (DelayWorkItemLease)state!;
                    lease.Work.TryCancel(lease.LeaseVersion);
                    lease.Return();
                }, DelayWorkItemLease.Rent(this, Volatile.Read(ref _leaseVersion)));
            CancellationRegistration = registration;
            Volatile.Write(ref _registrationInitializing, 0);

            if (Volatile.Read(ref _completed) != 0)
            {
                registration.Dispose();
                CancellationRegistration = default;
            }

            if (Interlocked.Exchange(ref _returnPending, 0) == 1) ReturnToPool();
        }

        private void TryCancel(int leaseVersion)
        {
            if (leaseVersion != Volatile.Read(ref _leaseVersion)) return;
            DelayScheduler.Cancel(this);
            TryComplete(true, leaseVersion);
        }

        private void TryComplete(bool canceled, int leaseVersion)
        {
            if (leaseVersion != Volatile.Read(ref _leaseVersion)) return;
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;

            try
            {
                CancellationRegistration.Dispose();
                if (canceled)
                    _source!.TrySetCanceled(_sourceVersion, _token);
                else
                    _source!.TrySetResult(_sourceVersion);
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
            _sourceVersion = 0;
            _token = default;
            _dueTimestamp = 0;
            _registrationInitializing = 0;
            _returnPending = 0;
            CancellationRegistration = default;
            HeapIndex = -1;
            Pool.Return(this);
        }

        public DelayWorkItemLease CaptureLease()
        {
            return DelayWorkItemLease.Rent(this, Volatile.Read(ref _leaseVersion));
        }
    }

    private sealed class DelayWorkItemLease
    {
        private static readonly ObjectPool<DelayWorkItemLease> Pool =
            new(() => new DelayWorkItemLease());

        private int _returned;

        private DelayWorkItemLease()
        {
        }

        public DelayWorkItem Work { get; private set; } = null!;

        public int LeaseVersion { get; private set; }

        public static DelayWorkItemLease Rent(
            DelayWorkItem work,
            int leaseVersion)
        {
            DelayWorkItemLease lease = Pool.Rent();

            lease.Work = work;
            lease.LeaseVersion = leaseVersion;
            Volatile.Write(ref lease._returned, 0);

            return lease;
        }

        public void Return()
        {
            if (Interlocked.Exchange(
                    ref _returned,
                    1) != 0)
            {
                return;
            }

            Work = null!;
            LeaseVersion = 0;
            Pool.Return(this);
        }
    }

    private static class DelayScheduler
    {
        private static readonly object s_lock = new();
        private static readonly List<DelayWorkItem> s_heap = new();
        private static readonly Timer s_timer = new(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        private static int s_peakPendingCount;
        private static int s_lockAcquisitions;

        public static int PendingCount => s_heap.Count;

        public static int PeakPendingCount => Volatile.Read(ref s_peakPendingCount);

        public static int LockContentionCount => Volatile.Read(ref s_lockAcquisitions);

        public static void Schedule(DelayWorkItem work, TimeSpan delay)
        {
            var timestamp = Stopwatch.GetTimestamp();
            var delayTicks = ToTimestampTicks(delay);
            var due = timestamp + delayTicks;
            if (due < timestamp) due = long.MaxValue;
            work.DueTimestamp = due;

            lock (s_lock)
            {
                Interlocked.Increment(ref s_lockAcquisitions);
                HeapPush(work);
                if (s_heap.Count > Volatile.Read(ref s_peakPendingCount))
                    Interlocked.CompareExchange(ref s_peakPendingCount, s_heap.Count, Volatile.Read(ref s_peakPendingCount));
                if (ReferenceEquals(s_heap[0], work)) ArmTimer(due);
            }
        }

        public static bool Cancel(DelayWorkItem work)
        {
            lock (s_lock)
            {
                Interlocked.Increment(ref s_lockAcquisitions);
                var index = work.HeapIndex;
                if (index < 0 || index >= s_heap.Count || !ReferenceEquals(s_heap[index], work))
                    return false;

                work.HeapIndex = -1;
                HeapRemoveAt(index);
                if (s_heap.Count > 0) ArmTimer(s_heap[0].DueTimestamp);
                else s_timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return true;
            }
        }

        private static void OnTimer(object? state)
        {
            while (true)
            {
                DelayWorkItem? dueWork = null;
                var now = Stopwatch.GetTimestamp();

                lock (s_lock)
                {
                    Interlocked.Increment(ref s_lockAcquisitions);
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

                if (dueWork != null) ThreadPool.QueueUserWorkItem(DelayWorkItem.OnTimer, dueWork.CaptureLease());
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

            var milliseconds = ticks * 1000.0 / Stopwatch.Frequency;
            if (milliseconds >= int.MaxValue)
            {
                s_timer.Change(int.MaxValue, Timeout.Infinite);
                return;
            }

            s_timer.Change(Math.Max(1, (int)milliseconds), Timeout.Infinite);
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
                s_heap[index].HeapIndex = index;
                index = parent;
            }

            s_heap[index] = item;
            item.HeapIndex = index;
        }

        private static DelayWorkItem HeapPop()
        {
            var root = s_heap[0];
            var lastIndex = s_heap.Count - 1;
            var last = s_heap[lastIndex];
            s_heap.RemoveAt(lastIndex);
            root.HeapIndex = -1;
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
                s_heap[index].HeapIndex = index;
                index = child;
            }

            s_heap[index] = last;
            last.HeapIndex = index;
            return root;
        }

        private static void HeapRemoveAt(int removeIndex)
        {
            var lastIndex = s_heap.Count - 1;
            if (removeIndex == lastIndex)
            {
                s_heap[lastIndex].HeapIndex = -1;
                s_heap.RemoveAt(lastIndex);
                return;
            }

            var replacement = s_heap[lastIndex];
            s_heap.RemoveAt(lastIndex);
            s_heap[removeIndex] = replacement;
            replacement.HeapIndex = removeIndex;

            var index = removeIndex;
            while (index > 0)
            {
                var parent = (index - 1) >> 1;
                if (s_heap[parent].DueTimestamp <= replacement.DueTimestamp) break;

                s_heap[index] = s_heap[parent];
                s_heap[index].HeapIndex = index;
                index = parent;
            }

            if (index != removeIndex)
            {
                s_heap[index] = replacement;
                replacement.HeapIndex = index;
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
                s_heap[index].HeapIndex = index;
                index = child;
            }

            s_heap[index] = replacement;
            replacement.HeapIndex = index;
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
        private LBTaskSource? _source;

        public static RunActionWorkItem Rent(Action action, LBTaskSource source)
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
        private readonly ILBTaskSource? _source;
        private readonly int _version;

        internal Awaiter(ILBTaskSource? source, int version)
        {
            _source = source;
            _version = version;
        }

        public bool IsCompleted => _source == null || _source.IsCompleted(_version);

        public void OnCompleted(Action continuation)
        {
            if (_source == null)
            {
                continuation();
                return;
            }

            _source.OnCompleted(continuation, _version);
        }

        public void GetResult()
        {
            _source?.GetResult(_version);
        }
    }
}

/// <summary>Lightweight awaitable task with result.</summary>
[AsyncMethodBuilder(typeof(LBTaskMethodBuilder<>))]
public readonly struct LBTask<T>
{
    internal readonly ILBTaskSource<T>? Source;
    internal readonly T? Result;
    internal readonly bool HasResult;
    internal readonly int Version;

    internal LBTask(ILBTaskSource<T>? source)
    {
        Source = source;
        Result = default;
        HasResult = false;
        Version = source?.Version ?? 0;
    }

    internal LBTask(ILBTaskSource<T>? source, int version)
    {
        Source = source;
        Result = default;
        HasResult = false;
        Version = source == null ? 0 : version;
    }

    internal LBTask(T result)
    {
        Source = null;
        Result = result;
        HasResult = true;
        Version = 0;
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
        var src = LBTaskSource<T>.Rent();
        src.SetException(ex);
        return new LBTask<T>(src);
    }

    public static LBTask<T> FromCanceled(CancellationToken token)
    {
        var src = LBTaskSource<T>.Rent();
        src.SetCanceled(token);
        return new LBTask<T>(src);
    }

    public static LBTask<T> Run(Func<T> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        var src = LBTaskSource<T>.Rent();
        var work = RunFuncWorkItem.Rent(func, src);
        ThreadPool.QueueUserWorkItem(RunFuncWorkItem.InvokeOnThreadPool, work);
        return new LBTask<T>(src);
    }

    public static LBTask<T> RunOnMainThread(Func<T> func, SynchronizationContext? ctx  = null)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        ctx ??= SynchronizationContext.Current;
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        var src = LBTaskSource<T>.Rent(ctx);
        var work = RunFuncWorkItem.Rent(func, src);
        
        if (ctx is LayerBaseSynchronizationContext lbCtx)
            lbCtx.ScheduleInFrames(static state => RunFuncWorkItem.InvokeOnContext(state), work, 1);
        else if (ctx != null)
            ctx.Post(RunFuncWorkItem.InvokeOnContext, work);
        else 
            ThreadPool.QueueUserWorkItem(static state => RunFuncWorkItem.InvokeOnContext(state), work);
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
        private LBTaskSource<T>? _source;

        public static RunFuncWorkItem Rent(Func<T> func, LBTaskSource<T> source)
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

        public bool IsCompleted => _task.HasResult || _task.Source == null || _task.Source.IsCompleted(_task.Version);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            if (_task.HasResult || _task.Source == null)
            {
                continuation();
                return;
            }

            _task.Source.OnCompleted(continuation, _task.Version);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult()
        {
            if (_task.HasResult) return _task.Result!;
            return _task.Source == null ? default! : _task.Source.GetResult(_task.Version);
        }
    }
}
