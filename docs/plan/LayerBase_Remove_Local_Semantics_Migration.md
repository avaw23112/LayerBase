# LayerBase 删除 Local 语义修改文档

## 1. 修改目标

本次修改的目标是删除事件系统中的 `Local` 语义，只保留全局事件派发模型。

修改前，事件发送侧同时存在：

```csharp
Send<T>(in T value)
SendLocal<T>(int layerIndex, in T value)

Post<T>(in T value)
PostLocal<T>(int layerIndex, in T value)
```

修改后，只保留：

```csharp
Send<T>(in T value)
Post<T>(in T value)
```

也就是说，事件发送侧不再区分 `Global` 和 `Local`。

---

## 2. 新语义

删除 `Local` 后，事件系统语义调整为：

```text
事件可见性：由事件类型 TEvent 决定
处理顺序：由 Layer 决定
发送方式：只区分 Send 和 Post
事件类型 ID：由 EventTypeId<TEvent>.Id 提供
```

### 2.1 事件可见性

事件可见性指：某个事件发布后，哪些订阅者可以收到它。

修改后：

```text
只要订阅者订阅了同一个事件类型 TEvent，就可以收到该事件。
```

不再支持：

```text
只让某一个指定 Layer 收到事件。
```

---

### 2.2 Layer 的职责

`Layer` 仍然保留。

但它不再表示“事件局部可见范围”，而只表示：

```text
事件处理阶段
事件处理顺序
异步事件 Pump 时机
模块生命周期归属
```

也就是说，`Layer` 负责“什么时候处理”，不负责“能不能收到”。

---

### 2.3 Send 和 Post 的职责

保留两个发送入口：

```text
Send<T>：立即同步派发事件
Post<T>：投递事件，稍后由事件队列处理
```

`Send` 和 `Post` 的差异只在于同步与异步，不再叠加 `Local` 语义。

---

## 3. 删除原因

### 3.1 Local 语义使用价值不高

`Local` 语义的核心能力是：

```text
只向指定 Layer 派发事件
```

但在当前 LayerBase 的事件模型中，`Layer` 更适合承担执行顺序和处理阶段的职责。

如果同时让 `Layer` 参与事件可见性控制，会导致语义变复杂。

---

### 3.2 API 数量膨胀

删除前，发送侧至少有四类入口：

```csharp
Send<T>()
SendLocal<T>()
Post<T>()
PostLocal<T>()
```

这会让使用者必须判断：

```text
这个事件该全局发，还是局部发？
这个事件该同步发，还是异步发？
Local 到底是当前对象、当前 Layer，还是当前队列？
```

删除后，发送侧只需要判断：

```text
立即处理：Send<T>
稍后处理：Post<T>
```

使用成本更低。

---

### 3.3 内部分发逻辑可以简化

删除 `Local` 后，事件桶内部不再需要维护两套分发路径：

```text
完整分发路径
指定 Layer 分发路径
```

可以统一为：

```text
按事件类型找到 EventBucket<T>
按 Layer 顺序扫描订阅者
依次执行符合条件的 handler
```

---

## 4. 涉及的新名词说明

### 4.1 EventBucket

`EventBucket` 指某一个事件类型对应的订阅者容器。

例如：

```text
DamageEvent -> EventBucket<DamageEvent>
MoveEvent   -> EventBucket<MoveEvent>
```

它负责保存该事件类型下的所有处理函数。

---

### 4.2 Handler

`Handler` 指事件处理函数。

例如：

```csharp
void OnDamage(DamageEvent value)
{
}
```

这里的 `OnDamage` 就是一个 handler。

---

### 4.3 Pump

`Pump` 指从事件队列中取出已经投递的事件，并执行对应处理逻辑。

在 `Post<T>` 模型中，事件不是立即处理，而是先进入队列，之后由某个时机统一 `Pump`。

---

### 4.4 Breaking Change

`Breaking Change` 指破坏性修改。

删除公开 API 后，旧代码可能无法编译，例如：

```csharp
PostLocal<DamageEvent>(layerIndex, value);
```

如果项目已经发布 NuGet 包，删除 `Local` API 应当提升主版本号，例如从 `1.x` 升到 `2.0.0`。

---

## 5. 建议删除的 API

### 5.1 GlobalEventCenter

删除：

```csharp
internal EventHandledState SendLocal<T>(int layerIndex, in T value) where T : struct;
internal void PostLocal<T>(int layerIndex, in T value) where T : struct;
internal EventHandledState DispatchLocal<T>(int layerIndex, in Event<T> @event) where T : struct;
```

保留：

```csharp
internal EventHandledState Send<T>(in T value) where T : struct;
internal void Post<T>(in T value) where T : struct;
```

---

### 5.2 EventBucket<T>

删除：

```csharp
DispatchLocal(int layerIndex, in T value)
PostLocal(int layerIndex, in T value)
DispatchLocal(int layerIndex, in Event<T> @event)
```

保留或整理为：

```csharp
Dispatch(in T value)
Post(in T value)
Dispatch(in Event<T> @event)
```

---

### 5.3 上层 Layer API

如果存在以下公开包装，也建议删除：

```csharp
Layer.SendLocal<T>(...)
Layer.PostLocal<T>(...)
Event.SendLocal<T>(...)
Event.PostLocal<T>(...)
```

统一替换为：

```csharp
Send<T>(...)
Post<T>(...)
```

---

## 6. 替换规则

### 6.1 SendLocal 替换

修改前：

```csharp
// layerIndex：目标 Layer 的索引。
// value：要发送的事件数据。
SendLocal<TEvent>(layerIndex, in value);
```

修改后：

```csharp
// value：要发送的事件数据。
// 删除 Local 后，事件会按事件类型 TEvent 派发给所有订阅者。
// Layer 只影响处理顺序，不再限制可见范围。
Send<TEvent>(in value);
```

---

### 6.2 PostLocal 替换

修改前：

```csharp
// layerIndex：目标 Layer 的索引。
// value：要投递的事件数据。
PostLocal<TEvent>(layerIndex, in value);
```

修改后：

```csharp
// value：要投递的事件数据。
// 删除 Local 后，事件进入对应事件类型的队列。
// 后续由事件系统按 Layer 顺序 Pump。
Post<TEvent>(in value);
```

---

### 6.3 DispatchLocal 替换

修改前：

```csharp
// layerIndex：目标 Layer 的索引。
// eventValue：已经包装后的事件对象。
DispatchLocal<TEvent>(layerIndex, in eventValue);
```

修改后：

```csharp
// eventValue：已经包装后的事件对象。
// 删除 Local 后，不再指定目标 Layer。
// Dispatch 内部统一按 Layer 顺序分发。
Dispatch<TEvent>(in eventValue);
```

---

## 7. 推荐后的 GlobalEventCenter 发送入口

```csharp
using System.Runtime.CompilerServices;
using System.Threading;

namespace LayerBase.Core.Event;

public sealed partial class GlobalEventCenter
{
    /// <summary>
    /// 立即同步发送事件。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// 例如 DamageEvent、MoveEvent、TurnStartedEvent。
    /// </typeparam>
    /// <param name="value">
    /// 要发送的事件数据。
    /// 使用 in 参数可以避免较大 struct 在调用时被复制。
    /// </param>
    /// <returns>
    /// 事件处理状态。
    /// 通常用于表示事件是否继续传播。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState Send<TEvent>(in TEvent value) where TEvent : struct
    {
        // _isResetting：
        // 表示事件中心当前是否正在重置。
        // 如果正在重置，直接跳过派发，避免访问已经释放或正在清理的数据结构。
        if (Volatile.Read(ref _isResetting) == 1)
        {
            return EventHandledState.Continue;
        }

        // BucketCache<TEvent>.Instance：
        // 每个事件类型 TEvent 自己的静态 bucket 缓存。
        // 命中缓存时，可以绕过 ConcurrentDictionary 查找。
        var cached = BucketCache<TEvent>.Instance;

        // cached.Owner == this：
        // 确认缓存属于当前 GlobalEventCenter 实例。
        // 如果项目中存在多个事件中心实例，这个判断可以避免错用其他实例的缓存。
        if (cached != null && cached.Owner == this)
        {
            return cached.Dispatch(in value);
        }

        // 缓存未命中时，创建或获取当前事件类型对应的 EventBucket<TEvent>。
        return GetBucket<TEvent>().Dispatch(in value);
    }

    /// <summary>
    /// 异步投递事件。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// </typeparam>
    /// <param name="value">
    /// 要投递的事件数据。
    /// 该事件不会在当前调用点立即完整处理，而是进入事件队列。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Post<TEvent>(in TEvent value) where TEvent : struct
    {
        // _isResetting：
        // 如果事件中心正在重置，则忽略本次投递。
        if (Volatile.Read(ref _isResetting) == 1)
        {
            return;
        }

        // 优先使用泛型静态缓存，降低事件投递路径上的字典访问。
        var cached = BucketCache<TEvent>.Instance;

        // 如果缓存命中，并且属于当前事件中心实例，则直接投递到对应 bucket。
        if (cached != null && cached.Owner == this)
        {
            cached.Post(in value);
            return;
        }

        // 缓存未命中时，获取 bucket 后再投递。
        GetBucket<TEvent>().Post(in value);
    }
}
```

---

## 8. 推荐后的 GetBucket<TEvent>

```csharp
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace LayerBase.Core.Event;

public sealed partial class GlobalEventCenter
{
    /// <summary>
    /// 获取指定事件类型对应的事件桶。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件数据类型。
    /// </typeparam>
    /// <returns>
    /// 当前事件类型对应的 EventBucket<TEvent>。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventBucket<TEvent> GetBucket<TEvent>() where TEvent : struct
    {
        // BucketCache<TEvent>.Instance：
        // 当前事件类型自己的静态 bucket 缓存。
        // 每个 TEvent 都有独立缓存，不会和其他事件类型混用。
        var cached = BucketCache<TEvent>.Instance;

        // Owner：
        // 表示该 bucket 属于哪个 GlobalEventCenter 实例。
        // 多实例场景下必须检查 Owner，避免拿到其他事件中心的 bucket。
        if (cached != null && cached.Owner == this)
        {
            return cached;
        }

        // EventTypeId<TEvent>.Id：
        // 当前事件类型的静态 int ID。
        // 该 ID 用作 _eventBuckets 的 key。
        var typeId = EventTypeId<TEvent>.Id;

        // _bucketCacheResetters：
        // 保存每个事件类型的缓存重置函数。
        // Reset 时会把 BucketCache<TEvent>.Instance 清空。
        _bucketCacheResetters.TryAdd(typeId, static () => BucketCache<TEvent>.Instance = null);

        // _eventBuckets：
        // 事件类型 ID 到事件桶的映射。
        // GetOrAdd 表示：存在就取出，不存在就创建。
        var bucket = (EventBucket<TEvent>)_eventBuckets.GetOrAdd(
            typeId,
            // _：
            // ConcurrentDictionary 传入的 key。
            // 这里不需要使用 key 本身，所以命名为 _。
            _ => new EventBucket<TEvent>(this));

        // 将 bucket 写入泛型静态缓存。
        // 下次相同 TEvent 访问时，可以跳过字典。
        BucketCache<TEvent>.Instance = bucket;

        return bucket;
    }
}
```

---

## 9. EventBucket<TEvent> 的推荐派发模型

删除 `Local` 后，`EventBucket<TEvent>` 内部可以统一为完整派发。

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Core.Event;

internal sealed partial class EventBucket<TEvent> where TEvent : struct
{
    /// <summary>
    /// 同步派发事件。
    /// </summary>
    /// <param name="value">
    /// 要派发的事件数据。
    /// </param>
    /// <returns>
    /// 事件处理状态。
    /// 如果某个 Flow handler 截断事件，可以返回对应状态。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState Dispatch(in TEvent value)
    {
        // EnsureClean：
        // 确保订阅者缓存是最新的。
        // 如果订阅关系发生过变化，这里会触发 Rebuild。
        EnsureClean();

        // 后续逻辑统一扫描当前事件类型下的所有 Layer 分段。
        // 删除 Local 后，不再接受 layerIndex 参数。
        return DispatchAllLayers(in value);
    }

    /// <summary>
    /// 投递事件。
    /// </summary>
    /// <param name="value">
    /// 要投递的事件数据。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post(in TEvent value)
    {
        // Post 的职责是将事件送入队列，而不是立即执行所有 handler。
        // 具体队列结构可以继续复用现有 LayerEventQueue。
        EnqueueToLayerQueues(in value);
    }
}
```

---

## 10. LayerEventQueue 的修改方向

如果删除 `PostLocal<T>`，`LayerEventQueue` 不再需要表达“只投递到指定 Layer”。

推荐语义：

```text
Post<TEvent>
-> EventBucket<TEvent>.Post
-> 根据订阅者所在 Layer 唤醒对应 Layer
-> 对应 Layer Pump 时处理自己的待处理事件
```

也就是说，`LayerEventQueue` 仍然可以存在，但它只负责异步处理节奏，不再负责 Local 可见性。

---

## 11. 测试修改建议

### 11.1 删除或改写 Local 测试

删除这类测试：

```text
SendLocal 只触发指定 Layer
PostLocal 只进入指定 Layer 队列
DispatchLocal 不触发其他 Layer
```

改为测试：

```text
Send 会按 Layer 顺序触发所有订阅者
Post 会进入队列并在 Pump 时触发订阅者
Layer 顺序仍然稳定
Flow handler 仍然可以截断后续处理
```

---

### 11.2 新增语义测试

建议新增：

```text
同一事件类型在多个 Layer 订阅后，Send<T> 会按 Layer 顺序执行
Post<T> 后，只唤醒存在订阅者的 Layer
删除 Local 后，不再存在只投递到单一 Layer 的行为
```

---

## 12. 文档修改建议

README 中建议把事件发送部分改成：

```markdown
### 发送事件

LayerBase 提供两种发送方式：

- `Send<T>(in T value)`：立即同步发送事件。
- `Post<T>(in T value)`：异步投递事件，由 Layer 在合适时机处理。

Layer 负责控制事件处理顺序和处理阶段，不再控制事件可见范围。
```

删除类似描述：

```markdown
SendLocal
PostLocal
Local Event
Only dispatch to current layer
```

---

## 13. 迁移示例

### 13.1 修改前

```csharp
public void DamageCurrentLayer(int layerIndex, in DamageEvent damage)
{
    // layerIndex：
    // 旧 Local 模型下的目标 Layer。
    // damage：
    // 要发送的伤害事件。
    PostLocal<DamageEvent>(layerIndex, in damage);
}
```

### 13.2 修改后

```csharp
public void Damage(in DamageEvent damage)
{
    // damage：
    // 要发送的伤害事件。
    // 删除 Local 后，所有订阅 DamageEvent 的 handler 都有机会收到该事件。
    // Layer 只决定这些 handler 的处理顺序。
    Post<DamageEvent>(in damage);
}
```

---

## 14. 版本建议

如果 `SendLocal` / `PostLocal` 是公开 API，建议作为破坏性修改发布。

推荐版本号：

```text
2.0.0
```

如果这些 API 尚未正式公开，或只存在于内部开发分支，可以直接删除并在 changelog 中说明。

---

## 15. 最终结论

建议删除 `Local` 语义。

删除后，LayerBase 的事件模型可以收敛为：

```text
Subscribe<T>：订阅事件
Send<T>：立即同步发送事件
Post<T>：异步投递事件
Layer：控制执行阶段和处理顺序
EventTypeId<T>.Id：提供高性能事件类型 ID
```

这比同时维护 `Global` 和 `Local` 两套发送语义更清晰，也更符合 LayerBase 当前的高性能事件系统定位。
