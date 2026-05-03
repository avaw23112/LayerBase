# LayerBase 单线程 Runtime 补丁方案

## 0. 改造清单

本次计划包含：

1. 稳定 `EventId / StableEventKey`
2. 删除 `BackpressurePolicy.Coalesce / BackpressurePolicy.Latest`
3. 完善 `MergeFailurePolicy`
4. 增加 `CompletionQueue` 异常策略
5. 增加 `FixedUpdate / PostBuild / RuntimeStart / RuntimeStop`
6. 增加 Runtime Policy Dump

---

## 1. 稳定 EventId / StableEventKey

### 1.1 改造目标

当前 `EventTypeId<TEvent>.Id` 是运行期动态分配的 int ID，适合热路径数组索引，但不适合作为日志、回放、存档、跨版本诊断的稳定标识。

本次不替换现有 `EventTypeId<TEvent>.Id`。

改造后保留两套 ID：

| 名称 | 用途 |
|---|---|
| `RuntimeEventId` | 当前运行期热路径 ID，继续使用 `EventTypeId<TEvent>.Id` |
| `StableEventId` | 稳定事件 ID，用于日志、回放、诊断、序列化 |
| `StableEventKey` | 稳定事件字符串 Key，便于人类阅读和跨版本迁移 |
| `StableEventVersion` | 事件结构版本，用于未来兼容检查 |

---

### 1.2 新增文件

建议新增：

```text
LayerBase/Event/Event/EventIdentityAttribute.cs
LayerBase/Event/Event/EventIdentity.cs
LayerBase/Event/Event/EventIdentityRegistry.cs
```

---

### 1.3 新增 `EventIdentityAttribute`

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 为事件类型声明稳定身份。
///
/// 这个特性只负责稳定诊断身份，不参与热路径事件派发。
/// 热路径仍然使用 EventTypeId<TEvent>.Id。
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class EventIdentityAttribute : Attribute
{
    /// <summary>
    /// 创建事件稳定身份。
    /// </summary>
    /// <param name="stableId">
    /// 稳定数字 ID。
    /// 这个值不应该依赖运行期分配顺序。
    /// 建议由用户显式维护，或由 Source Generator 根据稳定 key 生成并检查冲突。
    /// </param>
    /// <param name="stableKey">
    /// 稳定字符串 Key。
    /// 推荐格式类似 "Combat.DamageApplied"、"UI.InventoryChanged"。
    /// 它用于日志、调试、回放和跨版本迁移。
    /// </param>
    /// <param name="version">
    /// 事件结构版本。
    /// 当事件字段含义发生不兼容变化时，应增加版本号。
    /// </param>
    public EventIdentityAttribute(int stableId, string stableKey, int version = 1)
    {
        if (stableId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stableId),
                "Stable event id must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(stableKey))
        {
            throw new ArgumentException(
                "Stable event key is required.",
                nameof(stableKey));
        }

        StableId = stableId;
        StableKey = stableKey;
        Version = version <= 0 ? 1 : version;
    }

    /// <summary>
    /// 稳定数字 ID。
    /// 用于紧凑日志、回放流、二进制协议等场景。
    /// </summary>
    public int StableId { get; }

    /// <summary>
    /// 稳定字符串 Key。
    /// 用于人类可读诊断和跨版本迁移。
    /// </summary>
    public string StableKey { get; }

    /// <summary>
    /// 事件结构版本。
    /// 用于判断旧数据是否可以安全读取。
    /// </summary>
    public int Version { get; }
}
```

---

### 1.4 新增 `EventIdentity`

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 事件身份快照。
///
/// 它同时包含运行期 ID 和稳定 ID。
/// 运行期 ID 用于当前 Runtime 的热路径。
/// 稳定 ID 用于诊断、日志、回放、存档和跨版本识别。
/// </summary>
public readonly struct EventIdentity
{
    /// <summary>
    /// 创建事件身份快照。
    /// </summary>
    /// <param name="runtimeId">
    /// 运行期事件 ID。
    /// 它来自 EventTypeId<TEvent>.Id，只保证当前进程内可用。
    /// </param>
    /// <param name="stableId">
    /// 稳定数字 ID。
    /// 这个值不应该因为事件类型首次访问顺序不同而变化。
    /// </param>
    /// <param name="stableKey">
    /// 稳定字符串 Key。
    /// 它用于日志和跨版本迁移。
    /// </param>
    /// <param name="version">
    /// 事件结构版本。
    /// 用于判断事件数据结构是否兼容。
    /// </param>
    /// <param name="eventType">
    /// 事件 CLR 类型。
    /// CLR 是 .NET 运行时的类型系统。
    /// 这里保存 Type 只用于诊断，不用于热路径派发。
    /// </param>
    public EventIdentity(
        int runtimeId,
        int stableId,
        string stableKey,
        int version,
        Type eventType)
    {
        RuntimeId = runtimeId;
        StableId = stableId;
        StableKey = stableKey;
        Version = version;
        EventType = eventType;
    }

    /// <summary>
    /// 当前运行期事件 ID。
    /// </summary>
    public int RuntimeId { get; }

    /// <summary>
    /// 稳定数字 ID。
    /// </summary>
    public int StableId { get; }

    /// <summary>
    /// 稳定字符串 Key。
    /// </summary>
    public string StableKey { get; }

    /// <summary>
    /// 事件结构版本。
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// 事件 CLR 类型。
    /// </summary>
    public Type EventType { get; }
}
```

---

### 1.5 新增 `EventIdentityRegistry`

```csharp
using System.Collections.Concurrent;
using System.Reflection;

namespace LayerBase.Core.Event;

/// <summary>
/// 事件稳定身份注册表。
///
/// 注意：
/// 这个注册表不参与热路径事件派发。
/// 它只为调试、日志、Runtime Policy Dump 和回放系统提供稳定信息。
/// </summary>
public static class EventIdentityRegistry
{
    /// <summary>
    /// runtime event id 到事件身份的映射。
    /// key 是 EventTypeId<TEvent>.Id。
    /// value 是事件身份快照。
    /// </summary>
    private static readonly ConcurrentDictionary<int, EventIdentity> s_byRuntimeId = new();

    /// <summary>
    /// stable event id 到事件身份的映射。
    /// key 是 EventIdentityAttribute.StableId。
    /// value 是事件身份快照。
    /// </summary>
    private static readonly ConcurrentDictionary<int, EventIdentity> s_byStableId = new();

    /// <summary>
    /// stable event key 到事件身份的映射。
    /// key 是 EventIdentityAttribute.StableKey。
    /// value 是事件身份快照。
    /// </summary>
    private static readonly ConcurrentDictionary<string, EventIdentity> s_byStableKey = new();

    /// <summary>
    /// 获取或创建指定事件类型的身份信息。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// 它必须是 struct，以保持和现有事件系统约束一致。
    /// </typeparam>
    /// <returns>
    /// 当前事件类型的身份快照。
    /// </returns>
    public static EventIdentity GetOrCreate<TEvent>()
        where TEvent : struct
    {
        var runtimeId = EventTypeId<TEvent>.Id;

        if (s_byRuntimeId.TryGetValue(runtimeId, out var existing))
        {
            return existing;
        }

        return CreateAndRegister<TEvent>(runtimeId);
    }

    /// <summary>
    /// 创建并注册事件身份。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    /// <param name="runtimeId">
    /// 当前运行期事件 ID。
    /// </param>
    /// <returns>
    /// 新创建的事件身份快照。
    /// </returns>
    private static EventIdentity CreateAndRegister<TEvent>(int runtimeId)
        where TEvent : struct
    {
        var eventType = typeof(TEvent);
        var attr = eventType.GetCustomAttribute<EventIdentityAttribute>();

        var stableId = attr?.StableId ?? 0;
        var stableKey = attr?.StableKey ?? eventType.FullName ?? eventType.Name;
        var version = attr?.Version ?? 1;

        var identity = new EventIdentity(
            runtimeId: runtimeId,
            stableId: stableId,
            stableKey: stableKey,
            version: version,
            eventType: eventType);

        if (!s_byRuntimeId.TryAdd(runtimeId, identity))
        {
            return s_byRuntimeId[runtimeId];
        }

        if (stableId > 0 && !s_byStableId.TryAdd(stableId, identity))
        {
            throw new InvalidOperationException(
                $"Duplicate stable event id: {stableId}.");
        }

        if (!s_byStableKey.TryAdd(stableKey, identity))
        {
            throw new InvalidOperationException(
                $"Duplicate stable event key: {stableKey}.");
        }

        return identity;
    }

    /// <summary>
    /// 尝试通过运行期 ID 查找事件身份。
    /// </summary>
    /// <param name="runtimeId">
    /// 运行期事件 ID。
    /// </param>
    /// <param name="identity">
    /// 找到的事件身份。
    /// </param>
    /// <returns>
    /// true 表示找到；false 表示未注册。
    /// </returns>
    public static bool TryGetByRuntimeId(int runtimeId, out EventIdentity identity)
    {
        return s_byRuntimeId.TryGetValue(runtimeId, out identity);
    }

    /// <summary>
    /// 清理注册表。
    /// 该方法应在 LayerHub.Reset 时调用。
    /// </summary>
    public static void Reset()
    {
        s_byRuntimeId.Clear();
        s_byStableId.Clear();
        s_byStableKey.Clear();
    }
}
```

---

### 1.6 接入点

建议修改：

```text
LayerBase/Application/LayerHub.cs
```

在 `LayerHub.Reset()` 中加入：

```csharp
EventIdentityRegistry.Reset();
```

建议修改：

```text
LayerBase/Application/LayerRuntime.cs
```

在 `BuildEventPolicies` 读取 metadata 时调用：

```csharp
_ = EventIdentityRegistry.GetOrCreate<TEvent>();
```

实际这里当前遍历的是 `(Type type, meta)`，所以建议在 `IEventMetaData` 增加：

```csharp
EventIdentity GetIdentity();
```

并在 `EventMetaData<TEvent>` 中实现。

---

## 2. 删除 BackpressurePolicy.Coalesce / Latest

### 2.1 改造目标

`BackpressurePolicy` 只表示普通队列满时的处理策略。  
`Latest / Coalesced` 是 `PostDeliveryMode`，不应该同时出现在 `BackpressurePolicy` 中。

---

### 2.2 修改文件

```text
LayerBase/Event/PostScheduler/BackpressurePolicy.cs
```

修改为：

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 普通 Post 队列满时的背压策略。
///
/// 背压是指：
/// 当生产速度大于消费速度，队列容量不够时，系统如何处理新事件。
/// </summary>
public enum BackpressurePolicy
{
    /// <summary>
    /// 拒绝新事件。
    /// 适合不能丢失旧事件，也不能隐式覆盖事件的场景。
    /// </summary>
    RejectNew,

    /// <summary>
    /// 丢弃新事件。
    /// 适合允许跳过过量事件的通知类场景。
    /// </summary>
    DropNewest,

    /// <summary>
    /// 丢弃最旧事件，然后尝试放入新事件。
    /// 适合“越新的事件越有价值”的普通队列场景。
    /// </summary>
    DropOldest
}
```

---

### 2.3 注意事项

需要全局搜索并删除：

```text
BackpressurePolicy.Coalesce
BackpressurePolicy.Latest
```

如果存在旧调用，建议改成：

```csharp
new EventPostPolicy(
    mode: PostDeliveryMode.Latest,
    backpressure: BackpressurePolicy.RejectNew,
    maxPending: 0)
```

或：

```csharp
new EventPostPolicy(
    mode: PostDeliveryMode.Coalesced,
    backpressure: BackpressurePolicy.RejectNew,
    maxPending: 0,
    mergeFailure: MergeFailurePolicy.Reject)
```

---

## 3. 完善 MergeFailurePolicy

### 3.1 改造目标

当前 `MergeFailurePolicy` 已经有：

```text
Reject
FallbackToLatest
FallbackToNormal
```

但合并失败时没有真正按策略处理。

---

### 3.2 策略语义

| 策略 | 行为 |
|---|---|
| `Reject` | 保留旧 slot，拒绝新事件 |
| `FallbackToLatest` | 用新事件替换旧 slot |
| `FallbackToNormal` | 新事件不再合并，转入普通队列 |

---

### 3.3 修改文件

```text
LayerBase/Event/PostScheduler/PostScheduler.cs
```

---

### 3.4 建议实现结构

不要在 `_bufferLock` 内直接调用普通队列入队。  
因为普通队列入队会拿 `_queueLock`，未来如果有反向锁顺序，可能埋死锁风险。

建议把 `EnqueueCoalescedInternal` 改造成：

```csharp
private PostResult EnqueueCoalescedInternal<T>(int typeId, in T value)
    where T : struct
{
    var meta = _policyTable.GetMetaData<T>(typeId);

    // coalesceKey：
    // 用于决定哪些事件可以合并到同一个 slot。
    // 如果 metadata 没有提供 key，则默认使用 0，表示同类型事件进入同一个合并槽。
    int coalesceKey = meta?.GetPostCoalesceKey(value) ?? 0;

    var slotKey = new CoalescedSlotKey(typeId, coalesceKey);

    // fallbackToNormal：
    // 标记合并失败后是否需要退出 _bufferLock，再走普通队列入队。
    bool fallbackToNormal = false;

    // fallbackPlan：
    // 保存普通入队需要的策略。
    // 它必须在锁内复制出来，锁外不能再用 ref readonly 指向数组元素。
    PostTypePlan fallbackPlan = default;

    lock (_bufferLock)
    {
        if (_coalescedBuffer.TryGetValue(slotKey, out var slot))
        {
            ref T current = ref _payloadStorage.GetRef<T>(_runtimeId, slot.PayloadHandle);

            if (meta != null && meta.TryMergePostEvent(ref current, in value))
            {
                slot.LastSequenceId = Interlocked.Increment(ref _sequenceCounter);
                slot.MergeCount++;
                _coalescedBuffer[slotKey] = slot;
                return PostResult.Coalesced();
            }

            ref readonly var planRef = ref GetPlan(typeId);
            fallbackPlan = planRef;

            var result = HandleMergeFailureInternalLocked(
                slotKey: slotKey,
                slot: slot,
                value: in value,
                plan: in fallbackPlan,
                fallbackToNormal: out fallbackToNormal);

            if (!fallbackToNormal)
            {
                return result;
            }
        }
        else
        {
            var handle = _payloadStorage.Store(_runtimeId, value);
            var seq = Interlocked.Increment(ref _sequenceCounter);

            var newSlot = new CoalescedSlot
            {
                Key = slotKey,
                PayloadHandle = handle,
                FirstSequenceId = seq,
                LastSequenceId = seq,
                MergeCount = 1,
                Active = true
            };

            _coalescedBuffer[slotKey] = newSlot;
            _pendingCoalesced.Add(slotKey);
            return PostResult.Enqueued();
        }
    }

    // 注意：
    // FallbackToNormal 必须在 _bufferLock 外执行。
    // 这样可以避免 _bufferLock -> _queueLock 的锁顺序被固定下来。
    if (fallbackToNormal)
    {
        return EnqueueNormalWithPlan(typeId, in value, in fallbackPlan);
    }

    return PostResult.Failure("Merge failed");
}
```

---

### 3.5 新增锁内处理方法

```csharp
private PostResult HandleMergeFailureInternalLocked<T>(
    CoalescedSlotKey slotKey,
    CoalescedSlot slot,
    in T value,
    in PostTypePlan plan,
    out bool fallbackToNormal)
    where T : struct
{
    fallbackToNormal = false;

    switch (plan.MergeFailure)
    {
        case MergeFailurePolicy.Reject:
        {
            // Reject：
            // 保留旧 slot。
            // 新事件不会入队，也不会覆盖旧事件。
            return PostResult.Failure("Merge failed.");
        }

        case MergeFailurePolicy.FallbackToLatest:
        {
            // FallbackToLatest：
            // 新事件替换旧 slot。
            // 这适合状态快照类事件，因为旧状态已经没有继续派发的价值。
            _payloadStorage.Release(slot.PayloadHandle);

            slot.PayloadHandle = _payloadStorage.Store(_runtimeId, value);
            slot.LastSequenceId = Interlocked.Increment(ref _sequenceCounter);
            slot.MergeCount = 1;
            slot.Active = true;

            _coalescedBuffer[slotKey] = slot;
            return PostResult.Coalesced();
        }

        case MergeFailurePolicy.FallbackToNormal:
        {
            // FallbackToNormal：
            // 新事件不再合并，转入普通队列。
            // 这里只设置标记，不在锁内入队。
            fallbackToNormal = true;
            return PostResult.Enqueued();
        }

        default:
        {
            return PostResult.Failure(
                $"Unsupported merge failure policy: {plan.MergeFailure}.");
        }
    }
}
```

---

### 3.6 `PostTypePlan` 需要包含 `MergeFailure`

如果当前 `PostTypePlan` 没有保存 `MergeFailurePolicy`，需要补字段。

```csharp
internal readonly struct PostTypePlan
{
    /// <summary>
    /// 创建 Post 类型计划。
    /// </summary>
    /// <param name="eventTypeId">
    /// 运行期事件类型 ID。
    /// 它用于数组索引。
    /// </param>
    /// <param name="mode">
    /// 投递模式。
    /// 它决定事件进入普通队列、Dirty、Latest 还是 Coalesced 管线。
    /// </param>
    /// <param name="backpressure">
    /// 普通队列满时的处理策略。
    /// </param>
    /// <param name="maxPending">
    /// 当前事件类型允许的最大待处理数量。
    /// 0 表示不启用按类型 pending 数限制。
    /// </param>
    /// <param name="defaultBackpressure">
    /// Runtime 默认背压策略。
    /// 当事件没有显式策略时使用。
    /// </param>
    /// <param name="mergeFailure">
    /// Coalesced 模式下合并失败时的处理策略。
    /// </param>
    public PostTypePlan(
        int eventTypeId,
        PostDeliveryMode mode,
        BackpressurePolicy backpressure,
        int maxPending,
        BackpressurePolicy defaultBackpressure,
        MergeFailurePolicy mergeFailure = MergeFailurePolicy.Reject)
    {
        EventTypeId = eventTypeId;
        Mode = mode;
        Backpressure = backpressure;
        MaxPending = maxPending;
        DefaultBackpressure = defaultBackpressure;
        MergeFailure = mergeFailure;
    }

    public int EventTypeId { get; }
    public PostDeliveryMode Mode { get; }
    public BackpressurePolicy Backpressure { get; }
    public int MaxPending { get; }
    public BackpressurePolicy DefaultBackpressure { get; }
    public MergeFailurePolicy MergeFailure { get; }

    public bool TrackPending => MaxPending > 0;
}
```

---

## 4. CompletionQueue 异常策略

### 4.1 改造目标

当前 `MainThreadCompletionQueue.Drain` 执行 completion 时，如果 action 抛异常，会直接中断 Drain。  
这对 Debug 有用，但对 Release 运行时不够稳定。

---

### 4.2 新增枚举

建议新增：

```text
LayerBase.Task/CompletionExceptionPolicy.cs
```

```csharp
namespace LayerBase.Async;

/// <summary>
/// 主线程 CompletionQueue 的异常处理策略。
/// </summary>
public enum CompletionExceptionPolicy
{
    /// <summary>
    /// 抛出异常。
    /// 适合 Debug 模式，方便尽早暴露问题。
    /// </summary>
    Throw,

    /// <summary>
    /// 上报异常并继续处理后续 completion。
    /// 适合 Release 模式或容错运行环境。
    /// </summary>
    ReportAndContinue
}
```

---

### 4.3 新增统计结构

```csharp
namespace LayerBase.Async;

/// <summary>
/// CompletionQueue Drain 的统计结果。
/// </summary>
public readonly struct CompletionDrainStats
{
    /// <summary>
    /// 创建 CompletionQueue Drain 统计结果。
    /// </summary>
    /// <param name="processed">
    /// 本次成功执行或已处理的 completion 数量。
    /// </param>
    /// <param name="errors">
    /// 本次 Drain 捕获到的异常数量。
    /// </param>
    /// <param name="remaining">
    /// 本次 Drain 后仍留在队列中的 completion 数量。
    /// </param>
    public CompletionDrainStats(int processed, int errors, int remaining)
    {
        Processed = processed;
        Errors = errors;
        Remaining = remaining;
    }

    public int Processed { get; }
    public int Errors { get; }
    public int Remaining { get; }
}
```

---

### 4.4 修改 `MainThreadCompletionQueue`

```csharp
internal sealed class MainThreadCompletionQueue
{
    private readonly ConcurrentQueue<MainThreadCompletionItem> _queue = new();

    public int PendingCount => _queue.Count;

    public void Enqueue(MainThreadCompletionItem item)
    {
        _queue.Enqueue(item);
    }

    public void Enqueue(Action action)
    {
        _queue.Enqueue(new MainThreadCompletionItem(action));
    }

    public void Drain(
        int maxCount,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        var processed = 0;
        var errors = 0;

        while ((maxCount <= 0 || processed < maxCount) &&
               _queue.TryDequeue(out var item))
        {
            try
            {
                item.Complete();
            }
            catch (Exception ex)
            {
                if (exceptionPolicy == CompletionExceptionPolicy.Throw)
                {
                    throw;
                }
                reportException?.Invoke(ex);
            }
        }
    }
}

---

## 5. FixedUpdate / PostBuild / RuntimeStart / RuntimeStop

### 5.1 改造目标

增加引擎无关生命周期接口。  
这些接口不绑定 Unity、Godot 或任何具体引擎，只描述系统框架内部的 Runtime 生命周期。

---

### 5.2 新增接口文件

建议新增：

```text
LayerBase/Application/Lifecycle/IPostBuild.cs
LayerBase/Application/Lifecycle/IRuntimeStart.cs
LayerBase/Application/Lifecycle/IRuntimeStop.cs
LayerBase/Application/Lifecycle/IFixedUpdate.cs
```

---

### 5.3 接口定义

```csharp
namespace LayerBase;

/// <summary>
/// Build 完成后调用。
///
/// 此时：
/// 1. DI 已经完成。
/// 2. 自动订阅已经完成。
/// 3. Call 路由已经完成。
/// 4. SharedField 已经完成。
///
/// 适合做跨服务的最终检查或缓存预热。
/// </summary>
public interface IPostBuild
{
    void PostBuild();
}
```

```csharp
namespace LayerBase;

/// <summary>
/// Runtime 启动回调。
///
/// 它发生在 Build 完成之后。
/// 它表示当前 Runtime 已经可以开始正常 Pump。
/// </summary>
public interface IRuntimeStart
{
    void RuntimeStart();
}
```

```csharp
namespace LayerBase;

/// <summary>
/// Runtime 停止回调。
///
/// 它发生在 Runtime Dispose 释放服务之前。
/// 适合保存临时状态、取消外部订阅、清理非托管资源引用。
/// </summary>
public interface IRuntimeStop
{
    void RuntimeStop();
}
```

```csharp
namespace LayerBase;

/// <summary>
/// 固定步长更新接口。
///
/// 固定步长更新不依赖具体游戏引擎。
/// 它适合需要稳定 tick 的系统，例如模拟、战斗结算、输入缓冲推进。
/// </summary>
public interface IFixedUpdate
{
    /// <summary>
    /// 执行固定步长更新。
    /// </summary>
    /// <param name="fixedDeltaTime">
    /// 固定步长时间。
    /// 例如 1f / 60f 表示每秒 60 次固定更新。
    /// </param>
    void FixedUpdate(float fixedDeltaTime);
}
```

---

### 5.4 新增 FixedUpdate 配置

```csharp
namespace LayerBase;

/// <summary>
/// 固定步长更新配置。
/// </summary>
public readonly struct FixedUpdateOptions
{
    /// <summary>
    /// 创建固定步长更新配置。
    /// </summary>
    /// <param name="enabled">
    /// 是否启用固定步长更新。
    /// false 表示 Runtime 不执行 IFixedUpdate。
    /// </param>
    /// <param name="fixedDeltaTime">
    /// 固定步长时间。
    /// 例如 1f / 60f。
    /// </param>
    /// <param name="maxStepsPerPump">
    /// 单次 Pump 最多执行多少次 FixedUpdate。
    /// 它用于避免 deltaTime 很大时一次补太多帧导致卡死。
    /// </param>
    public FixedUpdateOptions(
        bool enabled,
        float fixedDeltaTime,
        int maxStepsPerPump)
    {
        Enabled = enabled;
        FixedDeltaTime = fixedDeltaTime <= 0 ? 1f / 60f : fixedDeltaTime;
        MaxStepsPerPump = maxStepsPerPump <= 0 ? 4 : maxStepsPerPump;
    }

    public bool Enabled { get; }
    public float FixedDeltaTime { get; }
    public int MaxStepsPerPump { get; }

    public static FixedUpdateOptions Disabled => new(false, 1f / 60f, 4);

    public static FixedUpdateOptions Default => new(true, 1f / 60f, 4);
}
```

---

### 5.5 Layer 收集生命周期对象

在 `Layer` 中增加：

```csharp
private readonly List<IPostBuild> m_postBuilds = new();
private readonly List<IRuntimeStart> m_runtimeStarts = new();
private readonly List<IRuntimeStop> m_runtimeStops = new();
private readonly List<IFixedUpdate> m_fixedUpdates = new();
```

在 `FinalizeBuild()` 解析服务后收集：

```csharp
foreach (var resolved in m_resolvedServices)
{
    if (resolved.Instance is IInitializable init)
    {
        init.Initialize();
    }

    if (resolved.Instance is IUpdate up)
    {
        m_serviceUpdates.Add(up);
    }

    if (resolved.Instance is IFixedUpdate fixedUpdate)
    {
        m_fixedUpdates.Add(fixedUpdate);
    }

    if (resolved.Instance is IPostBuild postBuild)
    {
        m_postBuilds.Add(postBuild);
    }

    if (resolved.Instance is IRuntimeStart runtimeStart)
    {
        m_runtimeStarts.Add(runtimeStart);
    }

    if (resolved.Instance is IRuntimeStop runtimeStop)
    {
        m_runtimeStops.Add(runtimeStop);
    }
}
```

Layer 本身也应该支持这些接口：

```csharp
if (this is IFixedUpdate layerFixedUpdate) m_fixedUpdates.Add(layerFixedUpdate);
if (this is IPostBuild layerPostBuild) m_postBuilds.Add(layerPostBuild);
if (this is IRuntimeStart layerRuntimeStart) m_runtimeStarts.Add(layerRuntimeStart);
if (this is IRuntimeStop layerRuntimeStop) m_runtimeStops.Add(layerRuntimeStop);
```

---

### 5.6 Layer 暴露内部调用方法

```csharp
internal void RunPostBuild()
{
    for (var i = 0; i < m_postBuilds.Count; i++)
    {
        m_postBuilds[i].PostBuild();
    }
}

internal void RunRuntimeStart()
{
    for (var i = 0; i < m_runtimeStarts.Count; i++)
    {
        m_runtimeStarts[i].RuntimeStart();
    }
}

internal void RunRuntimeStop()
{
    for (var i = 0; i < m_runtimeStops.Count; i++)
    {
        m_runtimeStops[i].RuntimeStop();
    }
}

internal void PumpFixed(float fixedDeltaTime)
{
    for (var i = 0; i < m_fixedUpdates.Count; i++)
    {
        m_fixedUpdates[i].FixedUpdate(fixedDeltaTime);
    }
}
```

---

### 5.7 LayerChain 调用顺序

`LayerChain.Build()` 建议在所有 Layer `FinalizeBuild()` 之后执行：

```text
FinalizeBuild all layers
RunPostBuild all layers
RunRuntimeStart all layers
EventGraphValidator.Validate
```

`RuntimeStop` 应在 Dispose 早期调用，必须早于服务 Dispose。

---

### 5.8 Runtime Pump 中增加 FixedUpdate

建议顺序：

```text
1. Timer.Tick
2. DelayManager.Tick
3. CompletionQueue.Drain
4. FixedUpdate accumulator
5. PostScheduler.Pump
6. Layer Update
```

原因：

- Timer / Delay 先推进时间源。
- Completion 先回主线程，保证后台结果本帧可见。
- FixedUpdate 再推进稳定模拟。
- Post 统一派发本帧已积累事件。
- Layer Update 最后执行普通更新。

---

## 6. 增加 Runtime Policy Dump

### 6.1 改造目标

当前拓扑报告可以看到 Layer、订阅、Call、SharedField。  
但看不到每个事件的 Post 策略、Timer 策略、Buffer 策略、稳定身份。

需要新增：

```csharp
public string GetPolicyMarkdown()
```

---

### 6.2 输出内容

建议输出：

```text
# LayerBase Runtime Policy Dump

## Event Policies

| RuntimeId | StableId | StableKey | Version | Event Type | Post Mode | Backpressure | MaxPending | MergeFailure | Timer | Buffer |
|---|---:|---|---:|---|---|---|---:|---|---|---|
```

---

### 6.3 需要的导出接口

建议在 `EventRuntimePolicyTable` 中增加：

```csharp
public IEnumerable<EventPolicySnapshot> ExportSnapshots()
```

新增结构：

```csharp
namespace LayerBase.Core.Event;

/// <summary>
/// 事件策略快照。
/// </summary>
public readonly struct EventPolicySnapshot
{
    /// <summary>
    /// 创建事件策略快照。
    /// </summary>
    /// <param name="runtimeId">
    /// 运行期事件 ID。
    /// </param>
    /// <param name="identity">
    /// 事件稳定身份。
    /// </param>
    /// <param name="postPolicy">
    /// Post 投递策略。
    /// </param>
    /// <param name="timerPolicy">
    /// Timer 策略。
    /// </param>
    /// <param name="bufferPolicy">
    /// Buffer 策略。
    /// </param>
    public EventPolicySnapshot(
        int runtimeId,
        EventIdentity identity,
        EventPostPolicy? postPolicy,
        EventTimerPolicy? timerPolicy,
        EventBufferPolicy? bufferPolicy)
    {
        RuntimeId = runtimeId;
        Identity = identity;
        PostPolicy = postPolicy;
        TimerPolicy = timerPolicy;
        BufferPolicy = bufferPolicy;
    }

    public int RuntimeId { get; }
    public EventIdentity Identity { get; }
    public EventPostPolicy? PostPolicy { get; }
    public EventTimerPolicy? TimerPolicy { get; }
    public EventBufferPolicy? BufferPolicy { get; }
}
```

---

### 6.4 Runtime 方法

在 `LayerRuntime` 中增加：

```csharp
public string GetPolicyMarkdown()
{
    if (_policyTable == null)
    {
        return "Runtime not built.";
    }

    var sb = new StringBuilder();

    sb.AppendLine("# LayerBase Runtime Policy Dump");
    sb.AppendLine();
    sb.AppendLine("## Event Policies");
    sb.AppendLine("| RuntimeId | StableId | StableKey | Version | Event Type | Post Mode | Backpressure | MaxPending | MergeFailure | Timer | Buffer |");
    sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |");

    foreach (var snapshot in _policyTable.ExportSnapshots())
    {
        var post = snapshot.PostPolicy;
        var timer = snapshot.TimerPolicy;
        var buffer = snapshot.BufferPolicy;
        var identity = snapshot.Identity;

        sb.Append("| ")
          .Append(snapshot.RuntimeId)
          .Append(" | ")
          .Append(identity.StableId)
          .Append(" | `")
          .Append(identity.StableKey)
          .Append("` | ")
          .Append(identity.Version)
          .Append(" | ")
          .Append(identity.EventType.Name)
          .Append(" | ")
          .Append(post?.Mode.ToString() ?? "Normal")
          .Append(" | ")
          .Append(post?.Backpressure.ToString() ?? "Default")
          .Append(" | ")
          .Append(post?.MaxPending.ToString() ?? "0")
          .Append(" | ")
          .Append(post?.MergeFailure.ToString() ?? "Reject")
          .Append(" | ")
          .Append(timer != null ? "Yes" : "No")
          .Append(" | ")
          .Append(buffer != null ? "Yes" : "No")
          .AppendLine(" |");
    }

    return sb.ToString();
}
```

---

## 8. 推荐提交顺序

### Commit 1：清理 Post 背压语义

```text
remove obsolete backpressure modes
```

内容：

- 删除 `BackpressurePolicy.Coalesce`
- 删除 `BackpressurePolicy.Latest`
- 修正所有引用
- 文档说明 Latest/Coalesced 属于 `PostDeliveryMode`

---

### Commit 2：完善 MergeFailurePolicy

```text
implement merge failure handling for coalesced post
```

内容：

- `PostTypePlan` 增加 `MergeFailure`
- `BuildEventPolicies` 填充 `MergeFailure`
- `HandleMergeFailureInternal` 实装三个策略
- 补测试

---

### Commit 3：稳定事件身份

```text
add stable event identity
```

内容：

- 新增 `EventIdentityAttribute`
- 新增 `EventIdentity`
- 新增 `EventIdentityRegistry`
- `EventMetaData<TEvent>` 暴露身份
- `LayerHub.Reset` 清理注册表

---

### Commit 4：CompletionQueue 异常策略

```text
add completion queue exception policy
```

内容：

- 新增 `CompletionExceptionPolicy`
- 修改 `MainThreadCompletionQueue.Drain`
- Runtime 按 Debug/Release 策略上报或抛出

---

### Commit 5：生命周期接口

```text
add runtime lifecycle interfaces
```

内容：

- `IPostBuild`
- `IRuntimeStart`
- `IRuntimeStop`
- `IFixedUpdate`
- `FixedUpdateOptions`
- Layer 收集并调用生命周期对象

---

### Commit 6：调度统计快照

```text
add runtime pump stats
```

内容：

- `RuntimePumpStats`
- `LayerChain.Pump` 返回更新数量
- `LayerRuntime.LastPumpStats`
- `LayerRuntime.PumpAndGetStats`

---

### Commit 6：Runtime Policy Dump

```text
add runtime policy dump
```

内容：

- `EventPolicySnapshot`
- `EventRuntimePolicyTable.ExportSnapshots`
- `LayerRuntime.GetPolicyMarkdown`
- Debug 模式下可选择输出 Policy Dump

---

## 9. 测试建议

### 9.1 Stable EventId

测试点：

```text
- 带 EventIdentityAttribute 的事件能正确注册 StableId 和 StableKey
- 重复 StableId 抛异常
- 重复 StableKey 抛异常
- LayerHub.Reset 后注册表清空
```

### 9.2 BackpressurePolicy

测试点：

```text
- 枚举不再包含 Coalesce / Latest
- Normal 队列满时 RejectNew / DropNewest / DropOldest 行为不变
- PostDeliveryMode.Latest 仍然正常工作
- PostDeliveryMode.Coalesced 仍然正常工作
```

### 9.3 MergeFailurePolicy

测试点：

```text
- Reject：合并失败后旧事件保留，新事件失败
- FallbackToLatest：合并失败后旧 payload 被释放，新 payload 替换旧 slot
- FallbackToNormal：合并失败后新事件进入普通队列
```

### 9.4 CompletionQueue

测试点：

```text
- Throw 策略下 completion 异常会抛出
- ReportAndContinue 策略下 completion 异常会上报，并继续处理后续 completion
```

### 9.5 生命周期

测试点：

```text
- PostBuild 在所有 FinalizeBuild 之后执行
- RuntimeStart 在 PostBuild 之后执行
- RuntimeStop 在服务 Dispose 前执行
- FixedUpdate 按 fixedDeltaTime 累积执行
- MaxStepsPerPump 能限制补帧次数
```

### 9.6 Policy Dump

测试点：

```text
- GetPolicyMarkdown 在 Runtime 未 Build 时返回明确提示
- Build 后能输出所有 EventMetaData 对应策略
- StableId / StableKey 能出现在表格中
- Latest / Coalesced 策略显示正确
```

---

## 10. 本次不做内容

本次明确不做：

```text
- 资源加载系统
- UI 管线
- 场景系统
- 音频系统
- 动画系统
- 网络系统
- 游戏对象系统
- ECS
- 引擎适配层
```

这些都应该放到具体引擎项目或上层框架中实现。

LayerBase 本次只继续强化：

```text
- 单线程 Runtime
- 事件身份
- Post 调度语义
- 生命周期
- Completion 异常策略
- 策略可视化
```
