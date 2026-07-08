using Arch.Core;

namespace LayerBase.ECS.Runtime;

internal sealed class AsyncEcsScheduler : IEcsWorkScheduler
{
    private readonly LayerRuntime _runtime;
    private readonly EcsWorkQueue _workQueue = new();
    private readonly EcsResultQueue _resultQueue = new();
    private readonly EcsWorker _worker;

    public AsyncEcsScheduler(LayerRuntime runtime, World world, EcsRuntimeOptions options)
    {
        _runtime = runtime;
        _worker = new EcsWorker(runtime, world, _workQueue, _resultQueue, options);
    }

    public EcsExecutionMode Mode => EcsExecutionMode.Async;

    public bool IsSchedulerThread => EcsThreadGuard.IsEcsThread(_runtime.Id);

    public void Schedule(IEcsWorkItem item)
    {
        _workQueue.Enqueue(item);
        _worker.Signal();
    }

    public EcsDrainStats DrainResults(int maxCount)
    {
        return _resultQueue.DrainToMainThread(_runtime, maxCount);
    }

    public void Start()
    {
        _worker.Start();
    }

    public void Stop()
    {
        _worker.Stop();
        _workQueue.Clear();
    }

    public void WaitIdleForTest(TimeSpan timeout)
    {
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
