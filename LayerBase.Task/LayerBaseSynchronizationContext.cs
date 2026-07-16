using System.Collections.Concurrent;
using System.Diagnostics;

namespace LayerBase.Async;

public sealed class LayerBaseSynchronizationContext : SynchronizationContext, IArchMainThreadPump, IDisposable
{
    private readonly object _lock = new();
    private readonly int _mainThreadId;
    private readonly HashSet<IContextDisposeCancellable> _pendingSources = new();
    private readonly ConcurrentQueue<WorkItem> _queue = new();
    private readonly FrameDelayWheel<WorkItem> _frameDelayWheel;
    internal MainThreadCompletionQueue CompletionQueue { get; } = new();
    private int _hasQueuedWork;
    private int _hasFrameWork;
    private int _allowClosingCancellationPosts;
    private bool _closing;
    private bool _disposed;

    private LayerBaseSynchronizationContext(int mainThreadId)
    {
        _mainThreadId = mainThreadId;
        _frameDelayWheel = new FrameDelayWheel<WorkItem>(EnqueueReadyFrameWork);
    }

    public bool HasPendingWork =>
        CompletionQueue.HasPending ||
        Volatile.Read(ref _hasQueuedWork) != 0 ||
        Volatile.Read(ref _hasFrameWork) != 0;

    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count + _frameDelayWheel.Count + CompletionQueue.Count;
            }
        }
    }

    public void Update(
        int                       maxItems        = 0,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>?        reportException = null)
    {
        if (_disposed || !HasPendingWork)
            return;

        CompletionQueue.Drain(maxItems, exceptionPolicy, reportException);

        if (Interlocked.Exchange(ref _hasFrameWork, 0) != 0)
        {
            lock (_lock)
            {
                _frameDelayWheel.Advance();

                if (_frameDelayWheel.Count != 0)
                    Volatile.Write(ref _hasFrameWork, 1);
            }
        }

        Interlocked.Exchange(ref _hasQueuedWork, 0);

        var processed = 0;
        try
        {
            while (_queue.TryDequeue(out var work))
            {
                try
                {
                    work.Invoke();
                }
                catch
                {
                    throw;
                }

                processed++;
                if (maxItems > 0 && processed >= maxItems)
                    break;
            }
        }
        finally
        {
            if (!_queue.IsEmpty)
                Volatile.Write(ref _hasQueuedWork, 1);
        }
    }

    public Scope EnterScope()
    {
        return new Scope(this);
    }

    public readonly struct Scope : IDisposable
    {
        private readonly SynchronizationContext? _previous;

        public Scope(LayerBaseSynchronizationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _previous = Current;
            SetSynchronizationContext(context);
        }

        public void Dispose()
        {
            SetSynchronizationContext(_previous);
        }
    }

    public void Dispose()
    {
        BeginClose(new OperationCanceledException("The LayerBase synchronization context has been disposed."));
        _disposed = true;
    }

    internal bool TryRegisterSource(IContextDisposeCancellable source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        lock (_lock)
        {
            if (_closing || _disposed)
                return false;

            _pendingSources.Add(source);
            return true;
        }
    }

    internal void UnregisterSource(IContextDisposeCancellable source)
    {
        if (source == null) return;

        lock (_lock)
            _pendingSources.Remove(source);
    }

    public void BeginClose(Exception reason)
    {
        if (reason == null) throw new ArgumentNullException(nameof(reason));

        IContextDisposeCancellable[] pendingSources;
        lock (_lock)
        {
            if (_closing)
                return;

            _closing = true;
            _frameDelayWheel.Clear();
            Volatile.Write(ref _hasFrameWork, 0);
            pendingSources = _pendingSources.ToArray();
            _pendingSources.Clear();
        }

        while (_queue.TryDequeue(out _))
        {
        }

        Volatile.Write(ref _hasQueuedWork, 0);

        Volatile.Write(ref _allowClosingCancellationPosts, 1);
        try
        {
            foreach (var source in pendingSources)
                source.CancelFromContext(reason);
        }
        finally
        {
            Volatile.Write(ref _allowClosingCancellationPosts, 0);
        }
    }

    public void DrainClosingOperations(
        int                       maxItems        = 0,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>?        reportException = null)
    {
        if (!_closing || _disposed) return;

        Update(maxItems, exceptionPolicy, reportException);
    }

    public static LayerBaseSynchronizationContext Install()
    {
        return new LayerBaseSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        if (_disposed) return;
        if (_closing && Volatile.Read(ref _allowClosingCancellationPosts) == 0) return;
        _queue.Enqueue(new WorkItem(d, state));
        Volatile.Write(ref _hasQueuedWork, 1);
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (_disposed) return;
        if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            d(state);
            return;
        }

        throw new NotSupportedException(
            "Synchronous Send to another LayerBaseSynchronizationContext thread is not supported.");
    }

    internal void ScheduleInFrames(Action action, int frames)
    {
        ScheduleInFrames(static state => ((Action)state!).Invoke(), action, frames);
    }

    internal void ScheduleInFrames(SendOrPostCallback callback, object? state, int frames)
    {
        if (_disposed || _closing) return;
        var workItem = new WorkItem(callback, state);
        if (frames <= 0)
        {
            _queue.Enqueue(workItem);
            Volatile.Write(ref _hasQueuedWork, 1);
            return;
        }

        lock (_lock)
        {
            _frameDelayWheel.Schedule(workItem, frames);
            Volatile.Write(ref _hasFrameWork, 1);
        }
    }

    private void EnqueueReadyFrameWork(WorkItem work)
    {
        _queue.Enqueue(work);
        Volatile.Write(ref _hasQueuedWork, 1);
    }

    private readonly struct WorkItem
    {
        private readonly SendOrPostCallback _callback;
        private readonly object? _state;

        public WorkItem(SendOrPostCallback callback, object? state)
        {
            _callback = callback;
            _state = state;
        }

        public void Invoke()
        {
            _callback(_state);
        }
    }
}

public interface IArchMainThreadPump
{
    void Update(
        int                       maxItems        = 0,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>?        reportException = null);
}
