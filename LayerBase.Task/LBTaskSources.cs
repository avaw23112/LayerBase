using System.Collections.Concurrent;

namespace LayerBase.Async;

internal interface ILBTaskSource
{
    bool IsCompleted { get; }
    void OnCompleted(Action continuation);
    void SetResult();
    void SetException(Exception        ex);
    void SetCanceled(CancellationToken token);
    void GetResult();
    void TryRelease(); // 新增：安全尝试回�?
}

internal interface ILBTaskSource<T>
{
    bool IsCompleted { get; }
    void OnCompleted(Action            continuation);
    void SetResult(T                   value);
    void SetException(Exception        ex);
    void SetCanceled(CancellationToken token);
    T GetResult();
    void TryRelease(); // 新增：安全尝试回�?
}

internal sealed class LBTaskSource : ILBTaskSource
{
    private static readonly ObjectPool<LBTaskSource> Pool = new(() => new LBTaskSource());
    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;

    private Action? _continuation;
    private Exception? _exception;
    private int _released; // 0 = in use, 1 = released
    private int _status;   // 0 = pending, -1 = completing, 1 = completed

    private LBTaskSource()
    {
        _context = SynchronizationContext.Current;
    }

    public bool IsCompleted => Volatile.Read(ref _status) == 1;

    public void OnCompleted(Action continuation)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));

        while (true)
        {
            if (IsCompleted)
            {
                Schedule(continuation);
                return;
            }

            var original = Volatile.Read(ref _continuation);
            var next = original == null
                ? continuation
                : () =>
                {
                    try
                    {
                        original();
                    }
                    finally
                    {
                        continuation();
                    }
                };

            if (Interlocked.CompareExchange(ref _continuation, next, original) != original) continue;

            if (IsCompleted && Interlocked.CompareExchange(ref _continuation, null, next) == next)
                Schedule(next);
            return;
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
        if (!IsCompleted) throw new InvalidOperationException("ArchTask not completed");
        var ex = _exception;
        TryRelease();
        if (ex != null) throw ex;
    }

    public void TryRelease()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0) Pool.Return(this);
    }

    public static LBTaskSource Rent(SynchronizationContext? context)
    {
        var src = Pool.Rent();
        src._continuation = null;
        src._exception = null;
        src._canceledToken = default;
        src._context = context;
        src._status = 0;
        src._released = 0;
        return src;
    }

    public static LBTaskSource Rent()
    {
        return Rent(SynchronizationContext.Current);
    }

    private void Complete(Exception? ex, CancellationToken canceledToken)
    {
        if (Interlocked.CompareExchange(ref _status, -1, 0) != 0) return;
        _exception = ex;
        _canceledToken = canceledToken;
        Volatile.Write(ref _status, 1);

        var cont = Interlocked.Exchange(ref _continuation, null);
        if (cont != null) Schedule(cont);
    }

    private void Schedule(Action continuation)
    {
        var ctx = _context;
        if (ctx != null)
            ctx.Post(static state => ((Action)state!).Invoke(), continuation);
        else
            ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
    }
}

internal sealed class LBTaskSource<T> : ILBTaskSource<T>
{
    private static readonly ObjectPool<LBTaskSource<T>> Pool = new(() => new LBTaskSource<T>());
    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;

    private Action? _continuation;
    private Exception? _exception;
    private int _released; // 0 = in use, 1 = released
    private T _result = default!;
    private int _status; // 0 = pending, -1 = completing, 1 = completed

    private LBTaskSource()
    {
        _context = SynchronizationContext.Current;
    }

    public bool IsCompleted => Volatile.Read(ref _status) == 1;

    public void OnCompleted(Action continuation)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));

        while (true)
        {
            if (IsCompleted)
            {
                Schedule(continuation);
                return;
            }

            var original = Volatile.Read(ref _continuation);
            var next = original == null
                ? continuation
                : () =>
                {
                    try
                    {
                        original();
                    }
                    finally
                    {
                        continuation();
                    }
                };

            if (Interlocked.CompareExchange(ref _continuation, next, original) != original) continue;

            if (IsCompleted && Interlocked.CompareExchange(ref _continuation, null, next) == next)
                Schedule(next);
            return;
        }
    }

    public void SetResult(T value)
    {
        if (Interlocked.CompareExchange(ref _status, -1, 0) != 0) return;
        _result = value;
        CompleteCore(null, default);
    }

    public void SetException(Exception ex)
    {
        if (Interlocked.CompareExchange(ref _status, -1, 0) != 0) return;
        CompleteCore(ex, default);
    }

    public void SetCanceled(CancellationToken token)
    {
        if (Interlocked.CompareExchange(ref _status, -1, 0) != 0) return;
        CompleteCore(new OperationCanceledException(token), token);
    }

    public T GetResult()
    {
        if (!IsCompleted) throw new InvalidOperationException("ArchTask not completed");
        var ex = _exception;
        var res = _result;
        TryRelease();
        if (ex != null) throw ex;
        return res;
    }

    public void TryRelease()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0) Pool.Return(this);
    }

    public static LBTaskSource<T> Rent(SynchronizationContext? context)
    {
        var src = Pool.Rent();
        src._continuation = null;
        src._exception = null;
        src._canceledToken = default;
        src._result = default!;
        src._context = context;
        src._status = 0;
        src._released = 0;
        return src;
    }

    public static LBTaskSource<T> Rent()
    {
        return Rent(SynchronizationContext.Current);
    }

    private void Complete(Exception? ex, CancellationToken canceledToken)
    {
        if (Interlocked.CompareExchange(ref _status, -1, 0) != 0) return;
        CompleteCore(ex, canceledToken);
    }

    private void CompleteCore(Exception? ex, CancellationToken canceledToken)
    {
        _exception = ex;
        _canceledToken = canceledToken;
        Volatile.Write(ref _status, 1);

        var cont = Interlocked.Exchange(ref _continuation, null);
        if (cont != null) Schedule(cont);
    }

    private void Schedule(Action continuation)
    {
        var ctx = _context;
        if (ctx != null)
            ctx.Post(static state => ((Action)state!).Invoke(), continuation);
        else
            ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
    }

    private void Release()
    {
        Pool.Return(this);
    }
}

internal sealed class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _bag = new();
    private readonly Func<T> _factory;

    public ObjectPool(Func<T> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public T Rent()
    {
        return _bag.TryTake(out var item) ? item : _factory();
    }

    public void Return(T item)
    {
        _bag.Add(item);
    }
}