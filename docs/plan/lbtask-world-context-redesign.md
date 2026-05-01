# LBTask 多世界上下文修订方案

## 1. 修订目的

当前 `LBTask` 的上下文捕获依赖 `SynchronizationContext.Current`。

这在单 World 下通常没有问题，但在多 World 共用同一线程时会出现上下文错位。

原因是：

```text
SynchronizationContext.Current 是当前线程上的上下文
不是当前 World 的上下文
```

因此，如果多个 World 都运行在同一个主线程上，那么同一时刻只有一个 `SynchronizationContext` 能成为 `SynchronizationContext.Current`。

多 World 下，仅仅为每个 World 创建一个 `LBTaskSynchronizationContext` 不足以让它自动生效。真正生效的只有当前线程上被设置为 `SynchronizationContext.Current` 的那个上下文。

本次修订目标是：

```text
每个 World 拥有自己的 LBTaskSynchronizationContext
World.Update() 时临时切换当前上下文
LBTaskSource 捕获当前 World 的上下文
任务完成后 continuation 回到创建它的 World
World.Update() 结束后恢复旧上下文
```

---

## 2. 核心问题

当前风险模型如下：

```text
主线程
    ├── World A
    │     └── LBTaskSynchronizationContext A
    ├── World B
    │     └── LBTaskSynchronizationContext B
    └── SynchronizationContext.Current 只能指向其中一个
```

如果 `LBTaskSource.Rent()` 内部直接读取：

```csharp
SynchronizationContext.Current
```

那么它捕获到的是当前线程上下文，而不是明确的 World 上下文。

这会导致：

```text
World A 创建的任务可能回到 World B
World B 创建的任务可能回到 World A
显式传入 ctx 的 API 也可能出现调度上下文和恢复上下文不一致
```

尤其需要注意 `NextFrame(ctx)`。

如果 `NextFrame(ctx)` 使用传入的 `ctx` 做帧调度，但 `LBTaskSource.Rent()` 仍然读取 `SynchronizationContext.Current`，就会出现：

```text
任务完成动作投递到 ctx
await 后续 continuation 却回到 SynchronizationContext.Current
```

这两个上下文可能不是同一个。

---

## 3. 修订原则

### 3.1 World 上下文必须显式存在

每个 World 应该持有自己的任务上下文：

```text
World
    └── LBTaskSynchronizationContext TaskContext
```

这个上下文负责保存当前 World 的：

```text
Post 队列
NextFrame 队列
await continuation
```

### 3.2 SynchronizationContext.Current 只能临时使用

`SynchronizationContext.Current` 不应该作为多 World 的全局注册点。

正确做法是：

```text
进入 World.Update()
    临时设置 SynchronizationContext.Current = 当前 World 的 TaskContext

退出 World.Update()
    恢复进入前的 SynchronizationContext.Current
```

### 3.3 显式传入 ctx 的 API 必须捕获同一个 ctx

凡是 API 参数中已经有 `SynchronizationContext ctx`，任务源也必须捕获这个 `ctx`。

例如：

```csharp
LBTask.NextFrame(ctx)
LBTask.RunOnMainThread(action, ctx)
LBTask<T>.RunOnMainThread(func, ctx)
```

这些 API 内部不能再让 `LBTaskSource` 隐式捕获别的上下文。

---

## 4. 修订 LBTaskSynchronizationContext

### 4.1 增加 EnterScope

`LBTaskSynchronizationContext` 需要提供一个临时作用域，用于在 `World.Update()` 中切换当前线程上下文。

```csharp
public sealed class LBTaskSynchronizationContext : SynchronizationContext, ILBTaskMainThreadPump, IDisposable
{
    private readonly int _mainThreadId;

    // 省略已有字段：
    // _queue：普通 Post 队列。
    // _frameWork：按帧延迟执行的任务列表。
    // _lock：保护 _frameWork 的锁。
    // _disposed：标记当前上下文是否已经释放。

    public Scope EnterScope()
    {
        // 参数：无。
        // 作用：进入一个临时上下文作用域。
        // 结果：当前线程的 SynchronizationContext.Current 会被设置为当前实例。
        return new Scope(this);
    }

    public readonly struct Scope : IDisposable
    {
        private readonly SynchronizationContext? _previous;

        public Scope(LBTaskSynchronizationContext context)
        {
            // context：
            // 当前 World 持有的 LBTaskSynchronizationContext。
            // 作用：在本作用域内，LBTaskSource.Rent() 会默认捕获该 World 的上下文。
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // _previous：
            // 保存进入作用域之前的 SynchronizationContext.Current。
            // 作用：Dispose 时恢复旧上下文，避免污染下一个 World。
            _previous = SynchronizationContext.Current;

            // SetSynchronizationContext(context)：
            // 将当前线程上下文临时切换为当前 World 的任务上下文。
            // 作用：让无参 LBTask API 在 World.Update() 内可以捕获正确 World。
            SetSynchronizationContext(context);
        }

        public void Dispose()
        {
            // 参数：无。
            // 作用：退出作用域时恢复旧上下文。
            // 必要性：如果不恢复，下一个 World 或宿主环境会错误继承当前 World 的上下文。
            SetSynchronizationContext(_previous);
        }
    }
}
```

---

## 5. 修订 World.Update

每个 World 的 Update 必须包裹自己的任务上下文作用域。

```csharp
public sealed class LayerWorld
{
    public LBTaskSynchronizationContext TaskContext { get; }

    public LayerWorld()
    {
        // Install()：
        // 创建当前 World 独占的 LBTaskSynchronizationContext。
        // 注意：这里不应该长期设置 SynchronizationContext.Current。
        TaskContext = LBTaskSynchronizationContext.Install();
    }

    public void Update()
    {
        // EnterScope()：
        // 临时把当前线程上下文设置为当前 World 的 TaskContext。
        // 作用：本次 Update 内创建的 LBTask 都会默认绑定到当前 World。
        using var scope = TaskContext.EnterScope();

        // UpdateSystems()：
        // 执行当前 World 的系统逻辑。
        // 如果系统内部调用 await LBTask.NextFrame() 或 async LBTask 方法，
        // 它们会捕获当前 World 的 TaskContext。
        UpdateSystems();

        // TaskContext.Update()：
        // 执行当前 World 的异步任务队列。
        // 作用：只恢复属于当前 World 的 continuation。
        TaskContext.Update();
    }

    private void UpdateSystems()
    {
        // 参数：无。
        // 作用：示例占位。
        // 实际项目中替换为当前 World 的系统更新逻辑。
    }
}
```

修订后，同一线程上的多个 World 应该按如下方式运行：

```csharp
worldA.Update();
worldB.Update();
worldC.Update();
```

实际效果是：

```text
worldA.Update() 内：
    SynchronizationContext.Current = worldA.TaskContext

worldB.Update() 内：
    SynchronizationContext.Current = worldB.TaskContext

worldC.Update() 内：
    SynchronizationContext.Current = worldC.TaskContext

每个 Update 结束后：
    SynchronizationContext.Current 恢复进入前状态
```

---

## 6. 修订 Install 与 InstallAsCurrent

### 6.1 Install 只创建上下文，不安装为 Current

```csharp
public static LBTaskSynchronizationContext Install()
{
    // 参数：无。
    // 作用：创建一个新的 LBTaskSynchronizationContext。
    // 多 World 下，每个 World 都应该获得自己的独立上下文。
    // 注意：该方法不应该调用 SetSynchronizationContext。
    return new LBTaskSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
}
```

### 6.2 InstallAsCurrent 仅保留给单 World 兼容模式

```csharp
[Obsolete("InstallAsCurrent is not multi-world safe. Use Install() and EnterScope() instead.")]
public static LBTaskSynchronizationContext InstallAsCurrent()
{
    // 参数：无。
    // 作用：兼容旧版单 World 使用方式。
    // 风险：同一线程只能有一个 SynchronizationContext.Current，
    // 因此该方法不适合作为多 World 初始化入口。
    if (Current is LBTaskSynchronizationContext existing)
        return existing;

    var ctx = new LBTaskSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
    SetSynchronizationContext(ctx);
    return ctx;
}
```

多 World 初始化时禁止使用：

```csharp
LBTaskSynchronizationContext.InstallAsCurrent();
```

因为它会把上下文长期挂到当前线程上，导致后续 World 无法自然获得自己的上下文。

---

## 7. 修订 LBTaskSource

### 7.1 增加显式上下文 Rent

`LBTaskSource` 需要支持显式传入上下文。

```csharp
internal sealed class LBTaskSource : ILBTaskSource
{
    private static readonly ObjectPool<LBTaskSource> Pool = new(() => new LBTaskSource());

    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;
    private Action? _continuation;
    private Exception? _exception;

    // _released：
    // 0 表示正在使用。
    // 1 表示已经归还对象池。
    // 作用：防止同一个任务源被重复归还。
    private int _released;

    // _status：
    // 0 表示 pending。
    // -1 表示 completing。
    // 1 表示 completed。
    // 作用：控制任务完成状态，避免重复完成。
    private int _status;

    public static LBTaskSource Rent(SynchronizationContext? context)
    {
        // context：
        // 当前任务完成后 continuation 应该恢复到的目标上下文。
        // 多 World 下，它应该是当前 World 的 TaskContext。
        // 如果为 null，则任务完成后回到 ThreadPool。
        var src = Pool.Rent();

        // _continuation：
        // 保存 await 后续逻辑。
        // 复用对象时必须清空，避免执行上一次任务残留的 continuation。
        src._continuation = null;

        // _exception：
        // 保存任务异常。
        // 复用对象时必须清空，避免旧异常污染新任务。
        src._exception = null;

        // _canceledToken：
        // 保存取消任务时使用的 CancellationToken。
        // 复用对象时必须恢复默认值。
        src._canceledToken = default;

        // _context：
        // 保存任务完成后的恢复位置。
        // 这是多 World 上下文修订的核心字段。
        src._context = context;

        // _status：
        // 恢复为 pending 状态。
        src._status = 0;

        // _released：
        // 恢复为未归还对象池状态。
        src._released = 0;

        return src;
    }

    public static LBTaskSource Rent()
    {
        // 参数：无。
        // 作用：兼容无参调用路径，默认捕获 SynchronizationContext.Current。
        // 多 World 要求：调用方必须已经处于 TaskContext.EnterScope() 内。
        return Rent(SynchronizationContext.Current);
    }

    private void Schedule(Action continuation)
    {
        // continuation：
        // await 后面需要恢复执行的逻辑。
        // 作用：根据 _context 决定把它投递到 World 队列还是 ThreadPool。
        var ctx = _context;

        if (ctx != null)
        {
            // ctx.Post：
            // 把 continuation 放入目标上下文队列。
            // 多 World 下，这里会进入对应 World 的 LBTaskSynchronizationContext。
            ctx.Post(static state => ((Action)state!).Invoke(), continuation);
        }
        else
        {
            // ThreadPool.QueueUserWorkItem：
            // 没有上下文时退回线程池。
            // 这种路径不具备 World 亲和性。
            ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
        }
    }
}
```

### 7.2 泛型任务源同样修订

```csharp
internal sealed class LBTaskSource<T> : ILBTaskSource<T>
{
    private static readonly ObjectPool<LBTaskSource<T>> Pool = new(() => new LBTaskSource<T>());

    private CancellationToken _canceledToken;
    private SynchronizationContext? _context;
    private Action? _continuation;
    private Exception? _exception;
    private T _result = default!;
    private int _released;
    private int _status;

    public static LBTaskSource<T> Rent(SynchronizationContext? context)
    {
        // context：
        // 当前任务完成后 continuation 应该恢复到的目标上下文。
        // 多 World 下，它应该是当前 World 的 TaskContext。
        var src = Pool.Rent();

        // _continuation：
        // 保存 await 后续逻辑。
        // 复用对象时必须清空。
        src._continuation = null;

        // _exception：
        // 保存任务异常。
        // 复用对象时必须清空。
        src._exception = null;

        // _canceledToken：
        // 保存取消信息。
        // 复用对象时必须重置。
        src._canceledToken = default;

        // _result：
        // 保存任务成功完成后的返回值。
        // 复用对象时必须恢复默认值。
        src._result = default!;

        // _context：
        // 保存任务完成后的恢复位置。
        // 泛型任务源也必须与无返回值任务源保持一致。
        src._context = context;

        // _status：
        // 恢复为 pending 状态。
        src._status = 0;

        // _released：
        // 恢复为未归还对象池状态。
        src._released = 0;

        return src;
    }

    public static LBTaskSource<T> Rent()
    {
        // 参数：无。
        // 作用：兼容无参调用路径，默认捕获 SynchronizationContext.Current。
        // 多 World 要求：调用方必须已经处于 TaskContext.EnterScope() 内。
        return Rent(SynchronizationContext.Current);
    }

    private void Schedule(Action continuation)
    {
        // continuation：
        // await 后面需要恢复执行的逻辑。
        var ctx = _context;

        if (ctx != null)
        {
            // ctx.Post：
            // 把 continuation 投递回捕获到的上下文。
            ctx.Post(static state => ((Action)state!).Invoke(), continuation);
        }
        else
        {
            // 没有上下文时退回线程池。
            ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
        }
    }
}
```

---

## 8. 修订 LBTask.NextFrame

`NextFrame` 的核心修订点是：

```text
帧调度使用哪个 ctx
LBTaskSource 就必须捕获同一个 ctx
```

```csharp
public static LBTask NextFrame(
    SynchronizationContext? ctx = null,
    CancellationToken token = default)
{
    // ctx：
    // 目标同步上下文。
    // 多 World 下，它应该是目标 World 的 TaskContext。
    // 如果为 null，则使用 SynchronizationContext.Current。
    // 无参调用要求当前代码已经处于 World 的 TaskContext.EnterScope() 内。

    // token：
    // 取消标记。
    // 如果调用前已经取消，则直接返回取消任务。
    if (token.IsCancellationRequested)
        return FromCanceled(token);

    // ctx ??= SynchronizationContext.Current：
    // 如果调用方没有显式传入上下文，就捕获当前线程上下文。
    // 多 World 下，这依赖外层已经进入当前 World 的上下文作用域。
    ctx ??= SynchronizationContext.Current;

    // LBTaskSource.Rent(ctx)：
    // 关键修订。
    // 任务源必须捕获与帧调度相同的 ctx。
    // 否则会出现任务在一个上下文完成，continuation 却回到另一个上下文的问题。
    var src = LBTaskSource.Rent(ctx);

    if (ctx is LBTaskSynchronizationContext lbCtx)
    {
        // ScheduleInFrames(..., 1)：
        // 将任务完成动作安排到目标 World 的下一帧。
        // state 参数是 src，用于到期后调用 SetResult()。
        lbCtx.ScheduleInFrames(
            static state => ((LBTaskSource)state!).SetResult(),
            src,
            1);
    }
    else if (ctx != null)
    {
        // ctx.Post：
        // 非 LBTaskSynchronizationContext 的兼容路径。
        // 例如外部 UI 框架或宿主引擎自己的 SynchronizationContext。
        ctx.Post(
            static state => ((LBTaskSource)state!).SetResult(),
            src);
    }
    else
    {
        // ThreadPool.QueueUserWorkItem：
        // 没有上下文时退回线程池。
        // 该路径不具备 World 亲和性。
        ThreadPool.QueueUserWorkItem(
            static state => ((LBTaskSource)state!).SetResult(),
            src);
    }

    return new LBTask(src);
}
```

---

## 9. 修订 RunOnMainThread

### 9.1 无返回值版本

```csharp
public static LBTask RunOnMainThread(
    Action action,
    SynchronizationContext ctx)
{
    // action：
    // 需要投递到目标上下文执行的逻辑。

    // ctx：
    // 目标同步上下文。
    // 多 World 下，它应该是目标 World 的 TaskContext。
    if (action == null)
        throw new ArgumentNullException(nameof(action));

    if (ctx == null)
        throw new ArgumentNullException(nameof(ctx));

    // LBTaskSource.Rent(ctx)：
    // 任务源必须捕获目标 ctx。
    // 这样 action 执行完成后，await 后续逻辑也会回到同一个 World。
    var src = LBTaskSource.Rent(ctx);

    // RunActionWorkItem.Rent(action, src)：
    // 把 action 和任务源打包。
    // action 执行成功时调用 src.SetResult()。
    // action 执行失败时调用 src.SetException(ex)。
    var work = RunActionWorkItem.Rent(action, src);

    // ctx.Post：
    // 把 work 投递到目标上下文执行。
    ctx.Post(RunActionWorkItem.InvokeOnContext, work);

    return new LBTask(src);
}
```

### 9.2 有返回值版本

```csharp
public static LBTask<T> RunOnMainThread(
    Func<T> func,
    SynchronizationContext ctx)
{
    // func：
    // 需要投递到目标上下文执行，并返回 T 结果的函数。

    // ctx：
    // 目标同步上下文。
    // 多 World 下，它应该是目标 World 的 TaskContext。
    if (func == null)
        throw new ArgumentNullException(nameof(func));

    if (ctx == null)
        throw new ArgumentNullException(nameof(ctx));

    // LBTaskSource<T>.Rent(ctx)：
    // 泛型任务源也必须捕获目标 ctx。
    // 这样 func 执行完成后，await 后续逻辑会回到同一个 World。
    var src = LBTaskSource<T>.Rent(ctx);

    // RunFuncWorkItem.Rent(func, src)：
    // 把 func 和任务源打包。
    // func 执行成功时，把返回值写入 src。
    // func 执行失败时，把异常写入 src。
    var work = RunFuncWorkItem.Rent(func, src);

    // ctx.Post：
    // 把 work 投递到目标上下文执行。
    ctx.Post(RunFuncWorkItem.InvokeOnContext, work);

    return new LBTask<T>(src);
}
```

---

## 10. 修订 Delay

`Delay` 可以继续使用全局 Timer 或全局 DelayScheduler。

但是 `LBTaskSource` 必须捕获当前 World 的上下文。

```csharp
public static LBTask Delay(
    TimeSpan delay,
    CancellationToken token = default)
{
    // delay：
    // 需要等待的真实时间长度。
    // 当前设计下通常基于 Stopwatch 或 Timer，不绑定 World 帧时间。

    // token：
    // 取消标记。
    // 作用：允许调用方取消等待。
    if (delay <= TimeSpan.Zero)
        return CompletedTask;

    if (token.IsCancellationRequested)
        return FromCanceled(token);

    // LBTaskSource.Rent()：
    // 默认捕获 SynchronizationContext.Current。
    // 多 World 要求：调用方必须已经处于当前 World 的 TaskContext.EnterScope() 内。
    var src = LBTaskSource.Rent();

    // DelayWorkItem.Rent(src, token)：
    // 创建延迟任务的内部工作项。
    // src 用于到期后完成任务。
    // token 用于取消任务。
    var work = DelayWorkItem.Rent(src, token);

    // DelayScheduler.Schedule(work, delay)：
    // 把 work 放入全局延迟调度器。
    // 到期后调用 src.SetResult()。
    // src.SetResult() 会把 continuation 投递回捕获到的 World 上下文。
    DelayScheduler.Schedule(work, delay);

    // work.RegisterCancellation()：
    // 注册取消逻辑。
    // token 取消时会尝试移除 work，并把任务源设置为取消状态。
    work.RegisterCancellation();

    return new LBTask(src);
}
```

如果未来要支持 World 暂停、World 时间缩放、World 销毁自动取消，建议新增：

```csharp
world.Tasks.Delay(...)
```

并让它使用 World 自己的时间系统，而不是全局 Timer。

---

## 11. 修订 LBTaskMethodBuilder 的使用约束

`LBTaskMethodBuilder` 内部无法天然知道当前 World。

因此它可以继续使用：

```csharp
LBTaskSource.Rent()
```

但必须满足一个前提：

```text
async LBTask 方法首次挂起时，当前代码必须处于 World 的 TaskContext.EnterScope() 内
```

也就是说，正确性由 `World.Update()` 的作用域保证。

示例逻辑：

```csharp
public struct LBTaskMethodBuilder
{
    private LBTaskSource? _source;
    private bool _earlyCompleted;

    public LBTask Task
    {
        get
        {
            if (_earlyCompleted)
                return LBTask.CompletedTask;

            if (_source == null)
            {
                // LBTaskSource.Rent()：
                // 默认捕获 SynchronizationContext.Current。
                // 多 World 下，这要求当前代码位于 TaskContext.EnterScope() 内。
                _source = LBTaskSource.Rent();
            }

            return new LBTask(_source);
        }
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        // awaiter：
        // 当前 await 对象的 awaiter。
        // 作用：注册 continuation。

        // stateMachine：
        // 编译器生成的 async 状态机。
        // stateMachine.MoveNext 表示 await 完成后继续执行 async 方法。
        if (_source == null)
            _source = LBTaskSource.Rent();

        awaiter.OnCompleted(stateMachine.MoveNext);
    }
}
```

不建议在 `LBTaskMethodBuilder` 中强行引入全局 World 查找。

原因：

```text
Builder 是编译器调用的底层结构
如果在其中做复杂上下文查询，会增加任务系统成本
World.Update() 作用域已经可以解决默认捕获问题
显式 ctx API 可以解决 World 外部调用问题
```

---

## 12. 推荐增加 WorldTaskApi

为了避免 World 外部代码直接依赖 `SynchronizationContext.Current`，建议为 World 增加任务门面。

```csharp
public sealed class WorldTaskApi
{
    private readonly LBTaskSynchronizationContext _context;

    public WorldTaskApi(LBTaskSynchronizationContext context)
    {
        // context：
        // 当前 World 持有的任务上下文。
        // 作用：所有通过该 API 创建的任务都会绑定到这个 World。
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public LBTask NextFrame(CancellationToken token = default)
    {
        // token：
        // 取消标记。
        // 作用：允许等待下一帧前取消。
        return LBTask.NextFrame(_context, token);
    }

    public LBTask RunOnMainThread(Action action)
    {
        // action：
        // 需要投递回当前 World 主上下文执行的逻辑。
        // 作用：让后台线程可以安全回到当前 World。
        return LBTask.RunOnMainThread(action, _context);
    }

    public LBTask<T> RunOnMainThread<T>(Func<T> func)
    {
        // func：
        // 需要投递回当前 World 主上下文执行，并返回 T 的函数。
        return LBTask<T>.RunOnMainThread(func, _context);
    }
}
```

World 内部持有：

```csharp
public sealed class LayerWorld
{
    public LBTaskSynchronizationContext TaskContext { get; }
    public WorldTaskApi Tasks { get; }

    public LayerWorld()
    {
        // TaskContext：
        // 当前 World 独占的任务上下文。
        TaskContext = LBTaskSynchronizationContext.Install();

        // Tasks：
        // 绑定当前 World 上下文的任务 API。
        Tasks = new WorldTaskApi(TaskContext);
    }
}
```

推荐 World 外部调用：

```csharp
await world.Tasks.NextFrame();
```

不推荐 World 外部调用：

```csharp
await LBTask.NextFrame();
```

因为 World 外部通常没有可靠的 `SynchronizationContext.Current`。

---

## 13. 调用规则

### 13.1 World.Update 内部

可以使用无参 API：

```csharp
await LBTask.NextFrame();
await LBTask.Delay(TimeSpan.FromSeconds(1));
```

前提是当前代码处于：

```csharp
using var scope = world.TaskContext.EnterScope();
```

之内。

### 13.2 World 外部

必须显式指定目标 World：

```csharp
await world.Tasks.NextFrame();
```

或者：

```csharp
await LBTask.NextFrame(world.TaskContext);
```

### 13.3 跨 World 调度

如果确实需要从 World A 切换到 World B：

```csharp
await LBTask.NextFrame(worldB.TaskContext);
```

这表示：

```text
await 后续逻辑将恢复到 World B
```

这种行为属于显式跨 World 迁移，应谨慎使用。

---

## 14. 必要测试

### 14.1 同线程双 World 隔离

测试目标：

```text
World A 创建的 NextFrame 只能在 World A.Update() 后恢复
World B 创建的 NextFrame 只能在 World B.Update() 后恢复
```

### 14.2 Delay 回到原 World

测试目标：

```text
World A 中创建 Delay
Timer 线程完成任务
continuation 必须进入 World A 的 TaskContext 队列
```

### 14.3 RunOnMainThread 上下文一致

测试目标：

```text
action 在目标 World 执行
await 后续 continuation 也在目标 World 执行
```

### 14.4 async LBTask 方法捕获正确 World

测试目标：

```text
async LBTask 方法在 World A 内首次挂起
LBTaskMethodBuilder 创建的 LBTaskSource 捕获 World A
方法完成后，等待方 continuation 回到 World A
```

---

## 15. 迁移步骤

### 第一阶段：任务源支持显式上下文

1. 给 `LBTaskSource` 增加 `Rent(SynchronizationContext? context)`。
2. 给 `LBTaskSource<T>` 增加 `Rent(SynchronizationContext? context)`。
3. 保留无参 `Rent()`，内部调用 `Rent(SynchronizationContext.Current)`。

### 第二阶段：修订显式 ctx API

1. 修改 `LBTask.NextFrame(ctx)`，让 `LBTaskSource` 捕获同一个 `ctx`。
2. 修改 `LBTask.RunOnMainThread(action, ctx)`，让 `LBTaskSource` 捕获同一个 `ctx`。
3. 修改 `LBTask<T>.RunOnMainThread(func, ctx)`，让 `LBTaskSource<T>` 捕获同一个 `ctx`。

### 第三阶段：World Update 作用域

1. 给 `LBTaskSynchronizationContext` 增加 `EnterScope()`。
2. 修改 `World.Update()`，使用 `using var scope = TaskContext.EnterScope();`。
3. 确保每个 World 都拥有独立的 `TaskContext`。
4. 多 World 初始化禁止使用 `InstallAsCurrent()`。

### 第四阶段：API 收口

1. 增加 `WorldTaskApi`。
2. World 外部调用优先使用 `world.Tasks`。
3. 文档中明确标注无参 `LBTask.NextFrame()` 只适用于 World 上下文作用域内。

---

## 16. 最终结论

本次修订的核心不是让多个 `LBTaskSynchronizationContext` 同时成为 `SynchronizationContext.Current`。

这是不可能的。

正确做法是：

```text
每个 World 持有自己的 LBTaskSynchronizationContext
World.Update() 时临时切换 Current
LBTaskSource 捕获当前 World
任务完成后 continuation 回到捕获到的 World
Update 结束后恢复旧 Current
```

最终保证：

```text
任务在哪个 World 创建
任务就回到哪个 World 继续
```

这是多 World 下 `LBTask` 上下文行为可靠的关键。
