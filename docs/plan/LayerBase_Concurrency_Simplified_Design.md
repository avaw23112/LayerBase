# LayerBase 并发模型收缩版设计文档

## 1. 设计结论

`CallParallel` 暂缓实现。

原因是：

```text
CallParallel 要求数据天然线性。
CallParallel 要求切片之间相互独立。
CallParallel 要求结果可以自动归约成一个 response。
CallParallel 需要源生成器、运行时、LBTask、切片调度、异常传播、主线程完成等多层实现。
```

但真实游戏业务中，多线程任务通常更常见的是：

```text
资源加载
配置解析
网络解包
日志写入
后台保存
压缩 / 解压
单个 AI 寻路
单个计算任务
远程请求
数据库或文件 IO
```

这些任务通常是独立任务，而不是一批线性 payload 的切片计算。

因此当前版本的 LayerBase 并发模型应收缩为：

```text
SubscribeParallel
    粗放后台订阅。
    事件触发后把 handler 投递到后台线程。
    不收集业务结果。
    用户自己负责线程安全。

LBTask.RunBackground
    通用后台任务入口。
    用于执行独立后台任务。
    支持返回结果。
    支持 await。

LBTask.SwitchToMainThread
    主线程恢复点。
    让后台任务完成后回到 LayerRuntime 主线程继续业务流程。

MainThreadCompletionQueue
    后台线程到主线程的完成队列。
    主线程 Pump 时 drain。
    用于完成 LBTask continuation 或投递 Post 事件。
```

一句话总结：

```text
当前版本不做复杂的 CallParallel。
当前版本只提供更常用、更简单、更稳定的后台任务与主线程恢复能力。
```

---

## 2. 为什么暂缓 CallParallel

`CallParallel` 的理论价值很高，但适用面较窄。

它适合：

```text
大规模纯数据计算
批量 AI 评分
批量路径候选点评估
批量实体模拟
批量数值变换
```

但它不适合多数普通游戏业务。

普通游戏业务通常包含：

```text
对象引用
Service 状态
Layer 状态
场景对象
引擎主线程对象
复杂业务流程
副作用
顺序语义
```

如果为了使用 `CallParallel`，用户必须先把业务对象转换为线性 payload：

```text
业务对象
    -> 线性 payload
    -> 并行计算
    -> response
    -> 回写业务对象
```

那么转换成本可能超过并行收益。

因此结论是：

```text
CallParallel 保留为未来设计储备。
当前版本不实现。
```

---

## 3. 新并发模型的目标

新的并发模型目标是：

```text
1. 保持可用性第一。
2. 不引入复杂的切片 handler。
3. 不要求用户把业务强行转成线性数组。
4. 支持常见独立后台任务。
5. 支持 await 结果。
6. 支持后台完成后回主线程。
7. 不使用运行时反射。
8. 不破坏现有 SubscribeParallel。
```

---

## 4. 最终能力分层

```text
LayerBase 并发能力
    SubscribeParallel
        事件驱动的后台订阅。

    LBTask.RunBackground
        主动发起的后台任务。

    LBTask.SwitchToMainThread
        主线程恢复。

    MainThreadCompletionQueue
        后台到主线程的完成回收。
```

---

## 5. SubscribeParallel 定位

`SubscribeParallel` 保留。

它的语义应明确写清楚：

```text
SubscribeParallel 是 fire-and-forget 后台执行。
fire-and-forget 表示任务发出后，调用方不等待结果。
```

它适合：

```text
日志写入
后台打点
非关键缓存刷新
用户自己加锁保护的后台副作用
```

它不适合：

```text
需要返回值的任务
需要 await 的任务
需要主线程回调的任务
需要框架保证线程安全的任务
```

示例：

```csharp
[SubscribeParallel]
private void OnLogEvent(in LogEvent e)
{
    // e 参数：日志事件数据。
    // 该方法在后台线程执行。
    // 如果访问共享对象，用户必须自己加锁或使用线程安全结构。
    // 该方法不返回业务结果。
    _logWriter.Write(e.Message);
}
```

约束：

```text
SubscribeParallel 不保证线程安全。
SubscribeParallel 不收集业务结果。
SubscribeParallel 不保证执行顺序。
SubscribeParallel 只负责把任务放到后台执行，并捕获异常。
```

---

## 6. LBTask.RunBackground 定位

`LBTask.RunBackground` 是新的重点能力。

它解决的是：

```text
我有一个独立后台任务。
我希望它在后台执行。
我希望它完成后能 await 结果。
```

示例：

```csharp
var result = await LBTask.RunBackground(static () =>
{
    // 这里在后台线程执行。
    // 适合纯计算、文件解析、压缩、独立寻路等任务。
    return BuildPath();
});
```

如果需要回主线程继续操作：

```csharp
var result = await LBTask.RunBackground(static () =>
{
    // 后台线程执行。
    return LoadConfig();
});

await LBTask.SwitchToMainThread();

// 这里恢复到 LayerRuntime 主线程。
// 可以安全访问主线程对象。
ApplyConfig(result);
```

---

## 7. LBTask.RunBackground API 设计

### 7.1 无返回值后台任务

```csharp
public static LBTask RunBackground(Action action)
{
    // action 参数：要在后台线程执行的同步任务。
    // 返回值：表示后台任务完成状态的 LBTask。
    throw new NotImplementedException();
}
```

使用：

```csharp
await LBTask.RunBackground(static () =>
{
    // 后台执行的任务。
    SaveFile();
});
```

### 7.2 有返回值后台任务

```csharp
public static LBTask<TResult> RunBackground<TResult>(
    Func<TResult> func)
{
    // func 参数：要在后台线程执行的同步函数。
    // TResult 类型参数：函数返回值类型。
    // 返回值：可 await 的 LBTask<TResult>。
    throw new NotImplementedException();
}
```

使用：

```csharp
var config = await LBTask.RunBackground(static () =>
{
    // 后台线程读取和解析配置。
    return ConfigLoader.Load();
});
```

### 7.3 带取消令牌的后台任务

```csharp
public static LBTask<TResult> RunBackground<TResult>(
    Func<CancellationToken, TResult> func,
    CancellationToken cancellationToken)
{
    // func 参数：要在后台线程执行的同步函数。
    // cancellationToken 参数：取消令牌，用于协作式取消。
    // TResult 类型参数：函数返回值类型。
    // 返回值：可 await 的 LBTask<TResult>。
    throw new NotImplementedException();
}
```

说明：

```text
CancellationToken 是取消令牌。
它不能强制杀死线程。
任务需要主动检查 cancellationToken.IsCancellationRequested。
```

使用：

```csharp
var result = await LBTask.RunBackground(
    static cancellationToken =>
    {
        for (var i = 0; i < 100000; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return SearchResult.Cancelled;
            }

            // 执行后台计算。
            SearchStep(i);
        }

        return SearchResult.Completed;
    },
    cancellationToken);
```

---

## 8. LBTask.SwitchToMainThread 设计

`SwitchToMainThread` 用于把 await continuation 切回主线程。

```csharp
public static LBTask SwitchToMainThread()
{
    // 返回值：一个会在 LayerRuntime 主线程 Pump 阶段完成的 LBTask。
    // await 它之后，后续代码应运行在 LayerRuntime 主线程上下文中。
    throw new NotImplementedException();
}
```

使用：

```csharp
var data = await LBTask.RunBackground(static () =>
{
    return LoadData();
});

await LBTask.SwitchToMainThread();

// 这里应处于主线程。
// 可以安全访问 UI、Layer、Service 或引擎主线程对象。
UpdateUI(data);
```

---

## 9. 后台结果如何回主线程

后台线程不应该直接执行 await 后续逻辑。

推荐流程：

```text
后台 worker 执行任务
    -> 得到结果或异常
    -> 写入 MainThreadCompletionQueue
主线程 Pump
    -> drain MainThreadCompletionQueue
    -> 完成 LBTask
    -> await continuation 在主线程继续
```

这样可以保证：

```text
await RunBackground 后，如果任务选择回主线程完成，后续逻辑不会误跑在后台线程。
```

---

## 10. MainThreadCompletionQueue

`MainThreadCompletionQueue` 是后台线程到主线程的完成队列。

它应该是 MPSC 队列。

MPSC 是：

```text
Multiple Producers Single Consumer
多个生产者，一个消费者
```

在这里：

```text
多个生产者：
    后台 worker 线程。

一个消费者：
    LayerRuntime 主线程 Pump。
```

示意结构：

```csharp
internal sealed class MainThreadCompletionQueue
{
    // _queue 表示后台线程写入、主线程读取的完成队列。
    // 第一版可以使用 ConcurrentQueue，后续可替换成 MPSC RingBuffer。
    private readonly ConcurrentQueue<MainThreadCompletionItem> _queue = new();

    // Enqueue 表示从后台线程写入一个完成项。
    // item 参数：要回到主线程处理的完成项。
    public void Enqueue(MainThreadCompletionItem item)
    {
        _queue.Enqueue(item);
    }

    // Drain 表示主线程 Pump 时处理完成项。
    // maxCount 参数：本次最多处理多少个完成项，避免一帧处理过多。
    // 返回值：实际处理了多少个完成项。
    public int Drain(int maxCount)
    {
        var count = 0;

        while ((maxCount <= 0 || count < maxCount) &&
               _queue.TryDequeue(out var item))
        {
            item.Complete();
            count++;
        }

        return count;
    }
}
```

`ConcurrentQueue<T>` 是 .NET 提供的线程安全队列。

第一版使用它可以降低实现风险。

后续如果追求低分配和更强背压，可以换成有界 MPSC RingBuffer。

---

## 11. MainThreadCompletionItem

```csharp
internal readonly struct MainThreadCompletionItem
{
    // _complete 表示主线程执行的完成动作。
    // 它通常用于完成 LBTask promise。
    private readonly Action _complete;

    // complete 参数：主线程执行的完成动作。
    public MainThreadCompletionItem(Action complete)
    {
        _complete = complete;
    }

    // Complete 表示在主线程执行完成动作。
    public void Complete()
    {
        _complete();
    }
}
```

说明：

```text
第一版可以使用 Action，优先保证可用性。
后续如果需要低 GC，可以改成 TypeId + PayloadHandle + PromiseHandle。
```

---

## 12. RunBackground 内部流程

### 12.1 有返回值版本

```text
RunBackground(func)
    1. 创建 LBTaskCompletionSource<TResult>。
    2. 把 func 投递到后台 executor。
    3. 后台线程执行 func。
    4. 成功时得到 TResult。
    5. 异常时捕获 Exception。
    6. 把 SetResult / SetException 动作写入 MainThreadCompletionQueue。
    7. 主线程 Pump 时完成 LBTask。
```

示意代码：

```csharp
public static LBTask<TResult> RunBackground<TResult>(
    Func<TResult> func)
{
    // func 参数：后台线程执行的同步函数。
    // TResult 类型参数：返回值类型。
    // 返回值：可 await 的 LBTask<TResult>。

    var source = LBTaskCompletionSource<TResult>.Create();

    ParallelExecutor.TrySchedule(() =>
    {
        try
        {
            var result = func();

            MainThreadCompletions.Enqueue(
                new MainThreadCompletionItem(() =>
                {
                    source.SetResult(result);
                }));
        }
        catch (Exception exception)
        {
            MainThreadCompletions.Enqueue(
                new MainThreadCompletionItem(() =>
                {
                    source.SetException(exception);
                }));
        }
    });

    return source.Task;
}
```

说明：

```text
这是结构示意。
实际实现需要避免静态全局依赖，最好挂在 LayerRuntime 上。
```

---

## 13. ParallelExecutor

`ParallelExecutor` 是后台任务执行器。

第一版可以直接包装 `ThreadPool`。

```csharp
internal sealed class ParallelExecutor
{
    // TrySchedule 表示尝试提交一个后台任务。
    // action 参数：要在线程池执行的任务。
    // 返回 true 表示提交成功，false 表示提交失败。
    public bool TrySchedule(Action action)
    {
        // action 参数会在线程池线程上执行。
        return ThreadPool.QueueUserWorkItem(static state =>
        {
            var callback = (Action)state!;
            callback();
        }, action);
    }
}
```

后续可以替换为：

```text
固定 worker pool
有界任务队列
MPSC task queue
work-stealing executor
平台专用 job system adapter
```

第一版不要过度设计。

---

## 14. LayerRuntime.Pump 顺序

推荐 Pump 顺序：

```text
LayerRuntime.Pump(deltaTime)
    1. TimeScheduler.Tick(deltaTime)
    2. DelayBufferSystem.Tick(deltaTime)
    3. MainThreadCompletionQueue.Drain(maxCompletions)
    4. PostScheduler.Pump(postOptions)
    5. EventMetaDataHandler.PumpExpectations()
```

为什么 completion drain 在 Post 前？

```text
后台任务完成后，主线程可以先恢复 await continuation。
如果 continuation 内部 Post 了事件，PostScheduler 可以在同一帧继续处理。
```

如果担心 continuation 太重，可以给 completion drain 增加预算：

```text
MaxCompletionsPerPump
MaxCompletionMilliseconds
```

---

## 15. Frame Budget 与 Completion Drain

后台任务可能在同一帧大量完成。

如果主线程一次 drain 太多 completion，可能造成尖峰。

因此 `MainThreadCompletionQueue.Drain` 应支持预算。

```csharp
public readonly struct MainThreadCompletionPumpOptions
{
    // MaxCompletions 表示本次 Pump 最多处理多少个完成项。
    // 小于等于 0 表示不限制数量。
    public readonly int MaxCompletions;

    // MaxMilliseconds 表示本次 Pump 最多使用多少毫秒。
    // 小于等于 0 表示不限制时间。
    public readonly float MaxMilliseconds;

    // maxCompletions 参数：最多处理多少完成项。
    // maxMilliseconds 参数：最多使用多少毫秒。
    public MainThreadCompletionPumpOptions(
        int maxCompletions,
        float maxMilliseconds)
    {
        MaxCompletions = maxCompletions;
        MaxMilliseconds = maxMilliseconds;
    }
}
```

---

## 16. Backpressure

后台任务提交需要背压。

背压是：

```text
当生产速度超过消费速度时，系统如何处理新任务。
```

第一版建议简单策略：

```text
如果后台队列满：
    RunBackground 返回失败的 LBTask。
```

不要默认阻塞调用方。

```csharp
public enum BackgroundTaskBackpressurePolicy
{
    // RejectNew 表示拒绝新任务。
    // 适合作为默认策略。
    RejectNew,

    // Block 表示阻塞提交者，直到有容量。
    // 不建议游戏主线程默认使用。
    Block,

    // DropNewest 表示丢弃新任务。
    // 适合低价值后台任务。
    DropNewest
}
```

---

## 17. 异常处理

后台任务异常必须回到 `LBTask`。

```text
后台线程抛异常
    -> 捕获 Exception
    -> enqueue SetException
    -> 主线程 Pump
    -> LBTask 进入异常状态
    -> await 方观察异常
```

不应让异常直接逃出 ThreadPool。

不应静默吞掉异常。

---

## 18. 取消策略

取消采用协作式取消。

```csharp
var result = await LBTask.RunBackground(
    static cancellationToken =>
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Result.Cancelled;
        }

        return DoWork(cancellationToken);
    },
    cancellationToken);
```

说明：

```text
C# 不能安全强制杀死线程。
CancellationToken 只是一种通知机制。
任务必须主动检查它。
```

---

## 19. 禁止运行时反射

并发模型不应使用运行时反射。

明确禁止：

```csharp
Assembly.GetTypes()
Type.GetInterfaces()
Type.GetCustomAttributes()
MethodInfo.Invoke()
Activator.CreateInstance()
MakeGenericType()
MakeGenericMethod()
```

当前收缩版并发模型不需要源生成器注册 `CallParallel`，因此也不需要运行时扫描。

如果未来恢复 `CallParallel`，必须遵守：

```text
handler 发现、路由注册、async wrapper 生成都由源生成器在编译期完成。
运行时只执行生成器注册好的强类型路径。
```

---

## 20. 与 PostScheduler 的关系

后台任务结果有两种使用方式。

### 20.1 await 结果

```csharp
var result = await LBTask.RunBackground(static () =>
{
    return Compute();
});
```

### 20.2 回主线程后 Post 事件

```csharp
var result = await LBTask.RunBackground(static () =>
{
    return Compute();
});

await LBTask.SwitchToMainThread();

LayerHub.Post(new ComputeFinishedEvent(result));
```

第一版不建议后台任务直接自动 Post。

原因：

```text
await 模型更通用。
是否 Post 应由业务显式决定。
```

---

## 21. 与 SubscribeParallel 的关系

| 能力 | SubscribeParallel | LBTask.RunBackground |
|---|---|---|
| 触发方式 | 事件触发 | 主动调用 |
| 是否返回结果 | 否 | 是 |
| 是否可 await | 否 | 是 |
| 是否自动回主线程 | 否 | 可以 |
| 线程安全责任 | 用户负责 | 用户负责，但结果回收由框架处理 |
| 适合场景 | 后台副作用 | 独立后台任务 |

---

## 22. 暂不实现的能力

当前版本不实现：

```text
CallParallel
自动 payload 切片
ParallelTransformHandler
ParallelResultStore
多 handler 并行归约
work-stealing scheduler
嵌套并发调度
复杂取消树
```

这些能力保留为未来设计储备。

---

## 23. 第一版实施路线

### P0：文档化 SubscribeParallel

明确：

```text
SubscribeParallel 是 fire-and-forget。
不收集结果。
不保证线程安全。
```

### P1：实现 MainThreadCompletionQueue

用于后台任务完成后回主线程。

### P2：实现 LBTask.RunBackground

支持：

```text
无返回值后台任务
有返回值后台任务
异常回传
```

### P3：实现 LBTask.SwitchToMainThread

支持从后台恢复到 LayerRuntime 主线程。

### P4：接入 LayerRuntime.Pump

在 Pump 中 drain completion queue。

### P5：增加预算和背压

包括：

```text
MaxCompletionsPerPump
MaxBackgroundTasks
RejectNew 策略
```

---

## 24. 最终总结

当前版本 LayerBase 并发模型应保持简单：

```text
SubscribeParallel
    用于事件驱动的后台副作用。
    用户自己负责线程安全。

LBTask.RunBackground
    用于主动发起独立后台任务。
    支持 await 结果。

LBTask.SwitchToMainThread
    用于回到主线程继续业务流程。

MainThreadCompletionQueue
    用于后台结果回主线程。
```

不实现 `CallParallel`。

理由：

```text
CallParallel 使用场景偏窄。
它要求数据天然线性、切片独立、结果可归约。
实现和维护成本高。
当前阶段收益不如后台任务 + 主线程恢复模型稳定。
```

一句话：

```text
并发模型先服务最常见的独立后台任务，而不是少数纯线性切片计算场景。
```
