# LayerBase PumpManyFast 与 ProjectedActorRef 优化设计文档

文件名：`pump-projection-optimization-design.md`

## 1. 背景

当前 LayerBase 的 ECS ↔ Actor 协作模型已经基本完成热路径 0 GC。最新 benchmark 显示：

| 项目 | Mean | Allocated | 结论 |
|---|---:|---:|---|
| Actor: PostTo Only × 1000 | 77.083 μs | 0 B | 写邮箱成本可接受 |
| Actor: Pump Only × 1000 | 212.433 μs | 0 B | 当前主要 CPU 瓶颈 |
| Actor: PostTo + Pump × 1000 | 235.000 μs | 0 B | 总成本主要被 Pump 拉高 |
| Projection: Entity → ActorId Lookup × 1000 | 84.533 μs | 0 B | Projection 查找成本明显 |
| Debug: Cached Projected ActorId Valid Count × 1000 | 18.700 μs | 0 B | 直接缓存 ActorId 明显更轻 |
| Full Pipeline: ECS Query → Projection Lookup → Actor PostTo → Pump × 1000 | 294.333 μs | 64 B | 完整链路主要瓶颈来自 Projection Lookup + Pump |
| Actor: Unsupported Event Post Only × 100 | 3.800 μs | 0 B | PostResult 字符串 GC 已移除成功 |

当前不再优先处理 GC，而是进入 CPU 热路径优化阶段。

---

## 2. 新名词解释

### 2.1 Pump

`Pump` 指 ActorWorld 从 Actor 邮箱中取出事件，并分发给对应 `ActorBehaviour` 的过程。

例如：

```text
ActorWorld.Pump
→ PumpActorBehaviours
→ TryPumpOne
→ EventColumn.PumpOneFast
→ _invoker(actor, in value)
```

### 2.2 Dirty Slot

`Dirty Slot` 指有待处理事件的 Actor 槽位。

Actor 邮箱收到事件后，会把对应 slot 标记为 dirty。Pump 时只遍历 dirty slot，而不是遍历所有 Actor。

### 2.3 Dirty Bucket

`Dirty Bucket` 指有待处理事件的事件桶。

一个事件类型可以对应一个 bucket，例如 `MoveEvent` 的 bucket。  
当某个事件类型存在待处理事件时，对应 bucket 会被标记为 dirty。

### 2.4 EventColumn

`EventColumn<TActor, TEvent>` 是某个 Actor 类型对某个事件类型的邮箱列。

例如：

```text
MinimalActor + MoveEvent
```

会对应一个 `EventColumn<MinimalActor, MoveEvent>`。

### 2.5 ProjectedActorMeta

`ProjectedActorMeta` 是框架内部的投影元数据，用于记录：

```text
Entity 是否可投影
Actor 类型 ID
ActorId
KeepAlive 时间
释放策略
激活状态
```

它是 internal，不应该直接暴露给业务层。

### 2.6 ProjectedActorRef

`ProjectedActorRef` 是本设计新增的公开组件。

它只缓存一个 `ActorId`，用于让业务 ECS Query 直接拿到 ActorId，避免每帧通过 Entity 反查 `ProjectedActorMeta`。

---

## 3. 优化目标

### 3.1 性能目标

第一阶段目标：

```text
Actor: Pump Only × 1000
从 212 μs 降到 120~160 μs 区间
```

第二阶段目标：

```text
Projection: Entity → ActorId Lookup × 1000
从 84 μs 降低到更接近 Cached ActorId 查询成本
```

完整链路目标：

```text
Full Pipeline × 1000
从 294 μs 继续下降
```

### 3.2 GC 目标

新增优化不能引入新的 GC 分配。

关键 benchmark 目标：

```text
Actor: Pump Only × 1000                       Allocated = 0 B
Actor: PostTo + Pump × 1000                   Allocated = 0 B
Hybrid Isolate: Cached ActorId PostTo + Pump  Allocated = 0 B
ProjectedActorRef 版 Full Pipeline            Allocated = 0 B 或仅保留当前 64 B lambda 捕获
```

### 3.3 兼容目标

必须保留现有 `PumpOne` 逻辑。

`PumpManyFast` 只作为快路径启用，不改变复杂配置下的行为。

---

## 4. 非目标

本设计不在第一阶段处理以下内容：

```text
1. 不重构整个 ActorWorld.Pump 调度模型。
2. 不删除现有 PumpOne。
3. 不改变 ActorBehaviour 语义。
4. 不优化 Cold Create 的 265 KB 分配。
5. 不优先处理 Hybrid Query 的 64 B lambda 捕获。
6. 不在第一阶段修改 ActorCall 邮箱批量 Pump。
```

---

# 第一部分：PumpManyFast 设计

## 5. 当前真实代码链路

当前 Pump 链路大致如下：

```text
ActorWorld.Pump
→ PumpActorBehaviours
→ TryPumpOne
→ TryPumpOneFromDirtyBuckets
→ IActorEventBucket.PumpOne
→ ActorEventBucket<TEvent>.PumpOne
→ ActorEventColumnRuntime.PumpOne
→ EventColumn<TActor, TEvent>.PumpOneFast
```

当前问题是：

```text
每处理 1 个事件，就完整返回 ActorWorld 外层调度器一次。
```

这会导致：

```text
TryPumpOne / DirtyBucket / DirtyColumn / Stats / Budget 检查
```

在每个事件上重复执行。

---

## 6. PumpManyFast 总体设计

新增批量 Pump 快路径：

```text
ActorWorld.Pump
→ PumpActorBehavioursManyFast
→ TryPumpMany
→ TryPumpManyFromDirtyBuckets
→ IActorEventBucket.PumpMany
→ ActorEventBucket<TEvent>.PumpMany
→ ActorEventColumnRuntime.PumpMany
→ EventColumn<TActor, TEvent>.PumpMany
```

核心变化：

```text
旧逻辑：每次 PumpOne 只处理 1 个事件。
新逻辑：同一个 Column 在预算允许时连续处理多个事件。
```

---

## 7. 启用条件

`PumpManyFast` 只在简单、高性能配置下启用：

```text
MaxMailsPerActorPerPump <= 0
MaxMailsPerBucketPerPump <= 0
ReleaseWhenEmpty == false
不启用复杂 Actor 限流
不启用复杂 Bucket 限流
```

如果任意条件不满足，回退旧 `PumpOne`。

---

## 8. 新增类型：ActorPumpManyResult

建议新增文件：

```text
LayerBase/Actor/Mail/ActorPumpManyResult.cs
```

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 批量 Pump 的结果。
/// 用于表达一次批量 Pump 实际处理了多少事件，以及为什么结束。
/// </summary>
internal readonly struct ActorPumpManyResult
{
    /// <summary>
    /// 本次实际处理的事件数量。
    /// </summary>
    public readonly int Processed;

    /// <summary>
    /// 批量 Pump 的结束原因。
    /// 复用现有 PumpOneResult，避免新增复杂状态枚举。
    /// </summary>
    public readonly PumpOneResult Result;

    /// <summary>
    /// 是否至少处理了一个事件。
    /// </summary>
    public bool HasProcessed => Processed > 0;

    /// <summary>
    /// 构造批量 Pump 结果。
    ///
    /// 参数说明：
    /// processed：本次实际处理的事件数量。
    /// result：结束原因。
    /// </summary>
    public ActorPumpManyResult(
        int processed,
        PumpOneResult result)
    {
        Processed = processed;
        Result = result;
    }

    /// <summary>
    /// 表示没有可处理工作。
    /// </summary>
    public static ActorPumpManyResult NoWork()
    {
        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.NoWork);
    }

    /// <summary>
    /// 表示成功处理了一批事件。
    ///
    /// 参数说明：
    /// processed：本批次处理的事件数量。
    /// </summary>
    public static ActorPumpManyResult ProcessedBatch(int processed)
    {
        return new ActorPumpManyResult(
            processed: processed,
            result: PumpOneResult.Processed);
    }
}
```

---

## 9. 修改 ActorEventColumnRuntime

目标文件：

```text
LayerBase/Actor/Mail/ActorEventColumnRuntime.cs
```

新增 `PumpMany` 虚方法，默认回退到 `PumpOne`。

```csharp
namespace LayerBase.Actor;

internal abstract class ActorEventColumnRuntime
{
    private DirtyBucketList? _dirtyBuckets;
    private int _bucketIndex;

    internal void BindDirtyBucket(
        DirtyBucketList dirtyBuckets,
        int bucketIndex)
    {
        _dirtyBuckets = dirtyBuckets;
        _bucketIndex = bucketIndex;
    }

    protected void NotifyBucketDirty()
    {
        _dirtyBuckets?.Mark(_bucketIndex);
    }

    public abstract ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats);

    /// <summary>
    /// 批量 Pump 当前 Column。
    ///
    /// 参数说明：
    /// budget：当前帧预算，包含事件数量预算和时间预算。
    /// options：邮箱 Pump 配置。
    /// stats：Pump 统计构建器。
    /// maxEvents：当前 Column 本次最多允许连续处理多少事件。
    ///
    /// 作用：
    /// 默认实现只调用一次 PumpOne，用于保持兼容。
    /// 真正的高性能 Column 可以 override 这个方法。
    /// </summary>
    public virtual ActorPumpManyResult PumpMany(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats,
        int maxEvents)
    {
        if (maxEvents <= 0)
        {
            return ActorPumpManyResult.NoWork();
        }

        ActorColumnPumpResult result = PumpOne(
            budget: ref budget,
            options: in options,
            stats: stats);

        if (result == ActorColumnPumpResult.Processed)
        {
            return ActorPumpManyResult.ProcessedBatch(1);
        }

        if (result == ActorColumnPumpResult.ActorLimited)
        {
            return new ActorPumpManyResult(
                processed: 0,
                result: PumpOneResult.ActorLimited);
        }

        return ActorPumpManyResult.NoWork();
    }

    public abstract bool HasPendingWork();

    public abstract void EnsureSlotCapacity(int slotIndex);

    public abstract void RefreshPostRowBinding();

    public abstract void ClearMail(int slotIndex);

    public abstract int GetPendingCount(int slotIndex);

    public abstract int GetTotalPendingCount();
}
```

---

## 10. 修改 EventColumn<TActor, TEvent>

目标文件：

```text
LayerBase/Actor/Mail/EventColumn.cs
```

新增 `PumpMany` override。

```csharp
public override ActorPumpManyResult PumpMany(
    ref RuntimeFrameBudget budget,
    in ActorMailPumpOptions options,
    ActorMailPumpStatsBuilder stats,
    int maxEvents)
{
    // 如果当前配置不适合批量快路径，则回退默认实现。
    // 这样可以保证复杂限流、释放空邮箱等场景不被破坏。
    if (!CanUsePumpManyFast(options))
    {
        return base.PumpMany(
            budget: ref budget,
            options: in options,
            stats: stats,
            maxEvents: maxEvents);
    }

    int processed = 0;

    while (processed < maxEvents &&
           budget.HasRemainingEventBudget() &&
           _dirtySlots.TryPeek(out int slotIndex))
    {
        ref EventMail<TEvent> mail = ref _mails[slotIndex];

        // 从当前 slot 的邮箱中取出一个事件。
        // 如果没有事件，说明 dirty 标记已经过期，直接移除。
        if (!EventMailReader.TryDequeue(ref mail, _mailPool, out TEvent value))
        {
            _dirtySlots.Pop();
            continue;
        }

        // 检查当前 slot 是否仍然可 Pump。
        // 这里会过滤 PendingDestroy、Destroying、空 Actor 等情况。
        if (!_owner.CanPumpSlot(slotIndex))
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

        // 调用 ActorBehaviour invoker。
        // _invoker 通常由生成器或 Actor 元数据构建。
        _invoker(actor, in value);

        // 消耗一个事件预算。
        budget.ConsumeEvent();
        processed++;

        // 当前邮箱清空后移除 dirty slot。
        // 如果还有事件，则移动到队尾，保留基本公平性。
        if (mail.Count == 0)
        {
            _dirtySlots.Pop();
        }
        else
        {
            _dirtySlots.MoveHeadToTail();
        }
    }

    if (processed > 0)
    {
        return ActorPumpManyResult.ProcessedBatch(processed);
    }

    return ActorPumpManyResult.NoWork();
}

/// <summary>
/// 判断当前 Column 是否可以使用批量 Pump 快路径。
///
/// 参数说明：
/// options：Actor 邮箱 Pump 配置。
///
/// 返回值：
/// true 表示可以连续处理多个事件。
/// false 表示必须回退 PumpOne。
/// </summary>
private bool CanUsePumpManyFast(in ActorMailPumpOptions options)
{
    return options.MaxMailsPerActorPerPump <= 0
           && options.MaxMailsPerBucketPerPump <= 0
           && !_options.ReleaseWhenEmpty;
}
```

---

## 11. 修改 IActorEventBucket

目标文件：

```text
LayerBase/Actor/Mail/IActorEventBucket.cs
```

新增 `PumpMany`：

```csharp
namespace LayerBase.Actor;

internal interface IActorEventBucket
{
    PumpOneResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats,
        int bucketIndex);

    /// <summary>
    /// 批量 Pump 当前 Bucket。
    ///
    /// 参数说明：
    /// budget：当前帧预算。
    /// options：邮箱 Pump 配置。
    /// stats：Pump 统计构建器。
    /// bucketIndex：当前 bucket 的索引。
    /// maxEvents：本次最多处理多少事件。
    ///
    /// 作用：
    /// 减少每处理一个事件就返回 ActorWorld 外层调度器的成本。
    /// </summary>
    ActorPumpManyResult PumpMany(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats,
        int bucketIndex,
        int maxEvents);

    bool HasPendingWork();
}
```

---

## 12. 修改 ActorEventBucket<TEvent>

目标文件：

```text
LayerBase/Actor/Mail/ActorEventBucket.cs
```

新增 `PumpMany`：

```csharp
public ActorPumpManyResult PumpMany(
    ref RuntimeFrameBudget budget,
    in ActorMailPumpOptions options,
    ActorMailPumpStatsBuilder stats,
    int bucketIndex,
    int maxEvents)
{
    if (_count == 0 || maxEvents <= 0)
    {
        return ActorPumpManyResult.NoWork();
    }

    if (!stats.CanProcessBucket(bucketIndex, options))
    {
        stats.BucketLimitHits++;

        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.BucketLimited);
    }

    int totalProcessed = 0;
    int checkedCount = 0;
    bool actorLimited = false;

    while (checkedCount < _count &&
           totalProcessed < maxEvents &&
           budget.HasRemainingEventBudget())
    {
        int index = _cursor;

        // 轮转 cursor，避免长期偏向某一个 column。
        _cursor = index + 1 == _count ? 0 : index + 1;
        checkedCount++;

        ActorEventColumnRuntime column = _columns[index];

        int remaining = maxEvents - totalProcessed;

        ActorPumpManyResult result = column.PumpMany(
            budget: ref budget,
            options: in options,
            stats: stats,
            maxEvents: remaining);

        if (result.Processed > 0)
        {
            totalProcessed += result.Processed;
            stats.ProcessedTotal += result.Processed;

            if (options.MaxMailsPerBucketPerPump > 0)
            {
                for (int i = 0; i < result.Processed; i++)
                {
                    stats.RecordBucketProcessed(bucketIndex);
                }
            }

            return ActorPumpManyResult.ProcessedBatch(totalProcessed);
        }

        if (result.Result == PumpOneResult.ActorLimited)
        {
            actorLimited = true;
        }

        if (result.Result == PumpOneResult.BucketLimited)
        {
            return result;
        }
    }

    if (totalProcessed > 0)
    {
        return ActorPumpManyResult.ProcessedBatch(totalProcessed);
    }

    return actorLimited
        ? new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.ActorLimited)
        : ActorPumpManyResult.NoWork();
}
```

---

## 13. 修改 ActorWorld.Pump

目标文件：

```text
LayerBase/Actor/Storage/ActorWorld.Pump.cs
```

建议把原来的 `PumpActorBehaviours(...)` 拆成：

```text
PumpActorBehaviours(...)
PumpActorBehavioursOneByOne(...)
PumpActorBehavioursManyFast(...)
```

入口：

```csharp
private ActorMailPumpStats PumpActorBehaviours(
    ref RuntimeFrameBudget budget,
    in ActorMailPumpOptions options)
{
    if (CanUsePumpManyFastPath(in options))
    {
        return PumpActorBehavioursManyFast(
            budget: ref budget,
            options: in options);
    }

    return PumpActorBehavioursOneByOne(
        budget: ref budget,
        options: in options);
}

/// <summary>
/// 判断 ActorWorld 是否可以使用批量 Pump 快路径。
///
/// 参数说明：
/// options：Actor 邮箱 Pump 配置。
///
/// 返回值：
/// true 表示可以使用 PumpManyFast。
/// false 表示保留旧的逐事件 PumpOne。
/// </summary>
private static bool CanUsePumpManyFastPath(
    in ActorMailPumpOptions options)
{
    return options.MaxMailsPerActorPerPump <= 0
           && options.MaxMailsPerBucketPerPump <= 0;
}
```

`PumpActorBehavioursOneByOne(...)` 直接放原来的 `PumpActorBehaviours(...)` 内容。

新增：

```csharp
private ActorMailPumpStats PumpActorBehavioursManyFast(
    ref RuntimeFrameBudget budget,
    in ActorMailPumpOptions options)
{
    ActorMailPumpStatsBuilder stats = _mailPumpStatsBuilder;
    stats.Reset();

    int processedSinceTimeCheck = 0;

    while (budget.HasRemainingEventBudget()
           && (options.MaxTotalMailsPerPump <= 0 ||
               stats.ProcessedTotal < options.MaxTotalMailsPerPump))
    {
        if (processedSinceTimeCheck <= 0)
        {
            if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
            {
                break;
            }

            processedSinceTimeCheck = options.TimeCheckInterval;
        }

        int remainingByBudget = budget.RemainingEventBudget;

        int remainingByOption = options.MaxTotalMailsPerPump > 0
            ? options.MaxTotalMailsPerPump - stats.ProcessedTotal
            : remainingByBudget;

        int maxEvents = Math.Min(
            remainingByBudget,
            remainingByOption);

        ActorPumpManyResult result = TryPumpMany(
            budget: ref budget,
            options: in options,
            stats: stats,
            maxEvents: maxEvents);

        if (result.Processed > 0)
        {
            processedSinceTimeCheck -= result.Processed;
            continue;
        }

        if (result.Result == PumpOneResult.EmptyBucket)
        {
            stats.EmptyBucketChecks++;

            if (options.MaxEmptyBucketChecksPerPump > 0 &&
                stats.EmptyBucketChecks >= options.MaxEmptyBucketChecksPerPump)
            {
                break;
            }

            continue;
        }

        if (result.Result == PumpOneResult.BucketLimited ||
            result.Result == PumpOneResult.ActorLimited ||
            result.Result == PumpOneResult.NoWork)
        {
            break;
        }
    }

    return stats.Build(CountRemainingDirtyBuckets());
}
```

新增：

```csharp
private ActorPumpManyResult TryPumpMany(
    ref RuntimeFrameBudget budget,
    in ActorMailPumpOptions options,
    ActorMailPumpStatsBuilder stats,
    int maxEvents)
{
    // 第一版只对 Event bucket 启用批量快路径。
    // Call bucket 涉及 request/response 语义，先保留旧路径。
    return TryPumpManyFromDirtyBuckets(
        dirtyBuckets: _dirtyEventBuckets,
        buckets: _eventBucketsByEventId,
        cursor: ref _bucketCursor,
        budget: ref budget,
        options: in options,
        stats: stats,
        maxEvents: maxEvents);
}
```

新增：

```csharp
private static ActorPumpManyResult TryPumpManyFromDirtyBuckets(
    DirtyBucketList dirtyBuckets,
    IActorEventBucket[] buckets,
    ref int cursor,
    ref RuntimeFrameBudget budget,
    in ActorMailPumpOptions options,
    ActorMailPumpStatsBuilder stats,
    int maxEvents)
{
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
            if (current.HasPendingWork())
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
        }
        else if (result.Result == PumpOneResult.ActorLimited)
        {
            sawActorLimit = true;
            dirtyBuckets.MoveHeadToTail();
        }
        else
        {
            dirtyBuckets.Pop();
        }
    }

    if (sawBucketLimit)
    {
        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.BucketLimited);
    }

    if (sawActorLimit)
    {
        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.ActorLimited);
    }

    return new ActorPumpManyResult(
        processed: 0,
        result: PumpOneResult.EmptyBucket);
}
```

---

## 14. RuntimeFrameBudget 增加 RemainingEventBudget

目标文件按真实位置调整。

```csharp
public int RemainingEventBudget
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
        // maxEvents <= 0 表示不限制事件数量。
        if (MaxEvents <= 0)
        {
            return int.MaxValue;
        }

        int remaining = MaxEvents - UsedEvents;
        return remaining > 0 ? remaining : 0;
    }
}
```

如果当前字段不是 `MaxEvents` / `UsedEvents`，按真实字段名替换。

---

# 第二部分：ProjectedActorRef 设计

## 15. 当前真实问题

当前 `WithProjectedActor<TActor>()` 会：

```text
ActorType<TActor>.Id
ProjectedActorTypeRegistry.RegisterGenerated(...)
WithProjectedActor(world, entity, actorTypeId, ...)
meta.MarkProjected(...)
```

它只标记 Entity 可投影，不提供公开的 ActorId 缓存组件。

`ProjectedActorMeta.HasActor` 只是判断 `ActorId.IsValid`，说明 ActorId 本身读取很便宜，真正成本在 `Entity → ProjectionMeta` 的查找。

---

## 16. 新增 ProjectedActorRef

新增文件：

```text
LayerBase/ECS/Projection/ProjectedActorRef.cs
```

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 的公开 ActorId 缓存组件。
///
/// 作用：
/// 1. 让业务 ECS Query 可以直接拿到 ActorId。
/// 2. 避免每帧通过 Entity 反查 ProjectedActorMeta。
/// 3. 不暴露 internal ProjectedActorMeta。
/// </summary>
public struct ProjectedActorRef
{
    /// <summary>
    /// 当前 Entity 绑定的 ActorId。
    /// ActorId 是 ActorWorld 中定位 Actor 的轻量句柄。
    /// </summary>
    public ActorId ActorId;

    /// <summary>
    /// 当前 ActorId 是否有效。
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ActorId.IsValid;
    }

    /// <summary>
    /// 构造 ProjectedActorRef。
    ///
    /// 参数说明：
    /// actorId：当前 Entity 对应的 ActorId。
    /// </summary>
    public ProjectedActorRef(ActorId actorId)
    {
        ActorId = actorId;
    }
}
```

---

## 17. 新增 ProjectedActorBindingUtility

新增文件：

```text
LayerBase/ECS/Projection/ProjectedActorBindingUtility.cs
```

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 绑定工具。
/// 作用：统一维护 ProjectedActorMeta 和 ProjectedActorRef 的一致性。
/// </summary>
internal static class ProjectedActorBindingUtility
{
    /// <summary>
    /// 绑定 Projected Actor。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// entity：需要绑定 Actor 的 Entity。
    /// meta：ProjectedActorMeta 引用。
    /// actorId：新绑定的 ActorId。
    ///
    /// 作用：
    /// 同时写入 internal meta 和 public ref。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta,
        ActorId actorId)
    {
        meta.BindActor(actorId);
        UpsertRef(world, entity, actorId);
    }

    /// <summary>
    /// 清理 Projected Actor 绑定。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// entity：需要清理绑定的 Entity。
    /// meta：ProjectedActorMeta 引用。
    ///
    /// 作用：
    /// 同时清理 internal meta 和 public ref。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta)
    {
        meta.ClearActor();
        UpsertRef(world, entity, ActorId.Invalid);
    }

    /// <summary>
    /// 插入或更新 ProjectedActorRef。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// entity：目标 Entity。
    /// actorId：要写入的 ActorId。
    ///
    /// 作用：
    /// 如果组件存在则 Set，不存在则 Add。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpsertRef(
        World world,
        Entity entity,
        ActorId actorId)
    {
        var actorRef = new ProjectedActorRef(actorId);

        if (world.Has<ProjectedActorRef>(entity))
        {
            world.Set(entity, actorRef);
        }
        else
        {
            world.Add(entity, actorRef);
        }
    }
}
```

---

## 18. 替换 BindActor / ClearActor 调用点

### 18.1 绑定点

把所有：

```csharp
meta.BindActor(actorId);
```

替换成：

```csharp
ProjectedActorBindingUtility.Bind(
    world,
    entity,
    ref meta,
    actorId);
```

### 18.2 清理点

把所有能拿到 `world` 和 `entity` 的：

```csharp
meta.ClearActor();
```

替换成：

```csharp
ProjectedActorBindingUtility.Clear(
    world,
    entity,
    ref meta);
```

尤其是 `ActiveProjectedActorList.Sweep(...)` 内部的清理逻辑需要同步更新 `ProjectedActorRef`。

---

## 19. ProjectedActorRef 高频投递 API

新增文件：

```text
LayerBase/ECS/Projection/ProjectedActorPostExtensions.cs
```

```csharp
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 高频投递扩展。
/// 作用：通过 ProjectedActorRef 快速投递事件，避免 TryGetProjectionMeta。
/// </summary>
public static class ProjectedActorPostExtensions
{
    /// <summary>
    /// 对带有 T1 / T2 / ProjectedActorRef 的实体批量投递事件。
    ///
    /// 类型参数说明：
    /// T1：第一个 ECS 组件类型，例如 Position。
    /// T2：第二个 ECS 组件类型，例如 Velocity。
    /// TEvent：投递给 Actor 的事件类型。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// actorWorld：ActorWorld。
    /// value：事件值。
    ///
    /// 作用：
    /// 直接通过 ProjectedActorRef.ActorId 投递，减少 Projection Lookup 成本。
    /// </summary>
    public static void PostProjected<T1, T2, TEvent>(
        this World world,
        ActorWorld actorWorld,
        in TEvent value)
        where T1 : struct
        where T2 : struct
        where TEvent : struct
    {
        var query = new QueryDescription()
            .WithAll<T1, T2, ProjectedActorRef>();

        world.Query(
            in query,
            (ref T1 c1, ref T2 c2, ref ProjectedActorRef actorRef) =>
            {
                if (!actorRef.IsValid)
                {
                    return;
                }

                actorWorld.PostTo(actorRef.ActorId, in value);
            });
    }
}
```

---

# 第三部分：测试与验证

## 20. 必须保留的旧 benchmark

```text
Actor: PostTo Only × 1000
Actor: Pump Only × 1000
Actor: PostTo + Pump × 1000
Hybrid Isolate: Cached ActorId PostTo + Pump × 1000
Projection: Entity → ActorId Lookup × 1000
Full Pipeline: ECS Query → Projection Lookup → Actor PostTo → Pump × 1000
```

---

## 21. 新增 PumpManyFast benchmark

如果你通过配置开关启用/禁用 PumpManyFast，建议新增对照：

```text
Actor: Pump Only × 1000 [PumpOne]
Actor: Pump Only × 1000 [PumpManyFast]
Actor: PostTo + Pump × 1000 [PumpOne]
Actor: PostTo + Pump × 1000 [PumpManyFast]
```

目标：

```text
Pump Only × 1000 从 212 μs 明显下降。
```

---

## 22. 新增 ProjectedActorRef benchmark

```text
Projection: ProjectedActorRef Read × 1000
Hybrid: ECS Query → ProjectedActorRef → Actor PostTo × 1000
Full Pipeline: ECS Query → ProjectedActorRef → Actor PostTo → Pump × 1000
```

目标：

```text
ProjectedActorRef Read × 1000 明显低于 Entity → ActorId Lookup × 1000。
```

---

## 23. 正确性测试

### 23.1 PumpManyFast 正确性

测试点：

```text
1. 1000 个 Actor 各 1 个 MoveEvent，Pump 后 MoveCount 全部 +1。
2. 1 个 Actor 收到 1000 个 MoveEvent，Pump 后 MoveCount +1000。
3. 多个 Actor 多个事件交错投递，Pump 后事件总数正确。
4. Actor PendingDestroy 后，不应继续执行事件。
5. Actor destroyed 后，dirty slot 应被清理。
6. MaxEvents 预算不足时，只处理预算内事件。
7. 下一帧继续 Pump 时，剩余事件能继续处理。
```

### 23.2 ProjectedActorRef 正确性

测试点：

```text
1. WithProjectedActor 后，首次绑定 Actor 时写入 ProjectedActorRef。
2. Actor 回收后，ProjectedActorRef.ActorId 变为 ActorId.Invalid。
3. Actor 重新绑定后，ProjectedActorRef.ActorId 更新为新 ActorId。
4. Entity 销毁或 ProjectionMeta 丢失时，不留下过期 ActorId。
5. Full Pipeline 使用 ProjectedActorRef 时，事件投递目标正确。
```

---

# 第四部分：风险与回退策略

## 24. PumpManyFast 风险

### 24.1 公平性风险

批量处理同一个 Column 可能降低不同 bucket / column 之间的公平性。

缓解：

```text
1. 每次 PumpManyFast 仍然受 maxEvents 限制。
2. 每处理一批后返回 ActorWorld 外层。
3. slot 有剩余事件时仍 MoveHeadToTail。
```

### 24.2 限流语义风险

`MaxMailsPerActorPerPump`、`MaxMailsPerBucketPerPump` 等配置可能被绕过。

缓解：

```text
只在这些配置关闭时启用 PumpManyFast。
```

### 24.3 Call 邮箱语义风险

Call 邮箱涉及 request/response，不适合第一阶段批量化。

缓解：

```text
第一阶段只对 Event bucket 启用 PumpManyFast。
```

---

## 25. ProjectedActorRef 风险

### 25.1 ActorId 过期风险

如果 `ProjectedActorMeta.ClearActor()` 后没有同步清理 `ProjectedActorRef`，业务可能投递到旧 ActorId。

缓解：

```text
统一通过 ProjectedActorBindingUtility.Bind / Clear 修改绑定状态。
```

### 25.2 组件 Add/Set 成本

绑定时写入 `ProjectedActorRef` 会增加一次组件 Add/Set。

判断：

```text
这是绑定/回收阶段成本，不是每帧热路径成本，可以接受。
```

---

# 第五部分：实施计划

## 26. 第一阶段：PumpManyFast

改动文件：

```text
LayerBase/Actor/Mail/ActorPumpManyResult.cs
LayerBase/Actor/Mail/ActorEventColumnRuntime.cs
LayerBase/Actor/Mail/IActorEventBucket.cs
LayerBase/Actor/Mail/ActorEventBucket.cs
LayerBase/Actor/Mail/EventColumn.cs
LayerBase/Actor/Storage/ActorWorld.Pump.cs
RuntimeFrameBudget 所在文件
```

验收：

```text
Actor: Pump Only × 1000 明显下降
Actor: PostTo + Pump × 1000 不引入 GC
所有 Actor 行为正确性测试通过
```

---

## 27. 第二阶段：ProjectedActorRef

改动文件：

```text
LayerBase/ECS/Projection/ProjectedActorRef.cs
LayerBase/ECS/Projection/ProjectedActorBindingUtility.cs
LayerBase/ECS/Projection/ProjectedActorPostExtensions.cs
ActiveProjectedActorList.cs
所有 meta.BindActor / meta.ClearActor 调用点
```

验收：

```text
Projection: ProjectedActorRef Read ×1000 明显低于 Entity → ActorId Lookup ×1000
ProjectedActorRef 不出现过期 ActorId
Full Pipeline ProjectedActorRef 版下降
```

---

## 28. 第三阶段：后续优化

后续再考虑：

```text
1. PostTo 使用 AlivePostGenerations 快路径。
2. Hybrid Query 使用 static lambda + context 消除 64B 捕获。
3. Source Generator 生成无捕获 ProjectedActorRef Query。
4. PumpManyFast 扩展到 Call bucket。
```

---

# 29. 最终结论

当前 LayerBase 不需要继续优先处理 GC。  
真实瓶颈已经变成：

```text
Pump 调度成本
Projection Lookup 成本
```

因此最合理的优化路线是：

```text
第一步：PumpManyFast
第二步：ProjectedActorRef
第三步：AlivePostGenerations 快路径
第四步：Hybrid 无捕获 Query
```

这条路线风险较低，并且和当前 benchmark 数据直接对应。
