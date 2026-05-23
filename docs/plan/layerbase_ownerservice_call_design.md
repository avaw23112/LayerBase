# LayerBase OwnerService 与 Call 语义收敛设计方案

## 1. 文档目标

本文档用于指导 Codex 对 LayerBase 的自动装配语义进行一次收敛改造，重点包括两部分：

```text
1. 新增 OwnerServiceAttribute。
2. 收紧 CallAttribute 的可用位置。
```

改造后的目标模型：

```text
Layer
  ├── Service
  │     ├── Manager / ILayerContext
  │     ├── IEventHandler<TEvent>
  │     └── IEventHandlerAsync<TEvent>
  │
  └── ILayerCallHandler<TRequest, TResponse>
```

核心语义：

```text
Layer 是执行边界。
Service 是业务领域。
Manager / EventHandler 是 Service 领域内的处理单元。
CallHandler 是 Layer 级单目标功能切片。
```

---

## 2. 当前语义问题

### 2.1 OwnerLayer 已存在，但缺少 OwnerService

当前已经存在：

```csharp
[OwnerLayer(typeof(BattleLayer))]
public sealed partial class CombatService : IService
{
}
```

它表达：

```text
CombatService 属于 BattleLayer。
```

但 Manager / EventHandler 当前没有等价的类型级归属声明。

现在通常只能写：

```csharp
public sealed partial class CombatService : IService
{
    [Mount]
    private DamageManager _damageManager = null!;
}
```

该方式能完成挂载与注入，但语义上是“父级主动挂载子级”，不是“子级声明所属业务域”。

因此三层结构变成：

```text
Service -> Layer 有 OwnerLayer。
Manager -> Service 没有 OwnerService。
EventHandler -> Service 没有 OwnerService。
```

这会导致拓扑文档、生成器、AI Agent 和开发者都难以从类型本身判断业务归属。

---

### 2.2 Call 当前允许出现在 IService 上，语义不够收敛

当前 `CallAutoBindGenerator` 中 `[Call]` 方法允许出现在：

```text
1. Layer
2. IService
```

但最终决定应改为：

```text
[Call] 方法只能定义在 Layer 上。
IService 和 ILayerContext 都不允许定义 [Call] 方法。
```

原因：

```text
Call 表达 Layer 对外暴露的单目标功能切片。
Service 表达业务领域聚合。
ILayerContext / Manager 表达领域内处理单元。
```

如果允许 Service 定义 `[Call]`，会让 Call 同时具备“Layer 功能入口”和“Service 内部功能”的双重含义，破坏模型一致性。

---

## 3. 最终语义规则

### 3.1 OwnerLayer

`OwnerLayerAttribute` 表达类型级 Layer 归属。

支持对象：

```text
1. IService
2. ILayerCallHandler<TRequest, TResponse>
```

语义：

```text
Service 属于某个 Layer。
CallHandler 属于某个 Layer。
```

示例：

```csharp
[OwnerLayer(typeof(BattleLayer))]
public sealed partial class CombatService : IService
{
}

[OwnerLayer(typeof(BattleLayer))]
public sealed class GetBattleStateHandler :
    ILayerCallHandler<GetBattleStateRequest, GetBattleStateResponse>
{
    public LBTask<GetBattleStateResponse> HandleAsync(
        GetBattleStateRequest request,
        CancellationToken cancellationToken = default)
    {
        return LBTask.FromResult(new GetBattleStateResponse());
    }
}
```

---

### 3.2 OwnerService

新增 `OwnerServiceAttribute`，表达类型级 Service 归属。

支持对象：

```text
1. ILayerContext
2. IEventHandler<TEvent>
3. IEventHandlerAsync<TEvent>
```

不支持对象：

```text
1. IService
2. ILayerCallHandler<TRequest, TResponse>
3. Layer
```

语义：

```text
Manager / EventHandler 属于某个 Service 业务领域。
```

示例：

```csharp
[OwnerService(typeof(CombatService))]
public sealed partial class DamageManager : ILayerContext
{
    public int LayerIndex { get; set; }
}

[OwnerService(typeof(CombatService))]
public sealed partial class DamageEventHandler : IEventHandler<DamageEvent>
{
    public void Handle(in DamageEvent value)
    {
    }
}
```

---

### 3.3 Mount

`MountAttribute` 保持现有定位。

支持位置：

```text
1. Layer 字段 / 属性
2. IService 字段 / 属性
3. ILayerContext 字段 / 属性
4. 构造函数
```

语义：

```text
1. 父级显式挂载子级。
2. 控制装配顺序。
3. 控制字段 / 属性注入。
4. 通过 [Mount(typeof(TImpl))] 指定 interface / abstract 字段的具体实现类型。
5. 在构造函数上指定 DI 构造器。
```

`Mount` 与 `OwnerService` 的关系：

```text
Mount 是父级显式装配。
OwnerService 是子级类型归属声明。
Mount 的顺序控制优先级高于 OwnerService。
```

---

### 3.4 Call

`CallAttribute` 方法只允许定义在 `Layer` 类型上。

合法示例：

```csharp
public sealed partial class BattleLayer : Layer
{
    [Call]
    private LBTask<GetBattleStateResponse> GetBattleState(
        GetBattleStateRequest request,
        CancellationToken cancellationToken = default)
    {
        return LBTask.FromResult(new GetBattleStateResponse());
    }
}
```

不合法示例：

```csharp
public sealed partial class BattleService : IService
{
    [Call]
    private LBTask<GetBattleStateResponse> GetBattleState(
        GetBattleStateRequest request)
    {
        return LBTask.FromResult(new GetBattleStateResponse());
    }
}
```

```csharp
public sealed partial class BattleManager : ILayerContext
{
    [Call]
    private LBTask<GetBattleStateResponse> GetBattleState(
        GetBattleStateRequest request)
    {
        return LBTask.FromResult(new GetBattleStateResponse());
    }
}
```

如果需要独立功能切片，应使用 `ILayerCallHandler<TRequest, TResponse>` + `OwnerLayer`：

```csharp
[OwnerLayer(typeof(BattleLayer))]
public sealed class GetBattleStateHandler :
    ILayerCallHandler<GetBattleStateRequest, GetBattleStateResponse>
{
    public LBTask<GetBattleStateResponse> HandleAsync(
        GetBattleStateRequest request,
        CancellationToken cancellationToken = default)
    {
        return LBTask.FromResult(new GetBattleStateResponse());
    }
}
```

---

## 4. 需要新增的 Attribute

新增文件建议：

```text
LayerBase/DI/Options/OwnerServiceAttribute.cs
```

推荐实现：

```csharp
using System;

namespace LayerBase.DI.Options;

/// <summary>
/// 声明当前业务处理单元归属于哪个 IService。
///
/// 适用对象：
/// 1. ILayerContext / Manager。
/// 2. IEventHandler&lt;TEvent&gt;。
/// 3. IEventHandlerAsync&lt;TEvent&gt;。
///
/// 不适用对象：
/// 1. IService。
/// 2. ILayerCallHandler&lt;TRequest, TResponse&gt;。
/// 3. Layer。
///
/// 设计意图：
/// OwnerService 用于补齐 Layer -> Service -> Manager/EventHandler 的领域归属语义。
/// 它不负责控制装配顺序；如果需要顺序控制，应在 Service 内使用 [Mount] 字段。
/// </summary>
[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = true,
    Inherited = false)]
public sealed class OwnerServiceAttribute : Attribute
{
    /// <summary>
    /// 创建 OwnerService 标记。
    /// </summary>
    /// <param name="serviceType">
    /// 当前业务处理单元归属的 Service 类型。
    /// 该类型必须实现 LayerBase.DI.IService。
    /// </param>
    public OwnerServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }

    /// <summary>
    /// 当前业务处理单元归属的 Service 类型。
    /// </summary>
    public Type ServiceType { get; }
}
```

---

## 5. LayerServiceGenerator 改造方案

目标文件：

```text
LayerBase.Generator/LayerBase.Generator/LayerServiceGenerator.cs
```

### 5.1 新增元数据名

新增常量：

```csharp
private const string OwnerServiceAttributeName = "LayerBase.DI.Options.OwnerServiceAttribute";
```

### 5.2 新增 OwnerService 收集器

在 `Initialize` 中新增收集逻辑：

```csharp
var ownerServiceRegistrations = context.SyntaxProvider
    .ForAttributeWithMetadataName(
        OwnerServiceAttributeName,
        static (node, _) => node is ClassDeclarationSyntax,
        static (ctx, _) => CreateOwnerServiceRegistrations(ctx))
    .SelectMany(static (items, _) => items);
```

然后将它与现有数据合并。

当前已有：

```text
OwnerLayer registrations
Mount members
```

改造后应具备：

```text
OwnerLayer registrations
OwnerService registrations
Mount members
```

---

### 5.3 新增数据结构

新增类似 `ServiceRegistration` 的结构：

```csharp
private sealed class ServiceContextRegistration
{
    public ServiceContextRegistration(
        INamedTypeSymbol contextType,
        INamedTypeSymbol serviceType,
        Location? location)
    {
        ContextType = contextType;
        ServiceType = serviceType;
        Location = location;
    }

    public INamedTypeSymbol ContextType { get; }
    public INamedTypeSymbol ServiceType { get; }
    public Location? Location { get; }
}
```

字段含义：

```text
ContextType:
  被 [OwnerService] 标记的类型。
  可能是 ILayerContext、IEventHandler<T>、IEventHandlerAsync<T>。

ServiceType:
  OwnerServiceAttribute 中指定的 IService 类型。

Location:
  Attribute 位置，用于诊断报错。
```

---

### 5.4 新增 CreateOwnerServiceRegistrations

```csharp
private static ImmutableArray<ServiceContextRegistration> CreateOwnerServiceRegistrations(
    GeneratorAttributeSyntaxContext context)
{
    var contextType = (INamedTypeSymbol)context.TargetSymbol;
    var builder = ImmutableArray.CreateBuilder<ServiceContextRegistration>();

    foreach (var attribute in context.Attributes)
    {
        if (attribute.ConstructorArguments.Length != 1)
        {
            continue;
        }

        if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol serviceType)
        {
            continue;
        }

        var location = attribute.ApplicationSyntaxReference
            ?.GetSyntax()
            ?.GetLocation();

        builder.Add(new ServiceContextRegistration(
            contextType,
            serviceType,
            location));
    }

    return builder.ToImmutable();
}
```

---

### 5.5 校验规则

#### LBG/OWNER-SERVICE-001：OwnerService 目标必须实现 IService

规则：

```text
[OwnerService(typeof(X))]
X 必须实现 IService。
```

错误示例：

```csharp
[OwnerService(typeof(NotAService))]
public sealed class DamageManager : ILayerContext
{
}
```

---

#### LBG/OWNER-SERVICE-002：被标记类型必须是支持类型

允许：

```text
ILayerContext
IEventHandler<TEvent>
IEventHandlerAsync<TEvent>
```

禁止：

```text
IService
ILayerCallHandler<TRequest, TResponse>
Layer
其他普通类
```

错误示例：

```csharp
[OwnerService(typeof(CombatService))]
public sealed class GetCombatStateHandler :
    ILayerCallHandler<GetCombatStateRequest, GetCombatStateResponse>
{
}
```

诊断建议：

```text
CallHandler should use [OwnerLayer], not [OwnerService]. Call represents a Layer-level functional slice.
```

---

#### LBG/OWNER-SERVICE-003：OwnerService 与 Mount 冲突

如果某个类型被 Service A 显式 `[Mount]`，但该类型声明 `[OwnerService(typeof(ServiceB))]`，应报错。

示例：

```csharp
public partial class CombatService : IService
{
    [Mount]
    private DamageManager _damageManager = null!;
}

[OwnerService(typeof(SkillService))]
public sealed partial class DamageManager : ILayerContext
{
}
```

错误原因：

```text
DamageManager 被 CombatService 显式挂载，但声明归属于 SkillService。
```

---

#### LBG/OWNER-SERVICE-004：Mount 与 OwnerService 指向同一个 Service 时去重

如果 Service 显式 Mount 了某类型，而该类型也声明了相同 OwnerService：

```csharp
public partial class CombatService : IService
{
    [Mount]
    private DamageManager _damageManager = null!;
}

[OwnerService(typeof(CombatService))]
public sealed partial class DamageManager : ILayerContext
{
}
```

应只注册一次。

---

#### LBG/OWNER-SERVICE-005：Owner-only 注册顺序提示

如果 Service 中存在显式 `[Mount]` 成员，同时又存在只通过 `[OwnerService]` 归属进来的类型，应给 Warning。

原因：

```text
[Mount] 字段有源代码位置顺序。
[OwnerService] 类型没有 Service 内字段顺序。
```

建议文案：

```text
Service '{0}' has explicit [Mount] members but also has owner-only registrations ({1}). Owner-only registrations will be appended after mounted members without field-order semantics.
```

---

### 5.6 生成规则

对于：

```csharp
[OwnerService(typeof(CombatService))]
public sealed partial class DamageManager : ILayerContext
{
}
```

生成到 `CombatService` 的自动注册中：

```csharp
services.TryAddScoped<DamageManager, DamageManager>();
```

对于：

```csharp
[OwnerService(typeof(CombatService))]
public sealed partial class DamageEventHandler : IEventHandler<DamageEvent>
{
}
```

也生成：

```csharp
services.TryAddScoped<DamageEventHandler, DamageEventHandler>();
```

注意：

```text
OwnerService 不负责 interface / abstract 暴露类型。
如果需要 interface / abstract 暴露类型，应使用 [Mount(typeof(TImpl))] 字段。
```

---

## 6. CallAutoBindGenerator 改造方案

目标文件：

```text
LayerBase.Generator/LayerBase.Generator/CallAutoBindGenerator.cs
```

### 6.1 收紧 GetOwnerKind

当前允许：

```text
Layer
IService
```

应改为只允许：

```text
Layer
```

推荐实现：

```csharp
private static CallOwnerKind GetOwnerKind(
    INamedTypeSymbol ownerType,
    INamedTypeSymbol layerSymbol,
    INamedTypeSymbol serviceSymbol,
    INamedTypeSymbol? layerContextSymbol)
{
    if (InheritsFrom(ownerType, layerSymbol))
    {
        return CallOwnerKind.Layer;
    }

    return CallOwnerKind.Invalid;
}
```

说明：

```text
serviceSymbol 和 layerContextSymbol 可以继续传入，用于未来分类诊断。
但它们不再决定合法性。
```

---

### 6.2 删除 Service OwnerKind

当前：

```csharp
private enum CallOwnerKind
{
    Invalid = 0,
    Layer = 1,
    Service = 2
}
```

改为：

```csharp
private enum CallOwnerKind
{
    Invalid = 0,
    Layer = 1
}
```

如果 `CallMethodBinding.OwnerKind` 后续没有实际用途，可以保留但仅用于记录；也可以移除，按最小改动优先。

---

### 6.3 修改诊断文案

当前诊断大意：

```text
[Call] methods are only supported on Layer and IService types. ILayerContext modules must not declare [Call].
```

应改为：

```text
[Call] methods are only supported on Layer types. IService and ILayerContext modules must not declare [Call]. Use an explicit ILayerCallHandler<TRequest, TResponse> with [OwnerLayer] for Layer-level functional slices.
```

---

### 6.4 保留 Layer 上 [Call] 方法生成

合法示例：

```csharp
public sealed partial class BattleLayer : Layer
{
    [Call]
    private LBTask<GetBattleStateResponse> GetBattleState(
        GetBattleStateRequest request,
        CancellationToken cancellationToken = default)
    {
        return LBTask.FromResult(new GetBattleStateResponse());
    }
}
```

仍然生成内部 `ILayerCallHandler<TRequest, TResponse>` 包装类，并注册到当前 Layer。

---

### 6.5 保留独立 CallHandler + OwnerLayer

合法示例：

```csharp
[OwnerLayer(typeof(BattleLayer))]
public sealed class GetBattleStateHandler :
    ILayerCallHandler<GetBattleStateRequest, GetBattleStateResponse>
{
    public LBTask<GetBattleStateResponse> HandleAsync(
        GetBattleStateRequest request,
        CancellationToken cancellationToken = default)
    {
        return LBTask.FromResult(new GetBattleStateResponse());
    }
}
```

该流程由 `LayerServiceGenerator` 的 OwnerLayer 处理逻辑继续负责。

---

## 7. LayerServiceGenerator 对 CallHandler 的规则

保留当前逻辑：

```text
实现 ILayerCallHandler<TRequest, TResponse> 的类型可以通过 [OwnerLayer] 直接注册到 Layer。
```

禁止新增逻辑：

```text
不要允许 ILayerCallHandler<TRequest, TResponse> 使用 [OwnerService]。
不要让 CallHandler 进入 Service DI 流程。
```

原因：

```text
CallHandler 语义上就是 Layer 级单目标功能切片。
它不是 Service 内部的事件处理单元。
```

---

## 8. 推荐新增或修改的测试

### 8.1 OwnerService 注册 ILayerContext

测试目标：

```text
[OwnerService(typeof(CombatService))] 的 ILayerContext 能自动注册进 CombatService 的 IServiceCollection。
```

示例：

```csharp
[OwnerLayer(typeof(BattleLayer))]
public sealed partial class CombatService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

[OwnerService(typeof(CombatService))]
public sealed partial class DamageManager : ILayerContext, IInitializable
{
    public int LayerIndex { get; set; }

    public void Initialize()
    {
        OwnerServiceTests.Trace.Add("DamageManager.Initialize");
    }
}
```

验收：

```text
Build 后 DamageManager.Initialize 被调用。
```

---

### 8.2 OwnerService 注册 IEventHandler

测试目标：

```text
[OwnerService] 的 IEventHandler<TEvent> 能作为 Service 域内对象进入自动订阅流程。
```

验收：

```text
发送事件后 Handler 被调用。
```

---

### 8.3 OwnerService 不允许 ILayerCallHandler

测试目标：

```text
[OwnerService] 标记 ILayerCallHandler<TRequest, TResponse> 时报诊断错误。
```

错误文案应说明：

```text
CallHandler should use [OwnerLayer], not [OwnerService].
```

---

### 8.4 Service 上 [Call] 时报错

示例：

```csharp
public sealed partial class CombatService : IService
{
    [Call]
    private LBTask<TestResponse> Handle(TestRequest request)
    {
        return LBTask.FromResult(new TestResponse());
    }
}
```

验收：

```text
生成器报告错误。
错误说明 [Call] 只能定义在 Layer 上。
```

---

### 8.5 ILayerContext 上 [Call] 继续报错

保持现有非法逻辑，但更新诊断文案。

---

### 8.6 Layer 上 [Call] 仍然合法

验收：

```text
Layer 上的 [Call] 方法仍能生成内部 Handler，并完成 Call 调用。
```

---

### 8.7 独立 CallHandler + OwnerLayer 仍然合法

验收：

```text
[OwnerLayer] ILayerCallHandler<TRequest, TResponse> 仍能注册到对应 Layer。
Call 请求能命中该 Handler。
```

---

### 8.8 Mount 与 OwnerService 去重

示例：

```csharp
public partial class CombatService : IService
{
    [Mount]
    private DamageManager _damageManager = null!;
}

[OwnerService(typeof(CombatService))]
public sealed partial class DamageManager : ILayerContext
{
}
```

验收：

```text
DamageManager 只注册一次。
不重复初始化。
```

---

### 8.9 Mount 与 OwnerService 冲突

示例：

```csharp
public partial class CombatService : IService
{
    [Mount]
    private DamageManager _damageManager = null!;
}

[OwnerService(typeof(SkillService))]
public sealed partial class DamageManager : ILayerContext
{
}
```

验收：

```text
生成器报错。
```

---

## 9. 实现顺序建议

### Step 1：新增 OwnerServiceAttribute

新增：

```text
LayerBase/DI/Options/OwnerServiceAttribute.cs
```

---

### Step 2：扩展 LayerServiceGenerator 数据收集

新增收集：

```text
OwnerServiceAttribute -> ServiceContextRegistration
```

---

### Step 3：接入 Service 自动注册生成

将 `OwnerService` 归属的类型合并到对应 Service 的 `IAutoServiceMount.__AutoMountContexts` 中。

注意去重：

```text
Mount 注册优先。
OwnerService 同目标去重。
不同目标报冲突。
```

---

### Step 4：补充诊断规则

新增或复用 DiagnosticDescriptor。

建议诊断码可以使用：

```text
LBOS001 OwnerService target must implement IService.
LBOS002 OwnerService can only be used on ILayerContext or event handlers.
LBOS003 CallHandler must use OwnerLayer instead of OwnerService.
LBOS004 OwnerService conflicts with explicit Mount.
LBOS005 Owner-only registrations appended after Mount members.
```

也可以沿用现有 LBG / LBMOUNT 编号体系，但必须保证错误文案清楚。

---

### Step 5：收紧 CallAutoBindGenerator

修改：

```text
GetOwnerKind 只允许 Layer。
CallOwnerKind 删除 Service。
UnsupportedOwner 文案更新。
```

---

### Step 6：补测试

优先补：

```text
OwnerService ILayerContext 注册测试。
OwnerService EventHandler 自动订阅测试。
OwnerService 禁止 CallHandler 测试。
Service [Call] 禁止测试。
Layer [Call] 保持合法测试。
独立 CallHandler + OwnerLayer 保持合法测试。
```

---

## 10. 最终验收标准

改造完成后，应满足：

```text
1. Service 仍可用 [OwnerLayer] 挂到 Layer。
2. CallHandler 仍可用 [OwnerLayer] 挂到 Layer。
3. Manager / ILayerContext 可用 [OwnerService] 挂到 Service。
4. EventHandler / EventHandlerAsync 可用 [OwnerService] 挂到 Service。
5. CallHandler 不允许使用 [OwnerService]。
6. [Call] 方法只允许定义在 Layer 上。
7. IService 上定义 [Call] 会报错。
8. ILayerContext 上定义 [Call] 会报错。
9. [Mount] 仍然可用于显式装配、字段注入、顺序控制和 interface 实现绑定。
10. [Mount] 与 [OwnerService] 同目标时不重复注册。
11. [Mount] 与 [OwnerService] 不同目标时生成器报错。
12. LayerBase 的运行时热路径不应因为该改造增加额外开销。
```

---

## 11. 最终心智模型

改造完成后，LayerBase 的三层模型应表达为：

```text
Layer：
  执行边界、事件路由边界、Call 路由边界。

Service：
  业务领域聚合，组织 Manager 与 EventHandler。

Manager / ILayerContext：
  Service 领域内的状态、生命周期、业务上下文。

EventHandler：
  Service 领域内的事件响应单元。

CallHandler：
  Layer 级单目标功能切片，不属于 Service。
```

对应 Attribute：

```text
[OwnerLayer]
  Service -> Layer
  CallHandler -> Layer

[OwnerService]
  Manager / ILayerContext -> Service
  EventHandler -> Service
  EventHandlerAsync -> Service

[Mount]
  父级显式装配子级
  控制顺序
  控制注入
  控制 interface / abstract 实现绑定

[Call]
  只允许 Layer 方法使用
```
