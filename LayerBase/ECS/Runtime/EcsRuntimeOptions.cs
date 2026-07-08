namespace LayerBase.ECS.Runtime;

public enum EcsWorkerIdlePolicy
{
    LowLatency,
    Balanced,
    PowerSaving
}

public readonly struct EcsRuntimeOptions
{
    public static readonly EcsRuntimeOptions Default = new(
        EcsExecutionMode.Sync,
        "LayerBase.EcsWorker",
        maxResultsDrainPerPump: 4096,
        maxWorkItemsPerWake: 0,
        workerIdlePolicy: EcsWorkerIdlePolicy.Balanced);

    public EcsRuntimeOptions(
        EcsExecutionMode executionMode,
        string workerName = "LayerBase.EcsWorker",
        int maxResultsDrainPerPump = 4096,
        int maxWorkItemsPerWake = 0,
        EcsWorkerIdlePolicy workerIdlePolicy = EcsWorkerIdlePolicy.Balanced)
    {
        ExecutionMode = executionMode;
        WorkerName = string.IsNullOrWhiteSpace(workerName) ? "LayerBase.EcsWorker" : workerName;
        MaxResultsDrainPerPump = maxResultsDrainPerPump;
        MaxWorkItemsPerWake = maxWorkItemsPerWake;
        WorkerIdlePolicy = workerIdlePolicy;
    }

    public EcsExecutionMode ExecutionMode { get; }

    public string WorkerName { get; }

    public int MaxResultsDrainPerPump { get; }

    public int MaxWorkItemsPerWake { get; }

    public EcsWorkerIdlePolicy WorkerIdlePolicy { get; }
}
