# ActorPost Aggressive API Cleanup Design

## 1. 目标

本设计用于清理 LayerBase 中 ActorPost 旧路径 API，并让所有公开入口、内部入口、批量投递入口、延迟投递入口都适配“构建期策略编译路线”。

核心目标：

```text
不保留运行期策略参数。
不保留旧 safe fallback 路径。
不保留 Row 持有 Pool 的旧结构。
不保留只支持 QueuedGrow 的旧 PostFast。
不允许 Post 期读取 EventMetaData。
不允许 Post 期读取 EventRuntimePolicyTable。
不允许 Post 期根据调用点动态选择策略。
```

最终结构：

```text
策略只在构建期确定。
事件类型表达固定投递语义。
ActorPostRouteKind 是唯一运行期路线来源。
ActorWorld.PostTo<TEvent> 是统一入口。
所有上层 API 只能调用已编译 ActorPost 路线。
```

---

## 2. 清理范围

需要清理的模块：

```text
ActorWorld.Post
Actor.PostInside
ActorContext Post API
ActorQueryResult.PostAll
ActorQueryPostExtensions
PostScheduler
TypedActorStorage.Post
BehaviourArchetype.Post
EventColumn.Post
EventMailWriter.Enqueue
EventPostRuntime 旧 Rows API
EventPostRow 旧 Pool 字段
```

保留的模块：

```text
PostResult
ActorPostStatus
PostFailureKind
EventColumn Pump
DirtySlotList
DirtyBucketList
EventMailPool<TEvent>
EventMail<TEvent>
ActorMailOptions
EventMetaData<TEvent>
```

保留原因：

```text
PostResult 用于诊断，不是主要性能瓶颈。
Pump 仍然可以复用现有 EventColumn 消费逻辑。
EventMailPool 和 EventMail 是邮箱底层结构，不需要删除。
ActorMailOptions 仍然是构建期策略配置，只是不再允许运行期覆盖。
```

---

## 3. 强制规则

### 3.1 Post API 不允许运行期策略参数

所有 Post API 删除以下参数：

```text
ActorPostPolicy? postPolicy
ActorMailFullPolicy? fullPolicy
EventPostPolicy? postPolicy
EventBufferPolicy? bufferPolicy
```

禁止：

```csharp
world.PostTo(actorId, in value, ActorPostPolicy.Latest);
world.PostTo(actorId, in value, ActorMailFullPolicy.DropOldest);
actor.PostInside(in value, ActorPostPolicy.Queued);
query.PostAll(in value, fullPolicy: ActorMailFullPolicy.RejectNew);
```

允许：

```csharp
world.PostTo(actorId, in value);
actor.PostInside(in value);
query.PostAll(in value);
```

---

### 3.2 不保留旧 fallback

删除：

```text
TryPostToSafe
PostFast 失败后回 BehaviourArchetype.Post
BehaviourArchetype.Post 作为默认投递路径
TypedActorStorage.Post 作为默认投递路径
EventColumn.Post 作为默认投递路径
EventMailWriter.Enqueue 作为默认投递路径
```

设计要求：

```text
如果 EventPostState<TEvent> 不存在，PostTo 返回 EventNotSupported。
如果 Row 不存在，PostTo 返回 EventNotSupported。
如果 ActorId 无效，PostTo 返回 InvalidActorId。
不允许通过旧路径补救。
```

---

### 3.3 EventPostRow 不允许保存 Pool

旧结构禁止：

```csharp
internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;
    public readonly DirtySlotList DirtySlots;
    public readonly int BucketIndex;
    public readonly int[] Generations;
    public readonly EventMailPool<TEvent> Pool;
}
```

新结构必须是：

```csharp
internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;
    public readonly DirtySlotList DirtySlots;
    public readonly int BucketIndex;
    public readonly int[] Generations;
    public readonly ActorSlotFlags[] SlotFlags;

    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[] generations,
        ActorSlotFlags[] slotFlags)
    {
        // mails 参数作用：
        // 当前 Archetype + TEvent 的邮箱数组。
        // PostTo 通过 actorId.SlotIndex 直接定位目标 Actor 的邮箱。

        // dirtySlots 参数作用：
        // 当前事件列的脏 slot 列表。
        // 当邮箱从空变为非空时，写入 slotIndex，供 Pump 阶段消费。

        // bucketIndex 参数作用：
        // 当前事件列在 ActorWorld dirty bucket 中的索引。
        // PostCore 写入新消息后，需要通过它标记事件 bucket 为 dirty。

        // generations 参数作用：
        // 当前 Archetype 的 slot 代际数组。
        // 用于判断 ActorId 是否已经过期。

        // slotFlags 参数作用：
        // 当前 Archetype 的 slot 状态数组。
        // 用于判断 Alive、Enabled、PendingDestroy、Destroying 等状态。

        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        Generations = generations;
        SlotFlags = slotFlags;
    }

    public bool IsValid => Mails != null;
}
```

Pool 只允许从：

```text
EventPostState<TEvent>.Pool
```

取得。

---

## 4. ActorWorld.Post API

### 4.1 删除旧签名

删除：

```csharp
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct;
```

替换为：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 的句柄。
    // PostTo 使用 ArchetypeId 定位 Row，使用 SlotIndex 定位邮箱，使用 Generation 校验生命周期。

    // value 参数作用：
    // 要投递给目标 Actor 的事件值。
    // 事件值会被写入该 Actor 对应的 TEvent 邮箱。

    EventPostState<TEvent>? state =
        EventPostRuntime<TEvent>.GetState(this);

    if (state == null)
    {
        return PostResult.Failure(
            ActorPostStatus.EventNotSupported,
            "Event post state is not built.",
            PostFailureKind.UnsupportedEvent);
    }

    return state.Route switch
    {
        ActorPostRouteKind.QueuedGrow =>
            PostQueuedGrow(actorId, in value, state),

        ActorPostRouteKind.QueuedRejectNew =>
            PostQueuedRejectNew(actorId, in value, state),

        ActorPostRouteKind.QueuedDropOldest =>
            PostQueuedDropOldest(actorId, in value, state),

        ActorPostRouteKind.Latest =>
            PostLatest(actorId, in value, state),

        ActorPostRouteKind.Dirty =>
            PostDirty(actorId, in value, state),

        ActorPostRouteKind.Disabled =>
            PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                "ActorPost is disabled for this event.",
                PostFailureKind.UnsupportedEvent),

        _ =>
            PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                "Unsupported actor post route.",
                PostFailureKind.UnsupportedEvent)
    };
}
```

---

### 4.2 删除旧 PostFast

删除：

```csharp
internal bool PostFast<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct;
```

原因：

```text
旧 PostFast 只支持 QueuedGrow。
旧 PostFast 返回 bool，失败后依赖 TryPostToSafe fallback。
旧 PostFast 从 EventPostRuntime<TEvent>.TryGetRows 只取 Rows，不取 Route / Pool / Options。
旧 PostFast 使用 row.Pool，违反 Row 不持有 Pool 的新规则。
```

替代：

```text
PostTo<TEvent>
  -> EventPostRuntime<TEvent>.GetState
  -> ActorPostRouteKind switch
  -> 专用 PostCore
```

---

### 4.3 删除 TryPostToSafe

删除：

```csharp
private PostResult TryPostToSafe<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
    where TEvent : struct;
```

原因：

```text
TryPostToSafe 会回到 BehaviourArchetype.Post。
BehaviourArchetype.Post 会回到 TypedActorStorage.Post。
TypedActorStorage.Post 会回到 EventColumn.Post。
EventColumn.Post 会回到运行期策略判断。
这条链路必须彻底删除出默认 Post 路径。
```

---

### 4.4 PostToMany 新签名

删除：

```csharp
public void PostToMany<TEvent>(
    ReadOnlySpan<ActorId> actorIds,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct;
```

替换为：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void PostToMany<TEvent>(
    ReadOnlySpan<ActorId> actorIds,
    in TEvent value)
    where TEvent : struct
{
    // actorIds 参数作用：
    // 目标 ActorId 列表。
    // 方法会逐个 ActorId 执行同一个已编译事件路线。

    // value 参数作用：
    // 要投递给多个 Actor 的事件值。
    // 所有目标 Actor 收到同一个 TEvent 值。

    foreach (ActorId actorId in actorIds)
    {
        _ = PostTo(actorId, in value);
    }
}
```

后续优化：

```text
PostToMany 可以按 ArchetypeId 分组。
同一个 Archetype 内批量写入 Row，避免每个 Actor 重复 route switch。
第一阶段可以先复用 PostTo。
```

---

## 5. Actor 内部 Post API

### 5.1 Actor.PostInside 清理

删除旧签名：

```csharp
protected PostResult PostInside<TEvent>(
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct;
```

替换为：

```csharp
protected PostResult PostInside<TEvent>(
    in TEvent value)
    where TEvent : struct
{
    // value 参数作用：
    // 当前 Actor 要投递给自身邮箱的事件值。
    // 该事件必须已经在构建期生成 EventPostState<TEvent>。

    return Context.World.PostTo(
        Context.ActorId,
        in value);
}
```

说明：

```text
Actor 内部不允许临时覆盖策略。
Actor 自投递和外部投递走同一条 ActorWorld.PostTo 路线。
```

---

### 5.2 ActorContext Post API 清理

删除旧签名：

```csharp
public PostResult Post<TEvent>(
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct;
```

替换为：

```csharp
public PostResult Post<TEvent>(
    in TEvent value)
    where TEvent : struct
{
    // value 参数作用：
    // 当前 ActorContext 所属 Actor 要接收的事件值。
    // 事件策略已经在构建期编译完成。

    return World.PostTo(
        ActorId,
        in value);
}
```

---

## 6. ActorQuery API

### 6.1 PostAll 删除策略参数

删除：

```csharp
public static void PostAll<TEvent>(
    this ActorQueryResult query,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct;
```

替换为：

```csharp
public static void PostAll<TEvent>(
    this ActorQueryResult query,
    in TEvent value)
    where TEvent : struct
{
    // query 参数作用：
    // Actor 查询结果。
    // 包含命中的 Archetype 缓存和所属 ActorWorld。

    // value 参数作用：
    // 要批量投递给查询结果中所有 Actor 的事件值。

    EventPostState<TEvent>? state =
        EventPostRuntime<TEvent>.GetState(query.World);

    if (state == null)
    {
        return;
    }

    switch (state.Route)
    {
        case ActorPostRouteKind.QueuedGrow:
            PostAllQueuedGrow(query, in value, state);
            break;

        case ActorPostRouteKind.QueuedRejectNew:
            PostAllQueuedRejectNew(query, in value, state);
            break;

        case ActorPostRouteKind.QueuedDropOldest:
            PostAllQueuedDropOldest(query, in value, state);
            break;

        case ActorPostRouteKind.Latest:
            PostAllLatest(query, in value, state);
            break;

        case ActorPostRouteKind.Dirty:
            PostAllDirty(query, in value, state);
            break;
    }
}
```

---

### 6.2 PostAll 多事件重载清理

所有 2 到 12 个事件参数的 `PostAll` 重载删除：

```text
ActorPostPolicy? postPolicy
ActorMailFullPolicy? fullPolicy
```

旧签名：

```csharp
public static void PostAll<TEvent1, TEvent2>(
    this ActorQueryResult query,
    in TEvent1 value1,
    in TEvent2 value2,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent1 : struct
    where TEvent2 : struct;
```

新签名：

```csharp
public static void PostAll<TEvent1, TEvent2>(
    this ActorQueryResult query,
    in TEvent1 value1,
    in TEvent2 value2)
    where TEvent1 : struct
    where TEvent2 : struct
{
    // query 参数作用：
    // Actor 查询结果。
    // 用于确定要批量投递的 Actor 集合。

    // value1 参数作用：
    // 第一个要投递的事件值。
    // 该事件使用 TEvent1 构建期编译出的固定路线。

    // value2 参数作用：
    // 第二个要投递的事件值。
    // 该事件使用 TEvent2 构建期编译出的固定路线。

    query.PostAll(in value1);
    query.PostAll(in value2);
}
```

优化建议：

```text
第一阶段可以复用单事件 PostAll。
第二阶段对 2 到 12 事件重载做融合循环，避免多次遍历 Query。
```

---

### 6.3 BehaviourArchetype.PostToAliveActors 清理

删除旧签名：

```csharp
public void PostToAliveActors<TEvent>(
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
    where TEvent : struct;
```

替换方向：

```text
BehaviourArchetype 不再负责 PostAll 默认路径。
PostAll 应该通过 EventPostState<TEvent>.RowsByArchetype 直接定位 Row。
BehaviourArchetype 只提供 Query 命中的 ArchetypeId 或缓存引用。
```

如果短期保留内部方法，新签名也必须删除策略参数：

```csharp
public void PostToAliveActors<TEvent>(
    in TEvent value)
    where TEvent : struct;
```

---

## 7. PostScheduler 清理

### 7.1 删除运行期策略参数

删除：

```csharp
public void Post<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct;
```

替换为：

```csharp
public void Post<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
{
    // actorId 参数作用：
    // 延迟投递或调度投递的目标 Actor。

    // value 参数作用：
    // 到期后要投递的事件值。
    // 事件策略必须已经在构建期编译完成。

    _world.PostTo(
        actorId,
        in value);
}
```

---

### 7.2 延迟任务只保存事件，不保存策略

旧任务数据禁止保存：

```text
ActorPostPolicy?
ActorMailFullPolicy?
```

新任务数据只保存：

```text
ActorId
TEvent value
Delay / DueTime
```

原因：

```text
延迟发生的是时间，不是策略。
事件策略仍然由 TEvent 的构建期 route 决定。
```

---

## 8. BehaviourArchetype 清理

### 8.1 删除默认 Post 路径

删除或降级为测试内部工具：

```csharp
public PostResult Post<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
    where TEvent : struct;
```

原因：

```text
BehaviourArchetype.Post 是旧路径中间层。
新 ActorPost 不应该从 ActorWorld 进入 BehaviourArchetype.Post。
```

如果保留诊断方法，必须改名并避免默认路径调用：

```csharp
internal PostResult DiagnosePostFailure<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct;
```

说明：

```text
诊断方法只能用于 Debug 工具或测试。
不能被 ActorWorld.PostTo 默认调用。
不能接受运行期策略参数。
```

---

### 8.2 保留非 Post 职责

BehaviourArchetype 继续保留：

```text
GetOrCreateStorage
IsAlive
IsEnable
MarkPendingDestroy
SweepPendingDestroy
DebugInfo
Query 匹配
Actor 枚举
```

---

## 9. TypedActorStorage 清理

### 9.1 删除默认 Post 方法

删除或降级为诊断内部方法：

```csharp
public override PostResult Post<TEvent>(
    int slotIndex,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
    where TEvent : struct;
```

原因：

```text
TypedActorStorage.Post 会 TryGetColumn。
TryGetColumn 后会调用 EventColumn.Post。
EventColumn.Post 是旧运行期策略分派入口。
```

---

### 9.2 保留 Column 构建职责

TypedActorStorage 继续负责：

```text
BuildColumns
EnsureColumnCapacity
RegisterLifecycleInterfaces
Slot 分配
Slot 状态维护
Generations 维护
SlotFlags 维护
```

新增要求：

```csharp
internal ActorSlotFlags[] SlotFlags => _slotFlags;
```

说明：

```text
EventPostRow 需要 SlotFlags 执行快速状态校验。
```

---

### 9.3 Column 构建时必须注册 Row

`BuildColumns` 或 `BuildColumnFromEntry` 创建 `EventColumn<TActor,TEvent>` 后，必须完成：

```text
构建 ActorEventPostPlan<TEvent>
获取或创建 EventPostState<TEvent>
注册 EventPostRow<TEvent>
```

这样运行期 `PostTo<TEvent>` 才能完全绕过 Storage。

---

## 10. EventColumn 清理

### 10.1 删除 EventColumn.Post 默认入口

删除或降级为私有测试工具：

```csharp
public PostResult Post(
    int slotIndex,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy);
```

原因：

```text
EventColumn.Post 包含运行期策略参数。
EventColumn.Post 会在 QueuedGrow 和 PostGeneral 之间动态选择。
PostGeneral 会进入 EventMailWriter.Enqueue 的策略分派。
```

---

### 10.2 保留 Pump 和邮箱存储

EventColumn 继续保留：

```text
Mails
DirtySlots
BucketIndex
PumpOne
PumpOneFast
DispatchNow
EnsureSlotCapacity
RefreshPostRowBinding
ClearMail
GetPendingCount
GetTotalPendingCount
HasPendingWork
```

说明：

```text
EventColumn 仍然是 Pump 侧消费邮箱的载体。
只是写入侧不再通过 EventColumn.Post。
```

---

### 10.3 RefreshPostRowBinding 新职责

```csharp
public override void RefreshPostRowBinding()
{
    // RefreshPostRowBinding 方法作用：
    // 当 EventColumn 的 Mails 数组扩容后，刷新 EventPostRow 中的 Mails 引用。
    // 这样 ActorWorld.PostTo 可以继续通过 Row 快速写入最新邮箱数组。

    ActorEventPostPlan<TEvent> plan =
        ActorEventPostPlanBuilder.Build<TEvent>(
            _world.DefaultMailOptions);

    _world.RegisterEventPostRow(
        archetypeId: _owner.ArchetypeId,
        mails: _mails,
        dirtySlots: _dirtySlots,
        bucketIndex: _bucketIndex,
        generations: _owner.Generations,
        slotFlags: _owner.SlotFlags,
        plan: plan);
}
```

---

## 11. EventMailWriter 清理

### 11.1 删除 Enqueue 作为默认路径

删除默认热路径对以下方法的依赖：

```csharp
EventMailWriter.Enqueue<TEvent>(
    ref EventMail<TEvent> mail,
    in TEvent value,
    EventMailPool<TEvent> bufferPool,
    DirtySlotList dirtySlots,
    int slotIndex,
    ActorMailOptions options,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy);
```

原因：

```text
EventMailWriter.Enqueue 内部根据 ActorPostPolicy switch。
这是运行期策略分派。
新方案中 route 已经在构建期确定，不允许再进入该 switch。
```

---

### 11.2 拆成专用 Writer

新增或保留专用方法：

```text
EventMailWriter.EnqueueQueuedGrow
EventMailWriter.EnqueueQueuedRejectNew
EventMailWriter.EnqueueQueuedDropOldest
EventMailWriter.EnqueueLatest
EventMailWriter.EnqueueDirty
```

示例：

```csharp
internal static PostResult EnqueueQueuedRejectNew<TEvent>(
    ref EventMail<TEvent> mail,
    in TEvent value,
    EventMailPool<TEvent> pool,
    DirtySlotList dirtySlots,
    int slotIndex,
    ActorMailOptions options)
    where TEvent : struct
{
    // mail 参数作用：
    // 目标 Actor 的 TEvent 邮箱引用。
    // 方法会根据当前 Count、Capacity 写入或拒绝新消息。

    // value 参数作用：
    // 要写入邮箱的事件值。

    // pool 参数作用：
    // 当前 ActorWorld + TEvent 的全局邮箱池。
    // 用于租用、读取、写入底层 buffer。

    // dirtySlots 参数作用：
    // 当前事件列的 dirty slot 列表。
    // 邮箱从空变非空时需要标记 slotIndex。

    // slotIndex 参数作用：
    // 当前 Actor 在 Archetype 内的 slot 下标。
    // 用于 dirty slot 标记。

    // options 参数作用：
    // 当前事件构建期确定的邮箱配置。
    // 本路径只读取 InitialCapacity 和 MaxCapacity，不允许读取运行期覆盖策略。

    if (mail.Count == 0 && mail.BufferId == 0)
    {
        mail.BufferId = pool.Rent(options.InitialCapacity);
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 0;
        mail.Capacity = pool.GetCapacity(mail.BufferId);
    }

    if (mail.Count >= mail.Capacity)
    {
        return PostResult.Failure(
            ActorPostStatus.MailFullRejected,
            "Actor mail is full.",
            PostFailureKind.MailboxFull);
    }

    pool.Write(mail.BufferId, mail.Tail, in value);
    mail.Tail++;

    if (mail.Tail == mail.Capacity)
    {
        mail.Tail = 0;
    }

    mail.Count++;

    if (mail.Count == 1)
    {
        dirtySlots.Mark(slotIndex);
    }

    return PostResult.Success;
}
```

---

## 12. EventPostRuntime 清理

### 12.1 删除 TryGetRows

删除：

```csharp
EventPostRuntime<TEvent>.TryGetRows(
    ActorWorld world,
    out EventPostRow<TEvent>[]? rows);
```

原因：

```text
Rows 不足以表达新运行期状态。
运行期还需要 Route、Pool、Options、RejectMask、RejectDisabled。
```

替换为：

```csharp
EventPostRuntime<TEvent>.GetState(ActorWorld world);
```

---

### 12.2 新 EventPostRuntime

```csharp
internal static class EventPostRuntime<TEvent>
    where TEvent : struct
{
    private static EventPostState<TEvent>?[] s_statesByWorld =
        new EventPostState<TEvent>?[4];

    public static EventPostState<TEvent>? GetState(
        ActorWorld world)
    {
        // world 参数作用：
        // 当前 ActorWorld。
        // 使用 world.RuntimeIndex 读取该 world 下 TEvent 的编译状态。

        int worldIndex = world.RuntimeIndex;

        if ((uint)worldIndex >= (uint)s_statesByWorld.Length)
        {
            return null;
        }

        return s_statesByWorld[worldIndex];
    }

    public static void BindWorld(
        ActorWorld world,
        EventPostState<TEvent> state)
    {
        // world 参数作用：
        // 当前 ActorWorld。
        // 使用 RuntimeIndex 决定写入哪个 world 槽位。

        // state 参数作用：
        // 当前 ActorWorld + TEvent 的编译后 Post 状态。
        // 包含 Route、Pool、Options 和 RowsByArchetype。

        int worldIndex = world.RuntimeIndex;

        if ((uint)worldIndex >= (uint)s_statesByWorld.Length)
        {
            Resize(worldIndex);
        }

        s_statesByWorld[worldIndex] = state;
    }

    public static void UnbindWorld(
        int worldIndex)
    {
        // worldIndex 参数作用：
        // 要解绑的 ActorWorld 运行期索引。
        // ActorWorld Dispose 时调用，防止静态数组持有旧状态。

        if ((uint)worldIndex < (uint)s_statesByWorld.Length)
        {
            s_statesByWorld[worldIndex] = null;
        }
    }

    private static void Resize(int worldIndex)
    {
        // worldIndex 参数作用：
        // 当前需要容纳的最大 world 索引。

        int newSize = s_statesByWorld.Length;

        while (newSize <= worldIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref s_statesByWorld, newSize);
    }
}
```

---

## 13. Delay / Timer 相关 API

### 13.1 延迟投递不允许保存策略

删除延迟任务中的：

```text
ActorPostPolicy?
ActorMailFullPolicy?
EventPostPolicy?
```

新延迟任务只保存：

```text
ActorId
TEvent
DueTick / DueTime
```

### 13.2 到期执行

```csharp
private void ExecuteDelayedPost<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
{
    // actorId 参数作用：
    // 延迟任务到期后要接收事件的 Actor。

    // value 参数作用：
    // 到期后投递的事件值。
    // 投递策略由 TEvent 对应的构建期 route 决定。

    _ = _world.PostTo(
        actorId,
        in value);
}
```

---

## 14. Source Generator 同步要求

如果源生成器生成 Actor 相关辅助代码，需要同步删除：

```text
ActorPostPolicy? 参数生成
ActorMailFullPolicy? 参数生成
PostInside 带策略参数的生成
PostAll 带策略参数的生成
旧 EventColumn.Post 调用
旧 TypedActorStorage.Post 调用
```

生成器应生成：

```text
EventMetaData<TEvent> 注册
ActorEventPostPlan<TEvent> 构建入口
EventColumn 创建后 Row 注册
Actor 支持事件列表
```

---

## 15. 编译错误驱动清理清单

第一轮删除运行期策略参数后，应该主动让编译器暴露旧调用点。

优先删除或修改以下调用：

```text
.PostTo(..., postPolicy, fullPolicy)
.PostTo(..., ActorPostPolicy.*)
.PostTo(..., ActorMailFullPolicy.*)
.PostInside(..., postPolicy, fullPolicy)
.PostAll(..., postPolicy, fullPolicy)
.PostManyToAliveActors(..., postPolicy, fullPolicy)
.EventMailWriter.Enqueue(..., postPolicy, fullPolicy)
.EventColumn.Post(..., postPolicy, fullPolicy)
.TypedActorStorage.Post(..., postPolicy, fullPolicy)
.BehaviourArchetype.Post(..., postPolicy, fullPolicy)
```

---

## 16. 删除项总表

### 16.1 ActorWorld

删除：

```text
PostTo<TEvent>(ActorId, in TEvent, ActorPostPolicy?, ActorMailFullPolicy?)
PostToMany<TEvent>(ReadOnlySpan<ActorId>, in TEvent, ActorPostPolicy?, ActorMailFullPolicy?)
PostFast<TEvent>
TryPostToSafe<TEvent>
EventPostRuntime<TEvent>.TryGetRows 相关调用
```

新增或保留：

```text
PostTo<TEvent>(ActorId, in TEvent)
PostToMany<TEvent>(ReadOnlySpan<ActorId>, in TEvent)
PostQueuedGrow
PostQueuedRejectNew
PostQueuedDropOldest
PostLatest
PostDirty
TryGetValidRow
```

---

### 16.2 Actor / ActorContext

删除：

```text
PostInside<TEvent>(in TEvent, ActorPostPolicy?, ActorMailFullPolicy?)
ActorContext.Post<TEvent>(in TEvent, ActorPostPolicy?, ActorMailFullPolicy?)
```

新增或保留：

```text
PostInside<TEvent>(in TEvent)
ActorContext.Post<TEvent>(in TEvent)
```

---

### 16.3 Query

删除：

```text
PostAll 所有运行期策略参数
PostManyToAliveActors 所有运行期策略参数
CanUsePostAllFastPath 中对 postPolicy/fullPolicy 的判断
```

新增或保留：

```text
PostAll<TEvent>(in TEvent)
PostAll<TEvent1...TEvent12>(in ...)
PostAllQueuedGrow
PostAllQueuedRejectNew
PostAllQueuedDropOldest
PostAllLatest
PostAllDirty
```

---

### 16.4 Storage / Archetype

删除：

```text
BehaviourArchetype.Post 默认路径
TypedActorStorage.Post 默认路径
```

保留：

```text
Storage 构建
Slot 管理
Lifecycle 注册
Query 支持
Pump 支持
Debug 支持
```

---

### 16.5 EventColumn

删除：

```text
EventColumn.Post 作为默认写入入口
PostGeneral
CanUseDefaultPostFastPath 中旧策略参数逻辑
```

保留：

```text
Mails
DirtySlots
BucketIndex
PumpOne
PumpOneFast
EnsureSlotCapacity
RefreshPostRowBinding
ClearMail
PendingCount
```

---

### 16.6 EventMailWriter

删除默认路径：

```text
Enqueue(..., ActorPostPolicy?, ActorMailFullPolicy?)
HandleFull 中运行期 fullPolicy 参数
```

拆分为：

```text
EnqueueQueuedGrow
EnqueueQueuedRejectNew
EnqueueQueuedDropOldest
EnqueueLatest
EnqueueDirty
```

---

## 17. 验收标准

### 17.1 API 验收

项目中不得出现公开 API：

```text
ActorPostPolicy? postPolicy
ActorMailFullPolicy? fullPolicy
```

项目中不得出现默认 Post 路径调用：

```text
TryPostToSafe
PostFast fallback
BehaviourArchetype.Post
TypedActorStorage.Post
EventColumn.Post
EventMailWriter.Enqueue with policy params
```

---

### 17.2 热路径验收

`ActorWorld.PostTo<TEvent>` 中只允许出现：

```text
EventPostRuntime<TEvent>.GetState
EventPostState<TEvent>
ActorPostRouteKind switch
TryGetValidRow
专用 PostCore
PostResult
```

不允许出现：

```text
EventMetaData<TEvent>
EventRuntimePolicyTable
ActorPostPolicy?
ActorMailFullPolicy?
BehaviourArchetype.Post
TypedActorStorage.Post
EventColumn.Post
EventMailWriter.Enqueue
```

---

### 17.3 Row / Pool 验收

必须满足：

```text
EventPostRow<TEvent> 不持有 Pool。
EventPostState<TEvent> 持有 Pool。
同一个 ActorWorld + TEvent 只有一个 EventMailPool<TEvent>。
EventColumn 不创建 Pool。
```

---

### 17.4 构建期验收

必须满足：

```text
所有事件策略在 ActorWorld Running 前完成编译。
ActorWorld Running 后不允许修改策略。
EventMetaData<TEvent> 只在构建期读取。
ActorEventPostPlan<TEvent> 只在构建期创建。
```

---

## 18. 推荐迁移顺序

### Step 1：删除 API 参数

先删除所有公开 API 和内部 API 的：

```text
ActorPostPolicy?
ActorMailFullPolicy?
```

让编译器暴露全部旧调用点。

---

### Step 2：替换 ActorWorld.PostTo

实现新的：

```text
EventPostRuntime<TEvent>.GetState
ActorPostRouteKind switch
TryGetValidRow
专用 PostCore
```

删除：

```text
PostFast
TryPostToSafe
```

---

### Step 3：清理 Row / Pool

完成：

```text
EventPostRow 删除 Pool。
EventPostState 持有 Pool。
RegisterEventPostRow 不再接收 Pool。
EventColumn.RefreshPostRowBinding 注册新 Row。
```

---

### Step 4：清理 Column / Writer

完成：

```text
EventColumn.Post 不再作为默认路径。
EventMailWriter.Enqueue 拆成专用方法。
ActorWorld 专用 PostCore 调用专用 Writer。
```

---

### Step 5：清理 Query / Scheduler

完成：

```text
PostAll 删除策略参数。
PostAll 改成 route 批量路径。
PostScheduler 不保存策略参数。
Delay 任务只保存事件和目标。
```

---

### Step 6：删除旧模块

删除无法再被调用的旧模块：

```text
BehaviourArchetype.Post
TypedActorStorage.Post
EventColumn.Post
EventMailWriter.Enqueue
CanUsePostAllFastPath 旧策略判断
```

---

## 19. 最终结论

激进清理后的 ActorPost API 应满足：

```text
所有事件策略只在构建期确定。
所有运行期 API 都不允许传策略。
所有 Post 入口都走 EventPostState<TEvent>。
所有 Row 都不持有 Pool。
所有 Pool 都归 ActorWorld + TEvent 管理。
所有旧 safe fallback 都删除。
所有旧动态策略分派都删除。
```

一句话：

```text
Post API 只负责投递事件，不负责决定事件策略。
事件策略属于构建期，事件投递属于运行期。
```
