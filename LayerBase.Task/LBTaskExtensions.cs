using System;
using System.Collections.Generic;
using System.Threading;

namespace LayerBase.Async
{
    public static class LBTaskExtensions
    {
        public static LBTask WithTimeout(this LBTask task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return LBTask.FromCanceled(cancellationToken);

            var tcs = new LBTaskCompletionSource();
            Timer? timer = null;
            CancellationTokenRegistration registration = default;

            timer = new Timer(_ =>
            {
                tcs.TrySetException(new TimeoutException());
            }, null, timeout, Timeout.InfiniteTimeSpan);

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled(cancellationToken);
                    timer?.Dispose();
                });
            }

            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    task.GetAwaiter().GetResult();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    timer.Dispose();
                    registration.Dispose();
                }
            });

            return tcs.Task;
        }

        public static LBTask<T> WithTimeout<T>(this LBTask<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return LBTask<T>.FromCanceled(cancellationToken);

            var tcs = new LBTaskCompletionSource<T>();
            Timer? timer = null;
            CancellationTokenRegistration registration = default;

            timer = new Timer(_ =>
            {
                tcs.TrySetException(new TimeoutException());
            }, null, timeout, Timeout.InfiniteTimeSpan);

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled(cancellationToken);
                    timer?.Dispose();
                });
            }

            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    var res = task.GetAwaiter().GetResult();
                    tcs.TrySetResult(res);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    timer.Dispose();
                    registration.Dispose();
                }
            });

            return tcs.Task;
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
            {
                task.GetAwaiter().OnCompleted(() =>
                {
                    try { task.GetAwaiter().GetResult(); }
                    catch (Exception ex) { tcs.TrySetException(ex); return; }

                    if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        tcs.TrySetResult();
                    }
                });
            }

            return tcs.Task;
        }

        public static LBTask<T[]> WhenAll<T>(params LBTask<T>[] tasks)
        {
            if (tasks == null || tasks.Length == 0) return LBTask<T[]>.FromResult(Array.Empty<T>());

            var remaining = tasks.Length;
            var results = new T[tasks.Length];
            var tcs = new LBTaskCompletionSource<T[]>();

            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                var task = tasks[i];
                task.GetAwaiter().OnCompleted(() =>
                {
                    try { results[index] = task.GetAwaiter().GetResult(); }
                    catch (Exception ex) { tcs.TrySetException(ex); return; }

                    if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        tcs.TrySetResult(results);
                    }
                });
            }

            return tcs.Task;
        }

        public static LBTask<int> WhenAny(params LBTask[] tasks)
        {
            if (tasks == null || tasks.Length == 0) return LBTask<int>.FromException(new InvalidOperationException("No tasks"));

            var tcs = new LBTaskCompletionSource<int>();
            var won = 0;
            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i].GetAwaiter().OnCompleted(() =>
                {
                    if (Interlocked.CompareExchange(ref won, 1, 0) == 0)
                    {
                        try { tasks[index].GetAwaiter().GetResult(); }
                        catch (Exception ex) { tcs.TrySetException(ex); return; }
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
            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    onException?.Invoke(ex);
                }
            });
        }

        public static LBTask WithCancellation(this LBTask task, CancellationToken token) => task.AttachExternalCancellation(token);

        public static LBTask<T> WithCancellation<T>(this LBTask<T> task, CancellationToken token) => task.AttachExternalCancellation(token);

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
            if (token.IsCancellationRequested)
            {
                tcs.TrySetCanceled(token);
                return tcs.Task;
            }

            token.Register(() => tcs.TrySetCanceled(token));
            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    var res = task.GetAwaiter().GetResult();
                    tcs.TrySetResult(res);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        public static LBTask AttachExternalCancellation(this LBTask task, CancellationToken token)
        {
            if (!token.CanBeCanceled) return task;

            var tcs = new LBTaskCompletionSource();
            if (token.IsCancellationRequested)
            {
                tcs.TrySetCanceled(token);
                return tcs.Task;
            }

            token.Register(() => tcs.TrySetCanceled(token));
            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    task.GetAwaiter().GetResult();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        public static LBTask WaitUntil(Func<bool> predicate, SynchronizationContext? ctx = null, CancellationToken cancellationToken = default)
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
                {
                    tcs.TrySetResult();
                }
                else
                {
                    LBTask.NextFrame(ctx, cancellationToken).GetAwaiter().OnCompleted(Tick);
                }
            }

            Tick();
            return tcs.Task;
        }

        public static LBTask WaitWhile(Func<bool> predicate, SynchronizationContext? ctx = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return WaitUntil(() => !predicate(), ctx, cancellationToken);
        }
    }
}
