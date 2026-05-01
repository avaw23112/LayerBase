# EventTypeId 静态化改造文档

## 1. 修改目标

当前 `EventTypeIdProvider` 通过字典维护事件类型和事件 ID 的映射关系：

```csharp
Dictionary<Type, int> s_typeToId
Dictionary<int, Type> s_idToType
```

这种设计具备动态查询能力，但事件系统的热路径会经过 `Dictionary<Type, int>` 查找，并且需要 `lock` 保护。

本次修改目标是：

1. 移除 `Type -> id` 字典查找。
2. 移除 `id -> Type` 反查能力。
3. 事件系统内部统一使用泛型静态字段获取事件 ID。
4. 将事件 ID 获取从“字典查找”优化为“静态字段读取”。

---

## 2. 核心方案

新的事件类型 ID 获取方式统一为：

```csharp
EventTypeId<TEvent>.Id
```

例如：

```csharp
var id = EventTypeId<CardPlayedEvent>.Id;
```

`EventTypeId<CardPlayedEvent>` 和 `EventTypeId<DamageEvent>` 是两个不同的闭合泛型类型。

**闭合泛型类型**：已经填入具体类型参数的泛型类型。

例如：

```csharp
EventTypeId<CardPlayedEvent>
EventTypeId<DamageEvent>
```

它们会各自拥有独立的静态字段 `Id`。

因此：

```csharp
EventTypeId<CardPlayedEvent>.Id
```

和：

```csharp
EventTypeId<DamageEvent>.Id
```

不会互相影响。

---

## 3. 修改前实现

```csharp
namespace LayerBase.Core.Event;

internal static class EventTypeIdProvider
{
    private static int s_nextId;
    private static readonly Dictionary<Type, int> s_typeToId = new();
    private static readonly Dictionary<int, Type> s_idToType = new();
    private static readonly object s_lock = new();

    public static int GetOrCreateId(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        lock (s_lock)
        {
            if (s_typeToId.TryGetValue(type, out var id)) return id;
            id = Interlocked.Increment(ref s_nextId);
            s_typeToId[type] = id;
            s_idToType[id] = type;
            return id;
        }
    }

    public static Type? GetType(int id)
    {
        lock (s_lock)
        {
            if (s_idToType.TryGetValue(id, out var type)) return type;
        }

        return null;
    }
}

internal class EventTypeId<Value>
{
    public static readonly int Id = EventTypeIdProvider.GetOrCreateId(typeof(Value));
}

internal class EventTypeId
{
    public static int GetId(Type type)
    {
        return EventTypeIdProvider.GetOrCreateId(type);
    }

    public static Type? GetType(int id)
    {
        return EventTypeIdProvider.GetType(id);
    }
}
```

---

## 4. 修改后实现

建议替换为以下实现：

```csharp
namespace LayerBase.Core.Event;

using System.Threading;

/// <summary>
/// 事件类型 ID 分配器。
///
/// 作用：
/// 为每一个首次访问的事件类型分配一个全局唯一的 int ID。
///
/// 注意：
/// 该类型只负责分配 ID。
/// 不再保存 Type -> id 的字典。
/// 不再保存 id -> Type 的反查表。
/// </summary>
internal static class EventTypeIdAllocator
{
    /// <summary>
    /// 下一个可分配的事件类型 ID。
    ///
    /// 说明：
    /// 0 通常保留为无效 ID。
    /// 第一个真实事件类型 ID 从 1 开始。
    /// </summary>
    private static int s_nextId;

    /// <summary>
    /// 分配一个新的事件类型 ID。
    ///
    /// 返回：
    /// 当前事件类型对应的全局唯一 int ID。
    ///
    /// 逻辑说明：
    /// Interlocked.Increment 会以线程安全的方式递增 s_nextId。
    /// 即使多个事件类型在多个线程中同时首次访问，也不会获得重复 ID。
    ///
    /// Interlocked：
    /// .NET 提供的原子操作工具。
    /// 原子操作指不会被其他线程打断的单个操作。
    /// </summary>
    public static int Allocate()
    {
        return Interlocked.Increment(ref s_nextId);
    }
}

/// <summary>
/// 每一种事件类型对应的静态 ID 容器。
///
/// 设计说明：
/// EventTypeId<TEvent> 会为每个 TEvent 生成独立的静态字段。
/// 例如：
/// EventTypeId<CardPlayedEvent>.Id
/// EventTypeId<DamageEvent>.Id
/// 是两份不同的静态字段。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// 例如 CardPlayedEvent、DamageEvent、TurnStartedEvent。
/// </typeparam>
internal static class EventTypeId<TEvent>
{
    /// <summary>
    /// 当前 TEvent 对应的事件类型 ID。
    ///
    /// 逻辑说明：
    /// 1. 第一次访问 EventTypeId<TEvent>.Id 时，会调用 EventTypeIdAllocator.Allocate()。
    /// 2. Allocate() 会分配一个新的唯一 int ID。
    /// 3. 后续再次访问 EventTypeId<TEvent>.Id 时，不会再次分配。
    /// 4. 后续访问只是读取静态 readonly 字段。
    ///
    /// 性能说明：
    /// 热路径中不再发生 Dictionary 查找。
    /// 热路径中不再发生 lock。
    /// 热路径中不再使用 Type 作为 key。
    /// </summary>
    public static readonly int Id = EventTypeIdAllocator.Allocate();
}
```

---

## 5. 需要删除的内容

删除 `EventTypeIdProvider`：

```csharp
internal static class EventTypeIdProvider
```

删除以下 API：

```csharp
EventTypeId.GetId(Type type)
EventTypeId.GetType(int id)
EventTypeIdProvider.GetOrCreateId(Type type)
EventTypeIdProvider.GetType(int id)
```

删除以下字段：

```csharp
private static readonly Dictionary<Type, int> s_typeToId;
private static readonly Dictionary<int, Type> s_idToType;
private static readonly object s_lock;
```

如果项目中没有其他地方需要 `EventTypeId` 非泛型类，也可以直接删除：

```csharp
internal class EventTypeId
```

---

## 6. 调用方式修改

### 6.1 修改前

```csharp
var eventId = EventTypeId.GetId(typeof(TEvent));
```

### 6.2 修改后

```csharp
var eventId = EventTypeId<TEvent>.Id;
```

---

## 7. Publish 修改示例

```csharp
public void Publish<TEvent>(TEvent eventValue)
{
    // eventValue：
    // 本次发布的事件数据。
    // 例如 CardPlayedEvent、DamageEvent、TurnStartedEvent。

    // 获取当前事件类型的静态 ID。
    // 这里不会查 Dictionary。
    // 这里不会 lock。
    // 这里不会使用 typeof(TEvent) 作为字典 key。
    var eventId = EventTypeId<TEvent>.Id;

    // 根据 eventId 找到对应的订阅者列表并分发事件。
    // Dispatch 是事件系统内部的分发逻辑。
    Dispatch(eventId, eventValue);
}
```

---

## 8. Subscribe 修改示例

```csharp
public void Subscribe<TEvent>(Action<TEvent> handler)
{
    // handler：
    // 用户传入的事件处理函数。
    // 当 TEvent 类型的事件被发布时，该函数会被调用。

    if (handler == null)
    {
        throw new ArgumentNullException(nameof(handler));
    }

    // 获取当前订阅事件类型的静态 ID。
    // 订阅和发布使用同一个 EventTypeId<TEvent>.Id。
    // 因此它们会落到同一个事件分组中。
    var eventId = EventTypeId<TEvent>.Id;

    // 将处理函数注册到 eventId 对应的订阅者列表中。
    AddSubscriber(eventId, handler);
}
```

---

## 9. 统一替换规则

### 9.1 替换 `EventTypeId.GetId(typeof(TEvent))`

替换前：

```csharp
EventTypeId.GetId(typeof(TEvent))
```

替换后：

```csharp
EventTypeId<TEvent>.Id
```

---

### 9.2 替换 `EventTypeIdProvider.GetOrCreateId(typeof(TEvent))`

替换前：

```csharp
EventTypeIdProvider.GetOrCreateId(typeof(TEvent))
```

替换后：

```csharp
EventTypeId<TEvent>.Id
```

---

### 9.3 删除 `EventTypeId.GetType(eventId)`

替换前：

```csharp
EventTypeId.GetType(eventId)
```

替换后：

```csharp
// 删除该逻辑。
// 新方案不再支持 id -> Type 反查。
```

如果这里只是用于日志，可以改成只输出 `eventId`：

```csharp
Console.WriteLine($"EventId: {eventId}");
```

---

## 10. 注意事项

### 10.1 不再支持动态 Type 查 ID

新方案不支持：

```csharp
var id = EventTypeId.GetId(runtimeType);
```

原因是该接口依赖运行时 `Type` 对象。

**运行时 Type 对象**：程序运行过程中拿到的类型信息。

例如：

```csharp
var runtimeType = eventValue.GetType();
```

这种场景无法直接访问：

```csharp
EventTypeId<TEvent>.Id
```

因为 `TEvent` 必须通过泛型参数明确传入。

---

### 10.2 不建议使用 object 发布事件

不推荐：

```csharp
object eventValue = new DamageEvent();
Publish(eventValue);
```

原因是这样调用时，`TEvent` 会被推断成 `object`。

实际拿到的是：

```csharp
EventTypeId<object>.Id
```

而不是：

```csharp
EventTypeId<DamageEvent>.Id
```

推荐写法：

```csharp
Publish(new DamageEvent());
```

或者显式指定类型：

```csharp
Publish<DamageEvent>(damageEvent);
```

---

### 10.3 ID 不保证跨进程稳定

该 ID 是运行时分配的。

第一次运行时可能是：

```text
DamageEvent -> 1
CardPlayedEvent -> 2
```

第二次运行时可能是：

```text
CardPlayedEvent -> 1
DamageEvent -> 2
```

原因是：谁先访问 `EventTypeId<TEvent>.Id`，谁就先获得 ID。

因此该 ID 适合用于：

```text
事件分发
订阅表索引
运行时缓存
数组下标
```

不适合用于：

```text
存档
网络协议
配置文件
跨进程通信
```

---

## 11. 性能变化

修改前热路径：

```text
typeof(TEvent)
Dictionary<Type, int>.TryGetValue
lock
return id
```

修改后热路径：

```text
EventTypeId<TEvent>.Id
```

也就是读取一个泛型静态字段。

**热路径**：事件系统中高频执行的代码路径。  
例如 `Publish<TEvent>` 在一帧内可能被调用很多次，因此它属于热路径。

**泛型静态字段**：泛型类型中的静态字段会根据具体泛型参数分开存储。

例如：

```csharp
EventTypeId<A>.Id
EventTypeId<B>.Id
```

它们是两份不同的字段。

---

## 12. 最终结论

本次修改后，事件类型 ID 系统只保留：

```csharp
EventTypeId<TEvent>.Id
```

不再支持：

```csharp
EventTypeId.GetId(Type type)
EventTypeId.GetType(int id)
```

事件系统内部应统一使用：

```csharp
var eventId = EventTypeId<TEvent>.Id;
```

这样可以让事件 ID 获取逻辑从字典查找变成静态字段读取，减少锁、字典访问和运行时类型查询带来的开销。
