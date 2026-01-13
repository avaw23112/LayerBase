using System.Runtime.CompilerServices;

namespace LayerBase.Async
{
    /// <summary>Lightweight awaitable task (no result).</summary>
    [AsyncMethodBuilder(typeof(LBTaskMethodBuilder))]
    public readonly struct LBTask
    {
        internal readonly IArchTaskSource? Source;

        internal LBTask(IArchTaskSource? source)
        {
            Source = source;
        }

        public Awaiter GetAwaiter() => new Awaiter(Source);

        public static LBTask CompletedTask => new LBTask(null);

        public static LBTask FromResult() => CompletedTask;

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

        public static LBTask Run(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var src = ArchTaskSource.Rent();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { action(); src.SetResult(); }
                catch (Exception ex) { src.SetException(ex); }
            });
            return new LBTask(src);
        }

        public static LBTask RunOnMainThread(Action action, SynchronizationContext ctx)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var src = ArchTaskSource.Rent();
            ctx.Post(_ =>
            {
                try { action(); src.SetResult(); }
                catch (Exception ex) { src.SetException(ex); }
            }, null);

            return new LBTask(src);
        }

        public static LBTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            var src = ArchTaskSource.Rent();
            Timer? timer = null;
            timer = new Timer(_ =>
            {
                timer?.Dispose();
                src.SetResult();
            }, null, delay, Timeout.InfiniteTimeSpan);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    timer?.Dispose();
                    src.SetCanceled(cancellationToken);
                });
            }

            return new LBTask(src);
        }

        /// <summary>Yield back to the current synchronization context or thread pool once.</summary>
        public static LBTask Yield(SynchronizationContext? ctx = null, CancellationToken cancellationToken = default)
        {
            ctx ??= SynchronizationContext.Current;
            var src = ArchTaskSource.Rent();

            if (cancellationToken.IsCancellationRequested)
            {
                src.SetCanceled(cancellationToken);
                return new LBTask(src);
            }

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => src.SetCanceled(cancellationToken));
            }

            if (ctx != null)
            {
                ctx.Post(_ => src.SetResult(), null);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ => src.SetResult());
            }

            return new LBTask(src);
        }

        public static LBTask DelayFrame(int frames, SynchronizationContext? ctx = null, CancellationToken cancellationToken = default)
        {
            if (frames <= 0) return CompletedTask;

            ctx ??= SynchronizationContext.Current;
            var src = ArchTaskSource.Rent();
            var canceled = 0;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    Interlocked.Exchange(ref canceled, 1);
                    src.SetCanceled(cancellationToken);
                });
            }

            if (ctx is LayerBaseSynchronizationContext archCtx)
            {
                archCtx.ScheduleInFrames(() =>
                {
                    if (Interlocked.CompareExchange(ref canceled, 0, 0) == 0)
                        src.SetResult();
                }, frames);
            }
            else
            {
                // Fallback: approximate with timer
                return Delay(TimeSpan.FromMilliseconds(Math.Max(frames, 1)), cancellationToken);
            }

            return new LBTask(src);
        }

        public static LBTask NextFrame(SynchronizationContext? ctx = null, CancellationToken cancellationToken = default)
            => DelayFrame(1, ctx, cancellationToken);

        public readonly struct Awaiter : System.Runtime.CompilerServices.INotifyCompletion
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

        internal LBTask(IArchTaskSource<T>? source)
        {
            Source = source;
        }

        public Awaiter GetAwaiter() => new Awaiter(Source);

        public static LBTask<T> FromResult(T value)
        {
            var src = ArchTaskSource<T>.Rent();
            src.SetResult(value);
            return new LBTask<T>(src);
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
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { src.SetResult(func()); }
                catch (Exception ex) { src.SetException(ex); }
            });
            return new LBTask<T>(src);
        }

        public static LBTask<T> RunOnMainThread(Func<T> func, SynchronizationContext ctx)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var src = ArchTaskSource<T>.Rent();
            ctx.Post(_ =>
            {
                try { src.SetResult(func()); }
                catch (Exception ex) { src.SetException(ex); }
            }, null);
            return new LBTask<T>(src);
        }

        public readonly struct Awaiter : System.Runtime.CompilerServices.INotifyCompletion
        {
            private readonly IArchTaskSource<T>? _source;

            internal Awaiter(IArchTaskSource<T>? source)
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

            public T GetResult()
            {
                return _source == null ? default! : _source.GetResult();
            }
        }
    }
}
