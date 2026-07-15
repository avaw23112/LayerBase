# 08 Layer-first DI 与 Scope 实例隔离迁移

> **强制执行规范：** 本文必须遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`；冲突时以 00、01 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `05_scope_static_composition_generators_revised.md`、`06_assembly_module_static_composition_revised.md`  
> **文档性质：** 独立阶段任务。本文只迁移 DI 的业务管理边界、实例隔离和运行查找路径，不重写 EventCenter、Call、Post、ECS 或生命周期接口。

---

## 0. 本阶段核心目的

DI 必须同时满足：

```text
Layer-first：
    Layer 管理 Service 注册和直接依赖范围。

Scope-local：
    每个 Scope 拥有独立的该 Layer Provider实例。
```

最终关系：

```text
LayerBuildPlan[GameplayLayer]
    ├── MainScope LayerProviderPlan
    ├── CombatScope LayerProviderPlan
    └── PathfindingScope LayerProviderPlan
```

运行期可以投影为：

```text
CombatScopeRuntime
    → LayerProviders[GameplayLayerIndex]
```

但该数组只是执行视图。

权威业务关系仍然是：

```text
GameplayLayer
    → CombatScope Provider
    → CombatScope Gameplay Service / Context
```

禁止：

```text
CombatScope
    → 一个 ScopeServiceProvider
    → 可解析所有 Layer
```

---

## 1. DI 范围公理

允许直接解析的必要条件：

```text
相同 RuntimeGeneration
相同 ScopeId
相同 LayerIndex
```

因此：

### 1.1 同 Scope、同 Layer

允许：

```text
构造注入
this.Get<T>()
Service 配置依赖
Context 依赖
Mount
Provide / From
```

### 1.2 同 Scope、不同 Layer

禁止 DI。

使用：

```csharp
Result result =
    await this.Call<
        Request,
        Result>(
            in request);
```

本地 Call 可以跨 Layer，因为地址是当前 Scope，而不是 Layer。

### 1.3 同 Layer、不同 Scope

禁止 DI。

使用：

```text
ScopeEvent
ScopeCall
```

同一个 Layer 在两个 Scope 中的 Provider、Service 和 Context 是不同实例。

### 1.4 不同 Scope、不同 Layer

同样禁止 DI。

---

## 2. 保持不变的公有 API

继续使用原 Layer 服务配置入口。

示例：

```csharp
public sealed class GameplayLayer :
    Layer
{
    public override void ConfigureServices(
        IServiceCollection services)
    {
        services.AddScoped<
            IInventoryRepository,
            InventoryRepository>();

        services.AddScoped<
            ICombatService,
            CombatService>();
    }
}
```

如果 `faster` 的实际注册入口允许 Service 自己继续调用 `ConfigureServices`，同样保持原 API。

业务对象仍使用：

```csharp
IInventoryRepository repository =
    this.Get<
        IInventoryRepository>();
```

构造注入保持：

```csharp
public sealed partial class CombatService :
    ICombatService
{
    public CombatService(
        IInventoryRepository repository,
        CombatConfig config)
    {
    }
}
```

本文不新增：

```text
builder.AddService<TScope,...>
ScopeRef.GetService<T>()
runtime.GetService<T>()
runtime.ServiceProvider
跨 Layer Service Proxy
Scope Service Locator
```

---

## 3. `AddScoped` 的准确语义

`AddScoped` 的 Scope 不是新架构中的 `ScopeRuntime` 全局可见范围。

它继续表达 `faster` 原 DI 中的注册作用域和实例复用语义。

迁移后：

```text
一个具体 Layer
+ 一个具体 Scope实例
+ RegistrationScopeId
→ 一个 Scoped 实例
```

完整地址：

```text
RuntimeGeneration
+ LayerIndex
+ ScopeId
+ RegistrationScopeId
+ ServiceSlot
```

`ScopeId` 用于隔离同一 Layer 在不同执行域中的实例。

`RegistrationScopeId` 继续保留原 ServiceCollection 的 DI 语义，不能被 ScopeId 替代或删除。

---

## 4. Layer Provider 的权威计划

### 4.1 Build 权威结构

```csharp
internal sealed class LayerBuildPlan
{
    internal int LayerIndex;

    internal LayerScopeProviderPlan[]
        ProvidersByScope;
}
```

```csharp
internal readonly struct
    LayerScopeProviderPlan
{
    internal readonly int ScopeId;
    internal readonly int ProviderSlot;

    internal readonly int ServiceStart;
    internal readonly int ServiceCount;

    internal readonly int ContextStart;
    internal readonly int ContextCount;

    internal readonly ServiceFactoryPlan[]
        ServiceFactories;

    internal readonly ContextFactoryPlan[]
        ContextFactories;
}
```

实际实现可使用全局连续数组与 Range，避免每 Plan 分配数组。

关键是：

```text
Provider Plan 先属于 LayerBuildPlan，
再被投影进 ScopeExecutionPlan。
```

### 4.2 Runtime 执行视图

```csharp
internal sealed class ScopeRuntime
{
    internal LayerServiceProvider[]
        LayerProviders;
}
```

数组下标：

```text
LayerIndex 或由 LayerIndex预计算的 ProviderSlot
```

这只是让 OwnerScope 快速访问当前 Layer Provider。

不能因此提供：

```text
ScopeRuntime.GetAnyService<T>()
遍历所有 LayerProvider 的 fallback
```

---

## 5. LayerServiceProvider

```csharp
internal sealed class
    LayerServiceProvider
{
    private object?[] _instances;
    private ServiceFactory[] _factories;
    private byte[] _states;

    internal T Get<T>(
        int serviceSlot);
}
```

必须优先复用 `faster` 的：

```text
ServiceDescriptor
ServiceLifetime
RegistrationScopeId
构造函数选择
循环依赖诊断
成员注入语义
Dispose 顺序
错误信息
```

本阶段只修改：

```text
Provider Owner：
    Runtime / Layer
        → Layer × Scope实例

依赖查找：
    Root fallback
        → 当前 LayerProvider Slot

创建线程：
    Build / MainThread
        → OwnerScope Thread Activate
```

---

## 6. 生命周期语义

所有通过 Layer 注册的生命周期都被当前 `LayerServiceProvider` 限定。

### 6.1 Singleton

```text
一个 Layer × 一个 Scope Provider实例内一个实例。
```

Singleton 表示当前 Provider 内的单例，不赋予跨 Layer或跨 Scope可见性。

### 6.2 Scoped

```text
一个 Layer × 一个 Scope Provider
× RegistrationScopeId
内一个实例。
```

### 6.3 Transient

```text
每次由当前 LayerProvider解析时创建。
```

可释放 Transient 由创建它的 Provider追踪，并在 Provider Dispose 时逆序释放。

### 6.4 Instance

外部实例必须绑定到唯一：

```text
RuntimeGeneration
LayerIndex
ScopeId
ProviderSlot
```

同一个可变 Instance 不得注册给：

```text
多个 Layer
多个 Scope
多个 Runtime
```

---

## 7. ScopeObjectBinding

每个 Layer 运行目标、Service 和 Context 必须绑定：

```csharp
internal readonly struct
    ScopeObjectBinding
{
    internal readonly int RuntimeId;
    internal readonly int RuntimeGeneration;

    internal readonly int LayerIndex;
    internal readonly int ScopeId;

    internal readonly int ProviderSlot;
    internal readonly int ServiceSlot;
    internal readonly int ContextSlot;
    internal readonly int ObjectSlot;

    internal readonly IScopeLocalAccess
        LocalAccess;

    internal readonly ScopeEndpoint
        Endpoint;
}
```

用途：

```text
LayerIndex / ProviderSlot：
    DI / Mount / Provide / From / Tool 范围。

ScopeId / LocalAccess：
    Event / Post / Timer / ECS / LocalCall。

Endpoint：
    显式 ScopeEvent / ScopeCall。
```

Binding 不允许暴露：

```text
其他 LayerProvider
其他 ScopeRuntime
Runtime Root Provider
ActorWorld
```

---

## 8. `this.Get<T>()` 路由

```csharp
public static T Get<T>(
    this IService owner)
    where T : class
{
    ScopeObjectBinding binding =
        ScopeObjectBinder.Get(
            owner);

    binding.LocalAccess
        .RequireOwnerThread();

    LayerServiceProvider provider =
        binding.LocalAccess
            .GetLayerProvider(
                binding.ProviderSlot);

    return provider.Get<T>(
        GeneratedServiceSlot<T>.For(
            binding.LayerIndex));
}
```

示例只表达路由。

热路径最终必须是：

```text
Binding
    → ProviderSlot
    → ServiceSlot
    → Instance Array
```

不得：

```text
按 Type 遍历 Scope 中全部 Provider
回退 MainScope
回退 Runtime Root
搜索其他 Layer
使用 Dictionary<Type, object> 热查找
```

---

## 9. 构造函数注入

构造 `GameplayLayer × CombatScope` 的 Service：

```text
GameplayLayer CombatScope Provider
    → 选择 faster 原构造函数
    → 每个参数映射当前 Provider ServiceSlot
    → 创建
```

如果参数只存在于：

```text
同 Scope 的其他 Layer
同 Layer 的其他 Scope
```

Build 失败。

错误示例：

```text
Cross-layer constructor dependency is not allowed.

Consumer:
    GameplayLayer / CombatScope / CombatService

Dependency:
    PresentationLayer / CombatScope / CombatHudService

Use:
    this.Call<Request,Response>()
```

跨 Scope错误：

```text
Cross-scope constructor dependency is not allowed.
Use ScopeEvent or ScopeCall.
```

不得自动：

```text
注入 ScopeRef
创建 Proxy
注入 Lazy Service Locator
选择第一个同类型实例
```

---

## 10. Build 阶段

正确流程：

```text
1. LayersBuilder.Push 分配 LayerIndex。
2. 每个 Layer执行原 ServiceCollection配置。
3. ServiceDescriptor记录 OwnerLayerIndex。
4. 解析每个 Service 的 OwnerScopeId。
5. 在 LayerBuildPlan 内按 ScopeId分区 Descriptor。
6. 为每个 Layer × Scope生成 LayerScopeProviderPlan。
7. 分配 RegistrationScopeId / ServiceSlot / ContextSlot。
8. 生成 Constructor Invoker / Factory / Member Setter。
9. 验证依赖仅在同 Layer、同 Scope。
10. 将 ProviderSlot投影到 ScopeExecutionPlan。
11. Freeze。
```

Build 冷路径允许：

```text
Dictionary
List
HashSet
受控反射
拓扑排序
```

Running 不允许使用这些结构做解析。

---

## 11. Build 验证

必须验证：

```text
Service Contribution 有唯一 OwnerLayer和 OwnerScope

构造依赖存在于同 LayerProvider

Context 与 OwnerService：
    同 Layer
    同 Scope

Mount：
    同 Layer
    同 Scope

Provide / From：
    同 Layer
    同 Scope
    From 显式 ProviderServiceType

Tool：
    同 Layer
    同 Scope

同 Provider内 Contract无歧义

循环构造依赖被拒绝

Instance没有跨 Provider复用
```

Call Handler不通过 DI 决定地址；它只要求 Handler实例能在自己的 LayerProvider中创建。

---

## 12. Activate

所有 Provider 和实例必须在 OwnerScope Thread创建：

```text
ScopeActivateCall
    → Owner Thread
    → 按 Push LayerIndex
        → 创建当前 LayerProvider
        → 创建 Service
        → 创建 Context
        → Attach Binding
        → Mount
        → Provide / From
        → Event Handler注册
        → LocalCall Handler绑定
        → Initialize / PostBuild / RuntimeStart
```

空 Layer：

```text
ProviderSlot = -1 或 Empty Provider
Count = 0
不创建对象
```

具体方案应避免为纯结构 Layer分配无用 Provider对象。

---

## 13. Stop 与 Dispose

Stop：

```text
关闭新业务解析入口
RuntimeStop 按 LayerIndex逆序
解除 Event / LocalCall Handler
```

Dispose 按 LayerIndex逆序：

```text
Unbind Provide / From
Dispose Context
Dispose Transient
Dispose Scoped
Dispose Singleton / Instance（按所有权）
Detach Binding
Dispose LayerServiceProvider
```

不能：

```text
Provider 已 Dispose 后继续 Tick其对象
Handler Target已 Dispose 后继续派发
从 MainScope线程释放 Worker Provider
```

---

## 14. 与 Mount、Provide / From、Tool 的关系

08 号只定义共同范围：

```text
相同 RuntimeGeneration
相同 LayerIndex
相同 ScopeId
```

具体规则由：

```text
09 Provide / From
13 Mount
14 LayerTool
```

负责。

08 不新增：

```text
跨 Layer DI fallback
跨 Scope资源代理
自动 ScopeRef注入
```

---

## 15. Call 是 Scope 内跨 Layer通讯边界

业务场景：

```text
GameplayLayer / CombatService
需要 PresentationLayer / CombatHudService 的结果
```

禁止：

```csharp
CombatHudService hud =
    this.Get<CombatHudService>();
```

正确：

```csharp
HudState state =
    await this.Call<
        QueryHudStateRequest,
        HudState>(
            in request);
```

本地 Call：

```text
CurrentScope
+ Request
+ Response
```

调用方不需要知道 Handler 所在 Layer。

跨 Scope：

```csharp
PathResult result =
    await this.Scope<PathfindingScope>()
        .Call<
            FindPathRequest,
            PathResult>(
                in request);
```

---

## 16. 多 Runtime 隔离

同一个 Layer 和 Scope Plan 可以创建多个 Runtime。

每个 Runtime 必须拥有独立：

```text
LayerServiceProvider
Service / Context实例
生命周期缓存
Dispose 状态
ScopeObjectBinding Generation
```

进程级可共享：

```text
不可变 Service TypeId
生成式 Factory / Invoker
只读 Metadata
```

不得共享：

```text
Provider实例
Singleton业务对象
Instance注册
可变 Constructor Cache
Resolved Service数组
```

---

## 17. faster 分支复用

### 17.1 直接复用

```text
LayerBase/DI/ServiceCollection.cs
    AddScoped
    PushRegistrationScope
    Descriptor收集

LayerBase/DI/ServiceDescriptor.cs
    Contract / Implementation
    Lifetime
    Factory / Instance
    RegistrationScopeId

LayerBase/DI/ServiceContracts.cs
    IServiceCollection / IServiceProvider API

LayerBase/Layer/Layer.cs
    每 Layer ServiceCollection
    ConfigureServices
    原 Layer Provider边界

LayerBase.Test/ServiceTests.cs
    多 Layer实例隔离
    生命周期
    构造选择
    循环依赖
```

### 17.2 修改后复用

```text
ServiceProvider：
    保留生命周期分派、构造选择、循环诊断和 Dispose；
    删除 WorldServiceRoot / Runtime Root fallback；
    Owner改为 Layer × Scope Provider；
    热路径改为 Slot / Generated Invoker。

LayerServiceGenerator：
    保留发现和 Factory生成；
    Contribution同时记录 OwnerLayer和 OwnerScope。

ScopeObjectBinding：
    同时记录 LayerIndex、ScopeId和 ProviderSlot。
```

### 17.3 禁止进入 Running

```text
ConcurrentDictionary<Type, ...>
WorldServiceRoot fallback
Expression.Compile
DynamicMethod
ConstructorInfo.Invoke
MethodInfo.Invoke
FieldInfo.SetValue
PropertyInfo.SetValue
运行期 Type搜索 Provider
Runtime级业务 Singleton共享
```

---

## 18. 需要修改的代码位置

优先检查：

```text
LayerBase/DI/
    ServiceCollection.cs
    ServiceDescriptor.cs
    ServiceProvider.cs
    ServiceContracts.cs

LayerBase/Layer/
    Layer.cs

LayerBase/Application/
    LayerRuntime.cs
    LayersBuilder.cs

LayerBase/Scope/
    ScopeObjectBinding.cs
    ScopeCompositionPlan.cs
    ScopeCompositionBuilder.cs
    ScopeRuntime.cs

LayerBase.Generator/
    LayerServiceGenerator.cs
```

仅在现有类型无法表达 Layer × Scope Provider 时新增：

```text
LayerScopeProviderPlan
LayerServiceProvider
```

不要并存三套容器：

```text
ScopeServiceProvider
LayerServiceProvider
Runtime Root Provider
```

---

## 19. Agent 执行任务

```text
1. 保留 Layer.ConfigureServices / ServiceCollection 原 API。
2. 删除一个 Scope统一 Provider设计。
3. 所有 Service Contribution同时记录 OwnerLayer和 OwnerScope。
4. LayerBuildPlan按 Scope分区 Descriptor。
5. 生成 LayerScopeProviderPlan。
6. ScopeExecutionPlan只保存 ProviderSlot执行视图。
7. Activate在 OwnerScope Thread按 LayerIndex创建 Provider。
8. this.Get<T>()通过 Binding ProviderSlot直接解析。
9. 构造注入只允许当前 LayerProvider。
10. 保留 RegistrationScopeId语义。
11. 删除 WorldServiceRoot / MainScope / Runtime Root fallback。
12. Mount / Provide / From / Tool复用同一 LayerProvider边界。
13. 同 Scope跨 Layer依赖改为 this.Call。
14. 跨 Scope依赖改为 ScopeEvent / ScopeCall。
15. Factory / Invoker / Setter由生成器或 Build预计算。
16. 保留 faster 生命周期、错误和 Dispose语义。
17. 更新 Layer-first 和 Scope隔离测试。
18. 验证稳态 Get<T>零分配。
```

---

## 20. 必须测试

```text
Same_layer_same_scope_services_resolve

Same_scope_different_layers_cannot_resolve

Same_layer_different_scopes_cannot_resolve

Cross_layer_constructor_dependency_fails_build

Cross_scope_constructor_dependency_fails_build

Cross_layer_error_recommends_local_call

Cross_scope_error_recommends_scope_call_or_event

Layer_plan_owns_provider_plan

Scope_runtime_only_holds_provider_execution_view

Each_scope_has_independent_layer_provider_instance

Same_service_type_in_different_layers_isolated

Same_service_type_in_different_scopes_isolated

Registration_scope_id_is_preserved

Worker_provider_is_created_on_worker_thread

Worker_service_and_context_are_created_on_worker_thread

Provider_never_falls_back_to_other_layer

Provider_never_falls_back_to_main_scope

Runtime_root_cannot_resolve_layer_business_service

Context_matches_owner_service_layer_and_scope

Dispose_order_is_reverse_layer_order

Context_disposes_before_owner_service

Same_plan_builds_multiple_isolated_runtimes

Steady_state_get_service_is_zero_allocation
```

保留并迁移 `faster` 原：

```text
Singleton / Scoped / Transient
Constructor Selection
Circular Dependency
Member Injection
Mount Implementation Type
Service Binding Safety
Multiple Layer Isolation
```

---

## 21. 验收否决项

出现以下任意一项，任务不通过：

```text
整个 Scope只有一个可解析所有 Layer的 Provider

ScopePlan成为 DI业务所有权根

Service只有 OwnerScope，没有 OwnerLayer

this.Get<T>()搜索多个 LayerProvider

构造依赖回退其他 Layer或 MainScope

WorldServiceRoot / Runtime Root可取得 Layer业务 Service

ScopeRef暴露 GetService

同一可变 Instance注册到多个 Layer或 Scope

Worker Provider在 MainScope创建

Provider使用 Type Dictionary作为稳态热路径

运行期反射构造或成员注入

为了跨 Layer DI引入 Proxy / Service Locator / 隐式 ScopeRef

删除 RegistrationScopeId原语义

为本任务重写 EventCenter、Call、Post、ECS或 ActorWorld
```

---

## 22. 本阶段不修改的内容

本文不修改：

```text
EventCenter派发与 Handler结构
ScopeLocalCall Handler Registry
ScopeEvent / ScopeCall Transport
PostScheduler
ECS Query执行
ActorWorld
WorkerEventJob
生命周期控制协议
```

本文只保证：

```text
Layer管理 DI边界。

Scope隔离 Provider实例和线程。

运行期通过 OwnerLayer + OwnerScope
定位唯一 LayerServiceProvider。

跨 Layer使用本地 Call。
跨 Scope使用 ScopeEvent / ScopeCall。
```
