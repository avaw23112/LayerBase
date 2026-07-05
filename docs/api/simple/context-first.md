# LayerBase Context-First Simple API

`ILayerContext` is the recommended entry point for normal business code.

The goal is simple:

```text
Most gameplay code should work through context.
```

## Recommended APIs

Use these APIs first:

```csharp
context.Send(new DamageEvent(10));
context.Post(new BattleTick(deltaTime));

context.Query<Position, Velocity>()
    .ForEach((ref Position position, in Velocity velocity) =>
    {
        position.Value += velocity.Value * deltaTime;
    });

MonsterActor monster = context.CreateActor<MonsterActor>();
ActorId monsterId = monster.GetActorId();

context.PostActor(monsterId, new HitMessage(10));

DamageResult result = await context.Ask<DamageRequest, DamageResult>(
    monsterId,
    new DamageRequest(10));

context.DestroyActor(monsterId);

InventoryService inventory = context.Get<InventoryService>();
context.Delay(new SpawnEvent(), ttl: 1.0f);
```

## Why This Style

This style keeps normal code away from low-level runtime concepts such as:

- `LayerRuntime`
- `ActorWorld`
- `World`
- `EventCenter`

That makes business code easier to read, teach, and refactor.

## Advanced APIs

The following APIs are still supported, but they are advanced entry points:

```csharp
ActorWorld actorWorld = context.Actors();
World world = context.ECSWorld();
```

Use them only when you need:

- batch actor operations
- lower-level ECS capabilities
- framework integration
- hot-path tuning

## Compatibility

Older APIs remain available for compatibility:

- `context.AskActor<TRequest, TResponse>(...)`
- `context.CreateActor<TActor>(bool usePool)`
- `context.Actors()`
- `context.ECSWorld()`

The recommended style for new code is the shorter context-first facade.
