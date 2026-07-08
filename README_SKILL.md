# LayerBase

> A data-oriented, high-performance C# architecture bus for Unity, Godot, and pure C# servers — organizing complex game logic through Layer, Service, Manager, ECS, Actor, and a tick-driven runtime pump.

## What is this?

LayerBase replaces the traditional singleton + EventBus pattern with a structured **Layer → Service → Manager** topology, backed by an SOA-optimized event dispatch engine. It gives mid-to-large game projects:

- A **deterministic event lifecycle** — no more hidden timing traps from scattered `Awake`/`Start` registrations
- **Cache-friendly dispatch** — SOA array layout + bitmask routing instead of `Dictionary<Type, List<Delegate>>`
- **Zero-allocation hot paths** — core dispatch and sync `LBTask` paths produce 0 GC heap allocations
- **ECS + Actor + Projection** — data-intensive batch processing (ECS) and behavioral encapsulation (Actor) seamlessly bridged

### What it is NOT

- LayerBase is **not** a game engine. It does not replace Unity, Godot, or any rendering/physics engine.
- LayerBase is **not** a full ECS framework (it uses [Arch](https://github.com/genaray/Arch) internally; a future iteration may ship a custom ECS).
- LayerBase is not designed for microservices or general-purpose enterprise messaging.
- LayerBase is designed for **game runtime communication** where predictable order, memory locality, and low latency matter.

## Features

| Feature | Description |
|---|---|
| **Layer Topology** | Structured `Layer → Service → Manager` hierarchy with DI and source-generated zero-reflection bindings |
| **Event Dispatch** | SOA array layout with bitmask inter-layer routing; 1.6 ns per dispatch |
| **LBTask** | Struct-based async model optimized for game loops; zero allocation on sync completion |
| **ECS World** | Archetype-based entity system (via Arch) with `Query`, `Blueprint`, `Bundle`, and `Projection` |
| **Actor Model** | Per-entity mailbox, lifecycle callbacks (`IStart`, `IUpdate`, `IDestroy`), pooling, tags & groups |
| **Projection** | Declarative ECS ↔ Actor bridge — ECS query results auto-delivered as Actor events |
| **Post Scheduler** | Frame-budgeted async delivery with `Normal`, `Latest`, `Coalesced`, `DirtySignal` modes + backpressure |
| **Call (Request/Response)** | Type-safe synchronous/async call channel with ~1.08 ns overhead |
| **Timer Scheduler** | Tick-driven one-shot and frequency-gated timers |
| **Job Scheduler** | Thread-pool background tasks with `PostFromAnyThread` bridge |
| **Snap** | Runtime business-field snapshot (`IFullSnap` / `IClipSnap<T>`) |
| **Cross-thread Ingress** | `PostFromAnyThread` for safe background-to-main-thread event submission |
| **Diagnostics** | Topology markdown export, runtime policy dump, static build-time deadlock audit |

## Quick Start

```csharp
using LayerBase;
using LayerBase.Layers;
using LayerBase.DI;

// 1. Define an event (must be struct — zero GC)
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}

// 2. Write business logic in a Manager
public partial class DamageManager : ILayerContext
{
    [Subscribe]
    private void OnDamage(in DamageEvent e)
    {
        // this.Send(new PlayerDeathEvent()); // broadcast another event
    }
}

// 3. Define a Service to group Managers
public partial class CombatService : IService
{
    [Mount] private DamageManager _damageManager; // auto-injected
}

// 4. Define Layers and build the runtime
public partial class GameLogicLayer : Layer
{
    [Mount] private CombatService _combatService;
}

public class GameRoot
{
    private LayerRuntime _runtime;

    public void Awake()
    {
        _runtime = LayerHub.CreateLayers()
            .Push(new GameLogicLayer())
            .Build()       // scans attributes, allocates SOA arrays
            .Prewarm();    // optional: preheat caches / event IDs
    }

    public void Update(float deltaTime)
    {
        _runtime.Pump(deltaTime); // dispatch async events, tick timers
    }
}
```

## Core Concepts

```
Layer (priority boundary)
 └── Service (functional domain)
      └── Manager (single-responsibility logic)

ECS World (data)
 ├── Entity (lightweight handle)
 ├── Component (struct data in SOA layout)
 ├── Query (batch filter)
 └── Blueprint (declarative entity definition)

Actor World (behavior)
 ├── Actor (independent behavior unit)
 ├── Mailbox (queue / latest / coalesced delivery)
 └── Projection (ECS ↔ Actor bridge)

Runtime Pump
 ├── Build (scan, allocate, validate)
 ├── Pump (async drain, timer tick, lifecycle callbacks)
 └── Dispose (cleanup)
```

| Concept | Role | Collaborates with |
|---|---|---|
| **Layer** | Priority & physical boundary; max 64 per runtime | Hosts Services via DI |
| **Service** | Aggregates related Managers; owns DI wiring | Owned by Layer; injects Managers |
| **Manager** | Single-responsibility business logic | Communicates via events only |
| **ECS World** | Bulk data storage in contiguous memory | Queried by Managers; projected to Actors |
| **Actor** | Per-entity event mailbox + lifecycle | Receives events from Projection or direct post |
| **Projection** | ECS → Actor bridge on query results | Auto-delivers ECS query data as Actor events |

## Architecture

### Module Relationship

```
                    ┌──────────┐
                    │  Layers  │ (1..64, priority-ordered)
                    └────┬─────┘
                         │ Build / Pump / Dispose
               ┌─────────┼──────────┐
               │         │          │
         ┌─────▼──┐ ┌───▼───┐ ┌───▼────┐
         │Service │ │Service │ │Service │ (functional domains)
         └────┬───┘ └───┬───┘ └───┬────┘
              │         │         │
         ┌────▼───┐ ┌──▼────┐ ┌──▼─────┐
         │Manager │ │Manager│ │Manager │ (SRP logic blocks)
         └───┬────┘ └───────┘ └───┬────┘
             │                     │
       ┌─────▼─────────┐   ┌──────▼──────┐
       │   ECS World   │   │  Actor World│
       │ (data layer)  │   │ (behavior)  │
       └─────┬─────────┘   └──────┬──────┘
             │                    │
             └──── Projection ────┘ (auto bridge)
```

### Dispatch Flow

```
Send<Event>()  ──►  Layer topology (bitmask routing)
                       │
                       ├── SubscribeNotify (fast path, no exception guard)
                       ├── Subscribe (safe, exception isolated)
                       ├── SubscribeFlow (truncatable)
                       └── SubscribeAsync (deferred to after sync)

Post<Event>()  ──►  PostScheduler (frame-budgeted queue)
                       │
                       └── Pump() drains per-frame with backpressure
```

### Threading Model

LayerBase uses a single-thread runtime. Only `PostFromAnyThread` / `TryPostFromAnyThread` are safe from background threads; events submitted this way are drained during `Runtime.Pump` on the owner thread.

## Performance

| Metric | Value | Conditions |
|---|---|---|
| Event dispatch (1 subscriber) | **1.66 ns** | Single event, warm JIT, .NET 8 |
| Event dispatch (16 subscribers) | **6.15 ns** | Same event, 1M calls |
| CallAsync Request/Response | **1.08 ns** | 100k calls, zero allocation |
| LayerBase vs MessagePipe (32 kinds) | **~41% faster** | 2 subs/event, batch dispatch |
| LayerBase vs C# delegate (16 subs) | **~5.8x faster** | 1 subscriber = baseline |

Key optimizations:
- **SOA array layout** for consecutive memory access and CPU prefetch
- **Bitmask inter-layer routing** (`TrailingZeroCount` — O(1) hops)
- **Branchless dispatch** via bitwise state merge and `Unsafe` stepping
- **Zero allocation** on sync `LBTask` and core dispatch paths

> Full benchmark data and methodology at [docs/BENCHMARKS.md](docs/BENCHMARKS.md).

## Project Status

**Beta** — v1.5.7

Core event dispatch, ECS integration, Actor model, Projection, Timer, Snap, and PostScheduler are functional and tested. The public API is stable but may evolve based on real-world usage feedback.

Key areas under active development:
- ECS async execution model & Bring query refinement
- ActorWorld hot-path allocation elimination
- Threading model hardening
- Documentation and example improvements

## Documentation

| Path | Content |
|---|---|
| [docs/BENCHMARKS.md](docs/BENCHMARKS.md) | Detailed performance benchmarks |
| [docs/THREADING.md](docs/THREADING.md) | Threading model specification |
| [docs/benchmark.md](docs/benchmark.md) | Benchmark reproduction guide |
| [docs/plan/](docs/plan/) | Design documents and roadmap |

## License

Apache 2.0 — see [LICENSE.txt](LICENSE.txt).
