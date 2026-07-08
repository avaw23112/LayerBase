using Arch.Core;
using System.Diagnostics;
using LayerBase.ECS.Runtime.Submission;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsWorker : IDisposable
{
    private readonly LayerRuntime _runtime;
    private readonly World _world;
    private readonly EcsWorkQueue _workQueue;
    private readonly EcsResultQueue _resultQueue;
    private readonly EcsSubmissionBatchPool _submissionBatchPool;
    private readonly EcsRuntimeOptions _options;
    private readonly AutoResetEvent _signal = new(false);

    private Thread? _thread;
    private volatile bool _running;
    private int _executing;
    private int _parked;
    private int _hadSubmissionThisFrame;
    private int _framesSinceLastSubmission;
    private long _lastWorkTicks;
    private long _lastSubmitTicks;
    private long _lastWakeLatencyTicks;
    private long _adaptiveWarmKeepAliveTicks;
    private EcsWorkerIdleOptions _idleOptions;

    public EcsWorker(
        LayerRuntime runtime,
        World world,
        EcsWorkQueue workQueue,
        EcsResultQueue resultQueue,
        EcsSubmissionBatchPool submissionBatchPool,
        EcsRuntimeOptions options)
    {
        _runtime = runtime;
        _world = world;
        _workQueue = workQueue;
        _resultQueue = resultQueue;
        _submissionBatchPool = submissionBatchPool;
        _options = options;
        _idleOptions = options.WorkerIdle.Clone();
        _adaptiveWarmKeepAliveTicks = ToStopwatchTicks(_idleOptions.WarmKeepAlive);
    }

    public bool IsExecuting => Volatile.Read(ref _executing) != 0;

    public bool IsParked => Volatile.Read(ref _parked) != 0;

    public long LastWakeLatencyTicks => Volatile.Read(ref _lastWakeLatencyTicks);

    public long AdaptiveWarmKeepAliveTicks => Volatile.Read(ref _adaptiveWarmKeepAliveTicks);

    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = _options.WorkerName
        };
        _thread.Start();
    }

    public void Signal()
    {
        SignalOnce();
    }

    public void SignalOnce()
    {
        NotifySubmission();

        EcsWorkerIdleOptions idleOptions = Volatile.Read(ref _idleOptions);
        if (!idleOptions.SignalOnlyWhenParked)
        {
            _signal.Set();
            return;
        }

        if (Volatile.Read(ref _parked) != 0)
        {
            _signal.Set();
        }
    }

    public void SetIdleOptions(EcsWorkerIdleOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        EcsWorkerIdleOptions next = options.Clone();
        Volatile.Write(ref _idleOptions, next);
        Volatile.Write(ref _adaptiveWarmKeepAliveTicks, ToStopwatchTicks(next.WarmKeepAlive));

        if (Volatile.Read(ref _parked) != 0)
        {
            _signal.Set();
        }
    }

    public void NotifySubmission()
    {
        Volatile.Write(ref _lastSubmitTicks, Stopwatch.GetTimestamp());
        Volatile.Write(ref _framesSinceLastSubmission, 0);
        Volatile.Write(ref _hadSubmissionThisFrame, 1);
    }

    public void NotifyFrameStart()
    {
        Volatile.Write(ref _hadSubmissionThisFrame, 0);
    }

    public void NotifyFrameEnd()
    {
        if (Volatile.Read(ref _hadSubmissionThisFrame) == 0)
        {
            Interlocked.Increment(ref _framesSinceLastSubmission);
        }
    }

    public void Stop()
    {
        _running = false;
        _signal.Set();
        _thread?.Join();
        _thread = null;
    }

    public void Dispose()
    {
        Stop();
        _signal.Dispose();
    }

    private void Run()
    {
        EcsThreadGuard.Bind(_runtime.Id, Environment.CurrentManagedThreadId);
        long startTicks = Stopwatch.GetTimestamp();
        Volatile.Write(ref _lastWorkTicks, startTicks);
        Volatile.Write(ref _lastSubmitTicks, startTicks);

        try
        {
            while (_running)
            {
                if (TryExecuteAllBatches())
                {
                    Volatile.Write(ref _lastWorkTicks, Stopwatch.GetTimestamp());
                    continue;
                }

                IdleOnce();
            }
        }
        finally
        {
            EcsThreadGuard.Unbind(_runtime.Id);
        }
    }

    private bool TryExecuteAllBatches()
    {
        bool didWork = false;
        int processed = 0;

        while ((_options.MaxWorkItemsPerWake <= 0 || processed < _options.MaxWorkItemsPerWake) &&
               _workQueue.TryDequeue(out EcsSubmissionBatch? batch))
        {
            if (batch == null)
            {
                continue;
            }

            didWork = true;
            Interlocked.Exchange(ref _executing, 1);
            try
            {
                ExecuteBatch(batch, ref processed);
            }
            finally
            {
                Interlocked.Exchange(ref _executing, 0);
                _workQueue.MarkCompleted(batch.Sequence);
                _submissionBatchPool.Return(batch);
            }
        }

        return didWork;
    }

    private void IdleOnce()
    {
        long now = Stopwatch.GetTimestamp();
        if (ShouldKeepWarm(now))
        {
            WarmIdle();
            return;
        }

        ParkIfStillIdle();
    }

    private bool ShouldKeepWarm(long now)
    {
        EcsWorkerIdleOptions idleOptions = Volatile.Read(ref _idleOptions);
        long elapsedSinceLastWork = now - Volatile.Read(ref _lastWorkTicks);

        return idleOptions.Policy switch
        {
            EcsWorkerIdlePolicy.LowLatency => elapsedSinceLastWork < ToStopwatchTicks(idleOptions.ParkAfterIdle),
            EcsWorkerIdlePolicy.PowerSaving => false,
            EcsWorkerIdlePolicy.Balanced => elapsedSinceLastWork < ToStopwatchTicks(idleOptions.WarmKeepAlive),
            EcsWorkerIdlePolicy.Adaptive => ShouldKeepAdaptiveWarm(now, idleOptions, elapsedSinceLastWork),
            _ => elapsedSinceLastWork < ToStopwatchTicks(idleOptions.WarmKeepAlive)
        };
    }

    private bool ShouldKeepAdaptiveWarm(long now, EcsWorkerIdleOptions idleOptions, long elapsedSinceLastWork)
    {
        if (Volatile.Read(ref _framesSinceLastSubmission) <= 1)
        {
            return elapsedSinceLastWork < ToStopwatchTicks(idleOptions.ParkAfterIdle);
        }

        long adaptiveWarmTicks = Volatile.Read(ref _adaptiveWarmKeepAliveTicks);
        return elapsedSinceLastWork < adaptiveWarmTicks;
    }

    private void WarmIdle()
    {
        EcsWorkerIdleOptions idleOptions = Volatile.Read(ref _idleOptions);

        for (int i = 0; i < idleOptions.SpinIterations && _running; i++)
        {
            if (HasPendingWork())
            {
                return;
            }

            Thread.SpinWait(idleOptions.SpinWaitCycles);
        }

        for (int i = 0; i < idleOptions.YieldIterations && _running; i++)
        {
            if (HasPendingWork())
            {
                return;
            }

            Thread.Yield();
        }

        for (int i = 0; i < idleOptions.Sleep0Iterations && _running; i++)
        {
            if (HasPendingWork())
            {
                return;
            }

            Thread.Sleep(0);
        }
    }

    private void ParkIfStillIdle()
    {
        Volatile.Write(ref _parked, 1);
        try
        {
            if (!_running || HasPendingWork())
            {
                return;
            }

            long waitStart = Stopwatch.GetTimestamp();
            _signal.WaitOne();
            long wakeTicks = Stopwatch.GetTimestamp() - waitStart;

            Volatile.Write(ref _lastWakeLatencyTicks, wakeTicks);
            UpdateAdaptivePolicy(wakeTicks);
        }
        finally
        {
            Volatile.Write(ref _parked, 0);
        }
    }

    private bool HasPendingWork()
    {
        return _workQueue.Count != 0;
    }

    private void UpdateAdaptivePolicy(long wakeLatencyTicks)
    {
        EcsWorkerIdleOptions idleOptions = Volatile.Read(ref _idleOptions);
        if (idleOptions.Policy != EcsWorkerIdlePolicy.Adaptive)
        {
            return;
        }

        long targetTicks = ToStopwatchTicks(idleOptions.TargetWakeLatency);
        long current = Volatile.Read(ref _adaptiveWarmKeepAliveTicks);
        if (current <= 0)
        {
            current = ToStopwatchTicks(idleOptions.MinWarmKeepAlive);
        }

        if (wakeLatencyTicks > targetTicks)
        {
            long next = Math.Min(
                current * 2,
                ToStopwatchTicks(idleOptions.MaxWarmKeepAlive));

            Volatile.Write(ref _adaptiveWarmKeepAliveTicks, next);
            return;
        }

        if (NoRecentSubmissions())
        {
            long next = Math.Max(
                current / 2,
                ToStopwatchTicks(idleOptions.MinWarmKeepAlive));

            Volatile.Write(ref _adaptiveWarmKeepAliveTicks, next);
        }
    }

    private bool NoRecentSubmissions()
    {
        long now = Stopwatch.GetTimestamp();
        long lastSubmit = Volatile.Read(ref _lastSubmitTicks);
        return now - lastSubmit > ToStopwatchTicks(TimeSpan.FromMilliseconds(250));
    }

    private static long ToStopwatchTicks(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return 0;
        }

        return (long)(value.TotalSeconds * Stopwatch.Frequency);
    }

    private void ExecuteBatch(EcsSubmissionBatch batch, ref int processed)
    {
        _resultQueue.BeginBatch();
        try
        {
            ReadOnlySpan<EcsSubmissionEntry> entries = batch.AsSpan();
            for (int i = 0; i < entries.Length; i++)
            {
                EcsSubmissionEntry entry = entries[i];
                processed++;

                if (entry.IsRecord)
                {
                    EcsWorkRecord record = entry.Record;
                    ExecuteRecord(batch, in record);
                }
                else if (entry.Item != null)
                {
                    ExecuteItem(entry.Item);
                }
            }
        }
        finally
        {
            _resultQueue.EndBatch();
        }
    }

    private void ExecuteRecord(EcsSubmissionBatch batch, in EcsWorkRecord record)
    {
        EcsThreadGuard.EnterExecution(_runtime.Id, _resultQueue);

        try
        {
            EcsExecutorRegistry.Execute(
                record.ExecutorId,
                _world,
                in record,
                batch);
        }
        catch (Exception ex)
        {
            string debugName = EcsExecutorRegistry.GetDebugName(record.ExecutorId);
            _resultQueue.Enqueue(new EcsWorkFailedResult(debugName, ex));
        }
        finally
        {
            EcsThreadGuard.ExitExecution(_runtime.Id);
        }
    }

    private void ExecuteItem(IEcsWorkItem item)
    {
        EcsThreadGuard.EnterExecution(_runtime.Id, _resultQueue);

        try
        {
            item.Execute(_world, _resultQueue);
        }
        catch (Exception ex)
        {
            _resultQueue.Enqueue(new EcsWorkFailedResult(item.DebugName, ex));
        }
        finally
        {
            EcsThreadGuard.ExitExecution(_runtime.Id);
            if (item is IPooledEcsWorkItem pooled)
            {
                pooled.ReturnToPool();
            }
        }
    }
}
