# Codex 实现文档：LayerBase P0/P1 生命周期与 DI 修复

## 范围

本次实现以下项目：

```text
P0:
1. DelayPublisher 生命周期闭环
2. ServiceLayerBinding Detach
4. Singleton / Instance 绑定策略收紧

P1:
2. EventStore Dispose 清理 buffer
3. DI 构造函数选择收紧
```
---

## P0-1：DelayPublisher 生命周期闭环

### 目标

解决旧 `DelayPublisher` 被 `DelayPublisherManager._publishers` 长期持有的问题。

当前风险链路：

```text
Layer.m_delayPublishers
    -> DelayPublisher<T>
    -> Owner: Layer

DelayPublisherManager._publishers
    -> DelayPublisher<T>
    -> Owner: Layer
```

`Layer.PrepareBuild()` 和 `Layer.Dispose()` 清空了 Layer 本地的 `m_delayPublishers`，但 `DelayPublisherManager` 仍然可能保留旧 publisher。

### 涉及文件

```text
LayerBase/Event/Delay/IDelayPublisherInternal.cs
LayerBase/Event/Delay/DelayPublisher.cs
LayerBase/Event/Delay/DelayPublisherManager.cs
LayerBase/Event/Delay/DelayBufferWheel.cs
LayerBase/Layer/Layer.cs
```

---

### 1. 修改 `IDelayPublisherInternal`

文件：

```text
LayerBase/Event/Delay/IDelayPublisherInternal.cs
```

增加 `PublisherId`。

```csharp
namespace LayerBase.Event.Delay;

internal interface IDelayPublisherInternal
{
    /// <summary>
    /// 当前 publisher 在 DelayPublisherManager 中的注册 ID。
    ///
    /// -1 表示尚未注册，或者已经注销。
    /// Layer 释放 DelayPublisher 时，通过该 ID 让 DelayPublisherManager 删除对应引用。
    /// </summary>
    int PublisherId { get; }

    void ClearValue();
    void Deactivate();
    bool TryExpire(int valueVersion);
    void Reset();
    bool HasActiveDelays { get; }
}
```

---

### 2. 修改 `DelayPublisher<T>`

文件：

```text
LayerBase/Event/Delay/DelayPublisher.cs
```

要求：

1. `_publisherId` 默认值改为 `-1`。
2. 实现 `PublisherId`。
3. `SetId` 保持 internal。
4. `Deactivate()` 必须取消未过期的 timer。
5. `Deactivate()` 必须清空 value。
6. `Deactivate()` 必须把 `_publisherId` 设回 `-1`。

示例实现：

```csharp
internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherInternal where T : struct
{
    private T _value;
    private bool _hasValue;
    private int _valueVersion;
    private DelayTimerHandle _timerHandle = DelayTimerHandle.Invalid;

    /// <summary>
    /// 当前 publisher 在 DelayPublisherManager 中的注册 ID。
    ///
    /// -1 表示当前 publisher 不在 manager 的有效注册表中。
    /// </summary>
    private int _publisherId = -1;

    private bool _deactivated;
    private readonly DelayPublisherManager _manager;
    private readonly object _lock = new();

    public int PublisherId => _publisherId;

    internal void SetId(int id)
    {
        // id：
        // DelayPublisherManager 分配的 publisher ID。
        // 后续过期回调、注销、contract replace 都通过这个 ID 找到 publisher。
        _publisherId = id;
    }

    public void Deactivate()
    {
        DelayTimerHandle oldHandle;

        lock (_lock)
        {
            if (_deactivated)
            {
                return;
            }

            _deactivated = true;

            // 先保存旧 timer handle。
            // ClearInternal 会把 _timerHandle 改成 Invalid。
            oldHandle = _timerHandle;

            ClearInternal();

            // 标记为未注册。
            // 避免 Layer 后续重复释放时再次请求 manager 注销同一个 ID。
            _publisherId = -1;
        }

        // 在对象锁外取消 timer，避免锁顺序反转。
        //
        // oldHandle：
        // 此 publisher 当前挂在 DelayBufferWheel 上的过期任务句柄。
        // 如果句柄已经失效，CancelExpire 会直接忽略。
        _manager.CancelExpire(oldHandle);
    }
}
```

注意：保留现有 `Publish`、`TryExpire`、`ClearValue` 的语义，不要改变对外行为。

---

### 3. 修改 `DelayPublisherManager`

文件：

```text
LayerBase/Event/Delay/DelayPublisherManager.cs
```

要求：

1. `_publishers` 改为可空列表。
2. 新增 `_freePublisherIds` 复用空槽。
3. `RegisterPublisher` 优先复用空槽。
4. 新增 `UnregisterPublisher(int publisherId)`。
5. 新增 `CancelExpire(DelayTimerHandle handle)`。
6. `ExpirePublisher`、`NotifyPublished` 需要处理空槽。
7. `Clear` 需要处理可空 publisher。

示例结构：

```csharp
internal sealed class DelayPublisherManager : IDelayPublisherManager
{
    /// <summary>
    /// publisher 注册表。
    ///
    /// 使用可空槽位，是为了支持 Layer 重建或释放时注销旧 publisher。
    /// null 表示该 ID 当前没有有效 publisher。
    /// </summary>
    private readonly List<IDelayPublisherInternal?> _publishers = new();

    /// <summary>
    /// 可复用的 publisher ID。
    ///
    /// UnregisterPublisher 会把空出来的 ID 放进这里。
    /// RegisterPublisher 优先复用这些 ID，避免 _publishers 无限增长。
    /// </summary>
    private readonly Stack<int> _freePublisherIds = new();

    public int RegisterPublisher(IDelayPublisherInternal publisher)
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        ThrowIfDisposed();

        lock (_lock)
        {
            ThrowIfDisposed();

            if (_freePublisherIds.Count > 0)
            {
                var reusedId = _freePublisherIds.Pop();
                _publishers[reusedId] = publisher;
                return reusedId;
            }

            var id = _publishers.Count;
            _publishers.Add(publisher);
            return id;
        }
    }

    public void UnregisterPublisher(int publisherId)
    {
        if (publisherId < 0)
        {
            return;
        }

        IDelayPublisherInternal? publisher = null;

        lock (_lock)
        {
            if ((uint)publisherId >= (uint)_publishers.Count)
            {
                return;
            }

            publisher = _publishers[publisherId];

            if (publisher == null)
            {
                return;
            }

            _publishers[publisherId] = null;
            _freePublisherIds.Push(publisherId);

            RemoveContractsForPublisherLocked(publisherId);
        }

        // 在 manager 锁外 Deactivate，避免 publisher 内部再访问 manager 时造成死锁。
        publisher.Deactivate();
    }

    private void RemoveContractsForPublisherLocked(int publisherId)
    {
        // publisherId：
        // 需要从 contract 活跃表中删除的 publisher ID。
        //
        // 这里不能在遍历 Dictionary 时直接删除当前项，
        // 所以先收集 key，再统一 Remove。
        var keysToRemove = new List<DelayContractKey>();

        foreach (var kvp in _contractToActivePublisher)
        {
            if (kvp.Value == publisherId)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _contractToActivePublisher.Remove(key);
        }
    }

    public void CancelExpire(DelayTimerHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        lock (_wheelLock)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            _wheel.Cancel(handle);
        }
    }

    internal void ExpirePublisher(int publisherId, int valueVersion)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        IDelayPublisherInternal? pub = null;

        lock (_lock)
        {
            if ((uint)publisherId < (uint)_publishers.Count)
            {
                pub = _publishers[publisherId];
            }
        }

        pub?.TryExpire(valueVersion);
    }

    public void NotifyPublished(int publisherId, int contractId)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        if (contractId == 0)
        {
            return;
        }

        var key = new DelayContractKey(0, contractId);
        IDelayPublisherInternal? publisherToClear = null;

        lock (_lock)
        {
            if (_contractToActivePublisher.TryGetValue(key, out var activeId))
            {
                if (activeId != publisherId &&
                    activeId >= 0 &&
                    activeId < _publishers.Count)
                {
                    publisherToClear = _publishers[activeId];
                }
            }

            _contractToActivePublisher[key] = publisherId;
        }

        publisherToClear?.ClearValue();
    }

    public void Clear()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        IDelayPublisherInternal?[] publishers;

        lock (_lock)
        {
            publishers = _publishers.ToArray();
            _publishers.Clear();
            _freePublisherIds.Clear();
            _contractToActivePublisher.Clear();
            PolicyTable = null;
        }

        lock (_wheelLock)
        {
            _wheel.Clear();
        }

        foreach (var pub in publishers)
        {
            pub?.Deactivate();
        }
    }
}
```

---

### 4. 修改 `Layer`

文件：

```text
LayerBase/Layer/Layer.cs
```

新增 helper：

```csharp
private void ReleaseDelayPublishers()
{
    if (m_delayPublishers.IsEmpty)
    {
        return;
    }

    var manager = OwnerContext?.DelayManager;

    foreach (var publisher in m_delayPublishers.Values)
    {
        if (manager != null && publisher.PublisherId >= 0)
        {
            // manager.UnregisterPublisher 会调用 publisher.Deactivate。
            manager.UnregisterPublisher(publisher.PublisherId);
        }
        else
        {
            // Runtime 或 DelayManager 已经不存在时，至少本地失活。
            publisher.Deactivate();
        }
    }

    m_delayPublishers.Clear();

    OwnerContext?.MarkDelayDirty();
}
```

在 `PrepareBuild()` 中，替换原来的：

```csharp
m_delayPublishers.Clear();
```

为：

```csharp
ReleaseDelayPublishers();
```

在 `Dispose()` 中，替换原来的：

```csharp
m_delayPublishers.Clear();
```

为：

```csharp
ReleaseDelayPublishers();
```

---

## P0-2：ServiceLayerBinding Detach

### 目标

解决源生成器绑定槽位导致旧 Runtime / Layer 被 service/context 强引用的问题。

`ServiceLayerBinding` 持有：

```text
Layer?
LayerRuntime
EventCenter
```

如果对象自身的 `__LayerBaseBinding` 不清空，用户只要仍持有 service/context，就可能间接持有整个旧 Runtime。

### 涉及文件

```text
LayerBase/DI/ServiceContracts.cs
LayerBase/Layer/Layer.cs
LayerBase/Application/LayerRuntime.cs
```

---

### 1. 修改 `ServiceLayerBinder`

文件：

```text
LayerBase/DI/ServiceContracts.cs
```

新增 `Detach(object service)`：

```csharp
public static void Detach(object service)
{
    if (service == null)
    {
        return;
    }

    if (service is ILayerBindingAccessor accessor)
    {
        // 清空源生成器写入对象自身的绑定槽位。
        // 这是避免 service/context 继续强引用 Runtime / Layer 的关键。
        accessor.__LayerBaseBinding = null;
    }

    // 兼容没有实现 ILayerBindingAccessor 的旧对象。
    // Remove 即使 key 不存在也安全。
    s_bindingMap.Remove(service);

    if (service is IInternalLayerContext internalContext)
    {
        // -1 表示该 context 当前没有有效 Layer 归属。
        internalContext.LayerIndex = -1;
    }
}
```

---

### 2. 修改 `Layer`

文件：

```text
LayerBase/Layer/Layer.cs
```

新增 helper：

```csharp
private void DetachResolvedObjects()
{
    ServiceLayerBinder.Detach(this);

    foreach (var registration in m_activeServices)
    {
        ServiceLayerBinder.Detach(registration.Service);
    }

    foreach (var registration in m_manualServices)
    {
        ServiceLayerBinder.Detach(registration.Service);
    }

    foreach (var resolved in m_resolvedServices)
    {
        ServiceLayerBinder.Detach(resolved.Instance);
    }
}
```

在 `PrepareBuild()` 开头清理旧构建产物前调用：

```csharp
DetachResolvedObjects();
```

注意：`PrepareBuild()` 后续会重新把 manual service 加回 active service，并通过 `AddActiveService` 重新 `Attach`。

在 `Dispose()` 中，在清空集合前调用：

```csharp
DetachResolvedObjects();
```

---

### 3. 清理 Layer 自身对 Runtime 的引用

`Layer` 自身还有 `OwnerContext`，即使清空 `ServiceLayerBinding`，用户若持有 Layer，也可能继续持有 Runtime。

在 `Layer` 中新增：

```csharp
internal void DetachFromContext()
{
    ServiceLayerBinder.Detach(this);

    OwnerContext = null;
    RouteIndex = -1;
}
```

在 `Dispose()` 的最后调用：

```csharp
DetachFromContext();
```

注意：`Dispose()` 中如果还有逻辑需要使用 `OwnerContext`，必须放在 `DetachFromContext()` 之前。

---

## P0-4：Singleton / Instance 绑定策略收紧

### 目标

避免世界级 `Singleton` / `Instance` 被某个 Layer 错误绑定。

当前风险：

```text
Singleton / Instance:
    如果已经有 Layer binding，则保留旧绑定
```

新策略：

```text
Singleton / Instance:
    必须绑定到 Runtime

Scoped / Transient:
    有 ownerLayer 时绑定到 Layer
```

### 涉及文件

```text
LayerBase/DI/ServiceProvider.cs
```

---

### 修改 `ServiceProvider.Resolve`

替换当前绑定逻辑：

```csharp
if (desc.Lifetime == ServiceLifetime.Singleton || desc.Lifetime == ServiceLifetime.Instance)
{
    if (!ServiceLayerBinder.HasLayerBinding(instance))
    {
        ServiceLayerBinder.AttachRuntime(instance, _worldRoot.Runtime);
    }
}
else if (_ownerLayer != null)
{
    ServiceLayerBinder.AttachLayer(instance, _ownerLayer);
}
```

为：

```csharp
if (desc.Lifetime == ServiceLifetime.Singleton || desc.Lifetime == ServiceLifetime.Instance)
{
    var existingBinding = ServiceLayerBinder.GetBinding(instance);

    if (existingBinding != null &&
        existingBinding.RuntimeId != _worldRoot.Runtime.Id)
    {
        throw new InvalidOperationException(
            $"Singleton/Instance service {instance.GetType().Name} is already bound to another LayerRuntime.");
    }

    if (existingBinding == null || existingBinding.Layer != null)
    {
        // Singleton / Instance 是 Runtime 级服务。
        // 即使它之前被某个 Layer 绑定过，也要覆盖成 Runtime binding。
        ServiceLayerBinder.AttachRuntime(instance, _worldRoot.Runtime);
    }
}
else if (_ownerLayer != null)
{
    // Scoped / Transient 是 Layer 级服务。
    // 它们需要知道自己属于哪个 Layer，才能使用 Subscribe、Delay、OnEvent 等 Layer-only API。
    ServiceLayerBinder.AttachLayer(instance, _ownerLayer);
}
```

---

## P1-2：EventStore Dispose 清理 buffer

### 目标

避免 `PayloadStoreCache<T>.Stores` 静态泛型缓存中的 `EventStore<T>` 残留 payload 引用。

### 涉及文件

```text
LayerBase/Event/PostScheduler/EventPayloadStorage.cs
```

---

### 修改 `EventStore<T>`

增加 `_disposed` 字段：

```csharp
private bool _disposed;
```

修改 `Add`：

```csharp
public PayloadHandle Add(in T value)
{
    lock (_lock)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EventStore<T>));
        }

        if (_freeHead == -1)
        {
            Grow();
        }

        var index = _freeHead;
        _freeHead = _nextFree[index];

        _buffer[index] = value;
        var version = _versions[index];

        return new PayloadHandle(EventTypeId<T>.Id, index, version);
    }
}
```

修改 `Dispose`：

```csharp
public void Dispose()
{
    lock (_lock)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // 清空 payload buffer。
        // 如果 T 是包含引用字段的 struct，这一步可以释放旧 payload 持有的对象引用。
        Array.Clear(_buffer, 0, _buffer.Length);
        Array.Clear(_versions, 0, _versions.Length);
        Array.Clear(_nextFree, 0, _nextFree.Length);

        _buffer = Array.Empty<T>();
        _versions = Array.Empty<int>();
        _nextFree = Array.Empty<int>();

        _freeHead = -1;
        _capacity = 0;
    }
}
```

要求：

1. `TryGet` 遇到 `_disposed` 时返回 false。
2. `Release` 遇到 `_disposed` 时直接 return。
3. `GetRef` 遇到 `_disposed` 时抛 `ObjectDisposedException`。
4. `Dispatch` 遇到 `_disposed` 时直接 return。
5. `DispatchDefault` 遇到 `_disposed` 时直接 return。

---

## P1-3：DI 构造函数选择收紧

### 目标

避免 DI 默认调用 private / protected / internal 构造函数。

当前逻辑会扫描：

```text
BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
```

并按参数数量最多的构造函数优先。

新策略：

```text
如果存在 [Mount] 标记的构造函数：
    使用该构造函数
    允许 public / internal / protected / private
    多个 [Mount] 构造函数时报错

如果不存在 [Mount] 标记的构造函数：
    只扫描 public 构造函数
    选择参数最多的 public 构造函数
    如果多个 public 构造函数参数数量相同且同为最多，报错
```

`MountAttribute` 当前已经允许标记构造函数，因此不需要新增新 Attribute。

### 涉及文件

```text
LayerBase/DI/ServiceProvider.cs
LayerBase/DI/ServiceContracts.cs
```

---

### 修改 `ServiceProvider.CreateInstance`

把构造函数选择逻辑抽成 helper。

```csharp
private static ConstructorInfo SelectConstructor(Type implementationType)
{
    // implementationType：
    // 当前需要由 DI 创建的实现类型。
    // 例如 CombatService、DamageManager、SomeRepository。

    var allConstructors = implementationType.GetConstructors(
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic);

    var markedConstructors = allConstructors
        .Where(static ctor => ctor.GetCustomAttribute<MountAttribute>() != null)
        .ToArray();

    if (markedConstructors.Length > 1)
    {
        throw new InvalidOperationException(
            $"Multiple [Mount] constructors found for {implementationType}.");
    }

    if (markedConstructors.Length == 1)
    {
        return markedConstructors[0];
    }

    var publicConstructors = implementationType.GetConstructors(
        BindingFlags.Instance |
        BindingFlags.Public);

    if (publicConstructors.Length == 0)
    {
        throw new InvalidOperationException(
            $"No public constructor found for {implementationType}. Use [Mount] on a non-public constructor if it should be used by DI.");
    }

    var maxParameterCount = publicConstructors.Max(static ctor => ctor.GetParameters().Length);

    var candidates = publicConstructors
        .Where(ctor => ctor.GetParameters().Length == maxParameterCount)
        .ToArray();

    if (candidates.Length > 1)
    {
        throw new InvalidOperationException(
            $"Ambiguous public constructors found for {implementationType}. Use [Mount] to select the constructor explicitly.");
    }

    return candidates[0];
}
```

在 `CreateInstance` 中替换原来的：

```csharp
var ctor = desc.ImplType!
               .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
               .OrderByDescending(c => c.GetParameters().Length)
               .FirstOrDefault();

if (ctor == null)
    throw new InvalidOperationException($"No accessible constructor found for {desc.ImplType}");
```

为：

```csharp
var ctor = SelectConstructor(desc.ImplType!);
```

保留后续参数解析逻辑。

---

## 测试要求

### P0-1 DelayPublisher 生命周期

新增或补充测试：

```text
1. Layer 创建 DelayPublisher 后 Dispose Runtime。
2. 使用 WeakReference 包住 Layer。
3. 强制 GC。
4. 验证 Layer 可以被回收。

1. Layer 创建 DelayPublisher 后重新 Build。
2. 旧 publisher 应该被 Deactivate。
3. DelayPublisherManager 不应继续持有旧 publisher。
4. 旧 timer 到期后不应访问旧 publisher。
```

---

### P0-2 Binding Detach

新增或补充测试：

```text
1. 创建 Runtime、Layer、Service。
2. 获取 Service 实例。
3. Dispose Runtime。
4. 使用 WeakReference 包住 Runtime / Layer。
5. 清理强引用后强制 GC。
6. 验证 Runtime / Layer 可以被回收。

1. Dispose 后调用旧 service.Send/Post。
2. 应抛出明确的未绑定异常，或者返回已释放错误。
3. 不允许继续发送到旧 EventCenter。
```

---

### P0-4 Singleton / Instance Runtime binding

新增或补充测试：

```text
1. 注册 Singleton。
2. 从 Layer provider 中解析。
3. 验证 ServiceLayerBinder.GetBinding(instance).Layer == null。
4. 验证 RuntimeId 等于当前 Runtime.Id。

1. 同一个 singleton 实例先被错误 Layer binding。
2. 再作为 Singleton 解析。
3. 应覆盖为 Runtime binding，或在跨 Runtime 时抛异常。
```

---

### P1-2 EventStore Dispose

新增或补充测试：

```text
1. Store 一个包含引用字段的 struct。
2. Release 或 Dispose。
3. Dispose 后 buffer 不应继续持有该引用。

1. EventStore Dispose 后调用 Add。
2. 应抛 ObjectDisposedException。

1. EventStore Dispose 后 Dispatch / Release。
2. 应安全 return，不应抛出非预期异常。
```

---

### P1-3 DI 构造函数选择

新增或补充测试：

```text
1. 类型只有 public 无参构造函数。
2. DI 正常创建。

1. 类型有 public 无参构造函数和 private 多参构造函数。
2. DI 应选择 public 构造函数，不应选择 private 构造函数。

1. 类型有一个 private [Mount] 构造函数。
2. DI 应选择该构造函数。

1. 类型有多个 [Mount] 构造函数。
2. DI 应抛明确异常。

1. 类型有多个 public 构造函数，且最大参数数量相同。
2. DI 应抛明确异常，提示使用 [Mount]。
```

---

## 验收标准

```text
1. Layer.Dispose 后，DelayPublisherManager 不再持有旧 Layer 的 publisher。
2. Layer.PrepareBuild 后，旧 DelayPublisher 不再活跃。
3. Runtime.Dispose 后，service/context 自身绑定槽位被清空。
4. Singleton / Instance 解析结果必须是 Runtime binding。
5. 跨 Runtime 复用同一个 Singleton / Instance 实例时必须抛异常。
6. EventStore<T>.Dispose 会清空 payload buffer。
7. DI 默认不再调用 non-public 构造函数。
8. [Mount] 可以显式选择 non-public 构造函数。
9. 不修改 P0/P1 范围外的逻辑。
```
