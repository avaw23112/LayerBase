# 28 架构测试、IL2CPP 门禁、Benchmark 与最终验收

> **最高原则：** 所有既有 `master` 测试必须原样通过；只在实际移植 `faster` 功能时选择性迁移对应测试。不得为了通过测试恢复已否决架构，也不得声称执行了仓库无法执行的 IL2CPP 构建。  
> **master 基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **faster 复用基线：** `8898a90bcb3e00a370e47f8b39f6eff32fa98980`  
> **依赖阶段：** `00_index.md` 至 `27_scope_diagnostics_observability_migration.md`。  
> **文档性质：** 最终测试与交付门禁。本文不设计新运行功能。

---

## 0. 验收原则

最终验收必须证明：

```text
原功能没有被 Scope迁移破坏。

Layer仍是 Scope上层业务管理结构。

每个 Scope本地资源独立。

OwnerThread规则真实成立。

跨 Scope只有 ScopeEvent / ScopeCall。

LocalCall只在 CurrentScope。

LayerTool按 Layer × Scope隔离。

ECS World按 Scope隔离。

ActorWorld只由 MainActorRuntime拥有并在 MainScope执行。

FullSnap通过 SafePoint协调。

Build / Activate / Prewarm / Freeze顺序唯一。

Running热路径没有被冷路径结构污染。

多 Runtime状态隔离。

停止、异常和 Dispose没有悬挂资源。
```

---

## 1. 代码基线纪律

实施前必须保存：

```text
master Commit：
    7dee16c46d72a68f502554f693aed0c314b22be3

faster Commit：
    8898a90bcb3e00a370e47f8b39f6eff32fa98980
```

每个移植记录：

```text
来源文件

来源 Commit

复用方式：
    原样沿用
    修改移植
    仅参考
    未复用

修改原因

对应测试
```

禁止只写：

```text
来自 faster
```

而不锁定文件和 Commit。

---

## 2. 仓库标准命令

仓库已有标准命令：

```bash
dotnet restore LayerBase.sln

dotnet build LayerBase.sln -c Debug

dotnet build LayerBase.sln -c Release

dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug

dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release
```

单独验证两个目标框架：

```bash
dotnet build LayerBase/LayerBase.csproj \
    -c Release \
    -f net8.0

dotnet build LayerBase/LayerBase.csproj \
    -c Release \
    -f netstandard2.1
```

Benchmark：

```bash
dotnet run \
    -c Release \
    --project LayerBase.BenchMark/LayerBase.BenchMark.csproj
```

如果 `LayerBase.BenchMark.Compare` 项目存在并可构建：

```bash
dotnet run \
    -c Release \
    --project LayerBase.BenchMark.Compare/LayerBase.BenchMark.Compare.csproj
```

不得跳过 Release测试。

---

## 3. 原 master 测试门禁

原则：

```text
原测试文件不通过修改断言适配迁移。

原 API仍承诺兼容时，调用方式不修改。

原测试失败先判断实现回归，而不是先改测试。
```

必须覆盖现有：

```text
DI / Mount / Provide / From

EventCenter

Post / Timer / Delay

LBTask

Layer生命周期

Call

ActorWorld

ProjectedActor

Query / Bring

CommandBuffer

Blueprint

Snap

Prewarm

Runtime Safety

Payload Lifecycle
```

允许修改原测试的唯一情况：

```text
原测试直接验证已明确删除的 API或结构，
且对应删除已经在00—27号文档中批准。
```

这类修改必须在提交记录中注明被哪份文档否决。

---

## 4. 架构静态测试

使用 Roslyn、反射或源码扫描证明：

```text
ScopeRuntime不包含 ActorWorld。

CustomScope LocalAccess不暴露 ActorWorld。

ActorWorld只存在于 MainActorRuntime。

ScopeRuntime不包含第二个业务 Worker Scheduler。

PostScheduler不包含 CrossThreadIngress。

不存在 PostFromAnyThread。

不存在 ScopePostEndpoint。

不存在 SubscribeParallel。

不存在 UnifiedCallRoute。

不存在 TLayer Call API。

LocalCall Entry不含 TargetScopeId。

ScopeRef不暴露 ScopeRuntime / ServiceProvider / EcsWorld / Tool实例。

LayerToolAttribute包含 Layer和 Scope。

Tool实现 Attribute不要求重复 Layer/Scope。

Tool Cache按 Scope隔离。

Generated Query入口返回 void。

Input不进入 IQueryJob泛型或 Execute参数。

Runtime可变全局状态不在 static字段。
```

源码扫描必须排除：

```text
文档

测试中的禁止字符串样本

生成快照预期文本
```

只扫描生产代码和生成代码。

---

## 5. Layer / Scope 管理测试

```text
Push顺序产生稳定 LayerIndex。

LayerBuildPlan管理所有 Scope中的 Layer Contribution。

具体 Layer实例运行于 MainScope。

CustomScope Service仍拥有 OwnerLayer。

空 Layer Slice保留 LayerIndex。

DI只在同 Layer、同 Scope。

Mount只在同 Layer、同 Scope。

Provide / From只在同 Layer、同 Scope。

同 Scope跨 Layer使用 LocalCall。

同 Layer跨 Scope使用 ScopeEvent / ScopeCall。
```

---

## 6. ScopeEvent 测试

```text
同 Producer FIFO。

多 Producer消息总数正确。

Queue Full返回拒绝，不 inline执行。

入队失败不转移 Payload所有权。

Dispatch后 Payload释放一次。

旧 RuntimeGeneration Endpoint被拒绝。

Stopped Scope拒绝 Business消息。

Internal / Control保留容量可用。

不存在第三条跨 Scope业务 Queue。
```

压力参数至少包含：

```text
2 Producer

4 Producer

8 Producer
```

消息数量使用现有压力测试可在 CI稳定完成的规模，不硬编码脱离机器能力的超大值。

---

## 7. ScopeCall 测试

```text
Request和 Response使用标准 CallInbox。

Accepted Call只有一个终态。

Timeout / Cancellation释放 Promise。

Stale Token不能完成新 Promise。

Scope Stop终结 Pending Call。

Control Call在 Stopping仍可进入。

LocalCall不写 ScopeCallInbox。

跨 Scope必须显式 Scope<T>().Call。

不同 Scope允许相同 Request / Response Handler。

同 Scope重复 Handler Build失败。
```

---

## 8. OwnerThread 测试

记录：

```text
Scope Activate

Service Constructor

Context Constructor

Mount

Provide / From

Event Handler

LocalCall Handler

ScopeCall Handler

Initialize

PostBuild

RuntimeStart

Update

ECS Query

Tool Factory

Snap Write / Read

Stop

Dispose
```

WorkerScope全部等于 Worker OwnerThread。

MainScope / InlineScope等于 Main OwnerThread，但 InlineScope仍具有正确 ScopeExecution上下文。

错误线程调用：

```text
Send

Post

Timer

ECSWorld

Tool

ClipSnap绑定对象

MainActorRuntime
```

必须失败或被 API隔离。

---

## 9. 生命周期与故障注入

注入异常：

```text
Service Factory

Context Factory

Mount

Provide Getter

From Setter

Event Subscribe

Initialize

PostBuild

Prewarm

RuntimeStart

Event Handler

LocalCall Handler

ScopeCall Handler

Timer / Delay

ECS Query

CommandBuffer Playback

Tool Factory

Snap Write / Read

Actor Apply

Stop

Dispose
```

验证：

```text
Activate失败按逆序回滚。

Fault不静默终止 Worker。

Accepted ScopeCall获得 Fault Response。

Faulted Scope仍可 Stop / Dispose。

MainScope最后停止。

CustomScope先释放 Projection Actor。

MainActorRuntime晚于 CustomScope停止。

所有 Worker可 Join。

Payload / Promise / Tool / CommandBuffer没有泄漏。
```

---

## 10. ECS 与 Query 测试

### 原设计

```text
Generated Query入口仍为 void。

普通 Query行为不变。

Bring链仍为 Batch().Post()。

ProjectResult仍只属于单 Entity。

runtime.EcsWorld仍是 MainScope兼容门面。
```

### Input

```text
Input优先于 Component识别。

Input按值或 in进入入口。

Input由 Job字段捕获。

Input不进入 Execute参数。

ref/out/byref-like Input被拒绝。

无 Input生成代码保持 master兼容。
```

### Scope ECS

```text
每 Scope有独立 World。

WorkerScope Query只在 Worker OwnerThread。

CustomScope不回退 MainScope World。

CommandBuffer在 Query完成后 SafePoint Playback。

Blueprint API和 Cache保持原行为。

Blueprint Cache不保存 World / Scope。

QueryBatch开关关闭时结果等于 master。

开启分批时结果等于未分批。

第一版 Query不跨 Tick。
```

---

## 11. Actor 与 Projection 测试

```text
LayerRuntime拥有 MainActorRuntime。

ScopeRuntime不拥有 ActorWorld。

ActorWorld只在 MainScope OwnerThread创建、Pump、Dispose。

CustomScope Actor Mail走标准 ScopeEvent。

CustomScope Actor Call走标准 ScopeCall。

Projection Command走 MainScope标准 EventInbox。

Projection Result回 OriginScope EventInbox。

OriginScopeId不重复保存于 Payload。

Ensure / Enable / Disable / Release状态机保持。

Touch Active只刷新本地期限。

Release等待 Result后清 Binding。

发送失败回滚 Pending。

CustomScope Stop释放全部 ProjectedActor。

Actor / Projection Payload恰好一次释放。
```

---

## 12. LayerTool 测试

```text
LayerTool元特性必须声明 Layer。

LayerTool元特性必须声明 Scope。

具体 Tool Attribute不重复声明 Layer / Scope。

同 Tool Attribute所有实现继承相同归属。

同 Layer、同 Scope缓存实例相同。

不同 Scope实例不同。

不同 Layer不能直接查 Tool。

Tool Factory只解析同 Layer、同 Scope Service。

Tool在 OwnerScope Thread创建和 Dispose。

Scope Dispose不影响其他 Scope Tool。
```

---

## 13. Snap 与 SafePoint 测试

```text
master SnapTests原样通过。

Snap Key和 JSON格式不变。

Snap Node按 OwnerScope执行。

全部 Scope先 Frozen后写入。

当前 Query和 CommandBuffer在 Snapshot前完成。

WorkerScope使用 Async FullSnap。

同步 FullSnap不阻塞 Worker。

Restore失败不报告成功。

ClipSnap普通对象行为不变。

框架不自动序列化 EcsWorld / ActorWorld / Queue。
```

---

## 14. Build / Audit / Freeze 测试

```text
Build()返回 Running Runtime，保留 master用法。

无效 OwnerLayer / OwnerScope在 Activate前失败。

同步 Event环保持 EventCycleException。

LocalCall冲突按 Scope检测。

ScopeEvent / ScopeCall无效目标 Build失败。

RouteId同输入稳定。

Topology Audit不扫描业务 IL。

RuntimeCompositionPlan先 Freeze再 Activate。

每 Scope Prewarm本地 EventCenter。

Prewarm后 Registry Freeze。

Running不能动态注册 Handler / Route。

Known Event不触发 Reflection Fallback。
```

---

## 15. Diagnostics 测试

```text
OnLayerEventInfo继续工作。

Topology / Policy Markdown继续工作。

Worker Snapshot通过 ScopeCall。

没有 Diagnostics Queue。

Diagnostics关闭时无额外 Allocation。

关闭时不对每 Handler计时。

Snapshot不暴露 Runtime内部对象。

Payload / Promise Outstanding在 Dispose后归零。
```

---

## 16. 多 Runtime 隔离

```text
Runtime A / B有独立 ScopeRuntime。

EventCenter不共享。

EcsWorld不共享。

Tool Cache不共享。

ActorWorld不共享。

FullSnap Coordinator不共享。

Dispose A不影响 B。

Static只保留不可变 Metadata / Factory / TypeId。

旧 Binding Generation不能访问新 Runtime。
```

---

## 17. AOT / IL2CPP 仓库内门禁

LayerBase库目标包括：

```text
net8.0

netstandard2.1
```

仓库内可以执行的 AOT相关门禁：

```bash
dotnet build LayerBase/LayerBase.csproj \
    -c Release \
    -f netstandard2.1
```

并执行源码 / 架构测试，禁止 Running路径依赖：

```text
System.Reflection.Emit

DynamicMethod

Expression.Compile

dynamic调用

运行期扫描全部程序集

运行期 MakeGenericType + Activator创建热路径

未生成的泛型 Handler调用

ModuleInitializer注册可变 Runtime状态
```

允许：

```text
Build冷路径受控反射

System.Text.Json现有 Snap路径

源生成器生成静态调用

不可变 Metadata
```

不能把普通 `dotnet build`称为“IL2CPP已通过”。

---

## 18. 真实 Unity IL2CPP 门禁

当前 LayerBase仓库不是 Unity工程，没有：

```text
Assets

ProjectSettings

Unity Editor Build Script

asmdef集成测试工程
```

因此本任务不得伪造：

```text
Unity -batchmode ...
```

作为仓库内已执行命令。

真实发布门禁必须在已经使用 LayerBase的现有 Unity集成项目执行：

```text
Scripting Backend = IL2CPP

Managed Stripping Level = 目标发布配置

Development Build和非 Development Build各至少一次

Windows或目标平台实际 Player Build

启动 Player并运行 Scope Smoke Test
```

Smoke Test至少覆盖：

```text
Build Runtime

MainScope / WorkerScope Activate

Event / LocalCall / ScopeCall

Generated Query + Input

LayerTool创建

Actor Projection

FullSnap MainScope路径

Stop / Dispose
```

如果当前执行环境没有该 Unity项目：

```text
最终报告必须写：
    IL2CPP Gate = Not Executed

不得写 Passed。

仓库实现可以达到 Repo Acceptance，
但不能宣称 Release Acceptance。
```

本迁移任务不新建一个空 Unity工程来冒充真实消费场景。

---

## 19. Benchmark 原则

使用同一：

```text
机器

.NET SDK

Release配置

进程优先级

BenchmarkDotNet配置

输入规模
```

比较：

```text
master基线 Commit

Scope迁移实现 Commit
```

faster Benchmark只在对应实现被移植时复用。

---

## 20. Benchmark 集合

### Event / Call

```text
Local Event Send

Local Post

ScopeEvent 1P / 4P / 8P Submit

ScopeEvent Dispatch

LocalCall

ScopeCall RoundTrip

Promise Complete / Cancel / Timeout
```

### Scope Runtime

```text
MainScope Tick

InlineScope Tick

WorkerScope Tick

ScopeExecution Enter / Restore

SynchronizationContext Drain

Activate / Stop / Dispose
```

### ECS

```text
Unbatched Query

Batched Query

Query + Input

Bring + Projection Batch

CommandBuffer Record / Playback

Blueprint Create

MainScope vs WorkerScope Query
```

### Actor

```text
Actor Mail

Actor Call

Projection Command Encode

MainActor Apply

Projection Result Apply

ProjectedActor Touch / Release
```

### Tool / Snap

```text
ScopeTool cached lookup

Tool first create

FullSnap MainScope

FullSnap多 Scope SafePoint协调

ClipSnap
```

### Prewarm

```text
First Send after Build

First Post after Build

First Query after Build

First Actor Mail after Build
```

---

## 21. 性能门槛

不设置没有现有基线支持的绝对纳秒目标。

强制规则：

```text
原本稳态 0 Allocation的路径必须继续 0 Allocation。

Running不得出现反射和程序集扫描。

ScopeEvent / ScopeCall Payload以外不得产生每消息分配。

LocalCall不得创建 Promise或入队。

Cached Tool Lookup不得分配。

无 Input Query关闭 Batch时不得因 Scope迁移增加结构分配。

Diagnostics关闭不得增加分配。

First Tick不得进行结构注册和大型首次扩容。
```

相对性能：

```text
优先使用现有 Benchmark Compare项目中的门槛。

若某 Benchmark没有现成阈值：
    输出 master与新实现的 Mean / Error / Allocated。
    标记需要人工审查。
    不随意写入新的百分比阈值。
```

数量级退化无论有无阈值都必须阻止合并。

---

## 22. 压力与泄漏测试

### 并发

```text
多个 Scope双向 Event / Call。

Queue满载与 Stop并发。

Fault与 Dispose并发。

Snapshot与 Stop并发。

Projection Result与 Scope退出并发。
```

### 泄漏

使用现有 WeakReference和池统计方法验证：

```text
Build失败 Runtime可回收。

Activate失败 Service / Context可回收。

Dispose后 Worker Thread退出。

ScopeRuntime可回收。

Payload Outstanding = 0。

Promise Pending = 0。

Tool Cache全部 Dispose。

CommandBuffer无租用对象。

旧 ScopeRef不保持 Runtime存活。
```

---

## 23. CI 阶段

```text
Stage 1：
    restore + Debug build

Stage 2：
    Generator / Analyzer /静态架构测试

Stage 3：
    全部 Debug NUnit测试

Stage 4：
    Release build + Release NUnit测试

Stage 5：
    多线程压力 / Fault / Leak测试

Stage 6：
    短 Benchmark Compare

Stage 7：
    netstandard2.1 AOT静态门禁

Stage 8：
    外部 Unity IL2CPP Gate（发布环境）
```

Stage失败：

```text
不得进入后续 Stage并声称最终通过。
```

---

## 24. 最终交付证据

必须保存：

```text
git commit SHA

dotnet --info

restore log

Debug build log

Release build log

Debug test TRX或完整 log

Release test TRX或完整 log

架构源码扫描结果

压力测试随机种子和结果

Leak测试结果

Benchmark结果文件

netstandard2.1 build log

Unity IL2CPP Build log或 Not Executed声明

faster复用记录
```

---

## 25. Repo Acceptance

以下全部成立：

```text
所有 master兼容测试通过。

所有新增 Scope测试通过。

Debug / Release Build通过。

net8.0 / netstandard2.1通过。

无架构否决项。

无死锁、悬挂 Promise、Worker Join失败。

无 Payload / Tool / CommandBuffer泄漏。

Benchmark无未解释数量级退化。

Diagnostics Disabled无热路径回归。
```

即可标记：

```text
Repository Scope Migration Accepted
```

---

## 26. Release Acceptance

还必须：

```text
在真实 Unity消费项目完成 IL2CPP Build。

Player启动并完成 Smoke Test。

没有 AOT泛型缺失、裁剪缺失或反射创建失败。
```

才能标记：

```text
Unity IL2CPP Release Accepted
```

Repo Acceptance不能替代 Release Acceptance。

---

## 27. 最终验收否决项

出现任意一项，不得宣告完成：

```text
修改 master原测试预期掩盖回归

只跑 Debug不跑 Release

只编 net8.0不编 netstandard2.1

ScopeRuntime仍持有 ActorWorld

PostScheduler仍有跨线程入口

存在第三条跨 Scope业务 Queue

LocalCall仍自动跨 Scope

LayerTool仍跨 Scope共享实例

Generated Query入口不是 void

ECS World仍 Runtime全局共享

FullSnap直接读 Worker对象

Build后仍动态注册 Route

Diagnostics关闭仍影响热路径

Payload / Promise / Tool存在泄漏

Worker Thread无法稳定 Join

没有 Benchmark基线

未执行 Unity Build却声称 IL2CPP Passed

为了测试通过恢复已删除 API
```

---

## 28. 本阶段最终结果

```text
测试首先保护 master原行为。

新增测试证明 Scope架构真实成立。

AOT仓库门禁与真实 Unity IL2CPP门禁被明确区分。

Benchmark使用已有项目和真实基线，
不写无证据的魔法数字。

最终报告能够明确回答：
    代码是否构建
    测试是否通过
    架构是否符合
    性能是否回归
    资源是否泄漏
    IL2CPP是否真的执行
```
