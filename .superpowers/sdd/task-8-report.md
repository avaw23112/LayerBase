# Task 8 Report: Timer FireAllCapped Semantics

## Changes Made

### Production Code
- **LayerBase/Event/TimeScheduler/TimeScheduler.cs**: Modified `ProcessCurrentSlot` to add a drain loop that re-processes the overdue queue when `FireAllCapped` timers re-queue themselves after firing. This enables bounded fixed-rate catch-up: a FixedRate repeating timer with `FireAllCapped` that falls behind fires up to `maxExpiredPerTick` times per tick until it catches up.

### Test Files
- **LayerBase.Test/TimerCatchUpPolicyTests.cs** (NEW): Contains 4 tests:
  - `Fire_all_capped_replays_missed_fixed_rate_intervals` — Verifies FireAllCapped catches up (>8 fires vs expected ~11 over 22 ticks)
  - `Skip_missed_only_fires_once_per_tick` — Verifies SkipMissed skips missed intervals (<8 fires)
  - `Fire_all_capped_catches_up_more_than_skip_missed` — Paired comparison showing FireAllCapped catches up strictly more than SkipMissed
  - `Overdue_fairness_is_preserved` — Verifies overdue fairness is not broken by the while loop

## Verification
- All 834 tests pass (0 failures)
- All 53 timer-related tests pass
- Existing `TimerFairnessTests.Overdue_timers_are_not_starved_by_new_ones` continues to pass

## Key Design
The `ProcessCurrentSlot` while-loop (`while (processedInTick < _maxExpiredPerTick && _overdueHead != -1)`) allows a `FireAllCapped` repeating timer that re-adds itself to the overdue queue via `AppendSingleToOverdue` during `RescheduleRepeatSlow` to be processed again in the same tick, up to the per-tick budget. This replaces the previous one-shot overdue queue processing.
