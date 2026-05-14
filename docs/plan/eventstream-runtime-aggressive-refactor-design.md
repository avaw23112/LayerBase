# EventStream Runtime Aggressive Refactor Design

## 1. 目标

本文档定义 LayerBase Actor 消息系统的激进重构方案。

目标是将当前按 Actor slot 分散存储的邮箱结构，重构为按事件类型组织的连续分段事件流：

```text
PostTo<TEvent>(ActorId, TEvent)
→ EventStreamCenter<TEvent>
→ Segmented Mail Queue
→ Pump
→ handlerTable[slotIndex](in TEvent)
```

本设计同时包含：

```text
1. EventStream 邮箱重构。
2. ProjectedActorRef 缓存。
3. batch 模板优化。
4. EventStreamSegmentPool 通过 EventMetaData 配置。
5. 基于现有 [ActorBehaviour] / ActorBehaviourGenerator / ActorTypeMetaBuilder 的接口重构。
```

---

## 2. 当前源码依据

### 2.1 ActorBehaviour 特性

当前项目已存在方法级特性：

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ActorBehaviourAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ActorBehavioursAttribute : Attribute
{
}
```

设计要求：

```text
保留 [ActorBehaviour] / [ActorBehaviours] 的使用方式。
继续由源生成器扫描 Actor 方法。
不要求业务 Actor 手动注册事件处理器。
```

---

### 2.2 ActorBehaviourGenerator 当前行为

当前生成器会：

```text
1. 扫描 partial class。
2. 要求 class 实现 IActor。
3. 要求 [ActorBehaviour] 方法是实例方法。
4. 要求方法返回 void。
5. 要求方法只有一个 in struct event 参数。
6. 生成 IGeneratedActorMeta。
7. 在 __BuildActorMeta 中调用 builder.AddBehaviour<TActor,TEvent>(...)。
```

当前生成的 `AddBehaviour` 形态类似：

```csharp
builder.AddBehaviour<TActor, TEvent>(
    static (TActor actor, in TEvent e) =>
    {
        actor.OnEvent(in e);
    });
```

新设计改为：

```csharp
builder.AddBehaviour<TActor, TEvent>(
    static (TActor actor) => actor.OnEvent);
```

新含义：

```text
旧模式：
存 static invoker，然后 Pump 时传 actor + event。

新模式：
Actor 创建时把 actor.OnEvent 转成闭包式实例委托。
Pump 时只拿 handlerTable[slotIndex] 并调用 handler(in event)。
```

---

### 2.3 ActorTypeMetaBuilder 当前职责

当前 `ActorTypeMetaBuilder.AddBehaviour<TActor,TEvent>` 接收：

```csharp
ActorBehaviourInvoker<TActor, TEvent>
```

当前 `ActorBehaviourInvoker` 是：

```csharp
public delegate void ActorBehaviourInvoker<TActor, TEvent>(
    TActor actor,
    in TEvent value)
    where TActor : class, IActor
    where TEvent : struct;
```

新设计替换为：

```csharp
public delegate ActorEventHandler<TEvent> ActorBehaviourHandlerFactory<TActor, TEvent>(
    TActor actor)
    where TActor : class, IActor
    where TEvent : struct;
```

目标：

```text
在 Actor 创建时完成 actor instance → event handler 的绑定。
在 Pump 时不再取 IActor，不再传 actor，不再走接口或虚方法。
```

---

### 2.4 EventMetaData 当前能力

当前 `EventMetaData<TEvent>` 已经有：

```csharp
public virtual ActorMailOptions? ActorMailOptions => null;
public ActorMailOptions? GetActorMailOptions() => ActorMailOptions;
```

当前 `ActorEventPostPlanBuilder.Build<TEvent>` 已经支持：

```text
EventMetaData<TEvent>.ActorMailOptions 优先。
ActorWorld 默认 ActorMailOptions 兜底。
```

新设计要求：

```text
EventStreamSegmentPool 配置必须能从 EventMetaData 中读取。
```

实现方式：

```text
扩展 ActorMailOptions，加入 EventStreamOptions。
ActorEventStreamPlanBuilder 继续从 EventMetaData<TEvent>.ActorMailOptions 读取配置。
```

这样不需要新增 EventMetaData 方法，也不破坏现有元数据入口。

---

## 3. 架构总览

### 3.1 新运行时链路

```text
Actor 创建
→ 源生成器生成的 meta 提供 HandlerFactory
→ TypedActorStorage 分配 slot
→ ActorWorld.RegisterEventHandler<TEvent>(actorId, handler)

Post
→ ActorWorld.PostTo<TEvent>(actorId, in value)
→ EventStreamCenter<TEvent>.Post(actorId, in value)
→ 写入分段队列 tail Segment

Pump
→ ActorWorld.PumpEventStreams(...)
→ dirty EventStreamCenter
→ EventStreamCenter<TEvent>.Pump(...)
→ 读取 Mail
→ generation 校验
→ handlerTable[slotIndex]
→ handler(in value)
```

---

### 3.2 删除旧邮件模型

新 Actor 事件流不再使用：

```text
EventMail<TEvent>[slotIndex]
EventPostRow<TEvent>
EventPostState<TEvent>
EventColumn<TActor,TEvent>
DirtySlotList
DirtyBucketList
Actor 私有 mailbox
Latest / Dirty / DropOldest / RejectNew mailbox 策略
```

保留或单独处理：

```text
ActorId
ActorWorld
TypedActorStorage
ActorTypeMeta
ActorTypeMetaCache
Actor tag / group
ActorCallColumn
Lifecycle
ProjectedActorRef
```

---

## 4. 配置结构

### 4.1 ActorMailBackend

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 消息后端类型。
///
/// 作用：
/// 让 ActorMailOptions 可以明确选择 Actor 消息系统使用哪种运行时。
/// 当前激进重构后，EventStream 是默认后端。
/// </summary>
public enum ActorMailBackend
{
    /// <summary>
    /// 事件流后端。
    ///
    /// 语义：
    /// 每个事件类型一个 EventStreamCenter。
    /// 邮件全局排队。
    /// 只支持 QueueGrow。
    /// </summary>
    EventStream = 0
}
```

---

### 4.2 EventStreamOptions

```csharp
namespace LayerBase.Actor;

/// <summary>
/// EventStream 队列配置。
///
/// 作用：
/// 控制每个事件类型的全局邮件队列如何分段扩容，以及池最多保留多少备用段。
/// </summary>
public readonly struct EventStreamOptions
{
    /// <summary>
    /// 每个 Segment 能容纳多少封邮件。
    ///
    /// 参数作用：
    /// 值越大，Segment 数量越少，但单个数组更大。
    /// 值越小，单个数组更轻，但 Segment 链接数量更多。
    /// </summary>
    public readonly int SegmentCapacity;

    /// <summary>
    /// Segment 池最多保留多少个空闲 Segment。
    ///
    /// 参数作用：
    /// 避免消息高峰结束后，池里长期保留大量空闲数组。
    /// </summary>
    public readonly int MaxRetainedSegments;

    /// <summary>
    /// 构造 EventStreamOptions。
    /// </summary>
    /// <param name="segmentCapacity">
    /// 每个 Segment 的邮件容量。
    /// 必须大于 0。
    /// 如果传入小于等于 0 的值，将回退到 512。
    /// </param>
    /// <param name="maxRetainedSegments">
    /// Segment 池最多保留多少个空闲 Segment。
    /// 可以为 0。
    /// 0 表示读空 Segment 后不缓存备用 Segment。
    /// </param>
    public EventStreamOptions(
        int segmentCapacity,
        int maxRetainedSegments)
    {
        SegmentCapacity = segmentCapacity > 0
            ? segmentCapacity
            : 512;

        MaxRetainedSegments = maxRetainedSegments >= 0
            ? maxRetainedSegments
            : 4;
    }

    /// <summary>
    /// 默认 EventStream 配置。
    /// </summary>
    public static EventStreamOptions Default =>
        new EventStreamOptions(
            segmentCapacity: 512,
            maxRetainedSegments: 4);
}
```

---

### 4.3 ActorMailOptions 扩展

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 邮件配置。
///
/// 作用：
/// 1. 保留当前 ActorMailOptions 作为 EventMetaData 的配置入口。
/// 2. 新增 EventStreamOptions，使 SegmentPool 可由 EventMetaData 配置。
/// 3. 将 Actor 消息后端固定为 EventStream。
/// </summary>
public readonly struct ActorMailOptions
{
    /// <summary>
    /// Actor 消息后端。
    /// 激进重构后默认使用 EventStream。
    /// </summary>
    public readonly ActorMailBackend Backend;

    /// <summary>
    /// EventStream 分段队列配置。
    /// </summary>
    public readonly EventStreamOptions StreamOptions;

    /// <summary>
    /// 构造 ActorMailOptions。
    /// </summary>
    /// <param name="backend">
    /// Actor 消息后端。
    /// 当前只支持 EventStream。
    /// </param>
    /// <param name="streamOptions">
    /// EventStream 分段队列配置。
    /// </param>
    public ActorMailOptions(
        ActorMailBackend backend,
        EventStreamOptions streamOptions)
    {
        Backend = backend;
        StreamOptions = streamOptions;
    }

    /// <summary>
    /// 默认 Actor 邮件配置。
    /// </summary>
    public static ActorMailOptions Default =>
        new ActorMailOptions(
            backend: ActorMailBackend.EventStream,
            streamOptions: EventStreamOptions.Default);

    /// <summary>
    /// 创建 EventStream 后端配置。
    /// </summary>
    /// <param name="segmentCapacity">
    /// 每个 Segment 的邮件容量。
    /// </param>
    /// <param name="maxRetainedSegments">
    /// Segment 池最多保留的空闲 Segment 数量。
    /// </param>
    /// <returns>
    /// ActorMailOptions。
    /// </returns>
    public static ActorMailOptions EventStream(
        int segmentCapacity = 512,
        int maxRetainedSegments = 4)
    {
        return new ActorMailOptions(
            backend: ActorMailBackend.EventStream,
            streamOptions: new EventStreamOptions(
                segmentCapacity,
                maxRetainedSegments));
    }
}
```

---

### 4.4 EventMetaData 配置示例

```csharp
using LayerBase.Actor;
using LayerBase.Event.EventMetaData;

public readonly struct MoveEvent
{
    public readonly float DeltaX;
    public readonly float DeltaY;

    /// <summary>
    /// 构造移动事件。
    /// </summary>
    /// <param name="deltaX">
    /// X 方向变化量。
    /// </param>
    /// <param name="deltaY">
    /// Y 方向变化量。
    /// </param>
    public MoveEvent(
        float deltaX,
        float deltaY)
    {
        DeltaX = deltaX;
        DeltaY = deltaY;
    }
}

/// <summary>
/// MoveEvent 元数据。
///
/// 作用：
/// 通过 ActorMailOptions 配置 MoveEvent 的 EventStream SegmentPool。
/// </summary>
public sealed class MoveEventMetaData : EventMetaData<MoveEvent>
{
    /// <summary>
    /// MoveEvent 的 Actor 邮件配置。
    /// </summary>
    public override ActorMailOptions? ActorMailOptions =>
        ActorMailOptions.EventStream(
            segmentCapacity: 1024,
            maxRetainedSegments: 8);
}
```

---

## 5. EventStream Plan

### 5.1 ActorEventStreamPlan

```csharp
using LayerBase.Core.Event;

namespace LayerBase.Actor;

/// <summary>
/// 单个 TEvent 的 Actor EventStream 构建计划。
///
/// 作用：
/// 1. 保存事件类型 ID。
/// 2. 保存 EventStreamOptions。
/// 3. 将 EventMetaData 解析结果编译到运行时结构中。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal readonly struct ActorEventStreamPlan<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 当前事件类型 ID。
    /// </summary>
    public readonly int EventId;

    /// <summary>
    /// 当前事件类型的 EventStream 配置。
    /// </summary>
    public readonly EventStreamOptions StreamOptions;

    /// <summary>
    /// 构造 ActorEventStreamPlan。
    /// </summary>
    /// <param name="eventId">
    /// 当前事件类型 ID。
    /// </param>
    /// <param name="streamOptions">
    /// 当前事件类型的 EventStream 配置。
    /// </param>
    public ActorEventStreamPlan(
        int eventId,
        EventStreamOptions streamOptions)
    {
        EventId = eventId;
        StreamOptions = streamOptions;
    }
}
```

---

### 5.2 ActorEventStreamPlanBuilder

```csharp
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Actor;

/// <summary>
/// Actor EventStream 构建计划生成器。
///
/// 作用：
/// 1. 从 EventMetaData<TEvent> 读取 ActorMailOptions。
/// 2. 如果事件没有元数据配置，则使用 ActorWorld 默认 ActorMailOptions。
/// 3. 输出 EventStreamCenter 构建所需配置。
/// </summary>
internal static class ActorEventStreamPlanBuilder
{
    /// <summary>
    /// 构建 TEvent 的 EventStream plan。
    /// </summary>
    /// <param name="worldDefaultMailOptions">
    /// ActorWorld 默认 ActorMailOptions。
    /// 当事件元数据没有提供 ActorMailOptions 时使用。
    /// </param>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    /// <returns>
    /// ActorEventStreamPlan。
    /// </returns>
    public static ActorEventStreamPlan<TEvent> Build<TEvent>(
        ActorMailOptions worldDefaultMailOptions)
        where TEvent : struct
    {
        EventMetaData<TEvent>? metaData =
            EventMetaDataHandler.ResolveRegisteredMetaData<TEvent>();

        ActorMailOptions mailOptions =
            metaData?.GetActorMailOptions() ?? worldDefaultMailOptions;

        return new ActorEventStreamPlan<TEvent>(
            eventId: EventTypeId<TEvent>.Id,
            streamOptions: mailOptions.StreamOptions);
    }
}
```

---

## 6. 核心数据结构

### 6.1 ActorEventHandler

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 对某个事件类型的处理委托。
///
/// 作用：
/// 1. 委托自身保存目标 Actor 实例和方法入口。
/// 2. Pump 不再需要拿 IActor。
/// 3. Pump 不再需要虚方法或接口分发。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// 必须是 struct，避免事件对象本身产生托管堆分配。
/// </typeparam>
/// <param name="value">
/// 事件值。
/// 使用 in 避免大结构体复制。
/// </param>
public delegate void ActorEventHandler<TEvent>(
    in TEvent value)
    where TEvent : struct;
```

---

### 6.2 ActorBehaviourHandlerFactory

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 行为处理委托工厂。
///
/// 作用：
/// 1. 在 Actor 创建时，把 Actor 实例绑定为闭包式 handler。
/// 2. 避免 Pump 阶段再传 actor 参数。
/// 3. 避免 Pump 阶段再做 IActor 查询或类型转换。
/// </summary>
/// <typeparam name="TActor">
/// Actor 类型。
/// </typeparam>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
/// <param name="actor">
/// 当前 Actor 实例。
/// </param>
/// <returns>
/// 当前 Actor 对 TEvent 的闭包式事件处理委托。
/// </returns>
public delegate ActorEventHandler<TEvent> ActorBehaviourHandlerFactory<TActor, TEvent>(
    TActor actor)
    where TActor : class, IActor
    where TEvent : struct;
```

---

### 6.3 EventStreamMail

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 全局事件流中的一封邮件。
///
/// 作用：
/// 1. 保存目标 Actor 的 slotIndex。
/// 2. 保存目标 Actor 的 generation。
/// 3. 保存事件值。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal struct EventStreamMail<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 目标 Actor 的 slot 下标。
    /// Pump 时用它索引 handlerTable。
    /// </summary>
    public int SlotIndex;

    /// <summary>
    /// 邮件创建时目标 Actor 的 generation。
    /// 用于防止 slot 复用后旧邮件打到新 Actor。
    /// </summary>
    public int Generation;

    /// <summary>
    /// 事件值。
    /// </summary>
    public TEvent Value;
}
```

---

## 7. 分段队列结构

### 7.1 EventStreamSegment

```csharp
namespace LayerBase.Actor;

/// <summary>
/// EventStream 的固定容量内存段。
///
/// 作用：
/// 1. 保存一批连续的 EventStreamMail。
/// 2. 避免全局队列扩容时搬迁旧消息。
/// 3. 读空后可以整体回收到池。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal sealed class EventStreamSegment<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 当前 Segment 内的邮件数组。
    /// </summary>
    public readonly EventStreamMail<TEvent>[] Items;

    /// <summary>
    /// 当前 Segment 的读取下标。
    /// Pump 从这里读取。
    /// </summary>
    public int ReadIndex;

    /// <summary>
    /// 当前 Segment 的写入下标。
    /// Post 从这里写入。
    /// </summary>
    public int WriteIndex;

    /// <summary>
    /// 下一个 Segment。
    /// Segment 链表用它连接。
    /// </summary>
    public EventStreamSegment<TEvent>? Next;

    /// <summary>
    /// 当前 Segment 是否已经写满。
    /// </summary>
    public bool IsFull => WriteIndex >= Items.Length;

    /// <summary>
    /// 当前 Segment 是否已经读空。
    /// </summary>
    public bool IsEmpty => ReadIndex >= WriteIndex;

    /// <summary>
    /// 构造 EventStreamSegment。
    /// </summary>
    /// <param name="capacity">
    /// 当前 Segment 能容纳多少封邮件。
    /// </param>
    public EventStreamSegment(
        int capacity)
    {
        Items = new EventStreamMail<TEvent>[capacity];
        ReadIndex = 0;
        WriteIndex = 0;
        Next = null;
    }

    /// <summary>
    /// 重置 Segment 状态。
    /// </summary>
    /// <param name="clearItems">
    /// 是否清空 Items 中的旧邮件。
    /// 如果 TEvent 包含托管引用，则必须清空，避免数组继续引用旧对象。
    /// </param>
    public void Reset(
        bool clearItems)
    {
        if (clearItems)
        {
            Array.Clear(
                Items,
                0,
                WriteIndex);
        }

        ReadIndex = 0;
        WriteIndex = 0;
        Next = null;
    }
}
```

---

### 7.2 EventStreamSegmentPool

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

/// <summary>
/// EventStream Segment 池。
///
/// 作用：
/// 1. 复用读空后的 Segment。
/// 2. 限制池中保留的备用 Segment 数量。
/// 3. 避免高峰流量结束后长期占用大量内存。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal sealed class EventStreamSegmentPool<TEvent>
    where TEvent : struct
{
    private readonly Stack<EventStreamSegment<TEvent>> _segments;
    private readonly int _segmentCapacity;
    private readonly int _maxRetainedSegments;
    private readonly bool _clearItemsOnReturn;

    /// <summary>
    /// 构造 Segment 池。
    /// </summary>
    /// <param name="options">
    /// EventStream 队列配置。
    /// </param>
    public EventStreamSegmentPool(
        EventStreamOptions options)
    {
        _segments = new Stack<EventStreamSegment<TEvent>>(
            options.MaxRetainedSegments);

        _segmentCapacity = options.SegmentCapacity;
        _maxRetainedSegments = options.MaxRetainedSegments;
        _clearItemsOnReturn =
            RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>();
    }

    /// <summary>
    /// 租用一个 Segment。
    /// </summary>
    /// <returns>
    /// 可写入的新 Segment。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventStreamSegment<TEvent> Rent()
    {
        if (_segments.Count > 0)
        {
            return _segments.Pop();
        }

        return new EventStreamSegment<TEvent>(
            _segmentCapacity);
    }

    /// <summary>
    /// 归还一个已经读空的 Segment。
    /// </summary>
    /// <param name="segment">
    /// 需要归还的 Segment。
    /// </param>
    public void Return(
        EventStreamSegment<TEvent> segment)
    {
        segment.Reset(
            clearItems: _clearItemsOnReturn);

        if (_segments.Count >= _maxRetainedSegments)
        {
            return;
        }

        _segments.Push(
            segment);
    }

    /// <summary>
    /// 清空池中所有备用 Segment。
    /// </summary>
    public void Clear()
    {
        _segments.Clear();
    }
}
```

---

## 8. EventStreamCenter

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

/// <summary>
/// 某个 TEvent 的全局事件流中心。
///
/// 作用：
/// 1. 每个事件类型只有一个全局邮件流。
/// 2. 邮件存储使用分段队列。
/// 3. Post 不搬迁旧消息。
/// 4. Pump 读空一个 Segment 后立刻回收到池。
/// 5. 池只保留可配置数量的备用 Segment。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal sealed class EventStreamCenter<TEvent> : IEventStreamCenterRuntime
    where TEvent : struct
{
    private readonly EventStreamSegmentPool<TEvent> _segmentPool;

    private EventStreamSegment<TEvent>? _head;
    private EventStreamSegment<TEvent>? _tail;
    private int _count;

    private ActorEventHandler<TEvent>?[] _handlersBySlot;
    private int[] _aliveGenerations;

    public bool IsEmpty => _count == 0;

    public int Count => _count;

    /// <summary>
    /// 构造 EventStreamCenter。
    /// </summary>
    /// <param name="options">
    /// EventStream 队列配置。
    /// </param>
    /// <param name="initialSlotCapacity">
    /// 初始 Actor slot 容量。
    /// </param>
    public EventStreamCenter(
        EventStreamOptions options,
        int initialSlotCapacity)
    {
        _segmentPool = new EventStreamSegmentPool<TEvent>(
            options);

        _head = null;
        _tail = null;
        _count = 0;

        _handlersBySlot =
            new ActorEventHandler<TEvent>?[Math.Max(initialSlotCapacity, 1)];

        _aliveGenerations =
            new int[Math.Max(initialSlotCapacity, 1)];

        Array.Fill(
            _aliveGenerations,
            -1);
    }

    /// <summary>
    /// 注册 Actor 对当前事件类型的处理委托。
    /// </summary>
    /// <param name="actorId">
    /// 目标 ActorId。
    /// </param>
    /// <param name="handler">
    /// 事件处理委托。
    /// 该委托已经保存具体 Actor 实例和方法入口。
    /// </param>
    public void Register(
        ActorId actorId,
        ActorEventHandler<TEvent> handler)
    {
        int slotIndex = actorId.SlotIndex;

        EnsureSlotCapacity(
            slotIndex);

        _handlersBySlot[slotIndex] = handler;
        _aliveGenerations[slotIndex] = actorId.Generation;
    }

    /// <summary>
    /// 取消注册 Actor 对当前事件类型的处理委托。
    /// </summary>
    /// <param name="actorId">
    /// 目标 ActorId。
    /// </param>
    public void Unregister(
        ActorId actorId)
    {
        int slotIndex = actorId.SlotIndex;

        if ((uint)slotIndex >= (uint)_handlersBySlot.Length)
        {
            return;
        }

        _handlersBySlot[slotIndex] = null;
        _aliveGenerations[slotIndex] = -1;
    }

    /// <summary>
    /// 投递事件到当前事件流。
    /// </summary>
    /// <param name="actorId">
    /// 目标 ActorId。
    /// </param>
    /// <param name="value">
    /// 要投递的事件值。
    /// </param>
    /// <returns>
    /// true：写入成功。
    /// false：目标 Actor 已失效或 slot 越界。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Post(
        ActorId actorId,
        in TEvent value)
    {
        int slotIndex = actorId.SlotIndex;

        if ((uint)slotIndex >= (uint)_aliveGenerations.Length)
        {
            return false;
        }

        if (_aliveGenerations[slotIndex] != actorId.Generation)
        {
            return false;
        }

        EventStreamSegment<TEvent> tail =
            EnsureWritableTail();

        tail.Items[tail.WriteIndex] = new EventStreamMail<TEvent>
        {
            SlotIndex = slotIndex,
            Generation = actorId.Generation,
            Value = value
        };

        tail.WriteIndex++;
        _count++;

        return true;
    }

    /// <summary>
    /// Pump 当前事件流。
    /// </summary>
    /// <param name="maxCount">
    /// 本次最多处理多少封邮件。
    /// 用于接入 RuntimeFrameBudget。
    /// </param>
    /// <returns>
    /// 实际处理数量。
    /// </returns>
    public int Pump(
        int maxCount)
    {
        int processed = 0;

        while (_head != null &&
               _count > 0 &&
               processed < maxCount)
        {
            EventStreamSegment<TEvent> head = _head;

            while (!head.IsEmpty &&
                   processed < maxCount)
            {
                ref EventStreamMail<TEvent> mail =
                    ref head.Items[head.ReadIndex];

                Dispatch(
                    in mail);

                ClearMailIfNeeded(
                    ref mail);

                head.ReadIndex++;
                _count--;
                processed++;
            }

            if (head.IsEmpty)
            {
                ReleaseHeadSegment();
            }
        }

        return processed;
    }

    private void EnsureSlotCapacity(
        int slotIndex)
    {
        if ((uint)slotIndex < (uint)_handlersBySlot.Length)
        {
            return;
        }

        int oldLength = _handlersBySlot.Length;
        int newLength = oldLength == 0 ? 4 : oldLength;

        while (newLength <= slotIndex)
        {
            newLength *= 2;
        }

        Array.Resize(
            ref _handlersBySlot,
            newLength);

        Array.Resize(
            ref _aliveGenerations,
            newLength);

        Array.Fill(
            _aliveGenerations,
            -1,
            oldLength,
            newLength - oldLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventStreamSegment<TEvent> EnsureWritableTail()
    {
        EventStreamSegment<TEvent>? tail = _tail;

        if (tail != null && !tail.IsFull)
        {
            return tail;
        }

        EventStreamSegment<TEvent> next =
            _segmentPool.Rent();

        if (_tail == null)
        {
            _head = next;
            _tail = next;
            return next;
        }

        _tail.Next = next;
        _tail = next;

        return next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Dispatch(
        in EventStreamMail<TEvent> mail)
    {
        int slotIndex = mail.SlotIndex;

        if ((uint)slotIndex >= (uint)_aliveGenerations.Length)
        {
            return;
        }

        if (_aliveGenerations[slotIndex] != mail.Generation)
        {
            return;
        }

        ActorEventHandler<TEvent>? handler =
            _handlersBySlot[slotIndex];

        if (handler == null)
        {
            return;
        }

        handler(
            in mail.Value);
    }

    private void ReleaseHeadSegment()
    {
        EventStreamSegment<TEvent>? oldHead = _head;

        if (oldHead == null)
        {
            return;
        }

        EventStreamSegment<TEvent>? next = oldHead.Next;

        _head = next;

        if (next == null)
        {
            _tail = null;
        }

        _segmentPool.Return(
            oldHead);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearMailIfNeeded(
        ref EventStreamMail<TEvent> mail)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>())
        {
            mail = default;
        }
    }

    public void Clear()
    {
        while (_head != null)
        {
            ReleaseHeadSegment();
        }

        _count = 0;
    }

    public void TrimPool()
    {
        _segmentPool.Clear();
    }
}
```

---

## 9. EventStream Runtime

### 9.1 IEventStreamCenterRuntime

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 非泛型 EventStreamCenter 运行时接口。
///
/// 作用：
/// 让 ActorWorld 可以在 dirty center 队列中统一 Pump 不同 TEvent 的中心。
/// </summary>
internal interface IEventStreamCenterRuntime
{
    bool IsEmpty { get; }

    int Pump(
        int maxCount);

    void Clear();

    void TrimPool();
}
```

---

### 9.2 EventStreamRuntime

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 每个 TEvent 的 EventStreamCenter 运行时入口。
///
/// 作用：
/// 1. 按 RuntimeIndex 隔离不同 ActorWorld。
/// 2. 避免 Dictionary<Type, object>。
/// 3. 使用泛型静态缓存定位事件中心。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal static class EventStreamRuntime<TEvent>
    where TEvent : struct
{
    private static EventStreamCenter<TEvent>?[] s_centers =
        Array.Empty<EventStreamCenter<TEvent>?>();

    public static EventStreamCenter<TEvent>? GetCenterUnchecked(
        int runtimeIndex)
    {
        if ((uint)runtimeIndex >= (uint)s_centers.Length)
        {
            return null;
        }

        return s_centers[runtimeIndex];
    }

    public static EventStreamCenter<TEvent> GetOrCreateCenter(
        int runtimeIndex,
        ActorEventStreamPlan<TEvent> plan,
        int initialSlotCapacity)
    {
        EnsureRuntimeCapacity(
            runtimeIndex);

        EventStreamCenter<TEvent>? center =
            s_centers[runtimeIndex];

        if (center != null)
        {
            return center;
        }

        center = new EventStreamCenter<TEvent>(
            plan.StreamOptions,
            initialSlotCapacity);

        s_centers[runtimeIndex] = center;
        return center;
    }

    private static void EnsureRuntimeCapacity(
        int runtimeIndex)
    {
        if ((uint)runtimeIndex < (uint)s_centers.Length)
        {
            return;
        }

        int newLength = s_centers.Length == 0 ? 4 : s_centers.Length;

        while (newLength <= runtimeIndex)
        {
            newLength *= 2;
        }

        Array.Resize(
            ref s_centers,
            newLength);
    }
}
```

---

## 10. ActorTypeMeta 重构

### 10.1 ActorBehaviourEntry

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 行为条目。
///
/// 作用：
/// 1. 保存事件类型 ID。
/// 2. 保存事件类型。
/// 3. 保存 handler factory。
/// 4. 保存注册 / 注销函数，供 TypedActorStorage 在 Actor 创建和销毁时调用。
/// </summary>
internal readonly struct ActorBehaviourEntry
{
    public readonly int EventTypeId;
    public readonly Type EventType;
    public readonly object HandlerFactory;
    public readonly ActorStreamRegisterThunk Register;
    public readonly ActorStreamUnregisterThunk Unregister;

    public ActorBehaviourEntry(
        int eventTypeId,
        Type eventType,
        object handlerFactory,
        ActorStreamRegisterThunk register,
        ActorStreamUnregisterThunk unregister)
    {
        EventTypeId = eventTypeId;
        EventType = eventType;
        HandlerFactory = handlerFactory;
        Register = register;
        Unregister = unregister;
    }
}
```

---

### 10.2 Register / Unregister Thunk

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor EventStream 注册函数。
///
/// 作用：
/// 在 Actor 创建时，把具体 Actor 实例的事件处理方法注册到 EventStreamCenter。
/// </summary>
/// <param name="storage">
/// 当前 Actor 类型对应的 TypedStorageRuntime。
/// </param>
/// <param name="rawFactory">
/// 未类型化的 handler factory。
/// </param>
/// <param name="world">
/// 当前 ActorWorld。
/// </param>
/// <param name="actorId">
/// 当前 ActorId。
/// </param>
/// <param name="slotIndex">
/// 当前 Actor slot 下标。
/// </param>
internal delegate void ActorStreamRegisterThunk(
    TypedStorageRuntime storage,
    object rawFactory,
    ActorWorld world,
    ActorId actorId,
    int slotIndex);

/// <summary>
/// Actor EventStream 注销函数。
///
/// 作用：
/// 在 Actor 销毁或 slot 失效时，从 EventStreamCenter 中移除 handler。
/// </summary>
internal delegate void ActorStreamUnregisterThunk(
    TypedStorageRuntime storage,
    ActorWorld world,
    ActorId actorId,
    int slotIndex);
```

---

### 10.3 ActorTypeMetaBuilder.AddBehaviour

```csharp
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed class ActorTypeMetaBuilder
{
    private readonly List<ActorBehaviourEntry> _entries = new();
    private readonly HashSet<int> _eventIds = new();

    /// <summary>
    /// 添加 Actor 事件行为。
    /// </summary>
    /// <param name="handlerFactory">
    /// handler factory。
    /// 作用是在 Actor 创建时，把具体 Actor 实例绑定成 ActorEventHandler。
    /// </param>
    /// <typeparam name="TActor">
    /// Actor 类型。
    /// </typeparam>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
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

        _entries.Add(new ActorBehaviourEntry(
            eventTypeId,
            typeof(TEvent),
            handlerFactory,
            static (storage, rawFactory, world, actorId, slotIndex) =>
            {
                var typedStorage = (TypedActorStorage<TActor>)storage;
                var typedFactory = (ActorBehaviourHandlerFactory<TActor, TEvent>)rawFactory;

                TActor? actor = typedStorage.GetActorAtSlot(slotIndex);

                if (actor == null)
                {
                    return;
                }

                ActorEventHandler<TEvent> handler =
                    typedFactory(actor);

                world.RegisterEventHandler(
                    actorId,
                    handler);
            },
            static (storage, world, actorId, slotIndex) =>
            {
                world.UnregisterEventHandler<TEvent>(
                    actorId);
            }));
    }
}
```

---

## 11. ActorBehaviourGenerator 修改

### 11.1 生成目标

当前生成目标：

```csharp
builder.AddBehaviour<TActor, TEvent>(
    static (TActor actor, in TEvent e) =>
    {
        actor.OnEvent(in e);
    });
```

新生成目标：

```csharp
builder.AddBehaviour<TActor, TEvent>(
    static (TActor actor) => actor.OnEvent);
```

### 11.2 约束保持不变

```text
1. 方法必须是实例方法。
2. 方法必须返回 void。
3. 方法必须只有一个 in struct event 参数。
4. 同一个 Actor 类型不能重复处理同一个 TEvent。
5. Actor class 必须是 partial。
6. Actor class 必须实现 IActor。
```

---

## 12. TypedActorStorage 修改

### 12.1 新增 GetActorAtSlot

```csharp
namespace LayerBase.Actor;

internal sealed class TypedActorStorage<TActor> : TypedStorageRuntime
    where TActor : class, IActor
{
    /// <summary>
    /// 获取指定 slot 上的 Actor。
    /// </summary>
    /// <param name="slotIndex">
    /// Actor slot 下标。
    /// </param>
    /// <returns>
    /// TActor 或 null。
    /// </returns>
    internal TActor? GetActorAtSlot(
        int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_actors.Length)
        {
            return null;
        }

        return _actors[slotIndex];
    }
}
```

---

### 12.2 Actor 创建后注册 handlers

`ActorWorld.CreateActor<TActor>` 中推荐顺序：

```csharp
generated.ActorInit(new ActorContext(this, actorId));
storage.RegisterStreamHandlers(actorId, slotIndex, this);
storage.RegisterLifecycleInterfaces(actor, actorId, slotIndex, this);
```

原因：

```text
1. ActorInit 先写 Context，保证 handler 内可以访问 Context。
2. StreamHandler 注册在生命周期 Start 前完成。
3. Start 中如果立即 Post 事件，handler 已经可用。
```

---

### 12.3 RegisterStreamHandlers

```csharp
namespace LayerBase.Actor;

internal sealed class TypedActorStorage<TActor> : TypedStorageRuntime
    where TActor : class, IActor
{
    /// <summary>
    /// 注册当前 Actor slot 支持的所有事件 handler。
    /// </summary>
    /// <param name="actorId">
    /// 当前 ActorId。
    /// </param>
    /// <param name="slotIndex">
    /// 当前 Actor slot 下标。
    /// </param>
    /// <param name="world">
    /// 当前 ActorWorld。
    /// </param>
    internal void RegisterStreamHandlers(
        ActorId actorId,
        int slotIndex,
        ActorWorld world)
    {
        if (_meta == null)
        {
            return;
        }

        foreach (ActorBehaviourEntry entry in _meta.Behaviours)
        {
            entry.Register(
                this,
                entry.HandlerFactory,
                world,
                actorId,
                slotIndex);
        }
    }

    /// <summary>
    /// 注销当前 Actor slot 支持的所有事件 handler。
    /// </summary>
    /// <param name="actorId">
    /// 当前 ActorId。
    /// </param>
    /// <param name="slotIndex">
    /// 当前 Actor slot 下标。
    /// </param>
    /// <param name="world">
    /// 当前 ActorWorld。
    /// </param>
    internal void UnregisterStreamHandlers(
        ActorId actorId,
        int slotIndex,
        ActorWorld world)
    {
        if (_meta == null)
        {
            return;
        }

        foreach (ActorBehaviourEntry entry in _meta.Behaviours)
        {
            entry.Unregister(
                this,
                world,
                actorId,
                slotIndex);
        }
    }
}
```

---

### 12.4 Destroy 时注销 handlers

`FinalizeDestroySlot` 在清理 Actor 引用前调用：

```csharp
UnregisterStreamHandlers(
    new ActorId(
        archetypeId: _archetypeId,
        slotIndex: slotIndex,
        generation: _generations[slotIndex]),
    slotIndex,
    world);
```

推荐位置：

```text
1. 设置 Destroying 状态。
2. 调用 IDestroy.Destroy。
3. UnregisterLifecycleInterfaces。
4. UnregisterStreamHandlers。
5. 清空 _actors[slotIndex]。
6. generation++。
7. freeList.Push(slotIndex)。
```

这样 handlerTable 不会继续强引用 Actor。

---

## 13. ActorWorld EventStream API

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    /// <summary>
    /// 注册 Actor 对某个事件类型的处理委托。
    /// </summary>
    /// <param name="actorId">
    /// ActorId。
    /// </param>
    /// <param name="handler">
    /// 事件处理委托。
    /// 委托内部已经保存目标 Actor 实例。
    /// </param>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    internal void RegisterEventHandler<TEvent>(
        ActorId actorId,
        ActorEventHandler<TEvent> handler)
        where TEvent : struct
    {
        ActorEventStreamPlan<TEvent> plan =
            ActorEventStreamPlanBuilder.Build<TEvent>(
                DefaultMailOptions);

        EventStreamCenter<TEvent> center =
            EventStreamRuntime<TEvent>.GetOrCreateCenter(
                RuntimeIndex,
                plan,
                InitialActorCapacity);

        center.Register(
            actorId,
            handler);

        RegisterEventStreamCenter(
            plan.EventId,
            center);
    }

    /// <summary>
    /// 注销 Actor 对某个事件类型的处理委托。
    /// </summary>
    /// <param name="actorId">
    /// ActorId。
    /// </param>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    internal void UnregisterEventHandler<TEvent>(
        ActorId actorId)
        where TEvent : struct
    {
        EventStreamCenter<TEvent>? center =
            EventStreamRuntime<TEvent>.GetCenterUnchecked(
                RuntimeIndex);

        center?.Unregister(
            actorId);
    }

    /// <summary>
    /// 投递事件到目标 Actor。
    /// </summary>
    /// <param name="actorId">
    /// 目标 ActorId。
    /// </param>
    /// <param name="value">
    /// 要投递的事件值。
    /// </param>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    /// <returns>
    /// true：投递成功。
    /// false：目标 Actor 已失效或事件没有可用 EventStreamCenter。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool PostTo<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        EventStreamCenter<TEvent>? center =
            EventStreamRuntime<TEvent>.GetCenterUnchecked(
                RuntimeIndex);

        if (center == null)
        {
            return false;
        }

        bool wasEmpty = center.IsEmpty;

        bool posted = center.Post(
            actorId,
            in value);

        if (posted && wasEmpty)
        {
            MarkDirtyEventCenter(
                center);
        }

        return posted;
    }
}
```

---

## 14. Dirty Center Pump

### 14.1 Dirty Center 队列

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    private IEventStreamCenterRuntime?[] _dirtyEventCenters =
        Array.Empty<IEventStreamCenterRuntime?>();

    private int _dirtyEventCenterCount;

    /// <summary>
    /// 标记一个 EventStreamCenter 为 dirty。
    /// </summary>
    /// <param name="center">
    /// 需要被 Pump 的事件中心。
    /// </param>
    private void MarkDirtyEventCenter(
        IEventStreamCenterRuntime center)
    {
        EnsureDirtyEventCenterCapacity(
            _dirtyEventCenterCount + 1);

        _dirtyEventCenters[_dirtyEventCenterCount] = center;
        _dirtyEventCenterCount++;
    }

    /// <summary>
    /// 确保 dirty event center 数组容量足够。
    /// </summary>
    /// <param name="required">
    /// 需要的容量。
    /// </param>
    private void EnsureDirtyEventCenterCapacity(
        int required)
    {
        if (required <= _dirtyEventCenters.Length)
        {
            return;
        }

        int newLength = _dirtyEventCenters.Length == 0 ? 4 : _dirtyEventCenters.Length;

        while (newLength < required)
        {
            newLength *= 2;
        }

        Array.Resize(
            ref _dirtyEventCenters,
            newLength);
    }
}
```

---

### 14.2 PumpEventStreams

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    /// <summary>
    /// Pump 所有 dirty EventStreamCenter。
    /// </summary>
    /// <param name="budget">
    /// 帧预算。
    /// 用于限制本帧最多处理多少事件。
    /// </param>
    private void PumpEventStreams(
        ref RuntimeFrameBudget budget)
    {
        int writeIndex = 0;

        for (int i = 0; i < _dirtyEventCenterCount; i++)
        {
            IEventStreamCenterRuntime? center =
                _dirtyEventCenters[i];

            if (center == null)
            {
                continue;
            }

            if (!budget.HasRemainingEventBudget())
            {
                _dirtyEventCenters[writeIndex] = center;
                writeIndex++;
                continue;
            }

            int remaining = budget.RemainingEvents;

            int processed = center.Pump(
                remaining);

            budget.ConsumeEvents(
                processed);

            if (!center.IsEmpty)
            {
                _dirtyEventCenters[writeIndex] = center;
                writeIndex++;
            }
        }

        for (int i = writeIndex; i < _dirtyEventCenterCount; i++)
        {
            _dirtyEventCenters[i] = null;
        }

        _dirtyEventCenterCount = writeIndex;
    }
}
```

`RuntimeFrameBudget.RemainingEvents` 与 `ConsumeEvents` 如果当前不存在，需要补充等价 API，或在现有 `HasRemainingEventBudget()` 基础上实现内部消耗。

---

## 15. ProjectedActorRef 缓存

### 15.1 组件

```csharp
namespace LayerBase.Actor;

/// <summary>
/// ECS Entity 到 Actor 的热路径缓存引用。
///
/// 作用：
/// 1. 避免每次 Query 时通过 Entity 反查 ProjectionMeta。
/// 2. 让热路径可以直接读取 ActorId。
/// 3. Actor 销毁、解绑或重绑时，通过 ActorId.Invalid 或 HasActor=false 让缓存失效。
/// </summary>
public struct ProjectedActorRef
{
    /// <summary>
    /// 当前 Entity 绑定的 ActorId。
    /// </summary>
    public ActorId ActorId;

    /// <summary>
    /// 当前缓存是否有效。
    /// </summary>
    public bool HasActor;

    /// <summary>
    /// 绑定版本。
    /// 用于调试 rebind 或缓存同步问题。
    /// </summary>
    public int Version;
}
```

---

### 15.2 Query 模板

```csharp
_ecsWorld.Query(
    in query,
    (
        ref Position position,
        ref Velocity velocity,
        ref ProjectedActorRef actorRef) =>
    {
        ActorId actorId = actorRef.ActorId;

        if (!actorRef.HasActor || !actorId.IsValid)
        {
            return;
        }

        MoveEvent moveEvent = new MoveEvent(
            deltaX: velocity.X,
            deltaY: velocity.Y);

        actorWorld.PostTo(
            actorId,
            in moveEvent);
    });
```

---

## 16. batch 模板优化

EventStream 后端下，batch 的职责是减少 Query 端多次调用的循环开销。

推荐模板从：

```csharp
for (int i = 0; i < count; i++)
{
    actorWorld.PostTo(
        actorIds[i],
        in events[i]);
}
```

改成：

```csharp
actorWorld.PostBatch(
    actorIds,
    events,
    count);
```

---

### 16.1 PostBatch

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    /// <summary>
    /// 批量投递事件。
    /// </summary>
    /// <param name="actorIds">
    /// 目标 ActorId 数组。
    /// </param>
    /// <param name="events">
    /// 事件数组。
    /// events[i] 会投递给 actorIds[i]。
    /// </param>
    /// <param name="count">
    /// 本次实际投递数量。
    /// </param>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostBatch<TEvent>(
        ActorId[] actorIds,
        TEvent[] events,
        int count)
        where TEvent : struct
    {
        EventStreamCenter<TEvent>? center =
            EventStreamRuntime<TEvent>.GetCenterUnchecked(
                RuntimeIndex);

        if (center == null)
        {
            return;
        }

        bool wasEmpty = center.IsEmpty;

        int i = 0;
        int unrolledCount = count - (count % 8);

        for (; i < unrolledCount; i += 8)
        {
            center.Post(actorIds[i], in events[i]);
            center.Post(actorIds[i + 1], in events[i + 1]);
            center.Post(actorIds[i + 2], in events[i + 2]);
            center.Post(actorIds[i + 3], in events[i + 3]);
            center.Post(actorIds[i + 4], in events[i + 4]);
            center.Post(actorIds[i + 5], in events[i + 5]);
            center.Post(actorIds[i + 6], in events[i + 6]);
            center.Post(actorIds[i + 7], in events[i + 7]);
        }

        for (; i < count; i++)
        {
            center.Post(
                actorIds[i],
                in events[i]);
        }

        if (!center.IsEmpty && wasEmpty)
        {
            MarkDirtyEventCenter(
                center);
        }
    }
}
```

---

## 17. 生命周期语义

### 17.1 Actor 创建

```text
1. new 或 pool rent Actor。
2. 获取 IGeneratedActorMeta。
3. ActorTypeMetaCache.GetOrBuild。
4. TypedActorStorage.AllocateSlot。
5. 构造 ActorId。
6. generated.ActorInit(new ActorContext(world, actorId))。
7. storage.RegisterStreamHandlers(actorId, slotIndex, world)。
8. storage.RegisterLifecycleInterfaces(actor, actorId, slotIndex, world)。
```

---

### 17.2 Actor 销毁

```text
1. MarkPendingDestroy 设置状态。
2. SweepPendingDestroy 进入 FinalizeDestroySlot。
3. 设置 Destroying。
4. 调用 IDestroy.Destroy。
5. UnregisterLifecycleInterfaces。
6. UnregisterStreamHandlers。
7. 清空 _actors[slotIndex]。
8. generation++。
9. slotIndex 回到 freeList。
```

---

### 17.3 旧邮件安全

旧邮件包含：

```text
slotIndex
generation
event
```

如果 Actor 已销毁并复用 slot：

```text
EventStreamCenter._aliveGenerations[slotIndex] != mail.Generation
```

旧邮件会被跳过。

---

## 18. 语义约束

新 EventStream Actor 消息系统只提供：

```text
QueueGrow
```

不再在 Actor mailbox 层提供：

```text
Latest
Dirty
QueuedRejectNew
QueuedDropOldest
Merge
每 Actor 私有 mailbox capacity
每 Actor 私有 backpressure
```

这些语义应交给：

```text
1. ECS 状态组件。
2. 事件生产侧去重。
3. 业务系统节流。
4. 普通 EventCenter 的合并策略。
```

---

## 19. Benchmark

新增：

```text
StreamBackend: Post Only ×1000
StreamBackend: Pump Only ×1000
StreamBackend: Post + Pump ×1000
StreamBackend: Post + Pump ×10000
StreamBackend: FullPipeline ×1000
StreamBackend: FullPipeline ×10000
StreamBackend: SegmentPool Burst ×10000
StreamBackend: SegmentPool Recycle ×10000
```

对照：

```text
Actor: PostTo Only ×1000
Actor: Pump Only ×1000
Actor: PostTo + Pump ×1000
FullPipeline ×1000
```

目标：

```text
Post Only ×1000 明显低于旧 PostTo。
FullPipeline ×1000 明显低于旧 FullPipeline。
SegmentPool 在消息高峰后不会长期保留超量 Segment。
Hot path 不新增 GC allocation。
```

---

## 20. 提交顺序

### Commit 1：EventStream 基础类型

```text
ActorEventHandler
ActorBehaviourHandlerFactory
EventStreamOptions
EventStreamMail
EventStreamSegment
EventStreamSegmentPool
EventStreamCenter
IEventStreamCenterRuntime
EventStreamRuntime
```

### Commit 2：EventMetaData 配置接入

```text
扩展 ActorMailOptions。
新增 ActorEventStreamPlan。
新增 ActorEventStreamPlanBuilder。
保证 EventStreamSegmentPool 可通过 EventMetaData<TEvent>.ActorMailOptions 配置。
```

### Commit 3：ActorBehaviourGenerator 改造

```text
AddBehaviour 生成 handler factory。
从 static (actor, in e) => actor.Method(in e)
改为 static actor => actor.Method。
```

### Commit 4：ActorTypeMetaBuilder 改造

```text
ActorBehaviourInvoker 替换为 ActorBehaviourHandlerFactory。
ActorBehaviourEntry 增加 Register / Unregister thunk。
```

### Commit 5：TypedActorStorage 接入

```text
新增 GetActorAtSlot。
Actor 创建后注册 StreamHandlers。
Actor 销毁前注销 StreamHandlers。
删除 EventColumn 构建路径。
```

### Commit 6：ActorWorld.Post/Pump 接入

```text
PostTo<TEvent> 改为 EventStreamCenter<TEvent>.Post。
新增 dirty EventStreamCenter 队列。
Pump 接入 EventStreamCenter.Pump。
```

### Commit 7：ProjectedActorRef

```text
Bind / Unbind / Destroy 同步 ProjectedActorRef。
模板 Query 直接读取 ProjectedActorRef.ActorId。
```

### Commit 8：batch 模板优化

```text
PostBatch<TEvent>。
模板从逐个 PostTo 改为 PostBatch。
8 路循环展开。
```

### Commit 9：Benchmark 与测试

```text
新增 StreamBackend benchmark。
新增 SegmentPool 回收测试。
新增 Actor destroy 后旧邮件跳过测试。
新增 EventMetaData 配置测试。
```

---

## 21. 验收标准

### 21.1 正确性

```text
1. [ActorBehaviour] 方法可以被注册为 ActorEventHandler<TEvent>。
2. Actor 创建后可以接收对应事件。
3. Actor 销毁后 handlerTable 不再强引用 Actor。
4. slot 复用后旧邮件不会打到新 Actor。
5. EventMetaData 可以配置 SegmentCapacity 和 MaxRetainedSegments。
6. Segment 读空后会回收到池。
7. 池满后多余 Segment 会断引用。
8. ProjectedActorRef 可替代 ProjectionMeta 热路径反查。
```

---

### 21.2 性能

```text
1. PostTo<TEvent> 不再进入 EventPostState / EventPostRow / EventMail[slotIndex]。
2. Pump 不再取 IActor。
3. Pump 不再走 ActorBehaviourInvoker<TActor,TEvent>(actor, in value)。
4. Pump 只做 generation check + delegate invoke。
5. FullPipeline ×1000 应低于旧架构。
6. Hot path 保持 0 allocation。
```

---

## 22. 最终架构

最终 Actor 消息运行时：

```text
ActorBehaviourGenerator
→ ActorBehaviourHandlerFactory<TActor,TEvent>
→ Actor 创建时注册 ActorEventHandler<TEvent>
→ EventStreamCenter<TEvent>.handlerTable[slotIndex]

PostTo
→ EventStreamCenter<TEvent>.Post
→ Segment tail append

Pump
→ dirty EventStreamCenter
→ Segment head read
→ handlerTable[slotIndex]
→ handler(in event)
```

最终 Projection 热路径：

```text
ECS Query
→ ProjectedActorRef.ActorId
→ PostBatch / PostTo
→ EventStreamCenter<TEvent>
```

最终内存模型：

```text
每个 TEvent 一个 EventStreamCenter
每个 EventStreamCenter 一个 SegmentPool
每个 Segment 固定容量
读空 Segment 回收
池满 Segment 交给 GC
```
