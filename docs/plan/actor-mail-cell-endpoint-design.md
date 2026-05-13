# Actor Mail Cell Endpoint Design

## 1. 文档目标

本文档用于指导 LayerBase 将 `EventMail<TEvent>` 从 `struct` 改为 `sealed class`，并基于稳定 mailbox object 引入 `ActorQueuedGrowMailEndpoint<TEvent>`，实现高频路径下的 **Post → 邮箱内存直写**。

核心目标：

```text
普通路径：
ActorWorld.PostTo(actorId, event)
→ 保持兼容、安全、通用。

极热路径：
ActorQueuedGrowMailEndpoint<TEvent>.PostChecked(event)
→ 直接写 EventMail<TEvent>.Buffer
→ 避免每次重新 ActorId → EventPostState → row → mails[slotIndex] 定位。
```

这个改动不是为了让 Actor 邮箱更接近 ECS 连续存储，而是为了让邮箱变成稳定引用对象，让 endpoint 可以直接持有目标邮箱，实现真正的直达投递。

---

## 2. 当前链路问题

当前 `PostTo<TEvent>` 的典型链路是：

```text
ActorWorld.PostTo<TEvent>(ActorId actorId, in TEvent value)
→ EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex)
→ state.RowsByArchetype[actorId.ArchetypeId]
→ row.Mails[actorId.SlotIndex]
→ generation / alive 校验
→ state.RouteCode 分发
→ PostQueuedGrowCore
→ EnsureMailAllocated
→ WriteQueued
→ dirtySlots.Mark(slotIndex)
→ dirtyEventBuckets.Mark(bucketIndex)
```

这条链路作为公共 API 是合理的，但对 ECS / Projection / 高频点对点投递来说有重复定位成本。

如果每帧都已经知道目标 Actor，并且事件类型固定，那么更理想的路径是：

```text
endpoint
→ mail
→ mail.Buffer[mail.Tail] = value
→ mail.Tail / mail.Count 更新
→ dirty 标记
```

---

## 3. 设计原则

### 3.1 `EventMail<TEvent>` 对象生命周期稳定

`EventMail<TEvent>` 改成 `sealed class` 后，每个 Actor slot 对每个事件类型拥有一个稳定 mailbox object。

规则：

```text
EventMail object 生命周期 = EventColumn slot 生命周期。
Actor 销毁时不替换 EventMail object。
slot 复用时复用同一个 EventMail object，但更新 AliveGeneration。
旧 endpoint 依靠 generation 不匹配自动失效。
```

### 3.2 Buffer 可以池化，Mail object 不频繁池化

`EventMail<TEvent>` 作为 endpoint 直达目标，不建议频繁归还池后给别的 slot 使用。

推荐：

```text
EventMail object 常驻。
EventMail.Buffer 可由 EventMailPool 管理。
默认热路径不归还 Buffer。
低频内存压缩路径再考虑释放 Buffer。
```

### 3.3 普通 `PostTo` 不删除

保留：

```text
ActorWorld.PostTo(actorId, event)
```

它是安全、通用、兼容路径。

新增：

```text
ActorQueuedGrowMailEndpoint<TEvent>
```

它是高频直写路径。

---

## 4. 文件修改总览

建议修改或新增这些文件：

```text
LayerBase/Actor/Mail/EventMail.cs
LayerBase/Actor/Mail/EventPostRow.cs
LayerBase/Actor/Mail/ActorQueuedGrowMailEndpoint.cs
LayerBase/Actor/Storage/ActorWorld.FastPath.cs
LayerBase/Actor/Storage/ActorWorld.Post.cs
LayerBase/Actor/Storage/ActorWorld.PostEndpoint.cs
LayerBase/Actor/Mail/EventColumn.cs
LayerBase/Actor/Mail/EventMailReader.cs
LayerBase/Actor/Storage/TypedActorStorage.cs
LayerBase.BenchMark/EcsActorBenchmarks.cs
```

重点：

```text
EventMail.cs
    struct → sealed class。

EventColumn.cs
    初始化和扩容时必须填充 EventMail object。

ActorWorld.FastPath.cs
    PostQueuedGrowCore / WriteQueued / EnsureMailAllocated 从 ref mail 改为 class mail。

ActorWorld.PostEndpoint.cs
    新增 TryGetQueuedGrowMailEndpoint<TEvent>。

ActorQueuedGrowMailEndpoint.cs
    新增 endpoint 直写结构。

Benchmark
    新增 endpoint direct write 测试。
```

---

# Part A：EventMail class 化

## 5. 修改 `EventMail<TEvent>`

文件：

```text
LayerBase/Actor/Mail/EventMail.cs
```

建议改为：

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 单个 Actor slot 对某个 TEvent 的邮箱对象。
///
/// 设计目标：
/// 1. 作为稳定引用对象被 endpoint 直接持有。
/// 2. 保存队列状态、Buffer、slotIndex 和 generation 校验位。
/// 3. 避免高频 Post 时每次通过 mails[slotIndex] 重新定位 mailbox。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// 该类型必须是 struct，避免事件对象本身产生托管堆分配。
/// </typeparam>
internal sealed class EventMail<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// Latest / Dirty 策略下使用的单值缓存。
    /// QueuedGrow 主路径通常写入 Buffer。
    /// </summary>
    public TEvent SingleValue;

    /// <summary>
    /// 当前邮箱租用的 Buffer 编号。
    /// 如果 EventMailPool 需要追踪 Buffer，可以使用该字段。
    /// </summary>
    public int BufferId;

    /// <summary>
    /// 队列事件缓冲区。
    /// QueuedGrow 直写时真正写入的是这个数组。
    /// </summary>
    public TEvent[]? Buffer;

    /// <summary>
    /// 队列头下标。
    /// Pump 从这里读取事件。
    /// </summary>
    public int Head;

    /// <summary>
    /// 队列尾下标。
    /// Post 从这里写入事件。
    /// </summary>
    public int Tail;

    /// <summary>
    /// 当前邮箱内待处理事件数量。
    /// </summary>
    public int Count;

    /// <summary>
    /// 当前 Buffer 的可容纳事件数量。
    /// </summary>
    public int Capacity;

    /// <summary>
    /// 当前邮箱对应的 Actor slot 下标。
    /// dirty slot 标记时使用。
    /// </summary>
    public int SlotIndex;

    /// <summary>
    /// 当前邮箱允许投递的 Actor generation。
    ///
    /// 语义：
    /// - 等于 endpoint 保存的 generation：endpoint 仍然有效。
    /// - -1：当前 slot 不可投递，例如 Actor 已销毁、PendingDestroy 或 Destroying。
    /// </summary>
    public int AliveGeneration;

    /// <summary>
    /// 重置邮箱绑定的 slot 信息。
    ///
    /// 参数说明：
    /// slotIndex：当前邮箱所属的 Actor slot。
    /// aliveGeneration：当前 slot 可投递 generation；不可投递时传 -1。
    ///
    /// 作用：
    /// Actor 创建、销毁、slot 复用时更新 endpoint 校验位。
    /// </summary>
    public void ResetSlot(
        int slotIndex,
        int aliveGeneration)
    {
        SlotIndex = slotIndex;
        AliveGeneration = aliveGeneration;
    }

    /// <summary>
    /// 清空邮箱内的待处理事件。
    ///
    /// 参数说明：
    /// clearSingleValue：是否清理 SingleValue。
    ///
    /// 作用：
    /// Destroy、Drain 或 slot recycle 时清理邮箱状态。
    /// 默认不释放 Buffer，避免下一次热路径重新分配。
    /// </summary>
    public void Clear(bool clearSingleValue = true)
    {
        if (clearSingleValue)
        {
            SingleValue = default;
        }

        Head = 0;
        Tail = 0;
        Count = 0;
    }

    /// <summary>
    /// 释放当前 Buffer 引用。
    ///
    /// 作用：
    /// 给低频内存压缩或显式释放路径使用。
    /// 默认热路径不要调用该方法。
    /// </summary>
    public void ReleaseBuffer()
    {
        BufferId = 0;
        Buffer = null;
        Head = 0;
        Tail = 0;
        Count = 0;
        Capacity = 0;
    }
}
```

---

## 6. `EventMail<TEvent>[]` 初始化规则

改成 class 后，数组元素默认是 `null`，所以 `EventColumn` 必须在初始化和扩容时填充对象。

建议在 `EventColumn<TActor,TEvent>` 中提供：

```csharp
/// <summary>
/// 确保邮箱数组容量，并为每个新增 slot 创建稳定 EventMail 对象。
/// </summary>
/// <param name="required">
/// 需要支持的 slot 数量。
/// </param>
private void EnsureMailCellCapacity(int required)
{
    if (required <= _mails.Length)
    {
        return;
    }

    int oldLength = _mails.Length;
    int newLength = oldLength == 0 ? 4 : oldLength;

    while (newLength < required)
    {
        newLength *= 2;
    }

    Array.Resize(ref _mails, newLength);

    for (int slotIndex = oldLength; slotIndex < newLength; slotIndex++)
    {
        var mail = new EventMail<TEvent>();

        mail.ResetSlot(
            slotIndex: slotIndex,
            aliveGeneration: -1);

        _mails[slotIndex] = mail;
    }
}
```

如果现有方法名是 `EnsureSlotCapacity(int slotIndex)`，可以在其中调用：

```csharp
EnsureMailCellCapacity(slotIndex + 1);
```

---

# Part B：EventPostRow 简化

## 7. 修改 `EventPostRow<TEvent>`

`EventMail<TEvent>` 内部已经有 `AliveGeneration` 和 `SlotIndex` 后，`EventPostRow` 不再需要额外保存：

```text
Generations
ActorExists
AlivePostGenerations
```

推荐结构：

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 单个 Actor archetype 对某个事件类型的投递行。
/// 一行对应一个 Actor archetype，一列 EventMail 对应该 archetype 下所有 slot。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal readonly struct EventPostRow<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 当前事件类型对应的邮箱对象数组。
    /// 下标是 ActorId.SlotIndex。
    /// 注意：数组元素是 EventMail 对象引用。
    /// </summary>
    public readonly EventMail<TEvent>[] Mails;

    /// <summary>
    /// 当前事件列的 dirty slot 列表。
    /// 当某个邮箱从空变为非空时，会把 slotIndex 标记进去。
    /// </summary>
    public readonly DirtySlotList DirtySlots;

    /// <summary>
    /// 当前事件类型对应的 dirty bucket 下标。
    /// ActorWorld.Pump 会通过它找到有待处理事件的 bucket。
    /// </summary>
    public readonly int BucketIndex;

    /// <summary>
    /// 当前 row 是否有效。
    /// 空 Mails 表示该 archetype 不支持这个事件类型。
    /// </summary>
    public bool IsValid => Mails.Length > 0;

    /// <summary>
    /// 构造 EventPostRow。
    /// </summary>
    /// <param name="mails">当前事件类型对应的邮箱对象数组。</param>
    /// <param name="dirtySlots">当前事件列的 dirty slot 列表。</param>
    /// <param name="bucketIndex">当前事件类型对应的 bucket 下标。</param>
    public EventPostRow(
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex)
    {
        Mails = mails;
        DirtySlots = dirtySlots;
        BucketIndex = bucketIndex;
    }
}
```

`CreateInvalidRow` 改为：

```csharp
private static EventPostRow<TEvent> CreateInvalidRow<TEvent>()
    where TEvent : struct
{
    return new EventPostRow<TEvent>(
        mails: Array.Empty<EventMail<TEvent>>(),
        dirtySlots: DirtySlotList.Empty,
        bucketIndex: -1);
}
```

---

# Part C：更新 generation 同步

## 8. `TypedActorStorage.RefreshPostGenerations`

原先可能更新 `_alivePostGenerations`。现在还需要同步每个事件邮箱对象的 `AliveGeneration`。

建议保留 `_alivePostGenerations` 一段时间，作为兼容或 debug，但 endpoint 路径以 `mail.AliveGeneration` 为准。

在 `TypedActorStorage<TActor>.RefreshPostGenerations(int slotIndex)` 后追加：

```csharp
/// <summary>
/// 刷新所有事件邮箱对象上的可投递 generation。
///
/// 参数说明：
/// slotIndex：需要刷新的 Actor slot。
///
/// 作用：
/// endpoint 通过 EventMail.AliveGeneration 判断自身是否仍然有效。
/// 因此 Actor 创建、销毁、PendingDestroy、Destroying、Enable 变化时都必须刷新。
/// </summary>
private void RefreshMailCellGenerations(int slotIndex)
{
    int aliveGeneration = _alivePostGenerations[slotIndex];

    foreach (ActorEventColumnRuntime? column in _columnsByEventId)
    {
        column?.SetMailAliveGeneration(
            slotIndex,
            aliveGeneration);
    }
}
```

然后在 `RefreshPostGenerations` 末尾调用：

```csharp
RefreshMailCellGenerations(slotIndex);
```

需要在 `ActorEventColumnRuntime` 增加抽象方法：

```csharp
/// <summary>
/// 设置指定 slot 对应邮箱对象上的可投递 generation。
/// </summary>
/// <param name="slotIndex">Actor slot 下标。</param>
/// <param name="aliveGeneration">可投递 generation；不可投递时为 -1。</param>
public abstract void SetMailAliveGeneration(
    int slotIndex,
    int aliveGeneration);
```

在 `EventColumn<TActor,TEvent>` 中实现：

```csharp
public override void SetMailAliveGeneration(
    int slotIndex,
    int aliveGeneration)
{
    if ((uint)slotIndex >= (uint)_mails.Length)
    {
        return;
    }

    EventMail<TEvent> mail = _mails[slotIndex];

    mail.ResetSlot(
        slotIndex: slotIndex,
        aliveGeneration: aliveGeneration);
}
```

---

# Part D：PostTo 兼容改造

## 9. 修改 `TryGetPhysicalRowWithGeneration`

由于 `EventMail` 已经有 `AliveGeneration`，可以改成：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static bool TryGetPhysicalRowWithGeneration<TEvent>(
    ActorId actorId,
    EventPostState<TEvent> state,
    out EventPostRow<TEvent> row,
    out int slotIndex)
    where TEvent : struct
{
    row = state.RowsByArchetype[actorId.ArchetypeId];
    slotIndex = actorId.SlotIndex;

    EventMail<TEvent> mail = row.Mails[slotIndex];

    return mail.AliveGeneration == actorId.Generation;
}
```

如果公共 API 安全性优先，可以保留一个 safe 版本：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static bool TryGetPhysicalRowWithGenerationSafe<TEvent>(
    ActorId actorId,
    EventPostState<TEvent> state,
    out EventPostRow<TEvent> row,
    out int slotIndex)
    where TEvent : struct
{
    EventPostRow<TEvent>[] rows = state.RowsByArchetype;
    int archetypeId = actorId.ArchetypeId;

    if ((uint)archetypeId >= (uint)rows.Length)
    {
        row = default;
        slotIndex = default;
        return false;
    }

    row = rows[archetypeId];
    slotIndex = actorId.SlotIndex;

    if ((uint)slotIndex >= (uint)row.Mails.Length)
    {
        return false;
    }

    EventMail<TEvent> mail = row.Mails[slotIndex];

    return mail.AliveGeneration == actorId.Generation;
}
```

建议：

```text
public PostTo 先走 Safe 版本。
endpoint / generated fast path 走 Fast 版本。
```

---

## 10. 修改 `PostQueuedGrowCore`

原来如果是：

```csharp
ref EventMail<TEvent> mail = ref mails[slotIndex];
```

现在改成：

```csharp
EventMail<TEvent> mail = mails[slotIndex];
```

完整建议：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal PostResult PostQueuedGrowCore<TEvent>(
    int slotIndex,
    in TEvent value,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    EventMailPool<TEvent> pool,
    ActorMailOptions options)
    where TEvent : struct
{
    EventMail<TEvent> mail = mails[slotIndex];

    EnsureMailAllocated(
        mail,
        pool,
        options.InitialCapacity);

    if (mail.Count >= mail.Capacity && !pool.TryGrow(mail))
    {
        PostResult growFailure = HandleGrowFailure(
            mail,
            in value,
            pool,
            options);

        if (!growFailure.IsSuccess || !growFailure.CountsAsPending)
        {
            return growFailure;
        }
    }

    WriteQueued(
        mail,
        in value,
        dirtySlots,
        bucketIndex);

    return PostResult.Success;
}
```

注意：

```text
pool.TryGrow(ref mail) 需要改成 pool.TryGrow(mail)。
HandleGrowFailure(ref mail, ...) 需要改成 HandleGrowFailure(mail, ...)。
```

---

## 11. 修改 `EnsureMailAllocated`

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void EnsureMailAllocated<TEvent>(
    EventMail<TEvent> mail,
    EventMailPool<TEvent> pool,
    int initialCapacity)
    where TEvent : struct
{
    if (mail.Buffer != null)
    {
        return;
    }

    EventMailRentResult<TEvent> rent = pool.RentWithBuffer(initialCapacity);

    mail.BufferId = rent.BufferId;
    mail.Buffer = rent.Buffer;
    mail.Head = 0;
    mail.Tail = 0;
    mail.Count = 0;
    mail.Capacity = rent.Buffer.Length;

    AssertMailBufferInvariant(mail);
}
```

---

## 12. 修改 `WriteQueued`

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void WriteQueued<TEvent>(
    EventMail<TEvent> mail,
    in TEvent value,
    DirtySlotList dirtySlots,
    int bucketIndex)
    where TEvent : struct
{
    TEvent[] buffer = mail.Buffer!;

    buffer[mail.Tail] = value;

    int nextTail = mail.Tail + 1;
    if (nextTail == mail.Capacity)
    {
        nextTail = 0;
    }

    mail.Tail = nextTail;
    mail.Count++;

    if (mail.Count == 1)
    {
        dirtySlots.Mark(mail.SlotIndex);
        _dirtyEventBuckets.Mark(bucketIndex);
    }
}
```

注意：不再传 `slotIndex`，因为 `mail.SlotIndex` 已经保存了 slot。

---

# Part E：Pump / Reader 改造

## 13. 修改 `EventMailReader`

如果当前 reader 使用：

```csharp
ref EventMail<TEvent> mail
```

需要改成：

```csharp
EventMail<TEvent> mail
```

示意：

```csharp
internal static class EventMailReader
{
    /// <summary>
    /// 尝试从邮箱中取出一个事件。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    /// <param name="mail">目标邮箱对象。</param>
    /// <param name="pool">事件 Buffer 池。</param>
    /// <param name="value">成功时输出事件值。</param>
    /// <returns>true 表示成功取出事件。</returns>
    public static bool TryDequeue<TEvent>(
        EventMail<TEvent> mail,
        EventMailPool<TEvent> pool,
        out TEvent value)
        where TEvent : struct
    {
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
            return true;
        }

        TEvent[] buffer = mail.Buffer;
        value = buffer[mail.Head];

        mail.Head++;
        if (mail.Head == mail.Capacity)
        {
            mail.Head = 0;
        }

        mail.Count--;

        return true;
    }
}
```

---

## 14. 修改 `EventColumn.PumpMany`

原来可能是：

```csharp
ref EventMail<TEvent> mail = ref _mails[slotIndex];
```

改为：

```csharp
EventMail<TEvent> mail = _mails[slotIndex];
```

示意：

```csharp
EventMail<TEvent> mail = _mails[slotIndex];

if (!EventMailReader.TryDequeue(
        mail,
        _mailPool,
        out TEvent value))
{
    _dirtySlots.Pop();
    continue;
}
```

---

## 15. 修改 `ClearMail`

```csharp
public override void ClearMail(int slotIndex)
{
    if ((uint)slotIndex >= (uint)_mails.Length)
    {
        return;
    }

    EventMail<TEvent> mail = _mails[slotIndex];

    mail.Clear();
}
```

不要写成：

```csharp
_mails[slotIndex] = new EventMail<TEvent>();
```

否则旧 endpoint 会持有旧对象，造成一致性问题。

---

# Part F：新增 endpoint

## 16. 新增 `ActorQueuedGrowMailEndpoint<TEvent>`

文件：

```text
LayerBase/Actor/Mail/ActorQueuedGrowMailEndpoint.cs
```

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

/// <summary>
/// QueuedGrow 策略专用的邮箱直写端点。
///
/// 作用：
/// 1. endpoint 创建时完成 ActorId → EventMail 的定位。
/// 2. endpoint 投递时直接写 EventMail.Buffer。
/// 3. 避免每次 Post 都重新走 EventPostState、row、slotIndex、route 分发。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// 必须是 struct，避免事件值本身产生托管堆分配。
/// </typeparam>
public readonly struct ActorQueuedGrowMailEndpoint<TEvent>
    where TEvent : struct
{
    private readonly EventMail<TEvent>? _mail;
    private readonly int _generation;
    private readonly DirtySlotList _dirtySlots;
    private readonly DirtyBucketList _dirtyBuckets;
    private readonly int _bucketIndex;

    /// <summary>
    /// endpoint 是否有基础数据。
    /// 注意：HasValue 不代表 endpoint 当前仍然有效。
    /// </summary>
    public bool HasValue => _mail != null;

    /// <summary>
    /// endpoint 当前是否仍然指向创建时的 Actor。
    /// </summary>
    public bool IsAlive
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _mail != null && _mail.AliveGeneration == _generation;
    }

    /// <summary>
    /// 构造邮箱直写 endpoint。
    /// </summary>
    /// <param name="mail">目标邮箱对象。</param>
    /// <param name="generation">创建 endpoint 时的 Actor generation。</param>
    /// <param name="dirtySlots">dirty slot 列表。</param>
    /// <param name="dirtyBuckets">dirty bucket 列表。</param>
    /// <param name="bucketIndex">当前事件类型对应的 bucket 下标。</param>
    internal ActorQueuedGrowMailEndpoint(
        EventMail<TEvent> mail,
        int generation,
        DirtySlotList dirtySlots,
        DirtyBucketList dirtyBuckets,
        int bucketIndex)
    {
        _mail = mail;
        _generation = generation;
        _dirtySlots = dirtySlots;
        _dirtyBuckets = dirtyBuckets;
        _bucketIndex = bucketIndex;
    }

    /// <summary>
    /// 安全直写邮箱。
    /// </summary>
    /// <param name="value">要投递的事件值。</param>
    /// <returns>成功写入时返回 Success；endpoint 失效或邮箱不可写时返回失败。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult PostChecked(in TEvent value)
    {
        EventMail<TEvent>? mail = _mail;

        if (mail == null || mail.AliveGeneration != _generation)
        {
            return PostResult.Failure(
                ActorPostStatus.PhysicalTargetInvalid,
                PostFailureKind.PhysicalTargetInvalid);
        }

        if (mail.Buffer == null || mail.Count >= mail.Capacity)
        {
            return PostResult.Failure(
                ActorPostStatus.MailFullRejected,
                PostFailureKind.MailboxFull);
        }

        TEvent[] buffer = mail.Buffer;

        buffer[mail.Tail] = value;

        int nextTail = mail.Tail + 1;
        if (nextTail == mail.Capacity)
        {
            nextTail = 0;
        }

        mail.Tail = nextTail;
        mail.Count++;

        if (mail.Count == 1)
        {
            _dirtySlots.Mark(mail.SlotIndex);
            _dirtyBuckets.Mark(_bucketIndex);
        }

        return PostResult.Success;
    }

    /// <summary>
    /// 无检查直写邮箱。
    /// </summary>
    /// <param name="value">要投递的事件值。</param>
    /// <remarks>
    /// 调用者必须保证 endpoint 有效、Buffer 已存在、邮箱未满。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PostUnchecked(in TEvent value)
    {
        EventMail<TEvent> mail = _mail!;
        TEvent[] buffer = mail.Buffer!;

        buffer[mail.Tail] = value;

        int nextTail = mail.Tail + 1;
        if (nextTail == mail.Capacity)
        {
            nextTail = 0;
        }

        mail.Tail = nextTail;
        mail.Count++;

        if (mail.Count == 1)
        {
            _dirtySlots.Mark(mail.SlotIndex);
            _dirtyBuckets.Mark(_bucketIndex);
        }
    }
}
```

---

## 17. 新增 `TryGetQueuedGrowMailEndpoint`

文件：

```text
LayerBase/Actor/Storage/ActorWorld.PostEndpoint.cs
```

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    /// <summary>
    /// 尝试创建 QueuedGrow 邮箱直写 endpoint。
    /// </summary>
    /// <typeparam name="TEvent">事件类型。</typeparam>
    /// <param name="actorId">目标 ActorId。</param>
    /// <param name="endpoint">成功时输出邮箱直写 endpoint。</param>
    /// <returns>true 表示创建成功。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetQueuedGrowMailEndpoint<TEvent>(
        ActorId actorId,
        out ActorQueuedGrowMailEndpoint<TEvent> endpoint)
        where TEvent : struct
    {
        EventPostState<TEvent>? state =
            EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);

        if (state == null || state.RouteCode != ActorPostRouteCode.QueuedGrow)
        {
            endpoint = default;
            return false;
        }

        EventPostRow<TEvent>[] rows = state.RowsByArchetype;
        int archetypeId = actorId.ArchetypeId;

        if ((uint)archetypeId >= (uint)rows.Length)
        {
            endpoint = default;
            return false;
        }

        EventPostRow<TEvent> row = rows[archetypeId];
        int slotIndex = actorId.SlotIndex;

        if ((uint)slotIndex >= (uint)row.Mails.Length)
        {
            endpoint = default;
            return false;
        }

        EventMail<TEvent> mail = row.Mails[slotIndex];

        if (mail.AliveGeneration != actorId.Generation)
        {
            endpoint = default;
            return false;
        }

        endpoint = new ActorQueuedGrowMailEndpoint<TEvent>(
            mail: mail,
            generation: actorId.Generation,
            dirtySlots: row.DirtySlots,
            dirtyBuckets: _dirtyEventBuckets,
            bucketIndex: row.BucketIndex);

        return true;
    }
}
```

---

# Part G：Benchmark 新增项

## 18. Endpoint 缓存字段

在 benchmark 中新增：

```csharp
private ActorQueuedGrowMailEndpoint<MoveEvent>[] _pureMoveEndpoints = null!;
private ActorQueuedGrowMailEndpoint<MoveEvent>[] _hybridMoveEndpoints = null!;
```

构建：

```csharp
private void BuildEndpointCaches()
{
    _pureMoveEndpoints = new ActorQueuedGrowMailEndpoint<MoveEvent>[SmallCount];

    for (int i = 0; i < SmallCount; i++)
    {
        if (!_pureActorWorld.TryGetQueuedGrowMailEndpoint<MoveEvent>(
                _pureActorIds[i],
                out _pureMoveEndpoints[i]))
        {
            throw new InvalidOperationException("Failed to create pure MoveEvent endpoint.");
        }
    }

    _hybridMoveEndpoints = new ActorQueuedGrowMailEndpoint<MoveEvent>[SmallCount];

    for (int i = 0; i < SmallCount; i++)
    {
        if (!_actorWorld.TryGetQueuedGrowMailEndpoint<MoveEvent>(
                _hybridSmallActorIds[i],
                out _hybridMoveEndpoints[i]))
        {
            throw new InvalidOperationException("Failed to create hybrid MoveEvent endpoint.");
        }
    }
}
```

`BuildEndpointCaches()` 应该放在 mailbox warmup 之后。

---

## 19. Endpoint 直写测试

```csharp
[Benchmark(Description = "Actor: Endpoint Mail Direct Write × 1000")]
[BenchmarkCategory("Actor-Endpoint")]
public void Actor_EndpointMailDirectWrite_1000()
{
    for (int i = 0; i < SmallCount; i++)
    {
        _ = _pureMoveEndpoints[i].PostChecked(in _moveEvent);
    }
}
```

```csharp
[Benchmark(Description = "Actor: Endpoint Mail Direct Write + Pump × 1000")]
[BenchmarkCategory("Actor-Endpoint")]
public void Actor_EndpointMailDirectWrite_Pump_1000()
{
    for (int i = 0; i < SmallCount; i++)
    {
        _ = _pureMoveEndpoints[i].PostChecked(in _moveEvent);
    }

    var budget = new RuntimeFrameBudget(
        maxEvents: SmallCount * 2,
        usedEvents: 0,
        deadlineTicks: 0);

    _pureActorWorld.Pump(
        deltaTime: 0.016f,
        fixedDeltaTime: 0.016f,
        pumpFixedUpdate: false,
        budget: ref budget);
}
```

```csharp
[Benchmark(Description = "Hybrid: Endpoint Mail Direct Write + Pump × 1000")]
[BenchmarkCategory("Hybrid-Endpoint")]
public void Hybrid_EndpointMailDirectWrite_Pump_1000()
{
    for (int i = 0; i < SmallCount; i++)
    {
        _ = _hybridMoveEndpoints[i].PostChecked(in _moveEvent);
    }

    var budget = new RuntimeFrameBudget(
        maxEvents: SmallCount * 2,
        usedEvents: 0,
        deadlineTicks: 0);

    _actorWorld.Pump(
        deltaTime: 0.016f,
        fixedDeltaTime: 0.016f,
        pumpFixedUpdate: false,
        budget: ref budget);
}
```

---

# Part H：验收标准

## 20. 正确性验收

必须满足：

```text
1. Benchmark 不出现 NA。
2. Actor: PostTo + Pump ×1000 能正常处理 1000 个 MoveEvent。
3. Actor: Endpoint Mail Direct Write + Pump ×1000 能正常处理 1000 个 MoveEvent。
4. Destroy / slot recycle 后旧 endpoint 不应继续投递成功。
5. Unsupported Event 仍然 0 GC。
6. Actor hot path 仍然 0 allocation。
```

建议加 smoke test：

```csharp
public static void Smoke_Endpoint_InvalidAfterDestroy()
{
    ActorWorld world = new ActorWorld();

    MinimalActor actor = world.CreateActor<MinimalActor>();
    ActorId actorId = actor.GetActorId();

    var moveEvent = new MoveEvent
    {
        DeltaX = 1,
        DeltaY = 0
    };

    if (!world.TryGetQueuedGrowMailEndpoint<MoveEvent>(
            actorId,
            out var endpoint))
    {
        throw new InvalidOperationException("Endpoint should be created.");
    }

    world.DestroyActor(actorId);

    PostResult result = endpoint.PostChecked(in moveEvent);

    if (result.IsSuccess)
    {
        throw new InvalidOperationException("Old endpoint must not post after actor destroy.");
    }
}
```

---

## 21. 性能验收

目标不是让普通 `PostTo` 必然下降，而是让 endpoint 路径明显低于普通路径：

```text
Actor: Endpoint Mail Direct Write ×1000
    < Actor: PostTo Only ×1000

Actor: Endpoint Mail Direct Write + Pump ×1000
    < Actor: PostTo + Pump ×1000

Hybrid: Endpoint Mail Direct Write + Pump ×1000
    < Hybrid Isolate: Cached ActorId PostTo + Pump ×1000
```

---

# Part I：风险

## 22. class 化风险

```text
1. EventMail<TEvent>[] 默认元素是 null，必须初始化。
2. class 邮箱对象分散在堆上，缓存局部性可能下降。
3. 对象数量增加，内存占用会上升。
4. endpoint 持有 mail object，所以不能随便替换 mail object。
5. slot recycle 时必须更新 mail.AliveGeneration。
6. ClearMail 不能替换 mail object，只能清空内容。
```

---

## 23. 迁移建议

建议按以下提交顺序：

```text
Commit 1：
EventMail<TEvent> 改 class。
调整 EventColumn 初始化 / 扩容，保证每个 slot 有 mail object。
所有 ref EventMail<TEvent> 改为 EventMail<TEvent>。

Commit 2：
修 PostQueuedGrowCore / WriteQueued / EnsureMailAllocated / EventMailReader / ClearMail。
保证原 PostTo benchmark 正常。

Commit 3：
将 mail.AliveGeneration 接入 RefreshPostGenerations。
保证 Destroy / PendingDestroy / SlotRecycle 能让旧 endpoint 失效。

Commit 4：
新增 ActorQueuedGrowMailEndpoint<TEvent> 和 TryGetQueuedGrowMailEndpoint。

Commit 5：
新增 endpoint benchmark。
对比普通 PostTo 和 endpoint direct write。

Commit 6：
Projection / T4 后续接入 endpoint。
```

---

# 24. 最终结论

`EventMail<TEvent>` class 化的核心价值是：

```text
让 endpoint 能直接持有 mailbox object。
让高频 Post 从 ActorId 定位路径变成邮箱直写路径。
```

最终高频路径应该变成：

```text
ActorQueuedGrowMailEndpoint<TEvent>
→ EventMail<TEvent>
→ Buffer[Tail] = value
→ Tail / Count 更新
→ Dirty 标记
```

这就是 `Post -> 邮箱内存直写`。
