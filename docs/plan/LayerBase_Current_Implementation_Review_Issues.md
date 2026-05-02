# LayerBase 当前实现复查问题汇总文档

## 1. 文档目的

本文档汇总当前 LayerBase 实现中需要进一步复查或修正的问题。

这些问题不是架构方向错误，而是实现层面的稳定性、语义一致性和热路径约束问题。

当前 LayerBase 的主方向已经成立：

```text
PostScheduler
    已具备 Frame Budget、Backpressure、DirtySignal、Latest、Coalesced 等调度能力。

EventMetaData
    已承担事件级策略配置。

EventTypeId<T>
    已替代运行时 Type 字典查找。

Time / Delay
    已开始按 dirty 标记减少空转。

PayloadStorage / RingBuffer
    已开始进入热路径优化阶段。
```

但在进入 README 更新、版本发布或继续扩展前，建议优先复查本文档列出的问题。

---

## 2. 最高优先级：FlushBuffers 的重入安全

### 2.1 问题描述

需要确认 `PostScheduler.FlushBuffers()` 是否在持有 `_bufferLock` 的情况下直接执行事件派发。

风险形态：

```text
FlushBuffers()
    lock (_bufferLock)
        foreach (_pendingCoalesced)
            Dispatch(handler)

handler 内部再次调用：
    Post(...)
    MarkDirty(...)
    PostLatest(...)
    PostCoalesced(...)
```

如果 handler 在派发过程中重新进入 `PostScheduler`，可能会修改当前正在 flush 的集合：

```text
_pendingDirtySignals
_pendingCoalesced
_pendingLatest
_dirtySignalBuffer
_coalescedBuffer
_latestBuffer
```

这会导致：

```text
当前 wave 被污染。
本应下一轮处理的事件进入当前轮。
foreach 期间集合被修改。
payload handle 生命周期错乱。
Coalesced slot 被提前删除或重复使用。
Latest / DirtySignal 状态被错误清空。
```

---

### 2.2 与循环检测的区别

已有循环检测不一定能解决该问题。

循环检测通常解决：

```text
A handler 触发 B。
B handler 又触发 A。
最终形成事件递归或拓扑环。
```

本文档所说的问题是：

```text
Flush 当前 pending 集合时，handler 重入并修改调度器内部集合。
```

只有当现有循环检测同时满足以下条件时，才算覆盖该问题：

```text
Dispatch 期间产生的新 Post / MarkDirty / PostLatest / PostCoalesced
不会写入当前正在 flush 的 pending 集合，
而是进入 next wave 或下一轮 pending 集合。
```

如果现有循环检测只是检测事件链路环、递归深度或拓扑循环，则不能替代 Flush snapshot。

---

### 2.3 建议修正

`FlushBuffers` 不应边遍历 live pending 集合边 Dispatch。

推荐规则：

```text
锁内只做 snapshot 和状态清理。
锁外再执行 Dispatch。
```

推荐流程：

```text
1. lock 内：
    复制 _pendingDirtySignals 到 localDirtySignals。
    复制 _pendingCoalesced 到 localCoalescedSlots。
    复制 _pendingLatest 到 localLatestEvents。

    清空原 pending 集合。
    清空 DirtySignal 标记。
    从 CoalescedBuffer 中移除本轮 slot。
    取出 Latest payload handle 并清空槽位。

2. lock 外：
    派发 localDirtySignals。
    派发 localCoalescedSlots。
    派发 localLatestEvents。
    释放本轮 payload。

3. handler 中新产生的 Post：
    进入已经清空后的 pending。
    等下一 wave 或下一次 Pump 处理。
```

示意：

实际实现需要用 `ArrayPool` 或内部复用 List 来降低分配，但核心原则不变：不要在锁内 Dispatch。

```csharp
private int FlushBuffers()
{
    List<int> dirtyItems;
    List<PayloadHandle> coalescedItems;
    List<PayloadHandle> latestItems;

    lock (_bufferLock)
    {
        dirtyItems = SnapshotDirtySignalsAndClear();
        coalescedItems = SnapshotCoalescedAndClear();
        latestItems = SnapshotLatestAndClear();
    }

    var count = 0;

    foreach (var eventTypeId in dirtyItems)
    {
        _payloadStorage.DispatchDefault(eventTypeId, _eventCenter);
        count++;
    }

    foreach (var handle in coalescedItems)
    {
        _payloadStorage.Dispatch(handle, _eventCenter);
        _payloadStorage.Release(handle);
        count++;
    }

    foreach (var handle in latestItems)
    {
        _payloadStorage.Dispatch(handle, _eventCenter);
        _payloadStorage.Release(handle);
        count++;
    }

    return count;
}
```

---

## 3. RingBuffer 容量语义问题

### 3.1 问题描述

当前 RingBuffer 如果将传入容量自动向上取整为 2 的幂：

```text
3 -> 4
5 -> 8
1000 -> 1024
```

虽然可以用：

```text
(index + 1) & mask
```

替代：

```text
(index + 1) % capacity
```

但会改变用户配置的容量语义。

例如用户配置：

```csharp
MaxPostQueueCapacity = 1000;
```

如果实际容量变成 1024，那么 Backpressure 的触发点也变了。

---

### 3.2 建议修正

区分逻辑容量和物理容量。

```text
logicalCapacity
    用户配置的容量。
    用于 IsFull / Backpressure 判断。

physicalCapacity
    内部数组容量。
    可以向上取整到 2 的幂。
    用于位运算优化。
```

示意：

```csharp
internal sealed class RingBuffer<T>
{
    private readonly T[] _buffer;
    private readonly int _logicalCapacity;
    private readonly int _physicalCapacity;
    private readonly int _mask;

    private int _head;
    private int _tail;
    private int _count;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _logicalCapacity = capacity;
        _physicalCapacity = NextPowerOfTwo(capacity);
        _mask = _physicalCapacity - 1;
        _buffer = new T[_physicalCapacity];
    }

    public bool IsFull => _count >= _logicalCapacity;

    public bool TryEnqueue(in T item)
    {
        if (_count >= _logicalCapacity)
        {
            return false;
        }

        _buffer[_tail] = item;
        _tail = (_tail + 1) & _mask;
        _count++;
        return true;
    }
}
```

这样可以同时满足：

```text
内部热路径用 mask。
外部配置语义不变。
Backpressure 按用户配置触发。
```

---

## 4. PostCoalesced / PostLatest 不应覆盖事件元数据策略

### 4.1 问题描述

如果便利 API 这样实现：

```csharp
PostLatest<T>(in T value)
    => TryPost(value, new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0));

PostCoalesced<T>(in T value)
    => TryPost(value, new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0));
```

那么它会覆盖事件元数据里的配置：

```text
Backpressure
MaxPending
MergeFailure
```

这会破坏心智模型。

---

### 4.2 建议修正

`PostCoalesced` / `PostLatest` 应只覆盖 Mode，不应强行覆盖完整策略。

推荐规则：

```text
Post(value)
    使用 EventMetaData 中的 PostPolicy。
    没有元数据时使用全局默认策略。

PostLatest(value)
    使用事件元数据策略作为基础。
    只将 Mode 改为 Latest。

PostCoalesced(value)
    使用事件元数据策略作为基础。
    只将 Mode 改为 Coalesced。

Post(value, explicitPolicy)
    使用用户显式传入的完整策略。
```

无 policy 参数的便利 API 不应偷偷构造完整默认策略并覆盖元数据。

---

## 5. BuildPlans 与无元数据事件的默认 Post 行为

### 5.1 问题描述

当前 `PostScheduler` 已经开始使用：

```text
PostTypePlan
BuildPlans(...)
```

这说明 Post 调度器从“完全运行时懒处理”转向“预构建事件类型计划”。

这是性能方向上正确的。

但需要确认：

```text
没有 EventMetaData 的普通事件，是否仍然可以 Post？
```

用户可能写：

```csharp
public readonly struct SimpleEvent
{
    public readonly int Value;
}

LayerHub.Post(new SimpleEvent(1));
```

但没有写：

```csharp
public class SimpleEventMetaData : EventMetaData<SimpleEvent>
{
}
```

如果 `SimpleEvent` 没有进入 `PostTypePlan`，可能出现：

```text
Post 被拒绝。
Post 使用未初始化策略。
Post 路径走错。
```

---

### 5.2 建议规则

推荐采用：

```text
源生成器优先生成 plan。
运行时提供默认 fallback。
```

具体规则：

```text
1. 生成器为已知事件生成 PostTypePlan。
2. EventMetaData 中的策略写入 EventRuntimePolicyTable。
3. 如果运行时 TryPost 遇到未知 eventId：
    走一次慢路径创建默认 Normal plan。
4. 之后该 eventId 使用缓存 plan。
```

默认 plan：

```text
Mode = Normal
Backpressure = 全局默认 Backpressure
MaxPending = 0
MergeFailure = Reject
```

---

## 6. IEventMetaData 的 object 接口不应进入热路径

### 6.1 问题描述

如果接口中存在：

```csharp
int GetPostCoalesceKey(object value);
```

对于 struct 事件，这会引入装箱。

这条路径如果进入 `PostScheduler.TryPost` 热路径，会破坏低 GC 目标。

---

### 6.2 建议约束

明确规定：

```text
IEventMetaData.GetPostCoalesceKey(object) 不允许进入 Post 热路径。
```

Post 热路径必须走 typed 方法：

```csharp
EventMetaData<TEvent>.GetPostCoalesceKey(in TEvent value)
EventMetaData<TEvent>.TryMergePostEvent(ref TEvent current, in TEvent next)
```

如果 object 方法只用于诊断或非热路径，可以保留。

如果没有明确用途，建议移除 object 方法，避免后续误用。

---

## 7. Coalesced Flush 中排序可能不必要

### 7.1 问题描述

如果 `_pendingCoalesced` 只在新 slot 第一次出现时追加：

```csharp
_pendingCoalesced.Add(slotKey);
```

那么它天然就是 `FirstSequenceId` 顺序。

如果 flush 时再执行排序，会增加额外成本：

```text
O(k log k)
```

其中 k 是本轮 coalesced slot 数量。

---

### 7.2 建议

如果可以保证：

```text
每个 CoalescedSlotKey 只在首次出现时 Add 到 _pendingCoalesced。
后续 merge 不重复 Add。
_pendingCoalesced 不会被乱序插入。
```

那么可以删除排序，直接按 List 顺序 flush。

如果暂时不确定是否会乱序，保留排序也可以。

优先级低于重入安全问题。

---

## 8. Delay dirty 标记需要确认关闭路径

### 8.1 问题描述

当前优化方向是：

```text
没有 Delay 时，不 Tick DelayPublisherManager。
有 Delay 时，才 Tick。
```

这需要确保 dirty 标记可以正确打开和关闭。

需要复查：

```text
首次创建 DelayPublisher 时是否 MarkDelayDirty？
DelayPublisher 中所有 delay 项过期或取消后，HasAnyDelay 是否会变回 false？
Reset / Dispose 后是否清理 delay dirty 状态？
多 runtime 下是否隔离？
```

---

### 8.2 建议测试

增加测试：

```text
1. 没有 DelayPublisher 时 Pump 不调用 DelayPublisherManager.Tick。
2. 创建一个 delay 后 HasAnyDelay = true。
3. delay 到期并清理后 HasAnyDelay = false。
4. 取消所有 delay 后 HasAnyDelay = false。
5. LayerHub.Reset 后 HasAnyDelay = false。
```

---

## 9. PayloadStorage fast path 需要确认 runtimeId 上限与清理

### 9.1 问题描述

当前已经出现类似：

```text
PayloadStoreCache<T>.Stores = new EventStore<T>[1024]
runtimeId < 1024 时走泛型静态缓存
```

这个方向是对的，但需要确认：

```text
LayerHub.Reset 是否清空所有 PayloadStoreCache<T>。
runtimeId 超过 1024 时是否行为稳定。
多个 LayerRuntime 是否不会误共用 EventStore。
EventStore 是否在 Dispose 时释放。
```

尤其是测试中频繁创建 runtime 时，要避免旧 store 污染新测试。

---

### 9.2 建议

保留：

```text
LayerHub.RegisterCacheResetter(...)
```

并确保以下缓存全部纳入 reset：

```text
LayerCallCache
LayerTargetCache
PayloadStoreCache<T>
DelayPublisherManager.Instance
EventMetaDataHandler
PostScheduler / EventPayloadStorage
```

---

## 10. PostTypePlan / EventRuntimePolicyTable 一致性

### 10.1 问题描述

当前初始化流程大致是：

```text
读取 EventMetaDataHandler.GetAllMetaData()
设置 EventRuntimePolicyTable
生成 PostTypePlan
scheduler.BuildPlans(plans)
```

需要确认：

```text
EventMetaDataHandler.RegisterMetaData 后，如果 scheduler 已经初始化，policy table 是否会更新？
测试中手动 RegisterMetaData 后是否需要重新 BuildPlans？
LayerHub.Reset 是否清空旧 metadata？
```

如果 metadata 注册发生在 scheduler 初始化之后，可能出现：

```text
policy table 有旧配置。
PostTypePlan 没有新配置。
PostScheduler 策略和 metadata 不一致。
```

---

### 10.2 建议规则

建议明确规定：

```text
EventMetaData 应在 LayerRuntime Build / InitializeScheduler 前完成注册。
运行时动态注册 EventMetaData 不保证自动刷新 PostTypePlan。
```

如果要支持动态注册，需要提供：

```csharp
runtime.RebuildEventPolicies();
```

否则不要隐式支持。

---

## 11. 便利 API 与显式策略 API 的边界

### 11.1 建议 API 分层

推荐保留以下 API：

```csharp
Post(value)
Post(value, policy)

MarkDirty<T>()
PostLatest(value)
PostLatest(value, policy)

PostCoalesced(value)
PostCoalesced(value, policy)
```

语义：

```text
无 policy 参数：
    以 EventMetaData / 全局默认策略为基础。

有 policy 参数：
    用户显式完全覆盖策略。
```

---

## 12. README 需要同步的关键点

当前代码能力已经明显超过旧 README。

README 至少需要同步以下内容：

```text
1. 版本号。
2. LayerBase 不只是 EventBus，而是游戏系统通信与调度心智模型。
3. Send / Post / Delay / Call / LBTask 的边界。
4. DirtySignal / Latest / Coalesced 的区别。
5. Coalesced 是数据合并，不是 default 信号。
6. TimeWheel / DelayPublisher 的时间语义。
7. SubscribeParallel 是 fire-and-forget，不收集结果，不保证线程安全。
8. 无运行时反射热路径约束。
```

---

## 13. 建议新增测试清单

### 13.1 Flush 重入测试

```text
Coalesced handler 内再次 PostCoalesced。
新事件不应在当前 wave 派发。
下一次 Pump 或下一 wave 才处理。
```

### 13.2 DirtySignal 重入测试

```text
DirtySignal handler 内 MarkDirty 同事件。
当前 Pump 不应无限递归。
新 DirtySignal 应进入下一 wave。
```

### 13.3 Latest 重入测试

```text
Latest handler 内 PostLatest 新值。
当前 wave 不应污染。
下一 wave 派发新 latest。
```

### 13.4 RingBuffer 逻辑容量测试

```text
RingBuffer(3) 只能成功 Enqueue 3 次。
第 4 次触发背压。
内部物理容量可以是 4，但用户语义必须是 3。
```

### 13.5 元数据策略保留测试

```text
EventMetaData 配置 Backpressure = DropOldest。
调用 PostCoalesced(value)。
确认实际使用 DropOldest，而不是 RejectNew。
```

### 13.6 无 metadata Post 测试

```text
无 EventMetaData 的普通事件。
Post 后 Pump 能正常派发。
```

### 13.7 object metadata 非热路径测试

```text
PostCoalesced 热路径不调用 IEventMetaData.GetPostCoalesceKey(object)。
```

---

## 14. 优先级排序

### P0：必须优先确认

```text
1. FlushBuffers 不在 lock 内 Dispatch。
2. Dispatch 期间新 Post 进入下一 wave。
3. RingBuffer 逻辑容量不被 2 的幂物理容量改变。
4. PostCoalesced / PostLatest 不覆盖 EventMetaData 策略。
```

### P1：建议尽快确认

```text
1. 无 metadata 普通 Post 的 fallback。
2. EventRuntimePolicyTable 与 PostTypePlan 的一致性。
3. PayloadStoreCache reset / runtime 隔离。
4. Delay dirty 标记关闭路径。
```

### P2：可以后续优化

```text
1. Coalesced flush 去掉不必要 sort。
2. object metadata 接口清理。
3. 更多 PumpStats / PostStats。
4. README 深度重写。
```

---

## 15. 最终结论

当前 LayerBase 的架构方向已经成立，框架级优化也基本到位。

现在不建议继续扩张新功能。

下一步应集中在：

```text
重入安全
配置语义一致性
热路径无装箱
缓存 reset 正确性
Post / Delay / Metadata 行为可预测
README 与代码同步
```

最重要的原则：

```text
PostScheduler 的 flush 必须基于稳定快照。
事件元数据必须是策略来源。
用户配置的容量语义不能被内部优化改变。
热路径不得引入反射、装箱或 Type 字典查找。
```
