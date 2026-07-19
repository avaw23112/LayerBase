# Task 5：修�?Coalesced 错误淘汰�?O(n²) 退�?
当前单类型数量需要遍历整�?`_coalescedBuffer`；某个类型超限后调用的是全局最旧淘汰，可能删除另一个事件类型�?当前全局列表还使�?`RemoveAt(0)`�?
## 文件

* 修改：`LayerBase/Event/PostScheduler/CoalescingStructures.cs`
* 修改：`LayerBase/Event/PostScheduler/PostScheduler.cs`
* 新增：`LayerBase.Test/PostSchedulerCoalescedEvictionTests.cs`

## 数据结构

将：

```csharp
private readonly List<CoalescedSlotKey> _pendingCoalesced = new();
```

改为�?
```csharp
private readonly LinkedList<CoalescedSlotKey>
    _pendingCoalesced = new();

private readonly Dictionary<int, LinkedList<CoalescedSlotKey>>
    _pendingCoalescedByType = new();
```

`CoalescedSlot` 增加内部节点�?
```csharp
internal LinkedListNode<CoalescedSlotKey>? GlobalOrderNode;
internal LinkedListNode<CoalescedSlotKey>? TypeOrderNode;
```

## 插入

```csharp
LinkedListNode<CoalescedSlotKey> globalNode =
    _pendingCoalesced.AddLast(slotKey);

if (!_pendingCoalescedByType.TryGetValue(
        typeId,
        out LinkedList<CoalescedSlotKey>? typeOrder))
{
    typeOrder = new LinkedList<CoalescedSlotKey>();
    _pendingCoalescedByType.Add(typeId, typeOrder);
}

LinkedListNode<CoalescedSlotKey> typeNode =
    typeOrder.AddLast(slotKey);

newSlot.GlobalOrderNode = globalNode;
newSlot.TypeOrderNode = typeNode;
```

## 单类型淘�?
```csharp
private bool EvictOldestCoalescedSlotForType(int eventTypeId)
{
    if (!_pendingCoalescedByType.TryGetValue(
            eventTypeId,
            out LinkedList<CoalescedSlotKey>? order) ||
        order.First == null)
    {
        return false;
    }

    return RemovePendingCoalescedSlot(
        order.First.Value,
        releasePayload: true,
        out _);
}
```

全局上限才允许使用：

```csharp
_pendingCoalesced.First
```

## 测试

至少包含�?
1. A 类型超限不能删除 B 类型�?2. A 类型 `DropOldest` 删除 A 最�?Key�?3. Snapshot 后所有全局节点和类型节点被清理�?4. 连续 10,000 次插入、淘汰后 Pending 数量不增长�?5. Dispose 不重复释�?Payload�?
提交�?
```powershell
git add LayerBase/Event/PostScheduler `
        LayerBase.Test/PostSchedulerCoalescedEvictionTests.cs

git commit -m "fix(post): make coalesced eviction type-correct and constant-time"
```

---

