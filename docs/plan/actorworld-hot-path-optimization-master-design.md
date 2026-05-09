# ActorWorld Hot Path Optimization Master Design

> 文件名：`actorworld-hot-path-optimization-master-design.md`  
> 适用仓库：`avaw23112/LayerBase`  
> 目标：在保持 ActorWorld 语义完整的前提下，降低 `ActorWorld Post + Pump` 热路径用时，并保持预热后 `0B GC Alloc`。  
> 执行对象：Codex / 自动化代码代理 / 开发者。  
> 范围：ActorWorld、Actor 邮箱、Pump 调度、Query.PostAll、ActorRef、Benchmark。  
> 禁止范围：不引入 AttributeSystem、UIBind、Network、Save、Unity/Godot Adapter，不破坏 Actor 生命周期和安全检查。

---

## 1. 当前状态

当前 benchmark 已经显示：

```text
ActorWorld Post + Pump - 20万次
Allocated: -
```

这说明第一阶段的 GC 分配问题已经解决，原先约 `91KB` 的托管堆分配已经清零。

当前性能重点从：

```text
解决 GC 分配
```

转为：

```text
降低纯 CPU 热路径用时
```

当前对比数据大致为：

```text
ActorWorld Post + Pump - 20万次                  约 9,498 μs
LayerBase PostScheduler - 20万次                约 7,299 μs
Dictionary<ActorId, Actor> + interface call      约 1,656 μs
LayerBase Send - 20万次                          约 1,170 μs
Direct method call - 20万次                      约 59 μs
```

重要判断：

```text
ActorWorld 不应该被优化成 Dictionary 直调。
ActorWorld 的目标是带邮箱、帧预算、生命周期、安全检查、Query、Debug 的高性能 Actor Runtime。
```

---

## 2. 优化目标

### 2.1 必须保持

```text
1. 预热后 0B GC Alloc。
2. ActorId / Generation 安全。
3. Actor Alive / PendingDestroy / Destroying 检查。
4. Disabled 策略。
5. Actor 邮箱策略。
6. 帧预算。
7. Query / Tag / Group 语义。
8. Debug Dump 能力。
9. Actor 池化能力。
10. 现有公开 API 尽量不破坏。
```

### 2.2 性能目标

短期目标：

```text
ActorWorld Post + Pump 单次从约 47ns 降到 35~40ns。
```

中期目标：

```text
ActorWorld 单体 Post + Pump 接近 PostScheduler。
Query.PostAll / 同 Archetype 批量路径明显优于普通 Dictionary 组合。
```

长期目标：

```text
ActorWorld 成为真实游戏工程中可用的高性能逻辑对象 Runtime。
```

---

## 3. 热路径拆解

当前热路径大致为：

```text
ActorWorld.Post
  -> ActorId 解析
  -> TypedActorStorage.Post<TEvent>
  -> TryGetColumn
  -> EventColumn.Post
  -> EventMailWriter.Enqueue
  -> DirtySlotList.AddIfNotExists

ActorWorld.Pump
  -> PumpActorBehaviours
  -> TryPumpOne
  -> TryPumpOneFromBuckets
  -> IActorEventBucket.PumpOne
  -> ActorEventBucket<TEvent>.PumpOne
  -> IActorEventColumn<TEvent>.PumpOne
  -> EventColumn<TActor,TEvent>.PumpOne
  -> EventMailReader.TryDequeue
  -> ActorBehaviourInvoker
```

主要成本来源：

```text
1. bucket 空扫描。
2. call bucket 和 event bucket 双路径扫描。
3. bucket / column 接口调用。
4. actor / bucket 公平限流统计。
5. Stopwatch.GetTimestamp 频繁调用。
6. EventMailWriter 策略 switch。
7. RingBuffer 取模。
8. PostAll 没有批量快路径。
9. QueryResult 没有缓存 column。
10. 单体 Post 每次都要重新解析 ActorId / Storage / Column。
```

---

## 4. 优化分级

### 4.1 第一阶段：低风险 CPU 优化

```text
1. 默认关闭 Actor / Bucket 公平限流。
2. 增加 TimeCheckInterval。
3. 跳过空 call bucket / event bucket 大类。
4. CountRemainingDirtyBuckets 改 O(1)。
5. ActorMailPumpStatsBuilder bucket 统计改数组。
6. AddColumn 改 EnsureCapacity。
7. 小方法加 AggressiveInlining。
8. nullable policy 在外层解析成非 nullable。
```

### 4.2 第二阶段：中风险结构优化

```text
1. DirtyBucketList。
2. EventColumn.PumpOneFast。
3. EventColumn.PostQueuedFast。
4. IActorEventColumn<TEvent>[] 改为更低成本调用结构。
5. RingBuffer 取模改 power-of-two mask。
6. SweepPendingDestroy 仅 pendingCount > 0 时执行。
7. DelayScheduler 无任务时跳过 Tick。
8. Debug / Release 统计路径分离。
```

### 4.3 第三阶段：API 级高收益优化

```text
1. ActorRef<TActor>。
2. ActorEventRef<TActor,TEvent>。
3. Query.PostAll 批量快路径。
4. EventColumn.PostToAliveSlotsFast。
5. QueryResult 缓存 EventColumn。
6. DispatchNowFast。
```

### 4.4 第四阶段：架构级优化

```text
1. EventMail<TEvent> single-slot 小邮箱。
2. Latest / Dirty / Coalesced 专用存储。
3. PumpBatch。
4. PumpColumn throughput 模式。
5. Actor 状态 bitset。
6. IsSlotPostable 位图化。
7. 源生成器直接注册 Actor Columns。
8. ActorBehaviourInvoker 静态内联化。
```

---

## 5. 方案一：默认关闭 Actor / Bucket 公平限流

### 5.1 问题

当前默认配置如果是：

```text
MaxMailsPerBucketPerPump = 128
MaxMailsPerActorPerPump = 8
```

则每处理一封 Actor 邮件，都会走：

```text
CanProcessBucket
RecordBucketProcessed
CanProcessActor
RecordActorProcessed
```

即使已经清除了分配，这些统计仍然有 CPU 成本。

### 5.2 修改目标

将默认配置改为：

```text
MaxMailsPerBucketPerPump = 0
MaxMailsPerActorPerPump = 0
```

含义：

```text
0 表示不限制单个 Bucket / Actor 的处理数量。
默认只保留 MaxTotalMailsPerPump 作为总预算。
```

### 5.3 推荐代码

目标文件：

```text
LayerBase/Actor/Pump/ActorMailPumpOptions.cs
```

```csharp
namespace LayerBase.Actor;

public readonly struct ActorMailPumpOptions
{
    public readonly int MaxTotalMailsPerPump;
    public readonly int MaxMailsPerBucketPerPump;
    public readonly int MaxMailsPerActorPerPump;
    public readonly int MaxEmptyBucketChecksPerPump;
    public readonly int TimeCheckInterval;

    public ActorMailPumpOptions(
        int maxTotalMailsPerPump,
        int maxMailsPerBucketPerPump,
        int maxMailsPerActorPerPump,
        int maxEmptyBucketChecksPerPump,
        int timeCheckInterval)
    {
        // maxTotalMailsPerPump 参数：
        // 单次 Pump 最多处理的 Actor 邮件数量。
        // 小于等于 0 表示不限制总数。
        MaxTotalMailsPerPump = maxTotalMailsPerPump;

        // maxMailsPerBucketPerPump 参数：
        // 单个事件 bucket 单次 Pump 最多处理多少邮件。
        // 小于等于 0 表示不启用 bucket 公平限流。
        MaxMailsPerBucketPerPump = maxMailsPerBucketPerPump;

        // maxMailsPerActorPerPump 参数：
        // 单个 Actor 单次 Pump 最多处理多少邮件。
        // 小于等于 0 表示不启用 Actor 公平限流。
        MaxMailsPerActorPerPump = maxMailsPerActorPerPump;

        // maxEmptyBucketChecksPerPump 参数：
        // 最多允许连续检查多少个空 bucket。
        // 该参数用于避免本帧在空 bucket 扫描上浪费过多时间。
        MaxEmptyBucketChecksPerPump = maxEmptyBucketChecksPerPump;

        // timeCheckInterval 参数：
        // 每处理多少封邮件检查一次时间预算。
        // 1 表示每封都检查。
        // 64 表示每 64 封检查一次。
        TimeCheckInterval = Math.Max(timeCheckInterval, 1);
    }

    public static ActorMailPumpOptions Default => new(
        maxTotalMailsPerPump: 1024,
        maxMailsPerBucketPerPump: 0,
        maxMailsPerActorPerPump: 0,
        maxEmptyBucketChecksPerPump: 64,
        timeCheckInterval: 64);

    public static ActorMailPumpOptions Fair => new(
        maxTotalMailsPerPump: 1024,
        maxMailsPerBucketPerPump: 128,
        maxMailsPerActorPerPump: 8,
        maxEmptyBucketChecksPerPump: 64,
        timeCheckInterval: 16);
}
```

### 5.4 DoD

```text
1. 默认 ActorWorld benchmark 不再走 actor / bucket Dictionary 统计。
2. Fair 配置仍然保留旧公平行为。
3. 所有原有公平限流测试通过。
```

---

## 6. 方案二：增加 TimeCheckInterval

### 6.1 问题

当前 ActorWorld Pump 循环频繁调用：

```text
Stopwatch.GetTimestamp()
```

每封邮件都检查时间预算时，CPU 成本会固定叠加。

### 6.2 修改目标

在 `ActorMailPumpOptions` 中加入：

```text
TimeCheckInterval
```

默认值：

```text
64
```

含义：

```text
每处理 64 封 Actor 邮件检查一次时间预算。
```

### 6.3 推荐代码片段

目标文件：

```text
LayerBase/Actor/Storage/ActorWorld.Pump.cs
```

```csharp
private ActorMailPumpStats PumpActorBehaviours(
    ref RuntimeFrameBudget budget,
    in ActorMailPumpOptions options)
{
    // budget 参数：
    // 当前帧预算。
    // 包含事件数量预算和时间预算。
    //
    // options 参数：
    // Actor 邮箱 Pump 策略。
    // TimeCheckInterval 用于降低 Stopwatch.GetTimestamp 的调用频率。

    ActorMailPumpStatsBuilder stats = _mailPumpStatsBuilder;
    stats.Reset();

    int processedSinceTimeCheck = 0;

    while (budget.HasRemainingEventBudget()
           && (options.MaxTotalMailsPerPump <= 0 || stats.ProcessedTotal < options.MaxTotalMailsPerPump))
    {
        // 作用说明：
        // 不再每轮都检查时间。
        // 每处理 TimeCheckInterval 封邮件后再检查一次。
        // 这样可以减少高吞吐 Pump 中 Stopwatch.GetTimestamp 的固定开销。
        if (processedSinceTimeCheck <= 0)
        {
            if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
            {
                break;
            }

            processedSinceTimeCheck = options.TimeCheckInterval;
        }

        PumpOneResult result = TryPumpOne(ref budget, options, stats);

        if (result == PumpOneResult.Processed)
        {
            processedSinceTimeCheck--;
            continue;
        }

        if (result == PumpOneResult.EmptyBucket)
        {
            stats.EmptyBucketChecks++;

            if (options.MaxEmptyBucketChecksPerPump > 0
                && stats.EmptyBucketChecks >= options.MaxEmptyBucketChecksPerPump)
            {
                break;
            }

            continue;
        }

        if (result == PumpOneResult.BucketLimited ||
            result == PumpOneResult.ActorLimited ||
            result == PumpOneResult.NoWork)
        {
            break;
        }
    }

    return stats.Build(CountRemainingDirtyBuckets());
}
```

### 6.4 DoD

```text
1. 默认 TimeCheckInterval = 64。
2. 设置 TimeCheckInterval = 1 时语义接近旧行为。
3. benchmark 中 ActorWorld Post + Pump 用时下降或不退化。
```

---

## 7. 方案三：跳过空 call bucket / event bucket 大类

### 7.1 问题

当前 `TryPumpOne` 先扫 call bucket，再扫 event bucket。

如果当前没有 call 邮件，也仍然会进入 call bucket 路径。

### 7.2 修改目标

维护：

```text
_dirtyCallBucketCount
_dirtyEventBucketCount
```

如果为 0，则跳过对应 bucket 数组。

### 7.3 设计

新增字段：

```csharp
private int _dirtyCallBucketCount;
private int _dirtyEventBucketCount;
```

需要由 DirtyBucketList 方案配合维护。  
如果暂时不实现 DirtyBucketList，可以先维护粗略 bool：

```csharp
private bool _hasCallPendingWork;
private bool _hasEventPendingWork;
```

但最终建议使用 DirtyBucketList。

### 7.4 DoD

```text
1. 纯 event benchmark 不进入 call bucket 扫描。
2. 纯 call benchmark 不进入 event bucket 扫描。
3. call / event 混合场景仍然正确处理。
```

---

## 8. 方案四：DirtyBucketList

### 8.1 问题

当前 bucket Pump 是扫描 bucket 数组：

```text
遍历 buckets
遇到 null continue
遇到空 bucket continue
```

事件类型越多，空扫描越浪费。

### 8.2 目标

新增 DirtyBucketList，只扫描有 pending work 的 bucket。

### 8.3 核心设计

新增内部类：

```csharp
namespace LayerBase.Actor;

internal sealed class DirtyBucketList
{
    private int[] _items;
    private bool[] _contains;
    private int _head;
    private int _count;

    public int Count => _count;

    public DirtyBucketList(int initialCapacity = 4)
    {
        // initialCapacity 参数：
        // 初始 bucket 数组容量。
        // 用于减少运行期扩容。
        int capacity = Math.Max(initialCapacity, 4);
        _items = new int[capacity];
        _contains = new bool[capacity];
    }

    public void AddIfNotExists(int bucketIndex)
    {
        // bucketIndex 参数：
        // 有 pending work 的 bucket 下标。
        //
        // 作用说明：
        // 同一个 bucket 在未清空前只能进入 dirty list 一次。
        EnsureContainsCapacity(bucketIndex + 1);

        if (_contains[bucketIndex])
        {
            return;
        }

        _contains[bucketIndex] = true;

        EnsureItemCapacity(_count + 1);

        int tail = (_head + _count) % _items.Length;
        _items[tail] = bucketIndex;
        _count++;
    }

    public bool TryPeek(out int bucketIndex)
    {
        if (_count == 0)
        {
            bucketIndex = default;
            return false;
        }

        bucketIndex = _items[_head];
        return true;
    }

    public void Pop()
    {
        if (_count == 0)
        {
            return;
        }

        int bucketIndex = _items[_head];

        if ((uint)bucketIndex < (uint)_contains.Length)
        {
            _contains[bucketIndex] = false;
        }

        _head = (_head + 1) % _items.Length;
        _count--;

        if (_count == 0)
        {
            _head = 0;
        }
    }

    public void MoveHeadToTail()
    {
        if (_count <= 1)
        {
            return;
        }

        int value = _items[_head];
        _head = (_head + 1) % _items.Length;

        int tail = (_head + _count - 1) % _items.Length;
        _items[tail] = value;
    }

    private void EnsureItemCapacity(int required)
    {
        if (required <= _items.Length)
        {
            return;
        }

        int newCapacity = _items.Length;

        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        int[] newItems = new int[newCapacity];

        for (int i = 0; i < _count; i++)
        {
            newItems[i] = _items[(_head + i) % _items.Length];
        }

        _items = newItems;
        _head = 0;
    }

    private void EnsureContainsCapacity(int required)
    {
        if (required <= _contains.Length)
        {
            return;
        }

        int newCapacity = _contains.Length;

        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        Array.Resize(ref _contains, newCapacity);
    }
}
```

### 8.4 接入点

需要让 `EventColumn` 在从空变非空时通知 bucket。

推荐链路：

```text
EventMailWriter.Enqueue
  -> mail.Count 从 0 变 1
  -> DirtySlotList.AddIfNotExists(slotIndex)
  -> EventColumn.NotifyOwnerHasWork()
  -> ActorEventBucket.NotifyDirty()
  -> ActorWorld.DirtyBucketList.AddIfNotExists(bucketIndex)
```

如果直接改动较大，可以先在 `ActorEventBucket.HasPendingWork()` 层验证，再逐步接入。

### 8.5 DoD

```text
1. ActorWorld Pump 不再扫描全部 event bucket。
2. CountRemainingDirtyBuckets 可 O(1) 返回 DirtyBucketList.Count。
3. 空 bucket 数组很大时，benchmark 明显改善。
4. 多事件类型混合投递仍正确。
```

---

## 9. 方案五：CountRemainingDirtyBuckets 改 O(1)

### 9.1 问题

当前每次 Pump 结束后，如果 `CountRemainingDirtyBuckets()` 扫描所有 call bucket 和 event bucket，会产生额外成本。

### 9.2 修改目标

DirtyBucketList 完成后：

```csharp
private int CountRemainingDirtyBuckets()
{
    // 作用说明：
    // DirtyBucketList.Count 已经代表还有 pending work 的 bucket 数量。
    // 不再扫描全部 bucket。
    return _dirtyCallBuckets.Count + _dirtyEventBuckets.Count;
}
```

### 9.3 DoD

```text
1. CountRemainingDirtyBuckets 不再循环扫描 bucket 数组。
2. LastMailPumpStats.RemainingDirtyBuckets 语义保持正确。
```

---

## 10. 方案六：EventColumn.PumpOneFast

### 10.1 问题

完整 `PumpOne` 每次都走：

```text
Actor limit
actorKey
RecordActorProcessed
ReleaseIfEmpty
复杂 options 判断
```

但默认高性能配置下，这些都可以跳过。

### 10.2 Fast Path 启用条件

```text
MaxMailsPerActorPerPump <= 0
ReleaseWhenEmpty == false
```

可以再加：

```text
MaxMailsPerBucketPerPump <= 0
```

由 bucket 层判断。

### 10.3 推荐设计

在 `EventColumn<TActor,TEvent>` 中新增：

```csharp
public ActorColumnPumpResult PumpOneFast(
    ref RuntimeFrameBudget budget,
    ActorMailPumpStatsBuilder stats)
{
    // budget 参数：
    // 当前帧预算。
    //
    // stats 参数：
    // 当前 Pump 统计。
    //
    // 作用说明：
    // Fast Path 用于默认高吞吐配置：
    // 1. 不启用单 Actor 公平限流。
    // 2. 邮箱空后不释放 buffer。
    // 3. 不构造 actorKey。
    // 4. 不访问 actor processed dictionary。

    while (_dirtySlots.TryPeek(out int slotIndex))
    {
        ref EventMail<TEvent> mail = ref _mails[slotIndex];

        if (!EventMailReader.TryDequeue(ref mail, _bufferPool, out TEvent value))
        {
            _dirtySlots.Pop();
            continue;
        }

        if (!_owner.IsAliveSlot(slotIndex))
        {
            _dirtySlots.Pop();
            continue;
        }

        TActor? actor = _owner.Actors[slotIndex];

        if (actor == null)
        {
            _dirtySlots.Pop();
            continue;
        }

        _invoker(actor, in value);
        budget.ConsumeEvent();

        if (mail.Count == 0)
        {
            _dirtySlots.Pop();
        }
        else
        {
            _dirtySlots.MoveHeadToTail();
        }

        return ActorColumnPumpResult.Processed;
    }

    return ActorColumnPumpResult.NoWork;
}
```

### 10.4 DoD

```text
1. 默认 ActorWorld Pump 使用 PumpOneFast。
2. Fair 配置仍走完整 PumpOne。
3. 行为测试全部通过。
4. benchmark 用时下降。
```

---

## 11. 方案七：EventColumn.PostQueuedFast

### 11.1 问题

当前 `EventMailWriter.Enqueue` 内部按策略 switch：

```text
Queued
Latest
Coalesced
Dirty
```

但默认路径通常是：

```text
Queued + Grow
```

### 11.2 修改目标

为默认策略增加快速入队。

### 11.3 推荐设计

在 `EventColumn<TActor,TEvent>.Post` 中判断：

```csharp
if (CanUseQueuedFastPath(postPolicy, fullPolicy))
{
    return PostQueuedFast(slotIndex, in value);
}
```

示例：

```csharp
private PostResult PostQueuedFast(int slotIndex, in TEvent value)
{
    // slotIndex 参数：
    // 目标 Actor slot。
    //
    // value 参数：
    // 要投递的事件值。
    //
    // 作用说明：
    // 该路径只处理默认 Queued + Grow。
    // 目的是绕过 ActorPostPolicy? / ActorMailFullPolicy? 合并和 switch。

    ref EventMail<TEvent> mail = ref _mails[slotIndex];

    if (mail.BufferId == 0)
    {
        mail.BufferId = _bufferPool.Rent(_options.InitialCapacity);
        mail.Head = 0;
        mail.Count = 0;
        mail.Capacity = _bufferPool.GetCapacity(mail.BufferId);
    }

    if (mail.Count >= mail.Capacity)
    {
        if (!TryGrowFast(ref mail))
        {
            return PostResult.Failure(
                ActorPostStatus.MailFullRejected,
                "Actor mail reached max capacity.",
                PostFailureKind.MailboxFull);
        }
    }

    int tail = (mail.Head + mail.Count) & (mail.Capacity - 1);
    _bufferPool.Write(mail.BufferId, tail, in value);
    mail.Count++;

    if (mail.Count == 1)
    {
        _dirtySlots.AddIfNotExists(slotIndex);
        // 后续 DirtyBucketList 接入时，应在这里通知 bucket dirty。
    }

    return PostResult.Success;
}
```

注意：

```text
该代码使用 & 取模，要求 mail.Capacity 是 2 的幂。
如果未完成 power-of-two 规范化，不要直接替换 %。
```

### 11.4 DoD

```text
1. 默认 Post 走 PostQueuedFast。
2. 非默认策略仍走 EventMailWriter.Enqueue。
3. mailbox full / grow 语义保持正确。
4. benchmark 用时下降。
```

---

## 12. 方案八：RingBuffer 取模改 power-of-two mask

### 12.1 问题

当前环形队列计算通常使用：

```text
(head + count) % capacity
```

`%` 比 `&` 成本更高。

### 12.2 修改目标

保证 capacity 永远为 2 的幂：

```text
4, 8, 16, 32, 64
```

然后将：

```text
x % capacity
```

替换为：

```text
x & (capacity - 1)
```

### 12.3 需要新增工具方法

```csharp
private static int NormalizePowerOfTwo(int value)
{
    // value 参数：
    // 用户传入或系统计算的容量。
    //
    // 返回：
    // 大于等于 value 的最小 2 的幂。
    //
    // 作用说明：
    // 环形队列使用位运算取模时，容量必须是 2 的幂。

    if (value <= 1)
    {
        return 1;
    }

    int result = 1;

    while (result < value)
    {
        result <<= 1;
    }

    return result;
}
```

### 12.4 修改点

```text
RingQueueBuffer.Rent
RingQueueBuffer.Resize
ActorMailOptions constructor 可选校验
EventMailWriter tail/head 计算
EventMailReader head 计算
```

### 12.5 DoD

```text
1. 所有 mailbox capacity 均为 2 的幂。
2. 环形队列读写测试通过。
3. benchmark 用时下降或不退化。
```

---

## 13. 方案九：减少接口调用

### 13.1 问题

当前路径有两层接口调用：

```text
IActorEventBucket.PumpOne
IActorEventColumn<TEvent>.PumpOne
```

### 13.2 修改方向

将：

```text
IActorEventColumn<TEvent>[]
```

改为：

```text
ActorEventColumnRuntime[]
```

或者：

```text
ActorEventColumnBase<TEvent>[]
```

### 13.3 推荐保守方案

第一版只把 bucket 中 column 存储从接口数组改为基类数组。

```text
接口数组 -> 抽象基类数组
```

这样降低接口分派成本，但不大改架构。

### 13.4 DoD

```text
1. Column Pump 行为不变。
2. benchmark 用时下降或不退化。
3. 代码复杂度不能明显上升。
```

---

## 14. 方案十：ActorRef<TActor>

### 14.1 问题

普通 `ActorWorld.Post(actorId, event)` 每次都需要：

```text
ActorId 解码
Storage 查找
Generation 检查
TryGetColumn
Column.Post
```

对高频点对点场景不够快。

### 14.2 目标

新增：

```text
ActorRef<TActor>
```

缓存：

```text
TypedActorStorage<TActor>
slotIndex
generation
```

### 14.3 推荐 API

```csharp
namespace LayerBase.Actor;

public readonly struct ActorRef<TActor>
    where TActor : class, IActor
{
    private readonly TypedActorStorage<TActor>? _storage;
    private readonly int _slotIndex;
    private readonly int _generation;

    internal ActorRef(
        TypedActorStorage<TActor> storage,
        int slotIndex,
        int generation)
    {
        // storage 参数：
        // 当前 Actor 所在的 typed storage。
        //
        // slotIndex 参数：
        // Actor 在 storage 中的 slot。
        //
        // generation 参数：
        // ActorId generation，用于防止旧引用误命中新 Actor。
        _storage = storage;
        _slotIndex = slotIndex;
        _generation = generation;
    }

    public bool IsAlive
    {
        get
        {
            // 作用说明：
            // 即使 ActorRef 缓存了 storage 和 slot，也必须检查 generation。
            // 否则 slot 被复用后，旧 ActorRef 可能误操作新 Actor。
            return _storage != null && _storage.IsAlive(_slotIndex, _generation);
        }
    }

    public PostResult Post<TEvent>(in TEvent value)
        where TEvent : struct
    {
        // value 参数：
        // 要投递给 Actor 的事件。
        //
        // 作用说明：
        // 该路径绕过 ActorId 解码和 ActorWorld storage 查找。
        // 但仍然保留 generation 安全检查。

        if (_storage == null || !_storage.IsAlive(_slotIndex, _generation))
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "ActorRef target is not alive.",
                PostFailureKind.ActorNotFound);
        }

        return _storage.Post(_slotIndex, in value, null, null);
    }
}
```

### 14.4 获取方式

```csharp
public ActorRef<TActor> GetActorRef<TActor>(ActorId actorId)
    where TActor : class, IActor
{
    // actorId 参数：
    // 目标 ActorId。
    //
    // 作用说明：
    // 只在创建 ref 时解析 ActorId。
    // 后续高频 Post 使用 ActorRef 缓存路径。

    // 按当前 ActorId 编码实现解析 storage / slot / generation。
}
```

### 14.5 DoD

```text
1. ActorRef<TActor>.Post 比 ActorWorld.Post 更快。
2. Actor 销毁并复用 slot 后，旧 ActorRef 不能命中新 Actor。
3. ActorRef API 不破坏现有 ActorWorld.Post。
```

---

## 15. 方案十一：ActorEventRef<TActor,TEvent>

### 15.1 目标

进一步缓存 EventColumn：

```text
TypedActorStorage<TActor>
EventColumn<TActor,TEvent>
slotIndex
generation
```

用于极高频同事件投递。

### 15.2 使用场景

```text
玩家每帧向 HUDActor 投递 UpdateHudEvent
AI 高频向目标投递 ThreatEvent
技能系统持续向同一 Actor 投递 TickDamageEvent
```

### 15.3 风险

```text
API 暴露后，用户可能长期保存失效引用。
必须严格检查 generation。
```

建议作为 advanced API，不作为普通文档主推。

---

## 16. 方案十二：Query.PostAll 批量快路径

### 16.1 问题

当前 `PostToAliveActors` 是逐 slot 调用 `column.Post`。

这会重复：

```text
policy 合并
strategy switch
PostResult 创建
IsSlotPostable
```

### 16.2 目标

为批量场景增加：

```text
PostToAliveSlotsFast
```

逻辑：

```text
同一个 storage
同一个 column
同一个事件
连续 slot 遍历
直接写 mailbox
```

### 16.3 推荐设计

在 `EventColumn<TActor,TEvent>` 新增：

```csharp
public void PostToAliveSlotsFast(
    TActor?[] actors,
    ActorSlotState[] states,
    bool[] enabled,
    int maxSlot,
    in TEvent value)
{
    // actors 参数：
    // 当前 storage 的 Actor 数组。
    //
    // states 参数：
    // 当前 storage 的 slot 状态数组。
    //
    // enabled 参数：
    // 当前 storage 的启用状态数组。
    //
    // maxSlot 参数：
    // 当前有效 slot 上限。
    //
    // value 参数：
    // 要批量投递的事件值。
    //
    // 作用说明：
    // 该方法用于 Query.PostAll / PostToAliveActors 的默认快路径。
    // 它避免每个 slot 都重复走完整 PostResult 和策略分发。

    for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
    {
        if (actors[slotIndex] == null)
        {
            continue;
        }

        if (states[slotIndex] != ActorSlotState.Alive)
        {
            continue;
        }

        if (_options.DisabledPolicy == ActorMailDisabledPolicy.Reject && !enabled[slotIndex])
        {
            continue;
        }

        _ = PostQueuedFast(slotIndex, in value);
    }
}
```

### 16.4 DoD

```text
1. Query.PostAll benchmark 增加。
2. PostAll 默认策略走批量快路径。
3. 非默认策略仍走完整路径。
4. 语义保持正确。
```

---

## 17. 方案十三：QueryResult 缓存 EventColumn

### 17.1 问题

QueryResult 如果只缓存 Archetype / Storage，那么每次 PostAll 仍然要 `TryGetColumn`。

### 17.2 目标

对常用事件类型缓存 column：

```text
QueryPostCache<TEvent>
```

包含：

```text
EventColumn<TActor,TEvent>[]
Storage[]
```

### 17.3 设计

```text
首次 query.PostAll<TEvent>
  -> 为每个 storage 找 column
  -> 缓存 column
后续同一 query + same TEvent
  -> 直接使用 column array
```

### 17.4 DoD

```text
1. Query.PostAll<TEvent> 第二次开始更快。
2. QueryCache 失效时，PostCache 同步失效。
3. 不影响 Query 的 Include / Exclude / Tag / Group 语义。
```

---

## 18. 方案十四：EventMail<TEvent> single-slot 小邮箱

### 18.1 问题

大多数 Actor 每帧同类事件可能只有 0 或 1 封。

当前即使只有一封，也需要 ring buffer。

### 18.2 目标

让 `EventMail<TEvent>` 支持：

```text
SingleValue
RingBuffer
```

伪结构：

```csharp
internal struct EventMail<TEvent>
    where TEvent : struct
{
    public TEvent SingleValue;
    public int BufferId;
    public int Head;
    public int Count;
    public int Capacity;
}
```

规则：

```text
Count == 0：空
Count == 1 && BufferId == 0：SingleValue 有效
Count >= 1 && BufferId != 0：RingBuffer 有效
```

第二封 Queued 事件进入时，再把 SingleValue 搬到 RingBuffer。

### 18.3 收益

```text
减少 buffer 读写
减少 head/capacity 处理
减少 cache miss
```

### 18.4 风险

```text
实现复杂
所有策略都要重新验证
Queued / Latest / Dirty / Coalesced 都受影响
```

建议放到后期。

---

## 19. 方案十五：Latest / Dirty / Coalesced 专用存储

### 19.1 问题

不同策略对存储需求不同：

```text
Queued：需要队列
Latest：只保留最后一个值
Dirty：只保留一个值或一个 dirty 标记
Coalesced：只保留合并后的一个值
```

当前统一走 `EventMail + RingQueueBuffer`，对 Latest / Dirty 来说偏重。

### 19.2 目标

拆分存储：

```text
QueuedEventColumn
LatestEventColumn
DirtyEventColumn
CoalescedEventColumn
```

或者在同一个 column 内部按策略初始化不同存储。

### 19.3 收益

```text
Latest / Dirty / Coalesced 更快
减少内存结构复杂度
```

### 19.4 风险

```text
架构复杂
测试量大
可能影响现有统一逻辑
```

建议放到第四阶段。

---

## 20. 方案十六：PumpBatch / Throughput 模式

### 20.1 问题

当前 `PumpOne` 每次只处理一封邮件，然后返回外层循环。

这会造成 20 万封邮件经历 20 万次：

```text
ActorWorld.TryPumpOne
ActorEventBucket.PumpOne
EventColumn.PumpOne
```

### 20.2 目标

新增：

```text
PumpBatch
ActorMailPumpMode.Throughput
ActorMailPumpMode.Fair
```

语义：

```text
Fair：每次尽量公平轮转，避免单 Actor / 单事件类型霸占。
Throughput：优先快速排空当前 column / slot。
```

### 20.3 DoD

```text
1. Default 可以考虑 Throughput。
2. Fair 保留旧语义。
3. PumpBatch benchmark 明显提升。
```

---

## 21. 方案十七：生命周期 Pump 活跃列表

### 21.1 目标

如果生命周期系统仍然扫描所有 Actor，再判断是否 runnable，则改为维护：

```text
FixedUpdateList
UpdateList
LateUpdateList
```

Actor 创建 / Enable / Disable / Destroy 时维护列表。

### 21.2 DoD

```text
1. 无 Update Actor 时 Lifecycle.PumpUpdate 快速返回。
2. 大量无生命周期 Actor 不影响生命周期 Pump。
3. Enable / Disable / Destroy 后列表正确。
```

---

## 22. 方案十八：SweepPendingDestroy 快速跳过

### 22.1 问题

`ActorWorld.Pump` 中会多次调用 `SweepPendingDestroy`。

### 22.2 目标

维护：

```text
_pendingDestroyCount
```

当为 0 时：

```csharp
if (_pendingDestroyCount == 0)
{
    return;
}
```

### 22.3 DoD

```text
1. 没有 pending destroy 时 SweepPendingDestroy O(1)。
2. DestroyActor 后 pending count 正确增加。
3. Sweep 后 pending count 正确归零或减少。
```

---

## 23. 方案十九：DelayScheduler 无任务时跳过 Tick

### 23.1 问题

`ActorWorld.Pump` 开头会调用 `DelayScheduler.Tick(deltaTime)`。

如果没有延迟任务，应快速跳过。

### 23.2 目标

新增：

```text
DelayScheduler.HasPending
```

ActorWorld 中：

```csharp
if (DelayScheduler.HasPending)
{
    DelayScheduler.Tick(deltaTime);
}
```

### 23.3 DoD

```text
1. 无 delay task 时 ActorWorld.Pump 不进入 DelayScheduler.Tick。
2. 有 delay task 时行为不变。
```

---

## 24. 方案二十：Debug / Stats 路径分离

### 24.1 问题

详细统计有价值，但不一定需要默认开启全部。

### 24.2 目标

新增：

```text
ActorMailStatsMode.None
ActorMailStatsMode.Basic
ActorMailStatsMode.Detailed
```

默认：

```text
Basic
```

或者对 benchmark 配置：

```text
None
```

### 24.3 DoD

```text
1. None 模式下不记录 actor / bucket 详细统计。
2. Detailed 模式保留完整调试信息。
3. LastMailPumpStats 基础字段仍可用。
```

---

## 25. 方案二十一：PostResult 快路径

### 25.1 目标

内部 fast path 可以返回：

```text
ActorPostStatus
```

只有外部需要完整结果时才包装为 `PostResult`。

### 25.2 原则

```text
公开 API 不删除 PostResult。
内部成功路径避免构造复杂失败信息。
失败路径按需创建 message。
```

### 25.3 DoD

```text
1. 成功路径不构造字符串。
2. 失败路径 Debug 信息仍完整。
```

---

## 26. 方案二十二：源生成器直接注册 Actor Columns

### 26.1 问题

当前 column 构建阶段存在 `MethodInfo`，属于构建期成本。

### 26.2 目标

源生成器生成：

```text
RegisterActorColumns(storage)
```

避免运行时反射创建 column。

### 26.3 DoD

```text
1. Build Runtime 更快。
2. 运行期 Post/Pump 不受影响。
3. 不作为当前热路径第一优先级。
```

---

## 27. 方案二十三：ActorBehaviourInvoker 静态内联化

### 27.1 目标

源生成器生成静态调用器：

```csharp
static void Invoke(TActor actor, in TEvent value)
{
    // actor 参数：
    // 目标 Actor。
    //
    // value 参数：
    // 要传入 ActorBehaviour 的事件。
    //
    // 作用说明：
    // 让 JIT 更容易内联，降低 delegate 调用开销。
    actor.OnEvent(in value);
}
```

### 27.2 风险

```text
delegate 调用在 .NET 8 已经不差
收益可能有限
生成器复杂度会上升
```

建议后做。

---

## 28. Benchmark 拆分计划

当前 `ActorWorld Post + Pump` 把 Post 和 Pump 混在一起，不利于定位。

新增 benchmark：

```text
ActorWorld_Post_SingleActor_200k
ActorWorld_Pump_SingleActor_200k_Preposted
ActorWorld_PostPump_SingleActor_200k
ActorWorld_PostAll_1000Actors
ActorWorld_QueryPostAll_1000Actors
ActorWorld_ActorRefPost_200k
ActorWorld_DispatchNow_200k
ActorWorld_Pump_OneEventType_ManyBuckets
ActorWorld_Pump_ManyEventTypes_OneDirtyBucket
```

### 28.1 DoD

```text
1. 能单独看出 Post 成本。
2. 能单独看出 Pump 成本。
3. 能验证 DirtyBucketList 收益。
4. 能验证 ActorRef 收益。
5. 能验证 Query.PostAll 收益。
```

---

## 29. 测试计划

### 29.1 正确性测试

```text
1. ActorWorld.Post 后 Pump 能调用 handler。
2. Actor pending destroy 后不能投递。
3. Actor destroying 后不能投递。
4. DisabledPolicy.Reject 生效。
5. Fair 配置下 Actor limit 生效。
6. Fair 配置下 Bucket limit 生效。
7. Default 配置下不启用 Actor / Bucket limit。
8. DirtyBucketList 不重复加入同一 bucket。
9. DirtyBucketList 在 bucket 空后移除。
10. ActorRef 旧 generation 失效。
11. Query.PostAll 命中正确 Actor。
12. Query.PostAll 不命中 excluded Tag / Group。
```

### 29.2 分配测试

```text
1. ActorWorld Post + Pump 预热后 0B。
2. ActorRef.Post 预热后 0B。
3. Query.PostAll 预热后 0B。
4. DirtyBucketList 稳态 0B。
5. RingQueueBuffer 稳态 0B。
```

### 29.3 性能测试

```text
1. ActorWorld Post + Pump 20万次。
2. Post only 20万次。
3. Pump only 20万次。
4. Query.PostAll 1000 Actor。
5. ActorRef.Post 20万次。
6. DirtyBucketList 多 bucket 场景。
```

---

## 30. 推荐执行顺序

### 30.1 第一批，立即执行

```text
1. 默认关闭 Actor / Bucket 公平限流。
2. 增加 TimeCheckInterval。
3. AddColumn 改 EnsureCapacity。
4. SweepPendingDestroy pendingCount 快速跳过。
5. DelayScheduler.HasPending 快速跳过。
6. 小方法 AggressiveInlining。
7. Benchmark 拆分。
```

### 30.2 第二批，结构优化

```text
1. DirtyBucketList。
2. CountRemainingDirtyBuckets O(1)。
3. 跳过空 call / event bucket 大类。
4. EventColumn.PumpOneFast。
5. EventColumn.PostQueuedFast。
6. RingBuffer power-of-two mask。
```

### 30.3 第三批，API 优化

```text
1. ActorRef<TActor>。
2. ActorEventRef<TActor,TEvent>。
3. Query.PostAll 批量快路径。
4. QueryResult 缓存 EventColumn。
```

### 30.4 第四批，架构优化

```text
1. EventMail single-slot 小邮箱。
2. Latest / Dirty / Coalesced 专用存储。
3. PumpBatch / Throughput 模式。
4. Actor 状态 bitset。
5. IsSlotPostable 位图化。
6. 源生成器 column 注册。
```

---

## 31. 禁止事项

本优化不得做：

```text
1. 不删除 PendingDestroy / Destroying 检查。
2. 不删除 ActorId / Generation 安全。
3. 不删除 DisabledPolicy。
4. 不默认关闭帧预算。
5. 不把 ActorWorld 改成 Dictionary 直调。
6. 不引入 unsafe 作为第一阶段方案。
7. 不引入 AttributeSystem。
8. 不引入 UI / Network / Save。
9. 不为 benchmark 特化掉真实业务语义。
```

---

## 32. 最终 DoD

全部完成后，应满足：

```text
1. ActorWorld Post + Pump 预热后 0B GC Alloc。
2. ActorWorld Post + Pump 单次成本降到 35~40ns 区间，或相对当前明显下降。
3. ActorRef<TActor>.Post 明显快于 ActorWorld.Post(actorId, event)。
4. Query.PostAll 在批量 Actor 场景明显快于逐 Actor Post。
5. DirtyBucketList 场景下，事件类型很多但 dirty bucket 很少时性能明显提升。
6. Fair 配置保留旧公平调度语义。
7. Default 配置优先吞吐。
8. 所有现有测试通过。
9. 新增 correctness / allocation / benchmark 测试通过。
10. README 或 docs 明确 ActorWorld 性能定位。
```

---

## 33. 最终定位

优化完成后的 ActorWorld 应这样定位：

```text
ActorWorld 是带邮箱、生命周期、安全检查、帧预算、Query、Debug 的高性能 Actor Runtime。

它不是 Dictionary 直调替代品。
它的单体 Post + Pump 应接近 PostScheduler。
它的 Query.PostAll / 同 Archetype 批量路径应体现 ActorRuntime 的结构性优势。
它在稳定运行态应保持 0B GC Alloc。
```
