# Task 2 Report: Fix Shutdown control result and fault paths

## Summary

Fixed `ScopeRuntimeHost` and `LayerRuntime` shutdown handling to:
1. Preserve control task exceptions (no silently lost/unobserved exceptions)
2. Prevent `ObjectDisposedException` from replacing original scope faults during shutdown
3. Ensure worker scopes exit properly after a failed dispose

## TDD Evidence

### RED Phase (both tests failed before fixes)

**Test 1: `ApplyFaultPolicy_does_not_throw_ObjectDisposedException_after_shutdown`**
- Failed with `ObjectDisposedException: Cannot access a disposed object. Object name: 'ScopeRuntimeHost'` at `ThrowIfDisposed()` → `TryGetRuntime()` → `ApplyFaultPolicy()`

**Test 2: `Worker_dispose_control_exception_is_reported_as_fault_not_silently_lost`**
- Failed with `capturedFaultException` being null — the `InvalidOperationException` from `DisposeReverse()` was never reported via `runtime.Faulted` because the worker loop caught `ObjectDisposedException` and exited, and the original exception was silently lost (never consumed via `GetResult`)

### GREEN Phase (both tests pass after fixes)

**Test 1:** After split state and using `_directory.TryGetRuntime()` directly in `ApplyFaultPolicy()`, no `ObjectDisposedException` is thrown.

**Test 2:** After fixing `RequestDisposeForAllScopes()` to use `ScopeControlBarrier.Wait()` + `EnsureSucceeded()` with try/catch → `ReportFatalFault()`, the exception is properly observed and reported via `runtime.Faulted`.

## Changes Made

### `LayerBase/Scope/ScopeRuntimeHost.cs`
- Split `_disposed` flag into `_shutdownStarted` + `_disposed` (int fields, 0/1)
- `_shutdownStarted = 1`: no new host-level business operations
- `_disposed = 1`: all resources cleaned up, no operations at all
- `ApplyFaultPolicy()` now uses `_directory.TryGetRuntime()` directly (bypasses `ThrowIfDisposed()`)
- `RequestDisposeForAllScopes()` worker section: replaced manual polling (without `GetResult()`) with `ScopeControlBarrier.Wait()` + `EnsureSucceeded()`, wrapped in try/catch to allow other scopes to dispose
- `WaitForControl()`: exceptions from `GetResult()` are now reported via `scope.ReportFatalFault()` instead of silently swallowed

### `LayerBase/Scope/ScopeWorker.cs`
- Worker loop now also exits on `Faulted` state (not just `Disposed`), so a failed dispose on a worker thread doesn't leave it running forever

### `LayerBase/Application/LayerRuntime.cs`
- `Dispose()`: `_disposed = true` moved to finally block, so scope cleanup during host dispose can call `ApplyFaultPolicy()` without hitting disposed state
- `AbortBuild()`: same treatment

### `LayerBase.Test/ScopeShutdownStateTests.cs` (NEW)
- Test: fault during shutdown is NOT replaced by ObjectDisposedException
- Test: dispose control exception is observed and reported, not silently lost

## Post-Review Fixes (Round 3)

### Critical Issue 1: `_shutdownStarted` was dead code (assigned but never read)

**Fix:** Restructured two-phase state management in `Dispose()`:
- `Interlocked.Exchange(ref _shutdownStarted, 1)` is now the re-entrance guard (set first)
- `Volatile.Write(ref _disposed, 1)` is set last in the `finally` block
- `ThrowIfDisposed()` only checks `_disposed` (after complete teardown)
- Added `Volatile.Read(ref _shutdownStarted)` guard in `StartWorkers()` to reject new host-level business during shutdown

### Critical Issue 2: Assignment order was inverted

Previously `_disposed` was set BEFORE `_shutdownStarted` via `Interlocked.Exchange`, causing `ThrowIfDisposed()` to throw immediately at the start of `Dispose()`. Now `_shutdownStarted` is the entrance guard, and `_disposed` is the final tombstone.

### Files changed

| File | Change |
|------|--------|
| `LayerBase/Scope/ScopeRuntimeHost.cs` | `Dispose()` now uses `_shutdownStarted` as re-entrance guard; `_disposed` set last via `Volatile.Write`; `StartWorkers()` checks `_shutdownStarted` instead of `ThrowIfDisposed()` |

## Verification

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release --filter "FullyQualifiedName~ScopeShutdownStateTests|FullyQualifiedName~WorkerShutdownTimeoutTests"
```
Result: 7 passed, 0 failed

Full suite: 814 passed, 1 failed (pre-existing failure in `ScopeDiagnosticsTests.Worker_snapshot_runs_on_worker_owner_thread`, unrelated to these changes)
