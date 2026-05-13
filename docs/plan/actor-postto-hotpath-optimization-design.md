# Actor PostTo Hot Path Optimization Design

## 1. 背景

当前 `PumpMany` 修正后，Actor 事件调度链路已经回到可信区间：

| Benchmark | 当前数据 |
|---|---:|
| `Actor: DispatchNow × 1000` | 53.033 μs |
| `Actor: Pump Only × 1000` | 49.500 μs |
| `Actor: PostTo Only × 1000` | 72.967 μs |
| `Actor: PostTo + Pump × 1000` | 139.667 μs |
| `Hybrid Isolate: Cached ActorId PostTo + Pump × 1000` | 107.067 μs |
| `Full Pipeline × 1000` | 207.933 μs |

这说明：

```text
Pump 阶段已经不是最大瓶颈。
PostTo Only 已经成为 Actor-only 链路中更明显的 CPU 成本。
```

本设计文档只讨论 `PostTo` 热路径优化，不讨论 `PumpMany`、`Projection Lookup` 和 `Lifecycle`。

---

## 2. 当前真实 PostTo 链路

当前入口：

```csharp
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
{
    EventPostState<TEvent>? state =
        EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);

    if (state == null || state.RouteCode == ActorPostRouteCode.Disabled)
    {
        return BuildEventNotSupportedCold<TEvent>();
    }

    if (!TryGetPhysicalRowWithGeneration(
            actorId,
            state,
            out EventPostRow<TEvent> row,
            out int slotIndex))
    {
        return BuildPostFailureCold(actorId);
    }

    switch (state.RouteCode)
    {
        case ActorPostRouteCode.QueuedGrow:
            return PostQueuedGrowCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool,
                state.Options);

        case ActorPostRouteCode.QueuedRejectNew:
            return PostQueuedRejectNewCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool,
                state.Options);

        case ActorPostRouteCode.QueuedDropOldest:
            return PostQueuedDropOldestCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool,
                state.Options);

        case ActorPostRouteCode.Latest:
            return PostLatestCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool);

        case ActorPostRouteCode.Dirty:
            return PostDirtyCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool);

        default:
            return BuildRouteUnsupportedCold<TEvent>();
    }
}
```

这表示每次 `PostTo` 都要经历：

```text
1. 读取 EventPostState<TEvent>
2. 校验 state 是否存在、route 是否可用
3. ActorId → EventPostRow
4. ActorId generation 校验
5. RouteCode switch
6. PostQueuedGrowCore / Latest / Dirty 等具体邮箱写入
7. DirtySlotList.Mark(slotIndex)
8. DirtyBucketList.Mark(bucketIndex)
```

其中 `ActorId` 已经包含：

```csharp
public readonly int ArchetypeId;
public readonly int SlotIndex;
public readonly int Generation;
```

所以本次优化不是“把 slotIndex 存入 ActorId”，而是减少拿到 `slotIndex` 后仍然发生的重复查找与分支。

---

## 3. 优化目标

### 3.1 短期目标

```text
Actor: PostTo Only × 1000
从 72.967 μs 压到 50~60 μs 区间
```

### 3.2 中期目标

```text
Actor: PostTo + Pump × 1000
从 139.667 μs 压到 100~120 μs 区间
```

### 3.3 长期目标

```text
Hybrid Isolate: Cached ActorId PostTo + Pump × 1000
稳定进入 100 μs 内
```

---

## 4. P0：EventPostRow 增加 AlivePostGenerations

### 4.1 当前问题

当前 `TryGetPhysicalRowWithGeneration` 大致是：

```csharp
private bool TryGetPhysicalRowWithGeneration<TEvent>(
    ActorId actorId,
    EventPostState<TEvent> state,
    out EventPostRow<TEvent> row,
    out int slotIndex)
    where TEvent : struct
{
    row = state.RowsByArchetype[actorId.ArchetypeId];
    slotIndex = actorId.SlotIndex;

    return (uint)slotIndex < (uint)row.Mails.Length
           && _archetypes[actorId.ArchetypeId].IsCurrentGeneration(actorId);
}
```

问题有两个：

```text
1. actorId.ArchetypeId 没有越界保护。
2. generation 校验需要进入 _archetypes[archetypeId].IsCurrentGeneration(actorId)。
```

其中第 2 点是热路径性能成本，第 1 点是 public API 安全风险。

### 4.2 设计思路

`TypedActorStorage<TActor>` 已经维护：

```csharp
private int[] _alivePostGenerations;
private int[] _enabledPostGenerations;
```

并且在 slot 状态改变时调用：

```csharp
RefreshPostGenerations(slotIndex);
```

因此可以把 `_alivePostGenerations` 挂到 `EventPostRow<TEvent>` 上，让 `PostTo` 直接用数组判断：

```text
row.AlivePostGenerations[slotIndex] == actorId.Generation
```

这样可以绕开：

```text
_archetypes[archetypeId].IsCurrentGeneration(actorId)
```

### 4.3 修改文件

```text
LayerBase/Actor/Mail/EventPostRow.cs
LayerBase/Actor/Storage/ActorWorld.FastPath.cs
LayerBase/Actor/Storage/TypedActorStorage.cs
```

### 4.4 修改 EventPostRow

```csharp
namespace LayerBase.Actor;

internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 当前事件类型对应的邮箱列。
    /// 下标是 ActorId.SlotIndex。
    /// </summary>
    public readonly EventMail<TEvent>[] Mails;

    /// <summary>
    /// 当前事件列的 dirty slot 列表。
    /// 当某个 slot 从无事件变为有事件时，会写入这里。
    /// </summary>
    public readonly DirtySlotList DirtySlots;

    /// <summary>
    /// 当前事件类型对应的 dirty bucket 下标。
    /// ActorWorld.Pump 会通过它找到有待处理事件的 bucket。
    /// </summary>
    public readonly int BucketIndex;

    /// <summary>
    /// 当前 archetype 下每个 slot 的可投递 generation。
    /// 下标是 ActorId.SlotIndex。
    /// 值等于 ActorId.Generation 时，表示这个 ActorId 当前仍然有效。
    /// 值为 -1 时，表示该 slot 当前不可投递。
    /// </summary>
    public readonly int[] AlivePostGenerations;

    /// <summary>
    /// 当前 row 是否有效。
    /// 空 Mails 表示该 archetype 不支持这个事件类型。
    /// </summary>
    public bool IsValid => Mails.Length > 0;

    /// <summary>
    /// 构造 EventPostRow。
    /// </summary>
    /// <param name="mails">
    /// 当前事件类型对应的邮箱数组。
    /// </param>
    /// <param name="dirtySlots">
    /// 当前事件列的 dirty slot 列表。
    /// </param>
    /// <param name="bucketIndex">
    /// 当前事件类型对应的 bucket 下标。
    /// </param>
    /// <param name="alivePostGenerations">
    /// 当前 archetype 下每个 slot 的可投递 generation 缓存。
    /// </param>
    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[] alivePostGenerations)
    {
        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        AlivePostGenerations = alivePostGenerations;
    }
}
```

### 4.5 修改无效 Row 创建

```csharp
private static EventPostRow<TEvent> CreateInvalidRow<TEvent>()
    where TEvent : struct
{
    return new EventPostRow<TEvent>(
        mails: Array.Empty<EventMail<TEvent>>(),
        dirtySlots: DirtySlotList.Empty,
        bucketIndex: -1,
        alivePostGenerations: Array.Empty<int>());
}
```

### 4.6 修改 RegisterEventPostRow

当前：

```csharp
internal void RegisterEventPostRow<TEvent>(
    int archetypeId,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    ActorEventPostPlan<TEvent> plan)
    where TEvent : struct
```

建议改成：

```csharp
internal void RegisterEventPostRow<TEvent>(
    int archetypeId,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    ActorEventPostPlan<TEvent> plan,
    int[] alivePostGenerations)
    where TEvent : struct
{
    EventPostState<TEvent> state = GetOrCreateEventPostState(plan);
    EnsureRowsCapacity(ref state.RowsByArchetype, archetypeId);

    state.RowsByArchetype[archetypeId] = new EventPostRow<TEvent>(
        mails: mails,
        dirtySlots: dirtySlots,
        bucketIndex: bucketIndex,
        alivePostGenerations: alivePostGenerations);
}
```

### 4.7 修改调用点

`EventColumn<TActor,TEvent>.RefreshPostRowBinding()` 或对应注册位置应传入：

```csharp
_owner.AlivePostGenerations
```

示意：

```csharp
_world.RegisterEventPostRow(
    archetypeId: _owner.ArchetypeId,
    mails: _mails,
    dirtySlots: _dirtySlots,
    bucketIndex: _bucketIndex,
    plan: _plan,
    alivePostGenerations: _owner.AlivePostGenerations);
```

### 4.8 修改 TryGetPhysicalRowWithGeneration

建议改成 `internal static`，方便 `PreparedActorPoster<TEvent>` 复用。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static bool TryGetPhysicalRowWithGeneration<TEvent>(
    ActorId actorId,
    EventPostState<TEvent> state,
    out EventPostRow<TEvent> row,
    out int slotIndex)
    where TEvent : struct
{
    EventPostRow<TEvent>[] rows = state.RowsByArchetype;
    int archetypeId = actorId.ArchetypeId;

    // 用 uint 比较同时处理负数和越界。
    if ((uint)archetypeId >= (uint)rows.Length)
    {
        row = default;
        slotIndex = default;
        return false;
    }

    row = rows[archetypeId];
    slotIndex = actorId.SlotIndex;

    // row.Mails 是当前 archetype 对应该事件的物理邮箱列。
    if ((uint)slotIndex >= (uint)row.Mails.Length)
    {
        return false;
    }

    // 直接用 row 内 generation 缓存判断 ActorId 是否仍然有效。
    return row.AlivePostGenerations[slotIndex] == actorId.Generation;
}
```

### 4.9 验证项

```text
Actor: PostTo Only × 1000
Actor: PostTo + Pump × 1000
Actor: Unsupported Event Post Only × 100
无效 ActorId PostTo 测试
跨 World ActorId PostTo 测试
```

新增单测：

```csharp
[Fact]
public void PostTo_InvalidActorId_ShouldReturnFailure_InsteadOfThrow()
{
    var world = new ActorWorld();
    var result = world.PostTo(ActorId.Invalid, in new MoveEvent());

    Assert.False(result.IsSuccess);
}
```

---

## 5. P1：PreparedActorPoster

### 5.1 当前问题

当前 `PostToMany` 是：

```csharp
public void PostToMany<TEvent>(
    ReadOnlySpan<ActorId> actorIds,
    in TEvent value)
    where TEvent : struct
{
    foreach (ActorId actorId in actorIds)
    {
        _ = PostTo(actorId, in value);
    }
}
```

这意味着批量投递时，仍然每次重复：

```text
1. GetStateUnchecked(RuntimeIndex)
2. state null / Disabled 判断
3. switch RouteCode
4. 读取 state.Pool / state.Options
```

### 5.2 设计思路

新增 `PreparedActorPoster<TEvent>`：

```text
Prepare 一次 EventPostState 和 RouteCode。
循环中只做 actorId → row → slotIndex → write mail。
```

### 5.3 修改文件

```text
LayerBase/Actor/Storage/ActorWorld.PreparedPost.cs
LayerBase/Actor/Storage/ActorWorld.Post.cs
```

### 5.4 新增 PreparedActorPoster

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

/// <summary>
/// 预准备的 Actor 事件投递器。
/// 适用于同一批次内向多个 Actor 投递同一种事件。
/// </summary>
/// <typeparam name="TEvent">
/// 要投递的事件类型。
/// </typeparam>
public readonly ref struct PreparedActorPoster<TEvent>
    where TEvent : struct
{
    private readonly ActorWorld _world;
    private readonly EventPostState<TEvent>? _state;
    private readonly ActorPostRouteCode _routeCode;

    /// <summary>
    /// 当前 poster 是否有效。
    /// false 表示该事件类型没有可用路由。
    /// </summary>
    public bool IsValid => _state != null && _routeCode != ActorPostRouteCode.Disabled;

    /// <summary>
    /// 构造 PreparedActorPoster。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    /// </param>
    /// <param name="state">
    /// 当前事件类型在该 ActorWorld 内的投递状态。
    /// </param>
    internal PreparedActorPoster(
        ActorWorld world,
        EventPostState<TEvent>? state)
    {
        _world = world;
        _state = state;
        _routeCode = state?.RouteCode ?? ActorPostRouteCode.Disabled;
    }

    /// <summary>
    /// 向指定 Actor 投递事件。
    /// </summary>
    /// <param name="actorId">
    /// 目标 ActorId，包含 ArchetypeId、SlotIndex、Generation。
    /// </param>
    /// <param name="value">
    /// 要投递的事件值。
    /// </param>
    /// <returns>
    /// 投递结果。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult Post(
        ActorId actorId,
        in TEvent value)
    {
        EventPostState<TEvent>? state = _state;
        if (state == null || _routeCode == ActorPostRouteCode.Disabled)
        {
            return ActorWorld.BuildEventNotSupportedCold<TEvent>();
        }

        if (!ActorWorld.TryGetPhysicalRowWithGeneration(
                actorId,
                state,
                out EventPostRow<TEvent> row,
                out int slotIndex))
        {
            return PostResult.Failure(
                ActorPostStatus.PhysicalTargetInvalid,
                PostFailureKind.PhysicalTargetInvalid);
        }

        return _routeCode switch
        {
            ActorPostRouteCode.QueuedGrow =>
                _world.PostQueuedGrowCore(
                    slotIndex,
                    in value,
                    row.Mails,
                    row.DirtySlots,
                    row.BucketIndex,
                    state.Pool,
                    state.Options),

            ActorPostRouteCode.QueuedRejectNew =>
                _world.PostQueuedRejectNewCore(
                    slotIndex,
                    in value,
                    row.Mails,
                    row.DirtySlots,
                    row.BucketIndex,
                    state.Pool,
                    state.Options),

            ActorPostRouteCode.QueuedDropOldest =>
                _world.PostQueuedDropOldestCore(
                    slotIndex,
                    in value,
                    row.Mails,
                    row.DirtySlots,
                    row.BucketIndex,
                    state.Pool,
                    state.Options),

            ActorPostRouteCode.Latest =>
                _world.PostLatestCore(
                    slotIndex,
                    in value,
                    row.Mails,
                    row.DirtySlots,
                    row.BucketIndex,
                    state.Pool),

            ActorPostRouteCode.Dirty =>
                _world.PostDirtyCore(
                    slotIndex,
                    in value,
                    row.Mails,
                    row.DirtySlots,
                    row.BucketIndex,
                    state.Pool),

            _ =>
                ActorWorld.BuildEventNotSupportedCold<TEvent>()
        };
    }
}
```

### 5.5 ActorWorld 新增 PreparePost

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public PreparedActorPoster<TEvent> PreparePost<TEvent>()
    where TEvent : struct
{
    EventPostState<TEvent>? state =
        EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);

    return new PreparedActorPoster<TEvent>(
        world: this,
        state: state);
}
```

### 5.6 修改 PostToMany

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void PostToMany<TEvent>(
    ReadOnlySpan<ActorId> actorIds,
    in TEvent value)
    where TEvent : struct
{
    PreparedActorPoster<TEvent> poster = PreparePost<TEvent>();

    for (int i = 0; i < actorIds.Length; i++)
    {
        _ = poster.Post(actorIds[i], in value);
    }
}
```

### 5.7 模板侧建议

T4 / source generator 中的批量 Post 应优先生成：

```csharp
var poster = actorWorld.PreparePost<TEvent>();

for (...)
{
    poster.Post(actorId, in evt);
}
```

不要在循环中生成：

```csharp
actorWorld.PostTo(actorId, in evt);
```

---

## 6. P2：QueuedGrowHotCore

### 6.1 当前问题

`PostQueuedGrowCore` 是通用路径。它必须处理：

```text
1. 邮箱未分配
2. 邮箱已满
3. grow 成功
4. grow 失败后的 RejectNew / DropOldest / DropNewest / OverwriteLatest
```

但 benchmark 和大多数稳定热路径中，通常满足：

```text
1. 邮箱已经预热
2. 邮箱未满
3. RouteCode 是 QueuedGrow
```

因此可以为 prepared poster 增加更短的 hot core。

### 6.2 设计要求

```text
1. 不替代通用 PostQueuedGrowCore。
2. 热路径前提不满足时必须 fallback。
3. 只在 PreparedActorPoster 或内部批量路径使用。
```

### 6.3 示例代码

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal PostResult PostQueuedGrowHotCore<TEvent>(
    int slotIndex,
    in TEvent value,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    EventMailPool<TEvent> pool,
    ActorMailOptions options)
    where TEvent : struct
{
    ref EventMail<TEvent> mail = ref mails[slotIndex];

    // 热路径前提不满足，回退通用路径。
    if (mail.Buffer == null || mail.Count >= mail.Capacity)
    {
        return PostQueuedGrowCore(
            slotIndex,
            in value,
            mails,
            dirtySlots,
            bucketIndex,
            pool,
            options);
    }

    TEvent[] buffer = mail.Buffer;
    buffer[mail.Tail] = value;

    int nextTail = mail.Tail + 1;
    if (nextTail == mail.Capacity)
    {
        nextTail = 0;
    }

    mail.Tail = nextTail;
    mail.Count++;

    if (mail.Count == 1)
    {
        dirtySlots.Mark(slotIndex);
        _dirtyEventBuckets.Mark(bucketIndex);
    }

    return PostResult.Success;
}
```

### 6.4 PreparedActorPoster 中使用

```csharp
ActorPostRouteCode.QueuedGrow =>
    _world.PostQueuedGrowHotCore(
        slotIndex,
        in value,
        row.Mails,
        row.DirtySlots,
        row.BucketIndex,
        state.Pool,
        state.Options),
```

---

## 7. P3：Dirty 标记批量优化

### 7.1 当前问题

`WriteQueued` 在邮箱从空变为非空时会调用：

```csharp
dirtySlots.Mark(slotIndex);
_dirtyEventBuckets.Mark(bucketIndex);
```

`DirtySlotList.Mark` 当前会做：

```text
1. EnsureMarkCapacity
2. _marks[slotIndex] == _stamp 去重
3. EnsureItemCapacity
4. tail = (_head + _count) % _items.Length
5. 写入 slotIndex
6. _count++
```

`DirtyBucketList.Mark` 类似。

在 `1000 Actor × 1 Event` 的场景下，`dirtySlots.Mark(slotIndex)` 会发生 1000 次。

### 7.2 优化策略 A：MarkKnownCapacity

新增不扩容版本，只在容量已确认足够时使用。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void MarkKnownCapacity(int slotIndex)
{
    if (_marks[slotIndex] == _stamp)
    {
        return;
    }

    _marks[slotIndex] = _stamp;

    int tail = _head + _count;
    if (tail >= _items.Length)
    {
        tail -= _items.Length;
    }

    _items[tail] = slotIndex;
    _count++;
}
```

使用前提：

```text
1. slotIndex < _marks.Length
2. _count + 1 <= _items.Length
```

通用 `Mark` 保留。

### 7.3 优化策略 B：批量投递时只 Mark bucket 一次

在同一个 prepared poster 批次内，如果事件类型相同，`bucketIndex` 也相同。可以只对 bucket mark 一次。

示意：

```csharp
private bool _bucketMarked;

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void MarkDirtyBatch(
    DirtySlotList dirtySlots,
    int slotIndex,
    int bucketIndex)
{
    dirtySlots.Mark(slotIndex);

    if (!_bucketMarked)
    {
        _world.MarkDirtyEventBucket(bucketIndex);
        _bucketMarked = true;
    }
}
```

注意：`_bucketMarked` 只能表达当前 poster 批次内是否已经 mark，不能替代 `DirtyBucketList` 自己的去重逻辑。

---

## 8. Benchmark 计划

### 8.1 新增 PostTo 拆分测试

```text
Actor: PostTo Only × 1000
Actor: PostToMany Prepared × 1000
Actor: PostToMany Prepared HotCore × 1000
Actor: PostTo InvalidId × 1000
```

### 8.2 新增 Dirty 标记测试

```text
DirtySlotList.Mark × 1000
DirtySlotList.MarkKnownCapacity × 1000
DirtyBucketList.Mark SameBucket × 1000
WriteQueued WithDirty × 1000
WriteQueued NoDirty × 1000
```

### 8.3 新增 Hybrid 测试

```text
Hybrid: ECS Query → PreparedPoster → Actor PostTo × 1000
Full Pipeline: ECS Query → PreparedPoster → Actor PostTo → Pump × 1000
```

### 8.4 正确性校验

`PumpOnly`、`PostTo+Pump`、`PreparedPoster` benchmark 必须校验：

```text
MoveCount 增加数量 == 投递事件数量
```

校验建议放在 `IterationCleanup(Target = ...)`，避免计入 benchmark 方法体。

---

## 9. 风险分析

### 9.1 AlivePostGenerations 风险

风险：

```text
RefreshPostGenerations 漏调用会导致 ActorId 校验错误。
```

必须覆盖这些状态变化：

```text
CreateActor
DestroyActor
MarkPendingDestroy
FinalizeDestroySlot
SetEnable
ReleaseProjectedActor
Slot recycle
```

### 9.2 PreparedActorPoster 风险

风险：

```text
state 被 world reset / unbind 后 poster 仍被使用。
```

控制策略：

```text
PreparedActorPoster 使用 ref struct。
只能在栈上使用，不能保存到字段，降低悬挂引用风险。
```

### 9.3 HotCore 风险

风险：

```text
绕过通用逻辑导致 mailbox 未分配或满载时语义错误。
```

控制策略：

```text
热路径前提不满足时 fallback 到 PostQueuedGrowCore。
```

### 9.4 Dirty 标记风险

风险：

```text
MarkKnownCapacity 使用前提不满足会越界。
```

控制策略：

```text
只在内部确认容量足够的路径使用。
public Mark 保留安全扩容逻辑。
```

---

## 10. 推荐施工顺序

```text
Step 1：EventPostRow 增加 AlivePostGenerations
Step 2：TryGetPhysicalRowWithGeneration 改成 row generation 数组校验
Step 3：补 invalid ActorId / cross-world ActorId 测试
Step 4：新增 PreparedActorPoster<TEvent>
Step 5：PostToMany 改用 PreparedActorPoster
Step 6：T4 / Source Generator 的批量投递改用 PreparePost
Step 7：新增 QueuedGrowHotCore，并只在 PreparedPoster 中使用
Step 8：单独 benchmark DirtySlotList.Mark / DirtyBucketList.Mark
Step 9：根据 Dirty benchmark 决定是否做 MarkKnownCapacity
```

---

## 11. 验收标准

### 11.1 性能标准

```text
Actor: PostTo Only × 1000
目标：72.967 μs → 50~60 μs

Actor: PostTo + Pump × 1000
目标：139.667 μs → 100~120 μs

Hybrid Isolate × 1000
目标：107.067 μs → 90~100 μs
```

### 11.2 GC 标准

```text
所有热路径保持 0 B 分配。
```

包括：

```text
Actor: PostTo Only × 1000
Actor: PostTo + Pump × 1000
Hybrid Isolate × 1000
PreparedPoster × 1000
```

### 11.3 正确性标准

```text
MoveCount 增加数量必须等于投递事件数量。
无效 ActorId 不能抛异常。
跨 World ActorId 不能抛异常。
Destroyed ActorId 必须返回 Failure。
PendingDestroy ActorId 必须返回 Failure。
```

---

## 12. 最终结论

`PostTo` 的优化重点不是 `ActorId` 字段设计，因为 `SlotIndex` 已经在 `ActorId` 里。

真正应该优化的是：

```text
1. 用 AlivePostGenerations 消除 generation 间接校验。
2. 用 PreparedActorPoster 减少批量投递时的重复 state 查询和 route switch。
3. 用 QueuedGrowHotCore 压缩默认队列写入路径。
4. 根据 Dirty benchmark 决定是否优化 dirty slot / bucket 标记。
```

其中最推荐先做的是：

```text
EventPostRow<TEvent> + AlivePostGenerations
```

这一步同时提升性能和安全性，是当前收益风险比最高的改造。
