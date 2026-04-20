# Task Spec: Dispatch Isolation And README Alignment

## Goal

Align LayerBase's observable behavior and documentation around three areas:

- synchronous backward dispatch fault isolation
- `LayerHub.Reset()` lifecycle semantics
- README structure and wording quality

## Context

- `DispatchSync()` already skips a faulted sync handler and resumes later handlers in the same frame.
- `DispatchSyncBackward()` still stops after `HandleFault(...)`, which makes Bubble-direction sync dispatch less isolated than forward paths.
- `README.md` currently duplicates the advanced-features section and overstates some guarantees.
- `LayerHub.Reset()` clears Hub tracking and resets `ServiceLayerBinder`, but does not dispose retained `LayerRuntime` instances.

## Constraints

- Keep the change narrow and reversible.
- Do not introduce new dependencies.
- Preserve existing runtime ownership rules unless a bug requires broader lifecycle changes.
- Lock behavior with tests before implementation when practical.

## Non-Goals

- Redesign `LayerHub` ownership or add automatic runtime disposal.
- Rewrite the entire README from scratch.
- Change async dispatch semantics beyond what is needed for this scoped fix.

## Done Criteria

- Bubble-direction synchronous dispatch continues executing later sync handlers after one handler throws.
- Regression tests cover backward sync isolation and the current `LayerHub.Reset()` ownership boundary.
- `README.md` no longer contains the duplicated advanced-features block.
- README fault-isolation and reset wording matches the implemented behavior.

## Risks

- The backward unrolled loop must preserve handler order and handled-state behavior while adding recovery.
- README cleanup should avoid accidentally dropping valid bilingual content outside the targeted duplicate block.
