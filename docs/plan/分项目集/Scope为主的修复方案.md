# LayerBase 基于真实代码的 LayerRuntime—ScopeRuntime 重构方案

## 0. 改造结论

保留 `LayerRuntime`，但重新确定其职责：

```text
LayerRuntime
    应用级聚合根
    Layer 层级所有者
    ActorWorld 所有者
    ScopeRuntimeHost 所有者
    Module Catalog 所有者
    异常汇总入口
    全局工具与诊断入口
    主线程 Pump 根节点

ScopeRuntime
    业务资源所有者
    Service / Context 所有者
    本地事件域
    本地 ECS 域
    本地时间域
    本地 DI 域
    本地执行域
```

不删除：

```text
LayerRuntime
Layer
ScopeRuntime
ScopeRuntimeHost
```

删除的是：

```text
LayerRuntime 中与 ScopeRuntime 重复的业务资源
Layer 中的业务资源与 Service 生命周期
ServiceLayerBinding 的 Runtime 资源倾向
ScopeServiceOwnerRegistry 旁路绑定
LayerRuntime fallback
```

最终结构：

```text
LayerRuntime
├─ Layer[]
├─ LayerHierarchy / RouteIndex
├─ ScopeRuntimeHost
│  ├─ MainScopeRuntime
│  ├─ CombatScopeRuntime
│  ├─ NetworkScopeRuntime
│  └─ ...
├─ ActorWorld
├─ LayerExceptionHub
├─ ModuleRuntimeCatalog
├─ FullSnap / Debug / Profiler / Trace
└─ Runtime 生命周期

ScopeRuntime
├─ EventCenter
├─ PostScheduler
├─ TimeScheduler
├─ DelayManager
├─ EcsWorld
├─ EcsQueryRegistry
├─ EcsScheduler
├─ ScopeServiceProvider
├─ IService[]
├─ ILayerContext[]
├─ SubscriptionRegistry
├─ Continuation Queue
├─ Post / Call Inbox
└─ Scope 生命周期
```

---

# 1. 当前真实结构与问题

## 1.1 LayerRuntime 当前承担了过多业务资源

当前 `LayerRuntime` 同时是：

```text
Layer 层级容器
EventCenter 所有者
PostScheduler 所有者
Timer 所有者
ECS World 所有者
ActorWorld 所有者
ScopeRuntimeHost 所有者
ExceptionHub 所有者
```

同时 `Pump` 还会推进旧 Runtime 资源与 ScopeHost。

问题不是 LayerRuntime 存在，而是：

```text
LayerRuntime 与 ScopeRuntime 同时拥有同类业务资源。
```

因此业务 API 很容易选择错误的资源入口。

---

## 1.2 ScopeRuntime 已经具备大部分正确资源

当前 `ScopeRuntime` 已经拥有：

```text
EventCenter
PostScheduler
Timer
EcsWorld
EcsQueryRegistry
Services
Post Inbox
Call Inbox
Continuation Queue
ScopeExecution
Worker Runner
```

这说明不需要重新发明 ScopeRuntime。

需要做的是：

```text
把它从“额外的执行容器”
提升为“业务资源和对象的真正所有者”。
```

---

## 1.3 Layer 仍然是业务容器

当前 `Layer` 仍负责：

```text
ServiceProvider
Service 注册和获取
自动订阅
Subscription Token
DelayPublisher
Layer EventCenter 路由
Service 生命周期
```

这会造成两个冲突：

```text
Service 的物理生命周期属于 Layer。
Service 的执行生命周期又属于 Scope。
```

正确模型必须变成：

```text
ScopeRuntime
    物理拥有 Service 和 Context。

Layer
    只记录 Service 的逻辑归属与层级位置。
```

---

## 1.4 当前 Binding 是双轨制

当前实际存在：

```text
ServiceLayerBinding
    -> LayerRuntime
    -> Layer
    -> Runtime EventCenter

ScopeServiceOwnerRegistry
    -> ScopeRuntime
```

`IService` 的部分扩展方法会先查 Scope Registry。

`ILayerContext` 没有对应 Scope Registry，因此继续使用：

```text
binding.Runtime.EcsWorld
binding.Runtime.Scheduler
binding.Runtime.Actors
```

这已经造成：

```text
Service.Query()
    -> ScopeRuntime.EcsWorld

Context.Query()
    -> LayerRuntime.EcsWorld
```

当前代码中 `IService.ECSWorld()` 已经优先返回 OwnerScope World，而 `ILayerContext.ECSWorld()` 仍直接返回 `ServiceLayerBinder.RequireBinding(context).Runtime.EcsWorld`。

---

## 1.5 Module 创建链尚未真正构建 Layer-Service-Context-Scope

当前 `ModuleRuntimeBuilder` 已经收集：

```text
LayerContract
ScopeDefinition
MessageContract
ServiceContribution
ContextContribution
HandlerContribution
```

但是 `ScopeRuntimeHost.CreateFromCatalog()` 当前只：

```text
读取 ScopeDefinition
读取 ServiceContribution
调用 Service.Factory
按 Scope 分组
创建 ScopeRuntime
```

没有：

```text
创建 Context
执行 Context Factory
执行 BindingInitializer
把 Service 注册到 OwnerLayer
创建 Scope 级 ServiceProvider
执行 Mount
执行自动 Subscribe
```

所以当前 Module 路径还只是：

```text
Module -> ScopeRuntime 中的裸 Service 数组
```

不是完整的 LayerBase 业务对象构建。

---

# 2. LayerRuntime 最终保留什么

## 2.1 保留当前 LayerRuntime 身份

`LayerRuntime` 继续作为：

```csharp
public sealed class LayerRuntime : IDisposable
{
    public int Id { get; }

    public int Version { get; }

    public IReadOnlyList<Layer> Layers { get; }

    public ScopeRuntimeHost ScopeHost { get; }

    public ActorWorld Actors { get; }

    public LayerExceptionHub ExceptionHub { get; }

    public ModuleRuntimeCatalog ModuleCatalog { get; }

    public void Start();

    public void Pump(float deltaTime);

    public void Stop();

    public void Dispose();
}
```

---

## 2.2 LayerRuntime 保留 Layer 层级

继续保留：

```text
Layer[]
Layer 类型到 LayerIndex 的索引
Layer 前后关系
Layer 父子关系
RouteIndex
层级最大数量限制
Layer 的 Build 顺序
Layer 调试和依赖关系
```

原有 Layer 的宏观逻辑意义不变。

例如：

```text
InputLayer
    ↓
GameplayLayer
    ↓
PresentationLayer
```

Scope 中的本地 EventCenter 仍然可以按照 LayerIndex 处理 Handler 顺序。

---

## 2.3 LayerRuntime 保留 ActorWorld

当前方向中 ActorWorld 仍然是整个 LayerRuntime 唯一实例。

```csharp
public ActorWorld Actors { get; }
```

只有 LayerRuntime：

```text
创建 ActorWorld
Pump ActorWorld
Dispose ActorWorld
```

ScopeRuntime 不再拥有和释放 ActorWorld。

Scope 与 ActorWorld 通讯通过：

```text
ActorCommandOutbox
ActorMessageQueue
ProjectedActorOutbox
ActorGateway
```

Worker Scope 不直接并发调用 ActorWorld。

---

## 2.4 LayerRuntime 保留异常汇总

继续保留：

```text
LayerExceptionHub
LayerHubExceptionCallbacks
Runtime 级停止策略
应用级日志入口
```

Scope 捕获异常后：

```text
ScopeRuntime
    -> LayerRuntime.ReportException(record)
    -> LayerExceptionHub
    -> owner thread Drain
```

异常记录补齐：

```text
RuntimeId
ScopeId
LayerIndex
ServiceSlot
ContextSlot
TaskId
Trace
Phase
ExceptionDispatchInfo
```

LayerRuntime 只负责聚合，不提供业务 EventCenter。

---

## 2.5 LayerRuntime 保留全局工具

可以保留：

```text
FullSnap
运行时 Profiler
TraceId 生成器
Module Catalog
路由诊断
全局只读配置
Debug View
类型 ID 表
运行时版本号
```

判断标准：

```text
不属于某个业务 Scope；
本身线程安全、只读或由 LayerRuntime owner thread 独占。
```

---

# 3. 必须从 LayerRuntime 删除的字段

从当前 `LayerRuntime` 删除：

```text
EventCenter
Scheduler / PostScheduler
Timer
EcsWorld
EcsQueryRegistry
EcsScheduler
业务 SynchronizationContext
业务 ServiceProvider
WorldServiceRoot
DelayManager
业务订阅集合
```

不提供 MainScope 兼容代理。

也就是说最终不存在：

```csharp
runtime.EventCenter
runtime.Scheduler
runtime.Timer
runtime.EcsWorld
runtime.GetService<T>()
runtime.Send(...)
runtime.Post(...)
```

用户已经明确不要求旧 API 兼容，因此应直接删除。

---

# 4. LayerRuntime.Pump 的真实目标结构

重构后的 `LayerRuntime.Pump` 只负责应用级推进：

```csharp
public void Pump(float deltaTime)
{
    ThrowIfDisposed();
    EnsureOwnerThread();

    // 1. 推进 MainScope 和 Inline Scope。
    ScopeHost.Pump(deltaTime);

    // 2. 消费所有 Scope 产生的 Actor 命令，
    //    然后只推进一次 ActorWorld。
    DrainActorCommands();
    Actors.Pump(deltaTime);

    // 3. 汇总 Worker / Inline Scope 异常。
    DrainExceptionHub();

    // 4. 推进真正的全局工具。
    PumpGlobalTools(deltaTime);
}
```

不能再出现：

```text
LayerRuntime Scheduler Pump
LayerRuntime Timer Pump
LayerRuntime ECS Pump
ScopeHost Pump
```

四套业务更新并行存在。

---

# 5. ScopeRuntime 最终结构

基于当前 `ScopeRuntime` 扩展，而不是重写一个新类型。

```csharp
public sealed class ScopeRuntime : IDisposable
{
    private readonly IService[] _services;
    private readonly ILayerContext[] _contexts;
    private readonly IDisposable[] _subscriptions;

    public int ScopeId { get; }

    public ScopeDescriptor Descriptor { get; }

    public LayerRuntime OwningRuntime { get; }

    public EventCenter EventCenter { get; }

    public PostScheduler PostScheduler { get; }

    public TimeScheduler<ITimerAction> Timer { get; }

    public ScopeDelayManager DelayManager { get; }

    public World EcsWorld { get; }

    public EcsQueryRegistry EcsQueryRegistry { get; }

    public EcsScheduler EcsScheduler { get; }

    public ScopeServiceProvider ServiceProvider { get; }

    public IReadOnlyList<IService> Services =>
        _services;

    public IReadOnlyList<ILayerContext> Contexts =>
        _contexts;
}
```

---

## 5.1 ScopeRuntime 新增的责任

当前 ScopeRuntime 需要新增：

```text
Context 实例所有权
ScopeServiceProvider
Scope SubscriptionRegistry
Scope DelayManager
Scope 层级 Handler 表
Service/Context Binding
Mount 初始化
完整生命周期数组
```

---

## 5.2 ScopeRuntime 删除的责任

从 ScopeRuntime 删除：

```text
ActorWorld 所有权
ActorWorld Pump
ActorWorld Dispose
```

当前 `ScopeRuntimeHost.Create` 会把同一个 `sharedActorWorld` 传给每个 ScopeRuntime。新设计中取消这个构造参数；ScopeRuntime 只持有一个向 LayerRuntime 提交 Actor 命令的 Gateway。

---

# 6. Layer 最终保留什么

不将 Layer 退化成纯类型，但删除其业务资源。

保留：

```csharp
public abstract class Layer
{
    public int RouteIndex { get; internal set; }

    public LayerRuntime OwnerContext { get; internal set; }

    public Layer? Previous { get; internal set; }

    public Layer? Next { get; internal set; }

    internal LayerServiceHandle[] ServiceHandles { get; set; }
}
```

可以继续保留：

```text
Layer 名称
Layer 类型
层级关系
RouteIndex
Service 逻辑归属列表
事件依赖统计
Produced / Subscribed Event 统计
调试信息
```

---

## 6.1 从 Layer 删除

删除：

```text
ServiceProvider
ServiceCollection
RegisterService
GetService
InitializeServices
DisposeServices
Subscribe
SubscribeAsync
SubscribeParallel
SubscribeFlow
OnEvent
Send
Post
Delay
DelayPublisher
Subscription Token
AutoBind EventCenter
```

特别删除当前用于临时切换 Scope EventCenter 的：

```text
s_autoBindEventCenter
EnterAutoBindEventCenter
```

因为订阅本来就应该直接注册到 OwnerScope EventCenter，而不是通过 Layer 临时替换 EventCenter。

---

## 6.2 LayerServiceHandle

Layer 记录 Service 的逻辑位置：

```csharp
internal readonly struct LayerServiceHandle
{
    public LayerServiceHandle(
        RuntimeTypeHandle serviceType,
        int scopeId,
        int serviceSlot)
    {
        ServiceType = serviceType;
        ScopeId = scopeId;
        ServiceSlot = serviceSlot;
    }

    public RuntimeTypeHandle ServiceType { get; }

    public int ScopeId { get; }

    public int ServiceSlot { get; }
}
```

例如：

```text
GameplayLayer
├─ CombatService
│  └─ CombatScope / Slot 0
├─ InventoryService
│  └─ MainScope / Slot 1
└─ NetworkSyncService
   └─ NetworkScope / Slot 0
```

Layer 可以展示和分析这些 Service，但不能直接调用它们。

---

# 7. 统一 ScopeObjectBinding

删除：

```text
ServiceLayerBinding
ServiceLayerBinder
ScopeServiceOwnerRegistry
IGeneratedScopeServiceBinding 的独立 OwnerScope 字段
```

新增唯一 Binding：

```csharp
internal sealed class ScopeObjectBinding
{
    public ScopeObjectBinding(
        LayerRuntime runtime,
        ScopeRuntime scope,
        int serviceSlot,
        int contextSlot,
        LayerMembership membership,
        ScopeObjectKind kind)
    {
        Runtime = runtime;
        Scope = scope;
        ServiceSlot = serviceSlot;
        ContextSlot = contextSlot;
        Membership = membership;
        Kind = kind;
    }

    public LayerRuntime Runtime { get; }

    public ScopeRuntime Scope { get; }

    public int RuntimeId =>
        Runtime.Id;

    public int ScopeId =>
        Scope.ScopeId;

    public int ServiceSlot { get; }

    public int ContextSlot { get; }

    public LayerMembership Membership { get; }

    public ScopeObjectKind Kind { get; }
}
```

---

## 7.1 为什么是 LayerMembership 而不是单个 Layer

当前 Module 模型的 `ServiceContribution` 保存：

```text
OwnerLayerTypes[]
```

而不是一个 OwnerLayer。

因此 Binding 不应继续假设一个 Service 只能对应一个 Layer。

```csharp
internal readonly struct LayerMembership
{
    public LayerMembership(
        int start,
        int count)
    {
        Start = start;
        Count = count;
    }

    public int Start { get; }

    public int Count { get; }
}
```

实际 LayerIndex 存在 Runtime 的连续表中。

---

## 7.2 Generator 生成 Binding 字段

当前 `ManagerAutoSubscribeGenerator` 已给 Service/Context 生成 `ILayerBindingAccessor`，并且只给 IService 生成 `OwnerScope` 和 `ServiceId`。

改成 Service 和 Context 都生成：

```csharp
partial class CombatService :
    IScopeObjectBindingAccessor
{
    private ScopeObjectBinding?
        __scopeObjectBinding;

    ScopeObjectBinding
        IScopeObjectBindingAccessor.Binding
    {
        get => __scopeObjectBinding
            ?? throw new UnboundScopeObjectException(
                typeof(CombatService));

        set => __scopeObjectBinding = value;
    }
}
```

Context 同样生成。

不再分别生成：

```text
__layerBaseBinding
OwnerScope
ServiceId
```

---

# 8. Scope 级 DI

## 8.1 改造现有 ServiceProvider

当前 `ServiceProvider` 使用：

```text
WorldServiceRoot
OwnerLayer
```

并把 Singleton/Instance 绑定为 Runtime Service，把 Scoped/Transient 绑定为 Layer Service。

这一套语义全部删除。

将其改造为：

```csharp
internal sealed class ScopeServiceProvider :
    IServiceProvider,
    IDisposable
{
    private readonly ScopeRuntime _scope;
    private readonly ScopeServiceDescriptor[] _descriptors;
    private readonly object?[] _instances;

    public object? GetService(Type serviceType);

    public T Get<T>();
}
```

---

## 8.2 生命周期语义

业务 DI 只保留：

```text
ScopeSingleton
Transient
Instance
```

含义：

```text
ScopeSingleton
    每个 ScopeRuntime 一个实例。

Transient
    在当前 Scope 内每次创建。

Instance
    明确归属当前 Scope 的外部实例。
```

不再存在业务意义上的：

```text
Runtime Singleton
Layer Scoped
跨 Scope Singleton
```

LayerRuntime 的全局工具不通过业务 `IService` DI 注册，而通过独立的：

```text
IRuntimeTool
RuntimeToolRegistry
```

管理。

---

## 8.3 跨 Scope DI 禁止

构造函数或 `[Mount]` 依赖必须属于同一 Scope。

```text
CombatService
    -> CombatContext
    -> DamageCalculator

全部属于 CombatScope。
```

禁止：

```text
CombatService
    -> InventoryService
```

Generator 或 Module Build 报错：

```text
LBSC100:
Service 'CombatService' in Scope 'CombatScope'
directly depends on 'InventoryService'
in Scope 'InventoryScope'.

Use ScopeCall or ScopeEvent.
```

---

# 9. Context 必须在 Scope 中创建

当前 `ContextContribution` 已进入 Catalog，但尚未在 `CreateFromCatalog` 中消费。

新建：

```csharp
internal sealed class ScopeCompositionBuilder
```

负责完整构建。

流程：

```text
1. 创建 LayerRuntime。
2. 构建 Layer 层级并分配 RouteIndex。
3. Build ModuleRuntimeCatalog。
4. 修正 ScopeId / ServiceSlot / ContextSlot。
5. 创建 ScopeRuntime Shell。
6. 每个 Scope 创建 ScopeServiceProvider。
7. 按 ServiceSlot 创建 Service。
8. 给 Service 写 ScopeObjectBinding。
9. 按 ContextContribution 创建 Context。
10. Context 继承 OwnerService Binding。
11. 执行 BindingInitializer / Mount。
12. 注册本地 Event Handler。
13. 注册 Scope Call/Event Handler。
14. 生成 LayerServiceHandle。
15. 冻结 ScopeRouteTable。
16. 启动 Scope。
```

Context Binding：

```csharp
contextBinding = new ScopeObjectBinding(
    runtime: serviceBinding.Runtime,
    scope: serviceBinding.Scope,
    serviceSlot: serviceBinding.ServiceSlot,
    contextSlot: contextSlot,
    membership: serviceBinding.Membership,
    kind: ScopeObjectKind.Context);
```

---

# 10. 替换 ScopeRuntimeHost.CreateFromCatalog

当前方法不应该继续边读 Catalog 边创建裸 Service 数组。

改为接受已经完成的 CompositionPlan：

```csharp
internal static ScopeRuntimeHost Create(
    LayerRuntime runtime,
    ScopeCompositionPlan plan)
```

计划结构：

```csharp
internal sealed class ScopeCompositionPlan
{
    public ScopePlan[] Scopes { get; init; }

    public ScopeCallRoute[] CallRoutes { get; init; }

    public ScopeEventRoute[] EventRoutes { get; init; }

    public ScopeEventHandlerRoute[]
        EventHandlerRoutes { get; init; }
}
```

```csharp
internal sealed class ScopePlan
{
    public ScopeDescriptor Descriptor { get; init; }

    public ScopeServicePlan[] Services { get; init; }

    public ScopeContextPlan[] Contexts { get; init; }
}
```

---

# 11. 修复当前 ScopeId 错位

当前 `ModuleRuntimeBuilder.AllocateScopeIds()` 从 0 开始给自定义 Scope 分配 ID。

但 `ScopeRuntimeHost.CreateFromCatalog()` 又先加入：

```csharp
Main ScopeId = 0
```

然后自定义 Scope 使用：

```csharp
sid + 1
```

而 Call/Event Route 保存的仍然是原始 `sid`。

最终方案：

```text
MainScope 固定 ScopeId = 0。

所有自定义 Scope 从 1 开始分配。

Catalog、Descriptor、RouteTable、ScopeRuntime
全程使用同一个 ScopeId。

禁止在 Create 阶段再执行 sid + 1。
```

修改：

```csharp
private static IReadOnlyDictionary<
    RuntimeTypeHandle,
    int> AllocateScopeIds(...)
{
    int nextScopeId = 1;

    // 按类型名稳定排序。
}
```

---

# 12. 修复当前 ServiceSlot 错位

当前 `ModuleRuntimeBuilder.AllocateServiceSlots()` 按：

```text
OwnerScope 分组
ServiceType 名称排序
```

分配 Slot。

但 `CreateFromCatalog()` 当前按 `catalog.Services` 原顺序：

```csharp
scopeServiceLists[scopeId].Add(instance);
```

写入数组。

因此：

```text
Route.ServiceSlot
不一定等于
ScopeRuntime.Services 数组下标。
```

最终必须由 `ServiceSlot` 决定数组位置：

```csharp
var services =
    new IService[scopePlan.ServiceCount];

services[servicePlan.ServiceSlot] =
    servicePlan.Factory();
```

Generator 不能自行重新计算 ServiceSlot。

删除生成器中按 Handler 列表重新 `AllocateServiceSlots()` 的逻辑。

唯一 Slot 权威：

```text
ModuleRuntimeBuilder / ScopeCompositionBuilder。
```

生成 Dispatcher 必须使用传入的：

```csharp
serviceSlot
```

```csharp
var service =
    (CombatService)scope.ServiceSlots[
        serviceSlot];
```

---

# 13. ScopeRuntimeHost 和 ScopeRouteTable

保留当前 `ScopeRouteTable` 的数组路由结构。

它已经按照 ScopeId 建立：

```text
ScopeRuntime?[]
```

并通过目标 ScopeId 执行 TryPost/TryCall。

修改点：

```text
1. ScopeId 必须由 CompositionBuilder 一次性确定。
2. ScopeRuntimeHost 不再修改 ScopeId。
3. ScopeRouteTable 不接收旧 ScopeType Resolver fallback。
4. RouteTable 属于对应 LayerRuntime，不允许全局静态路由。
```

---

# 14. 删除 Global Dispatcher Registry

当前最新 Generator 会向：

```text
GlobalDispatcherRegistry.PostDispatcher
GlobalDispatcherRegistry.CallDispatcher
```

写入静态 Dispatcher。

这在多程序集、多 LayerRuntime 下是不安全的：

```text
后初始化的程序集覆盖前一个 Dispatcher。
多个 Runtime 共用同一静态入口。
```

Module 模型已经有：

```text
ModuleSlot
ModuleCallDispatchHandler[]
ModuleEventDispatchHandler[]
```

因此删除：

```text
GlobalDispatcherRegistry
GeneratedScopeRuntimeHostFactory 静态注册 Dispatcher
IScopeHostFactoryRegistrar
ScopeHostFactory 全局单槽
```

Dispatcher 数组由：

```text
LayerRuntime.ModuleCatalog
```

构建，并传入该 Runtime 的 ScopeRouteTable。

---

# 15. Layer 级逻辑与 Scope EventCenter

每个 Scope 有独立 EventCenter，但 EventCenter 仍使用 Layer 顺序。

订阅记录：

```csharp
internal readonly struct ScopeEventHandler
{
    public int LayerIndex { get; }

    public int ServiceSlot { get; }

    public int HandlerSlot { get; }
}
```

在一个 Scope 中：

```text
同一 Event 类型的 Handler
按 LayerIndex 排序。
```

但不会访问其他 Scope 的 Handler。

例如：

```text
CombatScope
    GameplayLayer / CombatService
    PresentationLayer / CombatProjectionService

NetworkScope
    GameplayLayer / NetworkSyncService
```

即使都订阅 `TickEvent`：

```text
CombatScope.TickEvent
不进入 NetworkScope.EventCenter。
```

---

# 16. 自动订阅改造

当前 Generator 生成：

```csharp
auto.AutoBind(layer);
```

然后调用：

```csharp
layer.Subscribe(...)
```

新接口：

```csharp
internal interface IAutoScopeSubscribe
{
    void Bind(
        in ScopeSubscriptionContext context);
}
```

```csharp
public readonly struct ScopeSubscriptionContext
{
    public ScopeRuntime Scope { get; }

    public LayerMembership Membership { get; }

    public int ServiceSlot { get; }
}
```

Generator 生成：

```csharp
void IAutoScopeSubscribe.Bind(
    in ScopeSubscriptionContext context)
{
    context.Subscribe<DamageEvent>(
        this.OnDamage);
}
```

Subscription Token 保存到：

```text
ScopeRuntime.SubscriptionRegistry
```

不再保存到 Layer。

---

# 17. Delay、Timer 和 Scheduler

全部位于 ScopeRuntime：

```text
service.SchedulePost()
context.SchedulePost()
service.Delay()
context.Delay()
```

统一解析：

```csharp
binding.Scope.Timer
binding.Scope.PostScheduler
binding.Scope.DelayManager
```

LayerRuntime 和 Layer 都没有 Timer/Delay API。

---

# 18. ECS

所有 ECS API：

```text
IService.Query
IService.ECSWorld
IService.ECSQueryRegistry

ILayerContext.Query
ILayerContext.ECSWorld
```

统一执行：

```csharp
ScopeObjectBinding binding =
    ScopeObjectBinder.Require(instance);

ScopeAccessGuard.EnsureCurrent(binding);

return binding.Scope.EcsWorld;
```

不保留 Runtime fallback。

---

# 19. Service/Context Extension

统一内部实现：

```csharp
internal static class ScopeBoundObjectExtensions
{
    public static ScopeObjectBinding
        RequireBinding(object value);

    public static EventHandledState SendCore<T>(
        object value,
        in T message);

    public static bool PostCore<T>(
        object value,
        in T message);

    public static World ECSWorldCore(
        object value);
}
```

`IService` 和 `ILayerContext` 只提供薄重载。

这样不会再出现：

```text
Service Extension 修了 Scope。
Context Extension 忘记修。
```

---

# 20. this.Scope() 扩展

当前 `ScopeBindingResolver` 会从 `ServiceLayerBinding.Layer.OwnerContext` 反推 LayerRuntime。

改为：

```csharp
ScopeObjectBinding binding =
    ScopeObjectBinder.Require(instance);
```

Accessor 保存：

```csharp
public readonly struct ScopeAccessor<TScope>
{
    private readonly LayerRuntime _runtime;
    private readonly ScopeRuntime _sourceScope;
    private readonly int _targetScopeId;
}
```

调用：

```csharp
this.Scope().Post(message);

await this
    .Scope<InventoryScope>()
    .Call(request);
```

来源 Scope 始终是：

```text
binding.Scope
```

目标路由来自：

```text
binding.Runtime.ScopeHost.Routes
```

---

# 21. ActorWorld

## 21.1 LayerRuntime 唯一拥有 ActorWorld

```csharp
public ActorWorld Actors { get; }
```

创建：

```text
LayerRuntime 构造阶段
```

Pump：

```text
LayerRuntime.Pump
```

Dispose：

```text
LayerRuntime.Dispose
```

## 21.2 Scope 使用 ActorGateway

```csharp
public sealed class ScopeActorGateway
{
    private readonly LayerRuntime _runtime;
    private readonly int _sourceScopeId;

    public bool TryPost<T>(
        ActorId actorId,
        in T message);
}
```

每个 ScopeRuntime 有：

```csharp
public ScopeActorGateway Actors { get; }
```

注意这个属性不再返回 `ActorWorld`。

删除当前：

```text
service.Actors() -> ActorWorld
context.Actors() -> ActorWorld
```

改成：

```text
service.Actors() -> ScopeActorGateway
context.Actors() -> ScopeActorGateway
```

ActorWorld 的高级直接访问只留给 LayerRuntime owner-thread 工具 API。

---

# 22. LayerExceptionHub

保留在 LayerRuntime，但 ScopeRuntime 是异常来源。

```csharp
internal void ScopeRuntime.ReportException(
    LayerExceptionRecord record)
{
    OwningRuntime.ReportException(record);
}
```

删除任何：

```text
找不到 Scope 时回落 Primary Runtime
找不到 Runtime 时回落静态 LayerHub
```

每个 ScopeRuntime 构造时必须有非空：

```csharp
LayerRuntime OwningRuntime
```

---

# 23. LayerRuntime 构建顺序

最终唯一构建顺序：

```text
1. LayersBuilder 收集 Layer 实例。
2. 创建 LayerRuntime Shell。
3. LayerRuntime 为 Layer 分配 RouteIndex 和层级关系。
4. 收集全部 AssemblyModule。
5. ModuleRuntimeBuilder 创建 Catalog。
6. ScopeCompositionBuilder 修正并冻结：
   - ScopeId
   - ServiceSlot
   - ContextSlot
   - LayerMembership
   - MessageRouteId
7. 创建 ScopeRuntime Shell。
8. Scope 内创建 ServiceProvider。
9. 创建 Service。
10. 创建 Context。
11. 写入统一 ScopeObjectBinding。
12. 执行 Mount。
13. 注册本地 Event Handler。
14. 注册 Scope Event/Call Handler。
15. 向 Layer 写入 LayerServiceHandle。
16. 创建 ScopeRouteTable。
17. 启动 Main/Inline/Worker Scope。
18. LayerRuntime Build 完成。
```

---

# 24. LayerRuntime 字段调整表

## 保留

```text
Id
Version
Layers
Layer 类型索引
Layer 层级关系
ScopeHost
Actors
ExceptionHub
ExceptionCallbacks
ModuleCatalog
FullSnap
OwnerThreadId
全局工具
Disposed/Started 状态
```

## 删除

```text
EventCenter
Scheduler
Timer
EcsWorld
EcsQueryRegistry
EcsScheduler
业务 SynchronizationContext
WorldServiceRoot
业务 ServiceProvider
```

## 新增

```text
ScopeCompositionPlan
ActorCommandInbox
RuntimeToolRegistry
LayerServiceHandle 表
```

---

# 25. ScopeRuntime 字段调整表

## 保留

```text
Descriptor
ScopeId
Threading/Clock
PostInbox
CallInbox
ContinuationQueue
EventCenter
PostScheduler
Timer
EcsWorld
EcsQueryRegistry
Services
Runner
Routes
OwningRuntime
```

## 删除

```text
ActorWorld 所有权
sharedActorWorld 参数
ActorWorld Pump
ActorWorld Dispose
ScopeServiceOwnerRegistry Bind
```

## 新增

```text
ScopeServiceProvider
ILayerContext[]
ContextSlot
SubscriptionRegistry
DelayManager
ScopeActorGateway
LayerMembership 表
ScopeObjectBinding 初始化器
```

---

# 26. 文件级修改清单

## `LayerBase/Application/LayerRuntime.cs`

重写：

```text
构造函数
Build
PumpCore
Dispose
资源字段
```

删除 Runtime 本地 Event/ECS/Timer 推进。

保留 Layer、Actor、Exception、ScopeHost。

---

## `LayerBase/Layer/Layer.cs`

删除：

```text
ServiceProvider
RegisterService
GetService
Subscribe 系列
Delay 系列
Service 生命周期
```

保留层级和 Handle。

---

## `LayerBase/Scope/ScopeRuntime.cs`

加入：

```text
ScopeServiceProvider
Contexts
Subscriptions
DelayManager
ActorGateway
完整 Binding
```

移除 ActorWorld 所有权。

---

## `LayerBase/Scope/ScopeRuntimeHost.cs`

删除 `CreateFromCatalog` 当前实现。

改为接收 `ScopeCompositionPlan`。

禁止修改 Catalog ScopeId。

---

## `LayerBase/Modules/ModuleRuntimeBuilder.cs`

修复：

```text
ScopeId 从 1 开始
ServiceSlot 唯一权威
ContextSlot 分配
LayerMembership 分配
Route 与实际数组严格一致
```

---

## `LayerBase/DI/ServiceLayerBinder.cs`

删除文件。

替换为：

```text
ScopeObjectBinding.cs
ScopeObjectBinder.cs
ScopeAccessGuard.cs
```

---

## `LayerBase/DI/ServiceProvider.cs`

改造成：

```text
ScopeServiceProvider.cs
```

删除 WorldServiceRoot 和 OwnerLayer 生命周期语义。

---

## `LayerBase/Scope/ServiceScopeBinding.cs`

删除：

```text
ScopeServiceOwnerRegistry
IGeneratedScopeServiceBinding
```

由统一 Binding 取代。

---

## `LayerBase/DI/ServiceExtensions.cs`

删除所有 Runtime/Layer fallback。

统一走 `binding.Scope`。

---

## `LayerBase/ECS/Extensions/*`

Service 和 Context 全部走统一 Scope Binding。

删除 Layer/Runtime ECS Extension。

---

## `LayerBase/Actor/Extensions/*`

改为返回 `ScopeActorGateway`。

不再直接返回共享 ActorWorld。

---

## `ManagerAutoSubscribeGenerator`

不再生成：

```text
ILayerBindingAccessor
IGeneratedScopeServiceBinding
AutoBind(Layer)
```

改为：

```text
IScopeObjectBindingAccessor
IAutoScopeSubscribe
```

---

## `ScopeRuntimeHostGenerator`

删除：

```text
GlobalDispatcherRegistry
静态 ScopeHostFactory 注册
Generator 本地 ServiceSlot 分配
```

---

# 27. 直接删除的旧模型

用户已明确不需要兼容，因此直接删除：

```text
LayerRuntime.EventCenter
LayerRuntime.Scheduler
LayerRuntime.Timer
LayerRuntime.EcsWorld
LayerRuntime.GetService

Layer.Send/Post/Subscribe/Delay/GetService
LayerCall

ServiceLayerBinding
ServiceLayerBinder
ScopeServiceOwnerRegistry

LayerContext Runtime fallback
Service Runtime fallback

ScopeRuntimePlanner 的“先 Layer 创建，再按 Scope 分桶”
GlobalDispatcherRegistry
ScopeHostFactory 静态单槽
PrimaryRuntime fallback
```

---

# 28. 必须先写的测试

## 当前结构回归测试

```text
Module ScopeId 路由不会偏移到 MainScope。
Route.ServiceSlot 与 ScopeRuntime.ServiceSlots 一致。
ContextContribution 被真实实例化。
OwnerLayerTypes 被转为 LayerServiceHandle。
```

## 资源隔离

```text
Service 与 Context 的 EventCenter 相同。
Service 与 Context 的 EcsWorld 相同。
不同 Scope 的 EventCenter 不同。
不同 Scope 的 EcsWorld 不同。
不同 Scope 的 Timer 不同。
```

## LayerRuntime

```text
LayerRuntime 不存在 EventCenter。
LayerRuntime 不存在 EcsWorld。
LayerRuntime 只 Pump ActorWorld 一次。
LayerRuntime 只 Dispose ActorWorld 一次。
Layer 层级和 RouteIndex 保持有效。
```

## DI

```text
同 Scope Mount 成功。
跨 Scope Mount Build 失败。
同 Scope GetService 成功。
跨 Scope GetService 失败。
```

## Event

```text
相同 Event 类型在两个 Scope 中互不传播。
同 Scope 内 Handler 按 LayerIndex 排序。
自动订阅和手动订阅进入同一个 Scope EventCenter。
```

## 多 Runtime

```text
两个 LayerRuntime 的 Scope、Route、Actor 和 Catalog 完全隔离。
不存在静态 Dispatcher 覆盖。
```

---

# 29. 实施阶段

## Phase 1：修复当前 Module 结构错误

先处理：

```text
ScopeId 偏移
ServiceSlot 错位
Context 未创建
OwnerLayerTypes 未落地
```

否则后面所有 Binding 都建立在错误数组上。

## Phase 2：新增 ScopeCompositionBuilder

让它成为唯一：

```text
Catalog -> ScopeRuntimeHost
```

入口。

## Phase 3：统一 Binding

实现：

```text
ScopeObjectBinding
ScopeObjectBinder
Service/Context 同一 Binding
```

## Phase 4：Scope 级 DI 和 Context

将 Service/Context 创建从 Layer 移入 ScopeRuntime。

## Phase 5：迁移 Event/ECS/Timer/Delay

全部使用 `binding.Scope`。

## Phase 6：瘦身 Layer 与 LayerRuntime

删除重复业务资源，但保留：

```text
Layer 层级
ActorWorld
异常
ScopeHost
全局工具
```

## Phase 7：ActorWorld 单一所有权

Scope 改为 Gateway。

## Phase 8：Generator 重构

删除 Legacy/Global/本地 Slot 推导。

## Phase 9：删除旧文件与旧 API

不留兼容分支。

## Phase 10：性能回归

验证：

```text
Scope Empty Pump 仍为 0 B。
Binding 访问为 0 B。
Context 与 Service API 无额外分配。
LayerRuntime 删除重复资源后 Pump 不产生 GC。
```

---

# 30. 最终公理

```text
1. LayerRuntime 是应用聚合根，不是业务资源域。

2. ScopeRuntime 是唯一业务资源与对象所有权域。

3. LayerRuntime 保留 Layer 层级、ActorWorld、ScopeHost、
   异常汇总和全局工具。

4. LayerRuntime 不拥有 Event、Timer、ECS、业务 DI。

5. Layer 保留层级实例，但不持有 Service 生命周期和业务资源。

6. Service 物理属于一个 Scope，逻辑属于一个或多个 Layer。

7. Context 无条件继承 OwnerService 的 Scope 和 LayerMembership。

8. 本地 Event/ECS/Timer/Delay/DI 全部使用 OwnerScope。

9. 跨 Scope 只能使用 ScopeEvent 和 ScopeCall。

10. ActorWorld 只由 LayerRuntime Pump 和 Dispose。

11. ScopeId、ServiceSlot、ContextSlot 只有 CompositionBuilder 一个权威。

12. Module、Legacy 和 Generator 不得自行重新分配运行时 Slot。

13. 每个 LayerRuntime 拥有独立 Route 与 Dispatcher，不使用全局静态槽。

14. 不保留任何 Runtime/Layer 业务资源 fallback。
```

# 31. 一句话定义

```text
LayerRuntime 负责组织整个 LayerBase 应用；

ScopeRuntime 负责承载其中所有具体业务资源、Service、Context 与执行逻辑。
```
