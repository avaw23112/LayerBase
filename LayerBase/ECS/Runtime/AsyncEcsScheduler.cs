using Arch.Core;
using System.Diagnostics;
using LayerBase.ECS.Runtime.Submission;
using ArchQuery = Arch.Core.Query;

namespace LayerBase.ECS.Runtime;

internal sealed class AsyncEcsScheduler : IEcsWorkScheduler
{
    private readonly LayerRuntime _runtime;
    private readonly World _world;
    private readonly EcsSubmissionBatchPool _submissionBatchPool;
    private readonly EcsWorkQueue _workQueue;
    private readonly EcsResultQueue _resultQueue = new();
    private readonly EcsWorker _worker;
    private EcsSubmissionBatch _currentSubmissionBatch;
    private long _nextSequence;

    public AsyncEcsScheduler(LayerRuntime runtime, World world, EcsRuntimeOptions options)
    {
        _runtime = runtime;
        _world = world;
        _submissionBatchPool = new EcsSubmissionBatchPool(initialBatchCapacity: 16);
        _currentSubmissionBatch = _submissionBatchPool.Rent();
        _workQueue = new EcsWorkQueue();
        _worker = new EcsWorker(runtime, world, _workQueue, _resultQueue, _submissionBatchPool, options);
    }

    public EcsExecutionMode Mode => EcsExecutionMode.Async;

    public bool IsSchedulerThread => EcsThreadGuard.IsEcsThread(_runtime.Id);

    internal LayerRuntime Runtime => _runtime;

    internal World World => _world;

    public void Schedule(IEcsWorkItem item)
    {
        _currentSubmissionBatch.Add(item);
    }

    public void RecordPlainQuery<TJob>(
        int executorId,
        ArchQuery query,
        object? predicate,
        in TJob job)
        where TJob : struct
    {
        int jobOffset = _currentSubmissionBatch.JobArena.Store(in job);
        var record = new EcsWorkRecord(
            executorId,
            query,
            predicate,
            jobOffset);

        _currentSubmissionBatch.AddRecord(in record);
    }

    public EcsDrainStats DrainResults(int maxCount)
    {
        return _resultQueue.DrainToMainThread(_runtime, maxCount);
    }

    public void FlushSubmissions()
    {
        FlushSubmissionsCore();
    }

    public long FlushSubmissionsForTest()
    {
        return FlushSubmissionsCore();
    }

    private long FlushSubmissionsCore()
    {
        EcsSubmissionBatch batch = _currentSubmissionBatch;
        if (batch.Count == 0)
        {
            return _workQueue.CompletedSequence;
        }

        long sequence = Interlocked.Increment(ref _nextSequence);
        batch.Sequence = sequence;
        _currentSubmissionBatch = _submissionBatchPool.Rent();
        _workQueue.Enqueue(batch);
        _worker.Signal();
        return sequence;
    }

    public void Start()
    {
        _worker.Start();
    }

    public void Stop()
    {
        FlushSubmissions();
        _worker.Stop();
        _currentSubmissionBatch.Clear();
        _workQueue.Clear();
    }

    public void WaitIdleForTest(TimeSpan timeout)
    {
        FlushSubmissions();
        SpinWait.SpinUntil(
            () => _workQueue.Count == 0 && !_worker.IsExecuting,
            timeout);
    }

    public void WaitFenceForTest(long fence, TimeSpan timeout)
    {
        if (fence <= 0 || _workQueue.CompletedSequence >= fence)
        {
            return;
        }

        long start = Stopwatch.GetTimestamp();
        long timeoutTicks = (long)(timeout.TotalSeconds * Stopwatch.Frequency);

        while (_workQueue.CompletedSequence < fence)
        {
            if (Stopwatch.GetTimestamp() - start > timeoutTicks)
            {
                throw new TimeoutException($"Timed out waiting for ECS fence {fence}.");
            }

            Thread.SpinWait(64);
        }
    }

    public void Dispose()
    {
        Stop();
        _resultQueue.Clear();
        _worker.Dispose();
    }
}
