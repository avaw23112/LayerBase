namespace LayerBase.Worker;

internal sealed class WorkerJobSchedulerOptions
{
    public WorkerJobSchedulerOptions(int workerCount, int stateCapacity, int jobQueueCapacity)
    {
        WorkerCount = Math.Max(1, workerCount);
        StateCapacity = Math.Max(16, stateCapacity);
        JobQueueCapacity = Math.Max(1, jobQueueCapacity);
    }

    public int WorkerCount { get; }

    public int StateCapacity { get; }

    public int JobQueueCapacity { get; }

    public int ShutdownTimeoutMilliseconds { get; init; } = 5000;

    public int ShutdownTotalTimeoutMilliseconds { get; init; } = 15000;

    public int WorkerItemPoolCapacity { get; init; } = 64;

    public static WorkerJobSchedulerOptions Default => new(
        workerCount: Math.Max(1, Environment.ProcessorCount - 1),
        stateCapacity: 4096,
        jobQueueCapacity: 4096);
}
