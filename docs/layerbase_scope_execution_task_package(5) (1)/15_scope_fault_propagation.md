# 15 Scope 故障传播与最小异常策略

> **强制执行规范：** 本文的实现必须遵守 `01_mandatory_architecture_aot_performance_standards.md`；冲突时以该规范为准。  

> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **文档性质：** 独立故障设计文档。本文定义异常如何通过现有 Event/Call 管线传播，不建立异常专用通道。

---

<!-- ARCHITECTURE-REVISION-START -->
## 0. 架构位置

所有子系统故障先归属于当前 Scope，再由 ScopeFaultPolicy 决定是否上报 MainScope：

```text
Event/Post/ECS/Timer/Tool/WorkerJob
    → ScopeFaultRecord
    → ScopeFaultPolicy
       ├── Continue
       ├── StopScope
       └── StopRuntime
    → 必要时 ScopeFaultEvent<MainScope>
```

不建立复杂全局 ExceptionHub 作为业务真值。

### 0.1 最终公有 API

```csharp
public interface IScopeFaultPolicy
{
    ScopeFaultAction OnFault(
        in ScopeFaultContext context);
}

public sealed class LayerRuntime
{
    public event Action<ScopeFaultInfo>? Faulted;
}
```

配置：

```csharp
builder.AddScope<PathfindingScope>(
    options => options.FaultPolicy =
        ScopeFaultPolicies.StopScope);
```

业务 Handler 无需捕获框架异常；预期失败使用 `Try*`/结果枚举。

### 0.2 业务场景

PathfindingScope Query 抛异常：

```text
EcsScheduler 捕获
    → ScopeFaultRecord(EcsExecute)
    → Policy = StopScope
    → 关闭 PathfindingScope Admission
    → MainScope 收到 ScopeFaultEvent
    → Runtime 其他 Scope 继续运行
```

MainScope 致命故障通常升级为 StopRuntime。

### 0.3 关键结构

```csharp
public readonly struct ScopeFaultRecord
{
    public ScopeAddress Scope { get; }
    public ScopeFaultPhase Phase { get; }
    public Exception Exception { get; }
    public int RouteOrObjectSlot { get; }
}
```

Fault 路径是冷路径，可以保存 Exception 和类型信息；但不能在热路径预先构造字符串。

### 0.4 faster 复用

直接复用：

```text
LayerExceptionHub 的异常回调思想
Handler 熔断测试
WorkerJobFailedEvent
```

改造：

```text
异常先进入 Scope Fault
Runtime 回调只做聚合
```

禁止：

```text
跨 Scope 直接抛异常
多个错误队列
故障时扫描全部 Runtime 对象
```
<!-- ARCHITECTURE-REVISION-END -->

## 全局约束

1. 每个 Scope 独立持有运行资源，所有本地资源只由 Scope Owner Thread 访问。
2. Inline Scope 由 `LayerRuntime` 主线程按稳定顺序推进；Worker Scope 由 `ScopeWorker` 独立线程调度。
3. Scope 间只允许通过 `ScopeEvent` 和 `ScopeCall` 两条 MPSC 管线通讯。
4. `ScopeCall` 的 Request、Response、Activate、Stop、Dispose 使用同一条 Call 管线，不新增 Completion、Control、Stop 或 Dispose 通道。
5. Actor 操作由 CustomScope 通过内部 `ScopeEvent` 发往 MainScope；CustomScope 不持有 `ActorWorld`。
6. MainScope 是 `ActorWorld` 唯一写入者和推进者。
7. Worker 唤醒原语只负责唤醒线程，不承载命令语义。
8. Scope 内部不因跨线程问题引入锁；并发控制只存在于两条 MPSC Inbox 和线程唤醒边界。
9. 实现前必须检查 `faster` 对应代码。可直接或修改复用的代码不得重复实现。
10. 不引入 AssemblyModule、LayerTool、通用 WorkerJob、跨 Scope 资源注入、多线程 ActorWorld或Running 热路径程序集扫描或不受控 AppDomain 全量扫描。

## faster 复用记录要求

每个实现提交必须记录：

```text
复用来源：
复用方式：直接移植 / 修改移植 / 仅参考 / 禁止移植
修改原因：
未复用原因：
```

---


## 1. 设计目标

异常处理必须服从两条既有通讯管线：

```text
ScopeCall Handler 异常
    → ScopeCall Faulted Response

ScopeEvent/Update/Timer/ECS/Continuation/WorkerLoop 异常
    → ScopeFaultEvent<MainScope>
```

禁止：

```text
ExceptionQueue
ExceptionHub 独立 MPSC
跨线程直接调用用户异常回调
Worker 异常静默终止
```

---

## 2. 故障分类

```csharp
public enum ScopeFaultPhase
{
    Activate,
    ServiceInitialize,
    ServiceUpdate,
    ServiceStop,
    ServiceDispose,
    ContextInitialize,
    ContextUpdate,
    ContextDispose,

    EventDispatch,
    CallDispatch,
    CallResponseApply,

    SynchronizationContext,
    Continuation,
    Timer,
    Delay,
    PostScheduler,

    EcsSubmit,
    EcsExecute,
    EcsResultApply,

    ActorEventEncode,
    ActorEventApply,

    WorkerLoop,
    QueueAdmission,
    ResourceUnbind,
    Unknown
}
```

分类只用于诊断和策略，不为每一阶段创建独立通道。

---

## 3. ScopeFaultRecord

```csharp
public readonly struct ScopeFaultRecord
{
    public int RuntimeId { get; }
    public int RuntimeGeneration { get; }
    public int SourceScopeId { get; }

    public ScopeFaultPhase Phase { get; }
    public Exception Exception { get; }

    public int RouteId { get; }
    public int ServiceSlot { get; }
    public int ContextSlot { get; }
    public long Timestamp { get; }
}
```

不要求第一版构建复杂 Trace 树，只记录足以定位 Scope、阶段和 Handler 的字段。

---

## 4. ScopeFaultEvent

```csharp
[ScopeEvent<MainScope>]
internal readonly struct ScopeFaultEvent
{
    public ScopeFaultRecord Record { get; }
}
```

CustomScope 通过自己的 EventWriter 写入 MainScope EventInbox。

Fault Event 使用 `Critical` admission，消费 EventInbox 的保留容量。

---

## 5. ScopeCall Handler 异常

Call Dispatcher：

```text
try:
    执行 Handler
    生成 Succeeded Response
catch Exception:
    生成 Faulted Response
```

异常作为 Call Result 返回调用方，不额外发送 FaultEvent，避免同一故障重复上报。

只有以下情况额外发送 FaultEvent：

```text
Response 无法编码
Response 无法进入仍存活的 Origin CallInbox
Call Dispatcher 自身损坏
```

---

## 6. ScopeEvent Handler 异常

业务 ScopeEvent 没有调用方 Response。

Handler 抛异常：

```text
1. 当前 Event Payload 按所有权规则释放。
2. 创建 ScopeFaultRecord。
3. 发送 ScopeFaultEvent<MainScope>。
4. 根据 Scope 本地最低限度规则决定是否继续本 Tick。
```

最终 StopScope/StopRuntime 决策由 MainScope执行。

---

## 7. MainScope 故障

MainScope 无法向自己跨线程上报。

MainScope 内出现故障：

```text
直接调用 LayerRuntime 的本地 FaultDispatcher
```

这不是跨 Scope 通道，因为调用发生在 MainScope Owner Thread。

用户异常回调也只能由 MainScope/LayerRuntime Owner Thread 调用。

---

## 8. WorkerLoop 最外层异常

```csharp
try
{
    RunLoop();
}
catch (Exception exception)
{
    ReportCriticalFault(exception, ScopeFaultPhase.WorkerLoop);
    EnterFaultedControlLoop();
}
```

`EnterFaultedControlLoop`：

```text
停止业务 Tick
关闭 Business 准入
继续 Drain Response 和 Control Call
等待 MainScope 的 StopCall/DisposeCall
```

不允许线程因异常直接退出，使 MainScope 永远收不到 Dispose Response。

---

## 9. Critical Event 保证

Fault Event 不得静默丢弃。

同一 EventInbox：

```text
Business 配额
Internal 保留配额
Critical 保留配额
```

如果 Critical 区也已满：

```text
Worker 在 MainScope 仍存活时进行有界自旋/等待并唤醒 MainScope
直到成功入队或 Transport 已关闭
```

不增加 FaultQueue。

MainScope 在 Runtime Stop 期间必须持续 Drain Critical Event，直到所有 Worker Exited。

---

## 10. 最小策略

```csharp
public enum ScopeFaultPolicy
{
    ReportAndContinue,
    StopScope,
    StopRuntime
}
```

配置可以按大类指定：

```text
Activate
BusinessDispatch
Continuation
Scheduler
ECS
WorkerLoop
```

第一版不实现：

```text
RethrowOnMainScope
每个细分 Phase 独立策略
自动重启 Scope
复杂 Exception Fusion
FailFast 策略矩阵
```

宿主可在收到 `StopRuntime` 后自行选择终止进程。

---

## 11. 策略应用

MainScope 收到 FaultEvent：

```text
1. 调用 ILayerExceptionSink。
2. 查询 ScopeFaultPolicy。
3. ReportAndContinue：
       不发送控制命令。
4. StopScope：
       向 SourceScope 发送 StopCall，再 DisposeCall。
5. StopRuntime：
       启动 Runtime Stop 流程。
```

控制行为仍通过 ScopeCall。

---

## 12. 故障回调隔离

```csharp
public interface ILayerExceptionSink
{
    void OnScopeFault(in ScopeFaultRecord record);
}
```

Sink 在 MainScope 调用。

Sink 自己抛异常：

```text
记录为 MainScope 本地 EmergencyCallbackFailure
不得递归发送 ScopeFaultEvent
```

只允许一个极小的宿主级 emergency callback，不能成为新的跨线程异常通道。

---

## 13. Queue Admission 故障

普通 Business Post/Call 返回 `Full`：

```text
由调用方按 API 语义处理
不自动上报 FaultEvent
```

以下情况上报：

```text
Response 在 Origin 仍存活时无法进入保留区
Control Call 无法进入保留区
Critical Event 无法在 Transport 存活时最终进入
```

这表示协议不变量被破坏。

---

## 14. Activate/Stop/Dispose 异常

### Activate

通过 Faulted ActivateResponse 返回 MainScope。

### Stop

Stop 阶段异常被收集：

```text
继续执行其余 Stop 步骤
最终 StopResponse 包含 AggregateException
状态仍进入 Stopped 或 Faulted
```

### Dispose

Dispose 阶段异常同样聚合：

```text
尽量完成全部资源释放
DisposeResponse 返回 AggregateException
Worker 随后仍退出
```

不能因为第一个 Dispose 异常跳过剩余资源。

---

## 15. faster 复用清单

### 直接或修改复用

```text
ILayerExceptionSink
LayerExceptionRecord 的字段设计
LayerExceptionPhase 的阶段分类
LayerExceptionInfoAdapter
LayerHubExceptionCallbacks 的兼容适配
异常 Benchmark 的 Record 构造成本测试
```

### 删除或禁止移植

```text
LayerExceptionHub 的独立 LockedBoundedRingQueue
ExceptionQueue Overflow 通道
复杂 LayerExceptionPolicy 矩阵
ScopeRuntime 直接调用用户回调
```

复用方式：

```text
保留 Record、Sink、Adapter
用 ScopeFaultEvent 替换 ExceptionHub Queue
精简 Phase 和 Policy
```

---

## 16. 实施顺序

1. 定义 FaultRecord、FaultPhase、FaultPolicy。
2. 定义 ScopeFaultEvent<MainScope>。
3. Call Handler 异常转换为 Response。
4. Event/Timer/ECS/Continuation 异常转换为 FaultEvent。
5. WorkerLoop 加入 FaultedControlLoop。
6. MainScope FaultDispatcher 应用策略。
7. 接入 StopScope/StopRuntime。
8. 移植 Sink 兼容适配。
9. 删除 ExceptionHub Queue。

---

## 17. 必须测试

```text
Call_handler_exception_returns_faulted_response
Call_handler_exception_is_not_double_reported
Event_handler_exception_emits_scope_fault_event
Worker_loop_exception_does_not_silently_exit
Faulted_worker_still_accepts_stop_and_dispose
Fault_event_uses_main_scope_event_inbox
Fault_event_can_enter_when_business_quota_is_full
Main_scope_invokes_sink_on_main_thread
Stop_scope_policy_uses_control_call
Stop_runtime_policy_uses_runtime_stop_flow
Dispose_errors_are_aggregated_and_worker_still_exits
No_exception_queue_exists
```

---

## 18. 验收否决项

```text
新增 ExceptionHub MPSC Queue
Worker 异常直接终止线程
用户异常回调在 Worker Thread 执行
Call Handler 异常同时 Response 和普通 FaultEvent 重复上报
故障策略直接跨线程调用 StopLocal/DisposeLocal
Critical Fault 可能静默丢弃
```
