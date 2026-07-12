# LayerBase Scope 并发、生命周期与运行时所有权整改方案

## 1. 改造目标

本次改造不改变 LayerBase 的核心抽象：

```text
LayerRuntime
    ├── Layer 层级与拓扑
    ├── 全局 ActorWorld
    ├── Runtime 级异常通道
    ├── Runtime 级工具
    └── ScopeRuntimeHost
            ├── MainScope
            ├── GameplayScope
            ├── NetworkScope
            └── 其他 Scope
```

改造后的职责必须满足：

* `LayerRuntime` 只管理跨 Scope 的全局资源和 Layer 层级。
* `ScopeRuntime` 独占自己的 EventCenter、PostScheduler、Timer、Delay、ECS World、Service、Context 和执行线程。
* 跨 Scope 或跨线程操作只能通过显式入口队列。
* 所有不可丢失的控制消息必须具备可靠完成语义。
* Generator 负责生成路由、服务槽位、Mount、订阅和 Module 组合。
* 热路径不进行反射、Type 字典查找或接口装箱。
* EventCenter 的动态反射仅作为未生成事件的兼容慢路径保留。

当前代码已经将 Scope Inbox 统一成加锁队列，并引入 `_ownsActorWorld`，方向正确。

---

# 2. 全局设计公理

## 2.1 Owner Thread 公理

每一份可变资源必须存在唯一 Owner：

```text
Scope EventCenter       -> Scope Owner Thread
Scope PostScheduler     -> Scope Owner Thread
Scope Timer             -> Scope Owner Thread
Scope ECS World         -> Scope Owner Thread
共享 ActorWorld         -> LayerRuntime Pump Thread
独立 ActorWorld         -> 创建它的 Scope Thread
```

Owner 外部不得直接访问资源。

现有 `RequireAccess()` 保留，并扩展为统一的 `ScopeAccessGuard`。

```csharp
internal static class ScopeAccessGuard
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequireOwner(
        ScopeRuntime scope,
        string apiName)
    {
        if (!ReferenceEquals(ScopeExecution.Current.Runtime, scope))
        {
            throw new ScopeAccessViolationException(
                scope.ScopeId,
                scope.Descriptor.Name,
                apiName);
        }
    }
}
```

以下 API 必须执行检查：

```text
Send
Post
MarkDirty
SchedulePost
Delay
GetService
Query
ECSWorld
Subscribe
Actor 本地访问
Context 本地资源访问
```

以下 API 是跨域入口，不要求调用者处于目标 Scope：

```text
this.Scope<T>().Post(...)
this.Scope<T>().Call(...)
ScopeRuntimeHost.TryPost(...)
ScopeRuntimeHost.TryCall(...)
ActorCommandInbox.TryPost(...)
```

---

## 2.2 不可丢失消息公理

消息分成两类。

### 可拒绝业务消息

```text
Scope Post
Scope Call 请求
Actor 普通事件
Manual Pump
```

允许有界队列、RejectNew、DropOldest 或其他背压策略。

但 Call 被拒绝时必须立即失败 Promise。

### 不可丢失控制消息

```text
Promise Continuation
Scope 停止通知
异步任务取消
Actor Disable
Actor Release
Actor Destroy
资源释放命令
```

这些消息不能因为普通业务队列满而直接丢弃，也不能转移到错误线程执行。

---

# 3. ScopeRuntime 生命周期状态机

## 3.1 删除分散生命周期字段

当前同时维护：

```text
_startRequested
_stopRequested
_stopCleanupCompleted
_stopped
_disposed
_workerRunning
```

它们应合并为一个状态机：

```csharp
internal enum ScopeRuntimeState
{
    Created = 0,
    Starting = 1,
    Running = 2,
    StopRequested = 3,
    Stopping = 4,
    Stopped = 5,
    Disposing = 6,
    Disposed = 7,
    Faulted = 8
}
```

`ScopeRuntime` 内保留：

```csharp
private readonly object _lifecycleGate = new();
private readonly ManualResetEventSlim _terminated = new(false);

private ScopeRuntimeState _state;
private bool _workerRunRequested;
private Thread? _workerThread;
```

生命周期不是帧热路径，因此应优先使用单一锁保证正确性，不需要用多个 CAS 字段拼装状态。

## 3.2 合法状态转换

```text
Created -> Starting -> Running
Created -> Stopping -> Stopped
Starting -> StopRequested -> Stopping -> Stopped
Running -> StopRequested -> Stopping -> Stopped
Running -> Faulted -> Stopping -> Stopped
Stopped -> Disposing -> Disposed
Created -> Disposing -> Disposed
```

禁止：

```text
Stopped -> Start
Disposed -> Start
Disposed -> Stop
Disposed -> Enqueue
```

## 3.3 Worker 启动握手

`Start()` 不再由调用线程直接设置 Running。

```csharp
public void Start()
{
    lock (_lifecycleGate)
    {
        RequireState(ScopeRuntimeState.Created);
        _state = ScopeRuntimeState.Starting;

        if (Descriptor.Threading != ScopeThreadingMode.Worker)
        {
            StartScopeOnOwnerThread();
            _state = ScopeRuntimeState.Running;
            return;
        }

        _workerRunRequested = true;
        _workerThread = new Thread(WorkerMain);
        _workerThread.Start();
    }
}
```

Worker 在线程内部完成：

```text
安装 ScopeExecution
安装 SynchronizationContext
StartScope
Starting -> Running
循环 Pump
进入停止阶段
统一 Cleanup
设置 _terminated
```

Stop 遇到 Starting 状态时只设置 `StopRequested`，不能 Join 一个尚未启动的 Thread。

## 3.4 Dispose 幂等性

`Dispose()` 必须在 `_lifecycleGate` 下完成状态占有：

```text
任意线程只能有一个线程进入 Disposing
其他线程等待 _terminated 或直接返回
```

资源释放顺序固定为：

```text
关闭外部入口
停止业务 Pump
取消当前 Scope 发出的未完成 Call
排空可靠 Continuation
停止 ECS Scheduler
释放订阅
清空 Delay
Dispose Context
Dispose Service
Dispose Scope ECS World
Dispose Scope 自有 ActorWorld
Dispose Timer/PostScheduler/EventCenter
Disposed
```

---

# 4. 可关闭队列模型

## 4.1 新增 IClosableQueue

当前 `TryPost()` 采用“检查 `_stopped` 后入队”，Stop 可以在两步之间插入。

改为：

```csharp
internal interface IClosableBoundedQueue<T>
{
    int Count { get; }
    bool IsClosed { get; }

    QueueEnqueueResult TryEnqueue(in T item);
    bool TryDequeue(out T item);

    void Close();
    void CloseAndDrain(Action<T> drain);
}
```

```csharp
internal enum QueueEnqueueResult
{
    Accepted,
    Full,
    Closed
}
```

`TryEnqueue`、`Close`、`CloseAndDrain` 必须在队列自身的同一个同步边界内完成。

## 4.2 Scope 队列划分

`ScopeRuntime` 调整为：

```csharp
private readonly IClosableBoundedQueue<ScopePostMessage> _postInbox;
private readonly IClosableBoundedQueue<ScopeCallMessage> _callInbox;
private readonly ReliableContinuationInbox _completionInbox;
private readonly IClosableBoundedQueue<float> _manualPumpInbox;
```

停止时首先原子关闭入口：

```text
_postInbox.Close()
_callInbox.Close()
_manualPumpInbox.Close()
```

之后任何 Enqueue 都得到 Closed，不可能在清理完成后写回队列。

## 4.3 StopPolicy 语义

`ScopeStopPolicy` 只控制业务消息：

### Drain

```text
关闭入口
派发已有 Post
派发已有 Call
停止接收新消息
```

### Drop

```text
关闭入口
丢弃 Post
所有未派发 Call -> ScopeStoppedException
```

Continuation 不参与 Drop。Continuation 必须被执行或通过 Scope Await Registry 转换为取消完成。

---

# 5. ScopePromise 与 Continuation

## 5.1 禁止跨 Scope inline continuation

当前代码在目标 continuation 队列满时直接执行 continuation。

这必须删除。

只要 Promise 存在来源 Scope：

```text
Continuation 只能在来源 Scope 的完成阶段运行
不能在目标 Scope 线程执行
不能在线程池执行
不能在调用 SetResult 的线程执行
```

## 5.2 ScopeAwaitRegistry

每个 Scope 新增：

```csharp
internal sealed class ScopeAwaitRegistry
{
    private readonly object _gate = new();
    private readonly HashSet<IScopePromiseControl> _pending = new();

    public bool TryRegister(IScopePromiseControl promise);
    public void Unregister(IScopePromiseControl promise);
    public void CancelAll(Exception reason);
    public void Close();
}
```

创建跨 Scope Call 时：

```text
读取 ScopeExecution.Current.Runtime
创建 ScopePromise
向来源 Scope AwaitRegistry 注册
成功后向目标 Scope 投递 Call
Call 完成后从来源 Registry 注销
```

来源 Scope 停止时：

```text
关闭 AwaitRegistry
CancelAll(new ScopeStoppedException(...))
将所有 Promise 转为失败完成
产生的 Continuation 进入可靠 CompletionInbox
排空 CompletionInbox
结束 Scope
```

目标 Scope 晚到的 `SetResult()` 会因为 Promise 已完成而被忽略。

## 5.3 ReliableContinuationInbox

Completion 路径不能 RejectNew。

采用两级结构：

```text
Fast Lane:
    固定容量 MPSC RingQueue

Overflow Lane:
    仅当 Fast Lane 满时使用的短锁分段队列
```

正常路径无额外分配；只有异常拥塞时进入 Overflow，并记录：

```text
ContinuationOverflowCount
ContinuationPeakCount
ContinuationLatency
```

Overflow 不能直接执行 continuation。

## 5.4 Promise 完成流程

```csharp
private void Complete(...)
{
    Action? continuation;

    lock (_gate)
    {
        if (_completed)
            return;

        _completed = true;
        ...
        continuation = _continuation;
    }

    _originRegistry?.Unregister(this);

    if (continuation != null)
    {
        _originScope!.CompletionInbox.EnqueueReliable(
            new LayerContinuation(continuation, ...));
    }
}
```

没有来源 Scope 的 Call 可以直接执行 continuation，保持当前外部调用行为。

---

# 6. SynchronizationContext 与 LBTask 关闭协议

## 6.1 Context 状态

新增：

```csharp
private enum ContextState
{
    Running,
    Closing,
    Closed
}
```

`Post`、`Send`、`ScheduleInFrames`、CompletionQueue.Enqueue 必须与关闭状态使用统一同步边界。

## 6.2 Send 原子注册

不能再使用：

```text
检查 _disposed
Enqueue
Wait
```

改为：

```csharp
public override void Send(
    SendOrPostCallback callback,
    object? state)
{
    if (IsOwnerThread)
    {
        RequireRunning();
        callback(state);
        return;
    }

    SendWorkItem work;

    lock (_stateGate)
    {
        if (_state != ContextState.Running)
            throw new ObjectDisposedException(...);

        work = SendWorkItem.Rent(callback, state);
        _queue.Enqueue(work);
    }

    work.WaitAndRethrow();
}
```

Dispose 在同一个 `_stateGate` 下先切换到 Closing，再关闭队列。

## 6.3 可取消 WorkItem

统一定义：

```csharp
internal interface IContextWorkItem
{
    void Execute();
    void Cancel(Exception reason);
}
```

以下项目都必须实现取消：

```text
SendWorkItem
LBTask NextFrame
LBTask RunOnMainThread
Background Completion
FrameWorkItem
```

Dispose 时：

```text
关闭普通队列
取消 FrameWork
取消未执行 Send
关闭 CompletionQueue
取消迟到的后台完成
Closed
```

不得直接 `_frameWork.Clear()`。

## 6.4 MainThreadCompletionQueue 重命名

该队列不仅用于主线程，也用于 Worker Scope 安装线程。

重命名为：

```text
ScopeCompletionQueue
```

对应：

```csharp
public sealed class ScopeCompletionQueue : IDisposable
{
    public CompletionEnqueueResult TryEnqueue(
        IContextCompletion completion);

    public CompletionDrainStats Drain(...);

    public void CloseAndCancel(Exception reason);
}
```

---

# 7. ActorWorld 所有权与命令通道

## 7.1 ActorWorld 唯一所有权

共享模式下：

```text
LayerRuntime
    唯一 Prepare
    唯一 CompleteBuild
    唯一 Pump
    唯一 Dispose
```

Scope 只能持有 `ScopeActorGateway`。

独立 Scope 模式下，Scope 可以拥有自己的 ActorWorld。

## 7.2 禁止 ProjectedActor 直接访问共享 ActorWorld

当前 `SweepProjectedActors()` 仍会直接操作共享 ActorWorld。

`ActiveProjectedActorList.Sweep` 中的这些调用必须移除：

```text
TryGetPooledActor
DisableProjectedActor
ReleaseProjectedActor
```

修改为：

```csharp
internal void Sweep(
    World world,
    IProjectedActorCommandSink commandSink,
    int maxCount);
```

Scope 只根据 ECS 元数据生成命令：

```text
DisableProjectedActorCommand
ReleaseProjectedActorCommand
DestroyProjectedActorCommand
DetachProjectedActorCommand
```

命令由 LayerRuntime Actor Owner Thread 执行。

## 7.3 Actor 命令分双通道

### ActorEventInbox

用于：

```text
PostTo
PostToMany
```

特征：

* 有界；
* 可背压；
* 可统计拒绝；
* 不允许无限增长。

### ActorLifecycleInbox

用于：

```text
Disable
Release
Destroy
Detach
```

特征：

* 可靠；
* 不允许 Drop；
* Fast Ring + Overflow；
* LayerRuntime Dispose 前必须排空。

## 7.4 删除接口装箱

当前 `ConcurrentQueue<IRuntimeActorCommand>` 会让 struct Command 装箱。

目标模型：

```csharp
internal readonly struct ActorCommandEnvelope
{
    public readonly ActorCommandKind Kind;
    public readonly ActorId ActorId;
    public readonly int RouteId;
    public readonly int PayloadHandle;
}
```

事件负载进入生成式 typed payload storage：

```text
ActorCommandPayloadStorage<TEvent>
```

队列中只保存 Envelope。

`PostToMany` 的 ActorId 集合使用 `ArrayPool<ActorId>` 或项目现有池，不再直接 `ToArray()`。当前实现每次都会复制数组。

## 7.5 LayerRuntime Pump 顺序

```text
ScopeHost.Pump
Drain ActorLifecycleInbox
Drain ActorEventInbox
ActorWorld.Pump
TryDrainExceptions
```

Dispose 顺序：

```text
关闭 ActorEventInbox
关闭 ActorLifecycleInbox
停止 ScopeHost
排空 ActorLifecycleInbox
丢弃剩余 Actor Event
ActorWorld.RuntimeStop
ActorWorld.Dispose
```

---

# 8. Module 与 Dispatcher 去全局静态化

## 8.1 删除的全局注册表

最终删除：

```text
ScopeHostFactory.s_factory
ModuleDispatchRegistry.s_callDispatchers
ModuleDispatchRegistry.s_eventDispatchers
ModuleCatalogRegistry.s_modules
```

当前这些对象都是进程级可覆盖状态。

## 8.2 GeneratedModuleCatalog 显式安装

不再依赖静态构造函数碰巧执行。

Bootstrap 使用：

```csharp
using var runtime = LayerHub
    .CreateLayers()
    .Push(new GameplayLayer())
    .Install<GeneratedModuleCatalog>()
    .Build();
```

只需要一个生成类型，不需要用户逐个列出 Module。

定义：

```csharp
public interface ILayerBaseModuleCatalog
{
    ILayerBaseModule[] CreateModules();
}
```

Generator 生成：

```csharp
public sealed class GeneratedModuleCatalog
    : ILayerBaseModuleCatalog
{
    public ILayerBaseModule[] CreateModules()
    {
        return new ILayerBaseModule[]
        {
            CombatModule.Instance,
            NetworkModule.Instance,
            InventoryModule.Instance
        };
    }
}
```

`Install<TCatalog>()` 直接构造生成 Catalog，不使用反射扫描或静态 Registry。

## 8.3 Module Runtime Plan

每个 Runtime 构建自己的：

```csharp
internal sealed class ModuleRuntimePlan
{
    public ModuleSlot[] Modules;
    public ScopeRuntimePlan[] Scopes;
    public ModuleEventDispatchHandler[] EventDispatchers;
    public ModuleCallDispatchHandler[] CallDispatchers;
    public ServiceFactory[] ServiceFactories;
    public ContextFactory[] ContextFactories;
}
```

Dispatcher 由 `ScopeRuntimeHost` 实例持有。

不同 Runtime 即使 Module 数量相同，也不会共享 Dispatcher 数组。

## 8.4 Event 注册入口

每个生成 Module 增加：

```csharp
void RegisterEventTypes();
void Prewarm(EventCenter center, in LayerPrewarmOptions options);
```

Runtime Build 时逐 Module 调用。

这保证业务程序集中的已知事件会主动进入生成式 Event 注册路径，而不是依赖其他程序集同名静态类型被触发。

---

# 9. EventCenter 动态反射兜底保留规则

以下实现保留：

```text
MakeGenericType
Activator.CreateInstance
反射读取 EventTypeId<T>.Id
```

当前路径位于 `GetBucket(Type)` 和 `GetEventTypeId(Type)`。

但需要明确分层。

## 9.1 快路径

事件被 Generator 发现：

```text
Module.RegisterEventTypes
    -> EventCenter.RegisterEventType<T>
    -> 生成式 Factory
    -> 不使用反射
```

## 9.2 兼容慢路径

事件未被 Generator 发现：

```text
GetBucket(Type)
    -> 动态反射构造
```

## 9.3 可观测性

Debug 模式记录：

```csharp
public int ReflectionFallbackCount { get; }
public event Action<Type>? OnReflectionFallback;
```

第一次回退时报告：

```text
Event type X used EventCenter reflection fallback.
Add [PrewarmEvent] or ensure it appears in a generated event contract
to move it onto the generated fast path.
```

不报 Error，不阻止构建，不删除兜底。

---

# 10. Scope 路由和 Service 热路径

## 10.1 ScopeTypeRouteCache 恢复真正缓存

当前 `ScopeTypeRouteCache<TScope>` 每次仍查询 RouteTable。

利用 LayerBase 已有最多 256 Runtime 的约束，生成：

```csharp
internal static class ScopeTypeRouteCache<TScope>
{
    private static readonly long[] Entries = new long[256];

    public static int Resolve(LayerRuntime runtime);
}
```

缓存键：

```text
RuntimeId + RuntimeGeneration
```

缓存值：

```text
ScopeId
```

`RuntimeGeneration` 使用全局单调递增值，RuntimeId 即使被复用，也不会命中旧缓存。

可以将：

```text
generation : 32 bit
scopeId    : 32 bit
```

打包到一个 `long`，使用 Volatile 读写。

## 10.2 ScopeServiceProvider 改为 Slot

当前实现仍为 `Dictionary<Type, object>`，失败后线性扫描。

目标结构：

```csharp
internal sealed class ScopeServiceProvider
{
    private readonly object[] _instances;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetAt<T>(int slot)
        where T : class
    {
        return (T)_instances[slot];
    }
}
```

Generator 为每种访问生成：

```csharp
internal static class ScopeServiceSlot<T>
{
    public static int Resolve(ScopeRuntime scope);
}
```

接口和基类映射在 Module Build 阶段展开：

```text
ICombatService -> CombatService slot
CombatService  -> CombatService slot
IService       -> 不生成，避免歧义
```

同一个 Scope 存在多个接口实现时，构建阶段直接报歧义错误。

## 10.3 删除 Scope DI 反射注入

当前 `InjectMembers()` 扫描字段、属性并执行 `SetValue()`。

模块模式下完全删除这条路径。

Generator 为带 `[Mount]` 的 Service/Context 生成：

```csharp
public partial class CombatService
    : IGeneratedScopeMount
{
    void IGeneratedScopeMount.Mount(
        in ScopeMountContext context)
    {
        _damageService = context.GetAt<DamageService>(3);
        _combatContext = context.GetAt<CombatContext>(7);
    }
}
```

约束：

* 使用 `[Mount]` 的类型必须是 `partial`；
* 不可写属性直接 Generator Error；
* 找不到依赖直接 Generator Error；
* 多个候选实现直接 Generator Error；
* Runtime 不做反射兜底。

## 10.4 删除运行时接口订阅扫描

当前 `BindInterfaceEventHandlers()` 仍调用 `GetInterfaces()` 和泛型接口反射。

统一由 `IAutoScopeSubscribe` 生成代码完成。

未生成的 `IEventHandler<T>` 实现不再运行时自动扫描，应由 Analyzer 报错提示类型必须参与 Generator。

---

# 11. 文件结构调整

## 新增

```text
LayerBase/Scope/Lifecycle/ScopeRuntimeState.cs
LayerBase/Scope/Lifecycle/ScopeLifecycleController.cs
LayerBase/Scope/Queue/IClosableBoundedQueue.cs
LayerBase/Scope/Queue/ClosableLockedRingQueue.cs
LayerBase/Scope/Completion/ReliableContinuationInbox.cs
LayerBase/Scope/Completion/ScopeAwaitRegistry.cs
LayerBase/Scope/Completion/IScopePromiseControl.cs

LayerBase.Task/Context/ContextState.cs
LayerBase.Task/Context/IContextWorkItem.cs
LayerBase.Task/Context/ScopeCompletionQueue.cs

LayerBase/Actor/RuntimeCommands/ActorCommandEnvelope.cs
LayerBase/Actor/RuntimeCommands/ActorEventInbox.cs
LayerBase/Actor/RuntimeCommands/ActorLifecycleInbox.cs
LayerBase/Actor/RuntimeCommands/ActorCommandPayloadStorage.cs

LayerBase/Modules/ILayerBaseModuleCatalog.cs
LayerBase/Modules/ModuleRuntimePlan.cs
LayerBase/Modules/ModuleRuntimePlanBuilder.cs

LayerBase/Scope/Routing/ScopeTypeRouteCache.cs
LayerBase/Scope/DI/ScopeServiceSlot.cs
LayerBase/Scope/DI/IGeneratedScopeMount.cs
```

## 主要修改

```text
LayerBase/Scope/ScopeRuntime.cs
LayerBase/Scope/ScopePromise.cs
LayerBase/Scope/ScopeRuntimeHost.cs
LayerBase/Scope/ScopeActorGateway.cs
LayerBase/Scope/ScopeRouteTable.cs
LayerBase/Scope/ScopeServiceProvider.cs
LayerBase/Scope/ScopeSubscriptionRegistry.cs

LayerBase/Application/LayerRuntime.cs
LayerBase/Application/LayerRuntime.ActorCommands.cs
LayerBase/Application/LayerHub.cs

LayerBase.Task/LayerBaseSynchronizationContext.cs
LayerBase.Task/LBTask.cs
LayerBase.Task/MainThreadCompletionQueue.cs

LayerBase/ECS/Projection/World.Projection.cs
LayerBase/ECS/Projection/ActiveProjectedActorList.cs

LayerBase.Generator/AssemblyModuleGenerator.cs
LayerBase.Generator/ModuleCatalogGenerator.cs
LayerBase.Generator/ScopeRuntimeHostGenerator.cs
LayerBase.Generator/EventPrewarmGenerator.cs
```

## 删除

```text
LayerBase/Scope/ScopeHostFactory.cs
LayerBase/Scope/ModuleDispatchRegistry.cs
LayerBase/Scope/ModuleCatalogRegistry.cs
```

---

# 12. 实施顺序

## 阶段一：并发正确性

1. 实现 `IClosableBoundedQueue`。
2. Scope Post、Call、ManualPump 改成可关闭队列。
3. 实现统一 Scope 生命周期状态机。
4. 修复 Stop/Start/Dispose 竞态。
5. 加入 ScopeAwaitRegistry。
6. 删除 Promise 的跨线程 inline fallback。
7. Context、FrameWork、CompletionQueue 实现关闭取消协议。

完成该阶段后，必须保证不存在永久 Pending Promise 或 Send。

## 阶段二：Actor 所有权

1. ProjectedActor 生命周期改为命令。
2. Actor 命令分 Event/Lifecycle 通道。
3. LayerRuntime 成为共享 ActorWorld 唯一操作者。
4. 删除 `ConcurrentQueue<IRuntimeActorCommand>`。
5. 引入 typed payload storage 和数组池。

## 阶段三：Module 实例化

1. 引入 `ILayerBaseModuleCatalog`。
2. 增加 `.Install<TCatalog>()`。
3. Dispatcher、Factory 和 Catalog 移入 ModuleRuntimePlan。
4. 删除三个全局静态 Registry。
5. Module 显式注册 Event/AOT 类型。

## 阶段四：热路径收口

1. Scope route 使用 RuntimeGeneration 缓存。
2. Service 使用 Slot 数组。
3. Mount 改成生成代码。
4. Interface Event Handler 改成生成代码。
5. 加入热路径 Benchmark。

## 阶段五：文档与注释

补充公共 API 的：

```text
Owner Thread
允许调用线程
背压行为
关闭行为
异常行为
Dispose 幂等性
```

修正：

```text
EventCenter “全局事件中心”
SynchronizationContext “main thread”
ScopeTypeRouteCache “避免 Type 查找”
```

---

# 13. 必须通过的测试

## Scope 并发

```text
Stop 与 TryPost 并发 100 万次，不存在 Stop 后残留消息
Stop 与 TryCall 并发，所有 Promise 最终完成
Start 与 Stop 并发，不出现未启动 Thread Join
两个线程并发 Dispose，只释放一次
Stopped Scope 无法再次 Start
```

## Promise

```text
来源 Scope 停止时，所有 outbound Call 取消
目标 Scope 晚到 SetResult 不重复完成
Continuation Inbox 满时不在目标线程 inline
Continuation 必须在来源 ScopeExecution 中执行
```

## SynchronizationContext

```text
Send 与 Dispose 并发不永久等待
NextFrame 与 Dispose 并发得到取消
后台任务在 Context Dispose 后完成时不会遗留 Completion
FrameWork Dispose 时不直接丢失 TaskSource
```

## Actor

```text
Worker Scope 不直接调用共享 ActorWorld
ProjectedActor Disable 在 Runtime Owner Thread 执行
ProjectedActor Release 在 Runtime Owner Thread 执行
ActorLifecycleInbox 饱和时命令不丢失
PostToMany 稳态零数组分配
```

## Module

```text
两个 Runtime 安装不同 Module 集合且数量相同，不混用 Dispatcher
多个 Runtime 并行 Build 不覆盖 Scope Factory
Install<TCatalog>() 不依赖静态构造或程序集扫描
Event 注册由各 Module 显式执行
```

## 性能

```text
this.Scope<T>() 稳态无 Dictionary<Type,...> 查询
GetService<T>() 稳态为数组 Slot 访问
Scope Post 稳态零分配
Scope Call 除 PromiseSource 外无 DTO 装箱
Actor Post 稳态无接口装箱
```

---

# 14. 验收标准

本次改造完成的定义是：

1. Scope 停止后，任何入口都不可能再次写入其队列。
2. 所有 Call 最终必然得到 Result、Exception 或 Cancellation。
3. 所有 Continuation 都在正确 Scope 执行。
4. Dispose 不会因为并发调用而重复释放资源。
5. Worker Scope 不直接读写共享 ActorWorld。
6. Module、Dispatcher 和 Scope Factory 不再是进程级可覆盖状态。
7. Scope 路由、Service 获取和 Mount 不再依赖热路径 Type 字典或运行时反射。
8. EventCenter 动态反射兜底继续存在，但生成式事件默认走无反射快路径。
9. 公共 API 明确标记线程、背压、停止和释放契约。
