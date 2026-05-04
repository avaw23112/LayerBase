# LayerBase 高危逻辑修复补丁文档

## 0. 本补丁范围

本文档只覆盖当前扫描出的高危逻辑问题，不包含热路径封装优化，不引入 owner-thread 动态检查，也不展开完整外部线程模型。

本补丁目标：

```text
1. 给 PostIngressQueue.DrainTo 增加每帧预算，避免跨线程事件拖死一帧。
2. 让 PostFromAnyThread 的 TryPost 失败结果不再被吞掉。
3. 统一 PostScheduler 事件容量扩展，修复 PrewarmEvent / AddSpecialPolicy 扩容不完整问题。
4. 修复 FlushBuffers 异常中断后 snapshot payload 可能泄漏的问题。
5. 补 PayloadStore / RuntimeId 复用相关测试。
6. 明确线程模型文档契约。
```

本补丁不做：

```text
- 不给 Send/Post/TryPost 加 owner-thread 动态检查
- 不做 RuntimeCommandQueue
- 不做完整外部线程模型
- 不做 NativeArray / buffer handle 抽象
- 不改热路径 API 结构
- 不修改 LayerHub.Post / Runtime.TryPost 的现有语义
```

---

## 1. 问题一：PostIngressQueue.DrainTo 无预算

### 1.1 当前问题

当前 `PostIngressQueue.DrainTo` 默认：

```csharp
public int DrainTo(PostScheduler scheduler, int maxCount = 0)
```

`maxCount <= 0` 表示无限 Drain。

`LayerRuntime.Pump` 当前调用：

```csharp
_postIngress.DrainTo(_scheduler);
```

这意味着如果后台线程持续向 `PostFromAnyThread` 提交事件，主线程可能一直卡在 Drain 阶段，导致：

```text
PostScheduler.Pump 无法及时执行
LayerChain.Pump 被延后
本帧耗时不可控
```

这不是死锁，但会导致帧饥饿。

### 1.2 修改目标

给跨线程入口添加每帧搬运预算。

### 1.3 修改 `PostSchedulerOptions`

修改文件：

```text
LayerBase/Event/PostScheduler/PostSchedulerOptions.cs
```

新增配置：

```csharp
public sealed class PostSchedulerOptions
{
    /// <summary>
    /// 每次 Runtime.Pump 最多从 PostFromAnyThread 入口搬运多少个事件。
    ///
    /// 作用：
    /// 防止后台线程持续生产事件时，主线程一直停留在 DrainTo 阶段。
    ///
    /// 0 或负数：
    /// 不限制搬运数量。
    ///
    /// 建议默认值：
    /// 4096。
    /// </summary>
    public int MaxIngressPostsPerPump { get; init; } = 4096;
}
```

如果 `PostSchedulerOptions` 当前是 `readonly struct` 或已有构造函数，则按现有风格补一个字段 / 参数即可。

### 1.4 修改 `LayerRuntime.Pump`

修改文件：

```text
LayerBase/Application/LayerRuntime.cs
```

把：

```csharp
_postIngress.DrainTo(_scheduler);
```

改为：

```csharp
_postIngress.DrainTo(
    _scheduler,
    _scheduler.Options.MaxIngressPostsPerPump);
```

带注释版本：

```csharp
// 在 PostScheduler.Pump 前搬运跨线程事件。
// MaxIngressPostsPerPump 用于限制本帧最多搬运多少个跨线程事件，
// 防止后台线程持续生产事件导致主线程一帧被拖死。
_postIngress.DrainTo(
    _scheduler,
    _scheduler.Options.MaxIngressPostsPerPump);
```

---

## 2. 问题二：PostFromAnyThread 的 TryPost 失败结果被吞掉

### 2.1 当前问题

当前 `IIngressPostItem.PostTo` 返回 `void`：

```csharp
internal interface IIngressPostItem
{
    void PostTo(PostScheduler scheduler);
}
```

`IngressPostItem<T>.PostTo` 内部调用：

```csharp
scheduler.TryPost(_value, _policy);
```

但结果被丢弃。

这会吞掉：

```text
Scheduler disposed
Event type not registered
Queue full
MaxPending reached
Unsupported policy
```

对于后台线程提交的事件，失败被吞后非常难排查。

### 2.2 修改目标

让 `PostTo` 返回 `PostResult`，让 `DrainTo` 至少统计失败数量。

### 2.3 新增 `PostIngressDrainResult`

修改文件：

```text
LayerBase/Event/PostScheduler/PostIngressQueue.cs
```

新增：

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// PostIngressQueue 一次 Drain 的结果。
/// </summary>
internal readonly struct PostIngressDrainResult
{
    /// <summary>
    /// drained:
    ///   本次从跨线程入口队列取出的事件数量。
    ///
    /// failed:
    ///   本次搬运后调用 PostScheduler.TryPost 失败的数量。
    /// </summary>
    public PostIngressDrainResult(int drained, int failed)
    {
        Drained = drained;
        Failed = failed;
    }

    /// <summary>
    /// 本次实际取出的事件数量。
    /// </summary>
    public int Drained { get; }

    /// <summary>
    /// 本次投递失败的事件数量。
    /// </summary>
    public int Failed { get; }
}
```

### 2.4 修改 `IIngressPostItem`

把：

```csharp
internal interface IIngressPostItem
{
    void PostTo(PostScheduler scheduler);
}
```

改为：

```csharp
internal interface IIngressPostItem
{
    /// <summary>
    /// 把事件重新投递到 PostScheduler。
    /// </summary>
    /// <param name="scheduler">
    /// 当前 Runtime 的 PostScheduler。
    /// </param>
    /// <returns>
    /// PostScheduler.TryPost 的结果。
    /// </returns>
    PostResult PostTo(PostScheduler scheduler);
}
```

### 2.5 修改 `IngressPostItem<T>`

把：

```csharp
public void PostTo(PostScheduler scheduler)
{
    scheduler.TryPost(_value, _policy);
}
```

改为：

```csharp
public PostResult PostTo(PostScheduler scheduler)
{
    return scheduler.TryPost(_value, _policy);
}
```

### 2.6 修改 `DrainTo`

把返回值从 `int` 改为 `PostIngressDrainResult`：

```csharp
public PostIngressDrainResult DrainTo(PostScheduler scheduler, int maxCount = 0)
{
    if (scheduler == null)
    {
        throw new ArgumentNullException(nameof(scheduler));
    }

    var drained = 0;
    var failed = 0;

    while ((maxCount <= 0 || drained < maxCount) &&
           _queue.TryDequeue(out var item))
    {
        var result = item.PostTo(scheduler);

        if (!result.Success)
        {
            failed++;
        }

        drained++;
    }

    return new PostIngressDrainResult(drained, failed);
}
```

如果 `PostResult` 当前不是 `Success` 属性，而是 `IsSuccess` 或其他命名，按实际类型调整。

### 2.7 Runtime 暂不上报，只保留结果

`LayerRuntime.Pump` 可以先忽略返回值：

```csharp
_ = _postIngress.DrainTo(
    _scheduler,
    _scheduler.Options.MaxIngressPostsPerPump);
```

后续如果需要可在 Debug 模式下上报：

```csharp
var ingressResult = _postIngress.DrainTo(
    _scheduler,
    _scheduler.Options.MaxIngressPostsPerPump);

if (IsDebugMode && ingressResult.Failed > 0)
{
    ReportWarning(
        -1,
        "PostIngressQueue",
        "DrainTo",
        $"PostFromAnyThread drain failed: {ingressResult.Failed}/{ingressResult.Drained}");
}
```

---

## 3. 问题三：PrewarmEvent / AddSpecialPolicy 扩容不完整

### 3.1 当前问题

当前 `AddSpecialPolicy` 会扩 `_postPlans`，重建 `_postBitmap`，并更新 `_sealedMaxEventTypeId`。

但它没有统一扩展：

```text
_dirtyPendingBits
_latestPendingBits
_dirtySnapshotBits
_latestSnapshotBits
_latestBuffer
_latestSnapshotBuffer
_pendingCount
```

`PrewarmEvent<T>` 也存在类似问题：它只确保 store 和 `_postPlans`，没有完整更新 `_sealedMaxEventTypeId`、bitmap、latest/dirty buffer。

这会导致：

```text
计划表认为事件已注册
但 Latest / Dirty / Pending buffer 没有扩容
最终 TryPostLatest / MarkDirty / MaxPending 出现不一致行为
```

### 3.2 修改目标

新增统一容量保障方法：

```csharp
private void EnsureEventCapacity(int typeId)
```

所有事件注册入口都必须调用它。

### 3.3 新增 `EnsureEventCapacity`

修改文件：

```text
LayerBase/Event/PostScheduler/PostScheduler.cs
```

代码草案：

```csharp
private void EnsureEventCapacity(int typeId)
{
    // typeId：
    //   EventTypeId<TEvent>.Id 得到的运行期事件类型 ID。
    //
    // 作用：
    //   统一确保 PostScheduler 内部所有按事件 ID 索引的结构都有足够容量。
    //
    // 注意：
    //   这个方法用于 Build / Prewarm / AddSpecialPolicy / RegisterPostEvent。
    //   不建议放在极热的 TryPost 快路径里反复调用。

    if (typeId < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(typeId));
    }

    var requiredLength = typeId + 1;

    if (_postPlans.Length < requiredLength)
    {
        var newPlans = new PostTypePlan[NextPowerOfTwo(requiredLength)];
        Array.Copy(_postPlans, newPlans, _postPlans.Length);

        for (var i = _postPlans.Length; i < newPlans.Length; i++)
        {
            newPlans[i] = PostTypePlan.Default(i, _defaultBackpressure);
        }

        _postPlans = newPlans;
    }

    if (_pendingCount.Length < requiredLength)
    {
        var newPending = new int[NextPowerOfTwo(requiredLength)];
        Array.Copy(_pendingCount, newPending, _pendingCount.Length);
        _pendingCount = newPending;
    }

    if (_latestBuffer.Length < requiredLength)
    {
        var oldLength = _latestBuffer.Length;
        var newLength = NextPowerOfTwo(requiredLength);

        var newLatest = new PayloadHandle[newLength];
        Array.Copy(_latestBuffer, newLatest, oldLength);
        for (var i = oldLength; i < newLength; i++)
        {
            newLatest[i] = PayloadHandle.Invalid;
        }
        _latestBuffer = newLatest;

        var newLatestSnapshot = new PayloadHandle[newLength];
        Array.Copy(_latestSnapshotBuffer, newLatestSnapshot, _latestSnapshotBuffer.Length);
        for (var i = _latestSnapshotBuffer.Length; i < newLength; i++)
        {
            newLatestSnapshot[i] = PayloadHandle.Invalid;
        }
        _latestSnapshotBuffer = newLatestSnapshot;
    }

    var requiredSegments = (typeId >> 6) + 1;

    EnsureUlongArrayCapacity(ref _dirtyPendingBits, requiredSegments);
    EnsureUlongArrayCapacity(ref _latestPendingBits, requiredSegments);
    EnsureUlongArrayCapacity(ref _dirtySnapshotBits, requiredSegments);
    EnsureUlongArrayCapacity(ref _latestSnapshotBits, requiredSegments);

    if (typeId > _sealedMaxEventTypeId)
    {
        _sealedMaxEventTypeId = typeId;
    }

    RebuildPostBitmap();
}
```

辅助方法：

```csharp
private static void EnsureUlongArrayCapacity(ref ulong[] array, int requiredLength)
{
    if (array.Length >= requiredLength)
    {
        return;
    }

    var newArray = new ulong[NextPowerOfTwo(requiredLength)];
    Array.Copy(array, newArray, array.Length);
    array = newArray;
}

private static int NextPowerOfTwo(int value)
{
    if (value <= 1)
    {
        return 1;
    }

    value--;
    value |= value >> 1;
    value |= value >> 2;
    value |= value >> 4;
    value |= value >> 8;
    value |= value >> 16;
    value++;

    return value;
}
```

如果项目已有类似 `ArrayUtil.EnsureCapacity` 或 `BitHelper.NextPowerOfTwo`，优先复用项目已有工具。

### 3.4 修改 `BuildPlans`

`BuildPlans` 构造计划时，不要手写多处扩容逻辑。

应当：

```csharp
foreach (var plan in plans)
{
    EnsureEventCapacity(plan.EventTypeId);
    _postPlans[plan.EventTypeId] = plan;
}
```

然后最后统一：

```csharp
RebuildPostBitmap();
```

如果 `EnsureEventCapacity` 内部已经 Rebuild，则为了避免重复 Rebuild，可以拆成：

```text
EnsureEventCapacity(typeId, rebuildBitmap: false)
...
RebuildPostBitmap()
```

推荐：

```csharp
private void EnsureEventCapacity(int typeId, bool rebuildBitmap)
```

BuildPlans 中传 `false`，循环结束后一次 Rebuild。

### 3.5 修改 `AddSpecialPolicy`

`AddSpecialPolicy` 必须：

```csharp
public void AddSpecialPolicy<TEvent>(EventPostPolicy policy)
    where TEvent : struct
{
    var typeId = EventTypeId<TEvent>.Id;

    EnsureEventCapacity(typeId, rebuildBitmap: false);

    _postPlans[typeId] = new PostTypePlan(
        typeId,
        policy.Mode,
        policy.Backpressure,
        policy.MaxPending,
        _defaultBackpressure,
        policy.MergeFailure);

    RebuildPostBitmap();
}
```

### 3.6 修改 `PrewarmEvent<T>`

`PrewarmEvent<T>` 必须真正注册默认普通策略：

```csharp
public void PrewarmEvent<TEvent>()
    where TEvent : struct
{
    var typeId = EventTypeId<TEvent>.Id;

    EnsureEventCapacity(typeId, rebuildBitmap: false);

    if (!_postPlans[typeId].IsRegistered)
    {
        _postPlans[typeId] = PostTypePlan.Default(typeId, _defaultBackpressure);
    }

    _payloadStorage.EnsureStore<TEvent>(_runtimeId);

    RebuildPostBitmap();
}
```

如果当前 `PostTypePlan` 没有 `IsRegistered`，可以增加一个字段或用 `EventTypeId >= 0` / `Mode` 判定。更稳的是给 `PostTypePlan` 增加：

```csharp
public readonly bool IsRegistered;
```

默认空 plan 为 `false`，有效 plan 为 `true`。

---

## 4. 问题四：FlushBuffers 异常中断导致 snapshot payload 泄漏

### 4.1 当前问题

`FlushBuffers` 会把 Dirty / Latest / Coalesced 复制到 snapshot，然后开始派发。

如果派发过程中某个 handler 抛异常，`FlushBuffers` 可能中断，导致：

```text
_snapshotCoalesced 中后续 slot 没有 Release
_latestSnapshotBuffer 中后续 handle 没有 Release
_snapshotCoalesced.Clear 没执行
_latestSnapshotBuffer 没置回 Invalid
```

尤其是 `SubscribeNotify` / Notify 快路径可能直接抛异常，不一定被 EventCenter 捕获。

### 4.2 修改目标

确保不管派发是否中断，snapshot 中剩余 payload 都会被释放。

### 4.3 修改 Coalesced snapshot 派发

建议结构：

```csharp
private int DispatchCoalescedSnapshotSafely()
{
    var processed = 0;

    try
    {
        for (var i = 0; i < _snapshotCoalesced.Count; i++)
        {
            var slot = _snapshotCoalesced[i];

            if (!slot.Active || slot.PayloadHandle.IsInvalid)
            {
                continue;
            }

            try
            {
                DispatchPayload(slot.PayloadHandle, slot.Key.EventTypeId);
                processed++;
            }
            finally
            {
                _payloadStorage.Release(slot.PayloadHandle);

                // 将当前 slot 标记为已清理，避免外层 finally 重复释放。
                slot.PayloadHandle = PayloadHandle.Invalid;
                slot.Active = false;
                _snapshotCoalesced[i] = slot;
            }
        }

        return processed;
    }
    finally
    {
        ReleaseRemainingCoalescedSnapshot();
        _snapshotCoalesced.Clear();
    }
}
```

残留释放：

```csharp
private void ReleaseRemainingCoalescedSnapshot()
{
    for (var i = 0; i < _snapshotCoalesced.Count; i++)
    {
        var slot = _snapshotCoalesced[i];

        if (!slot.PayloadHandle.IsInvalid)
        {
            _payloadStorage.Release(slot.PayloadHandle);
            slot.PayloadHandle = PayloadHandle.Invalid;
            slot.Active = false;
            _snapshotCoalesced[i] = slot;
        }
    }
}
```

### 4.4 修改 Latest snapshot 派发

建议结构：

```csharp
private int DispatchLatestSnapshotSafely()
{
    var processed = 0;

    try
    {
        for (var segment = 0; segment < _latestSnapshotBits.Length; segment++)
        {
            var bits = _latestSnapshotBits[segment];

            while (bits != 0)
            {
                var bitIndex = BitHelper.TrailingZeroCount(bits);
                var typeId = (segment << 6) + bitIndex;

                bits &= ~(1UL << bitIndex);

                if ((uint)typeId >= (uint)_latestSnapshotBuffer.Length)
                {
                    continue;
                }

                var handle = _latestSnapshotBuffer[typeId];
                if (handle.IsInvalid)
                {
                    continue;
                }

                try
                {
                    DispatchPayload(handle, typeId);
                    processed++;
                }
                finally
                {
                    _payloadStorage.Release(handle);
                    _latestSnapshotBuffer[typeId] = PayloadHandle.Invalid;
                }
            }

            _latestSnapshotBits[segment] = 0;
        }

        return processed;
    }
    finally
    {
        ReleaseRemainingLatestSnapshot();
    }
}
```

残留释放：

```csharp
private void ReleaseRemainingLatestSnapshot()
{
    for (var i = 0; i < _latestSnapshotBuffer.Length; i++)
    {
        var handle = _latestSnapshotBuffer[i];

        if (!handle.IsInvalid)
        {
            _payloadStorage.Release(handle);
            _latestSnapshotBuffer[i] = PayloadHandle.Invalid;
        }
    }

    Array.Clear(_latestSnapshotBits, 0, _latestSnapshotBits.Length);
}
```

### 4.5 DirtySignal 不需要 payload release

DirtySignal 通常只派发默认 payload，不涉及 payload handle，重点是清空 `_dirtySnapshotBits`。

也建议用 finally 保证：

```csharp
try
{
    DispatchDirtySnapshot();
}
finally
{
    Array.Clear(_dirtySnapshotBits, 0, _dirtySnapshotBits.Length);
}
```

---

## 5. 问题五：EventPayloadStorage 生命周期需要测试兜底

### 5.1 当前情况

`EventPayloadStorage.Dispose()` 只清理本地 `_typeIdStores`，真正的 store 还存在于静态 `PayloadStoreCache<T>.Stores[runtimeId]` 中。

目前 Runtime Dispose 依赖：

```text
LayerHub.ClearRuntimeCaches(runtimeId)
```

清理静态缓存。

这个设计可以接受，但必须有测试兜底。

### 5.2 必须补的测试

```text
1. Runtime Dispose 后，PayloadStoreCache<T>.Stores[runtimeId] 被清空。
2. RuntimeId 复用后，不能读到旧 Runtime 的 payload store。
3. PostScheduler.Dispose 后，readyQueue / nextQueue / latest / coalesced pending payload 被释放。
4. FlushBuffers 抛异常后，latest/coalesced snapshot payload 被释放。
```

### 5.3 文档说明

在代码注释中明确：

```text
EventPayloadStorage 不单独拥有 EventStore<T> 的完整生命周期。
EventStore<T> 的 runtime 级静态缓存由 LayerHub.ClearRuntimeCaches(runtimeId) 清理。
PostScheduler.Dispose 负责释放队列中仍持有的 PayloadHandle。
```

---

## 6. 线程模型契约文档

### 6.1 当前取舍

当前不做 owner-thread 动态检查，也不做完整外部线程模型。

因此必须写清楚：

```text
LayerBase 是单线程 Runtime 框架。
除 AnyThread 后缀 API 外，其余 Runtime API 默认只能由 Runtime 所在线程调用。
Release 模式不做线程检查。
错误线程调用普通 API 属于未定义行为。
```

### 6.2 建议加入 README 或 THREADING.md

新增文档：

```text
docs/THREADING.md
```

内容建议：

```markdown
# LayerBase Threading Model

LayerBase Runtime uses a single-thread runtime model.

The following APIs are owner-thread only:

- Send
- Post
- TryPost
- PostLatest
- PostCoalesced
- MarkDirty
- CallAsync
- Pump
- Build
- Dispose
- Reset

The following APIs may be called from any thread:

- PostFromAnyThread
- TryPostFromAnyThread

LayerBase does not perform runtime thread checks on hot-path APIs.
Calling owner-thread-only APIs from the wrong thread is undefined behavior.

PostFromAnyThread is a cross-thread ingress API.
It does not dispatch immediately.
It is drained during Runtime.Pump before PostScheduler.Pump.
```

### 6.3 Dispose 并发约束

当前如果不做 `PostIngressQueue.CloseAndClear`，文档要写：

```text
Dispose / Reset 不允许和 PostFromAnyThread 并发执行。
```

如果要支持并发 Dispose，则需要新增：

```text
PostIngressQueue.CloseAndClear
TryEnqueue
```

---

## 7. 推荐提交顺序

### Commit 1：Ingress Drain 预算

```text
add ingress drain budget
```

内容：

```text
- PostSchedulerOptions.MaxIngressPostsPerPump
- LayerRuntime.Pump 使用 MaxIngressPostsPerPump
```

### Commit 2：Ingress PostResult 统计

```text
preserve ingress post result
```

内容：

```text
- IIngressPostItem.PostTo 返回 PostResult
- IngressPostItem<T>.PostTo 返回 TryPost 结果
- PostIngressDrainResult
- DrainTo 返回 PostIngressDrainResult
```

### Commit 3：统一 PostScheduler 容量扩展

```text
unify post scheduler event capacity
```

内容：

```text
- EnsureEventCapacity
- EnsureUlongArrayCapacity
- BuildPlans 改用 EnsureEventCapacity
- AddSpecialPolicy 改用 EnsureEventCapacity
- PrewarmEvent 改用 EnsureEventCapacity
- PostTypePlan 增加 IsRegistered 或等效判定
```

### Commit 4：FlushBuffers 异常清理

```text
make flush buffers exception safe
```

内容：

```text
- DispatchCoalescedSnapshotSafely
- ReleaseRemainingCoalescedSnapshot
- DispatchLatestSnapshotSafely
- ReleaseRemainingLatestSnapshot
- Dirty snapshot finally clear
```

### Commit 5：Payload 生命周期测试

```text
add payload storage lifecycle tests
```

内容：

```text
- Runtime Dispose 清理 payload store
- RuntimeId 复用不读旧 store
- FlushBuffers 异常不泄漏
```

### Commit 6：线程模型文档

```text
document threading model
```

内容：

```text
- docs/THREADING.md
- README 引用 THREADING.md
- 明确 AnyThread API 与 owner-thread-only API
```

---

## 8. 必须补的测试清单

### 8.1 PostIngressQueue

```text
- DrainTo respects MaxIngressPostsPerPump
- DrainTo returns failed count when scheduler.TryPost fails
- PostFromAnyThread Latest participates in PostScheduler Latest
- PostFromAnyThread Coalesced participates in PostScheduler Coalesced
```

### 8.2 PostScheduler Capacity

```text
- PrewarmEvent<T> 后 TryPost<T> 成功
- PrewarmEvent<T> 后 PostLatest<T> 不因 latest buffer 未扩容失败
- PrewarmEvent<T> 后 MarkDirty<T> 不因 dirty buffer 未扩容失败
- AddSpecialPolicy<T> 后 Latest / Dirty / Coalesced 相关路径可用
```

### 8.3 FlushBuffers

```text
- Coalesced dispatch 抛异常后，剩余 snapshot payload 被释放
- Latest dispatch 抛异常后，剩余 snapshot payload 被释放
- Dirty dispatch 抛异常后，dirty snapshot bits 被清空
```

### 8.4 PayloadStore

```text
- Runtime Dispose 后 runtime cache 清空
- RuntimeId 复用后没有旧 payload 残留
```

### 8.5 Threading Contract

```text
- 文档明确 Send/Post/TryPost 非 AnyThread
- 文档明确 PostFromAnyThread 不立即派发
- 文档明确 Dispose/Reset 与 PostFromAnyThread 的并发限制
```

---

## 9. 最终目标

修复完成后，LayerBase 在当前阶段应满足：

```text
1. 主线程 Send/Post 热路径不增加线程检查。
2. PostFromAnyThread 支持策略，但不会拖死一帧。
3. 跨线程 Post 失败不会完全静默丢失。
4. PrewarmEvent / AddSpecialPolicy 不会造成内部容量不一致。
5. FlushBuffers 即使遇到异常，也不会泄漏 snapshot payload。
6. Runtime 生命周期和 PayloadStore 生命周期有测试兜底。
7. 线程模型文档明确，用户不会误以为所有 API 都是线程安全的。
```
