using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Scope.DI;
using LayerBase.Scope.Resources;
namespace LayerBase.Scope;

internal sealed class ScopeServiceProvider : LayerBase.DI.IServiceProvider, IDisposable
{
    private readonly object[] _instances;
    private readonly Type[] _instanceTypes;
    private readonly ScopeServiceLookupEntry[] _lookup;
    private bool _disposed;

    public ScopeServiceProvider(object[] instances)
    {
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
        _instanceTypes = new Type[instances.Length];

        for (int i = 0; i < instances.Length; i++)
        {
            _instanceTypes[i] = instances[i]?.GetType()
                                ?? throw new ArgumentException("Scope service provider does not accept null instances.", nameof(instances));
        }

        _lookup = BuildLookup(_instanceTypes);
    }

    public object? GetService(Type serviceType)
    {
        ThrowIfDisposed();

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        int slot = ResolveSlot(serviceType);
        return slot >= 0 ? _instances[slot] : null;
    }

    public T Get<T>()
    {
        ThrowIfDisposed();

        int slot = ResolveSlot(typeof(T));
        if (slot < 0)
        {
            throw new InvalidOperationException($"Scope service not registered: {typeof(T)}");
        }

        return (T)_instances[slot];
    }

    public T GetAt<T>(int slot) where T : class
    {
        ThrowIfDisposed();

        if ((uint)slot >= (uint)_instances.Length)
        {
            throw new InvalidOperationException(
                $"Scope service slot {slot} is outside provider object table length {_instances.Length}.");
        }

        return (T)_instances[slot];
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private int ResolveSlot(Type serviceType)
    {
        for (int i = 0; i < _lookup.Length; i++)
        {
            if (_lookup[i].Type == serviceType)
            {
                return _lookup[i].Slot;
            }
        }

        return -1;
    }

    private static ScopeServiceLookupEntry[] BuildLookup(Type[] instanceTypes)
    {
        var entries = new List<ScopeServiceLookupEntry>(instanceTypes.Length);

        for (int i = 0; i < instanceTypes.Length; i++)
        {
            AddLookup(entries, instanceTypes, instanceTypes[i], i);
        }

        for (int i = 0; i < instanceTypes.Length; i++)
        {
            Type type = instanceTypes[i];
            Type? baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                AddLookup(entries, instanceTypes, baseType, i);
                baseType = baseType.BaseType;
            }

            Type[] interfaces = type.GetInterfaces();
            for (int j = 0; j < interfaces.Length; j++)
            {
                AddLookup(entries, instanceTypes, interfaces[j], i);
            }
        }

        return entries.ToArray();
    }

    private static void AddLookup(List<ScopeServiceLookupEntry> entries, Type[] instanceTypes, Type type, int slot)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Type == type)
            {
                if (entries[i].Slot != slot && IsAmbiguousServiceContract(type))
                {
                    Type first = instanceTypes[entries[i].Slot];
                    Type second = instanceTypes[slot];
                    throw new InvalidOperationException(
                        $"Scope service contract '{type.FullName}' is ambiguous between '{first.FullName}' and '{second.FullName}'.");
                }

                return;
            }
        }

        entries.Add(new ScopeServiceLookupEntry(type, slot));
    }

    private static bool IsAmbiguousServiceContract(Type type)
    {
        if (!type.IsInterface)
        {
            return false;
        }

        return type != typeof(IService) &&
               type != typeof(IInitializable) &&
               type != typeof(IDisposable) &&
               type != typeof(ILayerContext) &&
               type != typeof(IScopeObjectBindingAccessor) &&
               type != typeof(IGeneratedScopeMount) &&
               type != typeof(IGeneratedScopeMountMetadata) &&
               type != typeof(IGeneratedScopeResourcePublisher) &&
               type != typeof(IGeneratedScopeResourceConsumer) &&
               type != typeof(IGeneratedScopeResourceExportMetadata) &&
               type != typeof(IGeneratedScopeResourceImportMetadata);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopeServiceProvider));
        }
    }
}

internal readonly struct ScopeServiceLookupEntry
{
    public ScopeServiceLookupEntry(Type type, int slot)
    {
        Type = type;
        Slot = slot;
    }

    public Type Type { get; }

    public int Slot { get; }
}
