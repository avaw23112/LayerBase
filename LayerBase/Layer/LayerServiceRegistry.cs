using System.Collections.Concurrent;
using System.Reflection;

namespace LayerBase.Layers;

public static class LayerServiceRegistry
{
    private static readonly ConcurrentDictionary<Type, Action<Layer>> s_registrations = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo?> s_initMethods = new();

    public static void Register(Type layerType, Action<Layer> registrar)
    {
        if (layerType == null) throw new ArgumentNullException(nameof(layerType));
        if (registrar == null) throw new ArgumentNullException(nameof(registrar));

        s_registrations.AddOrUpdate(layerType, registrar, (_, existing) => existing + registrar);
    }

    public static void Reset()
    {
        s_registrations.Clear();
        s_initMethods.Clear();
    }
}