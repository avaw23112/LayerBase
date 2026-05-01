# LayerBase EventCenter 符号驻留与冷路径故障表设计方案

## 1. 背景

当前 `EventCenter` 的实现里，事件派发表不只保存真正执行所需的 handler delegate，还保存了大量只在异常发生时才会用到的诊断信息，例如：

```text
_syncNames
_asyncNames
_notifyNames
_notifySafeNames
_syncCircuits
_asyncCircuits
_notifySafeCircuits
```

这些信息的问题不在于“它们完全没用”，而在于：

```text
正常事件派发：几乎每次都会发生
异常诊断：极少发生
```

如果把只服务异常诊断的数据长期放在热路径数组旁边，就会让 CPU cache 携带大量正常情况下不会读取的数据。事件系统越强调高频、低延迟，这类数据布局问题越明显。

本方案的目标是：

```text
热路径只保存执行事件必须的数据。
诊断信息全部进入冷路径。
异常发生时，再通过稳定 ID 还原事件名、Layer 名、handler 名。
```

---

## 2. 术语说明

### 2.1 热路径

**热路径** 指高频执行的代码路径。

在 EventCenter 里，典型热路径是：

```text
Send<T>()
Dispatch<T>()
遍历 handler delegate
执行 handler
```

也就是说，只要事件被正常发送，就会反复经过这些代码。

热路径里的数据应该尽量少，最好只包含真正执行必须的数据。

---

### 2.2 冷路径

**冷路径** 指低频执行的代码路径。

在 EventCenter 里，典型冷路径是：

```text
handler 抛异常
异常上报
日志生成
故障熔断
Rebuild 派发表
```

冷路径不是完全不重要，而是不应该污染正常事件派发。

---

### 2.3 Symbol Interning / 符号驻留

**Symbol Interning** 可以理解成“把重复字符串登记成稳定整数 ID”。

例如不要在派发表里反复保存：

```text
"InventoryChangedEvent"
"PlayerDamagedEvent"
"BattleLayer"
"InventoryManager.OnInventoryChanged"
```

而是保存：

```text
EventNameId = 12
LayerNameId = 3
HandlerNameId = 44
```

真正要打印异常日志时，再通过符号表还原：

```text
12 -> "InventoryChangedEvent"
3  -> "BattleLayer"
44 -> "InventoryManager.OnInventoryChanged"
```

这里的“稳定”指的是：

```text
在同一次进程运行期间稳定。
```

如果需要跨运行、跨版本也稳定，就不能只靠运行时递增 ID，需要使用源生成器或固定哈希方案。

---

### 2.4 符号表

**符号表** 是保存字符串和整数 ID 对应关系的数据结构。

它通常包含两部分：

```text
string -> int
int    -> string
```

前者用于登记字符串，后者用于异常时还原字符串。

---

### 2.5 HandlerCircuit

`HandlerCircuit` 可以理解成“handler 故障状态”。

它表示：

```text
某个 handler 是否已经发生过异常？
它是否应该在下一次 Rebuild 时被剔除？
```

它不是正常事件派发所必需的数据，因此不应该跟 handler delegate 数组并排放在热路径里。

---

### 2.6 FaultTable

**FaultTable** 是本方案引入的冷路径故障表。

它保存异常发生后才需要的数据，例如：

```text
LayerIndex
HandlerCircuit
HandlerNameId
EventNameId
```

正常派发时不读取 FaultTable。

只有 handler 抛异常时，才通过当前 handler 的下标去 FaultTable 里找到对应的故障信息。

---

### 2.7 Slot / 槽位

**Slot** 可以理解成数组里的一个固定位置。

例如：

```text
_syncHandlers[5]
_syncFaults[5]
```

它们的下标都是 `5`，表示同一个 handler 的两份信息：

```text
_syncHandlers[5] -> 执行信息
_syncFaults[5]   -> 异常诊断信息
```

---

### 2.8 CPU Cache

**CPU cache** 是 CPU 内部的高速缓存。

CPU 访问内存比访问 cache 慢得多，所以高性能代码会尽量让热路径数据更小、更连续、更容易被 cache 命中。

如果热路径数组旁边放了大量只在异常时使用的字符串引用、诊断对象引用，就会降低 cache 的有效利用率。

---

### 2.9 Rebuild

**Rebuild** 指重新构建事件派发表。

它通常发生在：

```text
新增订阅
移除订阅
handler 被熔断
Layer 拓扑变化
```

Rebuild 不属于每次事件派发的核心热路径，因此可以在 Rebuild 阶段访问更多元数据。

---

## 3. 总体设计原则

### 3.1 派发表只保存执行数据

热路径派发表应该尽量只保存：

```text
handler delegate
handler count
layer range
single fast path cache
bit mask / version
```

不应该保存：

```text
string[]
HandlerCircuit[]
FullName
诊断文本
异常日志上下文
```

---

### 3.2 诊断信息进入冷路径

异常诊断需要的数据放到 `FaultTable`：

```text
FaultTable
├── SyncFaults
├── AsyncFaults
├── SubscribeFaults
└── EventNameId
```

正常派发不访问它。

---

### 3.3 字符串只在注册或异常时处理

字符串拼接、反射名称提取、符号表登记，都应该发生在：

```text
注册阶段
Rebuild 阶段
异常阶段
```

不应该发生在：

```text
每次 Send<T>()
每次 handler 调用
每次正常异步完成
```

---

### 3.4 EventTypeId 与 EventNameId 分离

事件类型 ID 和事件诊断名 ID 不应该混在一起。

建议分成：

```text
EventTypeId<T>.Id       -> 派发、索引、路由
EventTypeSymbol<T>.NameId -> 异常、日志、调试
```

`EventTypeId<T>.Id` 是热路径数据。

`EventTypeSymbol<T>.NameId` 是冷路径诊断数据。

---

## 4. 目标结构

### 4.1 改造前

```text
EventBucket<T>
├── EventHandleDelegate<T>[] _syncHandlers
├── HandlerCircuit[] _syncCircuits
├── string[] _syncNames
├── EventHandleDelegateAsync<T>[] _asyncHandlers
├── HandlerCircuit[] _asyncCircuits
├── string[] _asyncNames
├── EventNotifyDelegate<T>[] _notifyHandlers
├── string[] _notifyNames
├── EventNotifyDelegate<T>[] _subscribeHandlers
├── HandlerCircuit[] _notifySafeCircuits
└── string[] _notifySafeNames
```

问题是：

```text
handler delegate 是热路径数据。
circuit/name 是异常路径数据。
二者被放在了同一层级。
```

---

### 4.2 改造后

```text
EventBucket<T>
├── DispatchTable / 热路径数据
│   ├── EventHandleDelegate<T>[] _syncHandlers
│   ├── EventHandleDelegateAsync<T>[] _asyncHandlers
│   ├── EventNotifyDelegate<T>[] _notifyHandlers
│   ├── EventNotifyDelegate<T>[] _subscribeHandlers
│   ├── ParallelHandlerEntry<T>[] _parallelHandlers
│   ├── LayerRange[] _syncRanges
│   ├── LayerRange[] _asyncRanges
│   └── count / mask / single fast path
│
└── FaultTable / 冷路径数据
    ├── FaultSlot[] SyncFaults
    ├── FaultSlot[] AsyncFaults
    ├── FaultSlot[] SubscribeFaults
    └── int EventNameId
```

正常事件派发只碰 `DispatchTable`。

异常发生后才碰 `FaultTable`。

---

## 5. 事件类型 ID 设计

### 5.1 热路径事件 ID

```csharp
using System.Threading;

namespace LayerBase.Core.Event;

/// <summary>
/// 为每个事件泛型类型分配一个进程内唯一 ID。
/// 这里的 ID 用于派发、缓存、数组索引等热路径逻辑。
/// </summary>
/// <typeparam name="TEvent">
/// 事件结构体类型。
/// 例如 InventoryChangedEvent、PlayerDamagedEvent。
/// 每个不同的 TEvent 都会拥有独立的静态字段。
/// </typeparam>
internal static class EventTypeId<TEvent> where TEvent : struct
{
    /// <summary>
    /// 当前事件类型的运行时 ID。
    /// 该字段在泛型静态类初始化时分配一次。
    /// 初始化完成后，读取它只是普通静态字段读取，不需要 Dictionary 查询。
    /// </summary>
    public static readonly int Id = EventTypeIdAllocator.Next();
}

/// <summary>
/// 事件类型 ID 分配器。
/// 只负责递增分配，不负责 Type 到 ID 的动态查询。
/// </summary>
internal static class EventTypeIdAllocator
{
    /// <summary>
    /// 当前已经分配到的最大事件类型 ID。
    /// 0 保留给 Unknown 或未初始化状态，所以第一次分配会返回 1。
    /// </summary>
    private static int s_nextId;

    /// <summary>
    /// 分配下一个事件类型 ID。
    /// </summary>
    /// <returns>
    /// 新分配的事件类型 ID。
    /// 该 ID 在当前进程运行期间唯一。
    /// </returns>
    public static int Next()
    {
        // Interlocked.Increment 用于原子递增。
        // 原子递增表示多线程同时调用时不会分配出重复 ID。
        return Interlocked.Increment(ref s_nextId);
    }
}
```

### 5.2 冷路径事件名称 ID

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 为事件类型提供诊断名称符号 ID。
/// 它只用于异常日志、调试面板、诊断报告，不参与事件派发。
/// </summary>
/// <typeparam name="TEvent">
/// 事件结构体类型。
/// </typeparam>
internal static class EventTypeSymbol<TEvent> where TEvent : struct
{
    /// <summary>
    /// 当前事件类型名称对应的符号 ID。
    /// 只在异常路径中通过 EventDiagnosticSymbols.Resolve 还原成字符串。
    /// </summary>
    public static readonly int NameId =
        EventDiagnosticSymbols.Intern(typeof(TEvent).FullName ?? typeof(TEvent).Name);
}
```

---

## 6. 诊断符号表设计

```csharp
using System.Threading;

namespace LayerBase.Core.Event;

/// <summary>
/// EventCenter 专用诊断符号表。
/// 它把重复诊断字符串压缩成整数 ID。
/// </summary>
internal static class EventDiagnosticSymbols
{
    /// <summary>
    /// 保护符号登记过程的锁。
    /// 符号登记发生在注册、Rebuild 或首次诊断路径，不在每次事件派发中发生。
    /// </summary>
    private static readonly object s_lock = new();

    /// <summary>
    /// 诊断文本到符号 ID 的映射。
    /// key 是原始字符串，例如 "BattleLayer"。
    /// value 是该字符串对应的整数 ID。
    /// </summary>
    private static readonly Dictionary<string, int> s_textToId = new(StringComparer.Ordinal);

    /// <summary>
    /// 符号 ID 到诊断文本的映射。
    /// 数组下标就是符号 ID。
    /// 0 号位保留，不对应真实字符串。
    /// </summary>
    private static string?[] s_idToText = new string?[256];

    /// <summary>
    /// 当前已经分配到的最大符号 ID。
    /// 0 保留不用，因此第一个真实符号 ID 是 1。
    /// </summary>
    private static int s_nextId;

    /// <summary>
    /// 登记一个诊断字符串，并返回它的符号 ID。
    /// </summary>
    /// <param name="text">
    /// 要登记的诊断字符串。
    /// 可以是事件名、Layer 名、handler 名。
    /// null 或空字符串会被当作 Unknown。
    /// </param>
    /// <returns>
    /// text 对应的符号 ID。
    /// 同一个 text 在同一次运行中会返回同一个 ID。
    /// </returns>
    public static int Intern(string? text)
    {
        // 0 表示未知符号，避免为 null 或空字符串分配真实 ID。
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        lock (s_lock)
        {
            // 如果字符串已经登记过，直接返回已有 ID。
            if (s_textToId.TryGetValue(text, out var existingId))
            {
                return existingId;
            }

            // 分配新的符号 ID。
            var newId = ++s_nextId;

            // 确保 id -> text 数组足够容纳 newId。
            EnsureCapacity(newId);

            // 建立双向映射。
            s_textToId[text] = newId;
            s_idToText[newId] = text;

            return newId;
        }
    }

    /// <summary>
    /// 根据符号 ID 还原诊断字符串。
    /// </summary>
    /// <param name="id">
    /// 要还原的符号 ID。
    /// 0 或越界 ID 会被还原为 "Unknown"。
    /// </param>
    /// <returns>
    /// 符号 ID 对应的诊断字符串。
    /// </returns>
    public static string Resolve(int id)
    {
        // Volatile.Read 用于读取最新发布的数组引用。
        // 这样扩容后其他线程能看到新的数组。
        var table = Volatile.Read(ref s_idToText);

        // 使用 uint 比较可以同时处理 id < 0 和 id >= Length。
        if ((uint)id >= (uint)table.Length)
        {
            return "Unknown";
        }

        return table[id] ?? "Unknown";
    }

    /// <summary>
    /// 确保 id -> text 数组能容纳指定 ID。
    /// </summary>
    /// <param name="id">
    /// 即将写入的符号 ID。
    /// 如果 id 超出当前容量，就按 2 倍扩容。
    /// </param>
    private static void EnsureCapacity(int id)
    {
        if (id < s_idToText.Length)
        {
            return;
        }

        var newLength = s_idToText.Length;

        // 持续翻倍，直到新数组能容纳 id。
        while (newLength <= id)
        {
            newLength *= 2;
        }

        var next = new string?[newLength];

        // 复制旧映射到新数组。
        Array.Copy(s_idToText, next, s_idToText.Length);

        // 发布新数组引用。
        Volatile.Write(ref s_idToText, next);
    }
}
```

### 6.1 为什么不直接用 string.Intern

不建议使用 CLR 自带 `string.Intern()`，原因是：

```text
它驻留的是字符串对象本身，不会变成 int。
它仍然要求热路径携带 string 引用。
它的生命周期由运行时管理，不适合作为框架内部可控诊断表。
```

本方案需要的是：

```text
字符串 -> int
int -> 字符串
```

而不是：

```text
字符串 -> 驻留字符串对象
```

---

## 7. HandlerCircuit 设计

`HandlerCircuit` 保留，但不进入热路径数组。

```csharp
using System.Threading;

namespace LayerBase.Core.Event;

/// <summary>
/// handler 的故障状态。
/// 它记录某个 handler 是否已经被熔断。
/// </summary>
internal sealed class HandlerCircuit
{
    /// <summary>
    /// 熔断标记。
    /// 0 表示启用。
    /// 1 表示已熔断。
    /// </summary>
    private int _disabled;

    /// <summary>
    /// 判断当前 handler 是否已经被熔断。
    /// </summary>
    public bool IsDisabled => Volatile.Read(ref _disabled) == 1;

    /// <summary>
    /// 尝试熔断当前 handler。
    /// </summary>
    /// <returns>
    /// true 表示本次调用成功把 handler 从启用状态切换到熔断状态。
    /// false 表示之前已经被其他线程熔断过。
    /// </returns>
    public bool TryDisable()
    {
        // Interlocked.Exchange 会原子地把 _disabled 设置成 1。
        // 返回旧值为 0，说明本次调用是第一次熔断成功。
        return Interlocked.Exchange(ref _disabled, 1) == 0;
    }

    /// <summary>
    /// 重置熔断状态。
    /// 通常用于框架 Reset 或重新注册场景。
    /// </summary>
    public void Reset()
    {
        Volatile.Write(ref _disabled, 0);
    }
}
```

重点：

```text
HandlerCircuit 仍然用于 Rebuild 过滤。
HandlerCircuit 仍然用于异常后 TryDisable。
HandlerCircuit 不再跟 handler delegate 数组并排。
```

---

## 8. FaultSlot 设计

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 单个 handler 的故障诊断槽。
/// 它只在异常路径中使用。
/// </summary>
internal readonly struct FaultSlot
{
    /// <summary>
    /// 当前 handler 所属 Layer 的运行时下标。
    /// 异常上报时可通过它定位具体 Layer。
    /// </summary>
    public readonly int LayerIndex;

    /// <summary>
    /// 当前 handler 的故障状态对象。
    /// 异常发生后通过它执行 TryDisable。
    /// </summary>
    public readonly HandlerCircuit Circuit;

    /// <summary>
    /// 当前 handler 名称的符号 ID。
    /// 异常上报时通过 EventDiagnosticSymbols.Resolve 还原成字符串。
    /// </summary>
    public readonly int HandlerNameId;

    /// <summary>
    /// 创建一个故障诊断槽。
    /// </summary>
    /// <param name="layerIndex">
    /// 注册该 handler 的 Layer 下标。
    /// </param>
    /// <param name="circuit">
    /// 该 handler 对应的故障状态对象。
    /// </param>
    /// <param name="handlerNameId">
    /// 该 handler 名称对应的符号 ID。
    /// </param>
    public FaultSlot(int layerIndex, HandlerCircuit circuit, int handlerNameId)
    {
        LayerIndex = layerIndex;
        Circuit = circuit;
        HandlerNameId = handlerNameId;
    }
}
```

---

## 9. FaultTable 设计

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 当前 EventBucket 的异常诊断快照。
/// 它和派发数组在同一次 Rebuild 中生成。
/// </summary>
/// <typeparam name="TEvent">
/// 当前事件类型。
/// </typeparam>
internal sealed class FaultTable<TEvent> where TEvent : struct
{
    /// <summary>
    /// SubscribeFlow 同步 handler 的故障槽数组。
    /// 下标与 _syncHandlers 对齐。
    /// </summary>
    public readonly FaultSlot[] SyncFaults;

    /// <summary>
    /// SubscribeFlow 异步 handler 的故障槽数组。
    /// 下标与 _asyncHandlers 对齐。
    /// </summary>
    public readonly FaultSlot[] AsyncFaults;

    /// <summary>
    /// Subscribe 安全通知 handler 的故障槽数组。
    /// 下标与 _subscribeHandlers 对齐。
    /// </summary>
    public readonly FaultSlot[] SubscribeFaults;

    /// <summary>
    /// 当前事件类型名称对应的符号 ID。
    /// 异常日志需要事件名时，才会通过该 ID 还原字符串。
    /// </summary>
    public readonly int EventNameId;

    /// <summary>
    /// 创建异常诊断快照。
    /// </summary>
    /// <param name="syncFaults">
    /// 与同步 Flow handler 数组对齐的故障槽数组。
    /// </param>
    /// <param name="asyncFaults">
    /// 与异步 Flow handler 数组对齐的故障槽数组。
    /// </param>
    /// <param name="subscribeFaults">
    /// 与安全 Subscribe handler 数组对齐的故障槽数组。
    /// </param>
    public FaultTable(
        FaultSlot[] syncFaults,
        FaultSlot[] asyncFaults,
        FaultSlot[] subscribeFaults)
    {
        SyncFaults = syncFaults;
        AsyncFaults = asyncFaults;
        SubscribeFaults = subscribeFaults;

        // 事件名称只作为诊断符号保存，不参与派发。
        EventNameId = EventTypeSymbol<TEvent>.NameId;
    }
}
```

---

## 10. FaultKind 设计

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// handler 所属的故障类别。
/// 异常处理时通过它选择对应的 FaultSlot 数组。
/// </summary>
internal enum FaultKind
{
    /// <summary>
    /// SubscribeFlow 同步 handler。
    /// </summary>
    Sync,

    /// <summary>
    /// SubscribeFlow 异步 handler。
    /// </summary>
    Async,

    /// <summary>
    /// Subscribe 安全通知 handler。
    /// </summary>
    Subscribe
}
```

---

## 11. Handler 名称符号 ID 生成

```csharp
using System.Reflection;

namespace LayerBase.Core.Event;

internal static class HandlerNameSymbol
{
    /// <summary>
    /// 获取委托 handler 的诊断名称符号 ID。
    /// </summary>
    /// <param name="handler">
    /// 事件 handler 委托。
    /// 该委托可以是实例方法、静态方法、lambda 或闭包。
    /// </param>
    /// <returns>
    /// handler 诊断名称对应的符号 ID。
    /// </returns>
    public static int FromDelegate(Delegate handler)
    {
        // Method 表示委托绑定的方法。
        var method = handler.Method;

        // DeclaringType 表示声明该方法的类型。
        // 如果它为空，则尝试使用 Target 的运行时类型。
        var typeName =
            method.DeclaringType?.FullName ??
            handler.Target?.GetType().FullName ??
            "Global";

        // lambda 或闭包方法常见名称类似 "<MethodName>b__0_0"。
        // 这种名称过长且不稳定，所以可以折叠为 "lambda"。
        var methodName = NormalizeMethodName(method);

        // 只在注册或 Rebuild 阶段拼接字符串。
        // 正常事件派发阶段不会再次拼接。
        return EventDiagnosticSymbols.Intern($"{typeName}.{methodName}");
    }

    /// <summary>
    /// 获取接口 handler 对象的诊断名称符号 ID。
    /// </summary>
    /// <param name="handler">
    /// 实现 IEventHandler&lt;T&gt; 或类似接口的 handler 实例。
    /// </param>
    /// <returns>
    /// handler 类型名称对应的符号 ID。
    /// </returns>
    public static int FromInstance(object handler)
    {
        var type = handler.GetType();

        // 对接口式 handler 来说，类型名通常比方法名更有诊断意义。
        return EventDiagnosticSymbols.Intern(type.FullName ?? type.Name);
    }

    /// <summary>
    /// 规范化方法名，避免 lambda 或闭包名污染诊断输出。
    /// </summary>
    /// <param name="method">
    /// 需要规范化名称的方法信息。
    /// </param>
    /// <returns>
    /// 更适合日志展示的方法名。
    /// </returns>
    private static string NormalizeMethodName(MethodInfo method)
    {
        var name = method.Name;

        // 编译器生成方法通常包含尖括号。
        // 这里做保守处理，避免日志里出现过长的编译器内部名。
        if (name.StartsWith("<", StringComparison.Ordinal) &&
            name.Contains('>', StringComparison.Ordinal))
        {
            return "lambda";
        }

        return name;
    }
}
```

---

## 12. 同步派发设计

### 12.1 DispatchSync 示例

```csharp
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LayerBase.Core.Event;

internal partial class EventBucket<TEvent> where TEvent : struct
{
    /// <summary>
    /// 当前事件类型的同步 Flow handler 数组。
    /// 这是热路径数据，正常派发只访问它。
    /// </summary>
    private EventHandleDelegate<TEvent>[] _syncHandlers = Array.Empty<EventHandleDelegate<TEvent>>();

    /// <summary>
    /// 当前事件类型的故障诊断表。
    /// 这是冷路径数据，只有 handler 抛异常时才访问。
    /// </summary>
    private FaultTable<TEvent> _faultTable =
        new(Array.Empty<FaultSlot>(), Array.Empty<FaultSlot>(), Array.Empty<FaultSlot>());

    /// <summary>
    /// 派发指定范围内的同步 Flow handler。
    /// </summary>
    /// <param name="start">
    /// 起始下标，包含该位置。
    /// 通常来自 LayerRange.Start。
    /// </param>
    /// <param name="end">
    /// 结束下标，不包含该位置。
    /// 通常来自 LayerRange.End。
    /// </param>
    /// <param name="value">
    /// 当前正在派发的事件值。
    /// 使用 in 传参是为了避免大结构体复制。
    /// </param>
    /// <returns>
    /// 当前范围内 handler 合并后的处理状态。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventHandledState DispatchSync(int start, int end, in TEvent value)
    {
        if (start >= end)
        {
            return EventHandledState.Continue;
        }

        // 只获取 handler 数组的数据引用。
        // 正常路径不会读取 FaultTable，也不会读取 HandlerCircuit。
        ref var handlerBase = ref MemoryMarshal.GetArrayDataReference(_syncHandlers);

        var combinedState = 0;
        var currentIndex = start;

        try
        {
            for (var i = start; i < end; i++)
            {
                currentIndex = i;

                // 这里是同步 Flow handler 的真正调用点。
                // 该调用是热路径核心，因此不附带 name/circuit 读取。
                var state = Unsafe.Add(ref handlerBase, i)(in value);

                if (state == EventHandledState.Handled)
                {
                    return EventHandledState.Handled;
                }

                combinedState |= (int)state;
            }
        }
        catch (Exception exception)
        {
            // 只有 handler 抛出异常时，才进入冷路径。
            HandleFault(FaultKind.Sync, currentIndex, in value, exception);

            // 发生异常的 handler 会被熔断，当前事件继续交给后续机制处理。
            return EventHandledState.Continue;
        }

        return (combinedState & 2) != 0
            ? EventHandledState.HandledAndContinue
            : EventHandledState.Continue;
    }
}
```

---

## 13. 统一异常处理

```csharp
namespace LayerBase.Core.Event;

internal partial class EventBucket<TEvent> where TEvent : struct
{
    /// <summary>
    /// 处理 handler 抛出的异常。
    /// </summary>
    /// <param name="kind">
    /// handler 所属类别。
    /// 用于选择 SyncFaults、AsyncFaults 或 SubscribeFaults。
    /// </param>
    /// <param name="index">
    /// 抛出异常的 handler 在对应派发数组中的下标。
    /// </param>
    /// <param name="value">
    /// 抛出异常时正在处理的事件值。
    /// 用于保留现有 EventMetaDataHandler.OnEventExpectation 行为。
    /// </param>
    /// <param name="exception">
    /// handler 抛出的异常对象。
    /// </param>
    private void HandleFault(
        FaultKind kind,
        int index,
        in TEvent value,
        Exception exception)
    {
        // 保留现有元数据异常通知逻辑。
        // 这一步属于异常路径，不影响正常派发。
        EventMetaDataHandler.OnEventExpectation(value, exception);

        var faultTable = _faultTable;

        // 根据故障类别选择对应的故障槽。
        var slot = kind switch
        {
            FaultKind.Sync => faultTable.SyncFaults[index],
            FaultKind.Async => faultTable.AsyncFaults[index],
            FaultKind.Subscribe => faultTable.SubscribeFaults[index],
            _ => default
        };

        // 如果已经被其他线程熔断过，就不重复上报。
        if (!slot.Circuit.TryDisable())
        {
            return;
        }

        // 只有异常路径才把符号 ID 还原成字符串。
        var handlerName = EventDiagnosticSymbols.Resolve(slot.HandlerNameId);
        var eventName = EventDiagnosticSymbols.Resolve(faultTable.EventNameId);

        // LayerIndex 可以继续作为运行时 LayerId。
        // 如果需要 LayerName，则可以由 GlobalEventCenter 通过 LayerNameId 还原。
        LayerHub.ReportLayerEventError(
            slot.LayerIndex,
            handlerName,
            eventName,
            exception);

        // 标记派发表需要重建。
        // 下一次 Rebuild 会通过 Circuit.IsDisabled 剔除已熔断 handler。
        MarkDirty();
    }
}
```

---

## 14. 异步派发设计

异步 handler 的特殊点在于：

```text
异常不一定在调用 handler 时立刻出现。
异常可能在 task continuation 中出现。
```

因此不能只用局部变量 `currentIndex` 完成全部处理。

但也不应该把 `HandlerCircuit` 和 handler 名称字符串塞进每个异步上下文。

推荐做法是：

```text
异步上下文只保存：
- EventBucket 引用
- FaultKind
- faultIndex
- payload
- task
```

### 14.1 AsyncFaultContext 示例

```csharp
using System.Collections.Concurrent;
using System.Threading;

namespace LayerBase.Core.Event;

/// <summary>
/// 异步 handler 的异常观察上下文。
/// 它用于在异步任务完成后捕获异常，并转入 EventBucket 的冷路径故障处理。
/// </summary>
/// <typeparam name="TEvent">
/// 当前事件类型。
/// </typeparam>
internal sealed class AsyncFaultContext<TEvent> where TEvent : struct
{
    /// <summary>
    /// 异步上下文对象池。
    /// 复用上下文对象可以减少异步派发产生的临时分配。
    /// </summary>
    private static readonly ConcurrentQueue<AsyncFaultContext<TEvent>> s_pool = new();

    /// <summary>
    /// 对象池当前估计数量。
    /// 用于限制池容量。
    /// </summary>
    private static int s_poolCount;

    /// <summary>
    /// 对象池最大容量。
    /// 超过该数量的上下文会被丢弃，交给 GC 回收。
    /// </summary>
    private const int MaxPoolSize = 1024;

    /// <summary>
    /// 当前上下文所属的 EventBucket。
    /// 异常发生时通过它调用 HandleFault。
    /// </summary>
    private EventBucket<TEvent>? _owner;

    /// <summary>
    /// 当前异步 handler 对应的故障类别。
    /// 通常是 FaultKind.Async。
    /// </summary>
    private FaultKind _kind;

    /// <summary>
    /// 当前异步 handler 在对应派发数组中的下标。
    /// 该下标用于异常时定位 FaultSlot。
    /// </summary>
    private int _faultIndex;

    /// <summary>
    /// 当前事件负载。
    /// 保存它是为了异常时仍能调用 EventMetaDataHandler.OnEventExpectation。
    /// </summary>
    private TEvent _payload;

    /// <summary>
    /// 正在观察的异步任务。
    /// </summary>
    private LBTask _task;

    /// <summary>
    /// 缓存 continuation 委托，避免每次 Observe 都创建新委托。
    /// </summary>
    private readonly Action _continuation;

    /// <summary>
    /// 创建异步异常观察上下文。
    /// </summary>
    private AsyncFaultContext()
    {
        _continuation = Complete;
    }

    /// <summary>
    /// 开始观察一个异步 handler 返回的任务。
    /// </summary>
    /// <param name="owner">
    /// 当前事件类型对应的 EventBucket。
    /// 异常发生时需要回到 owner 处理故障。
    /// </param>
    /// <param name="kind">
    /// 异步 handler 的故障类别。
    /// 通常传 FaultKind.Async。
    /// </param>
    /// <param name="faultIndex">
    /// 异步 handler 在 _asyncHandlers 中的下标。
    /// </param>
    /// <param name="payload">
    /// 当前事件负载。
    /// 使用 in 传入可以避免调用 Observe 时复制大结构体；
    /// 内部保存字段时仍会复制一份，因为异步完成发生在未来。
    /// </param>
    /// <param name="task">
    /// handler 返回的异步任务。
    /// </param>
    public static void Observe(
        EventBucket<TEvent> owner,
        FaultKind kind,
        int faultIndex,
        in TEvent payload,
        LBTask task)
    {
        if (!s_pool.TryDequeue(out var context))
        {
            context = new AsyncFaultContext<TEvent>();
        }
        else
        {
            Interlocked.Decrement(ref s_poolCount);
        }

        context._owner = owner;
        context._kind = kind;
        context._faultIndex = faultIndex;
        context._payload = payload;
        context._task = task;

        // 任务完成时执行 Complete。
        // 如果任务成功，Complete 不会访问 FaultTable。
        // 如果任务失败，Complete 才会进入冷路径。
        task.GetAwaiter().OnCompleted(context._continuation);
    }

    /// <summary>
    /// 异步任务完成后的回调。
    /// </summary>
    private void Complete()
    {
        try
        {
            // GetResult 会在任务失败时重新抛出异常。
            _task.GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            // 只有任务失败时，才通过 owner 进入故障处理。
            _owner?.HandleFault(_kind, _faultIndex, in _payload, exception);
        }
        finally
        {
            _owner = null;
            _kind = default;
            _faultIndex = -1;
            _payload = default;
            _task = default;

            if (Interlocked.Increment(ref s_poolCount) <= MaxPoolSize)
            {
                s_pool.Enqueue(this);
            }
            else
            {
                Interlocked.Decrement(ref s_poolCount);
            }
        }
    }
}
```

---

## 15. Rebuild 设计

Rebuild 仍然可以访问 `HandlerCircuit.IsDisabled`。

这不违背“移出热路径”的目标，因为 Rebuild 不是每次事件派发都会发生。

### 15.1 Rebuild 填充逻辑示例

```csharp
namespace LayerBase.Core.Event;

internal partial class EventBucket<TEvent> where TEvent : struct
{
    /// <summary>
    /// 重建同步 Flow handler 派发表和对应故障表。
    /// </summary>
    /// <param name="entries">
    /// 注册表中当前事件类型的同步 Flow handler 条目。
    /// 每个条目包含 handler delegate、LayerIndex、Circuit、HandlerNameId。
    /// </param>
    private void RebuildSyncHandlers(IReadOnlyList<OrderedHandlerEntry<TEvent>> entries)
    {
        var handlerCount = 0;

        // 第一遍统计未熔断 handler 数量。
        // 熔断 handler 不进入新的派发表。
        for (var i = 0; i < entries.Count; i++)
        {
            if (!entries[i].Circuit.IsDisabled)
            {
                handlerCount++;
            }
        }

        var handlers = new EventHandleDelegate<TEvent>[handlerCount];
        var faults = new FaultSlot[handlerCount];

        var writeIndex = 0;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            // 已熔断 handler 不进入派发表。
            if (entry.Circuit.IsDisabled)
            {
                continue;
            }

            // 热路径数组只保存执行委托。
            handlers[writeIndex] = entry.Handler;

            // 冷路径数组保存异常诊断信息。
            faults[writeIndex] = new FaultSlot(
                layerIndex: entry.LayerIndex,
                circuit: entry.Circuit,
                handlerNameId: entry.HandlerNameId);

            writeIndex++;
        }

        // 发布新的派发数组。
        _syncHandlers = handlers;

        // FaultTable 可以在完整 Rebuild 结束后统一替换。
        // 这里展示的是局部构建思路。
        _faultTable = new FaultTable<TEvent>(
            syncFaults: faults,
            asyncFaults: _faultTable.AsyncFaults,
            subscribeFaults: _faultTable.SubscribeFaults);
    }
}
```

---

## 16. HandlerEntry 改造建议

当前 HandlerEntry 不应该保存 `FullName` 字符串。

建议改成保存 `HandlerNameId`。

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 有序 Flow handler 注册条目。
/// 它属于注册表数据，不属于最终派发热数组。
/// </summary>
/// <typeparam name="TEvent">
/// 当前事件类型。
/// </typeparam>
internal readonly struct OrderedHandlerEntry<TEvent> where TEvent : struct
{
    /// <summary>
    /// handler 所属 Layer 下标。
    /// </summary>
    public readonly int LayerIndex;

    /// <summary>
    /// handler 执行委托。
    /// Rebuild 时会复制到热路径派发表。
    /// </summary>
    public readonly EventHandleDelegate<TEvent> Handler;

    /// <summary>
    /// handler 故障状态。
    /// Rebuild 时用于判断是否需要剔除该 handler。
    /// 异常时通过 FaultSlot 访问它。
    /// </summary>
    public readonly HandlerCircuit Circuit;

    /// <summary>
    /// handler 名称的符号 ID。
    /// 不直接保存字符串，避免热路径长期携带 string 引用。
    /// </summary>
    public readonly int HandlerNameId;

    /// <summary>
    /// 创建有序 Flow handler 注册条目。
    /// </summary>
    /// <param name="layerIndex">
    /// 注册该 handler 的 Layer 下标。
    /// </param>
    /// <param name="handler">
    /// handler 执行委托。
    /// </param>
    /// <param name="circuit">
    /// handler 对应的故障状态对象。
    /// </param>
    /// <param name="handlerNameId">
    /// handler 诊断名称对应的符号 ID。
    /// </param>
    public OrderedHandlerEntry(
        int layerIndex,
        EventHandleDelegate<TEvent> handler,
        HandlerCircuit circuit,
        int handlerNameId)
    {
        LayerIndex = layerIndex;
        Handler = handler;
        Circuit = circuit;
        HandlerNameId = handlerNameId;
    }
}
```

---

## 17. Layer 名称 ID 设计

Layer 的运行时下标本身就可以作为 LayerId。

因此建议：

```text
LayerIndex -> 派发、路由、排序
LayerNameId -> 异常、日志、调试
```

示例：

```csharp
namespace LayerBase.Core.Event;

internal sealed partial class GlobalEventCenter
{
    /// <summary>
    /// 每个 Layer 下标对应的 Layer 名称符号 ID。
    /// 下标是 LayerIndex，值是 LayerNameId。
    /// </summary>
    private int[] _layerNameIds = Array.Empty<int>();

    /// <summary>
    /// 确保 Layer 名称表有足够容量，并登记指定 Layer 名称。
    /// </summary>
    /// <param name="layerIndex">
    /// Layer 的运行时下标。
    /// </param>
    /// <param name="layerName">
    /// Layer 的诊断名称。
    /// </param>
    private void SetLayerName(int layerIndex, string layerName)
    {
        if ((uint)layerIndex >= (uint)_layerNameIds.Length)
        {
            Array.Resize(ref _layerNameIds, layerIndex + 1);
        }

        // 只保存名称 ID，不在热路径保存字符串。
        _layerNameIds[layerIndex] = EventDiagnosticSymbols.Intern(layerName);
    }

    /// <summary>
    /// 根据 Layer 下标还原 Layer 名称。
    /// </summary>
    /// <param name="layerIndex">
    /// Layer 的运行时下标。
    /// </param>
    /// <returns>
    /// Layer 的诊断名称。
    /// </returns>
    private string ResolveLayerName(int layerIndex)
    {
        if ((uint)layerIndex >= (uint)_layerNameIds.Length)
        {
            return "Unknown";
        }

        return EventDiagnosticSymbols.Resolve(_layerNameIds[layerIndex]);
    }
}
```

---

## 18. ParallelSubscriptionQueue 改造建议

当前并行订阅队列如果保存事件名和 handler 名字符串，也应该改成 ID。

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 并行订阅队列。
/// 正常路径只负责排队和执行 handler。
/// 异常路径才还原诊断名称。
/// </summary>
/// <typeparam name="TEvent">
/// 当前事件类型。
/// </typeparam>
internal sealed class ParallelSubscriptionQueue<TEvent> where TEvent : struct
{
    /// <summary>
    /// 当前事件类型名称的符号 ID。
    /// 只在异常路径使用。
    /// </summary>
    private readonly int _eventNameId;

    /// <summary>
    /// 当前 handler 名称的符号 ID。
    /// 只在异常路径使用。
    /// </summary>
    private readonly int _handlerNameId;

    /// <summary>
    /// 异常报告回调。
    /// 参数依次为 LayerIndex、HandlerNameId、EventNameId、Exception。
    /// </summary>
    private readonly Action<int, int, int, Exception> _reportError;

    /// <summary>
    /// 创建并行订阅队列。
    /// </summary>
    /// <param name="handler">
    /// 负责处理事件的 handler 实例。
    /// </param>
    /// <param name="reportError">
    /// 异常报告回调。
    /// 只有 handler 抛异常时才会调用。
    /// </param>
    public ParallelSubscriptionQueue(
        IEventHandler<TEvent> handler,
        Action<int, int, int, Exception> reportError)
    {
        _eventNameId = EventTypeSymbol<TEvent>.NameId;
        _handlerNameId = HandlerNameSymbol.FromInstance(handler);
        _reportError = reportError;
    }

    /// <summary>
    /// 上报并行 handler 异常。
    /// </summary>
    /// <param name="layerIndex">
    /// 抛出异常的 handler 所属 Layer 下标。
    /// </param>
    /// <param name="exception">
    /// handler 抛出的异常。
    /// </param>
    private void ReportException(int layerIndex, Exception exception)
    {
        // 这里仍不还原字符串，只把 ID 交给外层。
        // 外层真正写日志时再 Resolve。
        _reportError(layerIndex, _handlerNameId, _eventNameId, exception);
    }
}
```

兼容旧错误报告接口：

```csharp
namespace LayerBase.Core.Event;

internal static class LayerErrorReporter
{
    /// <summary>
    /// 将基于符号 ID 的异常报告转换为旧版字符串报告。
    /// </summary>
    /// <param name="layerIndex">
    /// 抛出异常的 Layer 下标。
    /// </param>
    /// <param name="handlerNameId">
    /// handler 名称符号 ID。
    /// </param>
    /// <param name="eventNameId">
    /// 事件名称符号 ID。
    /// </param>
    /// <param name="exception">
    /// handler 抛出的异常。
    /// </param>
    public static void ReportBySymbolId(
        int layerIndex,
        int handlerNameId,
        int eventNameId,
        Exception exception)
    {
        var handlerName = EventDiagnosticSymbols.Resolve(handlerNameId);
        var eventName = EventDiagnosticSymbols.Resolve(eventNameId);

        LayerHub.ReportLayerEventError(
            layerIndex,
            handlerName,
            eventName,
            exception);
    }
}
```

---

## 19. 不建议的方案：只把 string[] 换成 int[]

一个容易想到的方案是：

```text
_syncNames -> _syncNameIds
_asyncNames -> _asyncNameIds
_notifySafeNames -> _notifySafeNameIds
```

这比 string[] 好，但不是最优。

原因是：

```text
int[] 虽然比 string[] 小，但仍然是热路径旁边的额外数组。
Rebuild 仍然要维护它。
事件派发实现仍然容易顺手读取它。
```

更干净的方案是：

```text
热路径完全不保存 name/nameId/circuit。
异常路径通过 FaultTable 查。
```

---

## 20. 是否可以连 FaultTable 都不要

理论上可以。

异常发生后，可以根据 handler 下标回到注册表里重新扫描，找到对应 handler 的 Circuit 和 NameId。

但不建议第一版这样做。

原因：

```text
注册表可能在派发后发生变化，异常时再扫描容易错位。
异步 handler 的异常可能延迟发生，更容易遇到版本不一致。
扫描注册表需要复刻 Rebuild 的过滤顺序，容易引入 bug。
```

因此推荐保留 `FaultTable`：

```text
它不是热路径数据。
它是 Rebuild 时生成的诊断快照。
它能保证异常时的 index 与派发表一致。
```

---

## 21. 迁移步骤

### 第一步：引入 EventDiagnosticSymbols

新增：

```text
EventDiagnosticSymbols
EventTypeSymbol<T>
HandlerNameSymbol
```

先不改派发逻辑，只让新注册的 handler 能得到 `HandlerNameId`。

---

### 第二步：HandlerEntry 从 FullName 改成 HandlerNameId

把注册条目里的：

```text
string FullName
```

替换为：

```text
int HandlerNameId
```

这一步已经能减少大量长期字符串引用。

---

### 第三步：引入 FaultSlot / FaultTable

在 Rebuild 中生成：

```text
FaultSlot[] SyncFaults
FaultSlot[] AsyncFaults
FaultSlot[] SubscribeFaults
```

并确保它们与派发 handler 数组下标一致。

---

### 第四步：移除 EventBucket 中的 string[] name 数组

删除：

```text
_syncNames
_asyncNames
_notifyNames
_notifySafeNames
```

异常时不再从 name 数组取字符串，而是：

```text
handler index -> FaultTable -> HandlerNameId -> Resolve
```

---

### 第五步：移除 EventBucket 中的 HandlerCircuit[] 热数组

删除：

```text
_syncCircuits
_asyncCircuits
_notifySafeCircuits
```

异常时从 FaultTable 获取 `HandlerCircuit`。

Rebuild 时仍从注册表读取 `Circuit.IsDisabled`。

---

### 第六步：改造异步异常上下文

异步上下文不再保存：

```text
HandlerCircuit
handlerName string
eventName string
```

只保存：

```text
EventBucket
FaultKind
faultIndex
payload
task
```

---

### 第七步：Layer 名称改成 LayerNameId

把 `_layerNames` 改成：

```text
_layerNameIds
```

异常时再还原 Layer 名称。

---

### 第八步：基准测试

至少测试：

```text
无订阅 Send<T>()
单 handler Send<T>()
多 Layer 多 handler Send<T>()
同步异常熔断
异步异常熔断
Subscribe 安全通知异常隔离
ParallelSubscriptionQueue 异常报告
频繁 Rebuild
```

---

## 22. 性能预期

### 22.1 内存布局收益

改造前，每类 handler 附近可能存在：

```text
delegate[]
circuit[]
string[]
```

改造后，热路径只保留：

```text
delegate[]
```

故障信息放到冷路径：

```text
FaultSlot[]
```

这样能减少热路径数组数量，降低 cache 污染。

---

### 22.2 正常派发收益

正常派发时：

```text
不读取 HandlerCircuit
不读取 handler name
不读取 event name
不还原字符串
不拼接字符串
```

同步派发只需要：

```text
遍历 delegate[]
执行 delegate
合并 EventHandledState
```

---

### 22.3 异常路径成本

异常发生时会多做：

```text
FaultTable 查表
符号 ID 还原字符串
Circuit.TryDisable
MarkDirty
日志上报
```

但异常本来就是冷路径，因此可以接受。

---

## 23. 兼容性说明

### 23.1 对外 API

如果当前对外暴露的是字符串错误报告，例如：

```text
ReportLayerEventError(int layerIndex, string handlerName, string eventName, Exception ex)
```

可以保留。

内部先使用 ID：

```text
ReportLayerEventError(int layerIndex, int handlerNameId, int eventNameId, Exception ex)
```

最终在边界处转换成字符串。

---

### 23.2 日志输出

日志格式可以不变。

旧格式：

```text
Layer=BattleLayer Handler=InventoryManager.OnChanged Event=InventoryChangedEvent
```

新内部流程：

```text
LayerIndex -> LayerNameId -> "BattleLayer"
HandlerNameId -> "InventoryManager.OnChanged"
EventNameId -> "InventoryChangedEvent"
```

用户看到的日志不需要变化。

---

### 23.3 ID 稳定性

运行时递增 ID 只保证：

```text
同一次进程运行期间稳定
```

不保证：

```text
跨进程稳定
跨版本稳定
不同启动顺序稳定
```

如果未来要把 ID 写入持久化日志，并希望跨版本分析，就需要：

```text
源生成器生成固定 ID
或使用稳定哈希 ID
```

---

## 24. 风险与处理

### 24.1 FaultTable 与 DispatchTable 下标错位

风险：

```text
_syncHandlers[i] 与 SyncFaults[i] 不对应
```

处理：

```text
必须在同一次 Rebuild 中同时生成派发数组和故障数组。
不要分别增量修改两边。
```

---

### 24.2 异步异常延迟导致表版本变化

风险：

```text
异步 handler 返回任务后，EventBucket 已经 Rebuild。
异常发生时，faultIndex 指向新表，导致错位。
```

处理：

```text
AsyncFaultContext 需要捕获当次派发使用的 FaultTable 快照。
不要在 Complete 时重新读取 _faultTable。
```

因此异步 Observe 最好传入：

```text
FaultTable<TEvent> capturedFaultTable
```

而不是只保存 owner 和 index。

修正后的异步上下文关键字段应为：

```csharp
/// <summary>
/// 当前异步派发捕获到的故障表快照。
/// 它保证异步异常发生时，faultIndex 仍然对应当初的 handler。
/// </summary>
private FaultTable<TEvent>? _capturedFaultTable;
```

对应调用：

```csharp
/// <summary>
/// 观察异步任务，并捕获当前故障表快照。
/// </summary>
/// <param name="owner">
/// 当前 EventBucket。
/// </param>
/// <param name="capturedFaultTable">
/// 当前派发使用的 FaultTable 快照。
/// 异步异常可能延迟发生，因此不能在完成时重新读取 owner._faultTable。
/// </param>
/// <param name="kind">
/// 故障类别。
/// </param>
/// <param name="faultIndex">
/// 当前 handler 在异步派发数组中的下标。
/// </param>
/// <param name="payload">
/// 当前事件负载。
/// </param>
/// <param name="task">
/// handler 返回的异步任务。
/// </param>
public static void Observe(
    EventBucket<TEvent> owner,
    FaultTable<TEvent> capturedFaultTable,
    FaultKind kind,
    int faultIndex,
    in TEvent payload,
    LBTask task)
{
    // 省略对象池获取逻辑。
}
```

`HandleFault` 也建议增加一个重载：

```csharp
/// <summary>
/// 使用指定 FaultTable 快照处理异常。
/// </summary>
/// <param name="faultTable">
/// 异常发生时要使用的故障表快照。
/// 对异步异常来说，它必须是派发当时捕获的快照。
/// </param>
/// <param name="kind">
/// 故障类别。
/// </param>
/// <param name="index">
/// handler 在对应派发数组中的下标。
/// </param>
/// <param name="value">
/// 事件负载。
/// </param>
/// <param name="exception">
/// handler 抛出的异常。
/// </param>
private void HandleFault(
    FaultTable<TEvent> faultTable,
    FaultKind kind,
    int index,
    in TEvent value,
    Exception exception)
{
    // 使用传入的 faultTable，而不是重新读取 _faultTable。
}
```

---

### 24.3 符号表无限增长

风险：

```text
如果动态生成大量 handler 名称，符号表会持续增长。
```

处理：

```text
handler 名称应尽量来自类型名和方法名。
避免把对象实例 ID、时间戳、参数值拼进符号。
```

---

### 24.4 lambda 名称不可读

风险：

```text
lambda 或闭包方法名可能很长，也可能不稳定。
```

处理：

```text
将编译器生成名称折叠为 lambda。
必要时允许用户显式传入诊断名称。
```

未来可以支持：

```csharp
Subscribe<TEvent>(
    handler,
    diagnosticName: "InventorySystem.OnChanged");
```

---

## 25. 最终建议形态

```text
EventBucket<T>
├── _syncHandlers
├── _asyncHandlers
├── _notifyHandlers
├── _subscribeHandlers
├── _parallelHandlers
├── _syncRanges
├── _asyncRanges
├── _faultTable
└── 不再保存 name 数组和 circuit 数组
```

```text
HandlerEntry<T>
├── LayerIndex
├── Handler delegate
├── HandlerCircuit
└── HandlerNameId
```

```text
FaultTable<T>
├── EventNameId
├── SyncFaults
├── AsyncFaults
└── SubscribeFaults
```

```text
FaultSlot
├── LayerIndex
├── HandlerCircuit
└── HandlerNameId
```

```text
EventDiagnosticSymbols
├── Intern(string) -> int
└── Resolve(int) -> string
```

---

## 26. 一句话总结

这次改造不应该停留在：

```text
string[] -> int[]
```

更合理的目标是：

```text
热路径只保留 handler delegate。
HandlerCircuit、handler name、event name、layer name 全部降级为冷路径诊断数据。
异常发生时通过 FaultTable + SymbolId 还原上下文。
```

这样 EventCenter 的数据布局会更符合高性能事件系统的目标：

```text
正常派发轻。
异常诊断完整。
Rebuild 负责生成一致快照。
热路径不为冷路径买单。
```
