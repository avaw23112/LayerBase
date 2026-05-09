# ActorWorld Allocation Elimination Design

> 文件名：`actorworld-allocation-elimination-design.md`  
> 适用仓库：`avaw23112/LayerBase`  
> 目标：彻底解决 `ActorWorld Post + Pump` benchmark 中的约 `91KB` 分配，并缩小 ActorWorld 与 `PostScheduler` / `Dictionary + interface call` 的性能差距。  
> 执行对象：Codex / 自动化代码代理 / 开发者。  
> 范围：只处理 ActorWorld 邮箱与 Pump 路径的运行时分配，不引入 AttributeSystem、UIBind、Network、Save 等新模块。

---

## 1. 背景

当前 benchmark 中，`ActorWorld Post + Pump - 20万次` 存在约 `91,200 B` 分配。

对比结果：

```text
ActorWorld Post + Pump - 20万次                 约 8,812 μs    Allocated 91,200 B
LayerBase PostScheduler - 20万次               约 6,588 μs    Allocated 0 B
Dictionary<ActorId, Actor> + interface call     约 1,578 μs    Allocated 0 B
Direct method call - 20万次                     约 56 μs       Allocated 0 B
```

ActorWorld 比 Dictionary 直调慢是正常的，因为 ActorWorld 提供了：

```text
ActorId / Generation 安全
ActorSlotState 检查
Actor 邮箱
DirtySlot 去重
分帧 Pump
Actor / Bucket 限流
帧预算
生命周期
PendingDestroy / Destroying 检查
Enable / Disable 策略
Query / Debug / Pool 基础
```

但是 `91KB` 分配不是必须成本，应该消除。

---

## 2. 术语解释

### 2.1 ActorWorld

`ActorWorld` 是 LayerBase 的 Actor 运行时容器。

它负责：

```text
创建 Actor
销毁 Actor
管理 ActorId
管理 Actor 邮箱
执行 Actor 生命周期
执行 Actor 邮件 Pump
管理 Query / Tag / Group / Debug / Pool
```

---

### 2.2 Post + Pump

`Post` 表示把事件投递到 Actor 邮箱。

`Pump` 表示在运行时循环中从邮箱取出事件并执行 ActorBehaviour。

链路大致是：

```text
ActorWorld.Post
  -> TypedActorStorage.Post
  -> EventColumn.Post
  -> EventMailWriter.Enqueue
  -> DirtySlotList.AddIfNotExists

ActorWorld.Pump
  -> ActorEventBucket.PumpOne
  -> EventColumn.PumpOne
  -> EventMailReader.TryDequeue
  -> ActorBehaviourInvoker
  -> EventMailReader.ReleaseIfEmpty
```

---

### 2.3 热路径

热路径是每帧频繁执行、性能敏感的代码。

本次涉及的热路径包括：

```text
ActorWorld.Post
ActorWorld.Pump
EventColumn.Post
EventColumn.PumpOne
EventMailWriter.Enqueue
EventMailReader.TryDequeue
DirtySlotList.AddIfNotExists
```

热路径目标：

```text
预热后 0B GC Alloc
不使用反射
不创建临时集合
不创建临时字符串
不创建临时异常对象
```

---

### 2.4 冷路径

冷路径是低频执行的代码。

例如：

```text
Actor 创建
Actor 销毁
EventColumn 创建
Storage 扩容
Debug Dump
Benchmark Setup
```

冷路径允许少量分配，但不能污染 steady-state benchmark。

**steady-state** 指系统预热后进入稳定运行状态。  
例如第一次创建 Actor / 第一次建立邮箱可以分配，但后续连续 Post + Pump 不应该继续分配。

---

## 3. 已确认的分配来源

### 3.1 ActorMailPumpStatsBuilder 每次 Pump 分配

当前 `ActorMailPumpStatsBuilder` 是 class，内部包含：

```csharp
private readonly Dictionary<long, int> _actorProcessedCounts = new();
private readonly Dictionary<int, int> _bucketProcessedCounts = new();
```

问题：

```text
如果每次 ActorWorld.Pump 都 new ActorMailPumpStatsBuilder，
则每次 Pump 至少分配：
1. ActorMailPumpStatsBuilder 对象
2. Dictionary<long, int> 对象
3. Dictionary<int, int> 对象
4. Dictionary 内部 buckets / entries
```

这是 91KB 的第一优先级嫌疑。

---

### 3.2 DirtySlotList 使用 HashSet<int>

当前 `DirtySlotList` 使用：

```csharp
private readonly HashSet<int> _contains = new();
```

问题：

```text
slotIndex 是连续整数下标。
HashSet<int> 做 dirty slot 去重过重。
HashSet 首次 Add 多个 slot 时会分配 buckets / entries。
```

应改为数组标记。

---

### 3.3 RingQueueBuffer.Rent 每次 new TEvent[]

当前 `RingQueueBuffer<TEvent>.Rent` 中每次都会：

```csharp
TEvent[] buffer = new TEvent[Math.Max(initialCapacity, 1)];
```

即使存在 `_freeIds`，仍然会先创建新数组。

问题：

```text
这不是数组池，只是 bufferId 池。
Release 后丢掉数组，下一次 Rent 仍然 new。
```

---

### 3.4 ActorMailOptions.Default 默认 releaseWhenEmpty = true

当前默认选项：

```csharp
releaseWhenEmpty: true
```

问题：

```text
Post 时 Rent buffer。
Pump 处理完后邮箱为空。
ReleaseIfEmpty 释放 buffer。
下一次 Post 又重新 Rent / new buffer。
```

对高频 Actor 邮箱来说，这个默认值会制造反复分配。

---

## 4. 总体改造目标

完成后应满足：

```text
1. ActorWorld Post + Pump 预热后 Allocated = 0 B。
2. ActorWorld.Pump 不再 new ActorMailPumpStatsBuilder。
3. ActorMailPumpStatsBuilder 内部 Dictionary 不再每帧重新分配。
4. DirtySlotList 不再使用 HashSet<int>。
5. RingQueueBuffer.Release 不再丢弃数组。
6. ActorMailOptions.Default 默认保留空邮箱 buffer。
7. 保留 MemorySaving 选项，允许低频 Actor 释放空邮箱 buffer。
8. 所有现有 Actor 邮箱语义保持不变。
```

---

## 5. 修改文件清单

必须修改：

```text
LayerBase/Actor/Pump/ActorMailPumpStatsBuilder.cs
LayerBase/Actor/Storage/ActorWorld.Pump.cs
LayerBase/Actor/Mail/DirtySlotList.cs
LayerBase/Actor/Mail/EventColumn.cs
LayerBase/Actor/Mail/RingQueueBuffer.cs
LayerBase/Actor/Mail/ActorMailOptions.cs
```

建议新增或修改测试：

```text
LayerBase.Test/Actor/ActorWorldAllocationTests.cs
LayerBase.Test/Actor/DirtySlotListTests.cs
LayerBase.Test/Actor/RingQueueBufferTests.cs
```

如果 benchmark 项目在仓库中：

```text
LayerBase.Benchmarks/ActorWorldBenchmarks.cs
```

如果 benchmark 不在仓库中，则只补单元测试。

---

## 6. 任务一：复用 ActorMailPumpStatsBuilder

### 6.1 修改目标

`ActorMailPumpStatsBuilder` 不应每次 Pump 创建。

它应该成为 `ActorWorld` 的字段，并在每次 Pump 前调用 `Reset()`。

---

### 6.2 修改 ActorMailPumpStatsBuilder

目标文件：

```text
LayerBase/Actor/Pump/ActorMailPumpStatsBuilder.cs
```

替换为以下设计。

```csharp
namespace LayerBase.Actor;

internal sealed class ActorMailPumpStatsBuilder
{
    private readonly Dictionary<long, int> _actorProcessedCounts = new();
    private readonly Dictionary<int, int> _bucketProcessedCounts = new();

    public int ProcessedTotal;
    public int BucketLimitHits;
    public int ActorLimitHits;
    public int EmptyBucketChecks;

    public void Reset()
    {
        // 作用说明：
        // Reset 会在每次 ActorWorld.Pump 开始前调用。
        // 它清空上一帧统计结果，但保留 Dictionary 已经分配过的内部容量。
        // 这样可以避免每帧重新 new Dictionary 或重新分配 buckets / entries。

        _actorProcessedCounts.Clear();
        _bucketProcessedCounts.Clear();

        ProcessedTotal = 0;
        BucketLimitHits = 0;
        ActorLimitHits = 0;
        EmptyBucketChecks = 0;
    }

    public bool CanProcessBucket(int bucketIndex, in ActorMailPumpOptions options)
    {
        // bucketIndex 参数：
        // 当前正在 Pump 的事件 bucket 下标。
        //
        // options 参数：
        // Actor 邮箱 Pump 配置。
        // MaxMailsPerBucketPerPump <= 0 表示不限制单个 bucket 每帧处理数量。
        //
        // 作用说明：
        // 不启用 bucket 限制时，不访问 Dictionary。
        // 这样可以避免无意义的哈希查询成本。

        if (options.MaxMailsPerBucketPerPump <= 0)
        {
            return true;
        }

        return !_bucketProcessedCounts.TryGetValue(bucketIndex, out int count)
               || count < options.MaxMailsPerBucketPerPump;
    }

    public void RecordBucketProcessed(int bucketIndex)
    {
        // bucketIndex 参数：
        // 当前处理成功的事件 bucket 下标。
        //
        // 作用说明：
        // 记录当前 bucket 本轮 Pump 已处理的邮件数量。
        // 该统计用于 MaxMailsPerBucketPerPump 限制。

        if (_bucketProcessedCounts.TryGetValue(bucketIndex, out int count))
        {
            _bucketProcessedCounts[bucketIndex] = count + 1;
        }
        else
        {
            _bucketProcessedCounts[bucketIndex] = 1;
        }
    }

    public bool CanProcessActor(long actorKey, in ActorMailPumpOptions options)
    {
        // actorKey 参数：
        // Actor Pump 统计 key。
        // 当前实现通常由 archetypeId 和 slotIndex 组合得到。
        //
        // options 参数：
        // Actor 邮箱 Pump 配置。
        // MaxMailsPerActorPerPump <= 0 表示不限制单个 Actor 每帧处理数量。
        //
        // 作用说明：
        // 不启用 Actor 限制时，不访问 Dictionary。
        // 这样可以避免无意义的哈希查询成本。

        if (options.MaxMailsPerActorPerPump <= 0)
        {
            return true;
        }

        return !_actorProcessedCounts.TryGetValue(actorKey, out int count)
               || count < options.MaxMailsPerActorPerPump;
    }

    public void RecordActorProcessed(long actorKey)
    {
        // actorKey 参数：
        // 当前处理成功的 Actor Pump key。
        //
        // 作用说明：
        // 记录当前 Actor 本轮 Pump 已处理的邮件数量。
        // 该统计用于 MaxMailsPerActorPerPump 限制。

        if (_actorProcessedCounts.TryGetValue(actorKey, out int count))
        {
            _actorProcessedCounts[actorKey] = count + 1;
        }
        else
        {
            _actorProcessedCounts[actorKey] = 1;
        }
    }

    public ActorMailPumpStats Build(int remainingDirtyBuckets)
    {
        // remainingDirtyBuckets 参数：
        // 本次 Pump 结束后仍然存在待处理工作的 bucket 数量。
        //
        // 作用说明：
        // Build 只创建 readonly struct，不产生托管堆分配。
        return new ActorMailPumpStats(
            ProcessedTotal,
            BucketLimitHits,
            ActorLimitHits,
            EmptyBucketChecks,
            remainingDirtyBuckets);
    }
}
```

---

### 6.3 修改 ActorWorld.Pump

目标文件：

```text
LayerBase/Actor/Storage/ActorWorld.Pump.cs
```

在 `ActorWorld` 中添加字段：

```csharp
private readonly ActorMailPumpStatsBuilder _mailPumpStatsBuilder = new();
```

在 Pump 邮箱时改为：

```csharp
private ActorMailPumpStats PumpActorBehaviours(
    ref RuntimeFrameBudget budget,
    in ActorMailPumpOptions options)
{
    // budget 参数：
    // 当前 Runtime 帧预算。
    // 用于限制本帧最多处理多少 Actor 邮件，以及是否超过时间预算。
    //
    // options 参数：
    // Actor 邮箱 Pump 策略。
    // 用于限制单次 Pump 的总邮件数、单 bucket 邮件数、单 Actor 邮件数等。
    //
    // 作用说明：
    // ActorWorld 是 owner-thread 模型。
    // 同一个 ActorWorld 不应该被多个线程同时 Pump。
    // 因此 StatsBuilder 可以作为 ActorWorld 字段复用。

    ActorMailPumpStatsBuilder stats = _mailPumpStatsBuilder;
    stats.Reset();

    // 保留现有 Pump 主循环逻辑。
    // 只把原先的 new ActorMailPumpStatsBuilder() 改为复用 stats。
}
```

要求：

```text
不得改变现有 Pump 行为。
只改变 StatsBuilder 生命周期。
```

---

## 7. 任务二：DirtySlotList 移除 HashSet<int>

### 7.1 修改目标

将：

```csharp
private readonly HashSet<int> _contains = new();
```

替换为：

```csharp
private bool[] _contains;
```

理由：

```text
slotIndex 是连续整数下标。
数组访问比 HashSet 更快。
数组标记不会产生 HashSet buckets / entries 分配。
```

---

### 7.2 替换 DirtySlotList

目标文件：

```text
LayerBase/Actor/Mail/DirtySlotList.cs
```

建议替换为：

```csharp
namespace LayerBase.Actor;

internal sealed class DirtySlotList
{
    private int[] _items;
    private bool[] _contains;
    private int _head;
    private int _count;

    public int Count => _count;

    public DirtySlotList(int initialCapacity = 4)
    {
        // initialCapacity 参数：
        // 初始 dirty slot 队列容量和 contains 标记容量。
        //
        // 作用说明：
        // EventColumn 创建时可以传入 initialSlotCapacity。
        // 这样 DirtySlotList 可以提前准备容量，减少运行期扩容。

        int capacity = Math.Max(initialCapacity, 4);
        _items = new int[capacity];
        _contains = new bool[capacity];
    }

    public void AddIfNotExists(int slotIndex)
    {
        // slotIndex 参数：
        // 当前有待处理邮件的 Actor slot 下标。
        //
        // 作用说明：
        // 同一个 slot 在邮件未清空之前只允许进入 dirty list 一次。
        // 否则 Pump 会重复扫描同一个 Actor。

        EnsureContainsCapacity(slotIndex + 1);

        if (_contains[slotIndex])
        {
            return;
        }

        _contains[slotIndex] = true;

        EnsureItemCapacity(_count + 1);

        int tail = (_head + _count) % _items.Length;
        _items[tail] = slotIndex;
        _count++;
    }

    public bool TryPeek(out int slotIndex)
    {
        // slotIndex 参数：
        // 输出当前队首 dirty slot。
        //
        // 作用说明：
        // TryPeek 只查看，不移除。
        // 真正移除由 Pop 完成。

        if (_count == 0)
        {
            slotIndex = default;
            return false;
        }

        slotIndex = _items[_head];
        return true;
    }

    public void Pop()
    {
        // 作用说明：
        // 移除队首 dirty slot。
        // 同时清除 _contains 标记，允许该 slot 后续再次进入 dirty list。

        if (_count == 0)
        {
            return;
        }

        int slotIndex = _items[_head];

        if ((uint)slotIndex < (uint)_contains.Length)
        {
            _contains[slotIndex] = false;
        }

        _head = (_head + 1) % _items.Length;
        _count--;

        if (_count == 0)
        {
            _head = 0;
        }
    }

    public void MoveHeadToTail()
    {
        // 作用说明：
        // 当前队首 slot 暂时不能继续处理时，将它移动到队尾。
        // 典型场景是 Actor 单帧处理数量达到限制。
        //
        // 注意：
        // 这里不能清除 _contains，因为该 slot 仍然在 dirty list 内。

        if (_count <= 1)
        {
            return;
        }

        int headValue = _items[_head];
        _head = (_head + 1) % _items.Length;

        int tail = (_head + _count - 1) % _items.Length;
        _items[tail] = headValue;
    }

    private void EnsureItemCapacity(int required)
    {
        // required 参数：
        // dirty queue 需要容纳的元素数量。
        //
        // 作用说明：
        // 只在 dirty slot 数超过当前数组容量时扩容。
        // 扩容后重新整理环形队列为从 0 开始的连续布局。

        if (required <= _items.Length)
        {
            return;
        }

        int newCapacity = _items.Length;

        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        int[] newItems = new int[newCapacity];

        for (int i = 0; i < _count; i++)
        {
            newItems[i] = _items[(_head + i) % _items.Length];
        }

        _items = newItems;
        _head = 0;
    }

    private void EnsureContainsCapacity(int required)
    {
        // required 参数：
        // contains 标记数组至少需要支持的 slot 数量。
        //
        // 作用说明：
        // slotIndex 是数组下标，所以 contains 数组需要覆盖最大 slotIndex。
        // Array.Resize 会保留旧标记，扩容后新增区域默认为 false。

        if (required <= _contains.Length)
        {
            return;
        }

        int newCapacity = _contains.Length;

        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        Array.Resize(ref _contains, newCapacity);
    }
}
```

---

### 7.3 修改 EventColumn 构造

目标文件：

```text
LayerBase/Actor/Mail/EventColumn.cs
```

把：

```csharp
_dirtySlots = new DirtySlotList();
```

改成：

```csharp
_dirtySlots = new DirtySlotList(initialSlotCapacity);
```

说明：

```text
initialSlotCapacity 参数来自 EventColumn 构造函数。
它通常和 storage 初始 slot 容量一致。
这样 DirtySlotList 可以提前持有合适容量。
```

---

## 8. 任务三：ActorMailOptions 默认不释放空邮箱

目标文件：

```text
LayerBase/Actor/Mail/ActorMailOptions.cs
```

将默认值：

```csharp
releaseWhenEmpty: true
```

改为：

```csharp
releaseWhenEmpty: false
```

完整推荐：

```csharp
public static ActorMailOptions Default => new(
    postPolicy: ActorPostPolicy.Queued,
    fullPolicy: ActorMailFullPolicy.Grow,
    growFailurePolicy: ActorMailFullPolicy.RejectNew,
    initialCapacity: 4,
    maxCapacity: 64,
    growFactor: 2,

    // 作用说明：
    // false 表示邮箱空了也保留已经租到的 ring buffer。
    // 高频 Actor 下一次收到同类型事件时可以复用 buffer。
    // 这能避免 Post / Pump 高频循环中反复 new TEvent[]。
    releaseWhenEmpty: false,

    disabledPolicy: ActorMailDisabledPolicy.Accept,
    pendingDestroyPolicy: ActorMailPendingDestroyPolicy.Reject);
```

新增省内存配置：

```csharp
public static ActorMailOptions MemorySaving => new(
    postPolicy: ActorPostPolicy.Queued,
    fullPolicy: ActorMailFullPolicy.Grow,
    growFailurePolicy: ActorMailFullPolicy.RejectNew,
    initialCapacity: 4,
    maxCapacity: 64,
    growFactor: 2,

    // 作用说明：
    // true 表示邮箱空后释放 buffer。
    // 适合低频 Actor 或临时 Actor。
    // 不适合高频 Post + Pump 路径。
    releaseWhenEmpty: true,

    disabledPolicy: ActorMailDisabledPolicy.Accept,
    pendingDestroyPolicy: ActorMailPendingDestroyPolicy.Reject);
```

---

## 9. 任务四：RingQueueBuffer 改为真正复用数组

目标文件：

```text
LayerBase/Actor/Mail/RingQueueBuffer.cs
```

当前问题：

```text
Release 只复用 id，不复用 TEvent[]。
Rent 每次都会 new TEvent[]。
```

推荐替换为：

```csharp
namespace LayerBase.Actor;

internal sealed class RingQueueBuffer<TEvent>
    where TEvent : struct
{
    private TEvent[]?[] _buffers = new TEvent[4][];
    private bool[] _inUse = new bool[4];
    private readonly Stack<int> _freeIds = new();

    public int Rent(int initialCapacity)
    {
        // initialCapacity 参数：
        // 当前邮箱需要的初始容量。
        // 至少为 1。
        //
        // 作用说明：
        // 优先复用 Release 后留下的 buffer。
        // 只有没有 buffer 或 buffer 容量不足时才 new。

        int capacity = Math.Max(initialCapacity, 1);

        if (_freeIds.Count > 0)
        {
            int reusedId = _freeIds.Pop();
            int index = reusedId - 1;

            TEvent[]? buffer = _buffers[index];

            if (buffer == null || buffer.Length < capacity)
            {
                buffer = new TEvent[capacity];
                _buffers[index] = buffer;
            }

            _inUse[index] = true;
            return reusedId;
        }

        int id = 1;

        while (id <= _buffers.Length && _buffers[id - 1] != null)
        {
            id++;
        }

        if (id > _buffers.Length)
        {
            int oldLength = _buffers.Length;
            Array.Resize(ref _buffers, oldLength * 2);
            Array.Resize(ref _inUse, oldLength * 2);
        }

        _buffers[id - 1] = new TEvent[capacity];
        _inUse[id - 1] = true;
        return id;
    }

    public int GetCapacity(int bufferId)
    {
        // bufferId 参数：
        // RingQueueBuffer.Rent 返回的 buffer 编号。
        //
        // 返回：
        // 对应数组容量。

        return GetBuffer(bufferId).Length;
    }

    public void Write(int bufferId, int index, in TEvent value)
    {
        // bufferId 参数：
        // 目标 buffer 编号。
        //
        // index 参数：
        // 写入位置。
        //
        // value 参数：
        // 要写入的事件值。
        //
        // 作用说明：
        // RingQueueBuffer 只负责存储值。
        // 环形队列的 head / count 由 EventMail<TEvent> 保存。

        GetBuffer(bufferId)[index] = value;
    }

    public TEvent Read(int bufferId, int index)
    {
        // bufferId 参数：
        // 目标 buffer 编号。
        //
        // index 参数：
        // 读取位置。
        //
        // 返回：
        // 对应位置的事件值。

        return GetBuffer(bufferId)[index];
    }

    public void Release(int bufferId)
    {
        // bufferId 参数：
        // 要释放的 buffer 编号。
        //
        // 作用说明：
        // Release 不再把 _buffers[index] 置空。
        // 它只把 buffer 标记为未使用，并把 id 放回 free list。
        // 下次 Rent 可以复用同一个数组，避免重新分配。

        if (bufferId <= 0 || bufferId > _buffers.Length)
        {
            return;
        }

        int index = bufferId - 1;

        if (!_inUse[index])
        {
            return;
        }

        _inUse[index] = false;
        _freeIds.Push(bufferId);
    }

    public void Resize(int bufferId, int head, int count, int newCapacity)
    {
        // bufferId 参数：
        // 要扩容的 buffer 编号。
        //
        // head 参数：
        // 旧环形队列的队首位置。
        //
        // count 参数：
        // 旧环形队列中的有效元素数量。
        //
        // newCapacity 参数：
        // 新 buffer 容量。
        //
        // 作用说明：
        // 扩容时把旧环形数据拷贝到新数组的连续区间 [0, count)。
        // 调用方会在扩容后把 mail.Head 重置为 0。

        if (newCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newCapacity));
        }

        TEvent[] oldBuffer = GetBuffer(bufferId);
        TEvent[] newBuffer = new TEvent[newCapacity];

        for (int i = 0; i < count; i++)
        {
            newBuffer[i] = oldBuffer[(head + i) % oldBuffer.Length];
        }

        _buffers[bufferId - 1] = newBuffer;
    }

    private TEvent[] GetBuffer(int bufferId)
    {
        // bufferId 参数：
        // 要访问的 buffer 编号。
        //
        // 作用说明：
        // bufferId 从 1 开始。
        // 内部数组下标从 0 开始，因此访问 _buffers[bufferId - 1]。

        if (bufferId <= 0 || bufferId > _buffers.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferId));
        }

        return _buffers[bufferId - 1]
               ?? throw new InvalidOperationException("Buffer is not allocated.");
    }
}
```

注意：

```text
如果 TEvent 内部持有引用字段，复用数组可能延长引用存活时间。
但 Actor 事件应尽量使用纯值类型字段。
如需严格释放引用，后续可增加 ClearOnRelease 策略。
```

---

## 10. 任务五：可选优化 Bucket 统计

当前 `ActorMailPumpStatsBuilder` 中：

```csharp
Dictionary<int, int> _bucketProcessedCounts
```

可以后续替换成数组，因为 bucketIndex 是连续整数。

第一阶段可以不做，先修 91KB。

如果需要继续优化：

```text
Dictionary<int, int> -> int[] bucketProcessedCounts
```

但这不是本轮必须项。

---

## 11. 新增测试

### 11.1 ActorWorld Post/Pump 预热后 0B 分配测试

新增文件：

```text
LayerBase.Test/Actor/ActorWorldAllocationTests.cs
```

示例：

```csharp
using LayerBase;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test.Actor;

public sealed class ActorWorldAllocationTests
{
    [Test]
    public void ActorWorld_PostPump_Should_Not_Allocate_After_Warmup()
    {
        // iterations 参数：
        // 重复执行 Post + Pump 的次数。
        // 用 200_000 对齐 benchmark。
        const int iterations = 200_000;

        LayerHub.Reset();

        LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new AllocationTestLayer())
            .Build();

        AllocationTestActor actor = runtime.Actors.CreateActor<AllocationTestActor>();
        ActorId actorId = actor.Context.ActorId;

        // 作用说明：
        // 第一次 Post + Pump 允许初始化邮箱 buffer、dirty list、stats builder 内部容量。
        // 真正测量从预热之后开始。
        runtime.Actors.Post(actorId, new AllocationTestEvent());
        runtime.Pump(0.016f);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < iterations; i++)
        {
            runtime.Actors.Post(actorId, new AllocationTestEvent());
            runtime.Pump(0.016f);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        long allocated = after - before;

        Assert.That(allocated, Is.EqualTo(0));
    }

    private sealed class AllocationTestLayer : Layer
    {
    }

    private readonly struct AllocationTestEvent
    {
    }

    private sealed partial class AllocationTestActor : IActor
    {
        public ActorContext Context { get; private set; }

        public void ActorInit(in ActorContext context)
        {
            // context 参数：
            // ActorWorld 注入的运行时上下文。
            // 包含 ActorId、ActorWorld 等信息。
            //
            // 作用说明：
            // 测试需要通过 Context.ActorId 获取当前 Actor 的身份。
            Context = context;
        }

        [ActorBehaviour]
        private void OnEvent(in AllocationTestEvent e)
        {
            // e 参数：
            // 测试事件。
            //
            // 作用说明：
            // 这个 handler 故意保持空逻辑。
            // 测试目标是测量 ActorWorld Post + Pump 的框架分配。
        }
    }
}
```

如果当前项目的 `ActorContext` 注入不是手写 `ActorInit`，请按现有生成器语义调整测试代码。

---

### 11.2 DirtySlotList 去重测试

```csharp
using NUnit.Framework;

namespace LayerBase.Test.Actor;

public sealed class DirtySlotListTests
{
    [Test]
    public void AddIfNotExists_Should_Not_Add_Duplicate_Slot()
    {
        var list = new DirtySlotList(initialCapacity: 4);

        list.AddIfNotExists(2);
        list.AddIfNotExists(2);

        Assert.That(list.Count, Is.EqualTo(1));

        Assert.That(list.TryPeek(out int slotIndex), Is.True);
        Assert.That(slotIndex, Is.EqualTo(2));
    }

    [Test]
    public void Pop_Should_Allow_Slot_To_Be_Added_Again()
    {
        var list = new DirtySlotList(initialCapacity: 4);

        list.AddIfNotExists(2);
        list.Pop();
        list.AddIfNotExists(2);

        Assert.That(list.Count, Is.EqualTo(1));
    }

    [Test]
    public void MoveHeadToTail_Should_Keep_Contains_Mark()
    {
        var list = new DirtySlotList(initialCapacity: 4);

        list.AddIfNotExists(1);
        list.AddIfNotExists(2);

        list.MoveHeadToTail();

        list.AddIfNotExists(1);

        Assert.That(list.Count, Is.EqualTo(2));
    }
}
```

如果 `DirtySlotList` 是 internal 且测试项目不能访问，请在主项目中添加：

```csharp
[assembly: InternalsVisibleTo("LayerBase.Test")]
```

或把测试放到现有 internal 可见测试配置下。

---

### 11.3 RingQueueBuffer 复用测试

```csharp
using NUnit.Framework;

namespace LayerBase.Test.Actor;

public sealed class RingQueueBufferTests
{
    [Test]
    public void Rent_After_Release_Should_Reuse_Buffer_Id()
    {
        var buffer = new RingQueueBuffer<TestEvent>();

        int firstId = buffer.Rent(initialCapacity: 4);
        buffer.Release(firstId);

        int secondId = buffer.Rent(initialCapacity: 4);

        Assert.That(secondId, Is.EqualTo(firstId));
    }

    [Test]
    public void Rent_After_Release_Should_Preserve_Capacity_When_Enough()
    {
        var buffer = new RingQueueBuffer<TestEvent>();

        int firstId = buffer.Rent(initialCapacity: 8);
        int firstCapacity = buffer.GetCapacity(firstId);

        buffer.Release(firstId);

        int secondId = buffer.Rent(initialCapacity: 4);
        int secondCapacity = buffer.GetCapacity(secondId);

        Assert.That(secondId, Is.EqualTo(firstId));
        Assert.That(secondCapacity, Is.EqualTo(firstCapacity));
    }

    private readonly struct TestEvent
    {
    }
}
```

---

## 12. Benchmark 验证

修改后重新运行：

```bash
dotnet run -c Release --project LayerBase.Benchmarks --filter "*Actor*"
```

如果 benchmark 项目名称不同，请按仓库实际名称调整。

期望：

```text
ActorWorld Post + Pump - 20万次
Allocated: 0 B
```

性能期望：

```text
ActorWorld Post + Pump 总耗时应下降。
至少不应比修改前更慢。
如果 releaseWhenEmpty 改 false，重复 Post/Pump 场景应明显减少分配和部分耗时。
```

---

## 13. DoD

完成标准：

```text
1. ActorWorld Post + Pump 预热后单元测试 Allocated = 0 B。
2. BenchmarkDotNet 中 ActorWorld Post + Pump 的 Allocated = 0 B。
3. DirtySlotList 不再引用 HashSet<int>。
4. RingQueueBuffer.Rent 不再每次都 new TEvent[]。
5. ActorMailPumpStatsBuilder 不再每次 Pump 创建。
6. ActorMailOptions.Default.releaseWhenEmpty = false。
7. ActorMailOptions.MemorySaving.releaseWhenEmpty = true。
8. 所有现有测试通过。
9. 新增 DirtySlotList / RingQueueBuffer / Allocation 测试通过。
10. 不修改 ActorWorld 对外 API，除非已有 API 需要增加 MemorySaving 选项文档。
```

---

## 14. 禁止事项

本任务中不要做：

```text
1. 不引入 AttributeSystem。
2. 不引入 UIBind。
3. 不引入网络同步。
4. 不引入存档系统。
5. 不重构整个 ActorWorld 架构。
6. 不删除现有 Actor 邮箱策略。
7. 不把 ActorWorld 改成 Dictionary 直调模型。
8. 不为了 benchmark 跳过 PendingDestroy / Disabled / Alive 检查。
```

---

## 15. 最终结论

本轮优化目标不是把 ActorWorld 做成最薄调用路径。

目标是：

```text
保留 ActorWorld 的邮箱、帧预算、生命周期、安全检查和 Debug 能力，
同时让稳定运行态 Post + Pump 不产生 GC 分配。
```

预期完成后，ActorWorld 的定位应为：

```text
带邮箱和生命周期的高性能 Actor Runtime。
预热后 Post + Pump 0B GC Alloc。
```
