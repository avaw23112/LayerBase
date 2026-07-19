namespace LayerBase.Worker;

internal sealed class WorkerJobSchedulerOptions
{
    public WorkerJobSchedulerOptions(
        int maxConcurrency,
        int queueCapacity,
        int maxBatchItems,
        TimeSpan maxBatchDuration)
    {
        MaxConcurrency = Math.Max(1, maxConcurrency);
        QueueCapacity = Math.Max(1, queueCapacity);
        MaxBatchItems = Math.Max(1, maxBatchItems);
        MaxBatchDuration = maxBatchDuration > TimeSpan.Zero
            ? maxBatchDuration
            : TimeSpan.FromMilliseconds(1);
    }

    public int MaxConcurrency { get; }

    public int QueueCapacity { get; }

    public int MaxBatchItems { get; }

    public TimeSpan MaxBatchDuration { get; }

    public int StateCapacity { get; init; } = 4096;

    public int ShutdownTimeoutMilliseconds { get; init; } = 5000;

    public int ShutdownTotalTimeoutMilliseconds { get; init; } = 15000;

    public int WorkerItemPoolCapacity { get; init; } = 64;

    public static WorkerJobSchedulerOptions Default => new(
        maxConcurrency: Math.Max(1, Environment.ProcessorCount - 1),
        queueCapacity: 4096,
        maxBatchItems: 64,
        maxBatchDuration: TimeSpan.FromMilliseconds(1));
}
