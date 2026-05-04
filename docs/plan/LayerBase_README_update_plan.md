# LayerBase README 改进方案

---

## 1. README 当前主要问题

当前 README 的主要问题不是内容不够，而是：

```text
1. 部分示例代码和当前 public API 不一致；
2. 部分生命周期描述仍停留在旧版本；
3. 新增的 PostFromAnyThread、FixedUpdate、Runtime Policy Dump、默认事件注册语义没有完整说明；
4. 线程模型没有明确写出，容易让用户误以为所有 API 都是线程安全的；
5. Benchmark 和性能宣传篇幅过重，且部分表述需要降级为当前代码可证明的描述；
6. README 太长，应该把细节迁移到 docs/。
```

目标不是重写 README，而是按当前代码事实修正和补充。

---

## 2. 必须修正的事实冲突

### 2.1 NuGet 版本号

当前 README 安装示例仍可能写旧版本，例如：

```bash
dotnet add package LayerBase --version 1.4.4
```

当前 `LayerBase.csproj` 中版本为：

```xml
<Version>1.4.7.1</Version>
```

建议修改为：

```bash
dotnet add package LayerBase --version 1.4.7.1
```

或者改成不固定版本：

```bash
dotnet add package LayerBase
```

并补充说明：

```text
如果需要固定版本，请以 NuGet 页面或 LayerBase.csproj 中的 Version 为准。
```

---

### 2.2 `.Prewarm()` 示例

`.Prewarm()` 当前以扩展方法存在，因此不要删除。

但 README 需要改成更清楚的写法：

```csharp
using LayerBase;
using LayerBase.Layers;
// TODO: 补充 Prewarm 扩展方法所在 namespace。
// 例如：using LayerBase.Extensions;

public class GameRoot
{
    private LayerRuntime _runtime;

    public void Awake()
    {
        _runtime = LayerHub.CreateLayers()
            .Push(new InteractionLayer())
            .Push(new CoreLogicLayer())
            .SetDebug()
            .Build()
            .Prewarm();
    }

    public void Update(float deltaTime)
    {
        _runtime.Pump(deltaTime);
    }
}
```

Codex 需要根据当前扩展方法签名确认最终写法。

README 中需要说明：

```text
Prewarm 是可选预热步骤。
它用于提前触发部分运行时缓存、事件 ID 或策略相关路径，避免首次运行时抖动。
不调用 Prewarm 也不影响基础功能正确性。
```

---

### 2.3 namespace 示例

如果 README 中存在：

```csharp
using LayerBase.LayerHub;
```

应改为：

```csharp
using LayerBase;
using LayerBase.Layers;
```

如果某些扩展 API 需要额外 namespace，例如 `.Prewarm()`、`this.Send()`、`this.Post()` 等，应按当前代码补齐对应 using。

---

### 2.4 CallAsync 性能描述

README 中如有类似：

```text
零字典查找、零锁竞争、零版本核对
```

需要改掉“零版本核对”。

当前更稳妥的描述是：

```text
CallAsync 使用 runtimeId + 泛型静态缓存保存目标 invoker，并通过轻量版本号判断缓存是否失效。缓存命中时避免字典查找和锁竞争。
```

---

### 2.5 示例代码中的明显错误

需要修正：

```csharp
this.MarkDirty<DamageEvent>()；
this.PostLatest(new DamageEvent())；
```

改为：

```csharp
this.MarkDirty<DamageEvent>();
this.PostLatest(new DamageEvent());
```

修正重复字段名：

```csharp
[Mount] private CombatService _combatService;
[Mount] private OtherService _combatService;
```

改为：

```csharp
[Mount] private CombatService _combatService;
[Mount] private OtherService _otherService;
```

修正拼写错误：

```csharp
this.Send(new PlayerMoveEvent (){e.delat = new Vector2(0,1)});
```

改为：

```csharp
this.Send(new PlayerMoveEvent
{
    Delta = new Vector2(0, 1)
});
```

如果 README 不希望依赖 `Vector2`，则改成纯 C# 字段：

```csharp
this.Send(new PlayerMoveEvent
{
    X = 0,
    Y = 1
});
```

---

## 3. 必须新增或补全的章节

### 3.1 线程模型

新增章节：

```markdown
## 线程模型

LayerBase 当前采用单线程 Runtime 模型。

除带 `AnyThread` 后缀的 API 外，其余 Runtime API 默认只能在 Runtime 所在线程调用。

Owner-thread only：

- `Send`
- `Post`
- `TryPost`
- `PostLatest`
- `PostCoalesced`
- `MarkDirty`
- `CallAsync`
- `Pump`
- `Build`
- `Dispose`
- `Reset`

允许跨线程：

- `PostFromAnyThread`
- `TryPostFromAnyThread`

`PostFromAnyThread` 不会立即派发事件。它会先进入跨线程入口队列，并在下一次 `Runtime.Pump` 中搬运到 `PostScheduler`，再参与正常的 `Normal / DirtySignal / Latest / Coalesced` 策略处理。

当前版本不在热路径 API 中做线程动态检查。错误线程调用普通 API 属于未定义行为。

`Dispose` / `Reset` 不建议和 `PostFromAnyThread` 并发执行。
```

---

### 3.2 PostFromAnyThread

新增章节：

````markdown
## PostFromAnyThread

`PostFromAnyThread` 是跨线程事件入口。

它适合后台线程提交：

- 计算完成通知
- 小型状态变更
- 后台任务结果
- 需要回到 Runtime.Pump 中统一处理的事件

示例：

```csharp
LayerHub.PostFromAnyThread(new DamageEvent
{
    TargetId = 1,
    Amount = 10
});
```

也可以显式指定策略：

```csharp
LayerHub.PostFromAnyThread(
    new DamageEvent
    {
        TargetId = 1,
        Amount = 10
    },
    new EventPostPolicy(
        PostDeliveryMode.Latest,
        BackpressurePolicy.RejectNew,
        maxPending: 0));
```

`PostFromAnyThread` 不立即派发。事件会先进入入口队列，在下一次 `Runtime.Pump` 中被搬运到 `PostScheduler`。
````

补充说明：

```text
PostFromAnyThread 是跨线程慢路径。
大数据不建议直接作为事件 payload 传递。
推荐传小型 struct、ID、handle 或不可变数据引用。
```

---

### 3.3 PostSchedulerOptions

新增配置章节，Codex 需要按当前构造函数签名校准：

```markdown
## PostScheduler 配置

`PostSchedulerOptions` 控制 Post 队列、帧预算、跨线程入口预算和默认背压策略。

关键配置：

- `ReadyCapacity`
- `NextCapacity`
- `MaxEventsPerPump`
- `MaxMillisecondsPerPump`
- `MaxWavesPerPump`
- `TimeCheckInterval`
- `DefaultBackpressure`
- `MaxCompletionsPerPump`
- `MaxIngressPostsPerPump`

`MaxIngressPostsPerPump` 用于限制每次 `Runtime.Pump` 最多搬运多少个 `PostFromAnyThread` 事件，避免后台线程持续生产事件导致主线程单帧被拖死。
```

示例由 Codex 按当前 `PostSchedulerOptions` 构造函数生成。

---

### 3.4 默认事件注册语义

新增章节：

```markdown
## 默认事件注册语义

LayerBase 会为事件类型分配运行期 `EventTypeId<T>.Id`。

在 Build 前已经分配过 `EventTypeId<T>.Id` 的事件，会在 `PostScheduler.BuildPlans` 中获得默认 `Normal` Post 策略。

这意味着：没有显式 metadata 的事件也可以作为普通事件被 `Post` 接受。

如果事件需要特殊投递行为，例如：

- `DirtySignal`
- `Latest`
- `Coalesced`
- `MaxPending`
- `MergeFailurePolicy`

则应通过 EventMetaData 或显式 EventPostPolicy 配置。
```

这是当前设计目标，不能删。

---

### 3.5 Runtime Policy Dump

新增章节：

````markdown
## Runtime Policy Dump

Debug 或诊断场景下可以导出当前 Runtime 的事件策略表：

```csharp
var markdown = runtime.GetPolicyMarkdown();
```

输出内容包括：

- RuntimeId
- StableId
- StableKey
- Version
- Event Type
- Post Mode
- Backpressure
- MaxPending
- MergeFailure
- Timer Policy
- Buffer Policy

这适合用于排查事件策略是否按预期注册，尤其是 `Latest` / `Coalesced` / `DirtySignal` 等非普通投递模式。
````

---

### 3.6 Event Identity / StableEventKey

新增章节：

```markdown
## Event Identity / StableEventKey

LayerBase 在运行时使用 `EventTypeId<T>.Id` 作为热路径数组索引。

对于诊断、PolicyDump、跨版本记录等场景，事件还会拥有稳定身份信息：

- StableId
- StableKey
- Version
- EventType

运行期热路径使用 `EventTypeId<T>.Id`；诊断与导出使用 Stable Identity。
```

Codex 根据当前实际类型名修正，例如 `EventIdentity`、`EventIdentityRegistry` 等。

---

### 3.7 FixedUpdate

新增章节，示例由 Codex 按当前 `FixedUpdateOptions` 定义校准：

```markdown
## FixedUpdate

LayerBase 支持独立的 FixedUpdate 管线。

通过 `SetFixedUpdateOptions` 开启固定步长更新。`Runtime.Pump(deltaTime)` 会累积 deltaTime，并在达到固定步长时调用 Layer 内部的 FixedUpdate 逻辑。

`MaxStepsPerPump` 用于防止低帧率下单帧补步过多。
```

示例不要硬写错误构造函数。Codex 需要读取 `FixedUpdateOptions` 的当前定义后再写。

---

### 3.8 Build 生命周期

新增或替换旧生命周期章节：

```markdown
## Runtime 构建生命周期

LayerBase 的 Build 阶段大致分为：

1. `Prebuild`
   - 分配 Layer RouteIndex
   - 绑定 Runtime / EventCenter
   - 执行 `PrepareBuild`
   - 执行源生成器生成的 AutoBinding

2. Runtime 基础设施初始化
   - 初始化 PostScheduler
   - 初始化 Timer
   - 初始化 DelayManager
   - 构建 ServiceProvider

3. Layer Build
   - SharedFieldBinder 绑定共享字段
   - LifecycleBuild / AutoBind / Initialize
   - PostBuild
   - RuntimeStart
   - EventGraphValidator 校验事件图

4. Runtime Dispose
   - RuntimeStop
   - DisposeLayers
   - 清理 Scheduler / Timer / Delay / Context / runtime cache
```

---

## 4. 需要降级或核对的宣传表述

### 4.1 SOA / DOD 表述

README 可以保留性能方向，但应避免把所有路径都说成“纯 SOA 原生数组”。

建议改成：

```text
LayerBase 在事件分发热路径中尽量使用数组、位图和泛型静态缓存，减少 Dictionary 与反射访问。部分策略型功能如 Coalesced、Delay、Topology Dump 属于慢路径或构建期路径。
```

---

### 4.2 自愈熔断机制

如果当前代码没有完整的“下一帧两段式零分配重建并剔除失效节点”，不要继续写强承诺。

保守写法：

```text
[Subscribe] 路径支持异常隔离与错误上报；[SubscribeNotify] 是极致性能路径，不捕获异常，需要用户自行保证处理器安全。
```

---

### 4.3 SubscribeParallel

README 里如果继续保留 `SubscribeParallel`，Codex 需要核对当前代码是否完整支持。

如果支持不完整或仍处于实验阶段，应标注：

```text
实验性功能 / 需配合 JobScheduler / 后续版本完善
```

不要写成稳定主功能。

---

## 5. 建议 README 重构结构

README 应精简为项目入口，不要继续堆所有细节。

建议结构：

```text
1. 项目定位
2. 安装方式
3. 最小可运行示例
4. 核心概念：Layer / Service / Manager
5. 事件模型：Send / Post / Delay / Call
6. PostScheduler 与投递策略
7. 线程模型
8. 生命周期
9. Debug / Topology / PolicyDump
10. Benchmark 摘要
11. docs 链接
```

建议新增 docs：

```text
docs/THREADING.md
docs/POST_SCHEDULER.md
docs/LIFECYCLE.md
docs/EVENT_METADATA.md
docs/BENCHMARKS.md
docs/DI_AND_MOUNT.md
```

README 里只放摘要，详细内容跳 docs。

---

## 6. Benchmark 处理

README 中 Benchmark 表格太长，而且数据容易随版本、CPU、JIT、BenchmarkDotNet 配置变化。

建议：

```text
README 保留简短摘要。
完整 Benchmark 表格迁移到 docs/BENCHMARKS.md。
```

并补充：

```text
Benchmark 数据会随运行环境、.NET 版本、CPU、JIT 配置变化。请以仓库内 BenchmarkDotNet 输出和对应 commit 为准。
```

---

## 7. Codex 执行指令

```text
任务：按当前 faster 分支代码更新 README，不重写项目定位，只修正冲突、补充落后能力、清理错误示例。

必须完成：

1. 更新安装版本：
   - README 中 LayerBase NuGet 版本改为当前 LayerBase.csproj Version 1.4.7.1。
   - 或改成 dotnet add package LayerBase，并说明固定版本以 NuGet/csproj 为准。

2. 保留 .Prewarm() 示例，但必须修正：
   - 确认 .Prewarm() 扩展方法所在 namespace，并在示例 using 中补上。
   - 说明 Prewarm 是可选预热步骤。
   - 如果 .Prewarm() 返回值不是 LayerRuntime，按实际签名改写示例。

3. 修正 namespace：
   - 删除 using LayerBase.LayerHub。
   - 使用 using LayerBase; using LayerBase.Layers;。
   - 所有扩展方法需要的 namespace 按实际代码补齐。

4. 修正 CallAsync 性能描述：
   - 删除“零版本核对”。
   - 改成“泛型静态缓存 + 轻量版本号校验 + 缓存命中避免字典查找”。

5. 新增线程模型章节：
   - LayerBase 是单线程 Runtime 模型。
   - Send/Post/TryPost/PostLatest/PostCoalesced/MarkDirty/CallAsync/Pump/Build/Dispose/Reset 是 owner-thread-only。
   - PostFromAnyThread/TryPostFromAnyThread 是 AnyThread API。
   - 普通热路径不做线程动态检查。
   - Dispose/Reset 不建议与 PostFromAnyThread 并发执行。

6. 新增 PostFromAnyThread 章节：
   - 说明不立即派发。
   - 说明在 Runtime.Pump 中 Drain 到 PostScheduler。
   - 说明支持 EventPostPolicy，因此可参与 Normal/DirtySignal/Latest/Coalesced。
   - 说明 MaxIngressPostsPerPump 用于限制每帧入口搬运数量。

7. 新增 PostSchedulerOptions 章节：
   - 说明 ReadyCapacity、NextCapacity、MaxEventsPerPump、MaxMillisecondsPerPump、MaxWavesPerPump、TimeCheckInterval、MaxCompletionsPerPump、MaxIngressPostsPerPump、DefaultBackpressure。
   - 按当前 PostSchedulerOptions 构造函数签名写示例。

8. 新增默认事件注册语义：
   - Build 前已分配 EventTypeId 的事件，会获得默认 Normal Post 策略。
   - 无 metadata 事件也可以普通 Post。
   - 特殊策略需要 metadata 或 EventPostPolicy。

9. 新增 Runtime Policy Dump 章节：
   - 说明 runtime.GetPolicyMarkdown()。
   - 说明输出 StableId、StableKey、Version、Post Mode、Backpressure、MaxPending、MergeFailure、Timer、Buffer。

10. 新增 Event Identity / StableEventKey 章节：
    - 说明 EventTypeId<T>.Id 用于运行期热路径索引。
    - 说明 StableId / StableKey / Version 用于诊断和导出。

11. 新增或修正 FixedUpdate 章节：
    - 按当前 FixedUpdateOptions 定义写示例。
    - 说明 fixedDeltaTime 和 maxStepsPerPump 的意义。

12. 更新生命周期章节：
    - Prebuild：AssignEventBus / PrepareBuild / BuildAutoBinding。
    - Runtime 初始化：Scheduler / Timer / Delay / ServiceProvider。
    - Build：SharedFieldBinder / LifecycleBuild / PostBuild / RuntimeStart / EventGraphValidator。
    - Dispose：RuntimeStop / DisposeLayers / cache clear。

13. 修正 README 示例代码：
    - 中文分号替换为英文分号。
    - 修正 duplicate _combatService。
    - 修正 e.delat 等拼写错误。
    - 按当前 IUpdate 接口签名修正 Update 示例。
    - 删除不确定或过期 using。

14. Benchmark 处理：
    - README 只保留摘要。
    - 完整表格迁移或链接到 docs/BENCHMARKS.md。
    - 加注版本、环境、结果会变化的说明。

15. 对“自愈熔断机制”“SubscribeParallel”等高承诺描述进行代码核对：
    - 如果当前代码不支持完整机制，改成保守表述或移动到 roadmap。
    - [SubscribeNotify] 明确为不捕获异常的极致性能路径。

验收标准：
- README 示例代码没有明显编译错误。
- README 不展示不存在的 public API。
- README 保留并正确解释 .Prewarm() 扩展方法。
- README 能解释当前 PostFromAnyThread / FixedUpdate / PolicyDump / Stable Identity / 默认事件注册语义。
- README 明确线程模型，不暗示所有 API 都是线程安全的。
- README 中版本号与 LayerBase.csproj 一致。
```

---

## 8. 最优先改的部分

如果时间有限，先让 Codex 改这几处：

```text
1. 安装版本号。
2. Prewarm 示例 namespace 与说明。
3. 线程模型章节。
4. PostFromAnyThread 章节。
5. 生命周期章节。
6. 示例代码错误。
7. 默认事件注册语义。
```
