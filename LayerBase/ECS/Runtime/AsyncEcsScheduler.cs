using Arch.Core;
using System.Diagnostics;
using LayerBase.ECS.Runtime.Submission;

namespace LayerBase.ECS.Runtime;

internal sealed class AsyncEcsScheduler : IEcsWorkScheduler
{
    private static int s_nextSchedulerId;

    private readonly LayerRuntime _runtime;
    private readonly World _world;
    private readonly EcsSubmissionBatchPool _submissionBatchPool;
    private readonly EcsWorkQueue _workQueue;
    private readonly EcsResultQueue _resultQueue = new();
    private readonly EcsWorker _worker;
    private readonly int _schedulerId;
    private EcsSubmissionBatch _currentSubmissionBatch;
    private long _nextSequence;
    private int _stopped;

    public AsyncEcsScheduler(LayerRuntime runtime, World world, EcsRuntimeOptions options)
    {
        _runtime = runtime;
        _world = world;
        _schedulerId = Interlocked.Increment(ref s_nextSchedulerId);
        _submissionBatchPool = new EcsSubmissionBatchPool(initialBatchCapacity: 16);
        _currentSubmissionBatch = _submissionBatchPool.Rent();
        _workQueue = new EcsWorkQueue();
        _worker = new EcsWorker(runtime, world, _schedulerId, _workQueue, _resultQueue, _submissionBatchPool, options);
    }

    public EcsExecutionMode Mode => EcsExecutionMode.Async;

    public bool IsSchedulerThread => EcsThreadGuard.IsEcsThread(_schedulerId);

    internal LayerRuntime Runtime => _runtime;

    internal World World => _world;

    internal int CurrentSubmissionCountForTest => _currentSubmissionBatch.Count;

    public void Schedule(IEcsWorkItem item)
    {
        if (!IsOwnerThread)
        {
            var exception = new InvalidOperationException("AsyncEcsScheduler can only be scheduled from the runtime owner thread.");
            item.Cancel(exception);
            throw exception;
        }

        if (Volatile.Read(ref _stopped) != 0)
        {
            item.Cancel(new OperationCanceledException("ECS scheduler has stopped."));
            return;
        }

        _currentSubmissionBatch.Add(item);
    }

    public EcsDrainStats DrainResults(int maxCount)
    {
        return _resultQueue.DrainToMainThread(_runtime, maxCount);
    }

    public void FlushSubmissions()
    {
        EnsureOwnerThread();
        FlushSubmissionsCore(allowWhenStopped: false);
    }

    public void SetWorkerIdlePolicy(EcsWorkerIdleOptions options)
    {
        _worker.SetIdleOptions(options);
    }

    public void NotifyFrameStart()
    {
        _worker.NotifyFrameStart();
    }

    public void NotifyFrameEnd()
    {
        _worker.NotifyFrameEnd();
    }

    public long FlushSubmissionsForTest()
    {
        EnsureOwnerThread();
        return FlushSubmissionsCore(allowWhenStopped: false);
    }

    public void EnsureCurrentSubmissionCapacityForTest(int entryCapacity)
    {
        EnsureOwnerThread();
        _currentSubmissionBatch.EnsureCapacity(entryCapacity);
    }

    public void SignalForTest()
    {
        _worker.Signal();
    }

    public void WaitWorkerParkedForTest(TimeSpan timeout)
    {
        long start = Stopwatch.GetTimestamp();
        long timeoutTicks = (long)(timeout.TotalSeconds * Stopwatch.Frequency);

        while (!_worker.IsParked)
        {
            if (Stopwatch.GetTimestamp() - start > timeoutTicks)
            {
                throw new TimeoutException("Timed out waiting for ECS worker to park.");
            }

            Thread.SpinWait(64);
        }
    }

    private long FlushSubmissionsCore(bool allowWhenStopped)
    {
        if (!allowWhenStopped && Volatile.Read(ref _stopped) != 0)
        {
            _currentSubmissionBatch.CancelPendingItems();
            return _workQueue.CompletedSequence;
        }

        EcsSubmissionBatch batch = _currentSubmissionBatch;
        if (batch.Count == 0)
        {
            return _workQueue.CompletedSequence;
        }

        long sequence = Interlocked.Increment(ref _nextSequence);
        batch.Sequence = sequence;
        _currentSubmissionBatch = _submissionBatchPool.Rent();
        if (!_workQueue.TryEnqueue(batch))
        {
            batch.CancelPendingItems();
            _submissionBatchPool.Return(batch);
            throw new InvalidOperationException("ECS work queue rejected a submitted batch.");
        }

        _worker.Signal();
        return sequence;
    }

    public void Start()
    {
        EnsureOwnerThread();
        _worker.Start();
    }

    public void Stop()
    {
        EnsureOwnerThread();
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        FlushSubmissionsCore(allowWhenStopped: true);
        _workQueue.Close();
        _worker.Stop();
        _currentSubmissionBatch.CancelPendingItems();

        List<EcsSubmissionBatch> pending = _workQueue.DetachAll();
        for (int i = 0; i < pending.Count; i++)
        {
            EcsSubmissionBatch batch = pending[i];
            batch.CancelPendingItems();
            _submissionBatchPool.Return(batch);
        }
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
        EnsureOwnerThread();
        Stop();
        _resultQueue.Close();
        _resultQueue.Clear();
        _worker.Dispose();
    }

    private bool IsOwnerThread => _runtime.IsOwnerThreadForActorWorld;

    private void EnsureOwnerThread()
    {
        if (!IsOwnerThread)
        {
            throw new InvalidOperationException("AsyncEcsScheduler control APIs must be called from the runtime owner thread.");
        }
    }
}
