## Task 5：改造 Timer Wheel 为 FIFO，并保证异常后结构完整

当前 Wheel 头插形成 LIFO，同一过期 Tick 的 Latest 最终可能保留最早的值；处理槽位时又会先摘除整条链表，Sink 抛异常可能丢失剩余 Timer。

### Files

* Modify: `LayerBase/Event/TimeScheduler/TimeScheduler.cs`
* Modify: `LayerBase/Event/TimeScheduler/PostTimerScheduler.cs`
* Modify: `LayerBase.Test/PostSchedulerLatestTests.cs`
* Create: `LayerBase.Test/TimeSchedulerExceptionSafetyTests.cs`
* Create: `LayerBase.Test/TimeSchedulerBacklogTests.cs`

### Required structure

替换单个 `_wheel` Head 数组：

```csharp
private readonly int[] _wheelHeads;
private readonly int[] _wheelTails;
```

增加：

```csharp
private int _overdueHead = -1;
private int _overdueTail = -1;
private const int OverdueSlotIndex = -2;
```

### FIFO insertion

```csharp
entry.Next = -1;
entry.Prev = tail;

if (tail == -1)
    head = index;
else
    _pool[tail].Next = index;

tail = index;
```

### Backlog

达到 `MaxExpiredPerTick` 后：

* 第一次把剩余链表转为 Overdue Queue 时允许 O(n) 标记 `SlotIndex = OverdueSlotIndex`。
* 后续 Pump 从 `_overdueHead` 分批处理。
* 不允许每帧重新遍历全部剩余节点。
* Cancel 必须支持从 Overdue Queue O(1) 删除。

### Sink 异常处理

每个 Timer 单独执行：

```csharp
try
{
    accepted = sink.TryAcceptExpired(...);
}
catch (Exception ex)
{
    ReleaseTimer(index, ref entry);
    firstException ??= ex;
    accepted = false;
}
```

必须继续修复或处理剩余结构。

处理结束后允许重新抛出 `firstException`，由 Scope 的 Timer Fault 通道处理；但不得遗留 Active 且不可达的 Timer。

### Changes needed

1. **TimeScheduler.cs**:
   - Replace `private readonly int[] _wheel;` with `private readonly int[] _wheelHeads;` and `private readonly int[] _wheelTails;`
   - Add `_overdueHead`, `_overdueTail`, `OverdueSlotIndex`
   - Change `PlaceInWheel` to FIFO insertion
   - Change `ProcessCurrentSlot` to handle backlog (overdue queue)
   - Add per-timer try/catch in the processing loop
   - Change `RemoveFromStructure` to handle overdue queue
   - Change `RequeueRemainingForNextTick` to work with FIFO
   - Change constructor to initialize both `_wheelHeads` and `_wheelTails` with -1

2. **PostTimerScheduler.cs**: 
   - No structural changes needed but may need to compile against new TimeScheduler API

3. **TimeSchedulerBacklogTests.cs**: Test large backlog handling
4. **TimeSchedulerExceptionSafetyTests.cs**: Test sink exception safety

### Tests

```csharp
[TestFixture]
[Category("ProductionHardening")]
public sealed class TimeSchedulerBacklogTests
{
    [Test]
    public void Same_tick_timers_expire_in_schedule_order()
    {
        using var scheduler = new TimeScheduler<int>(TimeSchedulerOptions.Default);
        var expired = new List<int>();
        var sink = new CallbackSink<int>(p => expired.Add(p));

        scheduler.Schedule(1, 0.5f);
        scheduler.Schedule(2, 0.5f);
        scheduler.Schedule(3, 0.5f);

        scheduler.Tick(1f, sink);

        Assert.That(expired, Is.EqualTo(new[] { 1, 2, 3 }));
    }
}

[TestFixture]
[Category("ProductionHardening")]
public sealed class TimeSchedulerExceptionSafetyTests
{
    [Test]
    public void Throwing_sink_does_not_lose_remaining_timers()
    {
        using var scheduler = new TimeScheduler<int>(TimeSchedulerOptions.Default);
        var expired = new List<int>();
        int callCount = 0;

        scheduler.Schedule(1, 0.5f);
        scheduler.Schedule(2, 0.5f);
        scheduler.Schedule(3, 0.5f);

        Assert.That(() => scheduler.Tick(1f, new ThrowingThenCollectingSink(expired)),
            Throws.Exception);

        Assert.That(expired, Has.Count.GreaterThan(0));
        Assert.That(scheduler.PendingCount, Is.EqualTo(0)); // all timers released or processed
    }

    private sealed class ThrowingThenCollectingSink : IExpiredTimerSink<int>
    {
        private readonly List<int> _expired;
        private int _callCount;

        public ThrowingThenCollectingSink(List<int> expired) => _expired = expired;

        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            _callCount++;
            if (_callCount == 2) throw new InvalidOperationException("simulated fault");
            _expired.Add(payload);
            return true;
        }
    }
}
```

For `PostSchedulerLatestTests.cs`: Change expected value from `100` to `300` if test verifies timer Latest keeps last scheduled value.

### Step 2: Verify

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~TimeSchedulerExceptionSafetyTests|FullyQualifiedName~TimeSchedulerBacklogTests"
```

### Step 3: Commit

```powershell
git add LayerBase/Event/TimeScheduler/TimeScheduler.cs LayerBase/Event/TimeScheduler/PostTimerScheduler.cs LayerBase.Test/PostSchedulerLatestTests.cs LayerBase.Test/TimeSchedulerExceptionSafetyTests.cs LayerBase.Test/TimeSchedulerBacklogTests.cs
git commit -m "fix(timer): preserve fifo order and backlog integrity"
```
