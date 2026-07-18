# LayerBase 生产并发收敛详细实施计划

> **保存路径：**`docs/superpowers/plans/2026-07-18-scope-event-call-production-hardening.md`
>
> **执行要求：**使用独立 Worktree，严格按照任务编号执行。每个任务先写失败测试，再实现，再运行定向测试和全量测试，再提交。禁止跨任务提前重构。

## 目标

将 LayerBase 的跨线程状态变化统一为：

```text
跨线程请求  → ScopeCall
跨线程通知  → ScopeEvent
状态写入    → Owner Scope Thread
Worker线程  → 仅执行不可变 WorkItem
```

同时保留以下简洁公共接口：

```csharp
service.WorkerJobs().Run<TJob, TInput, TEvent>(
    in job,
    in input,
    options,
    cancellationToken);

runtime.Post(in value);

runtime.CallAsync<TRequest, TResponse>(
    request,
    cancellationToken);
```

调用者不得接触：

```text
ScopeEventEnvelope
ScopeCallEnvelope
ScopeTransport
ScopeEndpoint
PayloadHandle
RouteId
WorkerJobCoordinator
WorkerExecutionCompletedScopeEvent
WorkerCancelRequestedScopeEvent
```

---

# Task 0：锁定公共 API

创建 `LayerBase.Test/PublicApiStabilityTests.cs`：

- `Worker_job_public_api_remains_a_simple_accessor`：`WorkerJobAccessor.Run` 保持 3 泛型参数，参数不得泄漏 `ScopeRuntime/ScopeEndpoint/ScopeTransport/ScopeEventEnvelope/ScopeCallEnvelope/PayloadHandle`。
- `Internal_concurrency_protocol_does_not_become_public`：`WorkerJobCoordinator/WorkerExecutionCompletedScopeEvent/WorkerCancelRequestedScopeEvent/ShutdownDeadline` 必须保持 internal。
- `Scope_ref_does_not_expose_transport_protocol`：`ScopeRef<MainScope>` 公共成员不得包含 `Transport/Endpoint/EnqueueControlCall/EnqueueEventEnvelope/EnqueueCallEnvelope`。

此门禁测试在后续所有任务中必须持续通过。

Commit: `test(api): lock public concurrency facade`

---

# Task 1：将 Worker Job 状态移动到 Origin Scope

## 架构选择

不建立全局 MainScope Coordinator。每个 `ScopeRuntime` 拥有自己的 `WorkerJobCoordinator`：

1. `WorkerJobAccessor.Run()` 在 Service 所属 Scope 的 Owner Thread 调用。
2. Handle 同步返回，不需要把 `Run()` 改成异步 ScopeCall。
3. Worker Completion 通过 ScopeEvent 返回 Origin Scope。
4. CancellationToken 回调通过 ScopeEvent 返回同一个 Origin Scope。
5. 每个 Scope 独立拥有 Handle、CTS 和状态表。

共享的 `WorkerJobScheduler` 改成纯执行器：只拥有线程与 WorkItem Queue，不拥有 Handle/CTS/WorkerState。

## 文件变化

创建：`LayerBase/Worker/WorkerJobCoordinator.cs`、`LayerBase/Worker/WorkerExecutionItem.cs`

修改：`WorkerJobScheduler.cs`、`WorkerJobAccessor.cs`、`WorkerScopeEvents.cs`、`DI/ServiceExtensions.cs`、`Application/LayerRuntime.cs`、`Scope/ScopeRuntime.cs`

## 关键约束

- `WorkerScopeEventRouteIds`: ExecutionCompleted = -301, CancelRequested = -302。
- `WorkerExecutionCompletedScopeEvent { Handle, Kind, Result, Options, Error }`（Kind: Succeeded/Cancelled/Faulted）。
- `IWorkerExecutionResult.PostTo(PostScheduler, EventPostPolicy?)` 按 ResultPostPolicy 分发（Normal/Latest/Coalesced/DirtySignal）。
- `WorkerJobCoordinator` 单写者：仅 Owner Scope Thread 写入；跨线程只读 `PublicState/Version/ActiveCount/RunningCount`（Volatile/Interlocked）。禁止 `_gate`。
- 取消回调 static lambda 只通过 `Endpoint.Transport.EnqueueEvent(CancelRequested, Critical, ...)` 通知。
- `HandleCancelRequested` 只设置 CancelRequested 并 `cts.Cancel(throwOnFirstException:false)`；不完成 Slot。
- `HandleExecutionCompleted` 唯一负责终态与 Slot 回收（CompleteSlot：Dispose registration、归还 CTS、释放 free index）。
- `BeginStopOnOwnerThread`：`_accepting=false` 并对所有 InUse Slot 发起取消。
- `DisposeOnOwnerThread`：仍有 Active Job 时抛 InvalidOperationException。
- `WorkerExecutionItem<TJob,TInput,TEvent>`：Rent/Return 池化（上限 64），Execute 构造完成事件（Critical）发回 Origin，被拒绝时 Debug.Fail；CancelBeforeRun 发送 Cancelled 完成事件。禁止出现 WorkerState/StateSlot/CancellationTokenSource/MarkTerminal/ReleaseHandle/ReturnCtsToPool/LBTaskSource/PostScheduler。
- `WorkerJobScheduler` 纯执行器：TryEnqueue/BeginStop/Stop(in ShutdownDeadline)/WorkerLoop。`Stop` 返回 `WorkerExecutorShutdownResult`（Stopped/TimedOut/AlreadyStopped）；TimedOut 时不得 ReleaseResources（活线程仍可能 `_signal.Wait()`）。
- `WorkerJobAccessor` 仅包装 Coordinator；不得增加 ScopeRef/ScopeEndpoint/RouteId/Coordinator/Envelope 公共参数。
- `ServiceExtensions.WorkerJobs()` 绑定 `binding.OwnerScope.WorkerJobs`。
- `ScopeRuntime` 增加 `WorkerJobs` 属性，构造函数创建 Coordinator；事件分派传入 Coordinator；诊断填 `ActiveCount-RunningCount`/`RunningCount`。
- `LayerRuntime`：字段改 `_workerExecutor`，构造顺序 MainActorRuntime → WorkerExecutor → ScopeHost；`WorkerJobs` 转发 `_scopeHost.MainScope.WorkerJobs`。

## 测试

`LayerBase.Test/WorkerCoordinatorRaceTests.cs`：

- `Cancel_does_not_reuse_handle_before_physical_completion`：阻塞 Job + 取消后新句柄不复用旧槽位；释放后 Pump 至 Cancelled。
- `Completion_and_cancel_produce_one_terminal_state`：10,000 次 Run+Cancel+Pump 收敛到唯一终态，最终 ActiveCount == 0。

Commit: `refactor(worker): own job state inside origin scope`

---

# Task 2：生命周期与 Shutdown 使用统一 Deadline

创建 `LayerBase/Scope/ShutdownDeadline.cs`（Stopwatch 时间戳、IsExpired、RemainingMilliseconds、Start(TimeSpan)，溢出饱和）。

- `ScopeRuntime.StopOnOwnerThread`：Stopping → CloseBusinessAdmission → `WorkerJobs.BeginStopOnOwnerThread()` → RunRuntimeStop → Stopped。
- `DisposeAfterControlIfNeeded`：`!WorkerJobs.CanDispose` 时推迟。
- `DisposeOwnerThreadResources`：释放前校验 `WorkerJobs.CanDispose` 并 `WorkerJobs.DisposeOnOwnerThread()`。
- 新增 `ScopeRuntime.DisposeUnstarted()`：只允许 Build 未 StartWorkers、Host 构造失败、替换初始 MainScopeHost 路径调用。
- `ScopeWorker.Stop(in ShutdownDeadline)`：分离 Dispose；不再发送 DisposeCall（Host 统一发送）；超时置 background；资源释放幂等。
- `ScopeRuntimeHost`：`_workersStarted` 标志；`Dispose()` = 15s Deadline → 未启动走 DisposeUnstartedScopes；否则 RequestStopForAllScopes → RequestDisposeForAllScopes（超时 CloseBusinessAdmission + ReportFatalFault）→ StopWorkers（超时 ReportFatalFault）。`WaitForControl` 轮询 awaiter，Inline Scope 边等边 PumpIngress。
- `PumpInlineScopes` 改用宿主内 `_nextInlineScopeIndex` Round-Robin（Task 3 Step 7）。
- 删除 `LayerChain.DisposeLayers()`（RuntimeStop/Dispose 无限等待路径）；构建阶段 Initialize/PostBuild/RuntimeStart 保留。
- `LayerRuntime.Dispose()`：Stopping → `_workerExecutor.BeginStop()` → MainActor RuntimeStop → Disposing → `_scopeHost.Dispose()` → `_chain=null` → MainActor Dispose → Tools Dispose → `_workerExecutor.Stop(deadline)`（TimedOut 上报）→ finally 清理。禁止在 `_scopeHost.Dispose()` 之前调用 `_chain.DisposeLayers()`/`scope.Dispose()`/signal Dispose。

## 测试

`LayerBase.Test/BuildRollbackConcurrencyTests.cs`：

- `Failure_before_workers_start_does_not_wait_for_worker_control_call`（< 2s）。
- `Runtime_dispose_returns_at_deadline_when_worker_scope_is_blocked`（entered/release 屏障，< 17s）。

Commit: `fix(scope): coordinate lifecycle shutdown with one deadline`

---

# Task 3：修复 PostScheduler 数据丢失和预算问题

- 用 `SparseSnapshotCursor { WordListPosition, RemainingBits, Active }` 替换 `_dirtySnapshotWordIndex/_latestSnapshotWordIndex`（稀疏 WordId 不得当作 Snapshot 数组位置）。
- `TakeSpecialSnapshots` 以 `hasWords` Begin 游标。
- `DispatchDirtySnapshotBudgeted`/`DispatchLatestSnapshotBudgeted` 以 WordListPosition + RemainingBits 恢复。
- Flush 清理改为 `_dirtyCursor.Reset(); _latestCursor.Reset();`。
- `Pump(ref RuntimeFrameBudget)`：`effectiveCap <= 0` 时立即返回统计（不 Dispatch）；普通队列预算判断移动到 Dequeue 前。
- `ScopeRuntime.PumpScopeResources(ref budget)`：Pump 后 `budget.Consume(stats.ProcessedCount)`，每 Scope 只 Consume 一次；Main Scope 已在 `PumpCore` Consume，不得重复。
- `ScopeRuntimeHost.PumpInlineScopes` 使用 `_nextInlineScopeIndex` 持久 Round-Robin，删除对 `budget.StartingScopeIndex` 的依赖。

## 测试

- `PostSchedulerSparseCursorRegressionTests`：动态注册占位类型使 EventTypeId 跨至少三个 64-bit Word，maxEventsPerPump=1 多帧泵完不丢失（Latest 与 Dirty）。
- `RuntimePostBudgetRegressionTests`：零预算不 Dispatch；Inline Scope 统一预算；Round-Robin 公平性。

Commit: `fix(post): preserve sparse cursors and shared budget`

---

# Task 4：Coalesced 限流改为 O(1)

- 删除 `List<CoalescedSlotKey> _pendingCoalesced`；新增全局 LinkedList + 节点表、按 Type LinkedList + 节点表。
- `AddCoalescedOrder/RemoveCoalescedSlot/EvictOldestCoalescedOfType/EvictOldestCoalescedGlobal` O(1)。
- 新 Slot 限流：per-type 上限（plan.MaxPending>0 ? MaxPending : MaxSpecialPending）超限时仅 DropOldest 淘汰同类型最旧；全局 `_coalescedBuffer.Count >= MaxSpecialPending` 时 DropOldest 淘汰全局最旧；否则 Failure。
- Snapshot 按全局顺序 Drain（releasePayload:false）。

测试：EventA 上限只淘汰 EventA；EventB 不受影响；全局上限 DropOldest；100,000 keys 无 O(n²)（100k 耗时 ≤ 50k 的 3 倍）。

Commit: `perf(post): bound coalesced queues in constant time`

---

# Task 5：Timer Overdue、Promotion 和 CatchUp

- `ProcessCurrentSlot`：先处理旧 Overdue，再处理当前槽，剩余预算继续处理本 Tick 新入 Overdue（FireAllCapped 重排项）。
- `MoveToOverdue(head, tail)` O(1) 链接（写 SlotIndex 的 while 保留；不重构 TimerEntry 存储布局）。
- `ProcessOverdueQueue` 用 head/tail 交接。
- `PromoteLongTimers`：已到期项直接 `MoveToOverdue(index, index)`，否则 PlaceInWheel。
- `RescheduleRepeatSlow`：FixedRate 按 ExpireTick+Interval；CatchUp（FireAllCapped）允许 nextExpire ≤ currentTick 时进 Overdue；SkipMissed 跳到 `currentTick + Interval`。
- `TimeSchedulerOptions`：`maxCatchUpTicksPerPump <= 0` 抛；`longTimerThresholdSeconds` NaN/Infinity/负数抛。

测试：`Fire_all_capped_and_skip_missed_have_different_results`（Skip=1 次，FireAll>1 次）。

Commit: `fix(timer): preserve overdue fairness and catch-up semantics`

---

# Task 6：DI 引用去重释放

- `ServiceProvider` 增加 `ReferenceComparer`（RuntimeHelpers.GetHashCode）。
- `Dispose()`/`DisposeScope()` 以对象引用 HashSet 去重后仅 Dispose 一次。

测试：同一实例注册为两个接口只 Dispose 一次。

Commit: `fix(di): dispose aliased instances once`

---

# Task 7：LBTask.Delay 池化 Lease 与正确诊断

- `DelayWorkItemLease` 池化（ObjectPool），`Rent/Return` + `_returned` Interlocked 幂等。
- Timer 回调与取消回调 finally 归还 Lease；`RegisterCancellation` 记录 `_cancellationLease` 并在完成/回收路径归还。
- `PendingCount` 加锁读取。
- `s_lockAcquisitions` → `s_lockContentions`：`EnterSchedulerLock(ref lockTaken)` TryEnter 失败才计数；所有 `lock(s_lock)` 改写。
- Peak 采用 CAS 循环 `UpdatePeak(current)`。

Commit: `perf(task): pool delay leases and fix diagnostics`

---

# Task 8：仓库卫生与发布门禁

- `.gitignore` 增加 `**/TestResults/`、`*.trx`；`git rm -r --cached LayerBase.Test/TestResults`。
- 创建 `eng/verify-production-hardening.ps1`：restore → build Release → 全量测试 → `TestCategory=ProductionHardening` → `TestCategory=ProductionSoak` → Generator.Test → `git diff --check` → 工作区不残留 TRX。
- Soak 清单：Worker submit/cancel/complete 100,000；handle version reuse；dispose while executing；Build failure 前 100 次；Worker Scope shutdown timeout；admission close 中的 internal completion；Dirty/Latest 高位 EventTypeId 多帧；Budget zero；Inline Round-Robin 10,000 帧；Coalesced 100,000 keys；Timer 持续新到期下旧 Overdue；FireAllCapped vs SkipMissed；DI 同实例多接口；LBTask delay cancel/timer race 100,000。

Commit: `chore(ci): enforce production hardening gates`

---

# 强制执行顺序

Task 0 → Task 1 → Task 2 → Task 3 → Task 4 → Task 5 → Task 6 → Task 7 → Task 8。
Task 1 与 Task 2 不允许并行（共同修改 ScopeRuntime/LayerRuntime/WorkerJobScheduler/ScopeRuntimeHost）。

# Reviewer 拒绝条件

```text
[ ] Worker Thread 直接写 WorkerState
[ ] Worker Thread 回收 CTS
[ ] CancellationToken 回调直接写 StateSlot
[ ] CancellationToken 回调直接完成 LBTask
[ ] 已启动 Scope 出现直接 scope.Dispose() fallback
[ ] Shutdown 使用多个独立 5 秒等待
[ ] ScopeEvent 内部类型成为 public
[ ] WorkerJobs.Run 增加 Scope/Route/Envelope 参数
[ ] Internal Completion 与 Business Event 使用相同 Admission 限制
[ ] 超时后 Dispose 活线程仍会使用的 signal/CTS/Transport
[ ] Post 零预算仍 Dispatch
[ ] 稀疏 WordId 被当作 Snapshot 数组位置
[ ] Coalesced 每次插入遍历整个 Dictionary
[ ] DI 按 ServiceKey 而非对象引用去重 Dispose
[ ] Release 全量测试存在 Failed
```
