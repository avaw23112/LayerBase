# 09 Provide / From 的 Layer × Scope × SourceService 绑定迁移

> **强制执行规范：** 遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `08_di_scope_container_layer_first_revised.md`  
> **相关阶段：** `13_mount_layer_scope_boundary_revised.md`、`14_layer_tool_runtime_global_registry_revised.md`

---

## 0. 本阶段核心目的

Provide / From 是 DI 对象创建后的**本地资源引用绑定**。

绑定必须同时满足：

```text
相同 RuntimeGeneration
相同 LayerIndex
相同 ScopeId
```

最终逻辑地址：

```text
RuntimeGeneration
+ LayerIndex
+ ScopeId
+ ProviderServiceType
+ LocalKey
```

含义：

```text
LayerIndex：
    保持 Layer 的直接资源边界。

ScopeId：
    保持实例和线程边界。

ProviderServiceType：
    明确资源来自哪个 Service。

LocalKey：
    区分同一 Service 的多个资源。
```

Provide / From 不是跨 Layer、跨 Scope或 Runtime 全局资源系统。

---

## 1. 最终公有 API

Provide 保持显式 Key：

```csharp
public sealed partial class CombatService : IService
{
    [Provide("Combat.Registry")]
    private readonly CombatRegistry _registry = new();
}
```

From 必须显式声明来源 Service：

```csharp
public sealed partial class DamageService : IService
{
    [From(
        typeof(CombatService),
        "Combat.Registry")]
    private CombatRegistry _registry = null!;
}
```

保留：

```csharp
FromAttribute(
    Type providerServiceType,
    string localKey)
```

删除：

```csharp
[From]
[From("Combat.Registry")]
```

不得依据字段类型或 Key 猜测来源。

---

## 2. ProviderServiceType

`ProviderServiceType` 必须是当前 LayerProvider 中的 Service 注册身份。

它不是：

```text
字段类型
Context 类型
Layer 类型
LayerTool 类型
任意 Provider 对象类型
```

Provide 位于 Context 时，来源仍是 Context 的 OwnerService：

```csharp
[OwnerService(typeof(CombatService))]
public sealed partial class CombatContext : ILayerContext
{
    [Provide("Combat.Registry")]
    private readonly CombatRegistry _registry = new();
}
```

Consumer 仍写：

```csharp
[From(
    typeof(CombatService),
    "Combat.Registry")]
```

禁止使用 `CombatContext` 作为来源类型。

---

## 3. Layer-first Build 关系

Contribution 必须先进入：

```text
LayerBuildPlan
    → LayerScopeProviderPlan
        → Provide / From Contribution
        → BindingPlan
```

不能由 ScopePlan 扫描整个 Scope 对象集合。

每条 Contribution 至少记录：

```text
OwnerLayerIndex
OwnerScopeId
OwnerServiceType
MemberSlot
LocalKey
DeclaredType
SourceLocation
```

ScopeExecutionPlan 只保存冻结后的 Binding Range。

---

## 4. 绑定范围

### 同 Layer、同 Scope

允许：

```text
Service → Service
Service → Context
Context → Service
Context → Context
```

### 同 Scope、不同 Layer

Build Error：

```text
Cross-layer From is not allowed.
Use this.Call<TRequest,TResponse>().
```

### 同 Layer、不同 Scope

Build Error：

```text
Cross-scope From is not allowed.
Use ScopeEvent or ScopeCall.
```

InlineScope 与 MainScope 即使共享物理线程也不能绑定。

---

## 5. LayerTool 不参与 Provide / From

LayerTool 是：

```text
每 Runtime 一份的全局工具系统
同一 Runtime 的所有 Scope 均可直接使用
```

它不是当前 LayerProvider 的 Service 或 Context。

禁止：

```csharp
[From(
    typeof(SomeLayerTool),
    "Tool")]
private SomeLayerTool _tool = null!;
```

也禁止用 Provide 将 Tool 包装成 Scope-local资源。

正确访问：

```csharp
SomeLayerTool tool =
    this.Tools()
        .GetOrCreate<SomeLayerTool>();
```

必须长期持有 Scope Service / EcsWorld 的对象不应声明为 LayerTool。

---

## 6. 唯一性与类型契约

同一个来源 Service 内：

```text
ProviderServiceType + LocalKey
```

必须唯一。

不同 Service 可以使用相同 Key，由 ProviderServiceType 区分。

字段类型只负责赋值兼容：

```text
Provider Declared Type
    可赋值给
Consumer Field Type
```

诊断至少包含：

```text
Provider Layer / Scope / Service / Member / Type
Consumer Layer / Scope / Service / Field / Type
LocalKey
SourceLocation
```

---

## 7. Build Plan

冷路径键：

```csharp
internal readonly struct ProvideBindingKey
{
    internal readonly int LayerIndex;
    internal readonly int ScopeId;
    internal readonly RuntimeTypeHandle ProviderServiceType;
    internal readonly string LocalKey;
}
```

Freeze 后：

```csharp
internal readonly struct ProvideBindingPlan
{
    internal readonly int ProviderSlot;
    internal readonly int ProviderServiceSlot;
    internal readonly int ProviderObjectSlot;
    internal readonly int ProviderMemberSlot;

    internal readonly int ConsumerObjectSlot;
    internal readonly int ConsumerMemberSlot;

    internal readonly ProvideGetter Getter;
    internal readonly FromSetter Setter;
    internal readonly FromUnbinder Unbinder;
}
```

Running 不通过 Type、string 或 Dictionary 查找。

---

## 8. 绑定与解除顺序

OwnerScope Thread：

```text
1. 创建 LayerProvider。
2. 创建 Service / Context。
3. Attach ScopeObjectBinding。
4. Mount。
5. 收集 Provide值。
6. 执行 From Setter。
7. 注册 Event / LocalCall。
8. Initialize / RuntimeStart。
```

停止和释放：

```text
1. 关闭业务入口。
2. 解除 Event / LocalCall。
3. 执行 From Unbinder。
4. 清空 Consumer字段。
5. Dispose Context / Service / Provider。
```

Consumer 必须在 Provider Dispose 前解除引用。

---

## 9. 源生成器要求

复用 `faster`：

```text
Provide Getter
From Setter
Unbinder
Provider Type
Contract Type
LocalKey
SourceLocation
```

修正：

```text
ProviderType → ProviderServiceType
Contribution增加 OwnerLayer / OwnerScope
Context Provide关联 OwnerService
PlanBuilder只在同 Layer、同 Scope匹配
```

运行期禁止反射 Setter、字符串 Key查找和全 Scope搜索。

---

## 10. faster 分支复用

直接复用：

```text
SharedFieldAttributes.cs
ScopeResourceGenerator
IGeneratedScopeResourcePublisher
IGeneratedScopeResourceConsumer
ScopeResourceBindingTests
ScopeResourceGenerationTests
```

修改复用：

```text
ScopeResourceImportContribution
ScopeResourceExportContribution
ScopeResourcePlanBuilder
ScopeResourceRegistry
```

禁止新增第二套 Provide / From 系统。

---

## 11. Agent 执行任务

```text
1. 保留 From(Type, localKey)。
2. 删除无来源 Service 的 From 形式。
3. Contribution记录 OwnerLayer、OwnerScope、OwnerService。
4. Context Provide继承 OwnerService身份。
5. Build先写入 LayerBuildPlan。
6. 只在当前 Layer × Scope Provider解析。
7. 使用 ProviderServiceType + LocalKey唯一匹配。
8. Freeze为 Slot + Getter/Setter/Unbinder。
9. OwnerScope Thread执行 Bind/Unbind。
10. 跨 Layer提示 this.Call。
11. 跨 Scope提示 ScopeEvent/ScopeCall。
12. LayerTool只能通过 Tools API访问。
13. 复用 faster生成器和测试。
```

---

## 12. 必须测试

```text
From_requires_provider_service_type
From_provider_must_be_service_registration
Same_service_same_key_duplicate_fails
Different_services_can_use_same_key
Context_provide_uses_owner_service_identity
Same_layer_same_scope_from_binds
Same_scope_cross_layer_from_fails
Same_layer_cross_scope_from_fails
Layer_tool_cannot_be_from_provider
Layer_tool_is_accessed_through_tools_api
Consumer_unbinds_before_provider_dispose
No_runtime_reflection_binding
Steady_state_binding_uses_precomputed_slots
```

---

## 13. 验收否决项

```text
From不声明来源 Service
仅按字段类型或 Key推断来源
跨 Layer或跨 Scope绑定
ScopeResourceRegistry搜索整个 Scope
LayerTool通过 Provide / From注入
自动选择第一个 Provider
自动生成跨域代理
Consumer在 Provider Dispose后保留引用
```

本文只保证：

```text
Provide / From属于同 Layer、同 Scope业务资源绑定。

LayerTool属于 Runtime全局工具，
不进入 Provide / From。
```
