namespace LayerBase.Async;

/// <summary>
///     Fire-and-forget task; exceptions should be observed via OnException.
/// </summary>
public readonly struct LBTaskVoid
{
    internal LBTaskVoid(LBTask inner, Action<Exception>? onException)
    {
        Observer.Observe(inner, onException);
    }

    public static LBTaskVoid Run(Action action, Action<Exception>? onException = null)
    {
        var t = LBTask.Run(action);
        return new LBTaskVoid(t, onException);
    }

    public static LBTaskVoid RunOnMainThread(Action             action, SynchronizationContext ctx,
                                             Action<Exception>? onException = null)
    {
        var t = LBTask.RunOnMainThread(action, ctx);
        return new LBTaskVoid(t, onException);
    }

    private sealed class Observer
    {
        private static readonly ObjectPool<Observer> Pool = new(() => new Observer());
        private readonly Action _continuation;
        private Action<Exception>? _onException;
        private LBTask _task;

        private Observer()
        {
            _continuation = Complete;
        }

        public static void Observe(LBTask task, Action<Exception>? onException)
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
}

