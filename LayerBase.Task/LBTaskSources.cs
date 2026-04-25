using System.Collections.Concurrent;

namespace LayerBase.Async;

internal interface IArchTaskSource
{
    bool IsCompleted { get; }
    void OnCompleted(Action continuation);
    void SetResult();
    void SetException(Exception        ex);
    void SetCanceled(CancellationToken token);
    void GetResult();
    void TryRelease(); // 新增：安全尝试回�?
}

internal interface IArchTaskSource<T>
{
    bool IsCompleted { get; }
    void OnCompleted(Action            continuation);
    void SetResult(T                   value);
    void SetException(Exception        ex);
    void SetCanceled(CancellationToken token);
    T GetResult();
    void TryRelease(); // 新增：安全尝试回�?
}

internal sealed class ArchTaskSource : IArchTaskSource
{
    private static readonly ObjectPool<ArchTaskSource> Pool = new(() => new ArchTaskSource());
    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;

    private Action? _continuation;
    private Exception? _exception;
    private int _released; // 0 = in use, 1 = released
    private int _status;   // 0 = pending, 1 = completed

    private ArchTaskSource()
    {
        _context = SynchronizationContext.Current;
    }

    public bool IsCompleted { get; private set; }

    public void OnCompleted(Action continuation)
    {
        if (IsCompleted)
        {
            Schedule(continuation);
            return;
        }

        var original = Interlocked.CompareExchange(ref _continuation, continuation, null);
        if (original != null)
            _continuation = () =>
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

    public static ArchTaskSource Rent()
    {
        var src = Pool.Rent();
        src._continuation = null;
        src._exception = null;
        src._canceledToken = default;
        src.IsCompleted = false;
        src._context = SynchronizationContext.Current;
        src._status = 0;
        src._released = 0;
        return src;
    }

    private void Complete(Exception? ex, CancellationToken canceledToken)
    {
        if (Interlocked.CompareExchange(ref _status, 1, 0) != 0) return;
        _exception = ex;
        _canceledToken = canceledToken;
        IsCompleted = true;

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

internal sealed class ArchTaskSource<T> : IArchTaskSource<T>
{
    private static readonly ObjectPool<ArchTaskSource<T>> Pool = new(() => new ArchTaskSource<T>());
    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;

    private Action? _continuation;
    private Exception? _exception;
    private int _released; // 0 = in use, 1 = released
    private T _result = default!;
    private int _status; // 0 = pending, 1 = completed

    private ArchTaskSource()
    {
        _context = SynchronizationContext.Current;
    }

    public bool IsCompleted { get; private set; }

    public void OnCompleted(Action continuation)
    {
        if (IsCompleted)
        {
            Schedule(continuation);
            return;
        }

        var original = Interlocked.CompareExchange(ref _continuation, continuation, null);
        if (original != null)
            _continuation = () =>
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

    public static ArchTaskSource<T> Rent()
    {
        var src = Pool.Rent();
        src._continuation = null;
        src._exception = null;
        src._canceledToken = default;
        src.IsCompleted = false;
        src._result = default!;
        src._context = SynchronizationContext.Current;
        src._status = 0;
        src._released = 0;
        return src;
    }

    private void Complete(Exception? ex, CancellationToken canceledToken)
    {
        if (Interlocked.CompareExchange(ref _status, 1, 0) != 0) return;
        _exception = ex;
        _canceledToken = canceledToken;
        IsCompleted = true;

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

