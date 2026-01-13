using System;
using System.Threading;

namespace LayerBase.Async
{
    /// <summary>
    /// Fire-and-forget task; exceptions should be observed via OnException.
    /// </summary>
    public readonly struct LBTaskVoid
    {
        private readonly LBTask _inner;
        private readonly Action<Exception>? _onException;

        internal LBTaskVoid(LBTask inner, Action<Exception>? onException)
        {
            _inner = inner;
            _onException = onException;
            Observe();
        }

        private void Observe()
        {
            var inner = _inner;
            var handler = _onException;
            inner.GetAwaiter().OnCompleted(() =>
            {
                try { inner.GetAwaiter().GetResult(); }
                catch (Exception ex) { handler?.Invoke(ex); }
            });
        }

        public static LBTaskVoid Run(Action action, Action<Exception>? onException = null)
        {
            var t = LBTask.Run(action);
            return new LBTaskVoid(t, onException);
        }

        public static LBTaskVoid RunOnMainThread(Action action, SynchronizationContext ctx, Action<Exception>? onException = null)
        {
            var t = LBTask.RunOnMainThread(action, ctx);
            return new LBTaskVoid(t, onException);
        }
    }
}
