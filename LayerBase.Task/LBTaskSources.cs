using System.Collections.Concurrent;

namespace LayerBase.Async;

internal interface ILBTaskSource
{
    int Version { get; }
    bool IsCompleted(int version);
    void OnCompleted(int version, Action continuation);
    void SetResult();
    void SetException(Exception        ex);
    void SetCanceled(CancellationToken token);
    void GetResult(int version);
    void TryRelease(); // 新增：安全尝试回�?
}

internal interface ILBTaskSource<T>
{
    int Version { get; }
    bool IsCompleted(int version);
    void OnCompleted(int version, Action continuation);
    void SetResult(T                   value);
    void SetException(Exception        ex);
    void SetCanceled(CancellationToken token);
    T GetResult(int version);
    void TryRelease(); // 新增：安全尝试回�?
}

internal sealed class LBTaskSource : ILBTaskSource, IContextDisposeCancellable
{
    private static readonly ObjectPool<LBTaskSource> Pool = new(() => new LBTaskSource());
    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;

    private Action? _continuation;
    private LayerBaseSynchronizationContext? _registeredContext;
    private Exception? _exception;
    private int _consumed;
    private int _released; // 0 = in use, 1 = released
    private int _status;   // 0 = pending, -1 = completing, 1 = completed
    private int _version;

    private LBTaskSource()
    {
        _context = SynchronizationContext.Current;
    }

    public int Version => Volatile.Read(ref _version);

    public bool IsCompleted(int version)
    {
        if (!IsCurrent(version)) return true;
        return Volatile.Read(ref _status) == 1;
    }

    public void OnCompleted(int version, Action continuation)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateVersion(version);

        while (true)
        {
            if (IsCompleted(version))
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

            if (IsCompleted(version) && Interlocked.CompareExchange(ref _continuation, null, next) == next)
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

    public void GetResult(int version)
    {
        ValidateVersion(version);
        if (!IsCompleted(version)) throw new InvalidOperationException("ArchTask not completed");
        if (Interlocked.Exchange(ref _consumed, 1) != 0)
        {
            throw new InvalidOperationException("LBTask result has already been consumed.");
        }

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
        src._registeredContext = null;
        src._status = 0;
        src._consumed = 0;
        src._released = 0;
        src._version = NextVersion(src._version);
        if (context is LayerBaseSynchronizationContext lbContext)
        {
            if (!lbContext.TryRegisterSource())
            {
                src._status = 1;
                src._exception = new ObjectDisposedException(nameof(LayerBaseSynchronizationContext));
            }
            else
            {
                src._registeredContext = lbContext;
            }
        }

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
        UnregisterContextSource();
    }

    public void CancelOnDispose(Exception error)
    {
        SetCanceled(default);
    }

    private bool IsCurrent(int version)
    {
        return version != 0 && Version == version && Volatile.Read(ref _released) == 0;
    }

    private void ValidateVersion(int version)
    {
        if (!IsCurrent(version))
        {
            throw new InvalidOperationException("LBTask source is no longer valid for this awaiter.");
        }
    }

    private static int NextVersion(int current)
    {
        int next = unchecked(current + 1);
        return next == 0 ? 1 : next;
    }

    private void Schedule(Action continuation)
    {
        var ctx = _context;
        if (ctx != null)
        {
            try
            {
                ctx.Post(static state => ((Action)state!).Invoke(), continuation);
                return;
            }
            catch (ObjectDisposedException)
            {
                if (ctx is LayerBaseSynchronizationContext lbContext &&
                    !lbContext.AllowThreadPoolFallbackOnDispose)
                {
                    _ = lbContext.TryScheduleClosingContinuation(continuation);
                    return;
                }
            }
        }

        ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
    }

    private void UnregisterContextSource()
    {
        var context = Interlocked.Exchange(ref _registeredContext, null);
        context?.UnregisterSource();
    }
}

internal sealed class LBTaskSource<T> : ILBTaskSource<T>, IContextDisposeCancellable
{
    private static readonly ObjectPool<LBTaskSource<T>> Pool = new(() => new LBTaskSource<T>());
    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;

    private Action? _continuation;
    private LayerBaseSynchronizationContext? _registeredContext;
    private int _consumed;
    private Exception? _exception;
    private int _released; // 0 = in use, 1 = released
    private T _result = default!;
    private int _status; // 0 = pending, -1 = completing, 1 = completed
    private int _version;

    private LBTaskSource()
    {
        _context = SynchronizationContext.Current;
    }

    public int Version => Volatile.Read(ref _version);

    public bool IsCompleted(int version)
    {
        if (!IsCurrent(version)) return true;
        return Volatile.Read(ref _status) == 1;
    }

    public void OnCompleted(int version, Action continuation)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateVersion(version);

        while (true)
        {
            if (IsCompleted(version))
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

            if (IsCompleted(version) && Interlocked.CompareExchange(ref _continuation, null, next) == next)
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

    public T GetResult(int version)
    {
        ValidateVersion(version);
        if (!IsCompleted(version)) throw new InvalidOperationException("ArchTask not completed");
        if (Interlocked.Exchange(ref _consumed, 1) != 0)
        {
            throw new InvalidOperationException("LBTask result has already been consumed.");
        }

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
        src._registeredContext = null;
        src._status = 0;
        src._consumed = 0;
        src._released = 0;
        src._version = NextVersion(src._version);
        if (context is LayerBaseSynchronizationContext lbContext)
        {
            if (!lbContext.TryRegisterSource())
            {
                src._status = 1;
                src._exception = new ObjectDisposedException(nameof(LayerBaseSynchronizationContext));
            }
            else
            {
                src._registeredContext = lbContext;
            }
        }

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
        UnregisterContextSource();
    }

    private void Schedule(Action continuation)
    {
        var ctx = _context;
        if (ctx != null)
        {
            try
            {
                ctx.Post(static state => ((Action)state!).Invoke(), continuation);
                return;
            }
            catch (ObjectDisposedException)
            {
                if (ctx is LayerBaseSynchronizationContext lbContext &&
                    !lbContext.AllowThreadPoolFallbackOnDispose)
                {
                    _ = lbContext.TryScheduleClosingContinuation(continuation);
                    return;
                }
            }
        }

        ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
    }

    public void CancelOnDispose(Exception error)
    {
        SetCanceled(default);
    }

    private bool IsCurrent(int version)
    {
        return version != 0 && Version == version && Volatile.Read(ref _released) == 0;
    }

    private void ValidateVersion(int version)
    {
        if (!IsCurrent(version))
        {
            throw new InvalidOperationException("LBTask source is no longer valid for this awaiter.");
        }
    }

    private static int NextVersion(int current)
    {
        int next = unchecked(current + 1);
        return next == 0 ? 1 : next;
    }

    private void Release()
    {
        Pool.Return(this);
    }

    private void UnregisterContextSource()
    {
        var context = Interlocked.Exchange(ref _registeredContext, null);
        context?.UnregisterSource();
    }
}

internal sealed class ObjectPool<T> where T : class
{
    private const int DefaultMaxRetained = 1024;
    private readonly ConcurrentBag<T> _bag = new();
    private readonly Func<T> _factory;
    private readonly int _maxRetained;
    private int _count;

    public ObjectPool(Func<T> factory, int maxRetained = DefaultMaxRetained)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _maxRetained = Math.Max(0, maxRetained);
    }

    internal int Count => Volatile.Read(ref _count);

    internal int MaxRetained => _maxRetained;

    public T Rent()
    {
        if (_bag.TryTake(out var item))
        {
            Interlocked.Decrement(ref _count);
            return item;
        }

        return _factory();
    }

    public void Return(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        while (true)
        {
            int current = Volatile.Read(ref _count);
            if (current >= _maxRetained)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _count, current + 1, current) == current)
            {
                break;
            }
        }

        _bag.Add(item);
    }
}
