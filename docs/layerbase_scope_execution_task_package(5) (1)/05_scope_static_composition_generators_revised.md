# 05 Layer-first 静态编排、Scope 执行投影与生成器

> **强制执行规范：** 本文必须遵守 `01_mandatory_architecture_aot_performance_standards.md`；冲突时以该规范为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `00_index.md`、`01_mandatory_architecture_aot_performance_standards.md`  
> **文档性质：** 独立阶段任务。本文只修正 Build 组合结构，使 Layer 继续作为 Scope 的上层业务管理结构；不重写 DI、EventCenter、Call、ECS、ActorWorld 或生命周期算法。

---

## 0. 本阶段核心目的

`LayersBuilder.Push(Layer)` 继续定义 Runtime 的最高级业务结构和唯一 Layer 顺序。

```text
LayersBuilder.Push
    → LayerIndex
    → LayerPlan[]
        → 每个 Layer 管理自己的 Service / Context / DI / Mount / Tool / Lifecycle
        → 每个业务对象再声明 OwnerScope
    → 从 LayerPlan 投影 ScopeExecutionPlan[]
```

最终必须同时满足：

```text
业务管理结构：
    Layer-first

线程与资源隔离：
    Scope-local

运行布局：
    可以扁平化为数组、Slot、Offset、Count

禁止：
    把 Scope 提升为 Service / Context / DI 的业务管理根
```

Scope 运行期不需要重建一棵 Layer 对象树。

Build 可以把 Layer 的结构和顺序投影为：

```text
ScopeLayerSlice[]
ObjectSlot[]
LifecycleInvoker[]
ProviderSlot[]
```

但这些只是执行视图，不能改变以下事实：

```text
Service / Context 属于哪个 Layer
DI 在哪个 Layer 范围内解析
Handler 使用哪个 LayerIndex
Lifecycle 受哪个 Layer 管理
```

---

## 1. 最终公有 Build API

保持现有 Layer 构建入口：

```csharp
using LayerRuntime runtime = LayerHub.CreateLayers()
    .Push(new FoundationLayer())
    .Push(new GameplayLayer())
    .Push(new PresentationLayer())
    .Build();
```

如果现有 `faster` 已有 Catalog 或 Module 安装入口，可继续使用现有 API：

```csharp
using LayerRuntime runtime = LayerHub.CreateLayers()
    .Push(new FoundationLayer())
    .Push(new GameplayLayer())
    .Install(GameplayModule.Instance)
    .Build();
```

本文不得新增绕过 Layer 的业务对象注册入口：

```csharp
builder.AddService<
    PathfindingScope,
    PathfindingService>();

builder.AddContext<
    PathfindingService,
    PathfindingContext>();
```

Service / Context 必须继续通过其 OwnerLayer 的原注册流程进入组合计划。

Scope 定义和 ScopeOption 可以由现有生成 Catalog 或显式 Scope 配置提供，但不能因此跳过 Layer 归属。

---

## 2. 最终组合关系

```text
RuntimeCompositionPlan
    ├── LayerPlan[0] FoundationLayer
    │   ├── MainScope Contribution
    │   ├── CombatScope Contribution
    │   └── PathfindingScope Contribution
    │
    ├── LayerPlan[1] GameplayLayer
    │   ├── MainScope Contribution
    │   ├── CombatScope Contribution
    │   └── PathfindingScope Contribution
    │
    └── LayerPlan[2] PresentationLayer
        ├── MainScope Contribution
        └── 其他 Scope 为空

RuntimeCompositionPlan
    └── ScopeExecutionPlan[]
        └── 从 LayerPlan 按 LayerIndex 投影得到
```

`LayerPlan[]` 是业务组合的权威来源。

`ScopeExecutionPlan[]` 是线程执行视图，不是业务对象所有权来源。

---

## 3. RuntimeCompositionPlan

```csharp
internal sealed class RuntimeCompositionPlan
{
    internal LayerBuildPlan[] Layers;

    internal ScopeExecutionPlan[] Scopes;

    internal ScopeEventRoute[] ScopeEventRoutes;
    internal ScopeCallRoute[] ScopeCallRoutes;

    internal int RuntimeGeneration;
}
```

### 3.1 LayerBuildPlan

```csharp
internal sealed class LayerBuildPlan
{
    internal int LayerIndex;

    internal RuntimeTypeHandle LayerType;

    internal Layer MainScopeInstance;

    internal LayerScopeContribution[]
        ScopeContributions;
}
```

`LayerIndex` 只能来自 `LayersBuilder.Push` 顺序。

`MainScopeInstance` 是用户传入 `Push` 的原 Layer 实例。

CustomScope 不要求复制完整 Layer 对象。

只有当 Layer 类型本身包含必须存在于某 Scope 的运行状态或回调，并且 `faster` 原设计确实依赖 Layer 实例时，才允许通过生成式 Factory 创建必要目标。

不得为了保持树形外观，为每个 Scope 无条件创建全部 Layer 对象。

### 3.2 LayerScopeContribution

```csharp
internal readonly struct
    LayerScopeContribution
{
    internal readonly int ScopeId;

    internal readonly int ProviderPlanIndex;

    internal readonly int ServiceStart;
    internal readonly int ServiceCount;

    internal readonly int ContextStart;
    internal readonly int ContextCount;

    internal readonly int ToolStart;
    internal readonly int ToolCount;

    internal readonly int LifecyclePlanIndex;
}
```

该结构表达：

```text
某一个 Layer
    在某一个 Scope
    贡献了哪些运行对象和生命周期内容
```

实际字段应复用现有 Plan 类型。

本文不要求复制已有数据，也不要求每个范围都单独建数组；可以使用全局连续数组与 Range。

### 3.3 ScopeExecutionPlan

```csharp
internal sealed class ScopeExecutionPlan
{
    internal ScopeDescriptor Descriptor;

    internal ScopeLayerSlice[] Layers;

    internal ScopeTransportPlan Transport;
    internal ScopeEcsPlan Ecs;
    internal ScopeSchedulingPlan Scheduling;
}
```

ScopePlan 只包含：

```text
线程和 Tick 配置
EventCenter / Post / Timer / ECS 的创建参数
ScopeEvent / ScopeCall Inbox 容量
各 Layer 在当前 Scope 的轻量执行切片
```

ScopePlan 不应成为以下内容的权威来源：

```text
Service 的 OwnerLayer
Context 的 OwnerService / OwnerLayer
DI 的 Layer 范围
Tool 的 OwnerLayer
Event Handler 的 LayerIndex
Lifecycle 的管理顺序
```

这些信息必须先存在于 LayerPlan，再投影进 ScopePlan。

---

## 4. ScopeLayerSlice

Scope 运行期允许使用轻量切片：

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

该结构只保存执行所需的下标和范围。

它不表示：

```text
Scope 拥有 Layer
Scope 可以忽略 Layer 管理
Layer 只是一段无意义元数据
```

空 Layer 仍可以保留一个零长度 Slice，以保持 LayerIndex 和顺序稳定；实现也可以通过一个稳定的 Layer→Slice 映射省略空对象，但不能改变顺序语义。

---

## 5. Contribution 的收集顺序

Build 必须先确定 Layer，再确定 Scope。

正确流程：

```text
1. LayersBuilder.Push 收集 Layer。
2. 按 Push 顺序分配 LayerIndex。
3. 执行每个 Layer 的原 Service / Context / Tool / Handler 配置流程。
4. 每个 Contribution 记录 OwnerLayerIndex。
5. 根据 [Scope<TScope>] 或现有 Scope 元数据解析 OwnerScopeId。
6. 将 Contribution 写入对应 LayerPlan 的 ScopeContribution。
7. 从所有 LayerPlan 投影各 ScopeExecutionPlan。
8. Freeze。
```

错误流程：

```text
先收集 Scope
    → 给 Scope 添加 Service / Context
    → 最后把 Layer 当标签附加上去
```

---

## 6. Service / Context Contribution

Service Contribution 至少保留：

```csharp
internal readonly struct ServiceContribution
{
    internal readonly int OwnerLayerIndex;
    internal readonly int OwnerScopeId;

    internal readonly RuntimeTypeHandle
        ServiceType;

    internal readonly ServiceFactory Factory;
}
```

Context Contribution 至少保留：

```csharp
internal readonly struct ContextContribution
{
    internal readonly int OwnerLayerIndex;
    internal readonly int OwnerScopeId;

    internal readonly int OwnerServiceContribution;

    internal readonly RuntimeTypeHandle
        ContextType;

    internal readonly ContextFactory Factory;
}
```

完整规则：

```text
Layer：
    管理 Service / Context 的注册、DI、Mount、Provide/From 和生命周期。

Scope：
    决定这些实例在哪个执行域创建和运行。
```

禁止：

```text
Service 只有 OwnerScope，没有 OwnerLayer
Context 只跟随 Scope，不跟随 Layer/Service
ScopePlan 自行决定业务对象归属
```

---

## 7. DI Plan 的 Layer-first 关系

DI 计划必须按：

```text
LayerIndex
    → ScopeId
    → LayerProviderPlan
```

组织。

等价的运行访问可以是：

```text
ScopeRuntime.LayerProviders[LayerIndex]
```

但 Build 权威关系仍然是：

```text
LayerBuildPlan
    → 对应 Scope 的 LayerProviderPlan
```

这保证：

```text
同 Scope、同 Layer：
    可以 DI。

同 Scope、不同 Layer：
    不能 DI，使用本地 Call。

不同 Scope：
    使用 ScopeEvent / ScopeCall。
```

详细实现由 08 号文档负责。

---

## 8. Event Handler 计划边界

05 号文档不创建新的 Handler Range、Handler Entry 或第二套 Event Dispatcher。

Build 只需保证：

```text
Handler Instance 有 OwnerScopeId
Handler Instance 有 OwnerLayerIndex
Scope Activate 按 Push LayerIndex 执行 faster 原注册流程
```

注册完成后，Handler 已经存在于该 Scope 的原 EventCenter。

详细实现由 17 号文档负责。

---

## 9. Call 计划边界

本地 Call：

```text
ScopeId
+ RequestType
+ ResponseType
→ 当前 Scope 唯一 Handler
```

本地 Call 不以 Layer 寻址。

但是 Call Handler 的 Contribution 仍必须记录：

```text
OwnerLayerIndex
OwnerScopeId
OwnerService / Object
```

用途仅是：

```text
实例创建
DI 定位
生命周期
诊断
冲突报告
```

不得把 `OwnerLayerIndex` 放回本地 Call 的公开地址。

跨 Scope Call 使用独立 ScopeCall Transport，由 03 号文档负责。

---

## 10. Tool、Query、Snap 与 Actor Contribution

这些 Contribution 同样先归属 Layer，再声明 Scope：

```text
OwnerLayerIndex
+ OwnerScopeId
+ Factory / Invoker / Metadata
```

其中：

```text
Tool：
    保持 Layer 管理范围，实例按 Scope 隔离。

ECS Query：
    执行于 OwnerScope EcsWorld，但来源和诊断仍记录 OwnerLayer。

Snap：
    Node 在 OwnerScope 执行，生命周期仍受 OwnerLayer 管理。

Actor：
    CustomScope 只产生 ScopeEvent / ScopeCall；
    ActorWorld 仍由 MainScope 唯一写入和推进。
```

本文不重新设计这些模块。

---

## 11. 稳定下标分配

Build 阶段确定：

```text
LayerIndex：
    Push 顺序

ScopeId：
    现有 Scope Catalog / 配置的稳定顺序

ProviderSlot：
    当前 Scope 中按 LayerIndex稳定映射

ServiceSlot：
    当前 Layer × Scope Provider 内稳定分配

ContextSlot：
    当前 Layer × Scope Provider 内跟随 OwnerService

ToolSlot：
    当前 Layer × Scope 内稳定分配

Lifecycle Range：
    当前 Scope 内按 LayerIndex拼接

LocalCallId：
    当前 Scope 内按 Request/Response 稳定分配

ScopeEvent / ScopeCall RouteId：
    使用现有稳定 Route 规则
```

Running 阶段不得：

```text
根据 Type 查 Layer
根据 Type 查 Scope
重新计算 LayerIndex
重新计算 Range
遍历 Dictionary 确定执行顺序
```

---

## 12. Build 与 Running 分期

### Build 冷路径允许

```text
Dictionary
List
HashSet
受控反射
稳定排序
冲突检查
拓扑分析
容量计算
```

### Freeze 后

```text
释放临时 Contribution 集合
冻结 LayerPlan / ScopeExecutionPlan
输出连续数组、Slot、Offset、Count、生成式 Factory/Invoker
```

### Running 禁止

```text
程序集扫描
Type→Layer 查询
Type→Service 热查询
重新组合 ScopePlan
动态增加 Layer / Scope / Handler
按 Layer 对象树递归 Tick
```

---

## 13. Generator 职责

优先生成：

```text
Service / Context Factory
Mount Setter
Provide Getter / From Setter
Event AutoBind
LocalCall Invoker
ScopeEvent / ScopeCall Bridge
ECS Query Bridge
Tool Factory
Snap Invoker
AOT 泛型闭包
```

Build 冷路径处理：

```text
Push 顺序
Contribution 合并
Scope 分区
稳定下标
冲突检查
拓扑审计
容量规划
```

禁止：

```text
ModuleInitializer
运行期 Roslyn
运行期 SyntaxTree
Reflection.Emit
DynamicMethod
依赖程序集加载顺序的自动注册
```

---

## 14. 业务场景

Layer 顺序：

```text
0 FoundationLayer
1 GameplayLayer
2 PresentationLayer
```

Contribution：

```text
FoundationLayer:
    MainScope ClockService
    CombatScope SimulationClock

GameplayLayer:
    MainScope PreviewCombatService
    CombatScope CombatService
    PathfindingScope PathfindingService

PresentationLayer:
    MainScope CombatHudService
```

Build：

```text
LayerPlan[0] FoundationLayer
    ScopeContribution(MainScope)
    ScopeContribution(CombatScope)

LayerPlan[1] GameplayLayer
    ScopeContribution(MainScope)
    ScopeContribution(CombatScope)
    ScopeContribution(PathfindingScope)

LayerPlan[2] PresentationLayer
    ScopeContribution(MainScope)
```

投影后的 PathfindingScope：

```text
ScopeLayerSlice[0]:
    FoundationLayer，空

ScopeLayerSlice[1]:
    GameplayLayer，含 PathfindingService

ScopeLayerSlice[2]:
    PresentationLayer，空
```

PathfindingScope 运行时只执行 GameplayLayer 的非空范围，但其业务归属仍然是：

```text
GameplayLayer
    → PathfindingScope execution slice
```

而不是：

```text
PathfindingScope
    → 独立拥有 PathfindingService
```

---

## 15. faster 分支复用

### 15.1 直接复用

```text
LayerBase/Application/LayerRuntime.cs
    LayersBuilder.Push / Build 顺序

LayerBase/Layer/Layer.cs
    ConfigureServices
    ServiceCollection
    BuildAutoBinding
    Lifecycle 收集
    RouteIndex / Layer 顺序语义

LayerBase/Scope/ScopeOption*.cs
    Scope 配置外形

ScopeRef Post/Call Generator
ScopeEvent / ScopeCall Route 生成
现有稳定 ID 测试
```

### 15.2 修改后复用

```text
ScopeRuntimePlanner / ScopeCompositionBuilder：
    保留 Scope 发现、ScopeOption、Route 和容量规划；
    输入改为 LayerPlan Contribution；
    输出 ScopeExecutionPlan。

ScopeCompositionPlan：
    从 Scope-first 改为 LayerPlan[] + Derived ScopeExecutionPlan[]。

GeneratedScopeCatalog：
    保存静态 Contribution；
    每个业务 Contribution 必须包含 OwnerLayer 信息。
```

### 15.3 禁止整体移植

```text
ScopePlan 直接拥有全部业务对象的 Scope-first 结构

AddService<TScope,TService> 作为业务注册入口

运行期 Assembly 扫描

Build 阶段创建 Worker Service

为每个 Scope 无条件复制完整 Layer 对象树

ScopeSubscriptionPlan 作为第二套 EventCenter
```

---

## 16. Agent 执行任务

```text
1. 保留 LayersBuilder.Push 作为唯一 Layer 顺序来源。
2. 删除绕过 Layer 的 AddService<TScope,...> / AddContext<TScope,...> 设计。
3. 定义或修正 LayerBuildPlan[]。
4. 所有 Service/Context/Tool/Handler Contribution 增加 OwnerLayerIndex。
5. Contribution 先写入 LayerPlan，再按 OwnerScope 投影。
6. ScopeExecutionPlan 只保存执行资源和 ScopeLayerSlice。
7. DI Plan 改为 LayerIndex → ScopeId → LayerProviderPlan。
8. Lifecycle Range 按 LayerIndex拼接。
9. Event Handler 只提供 OwnerLayer/OwnerScope，不生成第二套派发计划。
10. LocalCall 仍按 Scope + Request/Response 寻址。
11. LocalCall Handler 保留 OwnerLayer 仅用于实例和生命周期。
12. Build 确定所有 Slot/Offset/Count。
13. Freeze 后释放临时集合。
14. Running 不查询 Layer 类型或遍历 Layer 树。
15. 保留 MainScope Push Layer 实例。
16. CustomScope 只在 Layer 本身确实需要运行对象时创建必要目标。
17. 更新所有 Scope-first Composition 测试。
18. 运行 faster 原 Build、Layer 顺序和 Generator 测试。
```

---

## 17. 必须测试

```text
Layer_index_follows_push_order

Composition_is_layer_first

Scope_execution_plan_is_derived_from_layer_plans

Service_contribution_requires_owner_layer

Context_follows_owner_service_layer

Same_layer_can_contribute_to_multiple_scopes

Custom_scope_service_still_belongs_to_original_layer

Empty_layer_slice_preserves_layer_order

Scope_plan_does_not_become_business_ownership_root

No_add_service_scope_api_bypasses_layer

Layer_provider_plan_is_partitioned_by_scope

Lifecycle_ranges_are_concatenated_by_layer_order

Event_registration_receives_push_layer_index

Local_call_address_does_not_include_layer

Running_does_not_query_layer_type

Running_does_not_traverse_layer_object_tree

Build_releases_temporary_contribution_collections

Stable_ids_do_not_depend_on_dictionary_order

IL2CPP_generated_factories_are_reachable
```

---

## 18. 验收否决项

出现以下任意一项，任务不通过：

```text
RuntimeCompositionPlan 只有 ScopePlan[]，没有 LayerPlan[]

Service / Context 只有 OwnerScope，没有 OwnerLayer

ScopePlan 直接决定 DI、Mount、Tool 和 Lifecycle 的业务归属

CustomScope 被描述为“没有 Layer”

AddService<TScope,TService> 绕过 Layer 注册

LayerIndex 不是 Push 顺序

运行期通过 Type 查找 Layer

运行期递归遍历 Layer 树

为了 Layer-first 强制为每个 Scope 创建完整 Layer 对象树

EventCenter 迁移又创建第二套 Handler Plan

本地 Call 再次以 Layer 作为公开地址

Build 阶段创建 Worker Service

运行期程序集扫描或 JIT 生成
```

---

## 19. 本阶段不修改的内容

本文不修改：

```text
DI 生命周期和解析算法
Mount / Provide / From 具体绑定
EventCenter 内部派发
本地 Call Invoker
ScopeEvent / ScopeCall MPSC
ECS Scheduler
ActorWorld
WorkerEventJob
Scope Tick 的具体执行算法
```

本文只保证：

```text
Layer 是业务管理上层。

Scope 是执行与资源隔离维度。

Build 从 Layer 结构投影 Scope 的轻量运行布局，
而不是把 Scope 变成业务对象组织根。
```
