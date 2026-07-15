# 18 Scope 本地 PostScheduler、Timer 与 Delay 迁移

> **强制执行规范：** 本文必须遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`；冲突时以 00、01 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `02_scope_runtime_resources_revised.md`、`03_scope_event_call_protocol.md`、`07_lbtask_synchronization_context.md`、`17_scope_local_event_center_subscription_migration_revised.md`  
> **相关阶段：** `12_worker_event_job_and_subscribe_parallel_removal.md`  
> **文档性质：** 独立阶段任务。本文只迁移 OwnerScope 本地 Post、Timer 和 Delay，不提供任意线程入口，不重新设计 ScopeEvent、EventCenter 或 WorkerEventJob。

---

## 0. 本阶段核心目的

每个 Scope独立拥有：

```text
PostScheduler
TimeScheduler
DelayPublisherManager
Post Budget
Timer Clock State
```

它们只由 OwnerScope Thread访问。

最终本地关系：

```text
OwnerScope Object
    → OwnerScope PostScheduler
    → OwnerScope EventCenter

OwnerScope Timer
    → OwnerScope PostScheduler

OwnerScope Delay
    → OwnerScope SynchronizationContext
    或 OwnerScope本地完成路径
```

跨线程和跨 Scope：

```text
只允许 ScopeEvent / ScopeCall
```

因此必须删除：

```text
PostScheduler CrossThreadIngress
PostIngressQueue
PostFromAnyThread
TryPostFromAnyThread
ScopePostEndpoint
网络线程直接写 PostScheduler
Worker Thread直接写 PostScheduler
```

---

## 1. 最终公有 API

本地 Post：

```csharp
this.Post(
    in value);
```

```csharp
PostHandle handle =
    this.Post(
        in value,
        EventPostPolicy.Latest);
```

Timer：

```csharp
TimerHandle once =
    this.Timers().Schedule(
        TimeSpan.FromSeconds(1),
        new RegenerateEvent(entity));
```

```csharp
TimerHandle repeating =
    this.Timers().Repeat(
        TimeSpan.FromMilliseconds(100),
        new TickPoisonEvent(entity));
```

Delay：

```csharp
await this.Delay(
    TimeSpan.FromMilliseconds(250));
```

如果 `faster` 已有：

```csharp
[SubscribeDelay]
public IDelayPublisher<InputEvent>
    Inputs { get; set; } = null!;
```

继续保留原 API和生成流程。

所有本地 API必须在对象 OwnerScope执行上下文调用。

---

## 2. 外部线程与跨 Scope入口

外部输入：

```csharp
runtime.Main.TryPost(
    in engineEvent);
```

显式目标 Scope：

```csharp
this.Scope<PathfindingScope>()
    .TryPost(
        in rebuildCommand);
```

底层：

```text
ScopeEndpoint
    → TargetScope ScopeEventInbox
```

不进入 PostScheduler MPSC。

ScopeEvent到达目标 Owner Thread后，按 03、17 号文档进入目标 EventCenter。

如果某个内部功能必须保留本地 Post策略，例如 WorkerEventJob结果：

```text
Worker Result ScopeEvent
    → OriginScope Owner Thread
    → 本地 PostScheduler.TryPost
```

跨线程能力属于 ScopeEvent，不属于 PostScheduler。

---

## 3. PostScheduler 的本地语义

```csharp
internal sealed class PostScheduler
{
    private readonly PostTypeState[]
        _types;

    private readonly PostNode[]
        _nodes;

    private readonly PostNodePool
        _pool;

    private readonly EventCenter
        _eventCenter;
}
```

不得包含：

```text
MPSC Queue
ConcurrentQueue
CrossThreadIngress
ScopeEndpoint
Runtime RouteTable
```

`EventId` 或冻结的 PostTypeSlot直接索引 `_types`。

### 3.1 策略

继续保留：

```csharp
public enum PostDeliveryMode
{
    All,
    Latest,
    Coalesced
}
```

以及 `faster` 原：

```text
Backpressure
MaxPending
Payload Pool
Cancellation Handle
Budget
```

本文不重新设计策略算法。

---

## 4. 本地提交

```csharp
internal bool TryPost<TEvent>(
    in TEvent value,
    in EventPostPolicy policy)
    where TEvent : struct
{
    RequireOwnerThread();

    int typeSlot =
        GeneratedPostTypeSlot<TEvent>.Value;

    return EnqueueLocal(
        typeSlot,
        in value,
        in policy);
}
```

实际类型和 API优先复用 `faster`。

运行路径：

```text
ScopeObjectBinding
    → OwnerScope LocalAccess
    → PostScheduler
    → 预计算 PostTypeSlot
```

不得：

```text
根据 Type查 Scope
搜索 Runtime Scheduler
回退 MainScope
跨线程自动入队
```

---

## 5. Timer

Timer完全属于 Scope本地：

```csharp
internal sealed class
    TimeScheduler<TAction>
{
    private TimerSlot[] _slots;
    private int[] _heap;
    private int _count;
}
```

可直接复用 `faster` 的 heap、wheel、slot、handle和取消算法。

Timer到期：

```text
OwnerScope Thread
    → Timer Action
    → OwnerScope本地 PostScheduler
```

Timer Callback不得：

```text
在任意线程执行
直接调用其他 Scope Handler
持有 ScopeRuntime之外的可变对象
```

---

## 6. Delay

Delay由 OwnerScope本地时间和 continuation管理。

```text
OwnerScope Delay请求
    → Delay Slot
    → OwnerScope Tick推进
    → 完成 LBTask Source
    → continuation回 OwnerScope SynchronizationContext
```

不需要：

```text
ThreadPool Timer
Task.Delay
全局 Delay线程
跨线程 CompletionQueue
```

如果 `faster` DelayPublisher使用 PostScheduler，应继续使用同 Scope本地路径。

---

## 7. Pump 顺序

建议：

```text
1. Drain ScopeCall。
2. Drain ScopeEvent。
3. Drain SynchronizationContext。
4. Advance TimeScheduler。
5. Advance DelayPublisher。
6. 将到期 Timer结果加入本地 PostScheduler。
7. Pump PostScheduler Budget。
8. FixedUpdate / Update / ECS。
```

不存在：

```text
DrainCrossThreadIngress
DrainPostIngress
DrainWorkerResultQueue
```

### 7.1 Post Pump

```csharp
internal void Pump(
    in PostBudget budget)
{
    int count = 0;
    long deadline =
        budget.CalculateDeadline();

    while (
        count < budget.MaxCount
        && Stopwatch.GetTimestamp()
            < deadline
        && TryDequeueLocal(
            out PostEnvelope envelope))
    {
        try
        {
            _eventCenter.SendEnvelope(
                in envelope);
        }
        finally
        {
            Release(
                in envelope);
        }

        count++;
    }
}
```

继续复用 `faster` 原展开循环和快路径。

---

## 8. 每 Scope隔离

相同 EventType：

```text
MainScope PostTypeState
CombatScope PostTypeState
PathfindingScope PostTypeState
```

必须彼此独立。

一个 Scope的：

```text
Latest覆盖
Coalesced状态
Pending Count
Budget耗尽
Timer积压
Delay积压
```

不能影响其他 Scope。

进程级不可变 EventTypeId可以共享，运行状态不能共享。

---

## 9. Layer 与 Scope 路由

Layer仍是上层业务管理结构。

本地资源路由：

```text
Service / Context：
    OwnerScope Post/Timer/Delay

Push Layer实例：
    MainScope Post/Timer/Delay
```

这不表示 Layer只管理 MainScope；CustomScope中的 Service/Context仍属于其 OwnerLayer，但使用自己的 OwnerScope调度资源。

---

## 10. WorkerEventJob 结果

18 号不实现 Worker线程入口。

只提供 OriginScope Owner Thread上的恢复点：

```csharp
internal void ApplyWorkerResult<TEvent>(
    in TEvent value,
    in EventPostPolicy policy)
    where TEvent : struct
{
    _postScheduler.TryPost(
        in value,
        in policy);
}
```

调用者只能是：

```text
OriginScope标准 ScopeEvent Dispatcher
```

不得让 Worker Thread取得该方法或 PostScheduler引用。

详细实现由 12 号文档负责。

---

## 11. Stop 与 Dispose

### 11.1 Drain

```text
关闭新本地业务 Post / Timer / Delay。
处理停止前已接受 Post。
取消未来 Timer。
完成或取消 Delay Source。
释放所有 Payload。
```

### 11.2 Drop

```text
不调用业务 Handler。
释放全部 Post Payload。
取消 Timer Handle。
取消 Delay Source。
```

### 11.3 Worker Result

Scope Stop必须先按 12 号：

```text
关闭 ScopeJobGroup
等待 Job归零
Drain已接受 Result ScopeEvent
```

之后才关闭 PostScheduler和 EventCenter。

PostScheduler自身不感知 Worker线程。

---

## 12. Freeze 与 Policy Plan

Build阶段允许：

```text
Attribute读取
Dictionary / List
策略冲突检查
容量估算
PostTypeSlot分配
```

Freeze为：

```text
PostTypePlan[]
PostTypeSlot
初始容量
Budget
```

Running禁止：

```text
RebuildEventPolicies
动态新增 Post Type
Type Dictionary查找
运行期排序
```

---

## 13. faster 分支复用

### 13.1 直接复用

```text
PostScheduler All / Latest / Coalesced
Backpressure
PostHandle
Payload Pool
TimeScheduler
DelayPublisherManager
TimerHandle
本地 RingQueue
PostScheduler / Timer Benchmark
```

### 13.2 修改后复用

```text
Runtime Scheduler：
    改为每 Scope本地实例。

Runtime PolicyTable：
    改为 Scope PostTypePlan运行状态。

Post扩展：
    通过 ScopeObjectBinding路由 OwnerScope。

Worker Result：
    仅保留 OwnerScope本地恢复入口。
```

### 13.3 删除或禁止移植

```text
PostIngressQueue
PostScheduler CrossThreadIngress
ScopePostEndpoint
PostFromAnyThread
TryPostFromAnyThread
网络线程直接 Post
Worker线程直接 Post
无界 ConcurrentQueue
```

---

## 14. 需要修改的代码位置

优先检查：

```text
LayerBase/Event/Post/
LayerBase/Time/
LayerBase/Delay/
LayerBase/Layer/Layer.cs
LayerBase/DI/ServiceExtensions.cs
LayerBase/Scope/ScopeRuntime.cs
LayerBase/Scope/ScopeObjectBinding.cs
LayerBase.Generator/
LayerBase.Test/
LayerBase.BenchMark/
```

Agent先搜索：

```text
CrossThreadIngress
PostIngress
PostFromAnyThread
ScopePostEndpoint
```

全部删除或改为 ScopeEvent调用点。

---

## 15. Agent 执行任务

```text
1. 每个 Scope创建独立 PostScheduler、Timer和 Delay。
2. PostScheduler只允许 OwnerThread本地访问。
3. 删除 MPSC CrossThreadIngress。
4. 删除 PostFromAnyThread / TryPostFromAnyThread。
5. 删除 ScopePostEndpoint。
6. 外部和跨 Scope输入统一走 ScopeEvent。
7. 保留 faster All / Latest / Coalesced算法。
8. 保留 Timer heap/wheel和 Delay算法。
9. Worker Result只在 OriginScope Dispatcher中恢复本地 Post。
10. Scope Stop按 JobGroup → Result Event → Post顺序终结。
11. Build冻结 PostTypePlan和容量。
12. Running使用 Slot和本地队列。
13. 迁移测试和 Benchmark。
```

---

## 16. 必须测试

```text
Each_scope_has_independent_post_scheduler

Each_scope_has_independent_timer

Each_scope_has_independent_delay_manager

Local_post_requires_owner_thread

Post_scheduler_has_no_cross_thread_ingress

Post_from_any_thread_api_does_not_exist

Scope_post_endpoint_does_not_exist

Cross_scope_try_post_uses_scope_event_inbox

External_main_post_uses_main_scope_event_inbox

Worker_result_reenters_local_post_on_owner_thread

Latest_state_is_isolated_per_scope

Coalesced_state_is_isolated_per_scope

Post_budget_is_isolated_per_scope

Timer_fires_on_owner_thread

Delay_continuation_returns_to_owner_scope

Stop_releases_all_post_payloads

Drop_does_not_invoke_handlers

Running_uses_precomputed_post_type_slot

Steady_state_local_post_is_zero_allocation
```

---

## 17. 验收否决项

出现以下任意一项，任务不通过：

```text
PostScheduler包含 MPSC或 ConcurrentQueue

存在 CrossThreadIngress

存在 PostFromAnyThread或 ScopePostEndpoint

网络线程或 Worker线程直接写 PostScheduler

所有 Scope共享 PostTypeState或 Timer状态

Timer在非 OwnerThread调用业务 Handler

Delay使用 Task.Delay或 ThreadPool Timer

Scope Stop先关闭 Post再等待 Worker Result

Running按 Type / Dictionary查 Post策略

为了本任务重写 ScopeEvent或 EventCenter
```

---

## 18. 本阶段不修改的内容

本文不修改：

```text
ScopeEvent / ScopeCall底层
EventCenter Handler结构
WorkerJob Scheduler
DI
LocalCall
ECS
ActorWorld
```

本文只保证：

```text
Post、Timer、Delay是 OwnerScope本地调度资源。

所有跨线程输入统一先进入 ScopeEvent。

PostScheduler不再承担跨线程通讯职责。
```
