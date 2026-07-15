# 03 ScopeEvent / ScopeCall 通讯协议

> **强制执行规范：** 本文的实现必须遵守 `01_mandatory_architecture_aot_performance_standards.md`；冲突时以该规范为准。  

> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **文档性质：** 独立协议设计文档。本文只定义 Scope 间通讯、消息终态、队列准入和负载所有权。

---

<!-- ARCHITECTURE-REVISION-START -->
## 0. 架构位置与模块关系

`ScopeEvent` 和 `ScopeCall` 是整个多 Scope 架构的唯一跨域通道：

```text
Scope A Local API
    → Generated RouteId
    → ScopeEndpoint
    → 目标 Scope Inbox
    → 目标 Owner Thread
    → Generated Handler Invoker
```

Lifecycle、ActorCommand、Projection、Snap 和 Diagnostics 的跨 Scope 控制都复用该协议，不再各建一套队列。

### 0.1 最终公有 API

```csharp
public readonly struct ScopeRef<TScope>
{
    public bool TryPost<TEvent>(in TEvent value)
        where TEvent : struct;

    public ScopePostResult Post<TEvent>(in TEvent value)
        where TEvent : struct;

    public LBTask<TResponse> Call<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct;
}
```

OwnerScope 对象的便捷入口：

```csharp
this.Scope<PathfindingScope>().TryPost(in command);

PathResult result =
    await this.Scope<PathfindingScope>()
        .Call<FindPathRequest, PathResult>(
            in request,
            cancellationToken);
```

不公开 Envelope、PayloadHandle、Inbox 和 PromiseTable。

### 0.2 业务场景：跨 Scope 寻路

```csharp
public readonly struct FindPathRequest
{
    public readonly int Start;
    public readonly int End;
}

public readonly struct PathResult
{
    public readonly PathStatus Status;
    public readonly PathHandle Path;
}
```

Handler：

```csharp
[Scope<PathfindingScope>]
public sealed partial class PathfindingService : IService
{
    [ScopeCall]
    private PathResult FindPath(in FindPathRequest request)
    {
        return _graph.Find(in request);
    }
}
```

调用流程：

```text
MainScope Register Promise
    → Request Envelope 写入 PathfindingScope CallInbox
    → Worker Owner Thread 调用 FindPath
    → Response Envelope 写回 MainScope CallInbox
    → MainScope TryComplete Promise
    → await continuation 留在 MainScope
```

### 0.3 关键内部数据结构

```csharp
internal readonly struct ScopeRoute
{
    public readonly int TargetScopeId;
    public readonly int HandlerSlot;
    public readonly ScopeRouteKind Kind;
}

internal readonly struct ScopeCallToken
{
    public readonly int OriginScopeId;
    public readonly int Sequence;
    public readonly int Version;
}
```

Inbox 分离：

```text
ScopeEventInbox：
    业务单向消息

ScopeCallInbox：
    Request / Response / Lifecycle Control
```

CallInbox 必须为 Response 和内部控制保留容量，防止请求塞满后响应无法返回。

### 0.4 热路径伪代码

```csharp
public bool TryPost<TEvent>(in TEvent value)
{
    int routeId = GeneratedScopeRoutes<TScope, TEvent>.RouteId;
    ref readonly ScopeRoute route = ref _runtime.EventRoutes[routeId];
    PayloadHandle payload = _payloadPool.RentAndWrite(in value);

    if (!_eventInboxTable[route.TargetScopeId].TryEnqueue(
            new ScopeEventEnvelope(routeId, payload)))
    {
        _payloadPool.Release(payload);
        return false;
    }

    _wakeTable[route.TargetScopeId].Signal();
    return true;
}
```

Call 完成必须遵守：

```text
Accepted Request
    → Completed / Faulted / Cancelled / ScopeStopped
    → 恰好一个终态
```

### 0.5 faster 复用

直接复用：

```text
ScopeAddress / RuntimeGeneration
ScopeRef Post/Call Generator
ScopeCallToken 的 Sequence + Version
Inbox 容量和保留槽测试
```

改造：

```text
统一所有内部控制消息
Response 回原 ScopeCallInbox
RouteId 使用 CompositionPlan
```

禁止：

```text
额外 Completion Queue
ScopeRef 直接找到 ScopeRuntime
跨 Scope 共享 Service/Context 引用
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

本协议必须成为所有跨 Scope 行为的唯一基础：

```text
业务单向消息      → ScopeEvent
业务请求与响应    → ScopeCall
Activate/Stop/Dispose → ScopeCall
Actor 投影与 Actor 消息 → ScopeEvent<MainScope>
Worker 故障上报   → ScopeEvent<MainScope>
```

协议完成后，不得存在以下旁路：

```text
CompletionPort
ControlQueue
DisposeQueue
ProjectionQueue
ActorCommandQueue
跨 Scope SynchronizationContext.Post
直接访问目标 ScopeRuntime
```

---

## 2. 协议边界

### 2.1 ScopeEvent

`ScopeEvent` 是不要求响应的单向消息。

适用范围：

```text
业务通知
状态快照或增量 DTO
Actor 投影批次
Actor 生命周期请求
ScopeFaultEvent
其他不要求返回值的内部通知
```

### 2.2 ScopeCall

`ScopeCall` 是具有唯一终态的请求—响应协议。

适用范围：

```text
业务查询和命令
需要返回结果的跨 Scope 操作
ScopeActivateCall
ScopeStopCall
ScopeDisposeCall
```

### 2.3 本地调用

同一个 Scope 内：

```text
EventCenter.Send
PostScheduler.Post
本地 Service 调用
本地 ECS 操作
```

不经过 ScopeEvent/ScopeCall。

---

## 3. 地址与 Endpoint

### 3.1 ScopeAddress

```csharp
public readonly struct ScopeAddress
{
    public int RuntimeId { get; }
    public int RuntimeGeneration { get; }
    public int ScopeId { get; }
}
```

`RuntimeGeneration` 防止旧 `ScopeRef` 把消息发送给重建后的新 Runtime。

### 3.2 ScopeEndpoint

```csharp
public readonly struct ScopeEndpoint
{
    public ScopeAddress Address { get; }

    internal IScopeEventWriter EventWriter { get; }
    internal IScopeCallWriter CallWriter { get; }
}
```

Endpoint 不得公开：

```text
ScopeRuntime
EventCenter
PostScheduler
TimeScheduler
EcsWorld
ServiceProvider
ActorWorld
```

### 3.3 ScopeRef

```csharp
public readonly struct ScopeRef<TScope>
{
    private readonly ScopeEndpoint _endpoint;
    private readonly ScopeRouteTable _routes;
}
```

旧 Endpoint 的 `RuntimeGeneration` 不匹配时，Post/Call 必须失败，不允许重定向到新的同 ID Scope。

---

## 4. ScopeEvent Envelope

```csharp
internal readonly struct ScopeEventEnvelope
{
    public ScopeAddress Origin { get; }
    public int RouteId { get; }
    public ScopeEventClass Class { get; }
    public PayloadHandle Payload { get; }
}
```

### 4.1 Event 分类

```csharp
internal enum ScopeEventClass : byte
{
    Business,
    Internal,
    Critical
}
```

含义：

- `Business`：普通业务事件。
- `Internal`：Actor 投影等框架内部消息。
- `Critical`：ScopeFaultEvent 等不能静默丢弃的消息。

分类只影响准入容量，不产生额外队列。

---

## 5. ScopeCall Envelope

```csharp
internal readonly struct ScopeCallEnvelope
{
    public ScopeCallEnvelopeKind Kind { get; }
    public ScopeCallClass Class { get; }

    public ScopeCallToken Token { get; }
    public ScopeAddress Origin { get; }

    public int RouteId { get; }
    public PayloadHandle Payload { get; }

    public ScopeCallResult Result { get; }
}
```

### 5.1 Envelope Kind

```csharp
internal enum ScopeCallEnvelopeKind : byte
{
    Request,
    Response
}
```

Request 和 Response 必须进入相同的 `ScopeCallInbox`。

### 5.2 Call 分类

```csharp
internal enum ScopeCallClass : byte
{
    BusinessRequest,
    Response,
    Control
}
```

控制 Call：

```text
ScopeActivateCall
ScopeStopCall
ScopeDisposeCall
```

仍然是普通 Call Envelope，只使用保留的内部 RouteId。

---

## 6. Call Token

```csharp
internal readonly struct ScopeCallToken
{
    public int RuntimeGeneration { get; }
    public int OriginScopeId { get; }
    public int Sequence { get; }
    public int Version { get; }
}
```

要求：

```text
在一个 RuntimeGeneration 内唯一
对象池复用时 Version 必须变化
Response 必须精确匹配 Token
过期 Response 不得完成新 Promise
```

---

## 7. ScopeCallRegistry

每个 Scope 本地持有：

```csharp
internal sealed class ScopeCallRegistry
{
    public ScopeCallToken Register(IScopeCallPromise promise);
    public bool TryComplete(in ScopeCallEnvelope response);
    public void FailAll(Exception reason);
}
```

约束：

1. 只由 Scope Owner Thread 修改，不加锁。
2. 外部 Scope 只能写入 Response Envelope。
3. 收到 Response 后，由调用方 Owner Thread完成 Promise。
4. Promise 使用普通 LBTaskSource，因此 continuation 仍由捕获的 `SynchronizationContext` 调度。
5. Scope Stop 时，尚未完成的 outgoing Call 必须全部进入 `ScopeStopped` 或 `Canceled` 终态。

这不是独立 Completion 通道，只是调用方 Scope 内的 pending-call 表。

---

## 8. 单物理队列的容量预留

### 8.1 问题

如果业务请求占满 CallInbox：

```text
Response 无法返回
StopCall 无法进入
DisposeCall 无法进入
```

因此必须在同一个物理 RingQueue 上实现分类准入。

### 8.2 CallInbox 配置

```csharp
public readonly struct ScopeCallInboxOptions
{
    public int Capacity { get; }
    public int ReservedForResponseAndControl { get; }
}
```

示例：

```text
总容量：1024
BusinessRequest 最大占用：896
Response + Control 保留：128
```

准入规则：

```text
BusinessRequest:
    Count < Capacity - ReservedForResponseAndControl

Response / Control:
    Count < Capacity
```

仍然只有一个 RingQueue 和一个 FIFO 顺序。

### 8.3 EventInbox 配置

```csharp
public readonly struct ScopeEventInboxOptions
{
    public int Capacity { get; }
    public int ReservedForInternal { get; }
    public int ReservedForCritical { get; }
}
```

普通业务事件不能消耗全部容量，必须为 Actor 内部消息和故障事件保留位置。

### 8.4 禁止方案

```text
再建一条 ControlQueue
再建一条 ResponseQueue
再建一条 FaultQueue
通过共享 volatile 状态绕过消息
```

---

## 9. MPSC Queue 接口

```csharp
internal interface IScopeInbox<T>
{
    ScopeEnqueueResult TryEnqueue(
        in T item,
        ScopeAdmissionClass admission);

    bool TryDequeue(out T item);

    void CloseBusinessAdmission();

    void CloseAllAdmission();
}
```

返回值：

```csharp
internal enum ScopeEnqueueResult
{
    Accepted,
    Full,
    BusinessClosed,
    Closed,
    StaleEndpoint
}
```

队列中的锁属于跨线程 MPSC 边界，不属于 Scope 本地资源锁。

---

## 10. 消息所有权

### 10.1 Accepted

入队成功后：

```text
Payload 所有权转移给目标 Inbox
Producer 不得再次访问或释放 Payload
Consumer Dispatch 完毕后释放
```

### 10.2 Rejected

入队失败后：

```text
Payload 所有权仍属于 Producer
Producer 必须释放或重试
```

### 10.3 Response

Call Handler 产生 Result 后：

```text
Response 入队成功：
    所有权转给 Origin CallInbox

Response 入队失败且 Origin 已关闭：
    释放 Result Payload
    目标不再保留 Promise 引用
```

### 10.4 Actor 批次

Actor 投影批次必须使用可释放的批量 Payload：

```text
CustomScope 创建批次
MainScope ScopeEventInbox 接受后获得所有权
MainScope Apply 到 ActorWorld
finally 释放池化数组
```

---

## 11. Dispatch 顺序

### 11.1 CallInbox

每次 Tick：

```text
1. 顺序 Dequeue。
2. Response → ScopeCallRegistry。
3. Control Request → 生命周期 Dispatcher。
4. Business Request → 生成式业务 Dispatcher。
```

队列保持 FIFO，不为了控制消息另建优先队列。

控制消息的最大等待量由有界容量保证。

### 11.2 EventInbox

```text
1. 顺序 Dequeue。
2. Internal/Critical → 内部 Dispatcher。
3. Business → 生成式业务 Dispatcher。
```

MainScope 必须在 `ActorWorld.Pump` 前至少 Drain 一次 Actor 内部 Event。

---

## 12. Call 终态

一个已被接受的 ScopeCall 必须且只能进入以下一种终态：

```csharp
internal enum ScopeCallTerminalState
{
    Succeeded,
    Faulted,
    Canceled,
    ScopeStopped
}
```

规则：

- Handler 正常返回 → `Succeeded`。
- Handler 抛异常 → `Faulted` Response。
- 请求尚未入队且调用方取消 → `Canceled`。
- 已接受请求在 StopPolicy.Drop 下被丢弃 → `ScopeStopped` Response。
- 目标已经停止或 Endpoint 过期 → 调用方本地立即 `ScopeStopped`，不创建悬挂 Promise。

第一版不实现远端执行中的强制取消。

---

## 13. 队列关闭

### Running

```text
接受 Business、Internal、Critical Event
接受 BusinessRequest、Response、Control Call
```

### Stopping

```text
拒绝新的 Business Event
拒绝新的 BusinessRequest
继续接受 Response、Control、Critical
根据 StopPolicy 处理已接受业务消息
```

### Disposing

```text
仅接受 Response 和 Dispose 控制流程所需消息
```

### Exited

```text
全部关闭
```

队列关闭由 Owner Thread 在处理控制 Call 时执行。

---

## 14. 生成器职责

生成器负责：

```text
ScopeEvent 类型 → RouteId
ScopeCall 类型 → RouteId
Handler 方法 → ServiceSlot + Bridge
ScopeRef<T>.Post 强类型重载
ScopeRef<T>.Call 强类型重载
Route 签名冲突诊断
Call Result 类型匹配诊断
```

运行时 Dispatcher 不做反射。

---

## 15. faster 复用清单

### 直接移植

```text
LayerBase/DataStruct/IBoundedQueue.cs
LayerBase/DataStruct/LocalRingQueue.cs
LayerBase/DataStruct/LockedBoundedRingQueue.cs
LayerBase/Scope/Queue/ClosableLockedRingQueue.cs
LayerBase.Generator/.../ScopeRefPostGenerator.cs
LayerBase.Generator/.../ScopeRefCallGenerator.cs
ScopePost/Call Handler bridge 的生成逻辑
```

### 修改移植

```text
ScopeMessages.cs
    → 分离 EventEnvelope / CallEnvelope，加入 Request/Response Kind

ScopeRouteTable.cs
    → 只保存 Endpoint 和 Route，不保存 ScopeRuntime

ScopeRef<TScope>
    → 保存 Endpoint + RuntimeGeneration

ScopePromise.cs
    → 使用 Origin ScopeCallRegistry，不使用 CompletionPort

ScopePostDispatchGenerator.cs
ScopeCallDispatchGenerator.cs
    → 删除 AssemblyModule Dispatcher 依赖
```

### 禁止移植

```text
ScopeCompletionPort
独立 Response/Control/Actor/Fault Queue
ScopeRef 直接找到 ScopeRuntime
ScopeRouteTable 返回 ScopeRuntime
```

---

## 16. 实施顺序

1. 移植 RingQueue 与 Closable Queue。
2. 定义 ScopeAddress、Endpoint、RuntimeGeneration。
3. 定义 Event/Call Envelope。
4. 实现同物理队列分类准入。
5. 实现 ScopeCallToken 和 ScopeCallRegistry。
6. 移植 ScopeRef Post/Call 生成器。
7. 移植 Handler Dispatch 生成器并删除 Module 依赖。
8. 接入控制 Call。
9. 接入 Actor/Fault 内部 Event。
10. 删除所有旁路通道。

---

## 17. 必须测试

```text
Business_request_cannot_consume_reserved_call_capacity
Response_can_enter_when_business_quota_is_full
Control_call_can_enter_when_business_quota_is_full
Event_internal_capacity_is_reserved
Critical_event_capacity_is_reserved
Request_and_response_use_same_physical_queue
Accepted_call_reaches_exactly_one_terminal_state
Stale_response_cannot_complete_reused_promise
Old_scope_ref_fails_after_runtime_generation_changes
Rejected_payload_remains_owned_by_producer
Accepted_payload_is_released_by_consumer
Call_response_completes_on_origin_scope_thread
No_completion_or_control_queue_exists
```

---

## 18. 验收否决项

```text
Response 使用独立 Completion Queue
Stop/Dispose 使用独立 Control Queue
Actor 投影使用独立 Projection Queue
业务请求能够耗尽控制和响应保留容量
ScopeRef 暴露 ScopeRuntime
过期 Endpoint 能向新 Runtime 投递
入队失败时 Payload 所有权不明确
```
