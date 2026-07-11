# LayerBase AssemblyModule 跨程序集完整贡献模型

## 1. 修正后的核心前提

大型项目采用如下结构：

```text
Game.Foundation
├─ DTO
├─ Layer
└─ Scope

Game.Combat
├─ CombatService
├─ CombatContext
└─ Combat Handler

Game.Inventory
├─ InventoryService
├─ InventoryContext
└─ Inventory Handler

Game.Bootstrap
└─ LayerApplication
```

例如：

```text
Game.Foundation
    GameplayLayer
    CombatScope
    CalculateDamageCall
    DamageResult
    DamageChangedEvent

Game.Combat
    CombatService
    CombatContext
```

因此：

```csharp
[OwnerLayer(typeof(GameplayLayer))]
[Scope<CombatScope>]
public partial class CombatService : IService
{
}
```

这里的 `GameplayLayer` 和 `CombatScope` 都属于外部基础程序集。

业务程序集的 Generator 既不能生成：

```csharp
partial class GameplayLayer
{
}
```

也不能生成：

```csharp
partial class CombatScope
{
}
```

因为 C# partial 不能跨程序集组合。

所以 Module 必须承担两种不同职责：

```text
基础程序集 Module：
    导出 Layer、Scope、DTO 契约定义。

业务程序集 Module：
    向已有 Layer 和 Scope 贡献 Service、Context 和 Handler。
```

---

# 2. Module 的完整定义

AssemblyModule 是：

```text
一个程序集向 LayerBase Runtime 提交的完整静态清单。
```

它可能包含六类贡献：

```text
1. Layer Contract
2. Scope Definition
3. Message Contract
4. Service Contribution
5. Context Contribution
6. Handler / Dispatcher Contribution
```

不是每个 Module 都必须拥有全部六类内容。

例如：

```text
Game.FoundationModule
    Layer Contract
    Scope Definition
    Message Contract

Game.CombatModule
    Service Contribution
    Context Contribution
    Handler Contribution

Game.InventoryModule
    Service Contribution
    Context Contribution
```

---

# 3. 用户侧最终写法

## 3.1 基础程序集

基础程序集也必须声明 Module：

```csharp
namespace Game.Foundation;

[AssemblyModule]
public sealed partial class GameFoundationModule
{
}
```

Layer 不需要是 partial：

```csharp
public sealed class GameplayLayer : Layer
{
}

public sealed class PresentationLayer : Layer
{
}
```

Scope 也不再需要是 partial：

```csharp
[ScopeOptions(
    threading: ScopeThreadingMode.Worker,
    clock: ScopeClockMode.FixedRate,
    tickRateHz: 60,
    stopPolicy: ScopeStopPolicy.Drain)]
public sealed class CombatScope
{
}
```

DTO：

```csharp
[ScopeCall<CombatScope, DamageResult>]
public readonly struct CalculateDamageCall
{
    public CalculateDamageCall(
        int attackerId,
        int targetId,
        int skillPower)
    {
        AttackerId = attackerId;
        TargetId = targetId;
        SkillPower = skillPower;
    }

    public int AttackerId { get; }

    public int TargetId { get; }

    public int SkillPower { get; }
}

public readonly struct DamageResult
{
    public DamageResult(
        int damage,
        int remainingHealth)
    {
        Damage = damage;
        RemainingHealth = remainingHealth;
    }

    public int Damage { get; }

    public int RemainingHealth { get; }
}

[ScopeEvent<CombatScope>]
public readonly struct UpsertCombatantEvent
{
    public int EntityId { get; init; }

    public int Attack { get; init; }

    public int Defense { get; init; }

    public int Health { get; init; }
}
```

当前 Scope API 已经通过 `[ScopeOptions]` 定义运行方式，并通过 `[ScopeCall<TScope,TResult>]` 和 `[ScopeEvent<TScope>]` 把消息契约指向目标 Scope。新模型保留这些用户接口，但不再依赖修改 Scope partial。

---

## 3.2 Combat 业务程序集

```csharp
namespace Game.Combat;

[AssemblyModule]
public sealed partial class CombatModule
{
}
```

Service：

```csharp
[OwnerLayer(typeof(GameplayLayer))]
[Scope<CombatScope>]
public sealed partial class CombatService : IService
{
    [Mount]
    private CombatContext _context = null!;

    public void ConfigureServices(
        IServiceCollection services)
    {
    }

    [ScopeEvent]
    private void OnUpsertCombatant(
        UpsertCombatantEvent message)
    {
        _context.Upsert(
            message.EntityId,
            message.Attack,
            message.Defense,
            message.Health);
    }

    [ScopeCall]
    private DamageResult OnCalculateDamage(
        CalculateDamageCall request)
    {
        return _context.CalculateDamage(request);
    }
}
```

Context：

```csharp
[OwnerService(typeof(CombatService))]
public sealed partial class CombatContext :
    ILayerContext
{
    // 业务状态和逻辑。
}
```

用户仍然只表达：

```text
OwnerLayer
Scope
OwnerService
Mount
ScopeCall
ScopeEvent
```

不写任何 Module 注册代码。

---

# 4. 修正后的推导链

Generator 可以完整推导关系，因为信息分别来自不同特性。

```text
Service 所在程序集
    -> 所属 AssemblyModule

OwnerLayer
    -> 逻辑 Layer

Scope<TScope>
    -> 运行 Scope

OwnerService
    -> Context 所属 Service

Mount
    -> Context / 依赖注入关系

ScopeCall 方法
    -> Call Handler

ScopeEvent 方法
    -> Event Handler

DTO 上的 ScopeCall<TScope,TResult>
    -> Call Contract 和目标 Scope

DTO 上的 ScopeEvent<TScope>
    -> Event Contract 和目标 Scope
```

所以：

```text
Module 不需要猜测。
Module 只是在编译期汇总已有声明。
```

---

# 5. 两类 Module Contribution

## 5.1 Definition Contribution

通常由基础程序集产生。

```csharp
internal readonly struct ScopeDefinitionContribution
{
    public readonly RuntimeTypeHandle ScopeType;
    public readonly ScopeThreadingMode Threading;
    public readonly ScopeClockMode Clock;
    public readonly int TickRateHz;
    public readonly ScopeStopPolicy StopPolicy;
}
```

```csharp
internal readonly struct LayerContractContribution
{
    public readonly RuntimeTypeHandle LayerType;
}
```

```csharp
internal readonly struct ScopeMessageContractContribution
{
    public readonly RuntimeTypeHandle MessageType;
    public readonly RuntimeTypeHandle TargetScopeType;
    public readonly RuntimeTypeHandle ResultType;
    public readonly ScopeMessageKind Kind;
}
```

基础程序集 Generator 从：

```text
Layer 子类
ScopeOptions
ScopeCall<TScope,TResult>
ScopeEvent<TScope>
```

生成这些定义。

---

## 5.2 Implementation Contribution

由业务程序集产生。

最重要的是统一的 Service Contribution：

```csharp
internal readonly struct ServiceContribution
{
    public readonly RuntimeTypeHandle ServiceType;

    // 逻辑归属。
    public readonly RuntimeTypeHandle[] OwnerLayerTypes;

    // 运行归属。
    // 未标记 Scope<T> 时指向 MainScope。
    public readonly RuntimeTypeHandle OwnerScopeType;

    public readonly ServiceFactory Factory;

    public readonly ServiceBindingInitializer BindingInitializer;

    public readonly int ModuleLocalServiceId;
}
```

这意味着 Module 不只是：

```text
CombatService -> GameplayLayer
```

而是完整提交：

```text
CombatService
    -> GameplayLayer
    -> CombatScope
```

Context：

```csharp
internal readonly struct ContextContribution
{
    public readonly RuntimeTypeHandle ContextType;
    public readonly RuntimeTypeHandle OwnerServiceType;
    public readonly ContextFactory Factory;
    public readonly int ModuleLocalContextId;
}
```

Handler：

```csharp
internal readonly struct ScopeHandlerContribution
{
    public readonly RuntimeTypeHandle MessageType;
    public readonly RuntimeTypeHandle ServiceType;
    public readonly RuntimeTypeHandle ScopeType;

    public readonly int ModuleLocalHandlerId;

    public readonly ScopeMessageKind Kind;
}
```

---

# 6. Foundation Module 生成结果

用户只写：

```csharp
[AssemblyModule]
public sealed partial class GameFoundationModule
{
}
```

Generator 产生：

```csharp
public sealed partial class GameFoundationModule :
    ILayerBaseModule
{
    public static GameFoundationModule Instance { get; } =
        new();

    ModuleManifest ILayerBaseModule.Manifest =>
        GeneratedGameFoundationManifest.Value;
}
```

Manifest 概念上包含：

```csharp
internal static class GeneratedGameFoundationManifest
{
    internal static readonly ModuleManifest Value =
        new ModuleManifest(
            layerContracts:
            [
                LayerContract<GameplayLayer>(),
                LayerContract<PresentationLayer>()
            ],
            scopeDefinitions:
            [
                ScopeDefinition<CombatScope>(
                    ScopeThreadingMode.Worker,
                    ScopeClockMode.FixedRate,
                    60,
                    ScopeStopPolicy.Drain)
            ],
            messageContracts:
            [
                ScopeCallContract<
                    CalculateDamageCall,
                    CombatScope,
                    DamageResult>(),

                ScopeEventContract<
                    UpsertCombatantEvent,
                    CombatScope>()
            ],
            services: [],
            contexts: [],
            handlers: []);
}
```

这是生成代码，不是业务代码。

---

# 7. Combat Module 生成结果

Combat 程序集中：

```csharp
[AssemblyModule]
public sealed partial class CombatModule
{
}
```

Generator 概念上生成：

```csharp
internal static class GeneratedCombatManifest
{
    internal static readonly ModuleManifest Value =
        new ModuleManifest(
            layerContracts: [],
            scopeDefinitions: [],
            messageContracts: [],

            services:
            [
                new ServiceContribution(
                    serviceType:
                        typeof(CombatService)
                            .TypeHandle,

                    ownerLayerTypes:
                    [
                        typeof(GameplayLayer)
                            .TypeHandle
                    ],

                    ownerScopeType:
                        typeof(CombatScope)
                            .TypeHandle,

                    factory:
                        GeneratedCombatFactories
                            .CreateCombatService,

                    bindingInitializer:
                        GeneratedCombatBindings
                            .BindCombatService,

                    moduleLocalServiceId: 0)
            ],

            contexts:
            [
                new ContextContribution(
                    contextType:
                        typeof(CombatContext)
                            .TypeHandle,

                    ownerServiceType:
                        typeof(CombatService)
                            .TypeHandle,

                    factory:
                        GeneratedCombatFactories
                            .CreateCombatContext,

                    moduleLocalContextId: 0)
            ],

            handlers:
            [
                ScopeEventHandler<
                    CombatService,
                    UpsertCombatantEvent>(
                        localHandlerId: 0),

                ScopeCallHandler<
                    CombatService,
                    CalculateDamageCall,
                    DamageResult>(
                        localHandlerId: 1)
            ]);
}
```

这里没有修改：

```text
GameplayLayer
CombatScope
CalculateDamageCall
```

业务程序集只通过类型引用声明贡献。

---

# 8. Module Mode 下彻底禁止跨类型 partial 注入

Module Mode 中 Generator 只允许生成以下 partial：

```text
1. 当前程序集的 AssemblyModule partial。
2. 当前程序集的 Service partial。
3. 当前程序集的 Context partial。
```

用于：

```text
Service 私有 Handler Bridge
Mount 字段绑定
Context 自动注入
Service Factory
```

Generator 不允许生成：

```text
外部 Layer partial
外部 Scope partial
外部 DTO partial
```

事实上，新的 Module 模式下，即便 Layer 或 Scope 在当前程序集，也不需要通过它们的 partial 完成注册。

统一改为：

```text
Module Manifest Contribution
```

这样本地和跨程序集不再有两套注册机制。

---

# 9. Module Mode 与 Legacy Mode

## Legacy Mode

当前程序集没有 `[AssemblyModule]`：

```text
Layer、Service、Context 都位于同一程序集
```

可以暂时保留现有 Layer partial 自动注册方式。

如果出现任一跨程序集目标：

```text
OwnerLayer 指向外部 Layer
Scope<T> 指向外部 Scope
ScopeCall/Event DTO 指向外部 Scope
```

且当前程序集没有 `[AssemblyModule]`，产生编译错误。

不过推荐逐步把判断简化为：

```text
任何需要跨程序集参与 Runtime 组合的程序集，
都必须声明 AssemblyModule。
```

---

## Module Mode

存在 `[AssemblyModule]`：

```text
所有 Layer、Scope、DTO、Service、Context 和 Handler 信息
统一输出为 Module Manifest。
```

不再生成 Layer/Scope 注册 partial。

---

# 10. Runtime 合并流程

Module 合并必须分为定义阶段和实现阶段。

## Pass 1：收集 Module

Bootstrap Generator 根据项目引用生成 Module Catalog：

```text
GameFoundationModule
CombatModule
InventoryModule
NetworkModule
```

为每个 Module 分配：

```text
ModuleSlot
```

例如：

```text
Foundation  0
Combat      1
Inventory   2
Network     3
```

---

## Pass 2：收集定义

先处理：

```text
LayerContractContribution
ScopeDefinitionContribution
ScopeMessageContractContribution
```

建立冷路径注册表：

```text
Layer Type -> Layer Contract
Scope Type -> Scope Definition
Message Type -> Scope Message Contract
```

Scope Definition 只能有一个。

多个业务 Module 可以引用同一个 Scope，但不能重新定义它。

---

## Pass 3：收集实现

再处理：

```text
ServiceContribution
ContextContribution
ScopeHandlerContribution
```

验证每个 Service：

```text
OwnerLayer 必须存在
OwnerScope 必须存在
Factory 必须有效
Context OwnerService 必须存在
```

验证 Handler：

```text
DTO Contract 必须存在
Handler Scope 必须与 DTO 目标 Scope 一致
Handler Service 必须属于该 Scope
Call 只能有一个 Handler
Event 可以有多个 Handler
```

---

## Pass 4：统一分配运行时 ID

Module 内部 ID 只在本程序集有效：

```text
ModuleLocalServiceId
ModuleLocalHandlerId
ModuleLocalMessageId
```

Runtime Build 时统一产生：

```text
GlobalScopeId
ScopeServiceSlot
GlobalMessageRouteId
GlobalHandlerSlot
```

例如：

```text
MainScope    ScopeId 0
CombatScope  ScopeId 1
NetScope     ScopeId 2
```

当前 `ScopeRouteTable` 已经按照连续 ScopeId 构建数组，因此最终全局 ScopeId 继续由 Runtime 统一分配最合理。

---

# 11. Service 同时注册到 Layer 和 Scope

不能创建两份 Service。

错误模型：

```text
GameplayLayer 创建 CombatService A
CombatScope 创建 CombatService B
```

正确模型：

```text
CombatScopeRuntime
    拥有 CombatService 实例和生命周期

GameplayLayer
    持有 CombatService 的逻辑引用 / ServiceHandle
```

构建过程：

```text
1. 根据 ServiceContribution 创建一个 CombatService。
2. 将实例放入 CombatScopeRuntime.Services。
3. 分配 ScopeServiceSlot。
4. 将同一实例或 ServiceHandle 暴露给 GameplayLayer。
5. Context 由 CombatScopeRuntime 创建和管理。
```

也就是：

```text
Layer 负责逻辑归属和路由可见性。
ScopeRuntime 负责真实实例和生命周期所有权。
```

未标记 `[Scope<T>]` 的 Service：

```text
OwnerScope = MainScope
```

---

# 12. 多个业务 Module 共享同一个 Scope

例如：

```text
Game.Combat
    DamageService -> CombatScope

Game.AICombat
    ThreatService -> CombatScope

Game.StatusEffect
    BuffService -> CombatScope
```

三个 Module 都只提交：

```text
Service -> CombatScope
```

只有 Foundation Module 提交：

```text
CombatScope Definition
```

Runtime 合并后：

```text
CombatScopeRuntime
├─ DamageService
├─ ThreatService
└─ BuffService
```

不会创建三个 CombatScopeRuntime。

---

# 13. Scope Dispatcher 的模块化

当前生成式 Scope Call Dispatcher 会在目标 Scope 的 `IService[]` 中通过类型线性查找 Service。服务数量增长后，这会增加每次 Call 的扫描成本。

Module 模型应顺带改为直接 ServiceSlot。

每个业务 Module 生成本地 Dispatcher：

```csharp
internal static class GeneratedCombatDispatcher
{
    internal static void DispatchCall(
        ScopeRuntime scope,
        int serviceSlot,
        int localHandlerId,
        ScopeCallMessage message)
    {
        IService service =
            scope.Services[serviceSlot];

        switch (localHandlerId)
        {
            case 1:
                DispatchCalculateDamage(
                    (CombatService)service,
                    message);
                return;

            default:
                throw new InvalidOperationException(
                    $"Unknown Combat handler {localHandlerId}.");
        }
    }
}
```

运行时路由：

```csharp
internal readonly struct ScopeCallRoute
{
    public readonly int ScopeId;
    public readonly ushort ModuleSlot;
    public readonly ushort LocalHandlerId;
    public readonly int ServiceSlot;
}
```

Call 热路径：

```text
Global CallId
    -> ScopeCallRoute
    -> ScopeRuntime
    -> Module Dispatcher
    -> services[ServiceSlot]
    -> Handler
```

不需要：

```text
反射
Type 查找
Dictionary
FindService 线性扫描
```

---

# 14. Event 路由

Event 可以有多个 Handler：

```csharp
internal readonly struct ScopeEventHandlerRoute
{
    public readonly ushort ModuleSlot;
    public readonly ushort LocalHandlerId;
    public readonly int ServiceSlot;
}
```

```csharp
internal readonly struct ScopeEventRoute
{
    public readonly int ScopeId;
    public readonly int HandlerStart;
    public readonly int HandlerCount;
}
```

冻结后：

```text
EventRoute[]
EventHandlerRoute[]
```

全部为连续数组。

多个 Module 都可以向同一 DTO 提供 Handler，只要它们对应的 Service 都属于 DTO 指向的 Scope。

---

# 15. DTO Contract 与 Handler 解耦

基础程序集中的 DTO 只声明：

```text
这是什么消息
发往哪个 Scope
返回什么结果
```

例如：

```csharp
[ScopeCall<CombatScope, DamageResult>]
public readonly struct CalculateDamageCall
{
}
```

它不声明哪个 Service 实现。

业务 Module 的 Handler Contribution 声明：

```text
CombatService 处理 CalculateDamageCall
```

Build 时将两者匹配。

因此：

```text
DTO/Scope 可以稳定地放在 Foundation。
具体 Handler 可以分散在业务程序集。
```

这正是分程序集独立编译所需要的边界。

---

# 16. ModuleIgnore 的作用

`ModuleIgnore` 只排除自动实现贡献。

```csharp
[ModuleIgnore]
[OwnerLayer(typeof(GameplayLayer))]
[Scope<CombatScope>]
public sealed class DebugCombatService :
    IService
{
}
```

Generator 不产生：

```text
ServiceContribution
HandlerContribution
Context 自动创建贡献
```

但仍可把该类型作为普通内部 DI 类型手动注册。

如果被忽略的 Service 存在 `[ScopeCall]` 或 `[ScopeEvent]` Handler，给出警告：

```text
Handler 不会进入 Runtime 路由。
```

---

# 17. 修正后的编译错误

## LBM001：存在跨程序集贡献但无 AssemblyModule

触发条件包括：

```text
OwnerLayer 指向外部程序集
Scope<T> 指向外部程序集
ScopeCall/Event DTO 指向外部 Scope
OwnerService 指向外部 Service
```

错误：

```text
Assembly '{0}' contributes runtime metadata that references
types from another assembly, but no [AssemblyModule] is declared.
```

---

## LBM002：Scope 缺少定义

业务 Service：

```csharp
[Scope<CombatScope>]
```

但所有已安装 Module 都没有导出：

```csharp
[ScopeOptions] CombatScope
```

Build 失败：

```text
Service 'CombatService' targets Scope 'CombatScope',
but no installed Module defines that Scope.
```

---

## LBM003：Handler Scope 不一致

```text
CalculateDamageCall -> CombatScope
CombatService -> NetworkScope
```

编译或 Build 失败：

```text
Handler service 'CombatService' belongs to 'NetworkScope',
but message 'CalculateDamageCall' targets 'CombatScope'.
```

---

## LBM004：重复 Scope Definition

两个 Module 都定义同一个 Scope：

```text
Scope 'CombatScope' is defined by multiple Modules.
Only one ScopeOptions definition is allowed.
```

---

## LBM005：Call 没有 Handler

可以根据模式设为 Warning 或 Build Error：

```text
Call contract 'CalculateDamageCall' has no installed handler.
```

推荐默认 Build Error。

---

## LBM006：Call 有多个 Handler

```text
Call contract 'CalculateDamageCall' has multiple handlers:
CombatService, AlternativeCombatService.
```

Build Error。

---

# 18. 修正后的生成器职责

## Foundation 程序集 Generator

生成：

```text
Module Identity
Layer Contracts
Scope Definitions
DTO Contracts
Module Export
```

## 业务程序集 Generator

生成：

```text
Module Identity
Service Contributions
Context Contributions
Service Factories
Mount Bindings
Handler Contributions
Module-local Dispatchers
Module Export
```

## Bootstrap Generator

生成：

```text
Application Module Catalog
Layer 构建入口
Module 收集顺序
Runtime.Build()
```

三者生成的是不同层次的信息。

---

# 19. 推荐项目结构

```text
Game.Foundation
├─ GameFoundationModule.cs
├─ Layers/
├─ Scopes/
└─ Contracts/

Game.Combat
├─ CombatModule.cs
├─ CombatService.cs
├─ CombatContext.cs
└─ Internal/

Game.Inventory
├─ InventoryModule.cs
├─ InventoryService.cs
└─ InventoryContext.cs

Game.Bootstrap
└─ GameApplication.cs
```

Foundation 相对稳定。

修改 Combat 只重编译：

```text
Game.Combat
Game.Bootstrap
```

不会重编译：

```text
Game.Inventory
Game.Foundation
```

除非修改公共 DTO、Layer 或 Scope。

---

# 20. 最终用户体验

基础程序集：

```csharp
[AssemblyModule]
public sealed partial class GameFoundationModule
{
}

public sealed class GameplayLayer : Layer
{
}

[ScopeOptions(
    threading: ScopeThreadingMode.Worker,
    clock: ScopeClockMode.FixedRate,
    tickRateHz: 60)]
public sealed class CombatScope
{
}

[ScopeCall<CombatScope, DamageResult>]
public readonly struct CalculateDamageCall
{
}
```

业务程序集：

```csharp
[AssemblyModule]
public sealed partial class CombatModule
{
}

[OwnerLayer(typeof(GameplayLayer))]
[Scope<CombatScope>]
public sealed partial class CombatService :
    IService
{
    [Mount]
    private CombatContext _context = null!;

    public void ConfigureServices(
        IServiceCollection services)
    {
    }

    [ScopeCall]
    private DamageResult Handle(
        CalculateDamageCall call)
    {
        return _context.Calculate(call);
    }
}
```

用户仍然不写：

```text
AddService
AddScopedService
AddContext
AddScopeHandler
UseModule
```

---

# 21. 最终定案

```text
1. Foundation Module 注册 Layer Contract、Scope Definition 和 DTO Contract。

2. 业务 Module 注册 Service、Context 和 Handler。

3. ServiceContribution 同时包含 OwnerLayer 和 OwnerScope。

4. Module 不给外部 Layer 或 Scope 生成 partial。

5. Layer 和 Scope 在 Module 模式下都不需要 partial。

6. 一个 Service 只创建一个实例。

7. ScopeRuntime 拥有 Service 实例和生命周期。

8. Layer 持有该 Service 的逻辑引用或 ServiceHandle。

9. 多个 Module 可以向同一个 Scope 贡献 Service。

10. 一个 Scope Definition 只能来自一个 Module。

11. DTO Contract 与业务 Handler 分离。

12. Runtime Build 时合并所有 Module，并统一分配 ScopeId、ServiceSlot 和消息路由。

13. ModuleIgnore 排除 Service、Context 自动创建和 Handler 路由贡献。

14. 没有 AssemblyModule 时，任何跨程序集 Runtime 贡献都直接编译报错。
```

一句话总结：

```text
AssemblyModule 不只是把 Service 挂到 Layer；

它负责导出当前程序集全部 Runtime 元数据，
并让 Runtime 在 Build 时把基础程序集里的 Layer、Scope、DTO，
与业务程序集里的 Service、Context、Handler，
组合成同一套 LayerRuntime 和 ScopeRuntimeHost。
```
