# ActorWorld Post Fast Path Design

> 文件名：`actorworld-post-fast-path-design.md`  
> 适用仓库：`avaw23112/LayerBase`  
> 目标：在不重构 ActorWorld 整体架构的前提下，降低单 Actor 高频 Post 与 Query.PostAll 批量投递的热路径用时。  
> 执行对象：Codex / 自动化代码代理 / 开发者。  
> 范围：`ActorRef`、`ActorEventRef`、`EventColumn.PostQueuedFast`、`Query.PostAllFast`、Benchmark 与测试。  
> 不做内容：不引入 AttributeSystem、UI、Network、Save，不重写 Actor 存储结构，不讨论其它架构分支。

---

## 1. 当前问题

当前 benchmark 已经显示：

```text
Allocated = 0 B
```

说明 ActorWorld 的 GC 分配问题已经解决。

现在主要瓶颈是：

```text
单 Actor 高频 Post 入队路径仍然偏重。
```

当前拆分 benchmark 中：

```text
ActorWorld Post only      约 10ms / 大批量循环
ActorRef Post only        约 8ms / 大批量循环
ActorWorld Pump only      很低
Query.PostAll + Pump      表现较好
```

这说明：

```text
1. Pump 不是当前主要瓶颈。
2. ActorRef 方向有效，但还没有把 Post 路径打穿。
3. Query.PostAll 批量路径有继续强化价值。
4. 下一步应优化 Post 入队，不应重写整体 ActorWorld。
```

---

## 2. 总体方案

本次只做三条优化：

```text
1. ActorEventRef<TActor, TEvent>
2. EventColumn.PostQueuedFast
3. Query.PostAllFast
```

对应目标：

```text
ActorEventRef<TActor,TEvent>
- 用于高频点对点事件投递。
- 缓存 TypedActorStorage 与 EventColumn。
- 减少 ActorId 解析、Storage 查找、Column 查找。

EventColumn.PostQueuedFast
- 用于默认高性能邮箱策略。
- 绕过通用策略 switch。
- 成功路径只执行必要检查、写入 mailbox、标记 dirty slot。

Query.PostAllFast
- 用于批量 Actor 事件投递。
- 面向同类型、同 EventColumn、连续 slot 的批量场景。
- 减少逐 Actor 调用 column.Post 的重复成本。
```

---

## 3. 不变量

本次优化必须保持以下语义：

```text
1. 预热后 0B GC Alloc。
2. Actor generation 必须校验。
3. Actor 已销毁后不能被旧引用命中。
4. PendingDestroy / Destroying 语义不能被破坏。
5. Disabled 策略不能被破坏。
6. 非默认邮箱策略仍然走完整路径。
7. 公开旧 API 不删除。
8. 新快路径失败时可降级到安全路径。
```

---

## 4. ActorEventRef<TActor,TEvent>

### 4.1 目标

新增一个高频事件引用：

```text
ActorEventRef<TActor,TEvent>
```

它缓存：

```text
TypedActorStorage<TActor>
EventColumn<TActor,TEvent>
slotIndex
generation
```

用于替代高频场景中的：

```text
ActorWorld.Post(actorId, event)
ActorRef<TActor>.Post(event)
```

### 4.2 使用场景

```text
玩家每帧向 HUD Actor 投递 UI 更新事件
技能系统持续向目标 Actor 投递伤害 Tick
AI 高频向当前目标投递仇恨事件
输入系统持续向 PlayerActor 投递输入事件
```

### 4.3 新增文件

建议新增：

```text
LayerBase/Actor/Refs/ActorEventRef.cs
```

### 4.4 代码设计

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 高频 Actor 事件投递引用。
/// </summary>
/// <typeparam name="TActor">
/// TActor 参数：
/// 目标 Actor 的具体类型。
/// 用于缓存 TypedActorStorage，避免每次 Post 都重新解析 ActorId。
/// </typeparam>
/// <typeparam name="TEvent">
/// TEvent 参数：
/// 要投递的事件类型。
/// 用于缓存 EventColumn，避免每次 Post 都重新查找 Column。
/// </typeparam>
public readonly struct ActorEventRef<TActor, TEvent>
    where TActor : class, IActor
    where TEvent : struct
{
    private readonly TypedActorStorage<TActor>? _storage;
    private readonly EventColumn<TActor, TEvent>? _column;
    private readonly int _slotIndex;
    private readonly int _generation;

    internal ActorEventRef(
        TypedActorStorage<TActor> storage,
        EventColumn<TActor, TEvent> column,
        int slotIndex,
        int generation)
    {
        // storage 参数：
        // 目标 Actor 所在的具体类型存储。
        // 缓存它可以避免每次 Post 都通过 ActorId 查找 storage。
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));

        // column 参数：
        // 目标事件类型对应的 EventColumn。
        // 缓存它可以避免每次 Post 都 TryGetColumn<TEvent>()。
        _column = column ?? throw new ArgumentNullException(nameof(column));

        // slotIndex 参数：
        // Actor 在 storage 中的槽位下标。
        _slotIndex = slotIndex;

        // generation 参数：
        // ActorId 的 generation。
        // 用于防止 slot 复用后，旧引用误投递给新 Actor。
        _generation = generation;
    }

    /// <summary>
    /// 当前引用是否仍然指向存活 Actor。
    /// </summary>
    public bool IsAlive
    {
        get
        {
            // 作用说明：
            // 即使 ActorEventRef 缓存了 storage、column 和 slot，
            // 仍然必须检查 generation。
            // 这是避免旧引用误命中新 Actor 的关键安全条件。
            return _storage != null
                   && _storage.IsAlive(_slotIndex, _generation);
        }
    }

    /// <summary>
    /// 向目标 Actor 投递事件。
    /// </summary>
    /// <param name="value">
    /// value 参数：
    /// 要投递的事件值。
    /// </param>
    /// <returns>
    /// 返回投递结果。
    /// 成功时返回 PostResult.Success。
    /// 目标失效或策略不支持时返回失败结果。
    /// </returns>
    public PostResult Post(in TEvent value)
    {
        // 作用说明：
        // ActorEventRef 是高频路径，但不能跳过 generation 检查。
        // 如果 Actor 已销毁或 slot 已复用，必须拒绝投递。
        if (_storage == null ||
            _column == null ||
            !_storage.IsAlive(_slotIndex, _generation))
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "ActorEventRef target is not alive.",
                PostFailureKind.ActorNotFound);
        }

        // 作用说明：
        // PostQueuedFast 只处理默认高性能路径。
        // 如果当前 Column 配置无法使用快路径，它内部应降级到完整 Post 路径。
        return _column.PostQueuedFast(_slotIndex, in value);
    }
}
```

---

## 5. ActorWorld 获取 ActorEventRef

### 5.1 目标

新增 API：

```text
ActorWorld.GetActorEventRef<TActor,TEvent>(ActorId actorId)
```

### 5.2 建议位置

```text
LayerBase/Actor/Storage/ActorWorld.Ref.cs
```

### 5.3 API 设计

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    /// <summary>
    /// 获取高频事件投递引用。
    /// </summary>
    /// <typeparam name="TActor">
    /// TActor 参数：
    /// 目标 Actor 的具体类型。
    /// </typeparam>
    /// <typeparam name="TEvent">
    /// TEvent 参数：
    /// 要高频投递的事件类型。
    /// </typeparam>
    /// <param name="actorId">
    /// actorId 参数：
    /// 目标 Actor 的运行时 ID。
    /// </param>
    /// <returns>
    /// 返回 ActorEventRef。
    /// 如果 actorId 无效、类型不匹配、事件不支持，则返回 default。
    /// default 引用的 IsAlive 为 false，Post 会返回失败。
    /// </returns>
    public ActorEventRef<TActor, TEvent> GetActorEventRef<TActor, TEvent>(ActorId actorId)
        where TActor : class, IActor
        where TEvent : struct
    {
        // 作用说明：
        // 该方法只在创建高频引用时解析一次 ActorId。
        // 后续高频 Post 直接使用缓存的 storage / column / slot / generation。

        if (!TryResolveTypedStorage<TActor>(
                actorId,
                out TypedActorStorage<TActor>? storage,
                out int slotIndex,
                out int generation))
        {
            return default;
        }

        if (!storage.TryGetColumn(out EventColumn<TActor, TEvent>? column))
        {
            return default;
        }

        if (!storage.IsAlive(slotIndex, generation))
        {
            return default;
        }

        return new ActorEventRef<TActor, TEvent>(
            storage,
            column,
            slotIndex,
            generation);
    }
}
```

### 5.4 需要开放的内部方法

如果当前 `TypedActorStorage<TActor>.TryGetColumn<TEvent>` 是 private，需要改为 internal：

```csharp
internal bool TryGetColumn<TEvent>(out EventColumn<TActor, TEvent>? column)
    where TEvent : struct
```

如果当前 ActorWorld 没有 `TryResolveTypedStorage<TActor>`，需要补一个 internal helper。

该 helper 不应分配。

---

## 6. EventColumn.PostQueuedFast

### 6.1 目标

给默认策略增加快路径：

```text
Queued + Grow + releaseWhenEmpty=false
```

该路径绕过：

```text
ActorPostPolicy? 合并
ActorMailFullPolicy? 合并
ActorPostPolicy switch
ActorMailFullPolicy switch
通用 Failure message 构造
```

### 6.2 修改文件

```text
LayerBase/Actor/Mail/EventColumn.cs
```

或当前实际 `EventColumn<TActor,TEvent>` 所在文件。

### 6.3 API 设计

```csharp
internal PostResult PostQueuedFast(int slotIndex, in TEvent value)
{
    // slotIndex 参数：
    // 目标 Actor 在 storage 中的 slot 下标。
    //
    // value 参数：
    // 要投递的事件值。
    //
    // 作用说明：
    // 这是默认高性能路径。
    // 只处理 Queued + Grow + releaseWhenEmpty=false。
    // 不满足条件时降级到完整 Post。

    if (!CanUseQueuedFastPath)
    {
        return Post(slotIndex, in value, postPolicy: null, fullPolicy: null);
    }

    if (!CanPostToSlotFast(slotIndex))
    {
        return Post(slotIndex, in value, postPolicy: null, fullPolicy: null);
    }

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

    int tail = GetTailIndex(mail.Head, mail.Count, mail.Capacity);
    _bufferPool.Write(mail.BufferId, tail, in value);
    mail.Count++;

    if (mail.Count == 1)
    {
        _dirtySlots.AddIfNotExists(slotIndex);
        NotifyBucketDirtyIfNeeded();
    }

    return PostResult.Success;
}
```

### 6.4 CanUseQueuedFastPath

```csharp
private bool CanUseQueuedFastPath
{
    get
    {
        // 作用说明：
        // 只有默认高性能配置才能走快路径。
        // 其它策略必须走完整路径，避免破坏语义。
        return _options.PostPolicy == ActorPostPolicy.Queued
               && _options.FullPolicy == ActorMailFullPolicy.Grow
               && !_options.ReleaseWhenEmpty;
    }
}
```

### 6.5 CanPostToSlotFast

```csharp
private bool CanPostToSlotFast(int slotIndex)
{
    // slotIndex 参数：
    // 目标 Actor slot。
    //
    // 作用说明：
    // 该方法只做快路径必要检查。
    // 如果发现复杂策略场景，直接返回 false，让调用方降级完整 Post。

    if (!_owner.IsAliveSlot(slotIndex))
    {
        return false;
    }

    ActorSlotState state = _owner.GetSlotState(slotIndex);

    if (state == ActorSlotState.PendingDestroy ||
        state == ActorSlotState.Destroying)
    {
        return false;
    }

    if (_options.DisabledPolicy == ActorMailDisabledPolicy.Reject
        && !_owner.IsSlotEnabled(slotIndex))
    {
        return false;
    }

    return true;
}
```

注意：

```text
如果当前 storage 没有 GetSlotState / IsSlotEnabled 的 internal 快速方法，需要补。
不要在这里构造字符串。
不要在这里做复杂 PostResult。
复杂失败交给完整 Post。
```

### 6.6 TryGrowFast

```csharp
private bool TryGrowFast(ref EventMail<TEvent> mail)
{
    // mail 参数：
    // 当前 Actor slot 对应的事件邮箱。
    //
    // 作用说明：
    // 快路径只处理 Grow。
    // 如果已经到达 MaxCapacity，则返回 false。

    if (mail.Capacity >= _options.MaxCapacity)
    {
        return false;
    }

    int nextCapacity = mail.Capacity * Math.Max(_options.GrowFactor, 2);

    if (nextCapacity <= mail.Capacity)
    {
        nextCapacity = mail.Capacity + 1;
    }

    nextCapacity = Math.Min(nextCapacity, _options.MaxCapacity);

    if (nextCapacity <= mail.Capacity)
    {
        return false;
    }

    _bufferPool.Resize(mail.BufferId, mail.Head, mail.Count, nextCapacity);
    mail.Head = 0;
    mail.Capacity = nextCapacity;
    return true;
}
```

### 6.7 GetTailIndex

```csharp
private static int GetTailIndex(int head, int count, int capacity)
{
    // head 参数：
    // 当前 ring buffer 的队首位置。
    //
    // count 参数：
    // 当前 ring buffer 中有效事件数量。
    //
    // capacity 参数：
    // 当前 ring buffer 容量。
    //
    // 作用说明：
    // 如果 capacity 是 2 的幂，可用位运算替代取模。
    // 否则使用 % 保持正确性。

    int raw = head + count;

    if ((capacity & (capacity - 1)) == 0)
    {
        return raw & (capacity - 1);
    }

    return raw % capacity;
}
```

---

## 7. Query.PostAllFast

### 7.1 目标

强化批量路径。

当前 Query.PostAll 表现较好，应继续优化它，让它成为 ActorWorld 的优势路径。

### 7.2 修改范围

```text
ActorQueryPostExtensions
ActorQueryResult
ActorQueryCache
TypedActorStorage<TActor>
EventColumn<TActor,TEvent>
```

### 7.3 EventColumn 批量 API

新增：

```csharp
internal void PostToAliveSlotsFast(
    int maxSlot,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
{
    // maxSlot 参数：
    // storage 当前有效 slot 上限。
    //
    // value 参数：
    // 要投递给所有存活 Actor 的事件。
    //
    // postPolicy 参数：
    // 外部指定的投递策略。
    // null 表示使用 column 默认策略。
    //
    // fullPolicy 参数：
    // 外部指定的邮箱满策略。
    // null 表示使用 column 默认策略。
    //
    // 作用说明：
    // 默认策略下使用 PostQueuedFast。
    // 非默认策略下回退到完整 Post。

    bool useFastPath = postPolicy == null
                       && fullPolicy == null
                       && CanUseQueuedFastPath;

    for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
    {
        if (!_owner.IsSlotPostable(slotIndex))
        {
            continue;
        }

        if (useFastPath)
        {
            _ = PostQueuedFast(slotIndex, in value);
        }
        else
        {
            _ = Post(slotIndex, in value, postPolicy, fullPolicy);
        }
    }
}
```

如果 `IsSlotPostable` 当前不是 internal，需要暴露为 internal 快速方法。

### 7.4 TypedActorStorage 批量接入

把当前：

```csharp
for slotIndex:
    if IsSlotPostable
        column.Post(...)
```

改成：

```csharp
column.PostToAliveSlotsFast(
    maxSlot: MaxSlot,
    value: in value,
    postPolicy: postPolicy,
    fullPolicy: fullPolicy);
```

这样让批量路径进入 column 内部快路径，减少每层重复判断。

### 7.5 QueryResult 缓存 Column，可作为第二步

第一步先让 `PostToAliveActors<TEvent>` 走 `PostToAliveSlotsFast`。

第二步再做缓存：

```text
QueryPostCache<TEvent>
```

缓存内容：

```text
Storage
Column
MaxSlot 读取方式
```

暂不强制第一阶段完成。

---

## 8. Benchmark 修改要求

### 8.1 保留现有 benchmark

保留：

```text
ActorWorld Post + Pump
ActorWorld Post only
ActorWorld Pump only
ActorRef Post only
Query.PostAll + Pump
LayerBase PostScheduler
LayerBase Send
Dictionary baseline
Direct baseline
```

### 8.2 新增 benchmark

新增：

```text
ActorWorld.Post(actorId) only
Actor.Post extension only
ActorRef.Post only
ActorEventRef.Post only
Query.PostAll only - 1000 Actors
Query.PostAll + Pump - 1000 Actors
Query.PostAll + Pump - 10000 Actors
```

### 8.3 新增字段

```csharp
private ActorId _actorId;
private ActorEventRef<BenchmarkActor, ActorBenchEvent> _actorEventRef;
```

Setup 中：

```csharp
_actorId = _actor.GetActorId();
_actorEventRef = _actorWorld.GetActorEventRef<BenchmarkActor, ActorBenchEvent>(_actorId);
```

### 8.4 新增 benchmark 示例

```csharp
[Benchmark(Description = "ActorEventRef Post only")]
[BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.ActorEventRef")]
public void ActorEventRefPostOnly()
{
    for (int i = 0; i < OneMillion; i++)
    {
        _actorEventRef.Post(ActorBenchEvent.Instance);
    }
}
```

### 8.5 Benchmark 注意事项

```text
1. Description 必须和实际循环次数一致。
2. PostOnly 的 IterationCleanup 必须 DrainWorld，确保事件不跨迭代残留。
3. PumpOnly 的 IterationSetup 必须确认本轮只预投递一次。
4. ActorEventRef benchmark 必须和 ActorRef benchmark 使用相同 world 配置。
5. 所有 benchmark 必须保持 Allocated = 0 B。
```

---

## 9. DrainWorld 工具

建议 benchmark 内新增：

```csharp
private static void DrainWorld(ActorWorld world)
{
    // world 参数：
    // 要清空邮箱的 ActorWorld。
    //
    // 作用说明：
    // PostOnly benchmark 会把大量事件留在邮箱中。
    // IterationCleanup 必须把邮箱完全清空，避免下一轮迭代受到旧数据影响。

    while (true)
    {
        var budget = new RuntimeFrameBudget(
            maxEvents: 0,
            usedEvents: 0,
            deadlineTicks: 0);

        world.Pump(0f, 0f, false, ref budget);

        if (world.LastMailPumpStats.ProcessedTotal == 0 &&
            world.LastMailPumpStats.RemainingDirtyBuckets == 0)
        {
            break;
        }
    }
}
```

如果 `RemainingDirtyBuckets` 不可靠，应先修复统计，或增加测试专用 pending count。

---

## 10. 测试计划

### 10.1 ActorEventRef 正确性

新增测试：

```text
1. GetActorEventRef 对存活 Actor 返回有效 ref。
2. ActorEventRef.Post 能被 Pump 到 handler。
3. Actor 销毁后，旧 ActorEventRef.Post 返回失败。
4. slot 复用后，旧 ActorEventRef 不能命中新 Actor。
5. Actor 不支持 TEvent 时返回 default ref。
```

### 10.2 PostQueuedFast 正确性

新增测试：

```text
1. 默认 Queued + Grow 能走快路径。
2. 非 Queued 策略回退完整路径。
3. 非 Grow 策略回退完整路径。
4. releaseWhenEmpty=true 时回退完整路径。
5. mailbox 满时 Grow 正确。
6. 超过 MaxCapacity 时失败语义正确。
7. DisabledPolicy.Reject 生效。
8. PendingDestroy / Destroying 拒绝投递。
```

### 10.3 Query.PostAllFast 正确性

新增测试：

```text
1. Query.PostAll 能命中所有符合条件 Actor。
2. Query.PostAll 不命中 excluded Tag Actor。
3. Query.PostAll 不命中 excluded Group Actor。
4. Query.PostAll 对 disabled actor 的行为符合 DisabledPolicy。
5. Query.PostAll 默认策略走快路径。
6. Query.PostAll 非默认策略回退完整路径。
```

### 10.4 分配测试

新增或更新：

```text
1. ActorEventRef.Post 预热后 0B。
2. PostQueuedFast 预热后 0B。
3. Query.PostAllFast 预热后 0B。
4. ActorWorld Post + Pump 继续 0B。
```

---

## 11. DoD

完成标准：

```text
1. ActorEventRef<TActor,TEvent> 可用。
2. ActorEventRef.Post 快于 ActorRef.Post。
3. EventColumn.PostQueuedFast 可用。
4. 默认策略走快路径。
5. 非默认策略回退完整路径。
6. Query.PostAll 使用批量快路径。
7. 所有新增 correctness 测试通过。
8. 所有新增 allocation 测试通过。
9. 原有测试通过。
10. Benchmark 中 ActorEventRef.Post only 明显低于 ActorRef.Post only。
11. Benchmark 中 Query.PostAll + Pump 不退化。
12. 所有相关 benchmark 保持 Allocated = 0 B。
```

---

## 12. 禁止事项

本次任务不要做：

```text
1. 不重构 ActorWorld 整体存储。
2. 不引入新的外部模块。
3. 不删除旧 Post API。
4. 不删除 PostResult。
5. 不删除 PendingDestroy / Destroying 检查。
6. 不跳过 generation 检查。
7. 不为了 benchmark 特化 BenchmarkActor。
8. 不引入 unsafe 指针。
9. 不修改事件语义。
```

---

## 13. 推荐执行顺序

```text
1. 暴露 TypedActorStorage.TryGetColumn<TEvent> 为 internal。
2. 新增 ActorEventRef<TActor,TEvent>。
3. 新增 ActorWorld.GetActorEventRef<TActor,TEvent>。
4. 新增 EventColumn.PostQueuedFast。
5. ActorEventRef.Post 接入 PostQueuedFast。
6. TypedActorStorage.PostToAliveActors 接入 EventColumn.PostToAliveSlotsFast。
7. Query.PostAll 跑通批量快路径。
8. 添加 correctness 测试。
9. 添加 allocation 测试。
10. 添加 ActorEventRef benchmark。
11. 对比 ActorRef.Post 与 ActorEventRef.Post。
```

---

## 14. 最终定位

完成后 ActorWorld 应形成三条清晰路径：

```text
通用安全路径：
ActorWorld.Post(actorId, event)
Actor.Post(event)

高频点对点路径：
ActorRef<TActor>.Post(event)
ActorEventRef<TActor,TEvent>.Post(event)

高频批量路径：
Query.PostAll(event)
Query.PostAllFast(event)
```

其中：

```text
ActorWorld.Post 负责安全与通用性。
ActorEventRef 负责高频单点投递。
Query.PostAllFast 负责批量吞吐。
```

最终目标：

```text
ActorEventRef.Post 接近 PostScheduler 成本。
Query.PostAllFast 继续保持批量优势。
ActorWorld 全路径维持 0B GC Alloc。
```
