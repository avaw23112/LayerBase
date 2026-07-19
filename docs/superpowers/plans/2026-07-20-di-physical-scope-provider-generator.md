# DI Physical Scope Provider And Generator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make DI use one physical, owner-thread-only provider per Scope, and update the source generator so generated registrations and mount injection honor Scope ownership without hidden MainScope fallback.

**Architecture:** Build produces immutable service metadata in a `ServiceCatalog`, then compiles one immutable `ScopeServicePlan` per Scope. Each `ScopeRuntime` receives or owns exactly one `ScopeServiceProvider`, and that provider stores scope-local slots, instances, and disposable resources. `LayerServiceGenerator` remains the source of auto-registration metadata, but its generated code must emit owner-scope information and scope-safe mount injection paths instead of relying on root-provider default lookup.

**Tech Stack:** C# / .NET, NUnit, Roslyn incremental source generator.

## Global Constraints

- Scope is the physical thread and resource boundary.
- Scope mutable resources and state are owner-thread only.
- Cross-Scope interaction is only via ScopeEvent or ScopeCall.
- DI hot path must not use locks or `ConcurrentDictionary`.
- WorkerScope must not fall back to MainScope service lookup.
- Do not introduce compatibility adapters or obsolete forwarding APIs.
- Task 10 CI/NuGet/performance gates are explicitly out of scope for this pass.

---

## File Structure

- Modify `LayerBase/DI/ServiceProvider.cs`: downgrade to build/runtime composition facade; remove service instance ownership from the root.
- Create `LayerBase/DI/ServiceCatalog.cs`: immutable descriptor catalog, dependency validation, per-scope plan compilation.
- Create `LayerBase/DI/ScopeServicePlan.cs`: immutable per-scope descriptors, slot table, and dependency plan.
- Create `LayerBase/DI/ScopeServiceSlot.cs`: stable slot ids for service types inside a plan.
- Modify `LayerBase/DI/ScopeServiceProvider.cs`: physical provider per Scope; owner-thread-only access; array-backed instances; scoped disposal.
- Modify `LayerBase/DI/ScopeOwnedResourceList.cs`: keep owner-thread-only reverse release and duplicate ownership checks.
- Modify `LayerBase/DI/IGeneratedMountInject.cs`: keep public shape only if possible; prefer passing a scoped provider without root fallback.
- Modify `LayerBase/Layer/Layer.cs`: compile catalog during build, bind per-scope providers during scope initialization, and resolve active services through explicit owner scope.
- Modify `LayerBase/Scope/ScopeRuntime.cs`: carry scope-local service provider or expose owner-thread binding hook without depending on `LayerRuntime`.
- Modify `LayerBase.Generator/LayerBase.Generator/LayerServiceGenerator.cs`: emit scope-aware service registrations and generated injection that uses explicit scope provider lookup.
- Test `LayerBase.Test/ScopeServiceIsolationTests.cs`: runtime isolation, owner-thread access, fallback removal, disposal.
- Test `LayerBase.Test/ScopeServiceDisposalTests.cs`: reverse order and duplicate ownership.
- Test `LayerBase.Test/LayerGeneratorContractTests.cs`: generated registration/injection code contains owner-scope metadata and does not generate unscoped `services.Get<T>()` for scope-owned mounts.

### Task 1: Runtime DI Isolation Tests

**Files:**
- Modify: `LayerBase.Test/ScopeServiceIsolationTests.cs`
- Modify: `LayerBase.Test/ScopeServiceDisposalTests.cs`

**Interfaces:**
- Consumes: existing `LayerRuntime`, `Layer`, `ScopeRuntimeHost`, `ScopeAttribute`, `OwnerLayerAttribute`, `MountAttribute`.
- Produces: failing tests that define physical scope provider behavior.

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void Worker_scope_cannot_fallback_to_main_service()
{
    using var runtime = BuildRuntimeWithMainAndWorkerServices();
    var workerLayer = runtime.GetLayer<ScopeServiceIsolationLayer>();

    Assert.Throws<InvalidOperationException>(
        () => workerLayer.ResolveWorkerOwnedServiceFromMainThread());
}

[Test]
public void Scope_A_dispose_does_not_touch_scope_B()
{
    using var runtime = BuildRuntimeWithTwoWorkerScopes();

    runtime.DisposeScopeServices(FirstWorkerScope.ScopeId);

    Assert.That(FirstWorkerDisposable.DisposeCount, Is.EqualTo(1));
    Assert.That(SecondWorkerDisposable.DisposeCount, Is.EqualTo(0));
}

[Test]
public void Resources_release_in_reverse_creation_order()
{
    using var runtime = BuildRuntimeWithDependentScopedServices();

    runtime.DisposeScopeServices(FirstWorkerScope.ScopeId);

    CollectionAssert.AreEqual(
        new[] { "child", "parent" },
        DisposeRecorder.Events);
}
```

- [ ] **Step 2: Run tests and verify red**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ScopeServiceIsolationTests|FullyQualifiedName~ScopeServiceDisposalTests"`

Expected: FAIL because worker scope still falls back to Main provider and disposal/provider ownership is still rooted in the shared service provider.

### Task 2: Generator Contract Tests

**Files:**
- Modify: `LayerBase.Test/LayerGeneratorContractTests.cs`
- Modify: `LayerBase.Generator/LayerBase.Generator.Tests` only if existing generator tests require direct assertions.

**Interfaces:**
- Consumes: `LayerServiceGenerator`.
- Produces: failing source-generation assertions for explicit owner-scope registration and scoped injection.

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void Generated_mount_injection_uses_explicit_scope_service_lookup()
{
    var generated = GenerateLayerServiceSource("""
        using LayerBase.DI;
        using LayerBase.DI.Options;
        using LayerBase.Layers;
        using LayerBase.Scope;

        public sealed class WorkerScopeDefinition : IScopeDefinition
        {
            public int ScopeId => 101;
            public string Name => "worker";
            public ScopeOptions Options => ScopeOptions.Worker();
        }

        [Scope<WorkerScopeDefinition>]
        [OwnerLayer(typeof(TestLayer))]
        public partial class WorkerService : IService { }

        public partial class TestLayer : Layer { }
        """);

    StringAssert.Contains("typeof(global::WorkerScopeDefinition)", generated);
    StringAssert.DoesNotContain("services.Get<global::WorkerService>()", generated);
}
```

- [ ] **Step 2: Run tests and verify red**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~LayerGeneratorContractTests"`

Expected: FAIL because current generated injection still uses unscoped `services.Get<T>()`.

### Task 3: Compile Immutable Service Plans

**Files:**
- Create: `LayerBase/DI/ServiceCatalog.cs`
- Create: `LayerBase/DI/ScopeServicePlan.cs`
- Create: `LayerBase/DI/ScopeServiceSlot.cs`
- Modify: `LayerBase/DI/ServiceProvider.cs`

**Interfaces:**
- Produces:
  - `internal sealed class ServiceCatalog`
  - `internal ScopeServicePlan GetPlan(int ownerScopeId)`
  - `internal bool TryGetPlan(int ownerScopeId, out ScopeServicePlan plan)`
  - `internal sealed class ScopeServicePlan`
  - `internal int GetSlot(Type serviceType)`

- [ ] **Step 1: Implement minimal catalog**

```csharp
internal sealed class ServiceCatalog
{
    private readonly Dictionary<int, ScopeServicePlan> _plans;

    public ServiceCatalog(IEnumerable<ServiceDescriptor> descriptors)
    {
        _plans = descriptors
            .GroupBy(static descriptor => descriptor.OwnerScopeId)
            .ToDictionary(
                static group => group.Key,
                static group => ScopeServicePlan.Compile(group.Key, group));
    }

    public ScopeServicePlan GetPlan(int ownerScopeId)
    {
        if (_plans.TryGetValue(ownerScopeId, out var plan))
            return plan;

        throw new InvalidOperationException(
            $"Scope service plan not found for scope {ownerScopeId}.");
    }
}
```

- [ ] **Step 2: Run targeted tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ScopeServiceIsolationTests"`

Expected: still FAIL until provider and Layer wiring are changed.

### Task 4: Physical ScopeServiceProvider

**Files:**
- Modify: `LayerBase/DI/ScopeServiceProvider.cs`
- Modify: `LayerBase/DI/ScopeOwnedResourceList.cs`
- Modify: `LayerBase/DI/ServiceLayerBinder.cs` only if binding needs explicit duplicate owner checks.

**Interfaces:**
- Produces:
  - `internal ScopeServiceProvider(ScopeRuntime owner, ScopeServicePlan plan, Layer ownerLayer)`
  - `public T Get<T>()`
  - `internal object? GetService(Type serviceType)`
  - owner-thread assertion before cached access and disposal.

- [ ] **Step 1: Implement array-backed provider**

```csharp
internal sealed class ScopeServiceProvider : IServiceProvider, IDisposable
{
    private readonly ScopeRuntime _owner;
    private readonly ScopeServicePlan _plan;
    private readonly object?[] _instances;
    private readonly ScopeOwnedResourceList _resources = new();

    public object? GetService(Type serviceType)
    {
        _owner.RequireOwnerThread();

        if (!_plan.TryGetSlot(serviceType, out int slot))
            return null;

        return _instances[slot] ?? CreateAtSlot(slot);
    }
}
```

- [ ] **Step 2: Run targeted tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ScopeServiceIsolationTests|FullyQualifiedName~ScopeServiceDisposalTests"`

Expected: PASS for runtime DI tests after Layer wiring is complete.

### Task 5: Layer And Scope Wiring

**Files:**
- Modify: `LayerBase/Layer/Layer.cs`
- Modify: `LayerBase/Layer/LayerChain.cs`
- Modify: `LayerBase/Application/LayerRuntime.cs`
- Modify: `LayerBase/Scope/ScopeRuntime.cs`
- Modify: `LayerBase/Scope/ScopeRuntimeHost.cs`

**Interfaces:**
- Produces:
  - explicit per-scope provider creation on the scope owner path.
  - no root default fallback for scope-owned services.
  - dispose of only the matching scope provider.

- [ ] **Step 1: Route service creation through explicit scope plan**

```csharp
internal void InitializeScopeServices(ScopeRuntime scope)
{
    ScopeServicePlan plan = _serviceCatalog.GetPlan(scope.ScopeId);
    ScopeServiceProvider provider =
        new(scope, plan, this);

    scope.BindServiceProvider(provider);
    provider.InitializeEagerServices();
}
```

- [ ] **Step 2: Run targeted tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ScopeServiceIsolationTests|FullyQualifiedName~ScopeLifecycleOwnershipTests"`

Expected: PASS.

### Task 6: Generator Scope-Aware Output

**Files:**
- Modify: `LayerBase.Generator/LayerBase.Generator/LayerServiceGenerator.cs`
- Modify: `LayerBase/DI/IGeneratedMountInject.cs` if scoped injection requires a narrower provider interface.
- Modify: `LayerBase.Test/LayerGeneratorContractTests.cs`

**Interfaces:**
- Produces generated code that registers owner-scope metadata and avoids unscoped root `Get<T>()` for non-main scoped services.

- [ ] **Step 1: Emit explicit scoped lookup**

```csharp
this._workerService =
    services.GetRequiredScoped<global::WorkerService>(
        global::WorkerScopeDefinition.ScopeId);
```

Use the actual runtime helper name chosen in Tasks 3-5; do not preserve unscoped lookup for scoped mounts.

- [ ] **Step 2: Run generator contract tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~LayerGeneratorContractTests|FullyQualifiedName~ScopeDefinitionGeneratorTests"`

Expected: PASS.

### Task 7: Static Gates And Cleanup

**Files:**
- Modify only files touched above.

**Interfaces:**
- Produces clean dependency direction for DI and generator.

- [ ] **Step 1: Run static checks**

Run:

```powershell
Select-String -Path LayerBase\DI\*.cs -Pattern "ConcurrentDictionary|lock \(|ReaderWriterLockSlim"
Select-String -Path LayerBase\DI\ServiceProvider.cs -Pattern "LayerRuntime|ScopeRuntimeHost"
Select-String -Path LayerBase.Generator\LayerBase.Generator\LayerServiceGenerator.cs -Pattern "services.Get<"
```

Expected: no DI hot-path locks/concurrent maps, no ServiceProvider dependence on `LayerRuntime`, and no generated unscoped `services.Get<T>()` for scope-owned service injection.

- [ ] **Step 2: Run focused verification**

Run:

```powershell
dotnet build LayerBase.sln -c Debug
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ScopeServiceIsolationTests|FullyQualifiedName~ScopeServiceDisposalTests|FullyQualifiedName~LayerGeneratorContractTests|FullyQualifiedName~ScopeDefinitionGeneratorTests"
```

Expected: PASS.
