## Task 3：为每个 Scope 构建并执行独立生命周期计划

当前所有 Scope 初始持有空生命周期计划，但 `LayerChain.Build()` 只把真实计划安装给 MainScope。

### Files

* Modify: `LayerBase/Layer/Layer.cs`
* Modify: `LayerBase/Layer/LayerChain.cs`
* Modify: `LayerBase/DI/ServiceProvider.cs`
* Modify: `LayerBase/Scope/ScopeRuntimeModel.cs`
* Modify: `LayerBase/Scope/ScopeRuntime.cs`
* Modify: `LayerBase/Scope/ScopeRuntimeHost.cs`
* Modify: `LayerBase/Application/LayerRuntime.cs`
* Create: `LayerBase/Layer/ScopeLayerLifecycleState.cs`
* Create: `LayerBase.Test/ScopeLifecycleOwnershipTests.cs`

### Required behavior

* Service 生命周期根据 `ServiceDescriptor.OwnerScopeId` 分组。该字段已经存在，不得重新推断。
* MainScope 服务生命周期在 Main 线程执行。
* Inline Scope 生命周期在 Runtime Pump 线程执行。
* Worker Scope 生命周期在 Worker 自己的线程执行。
* Initialize、PostBuild、RuntimeStart、Update、FixedUpdate、RuntimeStop、Dispose 都遵循 Scope 归属。
* Layer 自身生命周期只属于 MainScope。
* 单独 Dispose Secondary Scope 时，只释放该 Scope 的服务和订阅，不释放整个 Layer。

### Current architecture problem

Currently:
1. `Layer.LifecycleBuild()` stores ALL lifecycle services (IInitializable, IUpdate, etc.) in per-layer flat lists like `_initializables`, `_serviceUpdates`, regardless of their OwnerScopeId.
2. `ScopeLifecyclePlan.Build(IReadOnlyList<Layer> layers)` creates ONE flat list for ALL scopes combined.
3. `LayerChain.Build()` calls `ScopeLifecyclePlan.Build(builtLayers)` and installs the single plan on MainScope only.
4. When disposing, `LayerChain.DisposeLayers()` calls `_owner.ScopeHost.MainScope.RunLifecycleDispose()` which disposes only MainScope services.

### Step 1: Write failing test

Create `LayerBase.Test/ScopeLifecycleOwnershipTests.cs`:

```csharp
[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeLifecycleOwnershipTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Disposing_secondary_scope_disposes_only_secondary_services()
    {
        var serviceDisposeOrder = new List<string>();

        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new OwnerLayer(serviceDisposeOrder))
            .Build();

        Assert.That(runtime.ScopeHost.Scopes, Has.Count.EqualTo(2));

        // Dispose secondary scope - should only dispose scope services, not layer
        runtime.ScopeHost.Scopes[1].Dispose();

        Assert.That(serviceDisposeOrder, Is.EquivalentTo(new[] { "Secondary" }));
    }

    [Test]
    public void Runtime_stop_runs_each_scope_services_exactly_once()
    {
        var runtimeStops = new List<string>();

        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new OwnerLayer(runtimeStops))
            .Build();

        runtime.Dispose();

        // Each scope's services should be stopped exactly once
        Assert.That(runtimeStops.Count, Is.EqualTo(2));
        Assert.That(runtimeStops, Contains.Item("Main"));
        Assert.That(runtimeStops, Contains.Item("Secondary"));
    }

    private sealed class MainService : IInitializable, IRuntimeStop, IDisposable
    {
        private readonly List<string> _log;
        public MainService(List<string> log) => _log = log;
        public void Initialize() { }
        public void RuntimeStop() => _log.Add("Main");
        public void Dispose() => _log.Add("MainDispose");
    }

    private sealed class SecondaryService : IInitializable, IRuntimeStop, IDisposable
    {
        private readonly List<string> _log;
        public SecondaryService(List<string> log) => _log = log;
        public void Initialize() { }
        public void RuntimeStop() => _log.Add("Secondary");
        public void Dispose() => _log.Add("SecondaryDispose");
    }

    private sealed class OwnerLayer : Layer
    {
        private readonly List<string> _log;
        public OwnerLayer(List<string> log) { _log = log; }

        public override void ConfigureServices(ServiceCollection services)
        {
            services.Add(MainServiceDescriptor.ForTypes(
                typeof(MainService), typeof(MainService),
                ownerScopeType: typeof(MainScope)));
            services.Add(ServiceDescriptor.ForTypes(
                typeof(SecondaryService), typeof(SecondaryService),
                ownerScopeType: typeof(SecondaryScope)));
        }

        public override GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 777,
                    identity: "scope:test:SecondaryScope",
                    scopeType: typeof(SecondaryScope),
                    factory: static () => new SecondaryScope())
            };
        }
    }

    private sealed class SecondaryScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
```

Note: `ServiceDescriptor.ForTypes` might not exist - check the actual API. It's `ServiceDescriptor.Singleton<TService, TImpl>()`, etc. Use `new ServiceDescriptor(typeof(MainService), typeof(MainService), ServiceLifetime.Singleton, null, null, ownerScopeId: ScopeDefinitionIds.Main)` or equivalent.

### Step 2: Confirm test failure

Expected: Disposing secondary scope disposes ALL services or the layer itself.

### Step 3: Implement ScopeLayerLifecycleState

Create `LayerBase/Layer/ScopeLayerLifecycleState.cs`:

```csharp
namespace LayerBase.Layers;

internal sealed class ScopeLayerLifecycleState
{
    public readonly List<IInitializable> Initializables = new();
    public readonly List<IPostBuild> PostBuilds = new();
    public readonly List<IRuntimeStart> RuntimeStarts = new();
    public readonly List<IUpdate> Updates = new();
    public readonly List<IFixedUpdate> FixedUpdates = new();
    public readonly List<IRuntimeStop> RuntimeStops = new();
    public readonly List<IDisposable> Disposables = new();

    public void RunInitialize()
    {
        for (int i = 0; i < Initializables.Count; i++)
            Initializables[i].Initialize();
    }

    public void RunPostBuild()
    {
        for (int i = 0; i < PostBuilds.Count; i++)
            PostBuilds[i].PostBuild();
    }

    public void RunRuntimeStart()
    {
        for (int i = 0; i < RuntimeStarts.Count; i++)
            RuntimeStarts[i].RuntimeStart();
    }

    public void PumpUpdate(float deltaTime)
    {
        for (int i = 0; i < Updates.Count; i++)
            Updates[i].Update();
    }

    public void PumpFixedUpdate(float fixedDeltaTime)
    {
        for (int i = 0; i < FixedUpdates.Count; i++)
            FixedUpdates[i].FixedUpdate(fixedDeltaTime);
    }

    public void RunRuntimeStop()
    {
        for (int i = RuntimeStops.Count - 1; i >= 0; i--)
            RuntimeStops[i].RuntimeStop();
    }

    public void RunDispose()
    {
        for (int i = Disposables.Count - 1; i >= 0; i--)
            Disposables[i].Dispose();
    }
}
```

### Step 4: Modify Layer.cs

Replace:
```csharp
private readonly List<IInitializable> _initializables = new();
private readonly List<IUpdate> _serviceUpdates = new();
// ... etc.
```

With:
```csharp
private readonly Dictionary<int, ScopeLayerLifecycleState> _lifecycleByScopeId = new();
```

In `LifecycleBuild()`:
```csharp
internal void LifecycleBuild()
{
    foreach (var resolved in _resolvedServices)
    {
        int scopeId = resolved.Descriptor.OwnerScopeId;
        var state = GetOrCreateLifecycleState(scopeId);

        if (resolved.Instance is IInitializable init) state.Initializables.Add(init);
        if (resolved.Instance is IUpdate up) state.Updates.Add(up);
        if (resolved.Instance is IFixedUpdate fixedUpdate) state.FixedUpdates.Add(fixedUpdate);
        if (resolved.Instance is IPostBuild postBuild) state.PostBuilds.Add(postBuild);
        if (resolved.Instance is IRuntimeStart runtimeStart) state.RuntimeStarts.Add(runtimeStart);
        if (resolved.Instance is IRuntimeStop runtimeStop) state.RuntimeStops.Add(runtimeStop);
        if (resolved.Instance is IDisposable disposable) state.Disposables.Add(disposable);
    }

    // Layer self-implements lifecycle methods only belong to MainScope
    var mainState = GetOrCreateLifecycleState(ScopeDefinitionIds.Main);
    if (this is IFixedUpdate layerFixedUpdate) mainState.FixedUpdates.Add(layerFixedUpdate);
    if (this is IInitializable layerInitializable) mainState.Initializables.Add(layerInitializable);
    if (this is IPostBuild layerPostBuild) mainState.PostBuilds.Add(layerPostBuild);
    if (this is IRuntimeStart layerRuntimeStart) mainState.RuntimeStarts.Add(layerRuntimeStart);
    if (this is IRuntimeStop layerRuntimeStop) mainState.RuntimeStops.Add(layerRuntimeStop);
}
```

Add:
```csharp
private ScopeLayerLifecycleState GetOrCreateLifecycleState(int scopeId)
{
    if (!_lifecycleByScopeId.TryGetValue(scopeId, out var state))
    {
        state = new ScopeLayerLifecycleState();
        _lifecycleByScopeId[scopeId] = state;
    }
    return state;
}
```

Change `AppendScopeLifecycle` to accept `ownerScopeId` and only append that scope's lifecycle:

```csharp
internal ScopeLayerLifecycleSlice AppendScopeLifecycle(
    int ownerScopeId,
    List<LifecycleInvoker> initialize,
    List<LifecycleInvoker> postBuild,
    List<LifecycleInvoker> runtimeStart,
    List<UpdateInvoke> update,
    List<FixedUpdateInvoke> fixedUpdate,
    List<LifecycleInvoker> runtimeStop,
    List<LifecycleInvoker> dispose)
{
    if (!_lifecycleByScopeId.TryGetValue(ownerScopeId, out var state))
        return CreateEmptySlice();

    var initializeStart = initialize.Count;
    // ... similar to current but using state instead of _initializables
    if (state.Initializables.Count > 0)
        initialize.Add(state.RunInitialize);
    if (state.PostBuilds.Count > 0)
        postBuild.Add(state.RunPostBuild);
    if (state.RuntimeStarts.Count > 0)
        runtimeStart.Add(state.RunRuntimeStart);
    if (state.Updates.Count > 0)
        update.Add(state.PumpUpdate);
    if (state.FixedUpdates.Count > 0)
        fixedUpdate.Add(state.PumpFixedUpdate);
    if (state.RuntimeStops.Count > 0)
        runtimeStop.Add(state.RunRuntimeStop);
    if (state.Disposables.Count > 0)
        dispose.Add(state.RunDispose);

    // Layer's own Pump still runs (for main scope logic)
    if (ownerScopeId == ScopeDefinitionIds.Main && HasActiveLogic)
        update.Add(Pump);

    return new ScopeLayerLifecycleSlice(RouteIndex, ...);
}
```

Add `DisposeScopeResources(int ownerScopeId)` to Layer:

```csharp
internal void DisposeScopeResources(int ownerScopeId)
{
    if (_lifecycleByScopeId.TryGetValue(ownerScopeId, out var state))
    {
        state.RunRuntimeStop();
        state.RunDispose();
    }
}
```

### Step 5: Modify ScopeLifecyclePlan.Build

Change signature to accept ownerScopeId:

```csharp
public static ScopeLifecyclePlan Build(
    IReadOnlyList<Layer> layers,
    int ownerScopeId)
```

Inside, call `layer.AppendScopeLifecycle(ownerScopeId, ...)` instead of `layer.AppendScopeLifecycle(...)`.

### Step 6: Modify ServiceProvider.cs

Add `DisposeScope(int ownerScopeId)`:

```csharp
internal void DisposeScope(int ownerScopeId)
{
    var keysToRemove = new List<ServiceKey>();
    foreach (var kvp in _instances)
    {
        if (kvp.Key.OwnerScopeId == ownerScopeId)
        {
            if (kvp.Value.IsValueCreated && kvp.Value.Value is IDisposable disposable)
                disposable.Dispose();
            keysToRemove.Add(kvp.Key);
        }
    }
    foreach (var key in keysToRemove)
        _instances.TryRemove(key, out _);
}
```

Also add `OwnerScopeId` property to `ServiceKey`:
```csharp
public int OwnerScopeId => _ownerScopeId;
```

### Step 7: Modify LayerChain.Build

Instead of creating one lifecycle plan, create per-scope plans:

```csharp
internal void Build(int eventStateSlabSize, bool releaseMode, Action? afterPostBuild = null)
{
    // ... existing code up to LifecycleBuild calls ...

    var builtLayers = new List<Layer>();
    foreach (var node in _responsibilityChain)
        if (node is Layer layer)
            builtLayers.Add(layer);

    SharedFieldBinder.Bind(builtLayers.SelectMany(static layer => layer.GetSharedFieldParticipants()));

    foreach (var layer in builtLayers)
    {
        layer.LifecycleBuild();
        if (layer.HasDelayPublisher) _hasDelayMask |= 1UL << layer.RouteIndex;
    }

    // Install per-scope lifecycle plans
    var compositionScopes = _owner.CompositionPlan.Scopes;
    for (int i = 0; i < compositionScopes.Length; i++)
    {
        var scopePlan = compositionScopes[i];
        int scopeId = scopePlan.Descriptor.ScopeId;
        var lifecylePlan = ScopeLifecyclePlan.Build(builtLayers, scopeId);
        if (_owner.ScopeHost.TryGetRuntime(scopeId, out var scopeRuntime))
            scopeRuntime.SetLifecyclePlan(lifecylePlan);
    }

    EventGraphValidator.Validate(builtLayers, _owner);

    // Run barriers for all scopes
    foreach (var scope in _owner.ScopeHost.Scopes)
        scope.LifecyclePlan.RunInitialize();
    foreach (var scope in _owner.ScopeHost.Scopes)
        scope.LifecyclePlan.RunPostBuild();
    afterPostBuild?.Invoke();
    foreach (var scope in _owner.ScopeHost.Scopes)
        scope.LifecyclePlan.RunRuntimeStart();
}
```

### Step 8: Modify DisposeLayers in LayerChain

```csharp
internal void DisposeLayers()
{
    // Run RuntimeStop for all scopes in reverse
    var scopes = _owner.ScopeHost.Scopes;
    for (int i = scopes.Count - 1; i >= 0; i--)
        scopes[i].LifecyclePlan.RunRuntimeStopReverse();

    // Run Dispose for all scopes in reverse
    for (int i = scopes.Count - 1; i >= 0; i--)
        scopes[i].LifecyclePlan.DisposeReverse();
}
```

### Step 9: Verification

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ScopeLifecycleOwnershipTests"
```

Then run all Scope tests:
```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~Scope"
```

### Step 10: Commit

```bash
git add LayerBase/Layer \
        LayerBase/DI/ServiceProvider.cs \
        LayerBase/Scope \
        LayerBase/Application/LayerRuntime.cs \
        LayerBase.Test/ScopeLifecycleOwnershipTests.cs
git commit -m "fix(scope): execute lifecycle on owning scope"
```
