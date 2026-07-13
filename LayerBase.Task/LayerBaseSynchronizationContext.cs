using System.Collections.Concurrent;
using System.Diagnostics;

namespace LayerBase.Async;

public sealed class LayerBaseSynchronizationContext : SynchronizationContext, IArchMainThreadPump, IDisposable
{
    private readonly List<FrameWorkItem> _frameWork = new();
    private readonly object _lock = new();
    private readonly int _mainThreadId;
    private readonly ConcurrentQueue<WorkItem> _queue = new();
    internal MainThreadCompletionQueue CompletionQueue { get; } = new();
    private bool _disposed;

    private LayerBaseSynchronizationContext(int mainThreadId)
    {
        _mainThreadId = mainThreadId;
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
            try
            {
                work.Invoke();
            }
            catch (Exception)
            {
                throw;
            }

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
        lock (_lock)
        {
            _disposed = true;
            for (int i = 0; i < _frameWork.Count; i++)
            {
                _frameWork[i].Work.CancelOnDispose(disposed);
            }
            _frameWork.Clear();
        }

        CompletionQueue.Close(disposed);
        while (_queue.TryDequeue(out WorkItem work))
        {
            work.CancelOnDispose(disposed);
        }
    }

    public static LayerBaseSynchronizationContext Install()
    {
        return new LayerBaseSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
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
