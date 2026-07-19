# Task 3 Report: ScopeWorker Delayed Resource Reclamation

## Summary

Added a deferred resource release mechanism to `ScopeWorker` so that `_ready` (ManualResetEventSlim) and `_workSignal` (AutoResetEvent) are reliably disposed even when `Stop()` times out and the worker thread exits later.

## TDD Results

### RED Phase (before implementation)
- Wrote `Delayed_thread_exit_releases_resources_after_timeout` test
- Build failed with `CS1061: 'ScopeWorker' does not contain a definition for 'ResourcesReleased'`

### GREEN Phase (after implementation)
- All 6 tests in `WorkerShutdownTimeoutTests` pass:
  - `Normal_scope_shutdown_disposes_resources` ✅ (70ms)
  - `Timed_out_scope_worker_does_not_dispose_live_resources` ✅
  - `Timed_out_scope_reports_shutdown_fault` ✅
  - `Shutdown_timeout_does_not_cause_object_disposed_exception_on_worker` ✅
  - `Runtime_dispose_is_bounded` ✅
  - `Delayed_thread_exit_releases_resources_after_timeout` ✅ (new, RED/GREEN verified)

## Changes

### Modified: `LayerBase/Scope/ScopeWorker.cs`

| Change | Description |
|--------|-------------|
| Fields | Added `_startWaitCompleted` (int), `_threadExited` (int); converted `_resourcesReleased` from `bool` to `int` |
| `SignalWork()` | New private method with disposed guard; replaces lambda in constructor |
| `Start()` | Wrapped body in `try/finally` to set `_startWaitCompleted = 1` and call `TryReleaseResourcesAfterExit()` |
| `Run()` | Added `Volatile.Write(ref _threadExited, 1)` and `TryReleaseResourcesAfterExit()` to inner `finally` |
| `Stop()` | Guarded `_workSignal.Set()` with `try/catch(ObjectDisposedException)` to handle race with thread exit |
| `TryReleaseResourcesAfterExit()` | New private method: checks both handshake flags, then calls `ReleaseResources()` |
| `ReleaseResources()` | Changed to use `Interlocked.Exchange` for thread-safe idempotent release |
| `ResourcesReleased` | New `internal bool` diagnostic property |

### Modified: `LayerBase.Test/WorkerShutdownTimeoutTests.cs`

| Addition | Description |
|----------|-------------|
| `Delayed_thread_exit_releases_resources_after_timeout` | Test that blocks a worker in `Update()`, verifies resources NOT released during timeout, unblocks, verifies resources ARE released after thread exit |
| `GetSingleWorker()` | Reflection helper to access `ScopeWorker` from `LayersBuilder` |
| `BlockableUpdateService` | Service that blocks on `ManualResetEventSlim.Wait()` |
| `BlockableWorkerLayer` | Layer registering the blockable service on a worker scope |
| `BlockableWorkerScope` | Worker scope definition with 10Hz tick rate |

## Verification Command

```
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release --filter "FullyQualifiedName~WorkerShutdownTimeoutTests"
```

All 6 tests passed.
