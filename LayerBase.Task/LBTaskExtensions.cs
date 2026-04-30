namespace LayerBase.Async;

public static class LBTaskExtensions
{
    public static LBTask WithTimeout(this LBTask task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return LBTask.FromCanceled(cancellationToken);

        var tcs = new LBTaskCompletionSource();
        var resultTask = tcs.Task;
        var state = new CompletionState(tcs, cancellationToken);

        state.SetTimer(new Timer(static s => ((CompletionState)s!).TrySetException(new TimeoutException()), state,
            timeout, Timeout.InfiniteTimeSpan));

        if (cancellationToken.CanBeCanceled)
            state.SetRegistration(cancellationToken.Register(static s => ((CompletionState)s!).TrySetCanceled(),
                state));

        task.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                task.GetAwaiter().GetResult();
                state.TrySetResult();
            }
            catch (Exception ex)
            {
                state.TrySetException(ex);
            }
        });

        return resultTask;
    }

    public static LBTask<T> WithTimeout<T>(this LBTask<T>    task, TimeSpan timeout,
                                           CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return LBTask<T>.FromCanceled(cancellationToken);

        var tcs = new LBTaskCompletionSource<T>();
        var resultTask = tcs.Task;
        var state = new CompletionState<T>(tcs, cancellationToken);

        state.SetTimer(new Timer(static s => ((CompletionState<T>)s!).TrySetException(new TimeoutException()), state,
            timeout, Timeout.InfiniteTimeSpan));

        if (cancellationToken.CanBeCanceled)
            state.SetRegistration(cancellationToken.Register(static s => ((CompletionState<T>)s!).TrySetCanceled(),
                state));

        task.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                var res = task.GetAwaiter().GetResult();
                state.TrySetResult(res);
            }
            catch (Exception ex)
            {
                state.TrySetException(ex);
            }
        });

        return resultTask;
    }

    public static LBTask<(bool isCanceled, T result)> SuppressCancellationThrow<T>(this LBTask<T> task)
    {
        var tcs = new LBTaskCompletionSource<(bool, T)>();
        task.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                var res = task.GetAwaiter().GetResult();
                tcs.TrySetResult((false, res));
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetResult((true, default!));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    public static LBTask<bool> SuppressCancellationThrow(this LBTask task)
    {
        var tcs = new LBTaskCompletionSource<bool>();
        task.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                task.GetAwaiter().GetResult();
                tcs.TrySetResult(false);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    public static LBTask WhenAll(params LBTask[] tasks)
    {
        if (tasks == null || tasks.Length == 0) return LBTask.CompletedTask;

        var remaining = tasks.Length;
        var tcs = new LBTaskCompletionSource();
        foreach (var task in tasks)
            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    return;
                }

                if (Interlocked.Decrement(ref remaining) == 0) tcs.TrySetResult();
            });

        return tcs.Task;
    }

    public static LBTask<T[]> WhenAll<T>(params LBTask<T>[] tasks)
    {
        if (tasks == null || tasks.Length == 0) return LBTask<T[]>.FromResult(Array.Empty<T>());

        var remaining = tasks.Length;
        var results = new T[tasks.Length];
        var tcs = new LBTaskCompletionSource<T[]>();

        for (var i = 0; i < tasks.Length; i++)
        {
            var index = i;
            var task = tasks[i];
            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    results[index] = task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    return;
                }

                if (Interlocked.Decrement(ref remaining) == 0) tcs.TrySetResult(results);
            });
        }

        return tcs.Task;
    }

    public static LBTask<int> WhenAny(params LBTask[] tasks)
    {
        if (tasks == null || tasks.Length == 0)
            return LBTask<int>.FromException(new InvalidOperationException("No tasks"));

        var tcs = new LBTaskCompletionSource<int>();
        var won = 0;
        for (var i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i].GetAwaiter().OnCompleted(() =>
            {
                if (Interlocked.CompareExchange(ref won, 1, 0) == 0)
                {
                    try
                    {
                        tasks[index].GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        return;
                    }

                    tcs.TrySetResult(index);
                }
            });
        }

        return tcs.Task;
    }

    public static void Forget(this LBTask task, Action<Exception>? onException = null)
    {
        _ = new LBTaskVoid(task, onException);
    }

    public static void Forget<T>(this LBTask<T> task, Action<Exception>? onException = null)
    {
        ForgetObserver<T>.Observe(task, onException);
    }

    public static LBTask WithCancellation(this LBTask task, CancellationToken token)
    {
        return task.AttachExternalCancellation(token);
    }

    public static LBTask<T> WithCancellation<T>(this LBTask<T> task, CancellationToken token)
    {
        return task.AttachExternalCancellation(token);
    }

    public static LBTask WaitUntilCanceled(this CancellationToken token)
    {
        if (!token.CanBeCanceled) return LBTask.CompletedTask;

        var tcs = new LBTaskCompletionSource();
        if (token.IsCancellationRequested)
        {
            tcs.TrySetCanceled(token);
            return tcs.Task;
        }

        var registration = token.Register(() => tcs.TrySetCanceled(token));
        tcs.Task.GetAwaiter().OnCompleted(() => registration.Dispose());
        return tcs.Task;
    }

    public static LBTask<T> AttachExternalCancellation<T>(this LBTask<T> task, CancellationToken token)
    {
        if (!token.CanBeCanceled) return task;

        var tcs = new LBTaskCompletionSource<T>();
        var resultTask = tcs.Task;
        if (token.IsCancellationRequested)
        {
            tcs.TrySetCanceled(token);
            return resultTask;
        }

        var state = new CompletionState<T>(tcs, token);
        state.SetRegistration(token.Register(static s => ((CompletionState<T>)s!).TrySetCanceled(), state));
        task.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                var res = task.GetAwaiter().GetResult();
                state.TrySetResult(res);
            }
            catch (Exception ex)
            {
                state.TrySetException(ex);
            }
        });

        return resultTask;
    }

    public static LBTask AttachExternalCancellation(this LBTask task, CancellationToken token)
    {
        if (!token.CanBeCanceled) return task;

        var tcs = new LBTaskCompletionSource();
        var resultTask = tcs.Task;
        if (token.IsCancellationRequested)
        {
            tcs.TrySetCanceled(token);
            return resultTask;
        }

        var state = new CompletionState(tcs, token);
        state.SetRegistration(token.Register(static s => ((CompletionState)s!).TrySetCanceled(), state));
        task.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                task.GetAwaiter().GetResult();
                state.TrySetResult();
            }
            catch (Exception ex)
            {
                state.TrySetException(ex);
            }
        });

        return resultTask;
    }

    public static LBTask WaitUntil(Func<bool>        predicate, SynchronizationContext? ctx = null,
                                   CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        ctx ??= SynchronizationContext.Current;

        var tcs = new LBTaskCompletionSource();

        void Tick()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }

            if (predicate())
                tcs.TrySetResult();
            else
                LBTask.NextFrame(ctx, cancellationToken).GetAwaiter().OnCompleted(Tick);
        }

        Tick();
        return tcs.Task;
    }

    public static LBTask WaitWhile(Func<bool>        predicate, SynchronizationContext? ctx = null,
                                   CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return WaitUntil(() => !predicate(), ctx, cancellationToken);
    }

    private sealed class ForgetObserver<T>
    {
        private static readonly ObjectPool<ForgetObserver<T>> Pool = new(() => new ForgetObserver<T>());

        private readonly Action _continuation;
        private Action<Exception>? _onException;
        private LBTask<T> _task;

        private ForgetObserver()
        {
            _continuation = Complete;
        }

        public static void Observe(LBTask<T> task, Action<Exception>? onException)
        {
            var observer = Pool.Rent();
            observer._task = task;
            observer._onException = onException;
            task.GetAwaiter().OnCompleted(observer._continuation);
        }

        private void Complete()
        {
            try
            {
                _task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _onException?.Invoke(ex);
            }
            finally
            {
                _task = default;
                _onException = null;
                Pool.Return(this);
            }
        }
    }

    private abstract class RegistrationState
    {
        private int _disposeRequested;
        private CancellationTokenRegistration _registration;
        private Timer? _timer;

        public void SetRegistration(CancellationTokenRegistration registration)
        {
            if (Volatile.Read(ref _disposeRequested) == 0)
            {
                _registration = registration;
                if (Volatile.Read(ref _disposeRequested) == 0) return;
            }

            registration.Dispose();
        }

        public void SetTimer(Timer timer)
        {
            if (Volatile.Read(ref _disposeRequested) == 0)
            {
                _timer = timer;
                if (Volatile.Read(ref _disposeRequested) == 0) return;
            }

            timer.Dispose();
        }

        protected void DisposeResources()
        {
            if (Interlocked.Exchange(ref _disposeRequested, 1) != 0) return;
            _timer?.Dispose();
            _timer = null;
            _registration.Dispose();
        }
    }

    private abstract class CompletionStateBase<TSource> : RegistrationState where TSource : class
    {
        private int _completed;
        private TSource? _source;

        protected CompletionStateBase(TSource source, CancellationToken token)
        {
            _source = source;
            Token = token;
        }

        protected CancellationToken Token { get; }

        protected void Complete(Action<TSource> complete)
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;
            try
            {
                var source = _source;
                if (source != null) complete(source);
            }
            finally
            {
                _source = null;
                DisposeResources();
            }
        }
    }

    private sealed class CompletionState : CompletionStateBase<LBTaskCompletionSource>
    {
        public CompletionState(LBTaskCompletionSource source, CancellationToken token)
            : base(source, token)
        {
        }

        public void TrySetResult()
        {
            Complete(static s => s.TrySetResult());
        }

        public void TrySetException(Exception ex)
        {
            Complete(s => s.TrySetException(ex));
        }

        public void TrySetCanceled()
        {
            Complete(s => s.TrySetCanceled(Token));
        }
    }

    private sealed class CompletionState<T> : CompletionStateBase<LBTaskCompletionSource<T>>
    {
        public CompletionState(LBTaskCompletionSource<T> source, CancellationToken token)
            : base(source, token)
        {
        }

        public void TrySetResult(T value)
        {
            Complete(s => s.TrySetResult(value));
        }

        public void TrySetException(Exception ex)
        {
            Complete(s => s.TrySetException(ex));
        }

        public void TrySetCanceled()
        {
            Complete(s => s.TrySetCanceled(Token));
        }
    }
}

