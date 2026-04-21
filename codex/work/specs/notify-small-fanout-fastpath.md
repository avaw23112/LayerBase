# Task Spec: Notify Small-Fanout Fast Path

## Goal

Reduce the fixed dispatch overhead for LayerBase notify events in the "many event types / few subscribers per event" scenario by adding a narrow bucket-level fast path, without changing event semantics or replacing the existing dynamic event pipeline.

## Context

- Recent compare benchmarks show LayerBase is still slightly slower than MessagePipe in fixed-batch notify scenarios with many event types and only 2~3 subscribers per event.
- Existing hot-path analysis shows the steady-state send path already benefits from `BucketCache<T>`, so dictionary lookup elimination is not the main remaining cost.
- The current `EventBucket<T>.Dispatch` path still pays a shared dispatch skeleton cost even when an event type only has notify handlers and the fanout is very small.
- A narrower middle-path optimization is preferred over a source-generated static dispatcher tree because it keeps runtime semantics and lifecycle behavior inside the existing bucket model.

## Constraints

- Preserve current LayerBase notify behavior and public API surface.
- Keep the optimization local to the notify dispatch path; do not broaden it into sync/async/parallel routing work.
- Keep dynamic subscription, reset, fault handling, and dirty-rebuild behavior correct.
- No new dependencies.
- Keep the diff small, reversible, and benchmark-driven.

## Non-goals

- Replacing `GlobalEventCenter` with a static generated dispatcher system.
- Optimizing sync, async, parallel, or propagation-heavy paths beyond what is required for correctness.
- Rewriting benchmark structure or changing MessagePipe comparison methodology.
- Expanding `Call` semantics; `Call` remains a single-target functional slice, not a workflow/orchestration boundary.

## Done Criteria

- `EventBucket<T>` detects a narrow notify-only small-fanout shape and uses a dedicated fast path.
- The fast path preserves notify error handling and dirty-state invalidation semantics.
- Non-qualifying event shapes continue to use the existing general dispatch path unchanged.
- Existing notify tests continue to pass.
- Targeted verification confirms the code builds and the dispatch tests remain healthy.

## Verification

- `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~NotifyTests"`
- `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~EventPipelineTests"`
- Optional: benchmark smoke check for the fixed-batch notify compare suite after tests are green.
