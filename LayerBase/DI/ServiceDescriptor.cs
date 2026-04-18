namespace LayerBase.DI;

public enum ServiceLifetime
{
    Singleton, // 全局唯一
    Scoped,    // 层级唯一
    Transient, // 每次创建
    Instance   // 外部预创建实例 (通常表现同 Scoped)
}

public sealed class ServiceDescriptor
{
    public ServiceDescriptor(Type                            serviceType, Type?   implType, ServiceLifetime lifetime,
                             Func<IServiceProvider, object>? factory,     object? instance)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ImplType = implType;
        Lifetime = lifetime;
        Factory = factory;
        Instance = instance;
    }

    public Type ServiceType { get; }
    public Type? ImplType { get; }
    public ServiceLifetime Lifetime { get; }
    public Func<IServiceProvider, object>? Factory { get; }
    public object? Instance { get; }

    public static ServiceDescriptor Singleton<TService, TImpl>() where TImpl : TService
    {
        return new ServiceDescriptor(typeof(TService), typeof(TImpl), ServiceLifetime.Singleton, null, null);
    }

    public static ServiceDescriptor Singleton<TService>(TService instance)
    {
        return new ServiceDescriptor(typeof(TService), null, ServiceLifetime.Instance, null, instance!);
    }

    public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> factory)
    {
        return new ServiceDescriptor(typeof(TService), null, ServiceLifetime.Singleton, sp => factory(sp)!, null);
    }

    public static ServiceDescriptor Transient<TService, TImpl>() where TImpl : TService
    {
        return new ServiceDescriptor(typeof(TService), typeof(TImpl), ServiceLifetime.Transient, null, null);
    }

    public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, TService> factory)
    {
        return new ServiceDescriptor(typeof(TService), null, ServiceLifetime.Transient, sp => factory(sp)!, null);
    }

    public static ServiceDescriptor LayerScoped<TService, TImpl>() where TImpl : TService
    {
        return new ServiceDescriptor(typeof(TService), typeof(TImpl), ServiceLifetime.Scoped, null, null);
    }

    public static ServiceDescriptor LayerScoped<TService>(Func<IServiceProvider, TService> factory)
    {
        return new ServiceDescriptor(typeof(TService), null, ServiceLifetime.Scoped, sp => factory(sp)!, null);
    }
}