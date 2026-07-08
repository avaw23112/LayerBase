# LayerBase ECS 双向 SPSC 优化设计：Main -> EcsWorker / EcsWorker -> Main

## 0. 正确线程模型

当前异步 ECS 流程应定义为：

```text id="tosxaq"
Main Thread
  负责业务编排、Service、Context、ActorWorld、ActorBehaviour。

EcsWorker Thread
  独占 EcsWorld。
  负责执行 ECS Query / Bring Query / Projection Query。
```

两条通信通道：

```text id="1xwuay"
Main -> EcsWorker:
  EcsWorkItem / EcsWorkRecord

EcsWorker -> Main:
  ActorProjectItem / ActorEventCommandBuffer / EcsResultItem
```

因此不是 MPSC，而是：

```text id="20157a"
SPSC A:
  Main producer -> EcsWorker consumer

SPSC B:
  EcsWorker producer -> Main consumer
```

这意味着可以彻底放弃热路径上的 `ConcurrentQueue<T>`。

---

# 1. 为什么 SPSC 更适合？

MPSC 需要解决的问题是：

```text id="v3h2tb"
多个生产者同时写入队列
竞争 tail
CAS / Interlocked
内存序列发布
可能的队列节点分配
```

但你的主流程只有：

```text id="o47685"
一个生产者：Main Thread
一个消费者：EcsWorker
```

所以热路径只需要：

```text id="n31k12"
Producer 写 tail。
Consumer 写 head。
双方只读对方 index。
```

这可以避免：

```text id="tcillw"
Interlocked
CAS loop
ConcurrentQueue node
per item allocation
每次 Signal
```

---

# 2. 最终目标

你的目标应该变成：

```text id="z2g3i7"
Main -> EcsWorker 投递：
  不分配。
  不装箱。
  不 Interlocked。
  不 Signal 每个任务。
  只写预分配 RingBuffer 槽位。

EcsWorker -> Main 回流：
  不分配或少分配。
  ActorProjectItem 批量写入 RingBuffer。
  Main.Pump 批量 Drain。
```

理想性能层级：

```text id="2l8tam"
Raw SPSC enqueue:
  10ns - 50ns 级别。

RecordPlainQuery:
  30ns - 150ns，取决于 Job 复制和 QueryRecord 填充。

QueryFlow.ForEach Async:
  如果不分配、不 Signal，有机会进 100ns - 300ns 级别。

完整 SubmitOnly:
  当前 5–20μs 应该能大幅下降。
```

20ns 是否稳定，要看 CPU、缓存命中、Benchmark 写法和 record 大小，但 SPSC 是唯一接近这个目标的方向。

---

# 3. 两条 SPSC 通道

## 3.1 Main -> ECS：EcsWorkRing

```text id="u1q0sd"
Main Thread:
  Write EcsWorkRecord

EcsWorker:
  Read EcsWorkRecord and execute ECS work
```

用途：

```text id="337q6t"
PlainQuery
BringQuery
SweepProjectedActors
StructuralCommand
```

---

## 3.2 ECS -> Main：EcsResultRing

```text id="mqvqtz"
EcsWorker:
  Write EcsResultRecord / ActorProjectItem

Main Thread:
  Drain records during Runtime.Pump
```

用途：

```text id="kfm1qv"
ActorEventCommand
ActorProjectTouch
ActorProjectEnsure
EcsWorkFailed
ProfilerStats
```

---

# 4. WorkItem 不要用 class

当前 `SubmitOnly` 每次分配约 432 B，说明热路径里还有对象分配。

目标是：

```text id="ved2gt"
不要 new PlainQueryWorkItem(...)
不要 interface IEcsWorkItem
不要 object jobBox
不要 lambda/delegate allocation
```

改成：

```csharp id="wlh0kq"
internal enum EcsWorkKind : byte
{
    PlainQuery,
    BringQuery,
    SweepProjectedActors
}

internal struct EcsWorkRecord
{
    public EcsWorkKind Kind;

    public int ExecutorId;
    public int QueryId;
    public int PredicateId;

    public int JobOffset;
    public int JobSize;

    public int DebugId;
}
```

`EcsWorkRecord` 是值类型，直接写入 ring buffer。

---

# 5. SPSC Ring Buffer 实现

## 5.1 Main -> EcsWorker 队列

```csharp id="il88cx"
internal sealed class SpscRing<T>
    where T : struct
{
    private readonly T[] _buffer;
    private readonly int _mask;

    private PaddedInt _head; // consumer writes
    private PaddedInt _tail; // producer writes

    public SpscRing(int capacityPowerOfTwo)
    {
        if ((capacityPowerOfTwo & (capacityPowerOfTwo - 1)) != 0)
            throw new ArgumentException("Capacity must be power of two.");

        _buffer = new T[capacityPowerOfTwo];
        _mask = capacityPowerOfTwo - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(in T item)
    {
        int tail = _tail.Value;
        int next = tail + 1;

        int head = Volatile.Read(ref _head.Value);

        if (next - head > _buffer.Length)
            return false;

        _buffer[tail & _mask] = item;

        Volatile.Write(ref _tail.Value, next);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out T item)
    {
        int head = _head.Value;

        int tail = Volatile.Read(ref _tail.Value);

        if (head == tail)
        {
            item = default;
            return false;
        }

        item = _buffer[head & _mask];

        Volatile.Write(ref _head.Value, head + 1);

        return true;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct PaddedInt
{
    [FieldOffset(0)]
    public int Value;
}
```

这个热路径没有 `Interlocked`，只有：

```text id="e3z27n"
读 head
写 buffer
发布 tail
```

---

# 6. Job 数据存储：EcsJobArena

如果 `EcsWorkRecord` 里放 `object JobBox`，就会装箱。

所以 Query Job 要复制到 arena：

```csharp id="9tmzgd"
internal sealed class EcsJobArena
{
    private byte[] _buffer;
    private int _offset;

    public EcsJobArena(int initialCapacity)
    {
        _buffer = new byte[initialCapacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Store<TJob>(in TJob job)
        where TJob : struct
    {
        int size = Unsafe.SizeOf<TJob>();
        int offset = Align(_offset, Math.Min(size, 16));

        EnsureCapacity(offset + size);

        Unsafe.WriteUnaligned(
            ref _buffer[offset],
            job);

        _offset = offset + size;
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TJob Get<TJob>(int offset)
        where TJob : struct
    {
        return ref Unsafe.As<byte, TJob>(ref _buffer[offset]);
    }

    public void Reset()
    {
        _offset = 0;
    }

    private static int Align(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length)
            return;

        Array.Resize(ref _buffer, Math.Max(required, _buffer.Length * 2));
    }
}
```

要求：

```text id="d4cc37"
Async-safe Query Job 必须是 struct。
最好是 unmanaged / readonly struct。
Job 内只保存输入值。
不能保存引用对象。
```

这和你强制 `[Query] static` 的方向一致。

---

# 7. 投递流程

## 7.1 QueryFlow.ForEach

```csharp id="k4tnsz"
public void ForEach<TJob>(ref TJob job)
    where TJob : struct, IQueryJob<T1, T2>
{
    var scheduler = _world.Runtime.EcsScheduler;

    if (scheduler.Mode == EcsExecutionMode.Sync)
    {
        ProjectionExecutor2<T1, T2>.ExecutePlain(
            _world,
            _query,
            _predicate,
            ref job);
        return;
    }

    scheduler.EnqueuePlainQuery<TJob, T1, T2>(
        _queryId,
        _predicateId,
        in job);
}
```

---

## 7.2 Async Scheduler.EnqueuePlainQuery

```csharp id="zwyoa2"
public void EnqueuePlainQuery<TJob, T1, T2>(
    int queryId,
    int predicateId,
    in TJob job)
    where TJob : struct, IQueryJob<T1, T2>
{
    int jobOffset = _mainToEcsJobArena.Store(in job);

    var record = new EcsWorkRecord
    {
        Kind = EcsWorkKind.PlainQuery,
        ExecutorId = EcsExecutorId<TJob, T1, T2>.Id,
        QueryId = queryId,
        PredicateId = predicateId,
        JobOffset = jobOffset,
        JobSize = Unsafe.SizeOf<TJob>()
    };

    if (!_mainToEcsRing.TryEnqueue(in record))
    {
        ThrowQueueFull();
    }
}
```

热路径：

```text id="rimroj"
Store job bytes
Fill record
SPSC enqueue
return
```

不做：

```text id="nn6lft"
new WorkItem
ConcurrentQueue.Enqueue
Signal
Wait
```

---

# 8. ExecutorId 设计

`EcsWorkRecord` 只有 `ExecutorId`，EcsWorker 根据它调用执行器。

```csharp id="jhpc5l"
internal static class EcsExecutorId<TJob, T1, T2>
    where TJob : struct, IQueryJob<T1, T2>
{
    public static readonly int Id =
        EcsExecutorRegistry.Register(
            static (world, queryId, predicateId, jobOffset, arena) =>
            {
                ref TJob job = ref arena.Get<TJob>(jobOffset);

                var query = world.Runtime.EcsQueryRegistry.GetQuery(queryId);
                var predicate = world.Runtime.EcsQueryRegistry
                    .GetPredicate<ProjectionPredicate<T1, T2>>(predicateId);

                ProjectionExecutor2<T1, T2>.ExecutePlain(
                    world,
                    query,
                    predicate,
                    ref job);
            });
}
```

注意：

```text id="0ajh31"
ExecutorId 第一次注册不能进入 benchmark。
GlobalSetup 里要预热。
```

---

# 9. EcsWorker 消费

```csharp id="lajzph"
private void Run()
{
    while (_running)
    {
        bool didWork = false;

        while (_mainToEcsRing.TryDequeue(out EcsWorkRecord record))
        {
            didWork = true;
            Execute(record);
        }

        if (!didWork)
        {
            WaitOrSpin();
        }
    }
}

private void Execute(in EcsWorkRecord record)
{
    EcsExecutorRegistry.Execute(
        record.ExecutorId,
        _world,
        record.QueryId,
        record.PredicateId,
        record.JobOffset,
        _mainToEcsJobArena);
}
```

---

# 10. ECS -> Main 回流也用 SPSC

ActorProjectItem 也不要每个 `new`。

```csharp id="q5tak5"
internal enum EcsResultKind : byte
{
    ActorEvent,
    ActorTouch,
    ActorEnsure,
    WorkFailed
}

internal struct EcsResultRecord
{
    public EcsResultKind Kind;

    public ActorId ActorId;
    public int EventTypeId;

    public int PayloadOffset;
    public int PayloadSize;

    public int DebugId;
}
```

EcsWorker 写入：

```csharp id="u9zqjd"
public void EnqueueActorEvent<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
{
    int payloadOffset = _ecsToMainPayloadArena.Store(in value);

    var record = new EcsResultRecord
    {
        Kind = EcsResultKind.ActorEvent,
        ActorId = actorId,
        EventTypeId = EventTypeId<TEvent>.Id,
        PayloadOffset = payloadOffset,
        PayloadSize = Unsafe.SizeOf<TEvent>()
    };

    if (!_ecsToMainRing.TryEnqueue(in record))
        ThrowResultQueueFull();
}
```

Main.Pump Drain：

```csharp id="aeeh2u"
public int DrainResults(int maxCount)
{
    int count = 0;

    while ((maxCount <= 0 || count < maxCount) &&
           _ecsToMainRing.TryDequeue(out EcsResultRecord record))
    {
        ApplyResult(in record);
        count++;
    }

    return count;
}
```

ActorEvent Apply：

```csharp id="26lk9v"
private void ApplyResult(in EcsResultRecord record)
{
    switch (record.Kind)
    {
        case EcsResultKind.ActorEvent:
            EcsResultExecutorRegistry.PostToActor(
                record.EventTypeId,
                _runtime.Actors,
                record.ActorId,
                record.PayloadOffset,
                _ecsToMainPayloadArena);
            break;
    }
}
```

---

# 11. PayloadArena 生命周期问题

这里要特别小心。

Main -> EcsWorker 的 JobArena 不能在主线程提交后立即 Reset，因为 EcsWorker 还没消费。

所以要做双缓冲或 fence。

## 11.1 推荐双 Arena

```text id="z1ik1o"
Main writing arena A
EcsWorker reading arena B

Flush / Swap:
  publish A
  main switches to empty B
```

但如果你用 ring 持续流式消费，就需要按 fence 回收。

更简单第一版：

```text id="2wswld"
FrameSubmissionBatch:
  一个 batch 包含 records + job arena。
  Flush 后整个 batch 交给 EcsWorker。
  EcsWorker 处理完 batch 后归还。
```

这比裸 ring 更容易管理内存生命周期。

---

# 12. 更推荐：Batch + SPSC 指针队列

为了避免 arena 生命周期复杂，我建议最终是：

```text id="2c2v7u"
Main 本地写 EcsSubmissionBatch
Flush 时把 batch 指针投递到 SPSC ring
EcsWorker 消费 batch
处理完归还 batch pool
```

SPSC ring 里不是每个 Query 一个 record，而是每个 batch 一个指针：

```csharp id="jyn1c7"
internal sealed class EcsSubmissionBatch
{
    public EcsWorkRecord[] Records;
    public int Count;
    public EcsJobArena JobArena;

    public void Clear()
    {
        Count = 0;
        JobArena.Reset();
    }
}
```

主线程：

```csharp id="5owc4x"
_currentBatch.AddPlainQuery(...);

public void Flush()
{
    if (_currentBatch.Count == 0)
        return;

    _mainToEcsBatchRing.TryEnqueue(_currentBatch);
    _currentBatch = _batchPool.Rent();
}
```

EcsWorker：

```csharp id="plz9tb"
while (_mainToEcsBatchRing.TryDequeue(out var batch))
{
    ExecuteBatch(batch);
    batch.Clear();
    _batchPool.Return(batch);
}
```

好处：

```text id="jbhcs2"
每个 Query 只写本地 batch，不跨线程。
每帧 Flush 只投递一个 batch。
arena 生命周期天然跟 batch 绑定。
更容易做到无分配。
```

如果你极致追求“主线程向队列投递”本身 20ns，batch 指针入 SPSC ring 更容易做到，因为投递的是一个引用，而不是每个 work record。

---

# 13. 但这改变了“投递”的定义

你现在问的是：

```text id="pqxhud"
主线程向队列投递 WorkItem 能不能 20ns？
```

如果每个 Query 都独立向队列投递一个 WorkRecord：

```text id="9s8o69"
有机会接近几十 ns，但 20ns 不保证。
```

如果每帧只投递一个 Batch 指针：

```text id="q69b04"
队列投递本身更容易接近 20ns。
但单 Query 只是写 batch，不是写跨线程队列。
```

我更建议你采用 batch，因为游戏运行时天然按帧组织。

---

# 14. 不要每次 Signal

两种策略：

## 14.1 Frame Flush Signal Once

```csharp id="p5ugck"
Flush()
{
    if (_mainToEcsBatchRing.TryEnqueue(batch))
    {
        _worker.Signal();
    }
}
```

每帧最多一次 signal。

## 14.2 Worker Spin Poll

```csharp id="eqg7v1"
while (_running)
{
    if (TryConsume())
        continue;

    Thread.SpinWait(spinCount);

    if (idleTooLong)
        WaitHandle.WaitOne();
}
```

高性能模式下可以完全不 signal 或少 signal。

---

# 15. 推荐最终方案

我建议你采用这个结构：

```text id="v2cvsy"
Main -> EcsWorker:
  Frame-local EcsSubmissionBatch
  + SPSC batch ring
  + no per-query signal
  + no per-query allocation

EcsWorker -> Main:
  EcsResultBatch
  + SPSC result batch ring
  + Main.Pump drain

Other threads:
  如果未来需要，走单独 MPSC ingress 慢路径。
```

也就是：

```text id="imrq7k"
主流程用 SPSC。
扩展流程才用 MPSC。
```

---

# 16. Benchmark 应该怎么改

新增三层 benchmark。

## 16.1 Raw SPSC Enqueue

测队列本身：

```csharp id="k2mdtt"
[Benchmark]
[OperationsPerInvoke(1024)]
public void Spsc_Enqueue_Record_1024()
{
    for (int i = 0; i < 1024; i++)
    {
        _ring.TryEnqueue(in _record);
    }
}
```

---

## 16.2 Batch Record

测 Query 记录到 batch：

```csharp id="tylcsn"
[Benchmark]
[OperationsPerInvoke(1024)]
public void Batch_RecordPlainQuery_1024()
{
    for (int i = 0; i < 1024; i++)
    {
        _batch.AddPlainQuery(...);
    }
}
```

---

## 16.3 Flush Batch

测主线程向 SPSC 队列投递 batch：

```csharp id="wb3bb7"
[Benchmark]
public void Spsc_Enqueue_Batch()
{
    _ring.TryEnqueue(_batch);
}
```

这样你就能分别知道：

```text id="y7965c"
纯队列投递成本是多少？
记录一个 Query 成本是多少？
批量发布成本是多少？
```

当前 `Async PlainQuery SubmitOnly` 太上层了，它包含太多东西。

---

# 17. 最终结论

你的流程不是 MPSC，应该优化成：

```text id="b72da0"
Main -> EcsWorker：SPSC
EcsWorker -> Main：SPSC
```

最优方案是：

```text id="82hp11"
EcsSubmissionBatch + SPSC batch ring
EcsResultBatch + SPSC result ring
JobArena / PayloadArena 跟 batch 生命周期绑定
Frame Flush 时 signal once
```

不要走：

```text id="3cjzzx"
ConcurrentQueue<IEcsWorkItem>
每 Query new WorkItem
每 Query Signal
每 Query object/interface 分发
```

这样你才能把“主线程投递到副线程”的热路径从当前 **5–20 μs** 往 **几十 ns / 百 ns 内**压。至于稳定 20ns，只有“投递一个已准备好的 batch 指针到 SPSC ring”比较有机会；“每个 Query 完整封装并投递”不太现实。
