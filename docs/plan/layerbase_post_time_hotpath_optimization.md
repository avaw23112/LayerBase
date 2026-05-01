# LayerBase Post / Time 热路径优化方案

> 核心原则：**Build 阶段生成 Plan + Bitmap；热路径只读稳定快照；复杂逻辑全部下沉到慢路径；最后才考虑 Unsafe。**

本文档用于指导 `LayerBase` 中 `PostScheduler` 与 `TimeScheduler` 的热路径优化。  
`Layer` 侧只保留 `HasDelayMask` 相关优化，不再引入 `UpdateMask`，因为 `IUpdate` 已经可以在 Build 阶段被收集成独立调度组。

---

## 目录

- [1. 总原则](#1-总原则)
- [2. 术语解释](#2-术语解释)
- [3. 当前问题概览](#3-当前问题概览)
- [4. Post 热路径优化方案](#4-post-热路径优化方案)
- [5. Time 热路径优化方案](#5-time-热路径优化方案)
- [6. Layer 侧结论：不要 UpdateMask，只保留 HasDelayMask](#6-layer-侧结论不要-updatemask只保留-hasdelaymask)
- [7. JIT 友好代码规则](#7-jit-友好代码规则)
- [8. Unsafe 使用边界](#8-unsafe-使用边界)
- [9. 分阶段落地计划](#9-分阶段落地计划)
- [10. 基准测试建议](#10-基准测试建议)

---

# 1. 总原则

LayerBase 的热路径优化不应该从“到处加 Unsafe”开始，而应该从架构原则开始：

```text
Build 阶段能知道的，不允许热路径重算。
Build 阶段能排序的，不允许热路径排序。
Build 阶段能绑定的，不允许热路径查字典。
Build 阶段能确定的策略，不允许热路径查 metadata。
Build 阶段能冻结的 List，不允许热路径继续遍历 List。
热路径只读数组、只读 Plan、只读 Bitmap、只读 Snapshot。
结构变化只改 cold data，然后标 dirty。
dirty rebuild 只能发生在 Build、Commit、Reset、Pump 开头等安全点。
Unsafe 只能用于已经由 Build 不变量保护的数组访问。
```

推荐命名：

```text
Build-Time BitPlan

中文：
构建期位图计划
```

完整表达：

```text
Build 阶段生成 Plan + Bitmap；
热路径先查 Bitmap 分流；
再读 Plan 数组执行；
禁止热路径查字典、重算策略、重建结构。
```

---

# 2. 术语解释

## 2.1 热路径

**热路径**指每帧、每次事件、每次计时器 Tick、每次 Call 都会反复执行的代码。

例如：

```text
Post<T>
Send<T>
Pump
Tick
CallAsync
ProcessCurrentSlot
```

这些路径上每多一个分支、锁、字典查找、接口调用、数组越界检查，都可能被调用次数放大。

## 2.2 冷路径

**冷路径**指低频执行的代码。

例如：

```text
Build
Subscribe
Unsubscribe
Reset
Grow
QueueFull
PolicyOverride
Coalesced Merge Failure
Timer CatchUp
```

冷路径可以更复杂，可以使用字典、List、锁、异常构造等结构。

## 2.3 Plan

**Plan** 是 Build 阶段生成的执行计划。

它把原本需要运行时判断的策略，提前压缩成稳定字段。

例如：

```text
PostTypePlan
TimerWheelPlan
LayerPumpPlan
```

热路径不再询问：

```text
这个事件是什么模式？
这个 timer 是什么策略？
这个 layer 有没有 update？
```

而是直接读 Plan。

## 2.4 Bitmap / 位图

**Bitmap** 是用整数的每一位表示一个对象是否具备某个属性。

例如：

```text
第 12 位为 1 表示 EventTypeId = 12 是 Latest 事件。
第 20 位为 1 表示 EventTypeId = 20 需要 TrackPending。
```

一个 `ulong` 有 64 位，所以可以表示 64 个事件类型的某种状态。

## 2.5 Mask / 掩码

**Mask** 是用于检查某一位是否存在的整数。

例如：

```csharp
var bit = 1UL << eventTypeId;
```

如果 `eventTypeId = 12`，那么 `bit` 的第 12 位为 1。

## 2.6 JIT

**JIT** 是 Just-In-Time Compiler 的缩写。

在 .NET 中，C# 代码会先编译成 IL，中间语言。程序运行时，JIT 会把 IL 编译成 CPU 能直接执行的机器码。

为了让 JIT 更容易优化，热路径代码应该：

```text
短
直
少分支
少虚调用
少异常路径
少泛型复杂逻辑
少不透明 helper
```

## 2.7 内联

**内联**指 JIT 把一个小函数直接展开到调用处。

例如：

```csharp
A() 调用 B()
```

如果 B 很小，JIT 可能把 B 的代码直接放进 A 里，减少函数调用成本。

## 2.8 NoInlining

`MethodImplOptions.NoInlining` 表示告诉 JIT 不要把该函数内联。

它适合慢路径。

例如：

```text
QueueFull
Grow
PolicyOverride
TimerCatchUp
CoalescedMergeFailure
```

这样可以避免慢路径代码污染快路径的机器码布局。

## 2.9 Unsafe

`Unsafe` 指 `System.Runtime.CompilerServices.Unsafe` 里的底层 API。

常见用途：

```text
Unsafe.Add(ref baseRef, index)
```

它可以绕过部分数组边界检查。

但是 Unsafe 不负责安全，一旦 index 错误，可能不是抛异常，而是破坏内存状态。

所以本文档的原则是：

```text
先 Plan 化、Bitmap 化、数组化、慢路径下沉；
最后才考虑 Unsafe。
```

---

# 3. 当前问题概览

## 3.1 Post 当前问题

当前 `PostScheduler` 的主要开销集中在：

```text
TryPost<T> 每次查 policy table
TryPost<T> 每次 switch policy.Mode
Normal Post 即使 MaxPending == 0，也会维护 pending count
EventPayloadStorage 通过 ConcurrentDictionary<int, IEventStore> 找 store
Dispatch / Release 时又根据 PayloadHandle.EventTypeId 查 store
EventStore<T> 内部有 lock
RingBuffer 使用 % capacity
Pump 总是 Stopwatch.StartNew
DirtySignal 使用 BitArray + pending list
Latest 使用数组 + pending list
Coalesced 使用 Dictionary，且排序发生在 Flush
```

其中最大的问题不是数组边界检查，而是：

```text
热路径仍然在做运行时决策。
```

## 3.2 Time 当前问题

当前 `TimeScheduler` 的主要开销集中在：

```text
Tick 中 while 结构没有区分 0/1 tick 快路径和多 tick 慢路径
WheelSize 使用 % 取模
_options 字段在热路径反复读取
long timer heap 使用泛型 PriorityQueue<int, long>
PriorityQueue 内部使用 tuple + IComparer<TPriority>
free list 使用 Stack<int>
一次性 timer 和重复 timer 的处理逻辑混在同一热循环里
RepeatMode / CatchUpPolicy 作为 enum 在过期处理时反复判断
```

Time 的方向已经正确：时间轮 + 长定时器堆。  
接下来要做的是让 `TickOnce` 和 `ProcessCurrentSlot` 足够短。

## 3.3 Layer 当前问题

`IUpdate` 不需要 `UpdateMask`。

原因：

```text
IUpdate 可以在 Build 阶段被收集成独立调度组。
热路径只遍历这个调度组。
没有必要再对每个 Layer 做 has update 判断。
```

但 `HasDelayMask` 有价值。

原因：

```text
DelayPublisher 可能不存在。
如果当前 runtime 没有任何 delay publisher，则每帧不应该进入 DelayPublisherManager.Tick。
```

---

# 4. Post 热路径优化方案

## 4.1 目标结构

Post 优化后的结构：

```text
Build 阶段：
    生成 PostTypePlan[]
    生成 PostBitmap
    生成 PayloadStore 缓存
    初始化 Dirty / Latest / Coalesced 所需冷结构

热路径：
    TryPost<T>
        disposed check
        typeId
        special bitmap check
        NormalFastPath

慢路径：
    DirtySignal
    Latest
    Coalesced
    TrackPending
    CustomBackpressure
    PolicyOverride
    QueueFull
    Grow
```

普通事件路径应该压缩为：

```text
typeId -> special mask 未命中 -> Store -> Enqueue -> return
```

## 4.2 PostTypePlan

`EventPostPolicy` 继续保留语义，但它不应该作为热路径查询对象。

Build 阶段应把它压缩为 `PostTypePlan`：

```csharp
internal readonly struct PostTypePlan
{
    // EventTypeId：事件类型稳定 ID。
    // 它用于索引 plan 数组、bitmap 数组、payload store 表。
    public readonly int EventTypeId;

    // Mode：投递模式。
    // 例如 Normal、DirtySignal、Latest、Coalesced。
    // 它保留语义，但普通热路径不应该直接 switch 它。
    public readonly PostDeliveryMode Mode;

    // Backpressure：背压策略。
    // 背压表示队列满时系统如何处理新事件。
    // 例如 RejectNew、DropNewest、DropOldest。
    public readonly BackpressurePolicy Backpressure;

    // MaxPending：该事件类型允许积压的最大数量。
    // 0 表示不限制积压。
    public readonly int MaxPending;

    // TrackPending：是否真的需要维护 pending counter。
    // 它在 Build 阶段根据 MaxPending 预计算。
    // 这样 MaxPending == 0 的普通事件不会在热路径上加锁增减计数。
    public readonly bool TrackPending;

    // HasCustomBackpressure：是否使用非默认背压策略。
    // 默认背压不需要进入特殊路径。
    public readonly bool HasCustomBackpressure;

    public PostTypePlan(
        int eventTypeId,                        // eventTypeId：事件类型稳定 ID。
        PostDeliveryMode mode,                  // mode：事件投递模式。
        BackpressurePolicy backpressure,        // backpressure：队列满时的处理策略。
        int maxPending,                         // maxPending：最大积压数量，0 表示不限制。
        BackpressurePolicy defaultBackpressure  // defaultBackpressure：runtime 默认背压策略。
    )
    {
        EventTypeId = eventTypeId;
        Mode = mode;
        Backpressure = backpressure;
        MaxPending = maxPending;

        // 只有 MaxPending > 0 时才需要维护 pending counter。
        // 这可以消掉默认 Normal Post 的一对 lock。
        TrackPending = maxPending > 0;

        // 只有非默认背压才需要进入特殊路径读取 plan。
        HasCustomBackpressure = backpressure != defaultBackpressure;
    }
}
```

## 4.3 PostBitmap

`PostBitmap` 用于快速判断某个事件是否可以走 NormalFastPath。

```csharp
using System.Runtime.CompilerServices;

internal sealed class PostBitmap
{
    // _specialMask：所有不能走 NormalFastPath 的事件集合。
    // 包括 DirtySignal、Latest、Coalesced、TrackPending、自定义背压。
    private ulong[] _specialMask = Array.Empty<ulong>();

    // _dirtyMask：DirtySignal 事件集合。
    // DirtySignal 表示只通知“该类型发生过”，不保留 payload。
    private ulong[] _dirtyMask = Array.Empty<ulong>();

    // _latestMask：Latest 事件集合。
    // Latest 表示同类型事件只保留最后一次 payload。
    private ulong[] _latestMask = Array.Empty<ulong>();

    // _coalescedMask：Coalesced 事件集合。
    // Coalesced 表示同类事件可以尝试合并 payload。
    private ulong[] _coalescedMask = Array.Empty<ulong>();

    // _trackPendingMask：需要维护 pending counter 的事件集合。
    private ulong[] _trackPendingMask = Array.Empty<ulong>();

    public void Build(
        ReadOnlySpan<PostTypePlan> plans // plans：Build 阶段生成的事件投递计划。
    )
    {
        var maxEventTypeId = 0;

        // 找出最大的 EventTypeId。
        // 这用于决定需要多少个 ulong segment。
        for (var i = 0; i < plans.Length; i++)
        {
            if (plans[i].EventTypeId > maxEventTypeId)
                maxEventTypeId = plans[i].EventTypeId;
        }

        // 一个 ulong 有 64 位。
        // eventTypeId >> 6 等价于 eventTypeId / 64。
        var segmentCount = (maxEventTypeId >> 6) + 1;

        _specialMask = new ulong[segmentCount];
        _dirtyMask = new ulong[segmentCount];
        _latestMask = new ulong[segmentCount];
        _coalescedMask = new ulong[segmentCount];
        _trackPendingMask = new ulong[segmentCount];

        for (var i = 0; i < plans.Length; i++)
        {
            var plan = plans[i];
            var typeId = plan.EventTypeId;

            // segment：当前 EventTypeId 落在哪个 ulong 段。
            var segment = typeId >> 6;

            // bit：当前 EventTypeId 在该 segment 内对应的位。
            // typeId & 63 等价于 typeId % 64。
            var bit = 1UL << (typeId & 63);

            if (plan.Mode == PostDeliveryMode.DirtySignal)
                _dirtyMask[segment] |= bit;

            if (plan.Mode == PostDeliveryMode.Latest)
                _latestMask[segment] |= bit;

            if (plan.Mode == PostDeliveryMode.Coalesced)
                _coalescedMask[segment] |= bit;

            if (plan.TrackPending)
                _trackPendingMask[segment] |= bit;

            if (plan.Mode != PostDeliveryMode.Normal ||
                plan.TrackPending ||
                plan.HasCustomBackpressure)
            {
                _specialMask[segment] |= bit;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSpecial(
        int eventTypeId // eventTypeId：事件类型稳定 ID。
    )
    {
        var segment = eventTypeId >> 6;
        if ((uint)segment >= (uint)_specialMask.Length)
            return false;

        var bit = 1UL << (eventTypeId & 63);
        return (_specialMask[segment] & bit) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDirty(
        int eventTypeId // eventTypeId：事件类型稳定 ID。
    )
    {
        var segment = eventTypeId >> 6;
        if ((uint)segment >= (uint)_dirtyMask.Length)
            return false;

        var bit = 1UL << (eventTypeId & 63);
        return (_dirtyMask[segment] & bit) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLatest(
        int eventTypeId // eventTypeId：事件类型稳定 ID。
    )
    {
        var segment = eventTypeId >> 6;
        if ((uint)segment >= (uint)_latestMask.Length)
            return false;

        var bit = 1UL << (eventTypeId & 63);
        return (_latestMask[segment] & bit) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCoalesced(
        int eventTypeId // eventTypeId：事件类型稳定 ID。
    )
    {
        var segment = eventTypeId >> 6;
        if ((uint)segment >= (uint)_coalescedMask.Length)
            return false;

        var bit = 1UL << (eventTypeId & 63);
        return (_coalescedMask[segment] & bit) != 0;
    }
}
```

## 4.4 TryPost 快路径

`TryPost<T>` 要尽量短。

不建议让常规入口携带 `EventPostPolicy? policyOverride`。  
兼容旧 API 可以保留，但它应该进入慢路径。

```csharp
using System.Runtime.CompilerServices;

public sealed partial class PostScheduler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPost<T>(
        in T value // value：事件 payload，struct 通过 in 传入，避免不必要复制。
    ) where T : struct
    {
        // disposed 是罕见分支。
        // 命中后进入 NoInlining 慢路径，避免污染普通 Post 的机器码。
        if (_disposed)
            return FailSchedulerDisposed();

        // typeId：泛型静态事件 ID。
        // EventTypeId<T>.Id 通常只初始化一次，之后读取很便宜。
        var typeId = EventTypeId<T>.Id;

        // 大多数事件应该是普通 Normal Post。
        // special mask 未命中时，不查 policy，不 switch，不维护 pending。
        if (!_postBitmap.IsSpecial(typeId))
            return EnqueueNormalFast(typeId, in value);

        // DirtySignal、Latest、Coalesced、TrackPending、自定义背压全部进入慢路径。
        return TryPostSpecial(typeId, in value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult FailSchedulerDisposed()
    {
        return PostResult.Failure("Scheduler disposed");
    }
}
```

## 4.5 Special 慢路径

```csharp
public sealed partial class PostScheduler
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult TryPostSpecial<T>(
        int typeId, // typeId：事件类型稳定 ID。
        in T value  // value：事件 payload。
    ) where T : struct
    {
        // DirtySignal 不保存 payload，只记录该事件类型发生过。
        if (_postBitmap.IsDirty(typeId))
            return MarkDirtyById<T>(typeId);

        // Latest 只保留最后一个 payload。
        if (_postBitmap.IsLatest(typeId))
            return EnqueueLatest(typeId, in value);

        // Coalesced 会读取 metadata 并尝试合并 payload。
        // 这是明显慢路径。
        if (_postBitmap.IsCoalesced(typeId))
            return EnqueueCoalesced(typeId, in value);

        // 剩余情况通常是 Normal + TrackPending 或自定义背压。
        ref readonly var plan = ref _postPlans.Get(typeId);
        return EnqueueNormalWithPlan(typeId, in value, in plan);
    }
}
```

## 4.6 NormalFastPath

普通 Post 快路径只做必要动作：

```text
Store payload
sequence++
ring enqueue
return
```

```csharp
public sealed partial class PostScheduler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult EnqueueNormalFast<T>(
        int typeId, // typeId：事件类型稳定 ID。
        in T value  // value：事件 payload。
    ) where T : struct
    {
        // GetStoreFast：直接获取 T 对应的 EventStore<T>。
        // 目标是避免 ConcurrentDictionary<int, IEventStore>。
        var store = _payloadStores.GetStoreFast<T>(_runtimeId);

        // handle：payload 的位置句柄。
        // 句柄包含 index 与 version，用于防止旧句柄误用。
        var handle = store.Add(in value);

        // sequenceId：全局递增序号。
        // 它用于需要保持出现顺序的逻辑。
        var sequenceId = Interlocked.Increment(ref _sequenceCounter);

        // PostItem：队列中的轻量投递项。
        // 普通路径使用默认背压策略。
        var item = new PostItem(typeId, handle, sequenceId, _defaultBackpressure);

        // 队列未满是常见情况。
        if (_readyQueue.TryEnqueue(in item))
            return PostResult.Success;

        // 队列满是少见情况，进入慢路径。
        return HandleQueueFullSlow(in item, store);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult HandleQueueFullSlow<T>(
        in PostItem item,       // item：入队失败的投递项。
        EventStore<T> store     // store：当前事件类型对应的 payload store。
    ) where T : struct
    {
        // 这里处理 RejectNew、DropNewest、DropOldest 等策略。
        // 该逻辑不应该进入普通 Post 的内联代码。
        store.Release(item.PayloadHandle);
        return PostResult.Failure("Queue full");
    }
}
```

## 4.7 PendingCounter 优化

当前最应该先做的优化：

```text
MaxPending == 0 时，完全不维护 pending counter。
```

错误方向：

```text
让 pending counter 的数组访问更快。
```

正确方向：

```text
默认不写 pending counter。
只有 TrackPendingMask 命中时，才维护 pending counter。
```

```csharp
private PostResult EnqueueNormalWithPlan<T>(
    int typeId,                  // typeId：事件类型稳定 ID。
    in T value,                  // value：事件 payload。
    in PostTypePlan plan         // plan：Build 阶段生成的投递计划。
) where T : struct
{
    // 只有需要限制积压数量时才进入 pending counter。
    if (plan.TrackPending && IsPendingFull(typeId, plan.MaxPending))
        return PostResult.Failure("Max pending reached");

    var store = _payloadStores.GetStoreFast<T>(_runtimeId);
    var handle = store.Add(in value);
    var sequenceId = Interlocked.Increment(ref _sequenceCounter);
    var item = new PostItem(typeId, handle, sequenceId, plan.Backpressure);

    var result = EnqueueItemWithPolicy(in item, plan.Backpressure);

    if (result.IsSuccess && plan.TrackPending)
        IncrementPending(typeId);

    return result;
}
```

## 4.8 PayloadStore 优化

当前 `EventPayloadStorage` 使用 `ConcurrentDictionary<int, IEventStore>`。  
建议改成泛型静态缓存，按 `runtimeId` 索引。

这种方式更接近 `LayerHub` 的静态泛型缓存思路。

```csharp
internal static class PayloadStoreCache<T> where T : struct
{
    // Stores：每个 runtime 一个 EventStore<T>。
    // runtimeId 必须是稳定小整数。
    // 当前 LayerHub 已经限制 runtimeId < 64。
    public static readonly EventStore<T>?[] Stores = new EventStore<T>[64];
}

internal sealed class PayloadStoreTable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventStore<T> GetStoreFast<T>(
        int runtimeId // runtimeId：当前 LayerRuntime 的稳定 ID。
    ) where T : struct
    {
        var store = PayloadStoreCache<T>.Stores[runtimeId];
        if (store != null)
            return store;

        return CreateStoreSlow<T>(runtimeId);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private EventStore<T> CreateStoreSlow<T>(
        int runtimeId // runtimeId：当前 LayerRuntime 的稳定 ID。
    ) where T : struct
    {
        lock (PayloadStoreCache<T>.Stores)
        {
            var store = PayloadStoreCache<T>.Stores[runtimeId];
            if (store != null)
                return store;

            store = new EventStore<T>();
            PayloadStoreCache<T>.Stores[runtimeId] = store;
            return store;
        }
    }
}
```

## 4.9 RingBuffer 优化

当前 `RingBuffer<T>` 使用 `% _capacity`。  
建议要求容量为 2 的幂，然后用 `& mask`。

```csharp
using System.Runtime.CompilerServices;

internal sealed class FastRingBuffer<T>
{
    private readonly T[] _buffer;

    // _mask：等于容量 - 1。
    // 当容量是 2 的幂时，index & _mask 等价于 index % capacity。
    private readonly int _mask;

    private int _head;
    private int _tail;
    private int _count;

    public int Count => _count;
    public bool IsEmpty => _count == 0;
    public bool IsFull => _count == _buffer.Length;

    public FastRingBuffer(
        int capacity // capacity：队列容量，必须是 2 的幂。
    )
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if ((capacity & (capacity - 1)) != 0)
            throw new ArgumentException("Capacity must be power of two.", nameof(capacity));

        _buffer = new T[capacity];
        _mask = capacity - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(
        in T item // item：要写入环形队列的元素。
    )
    {
        if (_count == _buffer.Length)
            return false;

        _buffer[_tail] = item;
        _tail = (_tail + 1) & _mask;
        _count++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(
        out T item // item：出队成功时返回队首元素。
    )
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];

        // 如果 T 含引用字段，清空可以避免延长对象生命周期。
        // 如果 T 是纯值类型且不含引用字段，可以考虑提供 no-clear 版本。
        _buffer[_head] = default!;

        _head = (_head + 1) & _mask;
        _count--;
        return true;
    }
}
```

## 4.10 DirtySignal 改为 ulong 位图

当前 DirtySignal 使用 `BitArray` 与 `List<int>`。  
建议改成 pending bitmap。

```csharp
using System.Numerics;

private int FlushDirtySignals()
{
    var flushed = 0;

    for (var segment = 0; segment < _dirtyPendingBits.Length; segment++)
    {
        var bits = _dirtyPendingBits[segment];
        if (bits == 0)
            continue;

        // 先清空 segment。
        // 即使 Dispatch 过程中再次 MarkDirty，也会重新置位。
        _dirtyPendingBits[segment] = 0;

        while (bits != 0)
        {
            // bitIndex：当前 segment 内最低位的 1。
            var bitIndex = BitOperations.TrailingZeroCount(bits);

            // typeId：还原完整 EventTypeId。
            var typeId = (segment << 6) + bitIndex;

            // DispatchDefaultById：按事件类型 ID 派发 default(T)。
            DispatchDefaultById(typeId);

            // 清掉最低位的 1。
            bits &= bits - 1;

            flushed++;
        }
    }

    return flushed;
}
```

## 4.11 Latest 也可以改为 pending bitmap

当前 Latest 使用 `_latestBuffer` + `_pendingLatest`。  
可以保留 `_latestBuffer`，但把 `_pendingLatest` 从 `List<int>` 改为 bitmap。

```text
_latestBuffer[typeId] 保存最后 payload handle
_latestPendingBits 标记哪些 typeId 当前有待派发 Latest
```

好处：

```text
不需要 List<int>.Add
不需要担心重复加入 pending list
Flush 时按 bit 枚举即可
```

## 4.12 Coalesced 保留 Dictionary，但只在慢路径

Coalesced 的 key 合并与 payload merge 本身就是复杂逻辑。  
它不应该为了极致性能强行无字典化。

建议：

```text
Coalesced 保留 Dictionary<CoalescedSlotKey, CoalescedSlot>
但它必须被 PostBitmap 挡在慢路径里
Normal Post 永远不触碰这个结构
```

## 4.13 Pump 统计按需启用

当前 `Pump()` 总是创建 `Stopwatch`。  
建议：

```text
MaxMillisecondsPerPump <= 0 且没有 stats 订阅时：
    不读时间
    不创建 Stopwatch

启用时间限制时：
    使用 Stopwatch.GetTimestamp()
```

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool ShouldStopByTime(
    long startTimestamp, // startTimestamp：Pump 开始时的 Stopwatch 时间戳。
    int processed        // processed：当前已经处理的事件数量。
)
{
    if (_maxMillisecondsPerPump <= 0)
        return false;

    // TimeCheckInterval：每处理多少事件检查一次时间。
    // 避免每个事件都读取高精度时钟。
    if (processed % _timeCheckInterval != 0)
        return false;

    var elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
    var elapsedMs = elapsedTicks * _timestampToMilliseconds;

    return elapsedMs >= _maxMillisecondsPerPump;
}
```

---

# 5. Time 热路径优化方案

## 5.1 目标结构

Time 优化后的结构：

```text
Build / 构造阶段：
    校验 WheelSize 是 2 的幂
    生成 WheelMask
    固化 TickDuration
    固化 TickDurationReciprocal
    固化 MaxExpiredPerTick
    固化 MaxPromotePerTick
    初始化 wheel
    初始化 timer pool
    初始化 LongTimerHeap
    初始化 IntStack

热路径：
    Tick
        disposed check
        accumulator
        0 tick 快速 return
        TickOnce

慢路径：
    多 tick catch-up
    timer pool grow
    heap grow
    repeat reschedule
    catch-up policy
```

## 5.2 TimerWheelPlan

```csharp
internal readonly struct TimerWheelPlan
{
    // WheelSize：时间轮槽数量。
    // 必须是 2 的幂，例如 256、512、1024。
    public readonly int WheelSize;

    // WheelMask：等于 WheelSize - 1。
    // 当 WheelSize 是 2 的幂时，tick & WheelMask 等价于 tick % WheelSize。
    public readonly int WheelMask;

    // TickDurationSeconds：每个逻辑 tick 对应多少秒。
    public readonly float TickDurationSeconds;

    // TickDurationReciprocal：TickDurationSeconds 的倒数。
    // 用 seconds * reciprocal 替代 seconds / tickDuration。
    public readonly float TickDurationReciprocal;

    public TimerWheelPlan(
        int wheelSize,             // wheelSize：时间轮槽数，必须是 2 的幂。
        float tickDurationSeconds  // tickDurationSeconds：单个 tick 的秒数。
    )
    {
        if (wheelSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(wheelSize));

        if ((wheelSize & (wheelSize - 1)) != 0)
            throw new ArgumentException("WheelSize must be power of two.", nameof(wheelSize));

        WheelSize = wheelSize;
        WheelMask = wheelSize - 1;
        TickDurationSeconds = tickDurationSeconds;
        TickDurationReciprocal = 1f / tickDurationSeconds;
    }
}
```

## 5.3 TimeScheduler 字段固化

不要在热路径反复读 `_options.Xxx`。

```csharp
private readonly int _wheelSize;
private readonly int _wheelMask;
private readonly int _maxPromotePerTick;
private readonly int _maxExpiredPerTick;
private readonly float _tickDuration;
private readonly float _tickDurationReciprocal;
```

构造阶段：

```csharp
public TimeScheduler(
    TimeSchedulerOptions options // options：用户配置的 timer 参数。
)
{
    var plan = new TimerWheelPlan(options.WheelSize, options.TickDurationSeconds);

    _wheelSize = plan.WheelSize;
    _wheelMask = plan.WheelMask;
    _tickDuration = plan.TickDurationSeconds;
    _tickDurationReciprocal = plan.TickDurationReciprocal;

    _maxPromotePerTick = options.MaxPromotePerTick;
    _maxExpiredPerTick = options.MaxExpiredPerTick;

    _pool = new TimerEntry<TPayload>[options.InitialTimerCapacity];
    _wheel = new int[_wheelSize];
    Array.Fill(_wheel, -1);

    _freeList = new IntStack(options.InitialTimerCapacity);
    _longHeap = new LongTimerHeap(16);
}
```

## 5.4 Tick 拆成单 tick 快路径

```csharp
using System.Runtime.CompilerServices;

public void Tick(
    float deltaTime,                 // deltaTime：本帧经过的秒数。
    IExpiredTimerSink<TPayload> sink  // sink：过期 timer 的接收者。
)
{
    if (_disposed)
        return;

    _accumulator += deltaTime;

    // 大多数帧不会推进 timer tick。
    if (_accumulator < _tickDuration)
        return;

    _accumulator -= _tickDuration;
    TickOnce(sink);

    // 多 tick 补偿属于慢路径。
    if (_accumulator >= _tickDuration)
        TickCatchUpSlow(sink);
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void TickOnce(
    IExpiredTimerSink<TPayload> sink // sink：过期 timer 的接收者。
)
{
    _currentTick++;

    if (_longHeap.Count != 0)
        PromoteLongTimers();

    ProcessCurrentSlot(sink);
}

[MethodImpl(MethodImplOptions.NoInlining)]
private void TickCatchUpSlow(
    IExpiredTimerSink<TPayload> sink // sink：过期 timer 的接收者。
)
{
    while (_accumulator >= _tickDuration)
    {
        _accumulator -= _tickDuration;
        TickOnce(sink);
    }
}
```

## 5.5 槽位计算用 mask

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private int GetSlot(
    long tick // tick：目标逻辑 tick。
)
{
    return (int)(tick & _wheelMask);
}
```

替代所有：

```csharp
(int)(tick % _options.WheelSize)
```

## 5.6 LongTimerHeap

用专用堆替代泛型 `PriorityQueue<int, long>`。

```csharp
using System.Runtime.CompilerServices;

internal sealed class LongTimerHeap
{
    // _indices：timer 在 pool 中的下标。
    private int[] _indices;

    // _expireTicks：timer 对应的过期 tick。
    private long[] _expireTicks;

    // _count：当前堆元素数量。
    private int _count;

    public int Count => _count;

    public LongTimerHeap(
        int capacity // capacity：初始容量。
    )
    {
        _indices = new int[capacity];
        _expireTicks = new long[capacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(
        int timerIndex, // timerIndex：TimerEntry 在 pool 中的下标。
        long expireTick // expireTick：该 timer 的过期 tick。
    )
    {
        if (_count == _indices.Length)
            GrowSlow();

        var i = _count++;
        _indices[i] = timerIndex;
        _expireTicks[i] = expireTick;

        HeapifyUp(i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(
        out int timerIndex, // timerIndex：堆顶 timer 下标。
        out long expireTick // expireTick：堆顶 timer 过期 tick。
    )
    {
        if (_count == 0)
        {
            timerIndex = -1;
            expireTick = 0;
            return false;
        }

        timerIndex = _indices[0];
        expireTick = _expireTicks[0];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Dequeue()
    {
        var result = _indices[0];

        _count--;

        _indices[0] = _indices[_count];
        _expireTicks[0] = _expireTicks[_count];

        HeapifyDown(0);

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowSlow()
    {
        Array.Resize(ref _indices, _indices.Length * 2);
        Array.Resize(ref _expireTicks, _expireTicks.Length * 2);
    }

    private void HeapifyUp(
        int index // index：新插入元素的位置。
    )
    {
        while (index > 0)
        {
            var parent = (index - 1) >> 1;

            if (_expireTicks[index] >= _expireTicks[parent])
                break;

            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(
        int index // index：需要下沉的位置。
    )
    {
        while (true)
        {
            var left = (index << 1) + 1;
            if (left >= _count)
                break;

            var right = left + 1;
            var smallest = left;

            if (right < _count && _expireTicks[right] < _expireTicks[left])
                smallest = right;

            if (_expireTicks[index] <= _expireTicks[smallest])
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Swap(
        int a, // a：第一个堆位置。
        int b  // b：第二个堆位置。
    )
    {
        (_indices[a], _indices[b]) = (_indices[b], _indices[a]);
        (_expireTicks[a], _expireTicks[b]) = (_expireTicks[b], _expireTicks[a]);
    }
}
```

## 5.7 IntStack

用专用 `IntStack` 替代 `Stack<int>`。

```csharp
using System.Runtime.CompilerServices;

internal sealed class IntStack
{
    private int[] _items;
    private int _count;

    public int Count => _count;

    public IntStack(
        int capacity // capacity：初始容量。
    )
    {
        _items = new int[capacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(
        int value // value：要压入栈的 timer index。
    )
    {
        if (_count == _items.Length)
            GrowSlow();

        _items[_count++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Pop()
    {
        return _items[--_count];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowSlow()
    {
        Array.Resize(ref _items, _items.Length * 2);
    }

    public void Clear()
    {
        _count = 0;
    }
}
```

## 5.8 TimerFlags

把重复模式、catch-up 策略压成 flags，减少热路径 enum 判断。

```csharp
[Flags]
internal enum TimerFlags : byte
{
    None = 0,

    // Active：timer 当前是否有效。
    Active = 1 << 0,

    // Repeat：是否是重复 timer。
    Repeat = 1 << 1,

    // FixedRate：固定频率模式。
    // 下一次过期时间基于上一次计划时间计算。
    FixedRate = 1 << 2,

    // FixedDelay：固定延迟模式。
    // 下一次过期时间基于当前处理时间计算。
    FixedDelay = 1 << 3,

    // CatchUp：是否允许追赶错过的 tick。
    CatchUp = 1 << 4
}
```

`TimerEntry` 中可以替换：

```text
Active bool
RepeatMode enum
CatchUpPolicy enum
```

为：

```csharp
public TimerFlags Flags;
```

## 5.9 Schedule 快路径

```csharp
using System.Runtime.CompilerServices;

public TimerHandle Schedule(
    in TPayload payload,       // payload：timer 过期后交给 sink 的数据。
    float delaySeconds,        // delaySeconds：延迟秒数。
    int repeatCount = 0,       // repeatCount：重复次数；0 表示只执行一次。
    float intervalSeconds = 0  // intervalSeconds：重复 timer 的间隔秒数。
)
{
    if (_disposed)
        return TimerHandle.Invalid;

    if (_freeList.Count == 0)
        GrowPoolSlow();

    var index = _freeList.Pop();
    ref var entry = ref _pool[index];

    entry.Payload = payload;
    entry.Flags = repeatCount == 0
        ? TimerFlags.Active
        : TimerFlags.Active | TimerFlags.Repeat | _defaultRepeatFlags;

    entry.RemainingRepeatCount = repeatCount;
    entry.ExpireTick = _currentTick + SecondsToTicks(delaySeconds);
    entry.IntervalTicks = SecondsToTicks(intervalSeconds);

    PlaceEntry(index, ref entry);

    return new TimerHandle(index, entry.Version);
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private long SecondsToTicks(
    float seconds // seconds：秒数。
)
{
    // 用乘法替代除法。
    // Ceiling 用于保证不足一个 tick 的延迟进入下一个 tick。
    return (long)MathF.Ceiling(seconds * _tickDurationReciprocal);
}
```

## 5.10 ProcessCurrentSlot

参考 `EventCenter.Dispatch` 的思路：短函数、局部变量、慢路径下沉。

```csharp
using System.Runtime.CompilerServices;

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void ProcessCurrentSlot(
    IExpiredTimerSink<TPayload> sink // sink：过期 timer 的接收者。
)
{
    var slot = (int)(_currentTick & _wheelMask);

    var current = _wheel[slot];
    if (current == -1)
        return;

    _wheel[slot] = -1;

    ProcessSlotChain(current, sink);
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void ProcessSlotChain(
    int current,                     // current：当前 timer index。
    IExpiredTimerSink<TPayload> sink // sink：过期 timer 的接收者。
)
{
    var processed = 0;
    var limit = _maxExpiredPerTick;

    while (current != -1)
    {
        ref var entry = ref _pool[current];

        // next 必须先保存。
        // 因为当前 entry 可能会被 Release 或 Reschedule 改写。
        var next = entry.Next;

        if ((entry.Flags & TimerFlags.Active) != 0)
        {
            if (sink.TryAcceptExpired(in entry.Payload, new TimerHandle(current, entry.Version)))
            {
                processed++;

                if ((entry.Flags & TimerFlags.Repeat) == 0)
                    ReleaseTimer(current, ref entry);
                else
                    RescheduleRepeatSlow(current, ref entry);
            }
            else
            {
                ReleaseTimer(current, ref entry);
            }
        }

        if (limit > 0 && processed >= limit)
        {
            ReattachRemainingSlot(next);
            return;
        }

        current = next;
    }
}
```

## 5.11 Repeat 慢路径

```csharp
[MethodImpl(MethodImplOptions.NoInlining)]
private void RescheduleRepeatSlow(
    int index,                  // index：当前 timer 在 pool 中的下标。
    ref TimerEntry<TPayload> entry // entry：当前 timer entry 的引用。
)
{
    if (entry.RemainingRepeatCount > 0)
        entry.RemainingRepeatCount--;

    if (entry.RemainingRepeatCount == 0)
    {
        ReleaseTimer(index, ref entry);
        return;
    }

    long nextExpire;

    if ((entry.Flags & TimerFlags.FixedRate) != 0)
    {
        // FixedRate：基于上一次计划过期时间计算下一次。
        nextExpire = entry.ExpireTick + entry.IntervalTicks;
    }
    else
    {
        // FixedDelay：基于当前 tick 计算下一次。
        nextExpire = _currentTick + entry.IntervalTicks;
    }

    if (nextExpire <= _currentTick)
        nextExpire = _currentTick + entry.IntervalTicks;

    entry.ExpireTick = nextExpire;
    PlaceEntry(index, ref entry);
}
```

---

# 6. Layer 侧结论：不要 UpdateMask，只保留 HasDelayMask

## 6.1 不需要 UpdateMask

原因：

```text
IUpdate 已经可以在 Build 阶段被收集成独立调度组。
热路径只遍历 update group。
没有必要再遍历所有 layer 并查 mask。
```

推荐结构：

```text
Build:
    收集所有 IUpdate
    生成 IUpdate[] 或 UpdateEntry[]
Pump:
    直接遍历 update group
```

## 6.2 HasDelayMask 有价值

原因：

```text
DelayPublisher 不是每个 runtime 都有。
没有 DelayPublisher 时，不应每帧进入 DelayPublisherManager.Tick。
```

建议：

```csharp
using System.Runtime.CompilerServices;

internal readonly struct LayerRuntimeHotFlags
{
    // HasDelayMask：第 N 位表示 RouteIndex=N 的 Layer 是否有 DelayPublisher。
    // 当前 Layer 数量限制为 64，所以一个 ulong 足够。
    public readonly ulong HasDelayMask;

    public LayerRuntimeHotFlags(
        ulong hasDelayMask // hasDelayMask：Build 或 dirty rebuild 后生成的 Delay 能力位图。
    )
    {
        HasDelayMask = hasDelayMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAnyDelay()
    {
        return HasDelayMask != 0;
    }
}
```

`LayerRuntime.Pump` 中：

```csharp
public void Pump(
    float deltaTime // deltaTime：本帧经过时间。
)
{
    if (_disposed)
        return;

    _timer?.Tick(deltaTime, _timerSink!);

    // 只有存在 DelayPublisher 时才进入 Delay tick。
    if (_hotFlags.HasAnyDelay())
        DelayPublisherManager.Instance!.Tick(deltaTime);

    _context?.Update(_scheduler?.Options.MaxCompletionsPerPump ?? 0);

    _scheduler?.Pump();

    _updateGroup.Pump(deltaTime);
}
```

如果 `SubscribeDelay<T>()` 允许 Build 后动态创建 publisher，则应：

```text
SubscribeDelay<T>()
    修改 cold data
    标记 delay dirty

下一帧 Pump 开头或 Commit:
    重建 HasDelayMask
```

---

# 7. JIT 友好代码规则

## 7.1 快路径必须短

推荐：

```text
TryPost<T>
    disposed check
    typeId
    special mask
    EnqueueNormalFast
```

不推荐：

```text
TryPost<T>
    查 policy
    switch mode
    pending 判断
    dirty 判断
    latest 判断
    coalesced 判断
    queue full 处理
    merge failure 处理
```

## 7.2 慢路径必须 NoInlining

适合 NoInlining 的逻辑：

```text
Grow
QueueFull
PolicyOverride
DirtySignal
Latest
Coalesced
PendingLimit
MergeFailure
TimerCatchUp
RepeatReschedule
HeapGrow
StoreCreate
DisposedFailure
```

## 7.3 字段固化

构造或 Build 阶段固化字段：

```text
_defaultBackpressure
_wheelMask
_wheelSize
_tickDuration
_tickDurationReciprocal
_maxExpiredPerTick
_maxPromotePerTick
_timestampToMilliseconds
```

热路径不要反复读 options 对象。

## 7.4 避免热路径接口和字典

Post 中应避免：

```text
ConcurrentDictionary<int, IEventStore>
IEventStore.Dispatch
IEventStore.Release
policy table 查询
metadata 查询
```

Time 中应避免：

```text
PriorityQueue<TElement, TPriority>
IComparer<TPriority>
Stack<int>
```

## 7.5 代码形态参考 EventCenter.Dispatch

核心形态：

```text
先检查单订阅 / 小 fanout 特化
再查 mask
再遍历扁平数组
循环展开
异常处理下沉
```

Post 与 Time 可以借鉴：

```text
Post:
    special mask 分流
    NormalFastPath 极短
    SpecialPost NoInlining

Time:
    Tick 0/1 tick 快路径
    TickCatchUpSlow NoInlining
    ProcessCurrentSlot 短函数
    RepeatReschedule NoInlining
```

---

# 8. Unsafe 使用边界

## 8.1 什么时候可以用 Unsafe

只有当以下条件满足时，才考虑 Unsafe：

```text
数组来自 Build 阶段稳定快照
index 来源受控
count <= array.Length 已由 Build 保证
Debug 构建有 Assert
Benchmark 证明边界检查是瓶颈
```

## 8.2 FastArray helper

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class FastArray
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T At<T>(
        T[] array, // array：目标数组；调用方必须保证非 null。
        int index  // index：目标下标；调用方必须保证没有越界。
    )
    {
        // Debug.Assert：只在 Debug 构建生效。
        // Release 构建不会执行这些检查。
        Debug.Assert(array != null);
        Debug.Assert((uint)index < (uint)array.Length);

        // GetArrayDataReference：取得数组第一个元素的引用。
        // Unsafe.Add：从第一个元素引用向后偏移 index 个元素。
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    }
}
```

## 8.3 不建议过早 Unsafe 化的地方

```text
Coalesced Dictionary
动态 Grow
Dispose
Reset
Subscribe
Unsubscribe
PolicyOverride
Timer CatchUp
QueueFull
```

这些都不是普通热路径。

---

# 9. 分阶段落地计划

## 第一阶段：Post 快路径瘦身

目标：

```text
让普通 TryPost<T> 不查 policy、不 switch、不维护 pending。
```

任务：

```text
1. 拆分 TryPost<T>(in T value) 与 TryPostOverride<T>(...)
2. 引入 PostTypePlan[]
3. 引入 PostBitmap
4. MaxPending == 0 时完全跳过 pending counter
5. SpecialPost 下沉 NoInlining
6. QueueFull 下沉 NoInlining
7. Stopwatch 按需启用
```

## 第二阶段：Post 存储与队列

目标：

```text
减少字典、锁、取模。
```

任务：

```text
1. EventPayloadStorage 改为数组表或泛型静态缓存
2. RingBuffer 容量强制 2 的幂
3. % 替换为 & mask
4. DirtySignal 改 ulong bitmap
5. Latest pending 改 ulong bitmap
6. Coalesced 保留 Dictionary，但只在慢路径使用
```

## 第三阶段：Time 快路径

目标：

```text
让 TickOnce 足够短。
```

任务：

```text
1. WheelSize 强制 2 的幂
2. 生成 WheelMask
3. options 固化字段
4. Tick 拆成 0/1 tick 快路径 + catch-up 慢路径
5. PriorityQueue<int,long> 改 LongTimerHeap
6. Stack<int> 改 IntStack
7. Repeat / CatchUp 下沉慢路径
8. TimerFlags 替代多个 enum 热判断
```

## 第四阶段：Layer Delay

目标：

```text
没有 DelayPublisher 时，Pump 不进入 Delay 系统。
```

任务：

```text
1. 不引入 UpdateMask
2. 保留 Build 出来的 IUpdate 调度组
3. 引入 HasDelayMask 或 ActiveDelayCount
4. DelayPublisher 动态变化只标 dirty
5. 在 Pump 开头安全点重建 delay hot flags
```

## 第五阶段：Unsafe 精修

目标：

```text
只在 Benchmark 证明有收益的稳定数组路径上使用 Unsafe。
```

候选：

```text
PostBitmap mask 访问
PostPlan[] 访问
FastRingBuffer buffer 访问
Timer wheel 访问
Timer pool 访问
LongTimerHeap 数组访问
```

---

# 10. 基准测试建议

## 10.1 Post Benchmark

至少测：

```text
Normal Post，无订阅
Normal Post，单 Subscribe
Normal Post，多个 Subscribe
Normal Post，队列容量足够
Normal Post，队列满 RejectNew
Latest Post
DirtySignal Post
Coalesced Post
MaxPending 开启
MaxPending 关闭
```

核心指标：

```text
ns/op
alloc/op
branch miss，若工具支持
lock contention，若工具支持
```

## 10.2 Time Benchmark

至少测：

```text
Schedule once timer
Cancel timer
Tick 0 timer
Tick 100 active timer
Tick 1000 active timer
大量 once timer 同槽过期
大量 repeat timer
大量 long timer promote
WheelSize 256 / 512 / 1024
```

核心指标：

```text
TickOnce ns/op
ProcessCurrentSlot ns/timer
Schedule ns/op
Cancel ns/op
promote ns/timer
alloc/op
```

## 10.3 回归安全测试

必须测：

```text
Post 顺序保持
DropOldest 行为
DropNewest 行为
RejectNew 行为
Latest 只保留最后一次
DirtySignal 一帧只派发一次
Coalesced merge 成功
Coalesced merge 失败 fallback
Timer once 正确释放
Timer repeat count 正确
Timer fixed rate 正确
Timer fixed delay 正确
Cancel 后 handle 失效
Version 溢出回避 0
Reset / Dispose 后无残留
```

---

# 最终结论

Post 的核心优化：

```text
NormalFastPath 极短化。
用 PostBitmap 替代热路径 policy switch。
用 PostPlan 替代热路径 policy table 查询。
MaxPending == 0 时完全不维护 pending counter。
PayloadStore 从字典查找改为数组或泛型静态缓存。
RingBuffer 改 2 的幂容量和 mask。
Dirty / Latest 用 bitmap 管 pending。
```

Time 的核心优化：

```text
TickOnce 极短化。
WheelSize 强制 2 的幂。
用 & mask 替代 %。
options 固化字段。
PriorityQueue<int,long> 改专用 LongTimerHeap。
Stack<int> 改专用 IntStack。
一次性 timer 快路径与 repeat/catch-up 慢路径分离。
```

Layer 的核心结论：

```text
不要 UpdateMask。
IUpdate 应该 Build 成独立调度组。
只保留 HasDelayMask 或 ActiveDelayCount，用于跳过无意义 Delay Tick。
```

总原则：

```text
先 Build-Time BitPlan。
再数组快照。
再慢路径下沉。
最后才 Unsafe。
```
