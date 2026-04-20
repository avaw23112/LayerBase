# Task Spec: Notify Delegate Performance Regression

## Goal

Remove the notify-path performance regression so that:
- notify dispatch does not copy `Event<T>` per handler invocation
- non-notify dispatch paths do not pay notify-only envelope construction costs

## Context

- Recent notify support added `Action<Event<T>>` into the event pipeline.
- Benchmark output shows the notify path is slower than the original path.
- The original path also regressed after notify support landed.

## Constraints

- Keep existing notify semantics: handlers still receive metadata (`TargetMask`, `Propagation`, payload).
- Avoid broad architectural rewrites.
- No new dependencies.
- Keep diffs reviewable and reversible.

## Non-goals

- Reworking the full event propagation model.
- Replacing BenchmarkDotNet setup.
- Tuning unrelated async/parallel paths.

## Suspected Causes

1. `Action<Event<T>>` passes the event envelope by value, forcing a struct copy on every notify invocation.
2. `GlobalEventCenter.Dispatch*` now constructs `Event<T>` eagerly even when there are no notify subscribers.
3. Timer notify-like callback storage still uses `Action<Event<T>>`, which repeats the same copy pattern.

## Done Criteria

- Notify subscriptions use a true byref delegate shape.
- Dispatch only constructs an `Event<T>` envelope when notify metadata is actually needed.
- Auto-subscribe generation still works for `[SubscribeNotify]`.
- Notify tests pass with the updated signature.
- At least one targeted verification run confirms build/test health.

## Verification

- `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~NotifyTests"`
- `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~PerformanceBenchmarks"`
- Optional benchmark smoke run for notify comparison if time permits.
