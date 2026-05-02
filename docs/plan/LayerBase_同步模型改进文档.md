# LayerBase 同步运行模型下的改进文档

## 前提

本改进文档基于以下运行约束：

1. 框架内部运行是纯同步行为。
2. 注册阶段同步完成。
3. 运行中不考虑事件合并、派发表重建、DelayPublisher 注册、Timer 调度之间的并发竞争。
4. 因此本文不处理运行期并发读写派发表、`EventPayloadStorage.GetRef()` 并发合并、`SubscribeDelay<T>()` 并发注册、`TimeScheduler` 多线程访问等问题。

本文只保留在上述同步模型下仍然成立、且值得动工修改的部分。

---

## 1. Runtime Dispose 时按 RuntimeId 清理静态缓存

### 问题

当前 `LayerRuntime.Dispose()` 会释放当前 Runtime 的 Layer、Scheduler、Timer、DelayManager、WorldServiceRoot，但没有按 `RuntimeId` 清理泛型静态缓存。

需要重点清理的静态缓存包括：

- `PayloadStoreCache<T>.Stores[runtimeId]`
- `LayerTargetCache<TLayer>.Layers[runtimeId]`
- `LayerCallCache<TLayer, TRequest, TResponse>.Invokers[runtimeId]`
- `LayerCallCache<TLayer, TRequest, TResponse>.Errors[runtimeId]`

其中：

- **Runtime**：一次独立的 LayerBase 世界实例。
- **RuntimeId**：Runtime 的整数编号，用于在静态缓存数组中定位槽位。
- **静态泛型缓存**：`static class Cache<T>` 这类每个泛型类型独立持有的静态数据。
- **槽位**：数组中由 `runtimeId` 指向的位置，例如 `Stores[runtimeId]`。
- **PayloadStore**：Post 系统存放事件负载的缓冲区。

如果只 Dispose 单个 Runtime，而不清对应槽位，旧 Runtime 的 Layer、Invoker、EventStore 可能继续被静态缓存引用。

### 修改目标

新增 Runtime 级缓存清理机制。

全局 `LayerHub.Reset()` 仍然保留，用于清理全部缓存。

单个 `LayerRuntime.Dispose()` 需要清理当前 RuntimeId 对应的缓存槽位。

### 修改点 1：LayerHub 增加 Runtime 级 resetter

```csharp
public static class LayerHub
{
    // 保存“清理全部缓存”的回调。
    // 参数：无。
    // 作用：用于 LayerHub.Reset() 时清理所有静态缓存。
    private static readonly ConcurrentBag<Action> s_cacheResetters = new();

    // 保存“清理指定 Runtime 缓存槽位”的回调。
    // 参数 int runtimeId：要清理的 LayerRuntime.Id。
    // 作用：用于单个 LayerRuntime.Dispose() 时释放该 Runtime 对应的静态缓存引用。
    private static readonly ConcurrentBag<Action<int>> s_runtimeCacheResetters = new();

    // 注册全局缓存清理回调。
    // 参数 resetter：清理全部缓存的方法。
    internal static void RegisterCacheResetter(Action resetter)
    {
        s_cacheResetters.Add(resetter);
    }

    // 注册 Runtime 级缓存清理回调。
    // 参数 resetter：清理指定 RuntimeId 槽位的方法。
    // 逻辑说明：每个泛型静态缓存类型在静态构造函数中调用一次即可。
    internal static void RegisterRuntimeCacheResetter(Action<int> resetter)
    {
        s_runtimeCacheResetters.Add(resetter);
    }

    // 清理某个 RuntimeId 对应的所有静态缓存槽位。
    // 参数 runtimeId：当前要释放的 LayerRuntime.Id。
    // 逻辑说明：只清当前 Runtime 的槽位，不影响其他仍在运行的 Runtime。
    internal static void ClearRuntimeCaches(int runtimeId)
    {
        foreach (var resetter in s_runtimeCacheResetters)
        {
            resetter(runtimeId);
        }
    }
}
```

### 修改点 2：LayerRuntime.Dispose 调用 Runtime 级清理

```csharp
public void Dispose()
{
    if (_disposed)
    {
        return;
    }

    _disposed = true;

    _chain?.DisposeLayers();
    _chain = null;

    Services.Dispose();

    _scheduler?.Dispose();
    _timer?.Dispose();

    DelayManager?.Clear();
    DelayManager = null;

    EventCenter.Reset();

    _context?.Dispose();

    // 参数 _id：当前 Runtime 的唯一编号。
    // 作用：清理 PayloadStore、LayerTargetCache、LayerCallCache 等静态泛型缓存中属于当前 Runtime 的槽位。
    // 逻辑说明：这一步必须在 Internal_Unregister 前后都可以，但建议放在对象释放流程末尾，避免释放中途还有内部逻辑访问缓存。
    LayerHub.ClearRuntimeCaches(_id);

    LayerHub.Internal_Unregister(this);
}
```

### 修改点 3：PayloadStoreCache<T> 支持按 RuntimeId 清理

```csharp
internal static class PayloadStoreCache<T> where T : struct
{
    // 每个事件类型 T 一份 Stores。
    // 参数 runtimeId 对应数组下标。
    // 作用：让同一 Runtime 内的 EventPayloadStorage 可以复用同一个 EventStore<T>。
    public static readonly EventStore<T>?[] Stores = new EventStore<T>[1024];

    static PayloadStoreCache()
    {
        // 注册全局清理。
        // 作用：LayerHub.Reset() 时清空全部 Runtime 槽位。
        LayerHub.RegisterCacheResetter(Reset);

        // 注册单 Runtime 清理。
        // 作用：LayerRuntime.Dispose() 时只清理当前 RuntimeId 的槽位。
        LayerHub.RegisterRuntimeCacheResetter(ResetRuntime);
    }

    private static void Reset()
    {
        for (int i = 0; i < Stores.Length; i++)
        {
            Stores[i]?.Dispose();
            Stores[i] = null;
        }
    }

    // 参数 runtimeId：要释放的 Runtime 槽位。
    // 逻辑说明：如果 runtimeId 越界，说明该 Runtime 没有对应静态槽位，直接返回。
    private static void ResetRuntime(int runtimeId)
    {
        if ((uint)runtimeId >= (uint)Stores.Length)
        {
            return;
        }

        Stores[runtimeId]?.Dispose();
        Stores[runtimeId] = null;
    }
}
```

### 修改点 4：LayerTargetCache<TLayer> 支持按 RuntimeId 清理

```csharp
private static class LayerTargetCache<TLayer> where TLayer : Layer
{
    // Versions：缓存版本。
    // Layers：缓存到的目标 Layer 实例。
    // States：缓存状态，表示 Found / Missing / Ambiguous。
    public static readonly int[] Versions = new int[256];
    public static readonly TLayer?[] Layers = new TLayer[256];
    public static readonly LayerTargetState[] States = new LayerTargetState[256];

    static LayerTargetCache()
    {
        for (int i = 0; i < Versions.Length; i++)
        {
            Versions[i] = -1;
        }

        LayerHub.RegisterCacheResetter(Reset);
        LayerHub.RegisterRuntimeCacheResetter(ResetRuntime);
    }

    private static void Reset()
    {
        for (int i = 0; i < Versions.Length; i++)
        {
            Layers[i] = null;
            States[i] = LayerTargetState.Unknown;
            Volatile.Write(ref Versions[i], -1);
        }
    }

    // 参数 runtimeId：要清除的 Runtime 槽位。
    // 作用：避免静态缓存继续强引用已释放 Runtime 的 Layer。
    private static void ResetRuntime(int runtimeId)
    {
        if ((uint)runtimeId >= (uint)Versions.Length)
        {
            return;
        }

        Layers[runtimeId] = null;
        States[runtimeId] = LayerTargetState.Unknown;
        Volatile.Write(ref Versions[runtimeId], -1);
    }
}
```

### 修改点 5：LayerCallCache<TLayer, TRequest, TResponse> 支持按 RuntimeId 清理

```csharp
private static class LayerCallCache<TLayer, TRequest, TResponse>
    where TLayer : Layer
    where TRequest : struct
    where TResponse : struct
{
    // Versions：缓存版本。
    // Invokers：缓存的调用委托。
    // Errors：缓存的调用路由错误。
    public static readonly int[] Versions = new int[256];
    public static readonly LayerCallInvoker<TRequest, TResponse>?[] Invokers =
        new LayerCallInvoker<TRequest, TResponse>[256];
    public static readonly Exception?[] Errors = new Exception?[256];

    static LayerCallCache()
    {
        for (int i = 0; i < Versions.Length; i++)
        {
            Versions[i] = -1;
        }

        LayerHub.RegisterCacheResetter(Reset);
        LayerHub.RegisterRuntimeCacheResetter(ResetRuntime);
    }

    private static void Reset()
    {
        for (int i = 0; i < Versions.Length; i++)
        {
            Invokers[i] = null;
            Errors[i] = null;
            Volatile.Write(ref Versions[i], -1);
        }
    }

    // 参数 runtimeId：要清理的 Runtime 槽位。
    // 作用：避免静态缓存继续强引用旧 Layer 的 handler 委托。
    private static void ResetRuntime(int runtimeId)
    {
        if ((uint)runtimeId >= (uint)Versions.Length)
        {
            return;
        }

        Invokers[runtimeId] = null;
        Errors[runtimeId] = null;
        Volatile.Write(ref Versions[runtimeId], -1);
    }
}
```

---

## 2. 明确 RuntimeId 策略

### 问题

当前 `LayerHub.CreateLayers()` 通过 `s_runtimeIdCounter++` 分配 RuntimeId，并且限制最大 256。

如果只增加 Runtime 级缓存清理，但不复用 RuntimeId，那么进程生命周期中仍然最多创建 256 个 Runtime。

### 修改目标

二选一：

1. 文档明确：进程生命周期最多创建 256 个 Runtime。
2. 实现 RuntimeId 复用。

建议实现 RuntimeId 复用。

### 修改方案

在 `LayerHub` 中维护空闲 id 栈。

```csharp
public static class LayerHub
{
    // 保存已释放、可复用的 RuntimeId。
    // 作用：避免 Dispose 后 RuntimeId 永远增长。
    private static readonly Stack<int> s_freeRuntimeIds = new();

    // 下一个尚未使用过的新 RuntimeId。
    // 作用：当没有可复用 id 时分配新 id。
    private static int s_runtimeIdCounter;

    public static LayerRuntime.LayersBuilder CreateLayers()
    {
        lock (s_lock)
        {
            int id;

            // 优先复用已释放的 RuntimeId。
            // 逻辑说明：Dispose 时已经清理过该 id 的静态缓存槽位，所以这里可以安全复用。
            if (s_freeRuntimeIds.Count > 0)
            {
                id = s_freeRuntimeIds.Pop();
            }
            else
            {
                id = s_runtimeIdCounter++;

                if (id >= 256)
                {
                    throw new InvalidOperationException(
                        "Max 256 concurrent LayerRuntimes supported by static caches.");
                }
            }

            var runtime = new LayerRuntime(id);

            if (s_primaryRuntime == null)
            {
                s_primaryRuntime = runtime;
            }

            return new LayerRuntime.LayersBuilder(runtime);
        }
    }

    internal static void Internal_Unregister(LayerRuntime runtime)
    {
        lock (s_lock)
        {
            if (ReferenceEquals(s_primaryRuntime, runtime))
            {
                s_primaryRuntime = null;
            }

            for (var i = s_runtimes.Count - 1; i >= 0; i--)
            {
                if (s_runtimes[i].TryGetTarget(out var r) && ReferenceEquals(r, runtime))
                {
                    s_runtimes.RemoveAt(i);
                    break;
                }
            }

            // 参数 runtime.Id：已释放 Runtime 的编号。
            // 作用：让后续 CreateLayers() 可以复用这个编号。
            s_freeRuntimeIds.Push(runtime.Id);
        }
    }

    public static void Reset()
    {
        lock (s_lock)
        {
            // Reset 是全局重置。
            // 逻辑说明：全部 Runtime 都释放后，RuntimeId 分配器回到初始状态。
            s_freeRuntimeIds.Clear();
            s_runtimeIdCounter = 0;

            // 其余原有 Reset 逻辑保持。
        }
    }
}
```

---

## 3. 修正 Singleton 的 Layer 归属语义

### 问题

`Singleton` 是 Runtime 级唯一实例。

但当前 Singleton 每次被某个 Layer 解析时，都会重新 `ServiceLayerBinder.Attach(instance, ownerLayer)`。

这会导致同一个 Singleton 的 Layer 归属被最后一次解析覆盖。

### 修改目标

Singleton 不再绑定到普通 Layer，避免其 `Post / Send / Delay / GetService` 等扩展方法受解析顺序影响。

### 推荐方案

将服务分为两类绑定：

1. Layer 级绑定：Scoped、Transient、手动注册服务。
2. Runtime 级绑定：Singleton。

其中：

- Layer 级服务可以使用 `Delay`、`GetService` 等依赖 Layer 的 API。
- Runtime 级 Singleton 可以使用 `Post`、`Send`、`SchedulePost` 等依赖 Runtime 的 API。
- Runtime 级 Singleton 不应使用 `Delay`，因为 DelayPublisher 是 Layer 级缓存。

### 修改点 1：ServiceLayerBinding 支持 Layer 可空

```csharp
internal sealed class ServiceLayerBinding
{
    // RuntimeId：当前对象所属 Runtime 的编号。
    public readonly int RuntimeId;

    // LayerIndex：当前对象所属 Layer 的索引。
    // 对 Runtime 级 Singleton，值为 -1。
    public readonly int LayerIndex;

    // Layer：当前对象所属 Layer。
    // 对 Runtime 级 Singleton，值为 null。
    public readonly Layer? Layer;

    // Runtime：当前对象所属 Runtime。
    public readonly LayerRuntime Runtime;

    // EventCenter：当前 Runtime 的事件中心。
    public readonly EventCenter EventCenter;

    public ServiceLayerBinding(
        int runtimeId,
        int layerIndex,
        Layer? layer,
        LayerRuntime runtime)
    {
        RuntimeId = runtimeId;
        LayerIndex = layerIndex;
        Layer = layer;
        Runtime = runtime;
        EventCenter = runtime.EventCenter;
    }
}
```

### 修改点 2：ServiceLayerBinder 增加 AttachRuntime

```csharp
internal static class ServiceLayerBinder
{
    private static ConditionalWeakTable<object, ServiceLayerBinding> s_bindingMap = new();

    // 参数 service：要绑定的 Singleton 服务实例。
    // 参数 runtime：该 Singleton 所属的 LayerRuntime。
    // 作用：将 Singleton 绑定到 Runtime，而不是具体 Layer。
    public static void AttachRuntime(object service, LayerRuntime runtime)
    {
        if (service == null)
        {
            return;
        }

        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        var binding = new ServiceLayerBinding(
            runtimeId: runtime.Id,
            layerIndex: -1,
            layer: null,
            runtime: runtime);

        s_bindingMap.Remove(service);
        s_bindingMap.Add(service, binding);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = -1;
        }
    }

    // 参数 service：要绑定的 Layer 级服务。
    // 参数 layer：服务所属 Layer。
    // 作用：保持 Scoped / Transient / 手动服务的原有绑定行为。
    public static void AttachLayer(object service, Layer layer)
    {
        if (service == null || layer == null)
        {
            return;
        }

        var runtime = layer.OwnerContext;

        if (runtime == null)
        {
            throw new InvalidOperationException("Layer is not attached to LayerRuntime.");
        }

        var binding = new ServiceLayerBinding(
            runtimeId: runtime.Id,
            layerIndex: layer.RouteIndex,
            layer: layer,
            runtime: runtime);

        s_bindingMap.Remove(service);
        s_bindingMap.Add(service, binding);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = layer.RouteIndex;
        }
    }
}
```

### 修改点 3：ServiceProvider.Resolve 按生命周期绑定

```csharp
private object Resolve(ServiceDescriptor desc, HashSet<Type> callstack)
{
    var instance = desc.Lifetime switch
    {
        ServiceLifetime.Instance => desc.Instance!,
        ServiceLifetime.Singleton => _worldRoot.GetOrCreate(
            desc,
            _ownerLayer,
            () => CreateInstance(desc, callstack)),
        ServiceLifetime.Scoped => GetOrCreateCached(desc, callstack),
        ServiceLifetime.Transient => CreateInstance(desc, callstack),
        _ => throw new NotSupportedException($"Unsupported lifetime {desc.Lifetime}")
    };

    // Singleton 是 Runtime 级对象。
    // 作用：避免 Singleton 被不同 Layer 重复解析时改绑。
    if (desc.Lifetime == ServiceLifetime.Singleton)
    {
        ServiceLayerBinder.AttachRuntime(instance, _worldRoot.Runtime);
    }
    else if (_ownerLayer != null)
    {
        ServiceLayerBinder.AttachLayer(instance, _ownerLayer);
    }

    return instance;
}
```

> 需要配套让 `WorldServiceRoot` 暴露 `Runtime` 只读属性。

```csharp
internal sealed class WorldServiceRoot : IDisposable
{
    private readonly LayerRuntime _runtime;

    // 作用：让 ServiceProvider 可以将 Singleton 绑定到 Runtime。
    public LayerRuntime Runtime => _runtime;
}
```

### 修改点 4：依赖 Layer 的扩展方法需要显式检查

```csharp
private static Layer RequireLayer(ServiceLayerBinding binding)
{
    // 作用：阻止 Runtime 级 Singleton 调用 Delay / GetService 等依赖具体 Layer 的 API。
    if (binding.Layer == null)
    {
        throw new InvalidOperationException(
            "This service is bound to Runtime, not to a specific Layer. Layer-only API is unavailable.");
    }

    return binding.Layer;
}
```

用于：

```csharp
public static void Delay<TValue>(
    this IService service,
    in TValue value,
    float ttl,
    int contractId = 0)
    where TValue : struct
{
    var binding = service.GetBinding();

    // DelayPublisher 是 Layer 级对象，因此这里必须要求存在 Layer。
    RequireLayer(binding)
        .SubscribeDelay<TValue>()
        .Publish(value, ttl, contractId);
}
```

---

## 4. 禁止重复 Singleton 注册静默覆盖

### 问题

`WorldServiceRoot.Register()` 当前直接覆盖 `_descriptors[descriptor.ServiceType]`。

如果多个 Layer 注册同一个 Singleton 服务类型，后注册的描述符会覆盖先注册的描述符。

这会导致描述符和已创建实例不一致。

### 修改目标

同一 Runtime 内，同一个 Singleton 服务类型不允许被不同实现静默覆盖。

### 修改方案

```csharp
public void Register(ServiceDescriptor descriptor)
{
    if (descriptor == null)
    {
        throw new ArgumentNullException(nameof(descriptor));
    }

    if (_descriptors.TryGetValue(descriptor.ServiceType, out var existing))
    {
        // 判断是否是完全相同的重复注册。
        // ServiceType：服务接口类型。
        // ImplType：实现类型。
        // Factory：工厂方法。
        // Instance：外部传入的实例。
        var sameRegistration =
            existing.ImplType == descriptor.ImplType &&
            existing.Factory == descriptor.Factory &&
            ReferenceEquals(existing.Instance, descriptor.Instance);

        if (!sameRegistration)
        {
            throw new InvalidOperationException(
                $"Duplicate singleton registration: {descriptor.ServiceType}");
        }

        return;
    }

    _descriptors[descriptor.ServiceType] = descriptor;
}
```

---

## 5. 完善 DI 循环依赖检测

### 问题

当前循环依赖检测依赖 `HashSet<Type> callstack`，主要覆盖构造函数注入。

但以下路径可能检测不完整：

- Factory 内部解析服务。
- `[Mount]` 字段注入。
- `[Mount]` 属性注入。
- Scoped Lazy 重新创建了新的 `HashSet<Type>`。

### 修改目标

一次服务解析过程使用同一个 `ResolutionContext`。

### 新增类型

```csharp
// ResolutionContext：一次服务解析过程的上下文。
// CallStack：记录当前正在构造的实现类型。
// 作用：检测 A -> B -> A 这种循环依赖。
private sealed class ResolutionContext
{
    public readonly HashSet<Type> CallStack = new();
}
```

### 修改入口

```csharp
public object? GetService(Type serviceType)
{
    // 每次从外部进入 GetService，都创建一个新的解析上下文。
    // 这个上下文会在构造函数、Factory、Mount 注入之间传递。
    return GetServiceInternal(serviceType, new ResolutionContext());
}
```

### 修改内部解析

```csharp
private object? GetServiceInternal(Type serviceType, ResolutionContext context)
{
    if (IsDisposed)
    {
        throw new ObjectDisposedException(nameof(ServiceProvider));
    }

    if (serviceType == null)
    {
        throw new ArgumentNullException(nameof(serviceType));
    }

    if (_map.TryGetValue(serviceType, out var desc))
    {
        return Resolve(desc, context);
    }

    if (_worldRoot.TryGetDescriptor(serviceType, out var parentDesc))
    {
        return _worldRoot.GetOrCreate(
            parentDesc!,
            _ownerLayer,
            () => CreateInstance(parentDesc!, context));
    }

    return null;
}
```

### 修改 CreateInstance

```csharp
private object CreateInstance(ServiceDescriptor desc, ResolutionContext context)
{
    if (desc.ImplType == null && desc.Factory == null)
    {
        throw new InvalidOperationException($"No implementation for {desc.ServiceType}");
    }

    var implementationType = desc.ImplType ?? desc.ServiceType;

    // 作用：如果当前实现类型已经在构造栈里，说明出现循环依赖。
    if (!context.CallStack.Add(implementationType))
    {
        throw new InvalidOperationException(
            $"Circular dependency detected: {implementationType}");
    }

    try
    {
        if (desc.Factory != null)
        {
            // Factory 内部如果继续 GetService，会复用当前 context。
            return desc.Factory(this);
        }

        var ctor = desc.ImplType!
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (ctor == null)
        {
            throw new InvalidOperationException(
                $"No accessible constructor found for {desc.ImplType}");
        }

        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            // 参数 parameters[i].ParameterType：构造函数参数类型。
            // 作用：递归解析构造函数依赖。
            var dep = GetServiceInternal(parameters[i].ParameterType, context);

            if (dep == null)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve dependency {parameters[i].ParameterType} for {desc.ImplType}");
            }

            args[i] = dep;
        }

        var instance = ctor.Invoke(args);

        // 作用：字段和属性注入也复用当前 context。
        InjectMembers(instance, context);

        return instance;
    }
    finally
    {
        context.CallStack.Remove(implementationType);
    }
}
```

### 修改 InjectMembers

```csharp
private void InjectMembers(object instance, ResolutionContext context)
{
    var t = instance.GetType();

    foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    {
        if (f.GetCustomAttribute<MountAttribute>() == null)
        {
            continue;
        }

        // 参数 f.FieldType：字段类型。
        // 作用：为带 [Mount] 的字段解析依赖。
        var dep = GetServiceInternal(f.FieldType, context);

        if (dep != null)
        {
            f.SetValue(instance, dep);
        }
    }

    foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    {
        if (p.GetCustomAttribute<MountAttribute>() == null || !p.CanWrite)
        {
            continue;
        }

        // 参数 p.PropertyType：属性类型。
        // 作用：为带 [Mount] 的属性解析依赖。
        var dep = GetServiceInternal(p.PropertyType, context);

        if (dep != null)
        {
            p.SetValue(instance, dep, null);
        }
    }
}
```

> 注意：如果要让 Factory 内部也复用同一个 `ResolutionContext`，需要改造 `IServiceProvider` 内部实现。否则 `desc.Factory(this)` 里再次调用公开 `GetService` 时仍然会新建 context。最小可行方案是先把 Factory 自解析作为不支持场景，后续再增加内部 provider scope。

---

## 6. 明确 Build 后事件类型策略

### 问题

`EventTypeId<T>.Id` 是首次访问时分配。

`PostScheduler.BuildPlans()` 根据当时的最大事件 ID 创建策略数组、bitmap、dirty/latest buffer。

如果 Build 后才首次出现某个事件类型，部分 Post 模式可能无法完整工作。

### 修改目标

建议采用冻结策略：

> Build 后不允许首次出现新的 Post 事件类型。所有可 Post 的事件类型必须在 Build 阶段通过源生成器、元数据注册或显式 Prewarm 进入系统。

### 修改方案

在 `PostScheduler` 中保存 Build 完成时的最大 EventTypeId。

```csharp
public sealed class PostScheduler : IDisposable
{
    // BuildPlans 完成时允许的最大事件类型 ID。
    // 作用：检测 Build 后才首次出现的新事件类型。
    private int _sealedMaxEventTypeId;

    public void BuildPlans(ReadOnlySpan<PostTypePlan> plans)
    {
        var maxTypeId = EventTypeIdAllocator.MaxId;

        foreach (var p in plans)
        {
            if (p.EventTypeId > maxTypeId)
            {
                maxTypeId = p.EventTypeId;
            }
        }

        _sealedMaxEventTypeId = maxTypeId;

        // 原有 BuildPlans 逻辑保持。
    }

    // 参数 typeId：当前事件类型 ID。
    // 作用：阻止 Build 后才首次出现的新事件类型进入不完整的 Post 策略表。
    private bool IsKnownEventType(int typeId)
    {
        return typeId <= _sealedMaxEventTypeId;
    }
}
```

在特殊模式入口加检查：

```csharp
private PostResult TryPostSpecial<T>(int typeId, in T value) where T : struct
{
    if (!IsKnownEventType(typeId))
    {
        return PostResult.Failure(
            $"Event type {typeof(T).Name} was not registered before Build.");
    }

    if (_postBitmap.IsDirty(typeId))
    {
        return MarkDirtyById<T>(typeId);
    }

    if (_postBitmap.IsLatest(typeId))
    {
        return EnqueueLatestInternal(typeId, in value);
    }

    if (_postBitmap.IsCoalesced(typeId))
    {
        return EnqueueCoalescedInternal(typeId, in value);
    }

    ref readonly var plan = ref GetPlan(typeId);
    return EnqueueNormalWithPlan(typeId, in value, in plan);
}
```

`MarkDirtyById` 也要避免假成功：

```csharp
private PostResult MarkDirtyById<T>(int typeId) where T : struct
{
    if (!IsKnownEventType(typeId))
    {
        return PostResult.Failure(
            $"Event type {typeof(T).Name} was not registered before Build.");
    }

    var segment = typeId >> 6;
    var bit = 1UL << (typeId & 63);

    lock (_bufferLock)
    {
        if (segment >= _dirtyPendingBits.Length)
        {
            return PostResult.Failure(
                $"Dirty buffer is not initialized for event type {typeof(T).Name}.");
        }

        if ((FastArray.At(_dirtyPendingBits, segment) & bit) == 0)
        {
            FastArray.At(_dirtyPendingBits, segment) |= bit;

            // 作用：确保当前事件类型有 payload store。
            // DirtySignal 派发 default(T)，但仍然需要通过 typeId 找到对应 store。
            _payloadStorage.EnsureStore<T>(_runtimeId);
        }
    }

    return PostResult.Coalesced();
}
```

---

## 7. 补 TryPost 风格 API，避免背压失败被吞掉

### 问题

`PostScheduler.TryPost()` 会返回 `PostResult`。

但 `LayerRuntime.Post<T>()`、`Layer.Post<T>()` 是 `void`，会吞掉失败结果。

在队列满、Scheduler 已释放、事件类型未注册等情况下，调用方无法知道事件没有进入队列。

### 修改目标

保留原有 `Post<T>()` 便捷 API，同时新增 `TryPost<T>()`。

### 修改点 1：LayerRuntime 增加 TryPost

```csharp
public PostResult TryPost<T>(in T value, EventPostPolicy? policy = default)
    where T : struct
{
    // 参数 value：要投递的事件值。
    // 参数 policy：本次投递使用的策略；default 表示使用事件元数据或默认策略。
    // 返回 PostResult：表示投递是否成功，以及失败原因。
    return policy.HasValue
        ? Scheduler.TryPost(value, policy.Value)
        : Scheduler.TryPost(value);
}

public void Post<T>(in T value) where T : struct
{
    // 保留旧 API。
    // 逻辑说明：旧 API 仍然忽略结果，以保持兼容。
    _ = TryPost(value);
}
```

### 修改点 2：Layer 增加 TryPost

```csharp
public PostResult TryPost<T>(in T value, EventPostPolicy? policy = default)
    where T : struct
{
    if (OwnerContext == null)
    {
        return PostResult.Failure("Layer not attached to context.");
    }

    return OwnerContext.TryPost(value, policy);
}

public void Post<T>(in T value) where T : struct
{
    _ = TryPost(value);
}
```

### 修改点 3：LayerHub 增加 TryPost

```csharp
public static PostResult TryPost<T>(in T value, EventPostPolicy? policy = default)
    where T : struct
{
    if (s_primaryRuntime == null)
    {
        return PostResult.Failure("No Primary LayerRuntime created.");
    }

    return s_primaryRuntime.TryPost(value, policy);
}

public static void Post<T>(in T value) where T : struct
{
    _ = TryPost(value);
}
```

---

## 8. 清理 Layer.Dispose 的 pending ops

### 问题

Layer 在未 Build 前调用 Subscribe，会把操作放进 `m_pendingOps`。

如果该 Layer 后续没有 Build，而是直接 Dispose，`m_pendingOps` 中的 delegate 会继续持有 handler 引用。

### 修改方案

```csharp
public void Dispose()
{
    if (Interlocked.Exchange(ref m_disposed, 1) != 0)
    {
        return;
    }

    lock (m_subscriptions)
    {
        foreach (var sub in m_subscriptions)
        {
            sub.Dispose();
        }

        m_subscriptions.Clear();
    }

    // 参数：无。
    // 作用：清理 Build 前暂存的订阅操作，释放 handler delegate 引用。
    // 逻辑说明：Dispose 后这些 pending op 不可能再执行。
    while (m_pendingOps.TryDequeue(out _))
    {
    }

    m_delayPublishers.Clear();
    m_serviceUpdates.Clear();
    m_activeServices.Clear();
    m_resolvedServices.Clear();

    m_serviceProvider?.Dispose();
    m_serviceProvider = null;
}
```

---

## 9. 建议新增测试

### 测试 1：单 Runtime Dispose 后 PayloadStore 释放

目标：

1. 创建 Runtime。
2. Post 一个事件，触发 `PayloadStoreCache<T>.Stores[runtimeId]` 创建。
3. Dispose Runtime。
4. 确认对应槽位被清空。

### 测试 2：RuntimeId 可复用

目标：

1. 创建 Runtime A，记录 id。
2. Dispose Runtime A。
3. 创建 Runtime B。
4. 确认 Runtime B 可以复用 Runtime A 的 id。
5. 确认缓存没有污染。

### 测试 3：Singleton 重复注册冲突

目标：

1. LayerA 注册 `AddSingleton<IServiceX, ImplA>()`。
2. LayerB 注册 `AddSingleton<IServiceX, ImplB>()`。
3. Build 时抛出重复注册异常。

### 测试 4：Singleton 不被不同 Layer 改绑

目标：

1. 同一个 Singleton 被多个 Layer 解析。
2. 确认绑定仍是 Runtime 级。
3. 调用 Layer-only API 时抛出清晰异常。

### 测试 5：Build 后未知事件类型策略

目标：

1. Build 前不预热某事件。
2. Build 后调用 `MarkDirty<NewEvent>()`。
3. 确认返回失败，而不是假成功。

### 测试 6：Dispose 清理 pending ops

目标：

1. Layer 未 Build 前调用 Subscribe。
2. Dispose Layer。
3. 确认 pending ops 被清空。

---

## 推荐实施顺序

1. Runtime 级静态缓存清理。
2. RuntimeId 复用。
3. Singleton Runtime 级绑定。
4. Singleton 重复注册冲突检测。
5. DI 统一 ResolutionContext。
6. Build 后事件类型冻结检查。
7. TryPost API。
8. Layer.Dispose 清 pending ops。
