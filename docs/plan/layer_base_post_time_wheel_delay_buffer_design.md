# LayerBase Post / TimeWheel / DelayPublisher 改造设计文档

## 1. 设计目标

本次改造的目标不是把现有 `Post`、`Delay`、`Pump` 简单优化，而是把它们拆成几个语义更清楚、职责更单一的运行时系统。

最终目标是：

```text
Send / Notify / Flow / Call
    立即通信，保持同步语义。

PostScheduler
    已经就绪的延后事件调度器。

TimeScheduler
    通用时间到期调度器。

DelayPublisher / DelayBufferSystem
    带 TTL 的缓存消息管道。

EventMetaData
    事件级全局策略配置入口。
```

核心原则：

```text
Post 不负责等待时间。
TimeWheel 不负责派发事件。
DelayPublisher 不负责事件派发，只负责缓存值。
EventMetaData 不直接参与热路径，而是在构建阶段解析成策略表。
```

---

## 2. 总体架构

推荐最终架构如下：

```text
LayerRuntime
    EventDispatcher
        Send
        Notify
        Flow
        Call

    PostScheduler
        Post
        TryPost
        PostCoalesced
        PostLatest
        FrameBudget
        Backpressure
        PumpStats

    TimeScheduler
        ShortWheel
        LongTimerHeap
        Schedule
        Cancel
        Tick
        Repeat
        Sink

    DelayBufferSystem
        DelayPublisher<T>
        TryGet
        TryTake
        TTL
        ContractId
        ShortDelayWheel

    EventMetaData System
        EventPostPolicy
        EventTimerPolicy
        EventBufferPolicy
        EventRuntimePolicyTable
```

这四个系统的关系：

| 系统 | 核心语义 | 是否自动派发 | 是否依赖时间 | 是否由业务主动读取 |
|---|---|---:|---:|---:|
| `Send` | 立即同步通信 | 是 | 否 | 否 |
| `PostScheduler` | 就绪事件延后派发 | 是 | 否 | 否 |
| `TimeScheduler` | 时间到期调度 | 否，交给 Sink 决定 | 是 | 否 |
| `DelayPublisher` | TTL 内缓存消息 | 否 | 是 | 是 |

---

## 3. Send / Notify / Flow / Call 的定位

即时通信通道不进入 PostScheduler，也不进入 TimeScheduler。

它们负责游戏业务中的即时因果链。

适合场景：

```text
战斗结算
输入即时判断
Call 请求响应
Flow 业务流控
初始化流程
需要同步结果的逻辑
```

设计原则：

```text
Send 要极简。
Send 不受帧预算影响。
Send 不被背压控制。
Send 不合并。
Send 不延迟。
Send 使用当前事件订阅快照立即派发。
```

Layer 在当前设计中不再表示运行阶段，只作为注册顺序、拓扑组织和审计信息来源。

---

## 4. PostScheduler 设计

### 4.1 Post 的定位

`Post` 不再按 Layer 推进，也不再包含 Delay 语义。

它只负责：

```text
已经就绪的事件如何入队。
队列满了怎么办。
本帧最多处理多少。
是否允许合并。
是否只保留最新值。
如何返回调度统计。
```

可以将它理解为：

```text
PostScheduler = Ready Event Scheduler
```

即“就绪事件调度器”。

---

### 4.2 Post 不再依赖 Layer

旧模型：

```text
Post 按 Layer 推进。
每帧处理一层。
事件从当前层推到下一层。
```

新模型：

```text
Post 进入全局 PostScheduler。
Post 按 FIFO / Wave / FrameBudget / Backpressure 处理。
Post 与 Layer 流动无关。
```

原因：

```text
Send 已经不再真正按 Layer 流动。
Layer 主要用于注册顺序和拓扑组织。
如果 Post 继续按 Layer 推进，会制造两套不一致的模型。
```

---

### 4.3 PostScheduler 底层结构

推荐结构：

```text
PostScheduler
    RingBuffer<PostItem> ReadyQueue
    RingBuffer<PostItem> NextQueue
    EventPayloadStorage
    CoalescedBuffer
    LatestBuffer
    EventRuntimePolicyTable
```

#### ReadyQueue

当前 Wave 可执行的普通 Post 队列。

#### NextQueue

当前 Pump 过程中产生的新 Post。

它用于避免 handler 内继续 Post 导致一次 Pump 无限延长。

#### Wave

`Wave` 表示“一轮 Post 批次”。

```text
Pump 开始前已经在 ReadyQueue 的事件属于当前 Wave。
Pump 过程中产生的新事件进入 NextQueue。
当前 Wave 结束后，NextQueue 才能切成 ReadyQueue。
```

#### RingBuffer

`RingBuffer` 是环形队列。

它用固定数组保存元素，读写位置到末尾后绕回开头。

优点：

```text
减少分配。
容量可控。
入队出队稳定。
CPU 缓存友好。
适合高频 Post。
```

---

### 4.4 PostItem

```csharp
public readonly struct PostItem
{
    // EventTypeId 表示事件类型编号。
    // 运行时用 int 编号查找事件策略和 handler 快照，避免热路径使用 Type 或字符串。
    public readonly int EventTypeId;

    // PayloadHandle 表示事件数据在 EventPayloadStorage 中的位置。
    // 队列只保存 handle，不直接保存完整事件数据，可以让队列元素更小。
    public readonly PayloadHandle PayloadHandle;

    // SequenceId 表示事件进入 PostScheduler 时的递增序号。
    // 它用于 Debug、诊断、稳定排序和丢弃记录。
    public readonly long SequenceId;

    // Policy 表示该事件使用的背压策略。
    // 背压是队列满时如何处理新事件的规则。
    public readonly BackpressurePolicy Policy;

    // eventTypeId 参数：事件类型编号。
    // payloadHandle 参数：事件数据存储句柄。
    // sequenceId 参数：事件入队顺序号。
    // policy 参数：队列满时使用的背压策略。
    public PostItem(
        int eventTypeId,
        PayloadHandle payloadHandle,
        long sequenceId,
        BackpressurePolicy policy)
    {
        EventTypeId = eventTypeId;
        PayloadHandle = payloadHandle;
        SequenceId = sequenceId;
        Policy = policy;
    }
}
```

---

### 4.5 EventPayloadStorage

Post 队列不建议直接保存 `object Payload`。

原因是事件多为 `struct`，放进 `object` 会产生装箱。

装箱是指把值类型包装成引用类型对象，通常会带来堆分配。

推荐结构：

```text
EventPayloadStorage
    EventTypeId 12 -> EventStore<InventoryChangedEvent>
    EventTypeId 18 -> EventStore<PlayerDamagedEvent>
    EventTypeId 25 -> EventStore<QuestChangedEvent>
```

每个 `EventStore<TEvent>` 内部可以使用：

```text
数组 + FreeList + 版本号
```

`FreeList` 是空闲链表，用于复用已经释放的槽位。

```csharp
public readonly struct PayloadHandle
{
    // EventTypeId 表示 payload 所属事件类型。
    public readonly int EventTypeId;

    // Index 表示事件数据在对应 EventStore 中的槽位编号。
    public readonly int Index;

    // Version 表示槽位版本号。
    // 它用于避免旧 handle 误读复用后的新 payload。
    public readonly int Version;

    // eventTypeId 参数：事件类型编号。
    // index 参数：事件数据所在槽位编号。
    // version 参数：槽位版本号。
    public PayloadHandle(int eventTypeId, int index, int version)
    {
        EventTypeId = eventTypeId;
        Index = index;
        Version = version;
    }
}
```

---

### 4.6 Post 投递模式

```csharp
public enum PostDeliveryMode
{
    // Normal 表示普通 Post。
    // 保留每次投递，并按队列顺序处理。
    Normal,

    // Coalesced 表示合并 Post。
    // 同类型事件在同一轮中只保留一次通知。
    Coalesced,

    // Latest 表示最新值 Post。
    // 同类型事件只保留最后一次数据。
    Latest
}
```

#### Normal

适合必须逐次处理的延后事件。

#### Coalesced

适合 UI 刷新、红点刷新、背包刷新、任务进度刷新。

它不保留投递次数，只表示“发生过”。

#### Latest

适合位置显示、音量变化、进度条刷新等只关心最新值的事件。

---

### 4.7 背压策略

背压是队列满时的处理机制。

```csharp
public enum BackpressurePolicy
{
    // RejectNew 表示队列满时拒绝新事件。
    // 适合不能丢、不能乱序的事件。
    RejectNew,

    // DropNewest 表示队列满时丢弃新事件。
    // 适合日志、统计、Debug 信息。
    DropNewest,

    // DropOldest 表示队列满时丢弃最旧事件，让新事件进入队列。
    // 适合只关心较新状态的非关键事件。
    DropOldest,

    // Coalesce 表示尝试合并同类事件。
    // 适合 UI 刷新、红点刷新、状态脏标记。
    Coalesce,

    // Latest 表示只保留最新值。
    // 适合位置显示、音量变化、进度条刷新。
    Latest
}
```

默认建议：

```text
普通 Post：RejectNew
状态刷新：Coalesce
最新状态：Latest
日志调试：DropNewest
```

不建议默认提供 `Block`。

`Block` 是阻塞线程，对游戏主线程不安全。

---

### 4.8 PostSchedulerOptions

```csharp
public readonly struct PostSchedulerOptions
{
    // ReadyCapacity 表示当前可执行队列的最大容量。
    // 它限制 ReadyQueue，防止已经就绪的事件无限堆积。
    public readonly int ReadyCapacity;

    // NextCapacity 表示下一轮队列的最大容量。
    // Pump 过程中产生的新 Post 会进入 NextQueue。
    public readonly int NextCapacity;

    // MaxEventsPerPump 表示一次 Pump 最多处理多少个 Post 事件。
    // 小于等于 0 可以表示不限制数量。
    public readonly int MaxEventsPerPump;

    // MaxMillisecondsPerPump 表示一次 Pump 最多消耗多少毫秒。
    // 小于等于 0 可以表示不限制时间。
    public readonly double MaxMillisecondsPerPump;

    // MaxWavesPerPump 表示一次 Pump 最多推进多少轮 Wave。
    // Wave 是一轮 Post 批次，用来隔离当前事件与处理中产生的新事件。
    public readonly int MaxWavesPerPump;

    // TimeCheckInterval 表示每处理多少个事件检查一次时间预算。
    // 不建议每个事件都检查 Stopwatch，因为高精度计时本身也有成本。
    public readonly int TimeCheckInterval;

    // DefaultBackpressure 表示默认背压策略。
    // 背压是队列满了之后如何处理新事件。
    public readonly BackpressurePolicy DefaultBackpressure;

    // readyCapacity 参数：ReadyQueue 的最大容量。
    // nextCapacity 参数：NextQueue 的最大容量。
    // maxEventsPerPump 参数：每次 Pump 最多处理多少事件。
    // maxMillisecondsPerPump 参数：每次 Pump 最多消耗多少毫秒。
    // maxWavesPerPump 参数：每次 Pump 最多推进多少轮 Wave。
    // timeCheckInterval 参数：每处理多少事件检查一次时间预算。
    // defaultBackpressure 参数：默认背压策略。
    public PostSchedulerOptions(
        int readyCapacity,
        int nextCapacity,
        int maxEventsPerPump,
        double maxMillisecondsPerPump,
        int maxWavesPerPump,
        int timeCheckInterval,
        BackpressurePolicy defaultBackpressure)
    {
        ReadyCapacity = readyCapacity;
        NextCapacity = nextCapacity;
        MaxEventsPerPump = maxEventsPerPump;
        MaxMillisecondsPerPump = maxMillisecondsPerPump;
        MaxWavesPerPump = maxWavesPerPump <= 0 ? 1 : maxWavesPerPump;
        TimeCheckInterval = timeCheckInterval <= 0 ? 64 : timeCheckInterval;
        DefaultBackpressure = defaultBackpressure;
    }
}
```

---

### 4.9 Post Pump 流程

```text
PostScheduler.Pump(options)
    1. Flush CoalescedBuffer
    2. Flush LatestBuffer
    3. 处理 ReadyQueue
    4. 每处理 N 个事件检查一次时间预算
    5. ReadyQueue 清空后，如果还有 Wave 预算，把 NextQueue 切成 ReadyQueue
    6. 如果事件数预算、时间预算、Wave 预算耗尽，则停止
    7. 返回 PostPumpStats
```

默认建议：

```text
MaxWavesPerPump = 1
```

这样可以避免 Post 链式触发把一帧吃光。

高级用户可以设为 2、4 或更高。

---

## 5. TimeScheduler 设计

### 5.1 TimeScheduler 的定位

`TimeScheduler` 是通用时间到期调度器。

它只回答：

```text
任务什么时候到期？
任务如何取消？
长期任务如何保存？
循环任务如何重排？
到期任务交给谁？
```

它不负责事件派发。

可以理解为：

```text
TimeScheduler = Expiration Scheduler
```

---

### 5.2 TimeScheduler 与 Post 的边界

```text
PostScheduler 不知道时间。
TimeScheduler 不知道事件。
Adapter 负责把到期结果转换成 Post 或其他行为。
```

例如：

```text
TimeScheduler.Tick(deltaTime, sink)
    -> sink.TryAcceptExpired(payload, handle)
        -> PostScheduler.TryPost(event)
```

TimeScheduler 核心只知道 payload，不知道 payload 是不是事件。

---

### 5.3 第二版结构：TimeWheel + LongTimerHeap

推荐结构：

```text
TimeScheduler<TPayload>
    ShortWheel
    LongTimerHeap
    TimerEntryPool
    FreeList
```

#### ShortWheel

处理短期任务。

例如：

```text
TickDuration = 0.05 秒
WheelSize = 512
WheelSpan = 25.6 秒
```

25.6 秒以内的任务进入 ShortWheel。

#### LongTimerHeap

处理长期任务。

超过 `WheelSpan` 的任务进入 LongTimerHeap。

LongTimerHeap 是小顶堆。

小顶堆可以快速取出最早到期的长期任务。

每次 Tick 时执行 Promote：

```text
如果 LongTimerHeap.Peek() 已经进入 ShortWheel 覆盖范围
    从 LongTimerHeap 取出
    转入 ShortWheel
```

Promote 可以理解为“从长期区转入短期时间轮”。

---

### 5.4 TimerEntry

```csharp
public struct TimerEntry<TPayload>
{
    // Payload 表示定时器到期时要输出的数据。
    // TimeScheduler 不理解这个数据，只负责保存并在到期时交给 Sink。
    public TPayload Payload;

    // ExpireTick 表示任务下一次计划到期的逻辑 tick。
    // 使用整数 tick 可以减少浮点误差。
    public long ExpireTick;

    // IntervalTicks 表示循环任务的间隔 tick。
    // 对一次性任务来说，这个值可以是 0。
    public long IntervalTicks;

    // RemainingRepeatCount 表示还要重复多少次。
    // -1 可以表示无限循环。
    // 0 表示不再重复。
    public int RemainingRepeatCount;

    // RepeatMode 表示循环模式。
    // Once 是一次性任务，FixedDelay 和 FixedRate 是循环任务。
    public TimerRepeatMode RepeatMode;

    // CatchUpPolicy 表示错过触发时间后的追赶策略。
    public TimerCatchUpPolicy CatchUpPolicy;

    // MaxCatchUpPerTick 表示 FireAllCapped 模式下，本次 Tick 最多补触发多少次。
    public int MaxCatchUpPerTick;

    // Version 表示槽位版本号。
    // 它用于避免旧 TimerHandle 误取消复用后的新任务。
    public int Version;

    // Next 表示当前时间槽链表中的下一个 entry index。
    public int Next;

    // Prev 表示当前时间槽链表中的上一个 entry index。
    public int Prev;

    // SlotIndex 表示当前所在时间槽编号。
    public int SlotIndex;

    // Active 表示任务是否仍然有效。
    public bool Active;
}
```

---

### 5.5 TimerHandle

```csharp
public readonly struct TimerHandle
{
    // Index 表示定时器任务在 TimerEntryPool 中的槽位编号。
    public readonly int Index;

    // Version 表示槽位版本号。
    // 如果槽位被复用，Version 会变化，旧 handle 就会失效。
    public readonly int Version;

    // index 参数：任务槽位编号。
    // version 参数：槽位版本号。
    public TimerHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }
}
```

它解决的问题：

```text
任务 A 使用槽位 5，版本 1。
A 被取消。
任务 B 复用槽位 5，版本变成 2。
旧的 A handle 不能误取消 B。
```

---

### 5.6 循环任务策略

默认策略：

```text
RepeatMode = FixedDelay
CatchUpPolicy = SkipMissed
```

#### FixedDelay

触发后，从当前时间重新等待一个间隔。

```text
nextExpireTick = currentTick + intervalTicks
```

适合游戏主线程，因为不会追赶历史触发。

#### SkipMissed

如果卡顿导致错过多次触发，只保留未来下一次触发，不补历史次数。

它可以避免补帧风暴。

补帧风暴是指：游戏卡住一段时间后，恢复时大量循环任务在同一帧补触发，导致再次卡顿。

---

### 5.7 TimerRepeatMode

```csharp
public enum TimerRepeatMode
{
    // Once 表示只执行一次。
    Once,

    // FixedDelay 表示固定延迟。
    // 下一次触发时间从当前触发时刻开始计算。
    FixedDelay,

    // FixedRate 表示固定频率。
    // 下一次触发时间从上一次计划触发时间开始计算。
    FixedRate
}
```

### 5.8 TimerCatchUpPolicy

```csharp
public enum TimerCatchUpPolicy
{
    // SkipMissed 表示跳过错过的触发次数。
    // 适合 UI、Debug、自动保存这类不需要补帧的任务。
    SkipMissed,

    // FireOnce 表示即使错过很多次，本次也只触发一次。
    // 后续从当前时间重新安排下一次。
    FireOnce,

    // FireAllCapped 表示补触发错过的次数，但受 MaxCatchUpPerTick 限制。
    // 适合需要追赶的服务器模拟，但必须限制上限。
    FireAllCapped
}
```

第一版建议只开放：

```text
Once
FixedDelay
SkipMissed
```

第二版再开放：

```text
FixedRate
FireAllCapped
```

---

### 5.9 TimeSchedulerOptions

```csharp
public readonly struct TimeSchedulerOptions
{
    // TickDurationSeconds 表示时间轮每一格代表多少秒。
    // 例如 0.05f 表示每个 tick 是 50 毫秒。
    public readonly float TickDurationSeconds;

    // WheelSize 表示短期时间轮槽数量。
    // TickDurationSeconds * WheelSize 就是短期时间轮一圈覆盖的时间。
    public readonly int WheelSize;

    // InitialTimerCapacity 表示 TimerEntry 池初始容量。
    // TimerEntry 池用于复用定时任务槽位，减少频繁分配。
    public readonly int InitialTimerCapacity;

    // LongTimerThresholdSeconds 表示超过多少秒的任务进入 LongTimerHeap。
    // 通常可以设置为 TickDurationSeconds * WheelSize。
    public readonly float LongTimerThresholdSeconds;

    // MaxExpiredPerTick 表示一次 Tick 最多输出多少个到期任务。
    // 这是 TimeScheduler 自己的到期提取上限，不是 Post 的帧预算。
    public readonly int MaxExpiredPerTick;

    // MaxPromotePerTick 表示一次 Tick 最多从 LongTimerHeap 转入 ShortWheel 多少任务。
    // 它用于避免某一帧大量长期任务集中进入短期轮。
    public readonly int MaxPromotePerTick;

    // DefaultRepeatMode 表示默认循环模式。
    // FixedDelay 表示触发后，从当前时间重新等待一个间隔。
    public readonly TimerRepeatMode DefaultRepeatMode;

    // DefaultCatchUpPolicy 表示默认追赶策略。
    // SkipMissed 表示跳过错过的触发，避免补帧风暴。
    public readonly TimerCatchUpPolicy DefaultCatchUpPolicy;

    // tickDurationSeconds 参数：时间轮每格秒数。
    // wheelSize 参数：短期时间轮槽数量。
    // initialTimerCapacity 参数：定时任务池初始容量。
    // longTimerThresholdSeconds 参数：进入长期堆的阈值。
    // maxExpiredPerTick 参数：一次 Tick 最多输出多少到期任务。
    // maxPromotePerTick 参数：一次 Tick 最多转入多少长期任务。
    // defaultRepeatMode 参数：默认循环模式。
    // defaultCatchUpPolicy 参数：默认追赶策略。
    public TimeSchedulerOptions(
        float tickDurationSeconds,
        int wheelSize,
        int initialTimerCapacity,
        float longTimerThresholdSeconds,
        int maxExpiredPerTick,
        int maxPromotePerTick,
        TimerRepeatMode defaultRepeatMode,
        TimerCatchUpPolicy defaultCatchUpPolicy)
    {
        TickDurationSeconds = tickDurationSeconds;
        WheelSize = wheelSize;
        InitialTimerCapacity = initialTimerCapacity;
        LongTimerThresholdSeconds = longTimerThresholdSeconds;
        MaxExpiredPerTick = maxExpiredPerTick;
        MaxPromotePerTick = maxPromotePerTick;
        DefaultRepeatMode = defaultRepeatMode;
        DefaultCatchUpPolicy = defaultCatchUpPolicy;
    }
}
```

---

### 5.10 TimeScheduler Tick 流程

```text
TimeScheduler.Tick(deltaTime, sink)
    1. accumulator += deltaTime
    2. 每凑够一个 TickDuration，currentTick++
    3. PromoteLongTimers
        从 LongTimerHeap 转入已进入短期范围的任务
        受 MaxPromotePerTick 限制
    4. ProcessCurrentWheelSlot
        处理当前时间槽
        到期任务输出给 sink
        受 MaxExpiredPerTick 限制
    5. 对循环任务
        按 FixedDelay + SkipMissed 计算下一次到期
        再次放入 ShortWheel 或 LongHeap
```

---

## 6. TimeScheduler Adapter

### 6.1 Adapter 的作用

Adapter 是适配器。

它负责把 TimeScheduler 的到期输出转成其他系统能理解的输入。

```text
TimeScheduler 到期 payload
    -> Adapter
        -> PostScheduler.TryPost(event)
```

---

### 6.2 IExpiredTimerSink

```csharp
public interface IExpiredTimerSink<TPayload>
{
    // TryAcceptExpired 表示接收一个到期任务。
    // payload 参数：到期任务携带的数据。
    // handle 参数：到期任务对应的 TimerHandle。
    // 返回 true 表示接收成功，false 表示接收失败。
    bool TryAcceptExpired(in TPayload payload, TimerHandle handle);
}
```

Sink 是接收器。

TimeScheduler 只输出“任务到期了”，Sink 决定到期后怎么处理。

---

### 6.3 PostTimerPayload

```csharp
public readonly struct PostTimerPayload<TEvent>
    where TEvent : struct
{
    // Event 表示到期后要投递到 PostScheduler 的事件。
    public readonly TEvent Event;

    // PostPolicyOverride 表示到期后 Post 时的单次策略覆盖。
    // 为 null 时，使用事件元数据里的 EventPostPolicy。
    public readonly EventPostPolicy? PostPolicyOverride;

    // eventValue 参数：到期后要 Post 的事件数据。
    // postPolicyOverride 参数：单次 Post 策略覆盖。
    public PostTimerPayload(
        TEvent eventValue,
        EventPostPolicy? postPolicyOverride)
    {
        Event = eventValue;
        PostPolicyOverride = postPolicyOverride;
    }
}
```

---

### 6.4 PostTimerSink

```csharp
public struct PostTimerSink<TEvent> :
    IExpiredTimerSink<PostTimerPayload<TEvent>>
    where TEvent : struct
{
    // _postScheduler 表示全局 Post 调度器。
    // 到期事件会通过它进入 Post 队列。
    private readonly PostScheduler _postScheduler;

    // _policyTable 表示事件运行时策略表。
    // 它用 EventTypeId 快速找到事件级 Post 策略。
    private readonly EventRuntimePolicyTable _policyTable;

    // postScheduler 参数：负责接收到期事件的 PostScheduler。
    // policyTable 参数：事件策略表，用于解析事件级全局配置。
    public PostTimerSink(
        PostScheduler postScheduler,
        EventRuntimePolicyTable policyTable)
    {
        _postScheduler = postScheduler;
        _policyTable = policyTable;
    }

    // TryAcceptExpired 表示接收一个到期任务。
    // payload 参数：包含到期后要 Post 的事件。
    // handle 参数：定时器句柄，可用于诊断日志。
    // 返回 true 表示成功交给 PostScheduler，false 表示被拒绝。
    public bool TryAcceptExpired(
        in PostTimerPayload<TEvent> payload,
        TimerHandle handle)
    {
        var eventTypeId = EventTypeId<TEvent>.Id;

        var policy = payload.PostPolicyOverride
            ?? _policyTable.GetPostPolicy(eventTypeId);

        var result = _postScheduler.TryPost(
            payload.Event,
            policy);

        return result.IsSuccess;
    }
}
```

---

## 7. DelayPublisher / DelayBufferSystem 设计

### 7.1 DelayPublisher 的定位

`DelayPublisher<T>` 不是 `PostDelayed`，也不是通用定时器。

它是：

```text
带 TTL 的缓存消息管道。
```

它负责：

```text
Publish 写入缓存值。
TryGet 在有效期内读取值但不清空。
TryTake 在有效期内读取值并清空。
TTL 到期后值失效。
contractId 支持互斥覆盖。
```

它适合：

```text
输入缓冲
跳跃预输入
连招缓冲
闪避缓冲
交互窗口
短时间技能确认窗口
```

它的消费模式是 pull，也就是业务主动取。

Post 的消费模式是 push，也就是框架派发给订阅者。

---

### 7.2 DelayPublisher 与 Post / TimeScheduler 的区别

| 能力 | DelayPublisher | PostScheduler | TimeScheduler |
|---|---|---|---|
| 核心语义 | TTL 内缓存消息 | 就绪事件延后派发 | 时间到期调度 |
| 消费方式 | 业务 `TryGet` / `TryTake` | handler 被调用 | Sink 接收到期任务 |
| 是否自动派发事件 | 否 | 是 | 否 |
| 是否需要 TTL | 是 | 否 | 是 |
| 是否适合输入缓冲 | 是 | 不适合 | 只适合辅助过期 |

---

### 7.3 DelayPublisher 的过期机制改造

保留 DelayPublisher 的缓存语义，但将 TTL 过期从每帧扫描改成短周期 TimeWheel。

```text
DelayPublisher<T>
    负责缓存值、TryGet、TryTake、覆盖、互斥。

ShortDelayWheel
    负责 TTL 到期后清空缓存。
```

它不需要：

```text
LongTimerHeap
循环任务
FixedRate
CatchUp
复杂 Sink
```

原因：

```text
DelayPublisher 时间跨度很短。
输入缓冲通常只有 0.05s ~ 0.5s。
精度要求不高。
到期动作只是 Clear。
```

---

### 7.4 DelayPublisher 内部结构

```csharp
public sealed class DelayPublisher<T> : IDelayPublisherInternal
    where T : struct
{
    // _value 表示当前缓存的消息值。
    // DelayPublisher 的核心语义是：在 TTL 有效期内保存这个值，供 TryGet / TryTake 使用。
    private T _value;

    // _hasValue 表示当前是否存在有效缓存值。
    // false 表示 TryGet / TryTake 应该失败。
    private bool _hasValue;

    // _version 表示当前缓存值的版本号。
    // 每次 Publish 都递增，用于防止旧的过期任务清掉新的缓存值。
    private int _version;

    // _timerHandle 表示当前过期任务在 DelayBufferWheel 中的句柄。
    // 句柄用于取消、刷新旧的过期任务。
    private DelayTimerHandle _timerHandle;

    // _publisherId 表示当前 DelayPublisher 在 DelayBufferSystem 中的编号。
    // TimeWheel 到期时会通过它找到对应 publisher。
    private readonly int _publisherId;

    // _manager 表示 DelayPublisher 所属的管理器。
    // Publish 时需要通过它注册 TTL 过期任务。
    private readonly DelayPublisherManager _manager;

    // publisherId 参数：当前 publisher 的唯一编号。
    // manager 参数：管理所有 DelayPublisher 和 DelayBufferWheel 的对象。
    public DelayPublisher(
        int publisherId,
        DelayPublisherManager manager)
    {
        _publisherId = publisherId;
        _manager = manager;
        _hasValue = false;
        _version = 0;
        _timerHandle = DelayTimerHandle.Invalid;
    }

    // Publish 表示写入一个新的缓存值，并指定它的有效时间。
    // value 参数：要缓存的消息值。
    // ttlSeconds 参数：缓存值的有效时间，单位是秒。
    public void Publish(in T value, float ttlSeconds)
    {
        _value = value;
        _hasValue = true;

        // 每次发布都递增版本号。
        // 这样旧的过期任务即使到期，也不会误清除新值。
        _version++;

        _timerHandle = _manager.ScheduleExpire(
            publisherId: _publisherId,
            version: _version,
            ttlSeconds: ttlSeconds,
            oldHandle: _timerHandle);
    }

    // TryGet 表示尝试读取当前缓存值，但不清空它。
    // value 参数：读取成功时输出缓存值。
    // 返回 true 表示当前存在有效值，false 表示没有有效值。
    public bool TryGet(out T value)
    {
        if (!_hasValue)
        {
            value = default;
            return false;
        }

        value = _value;
        return true;
    }

    // TryTake 表示尝试读取当前缓存值，并在成功后清空它。
    // value 参数：读取成功时输出缓存值。
    // 返回 true 表示成功取出值，false 表示当前没有有效值。
    public bool TryTake(out T value)
    {
        if (!_hasValue)
        {
            value = default;
            return false;
        }

        value = _value;
        Clear();
        return true;
    }

    // TryExpire 表示由 DelayBufferWheel 在 TTL 到期时尝试清空缓存。
    // version 参数：过期任务记录的版本号。
    // 返回 true 表示成功清空，false 表示该过期任务已经过期无效。
    public bool TryExpire(int version)
    {
        if (!_hasValue)
        {
            return false;
        }

        if (_version != version)
        {
            // 说明这个过期任务属于旧值。
            // 当前缓存已经被新的 Publish 覆盖，不能清空。
            return false;
        }

        Clear();
        return true;
    }

    // Clear 表示清空当前缓存值。
    // 它不会派发事件，只会让 TryGet / TryTake 之后失败。
    private void Clear()
    {
        _hasValue = false;
        _value = default;
        _timerHandle = DelayTimerHandle.Invalid;
    }
}
```

---

### 7.5 DelayBufferWheel

`DelayBufferWheel` 是专门给 DelayPublisher TTL 使用的短时间轮。

推荐默认：

```text
TickDurationSeconds = 1 / 60f
WheelSize = 64
覆盖约 1.06 秒
```

或者：

```text
TickDurationSeconds = 1 / 30f
WheelSize = 128
覆盖约 4.26 秒
```

输入缓冲通常足够。

---

### 7.6 DelayExpireEntry

```csharp
public struct DelayExpireEntry
{
    // PublisherId 表示要清空的 DelayPublisher 编号。
    // 到期时，DelayPublisherManager 会用它找到对应 publisher。
    public int PublisherId;

    // Version 表示发布缓存值时的版本号。
    // 到期时只有版本一致，才允许清空缓存。
    public int Version;

    // ExpireTick 表示这个过期任务应该在哪个逻辑 tick 到期。
    // 使用整数 tick 可以避免浮点误差。
    public long ExpireTick;

    // Next 表示当前 slot 链表中的下一个 entry index。
    // 用数组索引模拟链表，避免为每个节点 new 对象。
    public int Next;

    // Prev 表示当前 slot 链表中的上一个 entry index。
    // 有 Prev 后，可以在取消或重排时 O(1) 移除。
    public int Prev;

    // SlotIndex 表示当前 entry 所在的时间槽编号。
    // 取消或刷新时可以快速从 slot 中移除。
    public int SlotIndex;

    // EntryVersion 表示 entry 槽位版本号。
    // 它用于防止旧 DelayTimerHandle 操作复用后的 entry。
    public int EntryVersion;

    // Active 表示当前 entry 是否有效。
    // false 表示这个 entry 已到期、取消或在 FreeList 中。
    public bool Active;
}
```

---

### 7.7 DelayTimerHandle

```csharp
public readonly struct DelayTimerHandle
{
    // Index 表示过期任务在 DelayBufferWheel entry 池中的槽位编号。
    public readonly int Index;

    // Version 表示 entry 槽位版本号。
    // 槽位复用后版本会变化，旧 handle 会失效。
    public readonly int Version;

    // Invalid 表示无效句柄。
    // 没有注册过期任务，或者任务已经失效时可以使用它。
    public static readonly DelayTimerHandle Invalid = new DelayTimerHandle(-1, 0);

    // index 参数：entry 槽位编号。
    // version 参数：entry 槽位版本号。
    public DelayTimerHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    // IsValid 表示当前句柄是否可能有效。
    // 它只检查 Index，不代表任务一定仍然存在。
    public bool IsValid => Index >= 0;
}
```

---

### 7.8 Publish 时刷新过期任务

不建议 Lazy Expire。

Lazy Expire 是每次 Publish 都创建一个新的过期任务，到期时用版本号过滤旧任务。

它简单，但高频输入缓冲会积累很多无效 entry。

推荐 Refresh Timer：

```text
每个 DelayPublisher 只保留一个过期任务。
再次 Publish 时取消或重排旧 handle。
然后注册新的过期时间。
```

---

### 7.9 DelayBufferOptions

```csharp
public readonly struct DelayBufferOptions
{
    // TickDurationSeconds 表示 DelayBufferWheel 每一格代表多少秒。
    // 输入缓冲不需要高精度，通常 1/60 秒或 1/30 秒即可。
    public readonly float TickDurationSeconds;

    // WheelSize 表示 DelayBufferWheel 的槽数量。
    // TickDurationSeconds * WheelSize 就是最大推荐 TTL。
    public readonly int WheelSize;

    // InitialCapacity 表示过期任务池初始容量。
    // 它影响 DelayPublisher 高频 Publish 时的初始分配规模。
    public readonly int InitialCapacity;

    // MaxExpiredPerTick 表示每次 Tick 最多清理多少个到期缓存。
    // 避免同一帧大量缓存同时过期造成尖峰。
    public readonly int MaxExpiredPerTick;

    // tickDurationSeconds 参数：每个时间槽代表多少秒。
    // wheelSize 参数：时间轮槽数量。
    // initialCapacity 参数：初始过期任务容量。
    // maxExpiredPerTick 参数：单次 Tick 最多清理数量。
    public DelayBufferOptions(
        float tickDurationSeconds,
        int wheelSize,
        int initialCapacity,
        int maxExpiredPerTick)
    {
        TickDurationSeconds = tickDurationSeconds;
        WheelSize = wheelSize;
        InitialCapacity = initialCapacity;
        MaxExpiredPerTick = maxExpiredPerTick;
    }
}
```

---

### 7.10 contractId 互斥

保留 `contractId` 语义，但底层不再全表扫描。

推荐：

```text
DelayGroupTable
    key = ownerId + contractId
    value = active publisher handle / publisherId
```

发布时：

```text
如果同组已有 active publisher：
    清空旧 publisher。
    取消旧 publisher 的过期任务。

登记当前 publisher 为该组 active publisher。
```

`DelayContractKey`：

```csharp
public readonly struct DelayContractKey : IEquatable<DelayContractKey>
{
    // OwnerId 表示缓冲所属对象或系统编号。
    // 例如某个玩家、某个角色、某个 Layer。
    public readonly int OwnerId;

    // ContractId 表示互斥组编号。
    // 相同 OwnerId + ContractId 的 publisher 互斥。
    public readonly int ContractId;

    // ownerId 参数：缓冲所属对象或系统编号。
    // contractId 参数：互斥组编号。
    public DelayContractKey(int ownerId, int contractId)
    {
        OwnerId = ownerId;
        ContractId = contractId;
    }

    // Equals 表示两个 DelayContractKey 是否代表同一个互斥组。
    // other 参数：另一个要比较的 key。
    public bool Equals(DelayContractKey other)
    {
        return OwnerId == other.OwnerId &&
               ContractId == other.ContractId;
    }

    // GetHashCode 表示生成哈希值，用于 Dictionary 查找。
    public override int GetHashCode()
    {
        return HashCode.Combine(OwnerId, ContractId);
    }
}
```

---

## 8. 事件元数据策略设计

### 8.1 配置分层

配置分为两层，后续可以扩展第三层：

```text
整体配置
    PostSchedulerOptions
    TimeSchedulerOptions
    DelayBufferOptions

事件级全局配置
    EventMetaData<TEvent>.PostPolicy
    EventMetaData<TEvent>.TimerPolicy
    EventMetaData<TEvent>.BufferPolicy

单次调用覆盖
    TryPost(event, overridePolicy)
    Schedule(event, overrideTimerPolicy)
    Publish(value, overrideBufferPolicy)
```

推荐优先实现前两层。

单次调用覆盖不宜过度使用，因为它会让事件行为不稳定。

---

### 8.2 EventMetaData 扩展

```csharp
public abstract class EventMetaData<TEvent> : IEventMetaData
    where TEvent : struct
{
    // Category 表示事件分类。
    // 用于拓扑、审计、模块检索。
    public virtual EventCategoryToken Category => EventCategoryToken.Empty;

    // PostPolicy 表示这个事件类型进入 PostScheduler 时的默认策略。
    // 返回 null 时，使用 PostSchedulerOptions 中的整体默认配置。
    public virtual EventPostPolicy? PostPolicy => null;

    // TimerPolicy 表示这个事件类型被事件定时适配器调度时的默认策略。
    // 注意：TimeScheduler 核心不读取它，只有 Adapter 读取。
    public virtual EventTimerPolicy? TimerPolicy => null;

    // BufferPolicy 表示这个事件类型作为 DelayPublisher 缓存值时的默认策略。
    // 返回 null 时，使用 DelayBufferOptions 中的整体默认配置。
    public virtual EventBufferPolicy? BufferPolicy => null;

    // OnEventExpectation 表示事件处理异常时的全局观察点。
    public virtual void OnEventExpectation<TValue>(
        TValue e,
        Exception exception)
        where TValue : struct
    {
    }
}
```

---

### 8.3 EventPostPolicy

```csharp
public readonly struct EventPostPolicy
{
    // Mode 表示 Post 投递模式。
    // Normal 保留每次投递；Coalesced 合并同类通知；Latest 只保留最新值。
    public readonly PostDeliveryMode Mode;

    // Backpressure 表示队列满时采用的背压策略。
    // 例如 RejectNew、DropNewest、Coalesce、Latest。
    public readonly BackpressurePolicy Backpressure;

    // MaxPending 表示该事件类型最多允许挂起多少个。
    // 小于等于 0 表示不限制。
    public readonly int MaxPending;

    // mode 参数：Post 投递模式。
    // backpressure 参数：队列满时的处理策略。
    // maxPending 参数：该事件类型最多允许挂起多少个。
    public EventPostPolicy(
        PostDeliveryMode mode,
        BackpressurePolicy backpressure,
        int maxPending)
    {
        Mode = mode;
        Backpressure = backpressure;
        MaxPending = maxPending;
    }
}
```

---

### 8.4 EventTimerPolicy

```csharp
public readonly struct EventTimerPolicy
{
    // RepeatMode 表示循环任务的默认重复模式。
    // FixedDelay 更适合游戏主线程，因为它不会追赶补帧。
    public readonly TimerRepeatMode RepeatMode;

    // CatchUpPolicy 表示错过触发时的追赶策略。
    // SkipMissed 表示跳过错过触发，避免补帧风暴。
    public readonly TimerCatchUpPolicy CatchUpPolicy;

    // MaxCatchUpPerTick 表示单次 Tick 最多补触发多少次。
    // 只有 FireAllCapped 这类追赶策略才需要它。
    public readonly int MaxCatchUpPerTick;

    // PreferLongTimerHeap 表示长期任务是否优先进入 LongTimerHeap。
    // LongTimerHeap 用于减少短期时间轮反复扫描长期任务。
    public readonly bool PreferLongTimerHeap;

    // ExpiredPostPolicy 表示任务到期后进入 PostScheduler 时的策略。
    // 它属于 Adapter 语义，不属于 TimeScheduler 核心。
    public readonly EventPostPolicy? ExpiredPostPolicy;

    // repeatMode 参数：循环任务默认重复模式。
    // catchUpPolicy 参数：错过触发时的追赶策略。
    // maxCatchUpPerTick 参数：单次 Tick 最多补触发多少次。
    // preferLongTimerHeap 参数：是否优先使用长期任务堆。
    // expiredPostPolicy 参数：到期后 Post 的策略。
    public EventTimerPolicy(
        TimerRepeatMode repeatMode,
        TimerCatchUpPolicy catchUpPolicy,
        int maxCatchUpPerTick,
        bool preferLongTimerHeap,
        EventPostPolicy? expiredPostPolicy)
    {
        RepeatMode = repeatMode;
        CatchUpPolicy = catchUpPolicy;
        MaxCatchUpPerTick = maxCatchUpPerTick;
        PreferLongTimerHeap = preferLongTimerHeap;
        ExpiredPostPolicy = expiredPostPolicy;
    }
}
```

---

### 8.5 EventBufferPolicy

```csharp
public readonly struct EventBufferPolicy
{
    // Mode 表示缓冲模式。
    // Latest 表示只保留最新值。
    // Queue 表示保留多个值，适合输入缓冲队列。
    public readonly BufferMode Mode;

    // DefaultTtlSeconds 表示默认有效时间。
    // 小于等于 0 可以表示调用方必须显式传 TTL。
    public readonly float DefaultTtlSeconds;

    // Capacity 表示队列型缓冲最多保留多少条。
    // 对 Latest 模式可以忽略。
    public readonly int Capacity;

    // OverflowPolicy 表示缓冲满时如何处理。
    // 例如丢弃旧值、丢弃新值、覆盖最新值。
    public readonly BufferOverflowPolicy OverflowPolicy;

    // UseContractReplace 表示是否启用 contractId 互斥替换。
    // 它适合输入缓冲中同组动作互斥。
    public readonly bool UseContractReplace;

    // mode 参数：缓冲模式。
    // defaultTtlSeconds 参数：默认有效时间。
    // capacity 参数：队列容量。
    // overflowPolicy 参数：缓冲满时的处理方式。
    // useContractReplace 参数：是否启用 contractId 互斥替换。
    public EventBufferPolicy(
        BufferMode mode,
        float defaultTtlSeconds,
        int capacity,
        BufferOverflowPolicy overflowPolicy,
        bool useContractReplace)
    {
        Mode = mode;
        DefaultTtlSeconds = defaultTtlSeconds;
        Capacity = capacity;
        OverflowPolicy = overflowPolicy;
        UseContractReplace = useContractReplace;
    }
}

public enum BufferMode
{
    // Latest 表示只保留最新一次发布的值。
    Latest,

    // Queue 表示保留多个仍在有效期内的值。
    // 适合输入缓冲队列、连招缓冲等。
    Queue
}

public enum BufferOverflowPolicy
{
    // DropOldest 表示缓冲满时丢弃最旧值。
    DropOldest,

    // DropNewest 表示缓冲满时拒绝新值。
    DropNewest,

    // ReplaceLatest 表示覆盖最新值。
    ReplaceLatest
}
```

---

### 8.6 EventRuntimePolicyTable

事件元数据不要直接参与热路径。

构建阶段解析成：

```text
EventRuntimePolicyTable
    EventPostPolicy[] ByEventTypeId
    EventTimerPolicy[] ByEventTypeId
    EventBufferPolicy[] ByEventTypeId
```

运行时：

```text
PostScheduler 通过 EventTypeId 查 PostPolicy。
EventTimerAdapter 通过 EventTypeId 查 TimerPolicy / ExpiredPostPolicy。
DelayBufferSystem 通过 EventTypeId 查 BufferPolicy。
```

不要每次 Post / Schedule / Publish 都查 Type、Attribute 或 Dictionary。

---

## 9. LayerRuntime.Pump 顺序

推荐顺序：

```text
LayerRuntime.Pump(deltaTime)
    1. TimeScheduler.Tick(deltaTime)
        处理通用定时任务。
        通过 Sink 输出到期任务。

    2. DelayBufferSystem.Tick(deltaTime)
        使用 ShortDelayWheel 清理过期缓存。
        不派发事件。

    3. PostScheduler.Pump(postOptions)
        Flush CoalescedBuffer。
        Flush LatestBuffer。
        消费 ReadyQueue。
        根据 Wave / FrameBudget / Backpressure 控制执行量。

    4. EventMetaDataHandler.PumpExpectations()
        处理异常观察队列。
```

如果 DelayBufferSystem 复用底层 TimeWheelCore，也仍建议在语义上保留独立步骤。

---

## 10. 对外 API 建议

### 10.1 Post API

```csharp
LayerRuntime.Post(new SomeEvent());

var result = LayerRuntime.TryPost(new SomeEvent());

LayerRuntime.PostCoalesced(new InventoryChangedEvent());

LayerRuntime.PostLatest(new PlayerPositionViewEvent(position));
```

建议文档强调：

```text
Post 不立即执行。
Post 进入全局 PostScheduler。
Post 可能因为帧预算延后。
Post 可能因为背压被拒绝、丢弃、合并或覆盖。
```

---

### 10.2 Time API

核心 API 不叫 `PostDelayed`。

```csharp
var handle = LayerRuntime.Time.Schedule(
    new CooldownFinishedEvent(skillId),
    delaySeconds: 3.5f);

LayerRuntime.Time.Cancel(handle);
```

如果需要到期后 Post，用 Adapter 或语法糖：

```csharp
LayerRuntime.Time.SchedulePost(
    new CooldownFinishedEvent(skillId),
    delaySeconds: 3.5f);
```

文档注明：

```text
SchedulePost = TimeScheduler + PostTimerAdapter
不是 PostScheduler 的核心语义。
```

---

### 10.3 DelayPublisher API

```csharp
inputBuffer.Publish(new JumpInputEvent(), ttlSeconds: 0.15f);

if (inputBuffer.TryTake(out var input))
{
    LayerRuntime.Send(new PlayerJumpEvent());
}
```

文档强调：

```text
DelayPublisher 不会自动派发事件。
DelayPublisher 是有 TTL 的缓存消息管道。
消费者需要主动 TryGet / TryTake。
TTL 到期后缓存值失效。
```

---

## 11. 迁移路线

### 阶段 1：新增 PostScheduler

目标：替换旧 Post 底层队列，但不破坏用户 API。

完成：

```text
ReadyQueue
NextQueue
Wave
PostResult
PostPumpStats
BackpressurePolicy
Frame Budget
```

暂时只支持 `PostDeliveryMode.Normal`。

---

### 阶段 2：接入 EventPostPolicy

目标：让事件元数据控制事件级 Post 策略。

完成：

```text
EventPostPolicy
PostDeliveryMode.Coalesced
PostDeliveryMode.Latest
CoalescedBuffer
LatestBuffer
EventRuntimePolicyTable
```

---

### 阶段 3：新增 TimeScheduler

目标：引入通用定时调度能力，但不替代 DelayPublisher。

完成：

```text
ShortWheel
LongTimerHeap
TimerHandle
Schedule
Cancel
Tick
IExpiredTimerSink
Once
FixedDelay
SkipMissed
```

---

### 阶段 4：改造 DelayPublisher 过期机制

目标：保留 DelayPublisher 缓存语义，但用 ShortDelayWheel 管理 TTL。

完成：

```text
DelayBufferWheel
DelayTimerHandle
DelayExpireEntry
DelayPublisher version
ScheduleOrRefresh
contractId 表驱动互斥
```

移除或弱化旧的全量 Update 扫描。

---

### 阶段 5：接入 EventTimerPolicy / EventBufferPolicy

目标：事件级全局策略覆盖通用时间调度和缓存缓冲。

完成：

```text
EventTimerPolicy
EventBufferPolicy
EventRuntimePolicyTable 扩展
TimeScheduler Adapter 策略解析
DelayBufferSystem 策略解析
```

---

### 阶段 6：Parallel Bridge 独立扩展

MPSC 不进入核心 PostScheduler。

未来 Parallel / Async 服务需要跨线程投递时，单独提供：

```text
ThreadBridge
    MPSC Queue
    PostFromAnyThread
    Pump 时搬运到 PostScheduler
```

这样核心主线程 Post 热路径不被跨线程成本污染。

---

## 12. 最终设计边界总结

```text
Send
    即时同步通信。
    不受帧预算、背压、合并影响。

PostScheduler
    处理已经就绪的延后事件。
    负责 FrameBudget、Backpressure、Coalesced、Latest。

TimeScheduler
    处理通用定时任务。
    负责 TimeWheel、LongTimerHeap、循环任务、取消句柄。
    不直接派发事件。

DelayPublisher
    处理短时间缓存消息。
    负责 TryGet、TryTake、TTL、contractId 互斥。
    TTL 由 ShortDelayWheel 清理。
    不自动派发事件。

EventMetaData
    描述事件级全局策略。
    构建阶段解析成 EventRuntimePolicyTable。
    热路径只按 EventTypeId 查数组。
```

一句话总结：

```text
LayerBase 的运行时应拆成：
即时通信、就绪事件调度、时间到期调度、TTL 缓存消息管道。

它们可以组合，但不应该互相吞并。
```

这样改造后，LayerBase 会获得更清楚的语义边界：

```text
Send 管正确性。
Post 管延后调度。
TimeScheduler 管时间到期。
DelayPublisher 管短期输入 / 消息缓冲。
EventMetaData 管事件级默认策略。
```

这比继续把 `Post`、`Delay`、`Pump` 混在一条管线里更适合长期维护，也更符合 LayerBase 作为游戏架构总线的定位。

