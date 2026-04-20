# Task Spec: EventBucket SOA Isolation

## Goal

Reduce hot-path cache pressure in `GlobalEventCenter.EventBucket<T>` by isolating dispatch state and SOA regions so the root `EventBucket<T>` object does not carry every region's references and counters on every dispatch path.

## Context

- Current `EventBucket<T>` stores sync delegate arrays, async delegate arrays, interface-handler arrays, parallel arrays, names, circuits, masks, counts, and ranges directly on one object.
- Dispatch paths do not scan every region every time, but the hot object still holds all region references and metadata.
- The likely problem is hot-object footprint and mixed hot/cold state, not eager per-dispatch traversal of all arrays.

## Constraints

- Preserve existing event semantics.
- Preserve current runtime ownership and rebuild behavior.
- No new dependencies.
- Keep the change bounded to `GlobalEventCenter.EventBucket<T>` unless verification forces broader edits.

## Non-Goals

- Rework handler registration APIs.
- Redesign `HandlerBucket<T>` contents.
- Change fault-isolation semantics.

## Done Criteria

- `EventBucket<T>` root object no longer directly owns every SOA array and counter used by all dispatch modes.
- Dispatch paths resolve only the relevant region objects for the active path.
- Existing tests pass.
- Performance sanity checks do not show an obvious regression on benchmark-style tests.
