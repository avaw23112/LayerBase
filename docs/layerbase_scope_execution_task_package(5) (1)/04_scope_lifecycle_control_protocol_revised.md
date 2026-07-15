# 04 Layer 管理下的 Scope 生命周期与控制协议

> **强制执行规范：** 本文必须遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`；冲突时以 00、01 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `02_scope_runtime_resources_revised.md`、`03_scope_event_call_protocol.md`、`05_scope_static_composition_generators_revised.md`、`19_layer_service_context_scope_tick_lifecycle_migration_revised.md`  
> **文档性质：** 独立阶段任务。本文只规定 Scope Activate、Stop、Dispose、Fault 与线程退出的控制协议，不重新设计 DI、EventCenter、Post、ECS、ActorWorld 或业务生命周期接口。

---

## 0. 本阶段核心目的

Scope 生命周期控制必须满足两个同时成立的边界：

```text
控制边界：
    Activate / Stop / Dispose
    统一通过 ScopeCall 进入目标 Scope Owner Thread。

业务管理边界：
    目标 Scope 内的 Service / Context
    仍由 LayerBuildPlan 和 Push LayerIndex 管理。
```

最终控制流：

```text
LayerRuntime Coordinator
    → ScopeActivateCall
    → TargetScope CallInbox
    → TargetScope Owner Thread
    → 按 LayerIndex激活该 Scope 的 Layer Slice
    → ActivateResponse

LayerRuntime Coordinator
    → ScopeStopCall
    → TargetScope CallInbox
    → TargetScope Owner Thread
    → 按 LayerIndex逆序停止 Layer Slice
    → StopResponse

LayerRuntime Coordinator
    → ScopeDisposeCall
    → TargetScope CallInbox
    → TargetScope Owner Thread
    → 按 LayerIndex逆序释放 Layer Slice 与 Scope 本地资源
    → DisposeResponse
```

禁止：

```text
Scope 是无 Layer 的对象集合
ScopeActivate 直接遍历一个无层级 ObjectPlan
MainScope 跨线程直接调用 Worker Service/Context
独立 StopQueue / DisposeQueue / ControlQueue
```

---

## 1. 最终公有 API

保持 Runtime 级生命周期 API：

```csharp
public sealed class LayerRuntime :
    IDisposable
{
    public RuntimeState State {
        get;
    }

    public void Activate();

    public LBTask StopAsync(
        CancellationToken cancellationToken =
            default);

    public void Dispose();
}
```

第一阶段所有 Scope 随 Runtime 一起 Activate / Stop / Dispose。

动态 Scope API不是本阶段必需内容。若 `faster` 已有内部动态 Scope 管理入口，可以保留内部能力，但必须使用同一控制协议：

```csharp
internal LBTask<ScopeStartResult>
    StartScope<TScope>();

internal LBTask<ScopeStopResult>
    StopScope<TScope>();
```

不得为动态 Scope另建控制通道。

---

## 2. 唯一控制管线

ScopeCall 的以下消息共用同一个物理 CallInbox：

```text
Business Request
Business Response
Activate Request / Response
Stop Request / Response
Dispose Request / Response
Snap SafePoint Request / Response
Diagnostics Request / Response
```

控制消息可以有内部保留容量或优先级，但不能创建新的 Queue。

```csharp
internal readonly struct ScopeActivateCall
{
    internal readonly int RuntimeGeneration;
}

internal readonly struct ScopeStopCall
{
    internal readonly ScopeStopPolicy Policy;
}

internal readonly struct ScopeDisposeCall
{
}
```

实际 Envelope、RouteId 和 Response 类型必须复用 03 号文档及 `faster` 现有 ScopeCall 基础结构。

---

## 3. Bootstrap Handler

Activate 前业务资源尚未创建，因此 CallInbox 必须支持最小 Bootstrap Dispatcher：

```text
Transport 已创建
CallInbox 已创建
ScopeEndpoint 已创建
Bootstrap Dispatcher 可识别 Activate / Stop / Dispose
业务 LocalCall Registry 尚未创建
```

Bootstrap Dispatcher：

```csharp
internal sealed class
    ScopeLifecycleBootstrapDispatcher
{
    internal void Dispatch(
        ScopeRuntime shell,
        in ScopeCallEnvelope envelope);
}
```

它不是：

```text
第二个 CallInbox
第二套 ScopeCall 协议
新的生命周期线程
```

Activate 成功后，业务 ScopeCall Route 与生命周期 Route 继续共用同一 CallInbox。

---

## 4. 生命周期所有权

### 4.1 LayerRuntime Coordinator

负责：

```text
创建 Scope Transport
创建 Main / Inline / Worker Scope 壳体
启动 Worker Thread
发送生命周期 ScopeCall
Pump Main/Inline Bootstrap
等待 Response
Join Worker
最后释放 Transport 与 Endpoint
```

不负责：

```text
创建 Worker Service / Context
直接执行 CustomScope RuntimeStop
直接 Dispose CustomScope Provider / ECS / EventCenter
直接写 CustomScope State
```

### 4.2 Scope Owner Thread

只能由 Owner Thread执行：

```text
创建 Scope 本地资源
创建该 Scope 的各 LayerProvider
创建 Service / Context
Mount
Provide / From
Event Handler 注册
LocalCall Handler 绑定
Initialize / PostBuild / RuntimeStart
Tick
RuntimeStop
Dispose
修改 ScopeRuntimeState
```

### 4.3 Layer 的管理作用

Lifecycle Controller 必须消费：

```text
ScopeExecutionPlan
    → ScopeLayerSlice[]
    → ScopeLifecyclePlan
```

这些执行切片来源于：

```text
LayerBuildPlan[]
```

Controller 不允许把 Scope 中所有 Service 视为一个无 Layer 集合。

---

## 5. 状态模型

```csharp
internal enum ScopeRuntimeState :
    byte
{
    Created,
    Bootstrapping,
    Activating,
    Running,
    Stopping,
    Stopped,
    Disposing,
    Exited,
    Faulted
}
```

强制规则：

```text
状态只由 Owner Thread写入。
外部只通过 ScopeCall请求变化。
线程唤醒信号不改变状态。
MainScope 也使用同一状态机。
```

合法转换：

```text
Created
    → Bootstrapping
    → Activating
    → Running
    → Stopping
    → Stopped
    → Disposing
    → Exited
```

失败路径：

```text
Bootstrapping / Activating / Running / Stopping
    → Faulted
    → Stopping 或 Disposing
    → Exited
```

不得使用多线程 CAS 让调用线程抢占 Owner Thread 生命周期。

---

## 6. Transport 与业务资源分层

### 6.1 Transport 生命周期

Transport 在业务资源前创建，并晚于 DisposeResponse 释放：

```text
ScopeEndpoint
ScopeEventInbox
ScopeCallInbox
Wake Signal
RuntimeGeneration
```

### 6.2 业务资源生命周期

Activate 在 Owner Thread 创建：

```text
EventCenter
PostScheduler
Timer / Delay
EcsWorld / EcsScheduler
SynchronizationContext
ScopeLocalCallRegistry
LayerProviderRuntime[]
Service / Context
Tool
Lifecycle Plan Runtime State
```

Transport 必须允许：

```text
ActivateCall 在业务资源不存在时进入
Faulted Scope继续接收 Response / Stop / Dispose
DisposeResponse 发出后再关闭
```

---

## 7. MainScope、InlineScope 与 WorkerScope

三种 Scope 使用同一 Lifecycle Handler。

### 7.1 MainScope

```text
LayerRuntime 创建 MainScope Transport
    → 向 MainScope CallInbox 写 ActivateCall
    → 主线程执行 MainScope BootstrapPump
    → MainScope Handler Activate
    → Response
```

不允许 MainScope使用另一套直接初始化流程。

### 7.2 InlineScope

```text
创建 Inline Transport
    → ActivateCall
    → 主线程执行该 InlineScope BootstrapPump
    → InlineScope 在自己的 ScopeExecution Context中创建资源
    → Response 回 Origin CallInbox
```

InlineScope 与 MainScope 同线程，但资源、状态、Layer Slice 和 SynchronizationContext 独立。

### 7.3 WorkerScope

```text
创建 Worker Transport
    → 启动 Worker Thread
    → 安装 Scope SynchronizationContext
    → 进入 Bootstrapping，只 Drain CallInbox
    → 收到 ActivateCall
    → 在 Worker Thread Activate
    → Response
```

Worker Thread启动不等于 WorkerScope 已 Running。

---

## 8. Activate 事务

### 8.1 输入

Lifecycle Controller 接收：

```csharp
internal ScopeActivateResult Activate(
    ScopeRuntime scope,
    ScopeExecutionPlan plan);
```

`ScopeExecutionPlan` 是从 LayerBuildPlan 投影出的执行视图。

### 8.2 激活顺序

```text
1. State：Bootstrapping → Activating。
2. 创建 EventCenter / Post / Timer / Delay。
3. 创建 EcsWorld / EcsScheduler。
4. 创建 SynchronizationContext。
5. 创建 ScopeLocalCallRegistry。
6. 按 Push LayerIndex创建 LayerProviderRuntime。
7. 按 LayerIndex创建 Service。
8. 按 LayerIndex创建 Context。
9. Attach ScopeObjectBinding。
10. 按 LayerIndex执行 Mount。
11. 按 LayerIndex执行 Provide / From。
12. 按 LayerIndex执行 faster 原 Event Handler 注册。
13. 绑定当前 Scope 的 LocalCall Handler。
14. 绑定 Generated ECS Query。
15. Initialize：LayerIndex 正序。
16. PostBuild：LayerIndex 正序。
17. Prewarm / Freeze。
18. RuntimeStart：LayerIndex 正序。
19. 打开 Business Admission。
20. State = Running。
21. 返回 ActivateResponse。
```

各子系统具体内部算法由对应文档负责。

Lifecycle Controller 只执行预计算 Invoker 和 Range。

### 8.3 空 Layer

某 Scope 中某 Layer无对象：

```text
保留 LayerIndex
对应 Slice Count = 0
直接跳过
```

不能因此重新给后续 Layer编号。

---

## 9. Activate 失败与回滚

任一步失败：

```text
1. 记录 FailureStage、LayerIndex、ObjectSlot 和异常。
2. 关闭 Business Admission。
3. 逆序回滚已经完成的阶段。
4. 只释放已经成功创建或绑定的对象。
5. Transport 和 Bootstrap Dispatcher 保留。
6. 返回 Faulted ActivateResponse。
7. State = Faulted。
8. Coordinator 随后发送 DisposeCall。
```

回滚必须在目标 Scope Owner Thread执行。

逆序关系：

```text
已 RuntimeStart
    → RuntimeStop

已 Event/Call Bind
    → Unbind

已 Provide / From
    → Unbind

已 Mount
    → Unmount / Dispose 对应对象

已创建 Context
    → 按 Layer逆序 Dispose Context

已创建 Service / Provider
    → 按 Layer逆序 Dispose

已创建 Scope 本地资源
    → 逆序 Dispose
```

不得让 MainScope 直接释放 Worker 半成品。

---

## 10. Stop 协议

### 10.1 准入状态

收到 `ScopeStopCall`：

```text
Running / Faulted
    → Stopping
```

停止时将入口分为：

```text
Business：
    新 Event
    新 Business Call Request
    新 Post
    新 Timer
    新 ECS Query
    新 WorkerEventJob

Control/Critical：
    ScopeCall Response
    Stop / Dispose
    已接受 Worker Result
    Actor Projection Result
    Fault / Diagnostics
```

关闭 Business 准入，但保留 Control/Critical。

### 10.2 WorkerEventJob

```text
1. 关闭当前 Scope JobGroup，拒绝新 Job。
2. 请求 Pending/Running Job协作取消。
3. 保持 OriginScope ScopeEventInbox 可接收 Result。
4. 等待该 Scope JobGroup 归零。
5. Drain 已接受 Result / Failed Event。
```

禁止：

```text
保持 PostScheduler CrossThreadIngress
Worker Result 直接写 PostScheduler
全局 WorkerResultQueue
```

### 10.3 Drain Policy

```text
处理停止前已经接受的 Business Event / Call / Result。
每个已接受 Business Call必须获得正常、取消或 ScopeStopped终态。
```

### 10.4 Drop Policy

```text
未 Dispatch 的普通 Business Event可丢弃并释放 Payload。
未 Dispatch 的 Business Call返回 ScopeStopped。
Control/Critical 不丢弃。
```

### 10.5 RuntimeStop

所有已接受异步边界终结后：

```text
RuntimeStop：
    Push LayerIndex 逆序

解除 Event / LocalCall Handler：
    Push LayerIndex 逆序
```

然后：

```text
关闭本地 continuation 新入口
确认没有悬挂 Promise / Query Lease
State = Stopped
返回 StopResponse
```

---

## 11. Dispose 协议

前置：

```text
State = Stopped 或 Faulted
Business Admission 已关闭
```

Owner Thread 按 LayerIndex逆序执行：

```text
1. State → Disposing。
2. Unbind Generated ECS Query。
3. Unbind Provide / From。
4. Dispose Context。
5. Dispose Transient / Scoped / Singleton / Instance（按所有权）。
6. Detach ScopeObjectBinding。
7. Dispose LayerProviderRuntime。
8. Dispose EcsWorld / EcsScheduler。
9. Dispose Timer / Delay / Post / EventCenter。
10. Dispose ScopeLocalCallRegistry。
11. 确认 SynchronizationContext 无 pending source。
12. 清理业务资源引用。
13. 发送 DisposeResponse。
14. State = Exited。
```

Transport 必须在 `DisposeResponse` 成功写入 Origin CallInbox 后才能由 Coordinator 释放。

---

## 12. Worker 退出与 Join

WorkerLoop：

```text
while State != Exited:
    Drain CallInbox
    Drain EventInbox

    if State == Running:
        Tick Scope

    WaitUntilNextTickOrSignal
```

处理 DisposeCall 后：

```text
发送 DisposeResponse
State = Exited
退出循环
恢复线程原 SynchronizationContext
释放 Worker Thread本地设施
Thread 返回
```

Coordinator：

```text
收到 DisposeResponse
    → Join Worker
    → Dispose Transport
    → 移除 ScopeEndpoint
```

Wake Signal 只通知：

```text
Inbox 可能非空
需要重新检查 Tick 时间
```

不表示 Stop / Dispose。

---

## 13. Runtime 启动顺序

```text
1. Build / Freeze RuntimeCompositionPlan。
2. 创建 MainScope Transport。
3. 创建 InlineScope Transport。
4. 创建 WorkerScope Transport 和 Worker Thread。
5. Activate MainScope。
6. Activate InlineScope（稳定 ScopeId 顺序）。
7. Activate WorkerScope（可并发发送 Call）。
8. Pump Response。
9. 全部成功后 Runtime.State = Running。
```

如果任意 Scope Activate失败：

```text
记录失败 Scope
    → 已 Running Scope 逆序 Stop
    → 全部 Scope Dispose
    → Join Worker
    → MainScope 最后 Dispose
    → Runtime.State = Faulted
```

---

## 14. Runtime 停止顺序

```text
1. Runtime 停止外部新输入。
2. MainScope 向所有 CustomScope 发送 StopCall。
3. Pump Main/Inline Scope并收集 StopResponse。
4. 对已停止 Scope发送 DisposeCall。
5. 收集 DisposeResponse。
6. Join Worker。
7. 释放 CustomScope Transport。
8. MainScope 处理自己的 StopCall。
9. MainScope 处理自己的 DisposeCall。
10. Dispose MainActorRuntime / ActorWorld。
11. Runtime.State = Disposed。
```

MainScope 最后停止，确保它能接收：

```text
ScopeCall Response
Worker Result
Actor Projection Result
Fault Event
Diagnostics
```

---

## 15. Faulted Scope

不可恢复异常：

```text
1. 在 Owner Thread记录 Fault。
2. 关闭 Business Admission。
3. State = Faulted。
4. 通过 ScopeEvent向 MainScope上报。
5. 继续处理 Call Response、StopCall、DisposeCall。
6. Coordinator 根据策略发送 Stop / Dispose。
```

Faulted 不等于线程立即退出。

不得：

```text
异常线程直接 Thread.Abort
MainScope 强制清理 Worker 本地对象
Fault 后关闭 Transport导致 Response 泄漏
```

---

## 16. 超时与卡死策略

ScopeRuntime 内不实现强制线程终止。

宿主层可配置：

```text
ControlCallTimeout
WorkerJoinTimeout
```

超时只产生：

```text
ScopeControlTimeout
WorkerJoinTimeout
诊断快照
```

是否 FailFast 由宿主策略决定。

不得在超时后跨线程调用：

```text
StopLocal
DisposeLocal
EventCenter.Reset
EcsWorld.Dispose
```

---

## 17. faster 分支复用

### 17.1 直接复用

```text
ScopeStopPolicy
ScopeRuntimeState 的测试场景
Closable / Bounded RingQueue 关闭语义
WorkerRuntime 的 Thread / Signal / Join 模式
LayerBaseSynchronizationContext 的 Install / Drain / Close
ScopeSubscriptionRegistry 的逆序取消订阅语义
BuildFailureCleanupTests
ScopePromiseShutdownTests
RuntimeSafetyRegressionTests
```

### 17.2 修改后复用

```text
ScopeRuntimeHost：
    保留 Transport、Endpoint、Worker协调；
    控制统一进入 ScopeCallInbox；
    不直接 Dispose CustomScope。

ScopeLifecycleController：
    输入改为 Layer-first ScopeExecutionPlan；
    按 ScopeLayerSlice和预计算 Invoker执行。

WorkerRuntime：
    保留线程循环与唤醒；
    不使用 Job/Event 双队列承载 Scope 生命周期。
```

### 17.3 禁止移植

```text
独立 Completion Port
ManualResetEventSlim Dispose 完成通道
并发 Dispose 状态机
MainScope 直接 StopLocal / DisposeLocal
Worker StopFlag 承载命令
PostScheduler CrossThreadIngress 接收 Worker Result
ScopeRuntime 自己创建并管理 Thread
```

---

## 18. Agent 执行任务

```text
1. 定义 Activate / Stop / Dispose ScopeCall 和 Response。
2. 所有控制消息使用现有 CallInbox。
3. 建立 Activate 前 Bootstrap Dispatcher。
4. MainScope / InlineScope / WorkerScope使用同一 Lifecycle Handler。
5. Lifecycle Controller 消费 Layer-first ScopeExecutionPlan。
6. Activate 按 Push LayerIndex创建 Provider、Service、Context。
7. Mount / Provide / Event / Call / Query 按 Layer Slice执行。
8. Initialize/PostBuild/RuntimeStart 按 Layer正序。
9. RuntimeStop/Dispose 按 Layer逆序。
10. Activate 失败在 Owner Thread按完成阶段回滚。
11. Stop 区分 Business 与 Control/Critical 准入。
12. WorkerEventJob Result 保持通过 OriginScope ScopeEvent回流。
13. 每个已接受 Call必须得到终态 Response。
14. DisposeResponse 发送后才允许 Worker退出和 Transport关闭。
15. MainScope 最后 Stop / Dispose。
16. Faulted Scope继续接受 Stop / Dispose。
17. 删除独立 Stop/Dispose/Completion通道。
18. 复用 faster 的 Queue、Thread、Context、测试和清理算法。
```

---

## 19. 必须测试

```text
Main_scope_activation_uses_call_inbox

Inline_scope_activation_uses_call_inbox

Worker_scope_activation_uses_call_inbox

All_scope_activation_uses_layer_slices

Worker_service_is_created_on_worker_thread

Activate_runs_layers_in_push_order

Activate_failure_rolls_back_layers_in_reverse_order

Activate_failure_still_accepts_dispose_call

Stop_closes_business_but_accepts_control_and_response

Drain_policy_completes_all_accepted_calls

Drop_policy_returns_scope_stopped_for_pending_calls

Worker_result_returns_through_scope_event_during_stop

Runtime_stop_runs_in_reverse_layer_order

Dispose_runs_in_reverse_layer_order

Dispose_response_is_sent_before_worker_exit

Worker_is_joined_after_dispose_response

Transport_outlives_business_resources

Main_scope_is_disposed_last

Faulted_scope_still_accepts_stop_and_dispose

No_direct_cross_thread_stop_or_dispose_exists

No_extra_lifecycle_queue_exists

No_post_cross_thread_ingress_is_used
```

---

## 20. 验收否决项

出现以下任意一项，任务不通过：

```text
CustomScope Stop/Dispose 由 MainScope 直接调用

Scope 生命周期不经过 CallInbox

出现 StopQueue / DisposeQueue / ControlQueue / CompletionQueue

Worker 通过 volatile StopFlag 承载命令

Worker Service / Context 在 MainScope 创建

Activate 把 Scope 中对象视为无 Layer集合

CustomScope 被描述为没有 Layer

Activate 失败由错误线程释放半成品

Stop 时已接受 Call无终态

Worker Result 直接进入 PostScheduler

DisposeResponse 前关闭 Transport

MainScope 早于 CustomScope Dispose

ScopeRuntime 自己创建 Thread

为了本任务重写 DI、EventCenter、ECS 或 ActorWorld
```

---

## 21. 本阶段不修改的内容

本文不修改：

```text
DI Provider 内部解析算法
Mount / Provide / From 匹配算法
EventCenter Bucket 与派发
ScopeEvent / ScopeCall Envelope 底层实现
PostScheduler 策略
ECS Query 与 CommandBuffer
ActorWorld
WorkerEventJob 计算算法
```

本文只保证：

```text
生命周期控制通过 ScopeCall进入 Owner Thread。

LayerBuildPlan继续管理业务对象和顺序。

ScopeRuntime只在正确线程执行各 Layer 的轻量切片。
```
