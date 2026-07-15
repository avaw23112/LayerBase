# 23 Scope-local ECS Scheduler、QueryBatch、CommandBuffer 与 Blueprint 迁移

> **最高原则：** 以 `master` 现有 Query/Bring、Arch ECS、CommandBuffer、Blueprint API 与测试为功能基线；只把 ECS 资源迁移到 OwnerScope，并选择性复用 `faster` 已存在的 Scheduler 代码。  
> **master 基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **faster 复用基线：** `8898a90bcb3e00a370e47f8b39f6eff32fa98980`  
> **依赖阶段：** `11_input_scope_ecs_query_submission_revised.md`、`18_scope_post_scheduler_timer_delay_migration.md`、`22_ecs_actor_projection_projected_actor_migration.md`。  
> **文档性质：** ECS 资源归属与执行边界迁移。不得借本阶段重写 Query Generator、Blueprint API、Arch CommandBuffer 或 Projection 状态机。

---

## 0. 本阶段最终目标

每个 Scope 独立拥有：

```text
EcsWorld
ScopeEcsScheduler
QueryBatch运行配置
CommandBuffer运行实例
Blueprint创建入口所使用的 World
Projection本地输出缓冲
```

最终关系：

```text
LayerRuntime
    → ScopeRuntimeHost
        → MainScope
            → EcsWorld
            → ScopeEcsScheduler
        → CombatScope
            → EcsWorld
            → ScopeEcsScheduler
        → AIScope
            → EcsWorld
            → ScopeEcsScheduler
```

约束：

```text
一个 EcsWorld只由其 OwnerScope Thread读写。

ScopeEcsScheduler不再创建第二条 ECS业务线程。

WorkerScope本身已经是异步执行域。

MainScope、InlineScope、WorkerScope使用相同 OwnerThread规则。

跨 Scope不能传递 World、Chunk、ref Component或 CommandBuffer。
```

---

## 1. 现有能力处理表

### 1.1 master 原样沿用

```text
QueryBringGenerator生成 public void入口

普通 Query：
    Query().ForEach(ref job)

Bring Query：
    Query().Bring().ForEach(ref job).Batch().Post()

Arch.Core.World / Entity / QueryDescription / Chunk

Arch.Buffer.CommandBuffer及 Playback语义

EntityCreateBuilder

EntityBlueprintBuilder

EntityBlueprint / IEntityBlueprint

EntityBlueprintCache<TBlueprint>

BlueprintUnitCache

Layer / Service / Context Blueprint扩展

现有 Blueprint、Query、Bring、Projection测试

LayerRuntime.EcsWorld现有调用兼容
```

### 1.2 faster 修改复用

```text
IEcsScheduler生命周期接口

SyncEcsScheduler直接执行结构

EcsRuntimeOptions中可复用的配置组织方式

Scheduler Start / Stop / Dispose测试结构

EcsExecutionMode相关测试基架

ECS Benchmark项目和统计方式
```

### 1.3 faster 禁止直接移植

`faster` 的 `AsyncEcsScheduler` 内部再次创建：

```text
EcsWorker
EcsWorkQueue
EcsSubmissionBatchPool
EcsResultQueue
独立 Scheduler Thread
```

Scope改造后：

```text
WorkerScope Owner Thread
    就是该 Scope EcsWorld的执行线程。
```

因此禁止在 WorkerScope 内再创建嵌套 ECS Worker。

同样禁止保留：

```text
EcsResultQueue.DrainToMainThread

LayerRuntime作为 Scheduler OwnerThread判断来源

ECS Result回 MainScope的旧专用队列

Generated Query保存 Runtime并 SubmitQuery
```

### 1.4 当前不存在、只做最小新增

代码中没有已经完成的 `EcsQueryBatch` 实现。

`faster` 只有 QueryBatch设计稿。

因此本阶段新增的 QueryBatch必须满足：

```text
不改变生成入口返回 void。

不新增 QuerySlot。

不新增 QueryRequest队列。

不新增 InputPack。

不新增跨 Tick QueryCursor。

不引入第二个 Worker。

只在原 Query Flow内部做同步、顺序分批。
```

---

## 2. ScopeEcsScheduler 的准确职责

`ScopeEcsScheduler` 是：

```text
OwnerScope内的 ECS执行协调器
```

它不是：

```text
跨线程 Scheduler
通用业务任务系统
Query注册表
Query Route Table
MainScope结果回收器
```

推荐最小结构：

```csharp
internal sealed class ScopeEcsScheduler :
    IDisposable
{
    private readonly int _runtimeGeneration;
    private readonly int _scopeId;
    private readonly World _world;

    private readonly EcsQueryBatchOptions
        _batchOptions;

    private readonly CommandBuffer
        _commandBuffer;

    private ScopeEcsSchedulerState _state;

    internal World World => _world;

    internal void RequireOwnerThread();

    internal void BeginTick();

    internal void FlushStructuralChanges();

    internal void EndTick();

    internal void Stop();

    public void Dispose();
}
```

实际类型名优先沿用 `faster` 的：

```text
IEcsScheduler
SyncEcsScheduler
EcsRuntimeOptions
```

但必须删除其 Runtime级和跨线程语义。

---

## 3. 不改变 Query 公开入口

11号已经确定：

```csharp
public void Move(
    in FrameInput frame)
{
    var job =
        new __MoveJob(
            this,
            in frame);

    ServiceECSExtensions
        .Query<
            Position,
            Velocity>(this)
        .ForEach(ref job);
}
```

23号不得改为：

```csharp
public QueryHandle Move(...)

public EcsQuerySubmitResult Move(...)

_scheduler.SubmitQuery(querySlot, inputPack)
```

Scope迁移发生在：

```text
ServiceECSExtensions.Query

LayerQueryExtensions.Query

LayerContextECSExtensions.Query

ECSWorld扩展

ProjectionQueryFlow内部执行上下文
```

而不是生成入口。

---

## 4. Query 路由

Service：

```text
Service
    → ScopeObjectBinding
    → OwnerScope LocalAccess
    → ScopeEcsScheduler
    → OwnerScope EcsWorld
```

Context：

```text
Context
    → OwnerService Binding
    → OwnerScope ScopeEcsScheduler
```

Push Layer实例：

```text
Layer实例
    → MainScope ScopeEcsScheduler
```

禁止：

```text
ServiceLayerBinder.Runtime.EcsWorld

LayerRuntime全局 EcsWorld作为 CustomScope fallback

按 Type搜索 EcsWorld

自动切换到 MainScope
```

---

## 5. `LayerRuntime.EcsWorld` 兼容门面

master已有大量测试和业务代码使用：

```csharp
runtime.EcsWorld
```

第一阶段必须保留。

迁移后的准确语义：

```text
LayerRuntime.EcsWorld
    → MainScope EcsWorld兼容门面
```

示意：

```csharp
public World EcsWorld =>
    _scopeRuntimeHost
        .Main
        .LocalAccess
        .EcsScheduler
        .World;
```

约束：

```text
它不代表 Runtime只有一个 World。

CustomScope业务对象不能通过该属性取得自己的 World。

不得修改 master原测试来删除这个入口。
```

---

## 6. QueryBatch 的最小实现

### 6.1 目的

QueryBatch只解决：

```text
单次大 Query的工作集过大

Bring / Projection输出一次性过大

单次局部缓冲扩容过大

CommandBuffer记录一次性过大时的诊断与预算
```

第一阶段不解决：

```text
多线程并行 Query

跨 Tick继续执行

ReadSet / WriteSet调度

Query Barrier

Shard Lane

任务优先级
```

### 6.2 同步顺序分批

一次生成入口调用期间：

```text
Query开始
    → Batch 0顺序执行
    → Flush Batch-local Bring / Projection输出
    → Batch 1顺序执行
    → Flush Batch-local Bring / Projection输出
    → ...
    → Query全部完成
    → CommandBuffer Playback
    → 入口返回
```

因此：

```text
public void入口仍然同步完成。

Input字段生命周期仍限于本次 Job。

不存在 Pending Query。

不存在 Query Promise。

不存在下一 Tick恢复 Cursor。
```

### 6.3 默认行为

为保证 master兼容：

```text
EnableImplicitBatching默认 false。
```

关闭时：

```text
执行路径应与 master原 ForEach尽量一致。
```

开启时才使用分批执行器。

### 6.4 配置

选择性采用 faster设计稿的配置形状：

```csharp
public readonly struct EcsQueryBatchOptions
{
    public bool EnableImplicitBatching {
        get;
    }

    public int DefaultBatchLimitBytes {
        get;
    }

    public int MinBatchEntityCount {
        get;
    }

    public int MaxBatchEntityCount {
        get;
    }
}
```

默认建议沿用 faster设计稿：

```text
DefaultBatchLimitBytes = 512 KB

MinBatchEntityCount = 256

MaxBatchEntityCount = 32768
```

但只在用户显式开启后生效。

不得把这些值写死进 Generator。

---

## 7. Batch 大小计算

Generator已经知道参与 Query 的 Component类型。

如果现有生成模板可以安全获得静态尺寸，则生成：

```text
AccessBytesPerEntity
```

否则在 Build/首次静态泛型初始化时计算一次。

公式沿用 faster设计稿：

```text
BatchEntityCount
    =
DefaultBatchLimitBytes
    /
max(1, AccessBytesPerEntity)
```

然后：

```csharp
batchEntityCount =
    Math.Clamp(
        batchEntityCount,
        options.MinBatchEntityCount,
        options.MaxBatchEntityCount);
```

`[Input]` 不计入每 Entity访问大小，因为它由整个 Job共享。

禁止每次 Tick：

```text
反射组件字段

Marshal.SizeOf(Type)

Dictionary<Type,int>查尺寸

重新计算 Batch大小
```

若某 Component尺寸无法静态确定：

```text
使用 MaxBatchEntityCount或现有保守默认值。

不要为了精确尺寸增加运行期反射。
```

---

## 8. QueryBatch 应修改的位置

优先修改：

```text
ProjectionExecutor模板

ProjectionQueryFlow.ForEach内部循环

现有 Chunk / Entity遍历位置

ProjectionBatchBuffer Flush边界
```

不得修改：

```text
QueryBringGenerator生成的公开调用链

IQueryJob泛型参数

IProjectionJob接口

用户 Query方法签名
```

如果当前 Flow内部不能在不复制 Query算法的前提下分批：

```text
先抽取原 ForEach循环为一个共享 Executor。

普通路径和 Batched路径共用同一个组件访问内核。

禁止复制一套平行 Query实现。
```

---

## 9. Bring / Projection 的 Batch边界

每个 QueryBatch可独立 Flush：

```text
ActorPostBatch

Projection Command Batch

Projection Result所需本地输出
```

实际跨 Scope路径必须遵守 22号：

```text
CustomScope
    → 标准 MainScope ScopeEventInbox

MainScope
    → MainActorRuntime本地直达
```

禁止重新引入：

```text
EcsResultQueue

LayerRuntime.ActorInbox

ProjectionQueue

Action<ActorWorld>
```

Batch Flush失败时：

```text
按22号 Payload所有权规则回滚或释放。

不得 inline调用 MainActorRuntime。
```

---

## 10. CommandBuffer 沿用 master 实现

直接复用：

```text
Arch.Buffer.CommandBuffer

Create

Destroy

Set

Add

Remove

Playback

PooledList / SparseSet

现有实体解析和负 Entity Id语义
```

不新增：

```text
EcsCommandBuffer

ScopeCommandBuffer

CommandBatch DSL

BlueprintCommandId
```

`ScopeEcsScheduler`只负责：

```text
持有或租用现有 CommandBuffer。

在 OwnerScope Thread执行 Playback。

在 Stop / Dispose时清理。
```

---

## 11. CommandBuffer Safe Point

Query遍历过程中不应对正在遍历的 Archetype做结构变更。

第一阶段安全顺序：

```text
完整 Query所有 Batch执行完毕
    → Bring / Projection输出已 Detach
    → CommandBuffer.Playback(OwnerScope World)
    → World进入下一业务步骤
```

禁止默认在每个 Batch后 Playback，因为：

```text
结构变更可能使后续 Chunk / Entity枚举失效。
```

只有在 Arch现有 Query API明确保证结构版本变化后继续枚举安全时，才允许另立优化任务调整。

当前任务不做该假设。

---

## 12. Advanced World API 保持兼容

master公开：

```csharp
service.ECSWorld()
```

作为高级入口。

继续保留。

其文档语义：

```text
调用方自行遵守 OwnerThread和结构变更规则。
```

本阶段不禁止业务直接使用 World，也不偷偷把所有直接调用改为 CommandBuffer。

只要求：

```text
框架生成的 Batch路径使用安全点。

跨 Scope不能取得 World。
```

---

## 13. Blueprint 原样沿用

master已有：

```text
EntityBlueprintBuilder

EntityBlueprint

IEntityBlueprint

EntityBlueprintCache<TBlueprint>

BlueprintUnitCache

EntityCreateBuilder.WithBlueprint<TBlueprint>()

LayerBlueprintExtensions

ServiceBlueprintExtensions

ContextBlueprintExtensions

WorldBlueprintExtensions

Blueprint Analyzer

Bundle测试
```

这些功能不重新设计。

### 13.1 只修改 World路由

```csharp
service.CreateEntity()
```

必须使用：

```text
Service OwnerScope EcsWorld
```

Context和 Layer同理。

### 13.2 不新增 BlueprintId

旧稿中的：

```text
BlueprintId

BlueprintPlan[]

Blueprint Type → Id运行表

Generated Create Invoker数组
```

在 master/faster代码中都不是现有功能。

本阶段禁止新增。

继续使用：

```text
EntityBlueprintCache<TBlueprint>.GetOrBuild()
```

其缓存只能保存：

```text
不可变 ComponentType描述
```

不能保存：

```text
World

ScopeRuntime

Entity

Actor实例
```

### 13.3 ProjectedActor

`EntityCreateBuilder.WithProjectedActor<TActor>()` 的业务 API继续保留。

Scope改造后：

```text
Builder只给 Entity写入 Projection所需组件和类型信息。

不得直接取得 ActorWorld。

实际 Ensure由22号 Projection管线完成。
```

不得通过 Blueprint绕过 MainActorRuntime。

---

## 14. Scope Activate

OwnerScope Thread：

```text
1. 创建 EcsWorld。
2. 创建 ScopeEcsScheduler。
3. 创建或租用 CommandBuffer。
4. 绑定 Projection CommandSink。
5. 安装 QueryBatchOptions。
6. 创建 LayerProvider / Service / Context。
7. Attach ScopeObjectBinding。
8. Service/Context Query入口开始可用。
```

Query系统不需要：

```text
Query Registration

QuerySlot Freeze

Generated Query Binder
```

因为 Generator继续调用静态泛型 Query Flow。

---

## 15. Scope Tick

推荐固定位置：

```text
1. ScopeEvent / ScopeCall / continuation。
2. Timer / Post。
3. Layer Update调用生成 Query入口。
4. Query同步执行全部 Batch。
5. Flush Projection输出。
6. CommandBuffer Playback Safe Point。
7. Projection Sweep。
8. EndTick Diagnostics。
```

如果 Query入口内部已经完成 CommandBuffer Playback：

```text
Tick末尾只做兜底 Flush，不重复 Apply。
```

必须只有一个权威 Playback点。

---

## 16. Scope Stop / Dispose

Stop：

```text
1. 拒绝新的业务 Tick。
2. 等当前 Query调用返回。
3. Flush或取消未提交的 Projection Payload。
4. 按明确策略 Apply或清空 CommandBuffer。
5. 执行最终 ProjectedActor Release。
6. Scope进入可 Dispose状态。
```

因为第一阶段 Query不跨 Tick：

```text
不存在 Pending Query队列。

不存在 QueryCursor。

不存在 Input Lease等待。
```

Dispose：

```text
1. Dispose CommandBuffer。
2. Dispose ScopeEcsScheduler。
3. Dispose EcsWorld。
```

全部在 OwnerScope Thread。

---

## 17. faster 复用清单

### 直接或修改复用

```text
IEcsScheduler接口的 Start / Stop / Dispose职责

SyncEcsScheduler的直接 Execute思路

EcsExecutionMode枚举（若现有 API依赖）

EcsRuntimeOptions配置组织方式

EcsExecutionModeBenchmarks

AsyncEcsQueryTests中 OwnerThread、Stop、Fence测试写法

faster QueryBatch设计稿中的容量计算原则
```

### 只参考，不移植

```text
AsyncEcsScheduler

EcsWorker

EcsWorkQueue

EcsSubmissionBatchPool

EcsResultQueue

DrainResultsToMainThread

独立 Scheduler Thread
```

### master原样保留

```text
QueryBringGenerator

ProjectionQueryFlow

ProjectionExecutor

CommandBuffer

Blueprint全部公开 API

原测试
```

---

## 18. 需要修改的代码位置

```text
LayerBase/ECS/Runtime/
    IEcsScheduler.cs
    SyncEcsScheduler.cs
    EcsRuntimeOptions.cs
    新 ScopeEcsScheduler或现有类型收敛

LayerBase/ECS/Extensions/
    ServiceECSExtensions.cs
    LayerQueryExtensions.cs
    LayerContextECSExtensions.cs

LayerBase/ECS/Projection/
    Flow/
    Templates/ProjectionExecutor.tt
    Generated Executor
    ProjectionBatchBuffer

LayerBase/ECS/Buffer/
    不复制 CommandBuffer，只接入现有类型

LayerBase/ECS/Blueprint/
    只修改 Scope World路由和 Projection接入

LayerBase/Scope/
    ScopeRuntime.cs
    ScopeLocalAccess.cs
    ScopeObjectBinding.cs

LayerBase/Application/
    LayerRuntime.ECS.cs
    LayerRuntime.EcsWorld兼容门面
```

---

## 19. Agent 执行任务

```text
1. 记录 master Query、CommandBuffer、Blueprint测试基线。
2. 每 Scope创建独立 EcsWorld。
3. 将 faster IEcsScheduler/Sync结构收敛为 OwnerScope Scheduler。
4. 删除 Scheduler对 LayerRuntime、MainThread ResultQueue的依赖。
5. WorkerScope不创建嵌套 EcsWorker。
6. 保持 Generator public void入口和 Query链。
7. Service/Context/Layer Query路由到 OwnerScope Scheduler。
8. 保留 runtime.EcsWorld MainScope兼容门面。
9. 新增可关闭的同步隐式 QueryBatch。
10. Batch只改原 Executor内部遍历，不新增 Query队列。
11. Batch Flush Projection输出，跨 Scope走22号。
12. 完整 Query结束后使用现有 CommandBuffer Playback。
13. Blueprint API、Cache、Builder保持原设计。
14. Blueprint创建使用 OwnerScope World。
15. ProjectedActor Builder不直接访问 ActorWorld。
16. 保留所有 master原测试并新增 Scope隔离测试。
```

---

## 20. 必须测试

### 原测试不修改

```text
Query Generator测试

QueryBringJobTests

ProjectionQueryFlow测试

Bundle_Config_ShouldAddExpectedComponents

Blueprint Analyzer测试

CommandBuffer测试

ProjectedActor相关测试
```

### Scope ECS

```text
Each_scope_has_independent_world

Service_query_uses_owner_scope_world

Context_query_uses_owner_service_scope_world

Push_layer_query_uses_main_scope_world

Runtime_ecs_world_is_main_scope_facade

Worker_scope_world_is_only_used_on_worker_thread

Cross_scope_world_access_is_impossible

Disposing_scope_does_not_dispose_other_world
```

### QueryBatch

```text
Batching_disabled_matches_master_result

Batching_enabled_matches_unbatched_component_result

Generated_entry_remains_void

Input_is_shared_across_all_batches

Batching_does_not_create_query_request_queue

Batching_does_not_continue_across_tick

Bring_output_flushes_in_batch_order

Command_buffer_plays_back_after_query_completion

Structural_changes_do_not_invalidate_remaining_query

Batch_path_steady_state_has_no_unexpected_allocation
```

### Blueprint

```text
Blueprint_public_api_remains_compatible

Blueprint_cache_contains_no_world_or_scope

Service_blueprint_uses_owner_scope_world

Same_blueprint_creates_entities_in_two_independent_worlds

Projected_actor_blueprint_does_not_access_actor_world_directly
```

---

## 21. 验收否决项

出现任意一项，任务不通过：

```text
Generated Query入口返回非 void

新增 QuerySlot / QueryRegistry / QueryRequest

新增 InputPack或跨 Tick Cursor

WorkerScope内部再启动 EcsWorker

Scheduler Result通过专用 EcsResultQueue回 MainScope

Service Query回退 LayerRuntime.EcsWorld

多个 Scope共享同一个 World

复制或重写 Arch CommandBuffer

新增 BlueprintId / BlueprintPlan

Blueprint Cache保存 World / ScopeRuntime

Query Batch间默认 Playback导致枚举失效

CustomScope直接访问 ActorWorld

为通过新实现修改 master原测试预期
```

---

## 22. 本阶段最终结果

```text
Query/Bring用户 API不变。

[Input]增量设计不变。

每个 Scope有独立 EcsWorld和轻量 OwnerThread Scheduler。

QueryBatch是原 Query Flow内部的同步顺序分批，
不是新的异步任务系统。

CommandBuffer沿用 master实现，
在安全点 Playback。

Blueprint沿用 master API和缓存，
只切换到 OwnerScope World。

Scope ECS不会产生第二套线程域或跨 Scope资源。
```
