# LayerBase Improvement Plan

## 目标

将 LayerBase 从“个人可控的高性能框架”推进到“外部用户敢接入、敢测试、敢长期依赖的 C# 游戏逻辑通信框架”。

当前阶段不追求继续堆功能，优先完成：

1. 工程可信度；
2. 文档体系；
3. CI 与测试；
4. Unity / Godot 接入路径；
5. 架构审计能力产品化；
6. 公共 API 行为明确化。

暂不处理：

- 迁移日志；
- 多版本历史兼容文档；
- 大规模破坏性重构；
- 正式文档站部署。

---

## 阶段 1：CI 与基础工程可信度

### 1.1 添加 GitHub Actions CI

新增文件：

```text
.github/workflows/ci.yml
```

CI 需要执行：

1. restore；
2. build；
3. test；
4. pack；
5. 检查 Release 构建；
6. 上传测试结果或至少输出测试日志。

建议矩阵：

```text
dotnet: 8.0.x
dotnet: 9.0.x
```

验收标准：

- push 到默认分支触发 CI；
- pull request 触发 CI；
- CI 失败时能看出失败项目；
- `dotnet pack` 能成功生成包。

### 1.2 增加 netstandard2.1 构建验证

核心库支持 `net8.0;netstandard2.1`，所以 CI 必须证明 `netstandard2.1` 能构建。

要求：

- 不一定要在 `netstandard2.1` 上跑测试；
- 但必须至少能 build；
- 如果某些 API 只支持 net8.0，需要使用条件编译明确隔离。

### 1.3 测试项目 target 调整

当前测试项目只需要保留新版本 .NET 测试也可以，但建议至少增加：

```xml
<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

如果某些测试只能跑 net9.0，需要说明原因。

验收标准：

- `dotnet test` 在 net8.0 和 net9.0 都能通过；
- 不引入 Unity/Godot 依赖。

---

## 阶段 3：Wiki 骨架

### 3.1 新建 Wiki 草稿目录

暂时不直接写 GitHub Wiki，先在仓库内新建：

```text
docs/wiki/
```

目录结构：

```text
docs/wiki/
├─ Home.md
├─ Getting-Started.md
├─ Core-Concepts.md
├─ Layer-Design-Guide.md
├─ Event-System.md
├─ Call-System.md
├─ Shared-Field.md
├─ LBTask-And-Game-Loop.md
├─ Source-Generator-And-Diagnostics.md
├─ Topology-Audit.md
├─ Unity-Integration.md
├─ Godot-Integration.md
├─ Performance-And-Benchmark.md
├─ FAQ.md
└─ Design-Decisions.md
```

注意：不写 Migration Guide。

### 3.2 Home.md

内容必须包含：

- LayerBase 是什么；
- 适合什么项目；
- 不适合什么项目；
- 推荐阅读路径；
- 当前项目状态；
- 文档导航。

### 3.3 Core-Concepts.md

解释以下概念：

- Layer；
- Service；
- Event；
- Call；
- Shared Field；
- Send；
- Post；
- Pump；
- Local；
- Global；
- Source Generator；
- LBTask；
- Topology Audit。

每个概念都要包含：

1. 简短定义；
2. 适用场景；
3. 不适用场景；
4. 一个小例子。

### 3.4 Event / Call / Shared Field 选择指南

在 `Core-Concepts.md` 或独立页面中增加表格：

| 需求 | 推荐机制 | 原因 |
|---|---|---|
| 通知多个系统 | Event | 发送方不关心接收方 |
| 明确请求返回结果 | Call | 调用关系清楚 |
| 多组件读取同一状态 | Shared Field | 共享关系显式 |
| 当前 Layer 内部通信 | Local Event | 控制作用范围 |
| 下一帧处理 | Post / LBTask.NextFrame | 适合游戏循环 |

### 3.5 Source-Generator-And-Diagnostics.md

列出当前已有 Diagnostic。

至少包含：

- 错误码；
- 错误含义；
- 触发示例；
- 正确写法；
- 修复方式。

不要只写“会报错”，要告诉用户怎么改。

### 3.6 Topology-Audit.md

重点说明：

- 如何导出拓扑报告；
- 报告包含哪些表；
- Zombie Event 是什么；
- Unused Producer 是什么；
- Dead Call 是什么；
- Orphaned Provide 是什么；
- 如何用它清理废弃代码；
- 如何在 CI 中保存拓扑报告。

---

## 阶段 4：公共 API 行为文档

### 4.1 文档化 LayerHub 生命周期

给以下 API 补 XML 注释和 Wiki 说明：

- `LayerHub.CreateLayers()`
- `LayerHub.Pump(float deltaTime)`
- `LayerHub.Reset()`
- `LayerHub.Send<T>()`
- `LayerHub.Post<T>()`
- `LayerHub.CallAsync<TLayer, TRequest, TResponse>()`
- `LayerHub.For<TLayer>()`
- `LayerHub.GetTopologyMarkdown()`

每个 API 说明必须包含：

- 参数作用；
- 调用时机；
- 是否线程安全；
- 是否依赖 Build；
- 是否依赖 Pump；
- 常见错误。

### 4.2 明确 64 Layer 限制

在 README、Wiki、异常信息中统一说明：

```text
LayerBase 当前最多支持 64 个 Layer。
该限制来自位图路由设计，每个 Layer 使用一个 bit 表示状态。
```

要求：

- 说明为什么限制 64；
- 说明超过时怎么处理；
- 说明推荐 Layer 粒度；
- 不建议用户把每个小模块都拆成 Layer。

### 4.3 明确 SynchronizationContext 行为

文档必须说明：

- 什么情况下会安装 LayerBase 的 SynchronizationContext；
- 什么情况下不会安装；
- Pump 与 LBTask 的关系；
- Reset 后上下文如何恢复；
- Unity/Godot 中是否建议自动安装；
- 纯 C# 控制台中如何使用。

---

## 阶段 5：测试增强

### 5.1 增加 Reset 测试

测试内容：

- Build 后 Reset；
- Reset 后重新 Build；
- Reset 后事件不会残留；
- Reset 后服务不会残留；
- Reset 后 Call 缓存不会残留；
- Reset 后 Topology 为空或符合预期。

### 5.2 增加 64 Layer 边界测试

测试内容：

- 64 个 Layer 可以正常 Build；
- 第 65 个 Layer 抛出明确异常；
- 异常信息包含原因；
- Reset 后计数恢复。

### 5.3 增加 Source Generator 测试

测试内容：

- 正确 `[Call]` 能生成绑定代码；
- 非 partial owner 报错；
- abstract owner 报错；
- request 不是 struct 报错；
- response 不是 struct 报错；
- CancellationToken 参数位置错误时报错。

### 5.4 增加并发测试

测试内容：

- 多线程 Send；
- 多线程 Post；
- 多线程 Call；
- Send 与 Reset 交错；
- Post 与 Pump 交错；
- Subscribe 后高频 Send；
- Rebuild 后无异常。

注意：

- 并发测试要避免不稳定；
- 不追求测试绝对性能；
- 只验证不会崩溃、不会数据错乱、不会死锁。

### 5.5 增加 ArrayPool 相关测试

目标：

- 重复 Rebuild；
- 重复 Reset；
- 重复 Subscribe / Unsubscribe；
- 确认不会访问已归还数组；
- 确认不会在 Reset 后继续派发旧 handler。

---

## 阶段 6：Benchmark 可信化

### 6.1 新增 Benchmark 说明文档

新增：

```text
docs/benchmark.md
```

内容包含：

- 如何运行；
- 运行环境；
- BenchmarkDotNet 版本；
- .NET SDK 版本；
- Release 构建要求；
- 测试场景解释；
- 如何理解结果；
- 哪些场景 LayerBase 不一定最快。

### 6.2 README 中降低绝对性能宣传

将“极致最快”“超极速”等表达改为更可信的描述：

```text
LayerBase 的目标是在游戏高频通信中保持低分配、低抖动、可预测的执行路径。
```

### 6.3 保存原始 Benchmark 报告

建议目录：

```text
docs/benchmarks/results/
```

要求：

- 每次手动更新 benchmark 结果时保存原始 markdown；
- README 只摘录关键结论；
- 不在 README 中堆过长结果表。

---

## 阶段 9：Wiki 初稿质量要求

所有 Wiki 页面都必须遵守：

1. 每页只解决一个主题；
2. 每页都有推荐用法；
3. 每页都有不推荐用法；
4. 新名词首次出现必须解释；
5. 代码示例必须可复制；
6. 代码注释必须说明参数作用；
7. 不写空泛宣传；
8. 不写迁移历史；
9. 不写“未来会支持”式承诺，除非放在 Roadmap；
10. 不把 README 内容机械复制到 Wiki。

---

## 阶段 10：FAQ

新增：

```text
docs/wiki/FAQ.md
```

至少回答：

1. LayerBase 和普通 EventBus 有什么区别？
2. LayerBase 和 MessagePipe 有什么区别？
3. 我能只用事件系统，不用完整分层吗？
4. 为什么类要写 partial？
5. 为什么事件推荐 struct？
6. 为什么限制 64 个 Layer？
7. Unity 里必须每帧 Pump 吗？
8. Godot 里怎么 Pump？
9. Source Generator 没生效怎么办？
10. Reset 应该什么时候调用？
11. Send 和 Post 有什么区别？
12. Call 和 Event 应该怎么选？
13. Shared Field 会不会变成全局变量？
14. LBTask 和 Task 有什么区别？

---

## 阶段 11：完成标准

本计划完成时，至少满足：

- CI 可运行；
- README 第一屏清晰；
- Wiki 草稿目录完整；
- Getting Started 可跑通；
- Event / Call / Shared Field 选择指南完成；
- Source Generator 错误码页面完成；
- Topology Audit 页面完成；
- Unity / Godot 接入草案完成；
- Benchmark 复现说明完成；
- Reset、64 Layer、Source Generator 基础测试增加；
- 不引入新的破坏性 public API 变更；
- 不添加迁移日志。

---

## Codex 执行建议

请按阶段执行，不要一次性大改。

推荐 PR 拆分：

1. `docs: fix readme and project naming`
2. `ci: add build test pack workflow`
3. `docs: add wiki draft structure`
4. `docs: add getting started and core concepts`
5. `test: add reset and layer limit coverage`
6. `test: add source generator diagnostics coverage`
7. `docs: add unity and godot integration guides`
8. `docs: add benchmark reproduction guide`
9. `docs: add topology audit guide`
10. `docs: add layerhub instance design note`

每个 PR 要求：

- 范围小；
- 能单独 review；
- CI 通过；
- 不混入无关格式化；
- 不在同一 PR 同时改文档、核心逻辑和测试，除非必须。
