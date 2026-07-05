# Actor Method Lifecycle Tick Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add method-level lifecycle scheduling for Actors so a single actor can expose multiple `Update` / `LateUpdate` / `FixedUpdate` methods with independent `TickTier` values, while each executed method still consumes one `RuntimeFrameBudget` work unit.

**Architecture:** Keep interface-based lifecycle support intact and layer the new behavior on top of it. Generated actor metadata will expose lifecycle method descriptors; runtime storage will register them into new budget-aware method lanes; destroy paths will remove every method handle. The pump model remains single-threaded and continues to treat `RuntimeFrameBudget` as the unified work-unit budget.

**Tech Stack:** C# 12, .NET 8/9, NUnit, Roslyn incremental generator, BenchmarkDotNet

---

### Task 1: Lock the Generator Contract With Tests

**Files:**
- Modify: `LayerBase.Test/ActorGeneratorTests.cs`
- Test: `LayerBase.Test/LayerBase.Test.csproj`

- [ ] **Step 1: Add positive generator coverage for method lifecycle metadata**

```csharp
[Test]
public void Actor_lifecycle_method_attributes_generate_metadata_entries()
{
    GeneratorRunResult result = RunGenerator("""
        using LayerBase.Actor;

        namespace Sample;

        public sealed partial class EnemyActor : IActor
        {
            [ActorUpdate(TickTier.Hot)]
            private void Combat(float dt) { }

            [ActorLateUpdate(TickTier.Warm, Phase = 2)]
            private void Refresh(float dt) { }

            [ActorFixedUpdate(TickTier.Cold)]
            private void Sim(float dt) { }
        }
        """);

    Assert.That(GetGeneratorDiagnostics(result), Is.Empty);

    string generated = result.GeneratedSources.Single().SourceText.ToString();
    Assert.That(generated, Does.Contain("builder.AddLifecycleMethod("));
    Assert.That(generated, Does.Contain("global::LayerBase.Actor.ActorLifecyclePhase.Update"));
    Assert.That(generated, Does.Contain("global::LayerBase.Actor.TickTier.Warm"));
    Assert.That(generated, Does.Contain("Invoke_Lifecycle_Combat"));
}
```

- [ ] **Step 2: Add signature-validation coverage for the new attributes**

```csharp
[TestCase("LBACTOR301", """
    using LayerBase.Actor;
    public sealed partial class EnemyActor : IActor
    {
        [ActorUpdate]
        private static void Combat(float dt) { }
    }
    """)]
[TestCase("LBACTOR302", """
    using LayerBase.Actor;
    public sealed partial class EnemyActor : IActor
    {
        [ActorUpdate]
        private int Combat(float dt) => 0;
    }
    """)]
[TestCase("LBACTOR303", """
    using LayerBase.Actor;
    public sealed partial class EnemyActor : IActor
    {
        [ActorUpdate]
        private void Combat() { }
    }
    """)]
[TestCase("LBACTOR304", """
    using LayerBase.Actor;
    public sealed partial class EnemyActor : IActor
    {
        [ActorUpdate]
        private void Combat(float dt, float extra) { }
    }
    """)]
public void Invalid_actor_lifecycle_method_declarations_report_expected_diagnostic(string diagnosticId, string source)
{
    GeneratorRunResult result = RunGenerator(source);
    ImmutableArray<Diagnostic> diagnostics = GetGeneratorDiagnostics(result);

    Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId));
}
```

- [ ] **Step 3: Run the focused generator tests before implementation**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ActorGeneratorTests"`

Expected: FAIL because the new attributes, diagnostics, and generated metadata APIs do not exist yet.

### Task 2: Lock Runtime Registration and Budget Behavior With Tests

**Files:**
- Modify: `LayerBase.Test/ActorLifecycleTests.cs`
- Test: `LayerBase.Test/LayerBase.Test.csproj`

- [ ] **Step 1: Add a probe actor that uses only method-level lifecycle attributes**

```csharp
internal sealed partial class MethodLifecycleProbeActor : IActor
{
    [ActorUpdate(TickTier.Hot)]
    private void Combat(float dt)
    {
        ActorLifecycleTrace.Entries.Add($"method-hot:{dt:0.###}");
    }

    [ActorUpdate(TickTier.Warm)]
    private void Think(float dt)
    {
        ActorLifecycleTrace.Entries.Add($"method-warm:{dt:0.###}");
    }

    [ActorUpdate(TickTier.Cold)]
    private void Maintain(float dt)
    {
        ActorLifecycleTrace.Entries.Add($"method-cold:{dt:0.###}");
    }

    [ActorLateUpdate(TickTier.Hot)]
    private void RefreshLate(float dt)
    {
        ActorLifecycleTrace.Entries.Add($"method-late:{dt:0.###}");
    }
}
```

- [ ] **Step 2: Add registration and budget-cutoff tests**

```csharp
[Test]
public void Method_level_lifecycle_methods_are_registered_and_pumped()
{
    var world = new ActorWorld();
    world.CreateActor<MethodLifecycleProbeActor>();
    ActorLifecycleTrace.Entries.Clear();

    var budget = new RuntimeFrameBudget(16, 0, 0);
    world.Pump(0.5f, 0f, false, ref budget);

    Assert.That(
        ActorLifecycleTrace.Entries,
        Is.EqualTo(new[]
        {
            "method-hot:0.5",
            "method-warm:0.5",
            "method-cold:0.5",
            "method-late:0.5"
        }));
    Assert.That(budget.UsedEvents, Is.EqualTo(4));
}

[Test]
public void Method_level_lifecycle_respects_budget_and_prioritizes_hot_before_warm_and_cold()
{
    var world = new ActorWorld();
    world.CreateActor<MethodLifecycleProbeActor>();
    ActorLifecycleTrace.Entries.Clear();

    var budget = new RuntimeFrameBudget(1, 0, 0);
    world.Pump(0.5f, 0f, false, ref budget);

    Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "method-hot:0.5" }));
    Assert.That(budget.UsedEvents, Is.EqualTo(1));
}
```

- [ ] **Step 3: Add dormant and destroy-cleanup tests**

```csharp
[Test]
public void Dormant_lifecycle_methods_are_not_pumped_automatically()
{
    var world = new ActorWorld();
    world.CreateActor<DormantLifecycleProbeActor>();
    ActorLifecycleTrace.Entries.Clear();

    var budget = new RuntimeFrameBudget(8, 0, 0);
    world.Pump(0.25f, 0f, false, ref budget);

    Assert.That(ActorLifecycleTrace.Entries, Is.Empty);
    Assert.That(budget.UsedEvents, Is.EqualTo(0));
}
```

- [ ] **Step 4: Run the focused lifecycle tests before implementation**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ActorLifecycleTests"`

Expected: FAIL because method lifecycle registration and pumping are not implemented yet.

### Task 3: Add Public Method-Level Lifecycle Attribute Types

**Files:**
- Add: `LayerBase/Actor/Lifecycle/TickTier.cs`
- Add: `LayerBase/Actor/Lifecycle/ActorLifecyclePhase.cs`
- Add: `LayerBase/Actor/Lifecycle/ActorUpdateAttribute.cs`
- Add: `LayerBase/Actor/Lifecycle/ActorLateUpdateAttribute.cs`
- Add: `LayerBase/Actor/Lifecycle/ActorFixedUpdateAttribute.cs`

- [ ] **Step 1: Add the `TickTier` and phase enums**

```csharp
namespace LayerBase.Actor;

public enum TickTier
{
    Hot,
    Warm,
    Cold,
    Dormant
}

internal enum ActorLifecyclePhase
{
    Update,
    LateUpdate,
    FixedUpdate
}
```

- [ ] **Step 2: Add the three lifecycle method attributes**

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ActorUpdateAttribute : Attribute
{
    public TickTier Tier { get; }
    public int Phase { get; init; } = -1;

    public ActorUpdateAttribute(TickTier tier = TickTier.Hot)
    {
        Tier = tier;
    }
}
```

Repeat the same shape for `ActorLateUpdateAttribute` and `ActorFixedUpdateAttribute`.

### Task 4: Extend Actor Metadata to Carry Lifecycle Method Descriptors

**Files:**
- Add: `LayerBase/Actor/Lifecycle/ActorLifecycleMethodInvoker.cs`
- Add: `LayerBase/Actor/Lifecycle/ActorLifecycleMethodMeta.cs`
- Modify: `LayerBase/Actor/Meta/ActorTypeMeta.cs`
- Modify: `LayerBase/Actor/Meta/ActorTypeMetaBuilder.cs`

- [ ] **Step 1: Add the runtime metadata model**

```csharp
namespace LayerBase.Actor;

internal delegate void ActorLifecycleMethodInvoker(IActor actor, float deltaTime);

internal readonly struct ActorLifecycleMethodMeta
{
    public readonly ActorLifecyclePhase Phase;
    public readonly TickTier Tier;
    public readonly int TickPhase;
    public readonly ActorLifecycleMethodInvoker Invoker;

    public ActorLifecycleMethodMeta(
        ActorLifecyclePhase phase,
        TickTier tier,
        int tickPhase,
        ActorLifecycleMethodInvoker invoker)
    {
        Phase = phase;
        Tier = tier;
        TickPhase = tickPhase;
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
    }
}
```

- [ ] **Step 2: Thread lifecycle methods through actor metadata**

```csharp
public ActorLifecycleMethodMeta[] LifecycleMethods { get; }
```

Update `ActorTypeMeta<TActor>` constructor and `ActorTypeMetaBuilder.Build<TActor>()` so the built metadata includes `LifecycleMethods`.

- [ ] **Step 3: Add a builder API for generated code**

```csharp
public void AddLifecycleMethod(
    ActorLifecyclePhase phase,
    TickTier tier,
    int tickPhase,
    ActorLifecycleMethodInvoker invoker)
{
    if (invoker == null)
    {
        throw new ArgumentNullException(nameof(invoker));
    }

    _lifecycleMethods.Add(new ActorLifecycleMethodMeta(phase, tier, tickPhase, invoker));
}
```

### Task 5: Teach the Actor Generator to Emit Lifecycle Method Metadata

**Files:**
- Modify: `LayerBase.Generator/LayerBase.Generator/ActorBehaviourDiagnostics.cs`
- Modify: `LayerBase.Generator/LayerBase.Generator/ActorBehaviourGenerator.cs`

- [ ] **Step 1: Add lifecycle-specific diagnostics**

```csharp
public static readonly DiagnosticDescriptor LifecycleMethodMustBeInstance = new(
    id: "LBACTOR301",
    title: "Actor lifecycle method cannot be static",
    messageFormat: "Actor lifecycle method '{0}' cannot be static",
    category: "Usage",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);
```

Add matching descriptors for:
- `LBACTOR302`: must return `void`
- `LBACTOR303`: must have exactly one parameter
- `LBACTOR304`: parameter must be `float deltaTime`

- [ ] **Step 2: Scan for `[ActorUpdate]`, `[ActorLateUpdate]`, and `[ActorFixedUpdate]`**

Extend candidate extraction with:

```csharp
private sealed record LifecycleMethodCandidate(
    string MethodName,
    string MethodDisplay,
    ActorLifecyclePhase Phase,
    TickTier Tier,
    int TickPhase,
    IMethodSymbol MethodSymbol,
    Location? Location);
```

- [ ] **Step 3: Validate signature and emit metadata into generated actor code**

Generated source shape:

```csharp
private static void Invoke_Lifecycle_Combat(global::LayerBase.Actor.IActor actor, float deltaTime)
{
    ((global::Sample.EnemyActor)actor).Combat(deltaTime);
}

builder.AddLifecycleMethod(
    global::LayerBase.Actor.ActorLifecyclePhase.Update,
    global::LayerBase.Actor.TickTier.Hot,
    -1,
    Invoke_Lifecycle_Combat);
```

- [ ] **Step 4: Re-run generator tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ActorGeneratorTests"`

Expected: PASS

### Task 6: Add Budgeted Runtime Storage for Method Lifecycle Entries

**Files:**
- Add: `LayerBase/Actor/Lifecycle/ActorLifecycleMethodEntry.cs`
- Add: `LayerBase/Actor/Lifecycle/ActorLifecycleMethodFreeList.cs`
- Add: `LayerBase/Actor/Lifecycle/ActorLifecycleMethodTickLane.cs`

- [ ] **Step 1: Add method-entry storage**

```csharp
internal readonly struct ActorLifecycleMethodEntry
{
    public readonly ActorId ActorId;
    public readonly IActor Actor;
    public readonly ActorLifecycleMethodInvoker Invoker;

    public ActorLifecycleMethodEntry(ActorId actorId, IActor actor, ActorLifecycleMethodInvoker invoker)
    {
        ActorId = actorId;
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
    }
}
```

- [ ] **Step 2: Implement a budget-aware method free list**

`ActorLifecycleMethodFreeList.PumpBudgeted(...)` should mirror `ActorLifecycleFreeList<TLifecycle>.PumpBudgeted(...)`, except it invokes:

```csharp
entry.Invoker(entry.Actor, state.DeltaTime);
budget.ConsumeEvent();
```

- [ ] **Step 3: Add tiered method lanes**

Lane contract:

```csharp
public ActorLifecycleHandle Add(
    ActorId actorId,
    IActor actor,
    ActorLifecycleMethodInvoker invoker,
    TickTier tier,
    int phase);

public void Pump(
    int frameIndex,
    ref LifecycleFrameState state,
    ref RuntimeFrameBudget budget,
    int timeCheckInterval);
```

Pump order:
- hot first
- current warm bucket second
- current cold bucket third
- dormant never pumps

### Task 7: Wire Method Lanes Into the Actor Lifecycle Scheduler

**Files:**
- Modify: `LayerBase/Actor/Lifecycle/ActorLifecycleHandles.cs`
- Modify: `LayerBase/Actor/Lifecycle/ActorLifecycleScheduler.cs`

- [ ] **Step 1: Extend stored handles**

```csharp
internal struct ActorLifecycleHandles
{
    public ActorLifecycleHandle Update;
    public ActorLifecycleHandle LateUpdate;
    public ActorLifecycleHandle FixedUpdate;
    public ActorLifecycleHandle[]? Extra;
}
```

- [ ] **Step 2: Add method lanes alongside interface free lists**

```csharp
private readonly ActorLifecycleMethodTickLane _methodUpdates = new();
private readonly ActorLifecycleMethodTickLane _methodLateUpdates = new();
private readonly ActorLifecycleMethodTickLane _methodFixedUpdates = new();
private int _frameIndex;
```

- [ ] **Step 3: Add registration and removal APIs**

```csharp
public ActorLifecycleHandle AddMethod(
    ActorLifecyclePhase phase,
    ActorId actorId,
    IActor actor,
    ActorLifecycleMethodInvoker invoker,
    TickTier tier,
    int tickPhase)
{
    return phase switch
    {
        ActorLifecyclePhase.Update => _methodUpdates.Add(actorId, actor, invoker, tier, tickPhase),
        ActorLifecyclePhase.LateUpdate => _methodLateUpdates.Add(actorId, actor, invoker, tier, tickPhase),
        ActorLifecyclePhase.FixedUpdate => _methodFixedUpdates.Add(actorId, actor, invoker, tier, tickPhase),
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };
}
```

- [ ] **Step 4: Pump methods after their matching interface lifecycle phase**

Example for update:

```csharp
_updates.PumpBudgeted(...);
_methodUpdates.Pump(_frameIndex, ref state, ref budget, TimeCheckInterval);
```

Increment `_frameIndex` once per update-frame pump so warm/cold buckets advance deterministically.

### Task 8: Register Generated Lifecycle Methods From Typed Actor Storage

**Files:**
- Modify: `LayerBase/Actor/Storage/TypedActorStorage.cs`

- [ ] **Step 1: Register generated methods during actor creation**

```csharp
if (_meta.LifecycleMethods.Length > 0)
{
    var extra = new ActorLifecycleHandle[_meta.LifecycleMethods.Length];

    for (int i = 0; i < _meta.LifecycleMethods.Length; i++)
    {
        ActorLifecycleMethodMeta method = _meta.LifecycleMethods[i];
        extra[i] = world.Lifecycle.AddMethod(
            method.Phase,
            actorId,
            actor,
            method.Invoker,
            method.Tier,
            method.TickPhase);
    }

    handles.Extra = extra;
}
```

- [ ] **Step 2: Remove generated method handles on destroy**

```csharp
if (handles.Extra != null)
{
    for (int i = 0; i < handles.Extra.Length; i++)
    {
        world.Lifecycle.RemoveMethod(handles.Extra[i]);
    }
}
```

- [ ] **Step 3: Keep old interface lifecycle registration behavior unchanged**

Expected: actors implementing `IUpdate` / `ILateUpdate` / `IFixedUpdate` keep working with no source changes.

### Task 9: Verify Lifecycle Runtime Behavior

**Files:**
- Verify only

- [ ] **Step 1: Run the lifecycle tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ActorLifecycleTests"`

Expected: PASS

- [ ] **Step 2: Run the generator tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ActorGeneratorTests"`

Expected: PASS

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug`

Expected: PASS

- [ ] **Step 4: Build the solution**

Run: `dotnet build LayerBase.sln -c Debug`

Expected: PASS

- [ ] **Step 5: Run the existing lifecycle benchmark as a smoke gate**

Run: `dotnet run --project LayerBase.BenchMark/LayerBase.BenchMark.csproj -c Debug -- --filter "*Lifecycle_PumpUpdate_10000*"`

Expected: benchmark completes without new allocation regressions.
