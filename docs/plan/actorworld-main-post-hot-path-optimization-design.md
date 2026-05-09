# ActorWorld Main Post Hot Path Optimization Design

> 文件名：`actorworld-main-post-hot-path-optimization-design.md`  
> 适用仓库：`avaw23112/LayerBase`  
> 目标：优化 `Actor.Post / ActorWorld.Post` 默认主热路径，降低 Post 入队成本，并保持预热后 `0B GC Alloc`。  
> 范围：`EventColumn.Post`、`EventMailWriter`、`EventMail<TEvent>`、`DirtySlotList`、`TypedActorStorage<TActor>`、`Query.PostAll`。  
> 不做内容：不引入旁路快路径 API，不引入 AttributeSystem / UI / Network / Save，不重写 ActorWorld 整体架构。

---

## 1. 优化目标

当前主热路径应聚焦这条链路：

```text
Actor.Post / ActorWorld.Post
  -> TypedActorStorage<TActor>.Post<TEvent>
  -> EventColumn<TActor,TEvent>.Post
  -> Mailbox 入队
  -> DirtySlot 标记
```

本次优化目标：

```text
1. 默认 Queued + Grow 路径不再进入通用策略分发。
2. slot 状态判断改为 Mask 快判。
3. mailbox 入队不再使用 % 计算 tail。
4. DirtySlotList 不再使用 HashSet<int>。
5. Query.PostAll 使用批量入队快路径。
6. 所有优化后路径保持 0B GC Alloc。
7. 不破坏 PendingDestroy / Destroying / Disabled / Alive 语义。
```

---

## 2. 优化总览

本次按以下顺序实施：

```text
1. TypedActorStorage 增加 ActorSlotFlags。
2. EventColumn 预计算 Post 快路径配置。
3. EventColumn 增加 PostQueuedGrowFast。
4. EventMail<TEvent> 增加 Tail 字段。
5. EventMailReader / EventMailWriter 使用 Tail，去掉主路径 %。
6. DirtySlotList 使用 bool[] 代替 HashSet<int>。
7. Query.PostAll 接入 PostToAliveSlotsFast。
8. ActorMailOptions.Default 改为性能默认。
9. 增加 correctness / allocation / benchmark 测试。
```

---

## 3. ActorSlotFlags：状态 Mask 快判

### 3.1 目标

将多次状态判断压缩为一次 flags 读取和少量位运算。

原先 Post 前置判断通常包含：

```text
GetSlotState(slotIndex)
slotState == PendingDestroy
slotState == Destroying
DisabledPolicy == Reject && !IsSlotEnabled(slotIndex)
```

优化后改为：

```text
flags = _slotFlags[slotIndex]
flags & rejectMask
```

---

### 3.2 新增 ActorSlotFlags

建议文件：

```text
LayerBase/Actor/Storage/ActorSlotFlags.cs
```

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor slot 状态位。
/// </summary>
[Flags]
internal enum ActorSlotFlags : byte
{
    /// <summary>
    /// 空状态。
    /// </summary>
    None = 0,

    /// <summary>
    /// slot 当前持有存活 Actor。
    /// </summary>
    Alive = 1 << 0,

    /// <summary>
    /// Actor 当前启用。
    /// </summary>
    Enabled = 1 << 1,

    /// <summary>
    /// Actor 已请求销毁，但尚未完成 Sweep。
    /// </summary>
    PendingDestroy = 1 << 2,

    /// <summary>
    /// Actor 正在执行销毁流程。
    /// </summary>
    Destroying = 1 << 3
}
```

---

### 3.3 TypedActorStorage 增加 flags 数组

目标文件：

```text
LayerBase/Actor/Storage/TypedActorStorage.cs
```

新增字段：

```csharp
private ActorSlotFlags[] _slotFlags;
```

在 Actor 创建、启用、禁用、PendingDestroy、Destroying、释放 slot 时同步维护 `_slotFlags`。

示例：

```csharp
private void MarkSlotAlive(int slotIndex)
{
    // slotIndex 参数：
    // 要标记为 Alive 的 Actor slot 下标。
    //
    // 作用说明：
    // Actor 创建成功后，该 slot 应同时具备 Alive 与 Enabled。
    _slotFlags[slotIndex] = ActorSlotFlags.Alive | ActorSlotFlags.Enabled;
}

private void MarkSlotPendingDestroy(int slotIndex)
{
    // slotIndex 参数：
    // 请求销毁的 Actor slot 下标。
    //
    // 作用说明：
    // PendingDestroy 用于阻止后续 Post 进入该 Actor 邮箱。
    _slotFlags[slotIndex] |= ActorSlotFlags.PendingDestroy;
}

private void MarkSlotDestroying(int slotIndex)
{
    // slotIndex 参数：
    // 正在执行销毁流程的 Actor slot 下标。
    //
    // 作用说明：
    // Destroying 表示该 Actor 不再接受普通事件投递。
    _slotFlags[slotIndex] |= ActorSlotFlags.Destroying;
}

private void ClearSlotFlags(int slotIndex)
{
    // slotIndex 参数：
    // 要释放的 Actor slot 下标。
    //
    // 作用说明：
    // slot 被释放后必须清空 flags，避免旧状态影响新 Actor。
    _slotFlags[slotIndex] = ActorSlotFlags.None;
}
```

---

### 3.4 增加 CanPostFast

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

internal sealed partial class TypedActorStorage<TActor>
    where TActor : class, IActor
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanPostFast(
        int slotIndex,
        ActorSlotFlags rejectMask,
        bool rejectDisabled)
    {
        // slotIndex 参数：
        // 目标 Actor 在当前 TypedActorStorage 中的 slot 下标。
        //
        // rejectMask 参数：
        // 当前 EventColumn 需要拒绝的状态位集合。
        // 常见值为 PendingDestroy | Destroying。
        //
        // rejectDisabled 参数：
        // true 表示 disabled Actor 也应拒绝投递。
        // false 表示 disabled Actor 仍然可以接收邮箱事件。
        //
        // 作用说明：
        // 该方法用于 EventColumn.Post 的主热路径。
        // 它将多次状态判断压缩成一次 flags 读取和少量位运算。

        if ((uint)slotIndex >= (uint)_slotFlags.Length)
        {
            return false;
        }

        ActorSlotFlags flags = _slotFlags[slotIndex];

        if ((flags & ActorSlotFlags.Alive) == 0)
        {
            return false;
        }

        if ((flags & rejectMask) != 0)
        {
            return false;
        }

        if (rejectDisabled && (flags & ActorSlotFlags.Enabled) == 0)
        {
            return false;
        }

        return true;
    }
}
```

---

## 4. EventColumn 预计算快路径配置

### 4.1 目标

将 Actor 邮箱策略判断从每次 Post 移到 `EventColumn` 创建期。

---

### 4.2 新增 ActorMailWriteMode

建议文件：

```text
LayerBase/Actor/Mail/ActorMailWriteMode.cs
```

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 邮箱写入模式。
/// </summary>
internal enum ActorMailWriteMode : byte
{
    /// <summary>
    /// 通用模式。
    /// 支持所有策略，但热路径成本最高。
    /// </summary>
    General = 0,

    /// <summary>
    /// 默认队列增长模式。
    /// 对应 Queued + Grow + RejectNew。
    /// </summary>
    QueuedGrow = 1,

    /// <summary>
    /// Latest 模式。
    /// 只保留最后一条事件。
    /// </summary>
    Latest = 2,

    /// <summary>
    /// Dirty 模式。
    /// 重复投递只保持一个 dirty 邮件。
    /// </summary>
    Dirty = 3,

    /// <summary>
    /// Coalesced 模式。
    /// 多次投递合并为一个事件。
    /// </summary>
    Coalesced = 4
}
```

---

### 4.3 EventColumn 构造时预计算

目标文件：

```text
LayerBase/Actor/Mail/EventColumn.cs
```

新增字段：

```csharp
private readonly ActorMailWriteMode _writeMode;
private readonly ActorSlotFlags _postRejectMask;
private readonly bool _rejectDisabled;
```

构造中初始化：

```csharp
public EventColumn(
    TypedActorStorage<TActor> owner,
    ActorBehaviourInvoker<TActor, TEvent> invoker,
    ActorMailOptions options,
    int initialSlotCapacity)
{
    // owner 参数：
    // 当前 EventColumn 所属的 TypedActorStorage。
    //
    // invoker 参数：
    // 事件出队后调用 ActorBehaviour 的委托。
    //
    // options 参数：
    // 当前 Actor 邮箱策略。
    //
    // initialSlotCapacity 参数：
    // 初始 slot 容量，用于预分配 mails 与 dirty slot 容量。

    _owner = owner;
    _invoker = invoker;
    _options = options;
    _mails = new EventMail<TEvent>[Math.Max(initialSlotCapacity, 1)];
    _bufferPool = new RingQueueBuffer<TEvent>();
    _dirtySlots = new DirtySlotList(Math.Max(initialSlotCapacity, 1));

    // 作用说明：
    // 将策略判断提前到 Column 创建期，避免每次 Post 都 switch。
    _writeMode = ResolveWriteMode(options);

    // 作用说明：
    // PendingDestroy 和 Destroying 默认拒绝投递。
    _postRejectMask = ActorSlotFlags.PendingDestroy | ActorSlotFlags.Destroying;

    // 作用说明：
    // disabled 是否拒绝投递由 ActorMailOptions 决定。
    _rejectDisabled = options.DisabledPolicy == ActorMailDisabledPolicy.Reject;
}
```

新增：

```csharp
private static ActorMailWriteMode ResolveWriteMode(in ActorMailOptions options)
{
    // options 参数：
    // 当前 EventColumn 的邮箱策略。
    //
    // 作用说明：
    // 将策略转换为内部写入模式。
    // 主热路径只关心 QueuedGrow，复杂策略继续走通用路径。

    if (options.PostPolicy == ActorPostPolicy.Queued &&
        options.FullPolicy == ActorMailFullPolicy.Grow &&
        options.GrowFailurePolicy == ActorMailFullPolicy.RejectNew)
    {
        return ActorMailWriteMode.QueuedGrow;
    }

    if (options.PostPolicy == ActorPostPolicy.Latest)
    {
        return ActorMailWriteMode.Latest;
    }

    if (options.PostPolicy == ActorPostPolicy.Dirty)
    {
        return ActorMailWriteMode.Dirty;
    }

    if (options.PostPolicy == ActorPostPolicy.Coalesced)
    {
        return ActorMailWriteMode.Coalesced;
    }

    return ActorMailWriteMode.General;
}
```

---

## 5. PostQueuedGrowFast：默认主热路径

### 5.1 目标

默认 `Queued + Grow + RejectNew` 直接内联入队，不再进入 `EventMailWriter.Enqueue` 的策略 switch。

---

### 5.2 修改 EventColumn.Post

```csharp
public PostResult Post(
    int slotIndex,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
{
    // slotIndex 参数：
    // 目标 Actor slot。
    //
    // value 参数：
    // 要投递的事件值。
    //
    // postPolicy 参数：
    // 可选投递策略覆盖。
    // null 表示使用当前 EventColumn 默认策略。
    //
    // fullPolicy 参数：
    // 可选邮箱满策略覆盖。
    // null 表示使用当前 EventColumn 默认策略。
    //
    // 作用说明：
    // 默认无覆盖策略时，QueuedGrow 进入内联快路径。
    // 其它情况保留完整通用路径。

    if (postPolicy == null &&
        fullPolicy == null &&
        _writeMode == ActorMailWriteMode.QueuedGrow)
    {
        return PostQueuedGrowFast(slotIndex, in value);
    }

    return PostGeneral(slotIndex, in value, postPolicy, fullPolicy);
}
```

将现有 `Post` 的完整逻辑移动到 `PostGeneral`：

```csharp
private PostResult PostGeneral(
    int slotIndex,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
{
    // slotIndex 参数：
    // 目标 Actor slot。
    //
    // value 参数：
    // 要投递的事件值。
    //
    // postPolicy / fullPolicy 参数：
    // 策略覆盖参数。
    //
    // 作用说明：
    // 该方法保留完整旧行为，用于复杂策略和失败回退。

    ActorSlotState slotState = _owner.GetSlotState(slotIndex);

    if (slotState == ActorSlotState.PendingDestroy)
    {
        return PostResult.Failure(
            ActorPostStatus.ActorPendingDestroy,
            "Actor is pending destroy.",
            PostFailureKind.PendingDestroy);
    }

    if (slotState == ActorSlotState.Destroying)
    {
        return PostResult.Failure(
            ActorPostStatus.ActorNotAlive,
            "Actor is destroying.",
            PostFailureKind.Destroying);
    }

    if (_options.DisabledPolicy == ActorMailDisabledPolicy.Reject
        && !_owner.IsSlotEnabled(slotIndex))
    {
        return PostResult.Failure(
            ActorPostStatus.ActorDisabledRejected,
            "Actor is disabled.",
            PostFailureKind.DisabledActor);
    }

    EnsureSlotCapacity(slotIndex);

    return EventMailWriter.Enqueue(
        ref _mails[slotIndex],
        in value,
        _bufferPool,
        _dirtySlots,
        slotIndex,
        _options,
        postPolicy,
        fullPolicy);
}
```

---

### 5.3 实现 PostQueuedGrowFast

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

internal sealed partial class EventColumn<TActor, TEvent>
    where TActor : class, IActor
    where TEvent : struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedGrowFast(int slotIndex, in TEvent value)
    {
        // slotIndex 参数：
        // 目标 Actor slot。
        //
        // value 参数：
        // 要写入 Actor 邮箱的事件值。
        //
        // 作用说明：
        // 这是默认主热路径。
        // 只处理 Queued + Grow + RejectNew。
        // 复杂策略、失败诊断和非默认覆盖策略均走 PostGeneral。

        if (!_owner.CanPostFast(slotIndex, _postRejectMask, _rejectDisabled))
        {
            return PostGeneral(slotIndex, in value, postPolicy: null, fullPolicy: null);
        }

        EnsureSlotCapacity(slotIndex);

        ref EventMail<TEvent> mail = ref _mails[slotIndex];

        EnsureMailAllocatedFast(ref mail);

        if (mail.Count >= mail.Capacity)
        {
            if (!TryGrowQueuedFast(ref mail))
            {
                return PostResult.Failure(
                    ActorPostStatus.MailFullRejected,
                    "Actor mail reached max capacity.",
                    PostFailureKind.MailboxFull);
            }
        }

        EnqueueFast(ref mail, in value, slotIndex);

        return PostResult.Success;
    }
}
```

---

### 5.4 EnsureMailAllocatedFast

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void EnsureMailAllocatedFast(ref EventMail<TEvent> mail)
{
    // mail 参数：
    // 当前 slot 的事件邮箱。
    //
    // 作用说明：
    // 仅在该 slot 第一次收到当前事件类型时分配 buffer。
    // 稳态下 BufferId != 0，该方法只做一次快速判断。

    if (mail.BufferId != 0)
    {
        return;
    }

    mail.BufferId = _bufferPool.Rent(_options.InitialCapacity);
    mail.Head = 0;
    mail.Tail = 0;
    mail.Count = 0;
    mail.Capacity = _bufferPool.GetCapacity(mail.BufferId);
}
```

---

### 5.5 EnqueueFast

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void EnqueueFast(
    ref EventMail<TEvent> mail,
    in TEvent value,
    int slotIndex)
{
    // mail 参数：
    // 当前 slot 的事件邮箱。
    //
    // value 参数：
    // 要写入邮箱的事件值。
    //
    // slotIndex 参数：
    // 当前 Actor slot。
    //
    // 作用说明：
    // 使用 Tail 指针写入，避免 (Head + Count) % Capacity。
    // 当 Count 从 0 变为 1 时，将 slot 加入 dirty list。

    _bufferPool.Write(mail.BufferId, mail.Tail, in value);

    mail.Tail++;
    if (mail.Tail == mail.Capacity)
    {
        mail.Tail = 0;
    }

    mail.Count++;

    if (mail.Count == 1)
    {
        _dirtySlots.AddIfNotExists(slotIndex);
    }
}
```

---

### 5.6 TryGrowQueuedFast

```csharp
private bool TryGrowQueuedFast(ref EventMail<TEvent> mail)
{
    // mail 参数：
    // 当前 slot 的事件邮箱。
    //
    // 作用说明：
    // 只处理 Grow + RejectNew。
    // 到达 MaxCapacity 后返回 false，由调用方返回 mailbox full。

    if (mail.Capacity >= _options.MaxCapacity)
    {
        return false;
    }

    int growFactor = Math.Max(_options.GrowFactor, 2);
    int nextCapacity = mail.Capacity * growFactor;

    if (nextCapacity <= mail.Capacity)
    {
        nextCapacity = mail.Capacity + 1;
    }

    nextCapacity = Math.Min(nextCapacity, _options.MaxCapacity);

    if (nextCapacity <= mail.Capacity)
    {
        return false;
    }

    _bufferPool.Resize(mail.BufferId, mail.Head, mail.Count, nextCapacity);
    mail.Head = 0;
    mail.Tail = mail.Count;
    mail.Capacity = nextCapacity;
    return true;
}
```

---

## 6. EventMail 增加 Tail 字段

### 6.1 目标

避免主路径使用：

```text
(mail.Head + mail.Count) % mail.Capacity
```

---

### 6.2 修改 EventMail<TEvent>

目标文件：

```text
LayerBase/Actor/Mail/EventMail.cs
```

```csharp
namespace LayerBase.Actor;

internal struct EventMail<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// RingQueueBuffer 中的 buffer id。
    /// </summary>
    public int BufferId;

    /// <summary>
    /// 队首下标。
    /// </summary>
    public int Head;

    /// <summary>
    /// 队尾写入下标。
    /// </summary>
    public int Tail;

    /// <summary>
    /// 当前邮箱中的事件数量。
    /// </summary>
    public int Count;

    /// <summary>
    /// 当前 buffer 容量。
    /// </summary>
    public int Capacity;
}
```

---

### 6.3 修改 EventMailReader.TryDequeue

```csharp
public static bool TryDequeue<TEvent>(
    ref EventMail<TEvent> mail,
    RingQueueBuffer<TEvent> bufferPool,
    out TEvent value)
    where TEvent : struct
{
    // mail 参数：
    // 当前 slot 的事件邮箱。
    //
    // bufferPool 参数：
    // 当前 EventColumn 的 buffer 池。
    //
    // value 参数：
    // 出队成功时返回事件值。
    //
    // 作用说明：
    // 出队只移动 Head。
    // Tail 只由入队移动。

    if (mail.Count <= 0 || mail.BufferId == 0)
    {
        value = default;
        return false;
    }

    value = bufferPool.Read(mail.BufferId, mail.Head);

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
```

---

### 6.4 修改 Resize 后 Tail

所有调用 `RingQueueBuffer.Resize` 后，必须设置：

```csharp
mail.Head = 0;
mail.Tail = mail.Count;
mail.Capacity = nextCapacity;
```

---

## 7. DirtySlotList 去 HashSet

### 7.1 目标

将 `HashSet<int>` 替换为 `bool[]`，避免哈希操作。

---

### 7.2 修改 DirtySlotList

目标文件：

```text
LayerBase/Actor/Mail/DirtySlotList.cs
```

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
        // 初始 slot 容量。
        //
        // 作用说明：
        // _items 是 dirty slot 队列。
        // _contains 用 slotIndex 直接判断是否已经在队列中。

        int capacity = Math.Max(initialCapacity, 4);
        _items = new int[capacity];
        _contains = new bool[capacity];
    }

    public void AddIfNotExists(int slotIndex)
    {
        // slotIndex 参数：
        // 有待处理邮件的 Actor slot。
        //
        // 作用说明：
        // slotIndex 是连续整数，因此 bool[] 比 HashSet<int> 更适合热路径去重。

        EnsureContainsCapacity(slotIndex + 1);

        if (_contains[slotIndex])
        {
            return;
        }

        _contains[slotIndex] = true;

        EnsureItemCapacity(_count + 1);

        int tail = _head + _count;
        if (tail >= _items.Length)
        {
            tail -= _items.Length;
        }

        _items[tail] = slotIndex;
        _count++;
    }

    public bool TryPeek(out int slotIndex)
    {
        // slotIndex 参数：
        // 输出队首 dirty slot。
        //
        // 作用说明：
        // TryPeek 只查看，不移除。

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
        // 移除队首 dirty slot，并清除 contains 标记。

        if (_count == 0)
        {
            return;
        }

        int slotIndex = _items[_head];

        if ((uint)slotIndex < (uint)_contains.Length)
        {
            _contains[slotIndex] = false;
        }

        _head++;
        if (_head == _items.Length)
        {
            _head = 0;
        }

        _count--;

        if (_count == 0)
        {
            _head = 0;
        }
    }

    public void MoveHeadToTail()
    {
        // 作用说明：
        // 当前队首暂时不能继续处理时，将其移动到队尾。
        // 这里不能清除 contains，因为该 slot 仍在 dirty list 中。

        if (_count <= 1)
        {
            return;
        }

        int headValue = _items[_head];

        _head++;
        if (_head == _items.Length)
        {
            _head = 0;
        }

        int tail = _head + _count - 1;
        if (tail >= _items.Length)
        {
            tail -= _items.Length;
        }

        _items[tail] = headValue;
    }

    private void EnsureItemCapacity(int required)
    {
        // required 参数：
        // dirty 队列需要容纳的元素数量。
        //
        // 作用说明：
        // 扩容时重新整理环形队列，使新数组从 0 开始连续。

        if (required <= _items.Length)
        {
            return;
        }

        int newCapacity = _items.Length * 2;
        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        int[] newItems = new int[newCapacity];

        for (int i = 0; i < _count; i++)
        {
            int index = _head + i;
            if (index >= _items.Length)
            {
                index -= _items.Length;
            }

            newItems[i] = _items[index];
        }

        _items = newItems;
        _head = 0;
    }

    private void EnsureContainsCapacity(int required)
    {
        // required 参数：
        // _contains 至少需要支持的 slot 数量。
        //
        // 作用说明：
        // slotIndex 直接作为数组下标，因此需要保证数组容量覆盖它。

        if (required <= _contains.Length)
        {
            return;
        }

        int newCapacity = _contains.Length * 2;
        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        Array.Resize(ref _contains, newCapacity);
    }
}
```

---

## 8. ActorMailOptions 默认改为性能默认

### 8.1 目标

默认配置应服务高频 Actor 邮箱。

---

### 8.2 修改 Default

目标文件：

```text
LayerBase/Actor/Mail/ActorMailOptions.cs
```

```csharp
public static ActorMailOptions Default => new(
    postPolicy: ActorPostPolicy.Queued,
    fullPolicy: ActorMailFullPolicy.Grow,
    growFailurePolicy: ActorMailFullPolicy.RejectNew,
    initialCapacity: 4,
    maxCapacity: 64,
    growFactor: 2,

    // 作用说明：
    // 默认不在邮箱空后释放 buffer。
    // 这样高频 Actor 下一次收到同类事件时可以复用已有 buffer。
    releaseWhenEmpty: false,

    disabledPolicy: ActorMailDisabledPolicy.Accept,
    pendingDestroyPolicy: ActorMailPendingDestroyPolicy.Reject);
```

新增：

```csharp
public static ActorMailOptions MemorySaving => new(
    postPolicy: ActorPostPolicy.Queued,
    fullPolicy: ActorMailFullPolicy.Grow,
    growFailurePolicy: ActorMailFullPolicy.RejectNew,
    initialCapacity: 4,
    maxCapacity: 64,
    growFactor: 2,

    // 作用说明：
    // 低频 Actor 或临时 Actor 可以使用该配置节省内存。
    releaseWhenEmpty: true,

    disabledPolicy: ActorMailDisabledPolicy.Accept,
    pendingDestroyPolicy: ActorMailPendingDestroyPolicy.Reject);
```

---

## 9. Query.PostAll 批量快路径

### 9.1 目标

让批量路径不再逐 slot 调用完整 `Post`。

---

### 9.2 EventColumn 新增 PostToAliveSlotsFast

```csharp
internal void PostToAliveSlotsFast(
    int maxSlot,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
{
    // maxSlot 参数：
    // 当前 TypedActorStorage 的有效 slot 上限。
    //
    // value 参数：
    // 要批量投递的事件值。
    //
    // postPolicy 参数：
    // 可选投递策略覆盖。
    // null 表示使用 EventColumn 默认策略。
    //
    // fullPolicy 参数：
    // 可选邮箱满策略覆盖。
    // null 表示使用 EventColumn 默认策略。
    //
    // 作用说明：
    // 默认 QueuedGrow 模式下，批量路径使用 EnqueueUnchecked。
    // 非默认策略回退完整 Post。

    bool useFastPath = postPolicy == null &&
                       fullPolicy == null &&
                       _writeMode == ActorMailWriteMode.QueuedGrow;

    for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
    {
        if (!_owner.CanPostFast(slotIndex, _postRejectMask, _rejectDisabled))
        {
            continue;
        }

        if (useFastPath)
        {
            EnsureSlotCapacity(slotIndex);
            ref EventMail<TEvent> mail = ref _mails[slotIndex];
            EnsureMailAllocatedFast(ref mail);

            if (mail.Count >= mail.Capacity)
            {
                if (!TryGrowQueuedFast(ref mail))
                {
                    continue;
                }
            }

            EnqueueFast(ref mail, in value, slotIndex);
        }
        else
        {
            _ = PostGeneral(slotIndex, in value, postPolicy, fullPolicy);
        }
    }
}
```

---

### 9.3 TypedActorStorage 接入

在 `TypedActorStorage<TActor>.PostToAliveActors<TEvent>` 中，将逐个 `column.Post` 改为：

```csharp
column.PostToAliveSlotsFast(
    maxSlot: _maxSlot,
    value: in value,
    postPolicy: postPolicy,
    fullPolicy: fullPolicy);
```

参数说明：

```text
_maxSlot：
当前 storage 的有效 slot 上限。

value：
要投递的事件。

postPolicy / fullPolicy：
外部覆盖策略。
如果为 null，并且 column 为 QueuedGrow，则走批量快路径。
```

---

## 10. 测试计划

### 10.1 Correctness Tests

必须新增或更新：

```text
1. Default Post 能投递并被 Pump 消费。
2. PendingDestroy Actor 拒绝默认快路径投递。
3. Destroying Actor 拒绝默认快路径投递。
4. DisabledPolicy.Reject 时 disabled Actor 拒绝投递。
5. 非默认 ActorPostPolicy 走通用路径。
6. 非默认 ActorMailFullPolicy 走通用路径。
7. mailbox 满时 Grow 正确。
8. mailbox 到达 MaxCapacity 后返回 MailFullRejected。
9. DirtySlotList 不重复添加同一 slot。
10. DirtySlotList Pop 后允许同一 slot 再次加入。
11. Query.PostAll 能投递到所有符合条件 Actor。
12. Query.PostAll 不投递到不可投递 Actor。
```

---

### 10.2 Allocation Tests

新增：

```text
1. ActorWorld Post only 预热后 0B。
2. ActorWorld Post + Pump 预热后 0B。
3. Query.PostAll + Pump 预热后 0B。
4. DirtySlotList 稳态 Add/Pop 0B。
5. 默认 QueuedGrow 快路径 0B。
```

---

### 10.3 Benchmark Tests

保留并观察：

```text
ActorWorld Post only - 200k
ActorWorld Post + Pump - 200k
ActorWorld Pump only - 200k
ActorWorld Query.PostAll + Pump - 1000 Actors
LayerBase PostScheduler
Dictionary<ActorId, Actor> + interface call
LayerBase Send
```

目标：

```text
1. ActorWorld Post only 明显下降。
2. ActorWorld Post + Pump 明显下降。
3. Pump only 不退化。
4. Query.PostAll 不退化，最好下降。
5. Allocated 仍为 0B。
```

---

## 11. DoD

完成标准：

```text
1. DirtySlotList 不再使用 HashSet<int>。
2. EventMail<TEvent> 增加 Tail，并且主路径不再使用 % 计算 tail。
3. EventColumn.Post 默认 QueuedGrow 走 PostQueuedGrowFast。
4. 非默认策略仍走通用 PostGeneral。
5. TypedActorStorage 使用 ActorSlotFlags 维护 slot 状态。
6. EventColumn 使用 CanPostFast 做状态快判。
7. ActorMailOptions.Default.releaseWhenEmpty = false。
8. ActorMailOptions.MemorySaving.releaseWhenEmpty = true。
9. Query.PostAll 接入 PostToAliveSlotsFast。
10. 所有 correctness 测试通过。
11. 所有 allocation 测试通过。
12. 所有 benchmark 仍保持 Allocated = 0B。
13. ActorWorld Post only 用时明显下降。
```

---

## 12. 禁止事项

本次任务不要做：

```text
1. 不引入新的外部 API 快路径。
2. 不删除 ActorWorld.Post / Actor.Post。
3. 不删除 PostResult。
4. 不删除 PendingDestroy / Destroying / Disabled 语义。
5. 不跳过 Actor alive 检查。
6. 不引入 unsafe 指针。
7. 不引入 AttributeSystem。
8. 不引入 UI / Network / Save。
9. 不为了 benchmark 特化业务 Actor。
10. 不重写 ActorWorld 整体架构。
```

---

## 13. 推荐执行顺序

```text
1. DirtySlotList: HashSet<int> -> bool[]。
2. EventMail<TEvent>: 增加 Tail 字段。
3. EventMailReader: 出队移动 Head，清空时重置 Head/Tail。
4. EventMailWriter: Queued 路径改 Tail 写入。
5. TypedActorStorage: 增加 ActorSlotFlags 并同步维护。
6. TypedActorStorage: 增加 CanPostFast。
7. EventColumn: 增加 ActorMailWriteMode 与预计算字段。
8. EventColumn: 拆 PostGeneral。
9. EventColumn: 实现 PostQueuedGrowFast。
10. ActorMailOptions.Default 改 releaseWhenEmpty=false。
11. Query.PostAll 接入 PostToAliveSlotsFast。
12. 添加 correctness tests。
13. 添加 allocation tests。
14. 运行 benchmark 对比。
```

---

## 14. 最终定位

优化完成后，ActorWorld 的默认主路径应变成：

```text
状态 Mask 快判
-> QueuedGrow 内联入队
-> Tail 指针写入
-> DirtySlot bool[] 标记
-> 0B GC Alloc
```

最终目标：

```text
ActorWorld.Post 不依赖旁路 API 也能显著变快。
Query.PostAll 继续承担批量优势。
复杂策略仍保留完整通用路径。
```
