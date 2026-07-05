# Actor Structural Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove dead actor event-bucket infrastructure, clarify call-only abstractions, fix ECS extension naming, and decouple projection-only actor options from the Actor core assembly without regressing hot-path behavior.

**Architecture:** The cleanup is split into four narrow phases that preserve runtime behavior while deleting unused paths and renaming only internal types/files. Event delivery remains on `EventStreamCenter<TEvent>` and call delivery remains on `ActorCallBucket`/`ActorCallColumnRuntime`; projection configuration moves to `LayerBase.ECS.Projection` with an obsolete compatibility wrapper left in `LayerBase.Actor`.

**Tech Stack:** C# 12, .NET 8, NUnit, BenchmarkDotNet, Roslyn source generator

---

### Task 1: Lock Behavior With Tests

**Files:**
- Modify: `LayerBase.Test/ProjectedActorOptionsTests.cs`
- Modify: `LayerBase.Test/ActorMetaDataIntegrationTests.cs`
- Test: `LayerBase.Test/LayerBase.Test.csproj`

- [ ] **Step 1: Add a compatibility test for the projection options attribute**

```csharp
[ProjectedActorOptions(
    retirePolicy: ProjectedActorRetirePolicy.Disable,
    createPolicy: ProjectedActorCreatePolicy.Lazy,
    keepAliveSeconds: 1.0f,
    touchIntervalSeconds: 0.2f)]
internal sealed partial class ProjectionAttributeProbeActor : IPooledActor
{
    public void OnRent() { }
    public void OnReturn() { }
    public void OnEnable() { }
    public void OnDisable() { }
}

[Test]
public void RegisterGenerated_CachesOptions_FromProjectedActorOptionsAttribute()
{
    int actorTypeId = 103;
    ProjectedActorTypeRegistry.RegisterGenerated(
        actorTypeId,
        typeof(ProjectionAttributeProbeActor),
        static actorWorld => actorWorld.CreateProjectedActor<ProjectionAttributeProbeActor>());

    ProjectedActorOptions options = ProjectedActorTypeRegistry.GetOptions(actorTypeId);
    Assert.That(options.RetirePolicy, Is.EqualTo(ProjectedActorRetirePolicy.Disable));
    Assert.That(options.TouchIntervalTicks, Is.EqualTo(ProjectedActorTime.SecondsToTicks(0.2f)));
}
```

- [ ] **Step 2: Remove the dead reflection helper assertion and replace it with a dead-code cleanup guard**

```csharp
[Test]
public void ActorStorage_NoLongerKeepsLegacyEventColumnsField()
{
    LayerRuntime runtime = BuildRuntime();
    runtime.Start();

    ActorWorld world = runtime.GetActorWorld();
    ActorId actorId = world.CreateActor<ActorMetaActor>().GetActorId();

    object storage = GetStorage(world, actorId);
    FieldInfo? field = storage.GetType().GetField("_columnsByEventId", BindingFlags.Instance | BindingFlags.NonPublic);

    Assert.That(field, Is.Null);
}
```

- [ ] **Step 3: Run the targeted tests to verify the new expectations fail before implementation**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ProjectedActorOptionsTests|FullyQualifiedName~ActorMetaDataIntegrationTests"`

Expected: FAIL because `ProjectedActorOptionsAttribute` does not exist yet and the storage still contains `_columnsByEventId`.

### Task 2: Remove Legacy Event-Bucket Infrastructure and Unify Call Abstractions

**Files:**
- Delete: `LayerBase/Actor/Mail/ActorEventBucket.cs`
- Modify: `LayerBase/Actor/Mail/ActorEventColumnRuntime.cs`
- Modify: `LayerBase/Actor/Meta/ActorColumnFactories.cs`
- Modify: `LayerBase/Actor/Meta/ActorBehaviourEntry.cs`
- Modify: `LayerBase/Actor/Storage/ActorWorld.cs`
- Modify: `LayerBase/Actor/Storage/ActorWorld.Lifecycle.cs`
- Modify: `LayerBase/Actor/Storage/TypedActorStorage.cs`
- Modify: `LayerBase/Actor/Call/ActorCallBucket.cs`
- Modify: `LayerBase/Actor/Storage/ActorWorld.Pump.cs`
- Move: `LayerBase/Actor/Mail/IActorEventBucket.cs -> LayerBase/Actor/Call/IActorCallBucket.cs`
- Move: `LayerBase/Actor/Mail/ActorEventColumnRuntime.cs -> LayerBase/Actor/Call/ActorCallColumnRuntime.cs`

- [ ] **Step 1: Delete dead event-bucket members and keep only call-facing runtime primitives**

```csharp
private IActorCallBucket[] _callBucketsByRouteId = Array.Empty<IActorCallBucket>();
private readonly DirtyBucketList _dirtyCallBuckets = new();

internal sealed class ActorCallBucket<TRequest, TResponse> : IActorCallBucket
```

- [ ] **Step 2: Remove `ActorEventColumnRuntime`, `ActorEventColumnFactory`, the old `ActorBehaviourEntry` constructor, and the `_columnsByEventId` loops**

```csharp
public override int GetTotalPendingMailCount()
{
    int count = 0;
    foreach (ActorCallColumnRuntime? column in _callColumnsByRouteId)
    {
        if (column != null)
        {
            count += column.GetTotalPendingCount();
        }
    }

    return count;
}
```

- [ ] **Step 3: Keep `BuildColumns` on the EventStream path only**

```csharp
public void BuildColumns(ActorTypeMeta<TActor> meta, ActorWorld world)
{
    _meta = meta;
    _world = world;
    BuildCallRoutes(meta);
    BuildCallColumns(meta, world);

    foreach (ActorBehaviourEntry entry in meta.Behaviours)
    {
        EnsureEventStreamCapacity(entry.EventTypeId);
    }
}
```

- [ ] **Step 4: Run the targeted tests after the cleanup**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ActorMetaDataIntegrationTests"`

Expected: PASS with the legacy field removed and no actor metadata regressions.

### Task 3: Rename ECS Extension Files and Directories

**Files:**
- Move: `LayerBase/ECS/Extension/LayerActorExtensions.cs -> LayerBase/ECS/Extensions/LayerQueryExtensions.cs`
- Move: `LayerBase/ECS/Extension/LayerContextActorExtensions.cs -> LayerBase/ECS/Extensions/LayerContextECSExtensions.cs`
- Move: `LayerBase/ECS/Extension/ServiceActorExtensions.cs -> LayerBase/ECS/Extensions/ServiceECSExtensions.cs`

- [ ] **Step 1: Move the files so each path matches the existing class name**

```text
LayerBase/ECS/Extensions/LayerQueryExtensions.cs
LayerBase/ECS/Extensions/LayerContextECSExtensions.cs
LayerBase/ECS/Extensions/ServiceECSExtensions.cs
```

- [ ] **Step 2: Verify no source references still point at the old singular directory**

Run: `git grep -n "ECS/Extension" -- .`

Expected: no matches

### Task 4: Move Projection Actor Options Into `LayerBase.ECS.Projection`

**Files:**
- Add: `LayerBase/ECS/Projection/ProjectedActorOptionsAttribute.cs`
- Modify: `LayerBase/ECS/Projection/ProjectedActorTypeRegistry.cs`
- Modify: `LayerBase/ECS/Projection/ProjectedActorOptions.cs`
- Modify: `LayerBase.Generator/LayerBase.Generator/ActorBehaviourGenerator.cs`
- Modify: `LayerBase.Test/ProjectedActorOptionsTests.cs`
- Modify: `LayerBase/Actor/Core/ActorOptionsAttribute.cs`

- [ ] **Step 1: Introduce the new projection-owned attribute and retain a thin obsolete wrapper**

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ProjectedActorOptionsAttribute : Attribute
{
    public ProjectedActorRetirePolicy RetirePolicy { get; }
    public ProjectedActorCreatePolicy CreatePolicy { get; }
    public float KeepAliveSeconds { get; }
    public float TouchIntervalSeconds { get; }
}

[Obsolete("Use ProjectedActorOptionsAttribute in LayerBase.ECS.Projection.")]
public sealed class ActorOptionsAttribute : ProjectedActorOptionsAttribute
{
    public ActorOptionsAttribute(...) : base(...) { }
}
```

- [ ] **Step 2: Update the registry and generator to look for the new attribute name first, while still accepting the obsolete wrapper**

```csharp
private static bool HasProjectedActorOptionsAttribute(INamedTypeSymbol classSymbol)
{
    return classSymbol.GetAttributes().Any(static attribute =>
        attribute.AttributeClass?.ToDisplayString() is
            "LayerBase.ECS.Projection.ProjectedActorOptionsAttribute" or
            "LayerBase.Actor.ActorOptionsAttribute");
}
```

- [ ] **Step 3: Run the targeted projection tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ProjectedActorOptionsTests"`

Expected: PASS with both the new attribute and the compatibility wrapper supported.

### Task 5: Full Verification

**Files:**
- Verify only

- [ ] **Step 1: Build the solution**

Run: `dotnet build LayerBase.sln -c Debug`

Expected: build succeeds with no new warnings from the cleanup.

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug`

Expected: all tests pass

- [ ] **Step 3: Run targeted benchmark gates for hot paths**

Run: `dotnet run --project LayerBase.BenchMark/LayerBase.BenchMark.csproj -c Debug -- --filter "*Actor_PostTo_Pump_10000*" "*Pump_Only_1000*" "*Full_Pipeline*"`

Expected: benchmark run completes without worse allocations and without obvious throughput regression versus the design baseline.

- [ ] **Step 4: Run the ECS actor benchmark class when the targeted filter is insufficient**

Run: `dotnet run --project LayerBase.BenchMark/LayerBase.BenchMark.csproj -c Debug -- --filter "*EcsActorBenchmarks*"`

Expected: benchmark run completes and preserves the no-new-GC expectation on the hot path.
