 # ActorPost Safety And Memory Risk Review

## 1. 目标

本文档用于评估 LayerBase 当前 ActorPost 与 EventMail Buffer 缓存方案中的逻辑安全风险和内存滞留风险。

当前前提：

```text
ActorWorld 单线程运行。
ActorPost 采用 PhysicalSafe Only 语义。
PostTo 只负责把事件写入 EventMail。
Actor 是否执行事件由 Pump / Sweep 决定。
EventMail<TEvent> 已缓存 TEvent[]? Buffer 引用以减少热路径间接寻址。
```

本文重点检查：

```text
旧 ActorId 是否可能写入新 Actor。
EventMail.Buffer 是否可能悬挂或失效。
RingQueueBuffer 是否可能保留引用型事件导致内存滞留。
World Dispose 是否可能残留泛型静态状态。
Release / Resize / Pump 是否破坏 EventMail 不变量。
```

---

## 2. 当前结论

当前 Buffer 缓存方案整体方向是可行的。

已经成立的不变量：

```text
EventMail<TEvent> 同时保存 BufferId 和 Buffer。
EnsureMailAllocated 会同步写入 BufferId / Buffer / Capacity。
TryGrow 成功后会同步 mail.Buffer。
TryDequeue 会优先从 mail.Buffer 读取。
ReleaseIfEmpty / ForceRelease 最终通过 mail = default 清空 Buffer 引用。
```

主要风险有两个：

```text
1. 旧 ActorId 在 slot 被复用后，可能把消息写入新 Actor。
2. RingQueueBuffer.Release 不清理数组内容时，包含引用字段的 TEvent 可能导致对象长期滞留。
```

次要风险：

```text
ActorWorld Dispose 如果没有执行 EventPostRuntime<TEvent>.UnbindWorld，会导致泛型静态数组长期持有 EventPostState。
Latest / Dirty 的 SingleValue 与 Buffer 混合模式必须保持 BufferId / Buffer 不变量。
旧 Write / Read API 与新 Buffer 直写路径并存，后续维护时容易误用。
```

---

## 3. 风险一：旧 ActorId 写入新 Actor

### 3.1 问题描述

当前 PhysicalSafe Only 设计下，`PostTo` 只通过：

```text
ActorId.ArchetypeId
ActorId.SlotIndex
```

定位物理邮箱。

如果不检查 `ActorId.Generation`，就会出现以下情况：

```text
Actor A 创建，获得 ActorId(A)
Actor A 销毁
Actor A 的 slot 被 Actor B 复用
外部仍然保存旧 ActorId(A)
外部调用 PostTo(oldActorId, event)
PostTo 只检查 ArchetypeId / SlotIndex
事件被写入 Actor B 的邮箱
Actor B 可能执行本来属于 Actor A 的消息
```

这是当前最危险的逻辑风险。

它不是内存泄露，而是消息投递语义错误。

---

### 3.2 可接受前提

如果框架明确规定：

```text
ActorId 只能在当前生命周期内使用。
ActorId 不能长期缓存。
ActorId 不能跨 Destroy / Sweep 后继续投递。
旧 ActorId 写入复用 slot 属于未定义行为。
```

那么可以继续保持 PhysicalSafe Only。

但如果 ActorId 是对外公开的稳定句柄，就不建议完全去掉 generation 检查。

---

### 3.3 推荐方案

推荐采用双路径设计：

```text
公开 PostTo:
  检查 ActorId.Generation，避免旧 ActorId 写入新 Actor。

内部 Query.PostAll:
  保持 PhysicalSafe Only，不检查 Generation。

框架内部受控路径:
  可以继续使用 PhysicalSafe / Unchecked 快路径。
```

这样可以同时保留：

```text
公开 API 的安全性。
Query.PostAll 的高性能。
内部批量路径的无额外检查优势。
```

---

### 3.4 建议新增 TryGetPhysicalRowWithGeneration

示例代码：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool TryGetPhysicalRowWithGeneration<TEvent>(
    ActorId actorId,
    EventPostState<TEvent> state,
    out EventPostRow<TEvent> row,
    out int slotIndex)
    where TEvent : struct
{
    // actorId 参数作用：
    // 外部传入的目标 Actor 句柄。
    // 除了 ArchetypeId / SlotIndex 外，还需要验证 Generation。

    // state 参数作用：
    // 当前事件的编译后投递状态。
    // 提供 RowsByArchetype 以定位 EventPostRow。

    // row 参数作用：
    // 输出目标 Archetype + TEvent 的邮箱定位信息。

    // slotIndex 参数作用：
    // 输出目标 slot 下标。

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

    return IsActorGenerationAlive(
        actorId);
}
```

`IsActorGenerationAlive` 可放在 `ActorWorld` 或 `BehaviourArchetype` 中：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool IsActorGenerationAlive(
    ActorId actorId)
{
    // actorId 参数作用：
    // 要验证的 Actor 句柄。
    // 方法确认该 ActorId 仍然对应当前 slot 的当前 generation。

    if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
    {
        return false;
    }

    return _archetypes[actorId.ArchetypeId]
        .IsCurrentGeneration(actorId);
}
```

`BehaviourArchetype` 中：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal bool IsCurrentGeneration(
    ActorId actorId)
{
    // actorId 参数作用：
    // 要验证的 Actor 句柄。

    if (!TryGetStorage(out TypedStorageRuntime? storage)
        || storage == null)
    {
        return false;
    }

    return storage.IsCurrentGeneration(actorId);
}
```

`TypedStorageRuntime` 抽象方法：

```csharp
internal abstract bool IsCurrentGeneration(
    ActorId actorId);
```

`TypedActorStorage<TActor>` 实现：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal override bool IsCurrentGeneration(
    ActorId actorId)
{
    // actorId 参数作用：
    // 要验证的 Actor 句柄。
    // 只要 slot 越界、generation 不一致、slot 不是 Alive，就返回 false。

    int slotIndex = actorId.SlotIndex;

    return (uint)slotIndex < (uint)_generations.Length
           && _generations[slotIndex] == actorId.Generation
           && _states[slotIndex] == ActorSlotState.Alive;
}
```

---

### 3.5 PostTo 使用建议

公开 `PostTo` 使用 generation 检查版本：

```csharp
if (!TryGetPhysicalRowWithGeneration(
        actorId,
        state,
        out EventPostRow<TEvent> row,
        out int slotIndex))
{
    return BuildPostFailureCold(actorId);
}
```

Query / PostAll 内部路径继续使用不带 generation 的物理路径。

---

## 4. 风险二：引用型事件在 RingQueueBuffer 中滞留

### 4.1 问题描述

`RingQueueBuffer<TEvent>.Release(bufferId)` 当前只标记：

```text
_inUse[index] = false
_freeIds.Push(bufferId)
```

如果 `_buffers[index]` 中保存的是包含引用字段的结构体，例如：

```csharp
public struct SomeEvent
{
    public object Payload;
    public string Name;
}
```

即使 `EventMail<TEvent>` 已经 `mail = default`，池中的 `TEvent[]` 仍然持有旧元素，旧对象就可能无法被 GC 回收。

这属于内存滞留风险。

---

### 4.2 推荐修复

在 `RingQueueBuffer.Release` 中，对包含引用字段的事件类型清理数组：

```csharp
using System.Runtime.CompilerServices;

public void Release(
    int bufferId)
{
    // bufferId 参数作用：
    // 要释放的 1-based buffer 编号。
    // 释放后该编号会进入 free list，等待后续复用。

    if (bufferId <= 0 || bufferId > _buffers.Length)
    {
        return;
    }

    int index = bufferId - 1;
    if (!_inUse[index])
    {
        return;
    }

    if (RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>())
    {
        TEvent[]? buffer = _buffers[index];
        if (buffer != null)
        {
            Array.Clear(
                buffer,
                0,
                buffer.Length);
        }
    }

    _inUse[index] = false;
    _freeIds.Push(bufferId);
}
```

说明：

```text
RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>() 为 false 时，不执行 Array.Clear。
纯数值事件不会承担清数组成本。
含引用字段事件释放时会清空旧引用，避免对象滞留。
```

---

### 4.3 是否需要每次 Dequeue 清理单个元素

如果事件可能包含引用字段，最彻底的做法是在 `TryDequeue` 读取后清空对应 slot：

```csharp
if (RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>())
{
    buffer[oldHead] = default;
}
```

但这会影响 Pump 热路径。

推荐策略：

```text
默认只在 Release 时清理整个 buffer。
如果某类事件包含大对象引用，并且 mailbox 长期不 Release，则再考虑 Dequeue 时清理单个元素。
```

对于当前游戏运行时场景，建议优先保持事件为纯值类型：

```text
int
float
ActorId
EntityId
small struct
```

避免在高频事件中携带 object / string / class 引用。

---

## 5. 风险三：EventPostRuntime<TEvent> 静态状态泄露

### 5.1 问题描述

`EventPostRuntime<TEvent>` 使用泛型静态数组：

```text
EventPostState<TEvent>?[] s_statesByWorld
```

如果 ActorWorld Dispose 时没有调用：

```text
EventPostRuntime<TEvent>.UnbindWorld(RuntimeIndex)
```

那么静态数组会长期持有 `EventPostState<TEvent>`。

`EventPostState<TEvent>` 会进一步持有：

```text
EventMailPool<TEvent>
RowsByArchetype
EventPostRow<TEvent>
Mails
DirtySlotList
```

这会导致 World 相关对象无法被回收。

---

### 5.2 当前结构

`ActorWorld.GetOrCreateEventPostState<TEvent>` 会在创建 state 后注册 unbinder：

```text
_eventPostRuntimeUnbinders.Add(() => EventPostRuntime<TEvent>.UnbindWorld(RuntimeIndex))
```

这是正确方向。

关键是 `ActorWorld.Dispose()` 必须执行这些 unbinder。

---

### 5.3 推荐 Dispose 逻辑

```csharp
public void Dispose()
{
    if (_state == ActorWorldState.Disposed)
    {
        return;
    }

    _state = ActorWorldState.Disposed;

    foreach (Action unbinder in _eventPostRuntimeUnbinders)
    {
        unbinder();
    }

    _eventPostRuntimeUnbinders.Clear();

    GlobalEventMailPools.Clear();

    _archetypes = Array.Empty<BehaviourArchetype>();
    _archetypeMap.Clear();
    _queryCacheByDescriptor.Clear();
    _eventBucketsByEventId = Array.Empty<IActorEventBucket>();
    _callBucketsByRouteId = Array.Empty<IActorEventBucket>();

    ActorWorldRuntimeIndexAllocator.Return(RuntimeIndex);
}
```

说明：

```text
UnbindWorld:
  解除泛型静态数组对 EventPostState 的引用。

GlobalEventMailPools.Clear:
  解除 world 对所有 EventMailPool 的引用。

清空 archetype / bucket / query:
  解除 world 对 storage、event columns、dirty lists 的引用。

Return RuntimeIndex:
  归还 world runtime index。
```

---

## 6. Buffer 缓存不变量

必须长期保持以下不变量。

### 6.1 分配不变量

```text
mail.BufferId != 0 => mail.Buffer != null
mail.Buffer != null => mail.Capacity == mail.Buffer.Length
```

### 6.2 空邮箱不变量

```text
mail.Count == 0:
  可以保留 Buffer。
  如果 ReleaseWhenEmpty=false，Buffer 可继续复用。
  如果 ReleaseWhenEmpty=true，Pump 后应 Release 并 mail = default。
```

### 6.3 SingleValue 不变量

```text
mail.Buffer == null && mail.Count > 0:
  表示 SingleValue 模式。

mail.Buffer != null:
  表示 Buffer 队列模式。
```

### 6.4 扩容不变量

```text
pool.TryGrow(ref mail) 成功后:
  mail.Buffer 指向新数组。
  mail.Head = 0。
  mail.Tail = mail.Count。
  mail.Capacity = mail.Buffer.Length。
```

### 6.5 释放不变量

```text
ReleaseIfEmpty 且 ReleaseWhenEmpty=true:
  bufferPool.Release(mail.BufferId)
  mail = default

ForceRelease:
  bufferPool.Release(mail.BufferId)
  mail = default
```

---

## 7. Latest / Dirty 的注意点

`Latest` 与 `Dirty` 当前存在两种存储形态：

```text
BufferId == 0:
  使用 SingleValue。

BufferId != 0:
  使用 mail.Buffer![0]。
```

这可以工作，但需要注意：

```text
未来不要出现 BufferId != 0 但 Buffer == null。
未来不要只清 Buffer 而不清 BufferId。
未来不要只清 BufferId 而不清 Buffer。
```

建议在 Debug 模式下加断言：

```csharp
[Conditional("DEBUG")]
private static void AssertMailBufferInvariant<TEvent>(
    in EventMail<TEvent> mail)
    where TEvent : struct
{
    if (mail.BufferId != 0 && mail.Buffer == null)
    {
        throw new InvalidOperationException(
            "EventMail invariant broken: BufferId is set but Buffer is null.");
    }

    if (mail.Buffer != null && mail.Capacity != mail.Buffer.Length)
    {
        throw new InvalidOperationException(
            "EventMail invariant broken: Capacity does not match Buffer.Length.");
    }
}
```

在 `EnsureMailAllocated`、`TryGrow`、`ReleaseIfEmpty` 的 debug 路径中调用即可。

---

## 8. 旧 API 并存风险

当前 `EventMailPool<TEvent>` 仍保留：

```text
Write(bufferId, index, in value)
Read(bufferId, index)
Resize(bufferId, head, count, newCapacity)
```

同时新路径使用：

```text
mail.Buffer![index] = value
buffer[index]
ResizeWithBuffer(...)
```

这不是错误，但存在维护风险。

推荐注释约束：

```text
Post / Pump 热路径优先使用 EventMail.Buffer。
pool.Write / pool.Read 仅作为兼容路径或低频工具路径。
任何 Resize 影响现有 mail 时，必须使用 ResizeWithBuffer 并同步 mail.Buffer。
```

如果后续确定不再需要旧 API，可以逐步删除 `Write / Read / Resize`，减少误用。

---

## 9. 建议新增测试

### 9.1 旧 ActorId 不应写入新 Actor

测试目标：

```text
销毁 Actor A。
复用同一个 slot 创建 Actor B。
使用旧 ActorId(A) PostTo。
确认事件不会被 Actor B 执行。
```

如果保持 PhysicalSafe Only，则该测试应写入文档标记为未定义行为。

---

### 9.2 ReleaseWhenEmpty 清理 Buffer

测试目标：

```text
releaseWhenEmpty = true
Post 一条事件
Pump 消费
确认 mail.BufferId == 0
确认 mail.Buffer == null
再次 Post
确认重新 Rent 且可正常 Pump
```

---

### 9.3 TryGrow 后 Buffer 指向新数组

测试目标：

```text
initialCapacity = 4
maxCapacity = 8
连续 Post 5 条
确认 mail.Buffer.Length == 8
确认 Pump 顺序仍然正确
```

---

### 9.4 引用型事件释放后不滞留对象

测试目标：

```text
定义 struct RefEvent { public object Payload; }
Post RefEvent
Pump 消费
ReleaseWhenEmpty = true
强制 GC
确认 WeakReference 不再存活
```

前提：

```text
RingQueueBuffer.Release 对包含引用的 TEvent 执行 Array.Clear。
```

---

### 9.5 Latest / Dirty 模式正常工作

测试目标：

```text
Latest 连续 Post 多次，只消费最后一条。
Dirty 连续 Post 多次，只消费第一条或预期合并结果。
SingleValue 模式和 Buffer 模式都要覆盖。
```

---

## 10. 推荐修复优先级

### P0：必须尽快决定

```text
旧 ActorId 是否允许写入复用 slot。
```

推荐：

```text
公开 PostTo 检查 generation。
Query.PostAll 保持 PhysicalSafe。
内部快速路径保持 PhysicalSafe。
```

### P1：建议立即修复

```text
RingQueueBuffer.Release 对包含引用的 TEvent 清理数组。
```

原因：

```text
这是明确的内存滞留风险。
对纯值类型事件无额外成本。
```

### P2：建议确认

```text
ActorWorld.Dispose 必须执行所有 EventPostRuntime unbinders。
GlobalEventMailPools.Clear 必须被调用。
RuntimeIndex 必须归还。
```

### P3：后续整理

```text
给 EventMail 不变量加 Debug 断言。
给旧 pool.Write / pool.Read API 加注释或逐步删除。
补充 Release / Resize / old ActorId 测试。
```

---

## 11. 最终判断

当前 Buffer 缓存优化没有明显破坏主路径一致性。

可以继续保留：

```text
EventMail.Buffer
RentWithBuffer
ResizeWithBuffer
TryGrow 同步 Buffer
TryDequeue 直接读 Buffer
WriteQueued 直接写 Buffer
```

但在进入稳定版本前，建议至少修复：

```text
1. 引用型事件在 Release 时清理 buffer。
2. 公开 PostTo 的旧 ActorId 写入复用 slot 风险。
3. ActorWorld Dispose 的静态解绑确认。
```

如果这三点处理好，这版 ActorPost 的安全性和内存模型就比较稳。
