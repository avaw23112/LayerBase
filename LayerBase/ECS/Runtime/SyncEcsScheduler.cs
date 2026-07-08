using Arch.Core;

namespace LayerBase.ECS.Runtime;

internal sealed class SyncEcsScheduler : IEcsWorkScheduler
{
    private readonly LayerRuntime _runtime;
    private readonly World _world;
    private readonly EcsResultQueue _resultQueue = new();

    public SyncEcsScheduler(LayerRuntime runtime, World world)
    {
        _runtime = runtime;
        _world = world;
    }

    public EcsExecutionMode Mode => EcsExecutionMode.Sync;

    public bool IsSchedulerThread => false;

    public void Schedule(IEcsWorkItem item)
    {
        try
        {
            item.Execute(_world, _resultQueue);
        }
        catch (Exception ex)
        {
            _resultQueue.Enqueue(new EcsWorkFailedResult(item.DebugName, ex));
        }
    }

    public EcsDrainStats DrainResults(int maxCount)
    {
        return _resultQueue.DrainToMainThread(_runtime, maxCount);
    }

    public void FlushSubmissions()
    {
    }

    public void Start()
    {
    }

    public void Stop()
    {
    }

    public void WaitIdleForTest(TimeSpan timeout)
    {
    }

    public void Dispose()
    {
        _resultQueue.Clear();
    }
}
