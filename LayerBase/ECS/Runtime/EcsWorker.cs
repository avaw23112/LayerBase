using Arch.Core;
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
    }

    public bool IsExecuting => Volatile.Read(ref _executing) != 0;

    public bool IsParked => Volatile.Read(ref _parked) != 0;

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
        if (Volatile.Read(ref _parked) != 0)
        {
            _signal.Set();
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

        try
        {
            while (_running)
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

                if (!didWork)
                {
                    WaitForWork();
                }
            }
        }
        finally
        {
            EcsThreadGuard.Unbind(_runtime.Id);
        }
    }

    private void WaitForWork()
    {
        EcsWorkerIdleStrategy strategy = EcsWorkerIdleStrategy.For(_options.WorkerIdlePolicy);

        for (int i = 0; i < strategy.SpinIterations && _running; i++)
        {
            if (_workQueue.Count != 0)
            {
                return;
            }

            Thread.SpinWait(strategy.SpinWaitCycles);
        }

        for (int i = 0; i < strategy.YieldIterations && _running; i++)
        {
            if (_workQueue.Count != 0)
            {
                return;
            }

            Thread.Yield();
        }

        for (int i = 0; i < strategy.SleepZeroIterations && _running; i++)
        {
            if (_workQueue.Count != 0)
            {
                return;
            }

            Thread.Sleep(0);
        }

        Volatile.Write(ref _parked, 1);
        if (!_running || _workQueue.Count != 0)
        {
            Volatile.Write(ref _parked, 0);
            return;
        }

        _signal.WaitOne();
        Volatile.Write(ref _parked, 0);
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

    private readonly struct EcsWorkerIdleStrategy
    {
        private EcsWorkerIdleStrategy(int spinIterations, int spinWaitCycles, int yieldIterations, int sleepZeroIterations)
        {
            SpinIterations = spinIterations;
            SpinWaitCycles = spinWaitCycles;
            YieldIterations = yieldIterations;
            SleepZeroIterations = sleepZeroIterations;
        }

        public int SpinIterations { get; }

        public int SpinWaitCycles { get; }

        public int YieldIterations { get; }

        public int SleepZeroIterations { get; }

        public static EcsWorkerIdleStrategy For(EcsWorkerIdlePolicy policy)
        {
            return policy switch
            {
                EcsWorkerIdlePolicy.LowLatency => new EcsWorkerIdleStrategy(100_000, 16, 64, 16),
                EcsWorkerIdlePolicy.PowerSaving => new EcsWorkerIdleStrategy(16, 4, 1, 1),
                _ => new EcsWorkerIdleStrategy(2_048, 8, 16, 4)
            };
        }
    }
}
