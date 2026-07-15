# 06 Layer-first AssemblyModule 静态贡献组合

> **强制执行规范：** 本文必须遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`；冲突时以 00、01 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `05_scope_static_composition_generators_revised.md`  
> **文档性质：** 独立构建期任务。AssemblyModule 只提供跨程序集的不可变静态 Contribution，不进入 Runtime 热路径，不拥有 Layer、Scope、线程、实例或生命周期。

---

## 0. 本阶段核心目的

AssemblyModule 解决：

```text
不同程序集分别生成静态元数据
    → 应用显式安装模块
    → 合并 Contribution
    → 将每条业务 Contribution 绑定到已 Push 的 OwnerLayer
    → 写入 LayerBuildPlan[]
    → 再投影 ScopeExecutionPlan[]
```

最终关系：

```text
LayersBuilder.Push
    → 决定 LayerIndex 和 Layer 顺序

AssemblyModuleManifest
    → 描述某程序集贡献了什么
    → 每条业务 Contribution 必须声明 OwnerLayerType
    → 可声明 OwnerScopeType

AssemblyModuleComposer
    → 合并 Manifest
    → 解析 OwnerLayerType 到 Push LayerIndex
    → 写入 LayerBuildPlan
```

禁止：

```text
AssemblyModule 自动 Push Layer
ScopeCompositionBuilder 先按 Scope组织 Service
业务 Contribution 只有 OwnerScope、没有 OwnerLayer
模块 Dispatcher 进入 Running
```

---

## 1. 最终公有 API

```csharp
public interface IAssemblyModule
{
    AssemblyModuleId Id {
        get;
    }

    AssemblyModuleManifest Manifest {
        get;
    }
}
```

显式安装：

```csharp
LayerRuntime runtime = LayerHub.CreateLayers()
    .Push(new FoundationLayer())
    .Push(new GameplayLayer())
    .Push(new PresentationLayer())
    .AddAssemblyModule(
        CoreModule.Instance)
    .AddAssemblyModule(
        GameplayModule.Instance)
    .Build();
```

模块安装顺序不决定：

```text
LayerIndex
ScopeId
RouteId
ServiceSlot
```

LayerIndex 永远由 `Push` 决定。

禁止 API：

```text
module.InstallRuntime(runtime)
module.Start()
module.Stop()
module.Dispose()

builder.AddLayerFromModule(...)
builder.AutoPushModuleLayers(...)
```

对于无生成器程序集，可以保留受控冷路径：

```csharp
builder.AddAssemblyModule(
    ReflectionAssemblyModule.Build(
        typeof(GameplayMarker).Assembly));
```

该 API只扫描显式传入的 Assembly，并输出同一个 Manifest；Running 不保留反射对象或扫描结果。

---

## 2. AssemblyModule 的架构位置

```text
Gameplay.dll
    → Generated GameplayModule.Manifest

Rendering.dll
    → Generated RenderingModule.Manifest

LayerHub Builder
    → Push Layer[]
    → AddAssemblyModule[]
    → AssemblyModuleComposer
    → LayerBuildPlan[]
    → ScopeExecutionPlan[]
```

AssemblyModule 不是：

```text
插件运行容器
ServiceProvider
ScopeRuntime
LayerRuntime 子节点
热插拔单元
Handler Dispatcher
```

模块实例必须是无状态、不可变的 Manifest 访问器。

---

## 3. Layer Contract 与 Push 的关系

Manifest 可以声明：

```text
本程序集认识哪些 Layer Type
Contribution 期望属于哪个 Layer Type
```

但不能创建 Layer 顺序。

```csharp
public readonly struct
    LayerContractContribution
{
    public RuntimeTypeHandle LayerType {
        get;
    }

    public SourceLocation Location {
        get;
    }
}
```

Build 必须验证：

```text
每个业务 Contribution 的 OwnerLayerType
    → 在 LayersBuilder.Push 中恰好存在一次
    → 解析为稳定 LayerIndex
```

如果模块贡献指向未 Push Layer：

```text
Build Error：
    Owner Layer was not pushed.
```

不能：

```text
自动 Push
自动放入默认 Layer
自动放入 MainScope
将 LayerType 排序后生成新的 LayerIndex
```

---

## 4. Manifest 结构

```csharp
public sealed class
    AssemblyModuleManifest
{
    public AssemblyModuleId ModuleId {
        get;
    }

    public LayerContractContribution[]
        Layers {
        get;
    }

    public ScopeDefinitionContribution[]
        Scopes {
        get;
    }

    public ScopeOptionContribution[]
        ScopeOptions {
        get;
    }

    public ScopeEventContractContribution[]
        Events {
        get;
    }

    public ScopeCallContractContribution[]
        Calls {
        get;
    }

    public ServiceFactoryContribution[]
        Services {
        get;
    }

    public ContextFactoryContribution[]
        Contexts {
        get;
    }

    public EventHandlerContribution[]
        EventHandlers {
        get;
    }

    public LocalCallHandlerContribution[]
        LocalCallHandlers {
        get;
    }

    public ScopeEventBridgeContribution[]
        ScopeEventBridges {
        get;
    }

    public ScopeCallBridgeContribution[]
        ScopeCallBridges {
        get;
    }

    public MountContribution[]
        Mounts {
        get;
    }

    public ProvideContribution[]
        Providers {
        get;
    }

    public FromContribution[]
        Consumers {
        get;
    }

    public LayerToolContribution[]
        Tools {
        get;
    }

    public EcsQueryContribution[]
        Queries {
        get;
    }

    public SnapContribution[]
        Snaps {
        get;
    }
}
```

实际类型名应优先复用 `faster`。

Manifest 禁止保存：

```text
Layer 实例
Service / Context 实例
ScopeRuntime
LayerRuntime
ScopeWorker
Thread
ActorWorld
RuntimeId / Generation
可变 Registry
运行时 Cache
```

---

## 5. Contribution 的共同地址

所有业务 Contribution 至少包含：

```text
OwnerLayerType
OwnerScopeType
SourceLocation
```

Build 后转换为：

```text
OwnerLayerIndex
OwnerScopeId
```

### 5.1 Service

```csharp
public readonly struct
    ServiceFactoryContribution
{
    public RuntimeTypeHandle
        ContractType {
        get;
    }

    public RuntimeTypeHandle
        ImplementationType {
        get;
    }

    public RuntimeTypeHandle
        OwnerLayerType {
        get;
    }

    public RuntimeTypeHandle
        OwnerScopeType {
        get;
    }

    public ServiceLifetime Lifetime {
        get;
    }

    public int RegistrationScopeId {
        get;
    }

    public ServiceFactory Factory {
        get;
    }
}
```

禁止使用：

```text
OwnerLayerTypes[]
```

一个 Service Contribution 只能有一个 OwnerLayer。

同一个 Service 类型确实需要在多个 Layer 注册时，必须生成多条显式 Contribution，并分别进行冲突检查。

### 5.2 Context

```csharp
public readonly struct
    ContextFactoryContribution
{
    public RuntimeTypeHandle
        ContextType {
        get;
    }

    public RuntimeTypeHandle
        OwnerServiceType {
        get;
    }

    public RuntimeTypeHandle
        OwnerLayerType {
        get;
    }

    public RuntimeTypeHandle
        OwnerScopeType {
        get;
    }

    public ContextFactory Factory {
        get;
    }
}
```

Build 验证：

```text
Context.OwnerLayer == OwnerService.OwnerLayer
Context.OwnerScope == OwnerService.OwnerScope
```

不得自动修正不一致归属。

### 5.3 Event Handler

```csharp
public readonly struct
    EventHandlerContribution
{
    public RuntimeTypeHandle
        EventType {
        get;
    }

    public RuntimeTypeHandle
        OwnerServiceType {
        get;
    }

    public RuntimeTypeHandle
        OwnerLayerType {
        get;
    }

    public RuntimeTypeHandle
        OwnerScopeType {
        get;
    }

    public EventHandlerInvoker Invoker {
        get;
    }
}
```

Build 只解析 OwnerLayerIndex / OwnerScopeId。

Scope Activate 按 Push LayerIndex执行 `faster` 原 AutoBind。

禁止生成：

```text
Module Handler Dispatcher
ScopeSubscriptionPlan
ObjectSlot Event Dispatcher
```

### 5.4 Local Call Handler

```csharp
public readonly struct
    LocalCallHandlerContribution
{
    public RuntimeTypeHandle
        RequestType {
        get;
    }

    public RuntimeTypeHandle
        ResponseType {
        get;
    }

    public RuntimeTypeHandle
        OwnerServiceType {
        get;
    }

    public RuntimeTypeHandle
        OwnerLayerType {
        get;
    }

    public RuntimeTypeHandle
        OwnerScopeType {
        get;
    }

    public LocalCallInvoker Invoker {
        get;
    }
}
```

唯一性：

```text
OwnerScopeId
+ RequestType
+ ResponseType
```

Layer 不参与本地 Call 地址，只用于实例、DI、生命周期和诊断。

不同 Scope 可以拥有相同 Request/Response Handler。

---

## 6. Scope 定义 Contribution

模块可以贡献：

```text
ScopeDefinition
ScopeOption Template
ScopeEvent Contract
ScopeCall Contract
```

这些是执行域和 Transport 元数据，不需要 OwnerLayer。

但是：

```text
Service
Context
Handler
Mount
Provide / From
Tool
Query
Snap
```

必须有 OwnerLayer。

Scope Definition 冲突：

```text
同一 ScopeType 多个不一致定义
    → Build Error
```

模块不得通过静态构造写全局 ScopeOption Registry。

---

## 7. 合并流程

正确流程：

```text
1. LayersBuilder.Push 完成 Layer 收集。
2. 按 Push 顺序分配 LayerIndex。
3. 校验 ModuleId 唯一。
4. 合并所有 Manifest Contribution。
5. 对模块 Contribution 做稳定排序。
6. 解析 OwnerLayerType → LayerIndex。
7. 解析 OwnerScopeType → ScopeId。
8. 将业务 Contribution写入对应 LayerBuildPlan。
9. 在 Layer 内完成 DI/Mount/Provide/Tool/Handler/Lifecycle冲突检查。
10. 从 LayerBuildPlan投影 ScopeExecutionPlan。
11. 分配 RouteId / Slot / Offset / Count。
12. Freeze 并释放临时集合。
```

错误流程：

```text
先按 Scope收集 Service
    → 生成 ScopePlan
    → 最后附加 LayerType 标签
```

---

## 8. 模块安装顺序与确定性

以下两种顺序必须得到等价计划：

```csharp
builder
    .AddAssemblyModule(A.Instance)
    .AddAssemblyModule(B.Instance);
```

```csharp
builder
    .AddAssemblyModule(B.Instance)
    .AddAssemblyModule(A.Instance);
```

确定性排序键必须使用：

```text
ModuleId
OwnerLayerType FullName
OwnerScopeType FullName
Contribution Kind
Contract / Implementation FullName
Source StableOrder
```

不得使用：

```text
Runtime HashCode
程序集加载顺序
Dictionary 枚举顺序
随机 Guid
模块实例地址
```

LayerIndex 不参与模块排序生成；它由 Push 解析得出。

---

## 9. 跨程序集关系

跨程序集可以建立：

```text
模块 B 的 Service 属于模块 A 声明、应用已 Push 的 Layer
模块 B 的 Context 归属于模块 A 的 Service
模块 B 的 Handler 处理模块 A 的 Event
模块 B 的 LocalCall Handler 处理模块 A 的 Request
模块 B 的 Tool 使用模块 A 的 Contract
```

但必须继续满足：

```text
DI / Mount / Provide / From：
    同 Scope、同 Layer

本地 Call：
    同 Scope，可跨 Layer

ScopeEvent / ScopeCall：
    显式跨 Scope
```

程序集边界不能绕过 Layer 或 Scope 边界。

---

## 10. DI、Mount、Provide / From Contribution

### 10.1 DI

Service Contribution 先写入：

```text
LayerBuildPlan
    → OwnerScope 的 LayerProviderPlan
```

不能直接写入：

```text
ScopeServiceProvider
```

### 10.2 Mount

Mount Contribution 必须包含：

```text
OwnerLayerType
OwnerScopeType
OwnerServiceType
TargetContract
```

Build 只在相同：

```text
LayerIndex
ScopeId
```

内解析。

### 10.3 Provide / From

Provide：

```text
OwnerLayer
OwnerScope
ProviderServiceType
LocalKey
```

From：

```text
OwnerLayer
OwnerScope
ConsumerServiceType
ProviderServiceType
LocalKey
RequestedType
```

跨程序集不改变唯一键。

---

## 11. LayerTool Contribution

```csharp
public readonly struct
    LayerToolContribution
{
    public RuntimeTypeHandle
        OwnerLayerType {
        get;
    }

    public RuntimeTypeHandle
        OwnerScopeType {
        get;
    }

    public RuntimeTypeHandle
        ContractType {
        get;
    }

    public string LocalKey {
        get;
    }

    public LayerToolFactory Factory {
        get;
    }
}
```

唯一键：

```text
LayerIndex
+ ScopeId
+ ContractType
+ LocalKey
```

AssemblyModule 不创建 Tool Registry 或实例。

---

## 12. ECS Query 与 Snap Contribution

ECS Query：

```text
OwnerLayerType
OwnerScopeType
OwnerServiceType
Query Metadata
Generated Invoker
```

Query 在 OwnerScope EcsWorld执行，但生命周期和诊断仍归 OwnerLayer。

Snap：

```text
OwnerLayerType
OwnerScopeType
OwnerObjectType
Generated Invoker
```

Module 只携带元数据，不创建 Query Owner、Lease、Scheduler 或 Snap Runtime。

---

## 13. 为什么不保留 Module Dispatcher

旧路径：

```text
Route
    → ModuleSlot
    → Module Dispatcher
    → LocalHandlerId
    → Handler
```

最终路径：

```text
Build：
    Module Contribution
        → LayerBuildPlan
        → ScopeExecutionPlan

Running：
    RouteId / LocalCallId
        → Generated Invoker
        → ObjectSlot
```

原因：

```text
AssemblyModule 只属于 Build。
ModuleSlot 不应进入热路径。
Handler 的 Owner 已由 Layer/Scope/ObjectSlot确定。
删除一层分派和生命周期耦合。
```

---

## 14. 反射 Module Builder

允许：

```text
显式指定 Assembly
Build 冷路径
受控扫描
输出标准 Manifest
```

禁止：

```text
AppDomain.GetAssemblies 全量扫描
自动扫描引用程序集
Running 扫描
静态构造自动注册
反射 Module 保留 Service/MethodInfo 热调用
```

反射 Builder 输出的 Factory/Invoker必须在 Freeze 前转换为 AOT 可达路径；IL2CPP 产品路径优先生成器。

---

## 15. 冲突规则

### 15.1 必须唯一

```text
ModuleId
ScopeDefinition：ScopeType
ScopeOption：ScopeType
Service Contribution：OwnerLayer + OwnerScope + Contract
Context Contribution：OwnerLayer + OwnerScope + ContextType
Tool：OwnerLayer + OwnerScope + Contract + Key
```

### 15.2 Local Call Handler

```text
ScopeId + Request + Response
```

必须唯一。

不是：

```text
完整 Runtime 中 Request 只能有一个 Handler
```

### 15.3 Event Handler

同一 Event 可以有多个 Handler。

注册顺序：

```text
Push LayerIndex
    → faster 原 Layer 内注册顺序
```

不得按：

```text
ScopeId → ServiceSlot
模块安装顺序
Handler 类型名
```

重新定义 EventCenter 派发顺序。

### 15.4 Override

Service Override 只允许在显式规则下发生，并且必须限定：

```text
同 OwnerLayer
同 OwnerScope
同 Contract
```

不能跨 Layer覆盖。

---

## 16. AssemblyModuleComposer

```csharp
internal static class
    AssemblyModuleComposer
{
    internal static
        CompositionContributions Compose(
            ReadOnlySpan<IAssemblyModule>
                modules);
}
```

输出仍然只是构建期 Contribution：

```csharp
internal sealed class
    CompositionContributions
{
    internal AssemblyModuleId[]
        Modules;

    internal LayerContractContribution[]
        Layers;

    internal ScopeDefinitionContribution[]
        Scopes;

    internal ServiceFactoryContribution[]
        Services;

    internal ContextFactoryContribution[]
        Contexts;

    internal EventHandlerContribution[]
        EventHandlers;

    internal LocalCallHandlerContribution[]
        LocalCallHandlers;

    internal MountContribution[]
        Mounts;

    internal ProvideContribution[]
        Providers;

    internal FromContribution[]
        Consumers;

    internal LayerToolContribution[]
        Tools;

    internal EcsQueryContribution[]
        Queries;

    internal SnapContribution[]
        Snaps;
}
```

下一步必须是：

```text
Layer-first CompositionBuilder
```

而不是 Scope-first Builder。

---

## 17. Activate 与失败边界

AssemblyModuleComposer 不创建任何业务实例。

如果：

```text
模块合并失败
```

只释放 Build 临时集合。

如果：

```text
Scope Activate失败
```

由 04 号生命周期协议在目标 Owner Thread回滚。

模块对象不参与：

```text
Activate
Stop
Dispose
Fault
```

同一不可变 Manifest 可以构建多个隔离 Runtime。

---

## 18. Generator 职责

`AssemblyModuleGenerator` 负责：

```text
发现显式 [AssemblyModule] 类型
生成无状态 Module 单例
生成不可变 Manifest
生成 Service / Context Factory
生成 Mount / Provide / From Setter
生成 Event AutoBind Contribution
生成 LocalCall Invoker
生成 ScopeEvent / ScopeCall Bridge
生成 ECS Query / Tool / Snap Contribution
生成 SourceLocation
```

不负责：

```text
Push Layer
分配 LayerIndex
分配 ScopeId / RouteId / Slot
创建 ScopeRuntime
调用 Factory
静态 Replay ScopeOption
扫描引用程序集
```

---

## 19. faster 分支复用

### 19.1 直接复用

```text
AssemblyModuleAttribute
ModuleIgnoreAttribute
Manifest 分区思想
Incremental Generator 管线
Contribution Factory / Invoker
模块重复和冲突测试
稳定排序算法
多 Runtime 隔离测试
```

### 19.2 修改后复用

```text
ModuleManifest：
    每条业务 Contribution增加唯一 OwnerLayerType；
    保留 OwnerScopeType；
    不保存 Runtime对象。

ModuleRuntimeBuilder：
    收缩为 AssemblyModuleComposer；
    输出 Layer-first CompositionContributions。

AssemblyModuleGenerator：
    保留发现和 Factory/Invoker；
    删除全局 ScopeOption Replay；
    删除跨 Scope Resource Export/Import；
    直接输出 Handler/Call Invoker Contribution。
```

### 19.3 禁止移植

```text
ModuleSlot + Module Dispatcher热路径
ServiceBindingInitializer(IService, ScopeRuntime, ...)
ScopeOptionRegistry Replay
ModuleRuntimePlan 保存实例数组
模块静态构造自动注册
Scope-first Service Composition
运行期程序集扫描
```

---

## 20. 需要修改的代码位置

优先检查：

```text
LayerBase/Modules/
    AssemblyModuleAttribute.cs
    ModuleManifest.cs
    ModuleRuntimeBuilder.cs
    ModuleRuntimeCatalog.cs

LayerBase.Generator/
    AssemblyModuleGenerator.cs

LayerBase/Application/
    LayersBuilder.cs
    LayerRuntimeBuilder.cs

LayerBase/Scope/
    ScopeCompositionBuilder.cs
    ScopeCompositionPlan.cs
```

目标命名可以沿用现有文件，避免无必要的大规模重命名。

关键是语义变为：

```text
Manifest Contribution
    → OwnerLayer resolution
    → LayerBuildPlan
    → Scope projection
```

---

## 21. Agent 执行任务

```text
1. 保留显式 AddAssemblyModule。
2. 保留 Push 作为唯一 LayerIndex来源。
3. 每条业务 Contribution增加唯一 OwnerLayerType。
4. 删除 OwnerLayerTypes[] 多 Layer模糊归属。
5. 模块不能自动 Push Layer。
6. 未 Push OwnerLayer 在 Build失败。
7. Composer 先解析 LayerIndex，再写入 LayerBuildPlan。
8. ScopeExecutionPlan只能从 LayerBuildPlan投影。
9. LocalCall Handler按 Scope + Request/Response唯一。
10. Event Handler按 Push LayerIndex执行原注册。
11. DI/Mount/Provide/From保持同 Scope、同 Layer。
12. Tool 唯一键加入 LayerIndex。
13. 删除 ModuleSlot / Module Dispatcher热路径。
14. 删除 ScopeOption全局 Replay。
15. 删除运行期模块扫描和自动注册。
16. 保留生成式 Factory/Invoker。
17. 更新跨程序集冲突和确定性测试。
18. 运行 faster 原模块与多 Runtime隔离测试。
```

---

## 22. 必须测试

```text
One_assembly_generates_one_manifest

Manifest_contains_factories_not_instances

Business_contribution_requires_owner_layer

Module_cannot_auto_push_layer

Missing_pushed_owner_layer_fails_build

Push_order_is_the_only_layer_index_source

Module_install_order_does_not_change_plan

Contribution_is_written_to_layer_plan_before_scope_projection

Same_layer_can_receive_contributions_from_multiple_modules

Same_module_can_contribute_one_layer_to_multiple_scopes

Context_must_match_owner_service_layer_and_scope

Local_call_uniqueness_is_per_scope

Same_call_can_have_handlers_in_different_scopes

Event_registration_uses_push_layer_index

Cross_module_di_requires_same_layer_and_scope

Cross_module_mount_requires_same_layer_and_scope

Cross_module_from_requires_same_layer_and_scope

Tool_key_contains_layer_and_scope

Module_build_does_not_create_service_or_context

Runtime_has_no_module_dispatcher_hot_path

Same_manifest_builds_multiple_isolated_runtimes

No_global_scope_option_side_effect

No_runtime_assembly_scan
```

---

## 23. 验收否决项

出现以下任意一项，任务不通过：

```text
AssemblyModule 自动 Push Layer

Contribution 只有 OwnerScope，没有 OwnerLayer

OwnerLayerTypes[] 允许一个对象模糊属于多个 Layer

ScopePlan 先于 LayerPlan成为业务组合根

模块安装顺序改变 LayerIndex / ScopeId / RouteId

模块合并阶段创建 Service / Context

本地 Call 被限制为完整 Runtime唯一 Handler

Event Handler 按 ServiceSlot或模块顺序排序

跨程序集 DI/Mount/From 绕过 Layer边界

运行时 Route经过 ModuleSlot Dispatcher

模块静态构造写全局 ScopeOptionRegistry

运行时扫描程序集寻找模块

Manifest 保存 Runtime / Scope / Service实例
```

---

## 24. 本阶段不修改的内容

本文不修改：

```text
Scope Runtime资源
Scope 生命周期协议
DI Provider内部算法
EventCenter派发
LocalCall Registry
ScopeEvent / ScopeCall MPSC
ECS Query运行算法
ActorWorld
```

本文只保证：

```text
AssemblyModule提供静态 Contribution。

每条业务 Contribution先绑定 OwnerLayer。

LayerBuildPlan是组合权威结构。

ScopeExecutionPlan只是从 Layer投影出的执行视图。
```
