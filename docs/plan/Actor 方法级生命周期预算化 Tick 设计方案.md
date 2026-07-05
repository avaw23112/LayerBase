# Actor 方法级生命周期预算化 Tick 设计方案

> Status: Active
> Scope: Actor lifecycle method scheduling
> Goal: 支持同一个 Actor 内多个不同频率的 Update / LateUpdate / FixedUpdate 方法
> Core constraint: 每个 Tick 方法执行一次都必须消耗 `RuntimeFrameBudget`
> Non-goal: 不做 Actor 多线程，不重写 EventStream，不修改 Post / Ask 语义

---

## 1. 背景

前一版设计把 `TickTier` 放在 Actor 类上：

```csharp
[ActorTick(TickTier.Warm)]
public sealed class MonsterActor : IActor, IUpdate
{
}
```

这个设计只能表达：

```text
这个 Actor 整体是 Hot / Warm / Cold。
```

但真实业务里，一个 Actor 往往同时包含多种节奏：

```text
战斗状态：Hot
感知 / AI 决策：Warm
低频维护 / 资源恢复：Cold
```

所以 Tick 特性不应该放在 Actor 类上，而应该放在生命周期方法上。

最终设计目标：

```text
一个 Actor 可以拥有多个 Update 方法。
每个 Update 方法可以有独立 TickTier。
每个方法执行一次都消耗 RuntimeFrameBudget。
```

---

## 2. 当前代码基础

当前 Actor 生命周期注册逻辑是按接口判断：

```csharp
if (actor is IUpdate update)
{
    handles.Update = world.Lifecycle.AddUpdate(actorId, update);
}

if (actor is ILateUpdate lateUpdate)
{
    handles.LateUpdate = world.Lifecycle.AddLateUpdate(actorId, lateUpdate);
}

if (actor is IFixedUpdate fixedUpdate)
{
    handles.FixedUpdate = world.Lifecycle.AddFixedUpdate(actorId, fixedUpdate);
}
```

这说明当前模型是：

```text
一个 Actor 最多一个 IUpdate
一个 Actor 最多一个 ILateUpdate
一个 Actor 最多一个 IFixedUpdate
```

当前 `ActorLifecycleFreeList.PumpBudgeted` 已经具备预算化能力：

```text
1. 检查剩余预算。
2. 检查时间预算。
3. 调用生命周期方法。
4. 每调用一次后 ConsumeEvent。
```

并且注释已经说明：这里的 `RuntimeFrameBudget` 虽然字段叫 Event，但实际作为 WorkUnit 使用。

因此，本设计不是新增第二套预算系统，而是新增**方法级生命周期入口**，并继续复用现有 `RuntimeFrameBudget`。

---

## 3. 核心设计结论

### 3.1 TickTier 不绑定 Actor

不推荐：

```csharp
[ActorTick(TickTier.Warm)]
public sealed class MonsterActor : IActor
{
}
```

原因：

```text
Actor 类级 TickTier 只能控制整个 Actor。
无法表达同一个 Actor 内多个不同频率的生命周期逻辑。
```

---

### 3.2 TickTier 绑定生命周期方法

推荐：

```csharp
public sealed partial class MonsterActor : IActor
{
    [ActorUpdate(TickTier.Hot)]
    private void CombatUpdate(float deltaTime)
    {
        // 战斗状态、受击、技能释放。
    }

    [ActorUpdate(TickTier.Warm)]
    private void AiDecisionUpdate(float deltaTime)
    {
        // 感知、寻敌、仇恨刷新。
    }

    [ActorUpdate(TickTier.Cold)]
    private void MaintenanceUpdate(float deltaTime)
    {
        // 低频维护、脱战计时、资源恢复。
    }
}
```

开发者表达的是：

```text
这个方法是什么优先级。
```

而不是：

```text
这个 Actor 整体是什么优先级。
```

---

## 4. 对外 API 设计

### 4.1 TickTier

```csharp
public enum TickTier
{
    Hot,
    Warm,
    Cold,
    Dormant
}
```

语义：

```text
Hot：高优先级，每帧进入候选，但仍受预算限制。
Warm：中优先级，按相位进入候选，预算不足可延后。
Cold：低优先级，只吃剩余预算，预算不足可跳过。
Dormant：不主动 Tick，但 Actor 仍可接收 Post / Ask。
```

---

### 4.2 ActorUpdateAttribute

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ActorUpdateAttribute : Attribute
{
    public TickTier Tier { get; }

    public int Phase { get; init; } = -1;

    public ActorUpdateAttribute(TickTier tier = TickTier.Hot)
    {
        Tier = tier;
    }
}
```

用法：

```csharp
[ActorUpdate(TickTier.Warm)]
private void Sense(float deltaTime)
{
}
```

---

### 4.3 ActorLateUpdateAttribute

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ActorLateUpdateAttribute : Attribute
{
    public TickTier Tier { get; }

    public int Phase { get; init; } = -1;

    public ActorLateUpdateAttribute(TickTier tier = TickTier.Hot)
    {
        Tier = tier;
    }
}
```

用法：

```csharp
[ActorLateUpdate(TickTier.Hot)]
private void RefreshView(float deltaTime)
{
}
```

---

### 4.4 ActorFixedUpdateAttribute

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ActorFixedUpdateAttribute : Attribute
{
    public TickTier Tier { get; }

    public int Phase { get; init; } = -1;

    public ActorFixedUpdateAttribute(TickTier tier = TickTier.Hot)
    {
        Tier = tier;
    }
}
```

第一版建议限制：

```text
FixedUpdate 默认 Hot。
FixedUpdate Warm / Cold 可以先禁止，或允许但给 Analyzer warning。
```

原因：

```text
FixedUpdate 往往涉及固定步长模拟、同步、物理或高一致性逻辑。
低频化容易造成语义误用。
```

---

## 5. 方法签名规则

第一版只支持一种签名：

```csharp
void Method(float deltaTime)
```

合法：

```csharp
[ActorUpdate(TickTier.Warm)]
private void Sense(float deltaTime)
{
}
```

非法：

```csharp
[ActorUpdate(TickTier.Warm)]
private void Sense()
{
}

[ActorUpdate(TickTier.Warm)]
private int Sense(float deltaTime)
{
    return 0;
}

[ActorUpdate(TickTier.Warm)]
private async LBTask Sense(float deltaTime)
{
}
```

第一版约束：

```text
1. 必须是实例方法。
2. 返回值必须是 void。
3. 参数必须是一个 float deltaTime。
4. 可以是 private。
5. Actor 类型建议 partial，便于源生成器访问。
```

---

## 6. 与旧接口的关系

### 6.1 旧接口继续可用

旧写法保持兼容：

```csharp
public sealed class PlayerActor : IActor, IUpdate
{
    public void Update(float deltaTime)
    {
        // 默认 Hot。
    }
}
```

`IUpdate.Update` 等价于：

```text
[ActorUpdate(TickTier.Hot)]
```

`ILateUpdate.LateUpdate` 等价于：

```text
[ActorLateUpdate(TickTier.Hot)]
```

`IFixedUpdate.FixedUpdate` 等价于：

```text
[ActorFixedUpdate(TickTier.Hot)]
```

---

### 6.2 新写法支持多个方法

```csharp
public sealed partial class MonsterActor : IActor
{
    [ActorUpdate(TickTier.Hot)]
    private void CombatUpdate(float dt)
    {
    }

    [ActorUpdate(TickTier.Warm)]
    private void SensorUpdate(float dt)
    {
    }

    [ActorUpdate(TickTier.Cold)]
    private void MaintenanceUpdate(float dt)
    {
    }
}
```

---

### 6.3 共存规则

允许共存：

```csharp
public sealed partial class MonsterActor : IActor, IUpdate
{
    public void Update(float dt)
    {
        // 默认 Hot。
    }

    [ActorUpdate(TickTier.Warm)]
    private void SensorUpdate(float dt)
    {
    }

    [ActorUpdate(TickTier.Cold)]
    private void MaintenanceUpdate(float dt)
    {
    }
}
```

但文档建议：

```text
如果一个 Actor 有多个 Update 节奏，推荐全部使用方法级 Attribute。
```

原因：

```text
一个 Actor 同时使用 IUpdate 和多个 [ActorUpdate] 虽然合法，但可读性会变差。
```

---

## 7. 业务使用场景

### 7.1 怪物 Actor：三种 Update

```csharp
public sealed partial class MonsterActor : IActor
{
    [ActorUpdate(TickTier.Hot)]
    private void CombatUpdate(float dt)
    {
        // 当前攻击、硬直、受击、技能释放。
    }

    [ActorUpdate(TickTier.Warm)]
    private void AiDecisionUpdate(float dt)
    {
        // 目标搜索、仇恨刷新、状态切换。
    }

    [ActorUpdate(TickTier.Cold)]
    private void MaintenanceUpdate(float dt)
    {
        // 脱战计时、资源恢复、低频状态维护。
    }
}
```

开发者不需要拆成三个 Actor，也不需要手写：

```csharp
if (frame % 3 == 0)
{
}
```

---

### 7.2 NPC Actor：感知 + 日常刷新

```csharp
public sealed partial class NpcActor : IActor
{
    [ActorUpdate(TickTier.Warm)]
    private void LookAround(float dt)
    {
        // 检查玩家是否靠近。
    }

    [ActorUpdate(TickTier.Cold)]
    private void RefreshDailyState(float dt)
    {
        // 刷新对白、任务提示、闲逛状态。
    }
}
```

---

### 7.3 UI Actor：表现刷新分层

```csharp
public sealed partial class NameplateActor : IActor
{
    [ActorLateUpdate(TickTier.Warm)]
    private void RefreshScreenPosition(float dt)
    {
        // 名字牌跟随屏幕位置。
    }

    [ActorUpdate(TickTier.Cold)]
    private void RefreshText(float dt)
    {
        // 血量文字、状态图标、称号低频刷新。
    }
}
```

---

### 7.4 Dormant：不主动 Tick，但仍接收消息

```csharp
public sealed partial class DoorActor : IActor
{
    [ActorUpdate(TickTier.Dormant)]
    private void Idle(float dt)
    {
        // 默认不会主动执行。
    }

    [ActorBehaviour]
    private void OnOpen(in OpenDoorEvent e)
    {
        Open();
    }
}
```

Dormant 语义：

```text
不主动 Tick。
但仍然可以接收 Post / Ask / Dispatch。
```

---

## 8. Runtime 内部模型

### 8.1 当前模型

当前调度器存的是接口实例：

```text
ActorLifecycleFreeList<IUpdate>
ActorLifecycleFreeList<ILateUpdate>
ActorLifecycleFreeList<IFixedUpdate>
```

每个 Actor 每种生命周期最多一个 handle。

---

### 8.2 目标模型

新增方法级生命周期存储：

```text
ActorLifecycleMethodTickLane
  Hot: ActorLifecycleMethodFreeList
  WarmBuckets: ActorLifecycleMethodFreeList[]
  ColdBuckets: ActorLifecycleMethodFreeList[]
  Dormant: ActorLifecycleMethodFreeList
```

每个带 Attribute 的方法都会独立注册为一个生命周期条目。

例如：

```text
MonsterActor.CombatUpdate      -> Update Hot lane
MonsterActor.AiDecisionUpdate  -> Update Warm lane
MonsterActor.MaintenanceUpdate -> Update Cold lane
```

---

## 9. Method Entry 结构

```csharp
internal readonly struct ActorLifecycleMethodEntry
{
    public readonly ActorId ActorId;
    public readonly IActor Actor;
    public readonly ActorLifecycleMethodInvoker Invoker;

    public ActorLifecycleMethodEntry(
        ActorId actorId,
        IActor actor,
        ActorLifecycleMethodInvoker invoker)
    {
        ActorId = actorId;
        Actor = actor;
        Invoker = invoker;
    }
}
```

Invoker：

```csharp
internal delegate void ActorLifecycleMethodInvoker(
    IActor actor,
    float deltaTime);
```

生成器为每个方法生成静态 invoker：

```csharp
private static void Invoke_CombatUpdate(IActor actor, float deltaTime)
{
    ((MonsterActor)actor).CombatUpdate(deltaTime);
}
```

---

## 10. Method FreeList

新增：

```csharp
internal sealed class ActorLifecycleMethodFreeList
{
    private ActorLifecycleMethodEntry[] _entries;
    private int[] _versions;
    private bool[] _occupied;
    private int[] _free;
    private int _freeCount;
    private int _count;
    private int _cursor;

    public ActorLifecycleHandle Add(
        ActorId actorId,
        IActor actor,
        ActorLifecycleMethodInvoker invoker);

    public bool Remove(ActorLifecycleHandle handle);

    public void PumpBudgeted(
        ref LifecycleFrameState state,
        ref RuntimeFrameBudget budget,
        int timeCheckInterval);
}
```

`PumpBudgeted` 逻辑与当前 `ActorLifecycleFreeList<TLifecycle>.PumpBudgeted` 保持一致：

```text
1. 检查 WorkUnit 预算。
2. 检查时间预算。
3. cursor 续跑。
4. 判断 Actor 是否仍可运行。
5. 调用 invoker。
6. budget.ConsumeEvent()。
```

当前通用 FreeList 已经具备这些行为，可以直接参考实现。

---

## 11. Method TickLane

```csharp
internal sealed class ActorLifecycleMethodTickLane
{
    private readonly ActorLifecycleMethodFreeList _hot = new();
    private readonly ActorLifecycleMethodFreeList[] _warm;
    private readonly ActorLifecycleMethodFreeList[] _cold;
    private readonly ActorLifecycleMethodFreeList _dormant = new();

    public ActorLifecycleMethodTickLane(
        int warmBucketCount = 3,
        int coldBucketCount = 10)
    {
        _warm = CreateBuckets(warmBucketCount);
        _cold = CreateBuckets(coldBucketCount);
    }

    public ActorLifecycleHandle Add(
        ActorId actorId,
        IActor actor,
        ActorLifecycleMethodInvoker invoker,
        TickTier tier,
        int phase);

    public bool Remove(ActorLifecycleHandle handle);

    public void Pump(
        int frameIndex,
        ref LifecycleFrameState state,
        ref RuntimeFrameBudget budget,
        int timeCheckInterval);
}
```

Pump 规则：

```text
1. 先 Pump Hot。
2. 如果预算还有，再 Pump 当前 Warm bucket。
3. 如果预算还有，再 Pump 当前 Cold bucket。
4. Dormant 不主动 Pump。
```

---

## 12. ActorLifecycleScheduler 改造

当前：

```text
_updates: ActorLifecycleFreeList<IUpdate>
_lateUpdates: ActorLifecycleFreeList<ILateUpdate>
_fixedUpdates: ActorLifecycleFreeList<IFixedUpdate>
```

目标：

```text
_interfaceUpdates: ActorLifecycleTickLane<IUpdate>
_interfaceLateUpdates: ActorLifecycleTickLane<ILateUpdate>
_interfaceFixedUpdates: ActorLifecycleTickLane<IFixedUpdate>

_methodUpdates: ActorLifecycleMethodTickLane
_methodLateUpdates: ActorLifecycleMethodTickLane
_methodFixedUpdates: ActorLifecycleMethodTickLane
```

也就是说，保留接口生命周期，同时新增方法生命周期。

---

## 13. 注册流程

### 13.1 接口生命周期注册

保持现有逻辑：

```csharp
if (actor is IUpdate update)
{
    handles.Update = world.Lifecycle.AddUpdate(actorId, update);
}
```

---

### 13.2 方法生命周期注册

在 `TypedActorStorage.RegisterLifecycleInterfaces` 后新增方法注册：

```csharp
internal void RegisterLifecycleMethods(
    TActor actor,
    ActorId actorId,
    int slotIndex,
    ActorWorld world)
{
    if (_meta == null)
    {
        return;
    }

    ActorLifecycleHandle[]? methodHandles =
        _meta.RegisterLifecycleMethods(actor, actorId, world);

    _lifecycleHandles[slotIndex].Extra = methodHandles;
}
```

或者直接整合进当前方法：

```csharp
internal void RegisterLifecycleInterfaces(
    TActor actor,
    ActorId actorId,
    int slotIndex,
    ActorWorld world)
{
    ActorLifecycleHandles handles = ActorLifecycleHandles.Empty;

    // 旧接口生命周期。
    if (actor is IUpdate update)
    {
        handles.Update = world.Lifecycle.AddUpdate(actorId, update);
    }

    if (actor is ILateUpdate lateUpdate)
    {
        handles.LateUpdate = world.Lifecycle.AddLateUpdate(actorId, lateUpdate);
    }

    if (actor is IFixedUpdate fixedUpdate)
    {
        handles.FixedUpdate = world.Lifecycle.AddFixedUpdate(actorId, fixedUpdate);
    }

    // 新方法级生命周期。
    if (_meta != null)
    {
        handles.Extra = _meta.RegisterLifecycleMethods(actor, actorId, world);
    }

    _lifecycleHandles[slotIndex] = handles;

    if (actor is IStart start)
    {
        start.Start();
    }
}
```

---

## 14. Handle 存储改造

当前每个 slot 存一个 `ActorLifecycleHandles`。

方法级 Tick 后，一个 Actor 可能有多个 lifecycle method handle。

建议改为：

```csharp
internal struct ActorLifecycleHandles
{
    public ActorLifecycleHandle Update;
    public ActorLifecycleHandle LateUpdate;
    public ActorLifecycleHandle FixedUpdate;

    public ActorLifecycleHandle[]? Extra;
}
```

`Extra` 存：

```text
[ActorUpdate]
[ActorLateUpdate]
[ActorFixedUpdate]
```

所有方法级生命周期 handle。

这是创建/销毁路径，不是每帧热路径，因此 `ActorLifecycleHandle[]?` 可以接受。

---

## 15. 源生成器设计

### 15.1 生成器扫描目标

扫描 Actor partial class 中的方法：

```text
[ActorUpdate]
[ActorLateUpdate]
[ActorFixedUpdate]
```

要求签名：

```csharp
void Method(float deltaTime)
```

---

### 15.2 生成元数据

示例源代码：

```csharp
public sealed partial class MonsterActor : IActor
{
    [ActorUpdate(TickTier.Hot)]
    private void CombatUpdate(float dt) { }

    [ActorUpdate(TickTier.Warm)]
    private void SensorUpdate(float dt) { }

    [ActorUpdate(TickTier.Cold)]
    private void MaintenanceUpdate(float dt) { }
}
```

生成：

```csharp
internal static class MonsterActor_LifecycleMethods
{
    public static readonly ActorLifecycleMethodMeta[] Methods =
    {
        new ActorLifecycleMethodMeta(
            phase: ActorLifecyclePhase.Update,
            tier: TickTier.Hot,
            tickPhase: -1,
            invoker: Invoke_CombatUpdate),

        new ActorLifecycleMethodMeta(
            phase: ActorLifecyclePhase.Update,
            tier: TickTier.Warm,
            tickPhase: -1,
            invoker: Invoke_SensorUpdate),

        new ActorLifecycleMethodMeta(
            phase: ActorLifecyclePhase.Update,
            tier: TickTier.Cold,
            tickPhase: -1,
            invoker: Invoke_MaintenanceUpdate),
    };

    private static void Invoke_CombatUpdate(IActor actor, float dt)
    {
        ((MonsterActor)actor).CombatUpdate(dt);
    }

    private static void Invoke_SensorUpdate(IActor actor, float dt)
    {
        ((MonsterActor)actor).SensorUpdate(dt);
    }

    private static void Invoke_MaintenanceUpdate(IActor actor, float dt)
    {
        ((MonsterActor)actor).MaintenanceUpdate(dt);
    }
}
```

---

### 15.3 ActorTypeMeta 扩展

新增：

```csharp
internal sealed class ActorLifecycleMethodMeta
{
    public ActorLifecyclePhase Phase { get; }
    public TickTier Tier { get; }
    public int TickPhase { get; }
    public ActorLifecycleMethodInvoker Invoker { get; }
}
```

`ActorTypeMeta<TActor>` 增加：

```csharp
public IReadOnlyList<ActorLifecycleMethodMeta> LifecycleMethods { get; }
```

或者为了性能用数组：

```csharp
public ActorLifecycleMethodMeta[] LifecycleMethods { get; }
```

---

## 16. 调度语义

### 16.1 Update

执行顺序：

```text
Interface Update Hot
Method Update Hot
Interface Update Warm
Method Update Warm
Interface Update Cold
Method Update Cold
```

或者：

```text
All Hot Updates
All Warm Updates
All Cold Updates
```

推荐第二种，因为接口 Update 和方法 Update 都是 Update 语义，不应该人为割裂。

内部可以统一到同一个 lane：

```text
UpdateTickLane
  contains interface update entries
  contains method update entries
```

第一版实现简单起见，可以分两个 lane，但 Pump 顺序必须清楚。

---

### 16.2 LateUpdate

同 Update。

---

### 16.3 FixedUpdate

第一版建议：

```text
IFixedUpdate / [ActorFixedUpdate] 默认只支持 Hot。
Warm / Cold 给 Analyzer warning。
```

如果仍允许：

```text
必须明确不适合物理 / 同步关键逻辑。
```

---

## 17. RuntimeFrameBudget 语义

每个生命周期方法执行一次：

```text
消耗 1 个 WorkUnit。
```

例如：

```csharp
public sealed partial class MonsterActor : IActor
{
    [ActorUpdate(TickTier.Hot)]
    private void CombatUpdate(float dt) { }

    [ActorUpdate(TickTier.Warm)]
    private void SensorUpdate(float dt) { }

    [ActorUpdate(TickTier.Cold)]
    private void MaintenanceUpdate(float dt) { }
}
```

如果三者同一帧都被执行，则总共消耗：

```text
3 个 WorkUnit
```

如果预算只够执行 Hot：

```text
CombatUpdate 执行。
SensorUpdate 不执行。
MaintenanceUpdate 不执行。
```

---

## 18. 动态切换设计

方法级 Tick 后，动态切换不能只说：

```csharp
SetActorTickTier(actorId, TickTier.Warm)
```

因为一个 Actor 有多个 Tick 方法。

需要改成两类 API。

---

### 18.1 Actor 整体倍率 / 激活级别

用于 AOI：

```csharp
context.SetActorTickScale(actorId, ActorTickScale.Dormant);
context.SetActorTickScale(actorId, ActorTickScale.Reduced);
context.SetActorTickScale(actorId, ActorTickScale.Normal);
```

这个不改方法本身的 Tier，而是影响整个 Actor 的 Tick 参与度。

例如：

```text
ActorTickScale.Normal：
  方法按各自 Tier 运行。

ActorTickScale.Reduced：
  Hot 正常。
  Warm 降到 Cold。
  Cold 可跳过。

ActorTickScale.Dormant：
  所有方法级 Tick 都不主动执行。
  但消息仍然可接收。
```

第一版可以不做。

---

### 18.2 单方法动态切换

如果需要精细控制，需要方法句柄或方法名：

```csharp
context.SetActorMethodTickTier(
    actorId,
    "SensorUpdate",
    TickTier.Hot);
```

不推荐第一版做字符串 API。

更好的方式是生成方法 ID：

```csharp
public static class MonsterActorTickMethods
{
    public static readonly ActorTickMethodId SensorUpdate;
}
```

然后：

```csharp
context.SetActorMethodTickTier(
    actorId,
    MonsterActorTickMethods.SensorUpdate,
    TickTier.Hot);
```

但这会明显增加 API 复杂度。

---

### 18.3 第一版结论

第一版不做动态单方法迁移。

只支持：

```text
方法级静态 Tier。
Dormant 方法不主动 Tick。
Actor Enable / Disable 仍然可以控制整个 Actor 是否运行生命周期。
```

动态 AOI 后续用 `ActorTickScale` 做，而不是直接修改每个方法的 Tier。

---

## 19. 使用体验结论

### 19.1 顺畅写法

```csharp
public sealed partial class MonsterActor : IActor
{
    [ActorUpdate(TickTier.Hot)]
    private void Combat(float dt) { }

    [ActorUpdate(TickTier.Warm)]
    private void Think(float dt) { }

    [ActorUpdate(TickTier.Cold)]
    private void Maintain(float dt) { }
}
```

这个是顺的。

---

### 19.2 不推荐写法

```csharp
public sealed class MonsterActor : IActor, IUpdate
{
    public void Update(float dt)
    {
        Combat(dt);

        if (_frame % 3 == 0)
        {
            Think(dt);
        }

        if (_frame % 10 == 0)
        {
            Maintain(dt);
        }
    }
}
```

问题：

```text
1. 手写分帧不参与统一调度。
2. 无法被 RuntimeFrameBudget 精确控制。
3. 不利于框架统计 Hot / Warm / Cold。
4. 高压帧仍然可能爆。
```

---

## 20. 测试计划

### 20.1 生成器测试

```text
1. 能识别 [ActorUpdate] 方法。
2. 能识别 [ActorLateUpdate] 方法。
3. 能识别 [ActorFixedUpdate] 方法。
4. 非 void 方法报错。
5. 无 float 参数方法报错。
6. 多参数方法报错。
7. static 方法报错。
```

---

### 20.2 注册测试

```text
1. Actor 只实现 IUpdate，可正常注册。
2. Actor 只使用 [ActorUpdate]，可正常注册。
3. Actor 同时实现 IUpdate 和 [ActorUpdate]，两者都注册。
4. 一个 Actor 多个 [ActorUpdate]，全部注册。
```

---

### 20.3 预算测试

```text
1. 一个 Actor 三个方法，同帧全部执行时消耗 3 个 WorkUnit。
2. 预算只够 1 个 WorkUnit 时，只执行高优先级方法。
3. 预算不足时 Warm / Cold 不执行。
4. Dormant 方法不主动执行。
```

---

### 20.4 销毁测试

```text
1. Actor 销毁时移除所有接口生命周期 handle。
2. Actor 销毁时移除所有方法生命周期 handle。
3. Extra handles 不泄漏。
```

---

### 20.5 兼容测试

```text
1. 旧 IUpdate Actor 行为不变。
2. 旧 ILateUpdate Actor 行为不变。
3. 旧 IFixedUpdate Actor 行为不变。
4. EventStream / Post / Ask 不受影响。
```

---

## 21. Benchmark 计划

新增：

```text
Lifecycle Method: one Hot method × 10000 actors
Lifecycle Method: Hot + Warm + Cold methods × 10000 actors
Lifecycle Method: budget cutoff only Hot
Lifecycle Method: interface IUpdate vs [ActorUpdate] Hot
Lifecycle Method: registration cost
Lifecycle Method: destroy cleanup cost
```

重点指标：

```text
Mean
Allocated
ExecutedHot
ExecutedWarm
ExecutedCold
SkippedByBudget
UsedWorkUnits
```

---

## 22. 执行计划

### Commit 1：新增方法级 Attribute

```text
feat: add actor lifecycle method attributes
```

内容：

```text
1. TickTier。
2. ActorUpdateAttribute。
3. ActorLateUpdateAttribute。
4. ActorFixedUpdateAttribute。
```

---

### Commit 2：新增方法级生命周期元数据

```text
feat: add actor lifecycle method metadata
```

内容：

```text
1. ActorLifecyclePhase。
2. ActorLifecycleMethodMeta。
3. ActorLifecycleMethodInvoker。
4. ActorTypeMeta 增加 LifecycleMethods。
```

---

### Commit 3：源生成器支持生命周期方法

```text
feat(generator): emit actor lifecycle method metadata
```

内容：

```text
1. 扫描 [ActorUpdate]。
2. 扫描 [ActorLateUpdate]。
3. 扫描 [ActorFixedUpdate]。
4. 校验签名。
5. 生成静态 invoker。
```

---

### Commit 4：新增 Method FreeList / TickLane

```text
feat: add budgeted actor lifecycle method lanes
```

内容：

```text
1. ActorLifecycleMethodEntry。
2. ActorLifecycleMethodFreeList。
3. ActorLifecycleMethodTickLane。
4. 复用 RuntimeFrameBudget。
```

---

### Commit 5：注册流程接入 TypedActorStorage

```text
refactor: register generated actor lifecycle methods
```

内容：

```text
1. RegisterLifecycleInterfaces 中保留旧接口。
2. 追加方法级生命周期注册。
3. ActorLifecycleHandles 增加 Extra。
4. Destroy 时清理 Extra handles。
```

---

### Commit 6：测试与 benchmark

```text
test: cover actor lifecycle method tick scheduling
```

内容：

```text
1. 生成器测试。
2. 注册测试。
3. 预算测试。
4. 销毁测试。
5. 兼容测试。
6. benchmark。
```

---

## 23. 最终验收标准

```text
1. TickTier 不再要求放在 Actor 类上。
2. [ActorUpdate] / [ActorLateUpdate] / [ActorFixedUpdate] 可放在方法上。
3. 一个 Actor 可以拥有多个不同 TickTier 的 Update 方法。
4. 每个方法执行一次都消耗 RuntimeFrameBudget。
5. IUpdate / ILateUpdate / IFixedUpdate 旧代码不破坏。
6. Dormant 方法不主动 Tick。
7. Actor 销毁时方法级生命周期 handle 全部清理。
8. Warm / Cold 不绕过预算。
9. 测试通过。
10. Benchmark 不出现 GC 回退。
```

---

## 24. 总结

最终设计从：

```text
Actor 是 Hot / Warm / Cold。
```

改为：

```text
Actor 中的某个生命周期方法是 Hot / Warm / Cold。
```

这更贴近真实业务。

推荐开发者写法：

```csharp
public sealed partial class MonsterActor : IActor
{
    [ActorUpdate(TickTier.Hot)]
    private void CombatUpdate(float dt) { }

    [ActorUpdate(TickTier.Warm)]
    private void SensorUpdate(float dt) { }

    [ActorUpdate(TickTier.Cold)]
    private void MaintenanceUpdate(float dt) { }
}
```

调度器负责：

```text
相位打散。
预算截断。
游标续跑。
Warm / Cold 跳过。
RuntimeFrameBudget 消耗。
```

开发者只需要表达业务重要性。
