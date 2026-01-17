# LayerBase Event Runtime

LayerBase is a lightweight, layered event runtime for .NET that combines a responsibility-chain pipeline, pooled event dispatching, DI-first layer composition, and a tick-driven timer scheduler. It is built for games or real-time apps that need predictable update loops, fast event routing, and optional tracing.

## Core Features
- **Layer chain**: Build linked layers (`LayerHub`, `LayerChain`) that pump in order; each layer can bind sync/async handlers and forward events up/down.
- **Event pipeline**: Events are value types (`Event<T>`), support delegate or handler interfaces, and propagate with bubble/broadcast/drop semantics. Pooled containers reduce allocations.
- **Timers**: `TimerScheduler` schedules `After`, `At`, and `Frequency` invocations for handlers or `Action<Event<T>>`, driven by external `Tick`.
- **DI-first layers**: `IService` modules register dependencies into a per-layer container; a source generator fills services based on `[OwnerLayer]` hints.
- **Tracing & logging**: Optional `EventStateTracer`/`EventLogTracer` capture per-event lifecycle; export logs for debugging.

## Project Layout
- `LayerBase/`: core runtime (`Event/`, `Layer/`, `DI/`, `Tools/Timer/`, `DataStruct/`).
- `LayerBase.Test/`: NUnit tests for DI and layer behavior.
- `LayerBase.Usages/`: runnable samples (e.g., `LayerChainTest.cs`) to experiment with the API.
- `LayerBase.Generator/`, `LayerBase.Task/`: source generator and build-time tooling.

## Getting Started
1) Install .NET 8 SDK.  
2) Restore/build: `dotnet restore LayerBase.sln` then `dotnet build LayerBase.sln -c Debug`.  
3) Run tests: `dotnet test LayerBase.Test/LayerBase.Test.csproj`.  
4) Try a sample: `dotnet run --project LayerBase.Usages/LayerBase.Usages.csproj`.

### Minimal Usage
```csharp
internal struct Ping { public int Id; }

internal sealed class GameLayer : Layer
{
    public GameLayer()
    {
        Bind((in Event<Ping> e) => { Console.WriteLine(e.Value.Id); return EventHandledState.Handled; });
    }
}

// Build and pump
var hub = LayerHub.CreateLayers().Push(new GameLayer()).Build();
hub.Drop(new Event<Ping>(new Ping { Id = 1 }));
hub.Pump(0.016);
```

Schedule timers:
```csharp
var scheduler = TimerSchedulers.GetOrCreate("game");
scheduler.SetFrequency(0.5);
scheduler.FireAfter(1.0, new Ping { Id = 2 }, e => Console.WriteLine($"after: {e.Value.Id}"));
scheduler.Tick(0.5); // drive from your main loop
```

## Testing & Quality
- Tests use NUnit; prefer deterministic, isolated cases. Follow existing naming like `GeneratedServices_are_resolvable_with_expected_lifetimes`.
- Run `dotnet test` before contributing. Keep diffs focused and avoid unrelated formatting changes.
