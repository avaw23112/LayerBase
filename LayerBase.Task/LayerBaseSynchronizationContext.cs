using System.Collections.Concurrent;
using System.Diagnostics;

namespace LayerBase.Async;

public sealed class LayerBaseSynchronizationContext : SynchronizationContext, IArchMainThreadPump, IDisposable
{
    private readonly List<FrameWorkItem> _frameWork = new();
    private readonly object _lock = new();
    private readonly int _mainThreadId;
    private readonly ConcurrentQueue<WorkItem> _queue = new();
    private readonly Queue<WorkItem> _closingQueue = new();
    private int _pendingSourceCount;
    internal MainThreadCompletionQueue CompletionQueue { get; } = new();
    private bool _disposed;
    private bool _finalized;

    private LayerBaseSynchronizationContext(int mainThreadId, bool allowThreadPoolFallbackOnDispose)
    {
        _mainThreadId = mainThreadId;
        AllowThreadPoolFallbackOnDispose = allowThreadPoolFallbackOnDispose;
    }

    internal bool AllowThreadPoolFallbackOnDispose { get; }

    internal int PendingOperationCount
    {
        get
        {
            lock (_lock)
            {
                return _frameWork.Count + _queue.Count + _closingQueue.Count;
            }
        }
    }

    internal int PendingSourceCount => Volatile.Read(ref _pendingSourceCount);

    internal bool IsFinalized
    {
        get
        {
            lock (_lock) return _finalized;
        }
    }

    public void Update(
        int                       maxItems        = 0,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>?        reportException = null)
    {
        if (_disposed) return;

        CompletionQueue.Drain(maxItems, exceptionPolicy, reportException);

        lock (_lock)
        {
            for (var i = _frameWork.Count - 1; i >= 0; i--)
            {
                var item = _frameWork[i].Tick();
                if (item.ShouldRun)
                {
                    _queue.Enqueue(item.Work);
                    _frameWork.RemoveAt(i);
                }
                else
                {
                    _frameWork[i] = item;
                }
            }
        }

        var processed = 0;
        while (_queue.TryDequeue(out var work))
        {
            work.Invoke();
            processed++;
            if (maxItems > 0 && processed >= maxItems)
                break;
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
        var disposed = new ObjectDisposedException(nameof(LayerBaseSynchronizationContext));
        if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
        {
            BeginClose(disposed);
            throw new InvalidOperationException(
                "LayerBaseSynchronizationContext can only be disposed by its owner thread.");
        }

        BeginClose(disposed);
        DrainClosingOperations();
        if (PendingSourceCount == 0)
        {
            FinalizeClose();
        }
    }

    public void DisposeFromRuntime(
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        var disposed = new ObjectDisposedException(nameof(LayerBaseSynchronizationContext));
        BeginClose(disposed);
        if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            DrainClosingOperations(exceptionPolicy, reportException);
        }

        if (PendingSourceCount == 0 && PendingOperationCount == 0)
        {
            FinalizeClose();
        }
    }

    public void BeginClose(Exception reason)
    {
        var disposed = new ObjectDisposedException(nameof(LayerBaseSynchronizationContext));
        List<FrameWorkItem> frameWork;
        List<WorkItem> queued = new();
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            frameWork = new List<FrameWorkItem>(_frameWork);
            _frameWork.Clear();

            while (_queue.TryDequeue(out WorkItem work))
            {
                queued.Add(work);
            }
        }

        for (int i = 0; i < frameWork.Count; i++)
        {
            frameWork[i].Work.CancelOnDispose(reason ?? disposed);
        }

        for (int i = 0; i < queued.Count; i++)
        {
            queued[i].CancelOnDispose(reason ?? disposed);
        }

        CompletionQueue.Close(disposed);
    }

    public void DrainClosingOperations()
    {
        DrainClosingOperations(
            CompletionExceptionPolicy.Throw,
            null);
    }

    public void DrainClosingOperations(
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        while (true)
        {
            WorkItem work;
            lock (_lock)
            {
                if (_closingQueue.Count == 0)
                {
                    return;
                }

                work = _closingQueue.Dequeue();
            }

            try
            {
                work.Invoke();
            }
            catch (Exception ex)
            {
                reportException?.Invoke(ex);
                if (exceptionPolicy == CompletionExceptionPolicy.Throw)
                {
                    throw;
                }
            }
        }
    }

    public void FinalizeClose()
    {
        DrainClosingOperations();

        lock (_lock)
        {
            if (Volatile.Read(ref _pendingSourceCount) != 0 ||
                _closingQueue.Count != 0 ||
                _queue.Count != 0 ||
                _frameWork.Count != 0)
            {
                throw new InvalidOperationException(
                    "LayerBaseSynchronizationContext cannot finalize while accepted operations are pending.");
            }

            _disposed = true;
            _finalized = true;
        }
    }

    public static LayerBaseSynchronizationContext Install(bool allowThreadPoolFallbackOnDispose = true)
    {
        return new LayerBaseSynchronizationContext(
            Thread.CurrentThread.ManagedThreadId,
            allowThreadPoolFallbackOnDispose);
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        var item = new WorkItem(d, state);
        lock (_lock)
        {
            ThrowIfDisposedNoLock();
            _queue.Enqueue(item);
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        lock (_lock)
        {
            ThrowIfDisposedNoLock();
        }

        if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            d(state);
            return;
        }

        using var gate = new ManualResetEventSlim(false);
        var sendWork = new SendWorkItem(d, state, gate);
        var item = new WorkItem(static payload =>
        {
            var work = (SendWorkItem)payload!;
            if (!work.TryStart()) return;

            Exception? error = null;
            try
            {
                work.Callback(work.State);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                work.Complete(error);
            }
        }, sendWork);

        lock (_lock)
        {
            ThrowIfDisposedNoLock();
            _queue.Enqueue(item);
        }

        gate.Wait();
        if (sendWork.Error != null) throw sendWork.Error;
    }

    internal void ScheduleInFrames(Action action, int frames)
    {
        ScheduleInFrames(static state => ((Action)state!).Invoke(), action, frames);
    }

    internal void ScheduleInFrames(SendOrPostCallback callback, object? state, int frames)
    {
        var workItem = new WorkItem(callback, state);
        lock (_lock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LayerBaseSynchronizationContext));
            }

            if (frames <= 0)
            {
                _queue.Enqueue(workItem);
                return;
            }

            _frameWork.Add(new FrameWorkItem(frames, workItem));
        }
    }

    internal bool TryScheduleClosingContinuation(Action continuation)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));

        var workItem = new WorkItem(static state => ((Action)state!).Invoke(), continuation);
        lock (_lock)
        {
            if (!_disposed || _finalized)
            {
                return false;
            }

            _closingQueue.Enqueue(workItem);
            return true;
        }
    }

    internal bool TryRegisterSource()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return false;
            }

            _pendingSourceCount++;
            return true;
        }
    }

    internal void UnregisterSource()
    {
        int remaining = Interlocked.Decrement(ref _pendingSourceCount);
        if (remaining < 0)
        {
            Interlocked.Exchange(ref _pendingSourceCount, 0);
            throw new InvalidOperationException("LayerBaseSynchronizationContext source registry underflow.");
        }
    }

    internal void ScheduleForTest(Action invoke, Action<Exception> cancel, int frames)
    {
        if (invoke == null) throw new ArgumentNullException(nameof(invoke));
        if (cancel == null) throw new ArgumentNullException(nameof(cancel));

        var workItem = new WorkItem(static state => ((TestWorkItem)state!).Invoke(), new TestWorkItem(invoke, cancel));
        lock (_lock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LayerBaseSynchronizationContext));
            }

            if (frames <= 0)
            {
                _queue.Enqueue(workItem);
                return;
            }

            _frameWork.Add(new FrameWorkItem(frames, workItem));
        }
    }

    private void ThrowIfDisposedNoLock()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LayerBaseSynchronizationContext));
        }
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

        public void CancelOnDispose(Exception error)
        {
            if (_state is SendWorkItem sendWork)
            {
                sendWork.TryCancel(error);
                return;
            }

            if (_state is IContextDisposeCancellable cancellable)
            {
                cancellable.CancelOnDispose(error);
            }
        }
    }

    private sealed class SendWorkItem
    {
        public readonly SendOrPostCallback Callback;
        public readonly ManualResetEventSlim Gate;
        public readonly object? State;
        public Exception? Error;
        private int _state;

        public SendWorkItem(SendOrPostCallback callback, object? state, ManualResetEventSlim gate)
        {
            Callback = callback;
            State = state;
            Gate = gate;
        }

        public bool TryStart()
        {
            return Interlocked.CompareExchange(ref _state, 1, 0) == 0;
        }

        public void Complete(Exception? error)
        {
            if (error != null)
            {
                Error = error;
            }

            Volatile.Write(ref _state, 2);
            Gate.Set();
        }

        public bool TryCancel(Exception error)
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) != 0)
                return false;

            Error = error;
            Gate.Set();
            return true;
        }
    }

    private sealed class TestWorkItem : IContextDisposeCancellable
    {
        private readonly Action _invoke;
        private readonly Action<Exception> _cancel;

        public TestWorkItem(Action invoke, Action<Exception> cancel)
        {
            _invoke = invoke;
            _cancel = cancel;
        }

        public void Invoke()
        {
            _invoke();
        }

        public void CancelOnDispose(Exception error)
        {
            _cancel(error);
        }
    }

    private readonly struct FrameWorkItem
    {
        public readonly WorkItem Work;
        public readonly int FramesRemaining;

        public FrameWorkItem(int framesRemaining, WorkItem work)
        {
            FramesRemaining = framesRemaining;
            Work = work;
        }

        public FrameWorkItem Tick()
        {
            var next = Math.Max(FramesRemaining - 1, 0);
            return new FrameWorkItem(next, Work);
        }

        public bool ShouldRun => FramesRemaining <= 0;
    }
}

public interface IArchMainThreadPump
{
    void Update(
        int                       maxItems        = 0,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>?        reportException = null);
}

internal interface IContextDisposeCancellable
{
    void CancelOnDispose(Exception error);
}
