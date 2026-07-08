namespace LayerBase.ECS.Runtime;

public enum EcsWorkerIdlePolicy
{
    Adaptive,
    LowLatency,
    Balanced,
    PowerSaving,
    Manual
}

public sealed class EcsWorkerIdleOptions
{
    public EcsWorkerIdlePolicy Policy { get; set; } = EcsWorkerIdlePolicy.Adaptive;

    public int SpinIterations { get; set; } = 256;

    public int SpinWaitCycles { get; set; } = 64;

    public int YieldIterations { get; set; } = 64;

    public int Sleep0Iterations { get; set; } = 16;

    public TimeSpan WarmKeepAlive { get; set; } = TimeSpan.FromMilliseconds(2);

    public TimeSpan ParkAfterIdle { get; set; } = TimeSpan.FromMilliseconds(4);

    public TimeSpan MaxWarmKeepAlive { get; set; } = TimeSpan.FromMilliseconds(16);

    public TimeSpan MinWarmKeepAlive { get; set; } = TimeSpan.FromMilliseconds(0.25);

    public TimeSpan TargetWakeLatency { get; set; } = TimeSpan.FromTicks(1_000);

    public bool SignalOnlyWhenParked { get; set; } = true;

    public static EcsWorkerIdleOptions AdaptiveBalanced()
    {
        return new EcsWorkerIdleOptions();
    }

    public static EcsWorkerIdleOptions LowLatency()
    {
        return new EcsWorkerIdleOptions
        {
            Policy = EcsWorkerIdlePolicy.LowLatency,
            SpinIterations = 2_048,
            SpinWaitCycles = 128,
            YieldIterations = 256,
            Sleep0Iterations = 64,
            WarmKeepAlive = TimeSpan.FromMilliseconds(8),
            ParkAfterIdle = TimeSpan.FromMilliseconds(16),
            MaxWarmKeepAlive = TimeSpan.FromMilliseconds(32),
            TargetWakeLatency = TimeSpan.FromTicks(500),
            SignalOnlyWhenParked = true
        };
    }

    public static EcsWorkerIdleOptions Balanced()
    {
        return new EcsWorkerIdleOptions
        {
            Policy = EcsWorkerIdlePolicy.Balanced,
            SpinIterations = 512,
            SpinWaitCycles = 64,
            YieldIterations = 64,
            Sleep0Iterations = 16,
            WarmKeepAlive = TimeSpan.FromMilliseconds(1),
            ParkAfterIdle = TimeSpan.FromMilliseconds(4),
            MaxWarmKeepAlive = TimeSpan.FromMilliseconds(8),
            TargetWakeLatency = TimeSpan.FromTicks(1_000),
            SignalOnlyWhenParked = true
        };
    }

    public static EcsWorkerIdleOptions PowerSaving()
    {
        return new EcsWorkerIdleOptions
        {
            Policy = EcsWorkerIdlePolicy.PowerSaving,
            SpinIterations = 32,
            SpinWaitCycles = 32,
            YieldIterations = 4,
            Sleep0Iterations = 1,
            WarmKeepAlive = TimeSpan.Zero,
            ParkAfterIdle = TimeSpan.FromMilliseconds(0.25),
            MaxWarmKeepAlive = TimeSpan.FromMilliseconds(1),
            TargetWakeLatency = TimeSpan.FromMilliseconds(1),
            SignalOnlyWhenParked = true
        };
    }

    public EcsWorkerIdleOptions Clone()
    {
        return new EcsWorkerIdleOptions
        {
            Policy = Policy,
            SpinIterations = SpinIterations,
            SpinWaitCycles = SpinWaitCycles,
            YieldIterations = YieldIterations,
            Sleep0Iterations = Sleep0Iterations,
            WarmKeepAlive = WarmKeepAlive,
            ParkAfterIdle = ParkAfterIdle,
            MaxWarmKeepAlive = MaxWarmKeepAlive,
            MinWarmKeepAlive = MinWarmKeepAlive,
            TargetWakeLatency = TargetWakeLatency,
            SignalOnlyWhenParked = SignalOnlyWhenParked
        };
    }
}

public readonly struct EcsRuntimeOptions
{
    public static readonly EcsRuntimeOptions Default = new(
        EcsExecutionMode.Sync,
        "LayerBase.EcsWorker",
        maxResultsDrainPerPump: 4096,
        maxWorkItemsPerWake: 0,
        workerIdlePolicy: EcsWorkerIdlePolicy.Adaptive);

    public EcsRuntimeOptions(
        EcsExecutionMode executionMode,
        string workerName = "LayerBase.EcsWorker",
        int maxResultsDrainPerPump = 4096,
        int maxWorkItemsPerWake = 0,
        EcsWorkerIdlePolicy workerIdlePolicy = EcsWorkerIdlePolicy.Adaptive,
        EcsWorkerIdleOptions? workerIdleOptions = null)
    {
        ExecutionMode = executionMode;
        WorkerName = string.IsNullOrWhiteSpace(workerName) ? "LayerBase.EcsWorker" : workerName;
        MaxResultsDrainPerPump = maxResultsDrainPerPump;
        MaxWorkItemsPerWake = maxWorkItemsPerWake;
        WorkerIdle = workerIdleOptions?.Clone() ?? CreateIdleOptions(workerIdlePolicy);
        WorkerIdlePolicy = WorkerIdle.Policy;
    }

    public EcsExecutionMode ExecutionMode { get; }

    public string WorkerName { get; }

    public int MaxResultsDrainPerPump { get; }

    public int MaxWorkItemsPerWake { get; }

    public EcsWorkerIdlePolicy WorkerIdlePolicy { get; }

    public EcsWorkerIdleOptions WorkerIdle { get; }

    private static EcsWorkerIdleOptions CreateIdleOptions(EcsWorkerIdlePolicy policy)
    {
        return policy switch
        {
            EcsWorkerIdlePolicy.LowLatency => EcsWorkerIdleOptions.LowLatency(),
            EcsWorkerIdlePolicy.Balanced => EcsWorkerIdleOptions.Balanced(),
            EcsWorkerIdlePolicy.PowerSaving => EcsWorkerIdleOptions.PowerSaving(),
            EcsWorkerIdlePolicy.Manual => EcsWorkerIdleOptions.Balanced(),
            _ => EcsWorkerIdleOptions.AdaptiveBalanced()
        };
    }
}
