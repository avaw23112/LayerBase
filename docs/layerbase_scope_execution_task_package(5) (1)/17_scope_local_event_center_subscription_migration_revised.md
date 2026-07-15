# 17 EventCenter 迁移为 Scope 本地资源

> **强制执行规范：** 本文必须遵守 `01_mandatory_architecture_aot_performance_standards.md`；冲突时以该规范为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `02_scope_runtime_resources.md`、`05_scope_static_composition_generators.md`、`12_worker_event_job_and_subscribe_parallel_removal.md`  
> **文档性质：** 独立阶段任务。本文只处理 EventCenter 在 Scope 架构中的实例归属、注册目标和外部路由，不重新设计 EventCenter。

---

## 0. 本阶段核心目的

原架构中只有一个 Runtime EventCenter。

Scope 改造后必须变为：

```text
MainScope
    → MainScope.EventCenter

CombatScope
    → CombatScope.EventCenter

PathfindingScope
    → PathfindingScope.EventCenter
```

每个 Scope 的 EventCenter：

```text
只注册该 Scope 内实际存在的 Handler
只由该 Scope Owner Thread 同步派发
只与该 Scope 的 PostScheduler 绑定
不回退到 MainScope EventCenter
不与其他 Scope 共享 Handler 运行状态
```

Layer 仍然决定 Handler 的层级顺序：

```text
LayersBuilder.Push 顺序
    → LayerIndex 0
    → LayerIndex 1
    → LayerIndex 2
```

Scope Activate 时只需要：

```text
按 Push LayerIndex
依次执行原有 Handler 注册流程
```

注册结束后，所有 Handler 已经存在于当前 Scope 的原 `EventCenter` 中。

不得为运行期另外保存：

```text
ScopeSubscriptionPlan
EventHandlerRange[]
EventHandlerEntry[]
Handler ObjectSlot Table
Layer Handler Registry
重复的 Handler Contribution Runtime Cache
```

Build 阶段允许临时判断 Handler 属于哪个 Scope 和 Layer；注册完成后不得把这些临时信息保留成第二套派发系统。

---

## 1. 本阶段必须保留的原功能

以下 `faster` 原功能必须完整保留：

```text
EventCenter.RegisterEventType<TEvent>
EventTypeId<TEvent>
EventBucket<TEvent>
HandlerBucket<TEvent>
Bucket Fast Cache
Reflection Fallback
Freeze
PrewarmEvent
PrewarmDispatchTable
Rebuild Count
Reset

SubscribeNotify
Subscribe
SubscribeFlow
SubscribeAsync

对应的 Unsubscribe
Layer.OnEvent<T>()
手动订阅与取消订阅
IAutoSubscribe
GetEventDependencies
GetSubscribedEvents

Notify / Subscribe / Flow / Async 派发语义
Flow 截断语义
同步与异步 Handler 的原执行顺序
Handler Circuit
异常报告
事件依赖元数据
事件拓扑审计
原事件测试和 Benchmark
```

本文不得决定：

```text
新的 Handler 类型
新的派发顺序
新的异常吞吐规则
新的熔断状态机
新的手动订阅限制
新的 SubscriptionToken
新的 Bucket/数组布局
新的 EventCenter 公有接口
```

`SubscribeParallel` 按 12 号任务直接删除；除此之外不能借 Scope 迁移删除或修改其他 EventCenter 功能。

---

## 2. 最终架构关系

```text
LayerRuntime
    ├── MainScopeRuntime
    │   ├── EventCenter
    │   └── PostScheduler
    │
    ├── CombatScopeRuntime
    │   ├── EventCenter
    │   └── PostScheduler
    │
    └── PathfindingScopeRuntime
        ├── EventCenter
        └── PostScheduler
```

事件路径：

```text
同 Scope Send：
    OwnerScope Object
        → OwnerScope EventCenter.Send
        → OwnerScope Handler

同 Scope Post：
    OwnerScope Object
        → OwnerScope PostScheduler
        → OwnerScope EventCenter.Send

跨 Scope Event：
    ScopeEvent
        → TargetScope EventInbox
        → TargetScope Owner Thread
        → TargetScope EventCenter.Send
```

EventCenter 不需要知道 ScopeEvent、WorkerEventJob、Timer 或 ActorWorld 的实现。

---

## 3. 最终公有 API

业务 API保持：

```csharp
this.Send(
    in damageEvent);

this.Post(
    in damageEvent);
```

Attribute 订阅保持：

```csharp
[SubscribeNotify]
private void Observe(
    in DamageEvent value)
{
}

[Subscribe]
private void Apply(
    in DamageEvent value)
{
}

[SubscribeFlow]
private EventHandledState Validate(
    in DamageEvent value)
{
    return EventHandledState.Continue;
}

[SubscribeAsync]
private LBTask Persist(
    DamageEvent value)
{
    return _repository.SaveAsync(
        value);
}
```

现有手动订阅保持：

```csharp
layer.SubscribeNotify<DamageEvent>(
    OnDamage);

layer.SubscribeFlow<DamageEvent>(
    ValidateDamage);

layer.SubscribeAsync<DamageEvent>(
    PersistDamage);
```

现有 Fluent Event API如仍存在，也保持：

```csharp
layer.OnEvent<DamageEvent>();
```

只删除：

```text
SubscribeParallel
UnsubscribeParallel
Parallel HandlerKind
ParallelSubscriptionQueue
相关 Attribute、Generator、测试与示例
```

本文不新增：

```text
ScopeEventCenter
ScopeSubscribe
SubscribeInScope
ScopeSubscriptionToken
```

---

## 4. EventCenter 的实例所有权

`ScopeRuntime` 本地资源中直接保存原 `EventCenter`：

```csharp
internal sealed class ScopeRuntime
{
    internal EventCenter EventCenter {
        get;
    }

    internal PostScheduler PostScheduler {
        get;
    }
}
```

创建关系：

```csharp
EventCenter eventCenter =
    new EventCenter();

PostScheduler postScheduler =
    CreatePostScheduler(
        eventCenter);
```

实际构造方式必须复用 `faster` 原实现。

关键约束：

```text
每个 Scope 一个 EventCenter。
每个 Scope 一个 PostScheduler。
PostScheduler 只绑定同 Scope EventCenter。
```

不得：

```text
CustomScope 使用 LayerRuntime.EventCenter
CustomScope 使用 MainScope.EventCenter
多个 Scope 共享一个 EventCenter
通过 static 保存当前 EventCenter
通过 Primary Runtime 查 EventCenter
```

如果 `LayerRuntime.EventCenter` 为兼容内部代码暂时保留，它只能是：

```text
MainScope.EventCenter 的内部只读别名
```

不能作为 CustomScope fallback。

---

## 5. Handler 的 Scope 与 Layer 归属

Handler 注册到哪个 EventCenter，由 Handler 实例的 OwnerScope 决定。

```text
MainScope Service/Context Handler：
    MainScope.EventCenter

CombatScope Service/Context Handler：
    CombatScope.EventCenter

PathfindingScope Service/Context Handler：
    PathfindingScope.EventCenter
```

Layer 继续决定 EventCenter 使用的 `layerIndex`：

```text
RuntimeGeneration
+ ScopeId
+ LayerIndex
+ Handler Instance
```

其中：

```text
ScopeId：
    决定 EventCenter 实例。

LayerIndex：
    决定原 EventCenter 中的 Layer 顺序。

Handler Instance：
    决定实际回调目标。
```

不能把以下值当成 EventCenter 的 `layerIndex`：

```text
ServiceSlot
ContextSlot
ObjectSlot
LayerMembership.Start
```

原 EventCenter 的 `int layerIndex` 必须继续表示：

```text
LayersBuilder.Push 产生的 LayerIndex。
```

---

## 6. 按 Push 顺序执行原注册流程

### 6.1 Build 阶段

Build 只需要确定：

```text
Scope 中有哪些 Layer
每个 Layer 的 Push LayerIndex
该 Scope × Layer 中有哪些 Service/Context 实例
```

本文不要求生成 Handler Range 或 Handler Entry。

现有 Generator 继续生成：

```text
IAutoSubscribe.AutoBind
SubscribeNotify
Subscribe
SubscribeFlow
SubscribeAsync
GetEventDependencies
GetSubscribedEvents
```

### 6.2 Activate 阶段

当前 Scope 的 DI、Mount 和 Provide/From 完成后，按 LayerIndex 执行原自动绑定：

```csharp
private static void BindScopeHandlers(
    ScopeRuntime scope)
{
    for (
        int layerIndex = 0;
        layerIndex < scope.LayerCount;
        layerIndex++)
    {
        BindExistingLayerHandlers(
            scope,
            layerIndex);
    }

    scope.EventCenter.Freeze();
}
```

以上代码只表达流程，不要求新增 `BindExistingLayerHandlers` 类型。

Agent 应优先复用当前实际入口：

```text
Layer.BuildAutoBinding
IAutoSubscribe.AutoBind
Layer.SubscribeNotify
Layer.Subscribe
Layer.SubscribeFlow
Layer.SubscribeAsync
原自动订阅生成器
```

只修改：

```text
这些入口最终取得哪个 EventCenter。
```

它们必须取得当前对象 `OwnerScope.EventCenter`。

### 6.3 不保留第二份注册结果

注册完成后，原 EventCenter 已经持有：

```text
EventBucket
HandlerBucket
Delegate / Handler Instance
Circuit
Dispatch Table
```

因此不得继续保存：

```text
ScopeSubscriptionPlan
HandlerRange
HandlerEntry
ObjectSlot 派发表
Scope Handler 元数据副本
```

原 Layer 已经用于功能和生命周期的：

```text
_subscription tokens
DiscoveredSubscribers
SubscribedEvents
ProducedEvents
EventDependency metadata
```

必须保留。本文禁止的是新增重复运行路由，不是删除原有功能状态。

---

## 7. Layer 顺序保持方式

原 EventCenter 订阅入口已经接收 `layerIndex`：

```csharp
eventCenter.SubscribeNotify(
    layerIndex,
    handler);

eventCenter.Subscribe(
    layerIndex,
    handler);

eventCenter.SubscribeFlow(
    layerIndex,
    handler);

eventCenter.SubscribeAsync(
    layerIndex,
    handler);
```

Scope 改造只保证：

```text
1. layerIndex 来自 LayersBuilder.Push。
2. 各 Layer 按 Push 顺序执行原注册。
3. 同 Layer 内继续使用 faster 原注册顺序。
4. EventCenter 继续使用 faster 原 Bucket/DispatchTable。
```

不得修改为：

```text
LayerIndex + ServiceSlot
LayerMembership.Start
ContextSlot
ObjectSlot
新的 StableOrder
新的 HandlerKind 排序
```

也不得重新定义 Notify、Subscribe、Flow、Async 的内部先后与截断语义。

---

## 8. `ScopeSubscriptionRegistry` 的处理边界

`faster` 当前存在 `ScopeSubscriptionRegistry`，并通过：

```text
LayerMembership.Start
ServiceSlot
ContextSlot
```

计算 EventCenter RouteKey。

这些值不能替代 LayerIndex。

本阶段处理方式：

```text
优先：
    直接复用原 Layer/IAutoSubscribe 注册流程，
    不让 ScopeSubscriptionRegistry 成为 Handler Registry。

若现有 Scope 代码必须暂时经过它：
    收缩为薄转发器；
    明确传入 Push LayerIndex；
    不计算新的 RouteKey；
    不保存重复 Handler Plan。
```

该类型中与 DelayPublisher 有关的功能不属于本文，不得顺带重写。

---

## 9. Send 路由

### 9.1 Service / Context

```csharp
public static EventHandledState Send<TEvent>(
    this IService owner,
    in TEvent value)
    where TEvent : struct
{
    ScopeObjectBinding binding =
        ScopeObjectBinder.Get(
            owner);

    binding.LocalAccess
        .RequireOwnerThread();

    return binding.LocalAccess
        .EventCenter
        .Send(
            in value);
}
```

示例只表达路由；实际 API 和 Binder 复用现有实现。

### 9.2 Layer

原 `Layer.Send` 通过 `OwnerContext.EventCenter`。

迁移后应通过当前 Layer 实例的 OwnerScope 取得 EventCenter：

```text
Layer ScopeObjectBinding
    → OwnerScope LocalAccess
    → EventCenter.Send
```

### 9.3 Runtime

如果保留 `LayerRuntime.Send`：

```text
runtime.Send
    → MainScope.EventCenter.Send
```

它不自动查找 Handler 所在 Scope。

---

## 10. ScopeEvent 到达后的派发

```text
OriginScope
    → TargetScope ScopeEventInbox
    → TargetScope Owner Thread
    → Generated ScopeEvent Dispatcher
    → TargetScope.EventCenter.Send
```

Dispatcher 只负责：

```text
解码强类型 Event
调用 TargetScope 原 EventCenter.Send
按 Payload 规则释放资源
```

不得：

```text
直接调用 Handler
绕过 EventCenter Bucket
构建第二套 Handler Table
转回 MainScope EventCenter
保存 Target Handler ObjectSlot
```

---

## 11. PostScheduler 与 EventCenter

每个 Scope 的 PostScheduler 只绑定本 Scope EventCenter：

```text
Scope.PostScheduler
    → Scope.EventCenter.Send
```

本文不修改：

```text
Post All / Latest / Coalesced
Backpressure
Budget
Timer
DelayPublisher
对象池
Post Pump 顺序
```

只替换资源引用：

```text
LayerRuntime EventCenter
    → OwnerScope EventCenter
```

---

## 12. Prewarm 与 Freeze

继续复用：

```text
RegisterEventType<TEvent>
PrewarmEvent<TEvent>
PrewarmDispatchTable
Bucket Fast Cache
Freeze
EventCenterFrozenTypeException
Reflection Fallback 统计
```

执行关系：

```text
Scope 创建 EventCenter
    → 按 LayerIndex 注册该 Scope Handler
    → 预热该 Scope 使用的事件类型
    → EventCenter.Freeze
    → Scope Running
```

进程级 EventTypeId/生成元数据可共享。

以下状态必须每 Scope 独立：

```text
Bucket
Handler
Circuit
Dispatch Table
Freeze 状态
Reflection Fallback Count
```

---

## 13. 生命周期

### Activate

```text
创建 Scope EventCenter
创建 Scope PostScheduler
创建 LayerProvider / Service / Context
Mount
Provide / From
按 Push LayerIndex执行原 Handler 注册
Prewarm
Freeze
Initialize / RuntimeStart
```

总顺序以生命周期文档为准；关键是 Handler 实例已创建且 EventCenter 尚未 Freeze。

### Stop

```text
关闭新业务 ScopeEvent/Post 准入
处理既有事件
停止 Service/Context
```

不为 EventCenter新增 Stop 状态机。

### Dispose

继续使用原取消订阅和清理流程：

```text
按原 Layer 生命周期逆序 Dispose Subscription Token
Dispose Service / Context
Reset / Dispose 当前 Scope EventCenter
```

取消订阅必须操作注册时使用的同一个 Scope EventCenter。

---

## 14. 业务场景

Layer 顺序：

```text
0 FoundationLayer
1 GameplayLayer
2 PresentationLayer
```

CombatScope 中：

```text
FoundationLayer：
    CombatClockService.OnDamage

GameplayLayer：
    DamageService.OnDamage
    BuffService.OnDamage

PresentationLayer：
    无 CombatScope Handler
```

Activate：

```text
创建 CombatScope.EventCenter

注册 FoundationLayer Handler
    layerIndex = 0

注册 GameplayLayer Handler
    layerIndex = 1

PresentationLayer 无 Handler
    不注册

Prewarm
Freeze
```

发送：

```csharp
this.Send(
    new DamageEvent(
        target,
        amount));
```

路径：

```text
CombatScope DamageService
    → CombatScope.EventCenter
    → faster 原 EventBucket<DamageEvent>
    → faster 原 HandlerBucket
    → 原 Handler 派发
```

MainScope 和其他 Scope 的同类型 Handler 不会被调用。

---

## 15. faster 分支复用

### 15.1 直接复用

| faster 文件 | 必须保留 |
|---|---|
| `LayerBase/Event/Event/EventCenter.cs` | EventType 注册、Bucket、Fast Cache、Send、Freeze、Prewarm、Reflection Fallback |
| `LayerBase/Event/Event/HandlerBucket.cs` | HandlerBucket、原 Entry、Circuit、派发表 |
| `LayerBase/Layer/Layer.cs` | Subscribe/Unsubscribe、`_subscriptions`、`BuildAutoBinding`、事件元数据 |
| `LayerBase/DI/IAutoSubscribe.cs` | `AutoBind`、依赖和订阅元数据 |
| `EventPrewarmGenerator` | 原预热生成 |
| 原 EventCenter Tests/Benchmarks | 行为与性能基线 |

### 15.2 最小修改后复用

| faster 位置 | 只允许修改 |
|---|---|
| `ScopeRuntime` | 每 Scope 创建独立 EventCenter |
| `Layer.Send/Post` | 路由 OwnerScope EventCenter/PostScheduler |
| `Layer.GetSubscriptionEventCenter` | 返回当前 Layer OwnerScope EventCenter |
| `Layer.BuildAutoBinding` 调用位置 | 每 Scope Activate 中按 Push LayerIndex 执行 |
| ScopeEvent Dispatcher | 到达后调用 TargetScope EventCenter.Send |
| `ScopeSubscriptionRegistry` | 不再使用 ServiceSlot/ContextSlot 作为 layerIndex；必要时只做薄转发 |

### 15.3 由 12 号任务删除

```text
EventCenter.SubscribeParallel
EventCenter.UnsubscribeParallel
IEventBucketNonGeneric.AddParallel / RemoveParallel
HandlerBucket.MasterParallel
ParallelHandlerEntry
ParallelSubscriptionQueue
Layer.SubscribeParallel
Fluent Parallel API
SubscribeParallel Attribute/Generator/Test
```

### 15.4 禁止实现

```text
新的 EventCenter 类型
新的 HandlerRange/HandlerEntry
新的 Scope Handler Registry
新的 Event Dispatcher 算法
新的 Layer 排序算法
ObjectSlot 派发替代原 Handler
运行期重新扫描 Handler
```

---

## 16. 需要修改的代码位置

优先检查：

```text
LayerBase/Application/LayerRuntime.cs

LayerBase/Scope/
    ScopeRuntime.cs
    ScopeCompositionBuilder.cs
    ScopeSubscriptionRegistry.cs
    ScopeEvent Dispatcher

LayerBase/Layer/Layer.cs

LayerBase/DI/IAutoSubscribe.cs

LayerBase/Event/Event/
    EventCenter.cs
    HandlerBucket.cs

LayerBase.Generator/
    EventPrewarmGenerator.cs
    自动订阅生成器
    ScopeEvent Dispatcher Generator
```

原则：

```text
EventCenter.cs / HandlerBucket.cs：
    除删除 SubscribeParallel 外，
    不应因 Scope 迁移重写。

Layer.cs / ScopeRuntime / Scope Dispatcher：
    是本阶段主要修改位置。
```

---

## 17. Agent 执行任务

```text
1. 记录 faster 原 EventCenter 功能、测试和 Benchmark 基线。
2. 每个 ScopeRuntime 创建独立 EventCenter。
3. 每个 Scope PostScheduler 绑定同 Scope EventCenter。
4. 保留 Push 产生的原 LayerIndex。
5. Scope Activate 按 LayerIndex 从小到大执行原 BuildAutoBinding/AutoBind。
6. 自动订阅和手动订阅继续调用原 EventCenter.Subscribe*。
7. 所有订阅继续传 Push LayerIndex。
8. 禁止 ServiceSlot、ContextSlot、ObjectSlot 代替 LayerIndex。
9. 不生成 ScopeSubscriptionPlan、HandlerRange 或 HandlerEntry。
10. 注册完成后 Prewarm 并 Freeze 当前 Scope EventCenter。
11. this.Send 路由 OwnerScope EventCenter。
12. Layer.Send 路由当前 Layer OwnerScope EventCenter。
13. runtime.Send 固定 MainScope EventCenter。
14. ScopeEvent 到达后调用 TargetScope EventCenter.Send。
15. PostScheduler Pump 调用 OwnerScope EventCenter.Send。
16. Dispose 时在原 Scope EventCenter 上取消原订阅。
17. 删除 Runtime/MainScope EventCenter fallback。
18. 按 12 号任务删除 SubscribeParallel，其他 Handler 功能不动。
19. 运行全部 faster 原 EventCenter 测试和 Benchmark。
20. 增加多 Scope 隔离和 Layer 顺序测试。
```

---

## 18. 必须测试

保留并运行 `faster` 原测试：

```text
Notify / Subscribe / Flow / Async
Flow 截断
Manual Subscribe / Unsubscribe
IAutoSubscribe
Event Dependency Metadata
Handler Circuit
Exception Reporting
Prewarm
Freeze
Reflection Fallback
Bucket Fast Cache
Rebuild Count
PostScheduler → EventCenter
原 EventCenter Benchmark
```

新增：

```text
Each_scope_owns_independent_event_center

Main_scope_event_center_behavior_is_unchanged

Custom_scope_does_not_fallback_to_main_event_center

Same_event_type_in_different_scopes_is_isolated

Scope_activate_registers_layers_in_push_order

Event_center_receives_original_layer_index

Service_slot_is_not_used_as_layer_index

Context_slot_is_not_used_as_layer_index

Empty_layer_registers_no_handler_and_keeps_order

Scope_event_dispatches_through_target_event_center

Scope_post_dispatches_through_owner_event_center

Disposing_scope_unsubscribes_only_its_handlers

Circuit_state_is_not_shared_between_scopes

Prewarm_and_freeze_are_per_scope

No_scope_subscription_plan_is_retained

No_handler_range_or_object_slot_dispatch_is_added
```

---

## 19. 验收否决项

出现以下任意一项，任务不通过：

```text
CustomScope 使用 MainScope 或 LayerRuntime EventCenter

多个 Scope 共享 EventCenter 实例

使用 ServiceSlot / ContextSlot / Membership.Start 作为 EventCenter layerIndex

重新实现 EventHandlerRange / EventHandlerEntry / ObjectSlot 派发

新增 ScopeSubscriptionPlan 或第二套 Handler Registry

改变 Notify / Subscribe / Flow / Async 原语义

改变 Flow 截断规则

删除手动订阅、取消订阅、OnEvent、Prewarm、Freeze、Circuit 等原功能

ScopeEvent 到达后绕过 EventCenter 直接调用 Handler

PostScheduler 绕过本地 EventCenter

Dispose 一个 Scope 会 Reset 其他 Scope EventCenter

为了 Scope 迁移重写 EventCenter Bucket 或派发热路径

运行期扫描 Assembly、Type 或 Handler Metadata 注册事件
```

---

## 20. 本阶段不修改的内容

本文不修改：

```text
EventCenter 内部派发算法
HandlerBucket 数据结构
Handler Circuit
EventTypeId
事件依赖拓扑
PostScheduler 策略
ScopeEvent MPSC 实现
WorkerEventJob
DI / Mount / Provide / From
Call
ECS
ActorWorld
```

本文只完成：

```text
EventCenter：
    从 Runtime 单例资源
    迁移为每 Scope 独立资源。

Handler：
    按 Push LayerIndex
    执行原注册流程
    注册进 OwnerScope EventCenter。

Send/Post/ScopeEvent：
    最终进入正确 Scope 的原 EventCenter。
```
