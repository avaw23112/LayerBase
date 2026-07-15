# 19 Layer 管理下的 Service / Context 生命周期与 Scope Tick 迁移

> **强制执行规范：** 本文必须遵守 `01_mandatory_architecture_aot_performance_standards.md`；冲突时以该规范为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `05_scope_static_composition_generators_revised.md`、`08_di_scope_container_revised.md`  
> **文档性质：** 独立阶段任务。本文只迁移 Layer、Service、Context 的生命周期与 Tick 到 Scope Owner Thread，不改变原生命周期接口和 Layer 内顺序语义。

---

## 0. 本阶段核心目的

Layer 继续作为 Service / Context 的上层业务管理结构。

Scope 只决定：

```text
在哪个线程执行
使用哪一份 Event / Post / Timer / ECS 资源
使用哪一个 Scope-local 实例
使用哪一个 FixedUpdate accumulator
```

最终关系：

```text
Layer
    → 管理自己的 Service / Context / Lifecycle
    → 每个对象声明 OwnerScope
    → Build 生成各 Scope 的 Layer 生命周期切片
    → Scope Owner Thread 执行这些切片
```

因此：

```text
所有 Scope 都保留 LayerIndex 和 Layer 顺序。

CustomScope 不能“没有 Layer”。

CustomScope 可以不创建完整 Layer 对象树，
但它执行的每个 Service / Context
必须属于一个 Push Layer。
```

---

## 1. 保持不变的业务接口

继续保留：

```csharp
public interface IInitializable
{
    void Initialize();
}

public interface IPostBuild
{
    void PostBuild();
}

public interface IRuntimeStart
{
    void RuntimeStart();
}

public interface IUpdate
{
    void Update(float deltaTime);
}

public interface IFixedUpdate
{
    void FixedUpdate(
        float fixedDeltaTime);
}

public interface IRuntimeStop
{
    void RuntimeStop();
}
```

如果 `faster` 中存在其他原生命周期接口，全部继续保留。

本文不新增：

```text
IScopeUpdate
IScopeRuntimeStart
IWorkerUpdate
ILayerScopeLifecycle
```

Scope 归属由对象 Binding 和 Build Plan 决定，不通过复制生命周期接口表达。

---

## 2. 业务归属与执行归属

一个 Service 同时具有：

```text
OwnerLayer：
    业务管理、DI、Mount、Provide/From、Tool 和生命周期顺序。

OwnerScope：
    实例隔离、线程、EventCenter、Post、Timer、ECS 和 Call 范围。
```

示例：

```csharp
[Scope<PathfindingScope>]
public sealed partial class PathfindingService :
    IService,
    IRuntimeStart,
    IUpdate,
    IRuntimeStop
{
    public void RuntimeStart()
    {
        _graph.Build();
    }

    public void Update(
        float deltaTime)
    {
        _requests.ProcessBudget();
    }

    public void RuntimeStop()
    {
        _graph.Clear();
    }
}
```

如果该 Service 由 `GameplayLayer` 注册，则完整归属是：

```text
GameplayLayer
    → PathfindingScope
    → PathfindingService
```

不能简化成：

```text
PathfindingScope
    → PathfindingService
```

---

## 3. Layer-first 生命周期计划

05 号文档产生：

```text
LayerBuildPlan[]
    → 每个 Layer 的 ScopeContribution
```

19 号文档将这些 Contribution 投影成 Scope 本地执行数组。

### 3.1 LayerLifecycleContribution

```csharp
internal readonly struct
    LayerLifecycleContribution
{
    internal readonly int LayerIndex;
    internal readonly int ScopeId;

    internal readonly int ObjectSlot;

    internal readonly LifecycleKind Kind;
    internal readonly LifecycleInvoker Invoker;

    internal readonly int StableOrder;
}
```

实际实现可以直接在 Build 阶段展开为各阶段连续数组，不必保留该结构到 Running。

### 3.2 ScopeLayerLifecycleSlice

```csharp
internal readonly struct
    ScopeLayerLifecycleSlice
{
    internal readonly int LayerIndex;

    internal readonly int InitializeStart;
    internal readonly int InitializeCount;

    internal readonly int PostBuildStart;
    internal readonly int PostBuildCount;

    internal readonly int RuntimeStartStart;
    internal readonly int RuntimeStartCount;

    internal readonly int UpdateStart;
    internal readonly int UpdateCount;

    internal readonly int FixedUpdateStart;
    internal readonly int FixedUpdateCount;

    internal readonly int RuntimeStopStart;
    internal readonly int RuntimeStopCount;

    internal readonly int DisposeStart;
    internal readonly int DisposeCount;
}
```

该 Slice 是执行索引，不是新的 Layer 对象。

### 3.3 ScopeLifecyclePlan

```csharp
internal sealed class ScopeLifecyclePlan
{
    internal ScopeLayerLifecycleSlice[]
        Layers;

    internal LifecycleInvoker[]
        Initialize;

    internal LifecycleInvoker[]
        PostBuild;

    internal LifecycleInvoker[]
        RuntimeStart;

    internal UpdateInvoker[]
        Update;

    internal FixedUpdateInvoker[]
        FixedUpdate;

    internal LifecycleInvoker[]
        RuntimeStop;

    internal LifecycleInvoker[]
        Dispose;
}
```

数组按 Push LayerIndex 拼接。

运行期只遍历连续数组和 Slice Range。

---

## 4. Layer 顺序规则

对每个 Scope 都适用：

```text
LayerIndex 0
    → LayerIndex 1
    → LayerIndex 2
```

不能只对 MainScope 使用 LayerChain 顺序。

### 4.1 正向阶段

以下阶段按 LayerIndex 正序：

```text
Initialize
PostBuild
RuntimeStart
FixedUpdate
Update
```

### 4.2 逆向阶段

以下阶段按 LayerIndex 逆序：

```text
RuntimeStop
Dispose
```

Layer 内的 Service / Context 顺序：

```text
保持 faster 原 Layer 内注册、Mount 和生命周期顺序。
```

本文不重新定义：

```text
Service 与 Context 谁先 Update
多个 Service 的 StableOrder
PostBuild 与 Initialize 的原细节
```

只要求同样的原顺序在每个 Scope 的该 Layer Slice 中复现。

---

## 5. Scope Tick 的执行视图

Scope Owner Thread 持有：

```text
ScopeLifecyclePlan
FixedUpdate accumulator
OwnerScope Event/Post/Timer/ECS
```

运行时不调用 Layer 对象树递归 Tick。

### 5.1 Update

```csharp
private static void PumpUpdate(
    ScopeLifecyclePlan plan,
    float deltaTime)
{
    ScopeLayerLifecycleSlice[] layers =
        plan.Layers;

    for (
        int layerIndex = 0;
        layerIndex < layers.Length;
        layerIndex++)
    {
        ref readonly
            ScopeLayerLifecycleSlice slice =
            ref layers[layerIndex];

        int end =
            slice.UpdateStart
            + slice.UpdateCount;

        for (
            int i = slice.UpdateStart;
            i < end;
            i++)
        {
            plan.Update[i](
                deltaTime);
        }
    }
}
```

如果所有 Update 已经按 LayerIndex 完整拼接，也可以直接遍历整个 `Update[]`。

保留 Slice 的价值是：

```text
诊断 Layer 耗时
按 Layer 跳过 Range
保留 Layer 管理语义
```

不得在每 Tick 重新计算 Range。

### 5.2 FixedUpdate

```csharp
scope.FixedAccumulator +=
    deltaTime;

int steps = 0;

while (
    scope.FixedAccumulator
        >= scope.FixedDeltaTime
    && steps
        < scope.MaxFixedCatchUpSteps)
{
    PumpFixedUpdate(
        scope.Lifecycle,
        scope.FixedDeltaTime);

    scope.FixedAccumulator -=
        scope.FixedDeltaTime;

    steps++;
}
```

Accumulator 属于 ScopeRuntime。

不同 Scope：

```text
互不共享 accumulator
可以使用不同 TickRate / FixedDelta 配置
只在自己的 Owner Thread 修改
```

---

## 6. MainScope、InlineScope 与 WorkerScope

Layer 管理关系在三种 Scope 中相同。

差异只在调度器。

### 6.1 MainScope

```text
runtime.Pump(deltaTime)
    → MainScope Owner Thread
    → MainScope ScopeLifecyclePlan
    → 按 LayerIndex执行
```

### 6.2 InlineScope

```text
MainScope Scheduler
    → 按稳定 Scope 顺序推进 InlineScope
    → InlineScope 使用自己的 ScopeLifecyclePlan
    → 仍按 LayerIndex执行
```

InlineScope 与 MainScope 在同一物理线程，也必须使用独立：

```text
EventCenter
PostScheduler
Timer / Delay
EcsWorld
Call Registry
Lifecycle Plan
FixedAccumulator
```

### 6.3 WorkerScope

```text
ScopeWorker
    → Worker Owner Thread
    → 固定频率 Tick
    → WorkerScope ScopeLifecyclePlan
    → 按 LayerIndex执行
```

MainScope 不直接调用 Worker Service 的生命周期方法。

但 Worker Service 仍归其 OwnerLayer 管理。

---

## 7. Scope Pump 中的生命周期位置

本文只定义 Layer 生命周期回调的相对位置，不重写 Event、Post、ECS 或 Actor 的完整 Pump。

建议关系：

```text
1. 进入 ScopeExecution Context
2. 处理 ScopeCall / ScopeEvent
3. 处理本地 continuation
4. Timer / Delay / Post
5. FixedUpdate：按 LayerIndex
6. Update：按 LayerIndex
7. ECS / Structural SafePoint
8. MainScope 固定 ActorWorld 阶段
9. 退出 ScopeExecution Context
```

准确的 Event/Post/ECS/Actor 子阶段以对应文档为准。

本阶段不可为了生命周期管理新增队列或线程同步通道。

---

## 8. Activate 生命周期

`ScopeActivateCall` 到达 Owner Thread 后：

```text
1. 创建 Scope 本地资源。
2. 按 LayerIndex 创建当前 Scope 各 LayerProvider。
3. 按 LayerIndex 创建 Service / Context。
4. Attach ScopeObjectBinding。
5. Mount。
6. Provide / From。
7. Event Handler 注册。
8. LocalCall Handler 绑定。
9. Initialize：LayerIndex 正序。
10. PostBuild：LayerIndex 正序。
11. Prewarm / Freeze。
12. RuntimeStart：LayerIndex 正序。
13. Scope 进入 Running。
```

每一步都操作：

```text
当前 Layer
在当前 Scope 中的实例和 Slice
```

不能将 Scope 中所有 Service 作为一个无 Layer 集合统一初始化。

---

## 9. Stop 与 Dispose

### 9.1 Stop

`ScopeStopCall` 在 Owner Thread 执行：

```text
1. 关闭新业务入口。
2. 处理已接受 Event / Call / Worker Result。
3. RuntimeStop：LayerIndex 逆序。
4. Scope 状态进入 Stopped。
```

### 9.2 Dispose

```text
1. 解除 Event / LocalCall Handler。
2. Unbind Provide / From。
3. 按 LayerIndex 逆序释放 Context。
4. 按 LayerIndex 逆序释放 Service。
5. Dispose LayerProvider。
6. Dispose Scope 本地 Event/Post/Timer/ECS。
```

Layer 内具体 Context / Service Dispose 顺序继续遵守 08、09、13 和 `faster` 原语义。

不能在 Handler Target 已 Dispose 后继续 Pump 该 Layer Slice。

---

## 10. Layer 启用、禁用与跳过

如果 `faster` 已有 Layer Active / Enable / Disable 语义，必须继续作用于该 Layer 在所有 Scope 中的执行切片。

运行期不允许：

```text
MainScope 直接写 WorkerScope 的 Layer 状态
共享一个跨线程可变 bool
```

正确边界：

```text
Layer 管理命令
    → 通过既有 ScopeEvent / ScopeCall
    → 目标 Scope Owner Thread
    → 更新本 Scope 对应 Layer Slice 的 Active 状态
```

如果本次 Scope 迁移不涉及动态 Layer Enable/Disable，则保持现有 API和语义，不额外新增状态结构。

---

## 11. 空 Layer 与轻量布局

某 Scope 中某 Layer没有 Service / Context：

```text
该 Layer Slice 的所有 Count = 0。
```

运行期可以：

```text
一次 Range 判断后跳过
```

不得：

```text
认为该 Scope 没有这个 Layer
重新给后续 Layer 编号
改变 Handler / Lifecycle 顺序
```

也不要求创建空 Layer 对象。

这实现：

```text
语义上 Layer 顺序完整
物理上空 Layer 接近零成本
```

---

## 12. 业务场景

Push 顺序：

```text
0 FoundationLayer
1 GameplayLayer
2 PresentationLayer
```

### CombatScope

```text
FoundationLayer:
    SimulationClock
    RuntimeStart / Update

GameplayLayer:
    CombatService
    BuffContext
    RuntimeStart / FixedUpdate / Update / RuntimeStop

PresentationLayer:
    空
```

CombatScope Update：

```text
FoundationLayer Slice
    SimulationClock.Update

GameplayLayer Slice
    CombatService.Update
    BuffContext.Update

PresentationLayer Slice
    0 Count，跳过
```

CombatScope Stop：

```text
PresentationLayer
    空

GameplayLayer
    Buff / Combat RuntimeStop

FoundationLayer
    SimulationClock RuntimeStop
```

### PathfindingScope

只有 GameplayLayer 有对象，但仍保持：

```text
FoundationLayer 空 Slice
GameplayLayer PathfindingService Slice
PresentationLayer 空 Slice
```

因此不能写成“PathfindingScope 没有 Layer”。

---

## 13. faster 分支复用

### 13.1 直接复用

```text
LayerBase/Layer/Layer.cs
    IInitializable / IPostBuild / IRuntimeStart /
    IUpdate / IFixedUpdate / IRuntimeStop 收集逻辑

LayerChain
    Push 顺序
    MainScope 原层级顺序语义

FixedUpdateOptions
FixedAccumulator / MaxCatchUp 语义

Layer 生命周期测试
Service / Context 生命周期测试
RuntimeStart / RuntimeStop 测试
Actor 生命周期测试中与顺序相关的部分
```

### 13.2 修改后复用

```text
Layer.LifecycleBuild：
    保留接口发现和原 Layer 内顺序；
    输出 LayerScopeContribution，
    不只保存 MainScope 实例列表。

LayerRuntime Pump：
    MainScope 使用 ScopeLifecyclePlan；
    InlineScope 由 Runtime 调度；
    WorkerScope 由 ScopeWorker 调度。

ScopeRuntime：
    保存当前 Scope 的扁平 LifecyclePlan 和 accumulator；
    不成为业务对象管理根。
```

### 13.3 禁止实现

```text
CustomScope 没有 Layer 的 ObjectPlan 根顺序

只为 MainScope 保留 Layer 顺序

Scope 自动决定 Service / Context 生命周期顺序

运行期接口扫描

每 Tick 遍历 Type / Dictionary / List

为每个 Scope 复制完整 LayerChain 对象树

MainScope 直接调用 Worker Update
```

---

## 14. Agent 执行任务

```text
1. 删除“Layer 只排序 MainScope”的设计。
2. 删除“CustomScope 没有 Layer”的设计。
3. 所有 Lifecycle Contribution 增加 OwnerLayerIndex 和 OwnerScopeId。
4. Build 按 LayerIndex收集原 Layer 内生命周期顺序。
5. 为每个 Scope 生成 ScopeLayerLifecycleSlice[]。
6. 各阶段 Invoker 数组按 LayerIndex连续拼接。
7. Initialize/PostBuild/RuntimeStart 正序。
8. RuntimeStop/Dispose 逆序。
9. 每 Scope 独立 FixedAccumulator。
10. MainScope/InlineScope/WorkerScope 都按 LayerIndex执行。
11. WorkerScope 仅由 Owner Thread调用生命周期。
12. 空 Layer 使用零长度 Slice，不改变 LayerIndex。
13. 保留 faster 原 Layer 内 Service/Context 顺序。
14. 不创建新的生命周期接口。
15. 不为每个 Scope 重建完整 Layer 对象树。
16. 生命周期诊断保留 OwnerLayerIndex。
17. 如果存在 Layer Enable/Disable，作用到各 Scope 对应 Slice。
18. 更新 MainScope-only 生命周期测试。
19. 增加多 Scope Layer 顺序和线程归属测试。
20. 运行 faster 原生命周期和性能测试。
```

---

## 15. 必须测试

```text
All_scopes_preserve_push_layer_order

Custom_scope_service_has_owner_layer

Custom_scope_cannot_use_object_plan_as_root_order

Main_scope_lifecycle_behavior_is_preserved

Inline_scope_runs_layer_slices_in_push_order

Worker_scope_runs_layer_slices_in_push_order

Worker_lifecycle_runs_on_owner_thread

Main_thread_never_invokes_worker_update

Initialize_runs_in_forward_layer_order

Post_build_runs_in_forward_layer_order

Runtime_start_runs_in_forward_layer_order

Runtime_stop_runs_in_reverse_layer_order

Dispose_runs_in_reverse_layer_order

Empty_layer_slice_preserves_order

Same_update_type_in_two_scopes_has_two_instances

Each_scope_has_independent_fixed_accumulator

Layer_internal_service_context_order_is_preserved

Disposed_layer_slice_is_never_pumped

Running_tick_uses_precomputed_ranges

Steady_state_update_is_zero_allocation
```

---

## 16. 验收否决项

出现以下任意一项，任务不通过：

```text
文档仍写 Layer 只属于 MainScope

文档仍写 CustomScope 没有 Layer

Scope ObjectPlan 成为生命周期根顺序

Service / Context 只有 OwnerScope，没有 OwnerLayer

WorkerScope 生命周期顺序不受 Push LayerIndex约束

MainScope 直接调用 Worker Service.Update

运行期扫描生命周期接口

每 Tick 通过 List/Dictionary/Type 查找回调

为了保持 Layer 顺序复制完整 LayerChain 对象树

空 Layer 导致后续 LayerIndex 改变

RuntimeStop / Dispose 不按 Layer 逆序

Layer 内 faster 原顺序被重新定义
```

---

## 17. 本阶段不修改的内容

本文不修改：

```text
ScopeEvent / ScopeCall Transport
EventCenter 派发
PostScheduler
DI 解析和生命周期算法
Mount / Provide / From
LocalCall Registry
ECS Query 与 Scheduler
ActorWorld
WorkerEventJob
```

本文只保证：

```text
Layer：
    继续管理 Service / Context 和生命周期顺序。

Scope：
    在自己的 Owner Thread
    执行由 Layer 贡献的轻量生命周期切片。

运行期：
    使用连续数组和 Range，
    不需要重建 Layer 对象树。
```
