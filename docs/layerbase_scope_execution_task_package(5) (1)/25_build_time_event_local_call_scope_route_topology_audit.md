# 25 Build-time Event、LocalCall 与 ScopeRoute 拓扑审计

> **最高原则：** 复用 `master` 已存在的 `EventGraphValidator`、`IAutoSubscribe.GetEventDependencies()`、`GetSubscribedEvents()`、Topology/Policy Dump；审计只发生在 Build 冷路径，不参与 Running。  
> **master 基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **faster 复用基线：** `8898a90bcb3e00a370e47f8b39f6eff32fa98980`  
> **依赖阶段：** `05_scope_static_composition_generators.md`、`06_assembly_module_static_composition.md`、`17_scope_local_event_center_subscription_migration_revised.md`、`20_scope_local_call_registry_migration.md`。  
> **文档性质：** Build阶段结构审计。不得增加 Runtime热路径校验，不得通过扫描业务方法体猜测真实调用关系。

---

## 0. master 现有能力

master 已经提供：

```text
EventGraphValidator

EventCycleException

IAutoSubscribe.GetEventDependencies()

IAutoSubscribe.GetSubscribedEvents()

同步事件 DFS环检测

发送但无订阅者 Debug Warning

LayerRuntime.GetTopologySummary()

LayerRuntime.GetTopologyMarkdown()

LayerRuntime.GetPolicyMarkdown()
```

现有 `EventGraphValidator` 使用：

```text
Dictionary<Type, HashSet<Type>>

Dictionary<Type, NodeColor>

List<Type>
```

这些属于 Build冷路径，可以继续使用。

Scope迁移只需要：

```text
把输入从运行期 Layer对象扫描
改为 Build Composition数据。

增加 ScopeId、LayerIndex和 Route信息。

将 LocalCall / ScopeEvent / ScopeCall结构完整性纳入审计。
```

---

## 1. 审计边界

25号只审计可以在 Build期确定的事实。

### 必须确定

```text
OwnerLayer是否存在并已 Push

OwnerScope是否存在并已安装

对象归属的 LayerIndex / ScopeId是否一致

Local Event Handler是否进入正确 Scope EventCenter

LocalCall Handler是否满足 Scope内唯一

ScopeEvent Route目标 Scope是否存在

ScopeCall Route目标 Scope和 Handler是否存在

RouteId / Slot / Range是否唯一且连续

同步 Send依赖图是否存在环

Snap / Tool / Query等 Contribution是否引用无效 Scope
```

### 只能在有显式元数据时审计

```text
ScopeCall潜在等待环

Handler内部发出哪些 ScopeEvent

Handler内部调用哪些 LocalCall
```

如果 Generator/Manifest没有提供依赖元数据：

```text
不得扫描 IL。

不得反编译方法体。

不得假设所有 Call都有静态关系。

不得输出伪精确结论。
```

---

## 2. 审计输入

最终输入来自冻结前的 Composition：

```csharp
internal sealed class
    TopologyAuditInput
{
    internal LayerBuildPlan[] Layers;

    internal ScopeBuildPlan[] Scopes;

    internal EventHandlerContribution[]
        EventHandlers;

    internal EventDependencyContribution[]
        EventDependencies;

    internal LocalCallHandlerContribution[]
        LocalCallHandlers;

    internal ScopeEventRouteContribution[]
        ScopeEventRoutes;

    internal ScopeCallRouteContribution[]
        ScopeCallRoutes;
}
```

实际类型名优先复用05、06号已经定义的结构。

不得为了25号再复制第二套 Contribution DTO。

---

## 3. 审计阶段

Build顺序中：

```text
Contribution收集完成
    → OwnerLayer / OwnerScope解析完成
    → Slot和 Route候选已生成
    → 运行 Plan尚未 Freeze
    → 执行 Topology Audit
    → 无 Error才 Freeze
```

审计失败时：

```text
不创建业务 Scope资源。

不启动 Worker Thread。

不创建 Service / Context。

不触发 Prewarm。
```

---

## 4. Layer / Scope 归属审计

每条业务 Contribution检查：

```text
OwnerLayerType能解析到唯一 Push LayerIndex。

OwnerScopeType能解析到唯一 ScopeId。

Service / Context的声明 Scope与 Contribution Scope一致。

Context OwnerService的 Layer / Scope一致。

Layer实例只绑定 MainScope。

ScopeExecutionPlan Slice来自对应 LayerBuildPlan。

ObjectSlot位于对应 Layer × Scope Provider Range。
```

Error示例：

```text
LBTOPOLOGY_LAYER_NOT_PUSHED

LBTOPOLOGY_SCOPE_NOT_INSTALLED

LBTOPOLOGY_OWNER_SCOPE_MISMATCH

LBTOPOLOGY_OBJECT_SLOT_OUT_OF_RANGE
```

诊断码可以是内部常量，不要求新增公共 Analyzer包。

---

## 5. Local Event 拓扑

### 5.1 Handler注册

验证：

```text
每个 Handler都有 EventTypeId。

Handler OwnerScope与目标 EventCenter一致。

Handler LayerIndex来自 Push顺序。

Handler ObjectSlot属于当前 Layer × Scope。

同一 Handler Contribution不会重复注册。
```

事件允许：

```text
一个 Event多个 Handler。

同一 Event在不同 Scope有不同 Handler集合。
```

### 5.2 同步 Send环

直接复用 master `EventGraphValidator` 的 DFS。

图按 Scope分区：

```text
ScopeId
    → Event Type Node
    → Local Send Dependency Edge
```

只有：

```text
同步 Local Send
```

进入环检测。

不进入同步图：

```text
Local Post

ScopeEvent

ScopeCall

Timer

Delay
```

因为它们不构成同一同步调用栈。

### 5.3 现有错误保持

继续抛出：

```text
EventCycleException
```

消息继续包含：

```text
Cycle path

提示将环中某个同步 Send改为 Post
```

只增加 Scope信息，不改现有异常类型。

---

## 6. 发送与订阅健康提示

继续使用 master已有：

```text
Produced Events

Subscribed Events
```

在同 Scope内判断：

```text
Produced但无 Subscriber：
    Warning

Subscribed但无已知 Producer：
    Warning
```

注意：

```text
外部系统可能通过 ScopeRef.TryPost产生 Event。

运行条件可能决定是否发送。

因此只能是 Warning，不能是 Build Error。
```

跨 Scope Route是已知 Producer时，应计入目标 Scope的 Produced集合。

---

## 7. LocalCall 审计

最新规则：

```text
LocalCall地址
    =
ScopeId
+ RequestType
+ ResponseType
```

### 7.1 唯一性

同 Scope内：

```text
相同 Request + Response只能有一个 Handler。
```

两个不同 Layer注册同一 Handler：

```text
Build Error
```

不同 Scope：

```text
允许相同 Request + Response。
```

### 7.2 Handler完整性

检查：

```text
Handler ObjectSlot存在。

Handler OwnerLayer / OwnerScope有效。

Invoker已生成。

Request / Response不为开放泛型。

LocalCallId稳定且无重复。
```

禁止重新引入：

```text
TLayer Call地址

Runtime全局 Request唯一

UnifiedCallRoute

TargetScopeId存在 LocalCall Entry中
```

---

## 8. ScopeEvent Route 审计

ScopeEvent Route必须检查：

```text
OriginScope存在。

TargetScope存在。

EventType已生成 EventTypeId。

RouteId唯一。

Target EventInbox配置存在。

Payload策略合法。

Route不会指向已删除或未安装 Scope。
```

ScopeEvent不要求目标存在业务 Handler：

```text
可能由运行时基础设施或后续模块处理。
```

但在 Debug Build中可以输出：

```text
Route存在但无已知 Handler
```

作为 Warning。

不得将其变成 Error。

---

## 9. ScopeCall Route 审计

ScopeCall Route检查：

```text
OriginScope存在。

TargetScope存在。

Request / Response类型有效。

目标 Scope有唯一 Handler。

Request Route和 Response Route映射完整。

Promise Token类型匹配。

Control / Lifecycle Route使用标准 CallInbox。
```

Error：

```text
目标 Scope没有 Handler

目标 Scope有多个 Handler

Response Route缺失

RouteId冲突
```

禁止：

```text
独立 Actor ResponseQueue

独立 Lifecycle CompletionQueue

ScopeCall自动回退 LocalCall
```

---

## 10. 静态 ScopeCall 等待图

只有 Generator/Manifest显式提供：

```text
Handler A可能 Call ScopeB RequestX

Handler B可能 Call ScopeA RequestY
```

时才构建等待图。

发现环：

```text
Warning
```

而不是必然 Error，因为实际代码可能：

```text
条件分支不同时成立

先返回再调用

有 Timeout / Cancellation
```

28号必须用并发测试验证真实死锁行为。

如果没有依赖元数据：

```text
跳过等待图。

不得做 IL扫描。
```

---

## 11. Tool、ECS、Snap 的交叉完整性

25号只做引用完整性，不重做对应子系统审计。

### LayerTool

```text
[LayerTool]元特性 OwnerLayer存在。

OwnerScope存在。

Tool Contribution进入正确 Layer × Scope。
```

### ECS

```text
Service / Context Query绑定的 OwnerScope有 EcsWorld。

Blueprint静态 Cache不绑定 Scope实例。
```

不审计 Query方法内部逻辑。

### Snap

```text
Snap Node OwnerScope有效。

SnapKey全 Runtime唯一。
```

---

## 12. 诊断输出

不新增必须由用户调用的：

```csharp
builder.Validate();
```

正常用户继续：

```csharp
LayerRuntime runtime =
    builder.Build();
```

`Build()`自动执行审计。

内部结果：

```csharp
internal readonly struct
    TopologyAuditDiagnostic
{
    internal readonly
        TopologyAuditSeverity Severity;

    internal readonly string Code;
    internal readonly string Message;
    internal readonly SourceLocation Location;
}
```

处理：

```text
Error：
    Build失败。

Event同步环：
    保持 EventCycleException。

其他结构 Error：
    使用现有 Build路径的 InvalidOperationException
    或已有 Composition异常，
    不新增公共异常层级。

Warning：
    存入 Frozen Build Diagnostics；
    Debug模式在 Activate后通过现有 LayerEventInfo报告。
```

---

## 13. 稳定性

诊断排序：

```text
Severity
→ Code
→ ScopeId
→ LayerIndex
→ SourceLocation
→ Message
```

不得依赖：

```text
Dictionary枚举顺序

程序集加载顺序

Module安装顺序之外的随机发现顺序
```

相同输入必须产生相同诊断顺序和 RouteId。

---

## 14. 冷路径释放

审计完成后释放：

```text
Adjacency Dictionary

NodeColor Dictionary

DFS Path

临时 Type集合

重复检测 HashSet

诊断构建 StringBuilder
```

Runtime只保留：

```text
冻结 Route数组

Slot / Range

必要 Descriptor

低频可读诊断文本或 Symbol Id
```

Running不得保留 Type图用于路由。

---

## 15. master / faster 复用

### 直接复用

```text
EventGraphValidator DFS

EventCycleException

GetEventDependencies

GetSubscribedEvents

ProducedEvents / SubscribedEvents

GetTopologySummary

GetTopologyMarkdown

GetPolicyMarkdown

现有同步环测试
```

### 修改复用

```text
EventGraphValidator输入：
    Layer对象集合
    → Build Composition Contributions

Event图：
    Runtime单图
    → 每 Scope一张同步图

Topology Markdown：
    增加 Scope、LocalCall、ScopeEvent、ScopeCall。
```

### 禁止新增

```text
运行期 Topology扫描

业务方法体 IL分析

动态代理调用追踪作为 Build真相

全 AppDomain程序集扫描

第二套 Route Registry
```

---

## 16. 需要修改的代码位置

```text
LayerBase/Application/
    EventGraphValidator.cs
    LayerRuntime.cs
    LayersBuilder Build流程
    Topology Markdown

LayerBase/Layer/
    LayerChain.cs
    Event Dependency描述

LayerBase/Scope/
    Scope Route Plan
    ScopeEvent Route
    ScopeCall Route

LayerBase/Call/
    ScopeLocalCall Plan

LayerBase.Generator/
    只补充已有可生成的 SourceLocation / Dependency元数据

LayerBase.Test/
    EventGraphValidatorTests
    TopologyAuditTests
```

---

## 17. Agent 执行任务

```text
1. 记录 master EventGraphValidator测试和异常消息。
2. 审计输入改为 Composition Plan。
3. 为每条 Contribution保留 LayerIndex / ScopeId / SourceLocation。
4. Event同步图按 Scope分区。
5. 仅 Local Send进入 DFS。
6. 保持 EventCycleException。
7. 检查 LocalCall在 Scope内唯一。
8. 检查 ScopeEvent目标 Scope和 RouteId。
9. 检查 ScopeCall目标 Handler和 Response Route。
10. 只在有显式依赖元数据时生成等待图 Warning。
11. 检查 Tool / ECS / Snap归属引用完整性。
12. Build自动执行 Audit。
13. Error阻止 Freeze和 Activate。
14. Warning通过现有 LayerEventInfo / Markdown输出。
15. Running只使用冻结数组，不运行 Audit。
```

---

## 18. 必须测试

```text
Master_synchronous_event_cycle_test_remains_valid

Local_send_cycle_is_checked_per_scope

Same_event_cycle_in_different_scopes_does_not_cross_connect

Post_edge_breaks_synchronous_cycle

Scope_event_edge_is_not_local_recursive_edge

Produced_without_subscriber_is_warning

External_scope_event_producer_prevents_false_zombie_warning

Local_call_is_unique_per_scope

Same_local_call_in_different_scopes_is_allowed

Duplicate_local_call_in_two_layers_same_scope_fails

Scope_event_target_must_exist

Scope_call_target_handler_must_exist

Scope_call_response_route_must_exist

Route_id_is_stable_for_same_composition

Layer_not_pushed_fails_before_activate

Scope_not_installed_fails_before_activate

Duplicate_snap_key_fails_build

Tool_meta_scope_must_exist

Audit_does_not_scan_method_il

Audit_temporary_graph_is_not_kept_in_runtime
```

---

## 19. 验收否决项

出现任意一项，任务不通过：

```text
Audit在 Running执行

Build通过后才发现无效 Scope Route

LocalCall按 Runtime全局唯一

ScopeEvent被误判为同步递归边

无显式元数据时扫描业务 IL

为了审计新增公共 Validate工作流并修改现有 Build用法

Error发生后仍启动 WorkerScope

Runtime保留 Dictionary<Type,...>图参与路由

修改 EventCycleException原核心语义

诊断顺序依赖 Dictionary枚举

为通过审计恢复 TLayer Call或 UnifiedCallRoute
```

---

## 20. 本阶段最终结果

```text
master EventGraphValidator得到保留并按 Scope扩展。

Build可以在创建运行资源前发现：
    错误归属
    同步事件环
    LocalCall冲突
    ScopeRoute缺失
    RouteId冲突

审计只使用显式 Composition元数据。

Running只使用冻结 Route和 Slot，
不会承担任何拓扑分析开销。
```
