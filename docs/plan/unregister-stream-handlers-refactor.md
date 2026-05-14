# unregister-stream-handlers-refactor.md

## 1. 目标

本文档用于指导修改 `UnregisterStreamHandlers` 链路。

目标：

```text
Actor 销毁或归还对象池时，
只注销当前 Actor 类型、当前事件类型、当前 slot 对应的 EventStream handler。
```

当前需要避免的问题：

```text
1. 注销时遍历 ActorWorld 内全部 EventStreamRuntime。
2. 不按 archetypeId 精确过滤。
3. 不按 TEvent 精确定位。
4. 不同 Actor 类型复用相同 slotIndex 时，可能误清其他 EventStreamCenter 的 handler。
5. EventStreamRuntime 数量增加后，Actor destroy / return 成本线性增长。
```

最终目标链路：

```text
TypedActorStorage.FinalizeDestroySlot
→ UnregisterStreamHandlers(actorId, slotIndex, world)
→ foreach ActorBehaviourEntry
→ entry.StreamUnregister(_archetypeId, slotIndex, world)
→ EventStreamRuntime<TEvent>.GetCenterUnchecked(world.RuntimeIndex, archetypeId)
→ center.UnregisterHandler(slotIndex)
```

---

## 2. 术语说明

### 2.1 handler

`handler` 是 Actor 对某个事件的处理委托。

例如：

```csharp
private void OnMove(in MoveEvent e)
{
}
```

经过生成器后会变成：

```csharp
ActorEventHandler<MoveEvent>
```

它保存：

```text
1. 目标 Actor 实例。
2. 目标 Actor 方法入口。
```

---

### 2.2 thunk

`thunk` 是一个小型转接委托。

这里的 `StreamUnregister` 就是 thunk。

它的作用是：

```text
TypedActorStorage 本身不知道 TEvent。
但 ActorTypeMetaBuilder.AddBehaviour<TActor,TEvent>() 创建 entry 时知道 TEvent。
所以可以在 entry 里保存一个强泛型注销委托。
```

这样销毁 Actor 时就不需要通过 `Type eventType` 或遍历 runtime 来找目标事件流。

---

### 2.3 archetypeId

`archetypeId` 是 Actor 类型对应的行为原型编号。

它用于区分：

```text
同一个 ActorWorld 内不同 Actor 类型的 EventStreamRuntime。
```

例如：

```text
EnemyActor 的 MoveEvent stream
PlayerActor 的 MoveEvent stream
```

两者事件类型相同，但 actor 类型不同，因此应该属于不同的 archetype。

---

## 3. 修改文件

需要修改：

```text
LayerBase/Actor/Meta/ActorBehaviourEntry.cs
LayerBase/Actor/Meta/ActorTypeMetaBuilder.cs
LayerBase/Actor/Storage/TypedActorStorage.cs
```

可选修改：

```text
LayerBase/Actor/Storage/ActorWorld.cs
LayerBase/Actor/EventStream/EventStreamRuntimeBase.cs
LayerBase/Actor/EventStream/EventStreamRuntime.cs
```

---

## 4. 修改 ActorBehaviourEntry.cs

### 4.1 新增注销委托

在 `ActorStreamHandlerRegister` 后新增：

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 委托：在 Actor 销毁或归还对象池时注销 EventStream handler。
///
/// 参数说明：
/// archetypeId：Actor 类型对应的行为原型编号。
/// slotIndex：Actor 在 TypedActorStorage 中的 slot 下标。
/// world：当前 ActorWorld。
///
/// 作用：
/// 保存 TEvent 的强泛型注销路径。
/// 让销毁时可以直接访问 EventStreamRuntime<TEvent>，
/// 避免遍历 ActorWorld 内全部 EventStreamRuntime。
/// </summary>
internal delegate void ActorStreamHandlerUnregister(
    int archetypeId,
    int slotIndex,
    ActorWorld world);
```

---

### 4.2 增加字段

在 `ActorBehaviourEntry` 中增加字段：

```csharp
/// <summary>
/// EventStream handler 注销委托。
///
/// 作用：
/// 由 ActorTypeMetaBuilder.AddBehaviour<TActor,TEvent>() 创建，
/// 内部保留 TEvent 泛型信息。
/// Actor 销毁时通过它精确注销当前事件流 handler。
/// </summary>
public readonly ActorStreamHandlerUnregister? StreamUnregister;
```

---

### 4.3 修改旧构造函数

旧的非 stream handler 构造函数中添加：

```csharp
StreamUnregister = null;
```

完整目标：

```csharp
public ActorBehaviourEntry(
    int                     eventTypeId,
    Type                    eventType,
    object                  invoker,
    ActorEventColumnFactory factory)
{
    EventTypeId = eventTypeId;
    EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
    Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
    Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    StreamRegister = null;
    StreamUnregister = null;
    IsStreamHandler = false;
}
```

---

### 4.4 修改 stream handler 构造函数

把当前构造函数：

```csharp
public ActorBehaviourEntry(
    int                          eventTypeId,
    Type                         eventType,
    object                       handlerFactory,
    ActorStreamHandlerRegister   streamRegister)
```

改成：

```csharp
public ActorBehaviourEntry(
    int                          eventTypeId,
    Type                         eventType,
    object                       handlerFactory,
    ActorStreamHandlerRegister   streamRegister,
    ActorStreamHandlerUnregister streamUnregister)
{
    EventTypeId = eventTypeId;
    EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
    Invoker = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
    Factory = null;
    StreamRegister = streamRegister ?? throw new ArgumentNullException(nameof(streamRegister));
    StreamUnregister = streamUnregister ?? throw new ArgumentNullException(nameof(streamUnregister));
    IsStreamHandler = true;
}
```

---

## 5. 修改 ActorTypeMetaBuilder.cs

### 5.1 当前问题

当前 `AddBehaviour<TActor,TEvent>` 只构造了 `streamRegister`。

目标是同时构造：

```text
streamRegister
streamUnregister
```

其中：

```text
streamRegister：
Actor 创建时注册 handler。

streamUnregister：
Actor 销毁时注销 handler。
```

---

### 5.2 新增 streamUnregister

在 `AddBehaviour<TActor,TEvent>` 内，`streamRegister` 后新增：

```csharp
ActorStreamHandlerUnregister streamUnregister =
    static (archetypeId, slotIndex, world) =>
    {
        // EventStreamRuntime<TEvent>：
        // 这里的 TEvent 来自 AddBehaviour<TActor,TEvent>() 的泛型参数。
        // 因此该委托可以直接定位对应事件类型的静态缓存。
        EventStreamCenter<TEvent>? center =
            EventStreamRuntime<TEvent>.GetCenterUnchecked(
                world.RuntimeIndex,
                archetypeId);

        center?.UnregisterHandler(
            slotIndex);
    };
```

参数作用：

```text
archetypeId：
用于区分同一个 ActorWorld 内不同 Actor 类型的 EventStreamCenter。

slotIndex：
用于清理当前 Actor 在 handler 表中的位置。

world.RuntimeIndex：
用于区分多个 ActorWorld。
```

---

### 5.3 修改 ActorBehaviourEntry 构造调用

把：

```csharp
_entries.Add(new ActorBehaviourEntry(
    eventTypeId,
    typeof(TEvent),
    handlerFactory,
    streamRegister));
```

改成：

```csharp
_entries.Add(new ActorBehaviourEntry(
    eventTypeId,
    typeof(TEvent),
    handlerFactory,
    streamRegister,
    streamUnregister));
```

---

### 5.4 修改后的 AddBehaviour 核心结构

目标结构如下：

```csharp
public void AddBehaviour<TActor, TEvent>(
    ActorBehaviourHandlerFactory<TActor, TEvent> handlerFactory)
    where TActor : class, IActor
    where TEvent : struct
{
    if (handlerFactory == null)
    {
        throw new ArgumentNullException(nameof(handlerFactory));
    }

    int eventTypeId = EventTypeId<TEvent>.Id;

    if (!_eventIds.Add(eventTypeId))
    {
        throw new InvalidOperationException(
            $"Actor type {typeof(TActor).Name} already has behaviour for event {typeof(TEvent).Name}.");
    }

    ActorStreamHandlerRegister streamRegister =
        (actor, archetypeId, slotIndex, generation, world) =>
        {
            var typedActor = (TActor)actor;

            ActorEventHandler<TEvent> handler =
                handlerFactory(typedActor);

            EventStreamCenter<TEvent>? center =
                EventStreamRuntime<TEvent>.GetCenterUnchecked(
                    world.RuntimeIndex,
                    archetypeId);

            if (center == null)
            {
                ActorEventStreamPlan<TEvent> plan =
                    ActorEventStreamPlanBuilder.Build<TEvent>();

                world.GetOrCreateEventStreamRuntime<TEvent>(
                    plan,
                    archetypeId);

                center =
                    EventStreamRuntime<TEvent>.GetCenterUnchecked(
                        world.RuntimeIndex,
                        archetypeId);
            }

            center?.RegisterHandler(
                slotIndex,
                generation,
                handler);
        };

    ActorStreamHandlerUnregister streamUnregister =
        static (archetypeId, slotIndex, world) =>
        {
            EventStreamCenter<TEvent>? center =
                EventStreamRuntime<TEvent>.GetCenterUnchecked(
                    world.RuntimeIndex,
                    archetypeId);

            center?.UnregisterHandler(
                slotIndex);
        };

    _entries.Add(new ActorBehaviourEntry(
        eventTypeId,
        typeof(TEvent),
        handlerFactory,
        streamRegister,
        streamUnregister));
}
```

---

## 6. 修改 TypedActorStorage.cs

### 6.1 当前问题

当前 `UnregisterStreamHandlers` 可能类似：

```csharp
private void UnregisterStreamHandlers(
    int slotIndex,
    ActorWorld world)
{
    foreach (ActorBehaviourEntry entry in _meta.Behaviours)
    {
        if (!entry.IsStreamHandler)
        {
            continue;
        }

        world.UnregisterStreamHandler(
            _archetypeId,
            slotIndex,
            entry.EventType);
    }
}
```

这个问题是：

```text
1. 把注销交给 ActorWorld。
2. ActorWorld 需要根据 Type 或遍历 runtime 找目标。
3. 无法利用 entry 创建时保存的 TEvent 泛型信息。
```

---

### 6.2 目标改法

改成直接调用 entry 的 `StreamUnregister`：

```csharp
/// <summary>
/// 注销当前 slot 上 Actor 的所有 EventStream handlers。
///
/// 参数说明：
/// slotIndex：Actor 在当前 TypedActorStorage 中的 slot 下标。
/// world：当前 ActorWorld。
///
/// 作用：
/// 通过 ActorBehaviourEntry.StreamUnregister 精确清理当前 Actor 的事件处理器。
/// 不再让 ActorWorld 遍历全部 EventStreamRuntime。
/// </summary>
private void UnregisterStreamHandlers(
    int slotIndex,
    ActorWorld world)
{
    if (_meta == null)
    {
        return;
    }

    foreach (ActorBehaviourEntry entry in _meta.Behaviours)
    {
        if (!entry.IsStreamHandler)
        {
            continue;
        }

        ActorStreamHandlerUnregister? unregister =
            entry.StreamUnregister;

        if (unregister == null)
        {
            continue;
        }

        unregister(
            _archetypeId,
            slotIndex,
            world);
    }
}
```

如果你的方法签名当前带 `ActorId actorId`，可以保留参数，但不需要用：

```csharp
private void UnregisterStreamHandlers(
    ActorId actorId,
    int slotIndex,
    ActorWorld world)
{
    _ = actorId;

    ...
}
```

---

## 7. ActorWorld.UnregisterStreamHandler 的处理

### 7.1 推荐处理

如果所有注销都改成 `entry.StreamUnregister`，那么 `ActorWorld.UnregisterStreamHandler(...)` 不再是主路径。

推荐保留为 fallback，并标记：

```csharp
[Obsolete("Use ActorBehaviourEntry.StreamUnregister instead.")]
```

或者直接删除调用点后删除该方法。

---

### 7.2 如果必须保留 fallback

至少不要无差别遍历所有 runtime。

需要在 `EventStreamRuntimeBase` 增加：

```csharp
/// <summary>
/// 当前 EventStreamRuntime 对应的 Actor archetypeId。
/// </summary>
public abstract int ArchetypeId { get; }
```

在 `EventStreamRuntime<TEvent>` 中实现：

```csharp
public override int ArchetypeId => _archetypeId;
```

然后 fallback 改成：

```csharp
internal void UnregisterStreamHandler(
    int archetypeId,
    int slotIndex,
    int eventTypeId)
{
    foreach (EventStreamRuntimeBase runtime in _eventStreamRuntimes)
    {
        if (runtime.ArchetypeId != archetypeId)
        {
            continue;
        }

        if (runtime.EventTypeId != eventTypeId)
        {
            continue;
        }

        runtime.UnregisterHandler(
            slotIndex);

        return;
    }
}
```

但注意：

```text
这仍然比 StreamUnregister 慢，因为它还要遍历 _eventStreamRuntimes。
```

因此主路径应使用 `StreamUnregister`。

---

## 8. 修改顺序

建议按以下顺序提交：

```text
1. 修改 ActorBehaviourEntry.cs
   - 增加 ActorStreamHandlerUnregister
   - 增加 StreamUnregister 字段
   - 修改构造函数

2. 修改 ActorTypeMetaBuilder.cs
   - AddBehaviour 中新增 streamUnregister
   - ActorBehaviourEntry 构造传入 streamUnregister

3. 修改 TypedActorStorage.cs
   - UnregisterStreamHandlers 改为 entry.StreamUnregister(...)

4. 删除或废弃 ActorWorld.UnregisterStreamHandler
   - 如果保留，改为 fallback 且精确过滤

5. 运行 benchmark
   - 重点看 destroy / rent / return 路径是否下降
```

---

## 9. 验收标准

### 9.1 正确性

```text
1. Actor 创建后能正常接收事件。
2. Actor 销毁后，对应 slot 的 handler 被清理。
3. Actor 销毁后，同一个 slotIndex 的其他 archetype 不被误清。
4. 同一个 TEvent 下，不同 Actor 类型各自的 EventStreamCenter 不互相影响。
5. Actor 归还对象池后，不再响应已销毁 slot 的事件。
```

---

### 9.2 性能

重点观察：

```text
Pooled Actor Runtime: Rent + Return ×1000
Actor Cold: New World + Create + Destroy ×1000
Actor: PostTo + Pump ×1000
Actor: PostTo + Pump ×10000
```

预期：

```text
1. UnregisterStreamHandlers 的销毁成本下降。
2. EventStreamRuntime 数量越多，收益越明显。
3. 不直接解决 handler delegate 分配。
   handler delegate 分配应由 ActorBehaviourGenerator 的 handler 缓存字段方案解决。
```

---

## 10. 注意事项

### 10.1 不要在注销时清 Actor handler 缓存字段

Actor 实例上的 handler 缓存字段用于池化复用。

它属于 Actor 自身，不属于 EventStreamCenter。

注销时只应该清：

```text
EventStreamCenter._handlersBySlot[slotIndex]
EventStreamCenter._aliveGenerations[slotIndex]
```

不要清 Actor 内部缓存的：

```text
__layerbase_cachedHandler_xxx
```

否则下次 Actor 从池中 rent 出来又会重新分配 delegate。

---

### 10.2 StreamUnregister 不需要 generation

注册需要 generation：

```text
RegisterHandler(slotIndex, generation, handler)
```

因为 post / pump 要校验 slot 是否被复用。

注销只需要：

```text
slotIndex
```

因为销毁当前 slot 时，目标就是清掉该 slot 上的 handler。

---

### 10.3 StreamUnregister 不需要 Actor 实例

注销只操作 EventStreamCenter 的 handler 表。

不需要访问 Actor 实例。

因此委托签名保持：

```csharp
ActorStreamHandlerUnregister(
    int archetypeId,
    int slotIndex,
    ActorWorld world)
```

不传 `object actor`，避免误用。

---

## 11. 最终结构

最终注册链路：

```text
ActorTypeMetaBuilder.AddBehaviour<TActor,TEvent>
→ 创建 streamRegister
→ 创建 streamUnregister
→ 保存到 ActorBehaviourEntry
```

最终创建链路：

```text
TypedActorStorage.RegisterStreamHandlers
→ entry.StreamRegister(actor, _archetypeId, slotIndex, generation, world)
→ EventStreamRuntime<TEvent>.GetCenterUnchecked(...)
→ center.RegisterHandler(...)
```

最终销毁链路：

```text
TypedActorStorage.UnregisterStreamHandlers
→ entry.StreamUnregister(_archetypeId, slotIndex, world)
→ EventStreamRuntime<TEvent>.GetCenterUnchecked(...)
→ center.UnregisterHandler(slotIndex)
```
