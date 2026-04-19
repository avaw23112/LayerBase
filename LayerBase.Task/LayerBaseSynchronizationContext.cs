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
    private readonly ConcurrentQueue<WorkItem> _queue = new();
    private bool _disposed;

    private LayerBaseSynchronizationContext(int mainThreadId)
    {
        _mainThreadId = mainThreadId;
    }

    /// <summary>Run queued work and frame-delayed work; call once per frame on the main thread.</summary>
    public void Update(int maxItems = 0)
    {
        if (_disposed) return;

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
            catch (Exception ex)
            {
                // logging omitted to keep this package independent
                Debug.WriteLine(ex);
            }

            processed++;
            if (maxItems > 0 && processed >= maxItems)
                break;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        lock (_lock)
        {
            _frameWork.Clear();
        }

        while (_queue.TryDequeue(out _))
        {
        }
    }

    /// <summary>Install this context as current on the calling thread; returns existing instance if already set.</summary>
    public static LayerBaseSynchronizationContext InstallAsCurrent()
    {
        if (Current is LayerBaseSynchronizationContext existing)
            return existing;

        var ctx = new LayerBaseSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
        SetSynchronizationContext(ctx);
        return ctx;
    }

    public static LayerBaseSynchronizationContext Install()
    {
        if (Current is LayerBaseSynchronizationContext existing)
            return existing;
        return new LayerBaseSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
    }


    public override void Post(SendOrPostCallback d, object? state)
    {
        if (_disposed) return;
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

        using var gate = new ManualResetEventSlim(false);
        var sendWork = new SendWorkItem(d, state, gate);
        _queue.Enqueue(new WorkItem(static payload =>
        {
            var work = (SendWorkItem)payload!;
            try
            {
                work.Callback(work.State);
            }
            catch (Exception ex)
            {
                work.Error = ex;
            }
            finally
            {
                work.Gate.Set();
            }
        }, sendWork));
        gate.Wait();
        if (sendWork.Error != null) throw sendWork.Error;
    }

    /// <summary>Schedule an action after the specified number of frames.</summary>
    internal void ScheduleInFrames(Action action, int frames)
    {
        ScheduleInFrames(static state => ((Action)state!).Invoke(), action, frames);
    }

    internal void ScheduleInFrames(SendOrPostCallback callback, object? state, int frames)
    {
        if (_disposed) return;
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

    private sealed class SendWorkItem
    {
        public readonly SendOrPostCallback Callback;
        public readonly ManualResetEventSlim Gate;
        public readonly object? State;
        public Exception? Error;

        public SendWorkItem(SendOrPostCallback callback, object? state, ManualResetEventSlim gate)
        {
            Callback = callback;
            State = state;
            Gate = gate;
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
    void Update(int maxItems = 0);
}