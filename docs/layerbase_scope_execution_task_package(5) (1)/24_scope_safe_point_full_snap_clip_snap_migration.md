# 24 Scope SafePoint、FullSnap 与 ClipSnap 迁移

> **最高原则：** 以 `master` 已存在的 FullSnap、ClipSnap、Generator、SnapDocument、Reader/Writer和 `SnapTests` 为功能基线；只增加多 Scope 一致性安全点与 OwnerThread执行。  
> **master 基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **faster 复用基线：** `8898a90bcb3e00a370e47f8b39f6eff32fa98980`  
> **依赖阶段：** `03_scope_event_call_protocol.md`、`04_scope_lifecycle_control_protocol.md`、`23_scope_local_ecs_scheduler_query_batch_blueprint_migration.md`。  
> **文档性质：** Snap 资源归属与一致性协调迁移。不得重写 Snap序列化格式或自动序列化 ECS/Actor内部状态。

---

## 0. master 已有能力必须保留

master 已经提供：

```text
IFullSnap

IFullSnapRuntime

FullSnapRuntime

IGeneratedFullSnapNode

FullSnapGenerator

SnapDocument / SnapSection

SnapWriter / SnapReader

SnapArrayWriter / SnapArrayReader

JSON Codec

IClipSnap<TClip>

ClipSnapExtensions

ClipSnapHandle<TClip>

Snap Key和 Version语义

SnapTests全部现有行为
```

现有同步 API：

```csharp
SnapDocument Serialize();

void Deserialize(
    SnapDocument document);

string SerializeJson(
    JsonSerializerOptions? options = null);

void DeserializeJson(
    string json,
    JsonSerializerOptions? options = null);
```

现有 Clip API：

```csharp
target.Clip<TClip>()
    .Serialize();

target.Clip<TClip>()
    .Deserialize(in clip);

target.TryClip<TClip>(
    out ClipSnapHandle<TClip> handle);
```

这些 API 不删除、不改名、不改变主线程单 Scope测试结果。

---

## 1. Scope 化后的归属

每个 Snap Node仍属于它原本的业务对象：

```text
Layer Node：
    OwnerLayer + MainScope

Service Node：
    OwnerLayer + OwnerScope

Context Node：
    OwnerLayer + OwnerScope
```

Build阶段分组：

```text
LayerBuildPlan
    → Snap Node Contribution
    → 按 OwnerScope投影 ScopeSnapPlan
```

运行关系：

```text
LayerRuntime
    → FullSnapCoordinator

ScopeRuntime
    → ScopeSnapExecutor
    → ScopeSnapPlan
```

`FullSnapCoordinator`只协调，不直接读取 CustomScope对象。

---

## 2. FullSnapGenerator 原样保留

`FullSnapGenerator` 继续只负责：

```text
识别 partial class + IFullSnap

补充 IGeneratedFullSnapNode

生成 __SnapKey

生成 __SnapVersion
```

不让 Generator生成：

```text
ScopeCall代码

ScopeId

对象 Slot查找

全局 Registry

自动 ECS序列化

自动 Actor序列化
```

OwnerScope来自已有 Service/Context/Layer Composition，不来自 Snap Generator猜测。

---

## 3. ScopeSnapPlan

Build阶段把已经创建的 Node描述冻结为：

```csharp
internal readonly struct
    ScopeSnapNodePlan
{
    internal readonly int LayerIndex;
    internal readonly int ObjectSlot;

    internal readonly string Key;
    internal readonly int Version;

    internal readonly SnapWriteInvoker Write;
    internal readonly SnapReadInvoker Read;
}
```

```csharp
internal sealed class ScopeSnapPlan
{
    internal ScopeSnapNodePlan[] Nodes;
}
```

稳定顺序：

```text
LayerIndex
→ ObjectSlot
→ SnapKey
```

运行时不再遍历 LayerChain和对象图。

但现有 master `BuildFullSnapCache()` 的：

```text
ReferenceEqualityComparer去重
IGeneratedFullSnapNode识别
```

可以修改复用到 Build Plan收集阶段。

---

## 4. Snap Key兼容

现有 master测试断言：

```text
Namespace.Type_FullSnap
```

因此：

```text
MainScope和 CustomScope都继续使用 Generator原 Key。

不得给 MainScope Key增加 Scope前缀。

不得修改现有 SnapDocument格式。
```

Build必须检查全 Runtime SnapKey唯一。

如果两个对象产生相同 Key：

```text
Build Error
```

不要在运行时自动追加：

```text
ScopeId
LayerIndex
随机后缀
```

这样可以避免旧存档格式静默变化。

---

## 5. 为什么需要 SafePoint

单 Runtime、单线程 master中：

```text
Serialize直接遍历 Node
```

已经能获得稳定状态。

多 Scope后，MainScope不能同时读取 WorkerScope对象，因为 Worker可能正在：

```text
执行 Handler

修改 Service状态

执行 ECS Query

记录或 Playback CommandBuffer

应用 Projection Result

执行 Timer / Delay continuation
```

所以 FullSnap需要：

```text
让每个 Scope在自己的 OwnerThread到达稳定边界。

所有 Scope保持业务冻结。

各 Scope在本地序列化或恢复。

全部完成后统一恢复。
```

---

## 6. SafePoint 不是什么

SafePoint不是：

```text
暂停操作系统线程

Thread.Suspend

全 Runtime大锁

MainScope跨线程读取 Worker对象

复制整个 ScopeRuntime

快照队列本身
```

SafePoint是 Scope OwnerThread中的一个控制状态：

```text
Business Admission关闭

Control / Response Call仍可进入

当前业务调用完成

结构变更已 Flush

业务 Tick暂时不再推进
```

---

## 7. SafePoint 状态

```csharp
internal enum ScopeSafePointState :
    byte
{
    Running,
    Requesting,
    Frozen,
    Restoring,
    Releasing,
    Faulted
}
```

每个 Scope只由 OwnerThread修改状态。

Coordinator通过现有 Control ScopeCall：

```text
EnterSafePoint

WriteSnapshot

ReadSnapshot

ExitSafePoint
```

这些 Request / Response使用标准 ScopeCallInbox。

禁止新增：

```text
SnapshotQueue

FreezeQueue

SnapshotCompletionPort

专用 Worker Signal语义
```

---

## 8. 进入 SafePoint 的条件

OwnerScope处理 `EnterSafePoint` 时：

```text
1. 关闭新的业务 Event / Call / Post / Query准入。
2. 已进入的 Handler返回或到达框架可控边界。
3. 当前同步 Query调用完成。
4. 23号 CommandBuffer完成 Playback或明确清空。
5. Projection Command Batch完成 Detach。
6. 已接受的 Projection Result完成本地 Apply。
7. Scope不处于 Activate / Stop / Dispose中。
8. 设置 State = Frozen。
9. 返回 SafePoint Token。
```

因为23号第一版 QueryBatch不跨 Tick：

```text
无需快照 QueryCursor。

只需等待当前 Query入口返回。
```

---

## 9. 默认不快照的运行设施

第一版不序列化：

```text
ScopeEventInbox

ScopeCallInbox

Post Pending

Timer Pending

Delay Pending

SynchronizationContext Queue

WorkerJob Pending

CommandBuffer Pending

Projection Payload

Actor Mailbox

Actor Call Promise

ScopeThread或 Signal
```

这些属于运行设施，不是业务 Snap Node。

需要保存业务含义时：

```text
由业务 IFullSnap节点显式写入等价状态。
```

不得自动遍历框架内部队列。

---

## 10. FullSnapCoordinator

```csharp
internal sealed class FullSnapCoordinator :
    IFullSnapRuntime
{
    private readonly LayerRuntime
        _runtime;

    private readonly ScopeRuntimeHost
        _scopes;

    internal LBTask<SnapDocument>
        SerializeAsync(
            CancellationToken cancellationToken = default);

    internal LBTask DeserializeAsync(
        SnapDocument document,
        CancellationToken cancellationToken = default);

    // 保留 master同步 API。
}
```

Coordinator归 `LayerRuntime`。

它不持有业务 Node引用，只持有：

```text
Scope Endpoint
Scope Snap Plan描述
RuntimeGeneration
```

---

## 11. 两阶段 Serialize

### Phase 1：全部冻结

```text
MainScope关闭业务准入。

向 Inline / WorkerScope发送 EnterSafePoint ScopeCall。

每个 Scope到达 Frozen并返回 Token。

任一 Scope失败：
    对已冻结 Scope发送 ExitSafePoint。
    Serialize失败。
```

### Phase 2：本地写入

每个 Scope OwnerThread：

```text
ScopeSnapExecutor
    → 按 ScopeSnapPlan顺序
    → 调用原 WriteFullSnap
    → 生成 SnapSection[]
```

MainScope也使用同一 Executor。

### Phase 3：合并与恢复

```text
Coordinator按稳定 ScopeId和 Node顺序合并 Section。

验证 Key唯一。

生成原 SnapDocument。

向所有 Scope发送 ExitSafePoint。

重新打开业务准入。
```

---

## 12. 两阶段 Deserialize

```text
1. 校验 document非空和 Section基本格式。
2. 全部 Scope进入 Frozen。
3. 按 ScopeSnapPlan把 Section分配到 OwnerScope。
4. OwnerThread调用原 ReadFullSnap。
5. 全部完成后 ExitSafePoint。
```

继续保留 master语义：

```text
缺少 Section：
    跳过。

Section Data为 null：
    SnapFormatException。

字段缺失、类型错误、数组越界：
    继续使用现有 SnapReader异常。
```

---

## 13. Restore 失败语义不扩大

master `Deserialize` 是顺序修改对象，不具备事务回滚。

Scope迁移不得虚构：

```text
自动回滚所有 Scope

自动保留旧 World副本

自动重建 ActorWorld

自动恢复队列
```

第一阶段失败语义：

```text
任一 Node Read失败：
    返回原异常。
    Runtime报告 Snap Restore Fault。
    业务 Admission保持关闭。
    Coordinator进入统一 Stop或由上层明确处理。
```

不得在部分失败后自动 Resume并假装恢复成功。

调用方如需回滚：

```text
应先保留一份可用 FullSnap并显式重新 Deserialize。
```

---

## 14. 同步 API兼容

### 14.1 仅 MainScope / InlineScope

当 Runtime没有独立 WorkerScope时：

```csharp
runtime.FullSnap.Serialize();
runtime.FullSnap.Deserialize(document);
```

可以在 MainScope OwnerThread同步协调。

### 14.2 存在 WorkerScope

同步阻塞 MainScope并等待 WorkerScope可能造成：

```text
ScopeCall Response无法 Pump

嵌套等待

死锁
```

因此增加：

```csharp
LBTask<SnapDocument>
    SerializeAsync(
        CancellationToken cancellationToken = default);

LBTask DeserializeAsync(
    SnapDocument document,
    CancellationToken cancellationToken = default);
```

现有同步 API在存在 WorkerScope时：

```text
抛出明确 InvalidOperationException，
提示使用 Async API。
```

不得内部 `.GetAwaiter().GetResult()`。

这保留 master测试，同时避免伪同步跨线程。

---

## 15. JSON API

保留：

```csharp
SerializeJson

DeserializeJson
```

并增加对等异步便利入口：

```csharp
LBTask<string>
    SerializeJsonAsync(
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default);

LBTask DeserializeJsonAsync(
    string json,
    JsonSerializerOptions? options = null,
    CancellationToken cancellationToken = default);
```

JSON Codec继续复用 master实现。

异步只来自 Scope协调，不来自线程池 JSON任务。

---

## 16. ClipSnap 原样保留

`ClipSnap` 是单对象立即能力：

```csharp
MoveClip clip =
    target.Clip<MoveClip>()
        .Serialize();
```

它不进入 FullSnapCoordinator。

master的普通对象场景必须继续成立：

```csharp
var carrier =
    new MultiClipCarrier();

carrier.Clip<MoveClip>()
    .Serialize();
```

因此禁止让 `ClipSnapExtensions` 强制要求：

```text
ScopeObjectBinding

LayerRuntime

ScopeId
```

对于已经绑定的 Layer / Service / Context：

```text
调用方必须在对象 OwnerScope Thread调用。
```

跨 Scope需要业务显式：

```text
ScopeCall
```

不新增：

```text
ScopeRef.Clip<T>()

RemoteClipHandle

全局 Clip Registry
```

---

## 17. ECS 与 Actor Snap边界

框架默认仍不自动序列化：

```text
Arch World

EntityInfo

Chunk

ActorWorld

Actor Mailbox

Actor Pool
```

master现有测试明确要求 FullSnap Section不自动包含：

```text
ActorWorld

EcsWorld
```

继续保留。

业务若要保存 ECS/Actor等价状态：

```text
实现 IFullSnap的 Service / Context
    → 使用业务 Exporter
    → 写入稳定 DTO
```

Restore后如何重建 Projection Actor：

```text
由业务状态和22号 Projection管线重新 Ensure。
```

不得把 Actor对象引用写入 SnapDocument。

---

## 18. Scope Stop 与 Snapshot竞争

规则：

```text
Snapshot开始后，Runtime Stop等待 Snapshot退出 SafePoint。

Runtime已经 Stopping时，拒绝新的 Snapshot。

Snapshot中出现 Fault时，Coordinator释放可释放的 Scope，
然后进入 Runtime Stop。

Dispose不能与 Write/Read并行。
```

控制消息继续使用 ScopeCall保留容量。

---

## 19. faster / master 复用

### master原样复用

```text
FullSnapRuntime序列化循环

IFullSnap / IGeneratedFullSnapNode

FullSnapGenerator

SnapDocument / Section

SnapWriter / Reader

SnapArrayWriter / Reader

JSON Codec

ClipSnapExtensions / Handle

SnapFormatException

SnapTests
```

### 修改复用

```text
BuildFullSnapCache：
    从 Runtime对象 List
    改为 Build期按 Scope生成 ScopeSnapPlan。

FullSnapRuntime：
    改为 Coordinator，
    MainScope-only路径保留原同步循环。

原 Register去重：
    移到 Build Composition阶段。
```

### 禁止新增或移植

```text
运行时反射扫描 Snap Node

自动 ECS/Actor快照

Snapshot专用线程

Snapshot专用 Queue

事务回滚系统

跨 Scope对象直读
```

---

## 20. 需要修改的代码位置

```text
LayerBase/Snap/
    IFullSnapRuntime.cs
    FullSnapRuntime.cs
    IFullSnap.cs
    IGeneratedFullSnapNode.cs
    ClipSnapExtensions.cs（原则上不改）
    SnapDocument.cs
    SnapReader / Writer
    JSON Codec

LayerBase.Generator/
    FullSnapGenerator.cs（原则上不改生成格式）

LayerBase/Application/
    LayerRuntime.cs
    BuildFullSnapCache迁移

LayerBase/Scope/
    ScopeRuntime.cs
    ScopeSafePointState.cs
    ScopeSnapExecutor.cs
    Scope控制 Call Handler

LayerBase.Test/
    SnapTests.cs（原内容不改）
    ScopeSnapTests.cs（新增）
```

---

## 21. Agent 执行任务

```text
1. 记录 master SnapTests和 Snap格式基线。
2. FullSnapGenerator保持原 Key / Version格式。
3. Build期把 Snap Node按 OwnerScope分组。
4. 检查全 Runtime SnapKey唯一。
5. 创建 ScopeSnapPlan和 ScopeSnapExecutor。
6. 创建 FullSnapCoordinator。
7. SafePoint控制复用标准 ScopeCall。
8. 实现 Freeze → Write/Read → Release两阶段协调。
9. 当前 Query完成并 Flush CommandBuffer后才 Frozen。
10. 保留同步 API的 Main/Inline路径。
11. WorkerScope场景新增 Async API。
12. 同步 API不得阻塞等待 Worker。
13. ClipSnap API和普通对象测试保持不变。
14. 不自动序列化 ECS/Actor/队列。
15. Restore失败不伪造事务回滚。
16. 新增 WorkerScope一致性和停止竞争测试。
```

---

## 22. 必须测试

### master原测试不改

```text
FullSnap_runtime_collects_generated_nodes_and_round_trips_state

FullSnap_deserialize_throws_when_required_field_is_missing

SnapArrayReader_round_trips_objects_and_reports_type_mismatch

SnapArrayReader_reports_out_of_range_access

ClipSnap_handles_multiple_clip_types_and_reports_missing_ones
```

### Scope SafePoint

```text
Snap_nodes_are_grouped_by_owner_scope

Snap_key_format_remains_master_compatible

Duplicate_snap_key_fails_build

Main_scope_only_sync_snap_still_works

Worker_scope_requires_async_snap_api

All_scopes_freeze_before_any_scope_resumes

Worker_snap_node_runs_on_worker_owner_thread

Main_snap_node_runs_on_main_owner_thread

Current_query_finishes_before_snapshot_write

Command_buffer_is_flushed_before_snapshot_write

Snapshot_does_not_capture_inboxes_or_timers

Snapshot_and_stop_do_not_deadlock

Restore_failure_does_not_resume_as_success

Scope_payload_and_call_tokens_are_released_once
```

### ClipSnap

```text
Plain_object_clip_snap_still_works

Bound_service_clip_runs_on_owner_thread

Clip_snap_has_no_global_registry

Cross_scope_clip_requires_explicit_scope_call
```

---

## 23. 验收否决项

出现任意一项，任务不通过：

```text
修改现有 Snap Key或 JSON格式

修改 master SnapTests预期

FullSnapCoordinator直接读取 Worker对象

使用 Thread.Suspend或 Runtime全局锁

新增 Snapshot专用队列

同步 API对 WorkerScope执行阻塞等待

自动序列化 EcsWorld或 ActorWorld

ClipSnap强制要求所有对象有 ScopeBinding

ClipSnap增加全局 Registry

Restore失败后自动 Resume并报告成功

SafePoint前 CommandBuffer未处理

运行时每次反射扫描 Snap Node
```

---

## 24. 本阶段最终结果

```text
master Snap格式和 API保持兼容。

FullSnap Node按 OwnerScope执行。

多 Scope通过标准 ScopeCall进入一致 SafePoint。

所有 Scope冻结后才写入或恢复。

WorkerScope使用异步协调，避免主线程阻塞死锁。

ClipSnap继续是原有单对象能力。

ECS、Actor和运行队列不会被框架擅自序列化。
```
