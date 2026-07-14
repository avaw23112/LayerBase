# MainScope 默认执行域与 Scope-first 资源所有权设计

## 目标

每个成功构建的 `LayerRuntime` 都必须拥有一个 `ScopeRuntimeHost`，并且 `ScopeId == 0` 永远对应 `MainScope`。未声明 `[Scope<TScope>]` 的业务 Service 默认归属 `MainScope`；自定义 Scope 只用于把对象从默认执行域迁出。

`LayerRuntime` 不再拥有独立的业务执行资源。`EventCenter`、`PostScheduler`、`TimeScheduler`、`DelayManager`、ECS World、ECS Scheduler 和业务同步上下文全部属于 `ScopeRuntime`，其中默认业务入口落到 `MainScope`。

## 最终职责边界

```text
LayerRuntime
├─ Layer 层级与 RouteIndex
├─ ScopeRuntimeHost
├─ ActorWorld
├─ WorkerRuntime
├─ LayerExceptionHub
├─ FullSnap / Tooling / Diagnostics
└─ Runtime 生命周期与主线程 Pump 根节点

ScopeRuntime
├─ EventCenter
├─ PostScheduler / PostIngress
├─ TimeScheduler
├─ DelayManager
├─ EcsWorld / EcsScheduler / EcsQueryRegistry
├─ SynchronizationContext / Continuation
├─ ScopeServiceProvider
├─ IService / ILayerContext 生命周期
└─ Scope 本地 Post / Call / Resource
```

## 核心不变量

1. `LayerRuntime.ScopeHost` 在 Build 成功后不为 null。
2. `ScopeRuntimeHost.Scopes` 至少包含一个 Scope。
3. `MainScope.ScopeId == 0`，自定义 Scope ID 从 1 开始。
4. 没有 `[Scope<TScope>]` 的 Service 进入 MainScope。
5. 显式 `[Scope<TScope>]` 的 Service 进入对应自定义 Scope。
6. Module 模式不要求显式贡献 MainScope Definition。
7. LayerRuntime 不创建、不 Pump、不释放第二套事件、时间、ECS 或同步上下文资源。
8. 所有业务 Service/Context 生命周期只由 OwnerScope 推进一次。
9. Layer 的每帧与固定帧回调在 MainScope 的 `ScopeExecution` 和同步上下文中运行。
10. Runtime 保留的旧入口若暂时存在，只能是 MainScope 的无状态转发属性或方法，不能保存资源字段。

## 构建路径

### 非 Module 模式

`LayerRuntime.InitializeScopeHost()` 收集所有已解析 Service。`ScopeRuntimePlanner.Build()` 负责分组：

- 未声明 Scope：MainScope。
- 声明 `[Scope<TScope>]`：对应自定义 Scope。
- 没有 Service：仍返回空 MainScope Plan。

`LayersBuilder` 只保存 Post、Timer、Delay 和 ECS 配置，创建 ScopeHost 时把配置封装成 `ScopeRuntimeOptions`。不再提前构建 Runtime 级业务资源。

### Module 模式

`ModuleRuntimeBuilder` 内建 `typeof(MainScope) -> 0` 映射。用户定义 Scope 从 1 开始稳定分配。Service、Context 和消息契约可以直接目标 MainScope，而不要求 Module 提供 MainScope 的 `ScopeDefinitionContribution`。

`ScopeCompositionBuilder` 必须把归属 MainScope 的 Service、Context 与 ResourcePlan 放入 `Scopes[0]`，而不是创建一个永远为空的占位 Scope。

## Pump 数据流

```text
Engine
  -> LayerRuntime.Pump(deltaTime)
      -> Worker 普通事件转交 MainScope
      -> ScopeRuntimeHost.Pump(deltaTime)
          -> MainScope.Pump
              -> Continuation / Timer / Delay
              -> Scope Post / Call
              -> MainScope Service / Context Update
              -> LayerChain Update / FixedUpdate
              -> MainScope ECS
          -> 其他 Inline Scope
      -> Drain Actor Commands
      -> ActorWorld.Pump 一次
      -> Drain ExceptionHub
```

`LayerRuntime.Pump` 中不得再出现 Runtime Scheduler、Runtime Timer、Runtime Delay、Runtime ECS 或 Runtime SynchronizationContext 的推进。

## 生命周期与释放

ScopeRuntime 负责：

- `IInitializable`
- `IPostBuild`
- `IRuntimeStart`
- `IUpdate`
- `IFixedUpdate`
- `IRuntimeStop`
- `IDisposable`

Layer 的旧 ServiceProvider 在迁移期间仍可用于构建实例，但发现对象已经绑定 `ScopeObjectBinding` 后，不得再执行生命周期或释放该对象，避免双 Initialize、双 Update 和双 Dispose。

## 事件与异步入口

默认 Layer Send、Post、Schedule、Delay 和订阅都解析 MainScope 资源。Service/Context 通过自身 `ScopeObjectBinding` 解析 OwnerScope。

跨线程普通 Post 入口属于目标 Scope；默认入口由 MainScope 持有 `PostIngressQueue`。业务 continuation 由 Scope 的同步上下文恢复，LayerRuntime 不安装业务同步上下文。

## 测试要求

- 空 Runtime 仍创建 MainScope。
- 未标记 Service 绑定 MainScope。
- 自定义 Scope 与 MainScope 正确分区。
- Module 无自定义 Scope Definition 时仍可构建 MainScope。
- MainScope ID 固定为 0。
- Runtime 的资源入口与 MainScope 实例引用相同，且不存在对应 Runtime 存储字段。
- MainScope Service 初始化与 Update 各执行一次。
- Layer Pump 在 `ScopeExecution.Current.ScopeId == 0` 下执行。
- Runtime Dispose 不会重复释放 Scope-owned Service/ECS/Event 资源。
- 全量测试在 .NET 8 与 .NET 9 通过。
