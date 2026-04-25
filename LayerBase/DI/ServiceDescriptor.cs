using System;

namespace LayerBase.DI;

public enum ServiceLifetime
{
    Singleton, // Globally unique
    Scoped,    // Layer unique
    Transient, // Created every time
    Instance   // Externally pre-created instance (usually acts as Scoped)
}

public sealed class ServiceDescriptor
{
    public ServiceDescriptor(Type                            serviceType, Type?   implType, ServiceLifetime lifetime,
                             Func<IServiceProvider, object>? factory,     object? instance,
                             int                             registrationScopeId = 0)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ImplType = implType;
        Lifetime = lifetime;
        Factory = factory;
        Instance = instance;
        RegistrationScopeId = registrationScopeId;
    }

    public Type ServiceType { get; }
    public Type? ImplType { get; }
    public ServiceLifetime Lifetime { get; }
    public Func<IServiceProvider, object>? Factory { get; }
    public object? Instance { get; }
    public int RegistrationScopeId { get; }

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

    internal ServiceDescriptor WithRegistrationScope(int registrationScopeId)
    {
        if (RegistrationScopeId == registrationScopeId) return this;

        return new ServiceDescriptor(ServiceType, ImplType, Lifetime, Factory, Instance, registrationScopeId);
    }
}

