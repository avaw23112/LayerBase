namespace LayerBase.Worker;

public sealed class WorkerShutdownTimeoutException : TimeoutException
{
    public string ThreadName { get; }

    public WorkerHandle RunningJobHandle { get; }

    public int TimeoutMilliseconds { get; }

    public WorkerShutdownTimeoutException(
        string threadName,
        WorkerHandle runningJobHandle,
        int timeoutMilliseconds)
        : base($"Worker thread '{threadName}' did not shut down within {timeoutMilliseconds}ms. Handle: ({runningJobHandle.Index}, {runningJobHandle.Version}).")
    {
        ThreadName = threadName;
        RunningJobHandle = runningJobHandle;
        TimeoutMilliseconds = timeoutMilliseconds;
    }
}
