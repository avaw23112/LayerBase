# LayerBase Threading Model

LayerBase Runtime uses a single-thread runtime model for most of its operations.

## Owner-Thread Only APIs

The following APIs are **NOT** thread-safe and must be called only from the thread that owns the `LayerRuntime` instance:

- `Send<T>(in T value)`
- `Post<T>(in T value)`
- `TryPost<T>(in T value, ...)`
- `PostLatest<T>(in T value)`
- `PostCoalesced<T>(in T value)`
- `MarkDirty<T>()`
- `CallAsync<...>(...)`
- `Pump(float deltaTime)`
- `Build()`
- `Dispose()`
- `Reset()`

LayerBase does **not** perform runtime thread checks on hot-path APIs for performance reasons. Calling owner-thread-only APIs from the wrong thread is **undefined behavior** and may lead to data corruption or race conditions.

## Any-Thread APIs

The following APIs are designed to be safe for calling from any thread:

- `PostFromAnyThread<T>(in T value, ...)`
- `TryPostFromAnyThread<T>(in T value, ...)`

`PostFromAnyThread` is a cross-thread ingress API. It does **not** dispatch the event immediately. Instead, it places the event into an internal ingress queue which is drained by the owner thread during `Runtime.Pump` (at the beginning of the frame, before `PostScheduler.Pump`).

## Concurrency Constraints

### Dispose and Reset
`Dispose()` and `Reset()` should not be called concurrently with `PostFromAnyThread`. Ensure that no background threads are posting events when the runtime is being shut down or reset.

### Event Handlers
All event handlers (subscribers) are executed on the owner thread during `Runtime.Pump`. You do not need to worry about thread safety within your layers as long as you only interact with other layers and the runtime from within these handlers.
