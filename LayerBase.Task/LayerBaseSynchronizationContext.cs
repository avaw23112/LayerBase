using System.Collections.Concurrent;
using System.Diagnostics;

namespace LayerBase.Async;

/// <summary>
///     SynchronizationContext that captures continuations and replays them on the main thread via Update().
/// </summary>
public sealed class LayerBaseSynchronizationContext : SynchronizationContext, IArchMainThreadPump, IDisposable
{
    private readonly List<FrameWorkItem> _frameWork = new();
    private readonly object _lock = new();
    private readonly int _mainThreadId;
    private readonly HashSet<IContextDisposeCancellable> _pendingSources = new();
    private readonly ConcurrentQueue<WorkItem> _queue = new();
    internal MainThreadCompletionQueue CompletionQueue { get; } = new();
    private int _allowClosingCancellationPosts;
    private bool _closing;
    private bool _disposed;

    private LayerBaseSynchronizationContext(int mainThreadId)
    {
        _mainThreadId = mainThreadId;
    }

    /// <summary>Run queued work and frame-delayed work; call once per frame on the main thread.</summary>
    public void Update(
        int                       maxItems        = 0,
        CompletionExceptionPolicy exceptionPolicy = CompletionExceptionPolicy.Throw,
        Action<Exception>?        reportException = null)
    {
        if (_disposed) return;

        // Drain completion queue first as per design
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
            catch
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
            _frameWork.Clear();
            pendingSources = _pendingSources.ToArray();
            _pendingSources.Clear();
        }

        while (_queue.TryDequeue(out _))
        {
        }

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

    /// <summary>Schedule an action after the specified number of frames.</summary>
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
            return;
        }

        lock (_lock)
        {
            _frameWork.Add(new FrameWorkItem(frames, workItem));
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
