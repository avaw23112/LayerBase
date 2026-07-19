using System.Collections.Concurrent;
using System.Diagnostics;
using LayerBase.Lifetime;
using LayerBase.Scope;

namespace LayerBase.Worker;

internal enum WorkerExecutorShutdownResult
{
    Stopped,
    TimedOut,
    AlreadyStopped
}

internal sealed class WorkerJobScheduler : ILifetimeParticipant, IDisposable
{
    private static readonly WaitCallback RunBatchCallback =
        static state => ((WorkerJobScheduler)state!).RunBatch();

    private readonly WorkerJobSchedulerOptions _options;
    private readonly ConcurrentQueue<IWorkerExecutionItem> _queue = new();
    private readonly ConcurrentQueue<int> _lanePool = new();
    private readonly ManualResetEventSlim _drained = new(initialState: true);
    private readonly long _maxBatchTimestampTicks;

    private int _accepting = 1;
    private int _queuedCount;
    private int _executingCount;
    private int _runnerCount;
    private int _drainCompleted = 1;
    private int _resourcesReleased;

    string ILifetimeParticipant.LifetimeName => "WorkerJobScheduler";

    void ILifetimeParticipant.CloseAdmission() => CloseAdmission();

    void ILifetimeParticipant.RequestStop() => RequestStop();

    LifetimeDrainResult ILifetimeParticipant.Drain(in ShutdownDeadline deadline)
    {
        return Drain(in deadline);
    }

    void ILifetimeParticipant.Release(TerminalCleanupRunner cleanup)
    {
        ReleaseResources();
    }

    public WorkerJobScheduler(WorkerJobSchedulerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _maxBatchTimestampTicks = Math.Max(
            1L,
            (long)(_options.MaxBatchDuration.TotalSeconds * Stopwatch.Frequency));

        for (int i = 0; i < _options.MaxConcurrency; i++)
            _lanePool.Enqueue(i);
    }

    internal bool TryEnqueue(IWorkerExecutionItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (Volatile.Read(ref _accepting) == 0)
            return false;

        if (!TryReserveQueueSlot())
            return false;

        if (Volatile.Read(ref _accepting) == 0)
        {
            Interlocked.Decrement(ref _queuedCount);
            TrySignalDrained();
            return false;
        }

        Volatile.Write(ref _drainCompleted, 0);
        _drained.Reset();
        _queue.Enqueue(item);
        EnsureRunners();

        return true;
    }

    internal void BeginStop()
    {
        RequestStop();
    }

    internal WorkerExecutorShutdownResult Stop(in ShutdownDeadline deadline)
    {
        LifetimeDrainResult result = Drain(in deadline);
        return result == LifetimeDrainResult.TimedOut
            ? WorkerExecutorShutdownResult.TimedOut
            : WorkerExecutorShutdownResult.Stopped;
    }

    internal LifetimeDrainResult Drain(in ShutdownDeadline deadline)
    {
        CloseAdmission();
        EnsureRunners();

        while (!IsDrained)
        {
            int remaining = deadline.RemainingMilliseconds;
            if (remaining <= 0 || !_drained.Wait(remaining))
                return LifetimeDrainResult.TimedOut;
        }

        ReleaseResources();
        return LifetimeDrainResult.Drained;
    }

    public void Dispose()
    {
        var deadline = ShutdownDeadline.Start(
            TimeSpan.FromMilliseconds(_options.ShutdownTotalTimeoutMilliseconds));

        _ = Drain(in deadline);
    }

    internal void CloseAdmission()
    {
        Interlocked.Exchange(ref _accepting, 0);
        TrySignalDrained();
    }

    internal void RequestStop()
    {
        CloseAdmission();
        EnsureRunners();
    }

    internal void AbortPending()
    {
        CloseAdmission();

        while (_queue.TryDequeue(out IWorkerExecutionItem? item))
        {
            Interlocked.Decrement(ref _queuedCount);
            item.CancelBeforeRun();
        }

        TrySignalDrained();
    }

    internal void ReleaseResources()
    {
        if (!IsDrained)
            return;

        if (Interlocked.Exchange(ref _resourcesReleased, 1) != 0)
            return;

        _drained.Dispose();
    }

    private bool TryReserveQueueSlot()
    {
        while (true)
        {
            int current = Volatile.Read(ref _queuedCount);
            if (current >= _options.QueueCapacity)
                return false;

            if (Interlocked.CompareExchange(
                    ref _queuedCount,
                    current + 1,
                    current) == current)
            {
                return true;
            }
        }
    }

    private void EnsureRunners()
    {
        while (true)
        {
            int queued = Volatile.Read(ref _queuedCount);
            int runners = Volatile.Read(ref _runnerCount);

            if (queued <= runners || runners >= _options.MaxConcurrency)
                return;

            if (Interlocked.CompareExchange(
                    ref _runnerCount,
                    runners + 1,
                    runners) != runners)
            {
                continue;
            }

            ThreadPool.UnsafeQueueUserWorkItem(
                RunBatchCallback,
                this);
        }
    }

    private void RunBatch()
    {
        int laneId = RentLane();

        try
        {
            long deadline = Stopwatch.GetTimestamp() + _maxBatchTimestampTicks;
            int executed = 0;

            while (executed < _options.MaxBatchItems &&
                   Stopwatch.GetTimestamp() < deadline)
            {
                if (!_queue.TryDequeue(out IWorkerExecutionItem? item))
                    break;

                Interlocked.Decrement(ref _queuedCount);
                Interlocked.Increment(ref _executingCount);

                try
                {
                    item.Execute(laneId);
                }
                catch (Exception exception)
                {
                    try
                    {
                        item.FailInfrastructure(exception);
                    }
                    catch
                    {
                        // Infrastructure fault reporting must not tear down the runner.
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _executingCount);
                }

                executed++;
            }
        }
        finally
        {
            ReturnLane(laneId);
            Interlocked.Decrement(ref _runnerCount);

            if (Volatile.Read(ref _queuedCount) != 0)
                EnsureRunners();

            TrySignalDrained();
        }
    }

    private int RentLane()
    {
        if (_lanePool.TryDequeue(out int laneId))
            return laneId;

        return 0;
    }

    private void ReturnLane(int laneId)
    {
        _lanePool.Enqueue(laneId);
    }

    private bool IsDrained =>
        Volatile.Read(ref _queuedCount) == 0 &&
        Volatile.Read(ref _executingCount) == 0 &&
        Volatile.Read(ref _runnerCount) == 0;

    private void TrySignalDrained()
    {
        if (!IsDrained)
            return;

        if (Interlocked.Exchange(ref _drainCompleted, 1) == 0)
            _drained.Set();
    }
}
