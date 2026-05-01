# LayerBase PostScheduler 接管后的旧 Layer 异步传播链迁移指导

## 1. 文档目标

本文档用于指导 LayerBase 从旧的“按 Layer 异步传播事件”模型，迁移到新的 `PostScheduler` 统一调度模型。

当前新架构已经形成：

```text
LayerRuntime.Post<T>
-> PostScheduler.TryPost<T>
-> PostScheduler.Pump
-> EventCenter.Send<T>
-> EventBucket<T>.Dispatch
```

因此，旧链路中的以下组件已经成为遗留结构：

```text
EventCenter.Post<T>
EventBucket<T>.Post
InternalDispatchToLayer<T>
LayerEventQueue
IEventQueue
EnqueueEventInternal<T>
EnqueueEventBatchInternal<T>
UnmanagedList<T>
_eventPendingMask
WakeLayer
PumpLayer
```

本次迁移的核心目标是：

```text
彻底删除旧的 Layer 异步传播模型。
Post 只由 PostScheduler 接管。
EventCenter 只负责同步派发和订阅管理。
Layer 只作为组织器和顺序控制单元存在。
```

---

## 2. 当前架构判断

### 2.1 新 Post 主路径

当前公开的 `Post<T>` 主路径应当是：

```csharp
public void Post<T>(in T value) where T : struct
{
    // value：
    // 要异步投递的事件数据。
    //
    // Scheduler：
    // 当前 LayerRuntime 持有的 PostScheduler。
    // PostScheduler 负责事件缓冲、合并、背压和 Pump 调度。
    Scheduler.TryPost(value);
}
```

这意味着：

```text
Post<T> 不再直接进入 EventCenter.Post<T>。
Post<T> 不再直接进入某个 Layer 的队列。
Post<T> 不再表达“逐层传播”。
```

---

### 2.2 新 Pump 主路径

当前 `LayerRuntime.Pump(float deltaTime)` 的职责应当是：

```text
1. 推进 TimeScheduler
2. 推进 DelayPublisher
3. 推进 PostScheduler
4. 推进 LayerChain
```

推荐结构：

```csharp
public void Pump(float deltaTime)
{
    // _disposed：
    // 表示当前 Runtime 是否已经释放。
    // 如果已经释放，不能继续访问内部调度器和事件中心。
    if (_disposed)
    {
        return;
    }

    // _context：
    // LayerBaseSynchronizationContext。
    // 用于处理同步上下文中排队的回调。
    _context?.Update();

    // _timer：
    // 时间调度器。
    // 用于处理 SchedulePost 这类延迟事件。
    _timer?.Tick(deltaTime, _timerSink!);

    // DelayPublisherManager：
    // 延迟发布器管理器。
    // 用于处理 DelayPublisher 缓冲事件。
    DelayPublisherManager.Instance?.Tick(deltaTime);

    // _scheduler：
    // PostScheduler。
    // 用于处理 Post<T> 投递的事件。
    _scheduler?.Pump();

    // _chain：
    // LayerChain。
    // 用于推进普通 Layer 生命周期逻辑。
    _chain?.Pump(deltaTime);
}
```

---

## 3. 新旧模型对比

### 3.1 旧模型

旧模型是：

```text
EventCenter.Post<T>
-> EventBucket<T>.Post
-> EnqueueEventInternal<T>
-> LayerEventQueue
-> UnmanagedList<T>
-> PumpLayer
-> InternalDispatchToLayer<T>
-> EventBucket<T>.InternalDispatchToLayer
```

它的特点是：

```text
异步事件被投递到某个 Layer 队列。
每个 Layer 在自己的 Pump 时处理自己的队列。
事件可能沿 Layer 顺序逐层传播。
```

---

### 3.2 新模型

新模型是：

```text
LayerRuntime.Post<T>
-> PostScheduler.TryPost<T>
-> PostScheduler.Pump
-> EventCenter.Send<T>
-> EventBucket<T>.Dispatch
```

它的特点是：

```text
PostScheduler 统一缓冲事件。
PostScheduler 统一处理合并、Latest、Coalesced、背压、预算。
Pump 时通过 Send<T> 完整派发事件。
Layer 不再作为异步事件传播管道。
```

---

## 4. 新名词说明

### 4.1 PostScheduler

`PostScheduler` 是异步事件调度器。

它负责：

```text
接收 Post<T> 投递的事件
按照事件策略缓存事件
处理背压
处理 Latest 模式
处理 Coalesced 模式
控制每次 Pump 的处理数量
控制每次 Pump 的处理时间
最终将事件派发给 EventCenter
```

其中：

- `Latest` 指只保留最新事件。
- `Coalesced` 指同一类事件在同一轮调度中合并成一次处理。
- `Backpressure` 指当事件积压过多时如何处理，例如拒绝新事件、丢弃旧事件、丢弃新事件。
- `Pump` 指从调度器中取出待处理事件并执行派发。

---

### 4.2 InternalDispatchToLayer

`InternalDispatchToLayer<T>` 是旧模型中的内部方法。

它的旧职责是：

```text
只派发给指定 layerIndex 对应的 Layer。
```

在新的 `PostScheduler` 模型中，异步事件最终通过 `EventCenter.Send<T>` 做完整派发，不再需要“只派发到指定 Layer”的内部入口。

因此它应当删除。

---

### 4.3 LayerEventQueue

`LayerEventQueue` 是旧模型中的 Layer 层级事件队列。

它的旧职责是：

```text
为每个 Layer 保存异步事件队列。
在指定 Layer Pump 时处理该 Layer 的事件。
```

新模型下，异步事件队列由 `PostScheduler` 统一管理。

因此它应当删除。

---

### 4.4 UnmanagedList<T>

`UnmanagedList<T>` 是旧模型中用于保存事件数据的队列容器。

它的旧职责是：

```text
存储某个事件类型在某个 Layer 中等待处理的事件。
```

新模型下，事件缓冲由 `PostScheduler` 管理。

因此它应当删除，除非项目中还有其他非事件系统用途。

---

### 4.5 EventBucket<T>

`EventBucket<T>` 是某个事件类型对应的订阅者容器。

例如：

```text
DamageEvent -> EventBucket<DamageEvent>
MoveEvent   -> EventBucket<MoveEvent>
```

迁移后，`EventBucket<T>` 应当只负责：

```text
订阅者管理
同步派发 Dispatch
订阅快照 Rebuild
```

不再负责：

```text
Post 入队
按 Layer 异步传播
InternalDispatchToLayer
```

---

## 5. 迁移后的职责划分

### 5.1 LayerRuntime

`LayerRuntime` 负责对外提供运行时入口：

```text
Send<T>
Post<T>
SchedulePost<T>
Pump
For<TLayer>
CallAsync
```

推荐职责：

```text
Send<T>：直接调用 EventCenter.Send<T>
Post<T>：直接调用 PostScheduler.TryPost<T>
Pump：推进 Timer、Delay、PostScheduler、LayerChain
```

---

### 5.2 EventCenter

`EventCenter` 负责事件订阅与同步派发。

迁移后，它应当只保留：

```text
SubscribeFlow<T>
SubscribeAsync<T>
SubscribeParallel<T>
SubscribeNotify<T>
Subscribe<T>

UnsubscribeFlow<T>
UnsubscribeAsync<T>
UnsubscribeParallel<T>
UnsubscribeNotify<T>
Unsubscribe<T>

Send<T>
Reset
GetBucket<T>
```

不应再保留：

```text
Post<T>
WakeLayer
PumpLayer
InternalDispatchToLayer<T>
EnqueueEventInternal<T>
EnqueueEventBatchInternal<T>
LayerEventQueue
IEventQueue
```

---

### 5.3 PostScheduler

`PostScheduler` 负责异步事件缓冲与调度。

推荐职责：

```text
TryPost<T>
TryPost<T>(policy)
MarkDirty<T>
Pump
Dispose
```

`PostScheduler.Pump` 最终应调用：

```csharp
_eventCenter.Send(in value);
```

而不是：

```csharp
_eventCenter.Post(in value);
```

---

### 5.4 Layer

`Layer` 迁移后仍然保留。

它负责：

```text
组织模块
承载生命周期
注册订阅方法
决定订阅者的处理顺序
承载依赖注入
承载 Call 路由
参与拓扑分析
```

它不再负责：

```text
异步事件逐层传播
事件队列本地 Pump
Local 事件可见性
```

---

## 6. 删除清单

### 6.1 EventCenter 中删除

删除字段：

```csharp
// _eventPendingMask：
// 旧模型中用于记录哪些 Layer 有待处理事件。
// 新模型下，待处理事件由 PostScheduler 管理。
private long _eventPendingMask;

// _layerSlots：
// 旧模型中按 Layer 保存事件队列。
// 新模型下不再需要每个 Layer 一套事件队列。
private IEventQueue[] _layerSlots;

// _layerNames：
// 如果只服务于旧 LayerEventQueue 调试，也可以删除。
// 如果还有拓扑文档用途，则迁移到 LayerChain 或 LayerRuntime。
private string[] _layerNames;
```

删除方法：

```csharp
internal void EnsureSlots(int count, string name);
internal void Post<T>(in T value) where T : struct;
internal void WakeLayer(int layerIndex);
internal void PumpLayer(int layerIndex);
internal EventHandledState InternalDispatchToLayer<T>(int layerIndex, in Event<T> @event) where T : struct;
internal void EnqueueEventInternal<T>(int layerIndex, in Event<T> @event) where T : struct;
internal void EnqueueEventBatchInternal<T>(int layerIndex, ReadOnlySpan<Event<T>> events) where T : struct;
private static void AtomicSetBit(ref long mask, int bit);
private static void AtomicClearBit(ref long mask, int bit);
```

删除内部类型：

```csharp
private interface IEventQueue;
private sealed class LayerEventQueue;
```

---

### 6.2 EventBucket<T> 中删除

删除方法：

```csharp
public void Post(in T value);
internal EventHandledState InternalDispatchToLayer(int layerIndex, in Event<T> @event);
```

删除只服务旧 Post 的逻辑：

```text
向 EventCenter.EnqueueEventInternal 投递
向指定 Layer 投递
按 layerIndex 局部 dispatch
```

保留：

```csharp
public EventHandledState Dispatch(in T value);
```

如果存在：

```csharp
public EventHandledState Dispatch(in Event<T> @event);
```

也可以保留，但建议最终统一入口，减少重复派发路径。

---

### 6.3 Core.UnmanagedList 中删除

如果 `UnmanagedList<T>` 只被旧 Layer 异步队列使用，可以删除：

```text
IUnmanagedList
UnmanagedList<T>
```

同时删除对应 namespace 引用：

```csharp
using LayerBase.Core.UnmanagedList;
```

如果项目其他地方仍然使用 `UnmanagedList<T>`，则不要直接删除，应先确认用途。

---

### 6.4 测试中删除或重写

删除旧语义测试：

```text
Post 进入指定 Layer 队列
PumpLayer 只处理指定 Layer
InternalDispatchToLayer 只触发指定 Layer
事件逐层传播
_eventPendingMask 标记 Layer 唤醒
```

新增新语义测试：

```text
Post<T> 不会立即触发 handler
PostScheduler.Pump 后触发 handler
PostScheduler.Pump 通过 EventCenter.Send<T> 完整派发
PostLatest<T> 只派发最新值
PostCoalesced<T> 同一轮只派发一次
BackpressurePolicy.RejectNew 生效
BackpressurePolicy.DropOldest 生效
BackpressurePolicy.DropNewest 生效
```

---

## 7. 推荐修改后的 EventCenter 结构

```csharp
namespace LayerBase.Core.Event;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// 事件中心。
/// 
/// 迁移后职责：
/// 1. 管理事件订阅。
/// 2. 根据事件类型获取 EventBucket。
/// 3. 执行同步派发 Send。
/// 
/// 注意：
/// EventCenter 不再负责 Post 缓冲。
/// Post 缓冲由 PostScheduler 负责。
/// </summary>
public sealed class EventCenter
{
    /// <summary>
    /// 事件类型 ID 到事件桶的映射。
    /// 
    /// key：
    /// EventTypeId<TEvent>.Id。
    /// 
    /// value：
    /// EventBucket<TEvent>，但由于不同 TEvent 类型不同，这里只能用 object 保存。
    /// </summary>
    private readonly ConcurrentDictionary<int, object> _eventBuckets = new();

    /// <summary>
    /// 事件桶缓存重置器。
    /// 
    /// 作用：
    /// Reset 时清理 BucketCache<TEvent>.Instance。
    /// </summary>
    private readonly ConcurrentDictionary<int, Action> _bucketCacheResetters = new();

    /// <summary>
    /// Reset 期间的状态标记。
    /// 
    /// 1 表示正在 Reset。
    /// 0 表示正常运行。
    /// </summary>
    private int _isResetting;

    /// <summary>
    /// 同步发送事件。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// </typeparam>
    /// <param name="value">
    /// 要发送的事件数据。
    /// </param>
    /// <returns>
    /// 事件处理状态。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState Send<TEvent>(in TEvent value) where TEvent : struct
    {
        // 如果 EventCenter 正在 Reset，直接跳过派发。
        if (Volatile.Read(ref _isResetting) == 1)
        {
            return EventHandledState.Continue;
        }

        // 先走泛型静态缓存。
        // 命中时可以避免 ConcurrentDictionary 查找。
        var cached = BucketCache<TEvent>.Instance;

        // Owner 检查用于多 EventCenter 实例场景。
        if (cached != null && cached.Owner == this)
        {
            return cached.Dispatch(in value);
        }

        // 缓存未命中时，通过事件类型 ID 获取或创建 bucket。
        return GetBucket<TEvent>().Dispatch(in value);
    }

    /// <summary>
    /// 获取某个事件类型对应的事件桶。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// </typeparam>
    /// <returns>
    /// 当前事件类型对应的 EventBucket。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventBucket<TEvent> GetBucket<TEvent>() where TEvent : struct
    {
        // 读取泛型静态缓存。
        var cached = BucketCache<TEvent>.Instance;
        if (cached != null && cached.Owner == this)
        {
            return cached;
        }

        // 读取纯泛型静态事件 ID。
        var typeId = EventTypeId<TEvent>.Id;

        // 注册 Reset 回调。
        _bucketCacheResetters.TryAdd(
            typeId,
            static () => BucketCache<TEvent>.Instance = null);

        // 从字典获取或创建 EventBucket<TEvent>。
        var bucket = (EventBucket<TEvent>)_eventBuckets.GetOrAdd(
            typeId,
            // _：
            // ConcurrentDictionary 传入的 key。
            // 当前创建逻辑不需要使用 key，所以用 _ 表示忽略。
            _ => new EventBucket<TEvent>(this));

        // 写入泛型静态缓存。
        BucketCache<TEvent>.Instance = bucket;

        return bucket;
    }

    /// <summary>
    /// 每个事件类型自己的 bucket 缓存。
    /// 
    /// 说明：
    /// BucketCache<DamageEvent>.Instance
    /// BucketCache<MoveEvent>.Instance
    /// 是不同的静态字段。
    /// </summary>
    private static class BucketCache<TEvent> where TEvent : struct
    {
        public static EventBucket<TEvent>? Instance;
    }
}
```

---

## 8. 推荐修改后的 LayerRuntime.Post

```csharp
namespace LayerBase;

using System.Runtime.CompilerServices;

public sealed partial class LayerRuntime
{
    /// <summary>
    /// 异步投递事件。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// </typeparam>
    /// <param name="value">
    /// 要投递的事件数据。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<TEvent>(in TEvent value) where TEvent : struct
    {
        // Scheduler：
        // 当前 Runtime 的 PostScheduler。
        //
        // TryPost：
        // 尝试把事件写入调度器。
        // 具体是否接收事件，取决于当前事件类型的 PostPolicy 和 BackpressurePolicy。
        Scheduler.TryPost(value);
    }

    /// <summary>
    /// 异步投递 Latest 模式事件。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// </typeparam>
    /// <param name="value">
    /// 要投递的事件数据。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostLatest<TEvent>(in TEvent value) where TEvent : struct
    {
        // Latest：
        // 同一事件类型积压时，只保留最新值。
        Scheduler.TryPost(
            value,
            new EventPostPolicy(
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                0));
    }

    /// <summary>
    /// 异步投递 Coalesced 模式事件。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// </typeparam>
    /// <param name="value">
    /// 要投递的事件数据。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostCoalesced<TEvent>(in TEvent value) where TEvent : struct
    {
        // Coalesced：
        // 同一事件类型在同一轮 Pump 中合并为一次处理。
        Scheduler.TryPost(
            value,
            new EventPostPolicy(
                PostDeliveryMode.Coalesced,
                BackpressurePolicy.RejectNew,
                0));
    }
}
```

---

## 9. 推荐修改后的 PostScheduler.Pump 方向

以下是结构示例，重点是调用 `EventCenter.Send`，不要调用 `EventCenter.Post`。

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// PostScheduler 的派发方向示例。
/// </summary>
internal sealed partial class PostScheduler
{
    /// <summary>
    /// 处理已经投递的事件。
    /// </summary>
    /// <returns>
    /// 本轮 Pump 的统计信息。
    /// </returns>
    public PostPumpStats Pump()
    {
        // 1. 从 ready 队列取出事件。
        // 2. 根据 maxEventsPerPump 限制数量。
        // 3. 根据 maxMillisecondsPerPump 限制耗时。
        // 4. 根据 maxWavesPerPump 限制波次数量。
        // 5. 对每个事件调用 Dispatch。
        //
        // 注意：
        // 这里不应该再把事件交给 EventCenter.Post。
        // 否则会重新进入旧 Layer 异步队列模型。
        return PumpCore();
    }

    /// <summary>
    /// 派发一个事件。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// </typeparam>
    /// <param name="value">
    /// 要派发的事件数据。
    /// </param>
    private void Dispatch<TEvent>(in TEvent value) where TEvent : struct
    {
        // _eventCenter：
        // 当前调度器绑定的 EventCenter。
        //
        // Send：
        // 同步完整派发事件。
        // 这会触发所有订阅了 TEvent 的 handler。
        _eventCenter.Send(in value);
    }
}
```

---

## 10. 替换规则

### 10.1 EventCenter.Post<T>

替换前：

```csharp
EventCenter.Post(value);
```

替换后：

```csharp
Scheduler.TryPost(value);
```

或在 `PostScheduler.Pump` 内部：

```csharp
EventCenter.Send(value);
```

选择规则：

```text
用户调用 Post<T>：替换为 Scheduler.TryPost<T>
调度器最终派发：替换为 EventCenter.Send<T>
```

---

### 10.2 EventBucket<T>.Post

替换前：

```csharp
bucket.Post(in value);
```

替换后：

```csharp
// 用户投递事件时：
Scheduler.TryPost(value);

// 调度器派发事件时：
bucket.Dispatch(in value);
```

---

### 10.3 InternalDispatchToLayer

替换前：

```csharp
InternalDispatchToLayer(layerIndex, in eventValue);
```

替换后：

```csharp
Dispatch(in eventValue.Value);
```

如果 `Event<T>` 仍然是必要包装，则保留一个完整派发入口：

```csharp
Dispatch(in eventValue);
```

但不再传入 `layerIndex`。

---

### 10.4 PumpLayer

替换前：

```csharp
EventCenter.PumpLayer(layerIndex);
```

替换后：

```csharp
Scheduler.Pump();
```

如果某处确实要推进某个 Layer 的生命周期，应调用：

```csharp
LayerChain.Pump(deltaTime);
```

不要再用事件队列的 `PumpLayer` 表达生命周期推进。

---

## 11. 测试迁移建议

### 11.1 基础 Post 测试

```csharp
[Test]
public void Post_DoesNotDispatch_BeforePump()
{
    // runtime：
    // 当前测试用 LayerRuntime。
    var runtime = LayerHub.CreateLayers()
        .Push(new TestLayer())
        .Build();

    var count = 0;

    // SubscribeNotify：
    // 注册一个通知型事件处理函数。
    runtime.EventCenter.SubscribeNotify<TestEvent>(
        0,
        (in TestEvent _) => count++);

    // Post：
    // 只投递事件，不应立即触发 handler。
    runtime.Post(new TestEvent());

    Assert.That(count, Is.EqualTo(0));

    // Pump：
    // 推进调度器后才应触发 handler。
    runtime.Pump(0);

    Assert.That(count, Is.EqualTo(1));
}
```

---

### 11.2 Latest 测试

```csharp
[Test]
public void PostLatest_DispatchesOnlyLatestValue()
{
    var runtime = LayerHub.CreateLayers()
        .Push(new TestLayer())
        .Build();

    var lastValue = -1;

    runtime.EventCenter.SubscribeNotify<TestEvent>(
        0,
        (in TestEvent e) => lastValue = e.Value);

    runtime.PostLatest(new TestEvent { Value = 1 });
    runtime.PostLatest(new TestEvent { Value = 2 });
    runtime.PostLatest(new TestEvent { Value = 3 });

    runtime.Pump(0);

    Assert.That(lastValue, Is.EqualTo(3));
}
```

---

### 11.3 Coalesced 测试

```csharp
[Test]
public void PostCoalesced_DispatchesOncePerPump()
{
    var runtime = LayerHub.CreateLayers()
        .Push(new TestLayer())
        .Build();

    var count = 0;

    runtime.EventCenter.SubscribeNotify<TestEvent>(
        0,
        (in TestEvent _) => count++);

    runtime.PostCoalesced(new TestEvent());
    runtime.PostCoalesced(new TestEvent());
    runtime.PostCoalesced(new TestEvent());

    runtime.Pump(0);

    Assert.That(count, Is.EqualTo(1));
}
```

---

## 12. 风险点

### 12.1 旧测试可能失效

凡是依赖以下行为的测试都会失效：

```text
Post 后只触发某个 Layer
PumpLayer 后只处理某个 Layer 队列
事件沿 Layer 逐层传播
_eventPendingMask 标记待处理 Layer
```

这些测试应删除或改写。

---

### 12.2 调试信息可能减少

旧模型中 `_layerNames` 和 `layerIndex` 可能用于调试输出。

删除旧队列后，如果仍需要显示 Layer 名称，应迁移到：

```text
LayerRuntime
LayerChain
Layer 节点自身
拓扑快照 GetTopologyMarkdown
```

不要继续让 `EventCenter` 保存 `_layerNames`。

---

### 12.3 内部引用需要彻底清理

删除 `UnmanagedList<T>` 前，需要确认项目中没有其他用途。

推荐搜索：

```text
UnmanagedList
IUnmanagedList
InternalDispatchToLayer
EnqueueEventInternal
EnqueueEventBatchInternal
PumpLayer
WakeLayer
_eventPendingMask
```

如果只剩事件旧队列相关引用，可以删除。

---

## 13. 推荐提交拆分

建议拆成多个提交，便于回滚。

### Commit 1：移除 EventCenter.Post 入口

```text
Remove legacy EventCenter.Post path
```

内容：

```text
删除 EventCenter.Post<T>
修正所有内部调用到 Scheduler.TryPost 或 EventCenter.Send
```

---

### Commit 2：移除 LayerEventQueue

```text
Remove legacy LayerEventQueue
```

内容：

```text
删除 IEventQueue
删除 LayerEventQueue
删除 EnsureSlots 中的队列初始化
删除 PumpLayer
删除 WakeLayer
删除 _eventPendingMask
```

---

### Commit 3：移除 EventBucket 旧 Post 分支

```text
Remove EventBucket legacy post dispatch
```

内容：

```text
删除 EventBucket<T>.Post
删除 InternalDispatchToLayer
保留 Dispatch 完整派发
```

---

### Commit 4：移除 UnmanagedList

```text
Remove legacy unmanaged event queue
```

内容：

```text
删除 IUnmanagedList
删除 UnmanagedList<T>
删除 LayerBase.Core.UnmanagedList 引用
```

---

### Commit 5：更新测试和文档

```text
Update tests for PostScheduler-only post model
```

内容：

```text
删除旧 Local / Layer queue 测试
新增 PostScheduler 行为测试
更新 README 和迁移文档
```

---

## 14. 最终目标结构

迁移完成后，推荐结构为：

```text
LayerRuntime
 ├─ Send<T>
 │   └─ EventCenter.Send<T>
 │       └─ EventBucket<T>.Dispatch
 │
 ├─ Post<T>
 │   └─ PostScheduler.TryPost<T>
 │
 ├─ SchedulePost<T>
 │   └─ TimeScheduler
 │       └─ PostScheduler.TryPost<T>
 │
 └─ Pump
     ├─ TimeScheduler.Tick
     ├─ DelayPublisherManager.Tick
     ├─ PostScheduler.Pump
     │   └─ EventCenter.Send<T>
     └─ LayerChain.Pump
```

`EventCenter` 最终结构：

```text
EventCenter
 ├─ Subscribe / Unsubscribe
 ├─ Send<T>
 ├─ GetBucket<T>
 ├─ Reset
 └─ BucketCache<T>
```

不再包含：

```text
Post<T>
LayerEventQueue
IEventQueue
UnmanagedList<T>
InternalDispatchToLayer
PumpLayer
WakeLayer
```

---

## 15. 最终结论

当前项目已经切换到 `PostScheduler` 统一调度方向。

因此旧的 Layer 异步传播链已经没有继续保留的架构意义。

建议最终收敛为：

```text
Post 语义：由 PostScheduler 统一管理
Send 语义：由 EventCenter 同步完整派发
Layer 语义：负责组织、顺序、生命周期和拓扑
EventBucket 语义：负责订阅快照和同步 Dispatch
```

这样可以删除重复队列、减少 API 分裂、降低维护成本，并让 LayerBase 的事件系统语义更清晰。
