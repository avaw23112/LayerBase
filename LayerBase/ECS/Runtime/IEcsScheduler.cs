namespace LayerBase.ECS.Runtime;

public interface IEcsScheduler : IDisposable
{
    EcsExecutionMode Mode { get; }

    EcsDrainStats DrainResults(int maxCount);

    void FlushSubmissions();

    void Start();

    void Stop();
}

internal interface IEcsWorkScheduler : IEcsScheduler
{
    bool IsSchedulerThread { get; }

    void Schedule(IEcsWorkItem item);

    void WaitIdleForTest(TimeSpan timeout);

    long FlushSubmissionsForTest();

    void WaitFenceForTest(long fence, TimeSpan timeout);
}
