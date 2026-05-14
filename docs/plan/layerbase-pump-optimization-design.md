# LayerBase Pump 成本优化完整设计方案

文件名：`layerbase-pump-optimization-design.md`  
适用分支：`faster`  
适用范围：`LayerBase.Actor` 下的 `ActorWorld.Pump`、Actor 邮箱 Pump、生命周期 Pump、帧预算、统计与 Benchmark。

---

## 1. 背景

当前 `ActorWorld.Pump` 已经从早期的逐 Actor 扫描，演进为基于脏队列的调度模型。它通过 `DirtyBucketList` 记录有待处理消息的事件桶，再由 Bucket 调度到 Column，最后由 Column 调度到具体 Actor 的 Mail Slot。

当前设计方向是正确的：  
不是每帧扫描所有 Actor，而是只处理有工作量的 Bucket 和 Slot。

但是从当前热路径看，Pump 的性能成本仍然主要来自以下部分：

1. 外层 `ActorWorld.Pump` 的固定流程成本。
2. `RuntimeFrameBudget` 的事件预算与时间预算检查。
3. `DirtyBucketList`、Bucket、Column、DirtySlot 的多层调度成本。
4. `HasPendingWork()` 带来的二次扫描。
5. `ActorLifecycleFreeList` 在生命周期 Pump 中的逐项预算检查。
6. FreeList 空洞造成的无效遍历。
7. `ActorMailPumpStatsBuilder` 在热路径下的统计写入成本。
8. `_invoker(actor, in value)` 的行为调用成本。
9. 邮箱读取、环形缓冲、释放策略带来的内存访问成本。

本设计文档的目标是：  
在不破坏 LayerBase 当前 Actor 心智模型的前提下，降低每条事件在 Pump 阶段穿越的调度层数和附加检查次数。

---

## 2. 新名词说明

### 2.1 Pump

Pump 指“驱动运行时处理一批积压工作”的过程。

在 LayerBase 中，`ActorWorld.Pump` 主要处理三类工作：

1. 已投递到 Actor 邮箱中的事件。
2. Actor 生命周期方法，例如 `Update`、`LateUpdate`、`FixedUpdate`。
3. 延迟任务和销毁清理。

### 2.2 Mail

Mail 指 Actor 的邮箱数据。  
每个 Actor 可以收到某种事件类型的消息，这些消息会进入对应的 `EventMail<TEvent>`。

### 2.3 Bucket

Bucket 是事件桶。  
它通常按事件类型或 Call 路由进行分组。

例如：

```text
ActorBenchEvent -> ActorEventBucket<ActorBenchEvent>
DamageEvent     -> ActorEventBucket<DamageEvent>
MoveEvent       -> ActorEventBucket<MoveEvent>
```

Bucket 的作用是：  
把同一种事件类型的待处理消息聚合起来，避免 Pump 时扫描所有事件类型。

### 2.4 Column

Column 是 Bucket 内部的实际处理列。  
同一种事件类型可能被多个 Actor 类型监听，因此一个 Bucket 下面可能有多个 Column。

例如：

```text
DamageEvent Bucket
├── EventColumn<PlayerActor, DamageEvent>
├── EventColumn<EnemyActor, DamageEvent>
└── EventColumn<NpcActor, DamageEvent>
```

Column 的作用是：  
保存某个 Actor 类型对某个事件类型的邮箱数据。

### 2.5 DirtyBucketList

DirtyBucketList 是脏 Bucket 列表。

“脏”的意思是：  
该 Bucket 当前有待处理消息。

DirtyBucketList 的目标是：  
Pump 时只处理有消息的 Bucket，而不是扫描所有 Bucket。

### 2.6 DirtySlotList

DirtySlotList 是脏 Slot 列表。

Slot 可以理解为某个 Actor 在 `TypedActorStorage<TActor>` 内部的数组位置。  
DirtySlotList 记录哪些 Actor Slot 有待处理 Mail。

### 2.7 RuntimeFrameBudget

RuntimeFrameBudget 是运行时帧预算。

它目前包含：

```text
MaxEvents     本帧最多处理多少个工作单元。
UsedEvents    本帧已经处理多少个工作单元。
DeadlineTicks 本帧处理工作的时间截止点。
```

虽然字段名叫 Event，但当前在生命周期 Pump 中也被当成 Work Unit 使用。  
Work Unit 指“一次可计量的运行时工作”，例如处理一个事件、调用一次 Update。

### 2.8 Invoker

Invoker 是行为调用委托。  
它负责真正执行 Actor 的业务处理方法。

例如：

```text
_invoker(actor, in value)
```

含义是：  
把事件 `value` 交给 `actor` 的对应处理方法。

---

## 3. 当前 Pump 链路

当前 `ActorWorld.Pump` 的简化链路如下：

```text
ActorWorld.Pump
├── 检查 ActorWorld 状态
├── DelayScheduler.Tick
├── SweepPendingDestroy
├── PumpActorBehaviours
│   ├── PumpActorBehavioursManyFast
│   │   └── TryPumpMany
│   │       ├── TryPumpManyFromDirtyBuckets(_dirtyCallBuckets)
│   │       └── TryPumpManyFromDirtyBuckets(_dirtyEventBuckets)
│   └── PumpActorBehavioursOneByOne
│       └── TryPumpOne
│           ├── TryPumpOneFromDirtyBuckets(_dirtyCallBuckets)
│           └── TryPumpOneFromDirtyBuckets(_dirtyEventBuckets)
├── Lifecycle.PumpFixedUpdate
├── Lifecycle.PumpUpdate
├── Lifecycle.PumpLateUpdate
└── SweepPendingDestroy
```

Actor 邮箱事件的实际处理链路如下：

```text
DirtyBucketList
→ IActorEventBucket
→ ActorEventBucket<TEvent>
→ ActorEventColumnRuntime
→ EventColumn<TActor, TEvent>
→ DirtySlotList
→ EventMail<TEvent>
→ TActor
→ ActorBehaviourInvoker<TActor, TEvent>
```

---

## 4. 当前成本模型

### 4.1 总成本公式

```text
Pump 总成本 =
  Pump 入口固定成本
+ DelayScheduler 成本
+ Destroy Sweep 成本
+ Actor Mail Pump 成本
+ Lifecycle Pump 成本
+ 预算检查成本
+ 统计写入成本
+ 业务 Invoker 成本
```

其中 Actor Mail Pump 成本又可以拆成：

```text
Actor Mail Pump 成本 =
  DirtyBucketList 调度成本
+ Bucket 接口调用成本
+ Bucket 内 Column 轮转成本
+ DirtySlotList 调度成本
+ Mail 读取成本
+ Actor 可运行状态检查成本
+ Invoker 调用成本
+ Dirty 状态更新成本
+ Budget 消耗成本
+ Stats 记录成本
```

### 4.2 当前最重要的性能瓶颈

从当前代码结构判断，最值得优先优化的不是 Dictionary，而是：

1. `current.HasPendingWork()` 导致 Bucket 内 Column 二次扫描。
2. 生命周期 Pump 每个条目都调用 `Stopwatch.GetTimestamp()`。
3. Lifecycle FreeList 空洞导致无效遍历。
4. Pump 统计信息在热路径中无法关闭。
5. 单 Column Bucket 仍然走通用 Bucket 调度。
6. Call Bucket 为空时仍然经过 Call Bucket 尝试路径。
7. Fair 模式和 Throughput 模式的边界还不够清晰。

---

## 5. 优化目标

### 5.1 性能目标

1. 降低每条事件的平均 Pump 调度成本。
2. 减少 `Stopwatch.GetTimestamp()` 调用频率。
3. 减少 Bucket、Column、Slot 的重复扫描。
4. 在高吞吐模式下减少公平性统计、Actor 限流、Bucket 限流带来的额外成本。
5. 保持 Actor 邮箱模型的安全性和可调度性。

### 5.2 架构目标

1. 不破坏现有 `ActorWorld.Pump` 入口。
2. 不破坏 `ActorMailPumpOptions.Default` 的兼容性。
3. 新增 Throughput 模式，用于 Benchmark、高频 ECS-Actor 桥接、批量事件分发。
4. Fair 模式继续保留，用于需要公平调度的业务场景。
5. 保留 `RuntimeFrameBudget` 参与 Actor 行为和生命周期 Pump 的能力。

### 5.3 非目标

本次不处理以下内容：

1. 不重写 ActorWorld 整体存储结构。
2. 不取消 DirtyBucketList。
3. 不取消 RuntimeFrameBudget。
4. 不取消生命周期参与预算。
5. 不强制把所有委托 invoker 改成源生成静态调用。
6. 不引入多线程并行 Pump。

---

## 6. 优化方案总览

推荐分五个阶段推进：

```text
Phase 1：修复 PumpMany 的二次扫描成本
Phase 2：生命周期 Pump 时间检查分摊
Phase 3：Lifecycle FreeList 空洞治理
Phase 4：新增 Throughput / Fair 明确模式
Phase 5：Stats 可关闭与 Benchmark 验收
```

每个阶段都应该单独提交，方便回滚和 Benchmark 对比。

---

## 7. Phase 1：消除 PumpMany 后的 HasPendingWork 二次扫描

### 7.1 当前问题

当前 `TryPumpManyFromDirtyBuckets` 在 bucket 处理出结果后，会调用：

```text
current.HasPendingWork()
```

如果 `current` 是 `ActorEventBucket<TEvent>`，它会遍历 `_columns`，逐个检查 Column 是否还有待处理消息。

问题是：  
刚刚 `PumpMany` 内部其实已经知道当前 Column 是否还有工作，但这个信息没有向外返回。  
因此外层只能再扫描一次。

### 7.2 设计目标

让 `PumpMany` 返回：

1. 本次处理了多少事件。
2. 本次结果状态。
3. 当前 Bucket 是否仍然有待处理工作。
4. 当前 Bucket 是否应该继续留在 DirtyBucketList 中。

### 7.3 修改 ActorPumpManyResult

建议修改为：

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 表示一次批量 Pump 的结果。
/// </summary>
internal readonly struct ActorPumpManyResult
{
    /// <summary>
    /// 本次实际处理的事件数量。
    /// 例如处理了 8 条 Actor 邮箱消息，则 Processed = 8。
    /// </summary>
    public readonly int Processed;

    /// <summary>
    /// 本次 Pump 的状态。
    /// Processed 表示成功处理了至少一条消息。
    /// NoWork 表示没有可处理工作。
    /// EmptyBucket 表示 Bucket 已经没有有效工作。
    /// BucketLimited 表示命中了 Bucket 级限流。
    /// ActorLimited 表示命中了 Actor 级限流。
    /// </summary>
    public readonly PumpOneResult Result;

    /// <summary>
    /// 当前 Bucket 在本次 Pump 后是否仍然有待处理工作。
    /// true 表示外层 DirtyBucketList 应该保留该 Bucket。
    /// false 表示外层 DirtyBucketList 可以移除该 Bucket。
    /// </summary>
    public readonly bool HasMoreWork;

    /// <summary>
    /// 创建批量 Pump 结果。
    /// </summary>
    /// <param name="processed">本次处理的事件数量。</param>
    /// <param name="result">本次 Pump 的状态。</param>
    /// <param name="hasMoreWork">当前 Bucket 是否仍有待处理工作。</param>
    public ActorPumpManyResult(
        int           processed,
        PumpOneResult result,
        bool          hasMoreWork)
    {
        Processed = processed;
        Result = result;
        HasMoreWork = hasMoreWork;
    }

    /// <summary>
    /// 创建无工作结果。
    /// </summary>
    /// <returns>表示当前没有可处理工作的结果。</returns>
    public static ActorPumpManyResult NoWork()
    {
        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.NoWork,
            hasMoreWork: false);
    }

    /// <summary>
    /// 创建已处理批量事件的结果。
    /// </summary>
    /// <param name="processed">本次处理的事件数量。</param>
    /// <param name="hasMoreWork">处理后是否仍有待处理工作。</param>
    /// <returns>表示本次成功处理了一批事件的结果。</returns>
    public static ActorPumpManyResult ProcessedBatch(
        int  processed,
        bool hasMoreWork)
    {
        return new ActorPumpManyResult(
            processed: processed,
            result: PumpOneResult.Processed,
            hasMoreWork: hasMoreWork);
    }
}
```

### 7.4 修改 TryPumpManyFromDirtyBuckets

当前逻辑：

```text
result.Processed > 0
→ current.HasPendingWork()
→ MoveHeadToTail 或 Pop
```

建议改为：

```csharp
private static ActorPumpManyResult TryPumpManyFromDirtyBuckets(
    DirtyBucketList           dirtyBuckets,
    IActorEventBucket[]       buckets,
    ref int                   cursor,
    ref RuntimeFrameBudget    budget,
    in  ActorMailPumpOptions  options,
    ActorMailPumpStatsBuilder stats,
    int                       maxEvents)
{
    // dirtyBuckets 参数表示当前待处理的脏 Bucket 队列。
    // buckets 参数表示 Bucket 索引到实际 Bucket 实例的数组。
    // cursor 参数表示当前轮转到哪个 Bucket。
    // budget 参数表示当前帧剩余预算。
    // options 参数表示邮箱 Pump 配置。
    // stats 参数表示本次 Pump 的统计构建器。
    // maxEvents 参数表示本次最多处理多少事件。

    if (dirtyBuckets.Count == 0 || buckets.Length == 0)
    {
        return ActorPumpManyResult.NoWork();
    }

    int checkedCount = 0;
    bool sawBucketLimit = false;
    bool sawActorLimit = false;
    int initialCount = dirtyBuckets.Count;

    while (checkedCount < initialCount &&
           dirtyBuckets.TryPeek(out int bucketIndex))
    {
        cursor = bucketIndex;
        checkedCount++;

        IActorEventBucket? current = buckets[bucketIndex];
        if (current == null)
        {
            dirtyBuckets.Pop();
            continue;
        }

        ActorPumpManyResult result = current.PumpMany(
            budget: ref budget,
            options: in options,
            stats: stats,
            bucketIndex: bucketIndex,
            maxEvents: maxEvents);

        if (result.Processed > 0)
        {
            if (result.HasMoreWork)
            {
                dirtyBuckets.MoveHeadToTail();
            }
            else
            {
                dirtyBuckets.Pop();
            }

            return result;
        }

        if (result.Result == PumpOneResult.BucketLimited)
        {
            sawBucketLimit = true;
            dirtyBuckets.MoveHeadToTail();
            continue;
        }

        if (result.Result == PumpOneResult.ActorLimited)
        {
            sawActorLimit = true;
            dirtyBuckets.MoveHeadToTail();
            continue;
        }

        dirtyBuckets.Pop();
    }

    if (sawBucketLimit)
    {
        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.BucketLimited,
            hasMoreWork: true);
    }

    if (sawActorLimit)
    {
        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.ActorLimited,
            hasMoreWork: true);
    }

    return new ActorPumpManyResult(
        processed: 0,
        result: PumpOneResult.EmptyBucket,
        hasMoreWork: false);
}
```

### 7.5 修改 EventColumn.PumpMany

`EventColumn<TActor, TEvent>.PumpMany` 在处理后可以直接知道当前 `DirtySlotList` 是否仍有内容。

建议返回：

```csharp
return processed > 0
    ? ActorPumpManyResult.ProcessedBatch(
        processed: processed,
        hasMoreWork: _dirtySlots.Count > 0)
    : ActorPumpManyResult.NoWork();
```

### 7.6 修改 ActorEventBucket.PumpMany

Bucket 内部从 Column 得到 `HasMoreWork` 后，不应再调用 `HasPendingWork()`。

建议：

```csharp
if (result.Processed > 0)
{
    totalProcessed += result.Processed;
    stats.ProcessedTotal += result.Processed;

    return ActorPumpManyResult.ProcessedBatch(
        processed: totalProcessed,
        hasMoreWork: result.HasMoreWork || HasOtherColumnsPending(index));
}
```

这里有一个设计点：  
如果只看当前 Column 的 `HasMoreWork`，可能漏掉其他 Column 的待处理消息。

因此建议为 Bucket 增加轻量 pending 计数，而不是调用 `HasPendingWork()` 扫描。

### 7.7 推荐增加 Bucket 级 PendingColumnCount

在 `ActorEventBucket<TEvent>` 中增加：

```text
_pendingColumnCount
```

当某个 Column 从无工作变成有工作时递增。  
当某个 Column 被 Pump 清空时递减。

但当前 DirtySlotList 的 Mark 发生在 Column 内部，Bucket 未必能直接知道 Column 从 0 到 1 的变化。  
如果短期不想大改写入链路，可以先保守实现：

1. `EventColumn.PumpMany` 返回 `HasMoreWork`。
2. `ActorEventBucket.PumpMany` 对当前 Column 使用 `result.HasMoreWork`。
3. 对其他 Column 不做额外扫描。
4. 如果担心漏处理，暂时保留 `HasPendingWork()`，但只在 `_count > 1` 且当前 Column 无工作时调用。

短期折中：

```csharp
bool hasMoreWork =
    result.HasMoreWork ||
    (_count > 1 && HasPendingWorkExcept(index));
```

长期最佳方案：  
让 Column 的 dirty 状态变化同步维护 Bucket 级 pending column 计数。

---

## 8. Phase 2：生命周期 Pump 时间检查分摊

### 8.1 当前问题

当前 `ActorLifecycleFreeList<TLifecycle>.PumpBudgeted` 中，每个条目都会调用：

```text
budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp())
```

这意味着如果有 5000 个生命周期 Actor，每帧可能产生 5000 次时间戳读取。

时间戳读取虽然不是特别慢，但在热路径中属于纯调度开销，不是业务逻辑本身。

### 8.2 设计目标

生命周期 Pump 应该和 Actor 邮箱 Pump 一样支持 `TimeCheckInterval`。

也就是说：

```text
每处理 N 个生命周期条目，再检查一次时间预算。
```

事件预算仍然可以每次检查，因为它只是整数比较。

### 8.3 新增配置

建议新增：

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 生命周期 Pump 配置。
/// </summary>
public readonly struct ActorLifecyclePumpOptions
{
    /// <summary>
    /// 时间预算检查间隔。
    /// 例如 64 表示每遍历 64 个生命周期条目检查一次 Stopwatch。
    /// </summary>
    public readonly int TimeCheckInterval;

    /// <summary>
    /// 每次生命周期 Pump 最多处理多少个生命周期调用。
    /// 小于等于 0 表示不单独限制，只受 RuntimeFrameBudget 控制。
    /// </summary>
    public readonly int MaxLifecycleCallsPerPump;

    /// <summary>
    /// 创建生命周期 Pump 配置。
    /// </summary>
    /// <param name="timeCheckInterval">时间预算检查间隔，最小为 1。</param>
    /// <param name="maxLifecycleCallsPerPump">单次最多生命周期调用数，小于等于 0 表示无限制。</param>
    public ActorLifecyclePumpOptions(
        int timeCheckInterval,
        int maxLifecycleCallsPerPump)
    {
        TimeCheckInterval = Math.Max(timeCheckInterval, 1);
        MaxLifecycleCallsPerPump = maxLifecycleCallsPerPump;
    }

    /// <summary>
    /// 默认生命周期 Pump 配置。
    /// </summary>
    public static ActorLifecyclePumpOptions Default => new(
        timeCheckInterval: 64,
        maxLifecycleCallsPerPump: 0);
}
```

### 8.4 修改 PumpBudgeted

建议改造为：

```csharp
public void PumpBudgeted(
    ref LifecycleFrameState        state,
    ref RuntimeFrameBudget         budget,
    in  ActorLifecyclePumpOptions  options,
    LifecycleCall<TLifecycle>      invoker)
{
    // state 参数表示生命周期遍历上下文。
    // budget 参数表示当前帧剩余预算。
    // options 参数表示生命周期 Pump 配置。
    // invoker 参数表示具体调用哪个生命周期方法。

    if (_count == 0)
    {
        return;
    }

    int checkedCount = 0;
    int maxCount = _count;
    int processed = 0;
    int processedSinceTimeCheck = 0;

    while (checkedCount < maxCount)
    {
        if (!budget.HasRemainingEventBudget())
        {
            return;
        }

        if (options.MaxLifecycleCallsPerPump > 0 &&
            processed >= options.MaxLifecycleCallsPerPump)
        {
            return;
        }

        if (processedSinceTimeCheck <= 0)
        {
            if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
            {
                return;
            }

            processedSinceTimeCheck = options.TimeCheckInterval;
        }

        int index = _cursor;

        _cursor = index + 1 == _count
            ? 0
            : index + 1;

        checkedCount++;

        if (!_occupied[index])
        {
            continue;
        }

        ActorLifecycleEntry<TLifecycle> entry = _entries[index];

        if (!state.World.IsLifecycleRunnable(entry.ActorId))
        {
            continue;
        }

        invoker(
            instance: entry.Instance,
            deltaTime: state.DeltaTime);

        budget.ConsumeEvent();
        processed++;
        processedSinceTimeCheck--;
    }
}
```

### 8.5 注意点

这里的 `processedSinceTimeCheck` 建议只在真正调用生命周期后递减，而不是每检查一个空洞递减。

原因：  
时间预算主要限制实际工作量，而不是限制 FreeList 空洞检查。  
不过如果空洞极多，空洞本身也会耗时，所以 Phase 3 必须处理空洞问题。

---

## 9. Phase 3：Lifecycle FreeList 空洞治理

### 9.1 当前问题

`ActorLifecycleFreeList<TLifecycle>` 删除元素后：

```text
_entries[index] = default
_occupied[index] = false
_free[_freeCount] = index
```

但是 `_count` 不会下降。

这意味着如果曾经创建过 10000 个生命周期 Actor，后来销毁了 9000 个，Pump 仍然可能在 `_count = 10000` 的范围内轮转，只是大部分位置 `_occupied[index] == false`。

### 9.2 设计目标

减少空洞导致的无效遍历。

### 9.3 推荐方案：阈值 Compact

新增字段：

```csharp
private int _occupiedCount;
private int _removeSinceCompact;
```

含义：

```text
_occupiedCount       当前真实存活的生命周期条目数量。
_removeSinceCompact  上次压缩后发生了多少次删除。
```

### 9.4 修改 Add

```csharp
public ActorLifecycleHandle Add(
    ActorId    actorId,
    TLifecycle instance)
{
    // actorId 参数表示该生命周期条目对应的 Actor。
    // instance 参数表示实现了生命周期接口的 Actor 实例。

    int index;

    if (_freeCount > 0)
    {
        _freeCount--;
        index = _free[_freeCount];
    }
    else
    {
        index = _count;
        _count++;
        EnsureCapacity(index + 1);
    }

    _entries[index] = new ActorLifecycleEntry<TLifecycle>(actorId, instance);
    _occupied[index] = true;
    _occupiedCount++;

    return new ActorLifecycleHandle(index, _versions[index]);
}
```

### 9.5 修改 Remove

```csharp
public bool Remove(ActorLifecycleHandle handle)
{
    // handle 参数表示 Add 时返回的生命周期条目位置。
    // Version 不匹配说明该位置已经被释放并复用，不能删除。

    if (!handle.IsValid)
    {
        return false;
    }

    if ((uint)handle.Index >= (uint)_entries.Length)
    {
        return false;
    }

    if (!_occupied[handle.Index])
    {
        return false;
    }

    if (_versions[handle.Index] != handle.Version)
    {
        return false;
    }

    _entries[handle.Index] = default;
    _occupied[handle.Index] = false;

    unchecked
    {
        _versions[handle.Index]++;
    }

    if (_freeCount == _free.Length)
    {
        Array.Resize(ref _free, _free.Length * 2);
    }

    _free[_freeCount] = handle.Index;
    _freeCount++;

    _occupiedCount--;
    _removeSinceCompact++;

    return true;
}
```

### 9.6 增加 CompactIfNeeded

```csharp
private void CompactIfNeeded()
{
    // 当空洞比例不高时，不做压缩。
    // 这样可以避免频繁 Compact 造成额外开销。
    if (_count < 64)
    {
        return;
    }

    int holes = _count - _occupiedCount;
    if (holes <= 0)
    {
        return;
    }

    // 空洞比例低于 30% 时不压缩。
    if (holes * 100 < _count * 30)
    {
        return;
    }

    Compact();
}
```

### 9.7 Compact 的风险

`ActorLifecycleHandle` 中保存了 index 和 version。  
如果直接移动条目，会导致外部旧 handle 失效。

因此 Compact 有两种路线：

#### 路线 A：不移动，只裁剪尾部空洞

这是低风险方案。

```csharp
private void TrimTrailingHoles()
{
    while (_count > 0)
    {
        int last = _count - 1;
        if (_occupied[last])
        {
            break;
        }

        _entries[last] = default;
        _count--;
    }
}
```

优点：  
不会改变存活条目的 index，handle 安全。

缺点：  
只能清理尾部空洞，不能清理中间空洞。

#### 路线 B：引入 Handle Remap

这是高收益方案，但改动较大。  
需要让 Actor 保存的生命周期 handle 同步更新，否则 Remove 会找不到位置。

当前建议先做路线 A。  
如果后续生命周期空洞仍然是瓶颈，再进入路线 B。

### 9.8 推荐短期实现

短期只做：

```text
Remove 后尝试 TrimTrailingHoles
Pump 前如果空洞过多，记录诊断指标
暂不移动中间存活条目
```

这样风险最低。

---

## 10. Phase 4：新增 Throughput 与 Fair 模式

### 10.1 当前问题

当前 `ActorMailPumpOptions.Default` 同时承担了默认业务模式和性能模式的职责。  
这会让 Benchmark、游戏热路径、高公平性业务混在一起。

建议明确分成：

1. `Throughput`：吞吐优先。
2. `Fair`：公平优先。
3. `Default`：兼容旧行为，可以先等于 `Throughput` 或保持当前值。

### 10.2 设计 Throughput 模式

```csharp
public static ActorMailPumpOptions Throughput => new(
    maxTotalMailsPerPump: 0,
    maxMailsPerBucketPerPump: 0,
    maxMailsPerActorPerPump: 0,
    maxEmptyBucketChecksPerPump: 64,
    timeCheckInterval: 128,
    maxEventCountPerPump: 128);
```

参数说明：

```text
maxTotalMailsPerPump = 0
表示单次 Pump 不额外限制总消息数量，只受 RuntimeFrameBudget 控制。

maxMailsPerBucketPerPump = 0
表示不对单个 Bucket 做额外限流。

maxMailsPerActorPerPump = 0
表示不对单个 Actor 做额外限流。

maxEmptyBucketChecksPerPump = 64
表示最多连续检查 64 个空 Bucket，防止异常 dirty 状态造成过长空转。

timeCheckInterval = 128
表示每处理约 128 个事件检查一次时间预算。

maxEventCountPerPump = 128
表示 Column 批量快路径中单次最多连续处理 128 个事件。
```

### 10.3 设计 Fair 模式

```csharp
public static ActorMailPumpOptions Fair => new(
    maxTotalMailsPerPump: 1024,
    maxMailsPerBucketPerPump: 128,
    maxMailsPerActorPerPump: 8,
    maxEmptyBucketChecksPerPump: 64,
    timeCheckInterval: 16,
    maxEventCountPerPump: 1);
```

Fair 模式适合：

1. 很多 Actor 同时有消息。
2. 不希望某个 Actor 或某个 Bucket 独占一帧。
3. 消息处理耗时差异较大。
4. 更重视响应均衡，而不是极限吞吐。

### 10.4 Default 的建议

建议短期保持：

```csharp
public static ActorMailPumpOptions Default => Throughput;
```

如果担心行为变化，可以先保持旧值，并新增：

```csharp
public static ActorMailPumpOptions CompatibilityDefault => new(...旧值...);
```

---

## 11. Phase 5：Stats 可关闭

### 11.1 当前问题

`ActorMailPumpStatsBuilder` 在 Pump 中承担统计记录。  
统计对调试有价值，但热路径中会带来额外写入和分支。

### 11.2 设计目标

支持三种统计模式：

```text
None   不统计。
Basic  只统计 ProcessedTotal 和 RemainingDirtyBuckets。
Full   统计 EmptyBucketChecks、BucketLimitHits、ActorLimitHits 等完整信息。
```

### 11.3 新增枚举

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 邮箱 Pump 统计模式。
/// </summary>
public enum ActorMailPumpStatsMode
{
    /// <summary>
    /// 不记录统计信息。
    /// 用于极限热路径和 Benchmark。
    /// </summary>
    None,

    /// <summary>
    /// 只记录基础统计。
    /// 例如本次处理了多少事件、剩余多少 Dirty Bucket。
    /// </summary>
    Basic,

    /// <summary>
    /// 记录完整统计。
    /// 用于调试、诊断、公平性调度分析。
    /// </summary>
    Full
}
```

### 11.4 集成到 ActorMailPumpOptions

```csharp
public readonly ActorMailPumpStatsMode StatsMode;
```

如果不希望破坏构造函数，可以先新增重载构造函数。

### 11.5 热路径处理

在 `PumpActorBehavioursManyFast` 中：

```csharp
ActorMailPumpStatsBuilder stats = _mailPumpStatsBuilder;
stats.Reset(options.StatsMode);
```

在 stats 内部：

```csharp
public void RecordBucketProcessed(int bucketIndex)
{
    // StatsMode.None 时直接返回。
    // StatsMode.Basic 时不记录 Bucket 级细节。
    // StatsMode.Full 时记录完整信息。
}
```

注意：  
如果某些限流逻辑依赖 stats，例如 `CanProcessBucket`、`CanProcessActor`，那么 StatsMode.None 不能关闭限流所需数据。  
因此应区分：

```text
调度状态数据
统计观测数据
```

调度状态数据必须保留。  
统计观测数据可以关闭。

---

## 12. 单 Column Bucket 快路径

### 12.1 当前问题

`ActorEventBucket<TEvent>` 即使只有一个 Column，也会进入 `_columns` 数组、`_cursor` 轮转、`checkedCount` 循环。

单 Column 是常见场景。  
例如某个事件只被一种 Actor 类型监听。

### 12.2 设计方案

在 `ActorEventBucket<TEvent>` 中增加：

```csharp
private ActorEventColumnRuntime? _singleColumn;
```

当 `_count == 1` 时：

```csharp
_singleColumn = _columns[0];
```

`PumpMany` 中：

```csharp
if (_count == 1)
{
    ActorPumpManyResult result = _singleColumn!.PumpMany(
        budget: ref budget,
        options: in options,
        stats: stats,
        maxEvents: maxEvents);

    if (result.Processed > 0)
    {
        stats.ProcessedTotal += result.Processed;
    }

    return result;
}
```

### 12.3 注意点

`AddColumn` 后如果 `_count > 1`，必须清除或忽略 `_singleColumn`。

---

## 13. 跳过空 Call Bucket

### 13.1 当前问题

`TryPumpMany` 会先尝试 `_dirtyCallBuckets`，再尝试 `_dirtyEventBuckets`。

如果项目没有使用 Actor Call，Call 路径会成为固定分支成本。

### 13.2 设计方案

在 `ActorWorld` 内新增：

```csharp
private bool _hasCallBuckets;
```

当注册 Call Bucket 时设置：

```csharp
_hasCallBuckets = true;
```

`TryPumpMany` 中：

```csharp
if (_hasCallBuckets && _dirtyCallBuckets.Count > 0)
{
    ActorPumpManyResult callResult = TryPumpManyFromDirtyBuckets(...);

    if (callResult.Processed > 0 ||
        callResult.Result == PumpOneResult.BucketLimited ||
        callResult.Result == PumpOneResult.ActorLimited)
    {
        return callResult;
    }
}
```

同理 `TryPumpOne` 也可以跳过空 Call Bucket。

---

## 14. EventColumn 批量处理策略

### 14.1 当前问题

`EventColumn<TActor,TEvent>.PumpMany` 已经可以在一个 Column 内连续处理多个事件。  
但注释中仍然提到“为了保持跨 Column 公平性，每次调用只处理一个事件”，实际代码中又使用 `MaxEventCountPerPump` 批量处理。

这里需要统一语义。

### 14.2 推荐语义

```text
Throughput 模式：
    一个 Column 可以连续处理多个事件。
    MaxEventCountPerPump 控制单次最多处理数量。

Fair 模式：
    一个 Column 每次只处理一个事件。
    通过 MaxEventCountPerPump = 1 达成。
```

### 14.3 文档与注释更新

需要把 `EventColumn.PumpMany` 注释改为：

```text
该方法在 Throughput 模式下允许同一 Column 连续处理多个事件。
该方法在 Fair 模式下通过 MaxEventCountPerPump = 1 保持跨 Actor / Column 的公平性。
```

---

## 15. Destroy 与 Dirty Slot 清理

### 15.1 当前问题

Actor Destroy 后，Column 中可能仍然有 dirty slot。  
Pump 时会通过 `_owner.CanPumpSlot(slotIndex)` 跳过不可运行 Actor。

这是安全的，但会让 Pump 承担清垃圾成本。

### 15.2 设计方案

在 Actor Destroy 或 Sweep 阶段，尽量主动清理：

1. Actor 对应 slot 的 mail。
2. Actor 对应 slot 的 dirty 标记。
3. 生命周期 handle。
4. 相关 query cache 的活跃状态。

### 15.3 风险

DirtySlotList 如果没有 O(1) 删除任意 slot 的能力，主动清理会复杂。  
短期可以保留 Pump 跳过逻辑。  
中长期建议 DirtySlotList 也采用 mark/stamp 机制，支持延迟失效。

---

## 16. Benchmark 设计

### 16.1 必须保留的 Benchmark

当前已有：

1. `DirectMethodCall`
2. `LayerBaseSend`
3. `LayerBasePostScheduler`
4. `ActorWorldPostAndPump`
5. `ActorWorldPostOnly`
6. `ActorWorldPumpOnlyPreposted`
7. `ActorWorldQueryPostAll`
8. `DictionaryActorDispatch`

这些 Benchmark 应继续保留。

### 16.2 新增 Benchmark：单 Column PumpMany

```text
目标：
测试一个事件类型、一个 Actor 类型、一个 Actor 的纯 Pump 吞吐。
```

场景：

```text
1 个 Actor
1 个 Event 类型
预投递 1,000,000 条事件
一次 Pump 消费完
```

验收：

```text
Pump only 不应出现 GC 分配。
优化后 Mean 应低于优化前。
```

### 16.3 新增 Benchmark：多 Column Bucket

```text
目标：
测试一个 Event Bucket 下多个 Column 的调度成本。
```

场景：

```text
1 个 Event 类型
8 个 Actor 类型
每个 Actor 类型 128 个 Actor
每个 Actor 各 1 条事件
```

验收：

```text
优化 HasPendingWork 后，多 Column 场景应明显减少调度开销。
```

### 16.4 新增 Benchmark：Lifecycle Pump

```text
目标：
测试生命周期调度成本。
```

场景：

```text
10000 个 IUpdate Actor
每帧 PumpUpdate
对比 TimeCheckInterval = 1 / 16 / 64 / 128
```

验收：

```text
TimeCheckInterval = 64 时性能明显优于每项检查。
功能上不应突破 RuntimeFrameBudget 的 MaxEvents 限制。
```

### 16.5 新增 Benchmark：Lifecycle 空洞

```text
目标：
测试 FreeList 空洞对 Pump 的影响。
```

场景：

```text
创建 10000 个 IUpdate Actor
删除 9000 个
剩余 1000 个
执行 PumpUpdate
```

验收：

```text
TrimTrailingHoles 后，如果删除集中在尾部，应显著减少遍历。
中间空洞场景至少应输出诊断数据。
```

---

## 17. 单元测试设计

### 17.1 PumpMany 不漏消息

测试目标：

```text
PumpMany 返回 HasMoreWork 后，DirtyBucketList 不应错误 Pop 掉仍有消息的 Bucket。
```

测试步骤：

```text
1. 创建 ActorWorld。
2. 创建一个 Actor。
3. 连续 Post 多条事件。
4. 设置 maxEvents 小于事件总数。
5. Pump 一次。
6. 确认剩余事件在下一次 Pump 中仍能被处理。
```

### 17.2 PumpMany 清空后移除 DirtyBucket

测试目标：

```text
当 Bucket 已经没有待处理消息后，DirtyBucketList 应移除该 Bucket。
```

测试步骤：

```text
1. 创建 ActorWorld。
2. Post 一条事件。
3. Pump 到清空。
4. 再 Pump 一次。
5. 确认第二次 Pump 不重复处理消息。
```

### 17.3 Lifecycle TimeCheckInterval 不破坏 MaxEvents

测试目标：

```text
即使时间检查被分摊，MaxEvents 仍然必须严格生效。
```

测试步骤：

```text
1. 创建 100 个 IUpdate Actor。
2. RuntimeFrameBudget.MaxEvents = 10。
3. PumpUpdate。
4. 确认只调用 10 次 Update。
```

### 17.4 Lifecycle TimeCheckInterval 不破坏时间预算

测试目标：

```text
时间预算不是每条检查后，仍然不能长时间无界执行。
```

测试步骤：

```text
1. 设置很小的 DeadlineTicks。
2. 设置 TimeCheckInterval = 16。
3. PumpUpdate。
4. 确认生命周期 Pump 会在检查点停止。
```

说明：  
该测试可能受机器性能影响，建议只做行为范围验证，不做纳秒级断言。

### 17.5 FreeList Remove 后 handle 安全

测试目标：

```text
Remove 后版本号变化，旧 handle 不能删除新条目。
```

测试步骤：

```text
1. Add 一个生命周期条目，保存 handleA。
2. Remove handleA。
3. Add 新生命周期条目，可能复用同一 index。
4. 再次 Remove handleA。
5. 确认删除失败。
```

---

## 18. 风险分析

### 18.1 HasMoreWork 风险

如果 `HasMoreWork` 计算错误，可能出现：

1. Bucket 被过早 Pop，导致消息滞留。
2. Bucket 被长期保留，导致空转。
3. 多 Column 场景下部分 Column 消息延迟处理。

规避方案：

```text
短期保守实现：
    仅在单 Column Bucket 使用 HasMoreWork 快路径。
    多 Column Bucket 保留 HasPendingWork 扫描。

中期优化：
    为 Bucket 增加 pending column 计数。
```

### 18.2 生命周期时间检查分摊风险

如果 `TimeCheckInterval` 太大，单帧可能略微超过时间预算。

规避方案：

```text
默认 64。
Fair 模式可用 16。
用户可以自行配置为 1，恢复严格检查。
```

### 18.3 FreeList Compact 风险

如果移动存活条目，会破坏 handle。  
所以短期只允许裁剪尾部空洞，不移动中间存活条目。

### 18.4 StatsMode 风险

如果把调度所需状态误认为统计状态关闭，可能破坏限流。

规避方案：

```text
调度状态必须保留。
观测统计可以关闭。
```

---

## 19. 预期收益

### 19.1 对 ActorWorld Pump Only

预期收益来源：

1. 减少 `HasPendingWork()` 二次扫描。
2. 单 Column Bucket 快路径减少循环与 cursor 更新。
3. Throughput 模式减少公平性统计。
4. StatsMode.None / Basic 减少统计写入。

### 19.2 对 Query.PostAll + Pump

预期收益来源：

1. Query.PostAll 往往会造成大量 Actor 同时 dirty。
2. PumpMany 批量处理可减少外层调度返回次数。
3. 如果事件类型对应单 Bucket / 少量 Column，优化收益更明显。

### 19.3 对 Lifecycle Pump

预期收益来源：

1. 时间预算检查分摊。
2. FreeList 尾部空洞裁剪。
3. 后续如果引入 active index list，中间空洞也能优化。

---

## 20. 推荐提交顺序

### Commit 1：ActorPumpManyResult 增加 HasMoreWork

修改文件：

```text
LayerBase/Actor/Mail/ActorPumpManyResult.cs
LayerBase/Actor/Storage/ActorWorld.Pump.cs
LayerBase/Actor/Mail/ActorEventBucket.cs
LayerBase/Actor/Mail/EventColumn.cs
```

验收：

```text
所有 ActorWorld Pump 测试通过。
PumpMany 不漏消息。
Benchmark 不出现新增 GC。
```

### Commit 2：Lifecycle TimeCheckInterval

修改文件：

```text
LayerBase/Actor/Lifecycle/ActorLifecycleFreeList.cs
LayerBase/Actor/Lifecycle/ActorLifecycleScheduler.cs
新增 ActorLifecyclePumpOptions.cs
```

验收：

```text
MaxEvents 严格生效。
生命周期 Benchmark 性能提升。
```

### Commit 3：Lifecycle FreeList 尾部空洞裁剪

修改文件：

```text
LayerBase/Actor/Lifecycle/ActorLifecycleFreeList.cs
```

验收：

```text
旧 handle 不会错误删除新条目。
删除尾部生命周期项后 _count 可以下降。
```

### Commit 4：新增 Throughput / Fair 配置

修改文件：

```text
LayerBase/Actor/Pump/ActorMailPumpOptions.cs
```

验收：

```text
Default 行为符合预期。
Benchmark 明确使用 Throughput。
公平性测试使用 Fair。
```

### Commit 5：StatsMode

修改文件：

```text
LayerBase/Actor/Pump/ActorMailPumpStatsMode.cs
LayerBase/Actor/Pump/ActorMailPumpStatsBuilder.cs
LayerBase/Actor/Pump/ActorMailPumpOptions.cs
LayerBase/Actor/Storage/ActorWorld.Pump.cs
```

验收：

```text
StatsMode.None 不影响实际消息处理。
StatsMode.Full 仍能输出完整诊断信息。
```

---

## 21. 最终建议

短期最值得做的是：

```text
1. 单 Column Bucket 快路径。
2. PumpMany 返回 HasMoreWork。
3. 生命周期时间检查分摊。
4. Throughput 模式明确化。
```

中期再做：

```text
1. Bucket 级 pending column 计数。
2. StatsMode 分层。
3. DirtySlotList 延迟失效优化。
4. 生命周期 active index list。
```

长期可以考虑：

```text
1. 源生成器生成更直接的 ActorBehaviour 调用路径。
2. 针对高频事件生成专用 EventColumn。
3. ECS Query.PostAll 与 ActorWorld.PostTo 的批量直通路径。
4. ActorWorld Pump 的 Profile Marker 和内建诊断面板。
```

当前 LayerBase 的 Pump 架构方向是成立的。  
后续优化重点不应该再放在“是否比 Dictionary 查询快”这个单点上，而应该放在：

```text
每条事件从 Mail 到 Invoker 之前，经过了多少层调度？
每条事件附带了多少公平性、统计、预算、清理检查？
这些检查是否能在 Throughput 模式下降到最低？
```

只要围绕这三个问题继续压缩，ActorWorld 就能更接近实体框架和游戏运行时热路径的使用要求。
