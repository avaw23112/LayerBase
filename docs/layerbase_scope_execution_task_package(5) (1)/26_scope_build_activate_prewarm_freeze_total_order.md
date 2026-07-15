# 26 Build、Activate、Prewarm 与 Freeze 总顺序

> **最高原则：** 统一 00—25 号文档中分散的 Build/Activate顺序；保留 `master` 的 `Build()` 即可使用行为，并选择性复用 `faster` 的 Prewarm Registry、Generator和测试。  
> **master 基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **faster 复用基线：** `8898a90bcb3e00a370e47f8b39f6eff32fa98980`  
> **依赖阶段：** `04_scope_lifecycle_control_protocol_revised.md`、`19_layer_service_context_scope_tick_lifecycle_migration_revised.md`、`25_build_time_event_local_call_scope_route_topology_audit.md`。  
> **文档性质：** 生命周期总顺序权威文档。其他阶段只描述本子系统插入点，不得再定义冲突顺序。

---

## 0. master 与 faster 现状

### master Build 已经执行

```text
LayerChain.Prebuild

InitializeScheduler

InitializeTimer

InitializeDelay

BuildServiceProvider

ActorWorld.PrepareRuntimeBuild

LayerChain.Build

ActorWorld.CompleteRuntimeBuild

BuildFullSnapCache

PolicyTable.Freeze
```

并直接返回可使用 Runtime。

现有测试普遍：

```csharp
LayerRuntime runtime =
    LayerHub.CreateLayers()
        .Push(layer)
        .Build();

// 立即使用 runtime
```

该使用方式不能被本阶段破坏。

### faster 已有 Prewarm

```text
LayerHubPrewarmExtensions

LayerBasePrewarmRegistry

EventPrewarmGenerator

LayerPrewarmOptions

EventCenter.PrewarmEvent<T>()

PrewarmTests
```

这些代码可以选择性复用。

但 faster 的 Prewarm是：

```text
Build后由用户手动调用 Runtime级 EventCenter。
```

Scope改造后必须变为：

```text
每个 Scope在自己的 OwnerThread预热自己的 EventCenter。
```

---

## 1. 对外兼容策略

### 1.1 正常用户继续只调用 Build

```csharp
LayerRuntime runtime =
    LayerHub.CreateLayers()
        .Push(new FoundationLayer())
        .Push(new GameplayLayer())
        .Build();
```

`Build()` 内部完成：

```text
Build Plan

Audit

Freeze Plan

Activate所有 Scope

Prewarm

Freeze运行 Registry

RuntimeStart

Open Admission
```

返回时 Runtime已经 Running。

### 1.2 内部阶段仍然拆分

内部必须保留：

```text
BuildPlan()

Activate()

Prewarm()

FreezeRuntimeRegistries()
```

用于：

```text
失败回滚

测试

WorkerScope OwnerThread执行

生命周期 Coordinator
```

但不要求普通用户手动拼顺序。

### 1.3 Public Activate

若前置文档已经保留：

```csharp
runtime.Activate();
```

则语义必须是幂等：

```text
Created / Built：
    执行 Activate。

Running：
    直接返回，不重复 Initialize / RuntimeStart。

Stopping / Disposed：
    抛出明确状态错误。
```

正常 `Build()` 已经调用过 Activate。

### 1.4 Public Prewarm兼容

选择性移植 faster：

```csharp
runtime.Prewarm();

runtime.Prewarm(options);
```

兼容语义：

```text
Build已自动完成必要预热。

再次调用只执行幂等的 MainScope Event预热兼容入口。

不重复创建 Service、Tool、Actor Storage。

不跨线程同步预热 WorkerScope。

多 Scope必要预热必须在各 Scope Activate内部完成。
```

保留 faster `PrewarmTests` 的“不抛异常”行为。

---

## 2. 生命周期状态

```csharp
public enum RuntimeState :
    byte
{
    Created,
    Building,
    Built,
    Activating,
    Running,
    Stopping,
    Stopped,
    Disposing,
    Disposed,
    Faulted
}
```

Scope状态由04号定义。

关键边界：

```text
Build只处理冷路径描述和 Runtime壳。

Activate才在 OwnerThread创建可变业务资源。

Running后不允许新增结构注册。
```

---

## 3. 总顺序概览

```text
LayersBuilder.Push / AddModule / Configure
    ↓
Build Contributions
    ↓
Resolve Layer / Scope Ownership
    ↓
Allocate Stable Id / Slot / Range
    ↓
25号 Topology Audit
    ↓
Freeze RuntimeCompositionPlan
    ↓
Create ScopeRuntime Shell / Endpoint / Bootstrap Inbox
    ↓
Activate MainScope
    ↓
Activate InlineScope
    ↓
Activate WorkerScope on Worker Owner Thread
    ↓
Per-Scope Prewarm
    ↓
Freeze Per-Scope Registries
    ↓
RuntimeStart
    ↓
Open Business Admission
    ↓
RuntimeState.Running
```

---

## 4. Build阶段

Build允许：

```text
Generator Manifest读取

受控反射

Dictionary / List / HashSet

稳定排序

容量估算

诊断字符串构建
```

Build步骤：

```text
1. Runtime State：Created → Building。
2. 固定 Push Layer顺序并分配 LayerIndex。
3. 读取显式 AssemblyModule Manifest。
4. 收集 Service / Context / Event / Call / Tool / Snap等 Contribution。
5. 解析每条 Contribution的 OwnerLayer / OwnerScope。
6. 生成 LayerBuildPlan。
7. 投影 ScopeExecutionPlan。
8. 分配 ObjectSlot / ProviderSlot / RouteId / LocalCallId / Range。
9. 构建 Event/Post/Timer等容量 Plan。
10. 构建 MainActorRuntime Plan。
11. 构建 Scope Snap Plan。
12. 执行25号 Topology Audit。
13. 存在 Error立即失败。
14. Freeze RuntimeCompositionPlan。
15. 释放 Build临时集合。
16. State：Building → Built。
```

Build阶段禁止：

```text
创建 Worker Service

创建 Worker EcsWorld

注册运行 Handler到 EventCenter

启动 Worker Thread执行业务

调用 RuntimeStart

执行用户 Update
```

---

## 5. Freeze RuntimeCompositionPlan

Freeze意味着：

```text
LayerIndex固定

ScopeId固定

ObjectSlot固定

ProviderSlot固定

EventTypeId / RouteId固定

LocalCallId固定

Tool Range固定

Snap Node顺序固定

容量 Plan固定
```

Freeze后：

```text
不允许 Push Layer。

不允许安装 Module。

不允许新增 Handler Route。

不允许改变 Tool归属。

不允许动态增加 Snap Node。
```

运行实例尚未完全创建，所以这不是 EventCenter的运行 Registry Freeze。

---

## 6. Bootstrap资源

为了通过 ScopeCall执行 Worker Activate，必须先创建最小 Bootstrap：

```text
Scope Endpoint

ScopeEventInbox

ScopeCallInbox

线程 Signal

Lifecycle Bootstrap Dispatcher

RuntimeGeneration

ScopeId
```

Bootstrap不包含：

```text
业务 EventCenter Handler

ServiceProvider

EcsWorld

Tool Registry

业务 LocalCall Handler
```

Worker Thread启动后只接受：

```text
Activate

Stop

Dispose

Fault控制
```

直到 Activate完成。

---

## 7. Scope Activate权威顺序

每个 Scope在 OwnerThread执行：

```text
1. State：Bootstrapping → Activating。

2. 创建 EventCenter。

3. 创建本地 PostScheduler。

4. 创建 Timer / Delay。

5. 创建 SynchronizationContext。

6. 创建 EcsWorld / ScopeEcsScheduler。

7. 创建 ScopeLocalCallRegistry壳体。

8. 创建 ScopeToolRegistry壳体。

9. 创建 ScopeSnapExecutor。

10. 按 Push LayerIndex创建 LayerProvider。

11. 按 LayerIndex创建 Service。

12. 按 LayerIndex创建 Context。

13. Attach ScopeObjectBinding。

14. 按 LayerIndex执行 Mount。

15. 按 LayerIndex执行 Provide / From。

16. 使用 faster原流程按 LayerIndex注册 Event Handler。

17. 绑定本 Scope LocalCall Handler。

18. 绑定 Projection Sink / MainActor Access。

19. Initialize：LayerIndex正序。

20. PostBuild：LayerIndex正序。

21. 执行框架 Prewarm。

22. Freeze Scope运行 Registry。

23. RuntimeStart：LayerIndex正序。

24. 打开 Business Admission。

25. State = Running。
```

不存在：

```text
ScopeServiceProvider

Scope中无 Layer的业务对象集合

MainScope代替 WorkerScope创建本地资源
```

---

## 8. MainActorRuntime Activate

MainActorRuntime属于 LayerRuntime，但只能在 MainScope OwnerThread激活。

顺序：

```text
MainScope基础资源建立后

MainScope业务 RuntimeStart之前

创建 ActorWorld

安装 Actor Type / Event / Call Plan

预热已配置 Actor Storage

开放 MainScope Actor本地访问
```

CustomScope Activate只绑定：

```text
RemoteActorAccessor

MainScope Endpoint
```

不创建 ActorWorld。

---

## 9. Prewarm准确范围

Prewarm只处理已经存在的首次结构成本。

### 9.1 EventCenter

复用 faster：

```text
LayerBasePrewarmRegistry

EventPrewarmGenerator

LayerPrewarmOptions

EventCenter.PrewarmEvent<T>
```

改造：

```text
每个 Scope只预热自己 ScopePlan中已知 Event Type。

在 OwnerScope Thread调用本地 EventCenter。

不使用全局 EventCenter。
```

### 9.2 Post / Timer / Delay

根据 Build容量 Plan：

```text
创建初始 Node / Slot / Pool容量。

注册已知 EventType Plan。

不发送真实业务事件。
```

### 9.3 ECS

预热：

```text
创建 World。

创建 ScopeEcsScheduler。

准备 CommandBuffer。

准备已启用 QueryBatch的基础缓冲。
```

不做：

```text
运行所有业务 Query。

创建测试 Entity。

建立 QuerySlot Registry。
```

### 9.4 LayerTool

14号 Tool默认懒创建。

Prewarm不应自动实例化所有 Tool，因为：

```text
Tool Factory可能昂贵。

Cache=false Tool不应预创建。

现有 LayerTool没有 Prewarm元数据。
```

只预分配：

```text
ScopeToolRegistry Descriptor和 Cache Slot数组。
```

### 9.5 Actor

只在 MainActorRuntime：

```text
复用现有 Actor Pool / Storage Prewarm能力。

只预热 Build Plan中明确配置的 Actor类型和容量。
```

不得猜测所有 Actor类型都需要实例。

### 9.6 Snap / Diagnostics

只创建固定 Plan和 Snapshot Buffer初始容量。

不执行真实 Serialize。

---

## 10. 不新增用户 Prewarm Hook

代码中没有现成：

```text
IScopePrewarm

ScopePrewarmContext

[ScopePrewarm]
```

因此本阶段禁止新增这些 API。

用户原有生命周期仍是：

```text
Initialize

PostBuild

RuntimeStart
```

框架 Prewarm插在：

```text
PostBuild之后

RuntimeStart之前
```

业务需要预创建资源时继续使用已有 PostBuild / RuntimeStart语义，不新增第四套用户 Hook。

---

## 11. Freeze Scope运行 Registry

Prewarm后，RuntimeStart前：

```text
EventCenter Freeze

Post Policy Table Freeze

LocalCall Registry Freeze

ScopeRoute Table Freeze

Tool Descriptor Freeze

Snap Plan Freeze

Diagnostics Descriptor Freeze
```

Freeze后允许变化的是运行状态：

```text
队列内容

Timer Slot

Entity数据

Tool Cache实例

Actor实例

Counters
```

不允许变化的是结构：

```text
Handler注册

Route

Tool定义

Service定义

Snap Node定义

Layer / Scope归属
```

---

## 12. EventCenter Reflection Fallback

faster PrewarmTests已有：

```text
RegisterEventType<T>

ReflectionFallbackCount

OnReflectionFallback
```

迁移要求：

```text
Build已知 Handler的 Event Type必须预注册。

正常 Running路径不应触发 Reflection Fallback。

用户通过高级非泛型动态订阅未知 Event时，
继续保留现有 Observable Fallback行为。
```

不要为了消除所有 Fallback删除 master/faster高级 API。

---

## 13. Build失败

Build失败时：

```text
RuntimeCompositionPlan未 Freeze或标记无效。

不启动业务 Worker。

释放 Runtime Shell。

清理已创建的静态外 Runtime引用。

不调用 Service Dispose，因为 Service尚未创建。
```

继续复用现有 Build Failure GC测试方式。

---

## 14. Activate失败回滚

按04号：

```text
当前 Scope OwnerThread记录已经完成的阶段。

按逆序解除 Handler / Call / Binding。

Dispose已创建 Context。

Dispose已创建 Service。

Dispose LayerProvider。

Dispose Tool Registry。

Dispose ECS / Timer / EventCenter。

返回 Faulted ActivateResponse。
```

若任意 WorkerScope Activate失败：

```text
停止已成功 Activate的其他 Scope。

MainScope最后回滚。

MainActorRuntime按实际完成阶段释放。
```

---

## 15. Public Build兼容

为了保留 master：

```csharp
LayerRuntime runtime =
    builder.Build();

runtime.Send(...);
runtime.Pump(...);
runtime.FullSnap.Serialize();
```

`Build()`返回前必须完成全部 Activate。

不得要求批量修改 master测试为：

```csharp
builder.Build();
runtime.Activate();
```

显式 Activate仅作为内部和高级生命周期入口。

---

## 16. faster 复用

### 直接或修改复用

```text
LayerBasePrewarmRegistry

EventPrewarmGenerator

LayerPrewarmOptions

LayerPrewarmTargets

EventCenter.PrewarmEvent<T>

RegisterEventType<T>

ReflectionFallbackCount

OnReflectionFallback

PrewarmTests

Actor Pool已有 Prewarm逻辑
```

### master原样沿用

```text
LayersBuilder.Build公开用法

LayerChain Prebuild / Build语义

Initialize / PostBuild / RuntimeStart顺序

PolicyTable.Freeze

Actor PrepareRuntimeBuild / CompleteRuntimeBuild核心能力

Build失败和 Dispose测试
```

### 禁止新增

```text
IScopePrewarm

ModuleInitializer Prewarm

全局 Prewarm Registry保存 Runtime实例

WorkerScope资源在 MainThread预热

Running阶段动态注册 Handler

为了兼容引入 Obsolete空壳生命周期
```

---

## 17. 需要修改的代码位置

```text
LayerBase/Application/
    LayerRuntime.cs
    LayersBuilder.Build
    LayerHubPrewarmExtensions.cs

LayerBase/Event/
    LayerBasePrewarmRegistry.cs
    LayerPrewarm.cs
    EventCenter.cs
    Event Runtime Policy

LayerBase.Generator/
    EventPrewarmGenerator.cs

LayerBase/Scope/
    ScopeRuntime.cs
    ScopeLifecycleController.cs
    ScopeRuntimeHost.cs

LayerBase/Actor/
    MainActorRuntime
    Actor Pool Prewarm

LayerBase/ECS/
    ScopeEcsScheduler

LayerBase.Test/
    PrewarmTests.cs
    BuildFailureCleanupTests
    ScopeActivateTests
```

---

## 18. Agent 执行任务

```text
1. 记录 master Build顺序和现有测试。
2. 内部拆分 BuildPlan / Activate / Prewarm / Freeze。
3. Public Build继续返回 Running Runtime。
4. Build生成并 Freeze RuntimeCompositionPlan。
5. Build前执行25号 Audit。
6. 创建 Lifecycle Bootstrap资源。
7. Main/Inline/Worker均在 OwnerThread Activate。
8. Activate按本文唯一顺序。
9. 复用 faster Event Prewarm Registry和 Generator。
10. 每 Scope只预热本地 EventCenter。
11. 不新增 IScopePrewarm。
12. Tool只预分配 Slot，不强制创建实例。
13. Actor只在 MainActorRuntime预热。
14. Prewarm后 Freeze运行 Registry。
15. RuntimeStart后才 Open Business Admission。
16. Public Prewarm保持幂等兼容。
17. Build/Activate失败按完成阶段回滚。
18. 更新测试和 First Tick Benchmark。
```

---

## 19. 必须测试

```text
Build_returns_running_runtime_for_master_compatibility

Explicit_activate_is_idempotent_when_running

Build_freezes_composition_before_scope_activation

Topology_error_prevents_worker_start

Main_scope_resources_created_on_main_owner_thread

Worker_scope_resources_created_on_worker_owner_thread

Layers_activate_in_push_order

Activate_failure_rolls_back_in_reverse_order

Prewarm_runs_after_post_build_before_runtime_start

Each_scope_prewarms_its_own_event_center

Known_event_types_do_not_use_reflection_fallback

Unknown_dynamic_event_fallback_remains_observable

Public_prewarm_remains_non_throwing_and_idempotent

Prewarm_does_not_create_all_tools

Prewarm_does_not_run_business_queries

Scope_registries_are_frozen_before_runtime_start

Running_cannot_register_new_handler_or_route

First_tick_does_not_perform_structure_discovery

Master_build_usage_tests_remain_unchanged
```

---

## 20. 验收否决项

出现任意一项，任务不通过：

```text
Public Build返回未 Activate Runtime并迫使改写 master测试

用户必须手动猜 Prewarm调用时机

新增 IScopePrewarm

WorkerScope在 MainThread创建或预热本地资源

Prewarm自动实例化全部 Tool

Prewarm执行真实业务 Query

Running允许动态新增 Handler / Route

Freeze发生在 Handler尚未注册前

Topology Audit发生在 Activate之后

Public Prewarm重复执行 Initialize / RuntimeStart

Build失败后遗留 Worker Thread

多个文档继续定义互相冲突的 Activate顺序
```

---

## 21. 本阶段最终结果

```text
Build对用户仍是一站式入口。

内部拥有清晰的：
    Build Plan
    Audit
    Freeze Plan
    Activate
    Prewarm
    Freeze Registry
    RuntimeStart
    Admission

每个 Scope在自己的 OwnerThread创建和预热资源。

faster Prewarm代码被复用，
但不会重新引入 Runtime全局 EventCenter。

本文件成为唯一总顺序。
```
