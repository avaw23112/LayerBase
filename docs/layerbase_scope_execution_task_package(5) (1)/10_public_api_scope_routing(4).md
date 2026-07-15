# 10 Layer、Service、Context 与 LayerRuntime 公有 API 路由

> **强制执行规范：** 本文必须遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`；LayerTool规则以 `14_layer_tool_runtime_global_registry_revised.md` 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `02_scope_runtime_resources_revised.md`、`05_scope_static_composition_generators_revised.md`、`08_di_scope_container_layer_first_revised.md`、`14_layer_tool_runtime_global_registry_revised.md`、`17_scope_local_event_center_subscription_migration_revised.md`、`20_scope_local_call_registry_migration.md`  
> **文档性质：** 独立阶段任务。本文只统一业务对象和 Runtime的公有访问路由，不重新设计各子系统算法。

---

## 0. 核心路由原则

Layer是 Scope的上层业务管理结构。

Scope是对象实例的执行和本地资源维度。

所有业务对象必须具有：

```text
OwnerLayer
OwnerScope
RuntimeGeneration
```

最终 API路由：

```text
Send / Post / Timer / Delay / ECS / WorkerJobs：
    OwnerScope

Get / Mount / Provide / From：
    OwnerLayer + OwnerScope

this.Call<Request,Response>：
    CurrentScope

this.Scope<TScope>().TryPost / Call：
    Explicit TargetScope

Tools：
    Current Runtime全局 LayerToolRegistry

Actor：
    MainScope本地 MainActorRuntime
    CustomScope显式 ScopeEvent / ScopeCall到 MainScope
```

禁止：

```text
ScopeServiceProvider
Runtime Root ServiceProvider
Layer只属于 MainScope
CustomScope对象没有 OwnerLayer
业务对象保存 LayerRuntime后任意取资源
```

---

## 1. Layer 的两层含义

### 1.1 LayerPlan

`LayersBuilder.Push` 产生的 Layer结构管理：

```text
所有 Scope中的 Service / Context Contribution
DI范围
Mount / Provide / From范围
Event Handler LayerIndex
生命周期顺序
Tool定义来源
```

因此不能说：

```text
Layer固定属于 MainScope
```

### 1.2 Push 的具体 Layer实例

当前 `faster` 用户传入的具体 Layer实例：

```csharp
.Push(new GameplayLayer())
```

作为该 Layer的 MainScope运行目标和 Runtime管理入口。

因此：

```text
具体 Layer实例上的本地 Send/Post/ECS API
    → MainScope
```

但这不影响：

```text
GameplayLayer Plan
    → 同时管理 CombatScope / PathfindingScope中的 Gameplay Service。
```

CustomScope使用轻量 Layer Slice，不要求复制完整 Layer实例。

---

## 2. ScopeObjectBinding

```csharp
internal readonly struct
    ScopeObjectBinding
{
    internal readonly int RuntimeId;
    internal readonly int RuntimeGeneration;

    internal readonly int LayerIndex;
    internal readonly int ScopeId;

    internal readonly int ProviderSlot;
    internal readonly int ObjectSlot;

    internal readonly IScopeLocalAccess
        LocalAccess;

    internal readonly IRuntimeAccess
        RuntimeAccess;

    internal readonly ScopeEndpoint
        OwnEndpoint;

    internal readonly ScopeEndpointTable
        Endpoints;
}
```

### LocalAccess

只允许访问 OwnerScope本地：

```text
EventCenter
PostScheduler
Timer / Delay
EcsWorld / EcsScheduler
SynchronizationContext
ScopeLocalCallRegistry
LayerProvider by ProviderSlot
WorkerJobAccessor
```

### RuntimeAccess

只允许访问 Runtime全局：

```text
LayerToolRegistry
只读 Runtime配置
Diagnostics
WorkerJobScheduler提交入口
```

不得暴露：

```text
其他 ScopeRuntime
Runtime Root Provider
ActorWorld
可变 LayerRuntime内部
```

---

## 3. 统一业务对象接口

可以通过内部生成绑定让：

```text
Layer
IService
ILayerContext
```

共享扩展 API，而不要求业务类手写公共基接口。

推荐公开入口：

```csharp
public static class ScopeObjectExtensions
{
    public static void Send<TEvent>(
        this IScopeObject owner,
        in TEvent value);

    public static bool Post<TEvent>(
        this IScopeObject owner,
        in TEvent value);

    public static TService Get<TService>(
        this IScopeObject owner)
        where TService : class;

    public static LBTask<TResponse> Call<
        TRequest,
        TResponse>(
        this IScopeObject owner,
        in TRequest request,
        CancellationToken cancellationToken = default);

    public static LayerToolAccessor Tools(
        this IScopeObject owner);

    public static WorkerJobAccessor WorkerJobs(
        this IScopeObject owner);

    public static ScopeRef<TScope> Scope<TScope>(
        this IScopeObject owner)
        where TScope : IScopeDefinition;
}
```

实际接口名可沿用 `faster`，重点是路由语义。

---

## 4. Service 与 Context API

### 4.1 Scope本地资源

```text
service.Send
context.Send
    → OwnerScope EventCenter

service.Post
context.Post
    → OwnerScope本地 PostScheduler

service.Timers / Delay
context.Timers / Delay
    → OwnerScope Timer / Delay

service.ECS
context.ECS
    → OwnerScope EcsWorld / EcsScheduler

service.WorkerJobs
context.WorkerJobs
    → 捕获 OwnerScope Endpoint
```

### 4.2 Layer范围资源

```text
service.Get<T>
context.Get<T>
    → OwnerScope
    → OwnerLayer ProviderSlot
    → ServiceSlot
```

禁止：

```text
搜索 Scope内其他 LayerProvider
回退 MainScope Provider
回退 Runtime Root
```

Context跟随 OwnerService的：

```text
OwnerLayer
OwnerScope
ProviderSlot
```

### 4.3 Runtime全局 Tool

```text
service.Tools()
context.Tools()
    → RuntimeAccess
    → 当前 Runtime LayerToolRegistry
```

Tool不按 OwnerScope或 OwnerLayer限制访问。

---

## 5. Layer实例 API

Push 的具体 Layer实例绑定：

```text
OwnerLayer = 当前 LayerIndex
OwnerScope = MainScope
```

因此其本地 API：

```text
Layer.Send / Post / Timer / Delay / ECS：
    MainScope本地资源

Layer.Get<T>：
    当前 Layer × MainScope Provider

Layer.Subscribe*：
    MainScope EventCenter
    并传入当前 Push LayerIndex

Layer.Tools：
    Runtime全局 Tool Registry

Layer.WorkerJobs：
    OriginScope = MainScope
```

这只是具体 Layer实例的运行位置。

禁止由此推导：

```text
CustomScope Service不属于 Layer
LayerPlan只管理 MainScope
```

---

## 6. 本地 Call API

```csharp
TResponse result =
    await this.Call<
        TRequest,
        TResponse>(
            in request);
```

固定路由：

```text
CurrentScope
    → ScopeLocalCallRegistry
```

调用方和 Handler可以在同 Scope的不同 Layer。

不自动跨 Scope。

没有本地 Handler时返回明确错误。

---

## 7. 跨 Scope API

```csharp
ScopeRef<TScope> scope =
    this.Scope<TScope>();
```

```csharp
bool accepted =
    scope.TryPost(
        in value);
```

```csharp
TResponse response =
    await scope.Call<
        TRequest,
        TResponse>(
            in request);
```

底层只使用：

```text
Target ScopeEventInbox
Target ScopeCallInbox
```

`ScopeRef` 不得暴露：

```text
ScopeRuntime
LocalAccess
ServiceProvider
EcsWorld
EventCenter
PostScheduler
ActorWorld
LayerToolRegistry实例
```

Tool不需要通过 ScopeRef访问，因为它本来就是 Runtime全局。

---

## 8. LayerRuntime 公有 API

```csharp
public sealed class LayerRuntime :
    IDisposable
{
    public RuntimeState State {
        get;
    }

    public ScopeRef<MainScope> Main {
        get;
    }

    public ScopeRef<TScope> GetScope<TScope>()
        where TScope :
            IScopeDefinition;

    public bool TryGetScope<TScope>(
        out ScopeRef<TScope> scope)
        where TScope :
            IScopeDefinition;

    public LayerToolAccessor Tools {
        get;
    }

    public IFullSnapRuntime FullSnap {
        get;
    }

    public RuntimeDiagnostics Diagnostics {
        get;
    }

    public void Activate();

    public void Pump(
        float deltaTime);

    public void PumpFixed(
        float fixedDeltaTime);

    public LBTask StopAsync(
        CancellationToken cancellationToken = default);

    public void Dispose();
}
```

Runtime允许公开：

```text
Tools访问器
ScopeRef
FullSnap协调接口
Diagnostics
生命周期控制
```

Runtime不公开：

```text
EventCenter
PostScheduler
Timer
EcsWorld
LayerServiceProvider
ScopeRuntime
ActorWorld
```

---

## 9. 外部线程入口

外部系统向 MainScope：

```csharp
runtime.Main.TryPost(
    in engineFrameEvent);
```

外部系统向指定 Scope：

```csharp
runtime.GetScope<NetworkScope>()
    .TryPost(
        in networkPacketEvent);
```

必须删除：

```text
PostFromAnyThread
TryPostFromAnyThread
runtime.Post(...)
runtime.EventCenter.Send(...)
```

外部输入总是 ScopeEvent。

---

## 10. Event API

### Send

```text
当前 OwnerScope同步 EventCenter.Send
```

只能在 OwnerScope Thread调用。

### Post

```text
当前 OwnerScope本地 PostScheduler
```

只能在 OwnerScope Thread调用。

### ScopeRef.TryPost

```text
显式 TargetScope ScopeEventInbox
```

它不是 PostScheduler跨线程入口。

---

## 11. Subscribe API

现有自动和手动订阅能力保留。

```text
Service / Context Handler：
    OwnerScope EventCenter
    OwnerLayer Push LayerIndex

具体 Layer实例 Handler：
    MainScope EventCenter
    当前 LayerIndex
```

不得：

```text
使用 ServiceSlot / ContextSlot代替 LayerIndex
创建第二套 Scope Handler Registry
```

---

## 12. ECS API

```text
Service / Context Query：
    OwnerScope EcsScheduler

具体 Layer实例 Query：
    MainScope EcsScheduler
```

Query Contribution仍记录 OwnerLayer和 OwnerScope。

跨 Scope不允许直接调用 Query入口。

必须先：

```text
ScopeEvent / ScopeCall进入目标 Scope
    → 目标 Handler本地提交 Query
```

---

## 13. WorkerEventJob API

```text
Layer.WorkerJobs：
    OriginScope = MainScope

Service / Context.WorkerJobs：
    OriginScope = OwnerScope
```

Worker Result：

```text
OriginScope ScopeEventInbox
    → OriginScope Owner Thread
    → 本地 PostScheduler
```

不使用 ScopePostEndpoint或 Post CrossThreadIngress。

---

## 14. Actor API

### MainScope

Push Layer实例以及 MainScope Service/Context可以通过受限 `MainActorAccess` 调用 MainActorRuntime。

通用 `ScopeLocalAccess` 不包含 ActorWorld。

### CustomScope

只能：

```text
ActorHandle / DTO
    → ScopeEvent或 ScopeCall<MainScope>
```

不能取得：

```text
ActorWorld
MainActorRuntime
Actor对象引用
```

具体 Projection管线由 21、22 号文档负责。

---

## 15. 错误边界

本地 API只报告：

```text
对象未绑定
RuntimeGeneration失效
错误 OwnerScope Thread
ProviderSlot无效
本地 Handler不存在
Scope正在停止
目标 Scope不存在
Event/Call Inbox满或已关闭
```

不增加：

```text
自动 MainScope fallback
自动跨 Layer查找
自动跨 Scope Call
任意线程安全容器猜测
```

---

## 16. 旧 API 迁移

直接删除：

```text
PostFromAnyThread
TryPostFromAnyThread
ScopeRef.GetService
runtime.GetService
runtime.EventCenter
runtime.EcsWorld
runtime.ActorWorld
TLayer Call API
RunOnMainThread / SwitchToMainThread隐式资源路由
```

除非用户另行确认，最终任务包不保留 `Obsolete` 壳。

迁移对应：

```text
跨线程 Post：
    ScopeRef.TryPost

同 Scope跨 Layer依赖：
    this.Call<Request,Response>

跨 Scope请求：
    this.Scope<TScope>().Call<Request,Response>

全局工具：
    this.Tools() / runtime.Tools
```

---

## 17. faster 分支复用

### 17.1 直接复用

```text
ScopeRef / ScopeAccessor外形
ScopeRef Post / Call生成器
Service / Context扩展 API名称
ScopeObjectBinding Generation校验
Layer原 Subscribe API
runtime.Tools心智模型
```

### 17.2 修改后复用

```text
ScopeBindingResolver：
    返回 OwnerLayer + OwnerScope Binding。

ServiceExtensions：
    本地资源走 OwnerScope；
    DI走 OwnerLayer + OwnerScope；
    Tools走 RuntimeAccess。

Layer：
    具体 Push实例绑定 MainScope，
    但保留 LayerPlan管理全部 Scope。

LayerRuntime：
    只公开控制面、ScopeRef、Tools和 Diagnostics。

Actor Extensions：
    MainScope直达 MainActorAccess；
    CustomScope走 ScopeEvent/Call。
```

### 17.3 禁止移植

```text
ServiceLayerBinder.Runtime fallback
ScopeServiceProvider
ScopeRef.Runtime
ScopeRef.GetService
binding.Runtime.EventCenter
binding.Runtime.EcsWorld
runtime根 ActorWorld API
Post CrossThreadIngress
自动 Unified Call
```

---

## 18. 需要修改的代码位置

优先检查：

```text
LayerBase/DI/ServiceExtensions.cs
LayerBase/Layer/Layer.cs
LayerBase/Application/LayerRuntime.cs
LayerBase/Scope/ScopeObjectBinding.cs
LayerBase/Scope/Extensions/ScopeRef.cs
LayerBase/Scope/Extensions/ScopeExtensions.cs
LayerBase/Scope/Extensions/ScopeBindingResolver.cs
LayerBase/ECS/Extensions/
LayerBase/Actor/Extensions/
LayerBase/Tooling/
LayerBase.Generator/
LayerBase.Test/
```

---

## 19. Agent 执行任务

```text
1. ScopeObjectBinding同时记录 OwnerLayer和 OwnerScope。
2. LocalAccess只暴露 Scope本地资源。
3. RuntimeAccess只暴露 Runtime全局 Tools和只读能力。
4. Service/Context Send/Post/Timer/ECS路由 OwnerScope。
5. Service/Context Get路由 OwnerLayer + OwnerScope Provider。
6. Service/Context Tools路由 Runtime全局 Registry。
7. this.Call固定 CurrentScope LocalCall。
8. Scope<T>().TryPost/Call固定显式目标 Scope。
9. Push Layer实例本地 API绑定 MainScope。
10. 保留 LayerPlan管理全部 Scope Contribution。
11. LayerRuntime公开 Tools访问器，不公开原始 Registry。
12. 删除 Runtime Root Service/Event/ECS/Actor API。
13. 删除 PostFromAnyThread和 TLayer Call。
14. Actor API按 MainScope/CustomScope分流。
15. 更新生成器、Analyzer、示例和测试。
```

---

## 20. 必须测试

```text
Service_event_api_uses_owner_scope

Context_event_api_uses_owner_service_scope

Service_get_uses_owner_layer_and_scope

Context_get_uses_owner_layer_and_scope

Service_get_does_not_search_other_layer

Push_layer_instance_uses_main_scope_local_resources

Layer_plan_still_manages_custom_scope_contributions

Service_tools_uses_runtime_global_registry

All_scopes_access_same_runtime_tools

Local_call_uses_current_scope

Local_call_does_not_auto_remote

Cross_scope_post_uses_event_inbox

Cross_scope_call_uses_call_inbox

Scope_ref_does_not_expose_runtime_or_provider

External_main_input_uses_main_scope_ref

Post_from_any_thread_api_does_not_exist

Worker_result_returns_to_origin_scope

Custom_scope_actor_api_does_not_expose_actor_world

Runtime_does_not_expose_event_ecs_provider_or_actor_world

Wrong_owner_thread_local_api_fails
```

---

## 21. 验收否决项

出现以下任意一项，任务不通过：

```text
文档或代码声称 Layer只属于 MainScope

CustomScope对象缺少 OwnerLayer

Service.Get使用 ScopeServiceProvider

DI搜索其他 LayerProvider

Tools被改成 Scope-local

ScopeRef暴露 LocalAccess或 ServiceProvider

this.Call自动跨 Scope

存在 PostFromAnyThread

Runtime公开 EventCenter / EcsWorld / ActorWorld / Provider

CustomScope直接取得 ActorWorld

为了本任务重写 EventCenter、DI或 ScopeCall协议
```

---

## 22. 本阶段不修改的内容

本文不修改：

```text
EventCenter内部派发
PostScheduler本地算法
LayerServiceProvider解析
ScopeLocalCall Invoker
ScopeEvent / ScopeCall Transport
ECS Query算法
ActorWorld内部实现
LayerTool Cache算法
```

本文只保证：

```text
Layer继续管理所有 Scope中的业务对象。

Scope决定实例的本地执行资源。

所有公有 API根据 OwnerLayer、OwnerScope
或显式 TargetScope正确路由。
```
