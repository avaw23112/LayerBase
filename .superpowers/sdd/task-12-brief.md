## Task 12：优化 LBTask.Delay 取消复杂度

### Files

* Modify: `LayerBase.Task/LBTask.cs`
* Create: `LayerBase.Task/DelayHeap.cs`，如果当前 Delay 实现仍位于大文件中
* Create: `LayerBase.Task.Tests/LBTaskDelayHeapTests.cs`

### Required behavior

* DelayWorkItem 保存 `HeapIndex`。
* 插入 O(log n)。
* 取消 O(log n)，禁止扫描整个 Heap。
* Heap Swap 时更新两个节点 HeapIndex。
* 完成、取消、异常后 HeapIndex 设为 `-1`。
* 取消注册和 WorkItem 不得长期保留用户状态。
* 10,000 个 Delay 逆序取消不得出现 O(n²) 时间增长。

### Tests

```csharp
Delay_cancel_removes_item_by_heap_index
Delay_heap_indexes_remain_valid_after_swaps
Cancelled_delay_completes_once
Reused_delay_item_ignores_stale_cancellation
```

### Verification

```powershell
dotnet test LayerBase.Task/LayerBase.Task.Tests/LayerBase.Task.Tests.csproj -c Debug --filter "FullyQualifiedName~LBTaskDelayHeapTests"
```

### Commit

```powershell
git add LayerBase.Task/LBTask.cs LayerBase.Task/DelayHeap.cs
git commit -m "perf(task): remove linear delay cancellation"
```
