# 13 Mount 的 Layer × Scope 本地装配迁移

> **强制执行规范：** 遵守 `00_index_revised.md`、`01_mandatory_architecture_aot_performance_standards_revised.md`。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `08_di_scope_container_layer_first_revised.md`  
> **相关阶段：** `09_provide_from_layer_scope_binding_revised.md`、`14_layer_tool_runtime_global_registry_revised.md`

---

## 0. 本阶段核心目的

Mount 表达：

```text
一个 Layer中的父对象
显式拥有并排序
同 Layer、同 Scope中的子对象
```

完整边界：

```text
RuntimeGeneration
+ LayerIndex
+ ScopeId
```

Mount 不是跨 Layer依赖、跨 Scope依赖或 Runtime全局工具访问。

---

## 1. 最终公有 API

Layer Mount：

```csharp
public sealed partial class GameplayLayer : Layer
{
    [Mount]
    private CombatService _combat = null!;
}
```

Push 的 Layer实例是 MainScope运行目标，因此 Layer对象只能 Mount：

```text
当前 Layer
+
MainScope
```

Service Mount：

```csharp
[Scope<CombatScope>]
public sealed partial class CombatService : IService
{
    [Mount]
    private DamageContext _damage = null!;

    [Mount]
    private BuffContext _buff = null!;
}
```

显式实现类型保持：

```csharp
[Mount(
    Implementation = typeof(PooledDamageContext))]
private IDamageContext _damage = null!;
```

---

## 2. 允许关系

```text
Layer → 同 Layer、MainScope Service/Context

Service → 同 Layer、同 Scope Service/Context

Context → 同 Layer、同 Scope Service/Context
```

禁止同 Scope跨 Layer Mount：

```text
GameplayLayer Service
    → PresentationLayer Service
```

使用本地：

```csharp
await this.Call<Request, Response>(
    in request);
```

禁止同 Layer跨 Scope Mount，使用 ScopeEvent / ScopeCall。

---

## 3. LayerTool 不参与 Mount

LayerTool 是 Runtime全局工具，不属于 LayerProvider对象表或 Scope对象表。

禁止：

```csharp
[Mount]
private SomeLayerTool _tool = null!;
```

正确：

```csharp
SomeLayerTool tool =
    this.Tools()
        .GetOrCreate<SomeLayerTool>();
```

业务对象可以自行保存 Tool引用，但该引用遵守 LayerTool 的全局并发契约，不参与 Mount生命周期或 Dispose逆序。

---

## 4. Layer-first Mount Plan

权威关系：

```text
LayerBuildPlan
    → LayerScopeProviderPlan
        → LayerMountPlan
```

运行投影：

```text
ScopeExecutionPlan
    → ScopeLayerSlice
        → Mount Range
```

不能由 ScopeMountPlan搜索整个 Scope对象集合。

```csharp
internal readonly struct MountEntry
{
    internal readonly int ParentObjectSlot;
    internal readonly int ChildObjectSlot;
    internal readonly MountSetter Setter;
    internal readonly int MemberOrder;
}
```

Parent 和 Child 必须属于相同：

```text
LayerIndex
ScopeId
ProviderSlot
```

---

## 5. Build Planner

输入：

```text
当前 Layer × Scope ServiceFactoryPlan
当前 Layer × Scope ContextFactoryPlan
生成器 Mount成员元数据
```

步骤：

```text
1. 取得 Parent OwnerLayer / OwnerScope。
2. 只搜索当前 LayerScopeProviderPlan。
3. 解析唯一 Child Slot。
4. 校验成员类型和 Implementation。
5. 校验重复、自己 Mount和不允许的环。
6. 按成员声明顺序生成 Entry。
7. 投影 Mount Range到 ScopeLayerSlice。
```

依赖只存在其他 Layer时报告 Cross-layer Mount。

依赖只存在其他 Scope时报告 Cross-scope Mount。

---

## 6. ScopeMountContext

保留 `faster` 类型名和生成接口，但只传当前 LayerProvider范围：

```csharp
public readonly struct ScopeMountContext
{
    private readonly object[] _objects;
    private readonly int[] _dependencySlots;
    private readonly int _offset;

    public T GetAt<T>(
        int localDependencyId)
        where T : class;
}
```

禁止提供：

```text
GetFromOtherLayer
GetFromOtherScope
GetRuntime
GetServiceProvider
GetActorWorld
GetLayerTool
```

---

## 7. 执行与生命周期

OwnerScope Thread：

```text
1. 创建当前 LayerProvider。
2. 创建 Service / Context。
3. Attach Binding。
4. 执行当前 Layer × Scope Mount。
5. Provide / From。
6. Event / LocalCall绑定。
7. Initialize / RuntimeStart。
```

Worker Mount只能在 Worker Owner Thread执行。

第一阶段不增加通用 Unmount。

Dispose保持 `faster` 原逆序规则：

```text
From Unbind
Context逆序 Dispose
Service逆序 Dispose
Provider Dispose
Binding Detach
```

LayerTool不参与该顺序。

---

## 8. 生成器约束

保留：

```text
Owner类型 partial
Mount字段非 readonly/const
属性有 setter
Implementation concrete且可赋值
字段声明顺序
生成式 Setter
依赖类型元数据
```

新增诊断：

```text
Mount dependency belongs to another Layer
Mount dependency belongs to another Scope
Layer object mounts non-MainScope object
Mount target is a LayerTool
```

---

## 9. faster 分支复用

直接复用：

```text
IGeneratedScopeMount
IGeneratedScopeMountMetadata
ScopeMountContext
LayerServiceGenerator Mount生成
字段顺序测试
显式 Implementation诊断
```

修改复用：

```text
ScopeCompositionBuilder Mount逻辑
ScopeMountContext对象范围
Layer Mount归属
Service/Context Mount归属
```

禁止：

```text
跨 Layer/Scope dependency slot
自动 ScopeRef
Mount LayerTool
运行期反射
MainScope给 Worker对象赋值
```

---

## 10. Agent 执行任务

```text
1. 保留 faster Mount API和 Setter。
2. Parent/Child记录 OwnerLayer和 OwnerScope。
3. MountPlan先写入 LayerBuildPlan。
4. Planner只搜索当前 Layer × Scope Provider。
5. Layer对象只 Mount当前 Layer MainScope对象。
6. Service/Context只 Mount同 Layer、同 Scope对象。
7. 跨 Layer提示 this.Call。
8. 跨 Scope提示 ScopeEvent/ScopeCall。
9. LayerTool Mount直接 Build Error。
10. Worker Mount在 Owner Thread执行。
11. Mount先于 Provide/From。
12. 不增加代理、锁或运行期反射。
```

---

## 11. 必须测试

```text
Layer_mount_resolves_same_layer_main_scope_service
Layer_cannot_mount_other_layer_service
Layer_cannot_mount_custom_scope_service
Service_mount_resolves_same_layer_same_scope_service
Service_mount_resolves_same_layer_same_scope_context
Same_scope_cross_layer_mount_fails
Same_layer_cross_scope_mount_fails
Layer_tool_mount_fails
Tools_api_can_be_used_in_mounted_object
Worker_mount_runs_on_worker_owner_thread
Mount_uses_precomputed_slots
Mount_runs_before_provide_from
No_runtime_reflection_mount
```

---

## 12. 验收否决项

```text
Mount搜索整个 Scope对象表
Mount取得其他 Layer或 Scope对象
Layer对象 Mount CustomScope Service
MainScope给 Worker实例赋值
MountContext暴露 Runtime / Provider / ActorWorld
Mount自动生成代理
LayerTool通过 Mount注入
运行期反射查找
```

本文只保证：

```text
Mount是同 Layer、同 Scope的预计算父子装配。

LayerTool通过 Runtime全局 Tools API访问。
```
