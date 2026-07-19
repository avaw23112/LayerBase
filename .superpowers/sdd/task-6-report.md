# Task 6: Route Scope Fault through reliable completion channel

## Summary

Changed fault routing for non-Main scopes from the bounded `EventInbox` (RingBuffer, capacity 1024) to the unbounded `ScopeCompletionInbox` (ConcurrentQueue), eliminating silent fault drops when the EventInbox is full.

## Changes

### `LayerBase/Scope/ScopeCompletionInbox.cs`
- Added `ScopeFault = 3` to `ScopeCompletionKind` enum
- Added `FaultRecord` property to `ScopeCompletionEnvelope`
- Added private constructor overload accepting `ScopeFaultRecord`
- Added `ScopeFault()` factory method

### `LayerBase/Scope/ScopeRuntime.cs`
- Added `case ScopeCompletionKind.ScopeFault` in `DrainCompletionInbox()` → calls `_runtime.ReportScopeFault(envelope.FaultRecord)`
- Changed `ReportFault()` non-Main scope path: replaced `EnqueueScopeFaultEvent` (EventInbox) with `ScopeCompletionEnvelope.ScopeFault` → `EnqueueCompletion` (CompletionInbox)

### `LayerBase.Test/ScopeFaultPropagationTests.cs`
- Updated `Update_exception_emits_scope_fault_event_to_main_scope_inbox` → renamed to `Update_exception_delivers_fault_through_completion_inbox_to_main_scope`, now checks `CompletionInbox.Count` then `runtime.Faulted` after `PumpIngress`
- Added `Non_main_scope_fault_delivered_via_completion_inbox_when_main_event_inbox_full` test: fills Main EventInbox to capacity, triggers fault on inline scope, verifies fault arrives via CompletionInbox and `runtime.Faulted` is invoked

## Verification

```
dotnet test --filter "FullyQualifiedName~ScopeFaultPropagationTests|FullyQualifiedName~WorkerCompletionInboxTests"
  Passed: 11, Failed: 0

dotnet test (full suite)
  Passed: 822, Failed: 2 (pre-existing, unrelated)
```

## Design Decisions

- Source scope still calls `ApplyFaultPolicy()` locally — Main scope only reports via `ReportScopeFault`
- `EnqueueScopeFaultEvent` method retained (still usable, not removed)
- No new public API added
