## Task 7：限制 Coalesced，并让特殊队列服从 Pump 预算

Coalesced 当前按 Key 扩张 Dictionary/List，新 Key 没有容量限制；FlushBuffers 会先完整排序和派发特殊事件，然后才检查普通队列预算。

### Files

* Modify: `LayerBase/Event/PostScheduler/PostSchedulerOptions.cs`
* Modify: `LayerBase/Event/PostScheduler/PostScheduler.cs`
* Modify: `LayerBase/Event/PostScheduler/PostTypePlan.cs`
* Create: `LayerBase.Test/CoalescedCapacityTests.cs`
* Create: `LayerBase.Test/SpecialPostBudgetTests.cs`

### Configuration

在 `PostSchedulerOptions` 增加：

```csharp
public readonly int MaxSpecialPending = 4096;
```

规则：
* `PostTypePlan.MaxPending > 0` 时优先使用类型级上限。
* 否则使用 `MaxSpecialPending`。
* 新 Coalesced Key 达到上限后应用该 Plan 的 Backpressure。
* 不得先 Store Payload 再发现超限。

### Budgeted snapshots

特殊快照必须保留处理游标：

```csharp
private int _dirtySnapshotWordIndex;
private int _coalescedSnapshotIndex;
private int _latestSnapshotWordIndex;
```

当预算耗尽时：
* 不释放未处理快照。
* 下一 Pump 从游标继续。
* 已处理 Payload 只释放一次。
* 新投递进入 Pending Buffer，不污染当前 Snapshot。

移除每次：
```csharp
_pendingCoalesced.Sort(...)
```

Owner Scope 单线程投递顺序已经由 `_pendingCoalesced` 的插入顺序保证。

### Verification

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~CoalescedCapacityTests|FullyQualifiedName~SpecialPostBudgetTests"
```

### Commit

```powershell
git add LayerBase/Event/PostScheduler/PostSchedulerOptions.cs LayerBase/Event/PostScheduler/PostScheduler.cs LayerBase/Event/PostScheduler/PostTypePlan.cs LayerBase.Test/CoalescedCapacityTests.cs LayerBase.Test/SpecialPostBudgetTests.cs
git commit -m "fix(post): bound and budget special post buffers"
```
