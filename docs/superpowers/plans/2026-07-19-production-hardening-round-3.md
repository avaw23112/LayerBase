# LayerBase 生产加固 Round 3：Agent 执行方案

> **Agent 执行要求：** 使用 `superpowers:subagent-driven-development`，每个任务分配一个独立实现 Agent；每项完成后分别进行规范审查和代码质量审查，不允许一个 Agent 连续修改全部模块。

**基线提交：**

```text
4a8e6451a2b534d1c63e52b5ed831ee18c99399e
Merge: production hardening round 2
```

**目标：** 修复剩余的生产稳定性、预算公平性、可靠故障投递、热路径退化和发布门禁问题。

**架构原则：**

* Scope 状态只能由 Owner Thread 修改。
* 跨线程状态变化只能经过 `ScopeCompletionInbox`、`ScopeEvent` 或 `ScopeCall`。
* 不增加游戏、ECS 业务功能。
* 不进行无关重构。
* 每个任务必须先制造失败测试，再写实现。
* 每个任务独立提交，不允许最后一次性提交全部修改。

---

# 一、总控 Agent 提示词

可直接交给总控 Agent：

```text
你正在修复 avaw23112/LayerBase 的生产稳定性问题。

执行前必须：
1. git fetch origin。
2. 查询 origin/master 的真实 SHA。
3. 如果 SHA 不等于 4a8e6451a2b534d1c63e52b5ed831ee18c99399e，
   先对比 4a8e6451..origin/master。
4. 如果后续提交修改了本计划涉及的文件，停止实施并重新审计冲突区域。
5. 创建独立 worktree 和分支 fix/production-hardening-round-3。
6. 先运行 Release 构建和现有全量测试，记录基线结果。

工程约束：
- 严格 TDD：先写失败测试并确认失败，再改实现。
- 所有跨线程 Worker 状态变化必须通过 ScopeCompletionInbox。
- 不允许 Worker Thread 直接写 WorkerJobCoordinator。
- 不修改公开 API，除非任务明确要求。
- 不进行无关格式化、目录重构或命名清理。
- 每项任务独立提交。
- 每次提交前运行定向测试。
- 完成所有任务后运行 Release 全量测试、ProductionHardening、
  ProductionSoak、NuGet pack 和依赖漏洞检查。
- 没有实际命令输出，不得声称测试通过。
```

---

# 二、执行顺序

```text
Task 0  基线验证
Task 1  Worker Running 状态闭环
Task 2  Shutdown 控制结果与故障路径
Task 3  ScopeWorker 超时后的延迟资源回收
Task 4  Inline Scope 公平轮转与共享预算
Task 5  Coalesced 类型正确淘汰与 O(1) 热路径
Task 6  Fault 可靠投递
Task 7  LBTask.Delay 注册竞态与 Lease 生命周期
Task 8  Timer FireAllCapped 语义
Task 9  CI、NuGet 与供应链门禁
Task 10 全量验证与最终审查
```

Task 1～7 为生产门禁。Task 8～9 完成后才允许正式发包。

---

# Task 0：建立干净基线

## 操作

```powershell
git fetch origin

$expected = "4a8e6451a2b534d1c63e52b5ed831ee18c99399e"
$actual = git rev-parse origin/master

Write-Host "Expected audit base: $expected"
Write-Host "Current origin/master: $actual"

if ($actual -ne $expected) {
    git diff --name-status $expected $actual
    throw "origin/master changed after the audit. Re-audit changed production-hardening files before implementation."
}

git worktree add `
  ../LayerBase-production-hardening-r3 `
  -b fix/production-hardening-round-3 `
  $actual

Set-Location ../LayerBase-production-hardening-r3

dotnet restore
dotnet build -c Release --no-restore
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release --no-build
```

## 验收

* 工作区无修改。
* Release Build 成功。
* 记录现有失败测试，不得把基线失败归因于新代码。
* 不允许跳过基线测试直接修改。

---

# Task 1：补齐 Worker Pending → Running → Terminal 状态

`MarkExecutionStarted()` 已经存在，并负责设置 `WorkerState.Running` 和增加 `_runningCount`，但 Worker 执行路径没有发布"开始执行"通知。 当前 `WorkerExecutionItem.Execute()` 直接执行 Job 并只发布最终完成事件。

## 文件

* 修改：`LayerBase/Scope/ScopeCompletionInbox.cs`
* 修改：`LayerBase/Scope/ScopeRuntime.cs`
* 修改：`LayerBase/Worker/WorkerExecutionItem.cs`
* 修改：`LayerBase/Worker/WorkerJobCoordinator.cs`
* 修改：`LayerBase.Test/WorkerCoordinatorRaceTests.cs`
* 修改：`LayerBase.Test/WorkerCompletionInboxTests.cs`

## 先写失败测试

新增：

```csharp
[Test]
public void Blocking_job_enters_running_before_physical_completion()
{
    using var entered = new ManualResetEventSlim(false);
    using var release = new ManualResetEventSlim(false);

    var service = new BlockingWorkerService(entered, release);
    var layer = new BlockingWorkerLayer();
    layer.RegisterService(service);

    using LayerRuntime runtime = LayerHub.CreateLayers()
        .Push(layer)
        .Build();

    WorkerHandle handle = service.Run(CancellationToken.None);

    Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);

    Assert.That(
        SpinUntil(() =>
        {
            runtime.Pump(0f);
            return runtime.WorkerJobs.GetState(handle) == WorkerState.Running;
        }),
        Is.True);

    Assert.That(runtime.WorkerJobs.RunningCount, Is.EqualTo(1));

    release.Set();

    Assert.That(
        SpinUntil(() =>
        {
            runtime.Pump(0f);
            return runtime.WorkerJobs.GetState(handle) == WorkerState.Completed;
        }),
        Is.True);

    Assert.That(runtime.WorkerJobs.RunningCount, Is.EqualTo(0));
}
```

先运行：

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --filter "FullyQualifiedName~Blocking_job_enters_running_before_physical_completion"
```

预期：失败，因为当前状态不会进入 `Running`。

## 实现要求

扩展枚举：

```csharp
internal enum ScopeCompletionKind : byte
{
    WorkerExecutionCompleted = 0,
    WorkerCancelRequested = 1,
    WorkerExecutionStarted = 2
}
```

增加工厂：

```csharp
public static ScopeCompletionEnvelope WorkerExecutionStarted(
    WorkerHandle handle)
{
    var emptyCompletion = default(WorkerExecutionCompletedScopeEvent);

    return new ScopeCompletionEnvelope(
        ScopeCompletionKind.WorkerExecutionStarted,
        in emptyCompletion,
        handle);
}
```

`WorkerExecutionItem.Execute()` 中：

```csharp
if (_token.IsCancellationRequested)
{
    completion = CreateCancelledCompletion();
}
else
{
    SubmitExecutionStarted();

    try
    {
        var context = new WorkerJobContext(workerIndex, _token);
        TEvent result = _job.Execute(in _input, in context);

        completion = _token.IsCancellationRequested
            ? CreateCancelledCompletion()
            : new WorkerExecutionCompletedScopeEvent(
                _handle,
                WorkerExecutionCompletionKind.Succeeded,
                new WorkerExecutionResult<TEvent>(in result),
                _options,
                WorkerJobExceptionInfo.None);
    }
    // 保留现有异常处理
}
```

新增：

```csharp
private void SubmitExecutionStarted()
{
    ScopeCompletionEnvelope envelope =
        ScopeCompletionEnvelope.WorkerExecutionStarted(_handle);

    _origin.Transport.EnqueueCompletion(in envelope);
}
```

`ScopeRuntime.DrainCompletionInbox()` 增加：

```csharp
case ScopeCompletionKind.WorkerExecutionStarted:
    WorkerJobs.MarkExecutionStarted(envelope.WorkerHandle);
    break;
```

## 禁止方案

禁止：

```csharp
coordinator.MarkExecutionStarted(handle);
```

从 Worker Thread 直接调用 Coordinator。这样会破坏 Owner Thread 单写模型。

## 验收

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --filter "FullyQualifiedName~WorkerCoordinatorRaceTests|FullyQualifiedName~WorkerCompletionInboxTests"
```

提交：

```powershell
git add LayerBase/Scope LayerBase/Worker LayerBase.Test
git commit -m "fix(worker): publish physical execution start to origin scope"
```

---

# Task 2：修复 Shutdown 控制结果和关闭期故障路径

当前 Host 一进入 `Dispose()` 就将自身视为 disposed，但 Scope 关闭过程中仍可能调用 `ApplyFaultPolicy()`；同时 Worker Scope 的 Dispose Task 完成后没有调用 `GetResult()`，异常和响应状态都可能被忽略。

## 文件

* 修改：`LayerBase/Scope/ScopeRuntimeHost.cs`
* 修改：`LayerBase/Application/LayerRuntime.cs`
* 新增：`LayerBase.Test/ScopeShutdownStateTests.cs`
* 修改：`LayerBase.Test/WorkerShutdownTimeoutTests.cs`

## 测试一：关闭过程中 Fault Policy 不得抛出 ObjectDisposedException

测试应：

1. 创建 Worker Scope。
2. 在 Dispose 生命周期中抛出指定异常。
3. 启动 Host shutdown。
4. 确认 Fault 记录包含原始异常。
5. 确认没有被 `ObjectDisposedException` 替换。

## 测试二：Dispose Control 异常必须被消费

创建一个 `DisposeReverse()` 抛异常的 Worker Scope，断言：

```csharp
Assert.That(
    recordedException,
    Is.TypeOf<InvalidOperationException>());

Assert.That(
    recordedException!.Message,
    Does.Contain("dispose failed"));
```

## 实现要求

将状态拆分为：

```csharp
private int _shutdownStarted;
private int _disposed;
```

规则：

```text
shutdownStarted:
    不再接受新的 Host 级业务操作
    仍允许内部目录查询、Fault Policy 和控制消息

disposed:
    Worker 已退出
    Scope 资源已清理
    不再允许任何操作
```

`ApplyFaultPolicy()` 不再调用会触发 `ThrowIfDisposed()` 的公开查询路径：

```csharp
public void ApplyFaultPolicy(in ScopeFaultRecord record)
{
    if (!_directory.TryGetRuntime(
            record.SourceScopeId,
            out ScopeRuntime sourceScope))
    {
        return;
    }

    switch (sourceScope.Options.FaultPolicy)
    {
        case ScopeFaultPolicy.ReportAndContinue:
            return;

        case ScopeFaultPolicy.StopScope:
            _ = sourceScope.RequestStopAsync();
            return;

        case ScopeFaultPolicy.StopRuntime:
            _ = MainScope.RequestStopAsync();
            return;

        default:
            throw new ArgumentOutOfRangeException();
    }
}
```

Worker Dispose 必须读取结果：

```csharp
ScopeDisposeResponse response =
    ScopeControlBarrier.Wait(
        scope.RequestDisposeAsync(),
        in deadline,
        $"{scope.Descriptor.Name}.Dispose");

ScopeControlBarrier.EnsureSucceeded(
    response.State,
    "Dispose",
    scope);
```

Shutdown 可以捕获异常并继续清理其他 Scope，但不能跳过 `GetResult()`。

## 验收

* 控制 Task 不存在未观察异常。
* Shutdown 故障不会被二次 `ObjectDisposedException` 覆盖。
* 正常关闭行为保持不变。

提交：

```powershell
git add LayerBase/Scope/ScopeRuntimeHost.cs `
        LayerBase/Application/LayerRuntime.cs `
        LayerBase.Test

git commit -m "fix(scope): preserve control and fault handling during shutdown"
```

---

# Task 3：ScopeWorker 超时后的延迟资源回收

先纠正执行目标：当前 Timeout 分支**不会立即释放正在使用的 WaitHandle**；真正的问题是线程稍后退出后，没有可靠的后续回收路径。当前资源只在成功 Join 或未启动时释放。

## 文件

* 修改：`LayerBase/Scope/ScopeWorker.cs`
* 修改：`LayerBase.Test/WorkerShutdownTimeoutTests.cs`

## 设计

增加握手状态：

```csharp
private int _startWaitCompleted;
private int _threadExited;
private int _resourcesReleased;
```

增加安全唤醒入口：

```csharp
private void SignalWork()
{
    if (Volatile.Read(ref _resourcesReleased) != 0)
        return;

    try
    {
        _workSignal.Set();
    }
    catch (ObjectDisposedException)
    {
        // Thread has already exited and released the wake handle.
    }
}
```

构造函数改为：

```csharp
_runtime.BindWorkerWakeSignal(SignalWork);
```

`Start()` 必须使用 finally 完成握手：

```csharp
public void Start(in ShutdownDeadline deadline)
{
    if (_startedThread)
        return;

    _startedThread = true;
    _thread.Start();

    try
    {
        int remaining = deadline.RemainingMilliseconds;

        if (remaining <= 0 || !_ready.Wait(remaining))
        {
            throw new TimeoutException(
                $"Scope worker `{_runtime.Descriptor.Name}` did not become ready before the build deadline.");
        }

        Exception? startupException =
            Volatile.Read(ref _startupException);

        if (startupException != null)
        {
            throw new InvalidOperationException(
                $"Scope worker `{_runtime.Descriptor.Name}` failed during startup.",
                startupException);
        }
    }
    finally
    {
        Volatile.Write(ref _startWaitCompleted, 1);
        TryReleaseResourcesAfterExit();
    }
}
```

Worker `Run()` 的 finally：

```csharp
finally
{
    try
    {
        if (_runtime.State != ScopeRuntimeState.Disposed)
            _runtime.RunRuntimeStop();
    }
    finally
    {
        SynchronizationContext.SetSynchronizationContext(previousContext);
        Volatile.Write(ref _threadExited, 1);
        TryReleaseResourcesAfterExit();
    }
}
```

释放逻辑：

```csharp
private void TryReleaseResourcesAfterExit()
{
    if (Volatile.Read(ref _startWaitCompleted) == 0 ||
        Volatile.Read(ref _threadExited) == 0)
    {
        return;
    }

    ReleaseResources();
}

private void ReleaseResources()
{
    if (Interlocked.Exchange(ref _resourcesReleased, 1) != 0)
        return;

    _ready.Dispose();
    _workSignal.Dispose();
}
```

## 测试

增加可解除阻塞的 Worker：

```text
开始阻塞
→ Stop 超时
→ 确认尚未释放资源
→ 解除阻塞
→ Worker 退出
→ 最终资源自动释放
```

允许添加内部诊断属性：

```csharp
internal bool ResourcesReleased =>
    Volatile.Read(ref _resourcesReleased) != 0;
```

不得添加公开 API。

提交：

```powershell
git add LayerBase/Scope/ScopeWorker.cs `
        LayerBase.Test/WorkerShutdownTimeoutTests.cs

git commit -m "fix(scope): release worker signals after delayed thread exit"
```

---

# Task 4：修复 Inline Scope 公平轮转和预算统计

当前公平游标存放在每帧重新创建的 `RuntimeFrameBudget.StartingScopeIndex` 中，因此下一帧重新从 0 开始。

同时 Inline Scope 调用 `PostScheduler.Pump(ref budget)` 后没有将处理数量计入 `UsedWorkItems`。 主 Scope 则会显式消费数量。

## 文件

* 修改：`LayerBase/Scope/ScopeRuntimeHost.cs`
* 修改：`LayerBase/Scope/ScopeRuntime.cs`
* 修改：`LayerBase.Test/RuntimeScopeBudgetTests.cs`
* 新增：`LayerBase.Test/InlineScopeFairnessTests.cs`

## 公平轮转实现

Host 新增：

```csharp
private int _nextInlineScopeIndex;
```

替换：

```csharp
int startIndex =
    budget.StartingScopeIndex % _inlineScopes.Length;
```

为：

```csharp
int startIndex =
    _nextInlineScopeIndex % _inlineScopes.Length;
```

结束后：

```csharp
_nextInlineScopeIndex =
    (startIndex + 1) % _inlineScopes.Length;
```

保留 `RuntimeFrameBudget.StartingScopeIndex` 字段，避免公开结构体破坏性变更，但不再将它作为 Host 持久状态。

## 预算消费实现

`ScopeRuntime.PumpScopeResourcesCore()`：

```csharp
PostPumpStats postStats =
    PostScheduler?.Pump(ref budget)
    ?? new PostPumpStats(0, 0, 0, 0);

budget.Consume(postStats.ProcessedCount);
```

## 公平测试

测试逻辑必须是：

```text
Scope A 和 B 各有一个事件
第一帧预算 1：A 被处理，B 保留
再次给 A 投递一个事件
第二帧预算 1：
    正确实现必须先处理 B
    若仍从 A 开始，B 会继续饥饿
```

最终断言：

```csharp
Assert.That(scopeB.PostScheduler!.HasPendingWork, Is.False);
Assert.That(scopeA.PostScheduler!.HasPendingWork, Is.True);
```

## 预算测试

断言 Inline Scope 处理三个 Post 后：

```csharp
Assert.That(budget.UsedWorkItems, Is.EqualTo(3));
Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
```

提交：

```powershell
git add LayerBase/Scope `
        LayerBase.Test/RuntimeScopeBudgetTests.cs `
        LayerBase.Test/InlineScopeFairnessTests.cs

git commit -m "fix(scope): persist inline fairness and consume shared work budget"
```

---

# Task 5：修复 Coalesced 错误淘汰和 O(n²) 退化

当前单类型数量需要遍历整个 `_coalescedBuffer`；某个类型超限后调用的是全局最旧淘汰，可能删除另一个事件类型。 当前全局列表还使用 `RemoveAt(0)`。

## 文件

* 修改：`LayerBase/Event/PostScheduler/CoalescingStructures.cs`
* 修改：`LayerBase/Event/PostScheduler/PostScheduler.cs`
* 新增：`LayerBase.Test/PostSchedulerCoalescedEvictionTests.cs`

## 数据结构

将：

```csharp
private readonly List<CoalescedSlotKey> _pendingCoalesced = new();
```

改为：

```csharp
private readonly LinkedList<CoalescedSlotKey>
    _pendingCoalesced = new();

private readonly Dictionary<int, LinkedList<CoalescedSlotKey>>
    _pendingCoalescedByType = new();
```

`CoalescedSlot` 增加内部节点：

```csharp
internal LinkedListNode<CoalescedSlotKey>? GlobalOrderNode;
internal LinkedListNode<CoalescedSlotKey>? TypeOrderNode;
```

## 插入

```csharp
LinkedListNode<CoalescedSlotKey> globalNode =
    _pendingCoalesced.AddLast(slotKey);

if (!_pendingCoalescedByType.TryGetValue(
        typeId,
        out LinkedList<CoalescedSlotKey>? typeOrder))
{
    typeOrder = new LinkedList<CoalescedSlotKey>();
    _pendingCoalescedByType.Add(typeId, typeOrder);
}

LinkedListNode<CoalescedSlotKey> typeNode =
    typeOrder.AddLast(slotKey);

newSlot.GlobalOrderNode = globalNode;
newSlot.TypeOrderNode = typeNode;
```

## 单类型淘汰

```csharp
private bool EvictOldestCoalescedSlotForType(int eventTypeId)
{
    if (!_pendingCoalescedByType.TryGetValue(
            eventTypeId,
            out LinkedList<CoalescedSlotKey>? order) ||
        order.First == null)
    {
        return false;
    }

    return RemovePendingCoalescedSlot(
        order.First.Value,
        releasePayload: true,
        out _);
}
```

全局上限才允许使用：

```csharp
_pendingCoalesced.First
```

## 测试

至少包含：

1. A 类型超限不能删除 B 类型。
2. A 类型 `DropOldest` 删除 A 最早 Key。
3. Snapshot 后所有全局节点和类型节点被清理。
4. 连续 10,000 次插入、淘汰后 Pending 数量不增长。
5. Dispose 不重复释放 Payload。

提交：

```powershell
git add LayerBase/Event/PostScheduler `
        LayerBase.Test/PostSchedulerCoalescedEvictionTests.cs

git commit -m "fix(post): make coalesced eviction type-correct and constant-time"
```

---

# Task 6：将 Scope Fault 移入可靠 Completion 通道

当前非 Main Scope 将 Fault 作为 Critical Event 投递到有界 EventInbox，并忽略失败结果。 现有测试也仍以 EventInbox 中出现 Fault Event 为验收。

## 文件

* 修改：`LayerBase/Scope/ScopeCompletionInbox.cs`
* 修改：`LayerBase/Scope/ScopeRuntime.cs`
* 修改：`LayerBase.Test/ScopeFaultPropagationTests.cs`
* 修改：`LayerBase.Test/WorkerCompletionInboxTests.cs`

## Envelope

```csharp
internal enum ScopeCompletionKind : byte
{
    WorkerExecutionCompleted = 0,
    WorkerCancelRequested = 1,
    WorkerExecutionStarted = 2,
    ScopeFault = 3
}
```

增加：

```csharp
public ScopeFaultRecord FaultRecord { get; }
```

以及工厂：

```csharp
public static ScopeCompletionEnvelope ScopeFault(
    in ScopeFaultRecord record)
{
    var emptyCompletion =
        default(WorkerExecutionCompletedScopeEvent);

    return new ScopeCompletionEnvelope(
        ScopeCompletionKind.ScopeFault,
        in emptyCompletion,
        WorkerHandle.Invalid,
        in record);
}
```

## Fault 投递

非 Main Scope：

```csharp
ScopeCompletionEnvelope envelope =
    ScopeCompletionEnvelope.ScopeFault(in record);

mainEndpoint.Transport.EnqueueCompletion(in envelope);
```

Main Scope Drain：

```csharp
case ScopeCompletionKind.ScopeFault:
    _runtime.ReportScopeFault(envelope.FaultRecord);
    break;
```

Source Scope 仍然在本地执行 `ApplyFaultPolicy()`，Main Scope 只负责统一报告，不重复执行策略。

## 测试修改

原测试：

```csharp
Assert.That(
    host.MainScope.Transport.EventInbox.TryDequeue(out var envelope),
    Is.True);
```

改为检查：

```csharp
Assert.That(
    host.MainScope.Transport.CompletionInbox.Count,
    Is.EqualTo(1));
```

再调用：

```csharp
host.MainScope.PumpIngress();
```

确认 `runtime.Faulted` 被调用。

新增关键测试：

```text
填满 Main EventInbox
→ Worker/Inline Scope 报错
→ Fault 仍进入 CompletionInbox
→ Main Pump 后 Faulted 回调被调用
```

提交：

```powershell
git add LayerBase/Scope `
        LayerBase.Test/ScopeFaultPropagationTests.cs `
        LayerBase.Test/WorkerCompletionInboxTests.cs

git commit -m "fix(scope): route fault records through reliable completion inbox"
```

---

# Task 7：修复 LBTask.Delay 注册竞态和 Lease 泄漏

当前 Delay 的执行顺序为先 Schedule，再 RegisterCancellation。 对于极短 Delay，Timer 可能在注册初始化标志设置之前完成并将 WorkItem 返回池。

此外 Cancellation 注册所使用的 Lease 只在 Cancellation Callback 真正运行时归还；正常到期时可能无法归还。

## 文件

* 修改：`LayerBase.Task/LBTask.cs`
* 新增：`LayerBase.Test/LBTaskDelayRaceTests.cs`

## 状态初始化

`DelayWorkItem.Rent()`：

```csharp
work._registrationInitializing =
    token.CanBeCanceled ? 1 : 0;
```

不得等进入 `RegisterCancellation()` 后才设置为 1。

新增字段：

```csharp
private DelayWorkItemLease? _cancellationLease;
```

新增：

```csharp
private void ReturnCancellationLease()
{
    Interlocked.Exchange(
        ref _cancellationLease,
        null)?.Return();
}
```

注册时：

```csharp
DelayWorkItemLease lease =
    DelayWorkItemLease.Rent(
        this,
        Volatile.Read(ref _leaseVersion));

_cancellationLease = lease;

CancellationTokenRegistration registration =
    _token.Register(
        static state =>
        {
            var callbackLease =
                (DelayWorkItemLease)state!;

            try
            {
                callbackLease.Work.TryCancel(
                    callbackLease.LeaseVersion);
            }
            finally
            {
                callbackLease.Return();
            }
        },
        lease);
```

正常完成：

```csharp
CancellationRegistration.Dispose();
CancellationRegistration = default;
ReturnCancellationLease();
```

`ReturnToPool()` 必须清空引用：

```csharp
_cancellationLease = null;
```

## 测试

至少：

1. 50,000 次 1ms 可取消 Delay 正常完成。
2. Schedule 后立即 Cancel。
3. Cancel 与 Timer 同时竞争。
4. WorkItem 复用后旧 Lease 不得完成新任务。
5. 全部完成后 `DelayHeapPendingCount == 0`。
6. 不出现重复完成和版本失效异常。

提交：

```powershell
git add LayerBase.Task/LBTask.cs `
        LayerBase.Test/LBTaskDelayRaceTests.cs

git commit -m "fix(task): close delay registration and lease lifetime races"
```

---

# Task 8：明确 Timer FireAllCapped 语义

当前重复 Timer 在遗漏周期时会重新加入 Overdue，但通常只能在下一次 Tick 再次触发。

本轮明确语义：

```text
SkipMissed:
    本 Tick 最多触发一次
    下一到期时间 = currentTick + interval

FireAllCapped:
    同一 Tick 内允许补发多个遗漏周期
    总触发数受 MaxExpiredPerTick 限制
    未补完部分继续保留在 Overdue
```

## 文件

* 修改：`LayerBase/Event/TimeScheduler/TimeScheduler.cs`
* 修改：`LayerBase.Test/TimerFairnessTests.cs`
* 新增：`LayerBase.Test/TimerCatchUpPolicyTests.cs`

## 测试

```csharp
[Test]
public void Fire_all_capped_replays_missed_fixed_rate_intervals()
```

构造：

```text
interval = 1 tick
落后 = 10 ticks
MaxExpiredPerTick = 4
```

断言：

```text
第一次 Tick：4 次
第二次 Tick：4 次
第三次 Tick：剩余次数
```

同时验证 `SkipMissed` 只触发一次。

必须保留旧 Overdue 公平性测试。

提交：

```powershell
git add LayerBase/Event/TimeScheduler `
        LayerBase.Test/TimerFairnessTests.cs `
        LayerBase.Test/TimerCatchUpPolicyTests.cs

git commit -m "fix(timer): implement bounded fixed-rate catch-up semantics"
```

---

# Task 9：加固 CI、NuGet 和依赖审计

当前 Workflow 同时声明 .NET 8 和 .NET 9，但测试项目只目标 `net9.0`。  当前脚本也没有执行 Pack 和依赖漏洞审计。

## 文件

* 修改：`.github/workflows/production-hardening.yml`
* 修改：`eng/verify-production-hardening.ps1`
* 可新增：`eng/verify-package.ps1`

## Workflow 划分

```text
library-build-net8:
    安装 .NET 8
    构建 LayerBase net8.0
    构建 LayerBase netstandard2.1

release-tests-net9:
    安装 .NET 9
    Release 全量测试
    ProductionHardening
    ProductionSoak

package:
    依赖 release-tests-net9
    dotnet pack
    漏洞审计
    上传 nupkg
```

增加：

```yaml
permissions:
  contents: read

concurrency:
  group: production-hardening-${{ github.ref }}
  cancel-in-progress: true
```

每个 Job 增加：

```yaml
timeout-minutes: 30
```

Actions 必须解析并固定到完整 SHA：

```powershell
gh api repos/actions/checkout/git/ref/tags/v4 --jq ".object.sha"
gh api repos/actions/setup-dotnet/git/ref/tags/v4 --jq ".object.sha"
gh api repos/actions/upload-artifact/git/ref/tags/v4 --jq ".object.sha"
```

将查询结果写入 YAML 注释，例如：

```yaml
- uses: actions/checkout@<full-sha> # v4
```

## 验证脚本追加

```powershell
dotnet list LayerBase/LayerBase.csproj `
  package `
  --vulnerable `
  --include-transitive

dotnet pack LayerBase/LayerBase.csproj `
  -c Release `
  --no-build `
  -o artifacts/packages `
  /p:ContinuousIntegrationBuild=true `
  /p:Deterministic=true

$packages = Get-ChildItem artifacts/packages -Filter *.nupkg

if ($packages.Count -eq 0) {
    throw "No NuGet package was produced."
}
```

## 验收

* .NET 8 Job 不依赖预装 .NET 9。
* .NET 9 测试独立运行。
* CI 生成的 nupkg 来自同一 SHA。
* 漏洞审计失败会阻止发布。
* Workflow Token 只有只读权限。

提交：

```powershell
git add .github/workflows/production-hardening.yml `
        eng/verify-production-hardening.ps1 `
        eng/verify-package.ps1

git commit -m "ci: harden release tests package provenance and dependency audit"
```

---

# Task 10：最终验证

## 定向门禁

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --filter "TestCategory=ProductionHardening"

dotnet test LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --filter "TestCategory=ProductionSoak"
```

## 全量

```powershell
dotnet clean
dotnet restore
dotnet build -c Release --no-restore
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release --no-build
```

## 重复并发测试

以下测试至少重复 20 次：

```powershell
1..20 | ForEach-Object {
    dotnet test LayerBase.Test/LayerBase.Test.csproj `
      -c Release `
      --no-build `
      --filter "FullyQualifiedName~WorkerCoordinatorRaceTests|FullyQualifiedName~LBTaskDelayRaceTests|FullyQualifiedName~ScopeShutdownStateTests"

    if ($LASTEXITCODE -ne 0) {
        throw "Concurrency test iteration $_ failed."
    }
}
```

## 包验证

```powershell
./eng/verify-production-hardening.ps1
./eng/verify-package.ps1
git status --short
```

预期：

```text
全部测试通过
没有未跟踪 TestResults / trx
没有工作区修改
生成至少一个 nupkg
无已知 vulnerable transitive package
```

---

# Agent 审查流程

每个任务完成后执行两轮独立审查。

## 第一轮：规范审查 Agent

只检查：

* 是否实现本任务全部要求；
* 是否遗漏失败测试；
* 是否违反 Owner Thread 单写原则；
* 是否修改了任务范围外的文件；
* 是否引入公开 API。

输出只允许：

```text
PASS
```

或：

```text
FAIL
- 文件:行号
- 违反的要求
- 必须修正的具体行为
```

## 第二轮：代码质量 Agent

检查：

* 并发竞态；
* Payload、CTS、Lease、WaitHandle 是否恰好释放一次；
* Timeout 路径；
* 异常是否被观察；
* 热路径是否新增分配；
* 测试是否真的能在旧实现上失败；
* 是否用 Sleep 掩盖竞态；
* 是否存在无界集合。

两轮都通过后才能进入下一任务。

---

# 最终 PR 门禁

PR 不满足以下全部条件，不允许合并：

```text
[ ] 基于真实最新 origin/master
[ ] 每个任务独立提交
[ ] Worker Running 状态可观测且计数归零
[ ] Shutdown 不丢失控制异常
[ ] Worker 超时后退出可自动回收资源
[ ] Inline Scope 跨帧公平轮转
[ ] Inline Post 计入共享 Work Budget
[ ] Coalesced 不会淘汰错误事件类型
[ ] Coalesced 插入和单类型淘汰不再扫描整个 Dictionary
[ ] Fault 不依赖有界 EventInbox
[ ] Delay 注册、取消、到期竞争测试通过
[ ] FireAllCapped 与 SkipMissed 行为有明确测试
[ ] .NET 8 Library Build 通过
[ ] .NET 9 Release Test 通过
[ ] ProductionHardening 通过
[ ] ProductionSoak 通过
[ ] NuGet Pack 通过
[ ] 依赖漏洞审计通过
[ ] 最新 Commit SHA 有 CI 绿灯
```

推荐采用"总控 Agent + 每任务新实现 Agent + 两阶段审查"的执行方式，不要让单个 Agent 一次性修改所有并发子系统。
