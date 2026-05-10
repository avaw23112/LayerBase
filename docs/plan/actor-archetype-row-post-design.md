# Actor Archetype Row Post Path Design

## 1. 设计目标

本设计用于评估并落地一套更短的 Actor Post 热路径。核心目标是把 Post 从“Actor + Event 级缓存”推进到“Archetype + Event 级缓存”。

目标路径：

```text
PostFast<TEvent>
  -> EventPostRuntime<TEvent>.RowsByArchetype[actorId.ArchetypeId]
  -> row.Mails[actorId.SlotIndex]
  -> row.Pool.Write(...)
  -> row.DirtySlots.Mark(...)
  -> DirtyBucketList.Mark(...)
```

约束：

1. Post 路径只依赖 `TEvent` 泛型，不依赖 `TActor`。
2. Pump 路径保留 `TActor + TEvent` 强类型。
3. 不使用反射、`MethodInfo`、`MakeGenericMethod`。
4. 不使用 Dictionary 作为热路径路由。
5. 不在 Post 热路径经过 `BehaviourArchetype.Post` 或 `TypedStorageRuntime.Post<TEvent>`。
6. 不在 Post 热路径检查 `disabled / pending / destroying / IsAlive`。
7. 不在 Post 热路径做 `EventId -> ColumnIndex` 转换。
8. 保留 public `PostTo<TEvent>` 兼容层，但新增 internal `bool PostFast<TEvent>` 作为极致路径。

---

## 2. 关键定义

### 2.1 Archetype

本方案中的 Archetype 不是单纯的行为集合，而是：

```text
Archetype = ConcreteActorType + BehaviourLayout
```

也就是说，同一个 Archetype 内必须满足：

1. Actor 具体类型相同。
2. 行为布局相同。
3. 事件列布局相同。
4. slot 空间一致。

不建议使用 `IActor[]` 作为 Archetype 的 Actor 存储。极致路径应使用：

```text
ArchetypeRuntime<TActor>
  -> TActor[] Actors
```

原因：`IActor[]` 会让 Pump 时重新 cast 回 `TActor`，而 `TActor[]` 可以让 Pump 在闭合泛型里直接调用 `ActorBehaviourInvoker<TActor,TEvent>`。

### 2.2 EventPostRow<TEvent>

`EventPostRow<TEvent>` 是 Post 热路径视图。

它只保存写邮箱需要的数据：

```text
EventMail<TEvent>[]
EventMailPool<TEvent>
DirtySlotList
BucketIndex
Generations[]
```

它不保存：

```text
TActor
IActor
Invoker
EventColumnRuntime
```

因为 Post 只负责写入事件邮箱，Pump 才需要 Actor 类型与 invoker。

---

## 3. 总体结构

```text
ActorWorld
  ├─ ArchetypeRuntime[] Archetypes
  ├─ DirtyBucketList DirtyBuckets
  └─ EventPostRuntime<TEvent>

ArchetypeRuntime<TActor>
  ├─ TActor[] Actors
  ├─ int[] Generations
  ├─ int[] EventIdToColumnIndex
  ├─ EventColumnRuntime[] Columns
  └─ EventColumn<TActor,TEvent> typed columns

EventColumn<TActor,TEvent>
  ├─ EventMail<TEvent>[] Mails
  ├─ EventMailPool<TEvent> Pool
  ├─ DirtySlotList DirtySlots
  ├─ int BucketIndex
  └─ ActorBehaviourInvoker<TActor,TEvent> Invoker

EventPostRuntime<TEvent>
  └─ EventPostRow<TEvent>[] RowsByArchetype
```

---

## 4. ActorId

极致 Post 路径只需要 `ArchetypeId`、`SlotIndex`、`Generation`。

```csharp
public readonly struct ActorId
{
    public readonly int ArchetypeId;
    public readonly int SlotIndex;
    public readonly int Generation;

    /// <param name="archetypeId">
    /// Actor 所属 Archetype 的编号。
    /// 作用：PostFast<TEvent> 用它直接访问 EventPostRuntime<TEvent>.RowsByArchetype。
    /// </param>
    /// <param name="slotIndex">
    /// Actor 在 ArchetypeRuntime<TActor> 内部数组中的槽位。
    /// 作用：PostFast<TEvent> 用它直接访问 row.Mails[slotIndex]。
    /// </param>
    /// <param name="generation">
    /// Actor slot 生命周期代际。
    /// 作用：防止旧 ActorId 命中复用后的新 Actor。
    /// </param>
    public ActorId(int archetypeId, int slotIndex, int generation)
    {
        ArchetypeId = archetypeId;
        SlotIndex = slotIndex;
        Generation = generation;
    }
}
```

可以保留旧字段用于 SafePath 兼容，但 `PostFast<TEvent>` 不依赖它们。

---

## 5. EventPostRow<TEvent>

```csharp
internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    public readonly EventMail<TEvent>[] Mails;
    public readonly EventMailPool<TEvent> Pool;
    public readonly DirtySlotList DirtySlots;
    public readonly int BucketIndex;
    public readonly int[] Generations;

    /// <param name="mails">
    /// 当前 Archetype + TEvent 对应的邮箱数组。
    /// 作用：PostFast<TEvent> 通过 mails[slotIndex] 直接定位目标 Actor 的事件邮箱。
    /// </param>
    /// <param name="pool">
    /// 当前 ActorWorld + TEvent 对应的邮箱池。
    /// 作用：提供 TEvent 环状队列 buffer 的租用、写入、增长、释放能力。
    /// </param>
    /// <param name="dirtySlots">
    /// 当前 EventColumn<TActor,TEvent> 的脏 slot 列表。
    /// 作用：当某个 Actor 的邮箱从空变为非空时，将 slotIndex 标记为待 Pump。
    /// </param>
    /// <param name="bucketIndex">
    /// 当前 EventColumn<TActor,TEvent> 在 DirtyBucketList 中的下标。
    /// 作用：当该列出现待处理消息时，将 bucketIndex 标记为待 Pump。
    /// </param>
    /// <param name="generations">
    /// 当前 Archetype 内每个 slot 的生命周期代际数组。
    /// 作用：PostFast<TEvent> 使用 generations[slotIndex] 与 actorId.Generation 做轻量校验。
    /// </param>
    public EventPostRow(
        EventMail<TEvent>[] mails,
        EventMailPool<TEvent> pool,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[] generations)
    {
        Mails = mails;
        Pool = pool;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
        Generations = generations;
    }

    public bool IsValid => Mails != null;
}
```

---

## 6. EventPostRuntime<TEvent>

`EventPostRuntime<TEvent>` 是每个事件类型的泛型静态运行时表。它通过 `ActorWorld.RuntimeIndex` 支持多 World。

```csharp
internal static class EventPostRuntime<TEvent>
    where TEvent : struct
{
    private static EventPostRow<TEvent>[][] s_rowsByWorld = new EventPostRow<TEvent>[4][];

    /// <summary>
    /// 绑定指定 World 的 RowsByArchetype 表。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    /// 作用：使用 world.RuntimeIndex 定位当前 World 对应的泛型静态行表。
    /// </param>
    /// <param name="rows">
    /// 当前 World 内 TEvent 对应的 Archetype 行表。
    /// 作用：PostFast<TEvent> 使用 rows[actorId.ArchetypeId] 取得 EventPostRow<TEvent>。
    /// </param>
    public static void BindWorld(ActorWorld world, EventPostRow<TEvent>[] rows)
    {
        int worldIndex = world.RuntimeIndex;
        EnsureWorldCapacity(worldIndex);
        s_rowsByWorld[worldIndex] = rows;
    }

    /// <summary>
    /// 获取当前 World 中 TEvent 的 Archetype 行表。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    /// 作用：只读取 world.RuntimeIndex，不进行 Dictionary 查找。
    /// </param>
    public static EventPostRow<TEvent>[] GetRows(ActorWorld world)
    {
        return s_rowsByWorld[world.RuntimeIndex];
    }

    private static void EnsureWorldCapacity(int worldIndex)
    {
        if ((uint)worldIndex < (uint)s_rowsByWorld.Length)
        {
            return;
        }

        int newSize = s_rowsByWorld.Length;
        while (newSize <= worldIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref s_rowsByWorld, newSize);
    }
}
```

---

## 7. ArchetypeRuntime<TActor>

```csharp
internal sealed class ArchetypeRuntime<TActor> : ArchetypeRuntime
    where TActor : class, IActor
{
    private TActor?[] _actors;
    private int[] _generations;
    private EventColumnRuntime[] _columns;
    private int[] _eventIdToColumnIndex;

    public TActor?[] Actors => _actors;
    public int[] Generations => _generations;
    public EventColumnRuntime[] Columns => _columns;
    public int[] EventIdToColumnIndex => _eventIdToColumnIndex;

    /// <param name="initialCapacity">
    /// 初始 Actor 槽位容量。
    /// 作用：初始化 Actors、Generations、EventMail 等列式数组。
    /// </param>
    /// <param name="maxEventId">
    /// 当前 World 已知最大 EventId。
    /// 作用：初始化 eventId -> columnIndex 的数组映射。
    /// </param>
    public ArchetypeRuntime(int initialCapacity, int maxEventId)
    {
        int capacity = Math.Max(initialCapacity, 4);
        _actors = new TActor[capacity];
        _generations = new int[capacity];
        _columns = Array.Empty<EventColumnRuntime>();

        _eventIdToColumnIndex = new int[Math.Max(maxEventId + 1, 4)];
        Array.Fill(_eventIdToColumnIndex, -1);
    }

    public EventColumn<TActor, TEvent> GetRequiredColumn<TEvent>()
        where TEvent : struct
    {
        int eventId = EventMetaData<TEvent>.Id;
        int columnIndex = _eventIdToColumnIndex[eventId];

        if (columnIndex < 0)
        {
            throw new InvalidOperationException(
                $"Event column for {typeof(TEvent).Name} does not exist.");
        }

        return (EventColumn<TActor, TEvent>)_columns[columnIndex];
    }
}
```

注意：`EventIdToColumnIndex` 用于构建期与冷路径。极致 `PostFast<TEvent>` 不应每次使用它。

---

## 8. EventColumn<TActor,TEvent>

```csharp
internal sealed class EventColumn<TActor, TEvent> : EventColumnRuntime
    where TActor : class, IActor
    where TEvent : struct
{
    private readonly ArchetypeRuntime<TActor> _archetype;
    private readonly ActorBehaviourInvoker<TActor, TEvent> _invoker;
    private readonly EventMail<TEvent>[] _mails;
    private readonly EventMailPool<TEvent> _pool;
    private readonly DirtySlotList _dirtySlots;
    private readonly int _bucketIndex;

    public EventMail<TEvent>[] Mails => _mails;
    public EventMailPool<TEvent> Pool => _pool;
    public DirtySlotList DirtySlots => _dirtySlots;
    public int BucketIndex => _bucketIndex;

    /// <param name="archetype">
    /// 当前列所属的强类型 Archetype。
    /// 作用：Pump 时通过 archetype.Actors[slotIndex] 取得 TActor。
    /// </param>
    /// <param name="invoker">
    /// 当前 TActor 收到 TEvent 时执行的静态行为调用器。
    /// 作用：Pump 时调用 invoker(actor, in value)。
    /// </param>
    /// <param name="pool">
    /// 当前 World + TEvent 的邮箱池。
    /// 作用：Post 写入与 Pump 读取都使用同一个 TEvent buffer pool。
    /// </param>
    /// <param name="bucketIndex">
    /// 当前列在 DirtyBucketList 中的下标。
    /// 作用：Post 时标记该列待 Pump。
    /// </param>
    /// <param name="initialSlotCapacity">
    /// 初始 slot 容量。
    /// 作用：初始化每个 Actor slot 对应的 EventMail<TEvent>。
    /// </param>
    public EventColumn(
        ArchetypeRuntime<TActor> archetype,
        ActorBehaviourInvoker<TActor, TEvent> invoker,
        EventMailPool<TEvent> pool,
        int bucketIndex,
        int initialSlotCapacity)
    {
        _archetype = archetype;
        _invoker = invoker;
        _pool = pool;
        _bucketIndex = bucketIndex;
        _mails = new EventMail<TEvent>[Math.Max(initialSlotCapacity, 4)];
        _dirtySlots = new DirtySlotList(initialSlotCapacity);
    }

    public override ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats)
    {
        while (_dirtySlots.TryPop(out int slotIndex))
        {
            TActor? actor = _archetype.Actors[slotIndex];
            if (actor == null)
            {
                ClearMail(slotIndex);
                continue;
            }

            ref EventMail<TEvent> mail = ref _mails[slotIndex];
            if (!EventMailReader.TryDequeue(ref mail, _pool, out TEvent value))
            {
                continue;
            }

            _invoker(actor, in value);
            budget.ConsumeEvent();

            if (mail.Count > 0)
            {
                _dirtySlots.Mark(slotIndex);
            }

            return ActorColumnPumpResult.Processed;
        }

        return ActorColumnPumpResult.NoWork;
    }

    public override void ClearMail(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_mails.Length)
        {
            return;
        }

        ref EventMail<TEvent> mail = ref _mails[slotIndex];
        EventMailReader.ForceRelease(ref mail, _pool);
    }
}
```

Pump 通过 `EventColumn<TActor,TEvent>` 恢复类型，不需要反射，也不需要 `Array[]` 动态转型。

---

## 9. PostFast<TEvent>

```csharp
public sealed partial class ActorWorld
{
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// 作用：JIT 为每个 TEvent 生成闭合泛型路径。
    /// </typeparam>
    /// <param name="actorId">
    /// 目标 ActorId。
    /// 作用：使用 ArchetypeId 定位 EventPostRow<TEvent>，使用 SlotIndex 定位目标邮箱。
    /// </param>
    /// <param name="value">
    /// 要写入的事件值。
    /// 作用：写入目标 Actor 的 TEvent 邮箱。
    /// </param>
    internal bool PostFast<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        EventPostRow<TEvent>[] rows = EventPostRuntime<TEvent>.GetRows(this);

        int archetypeId = actorId.ArchetypeId;
        if ((uint)archetypeId >= (uint)rows.Length)
        {
            return false;
        }

        ref readonly EventPostRow<TEvent> row = ref rows[archetypeId];
        if (!row.IsValid)
        {
            return false;
        }

        int slotIndex = actorId.SlotIndex;
        if ((uint)slotIndex >= (uint)row.Generations.Length)
        {
            return false;
        }

        if (row.Generations[slotIndex] != actorId.Generation)
        {
            return false;
        }

        return PostQueuedGrowFastNoResult(
            slotIndex,
            in value,
            row.Mails,
            row.DirtySlots,
            row.BucketIndex,
            row.Pool);
    }
}
```

---

## 10. PostQueuedGrowFastNoResult<TEvent>

```csharp
private bool PostQueuedGrowFastNoResult<TEvent>(
    int slotIndex,
    in TEvent value,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    EventMailPool<TEvent> pool)
    where TEvent : struct
{
    /// slotIndex 参数作用：Actor 在当前 Archetype 中的槽位。
    /// value 参数作用：要写入邮箱的事件数据。
    /// mails 参数作用：当前 Archetype + TEvent 对应的 EventMail<TEvent>[]。
    /// dirtySlots 参数作用：当前 EventColumn<TActor,TEvent> 的脏 slot 列表。
    /// bucketIndex 参数作用：当前 EventColumn<TActor,TEvent> 在 DirtyBucketList 中的下标。
    /// pool 参数作用：当前 World + TEvent 对应的邮箱池。

    ref EventMail<TEvent> mail = ref mails[slotIndex];

    if (mail.BufferId == 0)
    {
        mail.BufferId = pool.RentInitial();
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 0;
        mail.Capacity = pool.GetCapacity(mail.BufferId);
    }

    if (mail.Count >= mail.Capacity)
    {
        if (!pool.TryGrow(ref mail))
        {
            return false;
        }
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
        _dirtyBuckets.Mark(bucketIndex);
    }

    return true;
}
```

---

## 11. Public PostTo<TEvent> 兼容层

```csharp
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct
{
    /// postPolicy / fullPolicy 参数作用：兼容旧 API。
    /// 只要用户显式传入策略，就不进入极致 FastPath。
    if (postPolicy != null || fullPolicy != null)
    {
        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }

    if (PostFast(actorId, in value))
    {
        return PostResult.Success;
    }

    return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
}
```

---

## 12. 构建期注册 EventPostRow

当 `EventColumn<TActor,TEvent>` 创建完成后，需要把它注册到 `RowsByArchetype[archetypeId]`。

```csharp
internal void RegisterEventPostRow<TActor, TEvent>(
    ActorWorld world,
    int archetypeId,
    ArchetypeRuntime<TActor> archetype,
    EventColumn<TActor, TEvent> column)
    where TActor : class, IActor
    where TEvent : struct
{
    /// world 参数作用：当前 ActorWorld。
    /// archetypeId 参数作用：当前 Archetype 在 ActorWorld 中的编号。
    /// archetype 参数作用：提供 Generations 数组。
    /// column 参数作用：提供 Mails、Pool、DirtySlots、BucketIndex。

    EventPostRow<TEvent>[] rows = GetOrCreateRowsByArchetypeCold<TEvent>(world);
    EnsureRowsCapacity(ref rows, archetypeId);

    rows[archetypeId] = new EventPostRow<TEvent>(
        column.Mails,
        column.Pool,
        column.DirtySlots,
        column.BucketIndex,
        archetype.Generations);

    EventPostRuntime<TEvent>.BindWorld(world, rows);
}
```

`GetOrCreateRowsByArchetypeCold<TEvent>` 是冷路径，可以使用较重逻辑。Post 热路径不能调用它。

---

## 13. DirtyList 要求

`DirtySlotList` 和 `DirtyBucketList` 必须是 O(1) mark 数组。

禁止：

```text
List.Contains
HashSet
Dictionary
foreach 查重
线性扫描查重
```

Post 热路径只允许：

```csharp
dirtySlots.Mark(slotIndex);
_dirtyBuckets.Mark(bucketIndex);
```

---

## 14. 生命周期处理

PostFast 不检查：

```text
disabled
pending destroy
destroying
IsAlive
```

生命周期处理规则：

1. Actor 请求销毁时写入销毁脏位。
2. Actor slot 复用时递增 `Generation`。
3. PostFast 只做 generation 校验。
4. Pump 某 Actor 前再处理 disabled / pending / destroying。
5. 没有脏位时，Pump 不进入批量回收流程。

---

## 15. 与 ActorEventFastCache 方案对比

| 方案 | 缓存粒度 | Post 索引 | 内存占用 | 逐 Actor Post 潜力 |
|---|---|---|---:|---:|
| ActorEventFastCache<TEvent> | Actor + Event | fastIndex | 较高 | 中高 |
| EventPostRow<TEvent> | Archetype + Event | archetypeId + slotIndex | 更低 | 更高 |
| Query.PostAll | Query + Column | slot list | 最低 | 最高 |

新方案价值：

1. 去掉每 Actor 每 Event 的缓存项。
2. 将缓存压缩到每 Archetype 每 Event。
3. PostFast 不再需要 `ActorFastState[]`。
4. 对逐 Actor Post 更短。
5. Pump 仍保留泛型 Column，类型恢复清晰。

---

## 16. 风险与边界

### 16.1 Archetype 必须按具体 Actor 类型拆分

不允许同一个 Archetype 混合多个具体 Actor 类型。否则会退化成：

```text
IActor[] + cast + invokerRef[]
```

这会破坏 Pump 强类型路径。

### 16.2 EventIdToColumnIndex 可能浪费空间

`EventIdToColumnIndex[]` 是空间换时间。

如果 EventId 很稀疏，后续可改成分页数组。第一版不使用 Dictionary。

### 16.3 EventPostRuntime<TEvent> 需要多 World 隔离

通过 `ActorWorld.RuntimeIndex` 实现。World 释放时要回收 RuntimeIndex，并清理对应泛型静态表项。

### 16.4 PostFast 只支持默认 QueuedGrow

如果用户传入自定义 policy，回退 SafePath。

### 16.5 Pump 可暂时保留 virtual

`EventColumnRuntime.PumpOne` 的 virtual 调用不在 Post 热路径。若 Pump 后续成为瓶颈，再用 Source Generator 生成 Pump switch。

---

## 17. 实施顺序

### Phase 1：ArchetypeRuntime<TActor>

1. 将 Archetype 定义为 `ConcreteActorType + BehaviourLayout`。
2. 使用 `TActor[] Actors` 替代 `IActor[]`。
3. 使用 `int[] Generations` 保存 slot 生命周期代际。
4. 保留 `EventIdToColumnIndex[]` 作为构建期映射。

### Phase 2：EventColumn<TActor,TEvent>

1. 每个行为构建一个 `EventColumn<TActor,TEvent>`。
2. Column 持有 `EventMail<TEvent>[]`。
3. Column 持有 `EventMailPool<TEvent>`。
4. Column 持有 `DirtySlotList`。
5. Column 持有 `ActorBehaviourInvoker<TActor,TEvent>`。
6. Column 负责 Pump。

### Phase 3：EventPostRow<TEvent>

1. 构建 Column 后生成 `EventPostRow<TEvent>`。
2. Row 持有 `Mails / Pool / DirtySlots / BucketIndex / Generations`。
3. Row 注册到 `RowsByArchetype[archetypeId]`。

### Phase 4：EventPostRuntime<TEvent>

1. 新增泛型静态 `EventPostRuntime<TEvent>`。
2. 使用 `ActorWorld.RuntimeIndex` 隔离不同 World。
3. PostFast 直接读取 `RowsByArchetype`。

### Phase 5：PostFast<TEvent>

1. 新增 `internal bool PostFast<TEvent>`。
2. 只走 `archetypeId -> row -> mail`。
3. 不走 SafePath。
4. 不构造 PostResult。
5. 不做状态检查。

### Phase 6：Public PostTo<TEvent>

1. public `PostTo<TEvent>` 先尝试 `PostFast<TEvent>`。
2. 失败后回退 SafePath。
3. 显式传入 policy 时直接走 SafePath。

### Phase 7：Query.PostAll 对齐

1. Query 继续走 Column 级裸数组。
2. 多事件单次扫描 slot。
3. 保持当前 Query/PostAll 的高性能优势。

---

## 18. Benchmark 验收

新增或保留：

```text
ActorPost_ArchetypeRow_PostFast_OneActor_OneEvent
ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent
ActorPost_ArchetypeRow_1000Actors_OneEvent
ActorPost_Query_PostAll_1000Actors_OneEvent
ActorPost_Query_PostAll_1000Actors_12Events
ActorPost_DictionaryBaseline_1000Actors_LookupAndEnqueue
```

预期趋势：

```text
ArchetypeRow_PostFast < ActorEventFastCache_PostFast
ArchetypeRow_PostFast < PostTo
Query_PostAll_12Events 仍然最快或接近最快
Cold SafePath 慢于 Hot / PrewarmHot
Dictionary baseline 只作为参考线，不作为唯一目标
```

---

## 19. 最终结论

本方案将 Actor Post 热路径从：

```text
Actor + Event 级缓存
```

升级为：

```text
Archetype + Event 级缓存
```

最终 PostFast 只需要：

```text
TEvent
ActorId.ArchetypeId
ActorId.SlotIndex
ActorId.Generation
```

不需要：

```text
TActor
IActor[]
Dictionary
Reflection
MethodInfo
EventIdToColumnIndex
ActorFastState[]
ActorEventFastCache[fastIndex]
```

最终形态：

```text
Post:
  TEvent -> EventPostRow<TEvent> -> EventMail<TEvent>[]

Pump:
  EventColumn<TActor,TEvent> -> TActor[] -> Invoker
```

这实现：

1. Post 短路径。
2. Post 纯泛型。
3. Pump 类型明确。
4. 无反射。
5. 无字典。
6. 更少内存。
7. 更接近 ECS Archetype 模型。
