# LayerBase EventBucket 移除 Snapshot 修正迁移文档

## 1. 修改目标

本次修正目标是删除 `EventBucketSnapshot<T>`，保留 `MarkDirty / Rebuild`，并让 `EventBucket<T>` 在派发热路径中直接读取自身字段。

修改前：

```text
EventCenter.Send<T>
-> EventBucket<T>.Dispatch
-> EnsureClean()
-> EventBucketSnapshot<T>
-> snapshot 中的 handler 数组和计数字段
-> 执行 handler
```

修改后：

```text
EventCenter.Send<T>
-> EventBucket<T>.Dispatch
-> EnsureClean()
-> EventBucket<T> 自身字段
-> 执行 handler
```

这次修正不是删除重建机制，而是删除“并发快照发布”这层结构。

---

## 2. 架构前提

当前 LayerBase 的事件系统可以明确为：

```text
单线程主调度
Async 本质是单线程异步，不是多线程并发修改订阅表
Post 由 PostScheduler 统一调度
Time 由 TimeScheduler 负责
Delay 由 DelayPublisher 负责
Subscribe / Unsubscribe 不作为短期任务机制
SubscribeParallel 不应调用 Subscribe / Unsubscribe
CallParallel 与 Layer 隔离，不参与事件订阅表修改
```

因此 `EventBucketSnapshot<T>` 原本服务的并发读写场景不再是核心目标。

`EventBucketSnapshot<T>` 适合这种模型：

```text
一个线程正在 Dispatch
另一个线程可能 Subscribe / Unsubscribe
Subscribe / Unsubscribe 可能触发 Rebuild
Dispatch 必须继续读取旧快照
```

但 LayerBase 当前更适合：

```text
订阅关系在构建期、初始化期或同步安全点变化
MarkDirty 标记变更
下一次 Dispatch 前 Rebuild
Dispatch 直接读取 EventBucket<T> 字段
```

---

## 3. 保留与删除

### 3.1 保留

继续保留：

```text
MarkDirty
EnsureClean
Rebuild
RentArrays
ClearArrays
IdentifySpecializations
HandlerBucket<T>[] _buckets
连续派发数组
单 handler 快路径
小 fanout 快路径
```

这些结构仍然有价值。

`MarkDirty / Rebuild` 的作用不是只为并发安全服务，而是为了把多次订阅结构变化合并到下一次派发前统一重建。

### 3.2 删除

删除：

```text
EventBucketSnapshot<T>
_snapshot 字段
PublishDispatchSnapshot(...)
CopyPrefix(...)
Volatile.Write(ref _snapshot, ...)
所有以 snapshot 为参数的 Dispatch helper
```

---

## 4. 新名词说明

### Snapshot

`Snapshot` 是快照，表示某一刻派发数据的只读副本。

当前 `EventBucketSnapshot<T>` 保存：

```text
handler 数组
circuit 数组
handler 名称数组
handler 数量
快路径标记
mask 信息
```

删除后，这些数据直接由 `EventBucket<T>` 字段保存。

### HandlerBucket<T>

`HandlerBucket<T>` 是某个 Layer 对某个事件类型的原始订阅容器。

例如：

```text
Layer 0 对 DamageEvent 的订阅 -> HandlerBucket<DamageEvent>
Layer 1 对 DamageEvent 的订阅 -> HandlerBucket<DamageEvent>
```

`Rebuild` 会从这些源数据中生成连续派发数组。

### Rebuild

`Rebuild` 是重建派发缓存。

它会把分散在不同 Layer 的 handler 展平成连续数组，例如：

```text
_syncHandlers
_notifyHandlers
_subscribeHandlers
_flatParallel
```

### MarkDirty

`MarkDirty` 是标记派发缓存已失效。

调用 `Subscribe / Unsubscribe` 后，不立刻重建数组，只设置 `_isDirty = 1`，下一次 `Dispatch` 前统一 `Rebuild`。

---

## 5. 推荐字段结构

```csharp
private sealed class EventBucket<T> : IResetable where T : struct
{
    private readonly object _lock = new();

    /// <summary>
    /// 当前 EventBucket 所属的 EventCenter。
    ///
    /// 用于判断 BucketCache<T>.Instance 是否属于当前 EventCenter。
    /// 多 EventCenter 实例场景下，可以避免缓存串用。
    /// </summary>
    public readonly EventCenter? Owner;

    /// <summary>
    /// 当前 bucket 是否已经释放。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 派发缓存是否需要重建。
    ///
    /// 0：缓存干净。
    /// 1：订阅关系发生变化，下一次 Dispatch 前需要 Rebuild。
    /// </summary>
    private int _isDirty;

    /// <summary>
    /// 原始订阅数据。
    ///
    /// 数组下标是 layerIndex。
    /// 数组元素是该 Layer 对事件 T 的 handler 集合。
    /// </summary>
    private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();

    /// <summary>
    /// 同步 Flow handler 数组。
    ///
    /// Flow 指可以返回 EventHandledState，从而影响事件是否继续传播的处理模式。
    /// </summary>
    private EventHandleDelegate<T>[] _syncHandlers = Array.Empty<EventHandleDelegate<T>>();

    /// <summary>
    /// 同步 Flow handler 的熔断器数组。
    ///
    /// HandlerCircuit 用于记录 handler 是否禁用、是否处于异常保护状态。
    /// </summary>
    private HandlerCircuit[] _syncCircuits = Array.Empty<HandlerCircuit>();

    /// <summary>
    /// 同步 Flow handler 名称数组。
    ///
    /// 用于错误报告、调试信息和拓扑输出。
    /// </summary>
    private string[] _syncNames = Array.Empty<string>();

    /// <summary>
    /// 单线程异步 Flow handler 数组。
    ///
    /// 当前项目中的 Async 是单线程异步语义，不代表允许多线程修改订阅表。
    /// </summary>
    private EventHandleDelegateAsync<T>[] _asyncHandlers = Array.Empty<EventHandleDelegateAsync<T>>();

    private HandlerCircuit[] _asyncCircuits = Array.Empty<HandlerCircuit>();
    private string[] _asyncNames = Array.Empty<string>();

    /// <summary>
    /// 普通 Notify handler 数组。
    ///
    /// Notify 只接收通知，不影响事件传播状态。
    /// </summary>
    private EventNotifyDelegate<T>[] _notifyHandlers = Array.Empty<EventNotifyDelegate<T>>();

    private HandlerCircuit[] _notifyCircuits = Array.Empty<HandlerCircuit>();
    private string[] _notifyNames = Array.Empty<string>();

    /// <summary>
    /// 安全 Subscribe handler 数组。
    ///
    /// Subscribe 对应具备故障隔离语义的通知订阅。
    /// </summary>
    private EventNotifyDelegate<T>[] _subscribeHandlers = Array.Empty<EventNotifyDelegate<T>>();

    private HandlerCircuit[] _notifySafeCircuits = Array.Empty<HandlerCircuit>();
    private string[] _notifySafeNames = Array.Empty<string>();

    /// <summary>
    /// 并行 Notify handler 扁平数组。
    ///
    /// SubscribeParallel 可以并行执行 handler。
    /// 约束：SubscribeParallel handler 内不应调用 Subscribe / Unsubscribe。
    /// </summary>
    private ParallelHandlerEntry<T>[] _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();

    /// <summary>
    /// 各类 handler 的有效数量。
    ///
    /// 数组长度不等于有效数量，因为数组可能来自 ArrayPool。
    /// </summary>
    private int _syncCountTotal;
    private int _asyncCountTotal;
    private int _parallelCountTotal;
    private int _notifyCountTotal;
    private int _notifySafeCountTotal;

    /// <summary>
    /// 派发快路径标记。
    /// </summary>
    private bool _isSingleSync;
    private bool _isSingleNotify;
    private bool _isSingleNotifySafe;
    private bool _isSmallNotifyFanoutOnly;

    private EventHandleDelegate<T>? _singleSyncHandler;
    private HandlerCircuit? _singleSyncCircuit;
    private string? _singleSyncName;

    private EventNotifyDelegate<T>? _singleNotifyHandler;
    private HandlerCircuit? _singleNotifyCircuit;
    private string? _singleNotifyName;

    private EventNotifyDelegate<T>? _singleSubscribeHandler;
    private HandlerCircuit? _singleSubscribeCircuit;
    private string? _singleSubscribeName;

    /// <summary>
    /// 各类订阅 mask。
    ///
    /// mask 用 ulong 的每一位表示某个 Layer 是否存在对应 handler。
    /// </summary>
    private ulong _subscriberMask;
    private ulong _syncMask;
    private ulong _asyncMask;
    private ulong _parallelMask;
    private ulong _notifyMask;
    private ulong _notifySafeMask;

    public EventBucket(EventCenter center)
    {
        Owner = center;
    }
}
```

---

## 6. MarkDirty

推荐保留 `Volatile.Write` 版本：

```csharp
public void MarkDirty()
{
    // MarkDirty：
    // 标记派发缓存已经失效。
    //
    // 不在这里立刻 Rebuild。
    // 这样连续多次 Subscribe / Unsubscribe 时，只会在下一次 Dispatch 前重建一次。
    Volatile.Write(ref _isDirty, 1);
}
```

如果完全确认单线程，也可以写成：

```csharp
public void MarkDirty()
{
    // 单线程模型下普通字段写入即可。
    _isDirty = 1;
}
```

建议优先保留 `Volatile.Write`，语义更清楚，成本很低。

---

## 7. EnsureClean

推荐写法：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void EnsureClean()
{
    // 快路径：
    // 大多数 Dispatch 调用时订阅关系没有变化。
    if (Volatile.Read(ref _isDirty) == 0)
    {
        return;
    }

    // 单线程模型下，不需要生成不可变 Snapshot。
    // 直接重建 EventBucket<T> 内部的派发数组。
    Rebuild();

    // Rebuild 完成后清理脏标记。
    Volatile.Write(ref _isDirty, 0);
}
```

更激进的单线程版本：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void EnsureClean()
{
    if (_isDirty == 0)
    {
        return;
    }

    Rebuild();
    _isDirty = 0;
}
```

---

## 8. Rebuild 修正方向

### 8.1 删除快照发布

删除 Rebuild 末尾的：

```csharp
PublishDispatchSnapshot(
    newMask,
    newSyncMask,
    newAsyncMask,
    newParallelMask,
    newNotifyMask,
    newSubscribeMask);
```

改为直接写字段：

```csharp
_subscriberMask = newMask;
_syncMask = newSyncMask;
_asyncMask = newAsyncMask;
_parallelMask = newParallelMask;
_notifyMask = newNotifyMask;
_notifySafeMask = newSubscribeMask;

IdentifySpecializations();
```

---

### 8.2 推荐 Rebuild 骨架

```csharp
private void Rebuild()
{
    if (_disposed)
    {
        return;
    }

    int totalSync = 0;
    int totalAsync = 0;
    int totalParallel = 0;
    int totalNotify = 0;
    int totalSubscribe = 0;

    ulong newMask = 0;
    ulong newSyncMask = 0;
    ulong newAsyncMask = 0;
    ulong newParallelMask = 0;
    ulong newNotifyMask = 0;
    ulong newSubscribeMask = 0;

    // 第一轮：
    // 统计各类 handler 数量。
    // 这样可以提前准备足够大的连续数组。
    for (var layerIndex = 0; layerIndex < _buckets.Length; layerIndex++)
    {
        var bucket = _buckets[layerIndex];

        if (bucket == null || !bucket.HasHandlers)
        {
            continue;
        }

        int layerSyncCount = 0;
        int layerAsyncCount = 0;
        int layerParallelCount = bucket.MasterParallel.Count;
        int layerNotifyCount = 0;
        int layerSubscribeCount = 0;

        foreach (var handler in bucket.MasterOrdered)
        {
            if (handler.Circuit.IsDisabled)
            {
                continue;
            }

            if (handler.SyncHandler != null)
            {
                layerSyncCount++;
            }
            else if (handler.AsyncHandler != null)
            {
                layerAsyncCount++;
            }
        }

        foreach (var handler in bucket.MasterUnordered)
        {
            if (handler.Circuit.IsDisabled)
            {
                continue;
            }

            if (handler.SyncWrapper != null)
            {
                layerSyncCount++;
            }
            else if (handler.AsyncWrapper != null)
            {
                layerAsyncCount++;
            }
        }

        foreach (var handler in bucket.MasterNotify)
        {
            if (!handler.Circuit.IsDisabled)
            {
                layerNotifyCount++;
            }
        }

        foreach (var handler in bucket.MasterSubscribe)
        {
            if (!handler.Circuit.IsDisabled)
            {
                layerSubscribeCount++;
            }
        }

        totalSync += layerSyncCount;
        totalAsync += layerAsyncCount;
        totalParallel += layerParallelCount;
        totalNotify += layerNotifyCount;
        totalSubscribe += layerSubscribeCount;

        var bit = 1UL << layerIndex;

        if (layerSyncCount > 0)
        {
            newSyncMask |= bit;
        }

        if (layerAsyncCount > 0)
        {
            newAsyncMask |= bit;
        }

        if (layerParallelCount > 0)
        {
            newParallelMask |= bit;
        }

        if (layerNotifyCount > 0)
        {
            newNotifyMask |= bit;
        }

        if (layerSubscribeCount > 0)
        {
            newSubscribeMask |= bit;
        }

        if (layerSyncCount > 0 ||
            layerAsyncCount > 0 ||
            layerParallelCount > 0 ||
            layerNotifyCount > 0 ||
            layerSubscribeCount > 0)
        {
            newMask |= bit;
        }
    }

    // 根据统计结果准备数组容量。
    RentArrays(
        totalSync,
        totalAsync,
        totalNotify,
        totalSubscribe,
        totalParallel);

    int syncIndex = 0;
    int asyncIndex = 0;
    int parallelIndex = 0;
    int notifyIndex = 0;
    int subscribeIndex = 0;

    // 第二轮：
    // 将各个 Layer 的 handler 展平成连续数组。
    for (var layerIndex = 0; layerIndex < _buckets.Length; layerIndex++)
    {
        var bucket = _buckets[layerIndex];

        if (bucket == null || !bucket.HasHandlers)
        {
            continue;
        }

        foreach (var handler in bucket.MasterOrdered)
        {
            if (handler.Circuit.IsDisabled)
            {
                continue;
            }

            if (handler.SyncHandler != null)
            {
                _syncHandlers[syncIndex] = handler.SyncHandler;
                _syncCircuits[syncIndex] = handler.Circuit;
                _syncNames[syncIndex] = handler.FullName;
                syncIndex++;
            }
            else if (handler.AsyncHandler != null)
            {
                _asyncHandlers[asyncIndex] = handler.AsyncHandler;
                _asyncCircuits[asyncIndex] = handler.Circuit;
                _asyncNames[asyncIndex] = handler.FullName;
                asyncIndex++;
            }
        }

        foreach (var handler in bucket.MasterUnordered)
        {
            if (handler.Circuit.IsDisabled)
            {
                continue;
            }

            if (handler.SyncWrapper != null)
            {
                _syncHandlers[syncIndex] = handler.SyncWrapper;
                _syncCircuits[syncIndex] = handler.Circuit;
                _syncNames[syncIndex] = handler.FullName;
                syncIndex++;
            }
            else if (handler.AsyncWrapper != null)
            {
                _asyncHandlers[asyncIndex] = handler.AsyncWrapper;
                _asyncCircuits[asyncIndex] = handler.Circuit;
                _asyncNames[asyncIndex] = handler.FullName;
                asyncIndex++;
            }
        }

        foreach (var handler in bucket.MasterNotify)
        {
            if (handler.Circuit.IsDisabled)
            {
                continue;
            }

            _notifyHandlers[notifyIndex] = handler.Handler;
            _notifyCircuits[notifyIndex] = handler.Circuit;
            _notifyNames[notifyIndex] = handler.FullName;
            notifyIndex++;
        }

        foreach (var handler in bucket.MasterSubscribe)
        {
            if (handler.Circuit.IsDisabled)
            {
                continue;
            }

            _subscribeHandlers[subscribeIndex] = handler.Handler;
            _notifySafeCircuits[subscribeIndex] = handler.Circuit;
            _notifySafeNames[subscribeIndex] = handler.FullName;
            subscribeIndex++;
        }

        foreach (var handler in bucket.MasterParallel)
        {
            _flatParallel[parallelIndex] = handler;
            parallelIndex++;
        }
    }

    // 清理数组尾部，避免旧 handler 被对象引用保活。
    ClearArrays(
        syncIndex,
        asyncIndex,
        notifyIndex,
        subscribeIndex,
        parallelIndex);

    _syncCountTotal = syncIndex;
    _asyncCountTotal = asyncIndex;
    _parallelCountTotal = parallelIndex;
    _notifyCountTotal = notifyIndex;
    _notifySafeCountTotal = subscribeIndex;

    _subscriberMask = newMask;
    _syncMask = newSyncMask;
    _asyncMask = newAsyncMask;
    _parallelMask = newParallelMask;
    _notifyMask = newNotifyMask;
    _notifySafeMask = newSubscribeMask;

    // 根据当前 handler 数量刷新单 handler / 小 fanout 快路径。
    IdentifySpecializations();
}
```

---

## 9. Dispatch 修正方向

### 9.1 Dispatch 不再读取 Snapshot

修改前：

```csharp
public EventHandledState Dispatch(in T value)
{
    var snapshot = EnsureClean();

    if (snapshot.IsSingleNotify)
    {
        return DispatchSingleNotify(snapshot, in value);
    }

    return DispatchFull(snapshot, in value);
}
```

修改后：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public EventHandledState Dispatch(in T value)
{
    // 确保派发缓存是最新的。
    // 如果订阅关系没有变化，这里只做一次 _isDirty 判断。
    EnsureClean();

    // 单普通通知快路径。
    if (_isSingleNotify)
    {
        _singleNotifyHandler!(in value);
        return EventHandledState.Continue;
    }

    // 小 fanout 普通通知快路径。
    if (_isSmallNotifyFanoutOnly && _notifyCountTotal > 0)
    {
        DispatchSmallNotifyFanout(0, _notifyCountTotal, in value);
        return EventHandledState.Continue;
    }

    // 单安全通知快路径。
    if (_isSingleNotifySafe)
    {
        _singleSubscribeHandler!(in value);
        return EventHandledState.Continue;
    }

    // 单 Flow 快路径。
    if (_isSingleSync)
    {
        return _singleSyncHandler!(in value);
    }

    // 完整派发路径。
    return DispatchFull(in value);
}
```

---

### 9.2 DispatchSmallNotifyFanout

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void DispatchSmallNotifyFanout(int start, int count, in T value)
{
    // 将字段缓存到局部变量。
    // 好处：
    // 1. 减少循环内 this 字段读取。
    // 2. 更容易被 JIT 优化。
    // 3. 即使未来发生轻微重入，也能让当前循环读取同一组数组引用。
    var handlers = _notifyHandlers;
    var circuits = _notifyCircuits;

    var end = start + count;

    for (var i = start; i < end; i++)
    {
        if (circuits[i].IsDisabled)
        {
            continue;
        }

        handlers[i](in value);
    }
}
```

---

### 9.3 DispatchFull

以下是核心结构，实际项目中应接回现有 async、parallel、异常报告、Flow 截断逻辑。

```csharp
private EventHandledState DispatchFull(in T value)
{
    // 同步 Flow 派发。
    var syncHandlers = _syncHandlers;
    var syncCircuits = _syncCircuits;
    var syncCount = _syncCountTotal;

    for (var i = 0; i < syncCount; i++)
    {
        if (syncCircuits[i].IsDisabled)
        {
            continue;
        }

        var state = syncHandlers[i](in value);

        if (state == EventHandledState.Stop)
        {
            return state;
        }
    }

    // 普通 Notify 派发。
    var notifyHandlers = _notifyHandlers;
    var notifyCircuits = _notifyCircuits;
    var notifyCount = _notifyCountTotal;

    for (var i = 0; i < notifyCount; i++)
    {
        if (notifyCircuits[i].IsDisabled)
        {
            continue;
        }

        notifyHandlers[i](in value);
    }

    // 安全 Subscribe 派发。
    var subscribeHandlers = _subscribeHandlers;
    var subscribeCircuits = _notifySafeCircuits;
    var subscribeCount = _notifySafeCountTotal;

    for (var i = 0; i < subscribeCount; i++)
    {
        if (subscribeCircuits[i].IsDisabled)
        {
            continue;
        }

        subscribeHandlers[i](in value);
    }

    // Async 与 Parallel 逻辑按现有语义继续接入。
    // 注意：
    // Parallel handler 内不允许修改订阅结构。
    // 即不应调用 Subscribe / Unsubscribe。

    return EventHandledState.Continue;
}
```

---

## 10. ReturnArrays 修正

删除：

```csharp
Volatile.Write(ref _snapshot, EventBucketSnapshot<T>.Empty);
```

推荐：

```csharp
private void ReturnArrays()
{
    _singleSyncHandler = null;
    _singleSyncCircuit = null;
    _singleSyncName = null;

    _singleNotifyHandler = null;
    _singleNotifyCircuit = null;
    _singleNotifyName = null;

    _singleSubscribeHandler = null;
    _singleSubscribeCircuit = null;
    _singleSubscribeName = null;

    _isSingleSync = false;
    _isSingleNotify = false;
    _isSingleNotifySafe = false;
    _isSmallNotifyFanoutOnly = false;

    _syncCountTotal = 0;
    _asyncCountTotal = 0;
    _parallelCountTotal = 0;
    _notifyCountTotal = 0;
    _notifySafeCountTotal = 0;

    _subscriberMask = 0;
    _syncMask = 0;
    _asyncMask = 0;
    _parallelMask = 0;
    _notifyMask = 0;
    _notifySafeMask = 0;

    ReturnArrayHelper(ref _syncHandlers, ref _syncCircuits, ref _syncNames);
    ReturnArrayHelper(ref _asyncHandlers, ref _asyncCircuits, ref _asyncNames);
    ReturnArrayHelper(ref _notifyHandlers, ref _notifyCircuits, ref _notifyNames);
    ReturnArrayHelper(ref _subscribeHandlers, ref _notifySafeCircuits, ref _notifySafeNames);

    if (_flatParallel != null &&
        _flatParallel.Length > 0 &&
        _flatParallel != Array.Empty<ParallelHandlerEntry<T>>())
    {
        ArrayPool<ParallelHandlerEntry<T>>.Shared.Return(_flatParallel, true);
        _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();
    }
}
```

---

## 11. 订阅 API 约束建议

由于移除 Snapshot 后不再支持并发快照读，建议明确订阅 API 的边界：

```text
Subscribe / Unsubscribe 不是短期任务机制。
Subscribe / Unsubscribe 应在 Layer 构建期、初始化期或明确同步安全点调用。
不建议在事件 handler 内调用 Subscribe / Unsubscribe。
禁止在 SubscribeParallel handler 内调用 Subscribe / Unsubscribe。
```

未来可以进一步收紧：

```text
运行期 Subscribe / Unsubscribe 默认关闭
仅 Debug / Advanced 模式允许开启
Unsubscribe 标记为 Obsolete
短期任务统一使用 TimeScheduler / DelayPublisher / PostScheduler
```

---

## 12. 测试建议

### 12.1 保留测试

```text
Subscribe 后 Send 能触发 handler
多个 Layer 订阅后按顺序触发
MarkDirty 后下一次 Dispatch 前 Rebuild
Unsubscribe 后下一次 Dispatch 不再触发对应 handler
SingleNotify 快路径正确
SingleSync 快路径正确
SingleNotifySafe 快路径正确
SmallNotifyFanout 快路径正确
SubscribeParallel 正常执行
PostScheduler.Pump 后能通过 EventCenter.Send 派发
```

### 12.2 新增测试

```text
连续多次 Subscribe 只在下一次 Dispatch 前 Rebuild 一次
删除 Snapshot 后 Dispatch 仍能触发所有 handler
Remove 后 ClearArrays 不保留旧 handler 引用
SingleNotify / SingleSync / SingleNotifySafe 标记在 Rebuild 后正确刷新
```

### 12.3 不建议支持的测试

```text
Dispatch 过程中 Subscribe 立即影响当前派发
Dispatch 过程中 Unsubscribe 立即影响当前派发
SubscribeParallel handler 中调用 Subscribe / Unsubscribe
多线程同时 Dispatch 和 Subscribe
```

这些行为应视为非设计目标。

---

## 13. 推荐提交拆分

### Commit 1：删除 Snapshot 类型

```text
Remove EventBucketSnapshot
```

内容：

```text
删除 EventBucketSnapshot<T>
删除 _snapshot 字段
删除 PublishDispatchSnapshot
删除 CopyPrefix
```

### Commit 2：Rebuild 直接写 EventBucket 字段

```text
Write dispatch cache directly into EventBucket
```

内容：

```text
Rebuild 末尾直接写 mask 和 count
IdentifySpecializations 直接读取 EventBucket 字段
ReturnArrays 清空 EventBucket 字段
```

### Commit 3：Dispatch 改为直接字段派发

```text
Dispatch directly from EventBucket fields
```

内容：

```text
Dispatch 不再获取 snapshot
DispatchSingleNotify 不再接收 snapshot
DispatchSmallNotifyFanout 不再接收 snapshot
DispatchFull 不再接收 snapshot
循环内将字段缓存到局部变量
```

### Commit 4：补充订阅约束文档

```text
Document runtime subscription constraints
```

内容：

```text
说明 Subscribe / Unsubscribe 不是短期任务机制
说明 SubscribeParallel 内禁止修改订阅关系
说明运行期动态订阅不作为核心设计目标
```

---

## 14. 最终结构

修正后，`EventBucket<T>` 的核心结构应当是：

```text
EventBucket<T>
 ├─ HandlerBucket<T>[] _buckets
 │   └─ 原始订阅数据
 │
 ├─ _syncHandlers / _notifyHandlers / _subscribeHandlers / _flatParallel
 │   └─ Rebuild 后生成的连续派发数组
 │
 ├─ _syncCountTotal / _notifyCountTotal / ...
 │   └─ 有效 handler 数量
 │
 ├─ _isSingleNotify / _singleNotifyHandler / ...
 │   └─ 快路径缓存
 │
 ├─ _isDirty
 │   └─ 是否需要 Rebuild
 │
 └─ Dispatch
     └─ 直接读取 EventBucket 字段派发
```

不再包含：

```text
EventBucketSnapshot<T>
_snapshot
PublishDispatchSnapshot
CopyPrefix
snapshot 参数传递
snapshot 字段读取
```

---

## 15. 最终结论

在当前 LayerBase 设计前提下：

```text
单线程主调度
持久性订阅
PostScheduler 统一处理短期任务
Subscribe / Unsubscribe 不作为高频运行期机制
SubscribeParallel 不修改订阅结构
```

`EventBucketSnapshot<T>` 不再是必要结构。

建议删除 Snapshot，并让 `Rebuild` 直接写入 `EventBucket<T>` 字段。

收益：

```text
减少一次 snapshot 对象引用读取
减少快照对象分配
减少数组 CopyPrefix
减少 Dispatch helper 的 snapshot 参数传递
减少热路径中的间接字段访问
```

最终 Dispatch 热路径应收敛为：

```text
EnsureClean
-> EventBucket<T> 字段
-> handler 数组
-> handler 调用
```

这更符合 LayerBase 的高性能事件系统定位。
