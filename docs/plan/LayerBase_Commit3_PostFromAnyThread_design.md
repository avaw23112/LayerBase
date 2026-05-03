# Commit 3：PostFromAnyThread 设计方案

## 0. 目标

本提交只新增一个显式的跨线程 Post 入口：

```text
PostFromAnyThread
TryPostFromAnyThread
```
目标是：

```text
主线程：
  LayerHub.Post / Runtime.Post / Runtime.TryPost
  继续走原来的 PostScheduler 路径。

其他线程：
  显式调用 PostFromAnyThread。
  先进入跨线程入口队列。
  Runtime.Pump 开头统一搬运到 PostScheduler。
```

---

## 1. 本提交不做的事

本提交只做 `PostFromAnyThread`，不做以下内容：

```text
- 不删除 PostScheduler._queueLock
- 不删除 PostScheduler._bufferLock
- 不增加 owner-thread 检查
- 不修改 LayerHub.Post
- 不修改 Runtime.TryPost
- 不修改 Send
- 不修改 Call
- 不优化 IngressPostItem 分配
- 不改变 PostScheduler 原有调度语义
```

这样改动最小，风险最低。

---

## 2. 新增文件

建议新增：

```text
LayerBase/Event/PostScheduler/PostIngressQueue.cs
```

`Ingress` 表示“入口”。  
这里指从非 Runtime 主线程来的事件，不直接碰 `PostScheduler`，先进入一个跨线程入口队列。

---

## 3. 新增 `PostIngressQueue`

```csharp
using System.Collections.Concurrent;

namespace LayerBase.Core.Event;

/// <summary>
/// 跨线程 Post 入口队列。
///
/// 作用：
/// 允许任意线程提交事件，但不让外部线程直接修改 PostScheduler 内部队列。
///
/// 注意：
/// 这是跨线程慢路径。
/// 主线程内的 LayerHub.Post / Runtime.Post / Runtime.TryPost 不经过这里。
/// </summary>
internal sealed class PostIngressQueue
{
    /// <summary>
    /// 跨线程入口队列。
    ///
    /// ConcurrentQueue：
    /// .NET 提供的线程安全队列。
    /// 这里允许多个线程同时 Enqueue。
    /// Runtime.Pump 是唯一消费者。
    /// </summary>
    private readonly ConcurrentQueue<IIngressPostItem> _queue = new();

    /// <summary>
    /// 从任意线程提交一个事件。
    /// </summary>
    /// <typeparam name="T">
    /// 事件类型。
    /// 必须是 struct，以保持和 LayerBase 当前事件系统一致。
    /// </typeparam>
    /// <param name="value">
    /// 事件数据。
    /// 这里会复制一份到 IngressPostItem 中，避免保存外部可变引用。
    /// </param>
    /// <param name="policy">
    /// 可选 Post 策略。
    /// null 表示最终进入 PostScheduler 后使用默认策略。
    /// </param>
    public void Enqueue<T>(in T value, EventPostPolicy? policy)
        where T : struct
    {
        _queue.Enqueue(new IngressPostItem<T>(value, policy));
    }

    /// <summary>
    /// 把跨线程入口队列中的事件搬运到 PostScheduler。
    /// </summary>
    /// <param name="scheduler">
    /// 当前 Runtime 的 PostScheduler。
    /// 所有事件最终都通过它进入原有 Post 管线。
    /// </param>
    /// <param name="maxCount">
    /// 本次最多搬运多少个事件。
    /// 小于等于 0 表示不限制。
    /// </param>
    /// <returns>
    /// 本次实际搬运的事件数量。
    /// </returns>
    public int DrainTo(PostScheduler scheduler, int maxCount = 0)
    {
        if (scheduler == null)
        {
            throw new ArgumentNullException(nameof(scheduler));
        }

        var count = 0;

        while ((maxCount <= 0 || count < maxCount) &&
               _queue.TryDequeue(out var item))
        {
            item.PostTo(scheduler);
            count++;
        }

        return count;
    }

    /// <summary>
    /// 清空入口队列。
    /// Runtime Dispose 或 Reset 时调用。
    /// </summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _))
        {
        }
    }
}

/// <summary>
/// 跨线程 Post 项的非泛型接口。
///
/// 作用：
/// PostIngressQueue 需要保存不同事件类型的投递项，
/// 所以用非泛型接口统一存储。
/// </summary>
internal interface IIngressPostItem
{
    /// <summary>
    /// 把事件重新投递到 PostScheduler。
    /// </summary>
    /// <param name="scheduler">
    /// 当前 Runtime 的 PostScheduler。
    /// </param>
    void PostTo(PostScheduler scheduler);
}

/// <summary>
/// 泛型跨线程 Post 项。
/// </summary>
/// <typeparam name="T">
/// 事件类型。
/// </typeparam>
internal sealed class IngressPostItem<T> : IIngressPostItem
    where T : struct
{
    /// <summary>
    /// 事件数据的副本。
    /// </summary>
    private readonly T _value;

    /// <summary>
    /// 可选 Post 策略。
    /// null 表示使用事件默认策略。
    /// </summary>
    private readonly EventPostPolicy? _policy;

    /// <summary>
    /// 创建跨线程 Post 项。
    /// </summary>
    /// <param name="value">
    /// 事件数据。
    /// 构造时复制，避免跨线程持有外部引用。
    /// </param>
    /// <param name="policy">
    /// 可选 Post 策略。
    /// null 表示使用事件默认策略。
    /// </param>
    public IngressPostItem(T value, EventPostPolicy? policy)
    {
        _value = value;
        _policy = policy;
    }

    /// <summary>
    /// 在 Runtime.Pump 中重新进入原有 PostScheduler 管线。
    /// </summary>
    /// <param name="scheduler">
    /// 当前 Runtime 的 PostScheduler。
    /// </param>
    public void PostTo(PostScheduler scheduler)
    {
        scheduler.TryPost(_value, _policy);
    }
}
```

这版会在跨线程路径上产生一次 `new IngressPostItem<T>` 分配。  
但它只发生在 `PostFromAnyThread` 慢路径，不影响主线程 `Post` 热路径。

---

## 4. 修改 `LayerRuntime`

修改文件：

```text
LayerBase/Application/LayerRuntime.cs
```

---

### 4.1 增加字段

```csharp
/// <summary>
/// 跨线程 Post 入口队列。
///
/// 作用：
/// 非主线程提交的事件先进入这里，
/// Runtime.Pump 开头再统一搬运到 PostScheduler。
/// </summary>
private readonly PostIngressQueue _postIngress = new();
```

---

### 4.2 增加 `PostFromAnyThread`

```csharp
/// <summary>
/// 从任意线程提交事件。
///
/// 这个方法不会立即派发事件。
/// 它只把事件放入跨线程入口队列，
/// 真正投递发生在下一次 Runtime.Pump。
/// </summary>
/// <typeparam name="T">
/// 事件类型。
/// 必须是 struct。
/// </typeparam>
/// <param name="value">
/// 事件数据。
/// </param>
/// <param name="policy">
/// 可选 Post 策略。
/// null 表示使用事件默认策略。
/// </param>
public void PostFromAnyThread<T>(
    in T value,
    EventPostPolicy? policy = default)
    where T : struct
{
    if (_disposed)
    {
        return;
    }

    _postIngress.Enqueue(value, policy);
}
```

---

### 4.3 增加 `TryPostFromAnyThread`

```csharp
/// <summary>
/// 从任意线程尝试提交事件。
/// </summary>
/// <typeparam name="T">
/// 事件类型。
/// 必须是 struct。
/// </typeparam>
/// <param name="value">
/// 事件数据。
/// </param>
/// <param name="policy">
/// 可选 Post 策略。
/// null 表示使用事件默认策略。
/// </param>
/// <returns>
/// true 表示已经进入跨线程入口队列。
/// false 表示 Runtime 已经释放。
/// </returns>
public bool TryPostFromAnyThread<T>(
    in T value,
    EventPostPolicy? policy = default)
    where T : struct
{
    if (_disposed)
    {
        return false;
    }

    _postIngress.Enqueue(value, policy);
    return true;
}
```

---

### 4.4 在 `Pump` 开头 Drain

只增加跨线程事件搬运，不改原有 `Pump` 里的其他逻辑。

```csharp
public void Pump(float deltaTime)
{
    if (_disposed)
    {
        return;
    }

    // 先搬运跨线程提交的事件。
    //
    // 这样外部线程只接触 PostIngressQueue，
    // PostScheduler 仍然由 Runtime.Pump 所在线程统一访问。
    if (_scheduler != null)
    {
        _postIngress.DrainTo(_scheduler);
    }

    // 后面保持现有 Pump 逻辑：
    // 1. Timer.Tick
    // 2. Delay.Tick
    // 3. CompletionQueue.Update
    // 4. PostScheduler.Pump
    // 5. LayerChain.Pump
}
```

注意：  
如果你希望跨线程事件在本帧参与 Timer/Delay 之后的 Post 派发，就放在 `Pump` 最开头。  
如果你希望它们永远比 Timer/Delay 晚一拍，可以放在 Timer/Delay 后。  
我建议放在最开头，因为语义最直观：

```text
其他线程提交 -> 下一次 Pump 立即进入 PostScheduler。
```

---

### 4.5 Dispose 时清空

```csharp
public void Dispose()
{
    if (_disposed)
    {
        return;
    }

    _disposed = true;

    // 清理尚未搬运到 PostScheduler 的跨线程事件。
    _postIngress.Clear();

    // 后面保持原有 Dispose 逻辑。
}
```

---

## 5. 修改 `LayerHub`

修改文件：

```text
LayerBase/Application/LayerHub.cs
```

---

### 5.1 增加静态 `PostFromAnyThread`

```csharp
/// <summary>
/// 从任意线程向 Primary Runtime 提交事件。
/// </summary>
/// <typeparam name="T">
/// 事件类型。
/// 必须是 struct。
/// </typeparam>
/// <param name="value">
/// 事件数据。
/// </param>
/// <param name="policy">
/// 可选 Post 策略。
/// null 表示使用事件默认策略。
/// </param>
public static void PostFromAnyThread<T>(
    in T value,
    EventPostPolicy? policy = default)
    where T : struct
{
    s_primaryRuntime?.PostFromAnyThread(value, policy);
}
```

---

### 5.2 增加静态 `TryPostFromAnyThread`

```csharp
/// <summary>
/// 从任意线程尝试向 Primary Runtime 提交事件。
/// </summary>
/// <typeparam name="T">
/// 事件类型。
/// 必须是 struct。
/// </typeparam>
/// <param name="value">
/// 事件数据。
/// </param>
/// <param name="policy">
/// 可选 Post 策略。
/// null 表示使用事件默认策略。
/// </param>
/// <returns>
/// true 表示已进入 Primary Runtime 的跨线程入口队列。
/// false 表示当前没有 Primary Runtime，或 Runtime 已释放。
/// </returns>
public static bool TryPostFromAnyThread<T>(
    in T value,
    EventPostPolicy? policy = default)
    where T : struct
{
    return s_primaryRuntime != null &&
           s_primaryRuntime.TryPostFromAnyThread(value, policy);
}
```

---

### 5.3 不修改 `LayerHub.Post`

不要改：

```csharp
LayerHub.Post<T>
LayerHub.TryPost<T>
```

也不要让 `LayerHub.Post<T>` 在非主线程时自动转发到 `PostFromAnyThread`。

原因：

```text
Post 是原有快路径。
PostFromAnyThread 是显式跨线程慢路径。
两者语义应保持区分。
```

---

## 6. 行为语义

### 6.1 `PostFromAnyThread` 不立即派发

流程：

```text
后台线程调用 PostFromAnyThread
  -> 进入 PostIngressQueue
  -> 等下一次 Runtime.Pump
  -> Drain 到 PostScheduler
  -> 再由 PostScheduler.Pump 派发
```

所以它至少延迟到下一次 `Pump`。

---

### 6.2 `policy` 语义保持原样

例如：

```csharp
LayerHub.PostFromAnyThread(
    new DamageEvent(),
    new EventPostPolicy(
        PostDeliveryMode.Latest,
        BackpressurePolicy.RejectNew,
        maxPending: 0));
```

最终仍然调用：

```csharp
PostScheduler.TryPost(value, policy)
```

因此：

```text
Normal
DirtySignal
Latest
Coalesced
```

这些原有投递模式都不需要重新实现。

---

### 6.3 不影响主线程 Post 性能

原来的：

```csharp
LayerHub.Post(value);
runtime.TryPost(value);
```

不经过 `PostIngressQueue`。

这个提交的额外成本只发生在：

```csharp
PostFromAnyThread(...)
```

---

## 7. 推荐测试

### 7.1 从后台线程 Post

```csharp
public struct TestEvent
{
    public int Value;
}

[Fact]
public void PostFromAnyThread_ShouldDispatchOnNextPump()
{
    // received：
    //   用于记录订阅者收到的事件值。
    var received = 0;

    LayerHub.Reset();

    var layer = new TestLayer(e => received = e.Value);

    LayerHub.CreateLayers()
            .Push(layer)
            .Build();

    // 从后台线程提交事件。
    var thread = new Thread(() =>
    {
        LayerHub.PostFromAnyThread(new TestEvent
        {
            Value = 10
        });
    });

    thread.Start();
    thread.Join();

    // 此时还没有 Pump，所以不应该收到事件。
    Assert.Equal(0, received);

    // Pump 后，PostIngressQueue 会被搬运到 PostScheduler。
    LayerHub.Pump(0.016f);

    // 事件已经派发。
    Assert.Equal(10, received);
}
```

---

### 7.2 Runtime 已释放时返回 false

```csharp
[Fact]
public void TryPostFromAnyThread_ShouldReturnFalse_WhenRuntimeDisposed()
{
    LayerHub.Reset();

    var runtime = LayerHub.CreateLayers()
                          .Push(new EmptyLayer())
                          .Build();

    runtime.Dispose();

    var result = runtime.TryPostFromAnyThread(new TestEvent
    {
        Value = 1
    });

    Assert.False(result);
}
```

---

### 7.3 不影响原有 Post

```csharp
[Fact]
public void NormalPost_ShouldStillUseExistingPath()
{
    var received = 0;

    LayerHub.Reset();

    var layer = new TestLayer(e => received = e.Value);

    LayerHub.CreateLayers()
            .Push(layer)
            .Build();

    LayerHub.Post(new TestEvent
    {
        Value = 20
    });

    LayerHub.Pump(0.016f);

    Assert.Equal(20, received);
}
```

---

## 8. 最终改动清单

```text
新增：
LayerBase/Event/PostScheduler/PostIngressQueue.cs

修改：
LayerBase/Application/LayerRuntime.cs
  - 增加 PostIngressQueue 字段
  - 增加 PostFromAnyThread
  - 增加 TryPostFromAnyThread
  - Pump 开头 Drain
  - Dispose 时 Clear

LayerBase/Application/LayerHub.cs
  - 增加 PostFromAnyThread
  - 增加 TryPostFromAnyThread
```

---

## 9. 最终效果

```text
主线程：
  LayerHub.Post / Runtime.TryPost
  仍走原路径。

后台线程：
  LayerHub.PostFromAnyThread
  -> PostIngressQueue
  -> Runtime.Pump
  -> PostScheduler
```

该提交的价值：

```text
1. 增加明确的跨线程 Post 入口。
2. 不改变原有 PostScheduler 实现。
3. 不影响主线程 Post 热路径。
4. 为后续 PostScheduler 去锁提供安全边界。
```
