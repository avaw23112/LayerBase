# Task 5 Report: Coalesced Type-Correct Eviction and O(1) Hot Path

## Summary
Replaced the O(n) type count scan, wrong-type global eviction, and O(n) `RemoveAt(0)` in the coalesced event path with type-correct O(1) data structures.

## Changes

### `LayerBase/Event/PostScheduler/CoalescingStructures.cs`
- Added `GlobalOrderNode` and `TypeOrderNode` fields to `CoalescedSlot` struct for O(1) linked-list removal.

### `LayerBase/Event/PostScheduler/PostScheduler.cs`
1. **Data structures**: Replaced `List<CoalescedSlotKey> _pendingCoalesced` with `LinkedList<CoalescedSlotKey>` and added `Dictionary<int, LinkedList<CoalescedSlotKey>> _pendingCoalescedByType`.
2. **Type count** (line 533): Changed from O(n) `foreach` over the entire `_coalescedBuffer` to O(1) `_pendingCoalescedByType[typeId].Count`.
3. **Insertion** (lines 559-569): After creating a new slot, the key is appended to both the global linked list and the per-type linked list, and the corresponding `LinkedListNode` references are stored on the slot.
4. **Eviction**: Replaced `EvictOldestCoalescedSlot()` (global-O(1), wrong-type) with:
   - `EvictOldestCoalescedSlotForType(typeId)` — type-correct eviction using `_pendingCoalescedByType`
   - `EvictOldestCoalescedSlot()` — global eviction (kept for total capacity limits)
5. **Removal helper**: `RemovePendingCoalescedSlot(key, releasePayload, out slot)` removes from global linked list, per-type linked list, and `_coalescedBuffer`, optionally releasing the payload — all O(1).
6. **Snapshot** (lines 674-689): Now iterates via `LinkedListNode` and calls `RemovePendingCoalescedSlot` for each item, ensuring both linked lists are properly drained.
7. **Dispose** (lines 1243-1249): Uses `RemovePendingCoalescedSlot` in a while loop instead of a `foreach` + separate `Clear()`, preventing double-release.
8. **ClearPostBitmaps**: Added `_pendingCoalescedByType.Clear()`.

### `LayerBase.Test/PostSchedulerCoalescedEvictionTests.cs` (new)
5 tests covering:
- Type A overflow does not evict Type B
- `DropOldest` on type A evicts oldest of type A
- Snapshot clears all global and type order nodes
- 10k insert+evict cycles: pending count stays bounded
- Dispose does not double-release payloads

## Verification
- `dotnet test` — 823/823 passed (including 5 new + 5 existing coalesced tests)
