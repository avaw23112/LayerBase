# 任务 01：ScopeWorker 信号阻塞与绝对 Tick Deadline

> **Agent 执行要求：** 使用 TDD。每个步骤先写失败测试，再写最小实现，再运行回归。完成后只提交本任务涉及的文件。

## 目标

保留当前“一 Worker Scope 一 OS 线程”和固定 OwnerThread 语义，把 `ScopeWorker` 的 `Thread.Sleep` 轮询改成：

```text
有立即工作
    → 直接处理，不阻塞

Tick 已到期
    → 直接执行 Tick，不阻塞

无立即工作且 Tick 未到期
    → AutoResetEvent.WaitOne(距离 Deadline 的时间)
```

Call、Event、控制 Call 和可立即运行的异步 continuation 到达时，必须唤醒对应 Worker。

## 本任务不做

- 不实现线程池、用户态调度、工作窃取。
- 不实现 NUMA、CPU affinity、Processor Group。
- 不改变 Scope 创建顺序。
- 不让 Worker 创建 Scope。
- 不改变 MainScope、InlineScope 的 Pump 模式。
- 不加入自旋、保温时间或 `Thread.Yield`。
- 不增加第三方依赖。

---

## 一、修改文件

### 生产代码

```text
LayerBase/Scope/ScopeRuntimeModel.cs
LayerBase/Scope/ScopeRuntime.cs
LayerBase/Scope/ScopeWorker.cs
LayerBase.Task/LayerBaseSynchronizationContext.cs
LayerBase.Task/MainThreadCompletionQueue.cs
```

### 新增测试

```text
LayerBase.Test/ScopeWorkerBlockingTests.cs
```

### 必须回归

```text
LayerBase.Test/ScopeLifecycleMigrationTests.cs
现有 Scope Call/Event 测试
现有 LayerBaseSynchronizationContext 测试
整个 LayerBase.Test 项目
```

---

## 二、ScopeOptions 配置

所有 Tick 行为必须来自 `ScopeOptions`，不得向 `ScopeWorker` 构造函数增加独立 Tick 参数。

在 `ScopeRuntimeModel.cs` 新增：

```csharp
internal enum ScopeTickOverrunPolicy : byte
{
    // Tick 超期后只执行当前允许的一次 Tick，
    // 随后把 Deadline 推进到第一个未来时间点。
    Skip = 0,

    // Tick 超期后最多连续补指定次数。
    // 超过限制后把 Deadline 推进到第一个未来时间点，
    // 避免死亡螺旋。
    CatchUpLimited = 1
}
```

```csharp
internal readonly struct ScopeTickOptions
{
    public ScopeTickOptions(
        int rateHz,
        ScopeTickOverrunPolicy overrunPolicy,
        int maxCatchUpTicks)
    {
        if (rateHz < 0)
            throw new ArgumentOutOfRangeException(nameof(rateHz));

        if (overrunPolicy == ScopeTickOverrunPolicy.CatchUpLimited &&
            maxCatchUpTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCatchUpTicks));
        }

        if (overrunPolicy == ScopeTickOverrunPolicy.Skip &&
            maxCatchUpTicks != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCatchUpTicks));
        }

        RateHz = rateHz;
        OverrunPolicy = overrunPolicy;
        MaxCatchUpTicks = maxCatchUpTicks;
    }

    // 0 表示没有固定 Tick。
    public int RateHz { get; }

    public ScopeTickOverrunPolicy OverrunPolicy { get; }

    // 仅 CatchUpLimited 使用。
    public int MaxCatchUpTicks { get; }

    public bool IsEnabled => RateHz > 0;

    public static ScopeTickOptions None { get; } =
        new(
            0,
            ScopeTickOverrunPolicy.Skip,
            0);
}
```

`ScopeOptions` 修改为持有：

```csharp
public ScopeTickOptions Tick { get; }

// 旧调用点临时兼容，新代码不得继续读取它。
public int TickRateHz => Tick.RateHz;
```

`Worker` 工厂默认：

```csharp
public static ScopeOptions Worker(
    int tickRateHz = 60,
    ScopeTickOverrunPolicy overrunPolicy =
        ScopeTickOverrunPolicy.CatchUpLimited,
    int maxCatchUpTicks = 2)
```

默认值：

```text
OverrunPolicy = CatchUpLimited
MaxCatchUpTicks = 2
```

### 配置测试

必须覆盖：

```text
Worker(60) 默认是 CatchUpLimited + 2
Skip 拒绝非 0 MaxCatchUpTicks
CatchUpLimited 拒绝小于等于 0 的 MaxCatchUpTicks
FixedRate 拒绝 RateHz == 0
Main/Inline 使用 ScopeTickOptions.None
```

---

## 三、唤醒来源

### 3.1 ScopeRuntime 绑定 Worker 信号

在 `ScopeRuntime` 增加：

```csharp
// 仅 Worker Scope 使用。
// 在 ScopeWorker 构造阶段绑定一次，之后不允许替换。
private Action? _signalWorker;
```

```csharp
internal void BindWorkerWakeSignal(Action signalWorker)
{
    if (signalWorker == null)
        throw new ArgumentNullException(nameof(signalWorker));

    if (Options.Threading != ScopeThreadingMode.Worker)
    {
        throw new InvalidOperationException(
            "Only Worker Scope may bind a wake signal.");
    }

    if (_signalWorker != null)
    {
        throw new InvalidOperationException(
            "Worker wake signal is already bound.");
    }

    _signalWorker = signalWorker;
}
```

修改现有 `SignalIngress`：

```csharp
private void SignalIngress()
{
    Volatile.Write(ref _hasIngress, 1);

    // 只负责唤醒，不允许在生产者线程直接 Pump Scope。
    _signalWorker?.Invoke();
}
```

这样以下输入都会唤醒：

```text
EventInbox accepted
CallInbox accepted
Control Call accepted
Dispose / Stop 控制 Call accepted
```

### 3.2 SynchronizationContext 唤醒

`LayerBaseSynchronizationContext` 增加可选回调：

```csharp
private readonly Action? _onWorkAvailable;
```

安装接口改为：

```csharp
public static LayerBaseSynchronizationContext Install(
    Action? onWorkAvailable = null)
```

以下位置接受“可立即执行”工作后调用：

```csharp
_onWorkAvailable?.Invoke();
```

需要调用的位置：

```text
Post
ScheduleInFrames(frames <= 0)
EnqueueReadyFrameWork
MainThreadCompletionQueue.Enqueue
```

`frames > 0` 的 FrameDelay 不能立即唤醒，也不能被视为立即工作；它只在下一次物理 Tick 时推进。

### 3.3 区分 ReadyWork 和 FrameWork

当前 `HasPendingWork` 同时包含 ready queue 与 frame delay。阻塞模式必须拆分：

```csharp
public bool HasReadyWork =>
    CompletionQueue.HasPending ||
    Volatile.Read(ref _hasQueuedWork) != 0;

public bool HasPendingWork =>
    HasReadyWork ||
    Volatile.Read(ref _hasFrameWork) != 0;
```

`ScopeRuntime.HasImmediateWork` 只能读取 `HasReadyWork`：

```csharp
internal bool HasImmediateWork
{
    get
    {
        if (Volatile.Read(ref _hasIngress) != 0)
            return true;

        LayerBaseSynchronizationContext? context =
            SynchronizationContext;

        if (context != null && context.HasReadyWork)
            return true;

        PostScheduler? scheduler = PostScheduler;

        return scheduler != null &&
               scheduler.HasPendingWork;
    }
}
```

禁止用 `context.HasPendingWork`，否则仅存在 FrameDelay 时 Worker 会永远忙循环。

### 3.4 CompletionQueue 唤醒

`MainThreadCompletionQueue` 增加：

```csharp
private readonly Action? _onWorkAvailable;

internal MainThreadCompletionQueue(
    Action? onWorkAvailable = null)
{
    _onWorkAvailable = onWorkAvailable;
}
```

两个 `Enqueue` 都必须按顺序执行：

```text
1. Enqueue item
2. 发布 _hasItems = 1
3. 调用 _onWorkAvailable
```

---

## 四、拆分立即工作与物理 Tick

不能在 Event 唤醒时继续调用当前完整 `PumpScopeResources`，否则会提前执行 `IUpdate`、Timer 和 Delay。

在 `ScopeRuntime` 增加：

```csharp
internal void PumpWorkerImmediateWork(
    CompletionExceptionPolicy exceptionPolicy =
        CompletionExceptionPolicy.Throw,
    Action<Exception>? reportException = null);
```

它只执行：

```text
PumpIngress
PumpSynchronizationContext
PostScheduler.Pump
PumpEventExpectations
```

它不得执行：

```text
_tickCount++
TickTimer
DelayManager.Tick
PumpUpdate
```

增加：

```csharp
internal void PumpWorkerScheduledTick(
    float fixedDeltaTime,
    CompletionExceptionPolicy exceptionPolicy =
        CompletionExceptionPolicy.Throw,
    Action<Exception>? reportException = null);
```

它执行：

```text
_tickCount++
PumpIngress
PumpSynchronizationContext
TickTimer(fixedDeltaTime)
DelayManager.Tick(fixedDeltaTime)
PostScheduler.Pump
PumpEventExpectations
PumpUpdate(fixedDeltaTime)
```

两条入口都必须：

```text
验证 OwnerThread
进入当前 Scope 的 SynchronizationContext
尊重 Stop / Dispose / SafePoint 状态
```

保留现有 `PumpScopeResources` 给 Main/Inline 和旧调用点，不改变其公开行为。

### 语义测试

必须证明：

```text
1 Hz Worker 完成第一次 Tick
100ms 后 Event 到达并快速执行
Event 执行期间 UpdateCount 仍保持 1
Timer/Delay 不因 Event 唤醒而提前推进
```

---

## 五、ScopeWorker 阻塞循环

### 5.1 字段

在 `ScopeWorker` 增加：

```csharp
private readonly AutoResetEvent _workSignal =
    new(initialState: false);

private bool _disposed;
```

构造阶段绑定一次：

```csharp
_runtime.BindWorkerWakeSignal(_workSignal.Set);
```

不得在循环中重复创建委托。

### 5.2 删除旧轮询

删除：

```csharp
private float GetDeltaTime()
private void Sleep()
```

`ScopeWorker.cs` 中不得再存在：

```text
Thread.Sleep
SpinWait
Thread.Yield
```

### 5.3 时间计算

使用绝对 `Stopwatch` 时间轴：

```csharp
private static long CalculateIntervalTimestampTicks(
    int tickRateHz)
{
    if (tickRateHz <= 0)
        return long.MaxValue;

    return Math.Max(
        1L,
        Stopwatch.Frequency / tickRateHz);
}
```

第一帧保持现有语义，立即到期：

```csharp
long nextTickDeadline =
    Stopwatch.GetTimestamp();
```

后续始终：

```csharp
nextTickDeadline += intervalTimestampTicks;
```

禁止写成：

```csharp
nextTickDeadline =
    Stopwatch.GetTimestamp() +
    intervalTimestampTicks;
```

否则会累计漂移。

### 5.4 Worker 主循环

实现等价逻辑：

```csharp
private void Run()
{
    _started.Set();

    SynchronizationContext? previousContext =
        SynchronizationContext.Current;

    try
    {
        _runtime.InstallSynchronizationContext();

        SynchronizationContext.SetSynchronizationContext(
            _runtime.SynchronizationContext);

        ScopeTickOptions tick =
            _runtime.Options.Tick;

        long intervalTimestampTicks =
            CalculateIntervalTimestampTicks(
                tick.RateHz);

        long nextTickDeadline =
            Stopwatch.GetTimestamp();

        float fixedDeltaTime =
            tick.RateHz > 0
                ? 1f / tick.RateHz
                : 0f;

        while (_runtime.State !=
               ScopeRuntimeState.Disposed)
        {
            try
            {
                _runtime.PumpWorkerImmediateWork();

                if (_runtime.State ==
                    ScopeRuntimeState.Disposed)
                {
                    break;
                }

                long now =
                    Stopwatch.GetTimestamp();

                if (now >= nextTickDeadline)
                {
                    PumpDueTicks(
                        in tick,
                        intervalTimestampTicks,
                        fixedDeltaTime,
                        ref nextTickDeadline);

                    continue;
                }

                if (_runtime.HasImmediateWork)
                    continue;

                int waitMilliseconds =
                    CalculateWaitMilliseconds(
                        now,
                        nextTickDeadline);

                _workSignal.WaitOne(
                    waitMilliseconds);
            }
            catch (Exception ex)
            {
                _runtime.ReportFault(
                    ex,
                    ScopeFaultPhase.WorkerLoop);
            }
        }
    }
    finally
    {
        try
        {
            if (_runtime.State !=
                ScopeRuntimeState.Disposed)
            {
                _runtime.RunRuntimeStop();
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(
                previousContext);
        }
    }
}
```

核心硬约束：

```text
HasImmediateWork == true
    → 不 Wait

now >= nextTickDeadline
    → 不 Wait

只有无立即工作且 Deadline 在未来
    → WaitOne
```

### 5.5 Wait 超时换算

必须向上取整，避免主动早醒；使用整数计算，避免热循环浮点运算：

```csharp
private static int CalculateWaitMilliseconds(
    long now,
    long deadline)
{
    long remaining =
        deadline - now;

    if (remaining <= 0)
        return 0;

    long numerator;

    try
    {
        numerator = checked(
            remaining * 1000L +
            Stopwatch.Frequency -
            1L);
    }
    catch (OverflowException)
    {
        return int.MaxValue;
    }

    long milliseconds =
        numerator / Stopwatch.Frequency;

    if (milliseconds <= 0)
        return 1;

    return milliseconds >= int.MaxValue
        ? int.MaxValue
        : (int)milliseconds;
}
```

Wait 返回后必须回主循环重新读取物理时间，不得假设“超时返回等于精确到达 Deadline”。

### 5.6 Tick 超期策略

```csharp
private void PumpDueTicks(
    in ScopeTickOptions tick,
    long intervalTimestampTicks,
    float fixedDeltaTime,
    ref long nextTickDeadline)
{
    int executionLimit =
        tick.OverrunPolicy ==
        ScopeTickOverrunPolicy.CatchUpLimited
            ? tick.MaxCatchUpTicks
            : 1;

    int executed = 0;
    long now = Stopwatch.GetTimestamp();

    while (now >= nextTickDeadline &&
           executed < executionLimit &&
           _runtime.State !=
               ScopeRuntimeState.Disposed)
    {
        _runtime.PumpWorkerScheduledTick(
            fixedDeltaTime);

        nextTickDeadline +=
            intervalTimestampTicks;

        executed++;
        now = Stopwatch.GetTimestamp();
    }

    if (now < nextTickDeadline)
        return;

    long overdue =
        now - nextTickDeadline;

    long skippedIntervals =
        overdue /
        intervalTimestampTicks + 1L;

    nextTickDeadline +=
        skippedIntervals *
        intervalTimestampTicks;
}
```

必须保证：

```text
任务执行完成后已经超过 Deadline
    → 立即执行允许的 Tick
    → 不额外 Wait

超过 MaxCatchUpTicks 后仍落后
    → Deadline 跳到首个未来点
    → 不无限补 Tick
```

### 5.7 Dispose

`ScopeWorker.Dispose()` 顺序：

```text
1. 幂等检查
2. RequestDisposeAsync
3. _workSignal.Set()，确保阻塞线程被唤醒
4. Join Worker
5. 未启动时沿用 ScopeRuntime.Dispose
6. Dispose AutoResetEvent
```

不得在 Worker 自己的线程中 Join 自己。

---

## 六、必须新增的测试

测试类：

```csharp
[TestFixture]
[NonParallelizable]
public sealed class ScopeWorkerBlockingTests
```

### 测试 1：Event 提前唤醒

```text
Worker = 1 Hz
等待第一次 Tick 完成
立即向它投递 Event
Event Handler 必须在 250ms 内执行
不能等待下一次 1 秒 Tick
```

### 测试 2：控制 Call 唤醒 Dispose

```text
Worker = 1 Hz
第一次 Tick 后立刻 Dispose Host
Dispose 必须在 250ms 内完成
```

### 测试 3：SynchronizationContext.Post 唤醒

```text
Worker = 1 Hz
捕获 Worker Context
第一次 Tick 后从测试线程 Context.Post
回调必须在 250ms 内执行
回调线程 ID 必须等于 Worker OwnerThreadId
```

### 测试 4：Event 不提前执行 Update

```text
Worker = 1 Hz
第一次 Tick 后 UpdateCount == 1
100ms 后发送 Event
Event 快速执行
下一 Tick Deadline 前 UpdateCount 仍为 1
```

### 测试 5：Tick 超期后不等待

```text
Worker = 20 Hz，周期约 50ms
第一次 Update 人为执行约 140ms
记录第一次 Update 结束时间
记录第二次 Update 开始时间
两者间隔必须小于 25ms
证明没有再等待一个 50ms 周期
```

### 测试 6：有限补 Tick

```text
Worker = 100 Hz
MaxCatchUpTicks = 2
第一次 Update 人为执行约 120ms
后续不能瞬间补完全部错过 Tick
在限定时间窗内 UpdateCount 必须符合“最多补 2 次后跳到未来”
```

### 测试 7：FrameDelay 不造成忙循环

```text
Context 中只有 frames > 0 的 FrameDelay
HasPendingWork == true
HasReadyWork == false
Worker 不应因为它持续立即循环
下一物理 Tick 到达后才推进 FrameDelay
```

### 测试 8：OwnerThread 不变化

经历：

```text
Tick
阻塞
Event 唤醒
再次阻塞
Continuation 唤醒
Dispose
```

以下线程 ID 必须相同：

```text
Update
Event Handler
Continuation
RuntimeStop
```

---

## 七、执行命令

先运行目标测试：

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
  --filter FullyQualifiedName~ScopeWorkerBlockingTests
```

连续运行 20 次，排查时序竞争。

PowerShell：

```powershell
1..20 | ForEach-Object {
    dotnet test LayerBase.Test/LayerBase.Test.csproj `
      --filter FullyQualifiedName~ScopeWorkerBlockingTests

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
```

回归 OwnerThread 和 SynchronizationContext：

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
  --filter FullyQualifiedName~ScopeLifecycleMigrationTests
```

运行完整测试：

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj
```

检查禁止项：

```bash
git grep -n "Thread.Sleep\|SpinWait\|Thread.Yield" \
  -- LayerBase/Scope/ScopeWorker.cs
```

预期：无匹配。

---

## 八、验收标准

全部满足才算完成：

- `ScopeWorker` 不再使用 `Thread.Sleep`。
- 使用一个 `AutoResetEvent` 阻塞。
- 没有自旋和忙等。
- Call、Event、控制 Call 可以唤醒 Worker。
- Ready continuation 可以唤醒 Worker。
- FrameDelay 不被视为立即工作。
- Event 唤醒不会提前执行 `IUpdate`、Timer、Delay。
- 第一次 Worker Tick 仍立即执行。
- 后续 Tick 使用绝对 `Stopwatch` Deadline。
- Tick 已超期时绝不 Wait。
- 补 Tick 次数受 `ScopeOptions.Tick.MaxCatchUpTicks` 限制。
- 超过补 Tick 上限后 Deadline 跳到第一个未来时间点。
- Scope OwnerThread 在所有阻塞/唤醒后保持不变。
- Dispose 可以唤醒并 Join 阻塞 Worker。
- Main/Inline 行为不变。
- 新测试连续运行 20 次通过。
- 全部旧测试通过。
- 没有实现 NUMA、线程池或 Scope 创建顺序变更。

## 九、提交建议

建议拆成四个可审查提交：

```bash
git commit -m "refactor(scope): move worker tick policy into scope options"
git commit -m "feat(scope): signal worker when runnable work arrives"
git commit -m "refactor(scope): split immediate work from fixed tick"
git commit -m "perf(scope): block worker until work or tick deadline"
```

## 十、工作量

```text
ScopeOptions Tick 配置迁移       0.5 天
唤醒来源接入                    0.5～1 天
立即工作与 Tick Pump 拆分       0.5～1 天
Worker 阻塞循环                 0.5～1 天
并发与时序测试                  1～2 天

总计                            3～5 工程日
```

最大风险：

```text
FrameDelay 被误判为 ReadyWork 导致忙循环
Event 唤醒时意外提前执行 Update
CompletionQueue 没有触发唤醒
Dispose 时阻塞线程没有被唤醒
长 Tick 后进入无限补帧死亡螺旋
```
