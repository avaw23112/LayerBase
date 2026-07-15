using LayerBase.Layers;

namespace LayerBase.Scope;

internal delegate void LifecycleInvoker();

internal delegate void UpdateInvoker(float deltaTime);

internal delegate void FixedUpdateInvoker(float fixedDeltaTime);

internal enum ScopeRuntimeState
{
    Created = 0,
    Running = 1,
    StopRequested = 2,
    Stopping = 3,
    Stopped = 4,
    Disposing = 5,
    Disposed = 6,
    Faulted = 7
}

internal enum ScopeThreadingMode
{
    Main = 0,
    Inline = 1,
    Worker = 2
}

internal enum ScopeClockMode
{
    RuntimePump = 0,
    Manual = 1,
    FixedRate = 2
}

internal readonly struct ScopeOptions
{
    public ScopeOptions(
        ScopeThreadingMode threading,
        ScopeClockMode clock,
        int tickRateHz,
        ScopeFaultPolicy faultPolicy = ScopeFaultPolicy.ReportAndContinue)
    {
        if (tickRateHz < 0)
            throw new ArgumentOutOfRangeException(nameof(tickRateHz));

        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        FaultPolicy = faultPolicy;
    }

    public ScopeThreadingMode Threading { get; }

    public ScopeClockMode Clock { get; }

    public int TickRateHz { get; }

    public ScopeFaultPolicy FaultPolicy { get; }

    public static ScopeOptions Main { get; } = new(ScopeThreadingMode.Main, ScopeClockMode.RuntimePump, 0);

    public static ScopeOptions Inline { get; } = new(ScopeThreadingMode.Inline, ScopeClockMode.RuntimePump, 0);

    public static ScopeOptions Worker(int tickRateHz = 60)
    {
        return new ScopeOptions(ScopeThreadingMode.Worker, ScopeClockMode.FixedRate, tickRateHz);
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

    public static ScopeLifecyclePlan Build(IReadOnlyList<Layer> layers)
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

    public void PumpUpdate(float deltaTime)
    {
        for (int layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
        {
            var slice = Layers[layerIndex];
            int end = slice.UpdateStart + slice.UpdateCount;
            for (int i = slice.UpdateStart; i < end; i++)
                Update[i](deltaTime);
        }
    }

    public void PumpFixedUpdate(float fixedDeltaTime)
    {
        for (int layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
        {
            var slice = Layers[layerIndex];
            int end = slice.FixedUpdateStart + slice.FixedUpdateCount;
            for (int i = slice.FixedUpdateStart; i < end; i++)
                FixedUpdate[i](fixedDeltaTime);
        }
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
