# Actor ArchetypeRow Cleanup Final Design

## 1. 结论

本次重构采用激进清理策略：

```text
删除旧 Hot / PrewarmHot / Cold 行为语义。
删除旧 Actor + Event 级缓存体系。
删除旧 per-actor 预热缓存。
删除 Hot 首次绑定路径。
删除 PrewarmHot 创建期 ActorEventFastCache 绑定路径。
所有默认 ActorBehaviour 统一走 ArchetypeRow PostFast。
```

最终 Post 热路径统一为：

```text
PostFast<TEvent>
  -> EventPostRuntime<TEvent>.RowsByArchetype[actorId.ArchetypeId]
  -> row.Mails[actorId.SlotIndex]
  -> row.Pool.Write(...)
  -> row.DirtySlots.Mark(actorId.SlotIndex)
  -> DirtyBucketList.Mark(row.BucketIndex)
```

新设计不再需要旧的行为热度提示，因为 `Archetype + Event` 行缓存已经比旧 `Actor + Event` 缓存更短、更省内存。

---

## 2. 删除目标

### 2.1 删除 BehaviourType 语义

删除：

```csharp
BehaviourType.Cold
BehaviourType.Hot
BehaviourType.PrewarmHot
```

删除 Attribute 中的行为类型参数：

```csharp
[ActorBehaviour(BehaviourType.Hot)]
[ActorBehaviour(BehaviourType.PrewarmHot)]
[ActorBehaviour(BehaviourType.Cold)]
```

统一改为：

```csharp
[ActorBehaviour]
```

### 2.2 删除旧 Actor + Event 缓存

删除：

```text
ActorEventFastCache<TEvent>
ActorEventRuntime<TEvent> 旧 FastCache 版本
ActorFastState[]
ActorId.FastIndex
Hot 首次触发绑定缓存逻辑
PrewarmHot 创建期绑定缓存逻辑
ActorEventRefRecord<TEvent>
ActorEventRefPrewarmPool<TEvent>
ActorPostEndpoint<TEvent> 热路径路由
```

### 2.3 删除旧缓存 Benchmark

删除或停止使用：

```text
ActorPost_Hot_Cached_OneActor_OneEvent
ActorPost_PrewarmHot_Cached_OneActor_OneEvent
ActorPost_Hot_FirstBind_OneActor_OneEvent
ActorPost_PrewarmHot_1000Actors_OneEvent
ActorPost_PrewarmHot_1000Actors_4Events
```

这些 Benchmark 对旧缓存体系有意义，对 ArchetypeRow 最终架构没有意义。

---

## 3. 保留目标

### 3.1 保留 public PostTo

保留：

```csharp
PostTo<TEvent>(ActorId actorId, in TEvent value)
TryPostTo<TEvent>(ActorId actorId, in TEvent value)
```

但内部实现改为：

```text
先走 PostFast<TEvent>
失败后走 SafePath
```

### 3.2 保留 SafePath

SafePath 用于：

```text
ActorId 无效
Archetype 不支持该事件
显式 policy 调用
调试错误信息
特殊安全语义
```

SafePath 不再承担高频 Post 路由责任。

### 3.3 保留 ActorBehaviour

保留：

```csharp
[ActorBehaviour]
```

但不再接受 `BehaviourType` 参数。

---

## 4. 最终架构

### 4.1 ActorWorld

```text
ActorWorld
  ├─ ArchetypeRuntime[] Archetypes
  ├─ DirtyBucketList DirtyBuckets
  ├─ EventPostRuntime<TEvent>
  └─ SafePath fallback
```

### 4.2 ArchetypeRuntime<TActor>

```text
ArchetypeRuntime<TActor>
  ├─ TActor[] Actors
  ├─ int[] Generations
  ├─ EventColumnRuntime[] Columns
  ├─ int[] EventIdToColumnIndex
  └─ int ArchetypeId
```

### 4.3 EventColumn<TActor,TEvent>

```text
EventColumn<TActor,TEvent>
  ├─ EventMail<TEvent>[] Mails
  ├─ EventMailPool<TEvent> Pool
  ├─ DirtySlotList DirtySlots
  ├─ int BucketIndex
  └─ ActorBehaviourInvoker<TActor,TEvent> Invoker
```

### 4.4 EventPostRuntime<TEvent>

```text
EventPostRuntime<TEvent>
  └─ EventPostRow<TEvent>[] RowsByArchetype
```

### 4.5 EventPostRow<TEvent>

```text
EventPostRow<TEvent>
  ├─ EventMail<TEvent>[] Mails
  ├─ EventMailPool<TEvent> Pool
  ├─ DirtySlotList DirtySlots
  ├─ int BucketIndex
  └─ int[] Generations
```

---

## 5. ActorId 精简

最终 `ActorId` 至少需要：

```csharp
public readonly struct ActorId
{
    public readonly int ArchetypeId;
    public readonly int SlotIndex;
    public readonly int Generation;

    /// <param name="archetypeId">
    /// Actor 所属 Archetype 的编号。
    /// 作用：PostFast<TEvent> 使用它直接索引 EventPostRuntime<TEvent>.RowsByArchetype。
    /// </param>
    /// <param name="slotIndex">
    /// Actor 在当前 Archetype 内的槽位。
    /// 作用：PostFast<TEvent> 使用它直接索引 EventMail<TEvent>[] 和 Generations[]。
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

删除：

```text
FastIndex
TypeStorageIndex 如果只服务旧路径
ActorFastState 相关字段
```

如果 `TypeStorageIndex` 仍被 SafePath 使用，则在第一轮重构中可暂存，但最终应从 Post 热路径完全移除。

---

## 6. EventPostRow<TEvent>

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
    /// 当前 Archetype + TEvent 的邮箱数组。
    /// 作用：PostFast<TEvent> 使用 mails[slotIndex] 直接定位目标 Actor 的 TEvent 邮箱。
    /// </param>
    /// <param name="pool">
    /// 当前 ActorWorld + TEvent 的邮箱池。
    /// 作用：提供 TEvent 环状队列 buffer 的租用、写入、增长、释放能力。
    /// </param>
    /// <param name="dirtySlots">
    /// 当前 EventColumn<TActor,TEvent> 的脏 slot 列表。
    /// 作用：当邮箱从空变为非空时，将 slotIndex 标记为待 Pump。
    /// </param>
    /// <param name="bucketIndex">
    /// 当前 EventColumn<TActor,TEvent> 在 DirtyBucketList 中的下标。
    /// 作用：当该列出现消息时，将 bucketIndex 标记为待 Pump。
    /// </param>
    /// <param name="generations">
    /// 当前 Archetype 的 slot 代际数组。
    /// 作用：PostFast<TEvent> 使用 generations[slotIndex] 校验 ActorId 是否仍然有效。
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

## 7. EventPostRuntime<TEvent>

```csharp
internal static class EventPostRuntime<TEvent>
    where TEvent : struct
{
    private static EventPostRow<TEvent>[][] s_rowsByWorld =
        new EventPostRow<TEvent>[4][];

    /// <summary>
    /// 绑定指定 ActorWorld 的 TEvent 行表。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    /// 作用：使用 world.RuntimeIndex 定位多 World 静态槽。
    /// </param>
    /// <param name="rows">
    /// 当前 World 内 TEvent 对应的 RowsByArchetype。
    /// 作用：PostFast<TEvent> 通过 rows[actorId.ArchetypeId] 直接取得 EventPostRow<TEvent>。
    /// </param>
    public static void BindWorld(ActorWorld world, EventPostRow<TEvent>[] rows)
    {
        int worldIndex = world.RuntimeIndex;
        EnsureWorldCapacity(worldIndex);
        s_rowsByWorld[worldIndex] = rows;
    }

    /// <summary>
    /// 获取当前 World 内 TEvent 的 Archetype 行表。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    /// 作用：只读取 RuntimeIndex，不做 Dictionary 查找。
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

## 8. PostFast<TEvent>

```csharp
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
```

---

## 9. PostQueuedGrowFastNoResult<TEvent>

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
    /// mails 参数作用：当前 Archetype + TEvent 的 EventMail<TEvent>[]。
    /// dirtySlots 参数作用：当前 EventColumn<TActor,TEvent> 的脏 slot 列表。
    /// bucketIndex 参数作用：当前 EventColumn<TActor,TEvent> 在 DirtyBucketList 中的下标。
    /// pool 参数作用：当前 ActorWorld + TEvent 的邮箱池。

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

## 10. Public PostTo<TEvent>

```csharp
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct
{
    /// postPolicy / fullPolicy 参数作用：兼容显式策略调用。
    /// 一旦用户显式传入策略，就直接走 SafePath。
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

## 11. ActorBehaviour Attribute 精简

删除：

```csharp
public ActorBehaviourAttribute(BehaviourType behaviourType)
```

保留：

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ActorBehaviourAttribute : Attribute
{
    public ActorBehaviourAttribute()
    {
    }
}
```

使用方式：

```csharp
public partial class EnemyActor : IActor
{
    [ActorBehaviour]
    private void OnDamage(in DamageEvent value)
    {
    }
}
```

---

## 12. 构建期注册 EventPostRow

当 `EventColumn<TActor,TEvent>` 创建完成后，立即注册 `EventPostRow<TEvent>`。

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
    /// archetype 参数作用：当前强类型 Archetype，用于提供 Generations 数组。
    /// column 参数作用：当前 Archetype + TEvent 的事件列。

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

---

## 13. 删除清单

### 13.1 类型删除

删除：

```text
BehaviourType
ActorEventFastCache<TEvent>
ActorFastState
ActorPostEndpoint<TEvent>
ActorEventRefRecord<TEvent>
ActorEventRefPrewarmPool<TEvent>
```

如果其中部分类型仍被其他模块临时引用，应先移除引用，再删除类型。

### 13.2 字段删除

删除：

```text
ActorId.FastIndex
ActorWorld._fastStates
ActorWorld._postEndpointTables
ActorWorld._actorEventRefCaches
EventColumn.BehaviourType
ActorBehaviourEntry.BehaviourType
```

### 13.3 方法删除

删除：

```text
GetOrCreateFastCache<TEvent>
TryBindHotFastCache<TEvent>
BindPrewarmHotFastCaches
BindActorEventRef
ReleasePrewarmHotRef
PostByEndpoint<TEvent>
```

### 13.4 Benchmark 删除

删除：

```text
ActorPost_Hot_Cached_OneActor_OneEvent
ActorPost_PrewarmHot_Cached_OneActor_OneEvent
ActorPost_Hot_FirstBind_OneActor_OneEvent
ActorPost_PrewarmHot_1000Actors_OneEvent
ActorPost_PrewarmHot_1000Actors_4Events
```

---

## 14. 新 Benchmark 设计

### 14.1 目标

新的 Benchmark 只验证最终架构：

```text
ArchetypeRow PostFast
ArchetypeRow PostTo
ArchetypeRow 1000 Actors
Query.PostAll 12 Events
Dictionary baseline
Direct call baseline
```

### 14.2 关键规则

1. 所有内部循环都必须配 `OperationsPerInvoke`。
2. 所有方法都使用同一统计口径。
3. 单 Actor PostFast 使用 `OneMillion` 次循环。
4. 1000 Actors 使用 `OneMillion` 次循环，ActorId 通过 `i % ActorCount` 轮询。
5. Query 12Events 使用 `OperationsPerInvoke = ActorCount * 12`。
6. 不再测试 HotFirstBind。
7. 不再测试 PrewarmHot。

---

## 15. 新 Benchmark 代码模板

```csharp
using BenchmarkDotNet.Attributes;
using LayerBase.Actor;

namespace Benchmarks;

[MemoryDiagnoser]
public class ActorWorldArchetypeRowBenchmarks : EventBenchmarkBase
{
    private const int ActorCount = 1000;
    private const int PostLoopCount = 1_000_000;
    private const int QueryEventCount = ActorCount * 12;

    private ActorWorld _singleWorld = null!;
    private ActorId _singleActorId;

    private ActorWorld _batchWorld = null!;
    private ActorId[] _batchActorIds = null!;

    private ActorWorld _queryWorld = null!;
    private ActorQueryResult _query;

    private Dictionary<int, DictionaryReceiver> _dictionary = null!;
    private int[] _dictionaryKeys = null!;

    /// <summary>
    /// 初始化所有 benchmark 所需对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _singleWorld = CreateBenchmarkWorld(PostLoopCount);
        _singleActorId = _singleWorld.CreateActor<ArchetypeRowBenchmarkActor>().GetActorId();

        _batchWorld = CreateBenchmarkWorld(PostLoopCount);
        _batchActorIds = new ActorId[ActorCount];

        for (int i = 0; i < ActorCount; i++)
        {
            _batchActorIds[i] = _batchWorld.CreateActor<ArchetypeRowBenchmarkActor>().GetActorId();
        }

        _queryWorld = CreateBenchmarkWorld(32);

        for (int i = 0; i < ActorCount; i++)
        {
            _queryWorld.CreateActor<QueryBenchmarkActor>();
        }

        _query = _queryWorld.QueryActor<
            BenchEvent1,
            BenchEvent2,
            BenchEvent3,
            BenchEvent4,
            BenchEvent5,
            BenchEvent6,
            BenchEvent7,
            BenchEvent8,
            BenchEvent9,
            BenchEvent10,
            BenchEvent11,
            BenchEvent12>();

        _dictionary = new Dictionary<int, DictionaryReceiver>(ActorCount);
        _dictionaryKeys = new int[ActorCount];

        for (int i = 0; i < ActorCount; i++)
        {
            _dictionaryKeys[i] = i;
            _dictionary[i] = new DictionaryReceiver();
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_ArchetypeRow_PostFast_OneActor_OneEvent))]
    public void CleanupSinglePostFast()
    {
        PumpAll(_singleWorld);
    }

    [IterationCleanup(Target = nameof(ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent))]
    public void CleanupSinglePostTo()
    {
        PumpAll(_singleWorld);
    }

    [IterationCleanup(Target = nameof(ActorPost_ArchetypeRow_1000Actors_OneEvent))]
    public void CleanupBatch()
    {
        PumpAll(_batchWorld);
    }

    [IterationCleanup(Target = nameof(ActorPost_Query_PostAll_1000Actors_12Events))]
    public void CleanupQuery()
    {
        PumpAll(_queryWorld);
    }

    [Benchmark(
        Description = "ActorPost_ArchetypeRow_PostFast_OneActor_OneEvent",
        OperationsPerInvoke = PostLoopCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_ArchetypeRow_PostFast_OneActor_OneEvent()
    {
        for (int i = 0; i < PostLoopCount; i++)
        {
            _ = _singleWorld.PostFast(_singleActorId, ActorBenchEvent.Instance);
        }
    }

    [Benchmark(
        Description = "ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent",
        OperationsPerInvoke = PostLoopCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent()
    {
        for (int i = 0; i < PostLoopCount; i++)
        {
            _ = _singleWorld.PostTo(_singleActorId, ActorBenchEvent.Instance);
        }
    }

    [Benchmark(
        Description = "ActorPost_ArchetypeRow_1000Actors_OneEvent",
        OperationsPerInvoke = PostLoopCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_ArchetypeRow_1000Actors_OneEvent()
    {
        for (int i = 0; i < PostLoopCount; i++)
        {
            _ = _batchWorld.PostFast(
                _batchActorIds[i % ActorCount],
                ActorBenchEvent.Instance);
        }
    }

    [Benchmark(
        Description = "ActorPost_Query_PostAll_1000Actors_12Events",
        OperationsPerInvoke = QueryEventCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_Query_PostAll_1000Actors_12Events()
    {
        _query.PostAll(
            BenchEvent1.Instance,
            BenchEvent2.Instance,
            BenchEvent3.Instance,
            BenchEvent4.Instance,
            BenchEvent5.Instance,
            BenchEvent6.Instance,
            BenchEvent7.Instance,
            BenchEvent8.Instance,
            BenchEvent9.Instance,
            BenchEvent10.Instance,
            BenchEvent11.Instance,
            BenchEvent12.Instance);
    }

    [Benchmark(
        Description = "Dictionary_1000Actors_LookupAndHandle",
        OperationsPerInvoke = PostLoopCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void Dictionary_1000Actors_LookupAndHandle()
    {
        for (int i = 0; i < PostLoopCount; i++)
        {
            int key = _dictionaryKeys[i % ActorCount];

            if (_dictionary.TryGetValue(key, out DictionaryReceiver? receiver))
            {
                receiver.Handle();
            }
        }
    }

    private static ActorWorld CreateBenchmarkWorld(int maxCapacity)
    {
        /// maxCapacity 参数作用：设置邮箱最大容量。
        /// 单 Actor 一百万次 Post 需要足够大的邮箱容量，避免测到 Grow 或 RejectNew 分支。
        return new ActorWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: maxCapacity,
            maxCapacity: maxCapacity,
            growFactor: 2,
            releaseWhenEmpty: false));
    }

    private static void PumpAll(ActorWorld world)
    {
        /// world 参数作用：清理当前 iteration 中写入的邮箱消息。
        var budget = new RuntimeFrameBudget(
            maxEvents: 0,
            usedEvents: 0,
            deadlineTicks: 0);

        world.Pump(
            deltaTime: 0f,
            fixedDeltaTime: 0f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    private sealed class DictionaryReceiver
    {
        public void Handle()
        {
            BenchmarkSink.IntValue++;
        }
    }

    public readonly struct BenchEvent1 { public static readonly BenchEvent1 Instance = default; }
    public readonly struct BenchEvent2 { public static readonly BenchEvent2 Instance = default; }
    public readonly struct BenchEvent3 { public static readonly BenchEvent3 Instance = default; }
    public readonly struct BenchEvent4 { public static readonly BenchEvent4 Instance = default; }
    public readonly struct BenchEvent5 { public static readonly BenchEvent5 Instance = default; }
    public readonly struct BenchEvent6 { public static readonly BenchEvent6 Instance = default; }
    public readonly struct BenchEvent7 { public static readonly BenchEvent7 Instance = default; }
    public readonly struct BenchEvent8 { public static readonly BenchEvent8 Instance = default; }
    public readonly struct BenchEvent9 { public static readonly BenchEvent9 Instance = default; }
    public readonly struct BenchEvent10 { public static readonly BenchEvent10 Instance = default; }
    public readonly struct BenchEvent11 { public static readonly BenchEvent11 Instance = default; }
    public readonly struct BenchEvent12 { public static readonly BenchEvent12 Instance = default; }
}

public partial class ArchetypeRowBenchmarkActor : IActor
{
    [ActorBehaviour]
    private void OnActorBench(in ActorBenchEvent value)
    {
    }
}

public partial class QueryBenchmarkActor : IActor
{
    [ActorBehaviour] private void On1(in ActorWorldArchetypeRowBenchmarks.BenchEvent1 value) { }
    [ActorBehaviour] private void On2(in ActorWorldArchetypeRowBenchmarks.BenchEvent2 value) { }
    [ActorBehaviour] private void On3(in ActorWorldArchetypeRowBenchmarks.BenchEvent3 value) { }
    [ActorBehaviour] private void On4(in ActorWorldArchetypeRowBenchmarks.BenchEvent4 value) { }
    [ActorBehaviour] private void On5(in ActorWorldArchetypeRowBenchmarks.BenchEvent5 value) { }
    [ActorBehaviour] private void On6(in ActorWorldArchetypeRowBenchmarks.BenchEvent6 value) { }
    [ActorBehaviour] private void On7(in ActorWorldArchetypeRowBenchmarks.BenchEvent7 value) { }
    [ActorBehaviour] private void On8(in ActorWorldArchetypeRowBenchmarks.BenchEvent8 value) { }
    [ActorBehaviour] private void On9(in ActorWorldArchetypeRowBenchmarks.BenchEvent9 value) { }
    [ActorBehaviour] private void On10(in ActorWorldArchetypeRowBenchmarks.BenchEvent10 value) { }
    [ActorBehaviour] private void On11(in ActorWorldArchetypeRowBenchmarks.BenchEvent11 value) { }
    [ActorBehaviour] private void On12(in ActorWorldArchetypeRowBenchmarks.BenchEvent12 value) { }
}
```

---

## 16. 验收标准

### 16.1 删除验收

代码库中不应再出现：

```text
BehaviourType.Hot
BehaviourType.PrewarmHot
BehaviourType.Cold
ActorEventFastCache
ActorFastState
FastIndex
Hot_FirstBind
PrewarmHot
```

### 16.2 PostFast 验收

`PostFast<TEvent>` 命中路径中不允许出现：

```text
Dictionary
Reflection
MethodInfo
object cast
virtual route
SafePath
PostResult
BehaviourType 判断
```

### 16.3 性能验收

期望趋势：

```text
ArchetypeRow_PostFast_OneActor <= 旧 ActorEventRef Post only
ArchetypeRow_PostTo_OneActor <= 旧 ActorWorld Post only
ArchetypeRow_1000Actors_OneEvent <= Dictionary LookupAndHandle
Query_PostAll_12Events 保持当前强路径
```

---

## 17. 最终结论

ArchetypeRow 已经替代旧 Hot / PrewarmHot / Cold 缓存体系。

最终语义：

```text
所有 [ActorBehaviour] 默认注册 EventPostRow。
所有默认 QueuedGrow 行为都可走 PostFast。
低频行为不再通过 Cold 关闭缓存。
高频行为不再通过 Hot / PrewarmHot 提示缓存。
是否高频由调用方式决定：
  单体高频 -> PostFast
  批量高频 -> Query.PostAll
  安全兼容 -> PostTo / TryPostToSafe
```

最终删除旧语义后，系统只保留：

```text
ActorBehaviour
ArchetypeRuntime<TActor>
EventColumn<TActor,TEvent>
EventPostRow<TEvent>
EventPostRuntime<TEvent>
PostFast<TEvent>
SafePath fallback
```
