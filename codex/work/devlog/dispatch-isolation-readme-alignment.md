# Devlog: Dispatch Isolation And README Alignment

## Start

- Scope fixed to backward sync isolation, Reset semantics coverage, and README cleanup.
- Avoiding lifecycle redesign; documentation will reflect the current caller-owned runtime model.

## Progress

- Added regression coverage for Bubble-direction sync fault recovery.
- Added regression coverage for exact faulted-handler removal inside the backward 4x-unrolled dispatch block.
- Added regression coverage for `LayerHub.Reset()` detaching Hub pumping without disposing retained runtimes.
- Updated `DispatchSyncBackward(...)` to skip the faulted handler and resume remaining sync handlers in the same frame.
- Removed duplicated README advanced-features block and aligned fault-isolation / lifecycle wording with current behavior, including the remaining `LayerHub` global-entry role.
