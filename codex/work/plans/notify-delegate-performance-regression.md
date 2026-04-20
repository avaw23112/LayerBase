# Plan: Notify Delegate Performance Regression

## Scope

Files expected to change:
- `LayerBase/Event/Event/EventHandler.cs`
- `LayerBase/Event/Event/HandlerBucket.cs`
- `LayerBase/Event/Event/GlobalEventCenter.cs`
- `LayerBase/Layer/Layer.cs`
- `LayerBase/Tools/Timer/TimerScheduler.cs`
- `LayerBase.Generator/LayerBase.Generator/ManagerAutoSubscribeGenerator.cs`
- notify-related tests and benchmarks

## Steps

1. Replace notify callback storage from `Action<Event<T>>` to a byref delegate.
2. Update layer subscribe/unsubscribe APIs and auto-generated bindings to match.
3. Make dispatch lazily create `Event<T>` only when notify handlers are present.
4. Update timer notify-style registration to avoid by-value envelope callbacks in the same way.
5. Update tests/benchmarks to the new notify method signature.
6. Run targeted verification and record residual risks.

## Risks

- Public API compatibility for callers still using `Action<Event<T>>`.
- Auto-subscribe generation for notify handlers with unexpected signatures.
- Hidden copies may remain in timer code if adapter paths are left in place.

## Verification Notes

- Prefer targeted test runs first.
- Only run a benchmark smoke check after the build is green.
