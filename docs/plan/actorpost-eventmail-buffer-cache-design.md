# ActorPost EventMail Buffer Cache Design

## 1. 目标

本设计用于验证 LayerBase ActorPost 热路径中，是否可以通过让 `EventMail<TEvent>` 缓存底层 `TEvent[] Buffer` 引用，减少每次写入时从 `BufferId` 回查数组的间接寻址成本。

当前写入路径大致为：

```text
PostQueuedGrowCore
  -> WriteQueued
    -> EventMailPool.Write
      -> RingQueueBuffer.Write
        -> GetBufferUnchecked(bufferId)
          -> _buffers[bufferId - 1]
          -> buffer[index] = value
```

目标路径：

```text
PostQueuedGrowCore
  -> WriteQueued
    -> mail.Buffer![mail.Tail] = value
```

该实验属于“用 `EventMail<TEvent>` 内存换 ActorPost 热路径性能”的优化。

---

## 2. 设计原则

### 2.1 保留 BufferId

虽然新增 `Buffer` 引用，但仍然保留 `BufferId`。

原因：

```text
BufferId 仍用于 Release。
BufferId 仍用于 Resize。
BufferId 仍用于调试。
BufferId 仍可作为 RingQueueBuffer 内部槽位编号。
```

### 2.2 Buffer 只作为热路径缓存

`Buffer` 的职责：

```text
减少 Post / Pump 中的 bufferId -> array 查询。
不改变 RingQueueBuffer 的资源管理模型。
不替代 BufferId。
```

### 2.3 Resize 后必须同步 Buffer

如果邮箱扩容，底层数组会改变。

因此扩容成功后必须同步：

```text
mail.Buffer = newBuffer
mail.Capacity = newBuffer.Length
mail.Head = 0
mail.Tail = mail.Count
```

### 2.4 Release 后必须清空 Buffer

释放邮箱后必须执行：

```text
mail = default
```

这样 `BufferId` 和 `Buffer` 都会被清空。

---

## 3. EventMail<TEvent> 修改

修改文件：

```text
LayerBase/Actor/Mail/EventMail.cs
```

目标代码：

```csharp
namespace LayerBase.Actor;

internal struct EventMail<TEvent>
    where TEvent : struct
{
    public TEvent SingleValue;

    public int BufferId;

    public TEvent[]? Buffer;

    public int Head;

    public int Tail;

    public int Count;

    public int Capacity;
}
```

字段说明：

```text
SingleValue:
  Latest / Dirty 等单值模式可直接保存事件值。

BufferId:
  RingQueueBuffer 内部的 1-based buffer 编号。
  用于 Release / Resize / 调试。

Buffer:
  当前邮箱缓存的真实底层数组引用。
  Queued 写入和 Pump 读取可以直接访问该数组。

Head:
  当前队列头部位置。

Tail:
  当前队列尾部写入位置。

Count:
  当前邮箱内未消费事件数量。

Capacity:
  当前邮箱容量。
```

---

## 4. 新增 EventMailRentResult<TEvent>

新增文件：

```text
LayerBase/Actor/Mail/EventMailRentResult.cs
```

代码：

```csharp
namespace LayerBase.Actor;

internal readonly struct EventMailRentResult<TEvent>
    where TEvent : struct
{
    public readonly int BufferId;

    public readonly TEvent[] Buffer;

    public EventMailRentResult(
        int bufferId,
        TEvent[] buffer)
    {
        // bufferId 参数作用：
        // RingQueueBuffer 分配出来的 1-based buffer 编号。
        // 后续 Release / Resize 仍然可以用它定位池内槽位。

        // buffer 参数作用：
        // 实际底层事件数组。
        // EventMail 会缓存它，让 Post 热路径可以直接写数组。

        BufferId = bufferId;
        Buffer = buffer;
    }
}
```

---

## 5. RingQueueBuffer<TEvent> 修改

修改文件：

```text
LayerBase/Actor/Mail/RingQueueBuffer.cs
```

### 5.1 新增 RentWithBuffer

```csharp
public EventMailRentResult<TEvent> RentWithBuffer(
    int initialCapacity)
{
    // initialCapacity 参数作用：
    // 请求的初始容量。
    // Rent 内部会把它归一化为 2 的幂。

    int bufferId = Rent(initialCapacity);

    TEvent[] buffer =
        GetBufferUnchecked(bufferId);

    return new EventMailRentResult<TEvent>(
        bufferId,
        buffer);
}
```

### 5.2 新增 ResizeWithBuffer

```csharp
public TEvent[] ResizeWithBuffer(
    int bufferId,
    int head,
    int count,
    int newCapacity)
{
    // bufferId 参数作用：
    // 要扩容的 buffer 编号。

    // head 参数作用：
    // 当前环形队列的头部下标。
    // Resize 需要从 head 开始按顺序拷贝旧数据。

    // count 参数作用：
    // 当前邮箱中已有的消息数量。

    // newCapacity 参数作用：
    // 扩容后的目标容量。
    // 内部会归一化为 2 的幂。

    int capacity =
        ActorMailCapacity.NormalizePowerOfTwo(newCapacity);

    if (capacity <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(newCapacity));
    }

    TEvent[] oldBuffer =
        GetBuffer(bufferId);

    var newBuffer =
        new TEvent[capacity];

    int mask =
        oldBuffer.Length - 1;

    for (int i = 0; i < count; i++)
    {
        newBuffer[i] =
            oldBuffer[(head + i) & mask];
    }

    _buffers[bufferId - 1] =
        newBuffer;

    return newBuffer;
}
```

### 5.3 保留旧 Resize

```csharp
public void Resize(
    int bufferId,
    int head,
    int count,
    int newCapacity)
{
    // bufferId 参数作用：
    // 要扩容的 buffer 编号。

    // head 参数作用：
    // 当前环形队列头部下标。

    // count 参数作用：
    // 当前已有事件数量。

    // newCapacity 参数作用：
    // 新容量。

    // 逻辑说明：
    // 旧 API 保留给不需要直接拿新 buffer 引用的调用点。
    _ = ResizeWithBuffer(
        bufferId,
        head,
        count,
        newCapacity);
}
```

---

## 6. EventMailPool<TEvent> 修改

修改文件：

```text
LayerBase/Actor/Mail/EventMailPool.cs
```

### 6.1 新增 RentInitialWithBuffer

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public EventMailRentResult<TEvent> RentInitialWithBuffer()
{
    // RentInitialWithBuffer 方法作用：
    // 按当前 ActorMailOptions.InitialCapacity 租用初始 buffer。
    // 返回 BufferId 和真实数组引用。

    return _buffer.RentWithBuffer(
        _options.InitialCapacity);
}
```

### 6.2 新增 RentWithBuffer

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public EventMailRentResult<TEvent> RentWithBuffer(
    int capacity)
{
    // capacity 参数作用：
    // 请求的邮箱容量。
    // RingQueueBuffer 会归一化为 2 的幂。

    return _buffer.RentWithBuffer(
        capacity);
}
```

### 6.3 修改 TryGrow

```csharp
public bool TryGrow(
    ref EventMail<TEvent> mail)
{
    // mail 参数作用：
    // 当前目标 Actor 的事件邮箱。
    // 扩容成功时，需要同步更新 Buffer、Head、Tail、Capacity。

    if (mail.Capacity >= _options.MaxCapacity)
    {
        return false;
    }

    int growFactor =
        Math.Max(_options.GrowFactor, 2);

    int nextCapacity =
        mail.Capacity * growFactor;

    if (nextCapacity <= mail.Capacity)
    {
        nextCapacity =
            mail.Capacity + 1;
    }

    nextCapacity =
        Math.Min(nextCapacity, _options.MaxCapacity);

    if (nextCapacity <= mail.Capacity)
    {
        return false;
    }

    TEvent[] newBuffer =
        _buffer.ResizeWithBuffer(
            mail.BufferId,
            mail.Head,
            mail.Count,
            nextCapacity);

    mail.Buffer = newBuffer;
    mail.Head = 0;
    mail.Tail = mail.Count;
    mail.Capacity = newBuffer.Length;

    return true;
}
```

---

## 7. ActorWorld.FastPath 修改

修改文件：

```text
LayerBase/Actor/Storage/ActorWorld.FastPath.cs
```

### 7.1 修改 EnsureMailAllocated

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void EnsureMailAllocated<TEvent>(
    ref EventMail<TEvent> mail,
    EventMailPool<TEvent> pool,
    int initialCapacity)
    where TEvent : struct
{
    // mail 参数作用：
    // 当前 Actor 对应 TEvent 的邮箱。
    // 如果 Buffer 为空，说明还没有租用底层数组。

    // pool 参数作用：
    // 当前 ActorWorld + TEvent 的邮箱池。

    // initialCapacity 参数作用：
    // 当前事件构建期确定的初始邮箱容量。

    if (mail.Buffer == null)
    {
        EventMailRentResult<TEvent> rent =
            pool.RentWithBuffer(initialCapacity);

        mail.BufferId = rent.BufferId;
        mail.Buffer = rent.Buffer;
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 0;
        mail.Capacity = rent.Buffer.Length;
    }
}
```

### 7.2 修改 WriteQueued

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void WriteQueued<TEvent>(
    ref EventMail<TEvent> mail,
    in TEvent value,
    DirtySlotList dirtySlots,
    int slotIndex,
    int bucketIndex,
    EventMailPool<TEvent> pool)
    where TEvent : struct
{
    // mail 参数作用：
    // 当前目标 Actor 的事件邮箱。
    // 这里要求 EnsureMailAllocated 已经保证 mail.Buffer 不为空。

    // value 参数作用：
    // 要写入邮箱的事件值。

    // dirtySlots 参数作用：
    // 当前事件列的脏 slot 列表。
    // 当邮箱从空变为非空时，需要标记 slotIndex。

    // slotIndex 参数作用：
    // 当前 Actor 在 Archetype 内的 slot 下标。

    // bucketIndex 参数作用：
    // 当前事件列对应的 dirty bucket 下标。

    // pool 参数作用：
    // 保留参数是为了减少改动范围。
    // 当前方法直接写 mail.Buffer，不再调用 pool.Write。

    TEvent[] buffer =
        mail.Buffer!;

    buffer[mail.Tail] =
        value;

    mail.Tail++;
    mail.Count++;

    if (mail.Tail == mail.Capacity)
    {
        mail.Tail = 0;
    }

    if (mail.Count == 1)
    {
        dirtySlots.Mark(slotIndex);
        _dirtyEventBuckets.Mark(bucketIndex);
    }
}
```

### 7.3 修改 PostQueuedDropOldestCore 满队列分支

```csharp
if (mail.Count >= mail.Capacity)
{
    // 逻辑说明：
    // DropOldest 满队列时丢弃 Head 指向的旧消息。
    // 原来的 pool.Read 只是读出旧值但没有使用，因此可以删除。

    TEvent[] buffer =
        mail.Buffer!;

    mail.Head++;
    if (mail.Head == mail.Capacity)
    {
        mail.Head = 0;
    }

    buffer[mail.Tail] =
        value;

    mail.Tail++;
    if (mail.Tail == mail.Capacity)
    {
        mail.Tail = 0;
    }

    dirtySlots.Mark(slotIndex);
    return PostResult.Success;
}
```

### 7.4 修改 HandleGrowFailure

#### DropOldest

```csharp
case ActorMailFullPolicy.DropOldest:
    mail.Head++;
    if (mail.Head == mail.Capacity)
    {
        mail.Head = 0;
    }

    mail.Count--;
    return PostResult.Success;
```

#### DropNewest

```csharp
case ActorMailFullPolicy.DropNewest:
    mail.Tail--;
    if (mail.Tail < 0)
    {
        mail.Tail = mail.Capacity - 1;
    }

    mail.Count--;

    mail.Buffer![mail.Tail] =
        value;

    mail.Count++;
    return PostResult.Success;
```

#### OverwriteLatest

```csharp
case ActorMailFullPolicy.OverwriteLatest:
    if (mail.Count > 0)
    {
        int latestIndex =
            ActorMailCapacity.Wrap(
                mail.Head + mail.Count - 1,
                mail.Capacity);

        mail.Buffer![latestIndex] =
            value;

        return PostResult.Coalesced();
    }

    return PostResult.Success;
```

---

## 8. EventMailReader 修改

修改文件：

```text
LayerBase/Actor/Mail/EventMailReader.cs
```

目标代码：

```csharp
namespace LayerBase.Actor;

internal static class EventMailReader
{
    public static bool TryDequeue<TEvent>(
        ref EventMail<TEvent> mail,
        EventMailPool<TEvent> bufferPool,
        out TEvent value)
        where TEvent : struct
    {
        // mail 参数作用：
        // 当前 Actor 的事件邮箱。
        // 可能是 SingleValue 模式，也可能是 Buffer 队列模式。

        // bufferPool 参数作用：
        // 兼容旧签名保留。
        // 当前优先使用 mail.Buffer 直接读取，不再通过 bufferPool.Read。

        // value 参数作用：
        // 成功时输出一条事件值。

        if (mail.Count == 0)
        {
            value = default;
            return false;
        }

        if (mail.Buffer == null)
        {
            value = mail.SingleValue;
            mail.SingleValue = default;
            mail.Count = 0;
            mail.Head = 0;
            mail.Tail = 0;
            mail.Capacity = 0;
            return true;
        }

        TEvent[] buffer =
            mail.Buffer;

        value =
            buffer[mail.Head];

        mail.Head++;
        if (mail.Head == mail.Capacity)
        {
            mail.Head = 0;
        }

        mail.Count--;

        if (mail.Count == 0)
        {
            mail.Head = 0;
            mail.Tail = 0;
        }

        return true;
    }

    public static void ReleaseIfEmpty<TEvent>(
        ref EventMail<TEvent> mail,
        EventMailPool<TEvent> bufferPool,
        ActorMailOptions options)
        where TEvent : struct
    {
        // mail 参数作用：
        // 当前 Actor 的事件邮箱。

        // bufferPool 参数作用：
        // 用于释放 BufferId 对应的底层数组。

        // options 参数作用：
        // 当前事件邮箱配置。
        // ReleaseWhenEmpty=true 时，邮箱清空后释放底层 buffer。

        if (mail.Count != 0)
        {
            return;
        }

        if (mail.BufferId == 0)
        {
            mail = default;
            return;
        }

        if (!options.ReleaseWhenEmpty)
        {
            return;
        }

        bufferPool.Release(mail.BufferId);
        mail = default;
    }

    public static void ForceRelease<TEvent>(
        ref EventMail<TEvent> mail,
        EventMailPool<TEvent> bufferPool)
        where TEvent : struct
    {
        // mail 参数作用：
        // 当前 Actor 的事件邮箱。

        // bufferPool 参数作用：
        // 用于释放 BufferId 对应的底层数组。

        if (mail.BufferId != 0)
        {
            bufferPool.Release(mail.BufferId);
        }

        mail = default;
    }
}
```

---

## 9. Latest / Dirty 路径修改

在 `PostLatestCore` 和 `PostDirtyCore` 中，将：

```csharp
pool.Write(mail.BufferId, 0, in value);
```

改成：

```csharp
mail.Buffer![0] = value;
```

### PostLatestCore buffer 分支

```csharp
mail.Buffer![0] = value;
mail.Head = 0;
mail.Tail = 0;
mail.Count = 1;
```

### PostDirtyCore buffer 分支

```csharp
mail.Buffer![0] = value;
mail.Head = 0;
mail.Tail = 0;
mail.Count = 1;
dirtySlots.Mark(slotIndex);
_dirtyEventBuckets.Mark(bucketIndex);
return PostResult.Success;
```

---

## 10. 必须保持的不变量

### 10.1 分配不变量

```text
mail.BufferId != 0  => mail.Buffer != null
mail.Buffer != null => mail.Capacity == mail.Buffer.Length
```

### 10.2 扩容不变量

```text
pool.TryGrow(ref mail) 成功后：
  mail.Buffer 指向新数组
  mail.Head = 0
  mail.Tail = mail.Count
  mail.Capacity = mail.Buffer.Length
```

### 10.3 释放不变量

```text
ReleaseIfEmpty 且 ReleaseWhenEmpty=true：
  bufferPool.Release(mail.BufferId)
  mail = default

ForceRelease：
  bufferPool.Release(mail.BufferId)
  mail = default
```

### 10.4 SingleValue 不变量

```text
mail.Buffer == null 且 mail.Count > 0：
  使用 SingleValue 模式

mail.Buffer != null：
  使用 Buffer 队列模式
```

---

## 11. 建议验证测试

### 11.1 单 Actor 连续写入

```text
ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent
ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent_Prewarm
```

目标：

```text
验证直接缓存 Buffer 后，单体 PostTo 是否下降。
```

### 11.2 多 Actor 轮询写入

```text
ActorPost_ArchetypeRow_1000Actors_OneEvent
```

目标：

```text
验证每个 Actor 一个邮箱时，Buffer 引用增加后是否改善或恶化 cache 表现。
```

### 11.3 Post + Pump

```text
ActorWorld Post + Pump
ActorWorld Pump only
```

目标：

```text
验证 EventMailReader 直接读 Buffer 是否改善 Pump。
```

### 11.4 扩容场景

```text
ActorPost_WithGrow
```

目标：

```text
验证 TryGrow 后 mail.Buffer 是否正确更新。
```

### 11.5 Release 场景

```text
releaseWhenEmpty = true
Post -> Pump -> Post
```

目标：

```text
验证 Release 后 mail.Buffer 被清空，下次 Post 能重新分配。
```

---

## 12. 风险评估

### 12.1 内存增加

每个 `EventMail<TEvent>` 增加一个 `TEvent[]? Buffer` 字段。

在 64 位运行时下，通常增加 8 字节。

影响范围：

```text
Actor 数量 × EventColumn 数量
```

### 12.2 缓存局部性风险

`EventMail<TEvent>` 变大后，`EventMail<TEvent>[]` 中每个元素占用更多空间。

可能出现：

```text
单次 Post 更快。
批量扫 Mails 时 cache miss 增加。
```

所以必须同时观察：

```text
单体 PostTo
1000Actors Post
Query.PostAll
Pump
```

### 12.3 Resize 同步风险

如果扩容后忘记更新 `mail.Buffer`，会导致继续写旧数组。

必须保证：

```text
TryGrow 成功后同步 mail.Buffer。
ResizeWithBuffer 返回新数组。
```

### 12.4 Release 同步风险

如果 Release 后没有 `mail = default`，会导致 `mail.Buffer` 悬挂引用。

当前设计要求：

```text
ReleaseIfEmpty 和 ForceRelease 都必须最终 mail = default。
```

---

## 13. 实验结论判定

### 13.1 值得保留

如果出现：

```text
PostTo 单体明显下降。
1000Actors 没有明显变慢。
Pump 没有明显变慢。
内存增加可接受。
```

则保留该设计。

### 13.2 不值得保留

如果出现：

```text
PostTo 几乎不变。
1000Actors 或 Query.PostAll 变慢。
内存增加明显。
```

则回退该实验。

原因：

```text
说明 JIT 已经很好地内联了 pool.Write / GetBufferUnchecked。
Buffer 引用缓存没有抵消 EventMail 变大带来的成本。
```

---

## 14. 推荐落地顺序

```text
1. EventMail<TEvent> 增加 Buffer 字段。
2. 新增 EventMailRentResult<TEvent>。
3. RingQueueBuffer 增加 RentWithBuffer / ResizeWithBuffer。
4. EventMailPool 增加 RentWithBuffer / RentInitialWithBuffer。
5. TryGrow 同步 mail.Buffer。
6. EnsureMailAllocated 写入 mail.Buffer。
7. WriteQueued 直接写 mail.Buffer。
8. EventMailReader 直接读 mail.Buffer。
9. Latest / Dirty buffer 分支直接写 mail.Buffer。
10. 跑 benchmark。
```

---

## 15. 最终目标

本实验完成后，QueuedGrow 热路径应从：

```text
pool.Write(mail.BufferId, mail.Tail, in value)
```

变成：

```text
mail.Buffer![mail.Tail] = value
```

目标是减少一次池对象转发、一次 bufferId 查询和一次 `_buffers` 数组间接访问。
