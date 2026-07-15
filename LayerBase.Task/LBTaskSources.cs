using System.Collections.Concurrent;

namespace LayerBase.Async;

internal interface ILBTaskSource
{
    int Version { get; }
    bool IsCompleted(int token);
    void OnCompleted(Action continuation, int token);
    void SetResult();
    void SetException(Exception        ex);
    void SetCanceled(CancellationToken token);
    void GetResult(int token);
    void TryRelease();
}

internal interface ILBTaskSource<T>
{
    int Version { get; }
    bool IsCompleted(int token);
    void OnCompleted(Action            continuation, int token);
    void SetResult(T                   value);
    void SetException(Exception        ex);
    void SetCanceled(CancellationToken token);
    T GetResult(int token);
    void TryRelease();
}

internal interface IContextDisposeCancellable
{
    void CancelFromContext(Exception reason);
}

internal sealed class LBTaskSource : ILBTaskSource, IContextDisposeCancellable
{
    private static readonly ObjectPool<LBTaskSource> Pool = new(() => new LBTaskSource());
    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;
    private LayerBaseSynchronizationContext? _registeredContext;

    private Action? _continuation;
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

    public bool IsCompleted(int token)
    {
        return token == Version &&
               Volatile.Read(ref _released) == 0 &&
               Volatile.Read(ref _status) == 1;
    }

    public void OnCompleted(Action continuation, int token)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateToken(token);

        while (true)
        {
            if (IsCompleted(token))
            {
                Schedule(continuation);
                return;
            }

            var original = Volatile.Read(ref _continuation);
            if (original != null)
                throw new InvalidOperationException("LBTask only supports one awaiter continuation.");

            if (Interlocked.CompareExchange(ref _continuation, continuation, null) != null) continue;

            if (IsCompleted(token) && Interlocked.CompareExchange(ref _continuation, null, continuation) == continuation)
                Schedule(continuation);
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

    public void GetResult(int token)
    {
        ValidateToken(token);
        if (!IsCompleted(token)) throw new InvalidOperationException("LBTask is not completed.");
        if (Interlocked.Exchange(ref _consumed, 1) != 0)
            throw new InvalidOperationException("LBTask result has already been consumed.");

        var ex = _exception;
        TryRelease();
        if (ex != null) throw ex;
    }

    public void TryRelease()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
            return;

        _registeredContext?.UnregisterSource(this);
        _registeredContext = null;
        _context = null;
        _continuation = null;
        _exception = null;
        _canceledToken = default;
        Pool.Return(this);
    }

    public static LBTaskSource Rent(SynchronizationContext? context)
    {
        var src = Pool.Rent();
        unchecked
        {
            src._version++;
            if (src._version == 0)
                src._version = 1;
        }

        src._continuation = null;
        src._exception = null;
        src._canceledToken = default;
        src._context = context;
        src._registeredContext = null;
        src._consumed = 0;
        src._status = 0;
        src._released = 0;
        if (context is LayerBaseSynchronizationContext layerBaseContext)
        {
            if (layerBaseContext.TryRegisterSource(src))
                src._registeredContext = layerBaseContext;
            else
                src.SetCanceled(default);
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
    }

    public void CancelFromContext(Exception reason)
    {
        _registeredContext = null;
        Complete(reason, default);
    }

    private void Schedule(Action continuation)
    {
        var ctx = _context;
        if (ctx != null)
            ctx.Post(static state => ((Action)state!).Invoke(), continuation);
        else
            ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
    }

    private void ValidateToken(int token)
    {
        if (token != Version || Volatile.Read(ref _released) != 0)
            throw new InvalidOperationException("LBTask source version is no longer valid.");
    }
}

internal sealed class LBTaskSource<T> : ILBTaskSource<T>, IContextDisposeCancellable
{
    private static readonly ObjectPool<LBTaskSource<T>> Pool = new(() => new LBTaskSource<T>());
    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;
    private LayerBaseSynchronizationContext? _registeredContext;

    private Action? _continuation;
    private Exception? _exception;
    private int _consumed;
    private int _released; // 0 = in use, 1 = released
    private T _result = default!;
    private int _status; // 0 = pending, -1 = completing, 1 = completed
    private int _version;

    private LBTaskSource()
    {
        _context = SynchronizationContext.Current;
    }

    public int Version => Volatile.Read(ref _version);

    public bool IsCompleted(int token)
    {
        return token == Version &&
               Volatile.Read(ref _released) == 0 &&
               Volatile.Read(ref _status) == 1;
    }

    public void OnCompleted(Action continuation, int token)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateToken(token);

        while (true)
        {
            if (IsCompleted(token))
            {
                Schedule(continuation);
                return;
            }

            var original = Volatile.Read(ref _continuation);
            if (original != null)
                throw new InvalidOperationException("LBTask only supports one awaiter continuation.");

            if (Interlocked.CompareExchange(ref _continuation, continuation, null) != null) continue;

            if (IsCompleted(token) && Interlocked.CompareExchange(ref _continuation, null, continuation) == continuation)
                Schedule(continuation);
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

    public T GetResult(int token)
    {
        ValidateToken(token);
        if (!IsCompleted(token)) throw new InvalidOperationException("LBTask is not completed.");
        if (Interlocked.Exchange(ref _consumed, 1) != 0)
            throw new InvalidOperationException("LBTask result has already been consumed.");

        var ex = _exception;
        var res = _result;
        TryRelease();
        if (ex != null) throw ex;
        return res;
    }

    public void TryRelease()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
            return;

        _registeredContext?.UnregisterSource(this);
        _registeredContext = null;
        _context = null;
        _continuation = null;
        _exception = null;
        _canceledToken = default;
        _result = default!;
        Pool.Return(this);
    }

    public static LBTaskSource<T> Rent(SynchronizationContext? context)
    {
        var src = Pool.Rent();
        unchecked
        {
            src._version++;
            if (src._version == 0)
                src._version = 1;
        }

        src._continuation = null;
        src._exception = null;
        src._canceledToken = default;
        src._result = default!;
        src._context = context;
        src._registeredContext = null;
        src._consumed = 0;
        src._status = 0;
        src._released = 0;
        if (context is LayerBaseSynchronizationContext layerBaseContext)
        {
            if (layerBaseContext.TryRegisterSource(src))
                src._registeredContext = layerBaseContext;
            else
                src.SetCanceled(default);
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
    }

    public void CancelFromContext(Exception reason)
    {
        _registeredContext = null;
        Complete(reason, default);
    }

    private void Schedule(Action continuation)
    {
        var ctx = _context;
        if (ctx != null)
            ctx.Post(static state => ((Action)state!).Invoke(), continuation);
        else
            ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
    }

    private void ValidateToken(int token)
    {
        if (token != Version || Volatile.Read(ref _released) != 0)
            throw new InvalidOperationException("LBTask source version is no longer valid.");
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
