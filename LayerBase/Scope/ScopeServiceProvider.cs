using System.Threading;
using LayerBase.DI;

namespace LayerBase.Scope;

internal sealed class ScopeServiceProvider : LayerBase.DI.IServiceProvider, IDisposable
{
    private static int s_generation;

    private readonly object[] _instances;
    private readonly Type[] _instanceTypes;
    private readonly ScopeServiceLookupEntry[] _lookup;
    private readonly int _generation;
    private bool _disposed;

    public ScopeServiceProvider(object[] instances)
    {
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
        _instanceTypes = new Type[instances.Length];
        _generation = Interlocked.Increment(ref s_generation);

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

        if (ScopeServiceSlotCache<T>.TryGet(_generation, _instances, out T? cached))
        {
            return cached;
        }

        int slot = ResolveSlot(typeof(T));
        if (slot < 0)
        {
            throw new InvalidOperationException($"Scope service not registered: {typeof(T)}");
        }

        ScopeServiceSlotCache<T>.Store(_generation, slot);
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
            AddLookup(entries, instanceTypes[i], i);
        }

        for (int i = 0; i < instanceTypes.Length; i++)
        {
            Type type = instanceTypes[i];
            Type? baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                AddLookup(entries, baseType, i);
                baseType = baseType.BaseType;
            }

            Type[] interfaces = type.GetInterfaces();
            for (int j = 0; j < interfaces.Length; j++)
            {
                AddLookup(entries, interfaces[j], i);
            }
        }

        return entries.ToArray();
    }

    private static void AddLookup(List<ScopeServiceLookupEntry> entries, Type type, int slot)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Type == type)
            {
                return;
            }
        }

        entries.Add(new ScopeServiceLookupEntry(type, slot));
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

internal static class ScopeServiceSlotCache<T>
{
    private static int s_generation;
    private static int s_slot = -1;

    public static bool TryGet(int generation, object[] instances, out T value)
    {
        int slot = Volatile.Read(ref s_slot);
        if (Volatile.Read(ref s_generation) == generation &&
            (uint)slot < (uint)instances.Length &&
            instances[slot] is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    public static void Store(int generation, int slot)
    {
        Volatile.Write(ref s_slot, slot);
        Volatile.Write(ref s_generation, generation);
    }
}
