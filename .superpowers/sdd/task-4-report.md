# Task 4 Report: Inline Scope Fair Round-Robin and Shared Budget

## TDD RED/GREEN Evidence

### RED — Tests failing before fixes

```
失败 Inline_scopes_use_fair_round_robin_across_frames [35 ms]
  错误消息: Scope B should have consumed its event in frame 2 (fair round-robin)
  Expected: False  But was:  True

失败 Inline_scope_pump_consumes_budget_work_items [1 ms]
  错误消息: Assert.That(budget.UsedWorkItems, Is.EqualTo(3))
  Expected: 3  But was:  0
```

### Fix 1: Fair round-robin (ScopeRuntimeHost.cs)

- Added `private int _nextInlineScopeIndex;` field to persist rotation state across frames
- Replaced `budget.StartingScopeIndex` (per-frame, always 0) with `_nextInlineScopeIndex`
- Updated `_nextInlineScopeIndex` after each pump loop
- Kept `budget.StartingScopeIndex` assignment for backward compatibility

### Fix 2: Budget consumption (ScopeRuntime.cs)

- Captured `PostPumpStats` from `PostScheduler?.Pump(ref budget)`
- Called `budget.Consume(postStats.ProcessedCount)` to track work items

### GREEN — Tests passing after fixes

```
已通过! - 失败:     0，通过:     9，已跳过:     0，总计:     9 — filtered tests
已通过! - 失败:     0，通过:   818，已跳过:     0，总计:   818 — full suite
```

### Files Modified

| File | Change |
|------|--------|
| `LayerBase/Scope/ScopeRuntimeHost.cs` | Added `_nextInlineScopeIndex`; persistent round-robin cursor |
| `LayerBase/Scope/ScopeRuntime.cs` | `budget.Consume()` after `PostScheduler.Pump(ref budget)` |

### Files Added

| File | Purpose |
|------|---------|
| `LayerBase.Test/InlineScopeFairnessTests.cs` | Verifies fair round-robin across frames |
| Update: `LayerBase.Test/RuntimeScopeBudgetTests.cs` | Verifies `UsedWorkItems` increments on inline pump |

### Constraints

- No new public API introduced
- No changes to `RuntimeFrameBudget` struct
- `budget.StartingScopeIndex` assignment preserved for existing callers
