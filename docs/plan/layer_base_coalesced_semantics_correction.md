# LayerBase Coalesced 语义修正文档

## 1. 修正目标

本修正文档用于单独说明 `Coalesced` 的正确语义，并纠正将 `Coalesced` 理解为“信号合并”的问题。

最终结论：

```text
DirtySignal / MarkDirty
    表示信号合并。
    只记录“某类事件发生过”。
    不保存事件数据。
    可以派发 default(TEvent)。

Coalesced
    表示数据合并。
    保存真实事件数据。
    多个同类型、同合并键的事件会按照投递先后顺序合并成一个最终事件。
    Pump 时派发合并后的真实事件数据。
```

一句话：

```text
DispatchDefault 只能属于 DirtySignal，不能属于 Coalesced。
```

---

## 2. 问题来源

如果当前实现中 `Coalesced` 采用如下逻辑：

```text
PostCoalesced<TEvent>()
    -> 只在 DirtyMask / BitSet 中标记 EventTypeId
    -> Pump 时 DispatchDefault<TEvent>()
```

那么它本质上不是数据合并，而是：

```text
Signal Coalescing
Dirty Signal
MarkDirty
```

这种实现适合 UI 刷新、红点刷新、缓存失效通知，但不适合表达“多个事件数据合并成一个事件”的语义。

例如：

```text
InventoryChanged(slot 1)
InventoryChanged(slot 2)
InventoryChanged(slot 3)
```

如果最终派发的是：

```text
default(InventoryChanged)
```

那么事件数据已经丢失。

而真正的 `Coalesced` 应该派发：

```text
InventoryChanged(changedSlots: 1, 2, 3)
```

或者其他由事件元数据定义的合并结果。

---

## 3. 修正后的模式划分

Post 侧应明确区分四种模式。

```csharp
public enum PostDeliveryMode
{
    // Normal 表示普通 Post。
    // 每一次投递都会进入 Post 队列。
    // 它保留投递次数，也尽量保留 FIFO 顺序。
    Normal,

    // DirtySignal 表示脏信号。
    // 它只记录“这个事件类型发生过”，不保存事件数据。
    // Pump 时可以派发 default(TEvent)，或者未来扩展为无 payload 通知。
    // 适合 UI 刷新、红点刷新、状态脏标记、缓存失效通知。
    DirtySignal,

    // Latest 表示只保留最新一次事件数据。
    // 多次投递同类型事件时，旧 payload 会被新 payload 覆盖。
    // 适合位置显示、音量变化、进度条、鼠标坐标、网络状态显示等只关心最终值的场景。
    Latest,

    // Coalesced 表示数据合并。
    // 多次投递同类型、同 CoalesceKey 的事件时，框架会调用 EventMetaData<TEvent> 中的合并算法。
    // 合并顺序必须遵守投递先后顺序。
    // Pump 时派发的是合并后的真实事件数据，而不是 default(TEvent)。
    Coalesced
}
```

对照表：

| 模式 | 是否保存数据 | 是否合并数据 | Pump 时派发什么 | 典型用途 |
|---|---:|---:|---|---|
| `Normal` | 是 | 否 | 每个原始事件 | 普通延后事件 |
| `DirtySignal` | 否 | 否 | `default(TEvent)` 或无 payload 通知 | UI 刷新、红点、脏标记 |
| `Latest` | 是 | 覆盖 | 最后一次事件 | 坐标、进度、音量 |
| `Coalesced` | 是 | 是 | 合并后的事件 | 伤害累加、变化集合、批量统计 |

---

## 4. API 修正建议

建议将对外 API 明确拆分。

```csharp
// Post 表示普通延后事件。
// e 参数：要投递的事件数据。
LayerHub.Post(e);

// MarkDirty 表示只标记某个事件类型发生过。
// TEvent 类型参数：脏信号事件类型。
// 它不保存事件数据，Pump 时 payload 没有业务意义。
LayerHub.MarkDirty<TEvent>();

// PostLatest 表示只保留最新一次事件数据。
// e 参数：要覆盖旧值的新事件数据。
LayerHub.PostLatest(e);

// PostCoalesced 表示按事件元数据中的合并算法合并事件数据。
// e 参数：要参与合并的新事件数据。
LayerHub.PostCoalesced(e);
```

命名含义：

```text
MarkDirty
    信号合并。

PostLatest
    覆盖式数据合并。

PostCoalesced
    算法式数据合并。
```

`PostCoalesced` 不应派发 `default(TEvent)`。

---

## 5. EventPostPolicy 修正

`EventPostPolicy` 需要支持合并失败策略。

```csharp
public readonly struct EventPostPolicy
{
    // Mode 表示 Post 投递模式。
    // Normal 表示普通队列投递。
    // DirtySignal 表示只标记发生过。
    // Latest 表示只保留最新值。
    // Coalesced 表示按事件元数据合并数据。
    public readonly PostDeliveryMode Mode;

    // Backpressure 表示队列满时采用的背压策略。
    // 背压是生产速度超过消费速度时，框架如何处理新事件的规则。
    public readonly BackpressurePolicy Backpressure;

    // MaxPending 表示该事件类型最多允许挂起多少个待处理项。
    // 对 DirtySignal、Latest、Coalesced 来说，通常可以是 1，或者按 CoalesceKey 分槽后的上限。
    // 小于等于 0 表示不限制。
    public readonly int MaxPending;

    // MergeFailure 表示 Coalesced 模式下合并失败时的处理策略。
    // 只有 Mode == PostDeliveryMode.Coalesced 时才有意义。
    public readonly MergeFailurePolicy MergeFailure;

    // mode 参数：Post 投递模式。
    // backpressure 参数：队列满时采用的处理策略。
    // maxPending 参数：该事件类型最多允许挂起多少待处理项。
    // mergeFailure 参数：Coalesced 模式下合并失败时的处理策略。
    public EventPostPolicy(
        PostDeliveryMode mode,
        BackpressurePolicy backpressure,
        int maxPending,
        MergeFailurePolicy mergeFailure)
    {
        Mode = mode;
        Backpressure = backpressure;
        MaxPending = maxPending;
        MergeFailure = mergeFailure;
    }
}

public enum MergeFailurePolicy
{
    // Reject 表示合并失败时拒绝新事件。
    // 这是最安全的默认策略，因为它可以尽早暴露合并算法缺失或语义不兼容的问题。
    Reject,

    // FallbackToLatest 表示合并失败时用新事件覆盖旧事件。
    // 适合只关心最终状态，但又希望优先尝试合并的事件。
    FallbackToLatest,

    // FallbackToNormal 表示合并失败时退回普通 Post。
    // 这会增加普通 Post 队列压力，不建议作为默认策略。
    FallbackToNormal
}
```

默认建议：

```text
Coalesced 事件默认 MergeFailurePolicy.Reject。
```

原因：如果事件声明了 `Coalesced`，但没有提供正确的合并算法，框架应尽早暴露错误，而不是静默降级。

---

## 6. EventMetaData 增加数据合并接口

不建议强迫事件本体实现 `ICoalesceable<T>`。

原因：LayerBase 的事件应尽量保持纯数据结构，合并行为更适合放在 `EventMetaData<TEvent>` 中。

建议在 `EventMetaData<TEvent>` 中新增以下接口：

```csharp
public abstract class EventMetaData<TEvent> : IEventMetaData
    where TEvent : struct
{
    // Category 表示事件分类。
    // 用于拓扑、审计、模块检索。
    public virtual EventCategoryToken Category => EventCategoryToken.Empty;

    // PostPolicy 表示这个事件类型进入 PostScheduler 时的默认策略。
    // 返回 null 时使用框架整体默认策略。
    public virtual EventPostPolicy? PostPolicy => null;

    // GetPostCoalesceKey 表示获取当前事件的合并键。
    // value 参数：当前投递的事件数据。
    // 返回值：同事件类型且同 key 的事件才会进入同一个合并槽。
    // 默认返回 0，表示该事件类型的所有数据都合并到一个槽里。
    public virtual int GetPostCoalesceKey(in TEvent value)
    {
        return 0;
    }

    // TryMergePostEvent 表示尝试把 next 合并进 current。
    // current 参数：当前合并槽中已经缓存的事件数据，会被原地修改。
    // next 参数：新投递进来的事件数据，只读传入。
    // 返回 true 表示合并成功。
    // 返回 false 表示合并失败，框架需要根据 MergeFailurePolicy 处理。
    public virtual bool TryMergePostEvent(
        ref TEvent current,
        in TEvent next)
    {
        return false;
    }

    // OnEventExpectation 表示事件处理异常时的全局观察点。
    public virtual void OnEventExpectation<TValue>(
        TValue e,
        Exception exception)
        where TValue : struct
    {
    }
}
```

设计原则：

```text
事件 struct 继续保持纯数据。
合并算法由事件元数据描述。
PostScheduler 只依赖 EventRuntimePolicyTable 和构建阶段生成的合并委托。
热路径不应反射查找元数据。
```

---

## 7. CoalesceKey：支持同类型多槽合并

只按 `EventTypeId` 合并是不够的。

例如：

```text
DamageEvent(target 1, 10)
DamageEvent(target 2, 20)
DamageEvent(target 1, 15)
```

合理结果应该是：

```text
DamageEvent(target 1, 25)
DamageEvent(target 2, 20)
```

因此 `Coalesced` 应按以下键分槽：

```text
EventTypeId + CoalesceKey
```

合并槽 key：

```csharp
public readonly struct CoalescedSlotKey : IEquatable<CoalescedSlotKey>
{
    // EventTypeId 表示事件类型编号。
    public readonly int EventTypeId;

    // CoalesceKey 表示事件合并键。
    // 同事件类型、同 CoalesceKey 的事件会进入同一个合并槽。
    public readonly int CoalesceKey;

    // eventTypeId 参数：事件类型编号。
    // coalesceKey 参数：事件合并键。
    public CoalescedSlotKey(int eventTypeId, int coalesceKey)
    {
        EventTypeId = eventTypeId;
        CoalesceKey = coalesceKey;
    }

    // Equals 表示两个合并槽 key 是否相同。
    // other 参数：另一个要比较的 key。
    public bool Equals(CoalescedSlotKey other)
    {
        return EventTypeId == other.EventTypeId &&
               CoalesceKey == other.CoalesceKey;
    }

    // GetHashCode 表示生成哈希值，用于 Dictionary 或其他哈希表查找。
    public override int GetHashCode()
    {
        return HashCode.Combine(EventTypeId, CoalesceKey);
    }
}
```

第一版可以使用：

```text
Dictionary<CoalescedSlotKey, PayloadHandle>
```

后续可优化为：

```text
EventTypeId -> per-event-type coalesced bucket
```

---

## 8. DirtySignalBuffer 与 CoalescedBuffer 必须分离

原来的 `DirtyMask / BitSet` 只能支持 `DirtySignal`。

真正的 `CoalescedBuffer` 必须保存事件数据。

### DirtySignalBuffer

```text
DirtySignalBuffer
    DirtyMask / BitSet
    ActiveEventTypeIds
```

用途：

```text
MarkDirty<TEvent>()
DirtySignal 模式
```

特点：

```text
不保存 payload。
可以 DispatchDefault。
性能极高。
```

### CoalescedBuffer

```text
CoalescedBuffer
    CoalescedSlotKey -> CoalescedSlot
    ActiveSlots 按 FirstSequenceId 记录首次出现顺序
```

```csharp
public struct CoalescedSlot
{
    // Key 表示当前合并槽的事件类型和合并键。
    public CoalescedSlotKey Key;

    // PayloadHandle 表示当前合并后的事件数据在 EventPayloadStorage 中的位置。
    public PayloadHandle PayloadHandle;

    // FirstSequenceId 表示该合并槽第一次出现时的全局投递序号。
    // Pump flush 时可用它保证不同合并槽按首次出现顺序派发。
    public long FirstSequenceId;

    // LastSequenceId 表示该合并槽最后一次被合并时的全局投递序号。
    // 它主要用于诊断、统计和调试。
    public long LastSequenceId;

    // MergeCount 表示该槽累计合并了多少次事件。
    // 第一次写入可以记为 1。
    public int MergeCount;

    // Active 表示当前槽是否有效。
    public bool Active;
}
```

用途：

```text
PostCoalesced(e)
Coalesced 模式
```

特点：

```text
保存真实 payload。
按 CoalesceKey 分槽。
按投递顺序调用合并算法。
Pump 时派发真实合并结果。
```

---

## 9. PostCoalesced 投递流程

```text
PostCoalesced(e)
    1. 获取 EventTypeId<TEvent>.Id
    2. 获取 EventRuntimePolicyTable 中的事件合并策略
    3. coalesceKey = GetPostCoalesceKey(e)
    4. slotKey = (eventTypeId, coalesceKey)
    5. 如果 slot 不存在：
        写入 payload storage
        创建 CoalescedSlot
        记录 FirstSequenceId
    6. 如果 slot 已存在：
        读取 current payload
        调用 TryMergePostEvent(ref current, in e)
        合并成功则写回 current
        合并失败则按 MergeFailurePolicy 处理
```

示意代码：

```csharp
public PostResult PostCoalesced<TEvent>(in TEvent value)
    where TEvent : struct
{
    // eventTypeId 表示 TEvent 对应的事件类型编号。
    var eventTypeId = EventTypeId<TEvent>.Id;

    // meta 表示 TEvent 对应的运行时元数据策略。
    // 实际热路径中应从 EventRuntimePolicyTable 按 eventTypeId 取，不应反射查找。
    var meta = _policyTable.GetMetaData<TEvent>();

    // coalesceKey 表示本次事件应该进入哪个合并槽。
    var coalesceKey = meta.GetPostCoalesceKey(in value);

    var slotKey = new CoalescedSlotKey(
        eventTypeId: eventTypeId,
        coalesceKey: coalesceKey);

    if (!_coalescedBuffer.TryGetSlot(slotKey, out var slot))
    {
        // 第一次出现该合并槽时，直接保存原始事件数据。
        var handle = _payloadStorage.Write(value);

        _coalescedBuffer.CreateSlot(
            key: slotKey,
            payloadHandle: handle,
            firstSequenceId: NextSequenceId());

        return PostResult.Enqueued();
    }

    // 已有合并槽时，取出当前合并结果。
    ref var current = ref _payloadStorage.GetRef<TEvent>(slot.PayloadHandle);

    // 按投递顺序把新事件合并进 current。
    if (meta.TryMergePostEvent(ref current, in value))
    {
        _coalescedBuffer.MarkMerged(slotKey, NextSequenceId());
        return PostResult.Coalesced();
    }

    // 合并失败时，按照策略处理。
    return HandleMergeFailure(
        slotKey: slotKey,
        value: in value,
        policy: meta.PostPolicy?.MergeFailure ?? MergeFailurePolicy.Reject);
}
```

说明：

```text
上述代码是结构示意。
实际实现应避免每次虚调用和元数据对象查询。
构建阶段应将 GetPostCoalesceKey / TryMergePostEvent 编译或缓存为运行时策略表中的委托。
```

---

## 10. Flush 顺序

`Coalesced` 有两层顺序要求。

### 10.1 同槽内部顺序

同一个合并槽内，必须按投递顺序依次 merge。

```text
Merge(Merge(event1, event2), event3)
```

而不是随机合并。

### 10.2 不同槽之间顺序

不同合并槽之间，推荐按 `FirstSequenceId` 派发。

例如：

```text
t1: Damage(target 2, 20)
t2: Damage(target 1, 10)
t3: Damage(target 2, 5)
```

合并后：

```text
Damage(target 2, 25)  FirstSequenceId = t1
Damage(target 1, 10)  FirstSequenceId = t2
```

Pump flush 顺序：

```text
Damage(target 2, 25)
Damage(target 1, 10)
```

---

## 11. 与 Normal Post 的相对顺序

第一版不建议承诺 `Coalesced` 与 `Normal` 之间严格逐条 FIFO 混排。

推荐规则：

```text
Normal Post 保留普通队列顺序。
Coalesced 在每个 Pump wave 开始时 flush 成 PostItem。
Coalesced slot 之间按 FirstSequenceId 排序。
不承诺 Coalesced 与 Normal Post 在原始投递流中完全交错一致。
```

原因：

```text
Coalesced 的本质是把多个投递合并成一个最终事件。
如果还要求它与 Normal Post 保持完全逐条交错顺序，PostScheduler 必须维护更复杂的占位和回填逻辑。
第一版复杂度过高。
```

如果未来必须支持严格混排，可以考虑引入：

```text
CoalescedPlaceholderItem
```

但第一版不建议实现。

---

## 12. 示例：DamageEvent 数据合并

```csharp
public readonly struct DamageEvent
{
    // TargetId 表示受到伤害的目标编号。
    public readonly int TargetId;

    // Amount 表示伤害数值。
    public readonly int Amount;

    // targetId 参数：目标编号。
    // amount 参数：伤害数值。
    public DamageEvent(int targetId, int amount)
    {
        TargetId = targetId;
        Amount = amount;
    }
}

public sealed class DamageEventMetaData : EventMetaData<DamageEvent>
{
    // PostPolicy 表示 DamageEvent 默认使用数据合并模式。
    // 同一帧内同一目标的伤害可以累加为一个事件。
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(
            mode: PostDeliveryMode.Coalesced,
            backpressure: BackpressurePolicy.Coalesce,
            maxPending: 0,
            mergeFailure: MergeFailurePolicy.Reject);

    // GetPostCoalesceKey 表示获取合并键。
    // value 参数：当前投递的 DamageEvent。
    // 返回 TargetId，表示同一目标的伤害才合并到同一个槽。
    public override int GetPostCoalesceKey(in DamageEvent value)
    {
        return value.TargetId;
    }

    // TryMergePostEvent 表示把 next 合并进 current。
    // current 参数：当前合并槽中的伤害事件。
    // next 参数：新投递进来的伤害事件。
    // 返回 true 表示合并成功。
    public override bool TryMergePostEvent(
        ref DamageEvent current,
        in DamageEvent next)
    {
        // 由于 GetPostCoalesceKey 已经按 TargetId 分槽，正常情况下二者 TargetId 应一致。
        // 这里仍保留检查，避免错误数据进入同一槽时静默合并。
        if (current.TargetId != next.TargetId)
        {
            return false;
        }

        current = new DamageEvent(
            targetId: current.TargetId,
            amount: current.Amount + next.Amount);

        return true;
    }
}
```

投递：

```csharp
LayerHub.PostCoalesced(new DamageEvent(targetId: 1, amount: 10));
LayerHub.PostCoalesced(new DamageEvent(targetId: 2, amount: 20));
LayerHub.PostCoalesced(new DamageEvent(targetId: 1, amount: 15));
```

Pump 时派发：

```text
DamageEvent(targetId: 1, amount: 25)
DamageEvent(targetId: 2, amount: 20)
```

不同目标的派发顺序按各自合并槽的 `FirstSequenceId` 决定。

---

## 13. 示例：InventoryDirtyEvent 脏信号

```csharp
public readonly struct InventoryDirtyEvent
{
}

public sealed class InventoryDirtyEventMetaData : EventMetaData<InventoryDirtyEvent>
{
    // PostPolicy 表示该事件只作为脏信号使用。
    // 它不保存 payload，handler 收到后应主动从 InventoryService 拉取当前状态。
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(
            mode: PostDeliveryMode.DirtySignal,
            backpressure: BackpressurePolicy.Coalesce,
            maxPending: 1,
            mergeFailure: MergeFailurePolicy.Reject);
}
```

使用：

```csharp
LayerHub.MarkDirty<InventoryDirtyEvent>();
LayerHub.MarkDirty<InventoryDirtyEvent>();
LayerHub.MarkDirty<InventoryDirtyEvent>();
```

Pump 时只通知一次。

该事件的 payload 没有业务意义。

---

## 14. 对实现的直接替换规则

如果当前实现中存在：

```text
CoalescedBuffer
    DirtyMask / BitSet
    ActiveEventTypeIds
```

应改为：

```text
DirtySignalBuffer
    DirtyMask / BitSet
    ActiveEventTypeIds
```

并新增：

```text
CoalescedBuffer
    CoalescedSlotKey -> CoalescedSlot
    PayloadHandle
    FirstSequenceId
    LastSequenceId
    MergeCount
```

如果当前实现中存在：

```text
PostDeliveryMode.Coalesced
    -> DispatchDefault<TEvent>()
```

应改为：

```text
PostDeliveryMode.DirtySignal
    -> DispatchDefault<TEvent>()

PostDeliveryMode.Coalesced
    -> Dispatch merged payload
```

如果当前实现中存在：

```text
PostCoalesced<TEvent>() 无参数
```

应改为：

```text
MarkDirty<TEvent>() 无参数
```

真正的 `PostCoalesced` 应该接收事件数据：

```csharp
LayerHub.PostCoalesced(new SomeEvent(...));
```

---

## 15. 最终结论

修正后的 Post 调度语义为：

```text
Post
    逐次投递，保留每次事件。

MarkDirty / DirtySignal
    只表示发生过，不保存数据。
    可以派发 default(TEvent)。

PostLatest / Latest
    只保留最后一次真实事件数据。

PostCoalesced / Coalesced
    保留真实事件数据。
    按 EventTypeId + CoalesceKey 分槽。
    按投递顺序调用 EventMetaData<TEvent>.TryMergePostEvent。
    Pump 时派发合并后的真实事件。
```

一句话总结：

```text
DispatchDefault 属于 DirtySignal。
真正的 Coalesced 必须派发合并后的真实事件数据。
```

