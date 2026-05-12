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

    public LayerRuntime Runtime => _runtime;

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

        if (_descriptors.TryGetValue(descriptor.ServiceType, out var existing))
        {
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
        Type                   serviceType,
        out ServiceDescriptor? descriptor)
    {
        return _descriptors.TryGetValue(serviceType, out descriptor);
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
        Layers.Layer?     ownerLayer,
        Func<object>      factory)
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