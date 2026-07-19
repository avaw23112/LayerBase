# Task 1 Report: Worker Pending → Running → Terminal state

## What Was Implemented

Added the `WorkerExecutionStarted` completion kind to bridge the gap between worker job submission (`Pending`) and physical execution start (`Running`). Previously `MarkExecutionStarted()` existed but was never called from the execution path.

**Changes:**
1. `LayerBase/Scope/ScopeCompletionInbox.cs` - Added `WorkerExecutionStarted = 2` to `ScopeCompletionKind` enum; added `WorkerExecutionStarted(WorkerHandle)` factory method on `ScopeCompletionEnvelope`
2. `LayerBase/Scope/ScopeRuntime.cs` - Added `case ScopeCompletionKind.WorkerExecutionStarted:` in `DrainCompletionInbox()` to call `WorkerJobs.MarkExecutionStarted(envelope.WorkerHandle)`
3. `LayerBase/Worker/WorkerExecutionItem.cs` - Added `SubmitExecutionStarted()` private method that enqueues the `WorkerExecutionStarted` envelope; called before `try` in the `else` branch of `Execute()`
4. `LayerBase.Test/WorkerCoordinatorRaceTests.cs` - Added `Blocking_job_enters_running_before_physical_completion` test

## TDD Evidence

### RED phase
```
> dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release --filter "FullyQualifiedName~Blocking_job_enters_running_before_physical_completion"
...
  失败 Blocking_job_enters_running_before_physical_completion [20 s]
  Expected: True
  But was:  False
   at EventsTest.WorkerCoordinatorRaceTests.Blocking_job_enters_running_before_physical_completion()
```
Expected failure: `SpinUntil` returned `False` because `GetState(handle)` never reached `WorkerState.Running` — `MarkExecutionStarted` was never called.

### GREEN phase
```
> dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release --filter "FullyQualifiedName~Blocking_job_enters_running_before_physical_completion"
...
已通过! - 失败: 0，通过: 1，已跳过: 0，总计: 1，持续时间: 76 ms
```

## Test Results Summary

| Command | Tests | Result |
|---------|-------|--------|
| `--filter Blocking_job_enters_running_before_physical_completion` | 1 | PASS (76 ms) |
| `--filter WorkerCoordinatorRaceTests\|WorkerCompletionInboxTests` | 5 | PASS (153 ms) |
| Full suite (no filter) | 813 | ALL PASS (2 m 59 s) |

## Files Changed

```
M  LayerBase.Test/WorkerCoordinatorRaceTests.cs    (+41 lines)
M  LayerBase/Scope/ScopeCompletionInbox.cs         (+13 lines)
M  LayerBase/Scope/ScopeRuntime.cs                 (+5 lines)
M  LayerBase/Worker/WorkerExecutionItem.cs         (+10 lines)
```

## Self-Review Findings

- All changes are minimal, focused, and match the task brief exactly
- No changes needed to `WorkerJobCoordinator.cs` — `MarkExecutionStarted()` already existed at line 244
- The critical architectural rule (Owner Thread single-writer model, no direct `coordinator` calls from Worker Threads) is preserved: the execution started notification flows through `ScopeCompletionInbox` (enqueued by the worker, drained by the owner thread)
- `SubmitExecutionStarted()` is called before the `try` block so it fires even if the job throws synchronously
- The `Unit.cs` test does not exist (`WorkerCompletionInboxTests.cs` referenced in brief doesn't exist either) — not a concern, the required test was written and passes

## Concerns

None.
