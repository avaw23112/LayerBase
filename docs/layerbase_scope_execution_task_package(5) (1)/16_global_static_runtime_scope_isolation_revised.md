# 16 静态不可变元数据、Runtime 全局状态与 Scope 本地状态隔离

> **强制执行规范：** 遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`。LayerTool 最新规则以 `14_layer_tool_runtime_global_registry_revised.md` 为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`

---

## 0. 本阶段核心目的

所有状态必须分成：

```text
Static Immutable Metadata
Runtime Global State
Scope Local State
```

### Static

只保存不可变：

```text
TypeId
Factory / Invoker
Manifest
Contribution
SourceLocation
只读 Metadata
```

### Runtime Global

每 `LayerRuntime` 一份：

```text
RuntimeCompositionPlan
LayerBuildPlan[]
ScopeExecutionPlan[]
ScopeRuntimeHost / Endpoint Table
MainActorRuntime / ActorWorld
LayerToolRegistry
WorkerJobScheduler
Runtime Fault / Diagnostics
RuntimeId / Generation
```

### Scope Local

每 Scope一份：

```text
EventCenter
PostScheduler
Timer / Delay
EcsWorld / Scheduler
SynchronizationContext
ScopeLocalCallRegistry
EventInbox / CallInbox
LayerProvider实例
ScopeLayerSlice
Lifecycle状态
FixedAccumulator
```

---

## 1. Layer-first Composition

```text
Static Manifest
    → RuntimeCompositionBuilder
    → LayerBuildPlan[]
    → 投影 ScopeExecutionPlan[]
```

Runtime Plan：

```csharp
internal sealed class RuntimeCompositionPlan
{
    internal LayerBuildPlan[] Layers;
    internal ScopeExecutionPlan[] Scopes;
    internal ScopeEventRoute[] EventRoutes;
    internal ScopeCallRoute[] CallRoutes;
    internal RuntimeLayerToolPlan Tools;
}
```

ToolPlan属于 Runtime，不放入 ScopePlan。

---

## 2. LayerTool 的正确层级

正确：

```text
LayerRuntime
    → LayerToolRegistry
```

不是：

```text
static GlobalToolRegistry
ScopeRuntime → ScopeToolRegistry
```

同一 Runtime的所有 Scope访问同一 Registry和缓存。

不同 Runtime拥有不同 Registry和 Tool实例。

Tool Descriptor / Factory可 static共享，Registry / Cache不可 static共享。

---

## 3. 总架构

```text
Process
    ├── Immutable Generated Metadata
    │
    ├── LayerRuntime A
    │   ├── RuntimeCompositionPlan A
    │   ├── LayerToolRegistry A
    │   ├── MainActorRuntime A
    │   ├── Scope A.Main
    │   └── Scope A.Worker
    │
    └── LayerRuntime B
        ├── RuntimeCompositionPlan B
        ├── LayerToolRegistry B
        ├── MainActorRuntime B
        ├── Scope B.Main
        └── Scope B.Worker
```

必须满足：

```text
A Runtime State != B Runtime State
A Tool Cache != B Tool Cache
A Scope State != B Scope State
```

---

## 4. 公有 API

```csharp
LayerRuntime runtime = LayerHub.CreateLayers()
    .Push(new FoundationLayer())
    .Push(new GameplayLayer())
    .AddAssemblyModule(CoreModule.Instance)
    .Build();
```

保留：

```csharp
runtime.Tools
```

表示当前 Runtime的全局 LayerToolRegistry。

禁止：

```text
LayerHub.Current
GlobalServices
GlobalEventCenter
GlobalToolRegistry
ScopeRuntime.Current
```

---

## 5. 状态迁移表

| 内容 | 最终宿主 |
|---|---|
| Event/Service/Tool 不可变元数据 | Static Immutable |
| AssemblyModule Manifest | Static Immutable |
| LayerBuildPlan / ScopeExecutionPlan | LayerRuntime |
| Scope Endpoint Table | LayerRuntime |
| LayerTool Registry / Cache | LayerRuntime |
| WorkerJobScheduler | LayerRuntime |
| MainActorRuntime / ActorWorld | LayerRuntime |
| EventCenter / Post / Timer / ECS | ScopeRuntime |
| LayerProvider实例 | ScopeRuntime执行视图 |
| ScopeLocalCall Registry | ScopeRuntime |
| Current Runtime | 删除 |

---

## 6. static 字段审计

每个可变 static字段分类：

```text
StaticDescriptor：
    改为 readonly不可变。

RuntimeState：
    移入 LayerRuntime。

ScopeState：
    移入 ScopeRuntime。
```

禁止：

```text
static LayerRuntime Current
static ScopeRuntime Current
static LayerToolRegistry
static Tool Cache
static ServiceProvider
static EventCenter
static ActorWorld
static mutable ScopeOption Registry
static Dictionary<RuntimeId, Runtime>
```

允许 ThreadStatic ScopeExecution上下文，但必须 Enter/Exit恢复且不保存业务对象。

---

## 7. Build

```text
1. 读取显式 Module Manifest。
2. 收集 Push Layer。
3. 构建 LayerBuildPlan。
4. 投影 ScopeExecutionPlan。
5. 构建 RuntimeLayerToolPlan。
6. 分配 RouteId / Slot / Range。
7. Freeze。
8. 释放临时 Dictionary/List/HashSet。
9. 创建 LayerRuntime实例状态。
```

Running 不保留临时集合用于业务路由。

---

## 8. ScopeObjectBinding 与 RuntimeAccess

Binding 必须包含：

```text
RuntimeId / Generation
LayerIndex
ScopeId
ProviderSlot
LocalAccess
RuntimeAccess
ScopeEndpoint
```

`RuntimeAccess` 可以暴露：

```text
LayerToolRegistry
只读 Runtime配置
Diagnostics入口
```

不得暴露：

```text
其他 ScopeRuntime
其他 Scope Provider
ActorWorld
可变 Scope资源
```

`this.Tools()` 通过 RuntimeAccess取得 Registry。

---

## 9. 多 Runtime 场景

```csharp
using LayerRuntime a = BuildRuntime();
using LayerRuntime b = BuildRuntime();
```

必须：

```text
a.Service != b.Service
a.EventCenter != b.EventCenter
a.ActorWorld != b.ActorWorld
a.Tools != b.Tools
a.CachedTool != b.CachedTool
```

Dispose A后，B仍 Running，B的 Tool Cache仍有效。

---

## 10. Runtime Dispose

建议顺序：

```text
1. 停止外部输入。
2. Stop / Dispose CustomScope。
3. Stop / Dispose MainScope业务对象。
4. 确认没有 Scope业务代码继续执行。
5. Dispose Runtime LayerToolRegistry。
6. Dispose MainActorRuntime / ActorWorld。
7. Stop / Join WorkerJobScheduler。
8. Dispose Transport / Diagnostics。
9. 清理当前 Runtime实例引用。
```

不得清空 static Manifest或其他 Runtime状态。

某个 Scope Dispose不能清理 LayerTool Cache。

---

## 11. LayerTool 多 Scope并发

因为 Registry属于 Runtime全局：

```text
所有 Scope可并发读取 Descriptor和 Cache。
```

要求：

```text
Descriptor冻结后只读
Cache Slot并发安全
GetOrCreate单实例发布
ClearCache Dispose一次
Tool线程安全或不可变
```

禁止通过“每 Scope复制 Tool”回避并发语义。

---

## 12. 全局 Tool 不是 Service Locator

Registry和 CreateContext禁止：

```text
GetService<T>()
GetScopeRuntime<T>()
GetEcsWorld<T>()
GetEventCenter<T>()
GetActorWorld()
```

否则 Tool会绕过 Layer/Scope资源边界。

---

## 13. Manifest 无全局副作用

Manifest可以 static共享，但模块加载不得：

```text
自动注册 Tool
修改全局 ScopeOption
创建 Runtime
创建 Thread
缓存 Service实例
```

只有显式 `AddAssemblyModule` 才使 Contribution进入当前 Runtime。

---

## 14. faster 分支复用

直接复用：

```text
RuntimeId / Generation
LayerHub.CreateLayers
ScopeObjectBinding Generation校验
Manifest / Contribution
多 Runtime测试骨架
LayerToolRegistry挂 LayerRuntime的原模型
runtime.Tools API
```

修改复用：

```text
可变 static Registry → Runtime或 Scope
LayerToolRegistry → Runtime级 Freeze + 并发 Cache
RuntimeKernel → 保存 Tool、Actor和 Endpoint
ScopeObjectBinding → 增加受限 RuntimeAccess
```

禁止：

```text
ModuleInitializer
LayerHub.Current
AppDomain无边界扫描
static GlobalToolRegistry
ScopeToolRegistry
Runtime Dispose全局 Reset
```

---

## 15. Agent 执行任务

```text
1. 扫描全部可变 static字段。
2. 分类 StaticDescriptor / RuntimeState / ScopeState。
3. Static只保留不可变元数据。
4. RuntimePlan使用 LayerBuildPlan + ScopeExecutionPlan。
5. LayerToolPlan和 Registry放入 LayerRuntime。
6. Event/Post/Timer/ECS/Provider实例放入 ScopeRuntime。
7. 删除 Current Runtime / Global Service / Global Event / Global Tool。
8. Binding增加受限 RuntimeAccess。
9. this.Tools()从 RuntimeAccess取 Registry。
10. 不同 Runtime创建独立 Tool Cache和 ActorWorld。
11. Runtime Dispose只清理自身状态。
12. 建立多 Runtime并行和交叉 Dispose测试。
```

---

## 16. 必须测试

```text
Static_manifest_is_shared_and_immutable
Runtime_plans_are_independent
Scope_states_are_independent
One_runtime_has_one_global_tool_registry
All_scopes_in_runtime_share_tool_registry
Different_runtimes_do_not_share_tool_registry
Different_runtimes_do_not_share_cached_tools
Different_runtimes_do_not_share_actor_world
Different_runtimes_do_not_share_event_center
Disposing_runtime_a_keeps_runtime_b_running
Scope_dispose_does_not_clear_runtime_tools
Runtime_dispose_does_not_clear_static_manifest
No_layer_hub_current_exists
No_global_service_provider_exists
No_static_mutable_tool_cache_exists
Runtime_access_does_not_expose_scope_resources
```

---

## 17. 验收否决项

```text
LayerToolRegistry是进程级 static
LayerToolRegistry被改成每 Scope一份
不同 Runtime共享 Tool Cache或业务 Singleton
ScopeRuntime保存 ActorWorld
Static Manifest保存 Runtime / Service实例
Runtime Dispose清理其他 Runtime
存在 LayerHub.Current / GlobalServices / GlobalEventCenter
Tool Registry提供 Service Locator
模块静态构造修改全局 Registry
依赖全局 Reset维持测试隔离
```

本文只保证：

```text
不可变元数据可 static共享。

Runtime全局状态每 Runtime隔离。

Scope本地状态每 Scope隔离。

LayerTool属于 Runtime全局层，
供同一 Runtime所有 Scope使用。
```
