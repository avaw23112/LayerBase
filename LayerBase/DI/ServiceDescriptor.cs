namespace LayerBase.DI
{
    public sealed class ServiceDescriptor
    {
        public Type ServiceType { get; }
        public Type? ImplType { get; }
        public ServiceLifetime Lifetime { get; }
        public Func<IServiceProvider, object>? Factory { get; }
        public object? Instance { get; }

        public static ServiceDescriptor Singleton<TService, TImpl>() where TImpl : TService
            => new ServiceDescriptor(typeof(TService), typeof(TImpl), ServiceLifetime.Singleton, null, null);
        public static ServiceDescriptor Singleton<TService>(TService instance)
            => new ServiceDescriptor(typeof(TService), null, ServiceLifetime.Instance, null, instance!);
        public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> factory)
            => new ServiceDescriptor(typeof(TService), null, ServiceLifetime.Singleton, sp => factory(sp)!, null);

        public static ServiceDescriptor Transient<TService, TImpl>() where TImpl : TService
            => new ServiceDescriptor(typeof(TService), typeof(TImpl), ServiceLifetime.Transient, null, null);

        public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, TService> factory)
            => new ServiceDescriptor(typeof(TService), null, ServiceLifetime.Transient, sp => factory(sp)!, null);

        public static ServiceDescriptor LayerScoped<TService, TImpl>() where TImpl : TService
            => new ServiceDescriptor(typeof(TService), typeof(TImpl), ServiceLifetime.Scoped, null, null);

        public static ServiceDescriptor LayerScoped<TService>(Func<IServiceProvider, TService> factory)
            => new ServiceDescriptor(typeof(TService), null, ServiceLifetime.Scoped, sp => factory(sp)!, null);

        public ServiceDescriptor(Type serviceType, Type? implType, ServiceLifetime lifetime, Func<IServiceProvider, object>? factory, object? instance)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ImplType = implType;
            Lifetime = lifetime;
            Factory = factory;
            Instance = instance;
        }
    }
}
