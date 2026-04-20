# Test Spec: Dispatch Isolation And README Alignment

## Automated Verification

1. `EventPipelineTests` should prove Bubble-direction sync dispatch survives a fault and still runs the next handler in the same frame.
2. `EventPipelineTests` should prove `LayerHub.Reset()` removes runtimes from Hub-driven pumping but does not invalidate a runtime still held by the caller.
3. `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug` should pass after the implementation and README changes.

## Manual Verification

1. Inspect `README.md` to confirm the duplicate "进阶特性指南" block is removed.
2. Inspect README wording for:
   - single-frame isolation across synchronous propagation directions
   - explicit caller-managed lifetime after `LayerHub.Reset()`

## Not In Scope

- Async handler fault-recovery semantics beyond existing behavior.
- Benchmark or performance validation.
