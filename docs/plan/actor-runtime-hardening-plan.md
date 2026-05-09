# LayerBase Actor Runtime Hardening Plan

> 文件名：`actor-runtime-hardening-plan.md`  
> 目标：在 Actor 系统完成泛型 Tag / Group、Query v2、ForEach、PostAll 优化和可选池化后，继续补齐工程硬化能力。  
> 范围：ActorId 安全性与 Debug 诊断、生命周期顺序语义、Actor Mail 策略细化、Query 命中数量统计、Actor Pool 严格版本、Actor 邮箱 Pump 调度公平性。

---

## 1. 背景

Actor 系统完成基础工程化后，已经可以支撑常见游戏业务对象：

- Actor 创建与销毁。
- ActorContext 注入。
- 生命周期执行。
- Actor 邮箱。
- 泛型 Tag / Group。
- Query v2。
- QueryResult ForEach。
- PostAll 多事件优化。
- 可选 Actor Pool。
- Benchmark 基础覆盖。

下一阶段不应继续盲目堆功能，而应补齐工程硬化能力。

本阶段目标是：

```text
让 Actor 系统更可诊断、更可预测、更安全、更适合长期运行。
```

本阶段要做 6 个方向：

1. ActorId 安全性与 Debug 可诊断能力。
2. Actor 生命周期顺序的严格语义。
3. Actor Mail 策略继续细化。
4. Query 命中数量统计。
5. Actor Pool 的更严格版本。
6. Actor 邮箱 Pump 调度公平性。

---

## 2. 非目标

本阶段不做：

- 空间查询。
- AOI。
- 半径查询。
- 最近目标查询。
- 坐标索引。
- 低频生命周期调度。
- Actor 多线程并发执行。
- 网络同步协议。
- ECS 替代方案。
- 引擎对象绑定。

---

# 3. ActorId 安全性与 Debug 可诊断能力

## 3.1 问题

ActorId 是 Actor 系统的核心定位信息。

典型 ActorId 包含：

```text
ArchetypeId
TypeStorageIndex
SlotIndex
Generation
```

它能定位到：

```text
ActorWorld -> BehaviourArchetype -> TypedActorStorage -> Slot
```

但当 ActorId 失效、Actor 已销毁、Generation 不匹配、Storage 不存在、Query 没命中、消息没投递时，业务层很难快速判断问题出在哪里。

因此需要一套 Debug 诊断 API。

---

## 3.2 目标

新增：

```csharp
public ActorDebugInfo GetDebugInfo(ActorId actorId);

public string DescribeActor(ActorId actorId);

public string DumpActorWorld();

public string DumpQuery(ActorQueryResult query);
```

目标能力：

```text
能判断 ActorId 是否有效。
能判断 Actor 是否 Alive。
能判断 Actor 是否 Enabled。
能判断 Actor 是否 PendingDestroy。
能输出 Actor 类型名。
能输出 Tag / Group。
能输出所属 Archetype。
能输出 Storage 信息。
能输出邮箱积压情况。
能输出生命周期注册情况。
能输出 Query 命中情况。
```

---

## 3.3 ActorDebugInfo 设计

```csharp
namespace LayerBase.Actor;

public readonly struct ActorDebugInfo
{
    public readonly ActorId ActorId;

    public readonly bool IsValid;

    public readonly bool IsAlive;

    public readonly bool IsEnabled;

    public readonly bool IsPendingDestroy;

    public readonly string ActorTypeName;

    public readonly string ArchetypeInfo;

    public readonly string[] Tags;

    public readonly string[] Groups;

    public readonly int PendingMailCount;

    public readonly bool HasUpdate;

    public readonly bool HasLateUpdate;

    public readonly bool HasFixedUpdate;

    public readonly string FailureReason;

    public ActorDebugInfo(
        ActorId actorId,
        bool isValid,
        bool isAlive,
        bool isEnabled,
        bool isPendingDestroy,
        string actorTypeName,
        string archetypeInfo,
        string[] tags,
        string[] groups,
        int pendingMailCount,
        bool hasUpdate,
        bool hasLateUpdate,
        bool hasFixedUpdate,
        string failureReason)
    {
        // actorId 参数：
        // 被诊断的 ActorId。
        //
        // isValid 参数：
        // ActorId 是否能定位到有效 Storage 和 Slot。
        //
        // isAlive 参数：
        // Actor 当前是否处于 Alive 状态。
        //
        // isEnabled 参数：
        // Actor 当前是否启用。
        //
        // isPendingDestroy 参数：
        // Actor 是否已经标记为 PendingDestroy。
        //
        // actorTypeName 参数：
        // Actor 实际类型名。
        //
        // archetypeInfo 参数：
        // Actor 所属 Archetype 的摘要信息。
        //
        // tags 参数：
        // Actor 类型声明的 Tag 名称列表。
        //
        // groups 参数：
        // Actor 类型声明的 Group 名称列表。
        //
        // pendingMailCount 参数：
        // Actor 当前邮箱中待处理消息数量。
        //
        // hasUpdate 参数：
        // Actor 是否注册了 IUpdate。
        //
        // hasLateUpdate 参数：
        // Actor 是否注册了 ILateUpdate。
        //
        // hasFixedUpdate 参数：
        // Actor 是否注册了 IFixedUpdate。
        //
        // failureReason 参数：
        // 如果 ActorId 无效，这里记录失败原因。
        ActorId = actorId;
        IsValid = isValid;
        IsAlive = isAlive;
        IsEnabled = isEnabled;
        IsPendingDestroy = isPendingDestroy;
        ActorTypeName = actorTypeName;
        ArchetypeInfo = archetypeInfo;
        Tags = tags;
        Groups = groups;
        PendingMailCount = pendingMailCount;
        HasUpdate = hasUpdate;
        HasLateUpdate = hasLateUpdate;
        HasFixedUpdate = hasFixedUpdate;
        FailureReason = failureReason;
    }
}
```

---

## 3.4 ActorWorld.GetDebugInfo

```csharp
public ActorDebugInfo GetDebugInfo(ActorId actorId)
{
    // actorId 参数：
    // 要诊断的 ActorId。
    //
    // 必要逻辑：
    // Debug API 不应抛出普通定位错误。
    // 它应该尽量返回可读的失败原因，方便调试。
    if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
    {
        return ActorDebugInfo.Invalid(
            actorId,
            "Invalid ArchetypeId.");
    }

    BehaviourArchetype archetype = _archetypes[actorId.ArchetypeId];

    return archetype.GetDebugInfo(actorId);
}
```

---

## 3.5 DescribeActor 输出格式

```text
Actor Debug Info
----------------
ActorId:
  ArchetypeId: 3
  TypeStorageIndex: 1
  SlotIndex: 128
  Generation: 4

State:
  Valid: true
  Alive: true
  Enabled: true
  PendingDestroy: false

Type:
  EnemyActor

Archetype:
  Behaviour: DamageEvent, MoveEvent
  Tags: EnemyTag, DamageableTag
  Groups: BattleActorGroup

Mail:
  PendingMailCount: 3

Lifecycle:
  IUpdate: true
  ILateUpdate: false
  IFixedUpdate: true
```

---

## 3.6 DumpActorWorld

```csharp
public string DumpActorWorld()
{
    // DumpActorWorld 的作用：
    // 输出当前 ActorWorld 的整体运行状态。
    //
    // 必要逻辑：
    // 该方法只用于 Debug，不进入热路径。
    // 可以使用 StringBuilder 和 LINQ。
    var builder = new StringBuilder();

    builder.AppendLine("# ActorWorld Dump");
    builder.AppendLine();

    builder.AppendLine("## Archetypes");
    builder.AppendLine("| Id | Behaviours | Tags | Groups | ActorCount | Alive | PendingDestroy | MailCount |");
    builder.AppendLine("| -- | -- | -- | -- | -- | -- | -- | -- |");

    foreach (BehaviourArchetype archetype in _archetypes)
    {
        archetype.AppendDebugRow(builder);
    }

    return builder.ToString();
}
```

---

## 3.7 验收标准

- 无效 ActorId 不抛异常，返回明确失败原因。
- 已销毁 ActorId 能显示 Generation 不匹配。
- PendingDestroy Actor 能被诊断出来。
- Debug 输出包含 Actor 类型、Tag、Group、生命周期、邮箱积压。
- DumpActorWorld 能输出所有 Archetype 摘要。
- Debug API 不进入热路径，不要求零分配。

---

# 4. Actor 生命周期顺序的严格语义

## 4.1 问题

Actor 生命周期如果语义不严格，业务层会出现隐藏问题。

必须明确：

```text
IStart 什么时候调用？
DestroyActor 后本帧还会不会 Update？
Disabled Actor 是否还能收邮件？
PendingDestroy Actor 是否还能收邮件？
IDestroy 中能不能 Post？
IDestroy 中能不能 CreateActor？
FixedUpdate / Update / LateUpdate 的顺序如何？
```

---

## 4.2 生命周期顺序

推荐固定顺序：

```text
CreateActor
  -> new 或 pool rent
  -> ActorContext 注入
  -> 注册行为元数据
  -> 注册生命周期
  -> IStart.Start 立即调用

Runtime.Pump
  -> Actor Mail Pump
  -> SweepPendingDestroy
  -> IFixedUpdate.FixedUpdate
  -> IUpdate.Update
  -> ILateUpdate.LateUpdate
  -> SweepPendingDestroy
```

说明：

- `IStart` 在 `CreateActor` 期间立即调用。
- Actor 邮箱先于生命周期执行。
- Actor Mail 阶段产生的 DestroyActor 会让 Actor 本帧不再进入生命周期。
- 生命周期阶段产生的 DestroyActor 会在本帧末尾清理。
- `IDestroy` 只在真正 DestroyNow 时调用一次。

---

## 4.3 严格规则

### 规则 1：IStart 调用时机

```text
IStart 在 ActorContext 注入完成后立即调用。
```

理由：

```text
Actor 在 Start 中通常需要访问 Context。
如果 Context 未注入，Start 可用性会很差。
```

### 规则 2：DestroyActor 不立即释放对象

```text
DestroyActor 只标记 PendingDestroy。
真正释放发生在 SweepPendingDestroy。
```

理由：

```text
避免遍历 Actor 邮箱或生命周期时直接修改 Storage。
```

### 规则 3：PendingDestroy Actor 不再执行生命周期

```text
Actor 被标记 PendingDestroy 后，不再进入 IFixedUpdate / IUpdate / ILateUpdate。
```

理由：

```text
已经准备销毁的 Actor 不应继续执行业务逻辑。
```

### 规则 4：PendingDestroy Actor 不再接收新邮件

```text
Post 到 PendingDestroy Actor 应返回失败。
```

推荐失败原因：

```text
Actor is pending destroy.
```

### 规则 5：DestroyNow 清空所有邮件

```text
DestroyNow 必须 ClearAllMails。
```

理由：

```text
避免 Actor 被销毁后残留邮件污染复用 Slot 或池化对象。
```

### 规则 6：Disabled Actor 不执行生命周期

```text
Disabled Actor 不执行 IFixedUpdate / IUpdate / ILateUpdate。
```

### 规则 7：Disabled Actor 是否接收邮件由策略决定

推荐默认：

```text
Disabled Actor 可以接收邮件，但不执行生命周期。
```

可选策略：

```text
ActorMailDisabledPolicy.Accept
ActorMailDisabledPolicy.Reject
ActorMailDisabledPolicy.Buffer
```

### 规则 8：IDestroy 中允许读 Context，但不建议 Post 给自己

推荐规则：

```text
IDestroy 中允许访问 ActorContext。
IDestroy 中 Post 给自己应失败。
IDestroy 中 Post 给其他 Actor 允许。
IDestroy 中 CreateActor 允许，但不建议大量创建。
```

---

## 4.4 生命周期状态机

```csharp
public enum ActorSlotState
{
    Empty = 0,

    Alive = 1,

    PendingDestroy = 2,

    Destroying = 3
}
```

说明：

- `Empty`：Slot 未占用。
- `Alive`：Actor 正常存活。
- `PendingDestroy`：Actor 已请求销毁，等待 Sweep。
- `Destroying`：Actor 正在执行 DestroyNow，防止重入销毁。

---

## 4.5 DestroyNow 防重入

```csharp
private bool DestroyNow(
    int slotIndex,
    int generation,
    ActorWorld world)
{
    // slotIndex 参数：
    // 要销毁的 Actor 槽位。
    //
    // generation 参数：
    // ActorId 的代数，用于防止旧 ID 命中新对象。
    //
    // world 参数：
    // 当前 Actor 所属 ActorWorld。
    //
    // 必要逻辑：
    // Destroying 状态用于防止 IDestroy 内部再次 DestroyActor 导致重入。
    if (_states[slotIndex] == ActorSlotState.Destroying)
    {
        return false;
    }

    _states[slotIndex] = ActorSlotState.Destroying;

    TActor? actor = _actors[slotIndex];

    if (actor is IDestroy destroy)
    {
        destroy.Destroy();
    }

    UnregisterLifecycleInterfaces(slotIndex, world);
    ClearAllMails(slotIndex);

    // 后续执行 Slot 清理、Generation++、FreeList.Push、Pool.Return。
    return true;
}
```

---

## 4.6 验收标准

- `IStart` 中可访问 ActorContext。
- `DestroyActor` 后 Actor 不再执行后续生命周期。
- `PendingDestroy` Actor 收 Post 返回失败。
- `DestroyNow` 只调用一次 `IDestroy`。
- `IDestroy` 重入 DestroyActor 不会二次销毁。
- Disabled Actor 不执行生命周期。
- Disabled Actor 邮件行为由策略控制。
- 所有生命周期顺序有单元测试覆盖。

---

# 5. Actor Mail 策略继续细化

## 5.1 问题

Actor Mail 是 Actor 系统的核心消息机制。

现有邮箱需要继续细化：

```text
邮箱容量
邮箱满载策略
Disabled Actor 是否收邮件
PendingDestroy Actor 是否收邮件
LatestOnly
Merge
Priority
Debug 统计
```

---

## 5.2 ActorMailOptions

```csharp
public readonly struct ActorMailOptions
{
    public readonly int Capacity;

    public readonly ActorMailFullPolicy FullPolicy;

    public readonly ActorMailDisabledPolicy DisabledPolicy;

    public readonly ActorMailPendingDestroyPolicy PendingDestroyPolicy;

    public readonly ActorMailDeliveryMode DeliveryMode;

    public ActorMailOptions(
        int capacity,
        ActorMailFullPolicy fullPolicy,
        ActorMailDisabledPolicy disabledPolicy,
        ActorMailPendingDestroyPolicy pendingDestroyPolicy,
        ActorMailDeliveryMode deliveryMode)
    {
        // capacity 参数：
        // 单个 Actor 单个事件类型邮箱容量。
        //
        // fullPolicy 参数：
        // 邮箱满载时的处理策略。
        //
        // disabledPolicy 参数：
        // Actor Disabled 时是否接收邮件。
        //
        // pendingDestroyPolicy 参数：
        // Actor PendingDestroy 时是否接收邮件。
        //
        // deliveryMode 参数：
        // 邮件投递模式，例如普通队列、只保留最新、合并。
        Capacity = capacity;
        FullPolicy = fullPolicy;
        DisabledPolicy = disabledPolicy;
        PendingDestroyPolicy = pendingDestroyPolicy;
        DeliveryMode = deliveryMode;
    }
}
```

---

## 5.3 ActorMailFullPolicy

```csharp
public enum ActorMailFullPolicy
{
    Reject = 0,

    DropOldest = 1,

    DropNewest = 2,

    OverwriteLatest = 3
}
```

说明：

- `Reject`：邮箱满时拒绝新消息。
- `DropOldest`：丢弃最旧消息，写入新消息。
- `DropNewest`：丢弃新消息，保留旧消息。
- `OverwriteLatest`：覆盖当前最新消息。

---

## 5.4 ActorMailDeliveryMode

```csharp
public enum ActorMailDeliveryMode
{
    Queue = 0,

    LatestOnly = 1,

    Merge = 2
}
```

说明：

- `Queue`：普通队列模式。
- `LatestOnly`：只保留最新值。
- `Merge`：合并旧消息和新消息。

---

## 5.5 ActorMailDisabledPolicy

```csharp
public enum ActorMailDisabledPolicy
{
    Accept = 0,

    Reject = 1
}
```

说明：

- `Accept`：Disabled Actor 仍可收邮件。
- `Reject`：Disabled Actor 拒收邮件。

推荐默认：

```text
Accept
```

理由：

```text
Disabled 只表示不执行生命周期，不一定表示不接收外部状态变化。
```

---

## 5.6 ActorMailPendingDestroyPolicy

```csharp
public enum ActorMailPendingDestroyPolicy
{
    Reject = 0
}
```

说明：

```text
PendingDestroy Actor 默认只能 Reject。
```

不建议允许 PendingDestroy Actor 继续收邮件。

---

## 5.7 Merge 策略

```csharp
public interface IActorMailMerge<TEvent>
    where TEvent : struct
{
    static abstract TEvent Merge(
        in TEvent oldValue,
        in TEvent newValue);
}
```

参数说明：

- `oldValue`：邮箱里已有的事件值。
- `newValue`：新投递的事件值。
- 返回值：合并后的事件值。

示例：

```csharp
public readonly struct DamageEvent : IActorMailMerge<DamageEvent>
{
    public readonly int Value;

    public DamageEvent(int value)
    {
        // value 参数：
        // 本次伤害值。
        Value = value;
    }

    public static DamageEvent Merge(
        in DamageEvent oldValue,
        in DamageEvent newValue)
    {
        // oldValue 参数：
        // 已缓存的伤害。
        //
        // newValue 参数：
        // 新来的伤害。
        //
        // 必要逻辑：
        // 合并伤害时直接累加。
        return new DamageEvent(oldValue.Value + newValue.Value);
    }
}
```

---

## 5.8 LatestOnly 策略

适合事件：

```text
位置刷新
UI 数值刷新
血条刷新
目标锁定刷新
当前状态刷新
```

逻辑：

```text
邮箱中已有值时，直接覆盖旧值。
Pump 时只处理最新值。
```

---

## 5.9 PostResult 细化

```csharp
public enum ActorPostStatus
{
    Success = 0,

    ActorNotFound = 1,

    ActorNotAlive = 2,

    ActorDisabledRejected = 3,

    ActorPendingDestroy = 4,

    MailFullRejected = 5,

    EventNotSupported = 6
}
```

`PostResult` 应包含：

```csharp
public readonly struct PostResult
{
    public readonly ActorPostStatus Status;

    public readonly string? Message;

    public bool IsSuccess => Status == ActorPostStatus.Success;
}
```

---

## 5.10 验收标准

- Queue / LatestOnly / Merge 三种模式都有测试。
- Disabled Actor 是否收邮件由策略决定。
- PendingDestroy Actor 永远拒收邮件。
- 邮箱满载策略都有测试。
- Merge 事件可以正确合并。
- PostResult 能区分失败原因。
- Benchmark 覆盖 Queue / LatestOnly / Merge。

---

# 6. Query 命中数量统计

## 6.1 问题

业务层经常需要判断 Query 结果数量。

例如：

```text
当前场上还有多少敌人？
是否还有任务目标？
是否存在可交互物？
当前 Query 是否为空？
```

如果只能 ForEach 计数，业务代码会重复写样板逻辑。

---

## 6.2 API 设计

```csharp
public static class ActorQueryCountExtensions
{
    public static int CountAlive(this ActorQueryResult query)
    {
        // query 参数：
        // 要统计的 Query 结果。
        //
        // 返回值：
        // Query 命中的 Alive Actor 数量。
        query = query.RefreshIfNeeded();

        int count = 0;

        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            count += archetype.CountAlive();
        }

        return count;
    }

    public static int CountEnabled(this ActorQueryResult query)
    {
        // query 参数：
        // 要统计的 Query 结果。
        //
        // 返回值：
        // Query 命中的 Enabled Actor 数量。
        query = query.RefreshIfNeeded();

        int count = 0;

        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            count += archetype.CountEnabled();
        }

        return count;
    }

    public static bool IsEmpty(this ActorQueryResult query)
    {
        // query 参数：
        // 要判断是否为空的 Query 结果。
        //
        // 必要逻辑：
        // 可以提前返回，不必完整计数。
        query = query.RefreshIfNeeded();

        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            if (archetype.HasAnyAlive())
            {
                return false;
            }
        }

        return true;
    }
}
```

---

## 6.3 Storage 统计

```csharp
public override int CountAlive()
{
    // CountAlive 的作用：
    // 统计当前 Storage 中 Alive Actor 数量。
    //
    // 必要逻辑：
    // 第一阶段可以直接扫描 states。
    // 后续如果 Benchmark 显示计数频繁，可以维护 aliveCount 字段。
    int count = 0;

    int maxSlot = Math.Min(_nextSlotIndex, _actors.Length);

    for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
    {
        if (_states[slotIndex] == ActorSlotState.Alive)
        {
            count++;
        }
    }

    return count;
}
```

---

## 6.4 可选缓存计数

后续可在 Storage 中维护：

```csharp
private int _aliveCount;
private int _enabledCount;
private int _pendingDestroyCount;
```

规则：

```text
AllocateSlot -> aliveCount++
SetEnable(false) -> enabledCount--
SetEnable(true) -> enabledCount++
MarkPendingDestroy -> aliveCount--, pendingDestroyCount++
DestroyNow -> pendingDestroyCount--
```

第一阶段建议直接扫描。  
如果 Benchmark 显示 Count 查询很频繁，再维护缓存计数。

---

## 6.5 验收标准

- `CountAlive` 返回正确数量。
- `CountEnabled` 返回正确数量。
- `IsEmpty` 能提前返回。
- DestroyActor 后统计正确。
- SetEnable 后统计正确。
- QueryResult 过期时统计会刷新。
- Benchmark 覆盖 CountAlive / CountEnabled / IsEmpty。

---

# 7. Actor Pool 的更严格版本

## 7.1 问题

Actor Pool 引入后，必须避免：

```text
池无限增长。
池中对象状态未清理。
场景切换后池残留过多对象。
无法知道池是否真的有效。
usePool: true 误用于非 IPooledActor。
```

因此需要严格化 Actor Pool。

---

## 7.2 Pool API

```csharp
public void PrewarmPool<TActor>(int count)
    where TActor : class, IActor, IPooledActor, new()
{
    // count 参数：
    // 要预热的 Actor 数量。
    //
    // 必要逻辑：
    // 预热用于提前创建高频对象，避免战斗中突发分配。
    ActorPoolCache<TActor>.Pool.Prewarm(count);
}

public void SetPoolLimit<TActor>(int maxCount)
    where TActor : class, IActor, IPooledActor, new()
{
    // maxCount 参数：
    // 当前 Actor 类型池内最多保留多少个空闲对象。
    //
    // 必要逻辑：
    // 限制池无限增长。
    ActorPoolCache<TActor>.Pool.SetLimit(maxCount);
}

public ActorPoolStats GetPoolStats<TActor>()
    where TActor : class, IActor, IPooledActor, new()
{
    // 返回当前 Actor 类型对象池统计信息。
    return ActorPoolCache<TActor>.Pool.GetStats();
}

public void ClearPool<TActor>()
    where TActor : class, IActor, IPooledActor, new()
{
    // 清空指定 Actor 类型对象池。
    ActorPoolCache<TActor>.Pool.Clear();
}
```

---

## 7.3 ActorPoolStats

```csharp
public readonly struct ActorPoolStats
{
    public readonly int CreatedTotal;

    public readonly int RentTotal;

    public readonly int ReturnTotal;

    public readonly int AvailableCount;

    public readonly int DroppedOnReturn;

    public readonly int MaxRetained;

    public ActorPoolStats(
        int createdTotal,
        int rentTotal,
        int returnTotal,
        int availableCount,
        int droppedOnReturn,
        int maxRetained)
    {
        // createdTotal 参数：
        // 池创建过的总对象数。
        //
        // rentTotal 参数：
        // 从池中租用对象的总次数。
        //
        // returnTotal 参数：
        // 归还池的总次数。
        //
        // availableCount 参数：
        // 当前池中可复用对象数量。
        //
        // droppedOnReturn 参数：
        // 因超过池容量而丢弃的归还对象数量。
        //
        // maxRetained 参数：
        // 池最多保留的空闲对象数量。
        CreatedTotal = createdTotal;
        RentTotal = rentTotal;
        ReturnTotal = returnTotal;
        AvailableCount = availableCount;
        DroppedOnReturn = droppedOnReturn;
        MaxRetained = maxRetained;
    }
}
```

---

## 7.4 严格 ActorPool

```csharp
internal sealed class ActorPool<TActor>
    where TActor : class, IActor, IPooledActor, new()
{
    private readonly Stack<TActor> _items = new();

    private int _maxRetained = 1024;

    private int _createdTotal;

    private int _rentTotal;

    private int _returnTotal;

    private int _droppedOnReturn;

    public TActor Rent()
    {
        // Rent 表示从池中取出 Actor。
        // 如果池为空，则创建新实例。
        TActor actor;

        if (_items.Count > 0)
        {
            actor = _items.Pop();
        }
        else
        {
            actor = new TActor();
            _createdTotal++;
        }

        _rentTotal++;

        actor.OnRent();

        return actor;
    }

    public void Return(TActor actor)
    {
        // actor 参数：
        // 即将归还对象池的 Actor。
        //
        // 必要逻辑：
        // 先调用 OnReturn 清理状态。
        // 如果池已达到上限，则不再保留该对象。
        if (actor == null)
        {
            return;
        }

        actor.OnReturn();

        _returnTotal++;

        if (_items.Count >= _maxRetained)
        {
            _droppedOnReturn++;
            return;
        }

        _items.Push(actor);
    }

    public void Prewarm(int count)
    {
        // count 参数：
        // 需要预热到的可用对象数量。
        //
        // 必要逻辑：
        // 只补足差额，不重复创建超过目标数量。
        while (_items.Count < count && _items.Count < _maxRetained)
        {
            _items.Push(new TActor());
            _createdTotal++;
        }
    }

    public void SetLimit(int maxRetained)
    {
        // maxRetained 参数：
        // 池最多保留的空闲对象数量。
        //
        // 必要逻辑：
        // 如果当前池内对象超过新上限，直接裁剪。
        _maxRetained = Math.Max(0, maxRetained);

        while (_items.Count > _maxRetained)
        {
            _items.Pop();
            _droppedOnReturn++;
        }
    }

    public ActorPoolStats GetStats()
    {
        return new ActorPoolStats(
            createdTotal: _createdTotal,
            rentTotal: _rentTotal,
            returnTotal: _returnTotal,
            availableCount: _items.Count,
            droppedOnReturn: _droppedOnReturn,
            maxRetained: _maxRetained);
    }

    public void Clear()
    {
        // Clear 的作用：
        // 清空当前 Actor 类型的池。
        //
        // 必要逻辑：
        // 用于切场景、切关卡或释放内存。
        _items.Clear();
    }
}
```

---

## 7.5 CreateActor 约束更新

池化入口建议改成编译期约束重载：

```csharp
public TActor CreateActor<TActor>(bool usePool = false)
    where TActor : class, IActor, new()
{
    // usePool 参数：
    // false 表示默认非池化路径。
    // true 表示显式启用池化。
    //
    // 必要逻辑：
    // 该入口为了兼容旧 API 保留。
    // usePool 为 true 时运行时检查 IPooledActor。
    if (!usePool)
    {
        return RegisterActor(
            actor: new TActor(),
            createdFromPool: false);
    }

    if (typeof(IPooledActor).IsAssignableFrom(typeof(TActor)))
    {
        return RentAndRegisterPooledActor<TActor>();
    }

    throw new InvalidOperationException(
        $"Actor type {typeof(TActor).Name} must implement IPooledActor when usePool is true.");
}

private TActor RentAndRegisterPooledActor<TActor>()
    where TActor : class, IActor, new()
{
    // TActor 参数：
    // 要池化创建的 Actor 类型。
    //
    // 必要逻辑：
    // 由于 C# 泛型约束不能在运行时分支后收窄，
    // 具体实现可以通过内部泛型辅助类或反射缓存完成。
    // 对外 API 保持 CreateActor<TActor>(bool usePool = false) 简洁。
    throw new NotImplementedException();
}
```

实现时可以选择：

```text
方案 A：运行时检查 + 内部反射缓存调用严格 ActorPool<TActor>
方案 B：提供额外 CreatePooledActor<TActor>() where TActor : IPooledActor
```

推荐同时提供：

```csharp
public TActor CreatePooledActor<TActor>()
    where TActor : class, IActor, IPooledActor, new()
{
    // CreatePooledActor 是强类型池化创建入口。
    // 它能在编译期保证 TActor 实现 IPooledActor。
    return RegisterActor(
        actor: ActorPoolCache<TActor>.Pool.Rent(),
        createdFromPool: true);
}
```

这样：

```csharp
CreateActor<TActor>(usePool: true)
```

用于兼容用户要求的统一入口；

```csharp
CreatePooledActor<TActor>()
```

用于内部和性能敏感代码。

---

## 7.6 验收标准

- `CreateActor<TActor>()` 默认不池化。
- `CreateActor<TActor>(usePool: false)` 不池化。
- `CreateActor<TActor>(usePool: true)` 对非 `IPooledActor` 抛出明确异常。
- `CreatePooledActor<TActor>()` 编译期要求 `IPooledActor`。
- Pool 支持预热。
- Pool 支持容量上限。
- Pool 支持统计。
- Pool 支持清空。
- Pool 超过上限时归还对象会被丢弃。
- Pool Benchmark 能体现高频创建销毁收益。

---

# 8. Actor 邮箱 Pump 调度公平性

## 8.1 问题

Actor Mail Pump 需要避免某些消息源长期占用预算。

可能问题：

```text
某个 Event 类型消息过多，饿死其他 Event。
某个 Actor 邮箱过多，饿死其他 Actor。
PostAll 大量投递后，生命周期长期推迟。
某个 Bucket 总是有消息，其他 Bucket 处理不到。
```

---

## 8.2 目标

新增公平性约束：

```text
每帧总 Actor Mail 预算。
每个 EventBucket 每帧最多处理数量。
每个 Actor Slot 每帧最多处理数量。
Bucket 轮转 cursor。
Column 轮转 cursor。
Slot DirtyList 轮转。
Pump 统计输出。
```

---

## 8.3 ActorMailPumpOptions

```csharp
public readonly struct ActorMailPumpOptions
{
    public readonly int MaxTotalMailsPerPump;

    public readonly int MaxMailsPerBucketPerPump;

    public readonly int MaxMailsPerActorPerPump;

    public readonly int MaxEmptyBucketChecksPerPump;

    public ActorMailPumpOptions(
        int maxTotalMailsPerPump,
        int maxMailsPerBucketPerPump,
        int maxMailsPerActorPerPump,
        int maxEmptyBucketChecksPerPump)
    {
        // maxTotalMailsPerPump 参数：
        // ActorWorld 每帧最多处理多少封 Actor 邮件。
        //
        // maxMailsPerBucketPerPump 参数：
        // 单个 EventBucket 每帧最多处理多少封邮件。
        //
        // maxMailsPerActorPerPump 参数：
        // 单个 Actor 每帧最多处理多少封邮件。
        //
        // maxEmptyBucketChecksPerPump 参数：
        // 每帧最多检查多少个空 Bucket，避免大量空桶浪费时间。
        MaxTotalMailsPerPump = maxTotalMailsPerPump;
        MaxMailsPerBucketPerPump = maxMailsPerBucketPerPump;
        MaxMailsPerActorPerPump = maxMailsPerActorPerPump;
        MaxEmptyBucketChecksPerPump = maxEmptyBucketChecksPerPump;
    }

    public static ActorMailPumpOptions Default => new(
        maxTotalMailsPerPump: 1024,
        maxMailsPerBucketPerPump: 128,
        maxMailsPerActorPerPump: 8,
        maxEmptyBucketChecksPerPump: 64);
}
```

---

## 8.4 ActorMailPumpStats

```csharp
public readonly struct ActorMailPumpStats
{
    public readonly int ProcessedTotal;

    public readonly int BucketLimitHits;

    public readonly int ActorLimitHits;

    public readonly int EmptyBucketChecks;

    public readonly int RemainingDirtyBuckets;

    public ActorMailPumpStats(
        int processedTotal,
        int bucketLimitHits,
        int actorLimitHits,
        int emptyBucketChecks,
        int remainingDirtyBuckets)
    {
        // processedTotal 参数：
        // 本帧处理的 Actor 邮件总数。
        //
        // bucketLimitHits 参数：
        // 有多少次因为 Bucket 限制停止处理。
        //
        // actorLimitHits 参数：
        // 有多少次因为单 Actor 限制停止处理。
        //
        // emptyBucketChecks 参数：
        // 本帧检查到的空 Bucket 数量。
        //
        // remainingDirtyBuckets 参数：
        // 本帧结束后仍有积压的 Bucket 数量。
        ProcessedTotal = processedTotal;
        BucketLimitHits = bucketLimitHits;
        ActorLimitHits = actorLimitHits;
        EmptyBucketChecks = emptyBucketChecks;
        RemainingDirtyBuckets = remainingDirtyBuckets;
    }
}
```

---

## 8.5 PumpOne 调整方向

当前 `PumpOne` 每次处理一封邮件是安全的。  
公平性增强不一定要大改结构，可以在外层加限制：

```csharp
private ActorMailPumpStats PumpActorBehaviours(
    ref RuntimeFrameBudget budget,
    ActorMailPumpOptions options)
{
    // budget 参数：
    // 当前帧剩余预算。
    //
    // options 参数：
    // Actor Mail Pump 公平性配置。
    //
    // 必要逻辑：
    // 总预算限制由 RuntimeFrameBudget 和 MaxTotalMailsPerPump 共同控制。
    // Bucket 和 Actor 限制用于避免局部消息源饿死其他消息源。
    int processedTotal = 0;
    int emptyChecks = 0;

    while (budget.HasRemainingEventBudget()
           && processedTotal < options.MaxTotalMailsPerPump)
    {
        if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
        {
            break;
        }

        PumpOneResult result = TryPumpOneFair(options);

        if (result.Status == PumpOneStatus.Processed)
        {
            processedTotal++;
            continue;
        }

        if (result.Status == PumpOneStatus.EmptyBucket)
        {
            emptyChecks++;

            if (emptyChecks >= options.MaxEmptyBucketChecksPerPump)
            {
                break;
            }

            continue;
        }

        if (result.Status == PumpOneStatus.NoWork)
        {
            break;
        }
    }

    return new ActorMailPumpStats(
        processedTotal: processedTotal,
        bucketLimitHits: 0,
        actorLimitHits: 0,
        emptyBucketChecks: emptyChecks,
        remainingDirtyBuckets: CountDirtyBuckets());
}
```

---

## 8.6 Per Actor Limit

要限制单 Actor 每帧处理数量，需要 EventColumn 或 ActorWorld 记录本帧 Actor 处理次数。

第一阶段可以做轻量方案：

```text
只统计单 Column 内同一 Slot 连续处理数量。
超过 MaxMailsPerActorPerPump 后，将该 Slot 放回 dirty 队列尾部。
```

这样不用建立全局 ActorId -> Count 字典。

---

## 8.7 DirtySlotList 需要支持轮转

当前 DirtySlotList 如果只 Peek / Pop，可能某个 Slot 长期占头部。

需要支持：

```csharp
MoveHeadToTail();
```

用途：

```text
当前 Slot 达到单帧处理上限后，把它移到队尾，让其他 Slot 有机会执行。
```

---

## 8.8 验收标准

- 单个 EventBucket 消息过多不会永久饿死其他 Bucket。
- 单个 Actor 邮箱过多不会永久饿死其他 Actor。
- Pump 总量受 MaxTotalMailsPerPump 限制。
- 空 Bucket 扫描受 MaxEmptyBucketChecksPerPump 限制。
- Actor Mail Pump 输出统计。
- Debug Dump 能显示邮箱积压。
- Benchmark 覆盖高偏斜消息场景。

---

# 9. 实施阶段

## Phase 1：ActorId Debug 诊断

任务：

1. 新增 `ActorDebugInfo`。
2. 新增 `GetDebugInfo(ActorId)`。
3. 新增 `DescribeActor(ActorId)`。
4. 新增 `DumpActorWorld()`。
5. 新增 `DumpQuery(ActorQueryResult)`。

验收：

```text
无效 ActorId 有明确失败原因
已销毁 ActorId 能显示 Generation 不匹配
Debug 输出包含 Actor 类型、Tag、Group、生命周期、邮箱积压
```

---

## Phase 2：生命周期严格语义

任务：

1. 明确生命周期顺序。
2. 补充 `Destroying` 状态。
3. PendingDestroy Actor 禁止生命周期。
4. PendingDestroy Actor 禁止收邮件。
5. DestroyNow 防重入。
6. 补齐生命周期单元测试。

验收：

```text
IStart 中可访问 Context
DestroyActor 后本帧不再执行后续生命周期
IDestroy 只调用一次
DestroyNow 清空邮件
```

---

## Phase 3：Actor Mail 策略细化

任务：

1. 扩展 `ActorMailOptions`。
2. 增加 `ActorMailDeliveryMode`。
3. 增加 `ActorMailDisabledPolicy`。
4. 增加 `ActorMailPendingDestroyPolicy`。
5. 增加 LatestOnly。
6. 增加 Merge。
7. 细化 PostResult。

验收：

```text
Queue / LatestOnly / Merge 都有测试
Disabled Actor 邮件策略可配置
PendingDestroy Actor 永远拒收邮件
PostResult 能区分失败原因
```

---

## Phase 4：Query 命中数量统计

任务：

1. 新增 `CountAlive()`。
2. 新增 `CountEnabled()`。
3. 新增 `IsEmpty()`。
4. 增加 Query Count Benchmark。
5. 可选维护 Storage 级计数字段。

验收：

```text
DestroyActor 后统计正确
SetEnable 后统计正确
IsEmpty 可提前返回
```

---

## Phase 5：严格 Actor Pool

任务：

1. 增加 `PrewarmPool<TActor>()`。
2. 增加 `SetPoolLimit<TActor>()`。
3. 增加 `GetPoolStats<TActor>()`。
4. 增加 `ClearPool<TActor>()`。
5. 增加 `CreatePooledActor<TActor>()`。
6. 池容量上限与统计。
7. Pool Benchmark。

验收：

```text
Pool 可预热
Pool 可限量
Pool 可统计
Pool 可清理
usePool: true 非 IPooledActor 抛明确异常
```

---

## Phase 6：Actor Mail Pump 公平性

任务：

1. 新增 `ActorMailPumpOptions`。
2. 新增 `ActorMailPumpStats`。
3. 增加总邮件处理上限。
4. 增加 Bucket 处理上限。
5. 增加 Actor Slot 处理上限。
6. DirtySlotList 支持 MoveHeadToTail。
7. 增加偏斜消息 Benchmark。

验收：

```text
单 Bucket 不饿死其他 Bucket
单 Actor 不饿死其他 Actor
Pump 输出统计
偏斜场景 Benchmark 可复现
```

---

# 10. Agent 执行指令

```text
You are working on the LayerBase repository.

Goal:
Harden the Actor runtime after the core Actor engineering design is implemented.

Implement these six areas:
1. ActorId safety and debug diagnostics.
2. Strict actor lifecycle ordering semantics.
3. More detailed Actor Mail policies.
4. Query hit count statistics.
5. Stricter Actor Pool.
6. Actor Mail Pump fairness.

Do not implement:
- AOI
- SpatialCellId
- BVH
- quadtree
- grid index
- radius query
- nearest query
- position based query
- low-frequency lifecycle scheduling
- actor multi-threaded execution

Required APIs:
- GetDebugInfo(ActorId)
- DescribeActor(ActorId)
- DumpActorWorld()
- DumpQuery(ActorQueryResult)
- CountAlive()
- CountEnabled()
- IsEmpty()
- PrewarmPool<TActor>(int count)
- SetPoolLimit<TActor>(int maxCount)
- GetPoolStats<TActor>()
- ClearPool<TActor>()
- CreatePooledActor<TActor>()
- ActorMailPumpOptions
- ActorMailPumpStats

Required checks:
- dotnet test
- dotnet build -c Release
- dotnet run -c Release --project LayerBase.BenchMark

Constraints:
- Keep old APIs compatible.
- Keep hot paths allocation-free where practical.
- Debug APIs may allocate.
- Prefer generic static caches.
- Do not introduce Unity or Godot dependencies into core LayerBase.
```

---

## 11. 最终建议

这 6 项做完后，Actor 系统的工程质量会明显提升：

```text
能定位 ActorId 问题
能解释生命周期行为
能控制邮箱背压和合并
能统计 Query 命中
能安全使用 Actor Pool
能避免邮箱 Pump 饥饿
```

优先级建议：

```text
P0:
- 生命周期严格语义
- ActorId Debug 诊断

P1:
- Actor Mail 策略细化
- Actor Pool 严格版本

P2:
- Query 命中数量统计
- Actor Mail Pump 公平性
```

其中生命周期严格语义和 Debug 诊断应该最先做。

原因：

```text
生命周期语义不清会导致业务行为不稳定。
Debug 能力不足会导致后续所有问题都难定位。
```
