# 12 WorkerEventJob 结果回流与 SubscribeParallel 直接删除

> **强制执行规范：** 本文必须遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`；冲突时以 00、01 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `02_scope_runtime_resources_revised.md`、`03_scope_event_call_protocol.md`、`04_scope_lifecycle_control_protocol_revised.md`、`07_lbtask_synchronization_context.md`、`10_public_api_scope_routing.md`、`18_scope_post_scheduler_timer_delay_migration.md`  
> **文档性质：** 独立阶段任务。本文只保留显式 WorkerEventJob，并完整删除 SubscribeParallel；不重新设计 Scope、EventCenter、PostScheduler、ScopeEvent 或 WorkerScope。

---

## 0. 本阶段核心目的

并行职责固定为：

```text
长期状态、独立线程、独立资源和独立 Tick：
    WorkerScope

短生命周期、纯 CPU、显式输入和事件结果：
    WorkerEventJob

Scope 间或 Worker Thread → Scope：
    ScopeEvent

同 Scope Owner Thread中的结果预算和事件策略：
    本地 PostScheduler
```

WorkerEventJob 的最终管线：

```text
OriginScope Owner Thread
    → WorkerJobAccessor.Run
    → Runtime WorkerJobScheduler
    → Worker Thread纯计算
    → OriginScope Endpoint.TryPost(
        WorkerEventJobResultScopeEvent<TEvent>)
    → OriginScope ScopeEventInbox
    → OriginScope Owner Thread
    → 本地 PostScheduler.TryPost(
        TEvent,
        ResultPostPolicy)
    → OriginScope EventCenter
```

关键结论：

```text
Worker Thread不直接写 PostScheduler。

PostScheduler不增加 CrossThreadIngress。

Worker Result不进入全局 ResultQueue。

Worker Result固定回到提交它的 OriginScope。

SubscribeParallel直接删除，不保留兼容壳。
```

---

## 1. 最终公有 API

### 1.1 WorkerEventJob

```csharp
public interface IWorkerEventJob<
    TInput,
    TEvent>
    where TInput : struct
    where TEvent : struct
{
    TEvent Execute(
        in TInput input,
        in WorkerJobContext context);
}
```

```csharp
public readonly struct WorkerJobAccessor
{
    public WorkerHandle Run<
        TJob,
        TInput,
        TEvent>(
        in TJob job,
        in TInput input,
        WorkerEventJobOptions options = default,
        CancellationToken cancellationToken = default)
        where TJob :
            struct,
            IWorkerEventJob<
                TInput,
                TEvent>
        where TInput : struct
        where TEvent : struct;
}
```

调用：

```csharp
this.WorkerJobs().Run<
    VisibilityJob,
    VisibilityInput,
    VisibilityCalculatedEvent>(
        new VisibilityJob(),
        in input,
        WorkerEventJobOptions.Latest);
```

接收：

```csharp
[Subscribe]
private void OnVisibilityCalculated(
    in VisibilityCalculatedEvent value)
{
    ApplyVisibility(in value);
}
```

### 1.2 不提供的 API

```text
ScopeRef.WorkerJobs
ScopeRef.RunJob
RunJobOn<TScope>
WorkerJob返回 LBTask<TResult>
WorkerJob直接取得 ScopeRuntime
```

目标 Scope需要提交 Job时：

```text
先通过 ScopeEvent / ScopeCall进入目标 Scope
    → 目标 Scope Handler本地调用 WorkerJobs()
```

---

## 2. WorkerEventJob 适用范围

适合：

```text
短生命周期
纯 CPU
显式值输入
显式值结果事件
允许异步完成
结果最终回 OriginScope
```

不适合：

```text
长期持有状态
独立 Tick
IO
等待 ScopeCall
操作引擎主线程对象
访问 Scope Service / Context
访问 EcsWorld / ActorWorld
```

后者应使用：

```text
WorkerScope
或 Scope 内异步业务流程
```

---

## 3. WorkerJobContext

```csharp
public readonly struct WorkerJobContext
{
    public int WorkerIndex {
        get;
    }

    public bool IsCancellationRequested {
        get;
    }

    public CancellationToken CancellationToken {
        get;
    }
}
```

禁止暴露：

```text
LayerRuntime
ScopeRuntime
ScopeEndpoint
ScopeRef
EventCenter
PostScheduler
Timer
ServiceProvider
EcsWorld
ActorWorld
LayerToolRegistry
```

Job只能返回 `TEvent`，不能自行投递结果或修改 Scope资源。

---

## 4. 输入与结果所有权

```text
TJob   : struct
TInput : struct
TEvent : struct
```

规则：

```text
TJob和 TInput提交时复制进 JobItem。

值类型中的引用字段不会自动深复制。

Worker不得修改 OriginScope可变对象。

允许不可变快照、只读缓冲区和用户管理的稳定 Handle。

外部缓冲区必须保持有效直到 Job终态。

TEvent不得携带可由 Worker和 Scope同时修改的对象引用。
```

框架不扫描对象图，也不增加主观的“线程安全类型”运行检查。

---

## 5. OriginScope 捕获

提交时捕获：

```csharp
internal readonly struct
    WorkerJobOrigin
{
    internal readonly int RuntimeId;
    internal readonly int RuntimeGeneration;
    internal readonly int OriginScopeId;

    internal readonly ScopeEndpoint
        OriginEndpoint;

    internal readonly int ResultRouteId;
    internal readonly int FailureRouteId;
}
```

不得捕获：

```text
ScopeRuntime
ScopeLocalAccess
PostScheduler
EventCenter
EcsWorld
LayerProvider
Service / Context
```

`ScopeEndpoint` 是 Worker Thread可持有的唯一目标能力。

---

## 6. Result ScopeEvent

Worker Thread完成计算后投递内部 ScopeEvent：

```csharp
internal readonly struct
    WorkerEventJobResultScopeEvent<TEvent>
    where TEvent : struct
{
    internal readonly WorkerHandle Handle;
    internal readonly TEvent Value;

    internal readonly EventPostPolicy
        PostPolicy;
}
```

如果 `TEvent` 或 Envelope较大，应复用 03 号通用 Payload Lease，不为 WorkerJob建立专用 Payload Store。

投递：

```csharp
bool accepted =
    origin.OriginEndpoint.TryPost(
        in resultScopeEvent);
```

`accepted` 只表示：

```text
OriginScope EventInbox接受了 Result。
```

不表示业务订阅者已经消费。

Worker Thread不等待 OriginScope确认，也不创建 ScopeCall Response。

---

## 7. OriginScope Result Dispatcher

内部 Result Route到达 OriginScope Owner Thread后：

```csharp
private void Dispatch<TEvent>(
    in WorkerEventJobResultScopeEvent<TEvent> value)
    where TEvent : struct
{
    _scope.PostScheduler.TryPost(
        in value.Value,
        in value.PostPolicy);
}
```

实际入口必须复用 `faster` 本地 PostScheduler API和 EventPostPolicy。

这一步保留原 WorkerEventJob结果语义：

```text
All
Latest
Coalesced
Post Budget
EventCenter正常派发
```

同时消除跨线程 Post入口。

如果本地 PostScheduler拒绝：

```text
在 OriginScope Owner Thread
通过既有 Fault / Diagnostics路径记录 ResultPostRejected。

释放 Result Payload。

不得新建 WorkerResultFailureQueue。
```

第一阶段不要求 Worker Thread获得该二次拒绝的确认。

---

## 8. Failure ScopeEvent

执行异常、取消和投递失败分为：

```csharp
public enum WorkerJobFailureKind
{
    ExecutionFault,
    Cancelled,
    ResultScopeEventRejected,
    OriginScopeStopped
}
```

Worker Thread可投递：

```csharp
internal readonly struct
    WorkerEventJobFailedScopeEvent
{
    internal readonly WorkerHandle Handle;
    internal readonly WorkerJobFailureKind Kind;
    internal readonly WorkerJobExceptionInfo Error;
}
```

不要跨线程携带不可控的完整 `Exception` 对象进入长期队列；优先复用 `faster` 现有异常快照或受控 ExceptionInfo。

如果 OriginScope已经关闭，Failure Event也无法投递：

```text
更新 WorkerHandle终态
释放 JobItem和 Payload
写 Runtime Diagnostics
不建立额外错误队列
```

---

## 9. WorkerHandle 与状态

优先复用 `faster` 的 ID + Version：

```csharp
public readonly struct WorkerHandle :
    IEquatable<WorkerHandle>
{
    public int Id {
        get;
    }

    public int Version {
        get;
    }
}
```

状态：

```text
Pending
Running
Completed
Failed
Cancelled
```

定义：

```text
Completed：
    Job执行成功，
    Result ScopeEvent已被 OriginScope EventInbox接受。

Failed：
    Execute抛出异常，
    或 Result无法进入 OriginScope EventInbox。

Cancelled：
    执行前取消，
    或 Execute协作取消。
```

一个 Accepted Job必须且只能进入一个终态。

Handle完成不表示 Result Event已经由业务 Handler消费。

---

## 10. WorkerJobScheduler

```csharp
internal sealed class
    WorkerJobScheduler :
    IDisposable
{
    internal WorkerHandle RunEventJob<
        TJob,
        TInput,
        TEvent>(
        in WorkerJobOrigin origin,
        in TJob job,
        in TInput input,
        in WorkerEventJobOptions options,
        CancellationToken cancellationToken)
        where TJob :
            struct,
            IWorkerEventJob<
                TInput,
                TEvent>
        where TInput : struct
        where TEvent : struct;

    internal WorkerState GetState(
        WorkerHandle handle);

    internal bool Cancel(
        WorkerHandle handle);

    internal void BeginStop();
    internal void Join();
}
```

Scheduler属于 Runtime共享计算设施。

负责：

```text
有界 Job Queue
Worker Thread
JobItem池
Handle Slot
取消
异常捕获
Result ScopeEvent提交
ScopeJobGroup计数
Runtime Stop / Join
```

不负责：

```text
Event订阅
Post策略实现
Scope生命周期决策
DI
ECS
ActorWorld
```

---

## 11. WorkerJobAccessor

```csharp
public readonly struct
    WorkerJobAccessor
{
    private readonly WorkerJobScheduler
        _scheduler;

    private readonly ScopeJobGroup
        _group;

    private readonly WorkerJobOrigin
        _origin;

    private readonly int _ownerScopeId;
}
```

提交前验证：

```text
当前 ScopeExecution.ScopeId == OwnerScopeId

ScopeJobGroup仍接受任务

RuntimeGeneration有效

WorkerJobScheduler处于 Running
```

Accessor只能在 OwnerScope执行上下文使用。

---

## 12. ScopeJobGroup 与 Stop

每个 Scope有一个 Worker Job归属组，但 Worker线程和 Scheduler由 Runtime共享。

Scope Stop：

```text
1. 关闭该 Scope的新 Job提交。
2. 取消仍 Pending的 Job。
3. 对 Running Job发出协作取消。
4. 保持 OriginScope EventInbox接收已接受 Result。
5. 等待该 Scope JobGroup归零。
6. Drain已进入 EventInbox的 Result / Failure。
7. 继续 RuntimeStop。
```

不得在 JobGroup归零前关闭 OriginScope EventInbox的 Internal/Critical准入。

Drop StopPolicy也必须正确终结已 Accepted Job和 Payload。

---

## 13. SubscribeParallel 直接删除

必须删除：

```text
SubscribeParallelAttribute
Layer.SubscribeParallel
IService.SubscribeParallel
ILayerContext.SubscribeParallel
LayerEventStream.HandleParallel
UnsubscribeParallel
ParallelHandlerKind
ParallelHandlerEntry
ParallelSubscriptionQueue
EventCenter.SubscribeParallel
EventCenter.UnsubscribeParallel
IEventBucketNonGeneric.AddParallel
IEventBucketNonGeneric.RemoveParallel
HandlerBucket.MasterParallel
相关 Generator / Analyzer / 示例 / 测试 / Benchmark
```

不保留：

```text
Obsolete Attribute
error:true Stub
兼容 Extension
空实现
旧生成代码入口
```

遇到旧调用直接编译错误，由迁移说明指导改为：

```text
Handler在 OwnerScope接收事件
    → 显式构造 Input
    → this.WorkerJobs().Run(...)
    → Result Event回 OriginScope
```

---

## 14. 与 SubscribeAsync 的关系

`SubscribeAsync` 保留。

它用于：

```text
Delay
IO
ScopeCall
其他 LBTask异步流程
```

WorkerEventJob用于：

```text
纯 CPU计算
事件结果
不返回 LBTask<TResult>
```

不得为了删除 SubscribeParallel而删除或改写 SubscribeAsync。

---

## 15. faster 分支复用

### 15.1 直接复用

```text
WorkerHandle Id + Version
WorkerState Slot
固定 Worker Thread / Signal / Join
有界 Job Queue
JobItem池
协作取消
ScopeJobGroup或同类归属计数
原 WorkerEventJob接口语义
Worker性能测试
```

### 15.2 修改后复用

```text
原 Worker Result路径：
    目标改为 OriginScope Endpoint。

原 Worker Event Result：
    封装为内部 Result ScopeEvent。

原 EventPostPolicy：
    在 OriginScope Owner Thread交给本地 PostScheduler。

原 Failure Event：
    改为 OriginScope Failure ScopeEvent。
```

### 15.3 禁止移植

```text
ScopePostEndpoint
PostScheduler CrossThreadIngress
PostFromAnyThread
全局 Worker EventQueue
WorkerResultQueue
Task + Event双完成
QueueFull时 inline执行
ThreadPool fallback
Worker Thread直接 EventCenter.Send
```

---

## 16. 需要修改的代码位置

优先检查：

```text
LayerBase/Parallel/
LayerBase/Worker/
LayerBase/Event/
LayerBase/Scope/
LayerBase/Post/
LayerBase/Layer/Layer.cs
LayerBase/DI/ServiceExtensions.cs
LayerBase.Generator/
LayerBase.Test/
LayerBase.BenchMark/
```

实际路径以 `faster` 为准。

Agent先搜索 `SubscribeParallel` 和 WorkerEventJob全部引用，再分为：

```text
直接删除
结果路由修改
测试迁移
```

---

## 17. Agent 执行任务

```text
1. 记录 faster WorkerEventJob和 SubscribeParallel代码基线。
2. 删除 SubscribeParallel全部公开和内部实现。
3. 删除所有 Obsolete或兼容 Stub。
4. 保留 WorkerEventJob显式 Input/Event API。
5. Job提交捕获 OriginScope Endpoint，不捕获 PostScheduler。
6. 定义或生成 Result / Failure内部 ScopeEvent Route。
7. Worker Thread只写 OriginScope EventInbox。
8. OriginScope Dispatcher在 Owner Thread调用本地 PostScheduler。
9. 保留 All / Latest / Coalesced结果策略。
10. 删除 PostScheduler CrossThreadIngress。
11. WorkerHandle完成定义改为 Result Event被 EventInbox接受。
12. Stop等待 ScopeJobGroup归零并 Drain结果。
13. 删除 Worker ResultQueue和所有 fallback队列。
14. 保留 Worker线程、JobItem池、Handle Slot和取消算法。
15. 更新 API示例、Analyzer、测试和 Benchmark。
```

---

## 18. 必须测试

```text
Subscribe_parallel_api_does_not_exist

Subscribe_parallel_attribute_does_not_exist

Worker_job_executes_on_worker_thread

Worker_job_cannot_access_scope_local_resources

Worker_job_captures_origin_scope_endpoint

Worker_result_enters_origin_scope_event_inbox

Worker_result_never_enters_other_scope

Worker_result_is_posted_on_origin_owner_thread

Worker_result_preserves_all_policy

Worker_result_preserves_latest_policy

Worker_result_preserves_coalesced_policy

Post_scheduler_has_no_cross_thread_ingress

Worker_thread_never_calls_event_center

Worker_thread_never_calls_post_scheduler

Queue_full_does_not_inline_execute_job

Accepted_job_reaches_exactly_one_terminal_state

Scope_stop_closes_new_job_submission

Scope_stop_waits_job_group

Result_payload_is_released_exactly_once

Different_scopes_share_scheduler_but_not_result_destination

Steady_state_submit_is_zero_allocation_after_pool_warmup
```

---

## 19. 验收否决项

出现以下任意一项，任务不通过：

```text
SubscribeParallel仍有任何 API或 Stub

Worker Result直接写 PostScheduler

PostScheduler存在 CrossThreadIngress

Worker Result进入全局 ResultQueue

Worker Result固定回 MainScope而非 OriginScope

Worker Thread调用 EventCenter或业务 Handler

Queue Full时 inline或 ThreadPool fallback

WorkerJobContext暴露 ScopeRuntime / DI / ECS / ActorWorld

Scope Stop在 JobGroup终结前关闭 Result入口

为了本任务重写 EventCenter、ScopeEvent或 PostScheduler算法
```

---

## 20. 本阶段不修改的内容

本文不修改：

```text
WorkerScope
EventCenter派发
PostScheduler本地策略
ScopeEvent Envelope底层
LBTask SynchronizationContext
DI
ECS
ActorWorld
```

本文只保证：

```text
WorkerEventJob是显式纯计算能力。

Result通过 OriginScope ScopeEvent回流。

原 Post策略只在 OriginScope Owner Thread恢复。

SubscribeParallel彻底删除。
```
