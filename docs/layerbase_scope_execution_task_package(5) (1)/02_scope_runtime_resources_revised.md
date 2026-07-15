# 02 Layer 管理下的 Scope 运行内核与本地资源

> **强制执行规范：** 本文必须遵守 `00_index_revised.md` 和 `01_mandatory_architecture_aot_performance_standards_revised.md`；冲突时以 00、01 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `05_scope_static_composition_generators_revised.md`  
> **文档性质：** 独立阶段任务。本文只建立 Scope 运行内核和本地资源，不重新定义业务对象的 Layer 归属、DI 规则、EventCenter 派发、LocalCall Handler 或生命周期接口。

---

## 0. 本阶段核心目的

建立一个最小、明确的 Scope 运行内核：

```text
Layer：
    上层业务管理结构

ScopeRuntime：
    由 LayerBuildPlan 投影出的执行域
```

ScopeRuntime 负责：

```text
本地资源
Owner Thread
Tick
两条跨 Scope Inbox
执行各 Layer 在本 Scope 的轻量切片
```

ScopeRuntime 不负责：

```text
决定 Service 属于哪个 Layer
允许跨 Layer DI
重排 Handler Layer 顺序
重新定义 Lifecycle 管理顺序
```

---

## 1. 架构位置

```text
LayerRuntime
    ├── RuntimeCompositionPlan
    │   ├── LayerBuildPlan[]
    │   └── ScopeExecutionPlan[]
    │
    ├── ScopeRuntimeHost
    │   ├── MainScopeRuntime
    │   ├── InlineScopeRuntime[]
    │   └── WorkerScopeRuntime[]
    │
    ├── MainActorRuntime
    │   └── ActorWorld
    │
    └── WorkerJobScheduler
```

ScopeRuntime 由 `ScopeExecutionPlan` 创建。

`ScopeExecutionPlan` 来源于：

```text
LayerBuildPlan[]
    → 按 OwnerScopeId投影
    → ScopeLayerSlice[]
```

---

## 2. 最终内部结构

```csharp
internal sealed class ScopeRuntime
{
    internal readonly ScopeDescriptor Descriptor;

    internal readonly ScopeTransport Transport;

    internal readonly EventCenter EventCenter;
    internal readonly PostScheduler PostScheduler;
    internal readonly TimeScheduler<ITimerAction> Timer;
    internal readonly DelayPublisherManager DelayManager;

    internal readonly World EcsWorld;
    internal readonly IEcsScheduler EcsScheduler;

    internal readonly
        LayerBaseSynchronizationContext
        SynchronizationContext;

    internal readonly
        ScopeLocalCallRegistry
        LocalCalls;

    internal readonly
        LayerProviderRuntime[]
        LayerProviders;

    internal readonly
        ScopeLayerSlice[]
        LayerSlices;

    internal readonly
        ScopeLifecyclePlan
        Lifecycle;

    internal ScopeRuntimeState State;

    internal float FixedAccumulator;
}
```

字段名以现有实现为准。

结构必须表达：

```text
Event / Post / Timer / ECS / Context：
    Scope 本地所有。

LayerProvider / LayerSlice：
    从 Layer-first Plan 投影而来。

ActorWorld：
    不在 ScopeRuntime。
```

---

## 3. 不允许出现在 ScopeRuntime 的字段

```text
ActorWorld
LayerRuntime 可变引用
Thread
ScopeRouteTable
其他 ScopeRuntime
ControlQueue
CompletionQueue
StopQueue
DisposeQueue
ProjectionQueue
ActorCommandQueue
WorkerResultQueue
全局 ServiceProvider
Scope 根业务 Provider
```

ScopeRuntime 可以保存：

```text
RuntimeId / Generation
只读 Runtime 服务 Accessor
MainScope Actor Endpoint
ScopeEndpoint
```

但这些对象不能允许 ScopeRuntime直接访问其他 Scope 的本地资源。

---

## 4. ScopeLayerSlice

```csharp
internal readonly struct ScopeLayerSlice
{
    internal readonly int LayerIndex;

    internal readonly int ProviderSlot;

    internal readonly int ServiceStart;
    internal readonly int ServiceCount;

    internal readonly int ContextStart;
    internal readonly int ContextCount;

    internal readonly int UpdateStart;
    internal readonly int UpdateCount;

    internal readonly int FixedUpdateStart;
    internal readonly int FixedUpdateCount;

    internal readonly int ToolStart;
    internal readonly int ToolCount;
}
```

Slice 只用于：

```text
执行 Range
诊断归属
Layer Enable / Disable 跳过
Provider 定位
```

它不代表 Scope 管理 Layer。

所有 Scope 的 Slice 顺序必须与 `LayersBuilder.Push` 一致。

空 Layer：

```text
保留 LayerIndex
Count = 0
不创建无意义运行对象
```

---

## 5. LayerProviderRuntime

Scope 中可以物理保存：

```csharp
LayerProviderRuntime[]
```

但语义必须是：

```text
LayerBuildPlan
    → 该 Layer 在该 Scope 的 Provider 实例
```

不是：

```text
Scope 创建一个 Provider
    → 可以解析全部 Layer
```

解析规则：

```text
ScopeObjectBinding.ScopeId
+ ScopeObjectBinding.LayerIndex
+ ProviderSlot
```

同 Scope 跨 Layer不可解析。

详细实现由 08 号文档负责。

---

## 6. ScopeTransport

```csharp
internal sealed class ScopeTransport
{
    internal IBoundedQueue<
        ScopeEventEnvelope>
        EventInbox;

    internal IBoundedQueue<
        ScopeCallEnvelope>
        CallInbox;

    internal ScopeEndpoint Endpoint;
}
```

只有两条跨 Scope 通道。

### 6.1 EventInbox

用于：

```text
业务单向事件
Worker Result
Actor Projection Command / Result
Fault 上报
内部单向通知
```

### 6.2 CallInbox

用于：

```text
业务 ScopeCall Request / Response
Activate
Stop
Dispose
Snap SafePoint
Diagnostics Request / Response
```

Request 和 Response 共用同一个 CallInbox。

---

## 7. ScopeEndpoint

```csharp
public readonly struct ScopeEndpoint
{
    public int RuntimeId { get; }
    public int RuntimeGeneration { get; }
    public int ScopeId { get; }

    internal IScopeEventWriter
        EventWriter { get; }

    internal IScopeCallWriter
        CallWriter { get; }
}
```

Endpoint 是其他线程和 Scope 唯一可持有的目标句柄。

不得暴露：

```text
ScopeRuntime
EventCenter
PostScheduler
Timer
EcsWorld
LayerProviderRuntime
ServiceProvider
ActorWorld
```

---

## 8. ScopeRuntimeHost 与 Worker

### 8.1 ScopeRuntimeHost

负责：

```text
创建 ScopeTransport
创建 Main/Inline/Worker ScopeRuntime
安装 ScopeEndpoint
向 Scope 发送控制 Call
收集 Response
销毁 Endpoint
```

不负责：

```text
跨线程直接调用 Service
跨线程直接修改 Scope State
直接访问 WorkerScope EventCenter / ECS / DI
```

### 8.2 ScopeWorker

```csharp
internal sealed class ScopeWorker
{
    private readonly Thread _thread;
    private readonly ScopeRuntime _scope;

    internal void Start();
    internal void Join();

    private void Run();
}
```

ScopeWorker 只负责：

```text
线程创建
安装 SynchronizationContext
固定频率 Tick
队列非空唤醒
退出后 Join
```

它不承载：

```text
Stop 命令
Dispose 命令
业务消息
Actor 命令
```

这些内容全部来自 ScopeEvent / ScopeCall Inbox。

---

## 9. MainScope、InlineScope 与 WorkerScope

### 9.1 MainScope

```text
由 runtime.Pump 推进
Owner Thread = 引擎主线程
Event/Post/Timer/ECS 独立
使用 Layer-first MainScope Slice
```

### 9.2 InlineScope

```text
由 MainScope Scheduler 稳定推进
与 MainScope 同物理线程
但拥有独立 Event/Post/Timer/ECS/DI/Call/Lifecycle 状态
```

### 9.3 WorkerScope

```text
由 ScopeWorker 独立线程推进
业务对象在 Worker Owner Thread创建和释放
拥有独立 Event/Post/Timer/ECS/DI/Call/Lifecycle 状态
```

三者业务管理关系完全一致：

```text
都来自同一组 Push Layer
都保留 LayerIndex
都执行自己的 ScopeLayerSlice
```

---

## 10. Build 阶段

02 不重新收集业务对象。

它消费 05 的输出：

```text
RuntimeCompositionPlan
    → ScopeExecutionPlan
        → ScopeDescriptor
        → ScopeLayerSlice[]
        → LayerProvider Plan
        → Lifecycle Plan
        → Resource Capacity
```

Build 确定：

```text
Inbox Capacity
Event/Post/Timer 初始容量
ECS Scheduler 类型
Scope TickRate
FixedDelta
LayerProvider 数量
LayerSlice 数量
Lifecycle 数组长度
```

Build 不能：

```text
创建 Worker Service
启动线程
创建 ActorWorld 给 Scope
根据 Scope 重新决定 OwnerLayer
```

---

## 11. Activate 阶段

`ScopeActivateCall` 在目标 Owner Thread执行：

```text
1. 创建 Scope EventCenter。
2. 创建 Scope PostScheduler / Timer / Delay。
3. 创建 Scope EcsWorld / EcsScheduler。
4. 创建 Scope SynchronizationContext。
5. 创建 ScopeLocalCallRegistry。
6. 按 LayerIndex创建该 Scope 的 LayerProviderRuntime。
7. 按 LayerIndex创建 Service / Context。
8. Attach ScopeObjectBinding。
9. Mount。
10. Provide / From。
11. 按 LayerIndex执行 faster 原 Event Handler 注册。
12. 绑定 LocalCall Handler。
13. Initialize / PostBuild。
14. Prewarm / Freeze。
15. RuntimeStart。
16. State = Running。
```

步骤 6—15 的业务管理关系由 LayerPlan 决定。

ScopeRuntime 只负责在正确线程执行 Plan。

---

## 12. Running Tick

### 12.1 普通顺序

```text
1. Enter ScopeExecution
2. Drain ScopeCall Response / Control / Request
3. Drain ScopeEvent
4. Drain local SynchronizationContext
5. Timer / Delay
6. PostScheduler
7. FixedUpdate Layer Slice
8. Update Layer Slice
9. ECS Query / Command Apply / SafePoint
10. Exit ScopeExecution
```

### 12.2 MainScope

```text
1. 标准 Scope Tick 前半
2. MainScope Layer Slice
3. MainScope ECS
4. Tick InlineScope
5. 再次 Drain 标准 MainScope ScopeEvent
6. MainActorRuntime Apply / ActorWorld.Pump
7. Frame-end continuation
```

Actor 阶段的具体顺序以 21、22 号文档为准。

### 12.3 Layer 顺序

```text
FixedUpdate / Update：
    Push LayerIndex 正序

RuntimeStop / Dispose：
    Push LayerIndex 逆序
```

具体 Lifecycle Plan 由 19 号文档负责。

---

## 13. EventCenter 关系

每个 Scope 一个原 `EventCenter`。

Activate：

```text
按 Push LayerIndex
执行 faster 原 AutoBind / Subscribe 流程
```

注册完成后不保留：

```text
ScopeSubscriptionPlan
HandlerRange
HandlerEntry
ObjectSlot Dispatcher
```

ScopeEvent 到达：

```text
TargetScope EventInbox
    → TargetScope EventCenter.Send
```

不直接调用 Handler。

详细实现由 17 号文档负责。

---

## 14. Post、Timer 与 Delay

```text
Scope.PostScheduler
    → Scope.EventCenter

Scope.Timer
    → Scope-local callback / Post

Scope.Delay
    → Scope-local continuation / Post
```

删除：

```text
PostFromAnyThread
TryPostFromAnyThread
PostIngressQueue
```

外部线程使用：

```csharp
runtime.Scope<TScope>()
    .TryPost(
        in value);
```

---

## 15. 本地 Call 与跨 Scope Call

### 15.1 ScopeLocalCallRegistry

```text
CurrentScope
+ RequestType
+ ResponseType
→ Unique Handler
```

本地 Call 不进入 MPSC。

Handler 可以属于任意 Layer。

### 15.2 ScopeCall Transport

```text
OriginScope CallInbox
    ↔ TargetScope CallInbox
```

用于显式跨 Scope。

02 只创建本地 Registry 和 CallInbox，不统一两种实现。

---

## 16. WorkerEventJob

WorkerJobScheduler 属于 Runtime 共享计算设施，不是 Scope 本地资源。

提交时捕获：

```text
RuntimeGeneration
OriginScopeId
OriginScope Endpoint
Result Event RouteId
```

完成：

```text
Worker Thread
    → OriginScope ScopeEventInbox
```

不得：

```text
直接写 OriginScope PostScheduler
使用 PostFromAnyThread
统一回 MainScope
使用全局 ResultQueue
```

---

## 17. ActorWorld 固定通讯管线

ScopeRuntime 不持有 ActorWorld。

```text
LayerRuntime
    → MainActorRuntime
        → ActorWorld
```

CustomScope：

```text
Actor Command / Projection
    → MainScope ScopeEventInbox
    → MainActorRuntime
    → ActorWorld
    → Result ScopeEvent
    → OriginScope
```

MainScope 本地 Actor API可以直达 MainActorRuntime，因为当前线程已是 ActorWorld Owner Thread。

具体实现由 21、22 号文档负责。

---

## 18. Stop 与 Dispose

### 18.1 Stop

MainScope 发：

```text
ScopeStopCall
```

目标 Owner Thread：

```text
关闭新业务准入
保留 Response / Control / Critical Event
终结已接受 Call
停止 Worker JobGroup
RuntimeStop 按 LayerIndex逆序
返回 StopResponse
```

### 18.2 Dispose

MainScope 发：

```text
ScopeDisposeCall
```

目标 Owner Thread：

```text
解绑 Event / LocalCall
Unbind Provide / From
Dispose Context / Service / LayerProvider
Dispose ECS / Timer / Delay / Post / EventCenter
返回 DisposeResponse
State = Exited
```

MainScope 收到 Response 后：

```text
Join Worker
Dispose Transport
移除 Endpoint
```

MainScope 最后 Stop / Dispose。

---

## 19. faster 分支复用

### 19.1 直接复用

```text
LayerBase/DataStruct/IBoundedQueue.cs
LayerBase/DataStruct/LocalRingQueue.cs
LayerBase/DataStruct/LockedBoundedRingQueue.cs

LayerBase/Scope/ScopeMetadata.cs
LayerBase/Scope/ScopeTypeRouteCache.cs
ScopeEvent / ScopeCall Envelope 基础结构
ScopeExecution 的 ThreadStatic Context
ScopeOption 的调度配置

EventCenter
PostScheduler
TimeScheduler
DelayPublisherManager
EcsWorld / Scheduler
LayerBaseSynchronizationContext

Scope Queue / Pump / Lifecycle Tests
```

### 19.2 修改后复用

```text
ScopeRuntime：
    保留本地资源创建、Pump、容量和测试；
    删除 ActorWorld、Scope 根 Provider、额外通道和业务所有权。

ScopeRuntimeHost：
    保留 Endpoint、Scope 创建和调度；
    生命周期控制改为 ScopeCall；
    不保存可供其他 Scope 访问的 ScopeRuntime。

ScopeCompositionBuilder：
    输入改为 Layer-first ScopeExecutionPlan。

ScopeObjectBinding：
    同时保留 OwnerScope 和 OwnerLayer。
```

### 19.3 仅参考

```text
旧 ScopeRuntime 的完整对象关系
ScopeActorGateway
EcsResultQueue Actor 中转
LayerRuntime Actor Command Inbox
```

### 19.4 禁止移植

```text
ScopeRuntime.ActorWorld
ScopeServiceProvider 统一解析全部 Layer
ScopeCompletionPort
ControlQueue / StopQueue / DisposeQueue
Manual Pump Queue
直接跨线程 StopLocal / DisposeLocal
Post CrossThreadIngress
ScopeRuntime 内部启动和拥有 Thread
```

---

## 20. Agent 执行任务

```text
1. 消费 05 号 Layer-first CompositionPlan。
2. 定义或修正 ScopeExecutionPlan。
3. 每个 Scope 创建独立 Event/Post/Timer/ECS/Context/LocalCall。
4. ScopeRuntime 保存 ScopeLayerSlice[]。
5. ScopeRuntime 保存按 LayerIndex映射的 LayerProviderRuntime[]。
6. 删除 Scope 根 Provider。
7. 删除 ScopeRuntime ActorWorld。
8. 删除额外跨 Scope Queue。
9. ScopeRuntimeHost 只暴露 ScopeEndpoint。
10. Worker 生命周期控制改为 ScopeCall。
11. Main/Inline/Worker Scope都保留 Push LayerIndex。
12. Activate 在 Owner Thread按 LayerIndex创建对象。
13. Running 使用预计算 Slice / Range。
14. WorkerJob Result 回 OriginScope ScopeEvent。
15. Actor 操作固定进入 MainScope ScopeEvent/Call。
16. Stop / Dispose 在 Owner Thread按 Layer逆序执行。
17. MainScope 最后停止。
18. 复用 faster 原队列、资源、Pump、测试和 Benchmark。
```

---

## 21. 必须测试

```text
Runtime_always_has_main_scope

Each_scope_has_independent_event_center

Each_scope_has_independent_post_timer_ecs

All_scopes_preserve_push_layer_order

Scope_runtime_consumes_derived_layer_slices

Scope_runtime_does_not_become_business_ownership_root

Scope_runtime_has_no_actor_world

Scope_runtime_has_no_root_business_provider

Layer_provider_is_selected_by_layer_index

Custom_scope_service_keeps_owner_layer

Inline_scope_resources_are_independent

Worker_scope_resources_are_created_on_owner_thread

Route_table_stores_endpoint_not_runtime

Only_event_and_call_inboxes_exist

Stop_is_delivered_by_scope_call

Dispose_is_delivered_by_scope_call

Call_response_returns_through_origin_call_inbox

Worker_result_returns_through_origin_event_inbox

Custom_scope_projection_uses_main_scope_event

Actor_world_is_only_written_on_main_scope_thread

Running_tick_uses_precomputed_ranges

Steady_state_scope_tick_is_zero_allocation
```

---

## 22. 验收否决项

出现以下任意一项，任务不通过：

```text
ScopeRuntime 持有 ActorWorld

ScopeRuntime 持有可解析全部 Layer 的 Provider

ScopeRuntime 重新决定 Service / Context 的 OwnerLayer

CustomScope 被描述为没有 Layer

ScopeRouteTable 返回 ScopeRuntime

出现 Event / Call 之外的跨 Scope Queue

Stop / Dispose 使用共享字段或独立通道

Worker Service 在 MainScope创建

Worker Result 直接写 PostScheduler

ScopeEvent 到达后绕过 EventCenter

运行期重新计算 Layer Range

运行期通过 Type / Dictionary 查 Layer 或 Service

为了性能为每个 Scope复制完整 Layer 树

未复用 faster 的成熟 Queue、Scheduler 或测试
```

---

## 23. 本阶段不修改的内容

本文不修改：

```text
Layer-first Composition 的具体生成器
DI 生命周期算法
Mount / Provide / From
EventCenter 内部派发
本地 Call Handler Registry
ScopeEvent / ScopeCall 业务协议细节
ECS Query 实现
ActorWorld 内部存储
WorkerEventJob 算法
```

本文只完成：

```text
Layer 管理业务对象。

Scope 提供独立线程和本地资源。

LayerBuildPlan 被投影为 Scope 的轻量执行切片。

ScopeRuntime 在正确线程执行这些切片，
但不成为新的业务管理根。
```
