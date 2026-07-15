# 07 LBTask 与 SynchronizationContext 修复

> **强制执行规范：** 本文的实现必须遵守 `01_mandatory_architecture_aot_performance_standards.md`；冲突时以该规范为准。  

> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **文档性质：** 独立修复文档。本文只处理 LBTask 和 `SynchronizationContext`，不修改 ScopeCall、ScopeEvent、DI、Mount、Provide/From 或 ActorWorld 管线。

---

<!-- ARCHITECTURE-REVISION-START -->
## 0. 架构位置

LBTask 负责“异步等待后回到原 Scope Owner Thread”：

```text
Scope Handler
    → await IO / Delay / ScopeCall
    → Awaiter 捕获 SynchronizationContext.Current
    → Completion Thread SetResult
    → 原 Scope Context.Post
    → 原 Scope Owner Thread continuation
```

它不承担跨 Scope 通讯；跨 Scope 由 ScopeCall 完成，LBTask 只负责调用方 continuation 回归。

### 0.1 最终公有 API

业务 API 不需要显式传 Scope：

```csharp
[SubscribeAsync]
private async LBTask OnLoad(in LoadPlayerEvent value)
{
    PlayerData data =
        await _repository.LoadAsync(value.PlayerId);

    PathResult path =
        await this.Scope<PathfindingScope>()
            .Call<FindPathRequest, PathResult>(
                new FindPathRequest(data.Position, data.Target));

    Apply(data, path); // 自动回到当前 Scope。
}
```

延迟：

```csharp
await this.Delay(TimeSpan.FromMilliseconds(200));
```

不公开 `LBTaskSynchronizationContext` 给业务代码。

### 0.2 Context 安装

```text
MainScope/InlineScope Pump：
    using (scope.Context.Enter())
        scope.Pump()

WorkerScope Thread Main：
    SynchronizationContext.SetSynchronizationContext(scope.Context)
    while (Running)
        scope.Pump()
```

### 0.3 关键数据结构

```csharp
internal sealed class LBTaskSynchronizationContext
    : SynchronizationContext
{
    private readonly ScopeContinuationQueue _queue;

    public override void Post(
        SendOrPostCallback callback,
        object? state);
}
```

Continuation Queue 必须：

```text
有界或池化
MPSC 写入
Owner Thread Drain
Stop 时 Fail/Drop 有明确策略
```

`Send` 不对外支持，避免其他线程同步阻塞 Scope。

### 0.4 业务场景：WorkerScope 等待 IO

```text
WorkerScope Handler
    → await File IO
    → IO Thread 完成
    → Worker Scope Context Queue
    → Wake Worker
    → Worker Thread 恢复 Handler
```

不允许恢复到 MainScope。

### 0.5 faster 复用

直接复用：

```text
LBTask 核心、Awaiter、Promise Pool
LayerBaseSynchronizationContext 的队列和测试
同步完成 0 GC 路径
```

改造：

```text
从 Runtime 单 Context 改为每 Scope Context
捕获 SynchronizationContext.Current
Stop 时按 Scope Fail Pending
```

禁止：

```text
强制回 Main Thread
TaskScheduler.FromCurrentSynchronizationContext
ThreadPool fallback
```
<!-- ARCHITECTURE-REVISION-END -->

## 1. 修复目标

LBTask 保持现有设计：

```text
LBTaskSource 捕获 SynchronizationContext.Current
任务可在任意线程完成
continuation 通过捕获的 SynchronizationContext.Post 回到原执行线程
```

不新增：

```text
LBTaskResumeTarget
ScopeTaskRegistry
LBTaskCompletionEvent
Scope 专用 CompletionPort
ScopeEndpoint 捕获
```

Scope 与 LBTask 的连接只体现在：

```text
每个独立执行线程安装自己的 LBTaskSynchronizationContext
```

因此：

```text
MainScope 的异步代码
    → 捕获主线程的 LBTaskSynchronizationContext

Worker Scope 的异步代码
    → 捕获 Worker 线程自己的 LBTaskSynchronizationContext

Inline Scope 的异步代码
    → 在该 Scope Tick 期间临时进入其自己的 LBTaskSynchronizationContext
```

LBTask 本身不需要知道 RuntimeId、ScopeId 或 ScopeRuntime。

---

## 2. 核心结论

### 2.1 Worker Scope

每个 Worker Runtime 在线程入口创建并安装独立上下文：

```csharp
private void Run()
{
    SynchronizationContext? previous = SynchronizationContext.Current;
    var context = LBTaskSynchronizationContext.CreateForCurrentThread();

    SynchronizationContext.SetSynchronizationContext(context);

    try
    {
        RunScopeLoop(context);
    }
    finally
    {
        context.CloseAndDrain();
        SynchronizationContext.SetSynchronizationContext(previous);
        context.Dispose();
    }
}
```

Worker Scope 内执行的：

```text
async LBTask
LBTask.Yield
LBTask.NextFrame
LBTask.Delay
LBTask.RunBackground
```

都会自然捕获该 Worker 的上下文。

### 2.2 MainScope

MainScope 在主线程 Pump 时进入自己的上下文：

```csharp
using (mainContext.Enter())
{
    MainScope.TickLocal(deltaTime);
}
```

如果 MainScope 的上下文在 Runtime 生命周期内始终安装于主线程，也必须在 Runtime Dispose 后恢复原上下文。

### 2.3 Inline Scope

多个 Inline Scope 共用同一物理线程，但可以拥有独立上下文。

每个 Inline Scope Tick 前切换：

```csharp
using (inlineScope.SynchronizationContext.Enter())
{
    inlineScope.TickLocal(deltaTime);
}
```

Tick 结束后恢复前一个上下文。

因此在 Inline Scope 内创建的 LBTask 仍会捕获该 Scope 自己的上下文，而不是 MainScope 上下文。

---

## 3. 与 ScopeCall / ScopeEvent 的边界

LBTask 的 continuation 调度不属于 Scope 间业务通讯。

因此：

```text
Scope → Scope 的业务请求
    使用 ScopeCall

Scope → Scope 的单向业务消息
    使用 ScopeEvent

LBTask 在原线程恢复 continuation
    使用捕获的 LBTaskSynchronizationContext
```

ScopeCall Handler 返回结果后：

```text
1. ScopeCall Response 通过调用方 ScopeCall Inbox 返回。
2. 调用方 Scope 在 Owner Thread 处理 Response。
3. Promise/LBTaskSource 在调用方线程进入完成状态。
4. continuation 按捕获的 SynchronizationContext 调度。
```

不为 LBTask 单独增加跨 Scope 通讯协议。

---

## 4. faster 可复用代码

### 4.1 直接移植

| faster 文件 | 复用内容 |
|---|---|
| `LayerBase.Task/LBTask.cs` | LBTask/LBTask<T> Awaitable 外形 |
| `LayerBase.Task/LBTaskMethodBuilder.cs` | Early-completed 优化、Builder 骨架 |
| `LayerBase.Task/LBTaskSources.cs` | Version Token、单 Awaiter、单次消费、对象池引用清理 |
| `LayerBase.Task/LBTaskCompletionSource.cs` | 手动完成任务 |
| `LayerBase.Task/LayerContinuation.cs` | continuation 数据结构 |
| `LayerBase.Task/MainThreadCompletionQueue.cs` | 完成队列批量 Drain 逻辑 |

### 4.2 修改移植

| faster 文件 | 修改内容 |
|---|---|
| `LayerBase.Task/LayerBaseSynchronizationContext.cs` | 重命名或明确语义为 `LBTaskSynchronizationContext`；每个执行线程可独立创建 |
| `LayerBase.Task/LBTaskSources.cs` | 禁止 Context Dispose 后回退 ThreadPool 执行 Scope 内 continuation |
| `LayerBase.Task/LBTask.cs` | 修正 Yield、NextFrame、Delay、RunBackground 对捕获 Context 的使用 |
| `LayerBase/Scope/ScopePromise.cs` | Promise 完成后依赖普通 LBTask Source 捕获 Context，不再依赖 ScopeCompletionPort |
| Worker Runtime 线程入口 | 安装、Pump、关闭独立 SynchronizationContext |
| Inline Scope Tick | 临时 Enter 自己的 SynchronizationContext |

### 4.3 仅参考

```text
faster 中的 ScopeAwaitRegistry
faster 中的 Context Dispose 测试
faster 中的 LBTask Source Pool 回收测试
faster 中的 Worker Scope continuation 测试
```

### 4.4 禁止移植

```text
ScopeCompletionPort
ScopeTaskRegistry
LBTaskResumeTarget
LBTaskCompletionEvent
ScopeRuntime 引用捕获
Dispose 后 ThreadPool fallback
队列满时 Producer Thread inline 执行 continuation
```

---

## 5. LBTaskSynchronizationContext

### 5.1 职责

每个上下文只负责：

```text
接收 Post
保存本线程待执行 continuation
保存 NextFrame continuation
在 Owner Thread Drain
关闭时取消尚未执行的工作
记录未完成的 LBTaskSource
```

它不负责：

```text
ScopeCall
ScopeEvent
Scope 生命周期控制
ActorWorld
DI
跨 Scope 路由
```

### 5.2 最小结构

```csharp
public sealed class LBTaskSynchronizationContext
    : SynchronizationContext, IDisposable
{
    private readonly int _ownerThreadId;

    private readonly ConcurrentQueue<WorkItem> _posted;
    private readonly List<FrameWorkItem> _frameWork;
    private readonly HashSet<IContextDisposeCancellable> _pendingSources;

    private bool _closing;
    private bool _disposed;

    public static LBTaskSynchronizationContext CreateForCurrentThread();

    public ContextScope Enter();

    public override void Post(SendOrPostCallback callback, object? state);

    public override void Send(SendOrPostCallback callback, object? state);

    public void ScheduleInFrames(
        SendOrPostCallback callback,
        object? state,
        int frames);

    public void Update(int maxItems = 0);

    internal bool TryRegisterSource(IContextDisposeCancellable source);

    internal void UnregisterSource(IContextDisposeCancellable source);

    public void BeginClose(Exception reason);

    public void DrainClosingOperations();

    public void Dispose();
}
```

---

## 6. Post 语义

`Post` 可以从任意线程调用：

```text
Producer Thread
    → ConcurrentQueue<WorkItem>
    → Owner Thread Update
    → continuation
```

Post 只唤醒目标上下文，不直接执行 callback。

禁止：

```text
队列满时 inline callback
Dispose 后自动 ThreadPool callback
临时切换到目标 ScopeExecution 执行 callback
```

上下文队列是 LBTask 调度设施，不承担 Scope 控制命令。

---

## 7. Send 语义

### Owner Thread

```text
立即执行 callback
```

### 非 Owner Thread

本次修复不提供阻塞式跨线程 Send：

```csharp
throw new NotSupportedException(
    "Synchronous Send to another LBTaskSynchronizationContext thread is not supported.");
```

原因：

```text
阻塞式 Send 容易造成 Scope 间互相等待。
跨 Scope 同步请求应使用 ScopeCall。
```

不复用 faster 中基于 `ManualResetEventSlim` 的跨线程阻塞 Send。

---

## 8. LBTaskSource 修复

优先移植 faster 已完成的以下修复。

### 8.1 Version Token

每次从对象池 Rent 时递增 Version：

```text
旧 Awaiter 持有旧 Version
Source 被复用后 Version 改变
旧 Awaiter 访问时立即失败
```

防止池化对象 ABA。

### 8.2 单 Awaiter

LBTask 只允许一个 continuation：

```csharp
if (_continuation != null)
{
    throw new InvalidOperationException(
        "LBTask only supports one awaiter continuation.");
}
```

### 8.3 单次结果消费

`GetResult` 只能执行一次，防止 Source 提前重复归池。

### 8.4 引用清理

归还对象池前必须清理：

```text
Continuation
Exception
CancellationToken
Result
Captured SynchronizationContext
Registered Context
```

### 8.5 Context 注册

Scope 内创建的 TaskSource 向当前 `LBTaskSynchronizationContext` 注册：

```text
Rent
    → context.TryRegisterSource(source)

GetResult / Cancel / Fault / Release
    → context.UnregisterSource(source)
```

Context 关闭时取消所有仍注册的 Source。

---

## 9. 捕获规则

`LBTaskSource.Rent()`：

```csharp
SynchronizationContext? context = SynchronizationContext.Current;
```

如果当前 Context 是 `LBTaskSynchronizationContext`：

```text
注册 Source
保存 Context 引用
```

如果是普通 SynchronizationContext：

```text
只保存引用
不参与 LayerBase 的关闭管理
```

如果为 null：

```text
continuation 使用 ThreadPool
```

LBTask 不读取：

```text
ScopeExecution.Current
ScopeId
RuntimeId
ScopeEndpoint
```

---

## 10. continuation 调度

TaskSource 完成时：

```csharp
private bool Schedule(Action continuation)
{
    SynchronizationContext? context = _context;

    if (context != null)
    {
        context.Post(
            static state => ((Action)state!).Invoke(),
            continuation);

        return true;
    }

    ThreadPool.QueueUserWorkItem(
        static state => ((Action)state!).Invoke(),
        continuation);

    return true;
}
```

特殊规则：

```text
捕获的是 LBTaskSynchronizationContext 且该 Context 已关闭：
    不回退 ThreadPool；
    Source 进入取消或 ObjectDisposedException 状态。
```

普通外部 SynchronizationContext Post 失败时，是否回退 ThreadPool 保留原 .NET 兼容语义，但不得用于已绑定 LayerBase Scope 的任务。

---

## 11. LBTask API 修复

### 11.1 Yield

当前基线的 `Yield` 直接投递 ThreadPool，需要修正。

```text
存在 SynchronizationContext.Current：
    Post 到当前 Context

不存在：
    ThreadPool
```

```csharp
public static LBTask Yield()
{
    SynchronizationContext? context = SynchronizationContext.Current;
    var source = LBTaskSource.Rent(context);

    if (context != null)
    {
        context.Post(
            static state => ((LBTaskSource)state!).SetResult(),
            source);
    }
    else
    {
        ThreadPool.QueueUserWorkItem(
            static state => ((LBTaskSource)state!).SetResult(),
            source);
    }

    return new LBTask(source);
}
```

### 11.2 NextFrame

```text
当前 Context 是 LBTaskSynchronizationContext：
    ScheduleInFrames(..., 1)

普通 Context：
    Post 一次

无 Context：
    明确回退 ThreadPool
```

Worker Scope 中的“下一帧”表示 Worker Scope 下一次 Tick。

Inline Scope 中表示该 Inline Scope 下一次被 `LayerRuntime` 推进。

### 11.3 Delay

保留真实时间 Delay Scheduler。

`LBTaskSource` 在创建时已经捕获 Context，因此 Timer Thread 只调用：

```csharp
source.SetResult();
```

Source 会将 continuation Post 回捕获 Context。

无需增加 Scope Completion Event。

### 11.4 RunBackground

后台工作在线程池执行。

完成后只调用捕获了 Context 的 Source：

```csharp
source.SetResult();
source.SetException(exception);
source.SetCanceled(token);
```

不需要手工访问 `context.CompletionQueue`，避免两套完成逻辑。

### 11.5 RunOnMainThread / SwitchToMainThread

这两个 API 不是本次修复重点，不扩大设计。

迁移规则：

```text
保留现有签名
要求调用方显式传入目标 SynchronizationContext
未传入且当前 Context 为空时抛错
```

不引入全局 Primary Runtime 搜索。

后续对外 API 文档可以单独决定是否废弃名称。

---

## 12. Context Update 顺序

每个 Scope Tick 中：

```text
1. Enter 当前 Scope 的 LBTaskSynchronizationContext。
2. Drain 已 Post continuation。
3. 更新 FrameWork，移动到 ready queue。
4. 执行 ready continuation。
5. 推进 Scope 业务资源。
6. 可在 Tick 末尾再 Drain 一次本 Tick 新产生的 continuation。
7. Restore 前一个 SynchronizationContext。
```

具体是否帧首、帧尾各 Drain 一次由 Scope 运行文档决定；LBTask 文档只要求顺序稳定且每 Tick 有明确 Drain 点。

---

## 13. Worker 关闭

Worker 收到 `ScopeStopCall` 后，在 Worker Owner Thread：

```text
1. 禁止业务系统创建新的异步工作。
2. context.BeginClose(reason)。
3. 取消尚未执行的 FrameWork。
4. 取消已注册但未完成的 TaskSource。
5. Drain 关闭 continuation。
6. 确认 PendingSourceCount == 0。
7. 执行 Scope Stop。
8. 返回 StopCall Response。
```

收到 `ScopeDisposeCall` 后：

```text
1. 确认 Context 已关闭。
2. 确认无 Pending Source 和 Pending Work。
3. Dispose Scope 资源。
4. 返回 DisposeCall Response。
5. Dispose SynchronizationContext。
6. WorkerLoop 退出。
```

Stop/Dispose 命令仍由 ScopeCall 管线负责；SynchronizationContext 不新增生命周期控制队列。

---

## 14. Inline Scope 上下文切换

由于 Inline Scope 共用主线程，必须严格恢复上下文：

```csharp
using (inlineScope.SynchronizationContext.Enter())
{
    inlineScope.TickLocal(deltaTime);
}
```

`Enter()` 保存原 `SynchronizationContext.Current`，Dispose 时恢复。

必须测试嵌套：

```text
MainScope Context
    → InlineScopeA Context
    → 恢复 MainScope Context
    → InlineScopeB Context
    → 恢复 MainScope Context
```

防止 InlineScopeA 创建的 LBTask 错误捕获 MainScope 或 InlineScopeB Context。

---

## 15. 实施文件

### 从 faster 优先移植

```text
LayerBase.Task/LBTask.cs
LayerBase.Task/LBTaskSources.cs
LayerBase.Task/LBTaskMethodBuilder.cs
LayerBase.Task/LBTaskCompletionSource.cs
LayerBase.Task/LayerContinuation.cs
LayerBase.Task/LayerBaseSynchronizationContext.cs
```

### 修改

```text
LayerBase/Scope/ScopeWorker.cs
LayerBase/Scope/ScopeRuntime.cs
LayerBase/Application/LayerRuntime.cs
LayerBase/Scope/ScopePromise.cs
```

### 不新增

```text
LBTaskResumeTarget.cs
ScopeTaskRegistry.cs
LBTaskCompletionEvent.cs
ScopeCompletionPort.cs
```

---

## 16. 实施顺序

1. 从 faster 移植 LBTaskSource Version、单 Awaiter、单次消费和池清理。
2. 从 faster 移植 SynchronizationContext 的 Owner Thread、FrameWork 和关闭逻辑。
3. 将 Context 明确为每执行线程独立实例。
4. 在 Worker 线程入口安装独立 Context。
5. 在 MainScope 和 Inline Scope Tick 中正确 Enter/Restore Context。
6. 修复 `LBTask.Yield`。
7. 简化 `Delay` 和 `RunBackground`，统一依赖 Source 捕获 Context。
8. 删除 Dispose 后 ThreadPool fallback。
9. 删除 ScopeCompletionPort 依赖。
10. 添加 Worker、Inline Scope 和池化回归测试。

---

## 17. 必须测试

```text
Worker_installs_independent_synchronization_context
Different_workers_have_different_context_instances
Worker_task_captures_worker_context
Worker_task_continuation_runs_on_worker_thread
Main_scope_task_captures_main_context
Inline_scope_task_captures_inline_scope_context
Inline_scope_context_is_restored_after_tick
Nested_inline_scope_context_switch_restores_previous
Yield_posts_to_current_context
NextFrame_resumes_on_next_owner_scope_tick
Delay_resumes_on_captured_context
RunBackground_resumes_on_captured_context
Completion_never_runs_continuation_inline_on_producer_thread
Context_close_cancels_pending_sources
Disposed_scope_context_does_not_fallback_to_thread_pool
Pooled_source_version_rejects_stale_awaiter
Pooled_source_result_is_consumed_once
Pooled_source_clears_context_and_continuation_references
Cross_thread_send_is_not_supported
```

---

## 18. 验收否决项

出现以下任一项即否决：

```text
LBTaskSource 捕获 ScopeRuntime
LBTask 引入 ScopeId/RuntimeId/ScopeEndpoint
为 LBTask 新增独立 Scope Completion 通道
Worker Scope 共用 MainScope SynchronizationContext
Inline Scope Tick 没有切换并恢复自己的 Context
Context Dispose 后 continuation 回退 ThreadPool
队列满或 Post 失败时 Producer Thread inline 执行 continuation
RunBackground 同时使用 Source Context 和额外 CompletionQueue
```

---

## 19. 最终验收结论

修复完成后，LBTask 的模型必须保持简单：

```text
async 方法在哪个 Scope 执行
    → 当时线程安装哪个 SynchronizationContext
    → LBTask 就捕获哪个 SynchronizationContext
    → continuation 回到该 SynchronizationContext 的 Owner Thread
```

Scope 负责正确安装和推进上下文。

LBTask 只负责正确捕获、完成、调度、取消和回收，不感知 Scope 架构。
