# ActorPost Build-Time Policy Compiled Route Design

## 1. 目标

本设计用于重构 LayerBase 的 ActorPost 路径。

核心目标是：

```text
事件策略只允许在构建期确定。
运行期不允许动态修改事件策略。
PostTo 不允许传入运行期策略参数。
Post 热路径不读取 EventMetaData。
Post 热路径不读取 EventRuntimePolicyTable。
Post 热路径只执行已经编译好的 ActorPostRouteKind。
```

最终效果：

```text
EventMetaData<TEvent>
  作为事件策略的唯一声明来源。

构建期
  读取 EventMetaData<TEvent>。
  合并 ActorWorld 默认配置。
  编译 ActorPostRouteKind。
  创建 ActorWorld + TEvent 级 EventPostState<TEvent>。
  创建 ActorWorld + TEvent 级 EventMailPool<TEvent>。
  注册 Archetype + TEvent 级 EventPostRow<TEvent>。

运行期
  PostTo<TEvent> 只读取 EventPostState<TEvent>。
  PostTo<TEvent> 只按已编译 Route 分派。
  PostTo<TEvent> 不允许改变投递策略。
```

---

## 2. 核心原则

### 2.1 一个事件类型只对应一种稳定投递语义

禁止同一个事件在不同调用点使用不同策略。

禁止：

```csharp
world.PostTo(actorId, in value, ActorPostPolicy.Latest);
world.PostTo(actorId, in value, ActorPostPolicy.Queued);
world.PostTo(actorId, in value, fullPolicy: ActorMailFullPolicy.DropOldest);
```

允许：

```csharp
world.PostTo(actorId, in applyDamageEvent);
world.PostTo(actorId, in refreshDamagePreviewEvent);
world.PostTo(actorId, in markHealthDirtyEvent);
```

设计要求：

```text
如果业务需要不同策略，就定义不同事件类型。
事件类型本身应该表达用途。
EventMetaData<TEvent> 负责声明该事件的固定策略。
```

---

### 2.2 策略只能在构建期读取

允许在构建期读取：

```text
EventMetaData<TEvent>.PostPolicy
EventMetaData<TEvent>.BufferPolicy
EventMetaData<TEvent>.ActorMailOptions
ActorWorld.DefaultMailOptions
```

禁止在运行期读取：

```text
EventMetaData<TEvent>
EventRuntimePolicyTable
EventPostPolicy
EventBufferPolicy
ActorMailOptions
```

这里的“运行期”指 ActorWorld 已经进入 Running 状态，Actor 已经开始 Post、Pump、Update 的阶段。

---

### 2.3 PostResult 保留

本设计不删除 `PostResult`。

原因：

```text
PostResult 不是当前 Post 热路径最大性能问题。
真正的问题是运行期策略参数、通用策略分派、层级转发和动态 fallback。
```

保留：

```csharp
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct;
```

删除：

```csharp
ActorPostPolicy? postPolicy
ActorMailFullPolicy? fullPolicy
```

---

### 2.4 不引入 CoalescedQueued

本设计不引入 `CoalescedQueued` 路径。

原因：

```text
Coalesced 需要 key、索引、merge 检查和额外维护成本。
普通 Queued 路径不应承担 Coalesced 的结构成本。
当前阶段优先保证 Queued / RejectNew / DropOldest / Latest / Dirty 的稳定高性能。
```

如果未来需要 Coalesced，应作为独立路线单独设计，不进入本阶段。

---

## 3. 新运行模型

### 3.1 构建期模型

```text
EventMetaData<TEvent>
  -> ActorEventPostPlanBuilder
    -> ActorEventPostPlan<TEvent>
      -> EventPostState<TEvent>
        -> EventPostRow<TEvent>[]
```

构建期负责：

```text
读取事件元数据。
推导最终 ActorMailOptions。
编译 ActorPostRouteKind。
创建全局 EventMailPool<TEvent>。
绑定每个 Archetype 的 EventPostRow<TEvent>。
```

---

### 3.2 运行期模型

```text
ActorWorld.PostTo<TEvent>
  -> EventPostRuntime<TEvent>.GetState(world)
  -> state.Route
  -> state.RowsByArchetype[actorId.ArchetypeId]
  -> row.Mails[actorId.SlotIndex]
  -> PostCore
```

运行期不再经过：

```text
BehaviourArchetype.Post
TypedActorStorage.Post
TryGetColumn
EventColumn.Post
EventMailWriter.Enqueue 的策略 switch
```

旧路径可保留为诊断 fallback，但默认 Post 热路径不依赖旧路径。

---

## 4. ActorWorld 状态约束

### 4.1 ActorWorldState

建议明确 ActorWorld 生命周期状态：

```csharp
internal enum ActorWorldState
{
    Created,
    Building,
    Running,
    Disposed
}
```

语义：

```text
Created:
  ActorWorld 已创建，但尚未进入构建流程。

Building:
  允许注册 EventMetaData。
  允许构建 ActorPostRoute。
  允许创建 EventPostState。
  允许注册 EventPostRow。

Running:
  禁止修改事件策略。
  禁止重新注册 EventMetaData。
  禁止修改 ActorMailOptions。
  只允许执行已经构建完成的 Post 路径。

Disposed:
  清理 ActorWorld 资源。
  释放 RuntimeIndex。
  清理 EventPostRuntime<TEvent> 绑定。
```

---

## 5. EventMetaData 职责

`EventMetaData<TEvent>` 是事件策略的唯一声明来源。

```csharp
public abstract class EventMetaData<TEvent> : IEventMetaData
    where TEvent : struct
{
    public int EventId => EventTypeId<TEvent>.Id;

    public virtual EventCategoryToken Category => EventCategoryToken.Empty;

    public virtual EventPostPolicy? PostPolicy => null;

    public virtual EventTimerPolicy? TimerPolicy => null;

    public virtual EventBufferPolicy? BufferPolicy => null;

    public virtual ActorMailOptions? ActorMailOptions => null;

    public virtual int GetPostCoalesceKey(in TEvent value)
    {
        return 0;
    }

    public virtual bool TryMergePostEvent(
        ref TEvent current,
        in TEvent next)
    {
        return false;
    }
}
```

职责划分：

```text
EventId:
  构建期用于索引 EventPostState 和事件目录。

Category:
  用于统计、诊断和事件分类。

PostPolicy:
  构建期读取，用于推导 ActorPostRouteKind。

BufferPolicy:
  当前阶段不进入 ActorPost 热路径。
  可用于未来独立缓冲系统。

ActorMailOptions:
  构建期读取，用于创建 EventMailPool<TEvent> 和 route。

TimerPolicy:
  不参与 ActorPost 邮箱路径。

GetPostCoalesceKey:
  当前阶段不进入 ActorPost 热路径。

TryMergePostEvent:
  当前阶段不进入 ActorPost 热路径。
```

---

## 6. EventRuntimePolicyTable 处理

### 6.1 不再作为运行期策略表

现有 `EventRuntimePolicyTable` 不应继续作为运行期可变策略表。

建议改名为：

```text
EventBuildPolicyTable
```

或者：

```text
EventPolicyBuildContext
```

它只允许在构建期存在。

---

### 6.2 禁止运行期 SetPolicy

以下方法只能在构建期调用：

```text
SetMetaData
SetPostPolicy
SetTimerPolicy
SetBufferPolicy
SetActorMailOptions
```

进入 Running 后调用必须抛出异常。

```csharp
public void SetActorMailOptions(
    int eventTypeId,
    ActorMailOptions options)
{
    // eventTypeId 参数作用：
    // 表示事件类型的全局整数 ID。
    // 构建期使用它把事件策略写入策略表对应槽位。
    //
    // options 参数作用：
    // 表示该事件的 Actor 邮箱配置。
    // 只能在构建期参与 route 编译，运行期不允许修改。

    ThrowIfFrozen();

    EnsureActorMailCapacity(eventTypeId);
    _actorMailOptionsByEventId[eventTypeId] = options;
}
```

---

### 6.3 构建结束后冻结

```csharp
internal sealed class EventBuildPolicyTable
{
    private bool _frozen;

    public void Freeze()
    {
        // Freeze 方法作用：
        // 标记策略表已经构建完成。
        // 进入冻结状态后，任何 SetPolicy 行为都必须失败。
        _frozen = true;
    }

    private void ThrowIfFrozen()
    {
        // ThrowIfFrozen 方法作用：
        // 防止运行期继续修改策略。
        // 如果策略已经冻结，说明 ActorWorld 已经进入 Running 或即将进入 Running。
        if (_frozen)
        {
            throw new InvalidOperationException(
                "Event policy table is frozen. Event policies can only be changed during build time.");
        }
    }
}
```

---

## 7. ActorPostRouteKind

`ActorPostRouteKind` 是构建期编译出的内部路线枚举。

```csharp
internal enum ActorPostRouteKind : byte
{
    /// <summary>
    /// 普通队列。
    /// 邮箱满时尝试扩容。
    /// 扩容失败后按 GrowFailurePolicy 处理。
    /// </summary>
    QueuedGrow = 0,

    /// <summary>
    /// 固定容量队列。
    /// 邮箱满时拒绝新消息。
    /// </summary>
    QueuedRejectNew = 1,

    /// <summary>
    /// 固定容量队列。
    /// 邮箱满时丢弃最旧消息，然后写入新消息。
    /// </summary>
    QueuedDropOldest = 2,

    /// <summary>
    /// 只保留最新消息。
    /// 重复 Post 时覆盖旧消息。
    /// </summary>
    Latest = 3,

    /// <summary>
    /// 脏标记语义。
    /// 邮箱已有未处理消息时，不重复写入。
    /// </summary>
    Dirty = 4,

    /// <summary>
    /// 禁止 ActorPost。
    /// PostTo 直接返回失败结果。
    /// </summary>
    Disabled = 5,

    /// <summary>
    /// 只能走旧诊断路径。
    /// 用于暂时无法编译为 fast path 的特殊策略。
    /// </summary>
    DiagnosticOnly = 6
}
```

---

## 8. ActorEventPostPlan

`ActorEventPostPlan<TEvent>` 是构建期产物。

```csharp
internal readonly struct ActorEventPostPlan<TEvent>
    where TEvent : struct
{
    public readonly int EventId;
    public readonly EventIdentity Identity;
    public readonly EventCategoryToken Category;
    public readonly ActorPostRouteKind Route;
    public readonly ActorMailOptions MailOptions;
    public readonly ActorSlotFlags RejectMask;
    public readonly bool RejectDisabled;
    public readonly EventMetaData<TEvent> MetaData;

    public ActorEventPostPlan(
        int eventId,
        EventIdentity identity,
        EventCategoryToken category,
        ActorPostRouteKind route,
        ActorMailOptions mailOptions,
        ActorSlotFlags rejectMask,
        bool rejectDisabled,
        EventMetaData<TEvent> metaData)
    {
        // eventId 参数作用：
        // 事件类型 ID。
        // 用于构建 EventPostState、注册 Row、诊断统计和事件目录。

        // identity 参数作用：
        // 事件稳定身份。
        // 用于日志、诊断、导出事件目录。

        // category 参数作用：
        // 事件分类 token。
        // 用于分类统计、调试过滤和异常归类。

        // route 参数作用：
        // 构建期编译出的 ActorPost 路线。
        // 运行期 PostTo 只根据该路线进入专用 PostCore。

        // mailOptions 参数作用：
        // 最终合并后的邮箱配置。
        // 用于创建 EventMailPool<TEvent> 和执行专用 PostCore。

        // rejectMask 参数作用：
        // Slot 状态拒绝掩码。
        // 用于运行期快速拒绝 PendingDestroy / Destroying 等不可投递状态。

        // rejectDisabled 参数作用：
        // 表示 Disabled Actor 是否拒绝收信。
        // true 表示 Actor Disabled 时 PostTo 应失败。

        // metaData 参数作用：
        // 当前事件的强类型元数据。
        // 只允许构建期使用，不进入普通 Post 热路径。

        EventId = eventId;
        Identity = identity;
        Category = category;
        Route = route;
        MailOptions = mailOptions;
        RejectMask = rejectMask;
        RejectDisabled = rejectDisabled;
        MetaData = metaData;
    }
}
```

---

## 9. ActorEventPostPlanBuilder

构建期通过 `EventMetaData<TEvent>` 创建 `ActorEventPostPlan<TEvent>`。

```csharp
internal static class ActorEventPostPlanBuilder
{
    public static ActorEventPostPlan<TEvent> Build<TEvent>(
        ActorMailOptions worldDefaultMailOptions)
        where TEvent : struct
    {
        // worldDefaultMailOptions 参数作用：
        // ActorWorld 默认邮箱配置。
        // 当 EventMetaData<TEvent>.ActorMailOptions 没有提供配置时，使用它作为兜底配置。

        EventMetaData<TEvent> metaData =
            EventMetaDataHandler.GetOrCreate<TEvent>();

        ActorMailOptions mailOptions =
            metaData.GetActorMailOptions() ?? worldDefaultMailOptions;

        ActorPostRouteKind route =
            ResolveRoute(mailOptions);

        ActorSlotFlags rejectMask =
            ActorSlotFlags.PendingDestroy | ActorSlotFlags.Destroying;

        bool rejectDisabled =
            mailOptions.DisabledPolicy == ActorMailDisabledPolicy.Reject;

        return new ActorEventPostPlan<TEvent>(
            eventId: metaData.EventId,
            identity: metaData.GetIdentity(),
            category: metaData.GetEventCategoryToken(),
            route: route,
            mailOptions: mailOptions,
            rejectMask: rejectMask,
            rejectDisabled: rejectDisabled,
            metaData: metaData);
    }

    private static ActorPostRouteKind ResolveRoute(
        ActorMailOptions options)
    {
        // options 参数作用：
        // 当前事件最终使用的邮箱配置。
        // 构建期根据它把事件编译成固定 ActorPostRouteKind。

        return options.PostPolicy switch
        {
            ActorPostPolicy.Queued
                when options.FullPolicy == ActorMailFullPolicy.Grow
                => ActorPostRouteKind.QueuedGrow,

            ActorPostPolicy.Queued
                when options.FullPolicy == ActorMailFullPolicy.RejectNew
                => ActorPostRouteKind.QueuedRejectNew,

            ActorPostPolicy.Queued
                when options.FullPolicy == ActorMailFullPolicy.DropOldest
                => ActorPostRouteKind.QueuedDropOldest,

            ActorPostPolicy.Latest
                => ActorPostRouteKind.Latest,

            ActorPostPolicy.Dirty
                => ActorPostRouteKind.Dirty,

            _ => ActorPostRouteKind.DiagnosticOnly
        };
    }
}
```

---

## 10. GlobalEventMailPoolRegistry

`GlobalEventMailPoolRegistry` 是 ActorWorld 内部的事件邮箱池注册表。

它保证：

```text
同一个 ActorWorld + TEvent 只有一个 EventMailPool<TEvent>。
```

```csharp
internal sealed class GlobalEventMailPoolRegistry
{
    private object?[] _poolsByEventId = new object?[64];

    public EventMailPool<TEvent> GetOrCreate<TEvent>(
        ActorMailOptions options)
        where TEvent : struct
    {
        // options 参数作用：
        // 当前事件的邮箱配置。
        // 只在第一次创建 EventMailPool<TEvent> 时使用。

        int eventId = EventTypeId<TEvent>.Id;

        EnsureCapacity(eventId);

        object? existing = _poolsByEventId[eventId];

        if (existing != null)
        {
            return (EventMailPool<TEvent>)existing;
        }

        var pool = new EventMailPool<TEvent>(options);
        _poolsByEventId[eventId] = pool;
        return pool;
    }

    private void EnsureCapacity(int eventId)
    {
        // eventId 参数作用：
        // 事件类型 ID。
        // 用于保证 _poolsByEventId 能安全索引该事件。

        if ((uint)eventId < (uint)_poolsByEventId.Length)
        {
            return;
        }

        int newSize = _poolsByEventId.Length;

        while (newSize <= eventId)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _poolsByEventId, newSize);
    }
}
```

---

## 11. EventPostState

`EventPostState<TEvent>` 是 `ActorWorld + TEvent` 级运行期状态。

```csharp
internal sealed class EventPostState<TEvent>
    where TEvent : struct
{
    public readonly ActorPostRouteKind Route;
    public readonly EventMailPool<TEvent> Pool;
    public readonly ActorMailOptions Options;
    public readonly ActorSlotFlags RejectMask;
    public readonly bool RejectDisabled;
    public EventPostRow<TEvent>[] RowsByArchetype;

    public EventPostState(
        ActorPostRouteKind route,
        EventMailPool<TEvent> pool,
        ActorMailOptions options,
        ActorSlotFlags rejectMask,
        bool rejectDisabled,
        EventPostRow<TEvent>[] rowsByArchetype)
    {
        // route 参数作用：
        // 当前事件已经编译好的投递路线。
        // PostTo<TEvent> 根据它进入专用 PostCore。

        // pool 参数作用：
        // 当前 ActorWorld + TEvent 唯一的邮箱池。
        // 所有 Archetype 的 TEvent 邮箱都从这里租用 buffer。

        // options 参数作用：
        // 当前事件最终邮箱配置。
        // PostCore 使用它读取 InitialCapacity / MaxCapacity / GrowFactor 等固定值。

        // rejectMask 参数作用：
        // Slot 状态拒绝掩码。
        // 用于快速拒绝 PendingDestroy / Destroying 等状态。

        // rejectDisabled 参数作用：
        // 是否拒绝 Disabled Actor 收信。
        // true 表示 Actor disabled 时直接返回失败。

        // rowsByArchetype 参数作用：
        // 每个 Archetype 对应的 TEvent 邮箱定位表。
        // PostTo 通过 actorId.ArchetypeId 直接取 Row。

        Route = route;
        Pool = pool;
        Options = options;
        RejectMask = rejectMask;
        RejectDisabled = rejectDisabled;
        RowsByArchetype = rowsByArchetype;
    }
}
```

---

## 12. EventPostRow

`EventPostRow<TEvent>` 是 `Archetype + TEvent` 级定位信息。

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
        // PostTo 通过 actorId.SlotIndex 定位目标 Actor 的邮箱。

        // dirtySlots 参数作用：
        // 当前事件列的脏 slot 列表。
        // 当某个 Actor 邮箱从空变成非空时，将 slotIndex 标记为待 Pump。

        // bucketIndex 参数作用：
        // 当前事件列在 DirtyBucketList 中的 bucket 下标。
        // 邮箱出现新消息时，用它标记对应事件 bucket 为 dirty。

        // generations 参数作用：
        // 当前 Archetype 的 slot 代际数组。
        // 用于判断 ActorId 是否过期。

        // slotFlags 参数作用：
        // 当前 Archetype 的 slot 状态数组。
        // 用于快速判断 Alive、Enabled、PendingDestroy、Destroying 等状态。

        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        Generations = generations;
        SlotFlags = slotFlags;
    }

    public bool IsValid => Mails != null;
}
```

---

## 13. EventPostRuntime

`EventPostRuntime<TEvent>` 用泛型静态数组保存每个 ActorWorld 的 `EventPostState<TEvent>`。

```csharp
internal static class EventPostRuntime<TEvent>
    where TEvent : struct
{
    private static EventPostState<TEvent>?[] s_statesByWorld =
        new EventPostState<TEvent>?[4];

    public static void BindWorld(
        ActorWorld world,
        EventPostState<TEvent> state)
    {
        // world 参数作用：
        // 当前 ActorWorld。
        // 使用 world.RuntimeIndex 作为状态数组下标。

        // state 参数作用：
        // 当前 ActorWorld + TEvent 的编译后 Post 状态。
        // 保存 route、pool、options、rows。

        int worldIndex = world.RuntimeIndex;

        if ((uint)worldIndex >= (uint)s_statesByWorld.Length)
        {
            Resize(worldIndex);
        }

        s_statesByWorld[worldIndex] = state;
    }

    public static EventPostState<TEvent>? GetState(
        ActorWorld world)
    {
        // world 参数作用：
        // 当前 ActorWorld。
        // 用 RuntimeIndex 读取该 world 下当前 TEvent 的 Post 状态。

        int worldIndex = world.RuntimeIndex;

        if ((uint)worldIndex >= (uint)s_statesByWorld.Length)
        {
            return null;
        }

        return s_statesByWorld[worldIndex];
    }

    public static void UnbindWorld(
        int worldIndex)
    {
        // worldIndex 参数作用：
        // ActorWorld 的运行期索引。
        // ActorWorld Dispose 时用它清理静态状态表，避免旧状态泄漏。

        if ((uint)worldIndex < (uint)s_statesByWorld.Length)
        {
            s_statesByWorld[worldIndex] = null;
        }
    }

    private static void Resize(int worldIndex)
    {
        // worldIndex 参数作用：
        // 需要容纳的 ActorWorld 索引。
        // 如果当前数组不够大，则扩容到可以索引该 world。

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

## 14. ActorWorld 新增字段

```csharp
public sealed partial class ActorWorld
{
    internal GlobalEventMailPoolRegistry GlobalEventMailPools { get; } = new();

    internal ActorMailOptions DefaultMailOptions { get; }

    internal readonly int RuntimeIndex;
}
```

说明：

```text
GlobalEventMailPools:
  ActorWorld 内所有 TEvent 的全局邮箱池注册表。

DefaultMailOptions:
  当前 ActorWorld 默认邮箱配置。

RuntimeIndex:
  当前 ActorWorld 在 EventPostRuntime<TEvent> 中的索引。
```

---

## 15. 构建 EventPostState

```csharp
internal EventPostState<TEvent> GetOrCreateEventPostState<TEvent>(
    ActorEventPostPlan<TEvent> plan)
    where TEvent : struct
{
    // plan 参数作用：
    // 当前事件的构建期编译结果。
    // 包含 route、mail options、reject flags、metadata 等信息。

    EventPostState<TEvent>? existing =
        EventPostRuntime<TEvent>.GetState(this);

    if (existing != null)
    {
        return existing;
    }

    EventMailPool<TEvent> pool =
        GlobalEventMailPools.GetOrCreate<TEvent>(
            plan.MailOptions);

    EventPostRow<TEvent>[] rows =
        new EventPostRow<TEvent>[Math.Max(_archetypes.Length, 1)];

    var state = new EventPostState<TEvent>(
        route: plan.Route,
        pool: pool,
        options: plan.MailOptions,
        rejectMask: plan.RejectMask,
        rejectDisabled: plan.RejectDisabled,
        rowsByArchetype: rows);

    EventPostRuntime<TEvent>.BindWorld(
        this,
        state);

    return state;
}
```

---

## 16. 注册 EventPostRow

`EventColumn.RefreshPostRowBinding()` 当前会在邮箱数组扩容后刷新绑定。

新设计中，它应该注册 Row，但 Row 不保存 Pool。

```csharp
internal void RegisterEventPostRow<TEvent>(
    int archetypeId,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    int[] generations,
    ActorSlotFlags[] slotFlags,
    ActorEventPostPlan<TEvent> plan)
    where TEvent : struct
{
    // archetypeId 参数作用：
    // 当前 Archetype 的整数 ID。
    // 用于写入 RowsByArchetype[archetypeId]。

    // mails 参数作用：
    // 当前 Archetype + TEvent 的邮箱数组。
    // PostTo 通过 slotIndex 定位目标邮箱。

    // dirtySlots 参数作用：
    // 当前事件列的脏 slot 列表。
    // 邮箱从空变非空时需要标记 dirty slot。

    // bucketIndex 参数作用：
    // 当前事件列对应的 dirty bucket 下标。
    // PostCore 写入新消息时需要标记该 bucket。

    // generations 参数作用：
    // 当前 Archetype 的 slot 代际数组。
    // 用于校验 ActorId 是否仍然有效。

    // slotFlags 参数作用：
    // 当前 Archetype 的 slot 状态数组。
    // 用于校验 Alive、Enabled、PendingDestroy、Destroying 等状态。

    // plan 参数作用：
    // 当前事件的构建期编译结果。
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
            bucketIndex,
            generations,
            slotFlags);
}
```

---

## 17. PostTo

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // PostTo 使用 ArchetypeId、SlotIndex、Generation 定位目标邮箱。

    // value 参数作用：
    // 要投递给目标 Actor 的事件值。
    // 该值会被写入目标 Actor 对应 TEvent 邮箱。

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

        ActorPostRouteKind.DiagnosticOnly =>
            PostToDiagnostic(actorId, in value),

        _ =>
            PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                "Unknown actor post route.",
                PostFailureKind.UnsupportedEvent)
    };
}
```

---

## 18. Row 校验

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static bool TryGetValidRow<TEvent>(
    ActorId actorId,
    EventPostState<TEvent> state,
    out EventPostRow<TEvent> row,
    out PostResult failure)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 用于定位 Archetype、Slot 和 Generation。

    // state 参数作用：
    // 当前 ActorWorld + TEvent 的 Post 状态。
    // 保存 RowsByArchetype 和固定拒绝策略。

    // row 参数作用：
    // 输出定位成功后的 EventPostRow。
    // 调用方使用 row.Mails 和 slotIndex 执行写入。

    // failure 参数作用：
    // 输出定位失败时的 PostResult。
    // 用于保留可诊断的失败原因。

    EventPostRow<TEvent>[] rows = state.RowsByArchetype;

    int archetypeId = actorId.ArchetypeId;

    if ((uint)archetypeId >= (uint)rows.Length)
    {
        row = default;
        failure = PostResult.Failure(
            ActorPostStatus.ActorNotFound,
            "Invalid ActorId.ArchetypeId.",
            PostFailureKind.InvalidActorId);
        return false;
    }

    row = rows[archetypeId];

    if (!row.IsValid)
    {
        failure = PostResult.Failure(
            ActorPostStatus.EventNotSupported,
            "Target archetype does not support this event.",
            PostFailureKind.UnsupportedEvent);
        return false;
    }

    int slotIndex = actorId.SlotIndex;

    if ((uint)slotIndex >= (uint)row.Generations.Length)
    {
        failure = PostResult.Failure(
            ActorPostStatus.ActorNotFound,
            "Invalid ActorId.SlotIndex.",
            PostFailureKind.InvalidActorId);
        return false;
    }

    if (row.Generations[slotIndex] != actorId.Generation)
    {
        failure = PostResult.Failure(
            ActorPostStatus.ActorNotFound,
            "ActorId generation mismatch.",
            PostFailureKind.InvalidActorId);
        return false;
    }

    ActorSlotFlags flags = row.SlotFlags[slotIndex];

    if ((flags & ActorSlotFlags.Alive) == 0)
    {
        failure = PostResult.Failure(
            ActorPostStatus.ActorNotAlive,
            "Actor slot is not alive.",
            PostFailureKind.InvalidActorId);
        return false;
    }

    if ((flags & state.RejectMask) != 0)
    {
        failure = PostResult.Failure(
            ActorPostStatus.ActorPendingDestroy,
            "Actor slot is not postable.",
            PostFailureKind.PendingDestroy);
        return false;
    }

    if (state.RejectDisabled &&
        (flags & ActorSlotFlags.Enabled) == 0)
    {
        failure = PostResult.Failure(
            ActorPostStatus.ActorDisabledRejected,
            "Actor is disabled.",
            PostFailureKind.DisabledActor);
        return false;
    }

    failure = PostResult.Success;
    return true;
}
```

---

## 19. QueuedGrow 路径

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private PostResult PostQueuedGrow<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 用于定位目标 Actor 邮箱。

    // value 参数作用：
    // 要写入邮箱的事件值。

    // state 参数作用：
    // 当前 ActorWorld + TEvent 的编译后 Post 状态。
    // 提供 Route、Pool、Options、RowsByArchetype。

    if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
    {
        return failure;
    }

    int slotIndex = actorId.SlotIndex;

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

## 20. QueuedRejectNew 路径

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private PostResult PostQueuedRejectNew<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // state 参数作用：
    // 当前事件的固定 Post 状态。
    // QueuedRejectNew 路径使用其中的 Pool 和 Options，但邮箱满时不扩容。

    if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
    {
        return failure;
    }

    int slotIndex = actorId.SlotIndex;

    return PostQueuedRejectNewCore(
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

## 21. QueuedDropOldest 路径

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private PostResult PostQueuedDropOldest<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // state 参数作用：
    // 当前事件的固定 Post 状态。
    // QueuedDropOldest 路径在邮箱满时移动 Head 并覆盖旧消息。

    if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
    {
        return failure;
    }

    int slotIndex = actorId.SlotIndex;

    return PostQueuedDropOldestCore(
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

## 22. Latest 路径

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private PostResult PostLatest<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // state 参数作用：
    // 当前事件的固定 Post 状态。
    // Latest 路径只保留最新消息，重复写入会覆盖旧值。

    if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
    {
        return failure;
    }

    int slotIndex = actorId.SlotIndex;

    return PostLatestCore(
        slotIndex,
        in value,
        row.Mails,
        row.DirtySlots,
        row.BucketIndex,
        state.Pool);
}
```

---

## 23. Dirty 路径

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private PostResult PostDirty<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // state 参数作用：
    // 当前事件的固定 Post 状态。
    // Dirty 路径在邮箱已有消息时不重复写入，只保持待处理状态。

    if (!TryGetValidRow(actorId, state, out EventPostRow<TEvent> row, out PostResult failure))
    {
        return failure;
    }

    int slotIndex = actorId.SlotIndex;

    return PostDirtyCore(
        slotIndex,
        in value,
        row.Mails,
        row.DirtySlots,
        row.BucketIndex,
        state.Pool);
}
```

---

## 24. EventColumn 调整

### 24.1 EventColumn 不拥有 Pool 生命周期

`EventColumn<TActor,TEvent>` 可以暂时保留 `_mailPool` 字段用于 Pump，但 Pool 的生命周期归属必须是：

```text
ActorWorld.GlobalEventMailPools
  -> EventMailPool<TEvent>
```

不允许：

```text
EventColumn 自己 new EventMailPool<TEvent>
EventPostRow 保存 EventMailPool<TEvent>
每个 Archetype 创建独立 EventMailPool<TEvent>
```

---

### 24.2 RefreshPostRowBinding

```csharp
public override void RefreshPostRowBinding()
{
    // RefreshPostRowBinding 方法作用：
    // 当 EventColumn 的 Mails 数组扩容后，重新把最新 Mails 引用注册到 EventPostRow。
    // 这样 PostTo 热路径可以继续通过 Row 访问最新邮箱数组。

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

## 25. TypedActorStorage 调整

### 25.1 暴露 SlotFlags

当前 `TypedActorStorage<TActor>` 已经有 `_slotFlags`。

需要增加内部只读访问器：

```csharp
internal ActorSlotFlags[] SlotFlags => _slotFlags;
```

作用：

```text
EventPostRow<TEvent> 必须能读取 SlotFlags。
PostTo 快路径需要它判断 Alive、Enabled、PendingDestroy、Destroying。
```

---

### 25.2 Post 不再作为默认热路径

`TypedActorStorage.Post<TEvent>` 可以保留为诊断 fallback。

默认 `ActorWorld.PostTo<TEvent>` 不再进入：

```text
BehaviourArchetype.Post
TypedActorStorage.Post
EventColumn.Post
```

---

## 26. ActorQueryResult.PostAll 调整

`PostAll` 不应逐 Actor 调用 `PostTo`。

正确路径：

```text
PostAll<TEvent>
  -> 读取一次 EventPostState<TEvent>
  -> 根据 Route 进入 PostAllCore
  -> 遍历 Query 命中的 Archetype Row
  -> 遍历 alive slot
  -> 批量写入邮箱
```

示例：

```csharp
public static void PostAll<TEvent>(
    this ActorQueryResult query,
    in TEvent value)
    where TEvent : struct
{
    // query 参数作用：
    // Actor 查询结果。
    // 包含命中的 Archetype 缓存。

    // value 参数作用：
    // 要批量投递的事件值。
    // 会写入所有命中 Actor 的 TEvent 邮箱。

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

## 27. 迁移规则

### 27.1 删除运行期策略参数

从以下 API 移除：

```text
ActorWorld.PostTo
ActorWorld.TryPostTo
Actor.PostInside
ActorQueryResult.PostAll
PostScheduler.Post
```

删除参数：

```text
ActorPostPolicy? postPolicy
ActorMailFullPolicy? fullPolicy
```

---

### 27.2 保留 PostResult

保留：

```text
PostResult
ActorPostStatus
PostFailureKind
```

用途：

```text
保留失败原因。
保留测试可读性。
保留调试能力。
保留工具层诊断能力。
```

---

### 27.3 事件拆分替代动态策略

旧写法：

```csharp
world.PostTo(actorId, in value, ActorPostPolicy.Latest);
```

新写法：

```csharp
world.PostTo(actorId, in latestValueEvent);
```

旧写法：

```csharp
world.PostTo(actorId, in value, fullPolicy: ActorMailFullPolicy.DropOldest);
```

新写法：

```csharp
world.PostTo(actorId, in dropOldestEvent);
```

---

## 28. 验收标准

### 28.1 构建期验收

必须满足：

```text
EventMetaData<TEvent> 是唯一策略声明来源。
ActorPostRouteKind 只在构建期生成。
EventPostState<TEvent> 只在构建期创建或绑定。
EventPostRow<TEvent> 只在构建期或 Column 扩容时刷新。
ActorWorld 进入 Running 后不允许修改策略。
```

---

### 28.2 运行期验收

`PostTo<TEvent>` 中不得出现：

```text
EventMetaData<TEvent>
EventRuntimePolicyTable
GetPostPolicy
GetBufferPolicy
GetActorMailOptions
ActorPostPolicy?
ActorMailFullPolicy?
EventMailWriter.Enqueue 的策略分派
```

允许出现：

```text
EventPostRuntime<TEvent>.GetState
EventPostState<TEvent>
ActorPostRouteKind switch
PostResult
专用 PostCore
```

---

### 28.3 Pool 验收

必须满足：

```text
同一个 ActorWorld + TEvent 只有一个 EventMailPool<TEvent>。
EventPostRow<TEvent> 不持有 EventMailPool<TEvent>。
EventColumn<TActor,TEvent> 不创建 EventMailPool<TEvent>。
PostCore 使用 EventPostState<TEvent>.Pool。
```

---

### 28.4 Slot 状态验收

必须满足：

```text
PostTo 快路径必须检查 Generation。
PostTo 快路径必须检查 ActorSlotFlags.Alive。
PostTo 快路径必须检查 PendingDestroy。
PostTo 快路径必须检查 Destroying。
DisabledPolicy == Reject 时必须拒绝 Disabled Actor。
```

---

## 29. 测试要求

### 29.1 策略冻结测试

```text
ActorWorld 进入 Running 后：
  SetPostPolicy 应抛异常。
  SetBufferPolicy 应抛异常。
  SetActorMailOptions 应抛异常。
```

---

### 29.2 PostTo 不读 Metadata 测试

```text
通过代码扫描或测试替身确认：
  PostTo<TEvent> 不调用 EventMetaData<TEvent>。
  PostTo<TEvent> 不调用 EventRuntimePolicyTable。
```

---

### 29.3 Pool 复用测试

```text
同一个 ActorWorld 中：
  TestActorA 支持 TestEvent。
  TestActorB 支持 TestEvent。
  两个 Archetype 的 TestEvent 邮箱必须共享同一个 EventMailPool<TestEvent>。
```

---

### 29.4 Slot 状态测试

```text
Actor alive 时可以投递。
Actor generation mismatch 时投递失败。
Actor pending destroy 时投递失败。
Actor destroying 时投递失败。
DisabledPolicy == Reject 且 Actor disabled 时投递失败。
DisabledPolicy == Accept 且 Actor disabled 时允许投递。
```

---

## 30. Benchmark 要求

保留或新增：

```text
ActorPost_QueuedGrow_PostTo_OneActor
ActorPost_QueuedGrow_PostTo_1000Actors
ActorPost_QueuedRejectNew_NotFull_OneActor
ActorPost_QueuedRejectNew_Full_OneActor
ActorPost_QueuedDropOldest_NotFull_OneActor
ActorPost_QueuedDropOldest_Full_OneActor
ActorPost_Latest_PostTo_OneActor
ActorPost_Dirty_PostTo_OneActor
ActorPost_Query_PostAll_1000Actors
ActorPost_Query_PostAll_1000Actors_12Events
```

对比对象：

```text
旧 ActorWorld.PostTo
旧 TypedActorStorage.Post
旧 EventColumn.Post
旧 EventMailWriter.Enqueue
Dictionary 直接索引基准
```

---

## 31. 分阶段落地

### Phase 1：QueuedGrow 快路径

目标：

```text
引入 EventPostState<TEvent>。
引入 EventPostRow<TEvent>。
引入 GlobalEventMailPoolRegistry。
让默认 QueuedGrow 路径绕过 BehaviourArchetype / Storage / Column.Post。
保留旧路径作为 fallback。
```

---

### Phase 2：固定路线补全

目标：

```text
补齐 QueuedRejectNew。
补齐 QueuedDropOldest。
补齐 Latest。
补齐 Dirty。
移除 PostTo 的运行期策略参数。
```

---

### Phase 3：策略冻结

目标：

```text
EventRuntimePolicyTable 改为 EventBuildPolicyTable。
ActorWorld 进入 Running 后冻结策略。
禁止运行期 SetPolicy。
补充冻结测试。
```

---

### Phase 4：Query PostAll 批量快路径

目标：

```text
PostAll 不逐 Actor 调用 PostTo。
PostAll 按 Route 批量写入。
PostAll 支持 1 到 12 个事件参数。
```

---

## 32. 最终结构

```text
EventMetaData<TEvent>
  唯一策略声明来源。

ActorEventPostPlan<TEvent>
  构建期编译结果。

GlobalEventMailPoolRegistry
  ActorWorld 内 TEvent 全局邮箱池注册表。

EventPostState<TEvent>
  ActorWorld + TEvent 级运行期状态。

EventPostRow<TEvent>
  Archetype + TEvent 级邮箱定位信息。

EventPostRuntime<TEvent>
  泛型静态 world-state 表。

ActorWorld.PostTo<TEvent>
  只执行已编译路线。
  不接收运行期策略。
  不读取 Metadata。
  保留 PostResult。
```

一句话结论：

```text
策略属于构建期，投递属于运行期。
事件类型表达稳定语义。
PostTo 只执行已经编译好的路线。
```
