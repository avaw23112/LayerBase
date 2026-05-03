# PostFromAnyThread 策略合并设计文档

## 0. 目标

本文档只讨论 `PostFromAnyThread` 的设计。

核心目标：

```text
1. PostFromAnyThread 支持 Normal / DirtySignal / Latest / Coalesced 策略。
2. 不在 PostIngressQueue 中重复实现 Latest / Coalesced 合并逻辑。
3. 跨线程事件统一在 Runtime.Pump 中 Drain 到 PostScheduler。
4. Latest / Coalesced 仍然由 PostScheduler 处理，保证和主线程 Post 语义一致。
5. 明确“本帧参与合并”的边界。
6. 对“所有后台任务都必须参与合并”的情况，给出显式同步点设计。
```

本设计不做：

```text
- 不删除 PostScheduler 内部 lock
- 不修改 LayerHub.Post / Runtime.TryPost 的现有语义
- 不把 PostIngressQueue 做成第二套调度器
- 不在后台线程中直接执行 EventMetaData.TryMergePostEvent
- 不让 Runtime.Pump 默认等待未知后台任务
```

---

## 1. 问题背景

当前计划新增：

```text
PostFromAnyThread
TryPostFromAnyThread
```

用于从后台线程提交事件。

但如果后台线程大量提交事件，就会出现一个需求：

```text
后台线程提交的事件也应该能使用 Latest / Coalesced。
```

例如：

```text
多个后台线程提交 UI 状态刷新事件：
  只需要最后一次。

多个后台线程提交伤害统计事件：
  可以合并成一次。

多个后台线程提交路径计算结果：
  只需要最新版本。
```

因此 `PostFromAnyThread` 不能只是简单把所有事件无脑排队，否则会失去流程控制能力。

---

## 2. 设计结论

最终设计：

```text
PostFromAnyThread 不自己合并。

PostFromAnyThread 保存 EventPostPolicy。
Runtime.Pump 时 Drain 到 PostScheduler。
PostScheduler.TryPost 根据 policy 进入 Normal / DirtySignal / Latest / Coalesced 管线。
```

流程：

```text
后台线程
  -> PostFromAnyThread(value, policy)
  -> PostIngressQueue.Enqueue
  -> Runtime.Pump
  -> PostIngressQueue.DrainTo(PostScheduler)
  -> PostScheduler.TryPost(value, policy)
  -> PostScheduler 内部处理 Latest / Coalesced
  -> PostScheduler.Pump
  -> EventCenter.Send
```

这样可以保证：

```text
主线程 Post 和跨线程 Post 最终走同一套策略实现。
不会出现两套 Latest / Coalesced 行为。
```

---

## 3. 为什么不在 PostIngressQueue 中合并

不建议在 `PostIngressQueue` 内部做：

```text
ConcurrentDictionary<eventTypeId, latest>
ConcurrentDictionary<coalesceKey, coalescedSlot>
```

原因：

```text
1. 会重复 PostScheduler 已经存在的 Latest / Coalesced 管线。
2. 会让 Post 和 PostFromAnyThread 的语义分裂。
3. Coalesced 依赖 EventMetaData.TryMergePostEvent，应该在主线程统一执行。
4. 后台线程直接合并 payload 会引入额外线程安全问题。
5. PostIngressQueue 本应只是入口队列，不应变成第二个调度器。
6. 跨线程合并表需要锁或并发字典，会把复杂度转移到慢路径。
```

因此第一版只保留：

```text
PostIngressQueue:
  跨线程安全入口。

PostScheduler:
  统一策略执行者。
```

---

## 4. 关键概念：Drain Cut

### 4.1 定义

`Drain Cut` 指的是：

```text
Runtime.Pump 中某一次 Drain PostIngressQueue 的切面。
```

语义：

```text
在本次 Drain 中被取出的跨线程事件：
  会进入本帧 PostScheduler，
  并参与本帧 Latest / Coalesced。

本次 Drain 结束后才 Enqueue 的跨线程事件：
  进入下一次 Pump。
```

### 4.2 为什么需要 Drain Cut

主线程无法天然知道：

```text
所有后台线程是不是都已经提交完事件。
```

因为后台线程可能处于这些状态：

```text
线程 A 已经 Enqueue。
线程 B 正在计算，还没 Enqueue。
线程 C 即将 Enqueue。
主线程此时开始 Pump。
```

如果没有显式同步点，主线程不能等待“所有可能的后台线程”，因为这个集合没有边界。

所以普通 `PostFromAnyThread` 的合理语义只能是：

```text
Drain 到当前队列暂时为空。
已经进入队列并被本次 Drain 取出的事件参与本帧。
之后提交的事件进入下一帧。
```

这不是缺陷，而是帧循环系统中必须明确的边界。

---

## 5. Pump 中的 Drain 位置

必须满足：

```text
PostIngressQueue.DrainTo 必须早于 PostScheduler.Pump。
```

推荐顺序：

```text
Runtime.Pump
  1. Timer.Tick
  2. DelayManager.Tick
  3. CompletionQueue.Update
  4. PostIngressQueue.DrainTo(PostScheduler)
  5. PostScheduler.Pump
  6. LayerChain.Pump
```

原因：

```text
Timer / Delay / Completion 可能在本帧产生事件或完成后台任务。
Drain 放在 PostScheduler.Pump 前，可以让本帧已进入入口队列的跨线程事件参与本帧 Post 派发。
```

也可以放在 Pump 最开头：

```text
Runtime.Pump
  1. PostIngressQueue.DrainTo(PostScheduler)
  2. Timer.Tick
  3. DelayManager.Tick
  4. CompletionQueue.Update
  5. PostScheduler.Pump
  6. LayerChain.Pump
```

但推荐第一种：

```text
Timer / Delay / Completion
  -> Drain ingress
  -> PostScheduler.Pump
```

因为 CompletionQueue 可能包含后台任务的主线程完成回调，完成回调之后再 Drain，能更完整地收束本帧跨线程事件。

---

## 6. PostIngressQueue 设计

新增文件：

```text
LayerBase/Event/PostScheduler/PostIngressQueue.cs
```

代码草案：

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

---

## 7. LayerRuntime 接入

修改文件：

```text
LayerBase/Application/LayerRuntime.cs
```

### 7.1 增加字段

```csharp
/// <summary>
/// 跨线程 Post 入口队列。
///
/// 作用：
/// 非主线程提交的事件先进入这里，
/// Runtime.Pump 再统一搬运到 PostScheduler。
/// </summary>
private readonly PostIngressQueue _postIngress = new();
```

### 7.2 增加 PostFromAnyThread

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

### 7.3 增加 TryPostFromAnyThread

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

### 7.4 Pump 中 Drain

推荐放在 `PostScheduler.Pump` 之前。

```csharp
public void Pump(float deltaTime)
{
    if (_disposed)
    {
        return;
    }

    // 1. Timer.Tick
    // 2. Delay.Tick
    // 3. CompletionQueue.Update

    // 在 PostScheduler.Pump 前搬运跨线程事件。
    // 这样本次 Drain 取出的跨线程事件可以参与本帧 Latest / Coalesced。
    if (_scheduler != null)
    {
        _postIngress.DrainTo(_scheduler);
    }

    // 4. PostScheduler.Pump
    // 5. LayerChain.Pump
}
```

### 7.5 Dispose 清理

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

## 8. LayerHub 接入

修改文件：

```text
LayerBase/Application/LayerHub.cs
```

### 8.1 增加 PostFromAnyThread

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

### 8.2 增加 TryPostFromAnyThread

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

不要修改：

```text
LayerHub.Post
LayerHub.TryPost
```

原因：

```text
Post 是原有快路径。
PostFromAnyThread 是显式跨线程慢路径。
```

---

## 9. Latest / Coalesced 支持方式

跨线程事件通过 `policy` 支持策略。

示例：

```csharp
LayerHub.PostFromAnyThread(
    new HpChangedEvent
    {
        Value = 100
    },
    new EventPostPolicy(
        mode: PostDeliveryMode.Latest,
        backpressure: BackpressurePolicy.RejectNew,
        maxPending: 0));
```

Drain 时执行：

```csharp
scheduler.TryPost(_value, _policy);
```

因此该事件会进入 `PostScheduler` 的 `Latest` 管线。

Coalesced 同理：

```csharp
LayerHub.PostFromAnyThread(
    new DamageAccumulatedEvent
    {
        TargetId = 10,
        Damage = 5
    },
    new EventPostPolicy(
        mode: PostDeliveryMode.Coalesced,
        backpressure: BackpressurePolicy.RejectNew,
        maxPending: 0,
        mergeFailure: MergeFailurePolicy.FallbackToNormal));
```

它最终会进入 `PostScheduler` 的 `Coalesced` 管线。

---

## 10. 如何保证“所有并发任务都参与合并”

### 10.1 普通 PostFromAnyThread 不保证

普通 `PostFromAnyThread` 只能保证：

```text
本次 Drain 取出的事件参与本帧合并。
Drain 之后才进入队列的事件，进入下一帧。
```

它不能保证：

```text
所有后台线程都已经提交完。
```

因为 Runtime 不知道后台线程总数，也不知道它们是否还会继续提交。

---

### 10.2 需要显式同步点

如果业务要求：

```text
一批后台任务产生的所有事件必须参与本帧 Latest / Coalesced。
```

则需要高层任务系统提供同步点。

推荐概念：

```text
JobGroup
JobFence
ParallelCompletionBarrier
```

典型流程：

```text
1. 主线程启动一批后台任务
2. 后台任务执行计算
3. 后台任务调用 PostFromAnyThread
4. 主线程等待这批任务全部完成
5. 主线程 Drain PostIngressQueue
6. 主线程 PostScheduler.Pump
```

伪代码：

```csharp
// group：
//   一批后台任务的同步边界。
//   Complete 返回后，可以认为这批任务都已经提交完自己的 PostFromAnyThread。
var group = runtime.Jobs.CreateGroup();

group.Run(() =>
{
    runtime.PostFromAnyThread(
        new DamagePreviewEvent { Value = 10 },
        latestPolicy);
});

group.Run(() =>
{
    runtime.PostFromAnyThread(
        new DamagePreviewEvent { Value = 20 },
        latestPolicy);
});

// 等待这批任务全部完成。
// 注意：这不是 PostFromAnyThread 的职责，而是任务系统的职责。
group.Complete();

// 下一次 Pump 中，这批任务提交的事件都会参与 Drain Cut。
runtime.Pump(deltaTime);
```

### 10.3 不建议 Runtime.Pump 默认等待后台任务

不要让 `Runtime.Pump` 默认等待所有后台任务。

原因：

```text
1. 主线程可能被未知后台任务卡住。
2. Runtime 不知道哪些任务属于“本帧必须等待”。
3. 后台任务可能持续生产事件，没有天然终点。
4. 等待策略应该由业务显式声明。
```

因此：

```text
默认：Drain Cut。
需要强保证：JobGroup / Fence 显式同步。
```

---

## 11. 行为语义总结

### 11.1 普通跨线程投递

```text
PostFromAnyThread(value, policy)
  -> 进入 PostIngressQueue
  -> 下一次 Runtime.Pump Drain
  -> PostScheduler.TryPost(value, policy)
  -> 参与 PostScheduler 的 Latest / Coalesced
```

不保证所有后台线程已提交。

### 11.2 本帧合并边界

```text
本次 Drain 取出的事件：
  参与本帧 PostScheduler.Pump。

本次 Drain 之后 Enqueue 的事件：
  下一次 Pump 处理。
```

### 11.3 强同步合并

```text
JobGroup.Complete()
  -> 保证该组任务已提交完事件
  -> Runtime.Pump
  -> Drain
  -> PostScheduler.Pump
```

---

## 12. 推荐测试

### 12.1 Latest 测试

```text
- 后台线程多次 PostFromAnyThread Latest
- Pump 后只收到最后一次
```

### 12.2 Coalesced 测试

```text
- 后台线程多次 PostFromAnyThread Coalesced
- Pump 后进入 PostScheduler 合并
- 最终只派发一次合并结果
```

### 12.3 Drain Cut 测试

```text
- Drain 前已经进入队列的事件参与本帧
- Drain 后才进入队列的事件不参与本帧
```

### 12.4 原有 Post 不受影响

```text
- LayerHub.Post 仍然走原路径
- Runtime.TryPost 仍然走原路径
- 主线程 Post 不经过 PostIngressQueue
```

---

## 13. 最终结论

`PostFromAnyThread` 应该支持策略，但不应该自己实现策略。

最终设计是：

```text
PostFromAnyThread 保存 policy。
PostIngressQueue 只做跨线程入口。
Runtime.Pump 负责 Drain。
PostScheduler.TryPost 负责 Latest / Coalesced。
```

对于“所有并发任务都必须参与合并”的情况：

```text
PostFromAnyThread 本身不能保证。
必须由任务系统提供 JobGroup / Fence 等显式同步点。
```

这能保持：

```text
1. 主线程 Post 热路径不受影响。
2. 跨线程事件支持 Latest / Coalesced。
3. 策略实现只有 PostScheduler 一套。
4. 并发边界清晰。
5. 未来可以继续优化 PostScheduler 去锁。
```
