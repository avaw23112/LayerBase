namespace LayerBase.ECS.Runtime;

public interface IEcsScheduler : IDisposable
{
    EcsExecutionMode Mode { get; }

    EcsDrainStats DrainResults(int maxCount);

    void Start();

    void Stop();
}

internal interface IEcsWorkScheduler : IEcsScheduler
{
    bool IsSchedulerThread { get; }

    void Schedule(IEcsWorkItem item);

    void WaitIdleForTest(TimeSpan timeout);
}
