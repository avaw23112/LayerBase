# ActorPost Build-Time RouteCode And Deferred Structural Dirty Design

## 1. 目标

本设计用于重构 LayerBase 的 ActorPost 投递路径。

核心目标：

```text
事件策略只在构建期确定。
运行期 Post 不允许动态改变策略。
运行期 Post 不读取 EventMetaData。
运行期 Post 不读取 EventRuntimePolicyTable。
运行期 Post 尽量只做物理邮箱定位与写入。
Actor 生命周期状态变化通过 StructuralDirty 延迟整理。
必要的逻辑可投递检查通过 PostableGeneration 压缩为一次整数比较。
Route 选择使用 bit-packed RouteCode，默认路线走裸 if，非默认路线进入冷路径。
```

最终效果：

```text
构建期：
  读取 EventMetaData<TEvent>。
  编译 ActorPostRouteCode。
  创建 EventPostState<TEvent>。
  创建 EventPostRow<TEvent>。
  选择是否绑定 PostableGeneration。

运行期：
  PostTo<TEvent> 读取 EventPostState<TEvent>。
  默认 QueuedGrow_PhysicalSafe 通过一次 routeCode 相等比较直达。
  非默认路线进入 NoInlining 冷路径。
  Post 热路径不再做完整 ActorSlotFlags 判断。
  Query.PostAll 使用 route hoisting 和 unchecked 写入。

结构整理期：
  统一处理死亡、禁用、slot 回收、邮箱清理、Query 缓存刷新。
  统一刷新 PostableGeneration。
```

---

## 2. 术语说明

### 2.1 Hot Path

`Hot Path` 指高频执行路径。

在本设计中，Hot Path 主要是：

```text
ActorWorld.PostTo<TEvent>
ActorQueryResult.PostAll<TEvent>
EventMail 写入
DirtySlot 标记
```

设计原则：

```text
Hot Path 中不能放复杂策略判断。
Hot Path 中不能放多层 fallback。
Hot Path 中不能反复读取元数据。
Hot Path 中不能做完整生命周期诊断。
```

---

### 2.2 Cold Path

`Cold Path` 指低频执行路径。

在本设计中，Cold Path 包括：

```text
失败诊断。
非默认 Route 处理。
不常见邮箱策略。
结构整理。
SweepPendingDestroy。
Query cache rebuild。
```

`Cold Path` 可以使用更完整的判断和更清楚的 PostResult。

---

### 2.3 StructuralDirty

`StructuralDirty` 表示 Actor 的结构状态发生了变化，但不立即重建结构。

例子：

```text
Actor 被标记 PendingDestroy。
Actor Enable 状态改变。
Actor slot 等待回收。
Query 缓存需要刷新。
```

设计原则：

```text
结构变化先标记 StructuralDirty。
不在 Post 热路径立即处理结构变化。
在 Pump 前后或 Sweep 阶段统一整理。
```

---

### 2.4 PostableGeneration

`PostableGeneration` 是“可投递代际”。

它把多种状态检查压缩为一次整数比较：

```text
Generation 是否匹配。
Actor 是否 Alive。
Actor 是否 PendingDestroy。
Actor 是否 Destroying。
Actor Disabled 时是否允许接收该事件。
```

运行期判断：

```csharp
row.PostableGenerations[slotIndex] == actorId.Generation
```

如果相等：

```text
ActorId 没过期。
Actor 当前状态允许接收该事件。
```

如果不相等：

```text
ActorId 过期或当前状态不允许接收该事件。
```

---

### 2.5 bit-packed RouteCode

`bit-packed RouteCode` 指把多个含义压到一个 `byte` 里。

本设计中：

```text
低 3 位表示写入模式。
高 2 位表示校验模式。
```

例子：

```text
QueuedGrow + PhysicalSafe
QueuedGrow + PostableStamp
QueuedRejectNew + PhysicalSafe
Dirty + PhysicalSafe
```

这样运行期可以通过位运算拆解路线：

```csharp
byte writeMode = (byte)(routeCode & ActorPostRouteCode.WriteModeMask);
byte validation = (byte)(routeCode & ActorPostRouteCode.ValidationMask);
```

位运算比运行期对象策略、delegate 分发、接口调用更适合极短热路径。

---

## 3. 核心原则

### 3.1 策略属于构建期

禁止运行期传策略参数：

```csharp
world.PostTo(actorId, in value, ActorPostPolicy.Latest);
world.PostTo(actorId, in value, ActorMailFullPolicy.DropOldest);
query.PostAll(in value, fullPolicy: ActorMailFullPolicy.RejectNew);
```

允许：

```csharp
world.PostTo(actorId, in value);
query.PostAll(in value);
```

如果需要不同策略，应定义不同事件类型：

```text
ApplyDamageEvent
RefreshDamagePreviewEvent
MarkHealthDirtyEvent
DropOldestInputEvent
LatestPositionEvent
```

---

### 3.2 PostTo 成功只表示写入邮箱成功

`PostResult.Success` 表示：

```text
事件成功写入目标 Actor 当前物理邮箱。
```

不表示：

```text
目标 Actor 一定会执行该事件。
目标 Actor 当前一定业务 Alive。
目标 Actor 当前没有 PendingDestroy。
目标 Actor 当前没有 Disabled。
```

原因：

```text
Post 只负责邮箱写入。
Pump / Sweep 负责决定消息是否执行、何时清理。
```

---

### 3.3 生命周期检查从 Post 热路径迁出

Post 热路径不再反复判断：

```text
ActorSlotFlags.Alive
ActorSlotFlags.PendingDestroy
ActorSlotFlags.Destroying
ActorSlotFlags.Enabled
DisabledPolicy
PendingDestroyPolicy
```

迁移到：

```text
StructuralDirty 标记。
PostableGeneration 刷新。
Pump 消费阶段保护。
Sweep 清理阶段。
```

---

### 3.4 Route 选择不使用 delegate 数组

禁止将热路径分发改成：

```csharp
handlers[routeCode](actorId, in value, state);
```

原因：

```text
delegate 调用通常不能被 JIT 稳定内联。
函数指针或接口策略对象也可能产生间接调用。
对于 20ns 级别路径，间接调用可能比 switch 更贵。
```

推荐：

```text
默认路线用 if 直达。
非默认路线进入 NoInlining 冷路径。
路线特征用 RouteCode 位运算判断。
路线集合用 bit mask 判断。
```

---

## 4. RouteCode 设计

### 4.1 RouteCode 常量

```csharp
internal static class ActorPostRouteCode
{
    public const byte WriteModeMask = 0b0000_0111;
    public const byte ValidationMask = 0b0011_0000;

    public const byte WriteQueuedGrow = 0b0000_0000;
    public const byte WriteQueuedRejectNew = 0b0000_0001;
    public const byte WriteQueuedDropOldest = 0b0000_0010;
    public const byte WriteLatest = 0b0000_0011;
    public const byte WriteDirty = 0b0000_0100;
    public const byte WriteDisabled = 0b0000_0101;

    public const byte ValidationPhysicalSafe = 0b0000_0000;
    public const byte ValidationPostableStamp = 0b0001_0000;
    public const byte ValidationUnchecked = 0b0010_0000;

    public const byte QueuedGrowPhysicalSafe =
        WriteQueuedGrow | ValidationPhysicalSafe;

    public const byte QueuedGrowPostableStamp =
        WriteQueuedGrow | ValidationPostableStamp;

    public const byte QueuedGrowUnchecked =
        WriteQueuedGrow | ValidationUnchecked;

    public const byte QueuedRejectNewPhysicalSafe =
        WriteQueuedRejectNew | ValidationPhysicalSafe;

    public const byte QueuedRejectNewPostableStamp =
        WriteQueuedRejectNew | ValidationPostableStamp;

    public const byte QueuedRejectNewUnchecked =
        WriteQueuedRejectNew | ValidationUnchecked;

    public const byte QueuedDropOldestPhysicalSafe =
        WriteQueuedDropOldest | ValidationPhysicalSafe;

    public const byte QueuedDropOldestPostableStamp =
        WriteQueuedDropOldest | ValidationPostableStamp;

    public const byte QueuedDropOldestUnchecked =
        WriteQueuedDropOldest | ValidationUnchecked;

    public const byte LatestPhysicalSafe =
        WriteLatest | ValidationPhysicalSafe;

    public const byte DirtyPhysicalSafe =
        WriteDirty | ValidationPhysicalSafe;

    public const byte Disabled =
        WriteDisabled | ValidationPhysicalSafe;
}
```

说明：

```text
WriteModeMask:
  取出写入模式。

ValidationMask:
  取出校验模式。

WriteQueuedGrow:
  普通队列，可扩容。

WriteQueuedRejectNew:
  固定容量，满时拒绝新消息。

WriteQueuedDropOldest:
  固定容量，满时丢弃最旧消息。

WriteLatest:
  只保留最新消息。

WriteDirty:
  邮箱已有未处理消息时不重复写入。

ValidationPhysicalSafe:
  只做物理邮箱定位安全检查。

ValidationPostableStamp:
  额外做一次 PostableGeneration 比较。

ValidationUnchecked:
  框架内部使用，不做公开路径定位检查。
```

---

### 4.2 Route 集合位图

Route 集合位图用于快速判断路线类别。

```csharp
internal static class ActorPostRouteMasks
{
    public const uint QueuedRoutes =
        (1u << ActorPostRouteCode.QueuedGrowPhysicalSafe) |
        (1u << ActorPostRouteCode.QueuedGrowPostableStamp) |
        (1u << ActorPostRouteCode.QueuedGrowUnchecked) |
        (1u << ActorPostRouteCode.QueuedRejectNewPhysicalSafe) |
        (1u << ActorPostRouteCode.QueuedRejectNewPostableStamp) |
        (1u << ActorPostRouteCode.QueuedRejectNewUnchecked) |
        (1u << ActorPostRouteCode.QueuedDropOldestPhysicalSafe) |
        (1u << ActorPostRouteCode.QueuedDropOldestPostableStamp) |
        (1u << ActorPostRouteCode.QueuedDropOldestUnchecked);

    public const uint StampRoutes =
        (1u << ActorPostRouteCode.QueuedGrowPostableStamp) |
        (1u << ActorPostRouteCode.QueuedRejectNewPostableStamp) |
        (1u << ActorPostRouteCode.QueuedDropOldestPostableStamp);

    public const uint UncheckedRoutes =
        (1u << ActorPostRouteCode.QueuedGrowUnchecked) |
        (1u << ActorPostRouteCode.QueuedRejectNewUnchecked) |
        (1u << ActorPostRouteCode.QueuedDropOldestUnchecked);
}
```

集合判断：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static bool IsRouteInMask(
    byte routeCode,
    uint mask)
{
    // routeCode 参数作用：
    // 当前事件的路线编码。
    // 该值必须小于 32，因为当前实现使用 uint 的 32 个 bit 表示路线集合。

    // mask 参数作用：
    // 路线集合位图。
    // 某一位为 1 表示该 routeCode 属于该集合。

    return ((mask >> routeCode) & 1u) != 0;
}
```

使用原则：

```text
单个默认路线判断使用 routeCode == 常量。
多个路线归类使用 bit mask。
不要用 bit mask 替代所有函数选择。
```

---

## 5. EventPostState

```csharp
internal sealed class EventPostState<TEvent>
    where TEvent : struct
{
    public readonly byte RouteCode;
    public readonly EventMailPool<TEvent> Pool;
    public readonly ActorMailOptions Options;
    public readonly EventPostRow<TEvent>[] RowsByArchetype;

    public EventPostState(
        byte routeCode,
        EventMailPool<TEvent> pool,
        ActorMailOptions options,
        EventPostRow<TEvent>[] rowsByArchetype)
    {
        // routeCode 参数作用：
        // 构建期编译出的 ActorPost 路线编码。
        // 低位表示写入模式，高位表示校验模式。
        // 运行期通过一次相等判断或位运算决定进入哪条投递路径。

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

## 6. EventPostRow

```csharp
internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;
    public readonly DirtySlotList DirtySlots;
    public readonly int BucketIndex;
    public readonly int[]? PostableGenerations;

    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[]? postableGenerations)
    {
        // mails 参数作用：
        // 当前 Archetype + TEvent 的邮箱数组。
        // Post 路径通过 actorId.SlotIndex 定位目标 Actor 的事件邮箱。

        // dirtySlots 参数作用：
        // 当前事件列的脏 slot 列表。
        // 当目标 Actor 邮箱从空变为非空时，将 slotIndex 写入该列表。

        // bucketIndex 参数作用：
        // 当前事件列在 ActorWorld DirtyBucketList 中的 bucket 下标。
        // PostCore 写入新消息后，通过它标记对应事件 bucket 为 dirty。

        // postableGenerations 参数作用：
        // 当前事件可选的可投递代际表。
        // PostableStamp 路径用它把多状态检查压成一次整数比较。
        // PhysicalSafe 和 Unchecked 路径可以为 null。

        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        PostableGenerations = postableGenerations;
    }
}
```

要求：

```text
EventPostRow 不持有 EventMailPool<TEvent>。
EventPostRow 不持有 ActorMailOptions。
EventPostRow 不执行策略判断。
```

---

## 7. InvalidRow 哨兵

```csharp
internal static class EventPostRowSentinel<TEvent>
    where TEvent : struct
{
    public static readonly EventPostRow<TEvent> Invalid =
        new(
            mails: Array.Empty<EventMail<TEvent>>(),
            dirtySlots: DirtySlotList.Empty,
            bucketIndex: -1,
            postableGenerations: null);
}
```

创建 Rows：

```csharp
private static EventPostRow<TEvent>[] CreateRows<TEvent>(
    int archetypeCapacity)
    where TEvent : struct
{
    // archetypeCapacity 参数作用：
    // ActorWorld 当前可容纳的 Archetype 数量。
    // RowsByArchetype 需要至少覆盖所有已存在 ArchetypeId。

    var rows = new EventPostRow<TEvent>[
        Math.Max(archetypeCapacity, 1)];

    for (int i = 0; i < rows.Length; i++)
    {
        // 逻辑说明：
        // 未注册当前 TEvent 的 Archetype 使用 InvalidRow。
        // InvalidRow 的 Mails 长度为 0。
        // Post 热路径不需要 row.IsValid 分支，slot 范围检查会自然失败。
        rows[i] = EventPostRowSentinel<TEvent>.Invalid;
    }

    return rows;
}
```

---

## 8. StructuralDirty

### 8.1 Dirty Flags

```csharp
[Flags]
internal enum ActorStructuralDirtyFlags : byte
{
    None = 0,

    /// <summary>
    /// Actor 被标记为待销毁。
    /// </summary>
    PendingDestroy = 1 << 0,

    /// <summary>
    /// Actor Enable 状态发生变化。
    /// </summary>
    EnableChanged = 1 << 1,

    /// <summary>
    /// Actor slot 等待回收或复用。
    /// </summary>
    SlotRecycle = 1 << 2,

    /// <summary>
    /// Query 缓存需要刷新。
    /// </summary>
    QueryInvalidated = 1 << 3
}
```

---

### 8.2 TypedActorStorage 新增字段

```csharp
internal sealed class TypedActorStorage<TActor> : TypedStorageRuntime
    where TActor : class, IActor
{
    private ActorStructuralDirtyFlags[] _structuralDirtyFlags;

    private int[] _alivePostGenerations;

    private int[] _enabledPostGenerations;

    internal int[] AlivePostGenerations => _alivePostGenerations;

    internal int[] EnabledPostGenerations => _enabledPostGenerations;
}
```

字段说明：

```text
_structuralDirtyFlags:
  记录每个 slot 的结构变化。
  Sweep 阶段根据它统一处理结构更新。

_alivePostGenerations:
  Actor 处于可投递 Alive 状态时等于 generation。
  否则为 0。

_enabledPostGenerations:
  Actor 处于 Alive 且 Enabled 状态时等于 generation。
  否则为 0。
```

---

## 9. PostableGeneration 更新规则

```csharp
internal void RefreshPostGenerations(
    int slotIndex)
{
    // slotIndex 参数作用：
    // 当前需要刷新可投递代际的 Actor slot。
    // 该方法只更新单个 slot，不扫描整个 Storage。

    ActorSlotFlags flags = _slotFlags[slotIndex];
    int generation = _generations[slotIndex];

    bool alivePostable =
        (flags & ActorSlotFlags.Alive) != 0 &&
        (flags & ActorSlotFlags.PendingDestroy) == 0 &&
        (flags & ActorSlotFlags.Destroying) == 0;

    // 逻辑说明：
    // 如果 Actor 当前处于可投递 Alive 状态，
    // alivePostGenerations[slotIndex] 写入当前 generation。
    // 否则写 0，使 PostableStamp 路径的一次整数比较失败。
    _alivePostGenerations[slotIndex] =
        alivePostable ? generation : 0;

    bool enabledPostable =
        alivePostable &&
        (flags & ActorSlotFlags.Enabled) != 0;

    // 逻辑说明：
    // 如果事件要求 Disabled Actor 不接收消息，
    // Row 会绑定 enabledPostGenerations。
    _enabledPostGenerations[slotIndex] =
        enabledPostable ? generation : 0;
}
```

必须调用的位置：

```text
AllocateSlot
SetEnable
MarkPendingDestroy
EnterDestroying
SweepPendingDestroy
ReuseSlot
ReturnToPool
```

---

## 10. 构建 Row 时选择 PostableGeneration

```csharp
private static int[]? ResolvePostableGenerations<TActor, TEvent>(
    TypedActorStorage<TActor> storage,
    ActorEventPostPlan<TEvent> plan)
    where TActor : class, IActor
    where TEvent : struct
{
    // storage 参数作用：
    // 当前 Archetype 对应的强类型 Actor 存储。
    // 它提供 AlivePostGenerations 和 EnabledPostGenerations。

    // plan 参数作用：
    // 当前事件的构建期投递计划。
    // 它决定该事件是否需要 PostableStamp，以及 Disabled Actor 是否可接收消息。

    if (!plan.RequirePostableStamp)
    {
        return null;
    }

    if (plan.RejectDisabled)
    {
        return storage.EnabledPostGenerations;
    }

    return storage.AlivePostGenerations;
}
```

---

## 11. ActorEventPostPlan

```csharp
internal readonly struct ActorEventPostPlan<TEvent>
    where TEvent : struct
{
    public readonly int EventId;
    public readonly byte RouteCode;
    public readonly ActorMailOptions MailOptions;
    public readonly bool RequirePostableStamp;
    public readonly bool RejectDisabled;

    public ActorEventPostPlan(
        int eventId,
        byte routeCode,
        ActorMailOptions mailOptions,
        bool requirePostableStamp,
        bool rejectDisabled)
    {
        // eventId 参数作用：
        // 当前事件类型 ID。
        // 用于注册 EventPostState 和事件诊断统计。

        // routeCode 参数作用：
        // 当前事件构建期编译出的投递路线编码。
        // 运行期直接读取该 byte，避免复杂策略对象和动态 switch。

        // mailOptions 参数作用：
        // 当前事件最终邮箱配置。
        // 用于创建 EventMailPool<TEvent> 和执行邮箱写入逻辑。

        // requirePostableStamp 参数作用：
        // 表示该事件是否需要逻辑可投递检查。
        // true 时 Row 必须绑定 PostableGenerations。

        // rejectDisabled 参数作用：
        // 表示 Disabled Actor 是否拒绝该事件。
        // true 时 Row 绑定 EnabledPostGenerations。
        // false 时 Row 绑定 AlivePostGenerations。

        EventId = eventId;
        RouteCode = routeCode;
        MailOptions = mailOptions;
        RequirePostableStamp = requirePostableStamp;
        RejectDisabled = rejectDisabled;
    }
}
```

---

## 12. ActorEventPostPlanBuilder

```csharp
internal static class ActorEventPostPlanBuilder
{
    public static ActorEventPostPlan<TEvent> Build<TEvent>(
        ActorMailOptions worldDefaultMailOptions)
        where TEvent : struct
    {
        // worldDefaultMailOptions 参数作用：
        // ActorWorld 默认邮箱配置。
        // 当 EventMetaData<TEvent> 没有提供 ActorMailOptions 时，用它作为兜底配置。

        EventMetaData<TEvent> metaData =
            EventMetaDataHandler.GetOrCreate<TEvent>();

        ActorMailOptions options =
            metaData.GetActorMailOptions() ?? worldDefaultMailOptions;

        bool rejectDisabled =
            options.DisabledPolicy == ActorMailDisabledPolicy.Reject;

        bool requirePostableStamp =
            ResolveRequirePostableStamp(options);

        byte routeCode =
            ResolveRouteCode(
                options,
                requirePostableStamp);

        return new ActorEventPostPlan<TEvent>(
            eventId: metaData.EventId,
            routeCode: routeCode,
            mailOptions: options,
            requirePostableStamp: requirePostableStamp,
            rejectDisabled: rejectDisabled);
    }

    private static bool ResolveRequirePostableStamp(
        ActorMailOptions options)
    {
        // options 参数作用：
        // 当前事件最终邮箱配置。
        // 用于决定该事件是否需要逻辑可投递检查。

        return options.DisabledPolicy == ActorMailDisabledPolicy.Reject;
    }

    private static byte ResolveRouteCode(
        ActorMailOptions options,
        bool requirePostableStamp)
    {
        // options 参数作用：
        // 当前事件最终邮箱配置。
        // 用于判断写入策略。

        // requirePostableStamp 参数作用：
        // 表示该事件是否需要 PostableGeneration 检查。
        // 这个选择会被编译进 routeCode，运行期不再读取 ValidationMode。

        byte validation = requirePostableStamp
            ? ActorPostRouteCode.ValidationPostableStamp
            : ActorPostRouteCode.ValidationPhysicalSafe;

        byte writeMode = options.PostPolicy switch
        {
            ActorPostPolicy.Queued
                when options.FullPolicy == ActorMailFullPolicy.Grow
                => ActorPostRouteCode.WriteQueuedGrow,

            ActorPostPolicy.Queued
                when options.FullPolicy == ActorMailFullPolicy.RejectNew
                => ActorPostRouteCode.WriteQueuedRejectNew,

            ActorPostPolicy.Queued
                when options.FullPolicy == ActorMailFullPolicy.DropOldest
                => ActorPostRouteCode.WriteQueuedDropOldest,

            ActorPostPolicy.Latest
                => ActorPostRouteCode.WriteLatest,

            ActorPostPolicy.Dirty
                => ActorPostRouteCode.WriteDirty,

            _ => ActorPostRouteCode.WriteDisabled
        };

        if (writeMode == ActorPostRouteCode.WriteDisabled)
        {
            return ActorPostRouteCode.Disabled;
        }

        return (byte)(writeMode | validation);
    }
}
```

---

## 13. EventPostRuntime

```csharp
internal static class EventPostRuntime<TEvent>
    where TEvent : struct
{
    private static EventPostState<TEvent>?[] s_statesByWorld =
        new EventPostState<TEvent>?[4];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventPostState<TEvent>? GetStateUnchecked(
        int worldIndex)
    {
        // worldIndex 参数作用：
        // ActorWorld 的运行期索引。
        // 构建期应保证 s_statesByWorld 能容纳该索引。
        // 该方法用于 Post 热路径，避免额外世界对象访问。

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
        // 使用 world.RuntimeIndex 作为泛型静态状态表下标。

        // state 参数作用：
        // 当前 ActorWorld + TEvent 的编译后投递状态。

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
        // ActorWorld Dispose 时调用，防止静态数组继续持有旧状态。

        if ((uint)worldIndex < (uint)s_statesByWorld.Length)
        {
            s_statesByWorld[worldIndex] = null;
        }
    }

    private static void Resize(
        int worldIndex)
    {
        // worldIndex 参数作用：
        // 当前需要容纳的最大 world 索引。
        // 如果数组长度不够，则倍增扩容。

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

## 14. ActorWorld.PostTo

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // PhysicalSafe 路径使用 ArchetypeId 和 SlotIndex 定位物理邮箱。
    // PostableStamp 路径还使用 Generation 做一次可投递代际比较。

    // value 参数作用：
    // 要投递给目标 Actor 的事件值。
    // 当前事件策略已经在构建期编译到 RouteCode。

    EventPostState<TEvent>? state =
        EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);

    if (state == null)
    {
        return BuildEventNotSupportedCold<TEvent>();
    }

    byte routeCode = state.RouteCode;

    if (routeCode == ActorPostRouteCode.QueuedGrowPhysicalSafe)
    {
        return PostQueuedGrowPhysicalSafe(
            actorId,
            in value,
            state);
    }

    return PostToNonDefaultCold(
        actorId,
        in value,
        state,
        routeCode);
}
```

说明：

```text
默认路线只做一次相等比较。
非默认路线进入 NoInlining 冷路径。
避免让完整 switch 污染默认路线内联体积。
```

---

## 15. 非默认路线冷路径

```csharp
[MethodImpl(MethodImplOptions.NoInlining)]
private static PostResult PostToNonDefaultCold<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state,
    byte routeCode)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 冷路径根据 routeCode 决定使用 PhysicalSafe 或 PostableStamp。

    // value 参数作用：
    // 要投递的事件值。

    // state 参数作用：
    // 当前事件的编译后投递状态。
    // 包含 RouteCode、RowsByArchetype、Pool 和 Options。

    // routeCode 参数作用：
    // 当前事件路线编码。
    // 低位表示写入模式，高位表示校验模式。

    byte validation =
        (byte)(routeCode & ActorPostRouteCode.ValidationMask);

    byte writeMode =
        (byte)(routeCode & ActorPostRouteCode.WriteModeMask);

    if (validation == ActorPostRouteCode.ValidationPostableStamp)
    {
        return PostByWriteModePostableStampCold(
            actorId,
            in value,
            state,
            writeMode);
    }

    if (validation == ActorPostRouteCode.ValidationPhysicalSafe)
    {
        return PostByWriteModePhysicalSafeCold(
            actorId,
            in value,
            state,
            writeMode);
    }

    return BuildRouteUnsupportedCold<TEvent>();
}
```

---

## 16. 写入模式冷路径选择

```csharp
[MethodImpl(MethodImplOptions.NoInlining)]
private static PostResult PostByWriteModePhysicalSafeCold<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state,
    byte writeMode)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。

    // value 参数作用：
    // 要写入邮箱的事件值。

    // state 参数作用：
    // 当前事件的编译后状态。

    // writeMode 参数作用：
    // 从 RouteCode 低位拆出的写入模式。
    // 只决定邮箱满时如何处理、是否覆盖旧值等写入行为。

    return writeMode switch
    {
        ActorPostRouteCode.WriteQueuedGrow =>
            PostQueuedGrowPhysicalSafe(actorId, in value, state),

        ActorPostRouteCode.WriteQueuedRejectNew =>
            PostQueuedRejectNewPhysicalSafe(actorId, in value, state),

        ActorPostRouteCode.WriteQueuedDropOldest =>
            PostQueuedDropOldestPhysicalSafe(actorId, in value, state),

        ActorPostRouteCode.WriteLatest =>
            PostLatestPhysicalSafe(actorId, in value, state),

        ActorPostRouteCode.WriteDirty =>
            PostDirtyPhysicalSafe(actorId, in value, state),

        _ =>
            BuildRouteUnsupportedCold<TEvent>()
    };
}
```

说明：

```text
switch 只存在冷路径。
默认 QueuedGrowPhysicalSafe 不进入该 switch。
```

---

## 17. PhysicalSafe 路径

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static PostResult PostQueuedGrowPhysicalSafe<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 本路径只使用 ArchetypeId 和 SlotIndex 进行物理邮箱定位。
    // 不检查 Actor 生命周期状态。

    // value 参数作用：
    // 要写入目标邮箱的事件值。

    // state 参数作用：
    // 当前 ActorWorld + TEvent 的编译后状态。
    // 保存 RowsByArchetype、Pool 和 Options。

    EventPostRow<TEvent>[] rows = state.RowsByArchetype;

    int archetypeId = actorId.ArchetypeId;

    if ((uint)archetypeId >= (uint)rows.Length)
    {
        return BuildPostFailureCold(actorId);
    }

    EventPostRow<TEvent> row = rows[archetypeId];

    EventMail<TEvent>[] mails = row.Mails;
    int slotIndex = actorId.SlotIndex;

    if ((uint)slotIndex >= (uint)mails.Length)
    {
        return BuildPostFailureCold(actorId);
    }

    return PostQueuedGrowCore(
        slotIndex,
        in value,
        mails,
        row.DirtySlots,
        row.BucketIndex,
        state.Pool,
        state.Options);
}
```

---

## 18. PostableStamp 路径

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static PostResult PostQueuedGrowPostableStamp<TEvent>(
    ActorId actorId,
    in TEvent value,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // actorId 参数作用：
    // 目标 Actor 句柄。
    // 本路径使用 actorId.Generation 和 PostableGenerations 做一次整数比较，
    // 从而完成 ActorId 过期、Alive、PendingDestroy、Destroying、Disabled 等折叠检查。

    // value 参数作用：
    // 要写入目标邮箱的事件值。

    // state 参数作用：
    // 当前 ActorWorld + TEvent 的编译后状态。
    // 保存 RowsByArchetype、Pool 和 Options。

    EventPostRow<TEvent>[] rows = state.RowsByArchetype;

    int archetypeId = actorId.ArchetypeId;

    if ((uint)archetypeId >= (uint)rows.Length)
    {
        return BuildPostFailureCold(actorId);
    }

    EventPostRow<TEvent> row = rows[archetypeId];

    EventMail<TEvent>[] mails = row.Mails;
    int slotIndex = actorId.SlotIndex;

    if ((uint)slotIndex >= (uint)mails.Length)
    {
        return BuildPostFailureCold(actorId);
    }

    int[] postableGenerations = row.PostableGenerations!;

    if (postableGenerations[slotIndex] != actorId.Generation)
    {
        return BuildPostFailureCold(actorId);
    }

    return PostQueuedGrowCore(
        slotIndex,
        in value,
        mails,
        row.DirtySlots,
        row.BucketIndex,
        state.Pool,
        state.Options);
}
```

---

## 19. Unchecked 路径

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void PostQueuedGrowUnchecked<TEvent>(
    int slotIndex,
    in TEvent value,
    EventPostRow<TEvent> row,
    EventPostState<TEvent> state)
    where TEvent : struct
{
    // slotIndex 参数作用：
    // 框架内部枚举出的 slot 下标。
    // 调用方必须保证它在 row.Mails 范围内。

    // value 参数作用：
    // 要写入目标邮箱的事件值。

    // row 参数作用：
    // 当前 Archetype + TEvent 的邮箱定位信息。
    // 调用方必须保证该 Row 支持当前事件。

    // state 参数作用：
    // 当前 ActorWorld + TEvent 的编译后状态。
    // 提供 Pool 和 Options。

    _ = PostQueuedGrowCore(
        slotIndex,
        in value,
        row.Mails,
        row.DirtySlots,
        row.BucketIndex,
        state.Pool,
        state.Options);
}
```

适用：

```text
ActorQueryResult.PostAll。
框架内部确定安全的批量投递。
源生成器生成的受控代码。
```

禁止：

```text
公开 ActorWorld.PostTo。
外部传入 ActorId 的路径。
```

---

## 20. Query.PostAll

```csharp
public static void PostAll<TEvent>(
    this ActorQueryResult query,
    in TEvent value)
    where TEvent : struct
{
    // query 参数作用：
    // Actor 查询结果。
    // 它由框架维护，包含命中的 Archetype 和 slot 枚举信息。

    // value 参数作用：
    // 要批量投递给查询结果中 Actor 的事件值。

    EventPostState<TEvent>? state =
        EventPostRuntime<TEvent>.GetStateUnchecked(query.World.RuntimeIndex);

    if (state == null)
    {
        return;
    }

    byte routeCode = state.RouteCode;

    if (ActorPostRouteUtils.IsRouteInMask(
            routeCode,
            ActorPostRouteMasks.QueuedRoutes))
    {
        PostAllQueuedByRouteCode(
            query,
            in value,
            state,
            routeCode);
        return;
    }

    PostAllNonQueuedCold(
        query,
        in value,
        state,
        routeCode);
}
```

批量 Queued 分发：

```csharp
private static void PostAllQueuedByRouteCode<TEvent>(
    ActorQueryResult query,
    in TEvent value,
    EventPostState<TEvent> state,
    byte routeCode)
    where TEvent : struct
{
    // query 参数作用：
    // Actor 查询结果。
    // 本方法只处理 Queued 系路线。

    // value 参数作用：
    // 要批量写入邮箱的事件值。

    // state 参数作用：
    // 当前事件的编译后状态。

    // routeCode 参数作用：
    // 当前事件路线编码。
    // 用于提取写入模式。

    byte writeMode =
        (byte)(routeCode & ActorPostRouteCode.WriteModeMask);

    switch (writeMode)
    {
        case ActorPostRouteCode.WriteQueuedGrow:
            PostAllQueuedGrowUnchecked(query, in value, state);
            break;

        case ActorPostRouteCode.WriteQueuedRejectNew:
            PostAllQueuedRejectNewUnchecked(query, in value, state);
            break;

        case ActorPostRouteCode.WriteQueuedDropOldest:
            PostAllQueuedDropOldestUnchecked(query, in value, state);
            break;
    }
}
```

要求：

```text
PostAll 不逐 Actor 调用公开 PostTo。
PostAll 在循环外判断 route。
PostAll 内部循环使用 unchecked 写入。
```

---

## 21. Pump / Sweep 规则

### 21.1 Pump 前整理

推荐在 Pump 开头执行：

```text
SweepPendingDestroy
FlushStructuralDirty
RefreshQueryCacheIfNeeded
```

效果：

```text
PendingDestroy Actor 不再执行消息。
PendingDestroy Actor 的邮箱统一清理。
Query.PostAll 枚举更稳定。
```

---

### 21.2 Pump 消费保护

```csharp
private bool CanPumpSlot(
    int slotIndex)
{
    // slotIndex 参数作用：
    // 当前准备消费邮箱的 Actor slot。
    // Pump 阶段用它判断该 Actor 是否还能执行 handler。

    ActorSlotFlags flags = _slotFlags[slotIndex];

    return (flags & ActorSlotFlags.Alive) != 0 &&
           (flags & ActorSlotFlags.PendingDestroy) == 0 &&
           (flags & ActorSlotFlags.Destroying) == 0;
}
```

说明：

```text
Post 只负责写入。
Pump 决定消息是否执行。
Sweep 负责清理不会执行的消息。
```

---

### 21.3 Sweep 清理

```csharp
internal void SweepPendingDestroy(
    ActorWorld world)
{
    // world 参数作用：
    // 当前 ActorWorld。
    // Sweep 需要通过它访问事件列、生命周期调度器和 Query 刷新入口。

    for (int slotIndex = 0; slotIndex < _states.Length; slotIndex++)
    {
        if ((_structuralDirtyFlags[slotIndex] & ActorStructuralDirtyFlags.PendingDestroy) == 0)
        {
            continue;
        }

        // 逻辑说明：
        // PendingDestroy 的 Actor 不再执行邮箱消息。
        // 在 slot 回收前，必须释放该 slot 的所有事件邮箱。
        ClearAllMails(slotIndex);

        // 逻辑说明：
        // 结构更新集中在 Sweep 阶段完成，避免污染 Post 热路径。
        FinalizeDestroySlot(slotIndex);

        RefreshPostGenerations(slotIndex);

        _structuralDirtyFlags[slotIndex] = ActorStructuralDirtyFlags.None;
    }
}
```

---

## 22. PostResult 语义

保留 `PostResult`。

但语义改为：

```text
Success:
  成功写入邮箱。

EventNotSupported:
  当前事件没有构建 EventPostState。

PhysicalTargetInvalid:
  ActorId 无法定位到当前物理邮箱。

RejectedByPostableStamp:
  PostableGeneration 比较失败。

MailFullRejected:
  邮箱满且策略拒绝新消息。
```

不再表示：

```text
Actor 一定会执行消息。
Actor 当前一定 Alive。
Actor 当前没有 PendingDestroy。
```

---

## 23. 禁止项

Post 热路径禁止出现：

```text
EventMetaData<TEvent> 读取。
EventRuntimePolicyTable 读取。
ActorPostPolicy? 参数。
ActorMailFullPolicy? 参数。
ActorSlotFlags 多条件判断。
DisabledPolicy 运行期判断。
delegate handler array。
interface strategy dispatch。
每次 Post 完整 route switch。
旧 TryPostToSafe fallback。
旧 BehaviourArchetype.Post fallback。
旧 EventColumn.Post fallback。
```

允许出现：

```text
EventPostRuntime<TEvent>.GetStateUnchecked。
routeCode == ActorPostRouteCode.QueuedGrowPhysicalSafe。
RowsByArchetype 数组读取。
Mails 数组范围检查。
PostableGenerations 一次整数比较。
PostCore。
冷路径 switch。
```

---

## 24. Benchmark 要求

新增或保留：

```text
ActorPost_RouteCode_QueuedGrow_PhysicalSafe_OneActor
ActorPost_RouteCode_QueuedGrow_PostableStamp_OneActor
ActorPost_RouteCode_QueuedGrow_Unchecked_OneActor
ActorPost_RouteCode_QueuedRejectNew_PhysicalSafe_OneActor
ActorPost_RouteCode_QueuedDropOldest_PhysicalSafe_OneActor
ActorPost_Query_PostAll_RouteMask_Unchecked_1000Actors
ActorPost_Query_PostAll_RouteMask_Unchecked_1000Actors_12Events
SweepPendingDestroy_1000Actors_WithMailbox
RefreshPostGenerations_1000Actors
```

对比：

```text
旧 PostFast
旧 PostTo
旧 ArchetypeRow
Dictionary Lookup
```

目标：

```text
QueuedGrow_PhysicalSafe:
  尽量接近旧 PostFast。

QueuedGrow_PostableStamp:
  比 PhysicalSafe 多一次数组读取和整数比较。

Unchecked:
  作为 Query/PostAll 极限路径，应低于公开 PostTo。

RouteCode:
  默认路线不进入完整 switch。
```

---

## 25. 落地顺序

### Step 1：RouteCode 替换 Route enum

```text
新增 ActorPostRouteCode。
新增 ActorPostRouteMasks。
EventPostState 保存 byte RouteCode。
ActorEventPostPlan 保存 byte RouteCode。
```

---

### Step 2：PostTo 主路径改造

```text
QueuedGrowPhysicalSafe 使用裸 if。
非默认路线进入 NoInlining 冷路径。
冷路径按 Validation 和 WriteMode 拆分。
```

---

### Step 3：StructuralDirty 接入

```text
新增 ActorStructuralDirtyFlags。
Destroy / Enable / Disable 只标记结构脏。
Sweep 阶段统一整理。
```

---

### Step 4：PostableGeneration 接入

```text
新增 AlivePostGenerations。
新增 EnabledPostGenerations。
状态变化时刷新单 slot。
Row 构建期选择是否绑定 PostableGenerations。
```

---

### Step 5：Query.PostAll unchecked

```text
PostAll 不调用公开 PostTo。
PostAll 使用 route mask 分类。
PostAll 在循环外判断写入模式。
PostAll 内部循环使用 unchecked Core。
```

---

## 26. 最终结论

最终 ActorPost 热路径应接近：

```text
GetStateUnchecked
state null 冷路径
routeCode 默认路线相等判断
RowsByArchetype[archetypeId]
Mails[slotIndex]
可选 PostableGeneration 一次整数比较
PostCore
```

设计原则：

```text
Route 选择用 RouteCode，不用运行期策略对象。
默认路线用裸 if，不用完整 switch。
多路线归类用 bit mask。
函数选择保持直调，不用 delegate 数组。
Actor 生命周期变化延迟整理，不污染 Post 热路径。
必要状态检查用 PostableGeneration 压缩。
Query 批量路径使用 unchecked 写入。
```
