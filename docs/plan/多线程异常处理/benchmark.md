按照如下设计方案完成任务：# LayerBase Scope 与统一异常通道基准测试设计方案

## 0. 测试目标

本轮基准测试覆盖最近新增或大幅改造的能力：

```text
ScopeRuntime
ScopeExecution
Scope Post
Scope Call
ScopePromise
LBTask 跨 Scope continuation
有限环形队列
LayerExceptionHub
LayerExceptionHub 与 LayerHub 的兼容融合
Scope + ECS + Actor 综合运行
```

测试分为四层：

```text
L1：基础结构单片测试
    测量队列、上下文、Promise、异常记录等最小成本。

L2：单功能完整链路测试
    测量一次 Post、Call、continuation、异常上报的完整成本。

L3：多功能组合测试
    测量 Scope、LBTask、ECS、Actor、异常通道协同后的成本。

L4：游戏帧场景测试
    模拟真实游戏主线程与多个 Worker Scope，测量帧时间和吞吐。
```

最终需要回答：

```text
1. 引入 Scope 后，普通 MainScope 游戏的空转成本增加了多少？
2. ScopeExecution 的上下文进入和退出成本是多少？
3. Local / Worker Scope 的 Post 吞吐是多少？
4. Call 的入队、派发、Promise、continuation 分别占多少成本？
5. async LBTask ScopeCall 相比同步返回增加多少成本？
6. 多个 Scope 同时运行时，吞吐是否随 Scope 数量恶化？
7. LayerExceptionHub 的空队列检查是否影响普通帧？
8. 异常上报、排空、兼容 LayerHub 回调各自付出多少成本？
9. 多线程同时上报异常时是否出现锁竞争和吞吐崩塌？
10. Scope + ECS 是否真正降低主线程 p95 / p99 帧耗时？
```

---

# 1. 当前实现中必须重点测量的路径

## 1.1 Scope 队列

当前 Worker Scope 的 Post、Call 和 continuation 使用有限队列；默认容量为 1024。Worker 使用 `LockedBoundedRingQueue`，而 Inline Scope 的 Post/Call 可以使用 `LocalRingQueue`。

`LockedBoundedRingQueue` 的每次入队、出队和 Count 读取都会获得同一把锁，因此必须分别测量：

```text
单线程锁成本
一个生产者一个消费者
多个生产者一个消费者
队列接近满载时的竞争
队列满时的失败路径
```

其现有实现是固定数组加短锁，不会自动扩容。

## 1.2 ScopeExecution

当前 Scope 上下文通过 `AsyncLocal` 保存，进入 Scope 时创建 FrameHolder，并在退出时恢复之前的上下文。

必须测量：

```text
Enter + Dispose
Current 读取
嵌套 Scope Enter
async/await 前后的上下文流转
```

## 1.3 ScopePromise

当前 `ScopePromise<TResult>` 使用对象锁保护：

```text
IsCompleted
OnCompleted
SetResult
SetException
GetResult
```

完成后会把 continuation 投到原 Scope 的 continuation 队列。

因此要拆分测量：

```text
Promise 创建
注册 continuation
完成 Promise
投递 continuation
Drain continuation
GetResult
```

## 1.4 生成式 Scope Call

最新 Generator 已支持两类 Handler：

```text
同步 TResult 返回
LBTask<TResult> 返回
```

对于 `LBTask<TResult>`，生成代码还要执行：

```text
GetAwaiter
IsCompleted
OnCompleted
GetResult
Promise.SetResult / SetException
```

同时，当前生成的 Dispatcher 会在 Scope 的 `IService[]` 中线性查找目标 Service。

所以必须增加：

```text
Service 数量扩展测试：1 / 8 / 32 / 128
```

这能判断线性查找何时成为明显瓶颈。

## 1.5 LayerExceptionHub

当前异常记录构造时会执行：

```csharp
ExceptionDispatchInfo.Capture(exception)
```

异常记录创建成本和队列运输成本必须分开测量，不能混在一个结果里。

`LayerExceptionHub` 使用线程安全有限环形队列：

```text
任意线程 Report
主线程 DrainAndDispatch
队列满时记录 overflow
```

此外，`LayerRuntime.PumpCore` 目前每帧都会排空 ExceptionHub，因此必须验证空队列检查对普通无异常帧的影响。

---

# 2. 项目组织

保留现有：

```text
LayerBase.BenchMark
```

但不要继续把新测试全部放进已经很大的 `Program.cs`。

建议结构：

```text
LayerBase.BenchMark/
├─ Program.cs
├─ Config/
│  ├─ LayerBaseQuickBenchmarkConfig.cs
│  ├─ LayerBaseStableBenchmarkConfig.cs
│  └─ LayerBaseScenarioConfig.cs
│
├─ Infrastructure/
│  ├─ BenchmarkBlackhole.cs
│  ├─ BenchmarkScopeFactory.cs
│  ├─ BenchmarkRuntimeFactory.cs
│  ├─ BenchmarkLatencyRecorder.cs
│  ├─ BenchmarkWorkerBarrier.cs
│  └─ BenchmarkResultVerifier.cs
│
├─ Scope/
│  ├─ ScopeQueueBenchmarks.cs
│  ├─ ScopeExecutionBenchmarks.cs
│  ├─ ScopePostBenchmarks.cs
│  ├─ ScopeCallBenchmarks.cs
│  ├─ ScopePromiseBenchmarks.cs
│  ├─ ScopeContinuationBenchmarks.cs
│  ├─ ScopeRuntimePumpBenchmarks.cs
│  └─ ScopeRuntimeBuildBenchmarks.cs
│
├─ Exception/
│  ├─ LayerExceptionRecordBenchmarks.cs
│  ├─ LayerExceptionHubBenchmarks.cs
│  ├─ LayerExceptionFusionBenchmarks.cs
│  └─ LayerExceptionContentionBenchmarks.cs
│
├─ Integration/
│  ├─ ScopePostCallPipelineBenchmarks.cs
│  ├─ ScopeAsyncAwaitPipelineBenchmarks.cs
│  ├─ ScopeEcsPipelineBenchmarks.cs
│  └─ ScopeExceptionPipelineBenchmarks.cs
│
└─ Scenarios/
   ├─ ScopeFrameSimulationBenchmarks.cs
   ├─ MultiScopeGameScenario.cs
   └─ ScopeSoakScenario.cs
```

现有 `BenchmarkSwitcher.FromAssembly` 可以自动发现新文件中的 Benchmark 类型，不需要为每个类手动加入 Program。

---

# 3. 运行配置

## 3.1 Quick 模式

用途：

```text
本地开发
PR 冒烟
检查 Benchmark 是否能正常完成
```

配置：

```text
Job.ShortRun
MemoryDiagnoser
MarkdownExporter
按 Category 分组
```

可继续使用当前默认配置。

## 3.2 Stable 模式

用途：

```text
性能优化前后对比
发布前回归
主分支性能报告
```

建议：

```text
WarmupCount：5
IterationCount：15
LaunchCount：2
RunStrategy：Throughput
MemoryDiagnoser：开启
ThreadingDiagnoser：并发测试开启
```

不要使用 `ShortRun` 的单次结果作为性能回归结论。

## 3.3 Scenario 模式

真实帧场景不应完全依赖 BenchmarkDotNet 的单方法均值。

需要独立运行固定帧数：

```text
预热：2,000 帧
采样：10,000 帧
重复：3 轮
```

记录：

```text
p50
p90
p95
p99
最大值
标准差
超出 16.67 ms 的帧数
队列最大深度
端到端消息延迟
```

---

# 4. 通用指标

## 4.1 单片指标

```text
Mean ns/op
Median ns/op
Min / Max
Operations/s
Allocated B/op
Gen0 / Gen1
LockContentions
```

## 4.2 跨线程链路指标

```text
SubmitCost
DispatchCost
ContinuationCost
EndToEndLatency
Throughput
QueueHighWaterMark
QueueFullCount
PendingCallCount
```

## 4.3 帧场景指标

```text
MainThreadFrameTime p50 / p95 / p99
WorkerTickTime p50 / p95 / p99
MissedFrameRatio
MainThreadSavedRatio
TotalCompletedMessages
DroppedOrRejectedMessages
ScopeContextMismatchCount
UnhandledPromiseCount
ExceptionCallbackThreadMismatchCount
```

其中：

```text
MainThreadSavedRatio =
    1 - ScopeMainThreadCost / SingleRuntimeMainThreadCost
```

---

# 5. 第一组：有限环形队列单片基准

类别：

```text
08.Scope.Queue
```

## 5.1 测试对象

```text
Queue<T> 无锁单线程基线
LocalRingQueue<T>
lock + Queue<T> 基线
LockedBoundedRingQueue<T>
ConcurrentQueue<T> 对照
```

这里的 `ConcurrentQueue<T>` 只是横向参照，不代表最终设计一定改用它。

## 5.2 参数

```csharp
[Params(64, 1024, 16384)]
public int Capacity;

[Params(1, 64, 1024)]
public int BatchSize;
```

消息尺寸：

```text
4 字节 int
16 字节 struct
64 字节 struct
引用对象
LayerExceptionRecord
ScopePostMessage
ScopeCallMessage
```

## 5.3 Benchmark 方法

```text
Local_EnqueueOnly
Local_DequeueOnly
Local_RoundTrip

Locked_SameThread_EnqueueOnly
Locked_SameThread_RoundTrip

Locked_Spsc_RoundTrip
Locked_Mpsc_2Producers
Locked_Mpsc_4Producers
Locked_Mpsc_8Producers

Locked_EmptyDequeue
Locked_FullEnqueueFailure
Locked_CountRead
```

## 5.4 重点输出

```text
单线程锁成本
生产者数量增加后的吞吐曲线
容量是否影响正常路径
大 struct 是否带来明显复制成本
队列满失败路径成本
```

---

# 6. 第二组：ScopeExecution 上下文基准

类别：

```text
08.Scope.ExecutionContext
```

## 6.1 Benchmark

```text
DirectDelegateBaseline
AsyncLocalReadBaseline
ScopeExecution_CurrentRead
ScopeExecution_EnterExit
ScopeExecution_NestedEnterExit_Depth4
ScopeExecution_NestedEnterExit_Depth16
ScopeExecution_Enter_WithSynchronizationContext
```

## 6.2 async 上下文测试

分别测试：

```text
无 await
await 已完成 LBTask
await LBTask.Yield
跨线程完成后恢复
```

## 6.3 验证

每次运行结束必须断言：

```text
恢复后的 ScopeId == await 前 ScopeId
恢复线程 == 原 Scope Worker 线程
没有恢复到统一 MainScope
```

这项断言不能放在每个纳秒级操作内部，而是在 IterationCleanup 统一验证，避免污染测量结果。

---

# 7. 第三组：Scope Post 基准

类别：

```text
08.Scope.Post
```

## 7.1 单片拆分

```text
RawQueueEnqueue
ScopeRuntime.TryPost
ScopeRouteTable.TryPost
ScopeRef.TryPost
GeneratedPostDispatch
PostDrain
PostEnqueueAndDrain
```

## 7.2 Scope 类型

```text
Inline -> Inline
Main -> Worker
Worker -> Main
Worker A -> Worker B
```

## 7.3 Batch 参数

```csharp
[Params(1, 32, 256, 1024)]
public int MessageCount;
```

## 7.4 Payload 参数

```text
引用对象，不产生额外构造
小 struct 装箱
中型 struct 装箱
预构造 Payload
```

当前 `ScopeRef.TryPost` 接收 `object payload`，因此需要明确观察 struct 装箱带来的 GC。

## 7.5 输出

```text
仅提交成本
目标 Scope 派发成本
端到端耗时
每消息分配
每秒消息吞吐
批量提交的边际成本
```

---

# 8. 第四组：ScopePromise 与 continuation 单片基准

类别：

```text
08.Scope.Promise
08.Scope.Continuation
```

## 8.1 Promise Benchmark

```text
CreatePromise
IsCompleted_False
IsCompleted_True
RegisterContinuation_BeforeComplete
RegisterContinuation_AfterComplete
SetResult_NoContinuation
SetResult_WithContinuation
SetException_WithContinuation
GetResult_Success
GetResult_Exception
```

## 8.2 continuation Benchmark

```text
RawContinuationQueueEnqueue
ScopeTryEnqueueContinuation
DrainOneContinuation
DrainBatch32
DrainBatch256
CompleteOnWorker_ResumeInlineScope
CompleteOnWorker_ResumeWorkerScope
```

## 8.3 竞争矩阵

```text
完成先于 OnCompleted
OnCompleted 先于完成
完成与注册并发竞争
```

因为 `ScopePromise` 通过锁解决完成和注册的竞争，这组测试能判断锁在高频 Call 下的成本。

---

# 9. 第五组：Scope Call 基准

类别：

```text
08.Scope.Call
```

## 9.1 基线

```text
DirectServiceMethod
DirectDelegate
DirectLBTaskFromResult
```

## 9.2 框架路径

```text
ScopeCall_SyncHandler
ScopeCall_CompletedLBTaskHandler
ScopeCall_YieldingLBTaskHandler
ScopeCall_ExceptionHandler
```

## 9.3 Service 数量参数

```csharp
[Params(1, 8, 32, 128)]
public int ServiceCount;
```

目的：

```text
量化当前 GeneratedScopeCallDispatcher
线性 FindService<TService> 的扩展成本。
```

结果应展示：

```text
ServiceCount = 1 时成本
ServiceCount = 32 时成本
ServiceCount = 128 时成本
每增加一个 Service 的近似扫描成本
```

如果增长明显，下一轮优化应改为：

```text
生成 ServiceSlot
直接 services[slot]
```

## 9.4 调用拓扑

```text
Inline -> Inline
Main -> Worker
Worker -> Main
Worker A -> Worker B
```

## 9.5 Outstanding Call 参数

```csharp
[Params(1, 16, 64, 256)]
public int OutstandingCalls;
```

分别测：

```text
逐个 Call 并等待
批量提交后统一等待
流水线式提交和完成
```

## 9.6 输出

```text
Submit ns/call
Dispatch ns/call
Promise ns/call
Continuation ns/call
RoundTrip p50 / p95 / p99
Calls/s
B/call
```

---

# 10. 第六组：ScopeRuntime 空转与生命周期基准

类别：

```text
08.Scope.Runtime
```

## 10.1 空 Pump

```text
LegacyRuntime_EmptyPump
Runtime_WithMainScope_EmptyPump
Runtime_1InlineScope_EmptyPump
Runtime_4InlineScopes_EmptyPump
Runtime_8InlineScopes_EmptyPump
Runtime_1WorkerScope_EmptyPump
Runtime_4WorkerScopes_EmptyPump
```

目的：

```text
测量即使项目不使用重型多线程功能，
Scope 基础设施每帧带来的固定成本。
```

## 10.2 Service 扩展

```csharp
[Params(0, 1, 8, 32, 128)]
public int ServiceCount;
```

测试：

```text
无 IUpdate
全部为 IUpdate 空实现
少量工作 IUpdate
```

## 10.3 生命周期

```text
RuntimeBuild
ScopeHostBuild
ScopeRuntimeCreate
BindServices
StartInlineScope
StartWorkerScope
StopDrain
StopDrop
Dispose
```

输出：

```text
Build time
Start time
Stop time
Allocated bytes
Worker thread 创建成本
```

---

# 11. 第七组：LayerExceptionRecord 基准

类别：

```text
09.Exception.Record
```

必须把异常对象创建、记录创建和运输拆开。

## 11.1 Benchmark

```text
ReuseExistingExceptionBaseline
CreateNewException
CaptureExceptionDispatchInfo
CreateLayerExceptionRecord
CreateQueueOverflowException
```

测试时使用：

```csharp
private readonly Exception _existingException =
    new InvalidOperationException("Benchmark");
```

这样可以区分：

```text
Exception 分配成本
ExceptionDispatchInfo.Capture 成本
LayerExceptionRecord struct 构造成本
```

---

# 12. 第八组：LayerExceptionHub 运输基准

类别：

```text
09.Exception.Transport
```

## 12.1 无竞争

```text
Report_PrecreatedRecord
Drain_NoSubscriber
ReportAndDrain_NoSubscriber
ReportAndDrain_DetailedSubscriber
ReportAndDrain_LegacySubscriber
ReportAndDrain_BothSubscribers
```

## 12.2 空队列

```text
EmptyDrain
RuntimePump_EmptyExceptionHub
RuntimePump_WithoutExceptionDrainBaseline
```

最重要的普通路径目标：

```text
没有异常时：
    不产生 GC。
    每帧空检查成本稳定。
    不因融合 LayerHub 而明显增加普通帧成本。
```

## 12.3 多生产者

```csharp
[Params(1, 2, 4, 8)]
public int ProducerCount;
```

测试：

```text
MPSC_Report_ConsumerIdle
MPSC_Report_ConcurrentDrain
MPSC_ReportBurst
```

参数：

```csharp
[Params(64, 512, 4096)]
public int ExceptionCount;
```

## 12.4 Overflow

```text
Capacity2_Report5
Capacity64_Report1024
MultipleProducers_Overflow
OverflowSnapshotDrain
```

Overflow 是故障路径，不应与正常 Report 混在同一基准结果中。

---

# 13. 第九组：LayerExceptionHub 与 LayerHub 融合基准

类别：

```text
09.Exception.Fusion
```

这组测试只在融合实现完成后启用。

## 13.1 基线链路

```text
DirectActionLayerEventInfo
LegacyRuntimeReportInfo
LegacyLayerHubInternalNotify
```

## 13.2 融合链路

```text
ExceptionHubReportOnly
ExceptionHubDrainToDetailedCallback
ExceptionHubDrainToLegacyLayerEventInfo
ExceptionHubDrainToDetailedAndLegacy
OwnerThreadReportImmediateDrain
WorkerReportDeferredDrain
```

## 13.3 订阅者数量

```csharp
[Params(0, 1, 4, 16)]
public int SubscriberCount;
```

分别作用于：

```text
ExceptionCallbacks.OnExceptionRecord
LayerRuntime.OnLayerEventInfo
LayerHub.OnLayerEventInfo
```

## 13.4 关键指标

```text
Record -> LayerEventInfo 转换成本
字符串格式化分配
单异常兼容回调总分配
用户无订阅时的成本
多个订阅者扇出成本
```

注意：

```text
异常路径可以分配；
无异常路径必须保持零新增分配。
```

---

# 14. 第十组：跨 Scope async-await 综合链路

类别：

```text
10.Scope.AsyncPipeline
```

## 14.1 场景 A：Main -> Combat -> Main

```text
MainScope 发起 Call
CombatScope Worker 接收
Handler 返回 LBTask<TResult>
Promise 完成
continuation 投回 MainScope
MainScope Pump continuation
```

记录：

```text
Call submit
Worker dequeue
Handler start
Handler finish
Promise complete
Continuation enqueue
Continuation execute
```

## 14.2 场景 B：Combat -> Asset -> Combat

用途：

```text
证明 Worker Scope await 后仍回 Worker Scope，
并测量非 MainScope 回归成本。
```

## 14.3 场景 C：链式调用

```text
MainScope
    -> CombatScope
        -> AIScope
            -> AssetScope
        <- AIScope
    <- CombatScope
<- MainScope
```

参数：

```csharp
[Params(1, 2, 4)]
public int CallDepth;
```

输出：

```text
总延迟
每增加一级 Call 的成本
每级 continuation 投递成本
线程上下文错误数
```

---

# 15. 第十一组：Scope + ECS 综合基准

类别：

```text
10.Scope.ECS
```

现有 ECS Benchmark 已经将 Sync 执行、Async Submit 和 EndToEnd 分开，这是正确模式，应继续沿用。

新增 Scope 对照：

```text
LegacyRuntime_SyncEcs
LegacyRuntime_AsyncEcs
CombatScope_SyncEcs
CombatScope_AsyncEcs
CombatScope_AsyncEcs_WithActorOutput
```

参数：

```text
EntityCount：
    1,000
    10,000
    100,000

WorkIterations：
    0
    8
    32
    128
```

拆分指标：

```text
MainScope submit
CombatScope dequeue
ECS execute
ECS result drain
Actor event output
MainScope visible
```

最终回答：

```text
把 ECS 放进 Worker Scope 后，
相比原 AsyncEcsScheduler，
是否又增加了不必要的重复队列和调度层？
```

如果链路变成：

```text
Main -> ScopeQueue -> CombatScope
     -> EcsQueue -> EcsWorker
     -> ResultQueue -> CombatScope
     -> ActorWorld
```

必须明确量化“双重调度”的代价。

---

# 16. 第十二组：异常综合链路

类别：

```text
10.Scope.ExceptionPipeline
```

## 16.1 Worker 异常

```text
CombatScope Handler 抛异常
    -> ScopeRuntime 捕获
    -> Runtime.ExceptionHub
    -> Main owner thread Drain
    -> ExceptionCallbacks
    -> LayerRuntime.OnLayerEventInfo
    -> LayerHub.OnLayerEventInfo
```

输出：

```text
Worker catch 到 Report 成本
Report 到主线程可见延迟
Drain 和转换成本
完整回调成本
```

## 16.2 Call 异常

```text
Call handler 抛异常
    -> ExceptionHub Report
    -> Promise.SetException
    -> await 方恢复并观察异常
```

需要检查：

```text
LayerHub 只收到一次异常
await 方只收到一次异常
没有重复上报
没有 Promise 悬挂
```

## 16.3 continuation 异常

```text
Call 正常返回
await 后的 continuation 抛异常
```

检查：

```text
Phase == Continuation
异常归属 == continuation 所在 Scope
回调最终在 Runtime owner thread
```

---

# 17. 第十三组：游戏帧整体场景

建议建立三个固定 Profile。

## 17.1 Light Profile

```text
1 个 MainScope
1 个 CombatScope Worker
1,000 个 ECS 实体

每帧：
    100 Post
    10 Call
    10 continuation
    1 次 ECS Query
    无异常
```

## 17.2 Medium Profile

```text
MainScope
CombatScope
AIScope
NetScope

10,000 个 ECS 实体

每帧：
    1,000 Post
    100 Call
    100 continuation
    4 次 ECS Query
    100 ActorEvent
```

## 17.3 Heavy Profile

```text
MainScope
CombatScope
AIScope
NetScope
AssetScope

100,000 个 ECS 实体

每帧：
    5,000 Post
    500 Call
    500 continuation
    8 次重 ECS Query
    1,000 ActorEvent
```

## 17.4 对照模式

每个 Profile 至少有：

```text
SingleRuntime：
    所有逻辑在原 Runtime/MainScope 顺序执行。

ScopeInline：
    使用 Scope 资源域，但全部 Inline。

ScopeWorker：
    重计算 Scope 使用 Worker。

ScopeWorkerWithExceptionFusion：
    开启完整统一异常通道。
```

这样能够拆出：

```text
Scope 抽象本身的成本
Worker 并行带来的收益
异常通道融合的空转成本
```

---

# 18. 异常注入场景

整体场景增加四个异常率：

```text
None：
    0 异常

Rare：
    每 100,000 次操作 1 个异常

NormalFailure：
    每 10,000 次操作 1 个异常

Burst：
    某一帧连续产生 512 个异常
```

异常不是为了测试“越快越好”，而是验证：

```text
异常突发时是否阻塞主线程
ExceptionHub 是否快速达到满载
正常消息是否仍能推进
下一帧是否恢复
是否出现异常递归
```

---

# 19. 长时间稳定性测试

BenchmarkDotNet 不适合承担全部稳定性测试。

另建：

```text
ScopeSoakScenario
```

## 19.1 时长

```text
本地：10 分钟
夜间 CI：1 小时
发布前：4 小时
```

## 19.2 负载

```text
4 个 Worker Scope
持续 Post / Call / async-await
周期性 ECS Query
周期性异常注入
周期性队列接近满载
```

## 19.3 验收

```text
无 Promise 永久 Pending
无 continuation 投错 Scope
无 Worker 意外退出
无未限制内存增长
无异常回调线程错误
无异常重复通知
队列 Count 始终在 Capacity 内
Runtime Stop 能够完成
```

---

# 20. 编译性能基准

当多程序集 Module 模型实现后，再建立独立的：

```text
LayerBase.CompileBench
```

模拟 Unity asmdef：

```text
1 个程序集
4 个程序集
16 个程序集
32 个程序集
```

每个程序集包含：

```text
10 / 50 / 200 个 Service
5 / 20 个 Context
1 / 4 个 Scope
Event / Call Handler
```

测试：

```text
Clean Build
No-change Build
只修改一个 Service
只修改一个 Context
只修改公共 Layer Contract
修改 Generator
```

指标：

```text
总编译时间
受影响程序集数量
Generator 执行时间
生成源码大小
增量编译命中率
内存峰值
```

这组测试在 Module 模型落地前不应混入当前运行时性能报告。

---

# 21. 测试辅助接口

为了避免 Benchmark 通过反射访问私有字段，建议加入：

```csharp
[assembly: InternalsVisibleTo(
    "LayerBase.BenchMark")]
```

只开放内部测试钩子：

```text
ScopeRuntime.DrainPostInboxForBenchmark
ScopeRuntime.DrainCallInboxForBenchmark
ScopeRuntime.DrainContinuationsForBenchmark
ScopeRuntime.WaitWorkerParkedForBenchmark
ScopeRuntime.GetQueueSnapshotForBenchmark

LayerExceptionHub.CountForBenchmark
LayerExceptionHub.DrainCountForBenchmark

ScopePromise completion hook
Runtime owner thread ID
Scope worker thread ID
```

不要为了 Benchmark 扩大正式 public API。

---

# 22. Benchmark 编写规则

## 22.1 不把 Setup 算进热路径

这些操作放入 `GlobalSetup`：

```text
Runtime Build
创建 Scope
创建 Worker
创建 Service
创建实体
预热 Generator 路由
预热 Call
预热 Worker
```

生命周期测试除外。

## 22.2 跨线程测试先预热 Worker

测试前确保：

```text
Worker 已启动
Worker 已处理至少一个任务
Worker 处于稳定等待状态
```

冷启动单独建立：

```text
ColdWorkerStartBenchmark
```

不能把冷启动和稳定吞吐混在一个数字里。

## 22.3 IterationCleanup 必须排空状态

每轮结束：

```text
排空 Post
排空 Call
排空 continuation
等待所有 Promise
排空异常
确认队列 Count == 0
```

否则后一轮会受到上一轮残留影响。

## 22.4 不在 Benchmark 热循环里做 Assert

热循环只写入：

```csharp
Volatile.Write(
    ref BenchmarkSink.IntValue,
    value);
```

正确性在 `IterationCleanup` 检查。

## 22.5 不混合不同成本

例如异常测试必须拆成：

```text
创建 Exception
创建 LayerExceptionRecord
Report
Drain
转换 LayerEventInfo
调用用户回调
```

不能只给一个“异常总耗时”。

---

# 23. 性能回归标准

第一阶段先运行 5 次 Stable Benchmark，建立基线，不立即设置绝对纳秒门槛。

稳定后使用相对阈值：

## 23.1 单片回归

```text
Mean 增长 > 15%：
    警告

Mean 增长 > 25%：
    阻止合并

新增 B/op：
    对零分配热路径直接阻止合并
```

## 23.2 整体回归

```text
MainThread p95 增长 > 10%：
    警告

MainThread p99 增长 > 15%：
    阻止合并

MissedFrameRatio 增长：
    不允许高于基线 1 个百分点

Promise Pending：
    必须为 0

ScopeContextMismatch：
    必须为 0
```

## 23.3 异常路径

异常路径不要求零分配，但要求：

```text
无异常时零新增分配
异常突发后系统可以恢复
异常回调不在 Worker 执行
异常队列满不会导致死锁
```

---

# 24. 输出报告

每次正式运行保存：

```text
BenchmarkDotNet.Artifacts/
├─ commit-sha/
│  ├─ micro/
│  ├─ integration/
│  ├─ scenario/
│  └─ environment.json
```

环境信息：

```text
Commit SHA
.NET Runtime
CPU
核心数
内存
操作系统
电源计划
Debug / Release
Benchmark 配置
```

最终报告分为：

```text
1. 单片成本
2. 单功能完整链路
3. Scope 数量扩展
4. 多线程竞争
5. 异常通道
6. Scope + ECS
7. 游戏帧整体场景
8. 性能回归结论
```

---

# 25. 实施顺序

## Phase 1：单片基础

```text
ScopeQueueBenchmarks
ScopeExecutionBenchmarks
ScopePromiseBenchmarks
LayerExceptionRecordBenchmarks
LayerExceptionHubBenchmarks
```

## Phase 2：单功能链路

```text
ScopePostBenchmarks
ScopeCallBenchmarks
ScopeContinuationBenchmarks
ScopeRuntimePumpBenchmarks
LayerExceptionFusionBenchmarks
```

## Phase 3：组合链路

```text
ScopeAsyncAwaitPipelineBenchmarks
ScopeExceptionPipelineBenchmarks
ScopeEcsPipelineBenchmarks
```

## Phase 4：整体场景

```text
Light / Medium / Heavy
SingleRuntime / Inline / Worker 对照
帧时间 p95 / p99
```

## Phase 5：稳定性

```text
10 分钟本地 Soak
1 小时夜间 CI
异常 Burst
队列接近满载
```

---

# 26. 最终验收问题

完成这套 Benchmark 后，报告必须能够直接回答：

```text
1. ScopeRuntime 每帧最低固定成本是多少？
2. AsyncLocal ScopeExecution 是否成为热路径瓶颈？
3. LockedBoundedRingQueue 在几个生产者后开始明显退化？
4. Post 每秒能处理多少条消息？
5. Call 的主要成本来自路由、Service 查找、Promise 还是 continuation？
6. 当前线性 FindService 在多少 Service 后需要改成 ServiceSlot？
7. LBTask<TResult> Handler 比同步 TResult Handler 贵多少？
8. Worker Scope 是否真正降低 MainThread p95 / p99？
9. LayerExceptionHub 空转是否影响正常帧？
10. 异常融合到 LayerHub 后增加多少分配？
11. 异常 Burst 是否会拖垮普通 Scope 消息？
12. Scope + ECS 是否出现重复调度成本过高的问题？
13. 多 Scope 下 async-await 是否全部恢复到正确上下文？
14. LayerBase 是否适合承担中大型游戏的多运行域内核？
```

只有这些问题都有数据回答，Scope 的性能评估才算完整。

