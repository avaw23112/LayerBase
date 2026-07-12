# LayerBase Publish / From Scope 资源权限方案

## 1. 最终定义

`Publish / From` 不再被定义为“跨 Layer 共享字段”。

它应定义为：

```text
Scope 内部的只读资源发布与访问能力系统。
```

核心公理：

```text
1. 资源只能由某个 IService 或 ILayerContext 发布。

2. 发布者和消费者必须属于同一个 ScopeRuntime。

3. 发布者拥有资源写权限。

4. 消费者只能获得 Scope 管理的只读访问能力。

5. 不允许把实际资源对象直接注入消费者字段。

6. 不允许 Layer、GlobalScope 或 LayerRuntime 发布业务资源。

7. 跨 Scope 数据只能通过 ScopeEvent、ScopeCall 或快照 DTO 传递。

8. Scope 停止后，所有资源访问能力立即失效。
```

---

# 2. API 命名调整

删除：

```csharp
ProvideAttribute
GlobalScope
```

新增：

```csharp
[AttributeUsage(
    AttributeTargets.Field |
    AttributeTargets.Property)]
public sealed class PublishAttribute : Attribute
{
    public PublishAttribute(string localKey)
    {
        LocalKey = localKey;
    }

    public string LocalKey { get; }
}
```

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class FromAttribute : Attribute
{
    public FromAttribute(
        Type providerType,
        string localKey)
    {
        ProviderType = providerType;
        LocalKey = localKey;
    }

    public Type ProviderType { get; }

    public string LocalKey { get; }
}
```

`Publish` 不再接收 OwnerType。

发布者就是该成员所在的 Service 或 Context。

`From` 中的 `ProviderType` 用于明确资源来自哪个 Service 或 Context。

---

# 3. 用户侧写法

## 3.1 Service 发布资源

推荐发布只读 View，而不是发布可变实现对象。

```csharp
public interface ICombatStateView
{
    int AliveCount { get; }

    bool Contains(int entityId);
}
```

```csharp
[OwnerLayer(typeof(GameplayLayer))]
[Scope<CombatScope>]
public sealed partial class CombatService :
    IService
{
    private readonly CombatState _state =
        new();

    [Publish(CombatResourceKeys.State)]
    private ICombatStateView StateView =>
        _state;

    public void UpdateState(...)
    {
        // 只有发布者持有可变 CombatState。
        _state.Update(...);
    }
}
```

## 3.2 同 Scope 消费资源

```csharp
[OwnerLayer(typeof(GameplayLayer))]
[Scope<CombatScope>]
public sealed partial class ThreatService :
    IService
{
    [From(
        typeof(CombatService),
        CombatResourceKeys.State)]
    private ScopeRead<ICombatStateView>
        _combatState;

    public void UpdateThreat()
    {
        ICombatStateView state =
            _combatState.Value;

        ...
    }
}
```

消费者获得的不是：

```csharp
ICombatStateView
```

而是：

```csharp
ScopeRead<ICombatStateView>
```

这样每次访问都必须经过 Scope 权限检查。

---

# 4. ScopeRead<TView>

```csharp
public readonly struct ScopeRead<TView>
    where TView : class
{
    private readonly ScopeRuntime? _scope;
    private readonly int _resourceSlot;
    private readonly int _generation;

    internal ScopeRead(
        ScopeRuntime scope,
        int resourceSlot,
        int generation)
    {
        _scope = scope;
        _resourceSlot = resourceSlot;
        _generation = generation;
    }

    public TView Value
    {
        [MethodImpl(
            MethodImplOptions.AggressiveInlining)]
        get
        {
            ScopeRuntime scope =
                _scope ??
                throw new InvalidOperationException(
                    "The Scope resource handle is not bound.");

            ScopeAccessGuard.RequireOwner(
                scope,
                "ScopeRead.Value");

            return scope.Resources.Get<TView>(
                _resourceSlot,
                _generation);
        }
    }

    public bool IsBound =>
        _scope != null;
}
```

它保证：

```text
在其他 Scope 调用：
    ScopeAccessViolationException

在外部线程调用：
    ScopeAccessViolationException

在 Scope 停止后调用：
    ScopeResourceClosedException

使用旧构建版本的 Handle：
    ScopeResourceGenerationException
```

---

# 5. ScopeResourceTable

每个 `ScopeRuntime` 独立拥有：

```csharp
internal sealed class ScopeResourceTable
{
    private ScopeResourceEntry[] _entries;
    private int _generation;
    private ScopeResourceTableState _state;

    public int Generation =>
        _generation;

    public TView Get<TView>(
        int slot,
        int generation)
        where TView : class;

    internal void Initialize(
        ScopeResourceEntry[] entries);

    internal void EnterStopping();

    internal void CloseAndClear();
}
```

Entry：

```csharp
internal readonly struct ScopeResourceEntry
{
    public ScopeResourceEntry(
        object view,
        int providerServiceSlot,
        int providerContextSlot,
        RuntimeTypeHandle viewType,
        int resourceId)
    {
        View = view;
        ProviderServiceSlot =
            providerServiceSlot;
        ProviderContextSlot =
            providerContextSlot;
        ViewType = viewType;
        ResourceId = resourceId;
    }

    public object View { get; }

    public int ProviderServiceSlot { get; }

    public int ProviderContextSlot { get; }

    public RuntimeTypeHandle ViewType { get; }

    public int ResourceId { get; }
}
```

运行期读取流程只有：

```text
Scope access check
数组下标读取
generation check
类型转换
```

不使用：

```text
Dictionary
Type 查找
字符串 Key 查找
反射
```

字符串 Key 只存在于生成元数据和诊断信息中。

---

# 6. Scope 资源标识

资源最终标识为：

```text
ScopeId
ProviderType
ModuleLocalResourceId
```

Scope 内的唯一键为：

```text
ProviderType + LocalKey
```

ScopeId 不需要放入 Scope 内部字典，因为每个 Scope 有独立 ResourceTable。

例如：

```text
CombatScope
    CombatService / "State"

NetworkScope
    NetworkService / "State"
```

互不冲突。

同一个 Provider 中不允许出现两个同名 Key。

---

# 7. 权限规则

## 7.1 发布者权限

只有发布成员所在的 Service/Context 能持有可变实现。

推荐：

```csharp
private readonly CombatState _state;

[Publish(Keys.State)]
private ICombatStateView StateView =>
    _state;
```

禁止直接发布：

```csharp
[Publish(Keys.State)]
private List<Entity> _entities;
```

应该发布：

```csharp
private readonly List<Entity> _entities;

private readonly ReadOnlyCollection<Entity>
    _entityView;

[Publish(Keys.State)]
private IReadOnlyList<Entity>
    EntityView =>
        _entityView;
```

## 7.2 消费者权限

`[From]` 字段必须是：

```text
ScopeRead<TView>
```

不能是：

```text
TView
List<T>
Dictionary<TKey,TValue>
ICollection<T>
IList<T>
IDictionary<TKey,TValue>
```

因此框架不会再把实际资源对象引用直接写入消费者字段。

## 7.3 View 类型规则

`TView` 必须满足下列之一：

```text
只读接口
IReadOnlyCollection<T>
IReadOnlyList<T>
IReadOnlyDictionary<TKey,TValue>
IEnumerable<T>
被 [ImmutableScopeResource] 标记的不可变引用类型
```

禁止发布值类型作为实时资源。

原因是值类型在绑定时会被复制，后续 Provider 修改字段不会同步给 Consumer。

需要传递值快照时应使用：

```text
ScopeEvent
ScopeCall
不可变 DTO
```

## 7.4 跨 Scope 权限

下面的情况直接报错：

```csharp
// CombatService 属于 CombatScope。

[From(
    typeof(CombatService),
    Keys.State)]
private ScopeRead<ICombatStateView>
    _state;

// 当前 Service 属于 NetworkScope。
```

错误：

```text
Resource 'CombatService.State'
belongs to Scope 'CombatScope',
but consumer 'NetworkService'
belongs to Scope 'NetworkScope'.

Direct resource access across Scopes is forbidden.
Use ScopeCall or ScopeEvent.
```

即使资源 View 是不可变接口，也不允许 `[From]` 跨 Scope。

这样规则始终一致，不需要判断对象是否“看起来线程安全”。

---

# 8. 删除 GlobalScope 和 Layer Resource

当前 Analyzer 允许：

```text
GlobalScope
Layer
IService
```

作为资源 Owner。这个规则必须删除。

最终只允许：

```text
IService
ILayerContext
```

Layer 需要业务资源时，应把逻辑放到 MainScope Service 中。

例如删除：

```csharp
[Publish(typeof(GameplayLayer), Keys.Config)]
```

改为：

```csharp
[Scope<MainScope>]
public sealed partial class
    GameplayConfigurationService :
    IService
{
    [Publish(Keys.Config)]
    private IGameplayConfiguration
        Config =>
            _config;
}
```

真正的 Runtime 全局工具继续使用：

```text
LayerRuntime.Tools
RuntimeToolRegistry
```

不进入 Publish/From 系统。

---

# 9. Module Manifest 扩展

当前 `ModuleManifest` 只有 Layer、Scope、Message、Service、Context 和 Handler Contribution。

新增：

```csharp
public delegate object
    ScopeResourceViewFactory(
        object provider);

public delegate void
    ScopeResourceConsumerBinder(
        object consumer,
        ScopeRuntime scope,
        int resourceSlot,
        int generation);
```

```csharp
public readonly struct
    ScopeResourceExportContribution
{
    public RuntimeTypeHandle ProviderType;
    public int ModuleLocalResourceId;
    public string LocalKey;
    public RuntimeTypeHandle ViewType;
    public ScopeResourceViewFactory Factory;
}
```

```csharp
public readonly struct
    ScopeResourceImportContribution
{
    public RuntimeTypeHandle ConsumerType;
    public RuntimeTypeHandle ProviderType;
    public string LocalKey;
    public RuntimeTypeHandle ViewType;
    public int ModuleLocalImportId;
    public ScopeResourceConsumerBinder Binder;
}
```

`ModuleManifest` 增加：

```csharp
IReadOnlyList<
    ScopeResourceExportContribution>
    ResourceExports;

IReadOnlyList<
    ScopeResourceImportContribution>
    ResourceImports;
```

---

# 10. Generator 生成私有成员访问代码

删除运行时：

```text
FieldInfo.GetValue
FieldInfo.SetValue
Activator.CreateInstance
GetCustomAttribute
MetadataCache
```

当前这些操作都存在于 `SharedFieldBinder`。

Generator 在 partial 类型中生成：

```csharp
partial class CombatService :
    IGeneratedScopeResourcePublisher
{
    object
        IGeneratedScopeResourcePublisher
            .GetPublishedResource(
                int resourceId)
    {
        return resourceId switch
        {
            0 => StateView,
            _ => throw new
                ArgumentOutOfRangeException(
                    nameof(resourceId))
        };
    }
}
```

消费者：

```csharp
partial class ThreatService :
    IGeneratedScopeResourceConsumer
{
    void
        IGeneratedScopeResourceConsumer
            .BindScopeResource(
                int importId,
                ScopeRuntime scope,
                int slot,
                int generation)
    {
        switch (importId)
        {
            case 0:
                _combatState =
                    new ScopeRead<
                        ICombatStateView>(
                            scope,
                            slot,
                            generation);
                return;

            default:
                throw new
                    ArgumentOutOfRangeException(
                        nameof(importId));
        }
    }
}
```

这些方法能够直接访问 private 成员，不需要反射，也适合 IL2CPP。

---

# 11. ScopeCompositionBuilder 构建顺序

当前 `ScopeCompositionBuilder` 已经负责将 Service、Context 按 Scope 分组并构建 Plan。

资源绑定必须进入同一构建链。

最终顺序：

```text
1. 创建所有 ScopeRuntime Shell。

2. 创建 Scope 内全部 Service。

3. 创建 Scope 内全部 Context。

4. 写入 ScopeObjectBinding。

5. 执行同 Scope DI Mount。

6. 收集该 Scope 的 ResourceExport。

7. 验证 Publish View 非 null。

8. 为 ResourceExport 分配连续 ResourceSlot。

9. 创建 ScopeResourceTable。

10. 验证所有 ResourceImport：
    - Provider 存在
    - View 类型兼容
    - Provider 与 Consumer Scope 相同

11. 给 Consumer 写入 ScopeRead<TView>。

12. 注册订阅和 Handler。

13. 执行 Initialize / PostBuild。

14. 启动 Scope。
```

资源 Slot 按以下顺序稳定分配：

```text
ProviderType.FullName
LocalKey
```

Generator 中的 `ModuleLocalResourceId` 只用于调用生成 Getter，不作为 Runtime Slot。

---

# 12. 跨程序集验证

当前 `SharedFieldAnalyzer` 只收集当前 Compilation 中的 Provide/From，因此外部程序集中的 Provider 会被误判为 Orphan。

新模型将验证分两层。

## 编译期 Analyzer

只检查当前成员形状：

```text
Publish/From 所在类型是否 partial
是否实现 IService/ILayerContext
Key 是否有效
Publish View 是否只读
From 字段是否为 ScopeRead<T>
是否存在同类型、同 Key 的本地重复
```

不再对跨程序集 `[From]` 直接报 Orphan。

## ModuleRuntimeBuilder

在所有已安装 Module 合并后检查：

```text
Provider 是否存在
Provider 是否唯一
Provider 和 Consumer Scope 是否一致
View Type 是否兼容
Service/Context 是否被安装
```

这样：

```text
Game.Foundation 发布 Resource Contract
Game.Combat 发布资源
Game.AI 消费资源
```

可以正常组合。

---

# 13. 生命周期

`ScopeResourceTable` 的状态：

```csharp
internal enum ScopeResourceTableState
{
    Building,
    Ready,
    Stopping,
    Closed
}
```

规则：

```text
Initialize / RuntimeStart：
    可访问资源。

正常 Pump：
    可访问资源。

IRuntimeStop：
    可访问资源。

Dispose：
    不允许再访问 ScopeRead。

所有对象 Dispose 完成：
    清空 ResourceEntry[]。
```

停止顺序：

```text
关闭 Scope 外部入口
执行 IRuntimeStop
关闭 ResourceTable
Dispose Consumer Context/Service
Dispose Provider Context/Service
清空 ResourceTable
```

明确规定：

> Service 的 `Dispose()` 不允许再读取 `[From]` 资源。需要依赖其他资源完成清理的逻辑必须放在 `IRuntimeStop` 中。

ResourceTable 不负责 Dispose 发布对象。

发布资源的生命周期仍由 Provider Service/Context 负责。

---

# 14. Layer 中删除的旧结构

从 `Layer` 删除：

```text
_sharedFields
SharedFields
RecordSharedField
GetSharedFieldParticipants
```

当前这些字段仍被 Layer 用于全局健康报告。

从 `LayerChain.Build()` 删除：

```csharp
SharedFieldBinder.Bind(...)
```

当前绑定发生在整个 Layer 集合上。

删除文件：

```text
LayerBase/DI/SharedFieldBinder.cs
```

将资源拓扑报告移动到：

```text
ScopeRuntime
ScopeCompositionPlan
ModuleRuntimeCatalog
```

调试输出按 Scope 展示：

```text
CombatScope
├─ CombatService.State
│  ├─ ThreatService
│  └─ CombatProjectionService
└─ NavigationService.Grid
   └─ CombatService
```

---

# 15. 诊断规则

建议重置现有 `LBG401—LBG405`，因为该子系统是破坏性重写。

```text
LBG401
同一 Provider 中存在重复 Publish Key。

LBG402
Publish 成员类型不是合法只读 View。

LBG403
From 字段必须是 ScopeRead<TView>。

LBG404
Publish/From 只能用于 IService 或 ILayerContext。

LBG405
Layer 和 GlobalScope 不再允许作为资源 Owner。

LBG406
没有找到 Provider。

LBG407
Provider 与 Consumer 不属于同一 Scope。

LBG408
Provider View 与 Consumer TView 不兼容。

LBG409
Publish 成员在绑定阶段返回 null。

LBG410
Publish/From 所在类型必须是 partial。

LBG411
Provider 对同一 Key 导出了多个不同 View。

LBG412
From 直接请求可写容器或可变具体类型。
```

---

# 16. 必须新增的测试

## Scope 隔离

```text
同 Scope Publish/From 绑定成功。

不同 Scope Publish/From Build 失败。

相同 Key 在不同 Scope 不冲突。

Layer 不能 Publish/From。

GlobalScope 类型被删除。
```

## 权限

```text
Consumer 只得到 ScopeRead<TView>。

Consumer 不能声明 From List<T>。

Consumer 不能声明 From ICollection<T>。

发布者可以修改 backing resource。

消费者可以读取只读 View。

其他 Scope 访问 ScopeRead.Value 抛异常。

Scope 外部线程访问抛异常。

Scope 停止后访问抛异常。
```

## Module

```text
Provider 和 Consumer 位于不同程序集但同 Scope 时成功。

当前 Compilation 找不到 Provider 时，
Analyzer 不错误报告 Orphan。

安装 Module 后仍找不到 Provider时，
Module Build 失败。

Context 发布资源时继承 OwnerService Scope。

Context 消费资源时继承 OwnerService Scope。
```

## 生命周期

```text
IRuntimeStop 中仍可读取 From。

Dispose 中读取 From 抛 ResourceClosed。

Scope Dispose 后 ResourceTable 不持有 Provider View。

Provider 返回 null 时 Build 失败。
```

## 性能与 AOT

```text
ScopeRead.Value 稳态零分配。

ScopeRead.Value 不查 Dictionary。

资源绑定不调用 FieldInfo。

资源绑定不调用 Activator。

IL2CPP High Stripping 下生成绑定可用。
```

---

# 17. 文件级修改

## 删除

```text
LayerBase/DI/SharedFieldBinder.cs
```

## 替换

```text
LayerBase/DI/SharedFieldAttributes.cs
    -> ScopeResourceAttributes.cs

LayerBase.Generator/SharedFieldAnalyzer.cs
    -> ScopeResourceGenerator.cs
```

## 新增

```text
LayerBase/Scope/Resources/ScopeRead.cs
LayerBase/Scope/Resources/ScopeResourceTable.cs
LayerBase/Scope/Resources/ScopeResourceEntry.cs
LayerBase/Scope/Resources/ScopeResourceTableState.cs
LayerBase/Scope/Resources/ScopeResourceExceptions.cs
LayerBase/Scope/Resources/IGeneratedScopeResourcePublisher.cs
LayerBase/Scope/Resources/IGeneratedScopeResourceConsumer.cs
```

## 修改

```text
LayerBase/Modules/ModuleManifest.cs
LayerBase/Modules/ModuleRuntimeBuilder.cs
LayerBase/Modules/ModuleRuntimeCatalog.cs
LayerBase/Scope/ScopeCompositionBuilder.cs
LayerBase/Scope/ScopeCompositionPlan.cs
LayerBase/Scope/ScopeRuntime.cs
LayerBase/Layer/Layer.cs
LayerBase/Layer/LayerChain.cs
LayerBase/Application/LayerRuntime.cs
LayerBase.Generator/AssemblyModuleGenerator.cs
```

---

# 18. 并入总方案后的优先级

Publish/From 重构应放在以下位置：

```text
阶段一：
Scope 生命周期和可关闭队列。

阶段二：
ScopeObjectBinding 与 Publish/From
资源权限系统。

阶段三：
ActorWorld 所有权。

阶段四：
Module/Dispatcher 去全局静态化。

阶段五：
DI、Mount、Service Route 热路径 Slot 化。
```

它不能放到最后做，因为当前 `SharedFieldBinder` 可以直接把一个 Worker Scope 的可变对象引用交给另一个 Scope，这会让前面完成的 ScopeAccessGuard 和队列隔离失去意义。

---

# 19. 最终规则

```text
Publish/From 是 Scope 内的只读能力授权。

Publish 者保留写所有权。

From 者获得 ScopeRead<TView>。

ScopeRead 每次读取都验证 OwnerScope。

资源不允许跨 Scope 直接引用。

Layer 和 GlobalScope 不参与业务资源发布。

所有绑定由 Generator 与 ScopeCompositionBuilder 完成。

运行时热路径只做 Scope 检查和数组读取。
```
