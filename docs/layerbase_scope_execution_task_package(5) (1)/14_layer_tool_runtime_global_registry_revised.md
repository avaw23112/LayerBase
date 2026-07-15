# 14 LayerTool 的 Runtime 全局注册、访问、缓存与生命周期

> **最新架构规则：** LayerTool 是每 `LayerRuntime` 一份的全局工具系统，同一 Runtime 内任何 Scope 都可以直接使用。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `06_assembly_module_static_composition_revised.md`  
> **相关阶段：** `08_di_scope_container_layer_first_revised.md`、`16_global_static_runtime_scope_isolation_revised.md`

---

## 0. 核心结论

正确关系：

```text
LayerRuntime
    → LayerToolRegistry
        → Runtime全部 Tool Descriptor
        → Runtime全局 Tool Cache
```

访问：

```text
MainScope对象
InlineScope对象
WorkerScope对象
LayerRuntime API
    → 同一个 LayerToolRegistry
```

LayerTool 不是：

```text
进程级 static Registry
每 Scope独立 Registry
某个 Scope的业务资源
LayerProvider Service
跨 Scope代理
```

---

## 1. Runtime 全局而非进程全局

```text
Runtime A：
    Registry A
    Cached Tools A

Runtime B：
    Registry B
    Cached Tools B
```

必须满足：

```text
A.Registry != B.Registry
A.CachedTool != B.CachedTool
Dispose A不影响 B
```

静态层只允许共享：

```text
不可变 Tool Metadata
Factory / Invoker
TypeId
Manifest Contribution
```

---

## 2. Layer 的作用

Tool Contribution 必须有唯一 `OwnerLayerType`。

OwnerLayer表示：

```text
哪个 Push Layer贡献该 Tool
模块安装归属
Build诊断归属
```

OwnerLayer不表示：

```text
只有该 Layer或其 Scope能访问 Tool
```

未 Push OwnerLayer必须 Build Error。

Tool 不再包含 `OwnerScopeType`，也不按 Scope分区。

---

## 3. 最终公有 API

保持 `faster` 的全局访问模型：

```csharp
IView view =
    runtime.Tools
        .GetOrCreate<IView>(
            "Inventory");
```

任何 Scope中的对象：

```csharp
SomeTool tool =
    this.Tools()
        .GetOrCreate<SomeTool>();
```

API：

```text
Create<T>
Create<TContract>(key)
GetOrCreate<T>
GetOrCreate<TContract>(key)
ClearCache<T>
ClearCache<TContract>(key)
ClearAllCaches
Diagnostics
```

`this.Tools()` 通过对象 Binding 的 RuntimeAccess取得同一个 Registry。

不需要 `ScopeRef.GetTool`。

---

## 4. Tool 不是 DI、Mount 或 Provide资源

禁止：

```text
Tool Factory从当前 Scope DI解析 Service
Tool捕获 Scope Service / Context
Tool持有 EcsWorld / EventCenter / PostScheduler
Tool通过 Mount或 From注入
将 Tool注册成每 Scope Service
```

原因：

```text
同一个缓存 Tool可能被多个 Scope并发使用，
不能绑定首次调用 Scope的资源。
```

需要 Scope数据时，调用方把 DTO、Handle或只读参数传给 Tool方法。

必须长期持有 Scope资源的对象应实现为 Service / Context，而不是 LayerTool。

---

## 5. Tool 实例并发契约

### Cache=true

```text
一个 Runtime全局缓存实例
可能被多个 Scope并发调用
Tool必须不可变、无状态或线程安全
```

### Cache=false

```text
每次创建新实例
调用方拥有并负责 Dispose
Factory仍必须 Scope-neutral
```

禁止缓存 Tool持有：

```text
ScopeObjectBinding
ScopeRuntime
LayerProvider
Scope-local Service
EcsWorld
ActorWorld
```

主线程专用 Tool必须自行声明和校验线程契约；Registry不自动切换 Scope。

---

## 6. Attribute 与 Contribution

保留 `faster` 的 Attribute 形状，重点保留：

```text
ToolId
Contract
Key
Path
Cache
Factory
Layer
Service / Manager诊断元数据
```

不得增加：

```text
Scope属性
OwnerScopeType
ScopeToolPlan
ScopeToolRegistry
```

Contribution：

```csharp
public readonly struct LayerToolContribution
{
    public RuntimeTypeHandle OwnerLayerType { get; }
    public string ToolId { get; }
    public RuntimeTypeHandle ContractType { get; }
    public RuntimeTypeHandle ImplementationType { get; }
    public string Key { get; }
    public string? Path { get; }
    public bool Cache { get; }
    public LayerToolFactoryInvoker Factory { get; }
}
```

---

## 7. Runtime Tool Plan

```text
AssemblyModuleManifest.Tools
    → 解析 OwnerLayer到 Push LayerIndex
    → RuntimeLayerToolPlan
```

```csharp
internal sealed class RuntimeLayerToolPlan
{
    internal LayerToolDescriptor[] Entries;
    internal ToolLookupPlan Lookup;
}
```

Runtime全局唯一键：

```text
ContractType + Key
```

ImplementationType也必须在 Runtime内唯一。

OwnerLayer不是查找键，因此不同 Layer注册相同 Contract + Key必须冲突。

---

## 8. LayerToolRegistry

保持名称，归属 `LayerRuntime`：

```csharp
public sealed class LayerToolRegistry : IDisposable
{
    private readonly LayerToolDescriptor[] _entries;
    private readonly object?[] _cache;
    private readonly byte[] _states;
    private readonly int[] _creationOrder;
}
```

Build / Activate：

```text
合并 Contribution
冲突检查
分配 ToolSlot
构建 Lookup
创建 Registry
Freeze
```

Running 不允许新增注册。

生成式泛型入口应直接使用 ToolSlot。

字符串 Key API使用冻结 Lookup，不在每次调用分配 Tuple或 LINQ。

---

## 9. 多 Scope 并发缓存

`faster` 的普通 Dictionary和 `_cached ??=` 不足以支持多 Scope并发，需修改复用。

要求：

```text
Descriptor / Lookup冻结后只读
每个 CacheSlot独立状态
并发 GetOrCreate只发布一个实例
Factory失败后恢复 Empty
其他 Tool Slot不被阻塞
Factory不在全局 Registry锁中执行
```

状态至少表达：

```text
Empty
Creating
Ready
Disposing
Disposed
```

实现可使用现有 Once/CAS/每 Slot轻量同步结构，但不得新增线程或消息队列。

---

## 10. 创建上下文

删除：

```text
LayerToolCreateContext.Runtime
GetService<T>()
GetFactory<T>() 经 DI
ScopeId
ScopeRuntime
ActorWorld
```

最终 Context只提供：

```text
RuntimeId / Generation
当前 LayerToolRegistry
Create / GetOrCreate其他 LayerTool
只读 Runtime级配置（若确有现有接口）
```

外部 Factory不再由 DI解析，使用生成式创建路径。

创建优先级保持：

```text
1. 静态 [LayerToolFactory]
2. 显式外部 Factory
3. 公共无参构造
```

---

## 11. Cache 与 Clear

`Create` 永远创建非缓存实例。

`GetOrCreate`：

```text
Cache=true：
    返回 Runtime全局唯一实例

Cache=false：
    每次创建，调用方拥有
```

`ClearCache`：

```text
原子摘除缓存
允许后续重新创建
IDisposable实例 Dispose恰好一次
```

调用者必须保证不再使用已经取得的旧引用。

第一阶段不引入引用计数或 Lease。

`ClearAllCaches` 按创建逆序释放。

---

## 12. Runtime 生命周期

```text
Build / Activate：
    创建冻结 Registry，Tool懒创建

Running：
    所有 Scope可访问

CustomScope Stop / Dispose：
    不清理 Tool

MainScope业务 Stop / Dispose：
    Registry仍保留，直到所有 Scope业务回调结束

Runtime Dispose：
    关闭新 Tool创建
    逆序 Dispose缓存 Tool
    Dispose Registry
```

某个 Scope Dispose绝不能清除 Runtime全局缓存。

---

## 13. 与其他系统的边界

```text
DI：
    同 Layer、同 Scope Service。

Mount：
    同 Layer、同 Scope父子对象。

Provide / From：
    同 Layer、同 Scope资源引用。

LayerTool：
    Runtime全局工具。
```

禁止通过 Service、Mount、From重新 Scope化 Tool。

也禁止 Tool反向变成跨 Scope Service Locator。

---

## 14. Actor与引擎线程

ToolCreateContext不暴露 ActorWorld。

Actor算法 Tool接收 DTO / Handle，不直接修改 ActorWorld。

主线程引擎对象 Tool必须自行做 MainThread校验。

需要自动切到 MainScope的业务应由 MainScope Service + ScopeCall实现。

---

## 15. faster 分支复用

直接复用：

```text
LayerToolAttribute
LayerToolFactoryAttribute
ILayerToolFactory<T>
LayerToolGenerator发现和诊断
ToolId / Key / Path / Cache元数据
runtime.Tools API
LayerToolDiagnostics
基础 Registry测试
```

修改复用：

```text
LayerToolRegistry：
    保持 Runtime全局
    Freeze注册
    Slot查找
    多 Scope并发 Cache
    ClearCache Dispose

LayerToolEntry：
    Descriptor与 Cache状态分离

LayerToolCreateContext：
    删除 Runtime.GetService

LayerToolGenerator：
    输出 AssemblyModule Contribution
```

禁止：

```text
ScopeToolRegistry
ScopeToolPlan
OwnerScope Tool语义
进程级 static Registry
全局 Tool通过 Scope DI创建
```

---

## 16. Agent 执行任务

```text
1. 删除每 Scope Tool Registry设计。
2. 保留 LayerRuntime.Tools。
3. this.Tools()在所有 Scope返回同一 Registry。
4. Contribution只有 OwnerLayer，不含 OwnerScope。
5. Contract + Key按 Runtime全局唯一。
6. 生成 RuntimeLayerToolPlan和 ToolSlot。
7. Build后冻结注册。
8. 修改 Cache为多 Scope并发安全。
9. Factory不在全局大锁中执行。
10. 删除 CreateContext.GetService和 Runtime Root Provider。
11. 外部 Factory走生成式创建。
12. Tool不得捕获 Scope-local资源。
13. ClearCache正确 Dispose。
14. Runtime结束全部 Scope后再清理 Tool。
15. 删除 ScopeToolPlan / ScopeToolRegistry。
16. 复用 faster API、生成器、诊断和测试。
```

---

## 17. 必须测试

```text
One_runtime_has_one_layer_tool_registry
All_scopes_access_same_registry
Cached_tool_is_same_across_scopes
Different_runtimes_have_different_registries
Different_runtimes_have_different_cached_tools
Tool_has_no_owner_scope
Contract_key_is_unique_per_runtime
Concurrent_get_or_create_publishes_one_instance
Factory_failure_does_not_poison_cache
Factory_does_not_run_under_global_lock
Create_context_does_not_expose_service_provider
Tool_dependency_cycle_is_reported
Clear_cache_disposes_once
Scope_dispose_does_not_clear_tools
Runtime_dispose_clears_tools_after_all_scopes
Runtime_lookup_uses_precomputed_slot
```

---

## 18. 验收否决项

```text
每 Scope创建 Tool Registry或 Cache
Tool带 OwnerScope并限制其他 Scope
Tool Factory从 Scope DI解析 Service
Tool捕获 ScopeRuntime / EcsWorld / EventCenter
LayerToolRegistry是进程级 static
不同 Runtime共享缓存实例
并发 GetOrCreate产生多个缓存实例
Factory在全局大锁内执行
ClearCache泄漏或重复 Dispose
Scope Dispose清理 Runtime Tool
运行时扫描 Tool Attribute
```

本文只保证：

```text
LayerTool是每 Runtime一份的全局工具系统。

同一 Runtime的所有 Scope均可直接使用。

Tool不能绕过 Scope业务资源所有权。
```
