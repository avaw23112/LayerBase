# Plan: Notify Small-Fanout Fast Path

## Scope

Expected file touches:
- `LayerBase/Event/Event/GlobalEventCenter.cs`
- notify-related tests only if the optimization exposes an uncovered behavior gap

## Steps

1. Inspect the current `EventBucket<T>` rebuild and dispatch state to identify the smallest safe fast-path shape.
2. Add cached shape metadata for a notify-only small-fanout case that avoids the generic global dispatch skeleton.
3. Route qualifying global and local notify dispatches through the specialized path while preserving exception/fault semantics.
4. Keep all other event shapes on the existing general path.
5. Run targeted notify/event-pipeline verification and record remaining risks.

## Risks

- Exception handling must keep disabling faulty handlers consistently with the general notify path.
- Over-specialization could accidentally skip future runtime state transitions if dirty/rebuild bookkeeping is incomplete.
- Benchmark gains may be smaller than expected if the remaining overhead is dominated by delegate invocation rather than dispatch skeleton logic.

## Verification Notes

- Prefer targeted test runs first.
- If tests pass, assess whether a benchmark rerun is necessary before expanding the optimization further.
