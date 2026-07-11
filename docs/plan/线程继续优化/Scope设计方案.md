# LayerBase Scope 运行域最终设计

## 方案 A：设计方案重写版

### A.0 总体结论

LayerBase 新设计不应该是：

```text
LayerRuntime
  -> 全局 ServiceContainer
  -> 全局 EventCenter
  -> 全局 EcsWorld
  -> 全局 Scheduler
```

而应该是：

```text
LayerRuntime
  -> ActorWorld                         // 每个 LayerRuntime 唯一
  -> ScopeRuntime[0] MainScope
  -> ScopeRuntime[1] CombatScope
  -> ScopeRuntime[2] NetScope
  -> ScopeRuntime[N] UserDefinedScope
```

每个 `ScopeRuntime` 才是真正的资源独立域：

```text
ScopeRuntime
  -> IService[]
  -> EventCenter
  -> PostScheduler
  -> TimeScheduler
  -> LBTaskScheduler
  -> EcsWorld
  -> EcsScheduler
  -> QueryBatchScheduler
  -> CommandBatchQueue
  -> ScopeInbox
  -> ScopeCallQueue
  -> ContinuationQueue
  -> ActorEventOutbox
```

核心原则：

```text
1. Service 沿用当前 IService 接口，不新建 ScopeService。
2. Service 的运行归属是 ScopeRuntime，不是 LayerRuntime。
3. LayerRuntime 不直接管理 Service 生命周期，只调度 ScopeRuntime。
4. ScopeRuntime 管理自己内部 IService 的 Start / Stop / Event / Call / Task。
5. 每个 ScopeRuntime 可以有自己的 EcsWorld。
6. ActorWorld 每个 LayerRuntime 唯一。
7. Scope 内通讯走本 Scope 的 EventCenter / PostScheduler / TimeScheduler。
8. 跨 Scope 通讯走 Scope.Post / awaitable Scope.Call。
9. 热路径不用 Dictionary。
10. 热路径不用 ConcurrentQueue。
11. 所有运行期路由尽量使用 Source Generator 生成的 ID + 数组表。
12. 所有队列优先使用有限容量 RingBuffer。
```

---

## A.1 重新定义 LayerRuntime

`LayerRuntime` 的职责被收窄。

它负责：

```text
1. 创建所有 ScopeRuntime。
2. 持有唯一 ActorWorld。
3. 提供 Scope 之间的路由表。
4. 启动 / 停止所有 ScopeRuntime。
5. 在 Inline 模式下按顺序 Pump ScopeRuntime。
6. 汇总 Diagnostics。
```

它不应该直接负责：

```text
1. Service 生命周期。
2. Service 查找。
3. EventCenter 派发。
4. EcsWorld 操作。
5. QueryBatch 执行。
6. Scope 内 LBTask continuation。
```

这些全部下放给 `ScopeRuntime`。

---

## A.2 重新定义 ScopeRuntime

`ScopeRuntime` 是 LayerRuntime 下的独立资源域。

它负责：

```text
1. 持有本 Scope 的 IService[]。
2. 管理本 Scope 的 IService 生命周期。
3. 持有本 Scope 的 EventCenter。
4. 持有本 Scope 的 PostScheduler。
5. 持有本 Scope 的 TimeScheduler。
6. 持有本 Scope 的 LBTaskScheduler。
7. 持有本 Scope 的 EcsWorld。
8. 持有本 Scope 的 EcsScheduler。
9. 持有本 Scope 的跨域输入队列。
10. 持有本 Scope 的 Call 队列。
11. 持有本 Scope 的 continuation 队列。
12. 持有本 Scope 的 ActorEventOutbox。
13. Worker 模式下持有内部线程。
```

重点：

```text
Service 所属 ScopeRuntime。
不是 LayerRuntime。
```

所以：

```text
IService.OnStart()
IService.OnStop()
IService.OnEvent()
IService.OnCall()
IService 内部 PostScheduler
IService 内部 TimeScheduler
IService 内部 LBTask continuation
```

都应该在它所属的 `ScopeRuntime` 上执行。

---

## A.3 ScopeOptions 继续使用 Attribute

不引入 `IScope`。

```csharp
[ScopeOptions(
    threading: ScopeThreadingMode.Worker,
    clock: ScopeClockMode.FixedRate,
    tickRateHz: 60,
    stopPolicy: ScopeStopPolicy.Drain)]
public sealed class CombatScope
{
}
```

主 Scope 是内建的：

```text
MainScope
  threading = Inline
  clock = EngineDriven
  tickRateHz = 0
  stopPolicy = Drain
```

没有 `[Scope<T>]` 的 Service 默认进入 MainScope。

---

## A.4 Service 归属规则

沿用当前 `IService`。

新增：

```csharp
[Scope<CombatScope>]
public sealed class BulletCollisionService : IService
{
}
```

规则：

```text
1. [Scope<T>] 标记 IService 的 Scope 归属。
2. 无 [Scope<T>] 的 IService 默认属于 MainScope。
3. IService 不能同时归属多个 Scope。
4. IService 不能跨 Scope 直接 GetService。
5. IService 如果要和其他 Scope 交互，只能 Scope.Post / Scope.Call。
```

---

## A.5 LayerContext 规则

`LayerContext` 是用户自定义功能 Manager 聚合，不是运行时工具门面。

例如：

```text
LayerContext
  -> InventoryManager
  -> SaveManager
  -> SceneManager
  -> UIManager
  -> ConfigManager
```

不要把这些放进 LayerContext：

```text
EventCenter
PostScheduler
TimeScheduler
LBTaskScheduler
EcsWorld
ScopeInbox
ScopeCallRouter
```

这些属于 `ScopeRuntime`。

---

## A.6 Scope 内通讯和跨 Scope 通讯必须分开

### Scope 内通讯

同一个 ScopeRuntime 内部通讯使用：

```text
EventCenter
PostScheduler
TimeScheduler
LBTaskScheduler
```

例如：

```text
CombatService
  -> EventCenter.Publish(DamageEvent)

BuffService
  -> 同 Scope 内监听 DamageEvent
```

这不需要走跨域队列。

### 跨 Scope 通讯

不同 ScopeRuntime 之间通讯使用：

```text
Scope.Post
Scope.Call
```

例如：

```text
MainScope
  -> Scope<CombatScope>().Post(...)
  -> Scope<CombatScope>().Call(...)
```

跨 Scope 禁止：

```text
1. 直接拿对方 EventCenter。
2. 直接拿对方 IService。
3. 直接拿对方 EcsWorld。
4. 直接拿对方 Scheduler。
5. 直接拿对方用户 Manager。
```

---

## A.7 Call 的最终语义

`Call` 不是阻塞式调用。

它是：

```text
awaitable message call
```

流程：

```text
MainScope
  await Scope<CombatScope>().Call(...)

CombatScope
  执行 Call handler
  SetResult

Promise
  不直接执行 continuation
  把 continuation 投递回 MainScope continuation queue

MainScope
  Pump continuation
  await 后面的代码继续执行
```

禁止：

```text
Call(...).Result
Call(...).Wait()
```

因为这会产生死锁、主线程阻塞、Worker 卡死。

---

## A.8 资源传输规则

默认不强制 `Moved<T>`。

允许跨 Scope 直接传 class。

但是：

```text
LayerBase 保证代码在哪个 Scope 执行。
LayerBase 不保证传入的 class 天然线程安全。
```

因此：

```text
1. 开发者强行把可变 class 放进 Call / Post / Response，线程安全自负。
2. Analyzer 默认 warning。
3. 严格模式可以把 warning 升级为 error。
4. [CrossScopeSafe] / [ImmutableResource] / [UnsafeCrossScope] 可消除或调整诊断。
```

---

## A.9 高性能路线

运行期热路径必须避免：

```text
Dictionary<Type, ...>
ConcurrentQueue<T>
反射 Invoke
Type 查找
字符串查找
动态分配过多 Envelope
```

替代方案：

```text
1. Source Generator 分配 ScopeId。
2. Source Generator 分配 ServiceId。
3. Source Generator 分配 EventId。
4. Source Generator 分配 CallId。
5. 运行期使用数组表。
6. 跨域消息使用有限容量 RingBuffer。
7. Call 使用 PromisePool。
8. Envelope 使用 struct 或池化 class。
9. Service 查找使用 ScopeRuntime.Services[serviceId]。
10. Event 派发使用 EventId -> handler range。
```

---

# 方案 B：最终代码设计版

## B.0 代码设计总览

运行时核心结构：

```text
LayerRuntime
  ScopeRuntime[] _scopes
  ActorWorld _actorWorld
  ScopeRouteTable _routes

ScopeRuntime
  int ScopeId
  ScopeDescriptor Descriptor
  IService[] Services
  EventCenter EventCenter
  PostScheduler PostScheduler
  TimeScheduler TimeScheduler
  LBTaskScheduler LBTaskScheduler
  EcsWorld EcsWorld
  EcsScheduler EcsScheduler
  BoundedRingQueue<ScopePostMessage> PostInbox
  BoundedRingQueue<ScopeCallMessage> CallInbox
  BoundedRingQueue<LBContinuation> ContinuationQueue
  BoundedRingQueue<ActorEventBatch> ActorEventOutbox
```

---

## B.1 Attribute 与枚举

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScopeOptionsAttribute : Attribute
{
    // Threading：
    // 当前 Scope 的线程模式。
    // Inline 表示由 LayerRuntime 主调度同步执行。
    // Worker 表示由 ScopeRuntime 内部线程执行。
    public ScopeThreadingMode Threading { get; }

    // Clock：
    // 当前 Scope 的时钟模式。
    // EngineDriven 表示由引擎 Update 驱动。
    // FixedRate 表示固定频率驱动。
    // Realtime 表示真实时间驱动。
    // Manual 表示手动推进。
    public ScopeClockMode Clock { get; }

    // TickRateHz：
    // 当前 Scope 的目标 Tick 频率。
    // FixedRate 模式下必须大于 0。
    public int TickRateHz { get; }

    // StopPolicy：
    // 当前 Scope 停止时的队列处理策略。
    // Drain 表示消费完当前队列。
    // Drop 表示直接丢弃。
    public ScopeStopPolicy StopPolicy { get; }

    public ScopeOptionsAttribute(
        ScopeThreadingMode threading = ScopeThreadingMode.Inline,
        ScopeClockMode clock = ScopeClockMode.EngineDriven,
        int tickRateHz = 0,
        ScopeStopPolicy stopPolicy = ScopeStopPolicy.Drain)
    {
        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        StopPolicy = stopPolicy;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ScopeAttribute<TScope> : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ScopeCallAttribute : Attribute
{
}

public enum ScopeThreadingMode
{
    Inline,
    Worker
}

public enum ScopeClockMode
{
    EngineDriven,
    FixedRate,
    Realtime,
    Manual
}

public enum ScopeStopPolicy
{
    Drain,
    Drop
}
```

---

## B.2 生成 ID 设计

Source Generator 生成：

```csharp
internal static class __LbScopeIds
{
    // Main：
    // 内建 MainScope 的 ScopeId。
    public const int Main = 0;

    // Combat：
    // 用户定义 CombatScope 的 ScopeId。
    public const int Combat = 1;

    // Net：
    // 用户定义 NetScope 的 ScopeId。
    public const int Net = 2;

    // Count：
    // Scope 总数。
    public const int Count = 3;
}
```

ServiceId：

```csharp
internal static class __LbServiceIds
{
    // GameFlowService：
    // MainScope 内 GameFlowService 的 ServiceId。
    public const int GameFlowService = 0;

    // BulletCollisionService：
    // CombatScope 内 BulletCollisionService 的 ServiceId。
    public const int BulletCollisionService = 1;

    // Count：
    // Service 总数。
    public const int Count = 2;
}
```

CallId：

```csharp
internal static class __LbCallIds
{
    // BulletTickCall：
    // BulletTickCall 对应的 CallId。
    public const int BulletTickCall = 0;

    // Count：
    // Call 类型数量。
    public const int Count = 1;
}
```

EventId 同理。

运行期不应该在热路径用：

```text
typeof(T)
Dictionary<Type, int>
```

而应通过生成代码把泛型调用映射成 ID。

---

## B.3 ScopeDescriptor 数组

生成：

```csharp
internal static class __LbScopeTable
{
    public static readonly ScopeDescriptor[] Descriptors =
    {
        new ScopeDescriptor(
            scopeId: __LbScopeIds.Main,
            name: "MainScope",
            threading: ScopeThreadingMode.Inline,
            clock: ScopeClockMode.EngineDriven,
            tickRateHz: 0,
            stopPolicy: ScopeStopPolicy.Drain),

        new ScopeDescriptor(
            scopeId: __LbScopeIds.Combat,
            name: "CombatScope",
            threading: ScopeThreadingMode.Worker,
            clock: ScopeClockMode.FixedRate,
            tickRateHz: 60,
            stopPolicy: ScopeStopPolicy.Drain),

        new ScopeDescriptor(
            scopeId: __LbScopeIds.Net,
            name: "NetScope",
            threading: ScopeThreadingMode.Worker,
            clock: ScopeClockMode.Realtime,
            tickRateHz: 30,
            stopPolicy: ScopeStopPolicy.Drop),
    };
}
```

`ScopeDescriptor`：

```csharp
public readonly struct ScopeDescriptor
{
    // ScopeId：
    // Scope 的运行期整数编号。
    public readonly int ScopeId;

    // Name：
    // Scope 的诊断名称。
    public readonly string Name;

    // Threading：
    // Scope 的线程模式。
    public readonly ScopeThreadingMode Threading;

    // Clock：
    // Scope 的时钟模式。
    public readonly ScopeClockMode Clock;

    // TickRateHz：
    // Scope 的目标 Tick 频率。
    public readonly int TickRateHz;

    // StopPolicy：
    // Scope 停止策略。
    public readonly ScopeStopPolicy StopPolicy;

    public ScopeDescriptor(
        int scopeId,
        string name,
        ScopeThreadingMode threading,
        ScopeClockMode clock,
        int tickRateHz,
        ScopeStopPolicy stopPolicy)
    {
        ScopeId = scopeId;
        Name = name;
        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        StopPolicy = stopPolicy;
    }
}
```

---

## B.4 有限环形队列

### B.4.1 设计目标

所有热路径队列使用有限容量 RingBuffer。

RingBuffer 是环形队列：

```text
固定数组 + head + tail。
写到末尾后回到数组开头。
不扩容。
不分配。
容量满时按策略处理。
```

优点：

```text
1. 内存稳定。
2. 不触发扩容。
3. 不依赖链表节点分配。
4. Cache 友好。
5. 适合游戏运行时。
```

---

### B.4.2 单线程 Local RingQueue

用于 Inline -> Inline 或 Scope 内部队列。

```csharp
public sealed class LocalRingQueue<T>
{
    // _buffer：
    // 固定容量数组。
    // 队列不会自动扩容。
    private readonly T[] _buffer;

    // _head：
    // 下一个读取位置。
    private int _head;

    // _tail：
    // 下一个写入位置。
    private int _tail;

    // _count：
    // 当前队列元素数量。
    private int _count;

    public int Count => _count;

    public int Capacity => _buffer.Length;

    public LocalRingQueue(int capacity)
    {
        // capacity：
        // 队列容量。
        // 必须大于 0。
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _buffer = new T[capacity];
    }

    public bool TryEnqueue(T item)
    {
        // item：
        // 要写入队列的元素。
        // 如果队列满，返回 false。
        if (_count == _buffer.Length)
        {
            return false;
        }

        _buffer[_tail] = item;
        _tail = (_tail + 1) % _buffer.Length;
        _count++;

        return true;
    }

    public bool TryDequeue(out T item)
    {
        // item：
        // 出队结果。
        // 如果队列为空，返回 false。
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];
        _buffer[_head] = default!;
        _head = (_head + 1) % _buffer.Length;
        _count--;

        return true;
    }

    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _head = 0;
        _tail = 0;
        _count = 0;
    }
}
```

---

### B.4.3 跨线程 MPSC RingQueue

用于多个 Scope 向一个 Worker Scope 投递消息。

第一版可以先做带轻量锁的 bounded ring queue，避免过早写复杂无锁结构。

```csharp
public sealed class LockedBoundedRingQueue<T>
{
    // _buffer：
    // 固定容量数组。
    private readonly T[] _buffer;

    // _gate：
    // 队列锁。
    // 锁粒度只包住入队和出队，不包住 handler 执行。
    private readonly object _gate = new object();

    private int _head;
    private int _tail;
    private int _count;

    public int Capacity => _buffer.Length;

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _count;
            }
        }
    }

    public LockedBoundedRingQueue(int capacity)
    {
        // capacity：
        // 队列容量。
        // 必须大于 0。
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _buffer = new T[capacity];
    }

    public bool TryEnqueue(T item)
    {
        // item：
        // 要写入队列的消息。
        // 如果队列满，返回 false。
        lock (_gate)
        {
            if (_count == _buffer.Length)
            {
                return false;
            }

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;

            return true;
        }
    }

    public bool TryDequeue(out T item)
    {
        // item：
        // 出队消息。
        // 如果队列为空，返回 false。
        lock (_gate)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _buffer[_head];
            _buffer[_head] = default!;
            _head = (_head + 1) % _buffer.Length;
            _count--;

            return true;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}
```

说明：

```text
第一版可以接受短锁，因为锁只保护队列数组。
不允许在锁内执行 Service handler。
后续如果性能需要，再替换为无锁 MPSC ring queue。
```

---

## B.5 消息结构设计

避免泛型对象在热路径反射。

统一使用 `ScopeMessage`：

```csharp
public readonly struct ScopePostMessage
{
    // EventId：
    // 事件类型 ID。
    // 由 Source Generator 生成。
    public readonly int EventId;

    // Payload：
    // 消息内容。
    // 第一版允许 object。
    // 后续可以对 struct 做池化或泛型专用队列。
    public readonly object Payload;

    public ScopePostMessage(
        int eventId,
        object payload)
    {
        EventId = eventId;
        Payload = payload;
    }
}
```

Call 消息：

```csharp
public readonly struct ScopeCallMessage
{
    // CallId：
    // Call 类型 ID。
    // 由 Source Generator 生成。
    public readonly int CallId;

    // Payload：
    // Call 参数对象。
    public readonly object Payload;

    // Promise：
    // Call 完成对象。
    // Handler 执行完后通过它 SetResult / SetException。
    public readonly IScopePromise Promise;

    public ScopeCallMessage(
        int callId,
        object payload,
        IScopePromise promise)
    {
        CallId = callId;
        Payload = payload;
        Promise = promise;
    }
}
```

---

## B.6 ScopePromise 设计

接口：

```csharp
public interface IScopePromise
{
    // SetException：
    // 目标 Scope 执行失败时调用。
    void SetException(Exception exception);
}
```

泛型实现：

```csharp
public sealed class ScopePromise<TResult> : IScopePromise
{
    private readonly object _gate = new object();

    private bool _completed;
    private TResult? _result;
    private Exception? _exception;
    private Action? _continuation;
    private ScopeThreadContext? _resumeScope;

    public bool IsCompleted
    {
        get
        {
            lock (_gate)
            {
                return _completed;
            }
        }
    }

    public void OnCompleted(
        ScopeThreadContext resumeScope,
        Action continuation)
    {
        // resumeScope：
        // await 发起所在 Scope。
        //
        // continuation：
        // await 后续代码。
        bool shouldPostNow;

        lock (_gate)
        {
            if (!_completed)
            {
                _resumeScope = resumeScope;
                _continuation = continuation;
                return;
            }

            shouldPostNow = true;
        }

        if (shouldPostNow)
        {
            resumeScope.PostContinuation(continuation);
        }
    }

    public void SetResult(TResult result)
    {
        // result：
        // Call handler 返回结果。
        Action? continuation;
        ScopeThreadContext? resumeScope;

        lock (_gate)
        {
            if (_completed)
            {
                throw new InvalidOperationException("Promise already completed.");
            }

            _completed = true;
            _result = result;
            continuation = _continuation;
            resumeScope = _resumeScope;
        }

        if (continuation is not null && resumeScope is not null)
        {
            resumeScope.PostContinuation(continuation);
        }
    }

    public void SetException(Exception exception)
    {
        // exception：
        // Call handler 抛出的异常。
        Action? continuation;
        ScopeThreadContext? resumeScope;

        lock (_gate)
        {
            if (_completed)
            {
                throw new InvalidOperationException("Promise already completed.");
            }

            _completed = true;
            _exception = exception;
            continuation = _continuation;
            resumeScope = _resumeScope;
        }

        if (continuation is not null && resumeScope is not null)
        {
            resumeScope.PostContinuation(continuation);
        }
    }

    public TResult GetResult()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        return _result!;
    }
}
```

---

## B.7 LBTask Scope 回归点

```csharp
public readonly struct LBTask<TResult>
{
    private readonly ScopePromise<TResult> _promise;

    public LBTask(ScopePromise<TResult> promise)
    {
        // promise：
        // 当前异步结果承诺。
        _promise = promise;
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(_promise);
    }

    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly ScopePromise<TResult> _promise;

        public Awaiter(ScopePromise<TResult> promise)
        {
            // promise：
            // 当前 await 等待的 Promise。
            _promise = promise;
        }

        public bool IsCompleted => _promise.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            // continuation：
            // await 后面的代码。
            //
            // currentScope：
            // 当前 await 发生的 Scope。
            // await 完成后必须回到这个 Scope。
            ScopeThreadContext currentScope =
                ScopeExecution.Current;

            _promise.OnCompleted(
                resumeScope: currentScope,
                continuation: continuation);
        }

        public TResult GetResult()
        {
            return _promise.GetResult();
        }
    }
}
```

---

## B.8 ScopeThreadContext

```csharp
public sealed class ScopeThreadContext
{
    // ScopeId：
    // 当前 Scope 的整数 ID。
    public int ScopeId { get; }

    // Continuations：
    // 当前 Scope 的 continuation 队列。
    private readonly LockedBoundedRingQueue<Action> _continuations;

    public ScopeThreadContext(
        int scopeId,
        LockedBoundedRingQueue<Action> continuations)
    {
        // scopeId：
        // 当前 Scope 的整数 ID。
        //
        // continuations：
        // 当前 Scope 的 continuation 队列。
        ScopeId = scopeId;
        _continuations = continuations;
    }

    public bool PostContinuation(Action continuation)
    {
        // continuation：
        // await 后续代码。
        // 返回 false 表示队列满。
        return _continuations.TryEnqueue(continuation);
    }
}
```

---

## B.9 ScopeExecution

```csharp
public static class ScopeExecution
{
    [ThreadStatic]
    private static ScopeThreadContext? _current;

    public static ScopeThreadContext Current
    {
        get
        {
            if (_current is null)
            {
                throw new InvalidOperationException(
                    "No active ScopeThreadContext.");
            }

            return _current;
        }
    }

    internal static void Enter(ScopeThreadContext context)
    {
        // context：
        // 当前即将执行代码所属的 Scope 上下文。
        _current = context;
    }

    internal static void Exit()
    {
        _current = null;
    }
}
```

---

## B.10 ScopeRuntime 高性能版

```csharp
public sealed class ScopeRuntime
{
    // ScopeId：
    // 当前 Scope 的整数 ID。
    public readonly int ScopeId;

    // Descriptor：
    // 当前 Scope 的配置。
    public readonly ScopeDescriptor Descriptor;

    // Services：
    // 当前 Scope 拥有的 IService 数组。
    // 热路径通过 ServiceId 下标访问，不走字典。
    private readonly IService[] _services;

    // EventCenter：
    // 当前 Scope 内事件系统。
    private readonly EventCenter _eventCenter;

    // PostScheduler：
    // 当前 Scope 内部任务投递调度器。
    private readonly PostScheduler _postScheduler;

    // TimeScheduler：
    // 当前 Scope 自己的时间调度器。
    private readonly TimeScheduler _timeScheduler;

    // EcsWorld：
    // 当前 Scope 自己的 ECS World。
    private readonly EcsWorld _ecsWorld;

    // PostInbox：
    // 跨 Scope Post 消息队列。
    private readonly LockedBoundedRingQueue<ScopePostMessage> _postInbox;

    // CallInbox：
    // 跨 Scope Call 消息队列。
    private readonly LockedBoundedRingQueue<ScopeCallMessage> _callInbox;

    // Continuations：
    // LBTask await 后续代码队列。
    private readonly LockedBoundedRingQueue<Action> _continuations;

    // ThreadContext：
    // 当前 Scope 的线程上下文。
    private readonly ScopeThreadContext _threadContext;

    // _workerThread：
    // Worker 模式下的内部线程。
    private Thread? _workerThread;

    // _running：
    // Scope 是否运行中。
    private volatile bool _running;

    public ScopeRuntime(
        ScopeDescriptor descriptor,
        IService[] services,
        EventCenter eventCenter,
        PostScheduler postScheduler,
        TimeScheduler timeScheduler,
        EcsWorld ecsWorld,
        int queueCapacity)
    {
        // descriptor：
        // Scope 配置。
        //
        // services：
        // 当前 Scope 拥有的 IService 数组。
        //
        // eventCenter：
        // 当前 Scope 内事件中心。
        //
        // postScheduler：
        // 当前 Scope 内部 Post 调度器。
        //
        // timeScheduler：
        // 当前 Scope 的时间调度器。
        //
        // ecsWorld：
        // 当前 Scope 的 ECS World。
        //
        // queueCapacity：
        // 当前 Scope 各类环形队列容量。
        Descriptor = descriptor;
        ScopeId = descriptor.ScopeId;

        _services = services;
        _eventCenter = eventCenter;
        _postScheduler = postScheduler;
        _timeScheduler = timeScheduler;
        _ecsWorld = ecsWorld;

        _postInbox = new LockedBoundedRingQueue<ScopePostMessage>(queueCapacity);
        _callInbox = new LockedBoundedRingQueue<ScopeCallMessage>(queueCapacity);
        _continuations = new LockedBoundedRingQueue<Action>(queueCapacity);

        _threadContext = new ScopeThreadContext(
            scopeId: ScopeId,
            continuations: _continuations);
    }

    public bool TryPost(ScopePostMessage message)
    {
        // message：
        // 跨 Scope Post 消息。
        return _postInbox.TryEnqueue(message);
    }

    public bool TryCall(ScopeCallMessage message)
    {
        // message：
        // 跨 Scope Call 消息。
        return _callInbox.TryEnqueue(message);
    }

    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;

        if (Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            _workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"LB Scope Worker {Descriptor.Name}"
            };

            _workerThread.Start();
            return;
        }

        ExecuteInScope(StartServices);
    }

    public void Pump(float deltaTime)
    {
        // deltaTime：
        // 外部主循环传入的时间步长。
        // 只对 Inline Scope 生效。
        if (Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            return;
        }

        ExecuteInScope(() => PumpInternal(deltaTime));
    }

    public void Stop()
    {
        _running = false;

        if (Descriptor.Threading == ScopeThreadingMode.Worker)
        {
            _workerThread?.Join();
            _workerThread = null;
            return;
        }

        ExecuteInScope(StopServices);
    }

    private void WorkerLoop()
    {
        ExecuteInScope(StartServices);

        while (_running)
        {
            float deltaTime = _timeScheduler.GetDeltaTimeForWorker();
            ExecuteInScope(() => PumpInternal(deltaTime));
            _timeScheduler.SleepIfNeeded();
        }

        ExecuteInScope(StopServices);
    }

    private void PumpInternal(float deltaTime)
    {
        _timeScheduler.Advance(deltaTime);

        DrainPostInbox();
        DrainCallInbox();
        _postScheduler.Drain();
        DrainContinuations();

        FlushActorEvents();
    }

    private void DrainPostInbox()
    {
        while (_postInbox.TryDequeue(out ScopePostMessage message))
        {
            __LbEventDispatch.Dispatch(
                scopeId: ScopeId,
                eventId: message.EventId,
                payload: message.Payload,
                services: _services,
                eventCenter: _eventCenter);
        }
    }

    private void DrainCallInbox()
    {
        while (_callInbox.TryDequeue(out ScopeCallMessage message))
        {
            __LbCallDispatch.Dispatch(
                scopeId: ScopeId,
                callId: message.CallId,
                payload: message.Payload,
                promise: message.Promise,
                services: _services);
        }
    }

    private void DrainContinuations()
    {
        while (_continuations.TryDequeue(out Action continuation))
        {
            continuation();
        }
    }

    private void ExecuteInScope(Action action)
    {
        ScopeExecution.Enter(_threadContext);

        try
        {
            action();
        }
        finally
        {
            ScopeExecution.Exit();
        }
    }

    private void StartServices()
    {
        for (int i = 0; i < _services.Length; i++)
        {
            _services[i].Start();
        }
    }

    private void StopServices()
    {
        if (Descriptor.StopPolicy == ScopeStopPolicy.Drain)
        {
            DrainPostInbox();
            DrainCallInbox();
            _postScheduler.Drain();
            DrainContinuations();
        }
        else
        {
            _postInbox.Clear();
            _callInbox.Clear();
            _postScheduler.Clear();
            _continuations.Clear();
        }

        for (int i = _services.Length - 1; i >= 0; i--)
        {
            _services[i].Stop();
        }
    }

    private void FlushActorEvents()
    {
        // 当前 Scope 的 ActorEventOutbox 刷到 LayerRuntime 的唯一 ActorWorld。
        // 具体实现依赖现有 ActorWorld 管线。
    }
}
```

注意：

```text
这里的 IService.Start / IService.Stop 只是示意。
Agent 必须沿用现有 IService 接口真实方法名。
如果现有接口叫 OnStart / OnDestroy / Initialize，就按现有接口接入。
```

---

## B.11 LayerRuntime 高性能版

```csharp
public sealed class LayerRuntime
{
    // _scopes：
    // 所有 ScopeRuntime。
    // ScopeId 直接作为数组下标。
    private readonly ScopeRuntime[] _scopes;

    // _actorWorld：
    // 每个 LayerRuntime 唯一 ActorWorld。
    private readonly ActorWorld _actorWorld;

    public LayerRuntime(
        ScopeRuntime[] scopes,
        ActorWorld actorWorld)
    {
        // scopes：
        // 所有 ScopeRuntime。
        // 数组下标必须等于 ScopeId。
        //
        // actorWorld：
        // 当前 LayerRuntime 唯一 ActorWorld。
        _scopes = scopes;
        _actorWorld = actorWorld;
    }

    public void Start()
    {
        for (int i = 0; i < _scopes.Length; i++)
        {
            _scopes[i].Start();
        }
    }

    public void Pump(float deltaTime)
    {
        // deltaTime：
        // 主循环传入的时间步长。
        // 只 Pump Inline Scope。
        for (int i = 0; i < _scopes.Length; i++)
        {
            _scopes[i].Pump(deltaTime);
        }

        _actorWorld.Pump(deltaTime);
    }

    public void Stop()
    {
        for (int i = _scopes.Length - 1; i >= 0; i--)
        {
            _scopes[i].Stop();
        }
    }

    internal bool TryPost(
        int targetScopeId,
        ScopePostMessage message)
    {
        // targetScopeId：
        // 目标 ScopeId。
        //
        // message：
        // 要投递的跨 Scope Post 消息。
        return _scopes[targetScopeId].TryPost(message);
    }

    internal bool TryCall(
        int targetScopeId,
        ScopeCallMessage message)
    {
        // targetScopeId：
        // 目标 ScopeId。
        //
        // message：
        // 要投递的跨 Scope Call 消息。
        return _scopes[targetScopeId].TryCall(message);
    }
}
```

---

## B.12 ScopeRef 生成代码

用户写：

```csharp
await Scope<CombatScope>().Call(new BulletTickCall(deltaTime, tickId));
```

Generator 最好生成强类型扩展，避免运行时查找。

示意：

```csharp
public readonly struct ScopeRef<TScope>
{
    private readonly LayerRuntime _runtime;

    public ScopeRef(LayerRuntime runtime)
    {
        // runtime：
        // 当前 LayerRuntime。
        _runtime = runtime;
    }
}
```

事件请求类型通过 `[ScopeEvent<TScope>]` 标记：

```csharp
[ScopeEvent<CombatScope>]
public readonly struct SpawnBulletEvent
{
}
```

Call 请求类型通过 `[ScopeCall<TScope, TResult>]` 标记：

```csharp
[ScopeCall<CombatScope, BulletTickResult>]
public readonly struct BulletTickCall
{
}
```

针对 CombatScope 生成扩展：

```csharp
public static class __LbCombatScopeExtensions
{
    public static bool Post(
        this ScopeRef<CombatScope> scope,
        SpawnBulletEvent message)
    {
        // scope：
        // CombatScope 通讯引用。
        //
        // message：
        // 要投递到 CombatScope 的事件。
        return scope.Runtime.TryPost(
            targetScopeId: __LbScopeIds.Combat,
            message: new ScopePostMessage(
                eventId: __LbEventIds.SpawnBulletEvent,
                payload: message));
    }

    public static ScopePromise<BulletTickResult> Call(
        this ScopeRef<CombatScope> scope,
        BulletTickCall call)
    {
        // scope：
        // CombatScope 通讯引用。
        //
        // call：
        // BulletTickCall 调用参数。
        ScopePromise<BulletTickResult> promise =
            __LbPromisePool<BulletTickResult>.Rent();

        bool ok = scope.Runtime.TryCall(
            targetScopeId: __LbScopeIds.Combat,
            message: new ScopeCallMessage(
                callId: __LbCallIds.BulletTickCall,
                payload: call,
                promise: promise));

        if (!ok)
        {
            promise.SetException(
                new InvalidOperationException(
                    "CombatScope call queue is full."));
        }

        return promise;
    }
}
```

当前阶段先生成强类型入口并复用 `ScopeRef.TryPost` / `ScopeRef.Call<TResult>(callId, payload)`。
后续 Dispatch 表接入后，再把 eventId / callId 与生成的 dispatch switch 统一。

---

## B.13 生成 Dispatch 表

当前阶段生成 Post Dispatch 表和 Call Dispatch 表。业务 Service 写：

```csharp
[Scope<CombatScope>]
public sealed partial class BulletCollisionService : IService
{
    [ScopeEvent]
    private void OnSpawnBullet(SpawnBulletEvent message)
    {
    }

    [ScopeCall]
    private BulletTickResult OnBulletTick(BulletTickCall call)
    {
        return default;
    }
}
```

Generator 输出四部分：

```text
1. 同一个 partial Service 内的 internal event bridge，用于访问 private [ScopeEvent] 方法。
2. 同一个 partial Service 内的 internal call bridge，用于访问 private [ScopeCall] 方法。
3. LayerBase.Scope.GeneratedScopePostDispatcher.Dispatch(scope, message)，用于传给 ScopeRuntimeHost.Create 的 postDispatcher。
4. LayerBase.Scope.GeneratedScopeCallDispatcher.Dispatch(scope, message)，用于传给 ScopeRuntimeHost.Create 的 callDispatcher。
```

同时生成统一 Host 工厂，避免使用方手写 dispatcher 接线：

```csharp
public static class GeneratedScopeRuntimeHostFactory
{
    public static ScopeRuntimeHost Create(
        IReadOnlyList<IService> services,
        ScopeRuntimeOptions? options = null)
    {
        return Create(
            ScopeRuntimePlanner.Build(services),
            options);
    }

    public static ScopeRuntimeHost Create(
        IReadOnlyList<ScopeRuntimePlan> plans,
        ScopeRuntimeOptions? options = null)
    {
        return ScopeRuntimeHost.Create(
            plans,
            options,
            postDispatcher: GeneratedScopePostDispatcher.Dispatch,
            callDispatcher: GeneratedScopeCallDispatcher.Dispatch);
    }
}
```

如果当前编译中只有 Post 或只有 Call，工厂会只接入存在的 dispatcher，另一侧保持 `null`。

Post Dispatch：

```csharp
public static class GeneratedScopePostDispatcher
{
    public static void Dispatch(
        ScopeRuntime scope,
        ScopePostMessage message)
    {
        // scope：
        // 当前正在执行的 ScopeRuntime。
        //
        // message：
        // EventId / Payload。
        switch (message.EventId)
        {
            case __LbEventIds.SpawnBulletEvent:
                DispatchSpawnBullet(scope, message);
                return;

            default:
                throw new InvalidOperationException(
                    $"Unknown scope event id {message.EventId}.");
        }
    }

    private static void DispatchSpawnBullet(
        ScopeRuntime scope,
        ScopePostMessage message)
    {
        SpawnBulletEvent payload = (SpawnBulletEvent)message.Payload;

        BulletCollisionService service =
            FindService<BulletCollisionService>(scope.Services);

        service.__LayerBaseScopeEvent_OnSpawnBullet_SpawnBulletEvent(payload);
    }
}
```

Call Dispatch：

```csharp
public static class GeneratedScopeCallDispatcher
{
    public static void Dispatch(
        ScopeRuntime scope,
        ScopeCallMessage message)
    {
        // scope：
        // 当前正在执行的 ScopeRuntime。
        //
        // message：
        // CallId / Payload / Promise。
        try
        {
            switch (message.CallId)
            {
                case __LbCallIds.BulletTickCall:
                    DispatchBulletTick(scope, message);
                    return;

                default:
                    message.Promise.SetException(
                        new InvalidOperationException(
                            $"Unknown scope call id {message.CallId}."));
                    return;
            }
        }
        catch (Exception ex)
        {
            message.Promise.SetException(ex);
        }
    }

    private static void DispatchBulletTick(
        ScopeRuntime scope,
        ScopeCallMessage message)
    {
        BulletCollisionService service =
            FindService<BulletCollisionService>(scope.Services);

        BulletTickResult result =
            service.__LayerBaseScopeCall_BulletTickCall(
                (BulletTickCall)message.Payload);

        ((ScopePromise<BulletTickResult>)message.Promise)
            .SetResult(result);
    }
}
```

注意：

```text
1. __LayerBaseScopeEvent_* / __LayerBaseScopeCall_* 是 Generator 生成的桥接方法。
2. 如果原 [ScopeEvent] / [ScopeCall] 方法是 private，桥接仍可在同 partial 类型内访问。
3. 同一个 ScopeEvent 可以分发给多个 Service，或同一个 Service 内多个处理方法。
4. 当前阶段服务查找先用 Services[] 线性匹配类型。
5. 后续接入稳定 ServiceSlot 生成后，再把 FindService<T> 替换成数组下标直取。
```

---

## B.14 现有 IService 接入方式

必须沿用当前 `IService`。

如果现有接口类似：

```csharp
public interface IService
{
    void OnCreate();
    void OnStart();
    void OnStop();
    void OnDestroy();
}
```

则 `ScopeRuntime` 应调用现有方法。

不要新造：

```csharp
Service.Start()
Service.Stop()
```

除非现有接口就是这个名字。

建议增加生成器专用绑定接口，而不是改变 `IService`，也不要求业务 Service 继承额外基类：

```csharp
public interface IGeneratedScopeServiceBinding
{
    void BindScope(
        ScopeRuntime ownerScope,
        int serviceId);
}
```

参数说明：

```csharp
public interface IGeneratedScopeServiceBinding
{
    void BindScope(
        ScopeRuntime ownerScope,
        int serviceId);
        // ownerScope：
        // 当前 Service 所属的 ScopeRuntime。
        //
        // serviceId：
        // 当前 Service 在所属 Scope 内的数组下标。
}
```

开发者仍然只写 `partial class XxxService : IService`。源生成器在 partial 类型中生成 Scope 绑定成员：

```csharp
public sealed partial class CombatService : IService
{
    // OwnerScope：
    // 当前 Service 所属 ScopeRuntime。
    // 注意它不是 LayerRuntime。
    protected ScopeRuntime OwnerScope { get; private set; } = null!;

    // ServiceId：
    // 当前 Service 在所属 Scope 内的数组下标。
    protected int ServiceId { get; private set; }

    protected ScopeRef<TScope> Scope<TScope>()
    {
        return OwnerScope.GetScopeRef<TScope>();
    }

    void IGeneratedScopeServiceBinding.BindScope(
        ScopeRuntime ownerScope,
        int serviceId)
    {
        OwnerScope = ownerScope;
        ServiceId = serviceId;
    }
}
```

规则：

```text
1. 业务代码只依赖 IService。
2. ScopeRuntime 运行时识别 IGeneratedScopeServiceBinding。
3. OwnerScope / ServiceId / Scope<TScope>() 由源生成器生成到 partial IService。
4. 不引入 ServiceBase 继承要求。
```

---

# 测试设计

## T1：Service 属于 ScopeRuntime，不属于 LayerRuntime

### Arrange

```text
1. 定义 CombatScope。
2. 定义 [Scope<CombatScope>] CombatService。
3. Build LayerRuntime。
```

### Assert

```text
1. CombatService 存在于 CombatScopeRuntime.Services。
2. CombatService 不存在于 LayerRuntime 全局 ServiceContainer。
3. CombatService.OwnerScope.ScopeId == CombatScopeId。
```

---

## T2：无 Scope Service 默认进入 MainScope

### Arrange

```text
1. 定义 GameFlowService : IService。
2. 不添加 [Scope<T>]。
3. Build LayerRuntime。
```

### Assert

```text
1. GameFlowService 属于 MainScopeRuntime。
2. GameFlowService.OwnerScope.ScopeId == MainScopeId。
```

---

## T3：ScopeRuntime 各自持有 EcsWorld

### Arrange

```text
1. MainScopeRuntime.EcsWorld 创建 Entity A。
2. CombatScopeRuntime.EcsWorld 创建 Entity B。
```

### Assert

```text
1. MainScopeRuntime.EcsWorld 查得到 A。
2. MainScopeRuntime.EcsWorld 查不到 B。
3. CombatScopeRuntime.EcsWorld 查得到 B。
4. CombatScopeRuntime.EcsWorld 查不到 A。
```

---

## T4：Inline Scope 使用本地环形队列

### Arrange

```text
1. MainScope 和 UIScope 都是 Inline。
2. MainScope Post 消息到 UIScope。
```

### Assert

```text
1. Post 后 handler 不立即执行。
2. UIScope.Pump 后 handler 执行。
3. 队列类型是 LocalRingQueue 或等价无锁短队列。
```

---

## T5：Worker Scope 使用有限环形队列

### Arrange

```text
1. CombatScope 是 Worker。
2. queueCapacity = 2。
3. 连续 Post 3 条消息。
```

### Assert

```text
1. 前 2 条成功。
2. 第 3 条返回 false 或触发 QueueFull 策略。
3. 不发生扩容。
```

---

## T6：Call 不阻塞，await 后回原 Scope

### Arrange

```text
1. MainScope await CombatScope.Call。
2. CombatScope handler 返回结果。
3. 记录 await 前后 ScopeExecution.Current.ScopeId。
```

### Assert

```text
1. handler 在 CombatScope 执行。
2. SetResult 不直接执行 MainScope continuation。
3. MainScope Pump 后 continuation 执行。
4. await 前后 ScopeId 都是 MainScopeId。
```

---

## T7：CombatScope 内 await 后回 CombatScope

### Arrange

```text
1. CombatScope Service 内 await AssetBuildScope.Call。
2. AssetBuildScope 返回结果。
```

### Assert

```text
1. await 后代码继续在 CombatScope 执行。
2. 不回 MainScope。
```

---

## T8：Call 队列满

### Arrange

```text
1. CombatScope callQueueCapacity = 1。
2. 不 Pump CombatScope。
3. 连续 Call 2 次。
```

### Assert

```text
1. 第一次成功入队。
2. 第二次返回一个失败 Promise 或直接抛出可诊断异常。
3. 不扩容。
```

---

## T9：ActorWorld 唯一

### Arrange

```text
1. MainScope 输出 ActorEvent A。
2. CombatScope 输出 ActorEvent B。
```

### Assert

```text
1. LayerRuntime.ActorWorld 是唯一实例。
2. ActorWorld 收到 A 和 B。
3. A/B 的 header 中 ScopeId 不同。
```

---

## T10：Analyzer 检查 class 跨 Scope

### Arrange

```csharp
public sealed class MutablePayload
{
    public int Value;
}

await Scope<CombatScope>().Call(
    new InstallCall(new MutablePayload()));
```

### Assert

```text
1. CrossScopeReferencePolicy.Warn 下给 warning。
2. CrossScopeReferencePolicy.Error 下给 error。
3. [CrossScopeSafe] 后 warning 消失。
```

---

# 现有代码修改范围

## M1：IService / Service 基类

必须检查现有 `IService` 接口。

修改原则：

```text
1. 不替换 IService。
2. 不引入 ScopeService。
3. 如果需要持有 OwnerScope，则通过内部接口或现有 Service 基类注入。
4. Service.Scope<T>() 应从 OwnerScope 获取跨域引用。
5. Service 不应直接持有 LayerRuntime 作为运行归属。
```

可能新增：

```csharp
internal interface IServiceScopeBinding
{
    void BindScope(
        ScopeRuntime ownerScope,
        int serviceId);
}
```

---

## M2：LayerRuntime

需要从“资源拥有者”降级为“Scope 编排者”。

修改范围：

```text
1. 移除或废弃全局 ServiceContainer。
2. 移除或废弃全局 EventCenter。
3. 新增 ScopeRuntime[]。
4. 新增 ActorWorld 唯一实例。
5. Build 时创建 ScopeRuntime。
6. Start / Stop / Pump 转发给 ScopeRuntime。
7. Post / Call 只负责路由到目标 ScopeRuntime。
```

---

## M3：Service 注册

原先如果是：

```text
LayerRuntime.RegisterService(service)
```

要改成：

```text
ScopeRuntime.RegisterService(service)
```

生成器或 Builder 负责：

```text
1. 读取 [Scope<T>]。
2. 分配 ScopeId。
3. 分配 ServiceSlot。
4. 把 IService 放进对应 ScopeRuntime.Services[]。
```

---

## M4：EventCenter

修改范围：

```text
1. EventCenter 从 LayerRuntime 拆到 ScopeRuntime。
2. Event handler 注册到所属 Scope 的 EventCenter。
3. Scope 内 Publish 不跨 Scope。
4. 跨 Scope 事件走 Post。
```

---

## M5：LBTask

修改范围：

```text
1. Awaiter 不再固定回主线程。
2. Awaiter 捕获 ScopeExecution.Current。
3. SetResult 不直接运行 continuation。
4. continuation 入原 Scope 的 continuation ring queue。
5. 禁止 Result / Wait。
```

---

## M6：Queue

替换：

```text
ConcurrentQueue<T>
Queue<T> 热路径扩容
List<T> 作为消息队列
```

改为：

```text
LocalRingQueue<T>
LockedBoundedRingQueue<T>
后续可替换无锁 MPSC RingBuffer
```

---

## M7：ECS

修改范围：

```text
1. EcsWorld 每 Scope 一份。
2. EcsScheduler 每 Scope 一份。
3. QueryBatchScheduler 每 Scope 一份。
4. CommandBatchQueue 每 Scope 一份。
5. ActorEventBatch 输出到当前 Scope 的 ActorEventOutbox。
6. ActorWorld 仍然唯一。
```

---

## M8：Generator

需要生成：

```text
1. ScopeId 表。
2. ServiceId / ServiceSlot 表。
3. EventId 表。
4. CallId 表。
5. ScopeDescriptor[]。
6. ScopeRuntime 构建代码。
7. Event dispatch switch。
8. Call dispatch switch。
9. ScopeRef<TScope> 强类型 Post / Call 扩展。
```

---

## M9：Analyzer

需要新增：

```text
1. ScopeOptions 参数检查。
2. IService 多 Scope 归属检查。
3. 跨 Scope GetService 检查。
4. ScopeCall 方法签名检查。
5. Call.Result / Call.Wait 检查。
6. 引用类型跨 Scope 传输 warning / error。
7. 队列容量配置检查。
```

当前已落地：

```text
1. ScopeOptions tickRateHz 运行时 guard。
2. ScopeOptions 生成期诊断：
   - LBSD001：tickRateHz 不能为负数。
   - LBSD002：FixedRate 必须配置正数 tickRateHz。
3. ScopeEvent 生成期诊断：
   - LBSE001：方法必须为实例方法，且参数数量为 1、返回 void。
   - LBSE002：owner 必须为 partial。
   - LBSE003：owner 必须实现 IService。
4. ScopeCall 生成期诊断：
   - LBSC001：方法必须为实例方法，且参数数量为 1、返回非 void。
   - LBSC002：owner 必须为 partial。
   - LBSC003：handler 返回类型必须匹配请求的 TResult。
   - LBSC004：owner 必须实现 IService。
```

---

# 最终落地顺序

## Phase 1：ScopeRuntime 拆分

```text
1. 新增 ScopeOptions / ScopeAttribute。
2. 生成 ScopeId。
3. LayerRuntime 创建 ScopeRuntime[]。
4. IService 按 ScopeRuntime 分组。
5. Service 生命周期由 ScopeRuntime 调用。
```

## Phase 2：队列替换

```text
1. 实现 LocalRingQueue。
2. 实现 LockedBoundedRingQueue。
3. Post / Call / Continuation 全部使用有限环形队列。
4. 暂不做无锁 MPSC。
```

## Phase 3：Scope 内 EventCenter

```text
1. 每 Scope 一个 EventCenter。
2. Event handler 注册到所属 Scope。
3. Scope 内事件跑通。
```

## Phase 4：Post / Call

```text
1. Post 路由到目标 ScopeRuntime。
2. Call 路由到目标 ScopeRuntime。
3. Call 返回 LBTask。
4. Promise 完成后 continuation 回原 Scope。
```

## Phase 5：Worker Scope

```text
1. Worker Scope 内部线程。
2. OnStart / OnStop 在线程内执行。
3. FixedRate / Realtime / Manual 时钟跑通。
```

## Phase 6：ECS per Scope

```text
1. 每 Scope 创建 EcsWorld。
2. Query / CommandBatch / ActorEventOutbox 归属 Scope。
3. 子弹碰撞迁移到 CombatScope 验证。
```

## Phase 7：Generator / Analyzer 完善

```text
1. 去掉热路径 Dictionary。
2. 生成 dispatch switch。
3. 生成强类型 ScopeRef 扩展。
4. Analyzer 补安全诊断。
```

---

# 最终一句话

LayerRuntime 只编排 Scope；ScopeRuntime 才是资源所有者；IService 沿用现有接口并归属 ScopeRuntime；Scope 内用 EventCenter / Scheduler；跨 Scope 用 Post / awaitable Call；运行期热路径用生成 ID + 数组 + 有限环形队列；EcsWorld 每 Scope 可有一份；ActorWorld 每 LayerRuntime 唯一。
