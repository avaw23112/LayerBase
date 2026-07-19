using System.Runtime.CompilerServices;
using LayerBase.Layers;
using LayerBase.ECS;

namespace LayerBase.Scope;

internal delegate void LifecycleInvoker();

internal delegate void UpdateInvoker(float deltaTime);

internal delegate void FixedUpdateInvoker(float fixedDeltaTime);

public enum ScopeRuntimeState
{
    Created = 0,
    Ready = 1,
    Running = 2,
    StopRequested = 3,
    Draining = 4,
    Stopped = 5,
    Disposing = 6,
    Disposed = 7,
    Faulted = 8
}

internal enum ScopeDrainPhase : byte
{
    None,
    ClosingIngress,
    DrainingAcceptedWork,
    SealingLocalProducers,
    RunningRuntimeStop,
    WaitingForWorkerExit,
    Disposing,
    Completed,
    Faulted
}

internal readonly struct ScopeDrainSnapshot
{
    public ScopeDrainSnapshot(
        int eventCount,
        int callCount,
        int completionCount,
        int postCount,
        int continuationCount,
        int workerJobCount,
        int asyncOperationCount)
    {
        EventCount = eventCount;
        CallCount = callCount;
        CompletionCount = completionCount;
        PostCount = postCount;
        ContinuationCount = continuationCount;
        WorkerJobCount = workerJobCount;
        AsyncOperationCount = asyncOperationCount;
    }

    public int EventCount { get; }

    public int CallCount { get; }

    public int CompletionCount { get; }

    public int PostCount { get; }

    public int ContinuationCount { get; }

    public int WorkerJobCount { get; }

    public int AsyncOperationCount { get; }

    public bool IsEmpty =>
        EventCount == 0 &&
        CallCount == 0 &&
        CompletionCount == 0 &&
        PostCount == 0 &&
        ContinuationCount == 0 &&
        WorkerJobCount == 0 &&
        AsyncOperationCount == 0;
}

public enum ScopeSafePointState : byte
{
    Running = 0,
    Requesting = 1,
    Frozen = 2,
    Restoring = 3,
    Releasing = 4,
    Faulted = 5
}

public enum ScopeThreadingMode
{
    Main = 0,
    Inline = 1,
    Worker = 2
}

public enum ScopeClockMode
{
    RuntimePump = 0,
    Manual = 1,
    FixedRate = 2
}

public enum ScopeTickOverrunPolicy : byte
{
    Skip = 0,
    CatchUpLimited = 1
}

public readonly struct ScopeTickOptions
{
    public ScopeTickOptions(
        int rateHz,
        ScopeTickOverrunPolicy overrunPolicy,
        int maxCatchUpTicks)
    {
        if (rateHz < 0)
            throw new ArgumentOutOfRangeException(nameof(rateHz));

        if (overrunPolicy == ScopeTickOverrunPolicy.CatchUpLimited &&
            maxCatchUpTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCatchUpTicks));
        }

        if (overrunPolicy == ScopeTickOverrunPolicy.Skip &&
            maxCatchUpTicks != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCatchUpTicks));
        }

        RateHz = rateHz;
        OverrunPolicy = overrunPolicy;
        MaxCatchUpTicks = maxCatchUpTicks;
    }

    public int RateHz { get; }

    public ScopeTickOverrunPolicy OverrunPolicy { get; }

    public int MaxCatchUpTicks { get; }

    public bool IsEnabled => RateHz > 0;

    public static ScopeTickOptions None { get; } =
        new(0, ScopeTickOverrunPolicy.Skip, 0);
}

public readonly struct ScopeOptions
{
    public ScopeOptions(
        ScopeThreadingMode threading,
        ScopeClockMode clock,
        int tickRateHz,
        ScopeFaultPolicy faultPolicy = ScopeFaultPolicy.ReportAndContinue,
        EcsRuntimeOptions? ecsRuntime = null)
    {
        if (tickRateHz < 0)
            throw new ArgumentOutOfRangeException(nameof(tickRateHz));

        if (threading == ScopeThreadingMode.Worker &&
            clock != ScopeClockMode.FixedRate)
        {
            throw new ArgumentException(
                "Worker Scope must use FixedRate clock mode.",
                nameof(clock));
        }

        if (clock == ScopeClockMode.FixedRate && tickRateHz <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickRateHz),
                "FixedRate Scope requires a positive tick rate.");
        }

        if (clock != ScopeClockMode.FixedRate && tickRateHz != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickRateHz),
                "Only FixedRate Scope may declare a non-zero tick rate.");
        }

        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        Tick = tickRateHz > 0
            ? new ScopeTickOptions(tickRateHz, ScopeTickOverrunPolicy.Skip, 0)
            : ScopeTickOptions.None;
        FaultPolicy = faultPolicy;
        EcsRuntime = ecsRuntime ?? EcsRuntimeOptions.Default;
    }

    internal ScopeOptions(
        ScopeThreadingMode threading,
        ScopeClockMode clock,
        int tickRateHz,
        ScopeTickOptions tick,
        ScopeFaultPolicy faultPolicy,
        EcsRuntimeOptions? ecsRuntime)
    {
        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        Tick = tick;
        FaultPolicy = faultPolicy;
        EcsRuntime = ecsRuntime ?? EcsRuntimeOptions.Default;
    }

    public ScopeThreadingMode Threading { get; }

    public ScopeClockMode Clock { get; }

    internal ScopeTickOptions Tick { get; }

    public int TickRateHz { get; }

    public ScopeFaultPolicy FaultPolicy { get; }

    public EcsRuntimeOptions EcsRuntime { get; }

    public static ScopeOptions Main { get; } = new(
        ScopeThreadingMode.Main,
        ScopeClockMode.RuntimePump,
        tickRateHz: 0);

    public static ScopeOptions Inline { get; } = new(
        ScopeThreadingMode.Inline,
        ScopeClockMode.RuntimePump,
        tickRateHz: 0);

    public static ScopeOptions Manual(
        ScopeFaultPolicy faultPolicy = ScopeFaultPolicy.ReportAndContinue,
        EcsRuntimeOptions? ecsRuntime = null)
    {
        return new ScopeOptions(
            ScopeThreadingMode.Inline,
            ScopeClockMode.Manual,
            tickRateHz: 0,
            faultPolicy: faultPolicy,
            ecsRuntime: ecsRuntime);
    }

    public static ScopeOptions Worker(
        int tickRateHz = 60,
        ScopeFaultPolicy faultPolicy = ScopeFaultPolicy.ReportAndContinue,
        EcsRuntimeOptions? ecsRuntime = null)
    {
        return new ScopeOptions(
            ScopeThreadingMode.Worker,
            ScopeClockMode.FixedRate,
            tickRateHz: tickRateHz,
            faultPolicy: faultPolicy,
            ecsRuntime: ecsRuntime);
    }

    public ScopeOptions WithEcsRuntime(EcsRuntimeOptions ecsRuntime)
    {
        return new ScopeOptions(
            Threading,
            Clock,
            TickRateHz,
            Tick,
            FaultPolicy,
            ecsRuntime);
    }
}

internal sealed class ScopeExecutionPlan
{
    public ScopeExecutionPlan(
        ScopeDescriptor descriptor,
        ScopeOptions options,
        LayerProviderRuntime[]? layerProviders = null,
        ScopeLayerSlice[]? layerSlices = null,
        ScopeLifecyclePlan? lifecyclePlan = null)
    {
        Descriptor = descriptor;
        Options = options;
        LayerProviders = layerProviders ?? LayerProviderRuntime.Empty;
        LayerSlices = layerSlices ?? Array.Empty<ScopeLayerSlice>();
        LifecyclePlan = lifecyclePlan ?? ScopeLifecyclePlan.Empty;
    }

    public ScopeDescriptor Descriptor { get; }

    public ScopeOptions Options { get; }

    public LayerProviderRuntime[] LayerProviders { get; }

    public ScopeLayerSlice[] LayerSlices { get; }

    public ScopeLifecyclePlan LifecyclePlan { get; }

    public static ScopeExecutionPlan CreateMain()
    {
        return new ScopeExecutionPlan(
            new ScopeDescriptor(ScopeDefinitionIds.Main, nameof(MainScope), typeof(MainScope)),
            ScopeOptions.Main);
    }
}

internal readonly struct ScopeDescriptor
{
    public ScopeDescriptor(int scopeId, string name, Type scopeType)
    {
        if (scopeId < 0)
            throw new ArgumentOutOfRangeException(nameof(scopeId));

        ScopeId = scopeId;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Scope name is required.", nameof(name)) : name;
        ScopeType = scopeType ?? throw new ArgumentNullException(nameof(scopeType));
    }

    public int ScopeId { get; }

    public string Name { get; }

    public Type ScopeType { get; }
}

internal readonly struct ScopeLayerSlice
{
    public ScopeLayerSlice(int layerIndex)
    {
        if (layerIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));

        LayerIndex = layerIndex;
    }

    public int LayerIndex { get; }
}

internal readonly struct ScopeLayerLifecycleSlice
{
    public ScopeLayerLifecycleSlice(
        int layerIndex,
        int initializeStart,
        int initializeCount,
        int postBuildStart,
        int postBuildCount,
        int runtimeStartStart,
        int runtimeStartCount,
        int updateStart,
        int updateCount,
        int fixedUpdateStart,
        int fixedUpdateCount,
        int runtimeStopStart,
        int runtimeStopCount,
        int disposeStart,
        int disposeCount)
    {
        LayerIndex = layerIndex;
        InitializeStart = initializeStart;
        InitializeCount = initializeCount;
        PostBuildStart = postBuildStart;
        PostBuildCount = postBuildCount;
        RuntimeStartStart = runtimeStartStart;
        RuntimeStartCount = runtimeStartCount;
        UpdateStart = updateStart;
        UpdateCount = updateCount;
        FixedUpdateStart = fixedUpdateStart;
        FixedUpdateCount = fixedUpdateCount;
        RuntimeStopStart = runtimeStopStart;
        RuntimeStopCount = runtimeStopCount;
        DisposeStart = disposeStart;
        DisposeCount = disposeCount;
    }

    public int LayerIndex { get; }
    public int InitializeStart { get; }
    public int InitializeCount { get; }
    public int PostBuildStart { get; }
    public int PostBuildCount { get; }
    public int RuntimeStartStart { get; }
    public int RuntimeStartCount { get; }
    public int UpdateStart { get; }
    public int UpdateCount { get; }
    public int FixedUpdateStart { get; }
    public int FixedUpdateCount { get; }
    public int RuntimeStopStart { get; }
    public int RuntimeStopCount { get; }
    public int DisposeStart { get; }
    public int DisposeCount { get; }
}

internal sealed class LayerProviderRuntime
{
    private LayerProviderRuntime()
    {
    }

    public static LayerProviderRuntime[] Empty { get; } = Array.Empty<LayerProviderRuntime>();
}

internal sealed class ScopeLifecyclePlan
{
    public ScopeLifecyclePlan(
        ScopeLayerLifecycleSlice[] layers,
        LifecycleInvoker[] initialize,
        LifecycleInvoker[] postBuild,
        LifecycleInvoker[] runtimeStart,
        UpdateInvoker[] update,
        FixedUpdateInvoker[] fixedUpdate,
        LifecycleInvoker[] runtimeStop,
        LifecycleInvoker[] dispose)
    {
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
        Initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
        PostBuild = postBuild ?? throw new ArgumentNullException(nameof(postBuild));
        RuntimeStart = runtimeStart ?? throw new ArgumentNullException(nameof(runtimeStart));
        Update = update ?? throw new ArgumentNullException(nameof(update));
        FixedUpdate = fixedUpdate ?? throw new ArgumentNullException(nameof(fixedUpdate));
        RuntimeStop = runtimeStop ?? throw new ArgumentNullException(nameof(runtimeStop));
        Dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
    }

    public ScopeLayerLifecycleSlice[] Layers { get; }

    public LifecycleInvoker[] Initialize { get; }

    public LifecycleInvoker[] PostBuild { get; }

    public LifecycleInvoker[] RuntimeStart { get; }

    public UpdateInvoker[] Update { get; }

    public FixedUpdateInvoker[] FixedUpdate { get; }

    public LifecycleInvoker[] RuntimeStop { get; }

    public LifecycleInvoker[] Dispose { get; }

    public static ScopeLifecyclePlan Empty { get; } = new(
        Array.Empty<ScopeLayerLifecycleSlice>(),
        Array.Empty<LifecycleInvoker>(),
        Array.Empty<LifecycleInvoker>(),
        Array.Empty<LifecycleInvoker>(),
        Array.Empty<UpdateInvoker>(),
        Array.Empty<FixedUpdateInvoker>(),
        Array.Empty<LifecycleInvoker>(),
        Array.Empty<LifecycleInvoker>());

    public static ScopeLifecyclePlan EmptyForLayerIndexes(IEnumerable<int> layerIndexes)
    {
        if (layerIndexes == null)
            throw new ArgumentNullException(nameof(layerIndexes));

        var layers = layerIndexes
            .Select(static layerIndex => new ScopeLayerLifecycleSlice(
                layerIndex,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0))
            .ToArray();

        return new ScopeLifecyclePlan(
            layers,
            Array.Empty<LifecycleInvoker>(),
            Array.Empty<LifecycleInvoker>(),
            Array.Empty<LifecycleInvoker>(),
            Array.Empty<UpdateInvoker>(),
            Array.Empty<FixedUpdateInvoker>(),
            Array.Empty<LifecycleInvoker>(),
            Array.Empty<LifecycleInvoker>());
    }

    public static ScopeLifecyclePlan Build(IReadOnlyList<Layer> layers, int ownerScopeId)
    {
        if (layers == null)
            throw new ArgumentNullException(nameof(layers));

        var slices = new ScopeLayerLifecycleSlice[layers.Count];
        var initialize = new List<LifecycleInvoker>();
        var postBuild = new List<LifecycleInvoker>();
        var runtimeStart = new List<LifecycleInvoker>();
        var update = new List<UpdateInvoker>();
        var fixedUpdate = new List<FixedUpdateInvoker>();
        var runtimeStop = new List<LifecycleInvoker>();
        var dispose = new List<LifecycleInvoker>();

        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            slices[i] = layer.AppendScopeLifecycle(
                ownerScopeId,
                initialize,
                postBuild,
                runtimeStart,
                update,
                fixedUpdate,
                runtimeStop,
                dispose);
        }

        return new ScopeLifecyclePlan(
            slices,
            initialize.ToArray(),
            postBuild.ToArray(),
            runtimeStart.ToArray(),
            update.ToArray(),
            fixedUpdate.ToArray(),
            runtimeStop.ToArray(),
            dispose.ToArray());
    }

    public void RunInitialize()
    {
        for (int layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
        {
            var slice = Layers[layerIndex];
            int end = slice.InitializeStart + slice.InitializeCount;
            for (int i = slice.InitializeStart; i < end; i++)
                Initialize[i]();
        }
    }

    public void RunPostBuild()
    {
        for (int layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
        {
            var slice = Layers[layerIndex];
            int end = slice.PostBuildStart + slice.PostBuildCount;
            for (int i = slice.PostBuildStart; i < end; i++)
                PostBuild[i]();
        }
    }

    public void RunRuntimeStart()
    {
        for (int layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
        {
            var slice = Layers[layerIndex];
            int end = slice.RuntimeStartStart + slice.RuntimeStartCount;
            for (int i = slice.RuntimeStartStart; i < end; i++)
                RuntimeStart[i]();
        }
    }

    public bool HasUpdate => Update.Length != 0;

    public bool HasFixedUpdate => FixedUpdate.Length != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PumpUpdate(float deltaTime)
    {
        UpdateInvoker[] invokers = Update;

        for (int i = 0; i < invokers.Length; i++)
            invokers[i](deltaTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PumpFixedUpdate(float fixedDeltaTime)
    {
        FixedUpdateInvoker[] invokers = FixedUpdate;

        for (int i = 0; i < invokers.Length; i++)
            invokers[i](fixedDeltaTime);
    }

    public void RunRuntimeStopReverse()
    {
        for (int layerIndex = Layers.Length - 1; layerIndex >= 0; layerIndex--)
        {
            var slice = Layers[layerIndex];
            int end = slice.RuntimeStopStart + slice.RuntimeStopCount;
            for (int i = slice.RuntimeStopStart; i < end; i++)
                RuntimeStop[i]();
        }
    }

    public void DisposeReverse()
    {
        for (int layerIndex = Layers.Length - 1; layerIndex >= 0; layerIndex--)
        {
            var slice = Layers[layerIndex];
            int end = slice.DisposeStart + slice.DisposeCount;
            for (int i = slice.DisposeStart; i < end; i++)
                Dispose[i]();
        }
    }
}
