# LayerBase 事件系统 Prewarm 设计文档

## 1. 背景

LayerBase 的事件系统已经把运行期热路径尽量压缩到：

- 静态泛型缓存
- 数组派发
- 编译期源生成注册
- 避免运行时反射
- 避免热路径 Dictionary 查询
- 避免热路径 lock

其中事件类型 ID 已经可以通过：

```csharp
EventTypeId<TEvent>.Id
```

这种静态泛型方式获取。

这意味着事件系统在稳定运行后已经足够快。

但是仍然存在一个问题：

> 某个事件类型第一次被使用时，可能会发生一次性初始化成本。

这些成本通常不影响长期吞吐，但可能影响首帧、第一回合、第一次战斗结算、第一次 UI 事件、第一次 Post 队列创建等场景。

因此需要设计一个可选的 Prewarm 机制。

---

## 2. Prewarm 是什么

Prewarm，中文可以叫“预热”。

在本项目中，它的含义是：

> 在正式进入业务循环之前，主动触发事件系统的首次初始化逻辑，让第一次真实事件派发不再承担这些成本。

例如：

```text
不预热：

第一次 Send<DamageEvent>()
    初始化 EventTypeId<DamageEvent>
    创建 EventBucket<DamageEvent>
    重建派发表
    派发事件

第二次 Send<DamageEvent>()
    直接命中缓存
    派发事件
```

预热后：

```text
Build 后，第一帧前：

Prewarm()
    初始化 EventTypeId<DamageEvent>
    创建 EventBucket<DamageEvent>
    重建派发表

第一次 Send<DamageEvent>()
    直接命中缓存
    派发事件
```

---

## 3. 术语解释

### 3.1 热路径

热路径指高频执行的代码路径。

在 LayerBase 事件系统中，典型热路径包括：

```csharp
Send<TEvent>()
Post<TEvent>()
Dispatch<TEvent>()
```

这些方法可能在每帧、每个战斗步骤、每个 UI 输入中被频繁调用。

热路径中应该尽量避免：

- lock
- 反射
- Dictionary 查询
- 临时分配
- 首次初始化
- 大量分支判断

---

### 3.2 冷启动成本

冷启动成本指某段逻辑第一次执行时才会出现的额外成本。

例如：

```csharp
EventTypeId<DamageEvent>.Id
```

第一次访问时，可能会触发静态字段初始化。

后续再访问时，只是读取已经存在的静态字段。

---

### 3.3 静态泛型缓存

静态泛型缓存指：

```csharp
static class Cache<T>
{
    public static SomeValue Value;
}
```

对于不同的 `T`，运行时会生成不同的静态字段。

例如：

```csharp
Cache<DamageEvent>.Value
Cache<CardPlayedEvent>.Value
```

它们是两份互不干扰的静态字段。

LayerBase 可以利用这个特性，让每个事件类型拥有自己的缓存入口，从而避免用 `Type` 作为 key 去查 Dictionary。

---

### 3.4 EventBucket

EventBucket 是某个事件类型对应的订阅者集合与派发缓存。

可以理解成：

```text
DamageEvent 对应一个 DamageEvent 的 Bucket
CardPlayedEvent 对应一个 CardPlayedEvent 的 Bucket
```

Bucket 内部保存这个事件类型的订阅者、派发数组、Layer Mask 等数据。

---

### 3.5 DispatchTable

DispatchTable 可以理解成“派发表”。

它不是一定要真的叫这个类名，而是一个概念：

> 把订阅者整理成适合快速遍历和派发的数据结构。

例如原始状态可能是：

```text
Layer 0:
    Handler A
    Handler B

Layer 1:
    Handler C
```

重建后可能变成：

```text
handlers[] = [A, B, C]
layers[]   = [0, 0, 1]
mask       = 0b0011
```

这样派发时可以直接遍历数组，而不需要再分析订阅关系。

---

### 3.6 dirty

dirty 是“脏标记”。

它表示当前缓存已经过期，需要重建。

例如新增订阅者后：

```text
旧派发表已经不能代表当前订阅关系
所以 Bucket 被标记为 dirty
下一次派发前需要 Rebuild
```

Prewarm 的一个核心作用就是：

> 在第一次真实事件派发前，把 dirty 的 Bucket 提前 Rebuild 干净。

---

### 3.7 Rebuild

Rebuild 指重建派发表。

它通常会做这些事：

- 统计当前事件类型的订阅者
- 整理同步 Handler
- 整理异步 Handler
- 整理通知 Handler
- 生成 Layer Mask
- 生成连续数组
- 替换旧缓存

Rebuild 不应该频繁发生在热路径里。

---

## 4. 目标

Prewarm 设计目标如下：

1. 减少第一次事件派发的冷启动抖动。
2. 不破坏现有热路径性能。
3. 不引入运行时反射扫描。
4. 不强制用户使用。
5. 不默认预热高内存成本对象。
6. 与源生成器配合。
7. 允许用户按需选择预热范围。

---

## 5. 非目标

Prewarm 不解决以下问题：

1. 不用于提高稳定运行后的极限吞吐。
2. 不用于替代正常的事件注册流程。
3. 不用于测试事件派发是否正确。
4. 不通过发送假事件实现预热。
5. 不在运行时扫描程序集查找事件类型。
6. 不默认创建所有 Post 队列。

---

## 6. 收益分析

### 6.1 直接收益

Prewarm 可以提前处理：

| 成本 | 不预热时发生位置 | 预热后发生位置 |
|---|---|---|
| `EventTypeId<TEvent>.Id` 首次初始化 | 第一次 Send/Post | Build 后 |
| `EventBucket<TEvent>` 创建 | 第一次 Send/Post | Build 后 |
| `BucketCache<TEvent>.Instance` 写入 | 第一次 Send/Post | Build 后 |
| 派发表 Rebuild | 第一次派发 | Build 后 |
| Post 队列创建 | 第一次 Post | 可选 Build 后 |

---

### 6.2 对性能的真实影响

Prewarm 的收益不是：

```text
让每一次 Send<TEvent>() 都更快
```

而是：

```text
让第一次 Send<TEvent>() 不突然变慢
```

所以它更适合这些场景：

- 游戏首帧
- 第一回合战斗
- 第一次技能结算
- 第一次 UI 打开
- 第一次大量事件广播
- Benchmark 测试前准备
- 对帧时间稳定性敏感的项目

---

### 6.3 成本

Prewarm 也有成本：

| 成本 | 说明 |
|---|---|
| 启动时间增加 | 本来分散到第一次事件调用的成本，会集中到 Build 后 |
| 代码复杂度增加 | 需要新增 API、生成器逻辑和测试 |
| 内存可能增加 | 如果预热 Post 队列，可能提前创建大量队列 |
| API 心智负担增加 | 用户需要理解 Prewarm 是可选优化 |

---

## 7. 总体结论

Prewarm 对 LayerBase 是有价值的，但它不是核心性能能力。

更准确地说：

> Prewarm 是实时系统稳定性能力，而不是热路径吞吐能力。

推荐实现，但不强制启用。

默认预热范围应该保守，只包括：

```text
EventTypeId
Bucket
DispatchTable
```

不默认包括：

```text
PostQueue
```

---

## 8. 推荐 API 设计

### 8.1 预热目标枚举

```csharp
namespace LayerBase.Core.Event;

using System;

/// <summary>
/// 事件系统预热目标。
/// 
/// Flags：
/// 标记枚举。
/// 标记枚举允许多个选项组合使用。
/// 例如 EventTypeId | Bucket 表示同时预热事件类型 ID 和事件 Bucket。
/// </summary>
[Flags]
public enum LayerPrewarmTargets
{
    /// <summary>
    /// 不执行任何预热。
    /// </summary>
    None = 0,

    /// <summary>
    /// 预热事件类型 ID。
    /// 
    /// 作用：
    /// 提前访问 EventTypeId&lt;TEvent&gt;.Id。
    /// 这样第一次 Send/Post 时，不再承担静态泛型 ID 初始化成本。
    /// </summary>
    EventTypeId = 1 << 0,

    /// <summary>
    /// 预热事件 Bucket。
    /// 
    /// 作用：
    /// 提前创建 EventBucket&lt;TEvent&gt;。
    /// 同时让 BucketCache&lt;TEvent&gt;.Instance 提前命中。
    /// </summary>
    Bucket = 1 << 1,

    /// <summary>
    /// 预热派发表。
    /// 
    /// 作用：
    /// 如果 Bucket 当前是 dirty 状态，则提前执行 Rebuild。
    /// Rebuild 会把订阅者整理成适合快速派发的数组结构。
    /// </summary>
    DispatchTable = 1 << 2,

    /// <summary>
    /// 预热 Post 队列。
    /// 
    /// 作用：
    /// 提前为指定事件类型创建 Post 队列。
    /// 
    /// 注意：
    /// 这个选项可能增加启动期内存占用。
    /// 不建议放入默认预热。
    /// </summary>
    PostQueue = 1 << 3,

    /// <summary>
    /// 推荐默认预热目标。
    /// 
    /// 包含：
    /// - EventTypeId
    /// - Bucket
    /// - DispatchTable
    /// 
    /// 不包含：
    /// - PostQueue
    /// </summary>
    Default = EventTypeId | Bucket | DispatchTable,

    /// <summary>
    /// 完整预热目标。
    /// 
    /// 包含：
    /// - EventTypeId
    /// - Bucket
    /// - DispatchTable
    /// - PostQueue
    /// </summary>
    All = EventTypeId | Bucket | DispatchTable | PostQueue
}
```

---

### 8.2 预热参数

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 事件系统预热参数。
/// </summary>
public readonly struct LayerPrewarmOptions
{
    /// <summary>
    /// 要执行的预热目标。
    /// 
    /// 作用：
    /// 控制本次预热要处理哪些对象。
    /// 例如只预热 Bucket，或者连 PostQueue 一起预热。
    /// </summary>
    public readonly LayerPrewarmTargets Targets;

    /// <summary>
    /// PostQueue 预热的 Layer 数量上限。
    /// 
    /// 作用：
    /// 控制 PostQueue 预热范围，避免一次性为所有 Layer 创建大量队列。
    /// 
    /// 约定：
    /// - 小于等于 0：预热当前已经存在的全部 Layer slots。
    /// - 大于 0：最多预热前 LayerCount 个 Layer slots。
    /// 
    /// 注意：
    /// 这个参数只影响 PostQueue。
    /// 不影响 EventTypeId、Bucket、DispatchTable。
    /// </summary>
    public readonly int LayerCount;

    /// <summary>
    /// 创建预热参数。
    /// </summary>
    /// <param name="targets">
    /// 本次要执行的预热目标。
    /// 推荐默认值是 LayerPrewarmTargets.Default。
    /// </param>
    /// <param name="layerCount">
    /// PostQueue 预热的 Layer 数量上限。
    /// 传 0 表示不限制，由运行时使用当前 Layer slots 数量。
    /// </param>
    public LayerPrewarmOptions(
        LayerPrewarmTargets targets = LayerPrewarmTargets.Default,
        int layerCount = 0)
    {
        Targets = targets;
        LayerCount = layerCount;
    }

    /// <summary>
    /// 默认预热参数。
    /// 
    /// 作用：
    /// 预热事件类型 ID、Bucket 和派发表。
    /// 不预热 PostQueue。
    /// </summary>
    public static LayerPrewarmOptions Default => new();
}
```

---

## 9. Runtime 实现设计

### 9.1 GlobalEventCenter 增加 PrewarmEvent

```csharp
namespace LayerBase.Core.Event;

using System;
using System.Runtime.CompilerServices;

public sealed partial class GlobalEventCenter
{
    /// <summary>
    /// 预热单个事件类型。
    /// 
    /// 调用时机：
    /// 应该在 LayerHub.Build() 之后、第一帧业务逻辑之前调用。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 要预热的事件类型。
    /// 当前事件系统的事件一般是 struct，因此保留 where TEvent : struct 约束。
    /// </typeparam>
    /// <param name="options">
    /// 预热参数。
    /// 用于决定是否预热 EventTypeId、Bucket、DispatchTable、PostQueue。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrewarmEvent<TEvent>(in LayerPrewarmOptions options)
        where TEvent : struct
    {
        // 如果用户要求预热 EventTypeId，则主动读取静态泛型 ID。
        // 这样第一次真实 Send/Post 时，不再触发 EventTypeId<TEvent> 的首次初始化。
        if ((options.Targets & LayerPrewarmTargets.EventTypeId) != 0)
        {
            _ = EventTypeId<TEvent>.Id;
        }

        EventBucket<TEvent>? bucket = null;

        // Bucket 和 DispatchTable 都依赖 EventBucket<TEvent>。
        // 因此只要其中任意一个目标开启，就需要提前拿到 Bucket。
        if ((options.Targets & LayerPrewarmTargets.Bucket) != 0 ||
            (options.Targets & LayerPrewarmTargets.DispatchTable) != 0)
        {
            // GetBucket<TEvent>() 的作用：
            // 1. 创建当前事件类型对应的 EventBucket<TEvent>。
            // 2. 把结果写入 BucketCache<TEvent>.Instance。
            // 3. 后续 Send/Post 可以走静态泛型缓存快路径。
            bucket = GetBucket<TEvent>();
        }

        // 如果用户要求预热派发表，则主动让 Bucket 进入干净状态。
        // 干净状态表示当前派发表已经和订阅关系一致，不需要在第一次派发前 Rebuild。
        if ((options.Targets & LayerPrewarmTargets.DispatchTable) != 0)
        {
            // bucket 理论上已经在上面的分支创建。
            // 这里仍然使用 ??=，是为了防止后续维护时修改前置逻辑导致空引用。
            bucket ??= GetBucket<TEvent>();

            // PrewarmDispatchTable 内部会调用 EnsureClean。
            // 如果 Bucket 不是 dirty 状态，这一步几乎没有成本。
            bucket.PrewarmDispatchTable();
        }

        // PostQueue 是可选预热目标。
        // 它可能增加内存占用，所以不放入默认预热。
        if ((options.Targets & LayerPrewarmTargets.PostQueue) != 0)
        {
            // options.LayerCount 用于限制最多预热多少个 Layer 的队列。
            PrewarmPostQueues<TEvent>(options.LayerCount);
        }
    }

    /// <summary>
    /// 预热某个事件类型在各个 Layer 上的 Post 队列。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 要预热 Post 队列的事件类型。
    /// </typeparam>
    /// <param name="requestedLayerCount">
    /// 要预热的 Layer 数量上限。
    /// 小于等于 0 表示使用当前全部 Layer slots。
    /// </param>
    private void PrewarmPostQueues<TEvent>(int requestedLayerCount)
        where TEvent : struct
    {
        // 读取当前 Layer slots 快照。
        // 这样循环过程中不需要反复读取字段。
        var slots = _layerSlots;

        // 计算本次实际要处理的 Layer 数量。
        // 如果 requestedLayerCount <= 0，则处理全部 slots。
        // 如果 requestedLayerCount > 0，则最多处理 requestedLayerCount 个。
        var count = requestedLayerCount <= 0
            ? slots.Length
            : Math.Min(requestedLayerCount, slots.Length);

        for (var i = 0; i < count; i++)
        {
            // 只有 LayerEventQueue 才支持具体队列预热。
            // 如果 slot 是 null 或其他 IEventQueue 实现，则直接跳过。
            if (slots[i] is LayerEventQueue queue)
            {
                queue.PrewarmQueue<TEvent>();
            }
        }
    }
}
```

---

### 9.2 EventBucket 增加 PrewarmDispatchTable

```csharp
private sealed class EventBucket<TEvent> : IResetable
    where TEvent : struct
{
    /// <summary>
    /// 预热当前事件类型的派发表。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrewarmDispatchTable()
    {
        // EnsureClean 的作用：
        // 如果当前 Bucket 被标记为 dirty，则执行 Rebuild。
        // 如果当前 Bucket 已经是 clean，则直接返回。
        EnsureClean();
    }
}
```

这里不要直接把 `Rebuild()` 暴露给外部。

原因是：

```text
Rebuild 是实现细节
PrewarmDispatchTable 是语义接口
```

未来如果派发表重建策略变化，外部 API 不需要变。

---

### 9.3 LayerEventQueue 增加 PrewarmQueue

```csharp
private sealed class LayerEventQueue : IEventQueue
{
    /// <summary>
    /// 预热当前 Layer 上某个事件类型的 Post 队列。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 要预热队列的事件类型。
    /// </typeparam>
    public void PrewarmQueue<TEvent>()
        where TEvent : struct
    {
        // 如果队列所属 LayerEventQueue 已经释放，则不再创建任何对象。
        if (_disposed)
        {
            return;
        }

        // 获取事件类型 ID。
        // 该 ID 用作 _queuesByTypeArr 的数组下标。
        var typeId = EventTypeId<TEvent>.Id;

        // 读取当前队列数组快照。
        // 这是无锁快路径，用来避免每次预热都进入 lock。
        var arr = _queuesByTypeArr;

        // 如果数组长度足够，并且对应类型的队列已经存在，则无需重复创建。
        if (typeId < arr.Length && arr[typeId] != null)
        {
            return;
        }

        lock (_lock)
        {
            // 进入锁之后再次检查释放状态。
            // 这是为了避免另一个线程刚刚 Dispose 当前队列。
            if (_disposed)
            {
                return;
            }

            // 如果事件类型 ID 超过数组长度，需要扩容。
            // Math.Max 的作用：
            // 1. 至少扩容到 typeId + 1，保证当前 ID 可以作为下标。
            // 2. 同时保留数组翻倍策略，避免频繁扩容。
            if (typeId >= _queuesByTypeArr.Length)
            {
                var newSize = Math.Max(typeId + 1, _queuesByTypeArr.Length * 2);
                var newArr = new IUnmanagedList?[newSize];

                // 把旧数组内容复制到新数组。
                Array.Copy(_queuesByTypeArr, newArr, _queuesByTypeArr.Length);

                // 替换数组引用。
                _queuesByTypeArr = newArr;
            }

            // 如果队列仍然不存在，则创建当前事件类型对应的 UnmanagedList。
            // 注意：
            // 这里不能 Enqueue default(TEvent)。
            // 因为那会制造一条假的业务事件。
            if (_queuesByTypeArr[typeId] == null)
            {
                _queuesByTypeArr[typeId] = new UnmanagedList<TEvent>(
                    _center,
                    _layerIndex,
                    _onDirtyCallback);
            }
        }
    }
}
```

---

## 10. 源生成器设计

### 10.1 为什么需要源生成器参与

不建议 Runtime 自己扫描所有事件类型。

原因：

1. 运行时扫描会引入反射。
2. 运行时扫描会增加启动成本。
3. 运行时扫描不符合 LayerBase 当前的编译期设计方向。
4. 裁剪、AOT、Unity IL2CPP 等环境下，反射扫描容易出问题。

因此应该由源生成器生成预热清单。

---

### 10.2 生成文件示例

```csharp
// <auto-generated />

namespace LayerBase.Generated;

using System;
using LayerBase.Core.Event;

/// <summary>
/// LayerBase 源生成器生成的事件预热入口。
/// </summary>
public static class LayerBasePrewarmRegistry
{
    /// <summary>
    /// 对静态可知的事件类型执行预热。
    /// </summary>
    /// <param name="center">
    /// 全局事件中心。
    /// 用于执行具体事件类型的预热逻辑。
    /// </param>
    /// <param name="options">
    /// 预热参数。
    /// 用于控制预热 EventTypeId、Bucket、DispatchTable 或 PostQueue。
    /// </param>
    public static void Prewarm(GlobalEventCenter center, in LayerPrewarmOptions options)
    {
        // center 不能为空。
        // 如果为空，说明用户在 LayerHub 或事件中心尚未创建时调用了预热入口。
        if (center == null)
        {
            throw new ArgumentNullException(nameof(center));
        }

        // 以下内容由源生成器生成。
        // 每一行对应一个需要预热的事件类型。
        center.PrewarmEvent<DamageEvent>(in options);
        center.PrewarmEvent<CardPlayedEvent>(in options);
        center.PrewarmEvent<TurnStartedEvent>(in options);
    }
}
```

---

### 10.3 事件类型收集规则

源生成器应该收集以下来源：

| 来源 | 是否加入预热清单 | 说明 |
|---|---:|---|
| `[Subscribe]` | 是 | 常规订阅事件 |
| `[SubscribeNotify]` | 是 | 通知型事件 |
| `[SubscribeFlow]` | 是 | 流控型事件 |
| `[SubscribeAsync]` | 是 | 异步订阅事件 |
| `[SubscribeParallel]` | 是 | 并行订阅事件 |
| `EventMetaData<TEvent>` | 是 | 事件元数据相关类型 |
| `[PrewarmEvent]` | 是 | 手动强制预热 |
| 只有 `Send<TEvent>` 调用 | 否 | 不建议通过源码分析所有调用点 |
| 只有 `Post<TEvent>` 调用 | 默认否 | 除非配合 `[PrewarmEvent]` |

---

## 11. 手动预热标记

### 11.1 PrewarmEventAttribute

```csharp
namespace LayerBase.Core.Event;

using System;

/// <summary>
/// 标记某个事件类型需要加入源生成器预热清单。
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class PrewarmEventAttribute : Attribute
{
}
```

---

### 11.2 使用示例

```csharp
using LayerBase.Core.Event;

/// <summary>
/// 输入缓冲事件。
/// 
/// 这个事件可能没有静态订阅者，
/// 但运行时会被高频 Post，
/// 因此通过 PrewarmEvent 主动加入预热清单。
/// </summary>
[PrewarmEvent]
public readonly struct InputBufferedEvent
{
    /// <summary>
    /// 输入来源 ID。
    /// 用于区分键盘、手柄、AI 输入器等来源。
    /// </summary>
    public readonly int SourceId;

    /// <summary>
    /// 输入值。
    /// 可以表示按键强度、摇杆轴值或其他业务输入值。
    /// </summary>
    public readonly float Value;

    /// <summary>
    /// 创建输入缓冲事件。
    /// </summary>
    /// <param name="sourceId">
    /// 输入来源 ID。
    /// 用于标记这条事件来自哪个输入源。
    /// </param>
    /// <param name="value">
    /// 输入值。
    /// 用于承载本次输入的具体数值。
    /// </param>
    public InputBufferedEvent(int sourceId, float value)
    {
        SourceId = sourceId;
        Value = value;
    }
}
```

---

## 12. LayerHub 扩展方法设计

用户最好不要直接接触 `LayerBasePrewarmRegistry`。

推荐提供扩展方法：

```csharp
namespace LayerBase.Core;

using System;
using LayerBase.Core.Event;
using LayerBase.Generated;

/// <summary>
/// LayerHub 预热扩展方法。
/// </summary>
public static class LayerHubPrewarmExtensions
{
    /// <summary>
    /// 使用默认参数预热 LayerHub。
    /// </summary>
    /// <param name="hub">
    /// 已经 Build 完成的 LayerHub。
    /// </param>
    /// <returns>
    /// 返回原始 LayerHub，方便链式调用。
    /// </returns>
    public static LayerHub Prewarm(this LayerHub hub)
    {
        // 使用默认预热参数。
        // 默认只预热 EventTypeId、Bucket、DispatchTable。
        return hub.Prewarm(LayerPrewarmOptions.Default);
    }

    /// <summary>
    /// 使用指定参数预热 LayerHub。
    /// </summary>
    /// <param name="hub">
    /// 已经 Build 完成的 LayerHub。
    /// 预热必须在 Build 后执行，因为订阅关系要在 Build 后才完整。
    /// </param>
    /// <param name="options">
    /// 预热参数。
    /// 用于控制预热范围。
    /// </param>
    /// <returns>
    /// 返回原始 LayerHub，方便链式调用。
    /// </returns>
    public static LayerHub Prewarm(this LayerHub hub, in LayerPrewarmOptions options)
    {
        // hub 不能为空。
        // 如果为空，说明用户调用顺序错误或对象尚未创建。
        if (hub == null)
        {
            throw new ArgumentNullException(nameof(hub));
        }

        // EventCenter 属性名按项目实际公开 API 调整。
        // 这里的核心作用是把事件中心交给源生成器生成的预热入口。
        LayerBasePrewarmRegistry.Prewarm(hub.EventCenter, in options);

        return hub;
    }
}
```

---

## 13. 用户侧调用方式

### 13.1 默认预热

```csharp
var hub = LayerHub
    .CreateLayers()
    .Build()
    .Prewarm();
```

等价于：

```csharp
var hub = LayerHub
    .CreateLayers()
    .Build()
    .Prewarm(new LayerPrewarmOptions(
        targets: LayerPrewarmTargets.Default,
        layerCount: 0));
```

---

### 13.2 完整预热

```csharp
var hub = LayerHub
    .CreateLayers()
    .Build()
    .Prewarm(new LayerPrewarmOptions(
        targets: LayerPrewarmTargets.All,
        layerCount: 0));
```

说明：

```text
LayerPrewarmTargets.All
    预热 EventTypeId、Bucket、DispatchTable、PostQueue。

layerCount: 0
    PostQueue 使用当前全部 Layer slots。
```

---

### 13.3 只预热前几个 Layer 的 Post 队列

```csharp
var hub = LayerHub
    .CreateLayers()
    .Build()
    .Prewarm(new LayerPrewarmOptions(
        targets: LayerPrewarmTargets.All,
        layerCount: 4));
```

说明：

```text
layerCount: 4
    PostQueue 最多只预热前 4 个 Layer slots。
```

适合：

```text
只让核心 Layer 在首帧前完成队列创建
其他低频 Layer 延迟到第一次使用时创建
```

---

## 14. 禁止的实现方式

### 14.1 不允许通过发送 default 事件预热

错误示例：

```csharp
center.Send(default(DamageEvent));
```

原因：

1. 会真的调用业务 Handler。
2. `default(DamageEvent)` 可能是非法业务状态。
3. `[SubscribeFlow]` 可能改变业务流程。
4. `[SubscribeNotify]` 可能触发 UI、音效、日志等副作用。
5. 异常熔断统计可能被假事件污染。
6. 异步事件可能产生不可控任务。

Prewarm 必须只做缓存准备，不进入业务逻辑。

---

### 14.2 不允许 Runtime 扫描程序集

错误方向：

```text
启动时扫描 AppDomain
查找所有 struct event
逐个反射调用 PrewarmEvent<T>
```

原因：

1. 破坏零反射设计。
2. 增加启动成本。
3. 对 AOT、裁剪、IL2CPP 不友好。
4. 无法准确判断哪些事件真的属于 LayerBase 事件系统。
5. 和源生成器已有职责重复。

---

## 15. 默认策略

推荐默认策略：

```text
默认开启：
    EventTypeId
    Bucket
    DispatchTable

默认关闭：
    PostQueue
```

理由：

### 15.1 EventTypeId 值得默认预热

成本小，收益稳定。

尤其是当前使用：

```csharp
EventTypeId<TEvent>.Id
```

这种静态泛型 ID 时，预热可以提前完成静态字段初始化。

---

### 15.2 Bucket 值得默认预热

Bucket 是事件派发必经结构。

提前创建后，第一次事件派发可以直接命中：

```csharp
BucketCache<TEvent>.Instance
```

---

### 15.3 DispatchTable 值得默认预热

派发表 Rebuild 可能涉及数组租赁、统计、复制和 mask 生成。

这类逻辑不适合出现在第一次真实派发中。

---

### 15.4 PostQueue 不适合默认预热

PostQueue 可能按以下规模增长：

```text
Layer 数量 × 事件类型数量
```

如果默认全部创建，可能导致启动期内存增加明显。

因此只作为显式选项。

---

## 16. 测试计划

### 16.1 单元测试

需要覆盖：

1. `PrewarmEvent<TEvent>()` 后，第一次 `Send<TEvent>()` 不再创建 Bucket。
2. `PrewarmEvent<TEvent>()` 后，Bucket 已经 clean。
3. `PrewarmEvent<TEvent>()` 不会调用任何业务 Handler。
4. `PrewarmEvent<TEvent>()` 不会投递任何事件。
5. `PrewarmEvent<TEvent>()` 可以重复调用。
6. `PrewarmEvent<TEvent>()` 在没有订阅者时也不会异常。
7. `PostQueue` 关闭时，不会创建队列。
8. `PostQueue` 开启时，会创建指定事件类型的队列。
9. `layerCount` 能限制队列预热范围。
10. Dispose 后调用 `PrewarmQueue<TEvent>()` 不会创建新对象。

---

### 16.2 Benchmark 测试

需要对比：

```text
首次 Send<TEvent>，不预热
首次 Send<TEvent>，预热后

第 N 次 Send<TEvent>，不预热
第 N 次 Send<TEvent>，预热后
```

预期结果：

```text
首次 Send<TEvent>：
    预热后明显更稳定。

第 N 次 Send<TEvent>：
    两者应接近。
```

如果第 N 次 Send 差异很大，说明 Prewarm 可能意外改变了热路径，需要检查。

---

### 16.3 示例 Benchmark 结构

```csharp
using BenchmarkDotNet.Attributes;
using LayerBase.Core.Event;

/// <summary>
/// 事件预热 Benchmark。
/// 
/// Benchmark：
/// 基准测试，用于比较不同实现的执行时间和内存分配。
/// </summary>
public class EventPrewarmBenchmarks
{
    /// <summary>
    /// 未预热的事件中心。
    /// 用于测试第一次 Send 时的冷启动成本。
    /// </summary>
    private GlobalEventCenter _coldCenter = null!;

    /// <summary>
    /// 已预热的事件中心。
    /// 用于测试预热后的第一次 Send 成本。
    /// </summary>
    private GlobalEventCenter _prewarmedCenter = null!;

    /// <summary>
    /// Benchmark 每轮开始前执行的初始化逻辑。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // 创建未预热事件中心。
        // 这里的创建方式按项目实际 API 调整。
        _coldCenter = CreateCenter();

        // 创建已预热事件中心。
        // 它和 _coldCenter 应该拥有相同订阅拓扑。
        _prewarmedCenter = CreateCenter();

        // 对指定事件类型执行默认预热。
        // 这样 Benchmark 可以比较第一次 Send 的差异。
        _prewarmedCenter.PrewarmEvent<TestEvent>(
            new LayerPrewarmOptions(
                targets: LayerPrewarmTargets.Default,
                layerCount: 0));
    }

    /// <summary>
    /// 测试未预热情况下第一次发送事件的成本。
    /// </summary>
    [Benchmark]
    public void FirstSendWithoutPrewarm()
    {
        // 这里会承担第一次事件类型使用时的初始化成本。
        _coldCenter.Send(new TestEvent(1));
    }

    /// <summary>
    /// 测试预热后第一次发送事件的成本。
    /// </summary>
    [Benchmark]
    public void FirstSendWithPrewarm()
    {
        // 这里理论上应该直接进入派发逻辑。
        _prewarmedCenter.Send(new TestEvent(1));
    }

    /// <summary>
    /// 创建测试用事件中心。
    /// </summary>
    /// <returns>
    /// 返回拥有相同订阅拓扑的事件中心。
    /// </returns>
    private static GlobalEventCenter CreateCenter()
    {
        // 这里按项目实际创建方式实现。
        // 文档中只表达 Benchmark 结构，不绑定具体构造细节。
        throw new NotImplementedException();
    }

    /// <summary>
    /// 测试事件。
    /// </summary>
    private readonly struct TestEvent
    {
        /// <summary>
        /// 测试值。
        /// 用于避免事件完全没有字段。
        /// </summary>
        public readonly int Value;

        /// <summary>
        /// 创建测试事件。
        /// </summary>
        /// <param name="value">
        /// 测试值。
        /// 用于模拟真实事件携带的数据。
        /// </param>
        public TestEvent(int value)
        {
            Value = value;
        }
    }
}
```

---

## 17. 兼容性

### 17.1 对现有用户的影响

如果 Prewarm 是可选 API，则对现有用户没有破坏性影响。

已有代码：

```csharp
var hub = LayerHub
    .CreateLayers()
    .Build();
```

仍然可以正常工作。

新代码：

```csharp
var hub = LayerHub
    .CreateLayers()
    .Build()
    .Prewarm();
```

只是额外提前完成初始化。

---

### 17.2 对 AOT 的影响

AOT 是 Ahead-of-Time Compilation。

它指程序在运行前就完成编译，而不是运行时再 JIT 编译。

LayerBase 如果未来要支持 Unity IL2CPP、NativeAOT 等环境，源生成器生成明确泛型调用比运行时反射扫描更安全。

因此推荐：

```text
源生成器生成 center.PrewarmEvent<TEvent>()
```

不推荐：

```text
运行时 MakeGenericMethod 调用 PrewarmEvent<TEvent>()
```

---

## 18. 实施阶段

### 第一阶段：最小可用版本

实现：

1. `LayerPrewarmTargets`
2. `LayerPrewarmOptions`
3. `GlobalEventCenter.PrewarmEvent<TEvent>()`
4. `EventBucket<TEvent>.PrewarmDispatchTable()`
5. 源生成器生成 `LayerBasePrewarmRegistry.Prewarm(...)`
6. `LayerHub.Prewarm()` 扩展方法

默认范围：

```text
EventTypeId | Bucket | DispatchTable
```

不做：

```text
PostQueue
```

---

### 第二阶段：PostQueue 可选预热

实现：

1. `LayerEventQueue.PrewarmQueue<TEvent>()`
2. `LayerPrewarmTargets.PostQueue`
3. `LayerPrewarmTargets.All`
4. `LayerPrewarmOptions.LayerCount`

---

### 第三阶段：诊断信息

可以增加 Debug 日志或诊断统计：

```text
Prewarmed event count
Prewarmed bucket count
Rebuilt dispatch table count
Prewarmed post queue count
Elapsed time
```

注意：

诊断信息不应进入 Release 热路径。

---

## 19. 风险与规避

### 19.1 启动变慢

风险：

```text
大量事件类型同时预热，会让 Build 后耗时增加。
```

规避：

```text
Prewarm 默认可选。
PostQueue 默认关闭。
允许用户不调用 Prewarm。
```

---

### 19.2 内存增加

风险：

```text
PostQueue 预热会提前创建大量队列。
```

规避：

```text
PostQueue 不进入 Default。
提供 layerCount 限制范围。
```

---

### 19.3 假事件副作用

风险：

```text
如果通过 Send(default) 实现预热，会触发业务逻辑。
```

规避：

```text
严禁使用假事件。
Prewarm 只走缓存创建和派发表清理逻辑。
```

---

### 19.4 源生成器漏收集

风险：

```text
某些事件没有订阅者，但仍然需要预热。
```

规避：

```text
提供 [PrewarmEvent] 手动标记。
```

---

## 20. 最终推荐形态

用户侧最终体验应该是：

```csharp
var hub = LayerHub
    .CreateLayers()
    .Build()
    .Prewarm();
```

默认行为：

```text
预热 EventTypeId
预热 Bucket
预热 DispatchTable
不预热 PostQueue
```

激进模式：

```csharp
var hub = LayerHub
    .CreateLayers()
    .Build()
    .Prewarm(new LayerPrewarmOptions(
        targets: LayerPrewarmTargets.All,
        layerCount: 0));
```

手动标记事件：

```csharp
[PrewarmEvent]
public readonly struct InputBufferedEvent
{
}
```

---

## 21. 总结

LayerBase 的 Prewarm 不应该被设计成核心必需功能，而应该被设计成可选稳定性功能。

它的定位是：

```text
减少第一次事件派发的冷启动尖峰
改善首帧稳定性
改善实时系统帧时间一致性
让 Benchmark 数据更干净
让框架行为更可预测
```

它不应该承诺：

```text
提高长期平均吞吐
让每一次事件派发都更快
替代热路径优化
```

推荐实现优先级：

```text
第一优先级：
    EventTypeId
    Bucket
    DispatchTable

第二优先级：
    PostQueue

第三优先级：
    诊断统计
```

最终判断：

> Prewarm 值得做，但应该默认轻量、显式调用、源生成器驱动、零反射实现。
