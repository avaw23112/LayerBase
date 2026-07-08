using Arch.Core;
using LayerBase.ECS.Runtime.Submission;

namespace LayerBase.ECS.Runtime;

internal sealed class AsyncEcsScheduler : IEcsWorkScheduler
{
    private readonly LayerRuntime _runtime;
    private readonly EcsSubmissionBatchPool _submissionBatchPool;
    private readonly EcsWorkQueue _workQueue;
    private readonly EcsResultQueue _resultQueue = new();
    private readonly EcsWorker _worker;
    private EcsSubmissionBatch _currentSubmissionBatch;

    public AsyncEcsScheduler(LayerRuntime runtime, World world, EcsRuntimeOptions options)
    {
        _runtime = runtime;
        _submissionBatchPool = new EcsSubmissionBatchPool(initialBatchCapacity: 16);
        _currentSubmissionBatch = _submissionBatchPool.Rent();
        _workQueue = new EcsWorkQueue();
        _worker = new EcsWorker(runtime, world, _workQueue, _resultQueue, _submissionBatchPool, options);
    }

    public EcsExecutionMode Mode => EcsExecutionMode.Async;

    public bool IsSchedulerThread => EcsThreadGuard.IsEcsThread(_runtime.Id);

    public void Schedule(IEcsWorkItem item)
    {
        _currentSubmissionBatch.Add(item);
    }

    public EcsDrainStats DrainResults(int maxCount)
    {
        return _resultQueue.DrainToMainThread(_runtime, maxCount);
    }

    public void FlushSubmissions()
    {
        EcsSubmissionBatch batch = _currentSubmissionBatch;
        if (batch.Count == 0)
        {
            return;
        }

        _currentSubmissionBatch = _submissionBatchPool.Rent();
        _workQueue.Enqueue(batch);
        _worker.Signal();
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

    public void Dispose()
    {
        Stop();
        _resultQueue.Clear();
        _worker.Dispose();
    }
}
