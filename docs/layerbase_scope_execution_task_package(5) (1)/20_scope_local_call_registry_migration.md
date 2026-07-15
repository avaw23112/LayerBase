# 20 ScopeLocalCall 与当前 Scope Handler 迁移

> **强制执行规范：** 本文必须遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`；冲突时以 00、01 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `05_scope_static_composition_generators_revised.md`、`07_lbtask_synchronization_context.md`、`08_di_scope_container_layer_first_revised.md`、`19_layer_service_context_scope_tick_lifecycle_migration_revised.md`  
> **相关阶段：** `03_scope_event_call_protocol.md`  
> **文档性质：** 独立阶段任务。本文只实现当前 Scope内部的直接 Call，不实现跨 Scope Transport，不自动选择 Local/Remote路径。

---

## 0. 本阶段核心结论

本地 Call：

```csharp
TResponse response =
    await this.Call<
        TRequest,
        TResponse>(
            in request);
```

只表示：

```text
在调用对象的 CurrentScope中，
调用该 Scope唯一的
Request + Response Handler。
```

跨 Scope：

```csharp
TResponse response =
    await this.Scope<TTargetScope>()
        .Call<
            TRequest,
            TResponse>(
                in request);
```

属于 03 号 ScopeCall协议，是另一套实现。

必须删除：

```text
UnifiedCallRoute
自动判断 Local/Remote
TargetScopeId嵌入本地 Route
完整 Runtime唯一 Handler
旧 TLayer Call API
Obsolete兼容 Stub
```

---

## 1. 架构意义

Layer仍管理：

```text
Handler Service归属
DI
Mount
Provide / From
生命周期
诊断
```

但本地 Call的地址不包含 Layer。

原因：

```text
同 Scope不同 Layer之间禁止 DI，
需要使用本地 Call完成明确通信。

调用方不应知道 Handler位于哪个 Layer。
```

最终地址：

```text
RuntimeGeneration
+ ScopeId
+ RequestType
+ ResponseType
```

Build后转换为：

```text
ScopeId
+ LocalCallId
```

---

## 2. 最终公有 API

### 2.1 Handler

推荐最终契约：

```csharp
public interface IScopeLocalCallHandler<
    TRequest,
    TResponse>
{
    LBTask<TResponse> HandleAsync(
        in TRequest request,
        CancellationToken cancellationToken = default);
}
```

如果 `faster` 现有 Handler接口有成熟签名和大量实现，可修改复用其方法签名，但最终公开名称和语义不能继续表达 Layer寻址。

示例：

```csharp
[Scope<CombatScope>]
public sealed partial class
    InventoryQueryService :
    IService,
    IScopeLocalCallHandler<
        GetInventoryRequest,
        GetInventoryResponse>
{
    public LBTask<GetInventoryResponse>
        HandleAsync(
            in GetInventoryRequest request,
            CancellationToken cancellationToken = default)
    {
        return LBTask.FromResult(
            new GetInventoryResponse(
                _inventory.Read(
                    request.PlayerId)));
    }
}
```

该 Service可属于 `GameplayLayer`。

### 2.2 调用

同一 CombatScope中的任意 Layer对象：

```csharp
GetInventoryResponse response =
    await this.Call<
        GetInventoryRequest,
        GetInventoryResponse>(
            in request);
```

调用方和 Handler可以属于不同 Layer。

---

## 3. 明确不实现的行为

`this.Call<TRequest,TResponse>()` 不得：

```text
自动搜索其他 Scope
自动生成 ScopeCall Request
自动选择 TargetScope
回退 MainScope
读取 UnifiedCallRoute.TargetScopeId
根据 Handler位置切换 Transport
```

当前 Scope无 Handler时：

```text
LocalCallHandlerNotFound
```

即使其他 Scope存在相同 Handler，也不能自动调用。

显式跨 Scope必须写出目标：

```csharp
this.Scope<PathfindingScope>()
    .Call<
        FindPathRequest,
        FindPathResponse>(
            in request);
```

---

## 4. 唯一性规则

唯一键：

```text
ScopeId
+ RequestType
+ ResponseType
```

### 同一个 Scope

相同 Request/Response只能有一个 Handler。

两个不同 Layer在同 Scope注册同一 Handler：

```text
Build Error
```

诊断包含：

```text
Scope
Request
Response
两个 OwnerLayer
两个 OwnerService
SourceLocation
```

### 不同 Scope

允许：

```text
MainScope：
    GetConfigRequest → PreviewConfigResponse

CombatScope：
    GetConfigRequest → RuntimeConfigResponse
```

它们拥有不同 LocalCallRegistry和 Handler实例。

---

## 5. Contribution

```csharp
internal readonly struct
    LocalCallHandlerContribution
{
    internal readonly int OwnerLayerIndex;
    internal readonly int OwnerScopeId;

    internal readonly RuntimeTypeHandle
        RequestType;

    internal readonly RuntimeTypeHandle
        ResponseType;

    internal readonly int OwnerObjectSlot;

    internal readonly LocalCallInvoker
        Invoker;

    internal readonly SourceLocation
        Location;
}
```

`OwnerLayerIndex` 用于：

```text
创建 Handler
DI
生命周期
诊断
```

不用于 Route查找。

Contribution先进入：

```text
LayerBuildPlan
```

再按 OwnerScope投影：

```text
ScopeLocalCallPlan
```

---

## 6. Build 与 LocalCallId

Build步骤：

```text
1. 收集各 Layer的 LocalCall Handler Contribution。
2. 保留 OwnerLayer / OwnerScope。
3. 按 ScopeId分组。
4. 在每个 Scope内检查 Request + Response唯一。
5. 分配稳定 LocalCallId。
6. 生成或绑定直接 Invoker。
7. 输出 ScopeLocalCallPlan。
8. Freeze。
```

稳定排序不得依赖 Dictionary枚举顺序。

推荐键：

```text
Request FullName
Response FullName
OwnerLayerIndex
Source StableOrder
```

OwnerLayer只参与确定性和诊断，不参与运行寻址。

---

## 7. ScopeLocalCallRegistry

```csharp
internal sealed class
    ScopeLocalCallRegistry
{
    private readonly LocalCallEntry[]
        _entries;

    internal LBTask<TResponse> Invoke<
        TRequest,
        TResponse>(
        int localCallId,
        object[] objects,
        in TRequest request,
        CancellationToken cancellationToken);
}
```

```csharp
internal readonly struct
    LocalCallEntry
{
    internal readonly int HandlerObjectSlot;
    internal readonly LocalCallInvoker Invoker;
}
```

每个 Scope一个 Registry。

Registry不保存：

```text
TargetScopeId
ScopeEndpoint
PromiseTable
MPSC Writer
Layer Type
Handler Type Dictionary
```

---

## 8. 调用路径

```csharp
public static LBTask<TResponse> Call<
    TRequest,
    TResponse>(
    this IScopeObject owner,
    in TRequest request,
    CancellationToken cancellationToken = default)
{
    ScopeObjectBinding binding =
        ScopeObjectBinder.Get(
            owner);

    binding.LocalAccess
        .RequireOwnerThread();

    int localCallId =
        GeneratedLocalCallId<
            TRequest,
            TResponse>.ForScope(
                binding.ScopeId);

    return binding.LocalAccess
        .LocalCalls
        .Invoke<
            TRequest,
            TResponse>(
                localCallId,
                in request,
                cancellationToken);
}
```

实际生成 API可以直接把 LocalCallId写进生成方法，避免通用 `ForScope`查表。

热路径：

```text
Binding
    → Current ScopeLocalCallRegistry
    → LocalCallId
    → Handler ObjectSlot
    → Generated Invoker
```

不入队，不写 ScopeCallInbox。

---

## 9. 线程与重入

调用必须发生在调用对象 OwnerScope Thread。

Handler也在同一线程执行。

同步 Handler：

```text
直接返回 Completed LBTask
```

异步 Handler：

```text
使用当前 Scope SynchronizationContext
continuation回当前 Scope
```

本地 Call不创建跨 Scope PromiseTable。

框架不自动把递归 Call切成队列；业务递归和循环依赖由测试、诊断或业务设计负责。

---

## 10. Fault 与取消

Handler抛异常：

```text
返回 Faulted LBTask<TResponse>
继续使用当前 Scope Fault / Circuit规则
```

Cancellation：

```text
调用前已取消：
    不进入 Handler

Handler执行中：
    传递 CancellationToken
```

Scope开始 Stop后：

```text
拒绝新业务 LocalCall
返回 ScopeStopped或 Cancelled终态
```

已经进入 Handler的 Call必须获得：

```text
Success
Fault
Cancelled
```

之一。

---

## 11. 与 DI 边界的关系

Handler在自己的：

```text
OwnerLayer
+
OwnerScope
```

中创建。

Handler内部 DI仍只能访问同 Layer、同 Scope资源。

调用方可属于同 Scope的其他 Layer。

这就是跨 Layer通信边界：

```text
GameplayLayer Service
    → CurrentScope LocalCall
    → PresentationLayer Handler
```

不能因此让 Handler取得调用方 Layer的 DI资源。

---

## 12. 与 ScopeCall 的关系

本地 Call：

```text
直接 Invoker
无 MPSC
无 PromiseTable
无 TargetScope
```

跨 Scope Call：

```text
ScopeEndpoint
Request CallInbox
Response CallInbox
PromiseTable
Origin continuation
```

两者可以共享：

```text
Request / Response DTO
Handler业务方法签名
错误 DTO
部分生成器模板
```

但不能共享：

```text
Unified Route
自动 Local/Remote分支
公开 CallAccessor双路径
```

---

## 13. 旧 TLayer API 直接删除

必须删除：

```csharp
this.Call<
    TLayer,
    TRequest,
    TResponse>(
        in request);
```

以及：

```text
TLayer Route Metadata
LayerCallRoute
LayerCallRegistry
LayerCall Generator
LayerCall Analyzer自动补 Layer
LayerCall Extension
兼容 Alias
Obsolete Stub
```

不保留：

```text
[Obsolete]
error:true占位 API
旧泛型参数忽略实现
```

旧代码迁移：

```text
同 Scope：
    this.Call<TRequest,TResponse>()

跨 Scope：
    this.Scope<TScope>()
        .Call<TRequest,TResponse>()
```

---

## 14. faster 分支复用

### 14.1 直接复用

```text
旧 LayerCall Handler业务签名
Call异常和取消测试
LBTask<TResponse>
生成式 Handler Invoker
Handler SourceLocation
直接调用 Benchmark
```

### 14.2 修改后复用

```text
ILayerCallHandler：
    迁移为 ScopeLocal Handler语义和名称。

LayerCall Contribution：
    保留 Request / Response / Invoker；
    增加 OwnerScope；
    OwnerLayer仅作管理元数据。

LayerCall Registry：
    改为每 Scope ScopeLocalCallRegistry。

ServiceLayerBinder：
    改用 ScopeObjectBinding。
```

### 14.3 禁止移植

```text
TLayer公开地址
LayerCallRoute
UnifiedCallRoute
TargetScopeId
Local/Remote自动选择
完整 Runtime唯一 Handler
旧 API Obsolete兼容
Running按 Type查 Route
```

---

## 15. 需要修改的代码位置

优先搜索：

```text
ILayerCallHandler
LayerCall
Call<TLayer
UnifiedCallRoute
CallAccessor
TargetScopeId
```

涉及：

```text
LayerBase/Call/
LayerBase/Scope/Call/
LayerBase/DI/ServiceExtensions.cs
LayerBase/Layer/Layer.cs
LayerBase/Scope/ScopeObjectBinding.cs
LayerBase.Generator/
LayerBase.Analyzers/
LayerBase.Test/
LayerBase.BenchMark/
```

---

## 16. Agent 执行任务

```text
1. 将 20 号范围限制为当前 Scope LocalCall。
2. 删除 UnifiedCallRoute和自动 Local/Remote分支。
3. 定义或迁移 IScopeLocalCallHandler。
4. Contribution同时记录 OwnerLayer和 OwnerScope。
5. Build按 Scope分组并检查 Request + Response唯一。
6. 不同 Scope允许相同 Request/Response。
7. 分配稳定 LocalCallId。
8. 每 Scope创建 ScopeLocalCallRegistry。
9. this.Call固定访问 CurrentScope Registry。
10. LocalCall直接 Invoker，不入 MPSC。
11. 异步 continuation回当前 Scope Context。
12. Stop拒绝新 LocalCall并终结已接受调用。
13. 旧 TLayer API、Generator、Analyzer、测试直接删除。
14. 跨 Scope API完全交给 03 号。
15. 复用 faster Handler Invoker、LBTask和测试。
```

---

## 17. 必须测试

```text
Local_call_invokes_handler_in_current_scope

Local_call_never_searches_other_scope

Missing_local_handler_does_not_fallback_remote

Same_scope_request_response_is_unique

Duplicate_handler_in_two_layers_same_scope_fails

Same_request_response_in_different_scopes_is_allowed

Caller_and_handler_can_be_in_different_layers

Layer_is_not_part_of_runtime_call_address

Handler_di_remains_same_layer_same_scope

Local_call_does_not_write_call_inbox

Local_call_does_not_allocate_promise

Async_local_call_continues_on_current_scope

Wrong_thread_local_call_fails

Scope_stop_rejects_new_local_call

Accepted_local_call_reaches_terminal_state

TLayer_call_api_does_not_exist

Unified_call_route_does_not_exist

Steady_state_local_call_is_zero_allocation
```

---

## 18. 验收否决项

出现以下任意一项，任务不通过：

```text
this.Call自动调用其他 Scope

LocalCall Route包含 TargetScopeId

存在 UnifiedCallRoute或双路径 CallAccessor

完整 Runtime限制一个 Request/Response Handler

Layer参与运行时 LocalCall地址

旧 TLayer API或 Obsolete Stub仍存在

LocalCall写 ScopeCallInbox或创建 PromiseTable

Running通过 Type / Dictionary查 Handler

为了本任务重写 ScopeCall Transport
```

---

## 19. 本阶段不修改的内容

本文不修改：

```text
03 号跨 Scope Call协议
DI
Mount
Provide / From
EventCenter
PostScheduler
ECS
ActorWorld
```

本文只保证：

```text
this.Call只调用当前 Scope唯一 Handler。

Layer不参与 Call地址，
但继续管理 Handler实例和生命周期。

跨 Scope必须显式 this.Scope<TScope>().Call。
```
