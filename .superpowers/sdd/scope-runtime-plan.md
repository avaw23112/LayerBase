# LayerBase ScopeRuntime 统一所有权与并发安全实施计�?

> **�?Agent 执行�?*必须使用 `superpowers:subagent-driven-development` �?`superpowers:executing-plans` 按任务顺序实施。每个任务都必须先写失败测试，再修改实现，再运行对应测试。禁止跳过测试直接批量重构�?

**目标�?*�?LayerBase 完整收束为“LayerRuntime 负责层级和全局协调，ScopeRuntime 负责全部业务资源、对象、线程与生命周期”的单一运行时模型，并彻底解�?Scope 启停竞态、Promise/Continuation 丢失、ActorWorld 跨线程访问、Publish/From 反射绑定、Module 全局静态状态、DI 热路径反射和 IL2CPP 裁剪风险�?

**架构�?*`LayerRuntime` 只保�?Layer 层级、`ScopeRuntimeHost`、共�?`ActorWorld`、异常中心、模块计划与全局工具。所�?EventCenter、Scheduler、Timer、Delay、ECS、Service、Context、DI、Publish/From、Subscription 和异�?Continuation 均由具体 `ScopeRuntime` 独占。MainScope 也是标准 Scope，不再存�?LayerRuntime 业务资源旁路�?

**技术栈�?*C#�?NET、Roslyn Incremental Generator、NUnit、Arch.Core、Unity IL2CPP/AOT�?

---

# 一、全局强制约束

以下约束适用于所有任务，不得为了兼容旧代码而绕开�?

## 1. 允许破坏性修�?

�?API 不需要保留�?

必须删除�?

```text
LayerRuntime.EventCenter
LayerRuntime.Scheduler
LayerRuntime.Timer
LayerRuntime.EcsWorld
LayerRuntime.GetService<T>
LayerRuntime.Send/Post/Subscribe 等业�?API

Layer.GetService<T>
Layer.RegisterService
Layer �?ServiceProvider
Layer 级业务生命周期管�?
Legacy Scope Planner
Module 构建失败后的 Legacy fallback
```

不得增加 Obsolete 转发层，也不得通过 MainScope Proxy 保留这些�?API�?

唯一允许直接取得 Scope 的入口：

```csharp
LayerRuntime.MainScope
LayerRuntime.Scopes
ScopeRuntimeHost
```

业务代码只能从自身绑定对象取�?OwnerScope，或使用生成�?Scope 路由�?

---

## 2. Scope 是唯一业务所有权�?

以下对象必须唯一属于一�?ScopeRuntime�?

```text
IService
ILayerContext
EventCenter
PostScheduler
Timer
DelayManager
ECS World
ECS Query Registry
ECS Scheduler
Scope ServiceProvider
Publish / From Resource
Subscription
SynchronizationContext
Promise / Continuation
Post / Call Inbox
```

不存在：

```text
没有 Scope �?Service
只属�?LayerRuntime �?Context
Layer 级业�?EventCenter
Layer 级业�?ServiceProvider
```

没有显式自定�?Scope 的服务进�?MainScope�?

---

## 3. LayerRuntime 只保留全局能力

最终结构：

```text
LayerRuntime
├── LayerGraph
├── ScopeRuntimeHost
�?  ├── MainScopeRuntime
�?  ├── CombatScopeRuntime
�?  └── ...
├── ActorWorld
├── ActorLifecycleInbox
├── ActorEventInbox
├── RuntimeExceptionHub
├── ModuleRuntimePlan
├── ScopeRouteTable
└── RuntimeGlobalTools
```

`LayerRuntime` 不拥有业�?EventCenter、Timer、ECS World 或业�?Service�?

---

## 4. ActorWorld 所有权

共享 `ActorWorld` 只能�?`LayerRuntime` Owner Thread�?

```text
Prepare
CompleteBuild
Pump
Disable
Release
Destroy
Dispose
```

Worker Scope 不允许直接调用共�?ActorWorld�?

Scope 只能通过�?

```text
ScopeActorGateway
ActorEventInbox
ActorLifecycleInbox
```

发送命令�?

---

## 5. Publish / From 规则

Publish/From 是：

```text
同一�?ScopeRuntime 内部的生成式直接资源注入
```

最终字段访问必须等同于普通字段访问：

```csharp
[From(typeof(StorageContext), ResourceKeys.Items)]
private IReadOnlyList<Item> _items = null!;
```

禁止重新引入�?

```text
ScopeRead<T>
运行�?Resource.Get<T>
每次访问 Scope 检�?
每次访问 Generation 检�?
```

Scope 边界由构建期和对象访问入口保证，而不是字段读取时保证�?

---

## 6. 反射规则

唯一明确允许保留的动态反射兜底是�?

```text
EventCenter 未生成事件类型的动�?Bucket fallback
```

以下正式路径禁止反射�?

```text
Publish / From
Mount
Service / Context Factory
Module Dispatcher
Scope Contribution Discovery
Scope Handler Registration
Scope DI
```

正式路径禁止�?

```text
Assembly.GetType
GetMethod
MethodInfo.Invoke
GetFields
GetProperties
GetCustomAttribute
FieldInfo.SetValue
Activator.CreateInstance
MakeGenericType
```

EventCenter fallback 除外�?

---

## 7. 热路径规�?

以下稳定路径不能使用 Dictionary、反射、接口装箱或每次调用分配�?

```text
Scope<T>().Post
Scope<T>().Call
GetService / Mount
Publish / From 字段读取
Event 生成式分�?
Actor Event 命令
Continuation Enqueue / Dequeue
ECS Query
```

构建阶段允许使用 Dictionary 完成计划解析，但构建完成后必须冻结为数组�?Slot�?

---

## 8. IL2CPP 规则

必须满足�?

```text
正式运行不依赖字符串查找生成类型
不依赖私有反射写字段
不依赖静态构造器偶然运行
不依�?AppDomain.GetAssemblies
不依赖未显式引用的生�?Catalog
不依赖开放泛型的运行时动态构�?
```

---

# 二、当前基线问�?

当前 `1a7a6ba` 已经改善�?Worker Launch Signal、业务入口关闭、逐实�?Dispose 和生成式资源绑定，但仍存在以下问题�?

## 1. Start �?Stop 仍可在执行器发布前交�?

`Start()` 先把状态改�?`Starting`，随后才创建并发�?Worker Thread。并�?`Stop()` 可能�?`_workerThread == null` 时直接执�?Stop 清理，之�?Start 又启�?Worker�?

## 2. Stop �?Dispose 不同�?

`Dispose()` 发现 Stop Cleanup 已经开始时不会等待 `_stopCleanupCompleted`，会直接释放 Timer、ECS World、Context 等基础设施�?

## 3. Continuation Close �?Enqueue 不原�?

`ReliableContinuationInbox` 先读�?`_closed`，再写入另一个队列；Close 只是�?`_closed = true`，仍可能在最�?Drain 后写入新 Continuation�?

## 4. Worker Scope 仍直接修改共�?ActorWorld

每个 Scope 都会执行 `EcsWorld.SweepProjectedActors()`，�?Sweep 内部仍直接调用共�?ActorWorld �?Disable/Release�?

## 5. 资源系统仍依赖运行时反射发现 Catalog

`ScopeRuntime` 使用 `assembly.GetType("GeneratedScopeResourceContributions")`、`GetMethod` �?`Invoke` 加载生成贡献�?

## 6. 反射 Resource Binder 仍在正式路径

生成资源不存在或加载失败时仍调用 `ScopeResourceBinder` 扫描字段、属性和 Attribute�?

## 7. Module Dispatcher �?ScopeHostFactory 仍为全局静�?

不同 Runtime 仍可能覆盖彼此的 Dispatcher �?Factory�?

## 8. Scope DI 仍然使用 Dictionary、IsAssignableFrom 和反�?Mount

当前正式构建路径仍调用反射式 `InjectMembers()`�?

---

# 三、目标文件结�?

## 新增文件

```text
LayerBase/Scope/Lifecycle/ScopeLifecycleCoordinator.cs
LayerBase/Scope/Lifecycle/ScopeStartOperation.cs
LayerBase/Scope/Lifecycle/ScopeTerminationReason.cs

LayerBase/Scope/Completion/ScopeCompletionInbox.cs
LayerBase/Scope/Completion/ScopeCompletionShutdown.cs

LayerBase/Scope/Resources/ScopeResourceBindingRoute.cs
LayerBase/Scope/Resources/ScopeResourcePlan.cs
LayerBase/Scope/Resources/ScopeResourcePlanBuilder.cs
LayerBase/Scope/Resources/IScopeResourceContributionRegistrar.cs

LayerBase/Scope/DI/ScopeObjectSlot.cs
LayerBase/Scope/DI/ScopeMountPlan.cs
LayerBase/Scope/DI/GeneratedScopeMountContext.cs

LayerBase/Actor/RuntimeCommands/ProjectedActorLifecycleCommand.cs
LayerBase/Actor/RuntimeCommands/IProjectedActorLifecycleSink.cs

LayerBase/Modules/ModuleRuntimePlan.cs
LayerBase/Modules/ModuleRuntimePlanBuilder.cs
LayerBase/Modules/ModuleRuntimeInstance.cs
```

## 重点修改文件

```text
LayerBase/Scope/ScopeRuntime.cs
LayerBase/Scope/ScopePromise.cs
LayerBase/Scope/ScopeRuntimeHost.cs
LayerBase/Scope/ScopeRouteTable.cs

LayerBase/Scope/Completion/ScopeAwaitRegistry.cs
LayerBase/Scope/Completion/ReliableContinuationInbox.cs

LayerBase.Task/LayerBaseSynchronizationContext.cs
LayerBase/Async/MainThreadCompletionQueue.cs

LayerBase/Application/LayerRuntime.cs
LayerBase/Application/LayerRuntime.ActorCommands.cs

LayerBase/Actor/ScopeActorGateway.cs
LayerBase/Actor/RuntimeCommands/ActorEventInbox.cs
LayerBase/Actor/RuntimeCommands/ActorLifecycleInbox.cs

LayerBase/ECS/Projection/ActiveProjectedActorList.cs
LayerBase/ECS/Projection/World.ProjectedActor.cs

LayerBase/Scope/Resources/ScopeResourceRegistry.cs
LayerBase/Scope/Resources/ScopeResourceExportContribution.cs
LayerBase/Scope/Resources/ScopeResourceImportContribution.cs

LayerBase/Scope/ScopeServiceProvider.cs
LayerBase.Generator/LayerBase.Generator/ScopeResourceGenerator.cs
LayerBase.Generator/LayerBase.Generator/SharedFieldAnalyzer.cs
LayerBase.Generator/LayerBase.Generator/LayerServiceGenerator.cs

LayerBase/Layer/Layer.cs
```

## 删除文件

```text
LayerBase/Scope/ScopeResourceBinder.cs
LayerBase/Scope/ModuleDispatchRegistry.cs
LayerBase/Scope/ScopeHostFactory.cs
```

若仍存在以下旧文件，也应删除�?

```text
ServiceLayerBinder.cs
ServiceScopeBinding.cs
ScopeServiceOwnerRegistry.cs
LegacyScopeRuntimePlanner.cs
ModuleCatalogRegistry.cs
```

---

# 四、Task 0：建立失败测试基�?

**测试文件�?*

```text
LayerBase.Test/ScopeLifecycleConcurrencyTests.cs
LayerBase.Test/ScopePromiseShutdownTests.cs
LayerBase.Test/ProjectedActorOwnershipTests.cs
LayerBase.Test/ScopeResourceGenerationTests.cs
LayerBase.Test/ModuleRuntimeIsolationTests.cs
LayerBase.Test/ScopeDiGenerationTests.cs
```

## Step 1：Start/Stop 竞态测�?

创建一个阻塞在 Start 中的 Service�?

```csharp
private sealed class BlockingStartService : IService, IInitializable, IDisposable
{
    public readonly ManualResetEventSlim StartEntered = new(false);
    public readonly ManualResetEventSlim AllowStartReturn = new(false);

    public int InitializeCount;
    public int DisposeCount;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Initialize()
    {
        Interlocked.Increment(ref InitializeCount);
        StartEntered.Set();
        AllowStartReturn.Wait();
    }

    public void Dispose()
    {
        Interlocked.Increment(ref DisposeCount);
    }
}
```

测试�?

```csharp
[Test]
public void Start_and_stop_must_not_initialize_disposed_service()
{
    var service = new BlockingStartService();
    using var scope = CreateWorkerScope(service);

    Task start = Task.Run(scope.Start);
    Assert.That(service.StartEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);

    Task stop = Task.Run(scope.Stop);

    service.AllowStartReturn.Set();

    Assert.That(Task.WaitAll(new[] { start, stop }, TimeSpan.FromSeconds(5)), Is.True);
    Assert.That(service.InitializeCount, Is.EqualTo(1));
    Assert.That(service.DisposeCount, Is.EqualTo(1));
}
```

预期：当前实现应在压力循环下暴露顺序问题�?

---

## Step 2：Stop/Dispose 同步测试

```csharp
private sealed class BlockingDisposeService : IService, IDisposable
{
    public readonly ManualResetEventSlim DisposeEntered = new(false);
    public readonly ManualResetEventSlim AllowDisposeReturn = new(false);

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Dispose()
    {
        DisposeEntered.Set();
        AllowDisposeReturn.Wait();
    }
}
```

测试必须验证 `Dispose()` 不会�?Stop Cleanup 完成前返回�?

---

## Step 3：Continuation 原子关闭测试

使用 Barrier 控制�?

```text
生产者通过 IsClosed 检�?
关闭线程执行 CloseAndDrain
生产者继�?Enqueue
```

最终必须满足：

```text
Enqueue 成功 -> Continuation 一定被执行
Enqueue 失败 -> Continuation 从未进入 Inbox
```

不存在成功返回但未执行的情况�?

---

## Step 4：ProjectedActor Owner Thread 测试

创建 Worker Scope + Shared ActorWorld，记录：

```text
DisableProjectedActor 调用线程
ReleaseProjectedActor 调用线程
LayerRuntime Owner Thread
Scope Worker Thread
```

要求 Disable/Release 只能出现�?LayerRuntime Owner Thread�?

---

## Step 5：运行测试并确认失败

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release
```

提交�?

```bash
git commit -m "test: add scope ownership and shutdown regressions"
```

---

# 五、Task 1：重�?Scope 生命周期协调�?

## 目标

解决�?

```text
Start/Stop 发布竞�?
Stop/Dispose 并发
Cleanup 重入
Worker Launch 等待
Inline Start Owner 协调
状态数值比较错�?
```

## 1. 状态枚�?

修改�?

`LayerBase/Scope/Lifecycle/ScopeRuntimeState.cs`

```csharp
internal enum ScopeRuntimeState
{
    Created,
    Starting,
    Running,
    StopRequested,
    Stopping,
    Stopped,
    Disposing,
    Disposed
}
```

禁止继续使用�?

```csharp
state >= ScopeRuntimeState.StopRequested
```

统一改为显式方法�?

```csharp
internal static class ScopeRuntimeStateExtensions
{
    public static bool AcceptsBusinessIngress(this ScopeRuntimeState state)
    {
        return state is ScopeRuntimeState.Created
            or ScopeRuntimeState.Starting
            or ScopeRuntimeState.Running;
    }

    public static bool IsStoppingOrStopped(this ScopeRuntimeState state)
    {
        return state is ScopeRuntimeState.StopRequested
            or ScopeRuntimeState.Stopping
            or ScopeRuntimeState.Stopped
            or ScopeRuntimeState.Disposing
            or ScopeRuntimeState.Disposed;
    }
}
```

`Faulted` 不再作为生命周期状态�?

新增�?

```csharp
internal enum ScopeTerminationReason
{
    None,
    Requested,
    RuntimeStopping,
    StartFailure,
    WorkerFailure,
    Dispose
}
```

异常保存在独立字段：

```csharp
private ScopeTerminationReason _terminationReason;
private Exception? _terminationException;
```

---

## 2. 启动操作

新增�?

`LayerBase/Scope/Lifecycle/ScopeStartOperation.cs`

```csharp
internal sealed class ScopeStartOperation : IDisposable
{
    private readonly ManualResetEventSlim _published = new(false);
    private readonly ManualResetEventSlim _completed = new(false);

    private int _ownerThreadId;
    private int _launchSucceeded;

    public Thread? WorkerThread { get; private set; }

    public int OwnerThreadId => Volatile.Read(ref _ownerThreadId);

    public bool LaunchSucceeded => Volatile.Read(ref _launchSucceeded) != 0;

    public void PublishInlineOwner()
    {
        Volatile.Write(ref _ownerThreadId, Environment.CurrentManagedThreadId);
        _published.Set();
    }

    public void PublishWorker(Thread worker)
    {
        WorkerThread = worker ?? throw new ArgumentNullException(nameof(worker));
        _published.Set();
    }

    public void MarkLaunchSucceeded()
    {
        Volatile.Write(ref _launchSucceeded, 1);
    }

    public void MarkCompleted()
    {
        _completed.Set();
    }

    public void WaitPublished()
    {
        _published.Wait();
    }

    public void WaitCompleted()
    {
        _completed.Wait();
    }

    public void Dispose()
    {
        _published.Dispose();
        _completed.Dispose();
    }
}
```

---

## 3. Start 实现

`ScopeRuntime.Start()` 不再先通过独立 `TryTransition()` 修改状态�?

Worker Scope�?

```csharp
public void Start()
{
    ScopeStartOperation operation;
    Thread? worker = null;

    lock (_lifecycleGate)
    {
        ThrowIfDisposedLocked();

        if (_state != ScopeRuntimeState.Created)
        {
            return;
        }

        _state = ScopeRuntimeState.Starting;
        operation = new ScopeStartOperation();
        _startOperation = operation;

        if (Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"LayerBase.Scope.{Descriptor.Name}"
            };

            _workerThread = worker;
            operation.PublishWorker(worker);
            _workerRunning = true;
        }
        else
        {
            operation.PublishInlineOwner();
        }
    }

    if (worker != null)
    {
        try
        {
            worker.Start();
            operation.MarkLaunchSucceeded();
        }
        catch (Exception ex)
        {
            RecordTermination(ScopeTerminationReason.StartFailure, ex);
            RequestStopCore(ScopeTerminationReason.StartFailure, ex);
            operation.MarkCompleted();
            ExecuteStopInternalOnce();
            throw;
        }

        return;
    }

    RunInlineStart(operation);
}
```

Inline�?

```csharp
private void RunInlineStart(ScopeStartOperation operation)
{
    Exception? failure = null;

    try
    {
        ExecuteInScope(StartScope);
    }
    catch (Exception ex)
    {
        failure = ex;
        RecordTermination(ScopeTerminationReason.StartFailure, ex);
    }
    finally
    {
        bool stopRequested;

        lock (_lifecycleGate)
        {
            stopRequested = _state == ScopeRuntimeState.StopRequested || failure != null;

            if (!stopRequested)
            {
                _state = ScopeRuntimeState.Running;
            }
        }

        operation.MarkCompleted();

        if (stopRequested)
        {
            ExecuteStopInternalOnce();
        }
    }

    if (failure != null)
    {
        throw failure;
    }
}
```

WorkerLoop �?StartScope 完成后：

```csharp
private void WorkerLoop()
{
    ScopeStartOperation operation = _startOperation
        ?? throw new InvalidOperationException("Scope start operation is missing.");

    try
    {
        ExecuteInScope(StartScope);

        bool shouldRun;
        lock (_lifecycleGate)
        {
            shouldRun = _state == ScopeRuntimeState.Starting;
            if (shouldRun)
            {
                _state = ScopeRuntimeState.Running;
            }
        }

        operation.MarkCompleted();

        if (!shouldRun)
        {
            ExecuteStopInternalOnce();
            return;
        }

        RunWorkerPumpLoop();
    }
    catch (Exception ex)
    {
        operation.MarkCompleted();
        RecordTermination(ScopeTerminationReason.WorkerFailure, ex);
        RequestStopCore(ScopeTerminationReason.WorkerFailure, ex);
    }
    finally
    {
        ExecuteStopInternalOnce();
    }
}
```

---

## 4. Stop 核心

```csharp
private ScopeStopRequest RequestStopCore(
    ScopeTerminationReason reason,
    Exception? exception = null)
{
    ScopeStartOperation? startOperation;
    Thread? worker;

    lock (_lifecycleGate)
    {
        if (_state is ScopeRuntimeState.Stopped
            or ScopeRuntimeState.Disposing
            or ScopeRuntimeState.Disposed)
        {
            return ScopeStopRequest.AlreadyStopped;
        }

        if (_state != ScopeRuntimeState.StopRequested
            && _state != ScopeRuntimeState.Stopping)
        {
            _state = ScopeRuntimeState.StopRequested;
            _terminationReason = reason;
            _terminationException ??= exception;
        }

        startOperation = _startOperation;
        worker = _workerThread;
    }

    CloseBusinessIngress();
    _workerRunning = false;

    return new ScopeStopRequest(startOperation, worker);
}
```

`Stop()`�?

```csharp
public void Stop()
{
    ScopeStopRequest request =
        RequestStopCore(ScopeTerminationReason.Requested);

    if (request.IsAlreadyStopped)
    {
        WaitForStopCleanupIfNeeded();
        return;
    }

    if (request.StartOperation != null &&
        request.StartOperation.OwnerThreadId != Environment.CurrentManagedThreadId &&
        !ReferenceEquals(request.WorkerThread, Thread.CurrentThread))
    {
        request.StartOperation.WaitCompleted();
    }

    if (request.WorkerThread != null &&
        !ReferenceEquals(request.WorkerThread, Thread.CurrentThread))
    {
        if (request.StartOperation?.LaunchSucceeded == true)
        {
            request.WorkerThread.Join();
        }
    }
    else
    {
        ExecuteStopInternalOnce();
    }

    WaitForStopCleanupIfNeeded();
}
```

---

## 5. Stop Cleanup 单所有�?

字段�?

```csharp
private readonly ManualResetEventSlim _stopCleanupFinished = new(false);
private int _stopCleanupOwnerThreadId;
private int _stopCleanupStarted;
private int _stopCleanupCompleted;
```

```csharp
private void ExecuteStopInternalOnce()
{
    if (Interlocked.CompareExchange(ref _stopCleanupStarted, 1, 0) != 0)
    {
        WaitForStopCleanupIfNeeded();
        return;
    }

    Volatile.Write(
        ref _stopCleanupOwnerThreadId,
        Environment.CurrentManagedThreadId);

    try
    {
        lock (_lifecycleGate)
        {
            _state = ScopeRuntimeState.Stopping;
        }

        ExecuteInScope(StopInternal);

        lock (_lifecycleGate)
        {
            _state = ScopeRuntimeState.Stopped;
        }

        Volatile.Write(ref _stopCleanupCompleted, 1);
    }
    finally
    {
        Volatile.Write(ref _stopCleanupOwnerThreadId, 0);
        _stopCleanupFinished.Set();
    }
}
```

```csharp
private void WaitForStopCleanupIfNeeded()
{
    if (Volatile.Read(ref _stopCleanupStarted) == 0)
    {
        return;
    }

    if (Volatile.Read(ref _stopCleanupOwnerThreadId)
        == Environment.CurrentManagedThreadId)
    {
        return;
    }

    _stopCleanupFinished.Wait();
}
```

---

## 6. Dispose

```csharp
private int _disposeStarted;

public void Dispose()
{
    if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
    {
        WaitForStopCleanupIfNeeded();
        return;
    }

    RequestStopCore(ScopeTerminationReason.Dispose);
    Stop();
    WaitForStopCleanupIfNeeded();

    lock (_lifecycleGate)
    {
        if (_state == ScopeRuntimeState.Disposed)
        {
            return;
        }

        _state = ScopeRuntimeState.Disposing;
    }

    DisposeInfrastructure();

    lock (_lifecycleGate)
    {
        _state = ScopeRuntimeState.Disposed;
    }
}
```

`DisposeInfrastructure()` 只能�?Stop Cleanup 完成后执行�?

---

## 验证

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
  -c Release \
  --filter "FullyQualifiedName~ScopeLifecycleConcurrencyTests"
```

提交�?

```bash
git commit -m "fix: make scope start stop and dispose atomic"
```

---

# 六、Task 2：重�?Promise �?Completion 关闭协议

## 1. 替换 ReliableContinuationInbox

删除当前 volatile `_closed` + 两套锁实现�?

新增�?

`LayerBase/Scope/Completion/ScopeCompletionInbox.cs`

```csharp
internal sealed class ScopeCompletionInbox
{
    private readonly object _gate = new();
    private readonly LayerContinuation[] _ring;
    private readonly Queue<LayerContinuation> _overflow = new();

    private int _head;
    private int _tail;
    private int _count;
    private bool _closed;

    public ScopeCompletionInbox(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _ring = new LayerContinuation[capacity];
    }

    public bool TryEnqueue(in LayerContinuation continuation)
    {
        lock (_gate)
        {
            if (_closed)
            {
                return false;
            }

            if (_count < _ring.Length)
            {
                _ring[_tail] = continuation;
                _tail = (_tail + 1) % _ring.Length;
                _count++;
            }
            else
            {
                _overflow.Enqueue(continuation);
            }

            return true;
        }
    }

    public bool TryDequeue(out LayerContinuation continuation)
    {
        lock (_gate)
        {
            if (_count > 0)
            {
                continuation = _ring[_head];
                _ring[_head] = default;
                _head = (_head + 1) % _ring.Length;
                _count--;
                return true;
            }

            if (_overflow.Count > 0)
            {
                continuation = _overflow.Dequeue();
                return true;
            }

            continuation = default;
            return false;
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            _closed = true;
        }
    }

    public void CloseAndDrain(Action<LayerContinuation> invoke)
    {
        ArgumentNullException.ThrowIfNull(invoke);

        while (true)
        {
            LayerContinuation[] pending;

            lock (_gate)
            {
                _closed = true;

                int total = _count + _overflow.Count;
                if (total == 0)
                {
                    return;
                }

                pending = new LayerContinuation[total];
                int index = 0;

                while (_count > 0)
                {
                    pending[index++] = _ring[_head];
                    _ring[_head] = default;
                    _head = (_head + 1) % _ring.Length;
                    _count--;
                }

                while (_overflow.Count > 0)
                {
                    pending[index++] = _overflow.Dequeue();
                }
            }

            for (int i = 0; i < pending.Length; i++)
            {
                invoke(pending[i]);
            }
        }
    }
}
```

如需避免关闭阶段数组分配，可改成锁内逐项 detach 到池化链表，但第一阶段正确性优先�?

---

## 2. AwaitRegistry 生命周期

Promise 不得在刚完成时无条件注销�?

规则�?

```text
Pending                          -> Registry �?
Completed + continuation queued -> 注销
Completed + GetResult consumed  -> 注销
Completed 但尚�?continuation   -> 暂时保留
```

接口�?

```csharp
internal interface IScopePromiseControl
{
    bool IsTerminal { get; }

    bool HasScheduledContinuation { get; }

    bool TrySetException(Exception reason);

    void CloseWithoutContinuation();
}
```

`ScopeAwaitRegistry.CancelAll()`�?

```csharp
public void CancelAll(Exception reason)
{
    IScopePromiseControl[] snapshot;

    lock (_gate)
    {
        _closed = true;
        snapshot = _promises.ToArray();
    }

    for (int i = 0; i < snapshot.Length; i++)
    {
        snapshot[i].TrySetException(reason);
    }
}
```

不要在调�?Promise 前先清空集合�?

等所�?Promise 完成调度后，�?Promise 自己 `Unregister()`�?

关闭结束�?

```csharp
public void FinalizeClose()
{
    lock (_gate)
    {
        foreach (IScopePromiseControl promise in _promises)
        {
            promise.CloseWithoutContinuation();
        }

        _promises.Clear();
    }
}
```

---

## 3. ScopePromise 完成协议

`Complete()`�?

```csharp
private void Complete(TResult? result, Exception? exception)
{
    Action? continuation;

    lock (_gate)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _result = result;
        _exception = exception;
        continuation = _continuation;
    }

    if (continuation != null)
    {
        ScheduleContinuationAndUnregister(continuation);
    }
}
```

```csharp
private void ScheduleContinuationAndUnregister(Action continuation)
{
    if (_continuationScope == null)
    {
        continuation();
        return;
    }

    if (!_continuationScope.TryEnqueueContinuation(continuation))
    {
        throw new ScopeCompletionProtocolException(
            "A registered scope promise was completed after the owner completion inbox had closed.");
    }

    _continuationScope.AwaitRegistry.Unregister(this);
}
```

`OnCompleted()`�?

```csharp
public void OnCompleted(Action continuation)
{
    bool runNow;

    lock (_gate)
    {
        if (_continuation != null)
        {
            throw new InvalidOperationException(
                "ScopePromise supports only one continuation.");
        }

        runNow = _completed;

        if (!runNow)
        {
            _continuation = continuation;
        }
    }

    if (runNow)
    {
        ScheduleContinuationAndUnregister(continuation);
    }
}
```

`GetResult()` 最后注销�?

```csharp
public TResult GetResult()
{
    try
    {
        lock (_gate)
        {
            if (!_completed)
            {
                throw new InvalidOperationException("Promise is not completed.");
            }

            if (_exception != null)
            {
                ExceptionDispatchInfo.Capture(_exception).Throw();
            }

            return _result!;
        }
    }
    finally
    {
        _continuationScope?.AwaitRegistry.Unregister(this);
    }
}
```

---

## 4. Scope Stop 顺序

最终固定为�?

```text
1. CloseBusinessIngress
2. 停止 Worker Pump
3. Drain/Drop Post
4. Drain/Fail Call
5. AwaitRegistry.CancelAll
6. Drain Completion 到空
7. CompletionInbox.CloseAndDrain
8. AwaitRegistry.FinalizeClose
9. Dispose Subscription
10. Clear Delay
11. Unbind Resource
12. Dispose Context
13. Dispose Service
```

禁止在第 5 步之前关�?Completion Inbox�?

---

## 验证

必须新增�?

```text
Promise 完成�?OnCompleted 并发
Promise 完成�?Scope Stop 并发
多个 Promise 中一�?Continuation 抛异�?
FastLane 满进�?Overflow
Close �?Enqueue 并发
Stop 后所�?Promise 必须终�?
```

提交�?

```bash
git commit -m "fix: guarantee scope promise completion during shutdown"
```

---

# 七、Task 3：统一 SynchronizationContext �?Completion Queue

## 1. 重命�?

```text
MainThreadCompletionQueue
    -> ScopeCompletionQueue
```

它不�?MainThread 专用，而是 Scope Owner Thread Completion 通道�?

---

## 2. 状�?

```csharp
internal enum ScopeSynchronizationContextState
{
    Running,
    Closing,
    Closed
}
```

所�?Post、Send、ScheduleInFrames、Close 使用同一�?`_gate`�?

---

## 3. Post

Dispose 后禁止静默丢弃�?

```csharp
public override void Post(
    SendOrPostCallback callback,
    object? state)
{
    lock (_gate)
    {
        if (_state != ScopeSynchronizationContextState.Running)
        {
            throw new ObjectDisposedException(
                nameof(LayerBaseSynchronizationContext));
        }

        _queue.Enqueue(new ContextWorkItem(callback, state));
    }
}
```

---

## 4. Send

Owner Thread 也必须先检查状态：

```csharp
public override void Send(
    SendOrPostCallback callback,
    object? state)
{
    lock (_gate)
    {
        ThrowIfNotRunningLocked();

        if (Environment.CurrentManagedThreadId == _ownerThreadId)
        {
            callback(state);
            return;
        }

        // 注册 SendWorkItem 并入�?
    }

    // 锁外等待
}
```

不得在持�?`_gate` �?`gate.Wait()`�?

---

## 5. CloseAndCancel

```csharp
public void CloseAndCancel(Exception reason)
{
    ContextWorkItem[] pending;

    lock (_gate)
    {
        if (_state == ScopeSynchronizationContextState.Closed)
        {
            return;
        }

        _state = ScopeSynchronizationContextState.Closing;
        pending = DetachAllWorkLocked();
        _state = ScopeSynchronizationContextState.Closed;
    }

    foreach (ContextWorkItem item in pending)
    {
        item.Cancel(reason);
    }
}
```

所�?WorkItem 必须实现�?

```csharp
internal interface IContextWorkItem
{
    void Execute();

    void Cancel(Exception reason);
}
```

NextFrame、后�?Completion �?SendWorkItem 都必须可取消�?

---

## 验证

```text
Send �?Dispose 并发不死�?
Post �?Dispose 后明确抛异常
NextFrame �?Dispose 时完成取�?
后台 Completion �?Scope 结束后不保留闭包
Owner Thread Send 在关闭后不能继续执行
```

提交�?

```bash
git commit -m "fix: make scope synchronization context closable"
```

---

# 八、Task 4：ProjectedActor 生命周期命令�?

## 1. 新接�?

`LayerBase/Actor/RuntimeCommands/IProjectedActorLifecycleSink.cs`

```csharp
internal interface IProjectedActorLifecycleSink
{
    bool TryDisable(ActorId actorId);

    bool TryRelease(
        ActorId actorId,
        ProjectedActorReleasePolicy policy);
}
```

---

## 2. ScopeActorGateway 实现

```csharp
public sealed class ScopeActorGateway :
    IProjectedActorLifecycleSink
{
    private readonly LayerRuntime? _runtime;
    private readonly ActorWorld _actorWorld;

    public bool TryDisable(ActorId actorId)
    {
        if (_runtime == null)
        {
            _actorWorld.DisableProjectedActor(actorId);
            return true;
        }

        return _runtime.EnqueueActorLifecycle(
            ActorCommandEnvelope.Disable(actorId));
    }

    public bool TryRelease(
        ActorId actorId,
        ProjectedActorReleasePolicy policy)
    {
        if (_runtime == null)
        {
            _actorWorld.ReleaseProjectedActor(actorId, policy);
            return true;
        }

        return _runtime.EnqueueActorLifecycle(
            ActorCommandEnvelope.Release(actorId, policy));
    }
}
```

---

## 3. World 绑定 Gateway

修改�?

```csharp
private IProjectedActorLifecycleSink? _actorLifecycle;
```

```csharp
internal void BindScopeActors(
    ActorWorld actors,
    IProjectedActorLifecycleSink lifecycle)
{
    _scopeActors = actors;
    _actorLifecycle = lifecycle;
}
```

Sweep 不再接收 ActorWorld�?

```csharp
internal void SweepProjectedActors(int maxCount = 512)
{
    _activeProjectedActors.Sweep(
        this,
        _actorLifecycle
            ?? throw new InvalidOperationException(
                "Projected actor lifecycle sink is not bound."),
        maxCount);
}
```

---

## 4. ActiveProjectedActorList

删除�?

```text
TryGetPooledActor
IPooledActor 参数
actorWorld.DisableProjectedActor
actorWorld.ReleaseProjectedActor
```

Retire�?

```csharp
private bool TryRetireProjectedActor(
    World world,
    IProjectedActorLifecycleSink lifecycle,
    Entity entity,
    ref ProjectedActorMeta meta,
    ref ProjectedActorRef actorRef)
{
    switch (meta.RetirePolicy)
    {
        case ProjectedActorRetirePolicy.Disable:
            if (!lifecycle.TryDisable(meta.ActorId))
            {
                return false;
            }

            meta.State = ProjectedActorState.Disabled;
            actorRef.ExpireAtTicks = long.MaxValue;
            return true;

        case ProjectedActorRetirePolicy.ReturnToPool:
            if (!lifecycle.TryRelease(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.ReturnToPool))
            {
                return false;
            }

            ClearProjection(...);
            return true;

        case ProjectedActorRetirePolicy.DestroyImmediately:
            if (!lifecycle.TryRelease(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.DestroyImmediately))
            {
                return false;
            }

            ClearProjection(...);
            return true;

        case ProjectedActorRetirePolicy.DetachAndLetActorFinish:
            if (!lifecycle.TryRelease(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.DetachAndLetActorFinish))
            {
                return false;
            }

            ClearProjection(...);
            return true;

        default:
            return false;
    }
}
```

�?Lifecycle Inbox 满：

```text
不清理本地投�?
不修�?State
保留到下一�?Sweep 重试
```

这样不会丢失生命周期命令�?

---

## 5. LayerRuntime Pump 顺序

```csharp
public void Pump(float deltaTime)
{
    ScopeHost.Pump(deltaTime);

    DrainActorLifecycleCommands();
    DrainActorEventCommands();

    Actors.Pump(
        deltaTime,
        fixedDeltaTime,
        pumpFixedUpdate,
        ref budget);

    ExceptionHub.Drain();
    GlobalTools.Pump();
}
```

---

## 6. LayerRuntime Dispose

顺序固定为：

```text
1. 标记 Runtime StopRequested
2. ScopeHost.Stop / Dispose
3. 等待所�?Scope Worker 结束
4. Drain ActorLifecycleInbox 到空
5. Close ActorLifecycleInbox
6. Close ActorEventInbox
7. Drop Actor Event 并释放所�?Payload
8. Dispose ActorWorld
9. Dispose Global Tools
```

Actor Lifecycle 命令不得直接 Clear�?

普�?Actor Event 可以 Drop，但必须释放 Payload�?

---

## 验证

```text
Worker Scope Sweep 不调用共�?ActorWorld
Lifecycle Inbox 满时下帧重试
Disable/Release 只在 Runtime Owner Thread
Runtime Dispose 前处理完 Lifecycle 命令
Actor Event Drop 不泄�?Payload
```

提交�?

```bash
git commit -m "fix: route projected actor lifecycle through runtime owner"
```

---

# 九、Task 5：重建生成式 Publish / From

## 1. 删除运行期反�?Binder

删除�?

```text
LayerBase/Scope/ScopeResourceBinder.cs
```

`ScopeRuntime.RebuildScopeResources()` 不允许再 fallback�?

如果某个 `[Provide]/[From]` 类型没有实现生成接口，构建必须明确失败：

```csharp
throw new ScopeCompositionException(
    $"Scope resource owner '{type.FullName}' has [Provide]/[From] members but no generated binding. " +
    "Ensure the type and all containing types are partial.");
```

---

## 2. Contribution 不再使用全局 ExportId

删除字段�?

```text
ExportId
ImportId
_exportsById
```

最�?Contribution�?

```csharp
public readonly struct ScopeResourceExportContribution
{
    public ScopeResourceExportContribution(
        RuntimeTypeHandle providerType,
        RuntimeTypeHandle declaredResourceType,
        string localKey,
        int providerLocalSlot)
    {
        ProviderType = providerType;
        DeclaredResourceType = declaredResourceType;
        LocalKey = localKey;
        ProviderLocalSlot = providerLocalSlot;
    }

    public RuntimeTypeHandle ProviderType { get; }

    public RuntimeTypeHandle DeclaredResourceType { get; }

    public string LocalKey { get; }

    public int ProviderLocalSlot { get; }
}
```

```csharp
public readonly struct ScopeResourceImportContribution
{
    public ScopeResourceImportContribution(
        RuntimeTypeHandle consumerType,
        RuntimeTypeHandle providerType,
        RuntimeTypeHandle requestedResourceType,
        string localKey,
        int consumerLocalSlot)
    {
        ConsumerType = consumerType;
        ProviderType = providerType;
        RequestedResourceType = requestedResourceType;
        LocalKey = localKey;
        ConsumerLocalSlot = consumerLocalSlot;
    }

    public RuntimeTypeHandle ConsumerType { get; }

    public RuntimeTypeHandle ProviderType { get; }

    public RuntimeTypeHandle RequestedResourceType { get; }

    public string LocalKey { get; }

    public int ConsumerLocalSlot { get; }
}
```

---

## 3. 构建�?Route

```csharp
internal readonly struct ScopeResourceBindingRoute
{
    public ScopeResourceBindingRoute(
        int providerObjectSlot,
        int providerLocalSlot,
        int consumerObjectSlot,
        int consumerLocalSlot,
        RuntimeTypeHandle requestedType,
        string localKey)
    {
        ProviderObjectSlot = providerObjectSlot;
        ProviderLocalSlot = providerLocalSlot;
        ConsumerObjectSlot = consumerObjectSlot;
        ConsumerLocalSlot = consumerLocalSlot;
        RequestedType = requestedType;
        LocalKey = localKey;
    }

    public int ProviderObjectSlot { get; }

    public int ProviderLocalSlot { get; }

    public int ConsumerObjectSlot { get; }

    public int ConsumerLocalSlot { get; }

    public RuntimeTypeHandle RequestedType { get; }

    public string LocalKey { get; }
}
```

`ModuleRuntimePlanBuilder` 可以在构建阶段使�?Dictionary�?

```text
(ProviderType, LocalKey) -> Export Plan
```

构建完成后冻结成 `ScopeResourceBindingRoute[]`�?

---

## 4. Runtime Registry

```csharp
internal sealed class ScopeResourceRegistry
{
    private IGeneratedScopeResourceConsumer[] _consumers =
        Array.Empty<IGeneratedScopeResourceConsumer>();

    private bool _closed;

    public void Initialize(
        object[] scopeObjects,
        ScopeResourceBindingRoute[] routes)
    {
        if (_closed)
        {
            throw new InvalidOperationException(
                "Scope resource registry is closed.");
        }

        var consumers =
            new List<IGeneratedScopeResourceConsumer>();

        for (int i = 0; i < routes.Length; i++)
        {
            ref readonly ScopeResourceBindingRoute route =
                ref routes[i];

            var publisher =
                (IGeneratedScopeResourcePublisher)
                scopeObjects[route.ProviderObjectSlot];

            var consumer =
                (IGeneratedScopeResourceConsumer)
                scopeObjects[route.ConsumerObjectSlot];

            object resource =
                publisher.GetPublishedResource(
                    route.ProviderLocalSlot);

            Type requested =
                Type.GetTypeFromHandle(route.RequestedType);

            if (!requested.IsInstanceOfType(resource))
            {
                throw new ScopeCompositionException(...);
            }

            consumer.BindScopeResource(
                route.ConsumerLocalSlot,
                resource);

            consumers.Add(consumer);
        }

        _consumers = consumers
            .Distinct()
            .ToArray();
    }

    public void CloseAndUnbind(
        Action<Exception, object> report)
    {
        _closed = true;

        for (int i = 0; i < _consumers.Length; i++)
        {
            try
            {
                _consumers[i].UnbindScopeResources();
            }
            catch (Exception ex)
            {
                report(ex, _consumers[i]);
            }
        }

        _consumers = Array.Empty<
            IGeneratedScopeResourceConsumer>();
    }
}
```

运行时不再有资源 Dictionary�?

---

## 5. 静�?Catalog 注册

删除�?

```text
assembly.GetType("GeneratedScopeResourceContributions")
GetMethod
MethodInfo.Invoke
```

Generator 输出�?

```csharp
internal static class __LayerBaseScopeResources_A1B2C3
{
    public static void Register(
        global::LayerBase.Scope.Resources.ScopeResourcePlanBuilder builder)
    {
        builder.AddExport(...);
        builder.AddImport(...);
    }
}
```

对应 `GeneratedModuleCatalog` 必须直接调用�?

```csharp
__LayerBaseScopeResources_A1B2C3.Register(
    builder.ScopeResources);
```

`ModuleManifest` 增加�?

```csharp
public ScopeResourceContributionRegistrar
    ResourceRegistrar { get; }
```

或者在生成 Catalog 构建计划时直接调�?Builder，不保存 Delegate�?

不得通过程序集扫描发现资�?Catalog�?

---

## 6. 泛型和嵌套类�?

第一阶段明确禁止泛型资源 Owner�?

Analyzer 新增�?

```text
LBG413
包含 [Provide]/[From] �?IService/ILayerContext 不允许声明类型参数�?
```

禁止�?

```csharp
partial class CacheService<T>
```

嵌套 Owner 仅允许：

```text
public
internal
protected internal 且生�?Catalog 可访�?
```

禁止 private/protected 嵌套 Owner�?

```text
LBG414
Scope resource owner must be accessible from the generated module catalog.
```

这比生成无法编译或依赖私有反射更安全�?

---

## 7. Analyzer

必须检查：

```text
LBG401 重复 Provide Key
LBG402 类型不可赋�?
LBG403 缺失 Provider
LBG404 Owner 不是 IService/ILayerContext
LBG405 字符串字面量 Key 警告
LBG406 From readonly
LBG407 From 属性不可写
LBG408 禁止的可写集�?
LBG409 Provide 值类�?
LBG410 Owner �?partial
LBG411 同成�?Provide + From
LBG412 �?Scope Resource
LBG413 泛型 Resource Owner
LBG414 Catalog 不可访问的嵌�?Owner
```

跨程序集 Provider 通过 compile-time Assembly Manifest 验证可以保留，但�?Manifest 不能参与运行时发现�?

---

## 验证

必须包含�?

```text
多个 Provider 多个 Consumer
多个程序集相同本�?Slot
跨程序集 Provider
缺失 Key
类型不匹�?
泛型 Owner �?LBG413
private nested Owner �?LBG414
Unbind 异常进入 ExceptionHub
运行时没�?Assembly.GetType/GetMethod/Invoke
```

提交�?

```bash
git commit -m "refactor: make scope resources fully generated"
```

---

# 十、Task 6：Scope Composition �?DI Slot �?

## 1. 统一对象数组

每个 Scope 生成�?

```csharp
internal sealed class ScopeCompositionInstance
{
    public required object[] Objects { get; init; }

    public required IService[] Services { get; init; }

    public required ILayerContext[] Contexts { get; init; }

    public required ScopeMountPlan[] Mounts { get; init; }

    public required ScopeResourceBindingRoute[]
        ResourceRoutes { get; init; }

    public required ScopeSubscriptionPlan[]
        Subscriptions { get; init; }
}
```

所�?Service、Context 都有唯一 ObjectSlot�?

---

## 2. ScopeServiceProvider

```csharp
internal sealed class ScopeServiceProvider :
    LayerBase.DI.IServiceProvider,
    IDisposable
{
    private object[] _objects;
    private bool _disposed;

    public ScopeServiceProvider(object[] objects)
    {
        _objects = objects
            ?? throw new ArgumentNullException(nameof(objects));
    }

    public T GetAt<T>(int slot)
        where T : class
    {
        ThrowIfDisposed();

        if ((uint)slot >= (uint)_objects.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot));
        }

        return (T)_objects[slot];
    }

    public void Dispose()
    {
        _disposed = true;
        _objects = Array.Empty<object>();
    }
}
```

删除�?

```text
Dictionary<Type, object>
IsAssignableFrom 循环
GetService(Type) 热路�?
InjectMembers 反射
```

---

## 3. Generated Mount

接口�?

```csharp
internal interface IGeneratedScopeMount
{
    void BindScopeMount(
        in GeneratedScopeMountContext context);
}
```

Context�?

```csharp
internal readonly struct GeneratedScopeMountContext
{
    private readonly object[] _objects;

    public GeneratedScopeMountContext(
        object[] objects)
    {
        _objects = objects;
    }

    public T GetAt<T>(int slot)
        where T : class
    {
        return (T)_objects[slot];
    }
}
```

生成代码�?

```csharp
partial class CombatService :
    IGeneratedScopeMount
{
    void IGeneratedScopeMount.BindScopeMount(
        in GeneratedScopeMountContext context)
    {
        _damageService =
            context.GetAt<DamageService>(3);

        _combatContext =
            context.GetAt<CombatContext>(7);
    }
}
```

---

## 4. 构建顺序

唯一合法顺序�?

```text
1. 创建 ScopeRuntime Shell
2. 创建全部 Service
3. 创建全部 Context
4. 创建统一 ObjectSlot 数组
5. 写入 ScopeObjectBinding
6. 执行 Generated Mount
7. 建立 ScopeServiceProvider
8. 建立 Publish Export
9. 执行 From Bind
10. 建立 Subscription
11. 调用 Initialize
12. Start Scope
```

`ScopeRuntime` 构造函数不得：

```text
RebuildServiceProvider
RebuildScopeResources
RebindSubscriptions
```

`SetContexts()` 不得自动 Finalize�?

改成一次性：

```csharp
internal void ApplyComposition(
    ScopeCompositionInstance composition)
```

重复调用直接抛异常�?

---

## 验证

```text
Mount 走固�?Slot
多实现歧义构建失�?
�?Scope Mount 构建失败
运行�?GetAt 不查 Dictionary
ScopeRuntime 构造阶段不绑定资源
资源只绑定一�?
```

提交�?

```bash
git commit -m "refactor: compose scope services with generated slots"
```

---

# 十一、Task 7：Module 去全局�?

## 删除

```text
ModuleDispatchRegistry
ScopeHostFactory
ModuleCatalogRegistry
```

## ModuleRuntimePlan

```csharp
internal sealed class ModuleRuntimePlan
{
    public required ModuleSlot[] Modules { get; init; }

    public required ScopeRuntimePlan[] Scopes { get; init; }

    public required ModuleEventDispatchHandler[]
        EventDispatchers { get; init; }

    public required ModuleCallDispatchHandler[]
        CallDispatchers { get; init; }

    public required ServiceFactory[]
        ServiceFactories { get; init; }

    public required ContextFactory[]
        ContextFactories { get; init; }

    public required ScopeResourcePlan[]
        ResourcePlans { get; init; }
}
```

每个 `LayerRuntime` 构建并持有自己的实例�?

禁止�?

```text
static Dispatcher[]
static Factory
根据 Module 数量判断 Registry 是否匹配
```

生成 Catalog�?

```csharp
public sealed class GeneratedModuleCatalog :
    IModuleCatalog
{
    public ModuleRuntimePlan CreatePlan(
        ModuleRuntimeBuildContext context)
    {
        var builder =
            new ModuleRuntimePlanBuilder(context);

        GameplayModule.Register(builder);
        NetworkModule.Register(builder);
        __LayerBaseScopeResources_A1B2C3.Register(
            builder.ScopeResources);

        return builder.Build();
    }
}
```

Module Build 失败必须抛出 `ModuleBuildException`，不�?fallback �?Legacy Planner�?

---

## �?Runtime 测试

```text
Runtime A�? �?Module，Dispatcher A
Runtime B�? �?Module，Dispatcher B
A/B 数量相同但内容不�?
必须各自调用正确 Dispatcher
```

提交�?

```bash
git commit -m "refactor: isolate module plans per runtime"
```

---

# 十二、Task 8：LayerRuntime �?Layer 职责收口

## 1. LayerRuntime 删除业务资源

删除字段�?API�?

```text
EventCenter
Scheduler
Timer
EcsWorld
EcsQueryRegistry
业务 ServiceProvider
Send/Post/Subscribe
GetService
```

增加�?

```csharp
public ScopeRuntime MainScope =>
    ScopeHost.MainScope;
```

但不增加业务 API Proxy�?

---

## 2. Layer 只保留逻辑层级

`Layer` 保留�?

```text
Parent/Children
LayerIndex
RouteIndex
Membership
Name
Debug Metadata
```

删除�?

```text
ServiceCollection
ServiceProvider
ResolvedServices
IUpdate 列表
IRuntimeStart/Stop 列表
订阅列表
Delay Publisher
业务 Call Handler
RegisterService/GetService
```

Scope �?Service 只通过 Module Composition 安装�?

Layer �?Service 的关系改为只�?Handle�?

```csharp
public readonly struct LayerServiceHandle
{
    public LayerServiceHandle(
        int scopeId,
        int objectSlot,
        LayerMembership membership)
    {
        ScopeId = scopeId;
        ObjectSlot = objectSlot;
        Membership = membership;
    }

    public int ScopeId { get; }

    public int ObjectSlot { get; }

    public LayerMembership Membership { get; }
}
```

Handle 用于调试、排序和事件路由，不用于直接�?Service 实例�?

---

## 3. MainScope

MainScope 必须和其�?Scope 使用相同�?

```text
Event Dispatcher
Call Dispatcher
Resource Plan
Service Slots
Lifecycle
Promise
Completion
```

不得存在�?

```text
MainScope 特殊 null Dispatcher
MainScope �?LayerRuntime EventCenter
MainScope �?Legacy ServiceProvider
```

---

## 验证

删除�?Usage，更�?README 示例为：

```csharp
LayerRuntime runtime =
    LayerRuntimeBuilder
        .Create()
        .Install<GeneratedModuleCatalog>()
        .Build();

runtime.Start();
runtime.Pump(deltaTime);
runtime.Dispose();
```

业务对象中：

```csharp
this.Scope<CombatScope>().Post(...);
await this.Scope<CombatScope>().Call(...);
```

提交�?

```bash
git commit -m "refactor: remove legacy layer business runtime"
```

---

# 十三、Task 9：所�?Scope 本地 API 增加状态守�?

统一方法�?

```csharp
private void RequireBusinessIngress(
    string apiName)
{
    ScopeRuntimeState state;

    lock (_lifecycleGate)
    {
        state = _state;
    }

    if (!state.AcceptsBusinessIngress())
    {
        throw new InvalidOperationException(
            $"Scope '{Descriptor.Name}' cannot execute '{apiName}' while in state '{state}'.");
    }
}
```

以下 API 必须�?StopRequested 后拒绝：

```text
TryPost
TryCall
SchedulePost
Delay
Timer.Schedule
GetScopeRef
手动 Pump Enqueue
GetOrCreateDelayPublisher
�?Scope Actor Event
```

`TryXxx` 返回明确枚举�?

```csharp
public enum ScopeIngressResult
{
    Accepted,
    Full,
    Stopping,
    Disposed
}
```

�?Try API 抛出明确异常�?

---

# 十四、Task 10：EventCenter �?Handler 路径

EventCenter 动态反�?fallback 保留�?

生成路径�?

```text
生成 EventTypeId
生成 Bucket Factory
生成 Handler Subscribe
生成 Dispatcher
```

反射 fallback 只在未知动态类型触发，并记录：

```text
ReflectionFallbackCount
LastFallbackType
OnReflectionFallback
```

修正文档�?

```text
EventCenter 不是全局 EventCenter
它是当前 ScopeRuntime 的本地事件中�?
```

Interface Handler fallback 目前仍通过�?

```text
GetInterfaces
GetGenericArguments
```

正式 Module 路径必须�?Generator 生成 Handler 绑定；反射接口扫描只能作为调试或动态兼容路径，并默认关闭�?

---

# 十五、Task 11：注释与 IL2CPP 验证

所有公开 Scope API 必须补充�?

```text
Owner Thread
允许调用线程
是否允许�?Scope
队列满行�?
StopRequested 后行�?
Dispose 后行�?
异常语义
背压语义
```

必须修正旧词�?

```text
全局事件中心
main thread completion
Layer 级业务资�?
�?Layer 共享字段
```

统一为：

```text
Scope-local
Scope owner thread
Runtime owner thread
generated scope resource
```

IL2CPP 验证�?

```text
Managed Stripping Level = High
不添加资源系�?link.xml
不添加反�?Preserve
EventCenter fallback 可单�?Preserve
```

检查正式资源、DI、Module 路径不依赖反射保活�?

---

# 十六、Task 12：最终回归测试与性能验收

## 生命周期

必须通过�?

```text
Start �?Stop 并发 10,000 �?
Start �?Dispose 并发 10,000 �?
Stop �?Dispose 并发 10,000 �?
多个 Stop 并发
多个 Dispose 并发
初始化抛异常
Worker Pump 抛异�?
Service Dispose 抛异�?
Context Dispose 抛异�?
```

每次测试必须满足�?

```text
Initialize 最多一�?
Stop Cleanup 恰好一�?
Dispose 恰好一�?
无死�?
�?ThreadStateException
无状态回退
```

---

## Promise

```text
所�?Call 最终得�?Result/Exception/Cancellation
Continuation 只在来源 Scope
Stop 不丢 Continuation
Completion 满时进入 Overflow
Completion Close 后不成功写入
Promise 完成�?OnCompleted 竞�?
迟到结果不覆�?Stop Exception
```

---

## Actor

```text
Worker 不直接调用共�?ActorWorld
Lifecycle 命令不丢�?
Lifecycle Inbox 满时可重�?
Runtime Dispose �?Drain Lifecycle
Actor Event Drop 释放 Payload
PostMany 稳态无数组分配
```

---

## Publish / From

```text
�?Scope 成功
�?Scope 失败
跨程序集成功
Key 错误失败
重复 Provider 失败
类型不兼容失�?
泛型 Owner Analyzer Error
private nested Owner Analyzer Error
Stop 时主�?Unbind
Unbind 异常进入 ExceptionHub
字段读取等同普通字�?
```

---

## Module

```text
不同 Runtime 不共�?Dispatcher
不同 Runtime 不共�?Factory
Module 数量相同也不串路�?
Build 失败不会 fallback
不依赖程序集扫描
```

---

## DI 性能

Benchmark�?

```csharp
[Benchmark]
public DamageService GeneratedSlotGet()
{
    return _provider.GetAt<DamageService>(3);
}
```

验收�?

```text
0 B/op
�?Dictionary Lookup
�?IsAssignableFrom
无反�?
```

---

## 资源读取性能

```csharp
[Benchmark]
public int DirectResourceRead()
{
    return _items.Count;
}
```

验收�?

```text
与普�?IReadOnlyList 字段读取处于同一量级
0 B/op
�?ScopeRead
无锁
```

---

## 构建命令

```bash
dotnet build LayerBase/LayerBase.csproj -c Release
dotnet build LayerBase.Generator/LayerBase.Generator/LayerBase.Generator.csproj -c Release
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release --no-build
```

禁止只运行单个测试后宣称完成�?

---

# 十七、最终删除检�?

全仓库搜索以下关键字，正式路径应为零结果�?

```text
ScopeRead<
ScopeResourceBinder
ModuleDispatchRegistry
ScopeHostFactory
ServiceLayerBinder
ScopeServiceOwnerRegistry
LegacyScope
assembly.GetType("GeneratedScopeResourceContributions")
GetCustomAttribute<ProvideAttribute>
GetCustomAttribute<FromAttribute>
InjectMembers(
LayerRuntime.GetService
Layer.GetService
Layer.RegisterService
```

以下关键字只允许出现�?EventCenter fallback�?

```text
MakeGenericType
Activator.CreateInstance
MethodInfo.Invoke
```

---

# 十八、提交顺�?

Agent 必须按以下顺序提交，禁止一个超大提交：

```text
1. test: add scope ownership and shutdown regressions
2. fix: make scope start stop and dispose atomic
3. fix: guarantee scope promise completion during shutdown
4. fix: make scope synchronization context closable
5. fix: route projected actor lifecycle through runtime owner
6. refactor: make scope resources fully generated
7. refactor: compose scope services with generated slots
8. refactor: isolate module plans per runtime
9. refactor: remove legacy layer business runtime
10. fix: reject work after scope stop request
11. docs: document scope ownership and shutdown contracts
12. test: complete scope runtime regression suite
```

每个提交必须满足�?

```text
对应新增测试通过
此前测试不回退
没有临时 fallback
没有注释掉的测试
没有 TODO
没有未使用的新架构文�?
```

---

# 十九、禁止的局部修复方�?

Agent 不得采取以下做法�?

```text
只在 Start 中再加一�?volatile bool
只在 Dispose �?Thread.Sleep
只给 Continuation 增加一次重�?
�?Worker Sweep 外加 lock(actorWorld)
给资源生成类型加 Preserve
保留反射 Binder 作为“暂时兼容�?
保留 Layer.GetService 转发 MainScope
保留静�?Module Registry 并增�?RuntimeId
通过 link.xml 保活字符串查�?Catalog
捕获异常后空 catch
```

原因�?

```text
这些方式会继续保留双模型、竞态窗口或 IL2CPP 隐式依赖�?
不能形成可证明的所有权边界�?
```

---

# 二十、完成定�?

只有同时满足以下条件，本轮重构才算完成：

```text
1. LayerRuntime 不再拥有任何业务 Event/Timer/ECS/Service 资源�?

2. 所�?IService �?ILayerContext 都有唯一 OwnerScope�?

3. MainScope 与其�?Scope 使用完全相同的资源和生命周期模型�?

4. Start、Stop、Dispose 任意并发不会交错释放资源�?

5. 所�?Promise 最终都得到 Result、Exception �?Cancellation�?

6. Continuation 不会在错误线程执行，也不会在关闭阶段丢失�?

7. Worker Scope 不直接读取或修改共享 ActorWorld�?

8. Publish/From 只走生成式直接字段绑定�?

9. Publish/From 运行时不使用反射、字典或 ScopeRead�?

10. Scope DI �?Mount 使用构建�?Slot�?

11. Module Dispatcher、Factory �?Catalog 都是 Runtime 实例状态�?

12. �?Layer/LayerRuntime 业务 API 已删除�?

13. EventCenter 动态反射只保留为明确的兼容慢路径�?

14. IL2CPP High Stripping 下不依赖资源�?DI �?link.xml 保活�?

15. 完整 Release 测试、并发压力测试和 Benchmark 全部通过�?
```

最终架构定义：

```text
LayerRuntime 负责组织、层级、共�?ActorWorld 和全局协调�?

ScopeRuntime 负责业务资源、对象、线程、时间、ECS 与生命周期�?

Publish / From 是同 Scope 内的生成式直接资源注入�?

�?Scope 只能使用 ScopeEvent、ScopeCall �?Actor Command�?
```

