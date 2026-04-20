# Plan: Dispatch Isolation And README Alignment

1. Add focused tests that expose the backward-dispatch isolation gap and codify the current Reset ownership boundary.
2. Update `GlobalEventCenter.EventBucket<T>.DispatchSyncBackward(...)` to mirror forward fault recovery without changing propagation order.
3. Trim duplicated README sections and tighten wording around fault isolation and `LayerHub.Reset()`.
4. Run targeted verification, then the full test project.
