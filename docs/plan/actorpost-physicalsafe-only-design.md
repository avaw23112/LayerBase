# ActorPost PhysicalSafe Only Design

## 1. 目标

本设计用于进一步简化 LayerBase 的 ActorPost 投递路径。

核心目标：

```text
ActorPost 只负责把事件写入 EventMail。
PostTo 成功只代表邮箱接受成功。
Actor 是否真正执行该事件，由 Pump / Sweep 阶段决定。
运行期不再检查 Actor 是否 Alive、PendingDestroy、Destroying、Disabled。
运行期不再检查 ActorId.Generation。
运行期不再使用 PostableGeneration。
运行期不再存在 ValidationPhysicalSafe / ValidationPostableStamp 分支。
RouteCode 只表达邮箱写入策略。
```

最终原则：

```text
ActorPost 是邮箱接受层，不是生命周期判断层。
```

---

## 2. 核心语义

### 2.1 PostTo 成功语义

`PostTo` 成功表示：

```text
事件已经成功写入目标 Actor 当前物理邮箱。
```

`PostTo` 成功不表示：

```text
目标 Actor 一定会执行该事件。
目标 Actor 当前一定 Alive。
目标 Actor 当前没有 PendingDestroy。
目标 Actor 当前没有 Destroying。
目标 Actor 当前没有 Disabled。
ActorId.Generation 一定仍然匹配。
```

### 2.2 死亡 Actor 的消息处理

Actor 被标记死亡后：

```text
Post 阶段仍然可以写入该 Actor 的物理邮箱。
Pump 阶段可以跳过该 Actor。
Sweep 阶段统一清理该 Actor 的所有邮箱。
```

推荐流程：

```text
MarkPendingDestroy:
  标记 PendingDestroy。
  标记 StructuralDirty。
  不立即释放 slot。
  不立即释放邮箱。

PostTo:
  只要 ArchetypeId / SlotIndex 能定位到邮箱，就允许写入。

Pump:
  如果 slot 已经 PendingDestroy / Destroying，则不执行 handler。

Sweep:
  清理该 slot 所有 EventMail。
  移除生命周期。
  更新 generation。
  回收 slot。
  刷新 Query 缓存。
```

---

## 3. 删除 Validation 维度

删除以下概念：

```text
ValidationPhysicalSafe
ValidationPostableStamp
ValidationUnchecked
ValidationMask
PostableGeneration
RequirePostableStamp
RejectDisabled
```

保留的只有邮箱写入路线：

```text
QueuedGrow
QueuedRejectNew
QueuedDropOldest
Latest
Dirty
Disabled
```

---

## 4. ActorPostRouteCode

将 `ActorPostRouteCode` 改为只表达写入策略。

```csharp
namespace LayerBase.Actor;

internal static class ActorPostRouteCode
{
    public const byte QueuedGrow = 0;

    public const byte QueuedRejectNew = 1;

    public const byte QueuedDropOldest = 2;

    public const byte Latest = 3;

    public const byte Dirty = 4;

    public const byte Disabled = 5;
}
```

说明：

```text
QueuedGrow:
  普通队列，邮箱满时尝试扩容。

QueuedRejectNew:
  固定容量队列，邮箱满时拒绝新消息。

QueuedDropOldest:
  固定容量队列，邮箱满时丢弃最旧消息。

Latest:
  只保留最新消息。

Dirty:
  邮箱已有未处理消息时不重复写入。

Disabled:
  当前事件不支持 ActorPost。
```

---

## 5. ActorPostRouteMasks

`ActorPostRouteMasks` 只保留批量路径需要的路线集合。

```csharp
namespace LayerBase.Actor;

internal static class ActorPostRouteMasks
{
    public const uint QueuedRoutes =
        (1u << ActorPostRouteCode.QueuedGrow) |
        (1u << ActorPostRouteCode.QueuedRejectNew) |
        (1u << ActorPostRouteCode.QueuedDropOldest);
}
```

删除：

```text
StampRoutes
UncheckedRoutes
```

---

## 6. ActorEventPostPlan

`ActorEventPostPlan<TEvent>` 删除 `RequirePostableStamp` 和 `RejectDisabled`。

```csharp
namespace LayerBase.Actor;

internal readonly struct ActorEventPostPlan<TEvent>
    where TEvent : struct
{
    public readonly int EventId;

    public readonly byte RouteCode;

    public readonly ActorMailOptions MailOptions;

    public ActorEventPostPlan(
        int eventId,
        byte routeCode,
        ActorMailOptions mailOptions)
    {
        // eventId 参数作用：
        // 当前事件类型 ID。
        // 用于注册 EventPostState 和 EventPostRow。

        // routeCode 参数作用：
        // 当前事件的邮箱写入路线。
        // 只表达 QueuedGrow、QueuedRejectNew、QueuedDropOldest、Latest、Dirty、Disabled。

        // mailOptions 参数作用：
        // 当前事件构建期确定的邮箱配置。
        // PostCore 使用它读取容量、扩容和满队列处理配置。

        EventId = eventId;
        RouteCode = routeCode;
        MailOptions = mailOptions;
    }
}
```

---

## 7. ActorEventPostPlanBuilder

`ActorEventPostPlanBuilder` 不再计算 `RejectDisabled` 和 `RequirePostableStamp`。

```csharp
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Actor;

internal static class ActorEventPostPlanBuilder
{
    public static ActorEventPostPlan<TEvent> Build<TEvent>(
        ActorMailOptions worldDefaultMailOptions)
        where TEvent : struct
    {
        // worldDefaultMailOptions 参数作用：
        // ActorWorld 默认邮箱配置。
        // 如果 EventMetaData<TEvent> 没有提供 ActorMailOptions，则使用它作为兜底配置。

        EventMetaData<TEvent>? metaData =
            EventMetaDataHandler.ResolveRegisteredMetaData<TEvent>();

        ActorMailOptions mailOptions =
            metaData?.GetActorMailOptions() ?? worldDefaultMailOptions;

        byte routeCode =
            ResolveRouteCode(mailOptions);

        return new ActorEventPostPlan<TEvent>(
            eventId: EventTypeId<TEvent>.Id,
            routeCode: routeCode,
            mailOptions: mailOptions);
    }

    private static byte ResolveRouteCode(
        ActorMailOptions options)
    {
        // options 参数作用：
        // 当前事件最终邮箱配置。
        // 构建期根据它决定唯一的邮箱写入路线。

        return options.PostPolicy switch
        {
            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.Grow
                => ActorPostRouteCode.QueuedGrow,

            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.RejectNew
                => ActorPostRouteCode.QueuedRejectNew,

            ActorPostPolicy.Queued when options.FullPolicy == ActorMailFullPolicy.DropOldest
                => ActorPostRouteCode.QueuedDropOldest,

            ActorPostPolicy.Latest
                => ActorPostRouteCode.Latest,

            ActorPostPolicy.Dirty
                => ActorPostRouteCode.Dirty,

            _ => ActorPostRouteCode.Disabled
        };
    }
}
```

---

## 8. EventPostState

`EventPostState<TEvent>` 保持 `RouteCode / Pool / Options / RowsByArchetype`。

```csharp
namespace LayerBase.Actor;

internal sealed class EventPostState<TEvent>
    where TEvent : struct
{
    public readonly byte RouteCode;

    public readonly EventMailPool<TEvent> Pool;

    public readonly ActorMailOptions Options;

    public EventPostRow<TEvent>[] RowsByArchetype;

    public EventPostState(
        byte routeCode,
        EventMailPool<TEvent> pool,
        ActorMailOptions options,
        EventPostRow<TEvent>[] rowsByArchetype)
    {
        // routeCode 参数作用：
        // 当前事件构建期确定的邮箱写入路线。

        // pool 参数作用：
        // 当前 ActorWorld + TEvent 唯一的事件邮箱池。
        // 所有 Archetype 下的 TEvent 邮箱 buffer 都从这里租用和释放。

        // options 参数作用：
        // 当前事件构建期确定的邮箱配置。
        // PostCore 使用它读取 InitialCapacity、MaxCapacity、GrowFactor 等固定配置。

        // rowsByArchetype 参数作用：
        // 按 ArchetypeId 索引的事件邮箱定位表。
        // PostTo 使用 actorId.ArchetypeId 快速定位目标 Row。

        RouteCode = routeCode;
        Pool = pool;
        Options = options;
        RowsByArchetype = rowsByArchetype;
    }
}
```

---

## 9. EventPostRow

`EventPostRow<TEvent>` 删除 `PostableGenerations`。

```csharp
namespace LayerBase.Actor;

internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;

    public readonly DirtySlotList DirtySlots;

    public readonly int BucketIndex;

    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex)
    {
        // mails 参数作用：
        // 当前 Archetype + TEvent 的邮箱数组。
        // PostTo 通过 slotIndex 直接定位目标邮箱。

        // dirtySlots 参数作用：
        // 当前事件列的脏 slot 列表。
        // 当邮箱从空变为非空时，标记 slotIndex，等待 Pump 消费。

        // bucketIndex 参数作用：
        // 当前事件列对应的 dirty bucket 下标。
        // 写入第一条待处理消息时标记该 bucket。

        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
    }

    public bool IsValid => Mails.Length != 0;
}
```

Invalid Row：

```csharp
private static EventPostRow<TEvent> CreateInvalidRow<TEvent>()
    where TEvent : struct
{
    return new EventPostRow<TEvent>(
        Array.Empty<EventMail<TEvent>>(),
        DirtySlotList.Empty,
        -1);
}
```

---

## 10. RegisterEventPostRow

删除 `postableGenerations` 参数。

```csharp
internal void RegisterEventPostRow<TEvent>(
    int archetypeId,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    ActorEventPostPlan<TEvent> plan)
    where TEvent : struct
{
    // archetypeId 参数作用：
    // 当前 Archetype 的 ID。
    // 用于写入 state.RowsByArchetype[archetypeId]。

    // mails 参数作用：
    // 当前 Archetype + TEvent 的邮箱数组。

    // dirtySlots 参数作用：
    // 当前事件列的脏 slot 列表。

    // bucketIndex 参数作用：
    // 当前事件列对应的 dirty bucket 下标。

    // plan 参数作用：
    // 当前事件构建期生成的 Post 计划。
    // 用于获取或创建 EventPostState<TEvent>。

    EventPostState<TEvent> state =
        GetOrCreateEventPostState(plan);

    EnsureRowsCapacity(
        ref state.RowsByArchetype,
        archetypeId);

    state.RowsByArchetype[archetypeId] =
        new EventPostRow<TEvent>(
            mails,
            dirtySlots,
            bucketIndex);
}
```

`EventColumn.RefreshPostRowBinding()` 同步删除 `postableGenerations` 参数。

```csharp
public override void RefreshPostRowBinding()
{
    // RefreshPostRowBinding 方法作用：
    // 当 EventColumn 的 Mails 数组扩容后，重新注册最新的 EventPostRow。
    // PostTo 后续会通过该 Row 写入最新邮箱数组。

    _world.RegisterEventPostRow(
        archetypeId: _owner.ArchetypeId,
        mails: _mails,
        dirtySlots: _dirtySlots,
        bucketIndex: _bucketIndex,
        plan: _plan);
}
```

---

## 11. ActorWorld.PostTo

`PostTo` 只做物理邮箱写入路线选择。

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult PostTo<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        // actorId 参数作用：
        // 目标 Actor 句柄。
        // 这里只使用 ArchetypeId 和 SlotIndex 定位物理邮箱。
        // 不检查 Generation、Alive、PendingDestroy、Disabled。

        // value 参数作用：
        // 要写入目标 Actor 邮箱的事件值。

        EventPostState<TEvent>? state =
            EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);

        if (state == null)
        {
            return BuildEventNotSupportedCold<TEvent>();
        }

        byte routeCode = state.RouteCode;

        if (routeCode == ActorPostRouteCode.QueuedGrow)
        {
            return PostQueuedGrowPhysicalSafe(
                actorId,
                in value,
                state);
        }

        if (routeCode == ActorPostRouteCode.Disabled)
        {
            return BuildEventNotSupportedCold<TEvent>();
        }

        return PostToNonDefaultCold(
            actorId,
            in value,
            state,
            routeCode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostToMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value)
        where TEvent : struct
    {
        // actorIds 参数作用：
        // 要批量投递的目标 ActorId 列表。

        // value 参数作用：
        // 要投递给所有目标 Actor 的事件值。

        foreach (ActorId actorId in actorIds)
        {
            _ = PostTo(actorId, in value);
        }
    }
}
```

---

## 12. PostToNonDefaultCold

`PostToNonDefaultCold` 按写入路线直接分发。

```csharp
[MethodImpl(MethodImplOptions.NoInlining)]
private PostResult PostToNonDefaultCold<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state,
    byte routeCode)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 非默认路线仍然只用它定位物理邮箱。

    // value 参数作用：
    // 要写入邮箱的事件值。

    // state 参数作用：
    // 当前事件的编译后状态。
    // 提供 RowsByArchetype、Pool、Options。

    // routeCode 参数作用：
    // 当前事件的邮箱写入路线。

    switch (routeCode)
    {
        case ActorPostRouteCode.QueuedRejectNew:
            return PostQueuedRejectNewPhysicalSafe(
                actorId,
                in value,
                state);

        case ActorPostRouteCode.QueuedDropOldest:
            return PostQueuedDropOldestPhysicalSafe(
                actorId,
                in value,
                state);

        case ActorPostRouteCode.Latest:
            return PostLatestPhysicalSafe(
                actorId,
                in value,
                state);

        case ActorPostRouteCode.Dirty:
            return PostDirtyPhysicalSafe(
                actorId,
                in value,
                state);

        default:
            return BuildRouteUnsupportedCold<TEvent>();
    }
}
```

删除：

```text
PostQueuedGrowPostableStamp
PostQueuedRejectNewPostableStamp
PostQueuedDropOldestPostableStamp
PostLatestPostableStamp
PostDirtyPostableStamp
TryGetPostableRow
BuildPostableStampRejectedCold
```

---

## 13. TryGetPhysicalRow

保留最小物理定位检查。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool TryGetPhysicalRow<TEvent>(
    ActorId actorId,
    EventPostState<TEvent> state,
    out EventPostRow<TEvent> row,
    out int slotIndex)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 这里只使用 ArchetypeId 和 SlotIndex。

    // state 参数作用：
    // 当前事件的编译后状态。

    // row 参数作用：
    // 输出目标 Archetype + TEvent 的邮箱定位信息。

    // slotIndex 参数作用：
    // 输出目标 slot 下标。

    EventPostRow<TEvent>[] rows = state.RowsByArchetype;

    int archetypeId = actorId.ArchetypeId;
    if ((uint)archetypeId >= (uint)rows.Length)
    {
        row = default;
        slotIndex = default;
        return false;
    }

    row = rows[archetypeId];

    slotIndex = actorId.SlotIndex;
    return (uint)slotIndex < (uint)row.Mails.Length;
}
```

---

## 14. PhysicalSafe 写入方法

保留这些方法：

```text
PostQueuedGrowPhysicalSafe
PostQueuedRejectNewPhysicalSafe
PostQueuedDropOldestPhysicalSafe
PostLatestPhysicalSafe
PostDirtyPhysicalSafe
```

示例：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private PostResult PostQueuedGrowPhysicalSafe<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 只用于定位物理邮箱。

    // value 参数作用：
    // 要写入邮箱的事件值。

    // state 参数作用：
    // 当前事件的编译后状态。

    if (!TryGetPhysicalRow(
            actorId,
            state,
            out EventPostRow<TEvent> row,
            out int slotIndex))
    {
        return BuildPostFailureCold(actorId);
    }

    return PostQueuedGrowCore(
        slotIndex,
        in value,
        row.Mails,
        row.DirtySlots,
        row.BucketIndex,
        state.Pool,
        state.Options);
}
```

---

## 15. PostFast

`PostFast` 改成只支持 `QueuedGrow` 的 bool 快路径。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal bool PostFast<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 本方法只面向 QueuedGrow 路线做快速写入。

    // value 参数作用：
    // 要写入目标 Actor 邮箱的事件值。

    EventPostState<TEvent>? state =
        EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);

    if (state == null ||
        state.RouteCode != ActorPostRouteCode.QueuedGrow)
    {
        return false;
    }

    if (!TryGetPhysicalRow(
            actorId,
            state,
            out EventPostRow<TEvent> row,
            out int slotIndex))
    {
        return false;
    }

    return PostQueuedGrowCoreFastNoResult(
        slotIndex,
        in value,
        row.Mails,
        row.DirtySlots,
        row.BucketIndex,
        state.Pool,
        state.Options);
}
```

---

## 16. PostQueuedGrowCoreFastNoResult

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal bool PostQueuedGrowCoreFastNoResult<TEvent>(
    int slotIndex,
    in TEvent value,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    EventMailPool<TEvent> pool,
    ActorMailOptions options)
    where TEvent : struct
{
    // slotIndex 参数作用：
    // 目标 Actor 在当前 Archetype 内的 slot 下标。

    // value 参数作用：
    // 要写入邮箱的事件值。

    // mails 参数作用：
    // 当前 Archetype + TEvent 的邮箱数组。
    // slotIndex 用于定位具体 Actor 的邮箱。

    // dirtySlots 参数作用：
    // 当前事件列的脏 slot 列表。
    // 当邮箱从空变为非空时，需要标记 slotIndex。

    // bucketIndex 参数作用：
    // 当前事件列对应的 dirty bucket 下标。
    // 写入第一条待处理消息时，需要标记该 bucket。

    // pool 参数作用：
    // ActorWorld + TEvent 级邮箱池。
    // 用于分配、扩容和写入底层 buffer。

    // options 参数作用：
    // 构建期确定的邮箱配置。
    // 当前简单快路径只在首次分配和扩容时使用。

    ref EventMail<TEvent> mail = ref mails[slotIndex];

    EnsureMailAllocated(
        ref mail,
        pool);

    if (mail.Count >= mail.Capacity)
    {
        if (!pool.TryGrow(ref mail))
        {
            return false;
        }
    }

    pool.Write(
        mail.BufferId,
        mail.Tail,
        in value);

    mail.Tail++;
    if (mail.Tail == mail.Capacity)
    {
        mail.Tail = 0;
    }

    mail.Count++;

    if (mail.Count == 1)
    {
        dirtySlots.Mark(slotIndex);
        _dirtyEventBuckets.Mark(bucketIndex);
    }

    return true;
}
```

---

## 17. TypedActorStorage.PostAll

`PostAll` 不再传 `validation`。

```csharp
public override void PostAll<TEvent>(
    ActorWorld world,
    EventPostState<TEvent> state,
    byte routeCode,
    in TEvent value)
    where TEvent : struct
{
    // world 参数作用：
    // 当前 ActorWorld，用于调用 PostCore。

    // state 参数作用：
    // 当前事件的编译后状态。

    // routeCode 参数作用：
    // 当前事件邮箱写入路线。

    // value 参数作用：
    // 要批量写入的事件值。

    EventPostRow<TEvent>[] rows = state.RowsByArchetype;
    if ((uint)_archetypeId >= (uint)rows.Length)
    {
        return;
    }

    EventPostRow<TEvent> row = rows[_archetypeId];
    if (!row.IsValid)
    {
        return;
    }

    switch (routeCode)
    {
        case ActorPostRouteCode.QueuedGrow:
            PostAllQueuedGrow(world, row, state, in value);
            break;

        case ActorPostRouteCode.QueuedRejectNew:
            PostAllQueuedRejectNew(world, row, state, in value);
            break;

        case ActorPostRouteCode.QueuedDropOldest:
            PostAllQueuedDropOldest(world, row, state, in value);
            break;

        case ActorPostRouteCode.Latest:
            PostAllLatest(world, row, state, in value);
            break;

        case ActorPostRouteCode.Dirty:
            PostAllDirty(world, row, state, in value);
            break;
    }
}
```

Slot 判断：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool CanPostAllSlot(
    int slotIndex)
{
    // slotIndex 参数作用：
    // 当前批量扫描到的 Actor slot。
    // PostAll 只确认它当前仍然是活跃物理 Actor。

    return _states[slotIndex] == ActorSlotState.Alive
           && _actors[slotIndex] != null;
}
```

示例：

```csharp
private void PostAllQueuedGrow<TEvent>(
    ActorWorld world,
    EventPostRow<TEvent> row,
    EventPostState<TEvent> state,
    in TEvent value)
    where TEvent : struct
{
    // 参数作用：
    // world 提供 PostCore。
    // row 提供邮箱数组和 dirty 信息。
    // state 提供 Pool 和 Options。
    // value 是要批量写入的事件。

    for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
    {
        if (!CanPostAllSlot(slotIndex))
        {
            continue;
        }

        _ = world.PostQueuedGrowCore(
            slotIndex,
            in value,
            row.Mails,
            row.DirtySlots,
            row.BucketIndex,
            state.Pool,
            state.Options);
    }
}
```

其他路线同理：

```text
PostAllQueuedRejectNew -> world.PostQueuedRejectNewCore
PostAllQueuedDropOldest -> world.PostQueuedDropOldestCore
PostAllLatest -> world.PostLatestCore
PostAllDirty -> world.PostDirtyCore
```

---

## 18. 可删除字段

如果不再需要 PostableGeneration，删除：

```text
_alivePostGenerations
_enabledPostGenerations
AlivePostGenerations
EnabledPostGenerations
RefreshPostGenerations
ResolvePostableGenerations
```

同时删除相关调用：

```text
AllocateSlot 中的 RefreshPostGenerations(slotIndex)
SetEnable 中的 RefreshPostGenerations(slotIndex)
MarkPendingDestroy 中的 RefreshPostGenerations(slotIndex)
SweepPendingDestroy 中的 RefreshPostGenerations(slotIndex)
FinalizeDestroySlot 中的 RefreshPostGenerations(slotIndex)
```

如果需要降低一次性改动风险，可以先保留字段和刷新调用，但不要再让 ActorPost 使用它们。等 benchmark 稳定后再删。

---

## 19. 必须删除的旧符号

全项目不应再出现：

```text
ValidationPhysicalSafe
ValidationPostableStamp
ValidationUnchecked
ValidationMask
WriteModeMask
WriteQueuedGrow
WriteQueuedRejectNew
WriteQueuedDropOldest
WriteLatest
WriteDirty
WriteDisabled
RequirePostableStamp
RejectDisabled
PostableGenerations
TryGetPostableRow
PostableStamp
BuildPostableStampRejectedCold
```

---

## 20. Benchmark 目标

重点观察：

```text
ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent
ActorPost_ArchetypeRow_PostFast_OneActor_OneEvent
ActorPost_ArchetypeRow_1000Actors_OneEvent
ActorPost_Query_PostAll_1000Actors_12Events
ActorWorld Post only - 200k
ActorWorld Post + Pump - 20万次
```

预期：

```text
PostTo:
  少掉 PostableStamp / Validation 分支后应略微下降。

PostFast:
  如果改成 NoResult + QueuedGrow only，应重新快于 PostTo。

PostAll:
  少掉 validation 参数和 PostableGeneration 检查后应下降。

Pump:
  变化不大，执行与否仍然在 Pump/Sweep 处理。
```

---

## 21. 最终结论

最终 ActorPost 职责收敛为：

```text
定位物理邮箱。
写入 EventMail。
标记 DirtySlot。
标记 DirtyBucket。
```

不再负责：

```text
判断 Actor 是否逻辑可投递。
判断 Actor 是否 Disabled。
判断 Actor 是否 PendingDestroy。
判断 ActorId.Generation 是否匹配。
```

一句话：

```text
ActorPost 是邮箱接受层，不是生命周期判断层。
```
