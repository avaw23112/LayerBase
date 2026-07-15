# 27 Scope Diagnostics、队列、资源、Actor 与 Tool 可观测性

> **最高原则：** 延续 `master` 已有 `LayerEventInfo`、`OnLayerEventInfo`、Topology/Policy Markdown和 `faster` 的 Tool Diagnostics风格；只增加 Scope维度和安全 Snapshot，不另建遥测框架。  
> **master 基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **faster 复用基线：** `8898a90bcb3e00a370e47f8b39f6eff32fa98980`  
> **依赖阶段：** `15_scope_fault_propagation.md`、`21_main_scope_actor_world_mailbox_lifecycle_migration.md`、`23_scope_local_ecs_scheduler_query_batch_blueprint_migration.md`、`26_scope_build_activate_prewarm_freeze_total_order.md`。  
> **文档性质：** 低频调试与测试可观测性。Diagnostics不得改变业务时序，不得成为新的跨 Scope消息通道。

---

## 0. master / faster 已有能力

master已有：

```text
LayerRuntime.OnLayerEventInfo

LayerRuntime.ReportInfo

ReportLayerEventError

ReportWarning

GetTopologySummary

GetTopologyMarkdown

GetPolicyMarkdown

EventDiagnosticSymbols

LayerEventInfo / LayerEventInfoType
```

faster可复用：

```text
LayerToolDiagnosticsReport

RuntimeSafetyRegressionTests

PayloadLifecycleTests

各 Benchmark Counter与统计方式

Worker / ECS测试中的队列和状态读取方法
```

这些能力不能被新的 Diagnostics API替换或删除。

---

## 1. 本阶段需要回答的问题

```text
Runtime当前处于什么状态？

哪个 Scope没有推进？

哪个 Scope Tick超预算？

EventInbox / CallInbox是否接近满载？

Post / Timer / Delay积压多少？

ECS是否处于 Query或 CommandBuffer SafePoint？

WorkerJob还有多少 Pending / Running？

哪个 Scope发生 Fault？

MainActorRuntime Mail / Call / Lifecycle是否积压？

每个 Scope的 Tool缓存创建了多少实例？

FullSnap是否正在等待 SafePoint？

是否存在 Payload或 Promise未归零？
```

---

## 2. 不建立平行 Diagnostics总线

禁止新增：

```text
DiagnosticsQueue

MetricsThread

GlobalDiagnosticsHub

静态 Fault Callback

每条业务事件复制一份 Trace Event

Scope之间专用 Metrics通道
```

跨 Scope读取 Snapshot继续使用：

```text
标准 ScopeCall Control Route
```

实时 Fault继续使用15号：

```text
ScopeEvent / ScopeCall Response
```

Diagnostics只是读取和报告现有状态。

---

## 3. 对外 API

保留 master：

```csharp
runtime.OnLayerEventInfo +=
    OnLayerEventInfo;

string topology =
    runtime.GetTopologyMarkdown();

string policies =
    runtime.GetPolicyMarkdown();
```

新增最小 Snapshot API：

```csharp
RuntimeDiagnosticsSnapshot snapshot =
    runtime.CaptureDiagnostics();
```

只在没有独立 WorkerScope时允许同步 Capture。

存在 WorkerScope时：

```csharp
RuntimeDiagnosticsSnapshot snapshot =
    await runtime.CaptureDiagnosticsAsync(
        cancellationToken);
```

不得同步阻塞等待 WorkerScope。

不强制新增：

```text
Runtime.Diagnostics事件中心

IObservable

OpenTelemetry

Prometheus
```

---

## 4. RuntimeDiagnosticsSnapshot

```csharp
public sealed class
    RuntimeDiagnosticsSnapshot
{
    public int RuntimeId {
        get;
    }

    public int RuntimeGeneration {
        get;
    }

    public RuntimeState State {
        get;
    }

    public long Timestamp {
        get;
    }

    public ScopeDiagnosticsSnapshot[]
        Scopes {
        get;
    }

    public MainActorDiagnosticsSnapshot
        MainActor {
        get;
    }

    public PayloadDiagnosticsSnapshot
        Payloads {
        get;
    }
}
```

Snapshot是不可变数据。

不得暴露：

```text
ScopeRuntime

Queue实例

Service对象

Tool实例

Actor对象

World
```

---

## 5. ScopeDiagnosticsSnapshot

只包含已经存在或可低成本读取的状态：

```csharp
public readonly struct
    ScopeDiagnosticsSnapshot
{
    public readonly int ScopeId;
    public readonly string ScopeName;
    public readonly ScopeRuntimeState State;

    public readonly int OwnerThreadId;

    public readonly long TickCount;
    public readonly long LastTickDurationTicks;
    public readonly long MaxTickDurationTicks;

    public readonly int EventInboxCount;
    public readonly int EventInboxCapacity;
    public readonly long EventInboxRejected;

    public readonly int CallInboxCount;
    public readonly int CallInboxCapacity;
    public readonly long CallInboxRejected;

    public readonly int PostPending;
    public readonly int TimerPending;
    public readonly int DelayPending;
    public readonly int ContinuationPending;

    public readonly int WorkerJobsPending;
    public readonly int WorkerJobsRunning;

    public readonly EcsDiagnosticsSnapshot Ecs;
    public readonly ToolDiagnosticsSnapshot Tools;
    public readonly SnapDiagnosticsSnapshot Snap;

    public readonly long FaultCount;
}
```

字段只有在对应子系统已有可读状态时才加入。

不得为了填满 Snapshot：

```text
遍历所有 Entity

遍历所有 Timer对象

遍历所有 Tool实例

锁住整个 Scope
```

---

## 6. Ecs Diagnostics

23号应暴露固定值 Snapshot：

```csharp
public readonly struct
    EcsDiagnosticsSnapshot
{
    public readonly int EntityCount;
    public readonly bool QueryBatchEnabled;
    public readonly int LastQueryBatchCount;
    public readonly int LastQueryEntityCount;
    public readonly int CommandBufferSize;
    public readonly long StructuralPlaybackCount;
}
```

规则：

```text
EntityCount使用 World已有 Count或维护计数。

不为 Diagnostics遍历 World。

CommandBuffer读取现有 Size。

QueryBatch计数由 OwnerThread更新整数。
```

不恢复 faster旧：

```text
EcsResultQueue Depth

MainThread Drain Count
```

因为新架构没有该专用通道。

---

## 7. Tool Diagnostics

14号每 Scope拥有 `ScopeToolRegistry`。

复用 `LayerToolDiagnosticsReport` 的：

```text
Descriptor

Contract

Implementation

Key

Cache标记

创建状态

失败信息
```

Scope Snapshot只放计数：

```text
RegisteredCount

CachedCount

CreatedCount

CreateFailureCount
```

详细 Tool列表通过低频：

```csharp
ScopeToolDiagnosticsReport
    GetToolDiagnostics();
```

在 OwnerScope Snapshot Handler中生成。

禁止跨线程读取 Cache数组。

---

## 8. MainActor Diagnostics

ActorWorld只属于 MainActorRuntime，因此不伪装成每 Scope Actor指标。

```csharp
public readonly struct
    MainActorDiagnosticsSnapshot
{
    public readonly MainActorRuntimeState State;
    public readonly int ActorCount;
    public readonly int PendingMailCount;
    public readonly int PendingCallCount;
    public readonly int PendingLifecycleCount;
    public readonly int PendingDestroyCount;
    public readonly long PumpCount;
    public readonly long LastPumpDurationTicks;
    public readonly long FaultCount;
}
```

字段必须来自 ActorWorld / MainActorRuntime已有计数或低成本维护值。

禁止：

```text
遍历所有 Actor Behaviour生成 Snapshot

把 Actor引用放进报告

让 CustomScope直接读取 ActorWorld
```

---

## 9. Queue Diagnostics

MPSC Queue自身维护：

```text
Count

Capacity

Accepted

Rejected

HighWatermark
```

Producer跨线程只做已有原子计数。

不得在 Submit热路径：

```text
格式化字符串

解析 Type名

创建 Diagnostic对象

调用用户回调
```

OwnerThread在 Snapshot时复制值。

---

## 10. Tick与慢处理采样

默认：

```text
Diagnostics Disabled
```

关闭时：

```text
不得调用 Stopwatch.GetTimestamp用于每个 Handler。

不得增加 Allocation。

只保留已有必要 Queue Count。
```

可选 Debug配置：

```csharp
public readonly struct
    DiagnosticsOptions
{
    public bool Enabled {
        get;
    }

    public bool SampleTickDuration {
        get;
    }

    public bool SampleSlowHandlers {
        get;
    }

    public long SlowHandlerThresholdTicks {
        get;
    }
}
```

Slow Handler采样只记录：

```text
ScopeId

HandlerSlot

ElapsedTicks
```

Type名和源码在读取报告时从 Frozen Descriptor解析。

不得在热路径构造字符串。

---

## 11. WorkerScope Snapshot

MainScope不能直接读取 WorkerScope可变字段。

流程：

```text
Runtime Capture Coordinator
    → 标准 ScopeCall<CaptureScopeDiagnostics>
    → WorkerScope OwnerThread复制固定 Snapshot
    → 标准 ScopeCall Response
    → Coordinator合并
```

该 Control Request使用 CallInbox保留容量。

禁止：

```text
Diagnostics MPSC

MainScope锁 Worker字段

Volatile读取整个复杂对象图

Thread.Suspend
```

---

## 12. 同步与异步 Capture

### 同步

允许：

```text
只有 MainScope和 InlineScope。

当前线程是 MainScope OwnerThread。
```

### 异步

WorkerScope存在时：

```text
CaptureDiagnosticsAsync
```

Coordinator可以并发发送 Control Call，但合并顺序固定：

```text
ScopeId升序
```

取消时：

```text
终结 Pending ScopeCall。

不影响业务 Runtime状态。
```

---

## 13. Topology / Policy Dump迁移

master现有 Markdown继续保留，但数据来源改为：

```text
Frozen RuntimeCompositionPlan

Frozen Event Policy Plan

Frozen LocalCall / ScopeRoute Plan

LayerBuildPlan / ScopeExecutionPlan
```

不再扫描 Running对象和 Handler集合。

新增 Section：

```text
Scopes

Layer × Scope Object Ranges

LocalCall Routes

ScopeEvent Routes

ScopeCall Routes

Scope Tool Ranges

Snap Node Ranges
```

原有 Layer、Event、Call、Provide等内容继续保留。

---

## 14. LayerEventInfo 保留

错误和 Warning继续使用：

```text
LayerEventInfo

OnLayerEventInfo

ReportInfo

ReportWarning
```

Scope迁移后增加：

```text
ScopeId
```

如果直接修改结构会破坏现有构造调用，可以：

```text
增加兼容构造函数

默认 ScopeId = MainScope或 -1
```

不得删除现有事件回调。

16号要求的多 Runtime隔离仍成立：

```text
回调属于 Runtime实例。

Dispose后清空订阅。

不依赖静态 Global Diagnostics Hub。
```

---

## 15. Payload与Promise诊断

只读取池已有统计：

```text
Rented

Returned

Outstanding

PeakOutstanding
```

ScopeCall：

```text
PendingPromiseCount

Completed

Cancelled

Faulted

StaleTokenRejected
```

禁止为了统计保存每个 Payload的完整历史。

Debug泄漏测试可以在 Dispose后断言：

```text
Outstanding == 0
```

---

## 16. Snap Diagnostics

```csharp
public readonly struct
    SnapDiagnosticsSnapshot
{
    public readonly ScopeSafePointState State;
    public readonly int NodeCount;
    public readonly long SerializeCount;
    public readonly long DeserializeCount;
    public readonly long FailureCount;
}
```

不包含 SnapDocument内容。

Snapshot期间若正在 FullSnap：

```text
Diagnostics Control Call仍可以返回最小状态，
但不得打断 SafePoint。
```

---

## 17. faster / master 复用

### master原样保留

```text
LayerEventInfo

OnLayerEventInfo

ReportInfo / ReportWarning

EventDiagnosticSymbols

GetTopologySummary

GetTopologyMarkdown

GetPolicyMarkdown
```

### faster修改复用

```text
LayerToolDiagnosticsReport风格

RuntimeSafetyRegressionTests中的状态断言

PayloadLifecycleTests的池统计

ECS/Worker测试中的 Count / State测试入口

Benchmark Counter写法
```

### 禁止新增

```text
Metrics Thread

全局静态 Callback

Diagnostics专用 Queue

每事件 Trace对象

运行期反射枚举类型

跨 Scope直接读对象
```

---

## 18. 需要修改的代码位置

```text
LayerBase/Application/
    LayerRuntime.Diagnostics.cs
    LayerRuntime现有 Markdown方法

LayerBase/Scope/
    ScopeRuntime.Diagnostics.cs
    Capture Diagnostics Control Handler

LayerBase/Event/
    Event / Call Inbox Counter
    PostScheduler Counter
    Timer / Delay Counter

LayerBase/ECS/
    ScopeEcsScheduler Diagnostics

LayerBase/Actor/
    MainActorRuntime Diagnostics

LayerBase/Tooling/
    ScopeToolRegistry Diagnostics

LayerBase/Snap/
    ScopeSafePoint / Snap Counter

LayerBase.Test/
    ScopeDiagnosticsTests
    DiagnosticsDisabledBenchmarks
```

---

## 19. Agent 执行任务

```text
1. 保留 master LayerEventInfo和 Markdown API。
2. 为 Frozen Plan增加 Scope维度的描述数据。
3. 为每 Scope定义不可变 Snapshot。
4. Queue只维护整数和 HighWatermark。
5. WorkerScope Snapshot通过标准 ScopeCall。
6. MainActor指标只来自 MainActorRuntime。
7. Tool指标从本 Scope Tool Registry读取。
8. ECS指标不遍历 World。
9. Diagnostics关闭时不做 Handler计时。
10. 开启 Slow Handler时只记录 Slot和 Ticks。
11. Payload / Promise暴露池统计。
12. 同步 Capture仅支持无 WorkerScope。
13. WorkerScope场景使用 Async Capture。
14. Dispose后清空 Runtime实例回调。
15. 增加 Disabled开销 Benchmark。
```

---

## 20. 必须测试

```text
Master_layer_event_info_callback_remains_valid

Topology_markdown_uses_frozen_plan

Policy_markdown_remains_available

Each_scope_snapshot_reports_correct_scope_id

Worker_snapshot_runs_on_worker_owner_thread

Main_scope_does_not_read_worker_runtime_directly

Diagnostics_uses_standard_scope_call

No_diagnostics_queue_exists

Event_inbox_high_watermark_is_reported

Call_inbox_rejected_count_is_reported

Ecs_snapshot_does_not_enumerate_world

Tool_snapshot_only_reports_owner_scope_registry

Actor_snapshot_only_reads_main_actor_runtime

Payload_outstanding_returns_to_zero_after_dispose

Promise_pending_returns_to_zero_after_stop

Diagnostics_disabled_adds_no_allocation

Diagnostics_disabled_does_not_time_each_handler

Slow_handler_record_resolves_slot_on_read

Capture_cancel_does_not_change_runtime_state

Runtime_dispose_clears_event_subscribers
```

---

## 21. 验收否决项

出现任意一项，任务不通过：

```text
新增 Diagnostics专用跨 Scope队列

MainScope直接枚举 Worker对象

Snapshot暴露 ScopeRuntime / World / Actor / Tool实例

热路径格式化类型名或字符串

Diagnostics关闭仍对每 Handler调用 Stopwatch

为了 EntityCount遍历全部 Entity

Actor指标被复制到每个 Scope

删除 master OnLayerEventInfo或 Markdown API

静态全局回调持有 Runtime

Diagnostics改变 Handler顺序或 Tick顺序

Capture同步阻塞等待 WorkerScope
```

---

## 22. 本阶段最终结果

```text
现有 master Diagnostics API继续可用。

每个 Scope可以安全报告自己的：
    状态
    队列
    Post/Timer/Delay
    ECS
    WorkerJob
    Tool
    Snap
    Fault

Actor指标由 MainActorRuntime单独报告。

Worker数据通过标准 ScopeCall复制 Snapshot。

Diagnostics关闭时不影响热路径，
开启时也不改变业务时序。
```
