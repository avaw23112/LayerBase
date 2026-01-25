using System;
using System.Collections.Concurrent;
using System.Reflection;
using LayerBase.Layers;

namespace LayerBase.DI
{
    public sealed class ServiceProvider : IServiceProvider, IDisposable
    {
        private readonly Dictionary<Type, ServiceDescriptor> _map;
        private readonly ConcurrentDictionary<Type, object> _singletons = new ConcurrentDictionary<Type, object>();
        private readonly object _sync = new object();
        private readonly Layer? _ownerLayer;
        private static readonly ServiceProvider _root = new ServiceProvider();
        private bool _disposed;

        public bool IsDisposed => _disposed;

        internal ServiceProvider()
        {
            _map = new Dictionary<Type, ServiceDescriptor>();
            _ownerLayer = null;
        }
        public ServiceProvider(IEnumerable<ServiceDescriptor> descriptors, Layer? ownerLayer = null)
        {
            if (descriptors == null) throw new ArgumentNullException(nameof(descriptors));

            _map = new Dictionary<Type, ServiceDescriptor>();
            _ownerLayer = ownerLayer;
            foreach (var d in descriptors)
            {
                if (d.Lifetime == ServiceLifetime.Singleton)
                {
                    _root._map[d.ServiceType] = d;
                }
                else
                {
                    _map[d.ServiceType] = d;
                }
            }
        }

        public object? GetService(Type serviceType)
        {
            return GetServiceInternal(serviceType, new HashSet<Type>());
        }

        public T Get<T>()
        {
            var service = GetService(typeof(T));
            if (service == null)
                throw new InvalidOperationException($"Service not registered: {typeof(T)}");
            return (T)service;
        }

        private object? GetServiceInternal(Type serviceType, HashSet<Type> callstack)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceProvider));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            if (!_map.TryGetValue(serviceType, out var desc))
            {
                if (_root != this && _root._map.TryGetValue(serviceType, out var parentDesc))
                    return _root.Resolve(parentDesc, callstack);
                return null;
            }

            return Resolve(desc, callstack);
        }

        private object Resolve(ServiceDescriptor desc, HashSet<Type> callstack)
        {
            var instance = desc.Lifetime switch
            {
                ServiceLifetime.Singleton => _root.GetOrCreateSingleton(desc, callstack),
                ServiceLifetime.Transient => CreateInstance(desc, callstack),
                ServiceLifetime.Scoped => GetOrCreateSingleton(desc, callstack),
                _ => throw new NotSupportedException($"Unsupported lifetime {desc.Lifetime}")
            };
            return AttachLayerIfNeeded(instance, desc.Lifetime);
        }

        private object GetOrCreateSingleton(ServiceDescriptor desc, HashSet<Type> callstack)
        {
            return _singletons.GetOrAdd(desc.ServiceType, _ =>
            {
                lock (_sync)
                {
                    if (_singletons.TryGetValue(desc.ServiceType, out var existing))
                        return existing;
                    return CreateInstance(desc, callstack);
                }
            });
        }

        private object CreateInstance(ServiceDescriptor desc, HashSet<Type> callstack)
        {
            if (desc.Factory != null)
                return desc.Factory(this);

            if (desc.ImplType == null)
                throw new InvalidOperationException($"No implementation for {desc.ServiceType}");

            if (!callstack.Add(desc.ImplType))
                throw new InvalidOperationException($"Circular dependency detected: {desc.ImplType}");

            try
            {
                var ctor = desc.ImplType
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .OrderByDescending(c => c.GetParameters().Length)
                    .FirstOrDefault();

                if (ctor == null)
                    throw new InvalidOperationException($"No accessible constructor found for {desc.ImplType}");

                var parameters = ctor.GetParameters();
                var args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var dep = GetServiceInternal(parameters[i].ParameterType, callstack);
                    if (dep == null)
                    {
                        throw new InvalidOperationException($"Unable to resolve dependency {parameters[i].ParameterType} for {desc.ImplType}");
                    }
                    args[i] = dep;
                }

                var instance = ctor.Invoke(args);
                InjectMembers(instance);
                return instance;
            }
            finally
            {
                callstack.Remove(desc.ImplType);
            }
        }

        private void InjectMembers(object instance)
        {
            var t = instance.GetType();

            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.GetCustomAttribute<InjectAttribute>() == null) continue;
                var dep = GetService(f.FieldType);
                if (dep != null) f.SetValue(instance, dep);
            }

            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.GetCustomAttribute<InjectAttribute>() == null || !p.CanWrite) continue;
                var dep = GetService(p.PropertyType);
                if (dep != null) p.SetValue(instance, dep, null);
            }
        }

        private object AttachLayerIfNeeded(object instance, ServiceLifetime lifetime)
        {
            if (_ownerLayer != null && lifetime != ServiceLifetime.Singleton && instance is IService service)
            {
                ServiceLayerBinder.Attach(service, _ownerLayer);
            }

            return instance;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var obj in _singletons.Values)
            {
                if (obj is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}
