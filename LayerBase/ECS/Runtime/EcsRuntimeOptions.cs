namespace LayerBase.ECS.Runtime;

public readonly struct EcsRuntimeOptions
{
    public static readonly EcsRuntimeOptions Default = new(
        EcsExecutionMode.Sync,
        "LayerBase.EcsWorker",
        maxResultsDrainPerPump: 4096,
        maxWorkItemsPerWake: 0);

    public EcsRuntimeOptions(
        EcsExecutionMode executionMode,
        string workerName = "LayerBase.EcsWorker",
        int maxResultsDrainPerPump = 4096,
        int maxWorkItemsPerWake = 0)
    {
        ExecutionMode = executionMode;
        WorkerName = string.IsNullOrWhiteSpace(workerName) ? "LayerBase.EcsWorker" : workerName;
        MaxResultsDrainPerPump = maxResultsDrainPerPump;
        MaxWorkItemsPerWake = maxWorkItemsPerWake;
    }

    public EcsExecutionMode ExecutionMode { get; }

    public string WorkerName { get; }

    public int MaxResultsDrainPerPump { get; }

    public int MaxWorkItemsPerWake { get; }
}
