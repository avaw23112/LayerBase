# LayerBase DI 容器多世界化改造设计文档

## 1. 改造目标

本次改造只处理 **DI 容器从单世界变为多世界** 的问题，不改动事件发送热路径，不调整 `ServiceLayerBinder`，不删除扩展方法，不处理 `DelayPublisherManager` 世界化。

当前 DI 的核心问题是：`ServiceProvider` 内部存在静态根容器，导致 `Singleton` 服务描述符和实例可能在多个 `LayerRuntime` 之间共享。对于多世界架构来说，这是错误的。

改造后的目标是：

```text
LayerRuntime = 一个世界
每个 LayerRuntime 拥有自己的 DI 根容器
Singleton = 当前 LayerRuntime 内唯一
Scoped = 当前 Layer 内唯一
Transient = 每次解析创建
Instance = 用户显式传入的外部实例
```

也就是说，本次改造要把 DI 的根作用域从：

```text
Process Global
```

改为：

```text
LayerRuntime Local
```

## 2. 核心原则

### 2.1 不再存在全局 static root

当前类似下面这种结构需要移除：

```csharp
private static ServiceProvider _root = new();
```

以及相关的：

```csharp
ServiceProvider.ResetRoot();
```

多世界下，DI 根容器不能是进程全局静态对象。否则两个不同 `LayerRuntime` 的服务注册和服务实例会互相污染。

### 2.2 Singleton 改为 Runtime Singleton

改造后，`Singleton` 的语义不是“进程内全局唯一”，而是：

```text
在同一个 LayerRuntime 内唯一
不同 LayerRuntime 之间互相独立
```

例如：

```csharp
services.AddSingleton<IInventoryService, InventoryService>();
```

在 `WorldA` 和 `WorldB` 中会分别创建一个 `InventoryService`，两者不共享实例。

### 2.3 Scoped 仍然属于 Layer

`Scoped` 的语义保持不变：

```text
同一个 Layer 内唯一
不同 Layer 之间独立
不同 Runtime 之间也独立
```

### 2.4 Transient 保持每次创建

`Transient` 的语义保持不变：

```text
每次 Resolve 都创建新实例
```

### 2.5 Instance 是用户显式共享

`Instance` 是用户主动传入的对象：

```csharp
services.AddSingleton<IMyService>(existingInstance);
```

这种方式仍然允许共享外部对象。也就是说，如果用户把同一个 `existingInstance` 注册进多个 Runtime，它们就会共享这个实例。

因此文档和 API 注释中应明确：

```text
AddSingleton(instance) 是外部实例注册。
如果需要多世界隔离，应优先使用 AddSingleton<TService, TImpl>() 或 factory 注册。
```

## 3. 新增 WorldServiceRoot

### 3.1 定位

新增 `WorldServiceRoot`，作为单个 `LayerRuntime` 的 DI 根容器。

它只负责当前 Runtime 内的 Singleton 服务：

```text
WorldServiceRoot
├── Singleton ServiceDescriptor 表
└── Singleton 实例缓存
```

它不负责：

```text
Scoped 实例
Transient 实例
Layer 内局部服务缓存
事件发送热路径
ServiceLayerBinder 能力注入
```

### 3.2 结构设计

```csharp
using System.Collections.Concurrent;

namespace LayerBase.DI;

/// <summary>
/// 单个 LayerRuntime 的 DI 根容器。
/// 
/// 一个 LayerRuntime 表示一个世界。
/// 因此 WorldServiceRoot 只管理当前世界内的 Singleton 服务，
/// 不允许跨 Runtime 共享由容器创建的 Singleton 实例。
/// </summary>
internal sealed class WorldServiceRoot : IDisposable
{
    /// <summary>
    /// 当前世界所属的 Runtime。
    /// 
    /// 第一阶段仅用于诊断和后续扩展，
    /// 不会把 Runtime 注入到每个服务对象中。
    /// </summary>
    private readonly LayerRuntime _runtime;

    /// <summary>
    /// 当前世界内的 Singleton 服务描述符表。
    /// 
    /// key 是服务类型，例如 typeof(IInventoryService)。
    /// value 是该服务的创建规则。
    /// </summary>
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _descriptors = new();

    /// <summary>
    /// 当前世界内已经创建的 Singleton 实例。
    /// 
    /// Lazy 用于保证并发解析时只创建一次。
    /// 如果两个线程同时解析同一个 Singleton，最终也只会有一个实例生效。
    /// </summary>
    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();

    /// <summary>
    /// 创建当前 Runtime 的世界级 DI 根容器。
    /// </summary>
    /// <param name="runtime">
    /// 当前容器所属的 LayerRuntime。
    /// </param>
    public WorldServiceRoot(LayerRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// 注册当前世界内的 Singleton 服务描述符。
    /// </summary>
    /// <param name="descriptor">
    /// 服务描述符。
    /// descriptor.ServiceType 是服务类型；
    /// descriptor.ImplType / Factory / Instance 决定如何创建服务。
    /// </param>
    public void Register(ServiceDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        _descriptors[descriptor.ServiceType] = descriptor;
    }

    /// <summary>
    /// 尝试查找当前世界内的 Singleton 描述符。
    /// </summary>
    /// <param name="serviceType">
    /// 要解析的服务类型。
    /// </param>
    /// <param name="descriptor">
    /// 找到的服务描述符。
    /// </param>
    /// <returns>
    /// true 表示当前世界注册过该 Singleton；
    /// false 表示没有注册。
    /// </returns>
    public bool TryGetDescriptor(
        Type serviceType,
        out ServiceDescriptor descriptor)
    {
        return _descriptors.TryGetValue(serviceType, out descriptor!);
    }

    /// <summary>
    /// 获取或创建当前世界内的 Singleton 实例。
    /// </summary>
    /// <param name="descriptor">
    /// 服务描述符。
    /// </param>
    /// <param name="ownerLayer">
    /// 当前解析请求所属的 Layer。
    /// 
    /// 注意：
    /// Singleton 是 Runtime 内唯一实例，
    /// 但第一阶段仍保留旧 ServiceLayerBinder.Attach 行为，
    /// 因此这里仍接收 ownerLayer。
    /// </param>
    /// <param name="factory">
    /// 实例创建函数。
    /// 只有该 Singleton 第一次被解析时才会调用。
    /// </param>
    /// <returns>
    /// 当前 Runtime 内唯一的 Singleton 实例。
    /// </returns>
    public object GetOrCreate(
        ServiceDescriptor descriptor,
        Layer? ownerLayer,
        Func<object> factory)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        var lazy = _instances.GetOrAdd(
            descriptor.ServiceType,
            _ => new Lazy<object>(
                valueFactory: factory,
                mode: LazyThreadSafetyMode.ExecutionAndPublication));

        object instance;

        try
        {
            instance = lazy.Value;
        }
        catch
        {
            // 如果创建失败，需要移除失败的 Lazy。
            // 否则后续解析会一直拿到同一个失败状态。
            _instances.TryRemove(descriptor.ServiceType, out _);
            throw;
        }

        // 第一阶段保留旧绑定逻辑。
        // 后续再把 ServiceLayerBinder 从运行期查表改为能力注入器。
        if (ownerLayer != null)
        {
            ServiceLayerBinder.Attach(instance, ownerLayer);
        }

        return instance;
    }

    /// <summary>
    /// 释放当前 Runtime 内已经创建的 Singleton 实例。
    /// </summary>
    public void Dispose()
    {
        foreach (var lazy in _instances.Values)
        {
            if (!lazy.IsValueCreated)
            {
                continue;
            }

            if (lazy.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _instances.Clear();
        _descriptors.Clear();
    }
}
```

## 4. LayerRuntime 持有 WorldServiceRoot

### 4.1 新增属性

在 `LayerRuntime` 中新增：

```csharp
using LayerBase.DI;

namespace LayerBase;

public sealed partial class LayerRuntime : IDisposable
{
    /// <summary>
    /// 当前 Runtime 的世界级 DI 根容器。
    /// 
    /// Singleton 服务在这里按 Runtime 隔离，
    /// 不再使用 static ServiceProvider._root。
    /// </summary>
    internal WorldServiceRoot Services { get; }
}
```

### 4.2 构造函数初始化

`LayerRuntime` 构造函数中初始化 `Services`：

```csharp
internal LayerRuntime(int id)
{
    _id = id;

    EventCenter = new EventCenter();

    // 每个 Runtime 都有独立的 DI 根容器。
    Services = new WorldServiceRoot(this);

    LayerHub.Internal_Register(this);
}
```

### 4.3 Dispose 释放

`LayerRuntime.Dispose()` 中释放当前世界内的 Singleton 服务：

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

    // 释放当前世界内的 Singleton 实例。
    Services.Dispose();

    _scheduler?.Dispose();
    _timer?.Dispose();

    DelayPublisherManager.Instance?.Clear();
    EventCenter.Reset();

    _context?.Dispose();

    LayerHub.Internal_Unregister(this);
}
```

本阶段暂不调整 `DelayPublisherManager.Instance`，因为本次目标只处理 DI 多世界化。

## 5. ServiceProvider 改造

### 5.1 删除内容

删除静态根容器：

```csharp
private static ServiceProvider _root = new();
```

删除全局重置方法：

```csharp
public static void ResetRoot()
```

删除解析过程中的 static root fallback：

```text
当前 Provider 找不到服务 -> 去 static root 找
```

改为：

```text
当前 Provider 找不到服务 -> 去当前 Runtime 的 WorldServiceRoot 找 Singleton
```

### 5.2 新构造参数

`ServiceProvider` 需要接收当前 Runtime 的 `WorldServiceRoot`：

```csharp
public ServiceProvider(
    WorldServiceRoot worldRoot,
    IEnumerable<ServiceDescriptor> descriptors,
    Layer? ownerLayer = null)
```

参数含义：

```text
worldRoot:
    当前 Runtime 的世界级 DI 根容器。

ownerLayer:
    当前 Provider 所属 Layer。
    用于服务实例创建后执行 ServiceLayerBinder.Attach。

descriptors:
    当前 Layer 收集到的服务描述符。
```

### 5.3 描述符归属

构造函数中按生命周期分流：

```text
Singleton:
    注册到 worldRoot

Scoped / Transient / Instance:
    保存在当前 ServiceProvider
```

示例：

```csharp
public ServiceProvider(
    WorldServiceRoot worldRoot,
    IEnumerable<ServiceDescriptor> descriptors,
    Layer? ownerLayer = null)
{
    _worldRoot = worldRoot ?? throw new ArgumentNullException(nameof(worldRoot));
    _ownerLayer = ownerLayer;

    if (descriptors == null)
    {
        throw new ArgumentNullException(nameof(descriptors));
    }

    foreach (var descriptor in descriptors)
    {
        if (descriptor.Lifetime == ServiceLifetime.Singleton)
        {
            _worldRoot.Register(descriptor);
            continue;
        }

        _map[descriptor.ServiceType] = descriptor;
    }
}
```

### 5.4 解析优先级

新的解析顺序：

```text
1. 当前 Layer Provider 的 _map
2. 当前 Runtime 的 WorldServiceRoot
3. 返回 null
```

也就是：

```csharp
private object? GetServiceInternal(
    Type serviceType,
    HashSet<Type> callstack)
{
    if (IsDisposed)
    {
        throw new ObjectDisposedException(nameof(ServiceProvider));
    }

    if (serviceType == null)
    {
        throw new ArgumentNullException(nameof(serviceType));
    }

    if (_map.TryGetValue(serviceType, out var localDescriptor))
    {
        return Resolve(localDescriptor, callstack);
    }

    if (_worldRoot.TryGetDescriptor(serviceType, out var singletonDescriptor))
    {
        return _worldRoot.GetOrCreate(
            singletonDescriptor,
            _ownerLayer,
            () => CreateInstance(singletonDescriptor, callstack));
    }

    return null;
}
```

### 5.5 Scoped 实例缓存

`Scoped` 实例仍然由当前 `ServiceProvider` 持有：

```csharp
private readonly ConcurrentDictionary<Type, Lazy<object>> _scopedInstances = new();
```

示例：

```csharp
private object GetOrCreateScoped(
    ServiceDescriptor descriptor,
    HashSet<Type> callstack)
{
    var lazy = _scopedInstances.GetOrAdd(
        descriptor.ServiceType,
        _ => new Lazy<object>(
            valueFactory: () =>
            {
                var localCallstack = new HashSet<Type>(callstack);
                return CreateInstance(descriptor, localCallstack);
            },
            mode: LazyThreadSafetyMode.ExecutionAndPublication));

    try
    {
        return lazy.Value;
    }
    catch
    {
        _scopedInstances.TryRemove(descriptor.ServiceType, out _);
        throw;
    }
}
```

### 5.6 Resolve 逻辑

```csharp
private object Resolve(
    ServiceDescriptor descriptor,
    HashSet<Type> callstack)
{
    var instance = descriptor.Lifetime switch
    {
        ServiceLifetime.Instance => descriptor.Instance!,
        ServiceLifetime.Scoped => GetOrCreateScoped(descriptor, callstack),
        ServiceLifetime.Transient => CreateInstance(descriptor, callstack),

        ServiceLifetime.Singleton => _worldRoot.GetOrCreate(
            descriptor,
            _ownerLayer,
            () => CreateInstance(descriptor, callstack)),

        _ => throw new NotSupportedException(
            $"Unsupported lifetime {descriptor.Lifetime}")
    };

    if (_ownerLayer != null)
    {
        ServiceLayerBinder.Attach(instance, _ownerLayer);
    }

    return instance;
}
```

注意：理论上 `Singleton` 不应该经常走到这里，因为构造函数已经把它注册到了 `WorldServiceRoot`。保留该分支主要是防御式处理。

## 6. Layer 构建 ServiceProvider 的修改点

原先创建 `ServiceProvider` 的地方需要改为传入当前 Runtime 的 `Services`。

旧形式可能类似：

```csharp
Provider = new ServiceProvider(descriptors, this);
```

新形式：

```csharp
Provider = new ServiceProvider(
    OwnerContext.Services,
    descriptors,
    this);
```

这里要求 `Layer` 在创建 `ServiceProvider` 前已经拥有 `OwnerContext`。

如果当前构建顺序中 `OwnerContext` 设置较晚，需要调整为：

```text
1. Layer 加入 LayerRuntime / LayerChain
2. 设置 Layer.OwnerContext
3. 设置 Layer.RouteIndex
4. 创建 Layer 的 ServiceProvider
5. 初始化自动订阅和服务解析
```

外部 API 不需要改变，仍然保持：

```csharp
var runtime = LayerHub
    .CreateLayers()
    .Push(new BattleLayer())
    .Push(new UiLayer())
    .Build();
```

本次只改变 `Build()` 内部的 DI 根容器来源。

## 7. ServiceCollection 补全 Singleton 注册 API

当前如果只支持：

```csharp
AddSingleton<TService>(TService instance)
```

则用户更容易注册外部共享实例，不利于多世界隔离。

因此建议补全：

```csharp
public interface IServiceCollection
{
    IServiceCollection Add(ServiceDescriptor descriptor);

    IServiceCollection AddSingleton<TService>(TService instance);

    IServiceCollection AddSingleton<TService, TImpl>()
        where TImpl : TService;

    IServiceCollection AddSingleton<TService>(
        Func<IServiceProvider, TService> factory);

    IServiceCollection AddTransient<TService, TImpl>()
        where TImpl : TService;

    IServiceCollection AddTransient<TService>(
        Func<IServiceProvider, TService> factory);

    IServiceCollection AddScoped<TService, TImpl>()
        where TImpl : TService;

    IServiceCollection AddScoped<TService>(
        Func<IServiceProvider, TService> factory);

    IReadOnlyList<ServiceDescriptor> ToDescriptors();
}
```

实现示例：

```csharp
public IServiceCollection AddSingleton<TService, TImpl>()
    where TImpl : TService
{
    return Add(ServiceDescriptor.Singleton<TService, TImpl>());
}

public IServiceCollection AddSingleton<TService>(
    Func<IServiceProvider, TService> factory)
{
    return Add(ServiceDescriptor.Singleton(factory));
}
```

推荐使用：

```csharp
services.AddSingleton<IInventoryService, InventoryService>();
```

或：

```csharp
services.AddSingleton<IInventoryService>(provider =>
{
    return new InventoryService(
        provider.GetService<IConfigService>()!);
});
```

不推荐在多世界场景中复用同一个外部实例：

```csharp
var shared = new InventoryService();
services.AddSingleton<IInventoryService>(shared);
```

除非用户明确希望多个 Runtime 共享该对象。

## 8. 生命周期语义表

| 生命周期 | 改造前语义 | 改造后语义 | 实例缓存位置 |
|---|---|---|---|
| Singleton | 可能进程全局共享 | 当前 LayerRuntime 内唯一 | WorldServiceRoot |
| Scoped | 当前 Layer 内唯一 | 当前 Layer 内唯一 | ServiceProvider |
| Transient | 每次创建 | 每次创建 | 不缓存 |
| Instance | 用户传入实例 | 用户传入实例 | ServiceDescriptor |

## 9. 第一阶段不处理的内容

本阶段只处理 DI 容器多世界化，不处理以下内容：

```text
ServiceLayerBinder 热路径改造
ConditionalWeakTable 删除
Send/Post 扩展方法删除
按能力注入 EventCenter / PostScheduler
DelayPublisherManager 世界化
Build 内部 Scheduler 初始化顺序调整
EventCenter 符号驻留改造
HandlerCircuit 冷路径迁移
```

这些内容应放到后续阶段，避免一次性改动过大。

## 10. 验收测试

### 10.1 Singleton 不跨 Runtime 共享

定义服务：

```csharp
public interface ICounter
{
    int Value { get; set; }
}

public sealed class Counter : ICounter
{
    public int Value { get; set; }
}
```

注册：

```csharp
services.AddSingleton<ICounter, Counter>();
```

测试：

```csharp
var worldA = LayerHub.CreateLayers()
    .Push(new TestLayer())
    .Build();

var worldB = LayerHub.CreateLayers()
    .Push(new TestLayer())
    .Build();

var counterA = worldA.For<TestLayer>().GetService<ICounter>();
var counterB = worldB.For<TestLayer>().GetService<ICounter>();

counterA.Value = 100;

Debug.Assert(counterB.Value == 0);
Debug.Assert(!ReferenceEquals(counterA, counterB));
```

### 10.2 同一 Runtime 内 Singleton 共享

```csharp
var runtime = LayerHub.CreateLayers()
    .Push(new LayerA())
    .Push(new LayerB())
    .Build();

var counterA = runtime.For<LayerA>().GetService<ICounter>();
var counterB = runtime.For<LayerB>().GetService<ICounter>();

Debug.Assert(ReferenceEquals(counterA, counterB));
```

### 10.3 Scoped 不跨 Layer 共享

```csharp
services.AddScoped<ICounter, Counter>();
```

测试：

```csharp
var runtime = LayerHub.CreateLayers()
    .Push(new LayerA())
    .Push(new LayerB())
    .Build();

var counterA = runtime.For<LayerA>().GetService<ICounter>();
var counterB = runtime.For<LayerB>().GetService<ICounter>();

Debug.Assert(!ReferenceEquals(counterA, counterB));
```

### 10.4 Transient 每次创建

```csharp
services.AddTransient<ICounter, Counter>();
```

测试：

```csharp
var layer = runtime.For<LayerA>();

var counter1 = layer.GetService<ICounter>();
var counter2 = layer.GetService<ICounter>();

Debug.Assert(!ReferenceEquals(counter1, counter2));
```

## 11. 修改清单

### 新增文件

```text
LayerBase/DI/WorldServiceRoot.cs
```

### 修改文件

```text
LayerBase/Application/LayerRuntime.cs
    + WorldServiceRoot Services
    + 构造函数初始化 Services
    + Dispose 中释放 Services

LayerBase/DI/ServiceProvider.cs
    - static ServiceProvider _root
    - ResetRoot()
    - static root fallback
    + 构造函数接收 WorldServiceRoot
    + Singleton 注册到 WorldServiceRoot
    + Singleton 实例由 WorldServiceRoot 缓存
    + Scoped 实例继续由当前 ServiceProvider 缓存

LayerBase/DI/ServiceCollection.cs
    + AddSingleton<TService, TImpl>()
    + AddSingleton<TService>(Func<IServiceProvider, TService>)

LayerBase/Layer/Layer.cs 或 Layer 构建 Provider 的位置
    new ServiceProvider(runtime.Services, descriptors, this)
```

## 12. 最终结果

改造完成后，DI 容器结构变为：

```text
LayerRuntime
├── WorldServiceRoot
│   ├── Singleton descriptors
│   └── Singleton instances
│
├── Layer A
│   └── ServiceProvider
│       ├── Scoped descriptors
│       └── Scoped instances
│
└── Layer B
    └── ServiceProvider
        ├── Scoped descriptors
        └── Scoped instances
```

多个 Runtime 之间：

```text
WorldA.LayerRuntime.Services != WorldB.LayerRuntime.Services
WorldA.SingletonInstance != WorldB.SingletonInstance
```

这一步完成后，DI 就从单世界静态根容器，改造成了按 `LayerRuntime` 隔离的多世界 DI 容器。
