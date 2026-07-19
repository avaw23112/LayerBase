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

    public static LBTask FromTask(Task task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (task.IsCompletedSuccessfully) return CompletedTask;

        var src = LBTaskSource.Rent();
        var version = src.Version;
        task.ContinueWith(static (completedTask, state) =>
        {
            var (source, sourceVersion) = ((LBTaskSource Source, int Version))state!;
            try
            {
                completedTask.GetAwaiter().GetResult();
                source.TrySetResult(sourceVersion);
            }
            catch (OperationCanceledException ex)
            {
                source.TrySetCanceled(sourceVersion, ex.CancellationToken);
            }
            catch (Exception ex)
            {
                source.TrySetException(sourceVersion, ex);
            }
        }, (src, version), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return new LBTask(src, version);
    }

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
#if NET8_0_OR_GREATER
        return FromTask(Task.Delay(delay, TimeProvider.System, token));
#else
        return FromTask(Task.Delay(delay, token));
#endif
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
