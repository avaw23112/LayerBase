using System;
using System.Collections.Concurrent;
using System.Threading;

namespace LayerBase.Async
{
    internal interface IArchTaskSource
    {
        bool IsCompleted { get; }
        void OnCompleted(Action continuation);
        void SetResult();
        void SetException(Exception ex);
        void SetCanceled(CancellationToken token);
        void GetResult();
    }

    internal interface IArchTaskSource<T>
    {
        bool IsCompleted { get; }
        void OnCompleted(Action continuation);
        void SetResult(T value);
        void SetException(Exception ex);
        void SetCanceled(CancellationToken token);
        T GetResult();
    }

    internal sealed class ArchTaskSource : IArchTaskSource
    {
        private static readonly ObjectPool<ArchTaskSource> Pool = new ObjectPool<ArchTaskSource>(() => new ArchTaskSource());

        private Action? _continuation;
        private Exception? _exception;
        private CancellationToken _canceledToken;
        private bool _completed;
        private SynchronizationContext? _context;
        private int _status; // 0 = pending, 1 = completed

        private ArchTaskSource()
        {
            _context = SynchronizationContext.Current;
        }

        public static ArchTaskSource Rent()
        {
            var src = Pool.Rent();
            src._continuation = null;
            src._exception = null;
            src._canceledToken = default;
            src._completed = false;
            src._context = SynchronizationContext.Current;
            src._status = 0;
            return src;
        }

        public bool IsCompleted => _completed;

        public void OnCompleted(Action continuation)
        {
            if (_completed)
            {
                Schedule(continuation);
                return;
            }

            var original = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (original != null)
            {
                _continuation = () =>
                {
                    try { original(); }
                    finally { continuation(); }
                };
            }
        }

        public void SetResult()
        {
            Complete(null, default);
        }

        public void SetException(Exception ex)
        {
            Complete(ex, default);
        }

        public void SetCanceled(CancellationToken token)
        {
            Complete(new OperationCanceledException(token), token);
        }

        public void GetResult()
        {
            if (!_completed) throw new InvalidOperationException("ArchTask not completed");
            var ex = _exception;
            Release();
            if (ex != null) throw ex;
        }

        private void Complete(Exception? ex, CancellationToken canceledToken)
        {
            if (Interlocked.CompareExchange(ref _status, 1, 0) != 0) return;
            _exception = ex;
            _canceledToken = canceledToken;
            _completed = true;

            var cont = Interlocked.Exchange(ref _continuation, null);
            if (cont != null)
            {
                Schedule(cont);
            }
        }

        private void Schedule(Action continuation)
        {
            var ctx = _context;
            if (ctx != null)
            {
                ctx.Post(_ => continuation(), null);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ => continuation());
            }
        }

        private void Release()
        {
            Pool.Return(this);
        }
    }

    internal sealed class ArchTaskSource<T> : IArchTaskSource<T>
    {
        private static readonly ObjectPool<ArchTaskSource<T>> Pool = new ObjectPool<ArchTaskSource<T>>(() => new ArchTaskSource<T>());

        private Action? _continuation;
        private Exception? _exception;
        private CancellationToken _canceledToken;
        private bool _completed;
        private T _result = default!;
        private SynchronizationContext? _context;
        private int _status; // 0 = pending, 1 = completed

        private ArchTaskSource()
        {
            _context = SynchronizationContext.Current;
        }

        public static ArchTaskSource<T> Rent()
        {
            var src = Pool.Rent();
            src._continuation = null;
            src._exception = null;
            src._canceledToken = default;
            src._completed = false;
            src._result = default!;
            src._context = SynchronizationContext.Current;
            src._status = 0;
            return src;
        }

        public bool IsCompleted => _completed;

        public void OnCompleted(Action continuation)
        {
            if (_completed)
            {
                Schedule(continuation);
                return;
            }

            var original = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (original != null)
            {
                _continuation = () =>
                {
                    try { original(); }
                    finally { continuation(); }
                };
            }
        }

        public void SetResult(T value)
        {
            _result = value;
            Complete(null, default);
        }

        public void SetException(Exception ex)
        {
            Complete(ex, default);
        }

        public void SetCanceled(CancellationToken token)
        {
            Complete(new OperationCanceledException(token), token);
        }

        public T GetResult()
        {
            if (!_completed) throw new InvalidOperationException("ArchTask not completed");
            var ex = _exception;
            var res = _result;
            Release();
            if (ex != null) throw ex;
            return res;
        }

        private void Complete(Exception? ex, CancellationToken canceledToken)
        {
            if (Interlocked.CompareExchange(ref _status, 1, 0) != 0) return;
            _exception = ex;
            _canceledToken = canceledToken;
            _completed = true;

            var cont = Interlocked.Exchange(ref _continuation, null);
            if (cont != null)
            {
                Schedule(cont);
            }
        }

        private void Schedule(Action continuation)
        {
            var ctx = _context;
            if (ctx != null)
            {
                ctx.Post(_ => continuation(), null);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ => continuation());
            }
        }

        private void Release()
        {
            Pool.Return(this);
        }
    }

    internal sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();

        public ObjectPool(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public T Rent() => _bag.TryTake(out var item) ? item : _factory();

        public void Return(T item)
        {
            _bag.Add(item);
        }
    }
}
