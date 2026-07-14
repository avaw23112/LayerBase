# MainScope 默认执行域设计

## 目标

每个成功构建的 `LayerRuntime` 都必须拥有一个 `ScopeRuntimeHost`，并且该 Host 的 `ScopeId == 0` 永远是 `MainScope`。未声明 `[Scope<TScope>]` 的业务 Service 默认归属 `MainScope`；自定义 Scope 只用于把对象从默认执行域迁出。

## 本次改造边界

本次改造建立 MainScope 的必建语义和默认服务归属，但不立即删除 `LayerRuntime` 现有的 `EventCenter`、`PostScheduler`、`Timer`、ECS World 等兼容资源。旧资源的移除需要后续独立迁移，因为当前 Layer、Actor、旧事件 API 和构建顺序仍直接依赖这些资源。

## 核心不变量

1. `LayerRuntime.ScopeHost` 在 Build 成功后不为 null。
2. `ScopeRuntimeHost.Scopes` 至少包含一个 Scope。
3. `Scopes[0].ScopeId == 0` 且 `Scopes[0].Descriptor.Name == "MainScope"`。
4. 没有 `[Scope<TScope>]` 的 Service 被放入 MainScope。
5. 显式 `[Scope<TScope>]` 的 Service 继续进入对应自定义 Scope。
6. Module 模式即使没有自定义 Scope Definition，也必须构建 MainScope。
7. MainScope 中的 Service 生命周期和 Update 只由 ScopeRuntime 推进一次，Layer 不再重复推进已绑定到任意 Scope 的 Service。

## 非 Module 构建路径

`LayerRuntime.InitializeScopeHost()` 收集所有已解析 Service，而不是只收集带 Scope 标记的 Service。`ScopeRuntimePlanner.Build()` 已经具备默认分组逻辑：无 Scope 标记进入 MainScope，有 Scope 标记进入自定义 Scope，并且即使 Service 列表为空也返回 MainScope Plan。

生成式 Host Factory 与反射 Planner 接收相同的完整 Service 列表，因此两条路径保持一致。

## Module 构建路径

`MainScope` 是框架内建 Scope，不要求用户 Module 显式贡献 `ScopeDefinitionContribution`。

`ModuleRuntimeBuilder` 的 Scope ID 分配规则调整为：

- `MainScope` 固定映射到 0。
- 用户定义 Scope 从 1 开始按稳定顺序分配。
- Service 或消息契约目标为 MainScope 时，验证直接通过。

`LayerRuntime.TryBuildFromInstalledModules()` 不再以 `catalog.ScopeDefinitions.Count == 0` 作为放弃 ScopeHost 的条件。只要安装了 Module，就构建 Composition Plan；Plan 本身无条件包含 MainScope。

## 默认事件入口

为了让 MainScope 成为默认执行域，Layer 的同步 Send、Post、事件订阅和 Delay Publisher 默认选择 MainScope 的资源。已绑定到自定义 Scope 的 Service/Context 继续通过 `ScopeObjectBinding` 选择自身 Scope。

在兼容期内，`LayerRuntime` 的旧事件资源仍保留给内部旧路径，但普通 Layer 业务入口不再默认使用它。

## 生命周期与所有权

`ScopeRuntime` 已负责 Service 的 Initialize、Update、订阅绑定和停止释放。Layer 的生命周期收集逻辑改为跳过所有已经具有 `ScopeObjectBinding` 的 Service，而不是仅跳过带 `[Scope<TScope>]` 的类型，避免 MainScope Service 被初始化和更新两次。

本次不重写 ServiceProvider/WorldServiceRoot 的物理释放所有权；现有显式 Scope Service 已经使用同一兼容结构。完全消除双轨所有权属于后续 Scope-first 重构。

## 测试要求

新增测试覆盖：

- 没有 Service 时仍创建 MainScope。
- 只有未标记 Service 时，该 Service 位于 MainScope。
- 自定义 Scope 与 MainScope 同时存在。
- Module 没有自定义 Scope Definition 时仍分配 MainScope ID 0 并成功构建计划。
- MainScope Service 的 Initialize/Update 不被 Layer 重复调用。
- Layer 默认事件订阅和发送使用 MainScope EventCenter。
