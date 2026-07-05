# Context-First API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reframe the public-facing LayerBase API around `ILayerContext` so normal business code can create actors, send actor requests, post actor messages, destroy actors, and resolve services without dropping down to `ActorWorld`, `World`, or runtime internals.

**Architecture:** This pass is a facade-and-docs change, not a runtime rewrite. We keep the existing Actor/ECS/Event hot paths and compatibility APIs, then add thinner context-first entry points plus XML/doc guidance that moves `Actors()` and `ECSWorld()` into an explicitly advanced tier. `ParallelEcs` and `DelaySeconds` stay out of this implementation pass because the repository does not yet expose a stable context-level abstraction for them and the design doc's executable commit plan does not require them.

**Tech Stack:** C# 12, .NET 8/9, NUnit, Roslyn source generators, Markdown docs

---

### Task 1: Lock Context-First Behavior With Tests

**Files:**
- Modify: `LayerBase.Test/ActorCallIntegrationTests.cs`
- Modify: `LayerBase.Test/ServiceMountContextTests.cs`
- Test: `LayerBase.Test/LayerBase.Test.csproj`

- [ ] **Step 1: Add a context actor facade integration test**

```csharp
[Test]
public void Context_can_use_simplified_actor_facade_apis()
{
    var runtime = new LayerRuntime(1);
    var layer = new ActorCallIntegrationLayer();
    var builder = new LayerRuntime.LayersBuilder(runtime);
    builder.Push(layer);
    builder.Build();

    ILayerContext context = layer.Service;

    ActorCallIntegrationActor actor = context.CreateActor<ActorCallIntegrationActor>();
    ActorId actorId = actor.GetActorId();

    Assert.That(
        AskAndPump(runtime, context.Ask<ActorBridgeRequest, ActorBridgeResponse>(actorId, new ActorBridgeRequest(7))).Value,
        Is.EqualTo(8));

    context.DestroyActor(actorId);
    Assert.That(runtime.Actors.DestroyActor(actorId), Is.False);
}
```

- [ ] **Step 2: Add tests for `CreatePooledActor`, `PostActor`, and `Get<T>` aliases**

```csharp
[Test]
public void Context_can_create_pooled_actor_instances()
{
    PooledProbeActor.RentCount = 0;
    ILayerContext context = layer.Service;

    PooledProbeActor actor = context.CreatePooledActor<PooledProbeActor>();

    Assert.That(PooledProbeActor.RentCount, Is.EqualTo(1));
    Assert.That(actor, Is.Not.Null);
}

[Test]
public void Context_get_alias_resolves_layer_service()
{
    ILayerContext context = layer.Service!.MountedManager!;

    ServiceMountTestService service = context.Get<ServiceMountTestService>();

    Assert.That(service, Is.SameAs(layer.Service));
}
```

- [ ] **Step 3: Run the targeted tests before implementation**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ActorCallIntegrationTests|FullyQualifiedName~ServiceMountContextTests"`

Expected: FAIL because the new context-first facade methods do not exist yet.

### Task 2: Add Context-First Actor Facade APIs

**Files:**
- Modify: `LayerBase/Actor/Extensions/LayerContextActorExtensions.cs`

- [ ] **Step 1: Keep compatibility APIs but add the short context-first aliases**

```csharp
public static LBTask<TResponse> Ask<TRequest, TResponse>(
    this ILayerContext context,
    ActorId actorId,
    in TRequest request,
    CancellationToken cancellationToken = default)
    where TRequest : struct
    where TResponse : struct
{
    return context.AskActor<TRequest, TResponse>(actorId, in request, cancellationToken);
}

public static void PostActor<TMessage>(
    this ILayerContext context,
    ActorId actorId,
    in TMessage message)
    where TMessage : struct
{
    context.GetBinding().Runtime.PostTo(actorId, in message);
}

public static void DestroyActor(
    this ILayerContext context,
    ActorId actorId)
{
    context.Actors().DestroyActor(actorId);
}
```

- [ ] **Step 2: Split pooled creation from the simple path**

```csharp
public static TActor CreateActor<TActor>(this ILayerContext context)
    where TActor : class, IActor, new()
{
    return context.Actors().CreateActor<TActor>(usePool: false);
}

public static TActor CreatePooledActor<TActor>(this ILayerContext context)
    where TActor : class, IActor, new()
{
    return context.Actors().CreateActor<TActor>(usePool: true);
}
```

- [ ] **Step 3: Preserve `AskActor` and `CreateActor(bool usePool)` for compatibility**

Expected: old call sites keep compiling unchanged.

### Task 3: Mark Low-Level Accessors As Advanced APIs

**Files:**
- Modify: `LayerBase/Actor/Extensions/LayerContextActorExtensions.cs`
- Modify: `LayerBase/Actor/Extensions/LayerActorExtensions.cs`
- Modify: `LayerBase/Actor/Extensions/ServiceActorExtensions.cs`
- Modify: `LayerBase/ECS/Extensions/LayerContextECSExtensions.cs`
- Modify: `LayerBase/ECS/Extensions/LayerQueryExtensions.cs`
- Modify: `LayerBase/ECS/Extensions/ServiceECSExtensions.cs`

- [ ] **Step 1: Add XML summaries that explicitly place `Actors()` behind the advanced boundary**

```csharp
/// <summary>
/// Gets the <see cref="ActorWorld"/> bound to the current context.
///
/// Advanced API:
/// Prefer <c>CreateActor</c>, <c>CreatePooledActor</c>, <c>PostActor</c>, <c>Ask</c>, and <c>DestroyActor</c>
/// in normal business code. Access <see cref="ActorWorld"/> directly only when performing batch actor work,
/// framework integration, or low-level tuning.
/// </summary>
public static ActorWorld Actors(this ILayerContext context)
```

- [ ] **Step 2: Add XML summaries that explicitly place `ECSWorld()` behind the advanced boundary**

```csharp
/// <summary>
/// Gets the ECS <see cref="World"/> bound to the current context.
///
/// Advanced API:
/// Prefer <c>Query</c> in normal business code. Access <see cref="World"/> directly only when you need lower-level
/// ECS capabilities and understand the threading and structural-change rules yourself.
/// </summary>
public static World ECSWorld(this ILayerContext context)
```

### Task 4: Add the Context Service Alias

**Files:**
- Modify: `LayerBase/DI/ServiceContracts.cs`

- [ ] **Step 1: Add a short `Get<T>` alias next to `GetService<T>`**

```csharp
public static T Get<T>(this ILayerContext context)
    where T : class
{
    return context.GetService<T>();
}
```

- [ ] **Step 2: Keep `GetService<T>` as the compatibility API**

Expected: no runtime behavior change, only a shorter recommended entry point.

### Task 5: Publish the Simple API Narrative

**Files:**
- Add: `docs/api/simple/context-first.md`
- Modify: `README.md`

- [ ] **Step 1: Add a dedicated context-first guide**

Content goals:
- Show only `context`, `Layer`, `Service`, `Actor`, `Event`, `Query`
- Recommend `Send`, `Post`, `Query`, `CreateActor`, `CreatePooledActor`, `PostActor`, `Ask`, `DestroyActor`, `Get<T>`
- Mention `Actors()` and `ECSWorld()` only in an Advanced section

- [ ] **Step 2: Rewrite the README quick example to be context-first**

```csharp
public sealed class BattleService : IService, IUpdate
{
    [Mount]
    private BattleContext context = default!;

    public void Update(float deltaTime)
    {
        context.Query<Position, Velocity>()
            .ForEach((ref Position position, in Velocity velocity) =>
            {
                position.Value += velocity.Value * deltaTime;
            });

        context.Post(new BattleTick(deltaTime));
    }
}
```

### Task 6: Verify the Facade Pass

**Files:**
- Verify only

- [ ] **Step 1: Run the targeted tests**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ActorCallIntegrationTests|FullyQualifiedName~ServiceMountContextTests"`

Expected: PASS

- [ ] **Step 2: Build the solution**

Run: `dotnet build LayerBase.sln -c Debug`

Expected: PASS

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug`

Expected: PASS
